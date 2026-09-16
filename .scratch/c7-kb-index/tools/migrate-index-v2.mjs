// =============================================================================
// migrate-index-v2.mjs — KB 索引 v1 → v2 一次性迁移（C7 车道 A 步骤 2）
//
// 变更（严格限定，其余字段一律不动）：
//   1. version 为 string 的条目 -> [string]（v1 遗留形态，查询侧崩溃点）
//   2. source 为空的条目 -> 按 path 前缀填充：curated/ -> curated
//                                              wiki*/  -> wiki
//                                              archive/ -> archive
//   3. schema_version: 1 -> 2
//   4. generated 不变
//
// 安全：先把迁移前原文（逐字节）备份到
//   .scratch/c7-kb-index/goldens/index-pre-migration.json
//
// 格式：保持既有约定（先检测后写）——缩进宽度 / 行尾 / 尾随换行 / BOM 均按原文复现。
//
// 用法：node .scratch/c7-kb-index/tools/migrate-index-v2.mjs [--write]
//   缺省 dry-run（只报告不落盘）。
// =============================================================================

import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";

const INDEX_PATH = resolve("knowledge/spt-kb/index.json");
const BACKUP_PATH = resolve(".scratch/c7-kb-index/goldens/index-pre-migration.json");

const write = process.argv.includes("--write");

/** 检测文本格式约定（缩进宽度 / 行尾 / 尾随换行 / BOM） */
function detectFormat(text) {
  const bom = text.charCodeAt(0) === 0xfeff;
  const body = bom ? text.slice(1) : text;

  const eol = body.includes("\r\n") ? "\r\n" : "\n";
  const hasTrailingNewline = /\r?\n$/.test(body);

  // 缩进：取第一个以空白开头的行的前导空白数
  let indent = 1;
  for (const line of body.split(/\r?\n/)) {
    const match = /^( +)\S/.exec(line);
    if (match) {
      indent = match[1].length;
      break;
    }
  }

  return { bom, eol, hasTrailingNewline, indent };
}

/** 序列化并复现原文格式 */
function serialize(data, format) {
  const json = JSON.stringify(data, null, format.indent).replace(/\n/g, format.eol);
  const withTrailing = format.hasTrailingNewline ? json + format.eol : json;
  return (format.bom ? "\uFEFF" : "") + withTrailing;
}

/** 按 path 前缀推导 source */
function inferSource(path) {
  if (path.startsWith("curated/")) return "curated";
  if (path.startsWith("wiki")) return "wiki";
  if (path.startsWith("archive/")) return "archive";
  return null;
}

const original = readFileSync(INDEX_PATH, "utf8");
const format = detectFormat(original);
const index = JSON.parse(format.bom ? original.slice(1) : original);

console.log(
  `[migrate] 原文格式：indent=${format.indent} eol=${format.eol === "\r\n" ? "CRLF" : "LF"} ` +
    `trailingNewline=${format.hasTrailingNewline} bom=${format.bom}`,
);
console.log(
  `[migrate] 迁移前：schema_version=${index.schema_version} generated=${index.generated} entries=${index.entries.length}`,
);

const changes = [];

// 1. version string -> [string]
for (const entry of index.entries) {
  if (typeof entry.version === "string") {
    changes.push(`version 归一：${entry.path} "${entry.version}" -> ["${entry.version}"]`);
    entry.version = [entry.version];
  }
}

// 2. source 空 -> 按路径前缀填充
for (const entry of index.entries) {
  if (entry.source === "" || entry.source === undefined || entry.source === null) {
    const inferred = inferSource(entry.path);
    if (inferred === null) {
      throw new Error(`[migrate] 无法为 ${entry.path} 推导 source（未知前缀）`);
    }
    changes.push(`source 填充：${entry.path} (空) -> ${inferred}`);
    entry.source = inferred;
  }
}

// 3. schema_version 1 -> 2
if (index.schema_version !== 2) {
  changes.push(`schema_version：${index.schema_version} -> 2`);
  index.schema_version = 2;
}

console.log(`[migrate] 变更 ${changes.length} 处：`);
for (const change of changes) console.log("  - " + change);

if (!write) {
  console.log("[migrate] dry-run：未写盘（加 --write 落盘）");
  process.exit(0);
}

// 备份迁移前原文（逐字节）
mkdirSync(dirname(BACKUP_PATH), { recursive: true });
writeFileSync(BACKUP_PATH, original, "utf8");
console.log(`[migrate] 已备份迁移前原文 -> ${BACKUP_PATH}`);

writeFileSync(INDEX_PATH, serialize(index, format), "utf8");
console.log(`[migrate] 已写回 -> ${INDEX_PATH}`);
console.log(
  `[migrate] 迁移后：schema_version=${index.schema_version} generated=${index.generated} entries=${index.entries.length}`,
);
