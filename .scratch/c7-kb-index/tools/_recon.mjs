// 一次性只读侦察：index.json 条目 vs 磁盘源文件（C7 车道 A 迁移前基线）
import { readFileSync, readdirSync, existsSync } from "node:fs";
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
const fileSet = new Set(files);

console.log(`entries=${idx.entries.length} schema_version=${idx.schema_version} generated=${idx.generated}`);
console.log(`top-level keys: ${Object.keys(idx).join(", ")}`);
console.log(`scan files (all ext)=${files.length}  md=${files.filter((f) => f.endsWith(".md")).length}`);

const newFiles = files.filter((f) => !entryPaths.has(f));
console.log(`\n[新增] 文件无条目 = ${newFiles.length}`);
for (const f of newFiles) console.log("  " + f);

const orphans = idx.entries.filter((e) => !fileSet.has(e.path));
console.log(`\n[孤儿] 条目无文件 = ${orphans.length}`);
for (const e of orphans) console.log("  " + e.path);

const versionString = idx.entries.filter((e) => typeof e.version === "string");
console.log(`\n[瑕疵] version 为 string = ${versionString.length}`);
for (const e of versionString) console.log(`  ${e.path} => ${JSON.stringify(e.version)}`);

const emptySource = idx.entries.filter((e) => !e.source);
console.log(`[瑕疵] source 为空 = ${emptySource.length}`);
for (const e of emptySource) console.log(`  ${e.path}`);

const bySource = {};
for (const e of idx.entries) bySource[e.source || "<empty>"] = (bySource[e.source || "<empty>"] || 0) + 1;
console.log(`\nsource 分布: ${JSON.stringify(bySource)}`);

// 指针段
for (const k of ["tarkov_dev", "forge_catalog", "external_repos"]) {
  console.log(`pointer ${k}: ${JSON.stringify(idx[k])}`);
}
