// =============================================================================
// kb-registry.mjs — 知识库注册表的唯一解析实现
//
// 无依赖纯 ESM：脚本（scripts/kb/build-index.mjs）与 spt-mcp（src/kb/registry.ts）
// 各自相对导入本模块，路径解析 / 白名单遍历 / 排除匹配不再各自计算、不会互相漂移。
//
// 契约（与 shared/runtime-layout.mjs 同族）：
//   - 不抛错，返回逐库状态（path / source / ok / reason）。
//   - env 未设 -> 默认派生（仓库根相对）+ 存在性校验。
//   - env 显式设置但校验失败 -> ok:false + reason，**不回退**（显式契约不可被悄悄替换）。
//   - root 为绝对路径（盘符 / UNC / 前导斜杠）时按原样；否则相对仓库根。
//   - index 以 './' 开头 => 相对已解析的 root（跟随 root 覆盖）；否则绝对或仓库根相对。
//
// 为什么单列一个模块：注册表是「加库 = 加一条记录」的落点，builder 与运行时都必须
// 用同一套路径与白名单语义，否则索引会在 A 处生成、在 B 处找不到。
// =============================================================================

import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { isAbsolute, join, relative, resolve, sep } from "node:path";

export const REGISTRY_SCHEMA_VERSION = 1;
export const DEFAULT_REGISTRY_REL = "knowledge/kb-registry.json";

function isDirectory(p) {
  try {
    return statSync(p).isDirectory();
  } catch {
    return false;
  }
}

function isFile(p) {
  try {
    return statSync(p).isFile();
  } catch {
    return false;
  }
}

function envValue(env, key) {
  const value = env ? env[key] : undefined;
  return typeof value === "string" && value.length > 0 ? value : null;
}

function makeStatus(path, source, ok, reason) {
  return ok ? { path, source, ok: true } : { path, source, ok: false, reason };
}

/** 绝对路径判定：盘符（E:\ / E:/）、UNC（\\）、POSIX 前导斜杠 */
export function isAbsolutePath(p) {
  return isAbsolute(p) || /^[A-Za-z]:[\\/]/.test(p) || p.startsWith("\\\\");
}

/** 解析"绝对或仓库根相对"的路径 */
export function resolveFromRoot(pluginRoot, p) {
  return isAbsolutePath(p) ? resolve(p) : resolve(pluginRoot, p);
}

/**
 * 通配 -> 正则：`**` 跨目录、`*` 不跨目录、`?` 单字符；以 '/' 结尾等价于 '/**'。
 * 匹配对象是正斜杠相对路径（含文件名）。
 */
export function globToRegExp(pattern) {
  const normalized = pattern.endsWith("/") ? `${pattern}**` : pattern;
  let out = "";
  for (let i = 0; i < normalized.length; i++) {
    const ch = normalized[i];
    if (ch === "*") {
      if (normalized[i + 1] === "*") {
        out += ".*";
        i += 1;
      } else {
        out += "[^/]*";
      }
      continue;
    }
    if (ch === "?") {
      out += "[^/]";
      continue;
    }
    out += ch.replace(/[.+^${}()|[\]\\]/g, "\\$&");
  }
  return new RegExp(`^${out}$`);
}

/** 相对路径是否命中排除表（任一命中即排除） */
export function matchesExclude(relPath, patterns) {
  const list = Array.isArray(patterns) ? patterns : [];
  const posix = relPath.split(sep).join("/");
  for (const pattern of list) {
    if (globToRegExp(pattern).test(posix)) return true;
  }
  return false;
}

/**
 * 读注册表（不做路径解析）。
 * @returns {{ok:true, registry:object} | {ok:false, reason:string}}
 */
export function loadRegistry(registryPath) {
  if (!isFile(registryPath)) {
    return { ok: false, reason: `注册表不存在：${registryPath}` };
  }
  let raw;
  try {
    raw = readFileSync(registryPath, "utf8");
  } catch (error) {
    return { ok: false, reason: `注册表不可读：${registryPath}（${error.message}）` };
  }
  let parsed;
  try {
    parsed = JSON.parse(raw.replace(/^\uFEFF/, ""));
  } catch (error) {
    return { ok: false, reason: `注册表不是合法 JSON：${registryPath}（${error.message}）` };
  }
  if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
    return { ok: false, reason: `注册表根不是对象：${registryPath}` };
  }
  if (!Array.isArray(parsed.kbs)) {
    return { ok: false, reason: `注册表 kbs 不是数组：${registryPath}` };
  }
  return { ok: true, registry: parsed };
}

/**
 * 解析单个库条目（env 覆盖 + 默认派生 + 存在性校验）。
 * @param {string} pluginRoot 仓库（插件包）根
 * @param {object} entry 注册表条目
 * @param {NodeJS.ProcessEnv} [env]
 */
export function resolveKbEntry(pluginRoot, entry, env = process.env) {
  const id = typeof entry.id === "string" ? entry.id : null;
  const enabled = entry.enabled !== false;
  const rootRel = typeof entry.root === "string" ? entry.root : null;
  const indexRel = typeof entry.index === "string" ? entry.index : null;

  const base = {
    id: id ?? "(缺少 id)",
    title: typeof entry.title === "string" ? entry.title : "",
    authority: typeof entry.authority === "string" ? entry.authority : "",
    enabled,
    envKey: typeof entry.envKey === "string" ? entry.envKey : null,
    scan: entry.scan && typeof entry.scan === "object" ? entry.scan : { dirs: [], exclude: [] },
    sections: Array.isArray(entry.sections) ? entry.sections : [],
    versionDefaults: Array.isArray(entry.versionDefaults) ? entry.versionDefaults : [],
    versionPatterns: Array.isArray(entry.versionPatterns) ? entry.versionPatterns : [],
    domainDefault: typeof entry.domainDefault === "string" ? entry.domainDefault : "both",
    extract: entry.extract && typeof entry.extract === "object" ? entry.extract : {},
    indexPolicy: typeof entry.indexPolicy === "string" ? entry.indexPolicy : "preserve",
    builder: typeof entry.builder === "string" ? entry.builder : null,
  };

  if (id === null) {
    return {
      ...base,
      root: makeStatus("", "default", false, "注册表条目缺少 id"),
      index: makeStatus("", "default", false, "注册表条目缺少 id"),
    };
  }
  if (rootRel === null) {
    return {
      ...base,
      root: makeStatus("", "default", false, `条目 ${id} 缺少 root`),
      index: makeStatus("", "default", false, `条目 ${id} 缺少 root`),
    };
  }

  // ---- root：env 覆盖（显式无效即报错，不回退）或默认派生 ----
  const envPath = base.envKey ? envValue(env, base.envKey) : null;
  const rootSource = envPath ? "env" : "default";
  const rootPath = envPath ? resolveFromRoot(pluginRoot, envPath) : resolveFromRoot(pluginRoot, rootRel);
  const rootOk = isDirectory(rootPath);
  const root = makeStatus(
    rootPath,
    rootSource,
    rootOk,
    envPath
      ? `env ${base.envKey} 指向的知识库根不存在：${rootPath}（显式设置无效即报错，不回退）`
      : `知识库根目录不存在：${rootPath}`,
  );

  // ---- index：'./x' 跟随 root；否则绝对 / 仓库根相对 ----
  let indexPath = null;
  let indexSource = rootSource;
  if (indexRel !== null) {
    if (indexRel.startsWith("./")) {
      indexPath = join(rootPath, indexRel.slice(2));
    } else {
      indexPath = resolveFromRoot(pluginRoot, indexRel);
      indexSource = "default";
    }
  }
  const index =
    indexPath === null
      ? makeStatus("", rootSource, false, `条目 ${id} 缺少 index`)
      : makeStatus(
          indexPath,
          indexSource,
          isFile(indexPath),
          rootOk
            ? `索引未生成：${indexPath}（跑 ${base.builder ?? "builder"} --kb ${id} --write）`
            : `索引未生成：${indexPath}（知识库根不可用：${rootPath}）`,
        );

  return { ...base, root, index };
}

/**
 * 解析注册表内全部启用的库（保持注册表顺序 = 检索优先级）。
 * @returns {{ok:true, kbs:object[], registryPath:string} | {ok:false, reason:string, registryPath:string}}
 */
export function resolveKbs(pluginRoot, registryPath, env = process.env) {
  const loaded = loadRegistry(registryPath);
  if (!loaded.ok) return { ok: false, reason: loaded.reason, registryPath };
  const kbs = loaded.registry.kbs
    .map((entry) => resolveKbEntry(pluginRoot, entry, env))
    .filter((kb) => kb.enabled);
  return { ok: true, kbs, registryPath };
}

/**
 * 按白名单目录遍历库内 markdown，返回正斜杠相对路径（已排序）。
 * 供 builder 生成索引与运行时过期检测共用 —— 同一套 include/exclude 语义。
 */
export function listKbMarkdown(kbRoot, scan) {
  const dirs = Array.isArray(scan?.dirs) ? scan.dirs : [];
  const exclude = Array.isArray(scan?.exclude) ? scan.exclude : [];
  const out = [];
  const walk = (absDir, relPrefix) => {
    let entries;
    try {
      entries = readdirSync(absDir, { withFileTypes: true });
    } catch {
      return;
    }
    for (const dirent of entries) {
      const rel = relPrefix === "" ? dirent.name : `${relPrefix}/${dirent.name}`;
      if (matchesExclude(rel, exclude) || matchesExclude(`${rel}/`, exclude)) continue;
      const full = join(absDir, dirent.name);
      if (dirent.isDirectory()) {
        walk(full, rel);
      } else if (dirent.name.toLowerCase().endsWith(".md")) {
        out.push(rel);
      }
    }
  };
  for (const dir of dirs) {
    const abs = join(kbRoot, dir);
    if (!isDirectory(abs)) continue;
    walk(abs, dir);
  }
  out.sort((a, b) => a.localeCompare(b));
  return out;
}

/**
 * 白名单目录内最新的源文件 mtime（毫秒）。无文件返回 null。
 * @returns {number|null}
 */
export function newestSourceMtime(kbRoot, scan) {
  const files = listKbMarkdown(kbRoot, scan);
  let newest = null;
  for (const rel of files) {
    try {
      const mtime = statSync(join(kbRoot, rel)).mtimeMs;
      if (newest === null || mtime > newest) newest = mtime;
    } catch {
      // 文件在遍历与 stat 之间消失：忽略该文件，不崩
    }
  }
  return newest;
}

/** 文件 mtime（毫秒）；失败返回 null */
export function fileMtime(path) {
  try {
    return statSync(path).mtimeMs;
  } catch {
    return null;
  }
}

/** 注册表默认路径（仓库根相对） */
export function defaultRegistryPath(pluginRoot) {
  return resolve(pluginRoot, DEFAULT_REGISTRY_REL);
}

// -----------------------------------------------------------------------------
// 正文检索（仅 索引未命中 或 显式要求时使用）
//
// 为什么需要：索引字段（title / keywords / path）盖不住概念检索 —— 例如笔记库里
// 「以物易物」出现在 5 篇正文里，却不在任何标题里；只有索引会导致「查不到」，
// 而这比「查得慢」危险得多。检索范围仍严格限定在 scan 白名单目录内（archive/ 永不进入）。
// -----------------------------------------------------------------------------

/** 单文件读取上限：超过则跳过（避免超大笔记把一次查询拖成 IO 事件） */
export const CONTENT_SCAN_MAX_FILE_BYTES = 512 * 1024;

/** 单个文件最多计数的命中次数（防病态重复词把计数跑爆） */
const CONTENT_SCAN_MAX_HITS_PER_FILE = 999;

/** 命中片段：命中点前后各取一段，压平空白，两端加省略号 */
function snippetAround(text, index, needleLength) {
  const before = 40;
  const after = 60;
  const start = Math.max(0, index - before);
  const end = Math.min(text.length, index + needleLength + after);
  const raw = text.slice(start, end).replace(/\s+/g, " ").trim();
  return `${start > 0 ? "…" : ""}${raw}${end < text.length ? "…" : ""}`;
}

/**
 * 在 scan 白名单目录内按正文子串检索（不区分大小写）。
 * @returns 按命中次数降序的 {path, hits, snippet}
 */
export function searchKbContent(kbRoot, scan, needle, options = {}) {
  const trimmed = typeof needle === "string" ? needle.trim() : "";
  if (trimmed === "") return [];
  const dirs = Array.isArray(scan?.dirs) ? scan.dirs : [];
  if (dirs.length === 0 || !isDirectory(kbRoot)) return [];

  const maxFileBytes = Number.isInteger(options.maxFileBytes)
    ? options.maxFileBytes
    : CONTENT_SCAN_MAX_FILE_BYTES;
  const lowerNeedle = trimmed.toLowerCase();
  const out = [];

  for (const relPath of listKbMarkdown(kbRoot, scan)) {
    const full = join(kbRoot, relPath);
    let text;
    try {
      if (statSync(full).size > maxFileBytes) continue;
      text = readFileSync(full, "utf8");
    } catch {
      continue;
    }
    const lowerText = text.toLowerCase();
    let hits = 0;
    let firstIndex = -1;
    let from = 0;
    for (;;) {
      const index = lowerText.indexOf(lowerNeedle, from);
      if (index === -1) break;
      if (firstIndex === -1) firstIndex = index;
      hits += 1;
      if (hits >= CONTENT_SCAN_MAX_HITS_PER_FILE) break;
      from = index + lowerNeedle.length;
    }
    if (hits === 0) continue;
    out.push({ path: relPath, hits, snippet: snippetAround(text, firstIndex, trimmed.length) });
  }

  out.sort((a, b) => b.hits - a.hits || a.path.localeCompare(b.path));
  return out;
}

export { isDirectory, isFile, existsSync, relative };
