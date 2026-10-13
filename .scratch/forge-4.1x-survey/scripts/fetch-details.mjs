// fetch-details.mjs — 抓取报告候选 mod 详情（/mod/{id}），落盘 data/details/
import { execFileSync } from "node:child_process";
import { mkdirSync, writeFileSync, existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const OUT = join(SCRIPT_DIR, "..", "data", "details");
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";
const UA = "SamMeow-modpack-research/0.1";
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

// 报告头部候选 + 关键机制作者 mod
const IDS = [
  2326, 2512, 2642, 3073, 2706, 2903, 2942, 2383, 2788, 2965,
  3120, 784, 1623, 814, 2314, 2479, 2676, 2709, 2866, 3025,
  2937, 3022, 2843, 3096, 1232, 1348, 1023, 1538, 1025, 2851,
  2879, 1479, 2713, 2952, 2844, 1936, 3014, 3026, 2998, 3032,
  3089, 3077, 3080, 3102, 3121, 2893, 2905, 3038, 2921, 3129,
];

function curlJson(url) {
  const args = ["-g", "-sS", "-m", "90", "-A", UA];
  if (PROXY) args.push("-x", PROXY);
  args.push(url);
  return JSON.parse(execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 }));
}

mkdirSync(OUT, { recursive: true });
let ok = 0, skip = 0, fail = 0;
for (const id of IDS) {
  const f = join(OUT, `${id}.json`);
  if (existsSync(f)) { skip++; continue; }
  let done = false;
  for (let attempt = 1; attempt <= 3 && !done; attempt++) {
    try {
      const j = curlJson(`https://sp-mod.com/api/v0/mod/${id}`);
      if (j?.data?.id) {
        writeFileSync(f, JSON.stringify(j.data, null, 1), "utf8");
        done = true; ok++;
        console.log(`OK ${id} ${String(j.data.name).slice(0, 60)}`);
      }
    } catch {
      await sleep(attempt * 4000);
    }
  }
  if (!done) { fail++; console.log(`FAIL ${id}`); }
  await sleep(2100);
}
console.log(`done=${ok} skip=${skip} fail=${fail} total=${IDS.length}`);
