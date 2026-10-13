// phase2.mjs —— 降速版：filter[spt_version]=4.1.6 搜索 + 过滤版本抓取 + 统一终选
// 断点续跑：s4/<idx>.json 与 f416/<id>.json 缓存
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const LIFE = "E:\\Game\\EFT_Offline\\Life_in_Norvinsk_v0.3.2";
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";
const S4 = join(SCRATCH, "s4");
const F416 = join(SCRATCH, "f416");
mkdirSync(S4, { recursive: true });
mkdirSync(F416, { recursive: true });

const inv = JSON.parse(readFileSync(join(SCRATCH, "life-inventory.json"), "utf8"));
const mods = inv.mods;
const chosen = JSON.parse(readFileSync(join(SCRATCH, "forge-chosen.json"), "utf8"));
const report = JSON.parse(readFileSync(join(SCRATCH, "selection-report.json"), "utf8"));
const draft = JSON.parse(readFileSync(join(SCRATCH, "forge-match-draft.json"), "utf8"));

// ---------- helpers ----------
const GENERIC = new Set(["weapons", "items", "mods", "system", "pack", "tool", "core", "update"]);
const norm = (s) => String(s ?? "").toLowerCase().replace(/[^a-z0-9]/g, "");
const folderLatin = (name) => name.replace(/^\[\d+(\.\d+)?\]/, "").replace(/[^\x20-\x7E]/g, " ").replace(/\s+/g, " ").trim();
function dllTerms(rec) {
  const out = [];
  for (const d of rec.clientDlls ?? []) {
    let b = d.split("/").pop().replace(/\.dll$/i, "");
    b = b.replace(/^(DrakiaXYZ|Tyfon|AcidPhantasm|acidphantasm|MoxoPixel|SamSWAT|swiftxp|Skwizzy|ChooChoo|IcyClawz|MarsyApp|dvize|Terkoiz|Tyrian|Gaylatea|Kiki|desze|redlaser42|UmbigoPreto|vinihns|dazzuh|schkuromi)[.-]/i, "");
    if (b.length >= 5 && !/^(BepInEx|ConfigurationManager|0Harmony)/i.test(b)) out.push(b);
  }
  return [...new Set(out)];
}
function termCandidates(it) {
  const terms = [];
  const srvNames = (it.rec.server ?? []).map((s) => s.name).filter(Boolean);
  const good = srvNames.filter((n) => n.length >= 4 && !GENERIC.has(norm(n)));
  if (good.length) terms.push(good.sort((a, b) => b.length - a.length)[0]);
  if (srvNames.length) terms.push(srvNames[0]);
  const lat = folderLatin(it.entry);
  if (lat) terms.push(lat);
  terms.push(...dllTerms(it.rec));
  return [...new Set(terms.filter(Boolean))].slice(0, 4);
}
function scoreCandidate(term, c) {
  const t = norm(term), n = norm(c.name), s = norm(c.slug);
  if (!t) return 0;
  if (t === n || t === s) return 100;
  if (n.includes(t) || t.includes(n)) return 82;
  if (s.includes(t)) return 74;
  const tt = new Set(String(term).toLowerCase().split(/[^a-z0-9]+/).filter((x) => x.length > 1));
  const ct = new Set(String(c.name).toLowerCase().split(/[^a-z0-9]+/).filter((x) => x.length > 1));
  let inter = 0; for (const x of tt) if (ct.has(x)) inter++;
  return tt.size ? Math.round((inter / tt.size) * 60) : 0;
}

// ---------- paced http ----------
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const DELAY = 1500;
let lastReq = 0;
function curlRaw(url) {
  const args = ["-g", "-sS", "-m", "40", "-A", "SamMeow-modpack-research/0.1"];
  if (PROXY) args.push("-x", PROXY);
  args.push(url);
  return execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 });
}
async function paced(url) {
  for (let attempt = 0; attempt < 6; attempt++) {
    const wait = Math.max(0, DELAY - (Date.now() - lastReq));
    if (wait) await sleep(wait);
    lastReq = Date.now();
    let txt;
    try { txt = curlRaw(url); } catch { await sleep(4000); continue; }
    if (/slow down/i.test(txt)) { console.log("   [throttled] backoff 25s"); await sleep(25000); continue; }
    try { return JSON.parse(txt); } catch { await sleep(3000); continue; }
  }
  return null;
}

// ---------- stage 1: filtered searches for unresolved ----------
const unresolved = report.filter((r) => !r.selected);
console.log(`stage1: ${unresolved.length} unresolved entries`);
let s1done = 0;
for (const r of unresolved) {
  const f = join(S4, `${r.idx}.json`);
  if (existsSync(f)) { try { if (JSON.parse(readFileSync(f, "utf8"))?.done) { s1done++; continue; } } catch {} }
  const it = { idx: r.idx, entry: r.entry, rec: mods[r.entry] ?? {} };
  const terms = termCandidates(it);
  const row = { idx: r.idx, entry: r.entry, terms, all: [], done: true };
  for (const term of terms) {
    const d = await paced(`https://sp-mod.com/api/v0/mods?query=${encodeURIComponent(term)}&filter[spt_version]=4.1.6&per_page=10`);
    if (d?.data) for (const c of d.data) row.all.push({ term, id: c.id, name: c.name, slug: c.slug, dl: c.downloads, sc: scoreCandidate(term, c) });
    if (row.all.some((x) => x.sc >= 80)) break;
  }
  row.all.sort((a, b) => b.sc - a.sc || b.dl - a.dl);
  writeFileSync(f, JSON.stringify(row, null, 1), "utf8");
  s1done++;
  const top = row.all[0];
  console.log(`  ${String(r.idx).padStart(3)} ${top ? `${top.sc} ${top.id}|${top.name}` : "NO-MATCH"}`);
}
console.log(`stage1 done`);

// ---------- collect id set for filtered version fetch ----------
const idSet = new Set();
for (const r of unresolved) {
  const f = join(S4, `${r.idx}.json`);
  if (!existsSync(f)) continue;
  const row = JSON.parse(readFileSync(f, "utf8"));
  for (const c of row.all.slice(0, 5)) if (c.sc >= 55) idSet.add(c.id);
}
for (const e of chosen.entries) for (const cid of e.candidates) idSet.add(cid); // 原始候选也复核（过滤抓取）
for (const extra of [416, 742, 1261, 2248, 1459, 1192, 2344]) idSet.add(extra);
const ids = [...idSet];
console.log(`stage2: filtered version fetch for ${ids.length} ids`);

// ---------- stage 2: filtered versions ----------
let s2done = 0;
for (const id of ids) {
  const f = join(F416, `${id}.json`);
  if (existsSync(f)) { try { const j = JSON.parse(readFileSync(f, "utf8")); if (Array.isArray(j?.data)) { s2done++; continue; } } catch {} }
  const d = await paced(`https://sp-mod.com/api/v0/mod/${id}/versions?filter[spt_version]=4.1.6&per_page=100&include=dependencies`);
  writeFileSync(f, JSON.stringify(d, null, 1), "utf8");
  s2done++;
  if (s2done % 25 === 0) console.log(`  fetched ${s2done}/${ids.length}`);
}
console.log(`stage2 done`);

// ---------- stage 3: final selection ----------
function loadF(id) {
  try { const j = JSON.parse(readFileSync(join(F416, `${id}.json`), "utf8")); return Array.isArray(j?.data) ? j.data : null; } catch { return null; }
}
const nameMap = new Map();
try {
  for (const c of JSON.parse(readFileSync("E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\knowledge\\spt-kb\\archive\\forge\\api\\mods-catalog.json", "utf8").replace(/^\uFEFF/, ""))) nameMap.set(c.id, c.name);
} catch {}
// merge names from search4/1 drafts
for (const r of report) if (r.selected?.name) nameMap.set(r.selected.id, r.selected.name);
for (const r of unresolved) {
  const f = join(S4, `${r.idx}.json`);
  if (existsSync(f)) { try { for (const c of JSON.parse(readFileSync(f, "utf8")).all) nameMap.set(c.id, c.name); } catch {} }
}

const finalRows = [];
for (const e of chosen.entries) {
  const entryName = draft.find((d) => d.idx === e.idx)?.entry ?? `idx-${e.idx}`;
  const order = [];
  if (!report.find((r) => r.idx === e.idx)?.selected) {
    const f = join(S4, `${e.idx}.json`);
    if (existsSync(f)) { try { for (const c of JSON.parse(readFileSync(f, "utf8")).all) if (c.sc >= 55) order.push(c.id); } catch {} }
  }
  for (const cid of e.candidates) order.push(cid);
  const seen = new Set();
  let selected = null;
  for (const cid of order) {
    if (seen.has(cid)) continue; seen.add(cid);
    const vs = loadF(cid);
    if (!vs) continue;
    if (!vs.length) continue;
    const sorted = [...vs].sort((a, b) => String(b.published_at ?? "").localeCompare(String(a.published_at ?? "")));
    selected = {
      id: cid, name: nameMap.get(cid) ?? "?",
      version: sorted[0].version, constraint: sorted[0].spt_version_constraint,
      link: sorted[0].link, size: sorted[0].content_length,
      published: String(sorted[0].published_at ?? "").slice(0, 10),
      altVersions: sorted.slice(1, 4).map((v) => `${v.version}(${v.spt_version_constraint})`),
    };
    break;
  }
  finalRows.push({ idx: e.idx, entry: entryName, selected });
}

const tsv = finalRows.map((r) => {
  if (r.selected) { const s = r.selected; return [String(r.idx), r.entry, String(s.id), s.name, s.version, s.constraint ?? "", s.published, s.size ?? "", s.link ?? ""].join("\t"); }
  return [String(r.idx), r.entry, "", "", "", "", "", "", "UNRESOLVED"].join("\t");
}).join("\n");
writeFileSync(join(SCRATCH, "final-selected.tsv"), tsv, "utf8");
writeFileSync(join(SCRATCH, "final-report.json"), JSON.stringify(finalRows, null, 1), "utf8");
const ok = finalRows.filter((r) => r.selected).length;
console.log(`FINAL: selected ${ok} / ${finalRows.length}`);
for (const r of finalRows.filter((x) => !x.selected)) console.log(`  UNRESOLVED #${r.idx} ${r.entry}`);
