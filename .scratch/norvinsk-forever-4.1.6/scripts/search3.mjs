// search3.mjs —— 微检索：最后残项
import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";
const TERMS = ["Hoods", "SIG MCX", "QBZ", "Baba Yaga", "Unknown Survivor", "Survivor", "Profile Editor"];

function curlJsonOnce(url, proxy) {
  const args = ["-g", "-sS", "-m", "25", "-A", "SamMeow-modpack-research/0.1"];
  if (proxy) args.push("-x", proxy);
  args.push(url);
  return JSON.parse(execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 32 * 1024 * 1024 }));
}
function curlJson(url) {
  try { return curlJsonOnce(url, null); } catch { try { return curlJsonOnce(url, PROXY); } catch { return null; } }
}

const out = [];
for (const term of TERMS) {
  const url = `https://sp-mod.com/api/v0/mods?query=${encodeURIComponent(term)}&per_page=8`;
  const d = curlJson(url);
  const rows = (d?.data ?? []).map((c) => ({ id: c.id, name: c.name, slug: c.slug, dl: c.downloads }));
  out.push({ term, rows });
  console.log(`### ${term}`);
  for (const r of rows) console.log(`   ${r.id}|${r.name}|dl=${r.dl}`);
  await new Promise((r) => setTimeout(r, 150));
}
writeFileSync(join(SCRATCH, "forge-search3.json"), JSON.stringify(out, null, 1), "utf8");
console.log("saved");
