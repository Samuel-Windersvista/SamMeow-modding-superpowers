// extract-desc.mjs —— 将 mod 详情 HTML 描述转为可读文本，产出 mod-desc-text.json
import { readFileSync, writeFileSync, readdirSync, existsSync } from "node:fs";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const DIR = join(SCRATCH, "mod-detail");

function decodeEntities(s) {
  return s
    .replace(/&nbsp;/g, " ")
    .replace(/&amp;/g, "&")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&#0?39;/g, "'")
    .replace(/&#(\d+);/g, (_, d) => String.fromCharCode(+d));
}
function htmlToText(html) {
  let s = String(html ?? "");
  s = s.replace(/<\s*li[^>]*>/gi, "\n- ");
  s = s.replace(/<\s*br\s*\/?>/gi, "\n");
  s = s.replace(/<\/\s*(p|div|ul|ol|h[1-6]|tr|table)\s*>/gi, "\n");
  s = s.replace(/<\s*\/?strong[^>]*>/gi, "");
  s = s.replace(/<[^>]+>/g, " ");
  s = decodeEntities(s);
  s = s.replace(/[ \t]+/g, " ").replace(/ *\n */g, "\n").replace(/\n{2,}/g, "\n");
  return s.trim();
}

const out = {};
for (const f of readdirSync(DIR)) {
  if (!f.endsWith(".json")) continue;
  const id = f.replace(".json", "");
  try {
    const d = JSON.parse(readFileSync(join(DIR, f), "utf8"));
    let desc = htmlToText(d.description);
    if (desc.length > 9000) desc = desc.slice(0, 9000) + "\n…（描述过长已截断）";
    out[id] = { name: d.name ?? "", teaser: d.teaser ?? "", description: desc };
  } catch (e) {
    out[id] = { name: "", teaser: "", description: "", error: String(e).slice(0, 120) };
  }
}
writeFileSync(join(SCRATCH, "mod-desc-text.json"), JSON.stringify(out, null, 1), "utf8");
const ids = Object.keys(out);
const withDesc = ids.filter((i) => (out[i].description ?? "").length > 50);
console.log(`mods: ${ids.length}, with substantial description: ${withDesc.length}, thin: ${ids.filter((i) => !withDesc.includes(i)).join(",")}`);
