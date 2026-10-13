// fetch-patch-sets.mjs — 抓取「服务器权威」的按补丁兼容 ID 集
//   filter[spt_version]=<patch> 由服务端 SemVer 求值：返回「存在至少一个与该补丁兼容版本」的 mod。
// 输出：data/patch-<v>.ids.json（id 升序数组）；用于报告标注每个 mod 是否支持 4.1.5 基线 / 4.1.6 头版本。
import { execFileSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const DATA_DIR = join(SCRIPT_DIR, "..", "data");
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";
const API = "https://sp-mod.com/api/v0/mods";
const UA = "SamMeow-modpack-research/0.1";
const PATCHES = ["4.1.5", "4.1.6"];

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function curlJson(url) {
  const args = ["-g", "-sS", "-m", "90", "-A", UA];
  if (PROXY) args.push("-x", PROXY);
  args.push(url);
  return JSON.parse(execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 }));
}

async function fetchPatchSet(patch) {
  const ids = [];
  let page = 1;
  let lastPage = 1;
  let total = 0;
  for (;;) {
    const url = `${API}?per_page=50&page=${page}&sort=created_at&filter[spt_version]=${patch}`;
    let j = null;
    for (let attempt = 1; attempt <= 3 && !j; attempt++) {
      try {
        j = curlJson(url);
      } catch {
        await sleep(attempt * 5000);
      }
    }
    if (!j) throw new Error(`fetch failed: ${url}`);
    lastPage = Number(j?.meta?.last_page ?? 1);
    total = Number(j?.meta?.total ?? 0);
    for (const m of j.data ?? []) ids.push(m.id);
    console.log(`  [${patch}] page ${page}/${lastPage} (+${(j.data ?? []).length})`);
    if (page >= lastPage || page >= 200) break;
    page++;
    await sleep(1100);
  }
  return { patch, total, ids };
}

mkdirSync(DATA_DIR, { recursive: true });
for (const patch of PATCHES) {
  const { patch: p, total, ids } = await fetchPatchSet(patch);
  const uniq = [...new Set(ids)].sort((a, b) => a - b);
  const out = join(DATA_DIR, `patch-${p}.ids.json`);
  writeFileSync(out, JSON.stringify(uniq, null, 0), "utf8");
  console.log(`DONE ${p} total=${total} uniq=${uniq.length} -> ${out}`);
}
