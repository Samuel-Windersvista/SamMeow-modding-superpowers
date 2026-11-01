#!/usr/bin/env node
// verify-pack-doc.mjs —— 校验整合包档案页是否完整覆盖 scan.json（分组 / 行数 / 规范节）。
// 用法：node scripts/packs/verify-pack-doc.mjs --slug <slug> [--page <path>] [--scan <path>]
import { readFileSync, existsSync } from "node:fs";

const args = {};
for (let i = 2; i < process.argv.length; i++) {
  const a = process.argv[i];
  if (!a.startsWith("--")) continue;
  const next = process.argv[i + 1];
  if (next !== undefined && !next.startsWith("--")) { args[a.slice(2)] = next; i++; } else args[a.slice(2)] = true;
}
const slug = String(args.slug ?? "");
const page = String(args.page ?? "knowledge/spt-kb/curated/packs/" + slug + ".md");
const scanPath = String(args.scan ?? ".artifacts/packs/" + slug + "/scan.json");
if (!existsSync(page)) { console.log("[FAIL] 页面不存在: " + page); process.exit(1); }
if (!existsSync(scanPath)) { console.log("[FAIL] 扫描快照不存在: " + scanPath); process.exit(1); }

const pageText = readFileSync(page, "utf8");
const scan = JSON.parse(readFileSync(scanPath, "utf8"));
const profile = scan.profiles.find(p => p.name.includes("单机")) || scan.profiles.find(p => p.name === "Default") || scan.profiles[0];

const issues = [];
const info = [];
if (!pageText.startsWith("---")) issues.push("缺 frontmatter");
for (const key of ["version:", "domain:", "topic:", "source:", "title:", "keywords:", "summary:"]) {
  if (!pageText.includes(key)) issues.push("frontmatter 缺 " + key);
}
const required = ["## 一、", "## 二、", "## 三、", "## 四、", "## 五、", "## 六、", "## 七、"];
for (const h of required) if (!pageText.includes(h)) issues.push("缺小节 " + h);
if (!pageText.includes("render-pack-tables.mjs")) issues.push("缺复现命令（render-pack-tables.mjs）");

// 分组覆盖
let missingGroups = 0;
for (const g of profile.groups) {
  const raw = g.name.replace(/_separator$/, "").replace(/^《[^》]*》/, "").trim();
  if (raw && raw !== "(未分组)" && !pageText.includes(raw)) { missingGroups++; issues.push("缺分组: " + raw.slice(0, 40)); }
}
info.push("mods 目录=" + scan.counts.modDirs + " 扫描条目=" + profile.counts.mods + "（启用 " + profile.counts.enabled + " / 禁用 " + profile.counts.disabled + "）");
// 概览数字是否出现
if (!pageText.includes(String(scan.counts.modDirs))) issues.push("页面未出现 mods 目录数 " + scan.counts.modDirs);
if (!pageText.includes(String(profile.counts.enabled))) issues.push("页面未出现启用数 " + profile.counts.enabled);

const tableRows = (pageText.match(/^\|.*\|$/gm) || []).length;
info.push("表格行数=" + tableRows + " 页面字符=" + pageText.length + " 行数=" + pageText.split("\n").length);

console.log("== " + slug + " ==");
for (const i of info) console.log("  " + i);
if (issues.length) { console.log("  [WARN] " + issues.length + " 项："); for (const i of issues.slice(0, 20)) console.log("    - " + i); }
else console.log("  [OK] 结构与分组覆盖检查通过");
process.exit(issues.some(i => i.startsWith("缺分组")) ? 1 : 0);
