// analyze-compat.mjs — compat-4.1x.json（600 条）→ 报表用数据集
// 输入：archive/forge/api/compat-4.1x.json + data/patch-4.1.5.ids.json + data/patch-4.1.6.ids.json
// 输出（data/）：
//   all.tsv            全量（按下载降序）：id/name/downloads/favs/category/updated/created/latestVer/latestConstraint/supports41.5/supports41.6/fika/ads/ai?/profileNote/cheatNote/teaser
//   top.json           前 120（下载降序）
//   recent-updated.json 最近更新（updated_at >= 2026-09-01）
//   fresh.json         新发布（published_at >= 2026-08-01）
//   themes.json        主题关键词命中（名称+简介），用于报告分节
// 控制台：统计汇总。
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const DATA_DIR = join(SCRIPT_DIR, "..", "data");
const ARCHIVE_API = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\knowledge\\spt-kb\\archive\\forge\\api";

const compat = JSON.parse(readFileSync(join(ARCHIVE_API, "compat-4.1x.json"), "utf8").replace(/^\uFEFF/, ""));
const patch415 = new Set(existsSync(join(DATA_DIR, "patch-4.1.5.ids.json")) ? JSON.parse(readFileSync(join(DATA_DIR, "patch-4.1.5.ids.json"), "utf8")) : []);
const patch416 = new Set(existsSync(join(DATA_DIR, "patch-4.1.6.ids.json")) ? JSON.parse(readFileSync(join(DATA_DIR, "patch-4.1.6.ids.json"), "utf8")) : []);

const clean = (s) => String(s ?? "").replace(/\s+/g, " ").trim();
const esc = (s) => clean(s).replace(/\t/g, " ");

const rows = compat.map((m) => {
  const versions = Array.isArray(m.versions) ? [...m.versions] : [];
  versions.sort((a, b) => String(b.published_at ?? "").localeCompare(String(a.published_at ?? "")));
  const latest = versions[0] ?? null;
  const constraints = [...new Set(versions.map((v) => v.spt_version_constraint).filter(Boolean))].slice(0, 8);
  return {
    id: m.id,
    name: clean(m.name),
    slug: m.slug,
    downloads: m.downloads ?? 0,
    favourites: m.favourites_count ?? 0,
    category: m.category?.title ?? m.category?.slug ?? "",
    updated_at: m.updated_at ?? "",
    created_at: m.created_at ?? "",
    published_at: m.published_at ?? "",
    latestVer: latest?.version ?? "",
    latestConstraint: latest?.spt_version_constraint ?? "",
    latestDownloads: latest?.downloads ?? 0,
    constraints,
    supports415: patch415.has(m.id),
    supports416: patch416.has(m.id),
    fika: Boolean(m.fika_compatibility),
    ads: Boolean(m.contains_ads),
    profileNote: Boolean(m.shows_profile_binding_notice),
    cheatNote: Boolean(m.cheat_notice),
    srcCount: Array.isArray(m.source_code_links) ? m.source_code_links.length : 0,
    teaser: clean(m.teaser),
  };
});

rows.sort((a, b) => b.downloads - a.downloads);

mkdirSync(DATA_DIR, { recursive: true });
const header = ["id", "name", "downloads", "favourites", "category", "updated", "created", "latestVer", "latestConstraint", "sup415", "sup416", "fika", "ads", "profileNote", "cheatNote", "src", "teaser"];
const tsv = [header.join("\t")];
for (const r of rows) {
  tsv.push([r.id, esc(r.name), r.downloads, r.favourites, esc(r.category), r.updated_at.slice(0, 10), r.created_at.slice(0, 10), esc(r.latestVer), esc(r.latestConstraint), r.supports415 ? "1" : "0", r.supports416 ? "1" : "0", r.fika ? "1" : "0", r.ads ? "1" : "0", r.profileNote ? "1" : "0", r.cheatNote ? "1" : "0", r.srcCount, esc(r.teaser)].join("\t"));
}
writeFileSync(join(DATA_DIR, "all.tsv"), tsv.join("\n") + "\n", "utf8");
writeFileSync(join(DATA_DIR, "top.json"), JSON.stringify(rows.slice(0, 120), null, 1), "utf8");
writeFileSync(join(DATA_DIR, "recent-updated.json"), JSON.stringify(rows.filter((r) => r.updated_at >= "2026-09-01"), null, 1), "utf8");
writeFileSync(join(DATA_DIR, "fresh.json"), JSON.stringify(rows.filter((r) => r.published_at >= "2026-08-01"), null, 1), "utf8");

// 主题关键词（中文注释：命中即列出，供报告分节；匹配 name+teaser 小写）
const THEMES = {
  coop: /fika|co-?op|multiplayer|multi-player|friends/i,
  botAI: /\bai\b|bots?\b|sain|behaviou?r|combat|enem(y|ies)/i,
  quests: /quest|task/i,
  traders: /trader|merchant|fence/i,
  economy: /econom|flea|market|price|loot|money|roubles?|profit/i,
  maps: /\bmap\b|location|navmesh|waypoint|extract/i,
  hardcore: /hardcore|survival|realis|realism|permadeath|difficulty|challeng/i,
  weapons: /weapon|gun|ballistic|ammo|attachment|scope/i,
  visuals: /graphic|visual|shader|weather|night|light|fx|effect|photoreal/i,
  audio: /audio|sound|voice|radio|music|ambien/i,
  ui: /\bui\b|hud|interface|menu|screen|display|inventory|stash/i,
  animation: /animat|motion|inertia|movement/i,
  hideout: /hideout/i,
  skills: /skill|mastery|level|progression|xp/i,
  spawns: /spawn|pmc|scav|boss/i,
  raid: /raid|fir\b|insurance|death|respawn|extract/i,
};
const themes = {};
for (const [k, re] of Object.entries(THEMES)) {
  themes[k] = rows.filter((r) => re.test(`${r.name} ${r.teaser}`)).map((r) => ({ id: r.id, name: r.name, downloads: r.downloads, teaser: r.teaser.slice(0, 140) }));
}
writeFileSync(join(DATA_DIR, "themes.json"), JSON.stringify(themes, null, 1), "utf8");

// 汇总打印
const byCat = {};
for (const r of rows) byCat[r.category || "(none)"] = (byCat[r.category || "(none)"] ?? 0) + 1;
console.log(`compat rows: ${rows.length}`);
console.log(`supporting 4.1.5: ${rows.filter((r) => r.supports415).length} | 4.1.6: ${rows.filter((r) => r.supports416).length} | neither: ${rows.filter((r) => !r.supports415 && !r.supports416).length}`);
console.log(`fika: ${rows.filter((r) => r.fika).length} | ads: ${rows.filter((r) => r.ads).length} | profileNote: ${rows.filter((r) => r.profileNote).length} | cheatNote: ${rows.filter((r) => r.cheatNote).length}`);
console.log(`recent-updated(>=2026-09-01): ${rows.filter((r) => r.updated_at >= "2026-09-01").length} | fresh(>=2026-08-01): ${rows.filter((r) => r.published_at >= "2026-08-01").length}`);
console.log("categories:", Object.entries(byCat).sort((a, b) => b[1] - a[1]).map(([k, v]) => `${k}:${v}`).join(" | "));
console.log("theme counts:", Object.entries(themes).map(([k, v]) => `${k}:${v.length}`).join(" "));
console.log("\ntop 15 by downloads:");
for (const r of rows.slice(0, 15)) console.log(`  #${r.id} ${r.name} | dl=${r.downloads} | ${r.category} | sup415=${r.supports415 ? "Y" : "n"} sup416=${r.supports416 ? "Y" : "n"} | ${r.teaser.slice(0, 90)}`);
