#!/usr/bin/env node
// =============================================================================
// sync-index.mjs — KB 索引 upsert 生成器 + drift 报告（C7）
//
// 设计原则（D3）：
//   - 扫描 wiki/ + wiki-tushonka/ + curated/ 下的 *.md（archive/ 不在扫描范围）；
//   - **已有条目绝不改写**（title/keywords/summary 是手工资产，源文件无 title 字段）；
//   - 只为「文件缺条目」生成候选条目；孤儿（条目缺文件）仅报告、不删除；
//   - 默认 dry-run；`--write` 才落盘（仅追加新增条目 + 更新 generated）。
//
// 退出码（dry-run 与 --write 语义一致）：
//   0 = 无契约问题（dry-run 正常结束；或写盘成功且无非法条目）
//   1 = 索引存在非法条目（无论是否写盘；有新增时仍先写入，再以 1 提示待修复）
//   2 = 用法错误，或索引不可解析（坏 JSON / 根非对象 / entries 非数组）
//
// 为什么不是全量重建：wiki 与 wiki-tushonka 是同一页面的两种快照，同页标题不同，
// 源文件 frontmatter 里没有 title。全量重建会丢手工数据。
//
// frontmatter 解析：手写 YAML 子集，**刻意只支持**：
//   - `key: value`（含单/双引号字符串）
//   - `key: [a, b]` 行内 flow 数组
// **不支持**（现状数据未使用；出现时按字面量处理或忽略，不会抛错）：
//   - 引号内含逗号的 flow 数组（`[a, "b,c"]` 会被拆成 3 项）
//   - 块序列（多行 `- item`）
//   - 行内注释（`value # comment` 会连注释一起当值）
//   - 嵌套映射 / 多行标量（`|` / `>`）
//
// 契约复用：条目校验走 spt-mcp dist 的 validateIndex（单一契约实现）。
//
// 用法：
//   node scripts/spt-kb/sync-index.mjs [--write] [--kb <kbRoot>] [--index <path>]
// =============================================================================

import { existsSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { basename, dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const DEFAULT_KB = resolve(SCRIPT_DIR, "../../knowledge/spt-kb");
const CONTRACT_URL = new URL("../../tools/spt-mcp/dist/kb/index.js", import.meta.url);
const SCAN_DIRS = ["wiki", "wiki-tushonka", "curated"];
const KB_DOMAINS = ["server", "client", "both"];

// ---- CLI --------------------------------------------------------------------

const USAGE = [
  "用法: node scripts/spt-kb/sync-index.mjs [--write] [--kb <kbRoot>] [--index <path>]",
  "  --write          落盘（缺省 dry-run）",
  "  --kb <kbRoot>    KB 根目录（缺省 knowledge/spt-kb）",
  "  --index <path>   索引文件路径（缺省 <kb>/index.json）",
  "  --help, -h       显示本帮助",
].join("\n");

function usageError(message) {
  process.stderr.write(`[sync-index] ${message}\n${USAGE}\n`);
  process.exit(2);
}

/** 参数解析：支持 `--k v` 与 `--k=v`（与同目录既有脚本形态一致） */
function parseArgs(argv) {
  const out = { write: false, kbRoot: null, indexPath: null };

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    const eq = arg.indexOf("=");
    const name = eq === -1 ? arg : arg.slice(0, eq);
    const inlineValue = eq === -1 ? undefined : arg.slice(eq + 1);

    const takeValue = () => {
      if (inlineValue !== undefined) {
        if (inlineValue === "") usageError(`参数缺少取值：${name}`);
        return inlineValue;
      }
      const next = argv[i + 1];
      if (next === undefined || next.startsWith("--")) usageError(`参数缺少取值：${name}`);
      i += 1;
      return next;
    };

    if (name === "--write") {
      out.write = true;
      continue;
    }
    if (name === "--kb") {
      out.kbRoot = takeValue();
      continue;
    }
    if (name === "--index") {
      out.indexPath = takeValue();
      continue;
    }
    if (name === "--help" || name === "-h") {
      process.stdout.write(USAGE + "\n");
      process.exit(0);
    }
    usageError(`未知参数：${arg}`);
  }

  return out;
}

const opts = parseArgs(process.argv.slice(2));
const write = opts.write;
const kbRoot = opts.kbRoot ? resolve(opts.kbRoot) : DEFAULT_KB;
const indexPath = opts.indexPath ? resolve(opts.indexPath) : join(kbRoot, "index.json");

// ---- 契约导入 ---------------------------------------------------------------

let contract;
try {
  contract = await import(CONTRACT_URL.href);
} catch (error) {
  process.stderr.write(
    `[sync-index] 无法导入契约实现：${CONTRACT_URL.href}\n` +
      `[sync-index] ${error instanceof Error ? error.message : String(error)}\n` +
      "[sync-index] 请先构建：npm --prefix tools/spt-mcp run build\n",
  );
  process.exit(1);
}
const { KB_SCHEMA_VERSION, validateIndex } = contract;

// ---- 格式保持 ---------------------------------------------------------------

function detectFormat(text) {
  const bom = text.charCodeAt(0) === 0xfeff;
  const body = bom ? text.slice(1) : text;
  const eol = body.includes("\r\n") ? "\r\n" : "\n";
  const hasTrailingNewline = /\r?\n$/.test(body);
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

function serialize(data, format) {
  const json = JSON.stringify(data, null, format.indent).replace(/\n/g, format.eol);
  return (format.bom ? "\uFEFF" : "") + (format.hasTrailingNewline ? json + format.eol : json);
}

// ---- frontmatter（手写 YAML 子集，边界见文件头注释） -------------------------

function unquote(value) {
  if (
    (value.startsWith('"') && value.endsWith('"')) ||
    (value.startsWith("'") && value.endsWith("'"))
  ) {
    return value.slice(1, -1);
  }
  return value;
}

function parseValue(raw) {
  if (raw === "") return "";
  if (raw.startsWith("[") && raw.endsWith("]")) {
    const inner = raw.slice(1, -1).trim();
    if (inner === "") return [];
    return inner
      .split(",")
      .map((part) => unquote(part.trim()))
      .filter((part) => part.length > 0);
  }
  return unquote(raw);
}

/** 解析文件头部 `---` 块（无则返回 {}） */
function parseFrontmatter(text) {
  const normalized = text.replace(/^\uFEFF/, "");
  const match = /^---\r?\n([\s\S]*?)\r?\n---\r?\n?/.exec(normalized);
  if (!match) return {};
  const result = {};
  for (const line of match[1].split(/\r?\n/)) {
    const m = /^([A-Za-z_][A-Za-z0-9_-]*):\s*(.*)$/.exec(line);
    if (!m) continue;
    result[m[1]] = parseValue(m[2].trim());
  }
  return result;
}

/** 首个 H1 标题（`# xxx`） */
function firstH1(text) {
  for (const line of text.split(/\r?\n/)) {
    const m = /^#\s+(.+?)\s*$/.exec(line);
    if (m) return m[1];
  }
  return null;
}

/**
 * 按路径前缀推导 source。
 * SCAN_DIRS 的前缀已全覆盖（curated/ -> curated、wiki* -> wiki）；
 * archive/ 不在扫描范围，故此处**不推导 archive**（孤儿报告仍对 archive 条目加注记）。
 */
function inferSource(relPath) {
  if (relPath.startsWith("curated/")) return "curated";
  if (relPath.startsWith("wiki")) return "wiki";
  return null;
}

// ---- 扫描 -------------------------------------------------------------------

const scannedFiles = [];
for (const dir of SCAN_DIRS) {
  const absDir = join(kbRoot, dir);
  if (!existsSync(absDir)) continue;
  const walk = (abs) => {
    for (const dirent of readdirSync(abs, { withFileTypes: true })) {
      const full = join(abs, dirent.name);
      if (dirent.isDirectory()) {
        walk(full);
      } else if (dirent.name.endsWith(".md")) {
        scannedFiles.push(relative(kbRoot, full).split(sep).join("/"));
      }
    }
  };
  walk(absDir);
}

// ---- 索引读取（解析失败一律响亮退出，禁止静默降级） -------------------------

if (!existsSync(indexPath)) {
  process.stderr.write(`[sync-index] 索引不存在：${indexPath}\n`);
  process.exit(1);
}
const indexText = readFileSync(indexPath, "utf8");

let index;
try {
  index = JSON.parse(indexText.replace(/^\uFEFF/, ""));
} catch (error) {
  process.stderr.write(
    `[sync-index] 索引不是合法 JSON：${indexPath}\n` +
      `[sync-index] ${error instanceof Error ? error.message : String(error)}\n`,
  );
  process.exit(2);
}

if (!index || typeof index !== "object" || Array.isArray(index)) {
  process.stderr.write(`[sync-index] 索引根不是 JSON 对象：${indexPath}\n`);
  process.exit(2);
}

if (!Array.isArray(index.entries)) {
  // 关键防线：把非数组静默当空，会让扫描到的每个源文件都判为「新增」并被写入。
  process.stderr.write(
    `[sync-index] 索引 entries 不是数组（拒绝按空索引处理）：${indexPath}\n`,
  );
  process.exit(2);
}
const existingEntries = index.entries;

/** 条目 path（非对象 / 缺 path 时返回 null；调用方显式处理，不静默取值） */
function entryPathOf(entry) {
  if (!entry || typeof entry !== "object" || Array.isArray(entry)) return null;
  return typeof entry.path === "string" ? entry.path : null;
}

const entryByPath = new Map();
for (const entry of existingEntries) {
  const path = entryPathOf(entry);
  if (path !== null) entryByPath.set(path, entry);
}

// ---- drift 计算 -------------------------------------------------------------

/** 已有条目：逐条按契约严格校验（绝不改写，只报告；path 前缀保证可定位） */
const invalidEntries = [];
for (const entry of existingEntries) {
  const res = validateIndex({ schema_version: KB_SCHEMA_VERSION, entries: [entry] });
  if (!res.ok) {
    invalidEntries.push({ path: entryPathOf(entry) ?? "(缺少 path)", errors: res.errors });
  }
}

/** 新增：扫描到但索引里没有的文件 -> 生成候选条目 */
const additions = [];
for (const relPath of scannedFiles) {
  if (entryByPath.has(relPath)) continue;
  const abs = join(kbRoot, relPath);
  const text = readFileSync(abs, "utf8");
  const fm = parseFrontmatter(text);

  const title =
    (typeof fm.title === "string" && fm.title) || firstH1(text) || basename(relPath, ".md");

  let version;
  if (Array.isArray(fm.version)) version = fm.version;
  else if (typeof fm.version === "string" && fm.version) version = [fm.version];
  else version = ["通用"];

  const domain = KB_DOMAINS.includes(fm.domain) ? fm.domain : "both";
  const topic = (typeof fm.topic === "string" && fm.topic) || "uncategorized";
  // `|| "curated"` 为防御性兜底：SCAN_DIRS 前缀已全覆盖，正常不可达。
  const source = (typeof fm.source === "string" && fm.source) || inferSource(relPath) || "curated";

  const candidate = { path: relPath, title, version, domain, topic, source };
  if (Array.isArray(fm.keywords) && fm.keywords.length > 0) candidate.keywords = fm.keywords;
  if (typeof fm.summary === "string" && fm.summary) candidate.summary = fm.summary;
  additions.push(candidate);
}
additions.sort((a, b) => a.path.localeCompare(b.path));

/** 孤儿：条目路径在磁盘上不存在（不删除，仅报告；非对象/缺 path 已计入非法条目） */
const orphans = [];
for (const entry of existingEntries) {
  const path = entryPathOf(entry);
  if (path === null) continue;
  if (!existsSync(join(kbRoot, path))) orphans.push(path);
}

// ---- 报告 -------------------------------------------------------------------

// 统计只从「整索引契约校验成功」的结果派生：非法数据（如 string version）下
// collectStats 会对 string 逐字符迭代产出乱码，故失败时省略统计行。
const indexValidation = validateIndex(index);
const stats = indexValidation.ok ? indexValidation.stats : null;

const perDir = SCAN_DIRS.map((dir) => {
  const files = scannedFiles.filter((f) => f.startsWith(dir + "/"));
  const entries = existingEntries.filter((entry) => {
    const path = entryPathOf(entry);
    return path !== null && path.startsWith(dir + "/");
  });
  return `${dir}: 文件 ${files.length} / 条目 ${entries.length}`;
});

process.stdout.write(`[sync-index] KB 根：${kbRoot}\n`);
process.stdout.write(`[sync-index] 索引：${indexPath}\n`);
process.stdout.write(
  `[sync-index] 索引 schema_version=${index.schema_version} generated=${index.generated} total=${existingEntries.length}\n`,
);
process.stdout.write(`[sync-index] 扫描：${perDir.join("；")}\n`);
if (stats !== null) {
  process.stdout.write(
    `[sync-index] 分布：bySource=${JSON.stringify(stats.bySource)} byDomain=${JSON.stringify(stats.byDomain)} byVersion=${JSON.stringify(stats.byVersion)}\n`,
  );
} else {
  process.stdout.write(
    `[sync-index] 索引未通过契约校验：统计省略（${invalidEntries.length} 条非法条目，详见下方）\n`,
  );
}
process.stdout.write("\n");

process.stdout.write(`新增（文件缺条目）：${additions.length}\n`);
for (const entry of additions) {
  process.stdout.write(`  + ${entry.path}  title="${entry.title}" version=${JSON.stringify(entry.version)} domain=${entry.domain} topic=${entry.topic} source=${entry.source}\n`);
}

process.stdout.write(`孤儿（条目缺文件，不删除）：${orphans.length}\n`);
for (const path of orphans) {
  process.stdout.write(`  - ${path}${path.startsWith("archive/") ? "  [archive：不随包分发，属预期]" : ""}\n`);
}

process.stdout.write(`非法条目：${invalidEntries.length}\n`);
for (const invalid of invalidEntries) {
  for (const error of invalid.errors) {
    process.stdout.write(`  ! ${invalid.path}: ${error}\n`);
  }
}

// ---- 落盘 -------------------------------------------------------------------

// dry-run 与 --write 的退出码语义统一：存在非法条目即为 1。
if (!write) {
  process.stdout.write("\n[sync-index] dry-run：未写盘（加 --write 落盘）\n");
  process.exit(invalidEntries.length > 0 ? 1 : 0);
}

if (additions.length === 0) {
  process.stdout.write(
    invalidEntries.length > 0
      ? `\n[sync-index] 无新增条目；${invalidEntries.length} 条非法条目待修复：未写盘\n`
      : "\n[sync-index] 无新增条目：未写盘（generated 不变，保持写盘幂等）\n",
  );
  process.exit(invalidEntries.length > 0 ? 1 : 0);
}

const generated = new Date().toISOString().slice(0, 10);
const next = {
  ...index,
  generated,
  entries: [...existingEntries, ...additions],
};
const format = detectFormat(indexText);
writeFileSync(indexPath, serialize(next, format), "utf8");

if (invalidEntries.length > 0) {
  process.stdout.write(
    `\n[sync-index] 已写入 ${additions.length} 条新增；${invalidEntries.length} 条非法条目待修复（generated ${index.generated} -> ${generated}）\n`,
  );
  process.exit(1);
}

process.stdout.write(
  `\n[sync-index] 已写盘：新增 ${additions.length} 条，generated ${index.generated} -> ${generated}，total ${existingEntries.length} -> ${existingEntries.length + additions.length}\n`,
);
process.exit(0);
