// =============================================================================
// C10 共享夹具生成（从 pre 捕获产出，禁止手工编辑期望值）
//
// 输入：
//   .scratch/c10-bridge-contract/tools/corpus/*.json   （固定输入）
//   .scratch/c10-bridge-contract/goldens/pre/cs-*.json （C# 捕获，期望值来源）
//   .scratch/c10-bridge-contract/goldens/pre/ts-*.json （TS 捕获，交叉核对）
// 输出：
//   shared/bridge-contract/fixtures/log-normalization.json
//   shared/bridge-contract/fixtures/log-aggregation.json
//   shared/bridge-contract/fixtures/payloads/*.json
//
// 用法：node build-fixtures.mjs
// =============================================================================

import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(here, "..", "..", "..");
const corpusDir = path.join(here, "corpus");
const preDir = path.join(repoRoot, ".scratch", "c10-bridge-contract", "goldens", "pre");
const fixturesDir = path.join(repoRoot, "shared", "bridge-contract", "fixtures");
const payloadsDir = path.join(fixturesDir, "payloads");

const readJson = (file) => JSON.parse(readFileSync(file, "utf8"));

// 契约空白集合（含 U+0085 对抗位）中的不可见码点在 JSON 源里落成 \uXXXX 转义，
// 便于 review 与 diff；对 ASCII 内容（payload / 聚合夹具）无影响。
// 注意不含 U+000A / U+000D——JSON.stringify 已把字符串内的换行转义，源码中的原始
// 换行是缩进格式，误转义会把整份夹具压成一行。
const INVISIBLE_WHITESPACE = /[\u0085\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000\ufeff]/g;

const writeJson = (file, document) => {
  mkdirSync(path.dirname(file), { recursive: true });
  const json = JSON.stringify(document, null, 2).replace(
    INVISIBLE_WHITESPACE,
    (char) => "\\u" + char.codePointAt(0).toString(16).padStart(4, "0"),
  );
  writeFileSync(file, json + "\n", "utf8");
};

function indexByName(cases) {
  const map = new Map();
  for (const testCase of cases) {
    map.set(testCase.name, testCase);
  }
  return map;
}

function assertEqual(name, left, right) {
  const a = JSON.stringify(left);
  const b = JSON.stringify(right);
  if (a !== b) {
    throw new Error(`双端不一致（${name}）：\n  cs=${a}\n  ts=${b}`);
  }
}

// ---- 归一化夹具 -------------------------------------------------------------

const normalizationCorpus = readJson(path.join(corpusDir, "normalization-cases.json"));
const csNormalization = indexByName(readJson(path.join(preDir, "cs-normalization.json")).cases);
const tsNormalization = indexByName(readJson(path.join(preDir, "ts-normalization.json")).cases);

// 对抗向量（修复轮 F1/F2）：空白集合收敛 + ECMAScript 词类对齐。
// 不经 pre 捕获（语料冻结），期望值由两端测试套件共同验证（C# dotnet test / TS npm test）。
const adversarialNormalization = readJson(path.join(fixturesDir, "adversarial-normalization.json"));

const normalizationFixture = {
  $comment:
    "C10 共享夹具：日志归一化向量。前段期望值来自 pre 捕获（两端交叉核对一致），" +
    "尾段为对抗向量（空白集合 / ECMAScript 词类对齐，见 fixtures/adversarial-normalization.json），" +
    "由 .scratch/c10-bridge-contract/tools/build-fixtures.mjs 生成；请勿手工编辑。",
  cases: [
    ...normalizationCorpus.cases.map((testCase) => {
      const cs = csNormalization.get(testCase.name);
      const ts = tsNormalization.get(testCase.name);
      if (cs === undefined || ts === undefined) {
        throw new Error(`捕获缺少用例：${testCase.name}`);
      }
      assertEqual(testCase.name, cs.output, ts.output);
      return { name: testCase.name, input: testCase.input, expected: cs.output };
    }),
    ...adversarialNormalization.cases.map((testCase) => ({
      name: testCase.name,
      input: testCase.input,
      expected: testCase.expected,
    })),
  ],
};
writeJson(path.join(fixturesDir, "log-normalization.json"), normalizationFixture);

// ---- 聚合夹具 ---------------------------------------------------------------

const aggregationCorpus = readJson(path.join(corpusDir, "aggregation-cases.json"));
const csAggregation = indexByName(readJson(path.join(preDir, "cs-aggregation.json")).cases);
const tsAggregation = indexByName(readJson(path.join(preDir, "ts-aggregation.json")).cases);

const aggregationFixture = {
  $comment:
    "C10 共享夹具：日志聚合向量（wire 形状；ts 为 C# 往返格式以保证双端逐字节可比）。" +
    "期望值来自 pre 捕获（两端交叉核对一致），由 .scratch/c10-bridge-contract/tools/build-fixtures.mjs 生成；请勿手工编辑。",
  cases: aggregationCorpus.cases.map((testCase) => {
    const cs = csAggregation.get(testCase.name);
    const ts = tsAggregation.get(testCase.name);
    if (cs === undefined || ts === undefined) {
      throw new Error(`捕获缺少用例：${testCase.name}`);
    }
    assertEqual(testCase.name, cs, ts);

    const fixtureCase = { name: testCase.name };
    if (testCase.maxGroups !== undefined) {
      fixtureCase.maxGroups = testCase.maxGroups;
    }
    if (testCase.since !== undefined) {
      fixtureCase.since = testCase.since;
    }
    fixtureCase.entries = testCase.entries;
    fixtureCase.expected = { groups: cs.groups, overflowDropped: cs.overflowDropped };
    return fixtureCase;
  }),
};
writeJson(path.join(fixturesDir, "log-aggregation.json"), aggregationFixture);

// ---- payload 夹具 -----------------------------------------------------------

const csPayloads = readJson(path.join(preDir, "cs-payloads.json"));
for (const entry of csPayloads.bodies) {
  mkdirSync(payloadsDir, { recursive: true });
  writeFileSync(path.join(payloadsDir, `${entry.name}.json`), entry.body + "\n", "utf8");
}

const errorsFixture = {};
for (const entry of csPayloads.errors) {
  errorsFixture[entry.name] = entry.body;
}
writeJson(path.join(payloadsDir, "errors.json"), errorsFixture);

console.log(
  `[build-fixtures] 归一化 ${normalizationFixture.cases.length} 例 + 聚合 ${aggregationFixture.cases.length} 例 + ` +
    `payload ${csPayloads.bodies.length} 份 + errors ${csPayloads.errors.length} 项 → ${fixturesDir}`,
);
