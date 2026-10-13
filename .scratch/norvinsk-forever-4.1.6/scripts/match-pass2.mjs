// match-pass2.mjs —— 合并 pass1 结果 + 本地 catalog 离线模糊匹配，产出统一复核表
import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const CAT = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\knowledge\\spt-kb\\archive\\forge\\api\\mods-catalog.json";

const catalog = JSON.parse(readFileSync(CAT, "utf8").replace(/^\uFEFF/, ""));
const draft = JSON.parse(readFileSync(join(SCRATCH, "forge-match-draft.json"), "utf8"));
const inv = JSON.parse(readFileSync(join(SCRATCH, "life-inventory.json"), "utf8"));
const mods = inv.mods;

function norm(s) { return String(s ?? "").toLowerCase().replace(/[^a-z0-9]/g, ""); }
function tokens(s) {
  return String(s ?? "").toLowerCase().split(/[^a-z0-9]+/).filter((x) => x.length > 1);
}
const items = catalog.map((c) => ({ id: c.id, name: c.name, slug: c.slug, downloads: c.downloads, n: norm(c.name), s: norm(c.slug) }));

function matchScore(key, it) {
  const k = norm(key);
  if (k.length < 4) return 0;
  if (k === it.n || k === it.s) return 100;
  const shorter = Math.min(k.length, it.n.length);
  if (shorter >= 6 && (it.n.includes(k) || k.includes(it.n))) return 85;
  if (shorter >= 6 && (it.s.includes(k) || k.includes(it.s))) return 78;
  const kt = new Set(tokens(key));
  const nt = new Set(tokens(it.name));
  let inter = 0;
  for (const x of kt) if (nt.has(x)) inter++;
  const ratio = kt.size ? inter / kt.size : 0;
  if (ratio >= 0.75) return 70;
  if (ratio >= 0.5) return 50;
  return 0;
}

function buildKeys(entryName, rec) {
  const keys = new Set();
  for (const s of rec?.server ?? []) if (s.name) keys.add(s.name);
  const lat = entryName.replace(/^\[\d+(\.\d+)?\]/, "").replace(/[^\x20-\x7E]/g, " ").replace(/\s+/g, " ").trim();
  if (lat) {
    keys.add(lat);
    for (const t of lat.split(/[-\s.]+/)) if (t.length >= 5) keys.add(t);
  }
  for (const d of rec?.clientDlls ?? []) {
    let b = d.split("/").pop().replace(/\.dll$/i, "");
    b = b.replace(/^(DrakiaXYZ|Tyfon|AcidPhantasm|acidphantasm|MoxoPixel|SamSWAT|swiftxp|Skwizzy|ChooChoo|IcyClawz|MarsyApp|dvize|Terkoiz|Tyrian|Gaylatea|Kiki|desze|redlaser42|UmbigoPreto|vinihns|dazzuh|schkuromi)[.-]/i, "");
    if (b.length >= 5) keys.add(b);
  }
  return [...keys];
}

function suggest(keys) {
  const best = new Map();
  for (const it of items) {
    let sc = 0;
    for (const k of keys) {
      const s = matchScore(k, it);
      if (s > sc) sc = s;
    }
    if (sc > 0) best.set(it.id, { id: it.id, name: it.name, slug: it.slug, downloads: it.downloads, score: sc });
  }
  return [...best.values()].sort((a, b) => b.score - a.score || b.downloads - a.downloads).slice(0, 4);
}

const rows = [];
const stats = { p1autoOk: 0, p1autoSuspect: 0, p2resolves: 0, unresolved: 0 };
for (const r of draft) {
  const name = r.entry;
  const rec = mods[name];
  const keys = buildKeys(name, rec);
  const sug = suggest(keys);
  // 交叉验证: p1 匹配是否出现在 p2 建议中（或与 keys 有强关联）
  let p1verdict = r.status;
  if (r.match) {
    const hit = sug.find((s) => s.id === r.match.id);
    if (hit && hit.score >= 70) { p1verdict = "OK"; stats.p1autoOk++; }
    else { p1verdict = "CHECK"; stats.p1autoSuspect++; }
  }
  const srv = (rec?.server ?? []).map((s) => s.name).join(", ");
  const dlls = (rec?.clientDlls ?? []).map((d) => d.split("/").pop()).slice(0, 3).join(",");
  const p1 = r.match ? `${r.match.id}|${r.match.name}|${r.match.score}` : "";
  const p2 = sug.map((s) => `${s.id}|${s.name}|${s.score}`).join(" ;; ");
  rows.push([String(r.idx), name, srv, dlls, p1verdict, p1, p2].join("\t"));
  if (!r.match && sug.length) stats.p2resolves++;
  if (!r.match && !sug.length) stats.unresolved++;
}
writeFileSync(join(SCRATCH, "match-review.tsv"), rows.join("\n"), "utf8");
console.log("== stats ==", JSON.stringify(stats));
console.log("rows:", rows.length);
