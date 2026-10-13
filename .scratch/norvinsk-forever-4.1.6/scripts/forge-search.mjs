// forge-search.mjs v2 —— 对 Life 清单中的上游 mod 逐条查询 Forge API，产出候选匹配草稿
// 变更：代理优先、并发 4、超时 25s、断点续跑、二次检索（目录英文/搜索 dll 名）
// 用法: node forge-search.mjs
import { readFileSync, writeFileSync, mkdirSync, existsSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const LIFE = "E:\\Game\\EFT_Offline\\Life_in_Norvinsk_v0.3.2";
const SEARCH_DIR = join(SCRATCH, "forge-search");
mkdirSync(SEARCH_DIR, { recursive: true });
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";

const inv = JSON.parse(readFileSync(join(SCRATCH, "life-inventory.json"), "utf8"));
const mods = inv.mods;

const mlRaw = readFileSync(join(LIFE, "profiles", "单机运行整合包", "modlist.txt"), "utf8").replace(/^\uFEFF/, "");
const activeList = mlRaw
  .split(/\r?\n/)
  .map((l) => l.match(/^([+-])(.+)$/))
  .filter(Boolean)
  .map((m) => ({ enabled: m[1] === "+", name: m[2] }));

const excluded = [];
const entries = [];
for (const { enabled, name } of activeList) {
  const rec = mods[name] ?? { name };
  if (!enabled) { excluded.push({ name, reason: "disabled-in-reference" }); continue; }
  if (rec.separator) { excluded.push({ name, reason: "separator" }); continue; }
  if (name.startsWith("《生活在诺文斯克》")) { excluded.push({ name, reason: "custom-pack-content(deferred)" }); continue; }
  if (name.includes("修改本体") || name.includes("好奇猫自用游戏设置")) { excluded.push({ name, reason: "custom-pack-content(deferred)" }); continue; }
  if ((rec.clientDlls ?? []).some((d) => d.toLowerCase().includes("sammeow."))) { excluded.push({ name, reason: "custom-sammeow-guard(deferred)" }); continue; }
  entries.push({ name, rec });
}

const GENERIC = new Set(["weapons", "items", "mods", "system", "pack", "tool", "core", "update"]);
function norm(s) { return String(s ?? "").toLowerCase().replace(/[^a-z0-9]/g, ""); }
function folderLatin(name) {
  return name.replace(/^\[\d+(\.\d+)?\]/, "").replace(/[^\x20-\x7E]/g, " ").replace(/\s+/g, " ").trim();
}
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
  const lat = folderLatin(it.name);
  if (lat) terms.push(lat);
  terms.push(...dllTerms(it.rec));
  return [...new Set(terms.filter(Boolean))];
}

function curlJsonOnce(url, proxy) {
  const args = ["-g", "-sS", "-m", "25", "-A", "SamMeow-modpack-research/0.1"];
  if (proxy) args.push("-x", proxy);
  args.push(url);
  const out = execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 32 * 1024 * 1024 });
  return JSON.parse(out);
}
function curlJson(url) {
  try {
    return curlJsonOnce(url, PROXY);
  } catch {
    try {
      return curlJsonOnce(url, null);
    } catch {
      return null;
    }
  }
}

function scoreCandidate(term, c) {
  const t = norm(term);
  const n = norm(c.name);
  const s = norm(c.slug);
  if (!t) return 0;
  if (t === n || t === s) return 100;
  if (n.includes(t) || t.includes(n)) return 82;
  if (s.includes(t)) return 74;
  const tt = new Set(String(term).toLowerCase().split(/[^a-z0-9]+/).filter((x) => x.length > 1));
  const ct = new Set(String(c.name).toLowerCase().split(/[^a-z0-9]+/).filter((x) => x.length > 1));
  let inter = 0;
  for (const x of tt) if (ct.has(x)) inter++;
  const ratio = tt.size ? inter / tt.size : 0;
  return Math.round(ratio * 60);
}

function searchTerm(idx, term) {
  const cachePath = join(SEARCH_DIR, `t-${norm(term).slice(0, 40) || "x"}-${idx}.json`);
  if (existsSync(cachePath)) {
    try {
      const c = JSON.parse(readFileSync(cachePath, "utf8"));
      if (c?.data?.data) return c.data.data;
    } catch {}
  }
  const url = `https://sp-mod.com/api/v0/mods?query=${encodeURIComponent(term)}&per_page=10`;
  const data = curlJson(url);
  writeFileSync(cachePath, JSON.stringify({ term, data }, null, 1), "utf8");
  return data?.data ?? null;
}

async function processEntry(it, idx) {
  const row = { idx, entry: it.name, term: null, status: "MANUAL", match: null, candidates: [], tried: [] };
  const terms = termCandidates(it);
  for (const term of terms.slice(0, 3)) {
    row.tried.push(term);
    const list = searchTerm(idx, term);
    if (!list) continue;
    const cands = list.map((c) => ({ id: c.id, name: c.name, slug: c.slug, downloads: c.downloads, score: scoreCandidate(term, c) }));
    cands.sort((a, b) => b.score - a.score || b.downloads - a.downloads);
    if (cands[0] && cands[0].score > (row.match?.score ?? 0)) { row.match = cands[0]; row.term = term; }
    row.candidates = [...row.candidates, ...cands.slice(0, 3)].slice(0, 6);
    if (row.match && row.match.score >= 80) break;
    await new Promise((r) => setTimeout(r, 120));
  }
  if (row.match && row.match.score >= 80) row.status = "AUTO";
  else if (row.match && row.match.score >= 55) row.status = "LIKELY";
  return row;
}

const results = new Array(entries.length);
const CONC = 4;
let cursor = 0;
async function worker() {
  while (true) {
    const i = cursor++;
    if (i >= entries.length) return;
    try {
      results[i] = await processEntry(entries[i], i + 1);
    } catch (e) {
      results[i] = { idx: i + 1, entry: entries[i].name, status: "ERROR", error: String(e).slice(0, 200), candidates: [] };
    }
    const r = results[i];
    const m = r.match ? `${r.match.id}|${r.match.name}|${r.match.score}` : "-";
    console.log(`${String(i + 1).padStart(3)} ${String(r.status).padEnd(6)} ${String(r.term ?? "(none)").padEnd(30)} ${m}`);
  }
}
await Promise.all(Array.from({ length: CONC }, worker));

const draft = results
  .map((r) => {
    const alts = (r.candidates ?? []).slice(0, 3).map((c) => `${c.id}|${c.name}|${c.downloads}`).join(" ;; ");
    const m = r.match ? `${r.match.id}\t${r.match.name}\t${r.match.score}` : "\t\t";
    return [String(r.idx), r.entry, r.term ?? "", r.status, m, alts].join("\t");
  })
  .join("\n");
writeFileSync(join(SCRATCH, "forge-match-draft.tsv"), draft, "utf8");
writeFileSync(join(SCRATCH, "forge-match-draft.json"), JSON.stringify(results, null, 1), "utf8");
writeFileSync(join(SCRATCH, "excluded-entries.json"), JSON.stringify(excluded, null, 1), "utf8");

const stats = results.reduce((a, r) => ((a[r.status] = (a[r.status] ?? 0) + 1), a), {});
console.log("== stats ==", JSON.stringify(stats));
console.log("entries:", entries.length, "excluded:", excluded.length);
