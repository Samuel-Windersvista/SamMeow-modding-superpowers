#!/usr/bin/env node
// fetch-mods.mjs —— 从 sp-mod.com API v0 抓取 mod 列表（分页）→ mods-p<N>.json
//
// 用 curl.exe 子进程发请求（Node 原生 fetch 不支持 HTTP 代理；curl 的 -x 可靠）。
// 代理通过环境变量 SP_MOD_PROXY 覆盖，默认 http://127.0.0.1:7890。
//
// 用法：
//   node fetch-mods.mjs --spt-version 4.1.5 --since 2026-08-14 --until 2026-09-14 \
//        --out-dir <目录，默认 ../../knowledge/spt-kb/archive/forge/.incoming/>
//
// 输出：<out-dir>/mods-p1.json ... mods-p<N>.json（每页一个文件，compact JSON，形状与 API 一致）
import { execFileSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const DEFAULT_OUT_DIR = resolve(SCRIPT_DIR, "../../knowledge/spt-kb/archive/forge/.incoming");
const DEFAULT_PROXY = "http://127.0.0.1:7890";
const API_BASE = "https://sp-mod.com/api/v0/mods";
const PER_PAGE = 50;

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
const outDir = resolve(String(opts["out-dir"] ?? DEFAULT_OUT_DIR));
const proxy = process.env.SP_MOD_PROXY ?? DEFAULT_PROXY;
const sptVersion = opts["spt-version"] ? String(opts["spt-version"]) : null;
const since = opts["since"] ? String(opts["since"]) : null;
const until = opts["until"] ? String(opts["until"]) : null;

// 手工拼接 query：保留 [ ] 与逗号字面量（curl -g 关闭 globbing），与 API 模板一致。
function buildUrl(page) {
  const parts = [`per_page=${PER_PAGE}`, `page=${page}`];
  if (sptVersion) parts.push(`filter[spt_version]=${encodeURIComponent(sptVersion)}`);
  if (since || until) parts.push(`filter[updated_between]=${since ?? ""},${until ?? ""}`);
  parts.push("include=source_code_links,versions");
  parts.push("sort=-updated_at");
  return `${API_BASE}?${parts.join("&")}`;
}

function curlJson(url) {
  const args = ["-g", "-sS", "-m", "120"];
  if (proxy) args.push("-x", proxy);
  args.push(url);
  let stdout;
  try {
    stdout = execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 });
  } catch (err) {
    throw new Error(`curl 请求失败：${url}\n${String(err.stderr ?? err.message ?? err)}`);
  }
  try {
    return JSON.parse(stdout);
  } catch {
    throw new Error(`响应不是合法 JSON：${url}\n${stdout.slice(0, 300)}`);
  }
}

mkdirSync(outDir, { recursive: true });

console.log(`API   ${API_BASE}`);
console.log(`代理  ${proxy || "(直连)"}`);
console.log(`筛选  spt_version=${sptVersion ?? "(不限)"} updated_between=${since ?? "?"},${until ?? "?"}`);
console.log(`输出  ${outDir}`);
console.log("");

const first = curlJson(buildUrl(1));
const lastPage = Number(first?.meta?.last_page ?? 1);
const apiTotal = Number(first?.meta?.total ?? 0);

let count = 0;
writeFileSync(`${outDir}/mods-p1.json`, JSON.stringify(first), "utf8");
count += Array.isArray(first.data) ? first.data.length : 0;
console.log(`page 1/${lastPage}  entries=${Array.isArray(first.data) ? first.data.length : 0}`);

for (let p = 2; p <= lastPage; p++) {
  const j = curlJson(buildUrl(p));
  writeFileSync(`${outDir}/mods-p${p}.json`, JSON.stringify(j), "utf8");
  const n = Array.isArray(j.data) ? j.data.length : 0;
  count += n;
  console.log(`page ${p}/${lastPage}  entries=${n}`);
}

console.log("");
console.log(`DONE total=${apiTotal} count=${count} pages=${lastPage}`);
console.log(`文件：${outDir}/mods-p1.json .. mods-p${lastPage}.json`);
