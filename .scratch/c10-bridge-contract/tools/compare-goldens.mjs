// =============================================================================
// C10 golden 对照（Node 辅助）
//
// 深度比较两个捕获文件（同形 JSON）；相等 exit 0，差异逐条打印并 exit 1。
// 用于：① 同一阶段 CS vs TS 交叉核对；② pre vs post 行为对照。
//
// 用法：node compare-goldens.mjs <a.json> <b.json>
// =============================================================================

import { readFileSync } from "node:fs";

const [aPath, bPath] = process.argv.slice(2);
if (!aPath || !bPath) {
  console.error("[compare-goldens] 用法：node compare-goldens.mjs <a.json> <b.json>");
  process.exit(2);
}

const a = JSON.parse(readFileSync(aPath, "utf8"));
const b = JSON.parse(readFileSync(bPath, "utf8"));

const differences = [];
compare(a, b, "$");

if (differences.length === 0) {
  console.log(`[compare-goldens] MATCH: ${aPath} == ${bPath}`);
  process.exit(0);
}

console.error(`[compare-goldens] DIFF: ${aPath} vs ${bPath}（${differences.length} 处）`);
for (const line of differences) {
  console.error("  " + line);
}
process.exit(1);

function compare(left, right, pointer) {
  if (left === right) {
    return;
  }
  if (Array.isArray(left) && Array.isArray(right)) {
    if (left.length !== right.length) {
      differences.push(`${pointer}: 数组长度 ${left.length} != ${right.length}`);
    }
    const length = Math.min(left.length, right.length);
    for (let index = 0; index < length; index += 1) {
      compare(left[index], right[index], `${pointer}[${index}]`);
    }
    return;
  }
  if (isRecord(left) && isRecord(right)) {
    const keys = new Set([...Object.keys(left), ...Object.keys(right)]);
    for (const key of keys) {
      compare(left[key], right[key], `${pointer}.${key}`);
    }
    return;
  }
  differences.push(`${pointer}: ${JSON.stringify(left)} != ${JSON.stringify(right)}`);
}

function isRecord(value) {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}
