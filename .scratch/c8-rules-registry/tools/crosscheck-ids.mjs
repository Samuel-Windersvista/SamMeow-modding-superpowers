// prose ↔ 检查器 ID 交叉核对（C8 车道 A）
import { readFileSync, readdirSync } from "node:fs";
import { resolve } from "node:path";

const STD_DIR = resolve("knowledge/spt-kb/curated/modding-standard");
const rules = JSON.parse(
  readFileSync(resolve(".scratch/c8-rules-registry/rules-extracted.json"), "utf8"),
);
const proseIds = new Set(rules.map((r) => r.id));

const checker = readFileSync(resolve("scripts/check-mod-standard.ps1"), "utf8");
const checkerIds = [...checker.matchAll(/Resolve-Status\s+"(STD-[A-Z]+-\d{3})"/g)].map((m) => m[1]);
const uniq = [...new Set(checkerIds)];

console.log(`prose ids=${proseIds.size}  checker unique ids=${uniq.length} (occurrences=${checkerIds.length})`);
console.log(`checker 输出顺序:\n  ${uniq.join(", ")}`);
console.log(`\n[checker 有 / prose 无] ${uniq.filter((id) => !proseIds.has(id)).join(", ") || "(none)"}`);

// prose 全文（13 章 + README）里是否提到 checker 的 ID
const allText = readdirSync(STD_DIR)
  .filter((f) => f.endsWith(".md"))
  .map((f) => readFileSync(resolve(STD_DIR, f), "utf8"))
  .join("\n");

for (const id of uniq) {
  const inHeadings = proseIds.has(id);
  const mentions = [...allText.matchAll(new RegExp(id, "g"))].length;
  if (!inHeadings) console.log(`  !! ${id}: 无 prose 标题，正文提及 ${mentions} 次`);
}

// 反向：prose 标题里的 ID 是否出现在检查器
const notInChecker = rules.filter((r) => !uniq.includes(r.id));
console.log(`\n[prose 有 / checker 无] ${notInChecker.length} 条（预期：非可检子集）`);
