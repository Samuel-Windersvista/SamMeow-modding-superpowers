#!/usr/bin/env node
// build-hot-index.mjs —— 从 compat-4.1x.json 生成 Forge 热门索引（hot-index.json）
//
// 只收录至少有一个版本约束命中 SPT 4.1 线的 mod，best_version 取其中最新发布的版本。
// 输出字段与 tools/spt-mcp/src/forge-reader.ts 的 HotIndexEntry 保持兼容。
//
// 用法：
//   node scripts/spt-kb/build-hot-index.mjs
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(SCRIPT_DIR, "../..");
const ARCHIVE = join(REPO_ROOT, "knowledge", "spt-kb", "archive", "forge");
const INPUT = join(ARCHIVE, "api", "compat-4.1x.json");
const OUTPUT = join(ARCHIVE, "hot-index.json");

/** 去掉 UTF-8 BOM（Windows 下部分归档文件带 BOM） */
function stripBom(text) {
  return text.charCodeAt(0) === 0xfeff ? text.slice(1) : text;
}

/**
 * 判断版本约束是否覆盖 SPT 4.1 线。
 * 语义上排除 ~4.0 / ~4.0.0 / >=4.0 <4.1.0 这类明确止步于 4.1 之前的写法。
 */
function targets41(constraint) {
  const c = String(constraint ?? "").trim();
  if (!c) return false;
  // 显式上限低于 4.1（如 ~4.0 <4.1.0、>=4.0 <4.1.0）
  if (/<\s*4\.1(?:\.|\s|$)/.test(c)) return false;
  // 明确提到 4.1（4.1.6、4.1.x、~4.1、>= 4.1.3、~4.1. > 4.1.1 ...）
  if (c.includes("4.1")) return true;
  if (c === "*") return true;
  // 4 / ~4 / 4. / ~4. / 4.x / 4.*：没有 minor 或通配，覆盖 4.1
  if (/^~?4(?:\.(?:x|\*)?)?$/.test(c)) return true;
  // ^4.0(.x) 的 caret 范围跨到 4.1
  if (/^\^4\.0(?:\.\d+)?$/.test(c)) return true;
  // 开放下界 >=4.0 / >=4.0.13
  if (/^>=\s*4\.0(?:\.\d+)?$/.test(c)) return true;
  // 带 4.2 上界的开区间
  if (/^>=\s*4\.0(?:\.\d+)?\s+<\s*4\.2/.test(c)) return true;
  return false;
}

/** 版本发布时间戳，依次回退 published_at -> created_at -> updated_at */
function versionTs(v) {
  const raw = v?.published_at ?? v?.created_at ?? v?.updated_at;
  const ts = Date.parse(raw ?? "");
  return Number.isNaN(ts) ? 0 : ts;
}

const raw = stripBom(readFileSync(INPUT, "utf8"));
const mods = JSON.parse(raw);
if (!Array.isArray(mods)) {
  console.error(`[FAIL] ${INPUT} 不是数组`);
  process.exit(1);
}

const entries = [];
let skipped = 0;

for (const mod of mods) {
  const versions = Array.isArray(mod.versions) ? mod.versions : [];
  const compatible = versions.filter((v) => targets41(v.spt_version_constraint));
  if (compatible.length === 0) {
    skipped++;
    continue;
  }

  // 稳定选择：按时间戳降序，时间相同保持原顺序
  let best = compatible[0];
  let bestTs = versionTs(best);
  for (const v of compatible.slice(1)) {
    const ts = versionTs(v);
    if (ts > bestTs) {
      best = v;
      bestTs = ts;
    }
  }

  entries.push({
    id: mod.id,
    name: mod.name ?? null,
    slug: mod.slug ?? null,
    guid: mod.guid ?? null,
    owner: mod.owner?.name ?? null,
    downloads: Number(mod.downloads ?? 0),
    category: mod.category?.title ?? null,
    fika: Boolean(mod.fika_compatibility),
    github: (mod.source_code_links || [])
      .map((l) => l.url)
      .filter(Boolean)
      .join(" | "),
    version_count: versions.length,
    best_version: best.version ?? null,
    best_spt: best.spt_version_constraint ?? null,
    best_link: best.link ?? null,
    best_size: best.content_length ?? null,
    updated_at: mod.updated_at ?? null,
    detail_url: mod.detail_url ?? null,
  });
}

entries.sort((a, b) => {
  const diff = b.downloads - a.downloads;
  if (diff !== 0) return diff;
  return a.id - b.id;
});

// 归档文件统一带 BOM，保持既有风格
writeFileSync(OUTPUT, "\uFEFF" + JSON.stringify(entries, null, 4), "utf8");

console.log(`[DONE] hot-index.json entries=${entries.length} skipped=${skipped}`);
for (const e of entries.slice(0, 3)) {
  console.log(`[sample] ${e.id} ${e.name}: best=${e.best_version} spt=${e.best_spt}`);
}
