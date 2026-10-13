// search4.mjs —— 未决条目重检索：Forge 原生 filter[spt_version]=4.1.6
import { readFileSync, writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const LIFE = "E:\\Game\\EFT_Offline\\Life_in_Norvinsk_v0.3.2";
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";

const inv = JSON.parse(readFileSync(join(SCRATCH, "life-inventory.json"), "utf8"));
const mods = inv.mods;
const chosen = JSON.parse(readFileSync(join(SCRATCH, "forge-chosen.json"), "utf8"));
const report = JSON.parse(readFileSync(join(SCRATCH, "selection-report.json"), "utf8"));
const draft = JSON.parse(readFileSync(join(SCRATCH, "forge-match-draft.json"), "utf8"));

const unresolved = report.filter((r) => !r.selected);
const targets = unresolved.map((r) => ({ idx: r.idx, entry: r.entry, rec: mods[r.entry] ?? {} }));
console.log("targets:", targets.length);

const GENERIC = new Set(["weapons", "items", "mods", "system", "pack", "tool", "core", "update"]);
function norm(s) { return String(s ?? "").toLowerCase().replace(/[^a-z0-9]/g, ""); }
function folderLatin(name) { return name.replace(/^\[\d+(\.\d+)?\]/, "").replace(/[^\x20-\x7E]/g, " ").replace(/\s+/g, " ").trim(); }
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
function curlJsonOnce(url, proxy) {
  const args = ["-g", "-sS", "-m", "30", "-A", "SamMeow-modpack-research/0.1"];
  if (proxy) args.push("-x", proxy);
  args.push(url);
  return JSON.parse(execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 32 * 1024 * 1024 }));
}
function curlJson(url) { try { return curlJsonOnce(url, null); } catch { try { return curlJsonOnce(url, PROXY); } catch { return null; } } }
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function handle(it) {
  const row = { idx: it.idx, entry: it.entry, terms: [], best: null, all: [] };
  const terms = termCandidates(it);
  row.terms = terms;
  for (const term of terms) {
    const url = `https://sp-mod.com/api/v0/mods?query=${encodeURIComponent(term)}&filter[spt_version]=4.1.6&per_page=10`;
    const d = curlJson(url);
    if (d?.data) {
      for (const c of d.data) {
        const sc = scoreCandidate(term, c);
        row.all.push({ term, id: c.id, name: c.name, slug: c.slug, dl: c.downloads, sc });
        if (!row.best || sc > row.best.sc) row.best = { term, id: c.id, name: c.name, slug: c.slug, dl: c.downloads, sc };
      }
    }
    await sleep(120);
    if (row.best && row.best.sc >= 80) break;
  }
  return row;
}

const results = new Array(targets.length);
let cursor = 0, done = 0;
async function worker() {
  while (true) {
    const i = cursor++;
    if (i >= targets.length) return;
    results[i] = await handle(targets[i]);
    done++;
    const b = results[i].best;
    console.log(`${String(targets[i].idx).padStart(3)} ${b ? `${b.sc} ${b.id}|${b.name}` : "NO-MATCH"}  << ${results[i].entry.slice(0, 40)}`);
  }
}
await Promise.all(Array.from({ length: 4 }, worker));
writeFileSync(join(SCRATCH, "forge-search4.json"), JSON.stringify(results, null, 1), "utf8");

const tsv = results
  .map((r) => [String(r.idx), r.entry, r.best ? `${r.best.id}\t${r.best.name}\t${r.best.sc}` : "\t\t", (r.all ?? []).slice(0, 4).map((c) => `${c.id}|${c.name}|${c.sc}`).join(" ;; ")].join("\t"))
  .join("\n");
writeFileSync(join(SCRATCH, "search4-draft.tsv"), tsv, "utf8");
console.log("saved forge-search4.json / search4-draft.tsv");
