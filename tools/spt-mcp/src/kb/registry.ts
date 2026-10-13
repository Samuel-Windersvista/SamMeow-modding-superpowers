// =============================================================================
// kb/registry.ts — 知识库注册表的运行时入口（spt-mcp 侧薄包装）
//
// 单一解析实现在 shared/kb-registry.mjs（与 scripts/kb/build-index.mjs 共用），
// 本模块只负责：算注册表路径 + 逐库状态 + 索引读取 + 过期检测 + 模块级缓存。
//
// 退化契约：注册表缺失/不可解析时**不报错封死查询**，退化为「单库模式」——
// 只认 shared/runtime-layout.mjs 解析出的传统 KB 根（SPT_KB_ROOT / knowledge/spt-kb），
// 并在结果里显式标注 source=legacy 与原因（便携包/旧部署仍可用）。
//
// 过期检测（只提示、不阻断）：注册表 scan.dirs 白名单内最新源文件 mtime > 索引
// mtime 即视为过期，结果里挂 stale 标记。检测走与 builder 相同的遍历实现，
// 避免「生成时算一套、运行时算另一套」。
// =============================================================================

import { readFileSync } from "node:fs";

import {
  defaultRegistryPath,
  fileMtime,
  newestSourceMtime,
  resolveFromRoot,
  resolveKbs,
  type KbScanSpec,
  type ResourceStatus,
} from "../../../../shared/kb-registry.mjs";

import { getLayout, PLUGIN_ROOT } from "../runtime-layout.js";

/** 正文检索（白名单范围内）——由 kb/registry.ts 统一转出，工具层不直接触 shared */
export { searchKbContent } from "../../../../shared/kb-registry.mjs";
export type { ContentHit } from "../../../../shared/kb-registry.mjs";

/** 已解析的运行时库条目（注册表条目 或 传统单库退化） */
export interface RuntimeKb {
  id: string;
  title: string;
  authority: string;
  root: ResourceStatus;
  index: ResourceStatus;
  scan: KbScanSpec;
  /** 根覆盖用 env 键（注册表声明；退化库为 SPT_KB_ROOT） */
  envKey: string | null;
  source: "registry" | "legacy";
}

export interface RegistryStatus {
  /** 注册表路径（env KB_REGISTRY 或仓库根 knowledge/kb-registry.json） */
  path: string;
  /** registry = 用注册表；legacy = 退化为传统单库 */
  source: "registry" | "legacy";
  ok: boolean;
  reason?: string;
  kbs: RuntimeKb[];
}

/** 过期检测结果：stale=null 表示无法判定（未声明 scan.dirs 或无源文件） */
export interface StaleInfo {
  stale: boolean | null;
  newestSourceMtime: number | null;
  indexMtime: number | null;
}

const STALE_TTL_MS = 60_000;

let cachedStatus: RegistryStatus | null = null;
const staleCache = new Map<string, { at: number; info: StaleInfo }>();

/** 清缓存（测试用；改 env 后必须调用） */
export function resetRegistryForTest(): void {
  cachedStatus = null;
  staleCache.clear();
}

function registryPathOf(env: NodeJS.ProcessEnv = process.env): { path: string; fromEnv: boolean } {
  const value = env.KB_REGISTRY;
  if (typeof value === "string" && value.length > 0) {
    // 相对路径按仓库根解析（与注册表内 root/index 同一约定）
    return { path: resolveFromRoot(PLUGIN_ROOT, value), fromEnv: true };
  }
  return { path: defaultRegistryPath(PLUGIN_ROOT), fromEnv: false };
}

/** 传统单库退化（注册表不可用时的兜底，行为与 C7 之前一致） */
function legacyStatus(reason: string, path: string): RegistryStatus {
  const layout = getLayout();
  return {
    path,
    source: "legacy",
    ok: false,
    reason,
    kbs: [
      {
        id: "spt-kb",
        title: "SPT 知识库（注册表不可用时的单库退化）",
        authority: "SPT 接口事实：API 签名、版本差异、mod 加载与冲突机制",
        root: layout.kb.root,
        index: layout.kb.index,
        // 未声明 scan.dirs => 不做过期检测（遍历白名单来自注册表）
        scan: { dirs: [], exclude: [] },
        envKey: "SPT_KB_ROOT",
        source: "legacy",
      },
    ],
  };
}

/** 解析注册表（模块级缓存：env 改动后需 resetRegistryForTest） */
export function getRegistryStatus(): RegistryStatus {
  if (cachedStatus !== null) return cachedStatus;
  const { path } = registryPathOf();
  const resolved = resolveKbs(PLUGIN_ROOT, path, process.env);
  if (!resolved.ok) {
    cachedStatus = legacyStatus(resolved.reason, path);
    return cachedStatus;
  }
  cachedStatus = {
    path,
    source: "registry",
    ok: true,
    kbs: resolved.kbs.map((kb) => ({
      id: kb.id,
      title: kb.title,
      authority: kb.authority,
      root: kb.root,
      index: kb.index,
      scan: kb.scan,
      envKey: kb.envKey,
      source: "registry" as const,
    })),
  };
  return cachedStatus;
}

/** 按 id 取库；id 为空时返回 null */
export function getKbById(id: string | undefined): RuntimeKb | null {
  if (typeof id !== "string" || id.length === 0) return null;
  const status = getRegistryStatus();
  return status.kbs.find((kb) => kb.id === id) ?? null;
}

/** 读索引文件（兼容 UTF-8 BOM）；失败返回 {ok:false, reason} */
export function readIndexFile(
  path: string,
): { ok: true; raw: unknown } | { ok: false; reason: string } {
  let text: string;
  try {
    text = readFileSync(path, "utf8");
  } catch (error) {
    return { ok: false, reason: `索引不可读：${path}（${error instanceof Error ? error.message : String(error)}）` };
  }
  try {
    return { ok: true, raw: JSON.parse(text.replace(/^\uFEFF/, "")) };
  } catch (error) {
    return { ok: false, reason: `索引不是合法 JSON：${path}（${error instanceof Error ? error.message : String(error)}）` };
  }
}

/** 过期检测（60s 缓存）：源文件比索引新 => stale:true */
export function getStaleInfo(kb: RuntimeKb): StaleInfo {
  const dirs = Array.isArray(kb.scan.dirs) ? kb.scan.dirs : [];
  if (dirs.length === 0 || !kb.root.ok || !kb.index.ok) {
    return { stale: null, newestSourceMtime: null, indexMtime: null };
  }
  const key = kb.index.path;
  const hit = staleCache.get(key);
  const now = Date.now();
  if (hit !== undefined && now - hit.at < STALE_TTL_MS) return hit.info;

  const newestSourceMtimeValue = newestSourceMtime(kb.root.path, kb.scan);
  const indexMtime = fileMtime(kb.index.path);
  const info: StaleInfo = {
    stale:
      newestSourceMtimeValue !== null && indexMtime !== null
        ? newestSourceMtimeValue > indexMtime
        : null,
    newestSourceMtime: newestSourceMtimeValue,
    indexMtime,
  };
  staleCache.set(key, { at: now, info });
  return info;
}
