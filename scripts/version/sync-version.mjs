#!/usr/bin/env node
// =============================================================================
// sync-version.mjs — 版本单一源传播/校验（C5 · D1）
//
// 单一源：根 package.json 的 version（由 .version-bump.json 的 source 指定）。
// 目标清单：.version-bump.json 的 files[]（kind 决定写回方式）。
//
// 模式：
//   默认       check —— 逐目标比对源版本；有漂移 → 打印清单 + exit 1；干净 → exit 0。
//   --write    write —— 把源版本写入全部目标（幂等：已一致的目标不落盘）。
//   --set <v>  配合 --write，先更新源文件再传播（可选便利）。
//
// 退出码：
//   0 = 一致 / 传播成功
//   1 = check 模式存在漂移
//   2 = 配置 / IO / 定位错误（源非 semver、目标缺失、pattern 不匹配等）
//
// 字节保真：只替换命中区域的字节，其余（含行尾风格、缩进、BOM）原样保留。
//
// 用法：
//   node scripts/version/sync-version.mjs                 # check
//   node scripts/version/sync-version.mjs --write         # 传播
//   node scripts/version/sync-version.mjs --write --set 0.3.0
// =============================================================================

import { readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(SCRIPT_DIR, "../..");
const REGISTRY_PATH = resolve(REPO_ROOT, ".version-bump.json");

const SEMVER_RE = /^\d+\.\d+\.\d+/;

const USAGE = [
  "用法: node scripts/version/sync-version.mjs [--write] [--set <version>]",
  "  默认            check：逐目标比对源版本，漂移则 exit 1",
  "  --write         把源版本写入全部目标（幂等）",
  "  --set <v>       配合 --write：先更新源文件版本再传播",
  "  --help, -h      显示本帮助",
].join("\n");

// -----------------------------------------------------------------------------
// 基础工具
// -----------------------------------------------------------------------------

/** 用法/配置错误：exit 2。 */
function fail(message) {
  process.stderr.write(`[sync-version] ${message}\n`);
  process.exit(2);
}

function usageError(message) {
  process.stderr.write(`[sync-version] ${message}\n${USAGE}\n`);
  process.exit(2);
}

/** 解析参数：支持 `--k v` 与 `--k=v`（与同目录既有脚本形态一致）。 */
function parseArgs(argv) {
  const out = { write: false, set: null };

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    const eq = arg.indexOf("=");
    const name = eq === -1 ? arg : arg.slice(0, eq);
    const inlineValue = eq === -1 ? undefined : arg.slice(eq + 1);

    if (name === "--write") {
      out.write = true;
      continue;
    }
    if (name === "--set") {
      if (inlineValue !== undefined) {
        if (inlineValue === "") usageError("参数缺少取值：--set");
        out.set = inlineValue;
        continue;
      }
      const next = argv[i + 1];
      if (next === undefined || next.startsWith("--")) usageError("参数缺少取值：--set");
      out.set = next;
      i += 1;
      continue;
    }
    if (name === "--help" || name === "-h") {
      process.stdout.write(`${USAGE}\n`);
      process.exit(0);
    }
    usageError(`未知参数：${arg}`);
  }

  if (out.set !== null && !out.write) {
    usageError("--set 需配合 --write（先更新源再传播）");
  }

  return out;
}

/** 读取目标文件；剥离 BOM 后在 text 上做定位，写回时再还原 BOM。 */
function readTextFile(absPath) {
  let raw;
  try {
    raw = readFileSync(absPath, "utf8");
  } catch (error) {
    fail(`无法读取 ${absPath}: ${error.message}`);
  }
  const hasBom = raw.charCodeAt(0) === 0xfeff;
  return { raw, text: hasBom ? raw.slice(1) : raw, hasBom };
}

/** 按替换项（从后往前）重写文本，未命中区域字节不变。 */
function applyReplacements(text, replacements) {
  const sorted = [...replacements].sort((a, b) => b.start - a.start);
  let out = text;
  for (const r of sorted) {
    out = out.slice(0, r.start) + r.value + out.slice(r.end);
  }
  return out;
}

function splitField(field) {
  return String(field).split(".");
}

// -----------------------------------------------------------------------------
// JSON 定位（轻量扫描器：只找值区间，不做整文件序列化）
// -----------------------------------------------------------------------------

function skipWs(text, i) {
  while (i < text.length) {
    const c = text[i];
    if (c === " " || c === "\t" || c === "\n" || c === "\r") i += 1;
    else break;
  }
  return i;
}

/** text[i] 为 `"`；返回闭合引号之后的下标。 */
function scanStringEnd(text, i) {
  i += 1;
  while (i < text.length) {
    const c = text[i];
    if (c === "\\") {
      i += 2;
      continue;
    }
    if (c === '"') return i + 1;
    i += 1;
  }
  return text.length;
}

/** text[i] 为值起点；返回该值之后的下标。 */
function scanValueEnd(text, i) {
  const c = text[i];
  if (c === '"') return scanStringEnd(text, i);
  if (c === "{" || c === "[") {
    let depth = 0;
    let j = i;
    while (j < text.length) {
      const ch = text[j];
      if (ch === '"') {
        j = scanStringEnd(text, j);
        continue;
      }
      if (ch === "{" || ch === "[") depth += 1;
      else if (ch === "}" || ch === "]") {
        depth -= 1;
        if (depth === 0) return j + 1;
      }
      j += 1;
    }
    return text.length;
  }
  // number / true / false / null
  let j = i;
  while (j < text.length && !/[\s,}\]]/.test(text[j])) j += 1;
  return j;
}

/** 在对象 `{` 处查找 key，返回其值起点；未命中返回 -1。 */
function findObjectValueStart(text, objStart, key) {
  let i = skipWs(text, objStart + 1);
  while (i < text.length && text[i] !== "}") {
    if (text[i] !== '"') return -1;
    const keyEnd = scanStringEnd(text, i);
    let rawKey;
    try {
      rawKey = JSON.parse(text.slice(i, keyEnd));
    } catch {
      return -1;
    }
    i = skipWs(text, keyEnd);
    if (text[i] !== ":") return -1;
    i = skipWs(text, i + 1);
    if (rawKey === key) return i;
    i = scanValueEnd(text, i);
    i = skipWs(text, i);
    if (text[i] === ",") {
      i = skipWs(text, i + 1);
      continue;
    }
    break;
  }
  return -1;
}

/** 在数组 `[` 处查找下标 index 的元素值起点；未命中返回 -1。 */
function findArrayElementStart(text, arrStart, index) {
  let i = skipWs(text, arrStart + 1);
  let n = 0;
  while (i < text.length && text[i] !== "]") {
    if (n === index) return i;
    i = scanValueEnd(text, i);
    i = skipWs(text, i);
    if (text[i] === ",") {
      i = skipWs(text, i + 1);
      n += 1;
      continue;
    }
    break;
  }
  return -1;
}

/** 点路径定位值区间；数字段 → 数组下标。 */
function locateJsonSpan(text, segments) {
  let pos = skipWs(text, 0);
  for (const segment of segments) {
    if (text[pos] === "{") {
      pos = findObjectValueStart(text, pos, String(segment));
    } else if (text[pos] === "[") {
      const index = Number(segment);
      if (!Number.isInteger(index) || index < 0) return null;
      pos = findArrayElementStart(text, pos, index);
    } else {
      return null;
    }
    if (pos === -1) return null;
    pos = skipWs(text, pos);
  }
  return { start: pos, end: scanValueEnd(text, pos) };
}

// -----------------------------------------------------------------------------
// TOML / XML / code 定位
// -----------------------------------------------------------------------------

/** [project] 区内的 `version = "..."` 值区间。 */
function locateTomlVersionValue(text) {
  const header = /^[ \t]*\[project\][ \t]*$/m.exec(text);
  if (!header) return null;
  const sectionStart = header.index + header[0].length;
  const rest = text.slice(sectionStart);
  const nextHeader = /^[ \t]*\[/m.exec(rest);
  const sectionEnd = nextHeader ? sectionStart + nextHeader.index : text.length;
  const section = text.slice(sectionStart, sectionEnd);
  const line = /^([ \t]*version[ \t]*=[ \t]*")([^"]*)(")/m.exec(section);
  if (!line) return null;
  const start = sectionStart + line.index + line[1].length;
  return { start, end: start + line[2].length };
}

/** 首个 <Version>...</Version> 的值区间。 */
function locateXmlVersionValue(text) {
  const match = /<Version>([^<]*)<\/Version>/.exec(text);
  if (!match) return null;
  const start = match.index + "<Version>".length;
  return { start, end: start + match[1].length };
}

/** 按注册 pattern 的首个匹配，取捕获组 1 的值区间。 */
function locateCodeValue(text, pattern) {
  let regex;
  try {
    regex = new RegExp(pattern, "d");
  } catch (error) {
    throw new Error(`pattern 非法：${pattern}（${error.message}）`);
  }
  const match = regex.exec(text);
  if (!match) return null;
  if (!match.indices || !match.indices[1]) return null;
  const [start, end] = match.indices[1];
  return { start, end };
}

// -----------------------------------------------------------------------------
// 目标读写
// -----------------------------------------------------------------------------

/** 返回 { spans:[{start,end}], values:[string] }；定位失败抛错。 */
function locateTarget(entry, text) {
  switch (entry.kind) {
    case "json": {
      const span = locateJsonSpan(text, splitField(entry.field));
      if (!span) throw new Error(`JSON 路径未命中：${entry.field}`);
      return { spans: [span], values: [JSON.parse(text.slice(span.start, span.end))] };
    }
    case "npm-lock": {
      const top = locateJsonSpan(text, ["version"]);
      const root = locateJsonSpan(text, ["packages", "", "version"]);
      if (!top || !root) throw new Error("lockfile 缺少顶层 version 或 packages[\"\"] .version");
      return {
        spans: [top, root],
        values: [
          JSON.parse(text.slice(top.start, top.end)),
          JSON.parse(text.slice(root.start, root.end)),
        ],
      };
    }
    case "toml": {
      const span = locateTomlVersionValue(text);
      if (!span) throw new Error("[project] 区未找到 version 行");
      return { spans: [span], values: [text.slice(span.start, span.end)] };
    }
    case "xml": {
      const span = locateXmlVersionValue(text);
      if (!span) throw new Error("未找到 <Version> 元素");
      return { spans: [span], values: [text.slice(span.start, span.end)] };
    }
    case "code": {
      const span = locateCodeValue(text, entry.pattern);
      if (!span) throw new Error(`pattern 未命中：${entry.pattern}`);
      return { spans: [span], values: [text.slice(span.start, span.end)] };
    }
    default:
      throw new Error(`未知 kind：${entry.kind}`);
  }
}

function jsonString(value) {
  return JSON.stringify(value);
}

function describeTarget(entry) {
  if (entry.kind === "npm-lock") return `${entry.path} :: version + packages[""].version`;
  if (entry.kind === "json" || entry.kind === "code") return `${entry.path} :: ${entry.field ?? entry.pattern}`;
  return `${entry.path}`;
}

// -----------------------------------------------------------------------------
// 主流程
// -----------------------------------------------------------------------------

function loadRegistry() {
  let raw;
  try {
    raw = readFileSync(REGISTRY_PATH, "utf8");
  } catch (error) {
    fail(`无法读取 ${REGISTRY_PATH}: ${error.message}`);
  }
  let registry;
  try {
    registry = JSON.parse(raw.replace(/^\uFEFF/, ""));
  } catch (error) {
    fail(`.version-bump.json 解析失败: ${error.message}`);
  }
  if (!registry || typeof registry !== "object") fail(".version-bump.json 顶层必须是对象");
  if (!registry.source || typeof registry.source.path !== "string") {
    fail(".version-bump.json 缺少 source.path");
  }
  if (!Array.isArray(registry.files)) fail(".version-bump.json 缺少 files[]");
  return registry;
}

function readSourceVersion(registry) {
  const sourceField = registry.source.field ?? "version";
  const absPath = resolve(REPO_ROOT, registry.source.path);
  const { text } = readTextFile(absPath);
  let parsed;
  try {
    parsed = JSON.parse(text);
  } catch (error) {
    fail(`源文件解析失败 ${registry.source.path}: ${error.message}`);
  }
  const segments = splitField(sourceField);
  let current = parsed;
  for (const segment of segments) {
    if (current == null || typeof current !== "object") fail(`源路径未命中：${sourceField}`);
    current = current[segment];
  }
  if (typeof current !== "string") fail(`源版本不是字符串：${registry.source.path}#${sourceField}`);
  if (!SEMVER_RE.test(current)) fail(`源版本不是 semver：${current}`);
  return { version: current, absPath, field: sourceField };
}

/** --set：更新源文件版本（JSON 值区间替换）。 */
function writeSourceVersion(registry, newVersion) {
  const sourceField = registry.source.field ?? "version";
  const absPath = resolve(REPO_ROOT, registry.source.path);
  const { raw, text, hasBom } = readTextFile(absPath);
  const span = locateJsonSpan(text, splitField(sourceField));
  if (!span) fail(`源路径未命中：${sourceField}`);
  const out = (hasBom ? "\uFEFF" : "") + text.slice(0, span.start) + jsonString(newVersion) + text.slice(span.end);
  if (out !== raw) writeFileSync(absPath, out, "utf8");
}

function run() {
  const args = parseArgs(process.argv.slice(2));
  const registry = loadRegistry();

  if (args.set !== null) {
    if (!SEMVER_RE.test(args.set)) fail(`--set 值不是 semver：${args.set}`);
    writeSourceVersion(registry, args.set);
    process.stdout.write(`[source] ${registry.source.path} :: ${registry.source.field ?? "version"} -> ${args.set}\n`);
  }

  const source = readSourceVersion(registry);
  const mode = args.write ? "write" : "check";
  const version = source.version;

  let driftCount = 0;
  let writeCount = 0;
  let okCount = 0;
  let errorCount = 0;

  for (const entry of registry.files) {
    if (!entry || typeof entry.path !== "string" || typeof entry.kind !== "string") {
      errorCount += 1;
      process.stderr.write(`[error] 注册表条目非法（缺 path/kind）：${JSON.stringify(entry)}\n`);
      continue;
    }

    const absPath = resolve(REPO_ROOT, entry.path);
    const label = describeTarget(entry);

    let raw;
    let text;
    let hasBom;
    try {
      ({ raw, text, hasBom } = readTextFile(absPath));
    } catch (error) {
      errorCount += 1;
      process.stderr.write(`[error] ${entry.path} :: ${error.message}\n`);
      continue;
    }

    let located;
    try {
      located = locateTarget(entry, text);
    } catch (error) {
      errorCount += 1;
      process.stderr.write(`[error] ${entry.path} :: ${error.message}\n`);
      continue;
    }

    const drifted = located.values.filter((value) => value !== version);

    if (mode === "check") {
      if (drifted.length === 0) {
        okCount += 1;
        process.stdout.write(`[ok]    ${label} = ${version}\n`);
      } else {
        driftCount += 1;
        process.stdout.write(
          `[drift] ${label} (expected ${version}, found ${located.values.join(", ")})\n`,
        );
      }
      continue;
    }

    // write 模式
    if (drifted.length === 0) {
      okCount += 1;
      process.stdout.write(`[ok]    ${label} = ${version}（已一致）\n`);
      continue;
    }

    // JSON 类目标需带引号的字符串字面量；toml/xml/code 写原始值。
    const encoded =
      entry.kind === "json" || entry.kind === "npm-lock" ? jsonString(version) : version;
    const replacements = located.spans.map((span) => ({
      start: span.start,
      end: span.end,
      value: encoded,
    }));
    const out = (hasBom ? "\uFEFF" : "") + applyReplacements(text, replacements);

    if (out !== raw) {
      writeFileSync(absPath, out, "utf8");
      writeCount += 1;
      process.stdout.write(`[write] ${label} (${located.values.join(", ")} -> ${version})\n`);
    } else {
      okCount += 1;
      process.stdout.write(`[ok]    ${label} = ${version}（已一致）\n`);
    }
  }

  process.stdout.write("\n");
  if (errorCount > 0) {
    process.stdout.write(
      `版本同步失败：${errorCount} 个目标读取/定位失败（源版本 ${version}）。\n`,
    );
    process.exit(2);
  }
  if (mode === "check") {
    if (driftCount > 0) {
      process.stdout.write(
        `版本漂移：${driftCount} 个目标与源版本 ${version} 不一致（共 ${registry.files.length} 个目标）。\n`,
      );
      process.exit(1);
    }
    process.stdout.write(`版本一致：${okCount} 个目标全部匹配源版本 ${version}。\n`);
    process.exit(0);
  }
  process.stdout.write(
    `已传播源版本 ${version}：${writeCount} 个目标写入，${okCount} 个已一致。\n`,
  );
  process.exit(0);
}

run();
