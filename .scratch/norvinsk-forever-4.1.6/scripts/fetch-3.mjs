// fetch-3.mjs —— 取三项补装的详情（存 mod-detail）+ 全部兼容版本（选最新）+ 打印说明摘要
import { readFileSync, writeFileSync, existsSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const OUT = join(SCRATCH, "mod-detail");
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
    if (/slow down/i.test(t)) { await sleep(25000); continue; }
    try { return JSON.parse(t); } catch { await sleep(2500); continue; }
  }
  return null;
}
const strip = (html) => String(html ?? "").replace(/<\s*li[^>]*>/gi, "\n- ").replace(/<\s*br\s*\/?>/gi, "\n").replace(/<\/\s*(p|div|ul|ol|h[1-6])\s*>/gi, "\n").replace(/<[^>]+>/g, " ").replace(/&nbsp;/g, " ").replace(/&amp;/g, "&").replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, '"').replace(/&#0?39;/g, "'").replace(/[ \t]+/g, " ").replace(/ *\n */g, "\n").replace(/\n{2,}/g, "\n").trim();

for (const id of [954, 2960, 2953]) {
  const d = await paced(`https://sp-mod.com/api/v0/mod/${id}`);
  if (d?.data) writeFileSync(join(OUT, `${id}.json`), JSON.stringify(d.data, null, 1), "utf8");
  await sleep(1200);
  const v = await paced(`https://sp-mod.com/api/v0/mod/${id}/versions?filter[spt_version]=4.1.6&per_page=100`);
  await sleep(1200);
  const arr = (v?.data ?? []).slice().sort((a, b) => String(b.published_at ?? "").localeCompare(String(a.published_at ?? "")));
  const newest = arr[0];
  console.log(`===== #${id} ${d?.data?.name}`);
  console.log(`   newest compatible: ${newest?.version} | ${newest?.spt_version_constraint} | ${String(newest?.published_at ?? "").slice(0, 10)} | ${(newest?.content_length ?? 0) / 1048576 | 0}MB`);
  console.log(`   link: ${newest?.link}`);
  console.log(`   desc excerpt: ${strip(d?.data?.description).slice(0, 700).replace(/\n/g, " / ")}`);
  console.log("");
}
console.log("done");
