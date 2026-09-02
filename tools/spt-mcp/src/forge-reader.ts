// =============================================================================
// forge-reader.ts — Forge 归档读取与搜索
//
// 数据源（knowledge/spt-kb/archive/forge/）：
//   api/mods-catalog.json  1822 mod 元数据（快照，无 best_spt / mod_type 字段）
//   hot-index.json         95 热门 mod 索引（含 best_spt / best_version）
//
// sptVersion 过滤：mods-catalog 无 best_spt 字段，通过与 hot-index 按 id 合并
// 获得 best_spt；仅对 hot-index 中存在的 95 个 mod 生效，其余 sptVersion 为
// null。modType 过滤：归档快照无 mod_type 字段，命中为空并给出说明。
// =============================================================================

import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { isCompatibleWithTarget } from "./conflict-engine.js";
import type { ForgeModSummary } from "./types.js";

export const FORGE_CATALOG_REL = "archive/forge/api/mods-catalog.json";
export const FORGE_HOT_INDEX_REL = "archive/forge/hot-index.json";
export const KB_INDEX_REL = "index.json";

/** 知识库根目录：默认按 dist/src 文件位置回退到仓库 knowledge/spt-kb */
export function resolveKbRoot(): string {
  const env = process.env.BGS_SPT_KB_ROOT;
  if (env && env.length > 0) return env;
  const here = dirname(fileURLToPath(import.meta.url));
  return resolve(here, "..", "..", "..", "knowledge", "spt-kb");
}

/** 读取 JSON 文件（兼容 UTF-8 BOM），失败返回 null */
export function loadJson<T>(kbRoot: string, relPath: string): T | null {
  try {
    const raw = readFileSync(resolve(kbRoot, relPath), "utf8");
    return JSON.parse(raw.replace(/^\uFEFF/, "")) as T;
  } catch {
    return null;
  }
}

// -----------------------------------------------------------------------------
// 目录加载（模块级惰性缓存，无 daemon 无状态机）
// -----------------------------------------------------------------------------

export interface CatalogEntry {
  id: number;
  name?: string;
  slug?: string;
  teaser?: string;
  downloads?: number;
  category_id?: number | null;
  category?: { id?: number; title?: string; slug?: string } | null;
  guid?: string | null;
  [key: string]: unknown;
}

export interface HotIndexEntry {
  id: number;
  best_spt?: string | null;
  best_version?: string | null;
  [key: string]: unknown;
}

/** 按 kbRoot 分键的惰性缓存（支持测试用独立 fixture 根） */
const catalogCache = new Map<string, CatalogEntry[]>();
const hotIndexCache = new Map<string, HotIndexEntry[]>();

export function loadCatalog(kbRoot: string = resolveKbRoot()): CatalogEntry[] {
  const cached = catalogCache.get(kbRoot);
  if (cached) return cached;
  const data = loadJson<CatalogEntry[]>(kbRoot, FORGE_CATALOG_REL);
  const entries = Array.isArray(data) ? data : [];
  catalogCache.set(kbRoot, entries);
  return entries;
}

export function loadHotIndex(kbRoot: string = resolveKbRoot()): HotIndexEntry[] {
  const cached = hotIndexCache.get(kbRoot);
  if (cached) return cached;
  const data = loadJson<HotIndexEntry[]>(kbRoot, FORGE_HOT_INDEX_REL);
  const entries = Array.isArray(data) ? data : [];
  hotIndexCache.set(kbRoot, entries);
  return entries;
}

/** id -> best_spt 映射（来自 hot-index） */
export function bestSptMap(kbRoot: string = resolveKbRoot()): Map<number, string | null> {
  const map = new Map<number, string | null>();
  for (const entry of loadHotIndex(kbRoot)) {
    map.set(entry.id, entry.best_spt ?? null);
  }
  return map;
}

/** 清空缓存（测试用） */
export function resetForgeCache(): void {
  catalogCache.clear();
  hotIndexCache.clear();
}

// -----------------------------------------------------------------------------
// 搜索
// -----------------------------------------------------------------------------

export interface ForgeSearchOptions {
  query?: string;
  category?: string;
  sptVersion?: string;
  modType?: string;
}

export interface ForgeSearchResult {
  matches: ForgeModSummary[];
  total: number;
  note?: string;
}

export function searchForge(
  opts: ForgeSearchOptions,
  kbRoot: string = resolveKbRoot(),
): ForgeSearchResult {
  const catalog = loadCatalog(kbRoot);
  const sptMap = bestSptMap(kbRoot);
  const notes: string[] = [];

  const query = opts.query?.trim().toLowerCase();
  const category = opts.category?.trim().toLowerCase();
  const sptVersion = opts.sptVersion?.trim();
  const modType = opts.modType?.trim().toLowerCase();

  if (sptVersion) {
    notes.push(
      "sptVersion 过滤基于 hot-index 的 best_spt 字段，仅覆盖热门索引内的 mod（其余 mod 的 sptVersion 为 null，不参与匹配）",
    );
  }
  if (modType) {
    notes.push(
      "归档快照（mods-catalog.json）不含 mod_type 字段，modType 过滤当前无匹配项（.NET helper 或后续快照补充字段后可用）",
    );
  }

  const matches: ForgeModSummary[] = [];
  for (const entry of catalog) {
    const id = typeof entry.id === "number" ? entry.id : Number.NaN;
    if (Number.isNaN(id)) continue;

    const title = typeof entry.name === "string" ? entry.name : "";
    const teaser = typeof entry.teaser === "string" ? entry.teaser : "";
    const downloads = typeof entry.downloads === "number" ? entry.downloads : 0;

    if (query) {
      const haystack = `${title} ${teaser}`.toLowerCase();
      if (!haystack.includes(query)) continue;
    }

    if (category) {
      const catSlug = entry.category?.slug?.toLowerCase() ?? "";
      const catTitle = entry.category?.title?.toLowerCase() ?? "";
      if (!catSlug.includes(category) && !catTitle.includes(category)) continue;
    }

    if (sptVersion) {
      const best = sptMap.get(id) ?? null;
      if (!best || !isCompatibleWithTarget(best, sptVersion)) continue;
    }

    if (modType) {
      const rawType = typeof entry.mod_type === "string" ? entry.mod_type.toLowerCase() : "";
      if (!rawType.includes(modType)) continue;
    }

    matches.push({
      id,
      title,
      slug: typeof entry.slug === "string" ? entry.slug : "",
      category: entry.category?.title ?? null,
      sptVersion: sptMap.get(id) ?? null,
      downloads,
      teaser,
    });
  }

  matches.sort((a, b) => b.downloads - a.downloads);

  return {
    matches,
    total: matches.length,
    note: notes.length > 0 ? notes.join("；") : undefined,
  };
}
