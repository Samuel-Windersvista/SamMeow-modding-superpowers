// =============================================================================
// C10 pre/post 捕获（TS 端）
//
// 对固定语料（.scratch/c10-bridge-contract/tools/corpus/）运行 MCP 侧实现，
// 写出与 C# 捕获同形的 golden：
//   - ts-normalization.json：{ cases: [{ name, input, output }] }
//   - ts-aggregation.json  ：{ cases: [{ name, groups: [...], overflowDropped }] }
//
// 依赖已构建的 dist（脚本调用方先跑 npm run build）；导入 dist 而非 src，
// 使捕获对象与发布产物一致，且 dist 与 src 同深度，契约路径解析一致。
//
// 用法：node capture-ts.mjs <输出目录>
// =============================================================================

import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(here, "..", "..", "..");
const corpusDir = path.join(here, "corpus");
const mcpDist = path.join(repoRoot, "tools", "tarkov-runtime-mcp", "dist");

const outputDir = process.argv[2];
if (!outputDir) {
  console.error("[capture-ts] 缺少输出目录参数：node capture-ts.mjs <输出目录>");
  process.exit(2);
}

const { normalizeLogText } = await import(
  pathToFileURL(path.join(mcpDist, "logs", "log-normalizer.js")).href
);
const { LogSummaryAggregator, MAX_LOG_GROUPS } = await import(
  pathToFileURL(path.join(mcpDist, "logs", "log-aggregator.js")).href
);

function readJson(fileName) {
  return JSON.parse(readFileSync(path.join(corpusDir, fileName), "utf8"));
}

function writeJson(fileName, document) {
  mkdirSync(outputDir, { recursive: true });
  writeFileSync(path.join(outputDir, fileName), JSON.stringify(document, null, 2) + "\n", "utf8");
}

// ---- 归一化 -----------------------------------------------------------------

const normalizationCorpus = readJson("normalization-cases.json");
const normalizationDoc = {
  cases: normalizationCorpus.cases.map((testCase) => ({
    name: testCase.name,
    input: testCase.input,
    output: normalizeLogText(testCase.input),
  })),
};
writeJson("ts-normalization.json", normalizationDoc);

// ---- 聚合 -------------------------------------------------------------------

const aggregationCorpus = readJson("aggregation-cases.json");
const aggregationDoc = {
  cases: aggregationCorpus.cases.map((testCase) => {
    const maxGroups = testCase.maxGroups ?? MAX_LOG_GROUPS;
    const aggregator = new LogSummaryAggregator(maxGroups);

    for (const entry of testCase.entries) {
      aggregator.observe(entry);
    }

    const sinceMs = testCase.since === undefined ? null : Date.parse(testCase.since);
    const snapshot = aggregator.snapshot(sinceMs);

    return {
      name: testCase.name,
      groups: snapshot.groups.map((group) => ({
        key: group.key,
        level: group.level,
        source: group.source,
        count: group.count,
        firstTs: group.firstTs,
        lastTs: group.lastTs,
        sampleText: group.sampleText,
      })),
      overflowDropped: snapshot.overflowDropped,
    };
  }),
};
writeJson("ts-aggregation.json", aggregationDoc);

console.log(
  `[capture-ts] 写出 ${normalizationDoc.cases.length} 条归一化 + ${aggregationDoc.cases.length} 条聚合 → ${outputDir}`,
);
