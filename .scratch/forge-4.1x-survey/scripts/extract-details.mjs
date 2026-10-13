// extract-details.mjs — 50 条详情 → 紧凑摘要（供报告写作）
import { readFileSync, readdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const DIR = join(SCRIPT_DIR, "..", "data", "details");

function stripHtml(s) {
  return String(s ?? "")
    .replace(/<br\s*\/?>/gi, "\n")
    .replace(/<\/(p|li|ul|ol|h\d|div)>/gi, "\n")
    .replace(/<[^>]+>/g, " ")
    .replace(/&nbsp;/g, " ")
    .replace(/&amp;/g, "&")
    .replace(/&quot;/g, '"')
    .replace(/&#0?39;/g, "'")
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/[ \t]+/g, " ")
    .replace(/\n\s*\n+/g, "\n")
    .trim();
}

const files = readdirSync(DIR).filter((f) => f.endsWith(".json")).sort((a, b) => Number(a.split(".")[0]) - Number(b.split(".")[0]));
for (const f of files) {
  const j = JSON.parse(readFileSync(join(DIR, f), "utf8"));
  const firstFile = f === files[0];
  if (firstFile) console.log(`[keys] ${Object.keys(j).join(",")}\n`);
  const desc = stripHtml(j.description);
  console.log(`### #${j.id} ${j.name} | dl=${j.downloads ?? "?"} | fav=${j.favourites_count ?? "?"} | cat=${j.category?.title ?? j.category?.slug ?? "?"} | updated=${String(j.updated_at).slice(0, 10)} | fika=${j.fika_compatibility ?? "?"} | ads=${j.contains_ads ?? "?"} | cheat=${j.cheat_notice ?? "?"} | profile=${j.shows_profile_binding_notice ?? "?"}`);
  console.log(`T: ${String(j.teaser ?? "").replace(/\s+/g, " ").trim()}`);
  console.log(`D: ${desc.slice(0, 620).replace(/\n/g, " / ")}`);
  console.log("");
}
