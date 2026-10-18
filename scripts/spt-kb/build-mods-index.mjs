#!/usr/bin/env node
// build-mods-index.mjs —— 扫描 mods/*_source/ 目录，生成 Forge Mod 源码归档 ID 对照表
//
// 目录名格式：<Name>_<id>[_<批次序号>]_source[-<版本后缀>]
// 同一 id 可能有多个目录变体（重复克隆、改名、附加版本后缀），这里按 id 聚合计数。
//
// 用法：
//   node scripts/spt-kb/build-mods-index.mjs
import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(SCRIPT_DIR, "../..");
const ARCHIVE = join(REPO_ROOT, "knowledge", "spt-kb", "archive", "forge");
const MODS_DIR = join(ARCHIVE, "mods");
const CATALOG = join(ARCHIVE, "api", "mods-catalog.json");
const OUTPUT = join(ARCHIVE, "MODS-INDEX.md");

/** id -> 官方 mod 名称（catalog 缺失时返回空表，回退目录名） */
function loadCatalogNames() {
  try {
    const arr = JSON.parse(readFileSync(CATALOG, "utf8").replace(/^\uFEFF/, ""));
    const map = new Map();
    if (Array.isArray(arr)) {
      for (const m of arr) if (m?.id && m?.name) map.set(Number(m.id), String(m.name));
    }
    return map;
  } catch {
    return new Map();
  }
}

const SOURCE_DIR_RE = /^(.*)_(\d+)(?:-\d+)?_source(?:-(.+))?$/;

const now = new Date();
const pad = (n) => String(n).padStart(2, "0");
const today = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;

const dirs = readdirSync(MODS_DIR, { withFileTypes: true })
  .filter((d) => d.isDirectory())
  .map((d) => d.name);

/** id -> { names: string[], count: number } */
const groups = new Map();
let matchedDirs = 0;

for (const dir of dirs) {
  const m = SOURCE_DIR_RE.exec(dir);
  if (!m) continue; // BepInEx / release / SPT_Runtime 等非源码目录
  const name = m[1].trim();
  const id = Number(m[2]);
  matchedDirs++;
  if (!groups.has(id)) groups.set(id, { names: [], count: 0 });
  const group = groups.get(id);
  group.count++;
  if (name) group.names.push(name);
}

const catalogNames = loadCatalogNames();

const rows = [...groups.entries()]
  .map(([id, group]) => {
    // 优先用 catalog 官方名称；缺失时回退目录名（取最短变体）
    const dirName = group.names.slice().sort((a, b) => a.length - b.length || a.localeCompare(b))[0] ?? "";
    const name = catalogNames.get(id) ?? dirName;
    return { id, name, count: group.count };
  })
  .sort((a, b) => a.id - b.id);

const escapeCell = (s) => String(s).replace(/\|/g, "\\|");

const lines = [
  "# Forge Mod 源码归档 ID 对照表",
  "",
  "> 自动生成：`scripts/spt-kb/build-mods-index.mjs`",
  "> 来源：`mods/*_source/` 目录扫描（覆盖全部归档批次）",
  `> 生成日期：${today}`,
  "",
  "| ID | Mod 名称 | 源码目录数 |",
  "|----|---------|-----------|",
  ...rows.map((r) => `| ${r.id} | ${escapeCell(r.name)} | ${r.count} |`),
  "",
];

writeFileSync(OUTPUT, lines.join("\n"), "utf8");

console.log(`[DONE] MODS-INDEX.md entries=${rows.length} dirs=${matchedDirs}`);
