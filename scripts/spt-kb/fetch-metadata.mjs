#!/usr/bin/env node
// fetch-metadata.mjs —— 从 sp-mod.com API v0 抓取两份元数据快照，合并去重后写入 KB 归档。
//
//   catalog：全量 mod 元数据（不带 include），预期约 1969 条
//   compat ：SPT 4.1.x 兼容集（filter[spt_version]=~4.1.0 + include=versions,source_code_links），
//            预期约 600 条
//
// 用 curl.exe 子进程发请求（Node 原生 fetch 不支持 HTTP 代理；curl 的 -x 可靠）。
// 代理通过环境变量 SP_MOD_PROXY 覆盖，默认 http://127.0.0.1:7890。
//
// 用法：
//   node fetch-metadata.mjs [--out-dir <目录，默认 ../../knowledge/spt-kb/archive/forge/api/>]
//
// 输出（UTF-8 带 BOM + 4 空格缩进）：
//   <out-dir>/mods-catalog.json   ← catalog 数组
//   <out-dir>/compat-4.1x.json    ← compat 数组
//
// 退出码：
//   0 = 全部成功且计数一致
//   1 = 已写文件，但合并去重后的条数 ≠ 最后一次响应的 meta.total
//   2 = 任一页重试后仍失败（不写任何文件）
import { execFileSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, resolve, relative, sep } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = resolve(SCRIPT_DIR, "../..");
const DEFAULT_OUT_DIR = resolve(SCRIPT_DIR, "../../knowledge/spt-kb/archive/forge/api");
const DEFAULT_PROXY = "http://127.0.0.1:7890";
const API_BASE = "https://sp-mod.com/api/v0/mods";
const PER_PAGE = 50;
const UA = "spt-kb-refresh/1.0 (sp-mod.com metadata snapshot; local)";

// 限速与重试参数：服务端 X-RateLimit-Limit=300/分钟，默认 2s 间隔只是礼貌限速。
// 注意不要并发多进程同时抓取。
const RATE_DELAY_MS = Number(process.env.SP_MOD_RATE_MS ?? 2000); // 每两个请求之间的限速间隔
const MAX_RETRIES = 3; // 每页最多重试次数
const BACKOFF_MS = [10000, 30000, 60000]; // 重试退避
const MAX_PAGE = 200; // 无限循环护栏

// 与 fetch-mods.mjs 保持一致的参数解析（支持 --k v 与 --k=v）。
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
const CATALOG_PATH = resolve(outDir, "mods-catalog.json");
const COMPAT_PATH = resolve(outDir, "compat-4.1x.json");

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

// 路径展示：相对仓库根、统一正斜杠（用于 written ... 汇总行）。
function displayPath(p) {
  return relative(REPO_ROOT, p).split(sep).join("/");
}

// 手工拼接 query：保留 [ ] 与 ~ . 字面量（curl -g 关闭 globbing），与 API 模板一致。
// 注意 ~4.1.0 是 SemVer 约束（>=4.1.0 <4.2.0），不做 encodeURIComponent。
function buildUrl(page, { compat }) {
  const parts = [`per_page=${PER_PAGE}`, `page=${page}`, "sort=created_at"];
  if (compat) {
    parts.push("filter[spt_version]=~4.1.0");
    parts.push("include=versions,source_code_links");
  }
  return `${API_BASE}?${parts.join("&")}`;
}

function curlText(url) {
  const args = ["-g", "-sS", "-m", "120", "-A", UA];
  if (proxy) args.push("-x", proxy);
  args.push(url);
  return execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 });
}

// 全局限速：除首个请求外，每次真实 curl 调用前等待 RATE_DELAY_MS。
let requestCount = 0;
async function rateLimit() {
  if (requestCount > 0) await sleep(RATE_DELAY_MS);
}

// 单页抓取：curl 失败 / 响应非 JSON / 响应含 "slow down" → 退避重试（5s / 15s / 25s）。
async function fetchJsonPage(url, label) {
  let lastErr;
  for (let attempt = 0; attempt <= MAX_RETRIES; attempt++) {
    await rateLimit();
    requestCount++;
    try {
      const stdout = curlText(url);
      // 先解析 JSON：仅当解析失败时才判定为节流/无效响应。
      // 教训：不要用 /slow\s*down/i 直接扫全文——mod 内容里可能就有这个词
      // （实例：“Low Budget Bullet Time”的 teaser 含 "slow down time"），会造成假阳性。
      try {
        return JSON.parse(stdout);
      } catch {
        const head = stdout.slice(0, 500);
        lastErr = /slow\s*down|too many attempts/i.test(head)
          ? new Error(`响应节流：${head.replace(/\s+/g, " ")}`)
          : new Error(`响应非 JSON：${stdout.slice(0, 200)}`);
      }
    } catch (err) {
      lastErr = new Error(String(err.stderr ?? err.message ?? err).trim());
    }
    if (attempt < MAX_RETRIES) {
      const wait = BACKOFF_MS[attempt];
      console.log(`[retry] ${label} 第 ${attempt + 1}/${MAX_RETRIES} 次重试（退避 ${wait}ms）`);
      await sleep(wait);
    }
  }
  throw new Error(`${label} 抓取失败（已重试 ${MAX_RETRIES} 次）：${url}\n  ${lastErr?.message ?? "未知错误"}`);
}

// 按 id 去重合并各页 data（保留先出现者；无 id 的条目无法去重，原样保留）。
function mergePages(pages) {
  const seen = new Set();
  const merged = [];
  for (const page of pages) {
    const data = Array.isArray(page?.data) ? page.data : [];
    for (const item of data) {
      const id = item?.id;
      if (id === undefined || id === null) {
        merged.push(item);
        continue;
      }
      if (seen.has(id)) continue;
      seen.add(id);
      merged.push(item);
    }
  }
  return merged;
}

// 抓取一整份快照（翻页到 meta.last_page）。
async function fetchAll({ compat }) {
  const label = compat ? "compat" : "catalog";
  const pages = [];
  let page = 1;
  let lastPage = 1;
  let lastTotal = 0;

  for (;;) {
    if (page > MAX_PAGE) throw new Error(`${label} 超过页数护栏 ${MAX_PAGE}`);
    const url = buildUrl(page, { compat });
    const json = await fetchJsonPage(url, `${label} page ${page}`);
    pages.push(json);

    const entries = Array.isArray(json?.data) ? json.data.length : 0;
    const meta = json?.meta ?? {};
    lastTotal = Number(meta.total ?? lastTotal);
    if (page === 1) lastPage = Math.min(Number(meta.last_page ?? 1), MAX_PAGE);
    console.log(`${label} page ${page}/${lastPage}  entries=${entries}`);

    if (page >= lastPage) break;
    page++;
  }

  return { merged: mergePages(pages), total: lastTotal, lastPage };
}

function writeJson(file, arr) {
  // UTF-8 带 BOM + 4 空格缩进，与归档既有 mods-catalog.json 风格一致。
  writeFileSync(file, "\uFEFF" + JSON.stringify(arr, null, 4), "utf8");
}

async function main() {
  mkdirSync(outDir, { recursive: true });

  console.log(`API   ${API_BASE}`);
  console.log(`代理  ${proxy || "(直连)"}`);
  console.log(`输出  ${outDir}`);
  console.log("");

  console.log("== catalog（全量 mod 元数据）==");
  const catalog = await fetchAll({ compat: false });

  console.log("");
  console.log("== compat（SPT 4.1.x 兼容集）==");
  const compatData = await fetchAll({ compat: true });

  const missingVersions = compatData.merged.filter((m) => !Array.isArray(m?.versions)).length;

  writeJson(CATALOG_PATH, catalog.merged);
  writeJson(COMPAT_PATH, compatData.merged);

  console.log("");
  console.log(`DONE catalog=${catalog.merged.length} compat=${compatData.merged.length}`);
  console.log(`written ${displayPath(CATALOG_PATH)}`);
  console.log(`written ${displayPath(COMPAT_PATH)}`);
  console.log(`compat missing versions=${missingVersions}（预期 0，仅提示不失败）`);

  // 一致性检查：合并去重后的条数应等于最后一次响应的 meta.total。
  let mismatch = false;
  if (catalog.merged.length !== catalog.total) {
    console.log(`[WARN] count mismatch catalog: merged=${catalog.merged.length} total=${catalog.total}`);
    mismatch = true;
  }
  if (compatData.merged.length !== compatData.total) {
    console.log(`[WARN] count mismatch compat: merged=${compatData.merged.length} total=${compatData.total}`);
    mismatch = true;
  }
  if (mismatch) process.exit(1);
}

main().catch((err) => {
  console.error("");
  console.error(`[FAIL] ${err.message}`);
  process.exit(2);
});
