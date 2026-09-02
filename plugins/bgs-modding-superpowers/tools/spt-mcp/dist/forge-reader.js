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
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { isCompatibleWithTarget } from "./conflict-engine.js";
export const FORGE_CATALOG_REL = "archive/forge/api/mods-catalog.json";
export const FORGE_HOT_INDEX_REL = "archive/forge/hot-index.json";
export const KB_INDEX_REL = "index.json";
/**
 * 知识库根目录解析：
 * 1. 环境变量 BGS_SPT_KB_ROOT 优先；
 * 2. 否则从本模块所在目录向上探测候选路径，取第一个存在 index.json 的。
 *
 * 候选路径覆盖两种布局（src 与 dist 同深度，探测对两者一致）：
 * - 旧布局：tools/spt-mcp 上溯 3 级 = 仓库根
 * - 插件布局：plugins/bgs-modding-superpowers/tools/spt-mcp 上溯 5 级 = 仓库根
 * 探测保证工具无论位于仓库哪个深度，都能命中仓库根的 knowledge/spt-kb。
 */
export function resolveKbRoot() {
    const env = process.env.BGS_SPT_KB_ROOT;
    if (env && env.length > 0)
        return env;
    const here = dirname(fileURLToPath(import.meta.url));
    const candidates = [3, 4, 5, 6].map((up) => resolve(here, ...Array(up).fill(".."), "knowledge", "spt-kb"));
    const existing = candidates.find((c) => existsSync(resolve(c, KB_INDEX_REL)));
    if (existing)
        return existing;
    // 兜底：返回最深候选；调用方 loadJson 读到 null 时自会报错提示
    return candidates[candidates.length - 1];
}
/** 读取 JSON 文件（兼容 UTF-8 BOM），失败返回 null */
export function loadJson(kbRoot, relPath) {
    try {
        const raw = readFileSync(resolve(kbRoot, relPath), "utf8");
        return JSON.parse(raw.replace(/^\uFEFF/, ""));
    }
    catch {
        return null;
    }
}
/** 按 kbRoot 分键的惰性缓存（支持测试用独立 fixture 根） */
const catalogCache = new Map();
const hotIndexCache = new Map();
export function loadCatalog(kbRoot = resolveKbRoot()) {
    const cached = catalogCache.get(kbRoot);
    if (cached)
        return cached;
    const data = loadJson(kbRoot, FORGE_CATALOG_REL);
    const entries = Array.isArray(data) ? data : [];
    catalogCache.set(kbRoot, entries);
    return entries;
}
export function loadHotIndex(kbRoot = resolveKbRoot()) {
    const cached = hotIndexCache.get(kbRoot);
    if (cached)
        return cached;
    const data = loadJson(kbRoot, FORGE_HOT_INDEX_REL);
    const entries = Array.isArray(data) ? data : [];
    hotIndexCache.set(kbRoot, entries);
    return entries;
}
/** id -> best_spt 映射（来自 hot-index） */
export function bestSptMap(kbRoot = resolveKbRoot()) {
    const map = new Map();
    for (const entry of loadHotIndex(kbRoot)) {
        map.set(entry.id, entry.best_spt ?? null);
    }
    return map;
}
/** 清空缓存（测试用） */
export function resetForgeCache() {
    catalogCache.clear();
    hotIndexCache.clear();
}
export function searchForge(opts, kbRoot = resolveKbRoot()) {
    const catalog = loadCatalog(kbRoot);
    const sptMap = bestSptMap(kbRoot);
    const notes = [];
    const query = opts.query?.trim().toLowerCase();
    const category = opts.category?.trim().toLowerCase();
    const sptVersion = opts.sptVersion?.trim();
    const modType = opts.modType?.trim().toLowerCase();
    if (sptVersion) {
        notes.push("sptVersion 过滤基于 hot-index 的 best_spt 字段，仅覆盖热门索引内的 mod（其余 mod 的 sptVersion 为 null，不参与匹配）");
    }
    if (modType) {
        notes.push("归档快照（mods-catalog.json）不含 mod_type 字段，modType 过滤当前无匹配项（.NET helper 或后续快照补充字段后可用）");
    }
    const matches = [];
    for (const entry of catalog) {
        const id = typeof entry.id === "number" ? entry.id : Number.NaN;
        if (Number.isNaN(id))
            continue;
        const title = typeof entry.name === "string" ? entry.name : "";
        const teaser = typeof entry.teaser === "string" ? entry.teaser : "";
        const downloads = typeof entry.downloads === "number" ? entry.downloads : 0;
        if (query) {
            const haystack = `${title} ${teaser}`.toLowerCase();
            if (!haystack.includes(query))
                continue;
        }
        if (category) {
            const catSlug = entry.category?.slug?.toLowerCase() ?? "";
            const catTitle = entry.category?.title?.toLowerCase() ?? "";
            if (!catSlug.includes(category) && !catTitle.includes(category))
                continue;
        }
        if (sptVersion) {
            const best = sptMap.get(id) ?? null;
            if (!best || !isCompatibleWithTarget(best, sptVersion))
                continue;
        }
        if (modType) {
            const rawType = typeof entry.mod_type === "string" ? entry.mod_type.toLowerCase() : "";
            if (!rawType.includes(modType))
                continue;
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
//# sourceMappingURL=forge-reader.js.map