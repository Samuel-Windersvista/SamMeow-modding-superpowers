// 从 13 章 prose 机械提取规则元数据（C8 车道 A 步骤 1）
//
// 解析 `### STD-<DOMAIN>-<nnn> — <title>` + `- **Level:**` + `- **Applies:**`，
// 输出 JSON（prose 顺序）：[{ id, domain, level, applies, title, chapter }]
//
// 用法: node .scratch/c8-rules-registry/tools/extract-rules.mjs [outPath]
import { readFileSync, writeFileSync, readdirSync } from "node:fs";
import { resolve } from "node:path";

const STD_DIR = resolve("knowledge/spt-kb/curated/modding-standard");

/** 13 章：01-*.md ... 13-*.md（排除 README / evidence-index / version-matrix） */
const chapters = readdirSync(STD_DIR)
  .filter((f) => /^\d{2}-.*\.md$/.test(f))
  .sort();

const rules = [];
const domainMap = {};

for (const chapter of chapters) {
  const text = readFileSync(resolve(STD_DIR, chapter), "utf8");
  const slugMatch = /Domain slug:\*\*\s*`([A-Z]+)`/.exec(text);
  if (!slugMatch) throw new Error(`${chapter}: 未找到 Domain slug`);
  domainMap[slugMatch[1]] = chapter;

  const lines = text.split(/\r?\n/);
  for (let i = 0; i < lines.length; i++) {
    const heading = /^###\s+(STD-[A-Z]+-\d{3})\s+—\s+(.+?)\s*$/.exec(lines[i]);
    if (!heading) continue;
    const id = heading[1];
    const title = heading[2];

    let level = null;
    let applies = null;
    for (let j = i + 1; j < lines.length && j < i + 12; j++) {
      if (/^###\s/.test(lines[j])) break;
      const lm = /^-\s+\*\*Level:\*\*\s*(.+?)\s*$/.exec(lines[j]);
      if (lm) level = lm[1];
      const am = /^-\s+\*\*Applies:\*\*\s*(.+?)\s*$/.exec(lines[j]);
      if (am) applies = am[1];
    }

    const domain = id.split("-")[1];
    if (!level) throw new Error(`${id}: 缺 Level`);
    if (!applies) throw new Error(`${id}: 缺 Applies`);
    if (domainMap[domain] && domainMap[domain] !== chapter) {
      throw new Error(`${id}: domain ${domain} 出现在 ${chapter}，但已登记于 ${domainMap[domain]}`);
    }

    rules.push({ id, domain, level, applies, title, chapter });
  }
}

// 统计
const byLevel = {};
const byDomain = {};
for (const r of rules) {
  byLevel[r.level] = (byLevel[r.level] ?? 0) + 1;
  byDomain[r.domain] = (byDomain[r.domain] ?? 0) + 1;
}
const dupes = rules.map((r) => r.id).filter((id, i, arr) => arr.indexOf(id) !== i);

console.log(`chapters=${chapters.length} rules=${rules.length}`);
console.log(`byLevel=${JSON.stringify(byLevel)}`);
console.log(`byDomain=${JSON.stringify(byDomain)}`);
console.log(`duplicates=${dupes.length}${dupes.length ? " -> " + dupes.join(",") : ""}`);
console.log(`domains=${JSON.stringify(domainMap)}`);

const outPath = process.argv[2];
if (outPath) {
  writeFileSync(resolve(outPath), JSON.stringify(rules, null, 2) + "\n", "utf8");
  console.log(`written -> ${outPath}`);
}
