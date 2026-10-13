// diag-versions.mjs —— 针对未决条目，输出各候选 id 的最新版本+约束
import { readFileSync } from "node:fs";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const VDIR = join(SCRATCH, "forge-versions");
const chosen = JSON.parse(readFileSync(join(SCRATCH, "forge-chosen.json"), "utf8"));
const report = JSON.parse(readFileSync(join(SCRATCH, "selection-report.json"), "utf8"));

const unresolved = report.filter((r) => !r.selected);
console.log("unresolved entries:", unresolved.length);
for (const r of unresolved) {
  const e = chosen.entries.find((x) => x.idx === r.idx);
  console.log(`\n#${r.idx} ${r.entry}${e?.flag ? " [flag:" + e.flag + "]" : ""}`);
  for (const cid of e?.candidates ?? []) {
    let arr = [];
    try { arr = JSON.parse(readFileSync(join(VDIR, `${cid}.json`), "utf8"))?.data ?? []; } catch {}
    if (!arr.length) { console.log(`   id=${cid} (no data)`); continue; }
    const sorted = [...arr].sort((a, b) => String(b.published_at ?? "").localeCompare(String(a.published_at ?? "")));
    for (const v of sorted.slice(0, 3)) console.log(`   id=${cid} ${v.version} | ${v.spt_version_constraint} | ${(v.published_at ?? "").slice(0, 10)}`);
  }
  if (!(e?.candidates ?? []).length) console.log("   (no candidates - manual)");
}
