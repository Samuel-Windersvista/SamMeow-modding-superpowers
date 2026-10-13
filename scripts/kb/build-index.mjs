#!/usr/bin/env node
// =============================================================================
// build-index.mjs — 注册表驱动的知识库索引生成器
//
// 为什么存在：加一个知识库应当是「注册表加一条 + 跑一次本脚本」，而不是写新提示词、
// 新 MCP 工具、新扫描脚本。本脚本按 knowledge/kb-registry.json 的条目逐个生成索引，
// 路径解析 / 白名单遍历 / 排除匹配全部走 shared/kb-registry.mjs（与运行时同一实现）。
//
// 设计原则（与 scripts/spt-kb/sync-index.mjs 同族）：
//   - 只扫注册表 scan.dirs（白名单）；archive/、assets/ 之类永不进入扫描范围；
//   - **已有条目绝不改写**（title/keywords/summary 是手工资产，源文件可能没有对应字段）；
//   - 只为「文件缺条目」生成候选条目；孤儿（条目缺文件）仅报告、不删除；
//   - 默认 dry-run；`--write` 才落盘。
//   - 索引文件不存在时会**创建**（新库首次注册的唯一差别；sync-index 要求已存在）。
//
// 无 frontmatter 的库（如作者笔记库）靠注册表的 sections / versionDefaults /
// versionPatterns / extract 规则生成 topic、source、version、keywords、summary。
//
// 契约复用：条目校验走 spt-mcp dist 的 validateIndex（单一契约实现）。
//
// 退出码：
//   0 = 无契约问题（dry-run 正常结束；或写盘成功且无非法条目）
//   1 = 索引存在非法条目，或某库不可用（无论是否写盘）
//   2 = 用法错误，或索引不可解析（坏 JSON / 根非对象 / entries 非数组）
//
// 用法：
//   node scripts/kb/build-index.mjs [--write] [--kb <id>] [--registry <path>]
// =============================================================================

import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { basename, dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import {
  defaultRegistryPath,
  listKbMarkdown,
  resolveKbs,
} from "../../shared/kb-registry.mjs";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const PLUGIN_ROOT = resolve(SCRIPT_DIR, "../..");
const CONTRACT_URL = new URL("../../tools/spt-mcp/dist/kb/index.js", import.meta.url);
const KB_DOMAINS = ["server", "client", "both"];

// ---- CLI --------------------------------------------------------------------

const USAGE = [
  "用法: node scripts/kb/build-index.mjs [--write] [--kb <id>] [--registry <path>]",
  "  --write            落盘（缺省 dry-run）",
  "  --kb <id>          只处理注册表里的某个库（缺省全部启用库）",
  "  --registry <path>  注册表路径（缺省 knowledge/kb-registry.json）",
  "  --help, -h         显示本帮助",
].join("\n");

function usageError(message) {
  process.stderr.write(`[build-index] ${message}\n${USAGE}\n`);
  process.exit(2);
}

function parseArgs(argv) {
  const out = { write: false, kbId: null, registryPath: null };
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
      out.kbId = takeValue();
      continue;
    }
    if (name === "--registry") {
      out.registryPath = takeValue();
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
const registryPath = opts.registryPath ? resolve(opts.registryPath) : defaultRegistryPath(PLUGIN_ROOT);

// ---- 契约导入 ---------------------------------------------------------------

let contract;
try {
  contract = await import(CONTRACT_URL.href);
} catch (error) {
  process.stderr.write(
    `[build-index] 无法导入契约实现：${CONTRACT_URL.href}\n` +
      `[build-index] ${error instanceof Error ? error.message : String(error)}\n` +
      "[build-index] 请先构建：npm --prefix tools/spt-mcp run build\n",
  );
  process.exit(1);
}
const { KB_SCHEMA_VERSION, validateIndex } = contract;

// ---- 格式保持 ---------------------------------------------------------------

function detectFormat(text) {
  if (text === null) return { bom: false, eol: "\n", hasTrailingNewline: true, indent: 2 };
  const bom = text.charCodeAt(0) === 0xfeff;
  const body = bom ? text.slice(1) : text;
  const eol = body.includes("\r\n") ? "\r\n" : "\n";
  const hasTrailingNewline = /\r?\n$/.test(body);
  let indent = 2;
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

// ---- markdown 提取（手写子集，与 sync-index 同边界） -------------------------

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

/** 去掉 frontmatter，返回正文行数组 */
function bodyLines(text) {
  const normalized = text.replace(/^\uFEFF/, "");
  const stripped = normalized.replace(/^---\r?\n[\s\S]*?\r?\n---\r?\n?/, "");
  return stripped.split(/\r?\n/);
}

function firstH1(text) {
  for (const line of bodyLines(text)) {
    const m = /^#\s+(.+?)\s*$/.exec(line);
    if (m) return m[1];
  }
  return null;
}

/** 行内 markdown 去噪：链接保留文字、去强调/行内码/图片 */
function stripInline(line) {
  return line
    .replace(/!\[[^\]]*\]\([^)]*\)/g, " ")
    .replace(/\[([^\]]*)\]\([^)]*\)/g, "$1")
    .replace(/`([^`]*)`/g, "$1")
    .replace(/[*_~]{1,3}/g, "")
    .replace(/\s+/g, " ")
    .trim();
}

/** 首个正文段落（跳过标题 / 引用 / 代码块 / 表格 / 列表 / 分隔线） */
function firstParagraph(text, maxChars) {
  const lines = bodyLines(text);
  const skip = /^\s*(#|>|\||```|~~~|---|===|\*\s|-\s|\+\s|\d+[.)]\s|:::|<)/;
  const collected = [];
  for (const raw of lines) {
    const line = raw.trim();
    if (line === "") {
      if (collected.length > 0) break;
      continue;
    }
    if (collected.length === 0) {
      if (skip.test(line)) continue;
      collected.push(line);
      continue;
    }
    if (skip.test(line)) break;
    collected.push(line);
  }
  const joined = stripInline(collected.join(" "));
  if (joined === "") return null;
  return joined.length > maxChars ? `${joined.slice(0, maxChars)}…` : joined;
}

/** H2 标题（含编号去噪），用作关键词 */
function h2Headings(text, max) {
  const out = [];
  const seen = new Set();
  for (const line of bodyLines(text)) {
    const m = /^##\s+(.+?)\s*$/.exec(line);
    if (!m) continue;
    const cleaned = stripInline(m[1]).replace(/^[\d.、)（(]+/, "").replace(/[：:]\s*$/, "");
    if (cleaned === "" || cleaned.length > 24) continue;
    if (seen.has(cleaned)) continue;
    seen.add(cleaned);
    out.push(cleaned);
    if (out.length >= max) break;
  }
  return out;
}

// ---- 条目生成 ---------------------------------------------------------------

/** 最长前缀匹配 section（返回 {topic, source}） */
function sectionOf(relPath, sections) {
  let best = null;
  for (const section of sections) {
    const dir = typeof section?.dir === "string" ? section.dir : "";
    if (dir === "") continue;
    if (relPath === dir || relPath.startsWith(`${dir}/`)) {
      if (best === null || dir.length > best.dir.length) best = { dir, section };
    }
  }
  if (best === null) return null;
  const topic = typeof best.section.topic === "string" ? best.section.topic : null;
  const source = typeof best.section.source === "string" ? best.section.source : null;
  return {
    topic: topic ?? undefined,
    source: source ?? best.section.fallbackSource ?? best.dir,
  };
}

function versionOf(title, relPath, kb) {
  const out = [];
  const push = (value) => {
    if (typeof value !== "string" || value === "") return;
    if (!out.includes(value)) out.push(value);
  };
  for (const value of kb.versionDefaults) push(value);
  const haystack = `${title} ${relPath}`;
  for (const pattern of kb.versionPatterns) {
    let re;
    try {
      re = new RegExp(pattern, "g");
    } catch {
      continue;
    }
    for (const match of haystack.matchAll(re)) {
      if (match[1]) push(match[1]);
      else if (match[0]) push(match[0]);
    }
  }
  return out.length > 0 ? out : ["通用"];
}

function keywordsOf(text, relPath, kb) {
  const spec = Array.isArray(kb.extract.keywords) ? kb.extract.keywords : [];
  const max = Number.isInteger(kb.extract.keywordsMax) ? kb.extract.keywordsMax : 12;
  const out = [];
  const seen = new Set();
  const push = (value) => {
    const cleaned = typeof value === "string" ? stripInline(value).trim() : "";
    if (cleaned === "" || cleaned.length > 24) return;
    const key = cleaned.toLowerCase();
    if (seen.has(key)) return;
    seen.add(key);
    out.push(cleaned);
  };

  if (spec.includes("h2")) {
    for (const heading of h2Headings(text, max)) push(heading);
  }
  if (spec.includes("dirs") && out.length < max) {
    const segments = relPath.split("/");
    segments.pop();
    const sectionDir = sectionOf(relPath, kb.sections)?.dir;
    for (const segment of segments) {
      if (segment === sectionDir) continue;
      push(segment);
      if (out.length >= max) break;
    }
  }

  return out.slice(0, max);
}

function buildCandidate(relPath, text, kb) {
  const fm = parseFrontmatter(text);
  const section = sectionOf(relPath, kb.sections);

  const title =
    (typeof fm.title === "string" && fm.title) || firstH1(text) || basename(relPath, ".md");

  const topic =
    (typeof fm.topic === "string" && fm.topic) || section?.topic || "uncategorized";
  const source =
    (typeof fm.source === "string" && fm.source) || section?.source || relPath.split("/")[0];
  const domain = KB_DOMAINS.includes(fm.domain) ? fm.domain : kb.domainDefault;

  const candidate = {
    path: relPath,
    title: stripInline(String(title)) || basename(relPath, ".md"),
    version: versionOf(title, relPath, kb),
    domain: KB_DOMAINS.includes(domain) ? domain : "both",
    topic,
    source,
  };

  const keywords =
    Array.isArray(fm.keywords) && fm.keywords.length > 0
      ? fm.keywords
      : keywordsOf(text, relPath, kb);
  if (keywords.length > 0) candidate.keywords = keywords;

  const summary =
    (typeof fm.summary === "string" && fm.summary) ||
    (kb.extract.summary === "firstParagraph"
      ? firstParagraph(
          text,
          Number.isInteger(kb.extract.summaryMaxChars) ? kb.extract.summaryMaxChars : 220,
        )
      : null);
  if (summary) candidate.summary = summary;

  return candidate;
}

// ---- 逐库处理 ---------------------------------------------------------------

/** 写进索引的诚实声明：version 标签是词法匹配，不是权威版本声明 */
const VERSION_NOTE =
  "version 标签来自 versionDefaults + versionPatterns 对文件名/标题的词法匹配（如 'v4.2.0' 指该文档叙述的对象版本），" +
  "是检索线索而非权威版本声明；要改标签请改注册表规则后重跑，或直接手改本条目（条目一经写入不再被 builder 改写）。";

const resolved = resolveKbs(PLUGIN_ROOT, registryPath);
if (!resolved.ok) {
  process.stderr.write(`[build-index] 注册表不可用：${resolved.reason}\n`);
  process.exit(1);
}

const selected = opts.kbId
  ? resolved.kbs.filter((kb) => kb.id === opts.kbId)
  : resolved.kbs;
if (opts.kbId && selected.length === 0) {
  process.stderr.write(
    `[build-index] 注册表里没有启用的库：${opts.kbId}（可用：${resolved.kbs.map((kb) => kb.id).join(", ") || "无"}）\n`,
  );
  process.exit(2);
}

process.stdout.write(`[build-index] 仓库根：${PLUGIN_ROOT}\n`);
process.stdout.write(`[build-index] 注册表：${registryPath}\n\n`);

let totalAdditions = 0;
let totalInvalid = 0;
let unavailableKbs = 0;
const pendingWrites = [];

for (const kb of selected) {
  process.stdout.write(`── ${kb.id}\n`);
  process.stdout.write(`   用途：${kb.title || "(未填)"}\n`);
  process.stdout.write(`   权威：${kb.authority || "(未填)"}\n`);
  process.stdout.write(`   根：  ${kb.root.path}  [${kb.root.source}]\n`);
  process.stdout.write(`   索引：${kb.index.path}\n`);

  if (!kb.root.ok) {
    totalInvalid += 1;
    unavailableKbs += 1;
    process.stdout.write(`   ! 知识库根不可用：${kb.root.reason}\n\n`);
    continue;
  }

  const scanDirs = Array.isArray(kb.scan.dirs) ? kb.scan.dirs : [];
  if (scanDirs.length === 0) {
    process.stdout.write(`   ! 注册表条目缺少 scan.dirs（白名单未声明，拒绝遍历整个库）\n\n`);
    totalInvalid += 1;
    continue;
  }

  const files = listKbMarkdown(kb.root.path, kb.scan);

  // 索引读取（不存在 => 新建；存在但不可解析 => 响亮退出）
  let indexText = null;
  let index = null;
  if (existsSync(kb.index.path)) {
    indexText = readFileSync(kb.index.path, "utf8");
    try {
      index = JSON.parse(indexText.replace(/^\uFEFF/, ""));
    } catch (error) {
      process.stderr.write(
        `[build-index] 索引不是合法 JSON：${kb.index.path}\n[build-index] ${error.message}\n`,
      );
      process.exit(2);
    }
    if (!index || typeof index !== "object" || Array.isArray(index)) {
      process.stderr.write(`[build-index] 索引根不是 JSON 对象：${kb.index.path}\n`);
      process.exit(2);
    }
    if (!Array.isArray(index.entries)) {
      process.stderr.write(
        `[build-index] 索引 entries 不是数组（拒绝按空索引处理）：${kb.index.path}\n`,
      );
      process.exit(2);
    }
  }

  const existingEntries = Array.isArray(index?.entries) ? index.entries : [];
  const entryByPath = new Map();
  for (const entry of existingEntries) {
    if (entry && typeof entry === "object" && typeof entry.path === "string") {
      entryByPath.set(entry.path, entry);
    }
  }

  const invalidEntries = [];
  for (const entry of existingEntries) {
    const res = validateIndex({ schema_version: KB_SCHEMA_VERSION, entries: [entry] });
    if (!res.ok) {
      invalidEntries.push({
        path: entry && typeof entry.path === "string" ? entry.path : "(缺少 path)",
        errors: res.errors,
      });
    }
  }

  const additions = [];
  for (const relPath of files) {
    if (entryByPath.has(relPath)) continue;
    let text;
    try {
      text = readFileSync(join(kb.root.path, relPath), "utf8");
    } catch (error) {
      process.stdout.write(`   ! 读取失败，跳过：${relPath}（${error.message}）\n`);
      continue;
    }
    additions.push(buildCandidate(relPath, text, kb));
  }
  additions.sort((a, b) => a.path.localeCompare(b.path));

  const orphans = [];
  for (const entry of existingEntries) {
    if (!entry || typeof entry.path !== "string") continue;
    if (!existsSync(join(kb.root.path, entry.path))) orphans.push(entry.path);
  }

  const kbMeta = { id: kb.id, title: kb.title, root: kb.root.path };
  const metaChanged =
    index === null ||
    index.schema_version !== KB_SCHEMA_VERSION ||
    JSON.stringify(index.kb) !== JSON.stringify(kbMeta) ||
    index.versionNote !== VERSION_NOTE;

  const next = {
    ...(index ?? {}),
    schema_version: KB_SCHEMA_VERSION,
    generated: new Date().toISOString().slice(0, 10),
    kb: kbMeta,
    versionNote: VERSION_NOTE,
    entries: [...existingEntries, ...additions],
  };
  const nextValidation = validateIndex(next);
  const stats = nextValidation.ok ? nextValidation.stats : null;

  process.stdout.write(`   扫描：${files.length} 个 md（白名单目录 ${scanDirs.length} 个）\n`);
  process.stdout.write(`   条目：既有 ${existingEntries.length}，新增 ${additions.length}，孤儿 ${orphans.length}\n`);
  if (stats !== null) {
    process.stdout.write(
      `   分布：byTopic=${JSON.stringify(stats.byTopic)} byVersion=${JSON.stringify(stats.byVersion)}\n`,
    );
  } else {
    process.stdout.write(`   ! 整索引未通过契约校验（${nextValidation.errors.length} 项，前 5 项如下）\n`);
    for (const error of nextValidation.errors.slice(0, 5)) {
      process.stdout.write(`     ! ${error}\n`);
    }
  }
  for (const entry of additions.slice(0, 10)) {
    process.stdout.write(
      `     + ${entry.path}\n       title="${entry.title}" topic=${entry.topic} source=${entry.source} version=${JSON.stringify(entry.version)}\n`,
    );
  }
  if (additions.length > 10) {
    process.stdout.write(`     ... 另有 ${additions.length - 10} 条新增（--write 后可用 spt_kb_query 查看）\n`);
  }
  for (const orphan of orphans.slice(0, 5)) {
    process.stdout.write(`     - 孤儿：${orphan}\n`);
  }
  for (const invalid of invalidEntries) {
    for (const error of invalid.errors) {
      process.stdout.write(`     ! ${invalid.path}: ${error}\n`);
    }
  }
  process.stdout.write("\n");

  totalAdditions += additions.length;
  totalInvalid += invalidEntries.length + (stats === null ? 1 : 0);

  if (opts.write && (additions.length > 0 || metaChanged)) {
    pendingWrites.push({ kb, next, format: detectFormat(indexText), additions: additions.length });
  }
}

if (!opts.write) {
  process.stdout.write(
    `[build-index] dry-run：未写盘（加 --write 落盘）。新增合计 ${totalAdditions}，非法 ${totalInvalid}\n`,
  );
  process.exit(totalInvalid > 0 ? 1 : 0);
}

for (const { kb, next, additions, format } of pendingWrites) {
  mkdirSync(dirname(kb.index.path), { recursive: true });
  writeFileSync(kb.index.path, serialize(next, format), "utf8");
  process.stdout.write(
    `[build-index] 已写盘：${kb.index.path}（条目 ${next.entries.length}，新增 ${additions}，generated ${next.generated}）\n`,
  );
}

if (pendingWrites.length === 0) {
  process.stdout.write(
    "[build-index] 无新增条目、索引元数据一致：未写盘（保持幂等；条目是手工资产，绝不改写）\n",
  );
}

process.stdout.write(
  `[build-index] 完成：写盘 ${pendingWrites.length} 个库，新增 ${totalAdditions}，非法 ${totalInvalid}，库不可用 ${unavailableKbs}\n`,
);
process.exit(totalInvalid > 0 ? 1 : 0);
