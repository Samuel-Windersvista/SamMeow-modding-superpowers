// views.mjs — 从 all.tsv / fresh.json / themes.json 生成报告用细分视图（打印 + 存盘）
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const DATA = join(SCRIPT_DIR, "..", "data");

const rows = JSON.parse(readFileSync(join(DATA, "top.json"), "utf8")); // top 120（全量降序前 120）
const fresh = JSON.parse(readFileSync(join(DATA, "fresh.json"), "utf8"));
const allTsv = readFileSync(join(DATA, "all.tsv"), "utf8").trim().split(/\r?\n/);
const hdr = allTsv[0].split("\t");
const all = allTsv.slice(1).map((l) => {
  const c = l.split("\t");
  const o = {};
  hdr.forEach((h, i) => (o[h] = c[i]));
  return o;
});

const t = (s, n) => String(s ?? "").replace(/\s+/g, " ").trim().slice(0, n);
const line = (r, n = 90) => `#${r.id} ${r.name} | dl=${r.downloads} | ${r.category} | ${t(r.teaser, n)}`;

const out = {};
out.cheatNotice = all.filter((r) => r.cheatNote === "1");
out.ads = all.filter((r) => r.ads === "1").slice(0, 30);
out.fikaName = all.filter((r) => /fika/i.test(`${r.name} ${r.teaser}`));
out.sup416not415 = all.filter((r) => r.sup416 === "1" && r.sup415 === "0");
out.sup415not416 = all.filter((r) => r.sup415 === "1" && r.sup416 === "0");
out.freshTop = [...fresh].sort((a, b) => b.downloads - a.downloads).slice(0, 60);
out.newSinceSep = all.filter((r) => r.created >= "2026-09-01");

for (const [k, v] of Object.entries(out)) {
  writeFileSync(join(DATA, `view-${k}.json`), JSON.stringify(v, null, 1), "utf8");
}

const section = (title, arr, n = 100, limit = 999) => {
  console.log(`\n===== ${title} (${arr.length}) =====`);
  for (const r of arr.slice(0, limit)) console.log(line(r, n));
};

console.log(`总行数 ${all.length} | fresh ${fresh.length} | 9月起新建 ${out.newSinceSep.length}`);
section("cheat_notice=true", out.cheatNotice, 140);
section("fika 名义命中（名称/简介含 fika）", out.fikaName, 110, 40);
section("仅支持 4.1.6（不支持 4.1.5）", out.sup416not415, 100, 60);
section("仅支持 4.1.5（未标 4.1.6）— 前 40", out.sup415not416, 100, 40);
section("新面孔：2026-08 起发布，下载降序（前 60）", out.freshTop, 110, 60);

const cat = (name, limit = 999, n = 95) => {
  const arr = all.filter((r) => r.category === name);
  section(`类别 ${name}`, arr, n, limit);
};
cat("Overhauls", 60);
cat("Bots", 40);
cat("Quests", 40);
cat("Traders", 45);
cat("Locations", 20);
cat("Hideout", 20);
cat("Audio", 22);
cat("Equipment", 30);
cat("Items", 45);
cat("Weapons", 40);
