#!/usr/bin/env node
// finalize-manifest.mjs —— 以磁盘实际状态为准，重写 sp-mod.com 源码归档 MANIFEST
//
// 在 clone-sources.mjs（含 URL 规范化）跑完之后运行：不信任克隆时的内存结果，
// 而是重新扫描目标目录，逐条 `git rev-parse HEAD` 取实际 commit。
//
// 用法：
//   node finalize-manifest.mjs [--input-dir <目录>] [--month <YYYY-MM>] [--dry-run] \
//        [--spt-version <v>] [--since <YYYY-MM-DD>] [--until <YYYY-MM-DD>] [--date <YYYY-MM-DD>]
//   默认 input-dir = ../../knowledge/spt-kb/archive/forge/.incoming/
//   写出          = ../../knowledge/spt-kb/archive/forge/mods/MANIFEST-sp-mod-<YYYY-MM>.md
import { readFileSync, readdirSync, writeFileSync, existsSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const DEFAULT_INPUT_DIR = resolve(SCRIPT_DIR, "../../knowledge/spt-kb/archive/forge/.incoming");
const ARCHIVE_MODS = resolve(SCRIPT_DIR, "../../knowledge/spt-kb/archive/forge/mods");
const DEFAULT_PROXY = "http://127.0.0.1:7890";

function parseArgs(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (!a.startsWith("--")) continue;
    const eq = a.indexOf("=");
    if (eq !== -1) {
      out[a.slice(2, eq)] = a.slice(eq + 1);
    } else {
      const key = a.slice(2);
      const next = argv[i + 1];
      if (next !== undefined && !next.startsWith("--")) {
        out[key] = next;
        i++;
      } else {
        out[key] = true;
      }
    }
  }
  return out;
}

const opts = parseArgs(process.argv.slice(2));
const inputDir = resolve(String(opts["input-dir"] ?? DEFAULT_INPUT_DIR));
const dryRun = Boolean(opts["dry-run"]);
const proxy = process.env.SP_MOD_PROXY ?? DEFAULT_PROXY;
const sptVersion = opts["spt-version"] ? String(opts["spt-version"]) : null;
const since = opts["since"] ? String(opts["since"]) : null;
const until = opts["until"] ? String(opts["until"]) : null;

const now = new Date();
const pad = (n) => String(n).padStart(2, "0");
const todayDefault = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
const dateStr = opts["date"] ? String(opts["date"]) : todayDefault;
const monthStr = opts["month"] ? String(opts["month"]) : dateStr.slice(0, 7);
const manifestPath = join(ARCHIVE_MODS, `MANIFEST-sp-mod-${monthStr}.md`);

// 1) 收集源码条目（与 clone-sources.mjs 完全一致的顺序 / 去重 / 命名）
const pageFiles = readdirSync(inputDir)
  .filter((f) => /^mods-p\d+\.json$/.test(f))
  .sort((a, b) => Number(a.match(/\d+/)[0]) - Number(b.match(/\d+/)[0]));
if (pageFiles.length === 0) {
  console.error(`[FAIL] 未在 ${inputDir} 找到 mods-p*.json`);
  process.exit(1);
}

const entries = [];
for (const f of pageFiles) {
  const j = JSON.parse(readFileSync(join(inputDir, f), "utf8"));
  for (const m of j.data ?? []) {
    const links = Array.isArray(m.source_code_links) ? m.source_code_links : [];
    links.forEach((l, i) => {
      const url = String(l.url ?? "").replace(/\/+$/, "");
      if (!url) return;
      entries.push({ id: m.id, name: m.name, url, multi: i });
    });
  }
}

function sanitize(name) {
  return name
    .replace(/[<>:"/\\|?*]/g, "-")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-")
    .replace(/^-|-$/g, "")
    .slice(0, 80);
}

const seen = new Set();
const list = [];
for (const e of entries) {
  const key = e.url.replace(/\.git$/, "").toLowerCase();
  if (seen.has(key)) continue;
  seen.add(key);
  const suffix = e.multi > 0 ? `-${e.multi + 1}` : "";
  list.push({ ...e, dir: `${sanitize(e.name)}_${e.id}${suffix}_source` });
}

// 2) 扫描磁盘实际状态
let ok = 0, missing = 0;
const rows = [];
for (const e of list) {
  const dir = join(ARCHIVE_MODS, e.dir);
  if (!existsSync(dir)) {
    missing++;
    rows.push({ ...e, commit: "", status: "missing" });
    continue;
  }
  let commit = "";
  try {
    commit = execFileSync("git", ["-C", dir, "rev-parse", "HEAD"], { encoding: "utf8" }).trim();
  } catch { /* not a git dir */ }
  ok++;
  rows.push({ ...e, commit, status: "ok" });
}

// 3) 组装 MANIFEST
const sourceLine =
  sptVersion || since || until
    ? `- 来源：https://sp-mod.com API v0（filter[spt_version]=${sptVersion ?? "?"} & filter[updated_between]=${since ?? "?"},${until ?? "?"}）`
    : "- 来源：https://sp-mod.com API v0（mods-p*.json）";
const lines = [
  `# sp-mod.com 源码归档 MANIFEST（SPT ${sptVersion ?? "?"}）`,
  "",
  `- 抓取日期：${dateStr}`,
  sourceLine,
  `- 克隆方式：git clone --depth 1（经代理 ${proxy}；/tree/<branch> 链接以 --branch 补克隆）；目录 = mods/<Name>_<id>_source/`,
  `- 统计：total=${list.length} ok=${ok} missing=${missing}`,
  "",
  "| 目录 | Mod | id | 仓库 | commit | 状态 |",
  "|------|-----|----|------|--------|------|",
  ...rows.map((r) => `| ${r.dir} | ${r.name} | ${r.id} | ${r.url} | ${r.commit || "-"} | ${r.status} |`),
  "",
];

if (dryRun) {
  console.log(`input    ${inputDir}（${pageFiles.length} 页）`);
  console.log(`scan     ${ARCHIVE_MODS}`);
  console.log(`manifest ${manifestPath}`);
  console.log(`[DRY-RUN] total=${list.length} ok=${ok} missing=${missing}（未写文件）`);
  if (missing) {
    console.log("missing entries:");
    for (const r of rows.filter((x) => x.status === "missing")) console.log("  ", r.dir, "<-", r.url);
  }
  process.exit(missing ? 2 : 0);
}

writeFileSync(manifestPath, lines.join("\n"), "utf8");
console.log(`MANIFEST finalized: total=${list.length} ok=${ok} missing=${missing}`);
console.log(`file: ${manifestPath}`);
if (missing) {
  console.log("missing entries:");
  for (const r of rows.filter((x) => x.status === "missing")) console.log("  ", r.dir, "<-", r.url);
}
