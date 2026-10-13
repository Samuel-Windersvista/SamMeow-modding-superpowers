// sections.mjs — 分类分节落盘（UTF-8 直写，避免 PS 管道编码问题）
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const DATA = join(SCRIPT_DIR, "..", "data");
const allTsv = readFileSync(join(DATA, "all.tsv"), "utf8").trim().split(/\r?\n/);
const hdr = allTsv[0].split("\t");
const all = allTsv.slice(1).map((l) => {
  const c = l.split("\t");
  const o = {};
  hdr.forEach((h, i) => (o[h] = c[i]));
  return o;
});
const t = (s, n) => String(s ?? "").replace(/\s+/g, " ").trim().slice(0, n);
const line = (r, n = 110) => `#${r.id} ${r.name} | dl=${r.downloads} | fika=${r.fika} | 415=${r.sup415} 416=${r.sup416} | ${t(r.teaser, n)}`;

const parts = [];
const cat = (name, limit = 999) => {
  const arr = all.filter((r) => r.category === name);
  parts.push(`\n===== 类别 ${name} (${arr.length}) =====`);
  for (const r of arr.slice(0, limit)) parts.push(line(r));
};
cat("Overhauls");
cat("Bots");
cat("Quests");
cat("Traders");
cat("Locations");
cat("Hideout");
cat("Audio");
cat("Equipment");
cat("Items", 40);
cat("Weapons", 40);
cat("Tools", 40);
cat("Scripting", 40);
cat("Other", 60);
cat("Clothing");
cat("Models");
cat("Retextures");
cat("Locales");

// 4.1.5 支持且下载量前 150（报告主候选池）
parts.push(`\n===== 支持 4.1.5 + 下载前 150 =====`);
for (const r of all.filter((x) => x.sup415 === "1").slice(0, 150)) parts.push(line(r, 100));

writeFileSync(join(DATA, "sections.txt"), parts.join("\n") + "\n", "utf8");
console.log(`written sections.txt (${parts.length} lines)`);
