// =============================================================================
// mod-reader.ts — SPT mod 元数据读取（package.json + DLL IModMetadata via helper）
//
// 4.1 server mod = user/mods/<mod>/ 下顶层 DLL（AsmResolver 读 IModMetadata）。
// 本模块：listServerMods 识别"目录含顶层 .dll"即 mod；DLL 元数据由 .NET helper
// （helper/spt-metadata-reader）子进程读取。无 DLL 时回退到 package.json（旧版/兼容）。
// =============================================================================

import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { closeSync, openSync, readSync, readdirSync, readFileSync, statSync } from "node:fs";
import { basename, dirname, extname, join, relative, sep } from "node:path";

import type {
  ClientModMetadata,
  ModFileEntry,
  ModMetadata,
  ModSummary,
  ModType,
  ServerModMetadata,
} from "./types.js";

// -----------------------------------------------------------------------------
// helper 定位：优先用环境变量 SPT_MCP_HELPER，其次相对本模块的 helper 构建产物
// -----------------------------------------------------------------------------

function helperPath(): string | null {
  const env = process.env.SPT_MCP_HELPER;
  if (env && env.length > 0) return env;
  // 尝试常见相对位置（从 dist/ 或 src/ 上溯到 helper/bin/Release）
  const candidates = [
    new URL("../helper/bin/Release/spt-metadata-reader.exe", import.meta.url).pathname,
    new URL("../../helper/bin/Release/spt-metadata-reader.exe", import.meta.url).pathname,
  ];
  for (const c of candidates) {
    // pathname 在 Windows 下是 /E:/... 形式
    const p = c.replace(/^\/([A-Za-z]:)/, "$1");
    try {
      if (statSync(p).isFile()) return p;
    } catch {
      // continue
    }
  }
  return null;
}

export interface DllMetadata {
  path: string;
  ok: boolean;
  name?: string;
  guid?: string;
  author?: string;
  version?: string;
  sptVersion?: string;
  license?: string;
  modDependencies?: string[];
  error?: string;
}

/** 用 .NET helper 批量读 DLL 元数据（子进程同步调用） */
export function readDllMetadata(dllPaths: string[]): DllMetadata[] {
  if (dllPaths.length === 0) return [];
  const helper = helperPath();
  if (!helper) {
    return dllPaths.map((p) => ({ path: p, ok: false, error: "helper not found (set SPT_MCP_HELPER)" }));
  }
  try {
    const stdout = execFileSync(helper, dllPaths, {
      encoding: "utf8",
      maxBuffer: 64 * 1024 * 1024,
      windowsHide: true,
    });
    const parsed: unknown = JSON.parse(stdout);
    if (!Array.isArray(parsed)) return [];
    return parsed as DllMetadata[];
  } catch (error) {
    const msg = error instanceof Error ? error.message : String(error);
    return dllPaths.map((p) => ({ path: p, ok: false, error: `helper failed: ${msg}` }));
  }
}

// -----------------------------------------------------------------------------
// 工具函数
// -----------------------------------------------------------------------------

/** 相对路径统一为正斜杠，保证跨平台输出一致 */
export function toPosix(p: string): string {
  return p.split(sep).join("/");
}

/** 递归收集目录下所有文件路径（跳过符号链接） */
export function walkFiles(dir: string): string[] {
  const results: string[] = [];
  let entries;
  try {
    entries = readdirSync(dir, { withFileTypes: true });
  } catch {
    return results;
  }
  for (const entry of entries) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) {
      results.push(...walkFiles(full));
    } else if (entry.isFile()) {
      results.push(full);
    }
  }
  return results;
}

export function isDirectory(p: string): boolean {
  try {
    return statSync(p).isDirectory();
  } catch {
    return false;
  }
}

export function isFile(p: string): boolean {
  try {
    return statSync(p).isFile();
  } catch {
    return false;
  }
}

// -----------------------------------------------------------------------------
// package.json 解析（旧版/兼容回退）
// -----------------------------------------------------------------------------

export function readPackageJson(modPath: string): Record<string, unknown> | null {
  let text: string;
  try {
    text = readFileSync(join(modPath, "package.json"), "utf8");
  } catch {
    return null;
  }
  try {
    const parsed: unknown = JSON.parse(text);
    if (typeof parsed !== "object" || parsed === null || Array.isArray(parsed)) {
      return null;
    }
    return parsed as Record<string, unknown>;
  } catch {
    return null;
  }
}

/**
 * 提取依赖列表。package.json 中 dependencies 可能是对象映射
 * （{ "modA": "1.0.0" }）也可能是字符串数组，统一归一为数组。
 */
export function extractDependencies(pkg: Record<string, unknown>): string[] {
  const raw = pkg.dependencies;
  if (Array.isArray(raw)) {
    return raw.filter((x): x is string => typeof x === "string");
  }
  if (typeof raw === "object" && raw !== null) {
    return Object.keys(raw as Record<string, unknown>);
  }
  return [];
}

export function readServerModMetadataFromPkg(modPath: string): ServerModMetadata | null {
  const pkg = readPackageJson(modPath);
  if (!pkg) return null;
  return {
    type: "server",
    path: modPath,
    name: typeof pkg.name === "string" && pkg.name.length > 0 ? pkg.name : basename(modPath),
    version: typeof pkg.version === "string" ? pkg.version : undefined,
    guid: typeof pkg.guid === "string" && pkg.guid.length > 0 ? pkg.guid : undefined,
    dependencies: extractDependencies(pkg),
    typePriority: typeof pkg.typePriority === "number" ? pkg.typePriority : 0,
    sptVersion: typeof pkg.sptVersion === "string" ? pkg.sptVersion : undefined,
    compatibleVersion: typeof pkg.compatibleVersion === "string" ? pkg.compatibleVersion : undefined,
    packageJson: pkg,
  };
}

/** 兼容旧导出名（重构前名为 readServerModMetadata）；统一指向 FromPkg 实现 */
export const readServerModMetadata: typeof readServerModMetadataFromPkg = readServerModMetadataFromPkg;

/** 从 helper 输出构造 ServerModMetadata（DLL 来源，无 package.json） */
export function serverModFromDll(dllMeta: DllMetadata, modPath: string): ServerModMetadata {
  return {
    type: "server",
    path: modPath,
    name: dllMeta.name ?? basename(modPath),
    version: dllMeta.version,
    guid: dllMeta.guid,
    dependencies: dllMeta.modDependencies ?? [],
    typePriority: 0,
    sptVersion: dllMeta.sptVersion,
    packageJson: null,
    dllPath: dllMeta.path,
  };
}

export function readClientModMetadata(dllPath: string): ClientModMetadata | null {
  if (extname(dllPath).toLowerCase() !== ".dll" || !isFile(dllPath)) {
    return null;
  }
  const fileName = basename(dllPath);
  return {
    type: "client",
    path: dllPath,
    modPath: dirname(dllPath),
    name: fileName.replace(/\.dll$/i, ""),
    fileName,
    dependencies: [],
  };
}

/**
 * 读单个 mod 元数据。server 优先 DLL（4.1），回退嵌套 user/mods（3.11）或 package.json；
 * 纯 client（只有顶层 DLL）也识别。client 接受 DLL 路径，若传入目录则取其中第一个 DLL。
 */
export function readModMetadata(modPath: string, type: ModType): ModMetadata | null {
  if (type === "server") {
    // 4.1：顶层 DLL（含 IModMetadata 即 server）
    const topDll = findTopLevelDll(modPath);
    if (topDll) {
      const metas = readDllMetadata([topDll]);
      if (metas.length > 0 && metas[0].ok) {
        return serverModFromDll(metas[0], modPath);
      }
      // 顶层 DLL 存在但无 IModMetadata -> 纯 client mod
      const client = readClientModMetadata(topDll);
      if (client) {
        return { ...client, modPath };
      }
    }
    // 3.11 嵌套结构：user/mods/<name>/package.json
    const nested = listNestedServerMods(modPath);
    if (nested.length > 0) {
      const first = nested[0];
      // 读完整元数据
      return readServerModMetadataFromPkg(first.path);
    }
    // 3.11 纯 client：BepInEx/plugins/ 下有 DLL，但无 server 部分
    const anyDll = findAnyDll(modPath);
    if (anyDll) {
      const client = readClientModMetadata(anyDll);
      if (client) {
        return { ...client, modPath };
      }
    }
    // 顶层 package.json（旧式 server）
    return readServerModMetadataFromPkg(modPath);
  }
  let dll = modPath;
  if (isDirectory(modPath)) {
    const dlls = listClientMods(modPath);
    if (dlls.length === 0) return null;
    dll = dlls[0].path;
  }
  return readClientModMetadata(dll);
}

// -----------------------------------------------------------------------------
// 目录扫描
// -----------------------------------------------------------------------------

/** 返回 mod 目录顶层（非递归）的第一个 .dll（4.1 server mod 结构） */
export function findTopLevelDll(modPath: string): string | null {
  let entries;
  try {
    entries = readdirSync(modPath, { withFileTypes: true });
  } catch {
    return null;
  }
  for (const entry of entries) {
    if (!entry.isFile()) continue;
    if (extname(entry.name).toLowerCase() !== ".dll") continue;
    return join(modPath, entry.name);
  }
  return null;
}

/** 递归找 mod 目录内第一个 .dll（3.11 client mod 在 BepInEx/plugins/ 子目录） */
export function findAnyDll(modPath: string): string | null {
  for (const f of walkFiles(modPath)) {
    if (extname(f).toLowerCase() !== ".dll") continue;
    return f;
  }
  return null;
}

/**
 * 扫描目录列出所有 server mod。
 * 4.1：目录含顶层 .dll 即 mod（元数据从 DLL 读）；回退：含 package.json 的目录；
 * 3.11 混合结构：mod 目录下含 user/mods/<name>/package.json 的嵌套 server mod。
 */
export function listServerMods(basePath: string): ModSummary[] {
  const out: ModSummary[] = [];
  let entries;
  try {
    entries = readdirSync(basePath, { withFileTypes: true });
  } catch {
    return out;
  }

  const dllModPaths: string[] = [];
  for (const entry of entries) {
    if (!entry.isDirectory()) continue;
    const modPath = join(basePath, entry.name);

    const topDll = findTopLevelDll(modPath);
    if (topDll) {
      dllModPaths.push(modPath);
      continue;
    }

    const meta = readServerModMetadataFromPkg(modPath);
    if (meta) {
      out.push({
        name: meta.name,
        version: meta.version,
        guid: meta.guid,
        dependencies: meta.dependencies,
        path: modPath,
        type: "server",
      });
      continue;
    }

    // 3.11 混合结构：mod 根目录无 package.json，但含 user/mods/<name>/package.json
    // （客户端 BepInEx 部分在 mod 根，服务端 TS 部分在 user/mods/ 下）
    const nestedServers = listNestedServerMods(modPath);
    if (nestedServers.length > 0) {
      // 嵌套 server mod 归入该 mod 目录（视为一个 mod 条目）
      out.push(...nestedServers);
    }
  }

  // 批量读 DLL 元数据（一次子进程调用）
  const dllPaths = dllModPaths.map((p) => findTopLevelDll(p)!).filter(Boolean);
  const metas = readDllMetadata(dllPaths);
  const metaByPath = new Map(metas.map((m) => [m.path, m]));
  for (const modPath of dllModPaths) {
    const dll = findTopLevelDll(modPath);
    const meta = dll ? metaByPath.get(dll) : undefined;
    if (meta && meta.ok) {
      const sm = serverModFromDll(meta, modPath);
      out.push({
        name: sm.name,
        version: sm.version,
        guid: sm.guid,
        dependencies: sm.dependencies,
        path: modPath,
        type: "server",
      });
    } else {
      // DLL 存在但读不到元数据：以目录名兜底列出
      out.push({
        name: basename(modPath),
        path: modPath,
        dependencies: [],
        type: "server",
      });
    }
  }

  return out.sort((a, b) => a.name.localeCompare(b.name));
}

/**
 * 扫描 3.11 混合结构 mod 的嵌套 server mod：user/mods/<name>/package.json。
 * 一个 mod 目录可能含多个嵌套 server mod（罕见），返回所有找到的。
 */
export function listNestedServerMods(modPath: string): ModSummary[] {
  const out: ModSummary[] = [];
  const userModsDir = join(modPath, "user", "mods");
  if (!isDirectory(userModsDir)) return out;

  let nestedEntries;
  try {
    nestedEntries = readdirSync(userModsDir, { withFileTypes: true });
  } catch {
    return out;
  }

  for (const entry of nestedEntries) {
    if (!entry.isDirectory()) continue;
    const nestedPath = join(userModsDir, entry.name);
    const meta = readServerModMetadataFromPkg(nestedPath);
    if (!meta) continue;
    out.push({
      name: meta.name,
      version: meta.version,
      guid: meta.guid,
      dependencies: meta.dependencies,
      path: nestedPath,
      type: "server",
    });
  }
  return out.sort((a, b) => a.name.localeCompare(b.name));
}

/** 扫描目录列出所有 client mod：递归找 .dll，一个 DLL 一个条目 */
export function listClientMods(basePath: string): ModSummary[] {
  const out: ModSummary[] = [];
  for (const f of walkFiles(basePath)) {
    if (extname(f).toLowerCase() !== ".dll") continue;
    const meta = readClientModMetadata(f);
    if (!meta) continue;
    out.push({
      name: meta.name,
      dependencies: meta.dependencies,
      path: meta.path,
      type: "client",
    });
  }
  return out.sort((a, b) => a.name.localeCompare(b.name));
}

/**
 * 递归列出 mod 目录全部文件（相对路径 + 大小 + SHA-1 内容指纹）。
 * contentHash 用于冲突检测的内容比较：同路径文件 hash 相同 = 无害重复，不同 = 真冲突。
 */
export function scanModFiles(modPath: string): ModFileEntry[] {
  const out: ModFileEntry[] = [];
  for (const f of walkFiles(modPath)) {
    let size = 0;
    try {
      size = statSync(f).size;
    } catch {
      continue;
    }
    out.push({
      relativePath: toPosix(relative(modPath, f)),
      sizeBytes: size,
      contentHash: sha1File(f),
    });
  }
  return out.sort((a, b) => a.relativePath.localeCompare(b.relativePath));
}

/** 计算文件 SHA-1（流式，大文件安全）；失败返回 null */
export function sha1File(filePath: string): string | null {
  try {
    const crypto = createHash("sha1");
    const fd = openSync(filePath, "r");
    const buf = Buffer.alloc(64 * 1024);
    try {
      let bytes;
      while ((bytes = readSync(fd, buf, 0, buf.length, null)) > 0) {
        crypto.update(buf.subarray(0, bytes));
      }
    } finally {
      closeSync(fd);
    }
    return crypto.digest("hex");
  } catch {
    return null;
  }
}

// -----------------------------------------------------------------------------
// 配置文件 key 提取（冲突检测用）
// -----------------------------------------------------------------------------

export interface ConfigFileKeys {
  /** mod 内相对路径（正斜杠） */
  filename: string;
  /** JSON 顶层 key 列表 */
  keys: string[];
}

/** 收集 mod 目录内所有可解析 JSON 配置文件的顶层 key（跳过 package.json） */
export function readConfigKeys(modPath: string): ConfigFileKeys[] {
  const out: ConfigFileKeys[] = [];
  for (const f of walkFiles(modPath)) {
    if (extname(f).toLowerCase() !== ".json") continue;
    if (basename(f).toLowerCase() === "package.json") continue;
    let parsed: unknown;
    try {
      parsed = JSON.parse(readFileSync(f, "utf8"));
    } catch {
      continue;
    }
    if (typeof parsed !== "object" || parsed === null || Array.isArray(parsed)) continue;
    const keys = Object.keys(parsed as Record<string, unknown>);
    if (keys.length === 0) continue;
    out.push({ filename: toPosix(relative(modPath, f)), keys });
  }
  return out.sort((a, b) => a.filename.localeCompare(b.filename));
}
