// check-three.mjs —— 复核三个疑似已有 4.1.6 兼容版本的条目
import { execFileSync } from "node:child_process";

const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
function curlRaw(url) {
  const args = ["-g", "-sS", "-m", "40", "-A", "SamMeow-modpack-research/0.1"];
  if (PROXY) args.push("-x", PROXY);
  args.push(url);
  return execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 });
}
async function paced(url) {
  for (let a = 0; a < 6; a++) {
    let t;
    try { t = curlRaw(url); } catch { await sleep(3000); continue; }
    if (/slow down/i.test(t)) { console.log("   [throttled]"); await sleep(25000); continue; }
    try { return JSON.parse(t); } catch { await sleep(2500); continue; }
  }
  return null;
}

for (const id of [954, 2960, 2953]) {
  const d = await paced(`https://sp-mod.com/api/v0/mod/${id}`);
  await sleep(1200);
  const v = await paced(`https://sp-mod.com/api/v0/mod/${id}/versions?filter[spt_version]=4.1.6&per_page=100`);
  await sleep(1200);
  const arr = v?.data ?? [];
  console.log(`#${id} ${d?.data?.name ?? "?"}`);
  console.log(`   slug: ${d?.data?.slug ?? "?"}`);
  console.log(`   4.1.6-compatible versions: ${arr.length}`);
  for (const x of arr.slice(0, 4)) console.log(`     - ${x.version} | ${x.spt_version_constraint} | ${(x.published_at ?? "").slice(0, 10)} | ${x.link}`);
}
console.log("done");
