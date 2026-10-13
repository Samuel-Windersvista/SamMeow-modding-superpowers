// fetch-mod-details.mjs —— 拉取 69 个 mod 的页面详情（含完整 About/Features 描述）
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const OUT = join(SCRATCH, "mod-detail");
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";
mkdirSync(OUT, { recursive: true });

const targets = JSON.parse(readFileSync(join(SCRATCH, "install-targets.json"), "utf8"));
const ids = [...new Set(targets.map((t) => t.forgeId))];
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function curlRaw(url) {
  const args = ["-g", "-sS", "-m", "40", "-A", "SamMeow-modpack-research/0.1"];
  if (PROXY) args.push("-x", PROXY);
  args.push(url);
  return execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 });
}
async function paced(url) {
  for (let attempt = 0; attempt < 6; attempt++) {
    let txt;
    try { txt = curlRaw(url); } catch { await sleep(3000); continue; }
    if (/slow down/i.test(txt)) { console.log("   [throttled] backoff 25s"); await sleep(25000); continue; }
    try { return JSON.parse(txt); } catch { await sleep(2500); continue; }
  }
  return null;
}

let done = 0, fail = 0;
for (const id of ids) {
  const f = join(OUT, `${id}.json`);
  if (existsSync(f)) { try { if (JSON.parse(readFileSync(f, "utf8"))?.id) { done++; continue; } } catch {} }
  const d = await paced(`https://sp-mod.com/api/v0/mod/${id}`);
  if (d?.data?.id) { writeFileSync(f, JSON.stringify(d.data, null, 1), "utf8"); done++; console.log(`OK ${id} ${d.data.name?.slice(0, 50)}`); }
  else { fail++; console.log(`FAIL ${id}`); }
  await sleep(1200);
}
console.log(`done=${done} fail=${fail} total=${ids.length}`);
