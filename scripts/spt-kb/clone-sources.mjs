#!/usr/bin/env node
// clone-sources.mjs —— 批量浅克隆 sp-mod.com mod 源码 → KB archive
//
// 合并了两个来源脚本的逻辑：
//   - clone-mod-sources.mjs：读 mods-p*.json → git clone --depth 1
//   - fixup-tree-urls.mjs ：/tree/<ref>、/releases/tag/<tag> 网页 URL 的规范化补克隆
// 规范化内建：GitHub 网页 URL 会被转成 `git clone --branch <ref> https://github.com/<o>/<r>`。
//
// 用法：
//   node clone-sources.mjs [--input-dir <目录>] [--dry-run]
//   默认 input-dir = ../../knowledge/spt-kb/archive/forge/.incoming/
//   克隆目标     = ../../knowledge/spt-kb/archive/forge/mods/<Name>_<id>[_<n>]_source
//
// 代理通过环境变量 SP_MOD_PROXY 覆盖，默认 http://127.0.0.1:7890。
import { readFileSync, readdirSync, existsSync, mkdirSync } from "node:fs";
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

// 1) 收集源码条目（mods-p*.json 来自 fetch-mods.mjs）
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

// GitHub 网页 URL → 可 clone 的仓库 + 分支/标签
function normalize(url) {
  const m = url.match(
    /^https:\/\/github\.com\/([^/]+)\/([^/]+)\/(?:tree|releases\/tag)\/(.+)$/,
  );
  if (!m) return { cloneUrl: url, branch: null };
  const [, owner, repo, ref] = m;
  return { cloneUrl: `https://github.com/${owner}/${repo.replace(/\.git$/, "")}`, branch: ref };
}

// 按 URL 去重（跨 mod 复用同一仓库时只克隆一次）
const seen = new Set();
const list = [];
for (const e of entries) {
  const key = e.url.replace(/\.git$/, "").toLowerCase();
  if (seen.has(key)) continue;
  seen.add(key);
  const suffix = e.multi > 0 ? `-${e.multi + 1}` : "";
  list.push({ ...e, dir: `${sanitize(e.name)}_${e.id}${suffix}_source` });
}

console.log(`input  ${inputDir}（${pageFiles.length} 页）`);
console.log(`target ${ARCHIVE_MODS}`);
console.log(`代理   ${proxy || "(直连)"}${dryRun ? "  [DRY-RUN]" : ""}`);
console.log(`repos to process: ${list.length}`);
console.log("");

let ok = 0, skip = 0, fail = 0;
const failures = [];

for (const e of list) {
  const dir = join(ARCHIVE_MODS, e.dir);
  const { cloneUrl, branch } = normalize(e.url);
  const args = ["-c", `http.proxy=${proxy}`, "clone", "--depth", "1"];
  if (branch) args.push("--branch", branch);
  args.push(cloneUrl, dir);

  if (existsSync(dir)) {
    skip++;
    if (dryRun) console.log(`PLAN [skip]  ${e.dir} <- ${args.slice(2).join(" ")}`);
    continue;
  }

  if (dryRun) {
    ok++; // dry-run：ok = 将被克隆的数量
    console.log(`PLAN [clone] ${e.dir} <- ${args.slice(2).join(" ")}`);
    continue;
  }

  mkdirSync(ARCHIVE_MODS, { recursive: true });
  let cloned = false;
  let lastErr = "";
  for (let attempt = 1; attempt <= 2 && !cloned; attempt++) {
    try {
      execFileSync("git", args, { stdio: "pipe", timeout: 300000 });
      cloned = true;
    } catch (err) {
      lastErr = String(err.message ?? err).slice(0, 200);
    }
  }
  if (cloned) {
    let commit = "";
    try {
      commit = execFileSync("git", ["-C", dir, "rev-parse", "HEAD"], { encoding: "utf8" }).trim();
    } catch { /* ignore */ }
    ok++;
    console.log(`OK   ${e.name} (id=${e.id}) <- ${cloneUrl}${branch ? ` @ ${branch}` : ""}`);
  } else {
    fail++;
    failures.push({ ...e, msg: lastErr });
    console.log(`FAIL ${e.name} (id=${e.id}) <- ${cloneUrl}${branch ? ` @ ${branch}` : ""} :: ${lastErr}`);
  }
}

console.log("");
console.log(`DONE total=${list.length} ok=${ok} skip=${skip} fail=${fail}${dryRun ? "  (dry-run)" : ""}`);
for (const f of failures) console.log(`  FAILED: ${f.name} ${f.url} :: ${f.msg}`);
