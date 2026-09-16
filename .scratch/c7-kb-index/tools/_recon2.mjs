// 一次性只读侦察（续）：精确分类
import { readFileSync, readdirSync } from "node:fs";
import path from "node:path";

const root = "knowledge/spt-kb";
const idx = JSON.parse(readFileSync(path.join(root, "index.json"), "utf8"));
const scanDirs = ["wiki", "wiki-tushonka", "curated"];
const files = [];
for (const d of scanDirs) {
  const walk = (p) => {
    for (const e of readdirSync(p, { withFileTypes: true })) {
      const f = path.join(p, e.name);
      if (e.isDirectory()) walk(f);
      else files.push(path.relative(root, f).split(path.sep).join("/"));
    }
  };
  walk(path.join(root, d));
}
const entryPaths = new Set(idx.entries.map((e) => e.path));

const mdFiles = files.filter((f) => f.endsWith(".md"));
const mdWithoutEntry = mdFiles.filter((f) => !entryPaths.has(f));
console.log(`md files = ${mdFiles.length}; md WITHOUT entry = ${mdWithoutEntry.length}`);
for (const f of mdWithoutEntry) console.log("  MD-NEW: " + f);

const entriesWithExistingFile = idx.entries.filter((e) => files.includes(e.path));
console.log(`\nentries whose file exists in scan dirs = ${entriesWithExistingFile.length}`);
const ext = {};
for (const e of entriesWithExistingFile) {
  const x = path.extname(e.path) || "<none>";
  ext[x] = (ext[x] || 0) + 1;
}
console.log("  by extension: " + JSON.stringify(ext));

// 条目重复路径？
const seen = new Map();
for (const e of idx.entries) seen.set(e.path, (seen.get(e.path) || 0) + 1);
const dups = [...seen.entries()].filter(([, c]) => c > 1);
console.log(`\nduplicate entry paths = ${dups.length}`);
for (const [p, c] of dups) console.log(`  ${p} x${c}`);

// 各目录条目数 vs 文件数
for (const d of scanDirs) {
  const fAll = files.filter((f) => f.startsWith(d + "/"));
  const fMd = fAll.filter((f) => f.endsWith(".md"));
  const eN = idx.entries.filter((e) => e.path.startsWith(d + "/")).length;
  console.log(`${d}: entries=${eN} files=${fAll.length} md=${fMd.length}`);
}
