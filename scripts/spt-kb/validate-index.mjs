#!/usr/bin/env node
// =============================================================================
// validate-index.mjs — KB 索引契约校验（C7）
//
// 薄包装：读 index.json → 调 spt-mcp dist 里的 validateIndex（单一契约实现）
//        → 打印统计与错误 → exit 0/1。
//
// 契约实现位于 tools/spt-mcp/src/kb/contract.ts（编译到 dist/kb/contract.js）。
// 因此本脚本要求 spt-mcp 已构建（dist 存在）。
//
// 退出码：
//   0 = 契约通过
//   1 = 契约失败 / 索引不可读 / 无法导入契约实现（dist 缺失）
//   2 = 用法错误（未知参数 / 参数缺少取值）
//
// 用法：
//   node scripts/spt-kb/validate-index.mjs [--index <path>]
//   缺省 index = knowledge/spt-kb/index.json（相对本脚本的仓库/便携根）
// =============================================================================

import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const DEFAULT_INDEX = resolve(SCRIPT_DIR, "../../knowledge/spt-kb/index.json");
const CONTRACT_URL = new URL("../../tools/spt-mcp/dist/kb/index.js", import.meta.url);

const USAGE = [
  "用法: node scripts/spt-kb/validate-index.mjs [--index <path>]",
  "  --index <path>   索引文件路径（缺省 knowledge/spt-kb/index.json）",
  "  --help, -h       显示本帮助",
].join("\n");

function usageError(message) {
  process.stderr.write(`[validate-index] ${message}\n${USAGE}\n`);
  process.exit(2);
}

/** 参数解析：支持 `--k v` 与 `--k=v`（与同目录既有脚本形态一致） */
function parseArgs(argv) {
  const out = { indexPath: null };

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    const eq = arg.indexOf("=");
    const name = eq === -1 ? arg : arg.slice(0, eq);
    const inlineValue = eq === -1 ? undefined : arg.slice(eq + 1);

    if (name === "--index") {
      if (inlineValue !== undefined) {
        if (inlineValue === "") usageError("参数缺少取值：--index");
        out.indexPath = inlineValue;
        continue;
      }
      const next = argv[i + 1];
      if (next === undefined || next.startsWith("--")) usageError("参数缺少取值：--index");
      i += 1;
      out.indexPath = next;
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
const indexPath = opts.indexPath ? resolve(opts.indexPath) : DEFAULT_INDEX;

/** 导入契约实现；dist 缺失时给出明确构建提示 */
async function loadContract() {
  try {
    return await import(CONTRACT_URL.href);
  } catch (error) {
    process.stderr.write(
      `[validate-index] 无法导入契约实现：${CONTRACT_URL.href}\n` +
        `[validate-index] ${error instanceof Error ? error.message : String(error)}\n` +
        "[validate-index] 请先构建：npm --prefix tools/spt-mcp run build\n",
    );
    process.exit(1);
  }
}

function readIndex(path) {
  try {
    return readFileSync(path, "utf8").replace(/^\uFEFF/, "");
  } catch (error) {
    process.stderr.write(
      `[validate-index] 无法读取索引：${path}\n` +
        `[validate-index] ${error instanceof Error ? error.message : String(error)}\n`,
    );
    process.exit(1);
  }
}

const contract = await loadContract();
const { validateIndex, KB_SCHEMA_VERSION } = contract;

const rawText = readIndex(indexPath);
let raw;
try {
  raw = JSON.parse(rawText);
} catch (error) {
  process.stderr.write(
    `[validate-index] 索引不是合法 JSON：${indexPath}\n` +
      `[validate-index] ${error instanceof Error ? error.message : String(error)}\n`,
  );
  process.exit(1);
}

const result = validateIndex(raw);

if (!result.ok) {
  process.stderr.write(`[validate-index] FAIL：${indexPath}\n`);
  for (const error of result.errors) process.stderr.write(`  - ${error}\n`);
  process.stderr.write(`[validate-index] 共 ${result.errors.length} 个契约错误\n`);
  process.exit(1);
}

const stats = result.stats;
process.stdout.write(`[validate-index] OK：${indexPath}\n`);
process.stdout.write(`[validate-index] schema_version=${KB_SCHEMA_VERSION} total=${stats.total}\n`);
process.stdout.write(`  bySource  = ${JSON.stringify(stats.bySource)}\n`);
process.stdout.write(`  byDomain  = ${JSON.stringify(stats.byDomain)}\n`);
process.stdout.write(`  byVersion = ${JSON.stringify(stats.byVersion)}\n`);
process.stdout.write(`  byTopic   = ${JSON.stringify(stats.byTopic)}\n`);
process.stdout.write(`  withKeywords=${stats.withKeywords} withSummary=${stats.withSummary}\n`);
process.exit(0);
