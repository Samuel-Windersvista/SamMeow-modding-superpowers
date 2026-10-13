// forge-versions.mjs —— 拉取候选 mod 全量版本，按目标 SPT 4.1.6 选择版本
import { readFileSync, writeFileSync, mkdirSync, existsSync, readdirSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const VDIR = join(SCRATCH, "forge-versions");
const CATALOG = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\knowledge\\spt-kb\\archive\\forge\\api\\mods-catalog.json";
const PROXY = process.env.SP_MOD_PROXY ?? "http://127.0.0.1:7890";
const TARGET = "4.1.6";
mkdirSync(VDIR, { recursive: true });

const chosen = JSON.parse(readFileSync(join(SCRATCH, "forge-chosen.json"), "utf8"));
const draft = JSON.parse(readFileSync(join(SCRATCH, "forge-match-draft.json"), "utf8"));

// id -> name 多源合并
const nameMap = new Map();
try {
  const cat = JSON.parse(readFileSync(CATALOG, "utf8").replace(/^\uFEFF/, ""));
  for (const c of cat) nameMap.set(c.id, c.name);
} catch {}
for (const d of draft) for (const c of d.candidates ?? []) if (!nameMap.has(c.id)) nameMap.set(c.id, c.name);
for (const f of ["forge-search2.json", "forge-search3.json"]) {
  try {
    const arr = JSON.parse(readFileSync(join(SCRATCH, f), "utf8"));
    for (const t of arr) for (const r of t.rows ?? []) if (!nameMap.has(r.id)) nameMap.set(r.id, r.name);
  } catch {}
}
try {
  for (const f of readdirSync(join(SCRATCH, "forge-search")).filter((x) => x.startsWith("t-"))) {
    const j = JSON.parse(readFileSync(join(SCRATCH, "forge-search", f), "utf8"));
    for (const c of j?.data?.data ?? []) if (!nameMap.has(c.id)) nameMap.set(c.id, c.name);
  }
} catch {}

// ---------- semver ----------
function parseV(s) { const m = String(s).trim().match(/^(\d+)\.(\d+)\.(\d+)/); return m ? [+m[1], +m[2], +m[3]] : null; }
function cmp(a, b) { for (let i = 0; i < 3; i++) if (a[i] !== b[i]) return a[i] - b[i]; return 0; }
function nums(s) {
  const m = String(s).trim().match(/^(\d+)(?:\.(\d+))?(?:\.(\d+))?/);
  if (!m) return null;
  return [+m[1], +(m[2] ?? 0), +(m[3] ?? 0)];
}
function satPart(v, p) {
  p = String(p).trim(); if (!p || p === "*") return true;
  if (p.includes("||")) return p.split("||").some((x) => satPart(v, x));
  if (/\s/.test(p)) return p.split(/\s+/).every((x) => satPart(v, x));
  let m;
  if ((m = p.match(/^~\s*(.+)$/))) {
    const raw = m[1].split(".").filter((x) => /^\d+$/.test(x)).map(Number);
    if (raw.length >= 3) return cmp(v, raw) >= 0 && v[0] === raw[0] && v[1] === raw[1];
    if (raw.length === 2) return v[0] === raw[0] && v[1] === raw[1];
    if (raw.length === 1) return v[0] === raw[0];
    return false;
  }
  if ((m = p.match(/^\^\s*(.+)$/))) { const n = nums(m[1]); return n ? cmp(v, n) >= 0 && v[0] === n[0] : false; }
  if ((m = p.match(/^>=\s*(.+)$/))) { const n = nums(m[1]); return n ? cmp(v, n) >= 0 : false; }
  if ((m = p.match(/^<=\s*(.+)$/))) { const n = nums(m[1]); return n ? cmp(v, n) <= 0 : false; }
  if ((m = p.match(/^>(?!=)\s*(.+)$/))) { const n = nums(m[1]); return n ? cmp(v, n) > 0 : false; }
  if ((m = p.match(/^<(?!=)\s*(.+)$/))) { const n = nums(m[1]); return n ? cmp(v, n) < 0 : false; }
  if (/\.x$/i.test(p) || p.includes(".*")) {
    const parts = p.replace(/\.x$/i, ".*").split(".");
    const a = +parts[0], b = parts[1] === "*" ? null : +parts[1];
    return v[0] === a && (b === null || v[1] === b);
  }
  if (/^\d+\.\d+$/.test(p)) { const [a, b] = p.split(".").map(Number); return v[0] === a && v[1] === b; }
  const t = parseV(p); return t ? cmp(v, t) === 0 : false;
}
function satisfies(vStr, c) { const v = parseV(vStr); if (!v || !c) return false; return satPart(v, c); }

// ---------- http ----------
function curlJsonOnce(url, proxy) {
  const args = ["-g", "-sS", "-m", "30", "-A", "SamMeow-modpack-research/0.1"];
  if (proxy) args.push("-x", proxy);
  args.push(url);
  return JSON.parse(execFileSync("curl.exe", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 }));
}
function curlJson(url) {
  try { return curlJsonOnce(url, null); } catch { try { return curlJsonOnce(url, PROXY); } catch { return null; } }
}
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

// ---------- fetch all candidate versions ----------
const ids = [...new Set(chosen.entries.flatMap((e) => e.candidates))];
const fetchOne = async (id) => {
  const f = join(VDIR, `${id}.json`);
  if (existsSync(f)) { try { const j = JSON.parse(readFileSync(f, "utf8")); if (j?.data) return; } catch {} }
  const url = `https://sp-mod.com/api/v0/mod/${id}/versions?per_page=100&include=dependencies`;
  const d = curlJson(url);
  writeFileSync(f, JSON.stringify(d, null, 1), "utf8");
  if (!d) console.log(`  [warn] id=${id} fetch failed`);
  await sleep(120);
};
let cursor = 0, done = 0;
const CONC = 4;
await Promise.all(
  Array.from({ length: CONC }, async () => {
    while (true) {
      const i = cursor++;
      if (i >= ids.length) return;
      await fetchOne(ids[i]);
      done++;
      if (done % 20 === 0) console.log(`fetched ${done}/${ids.length}`);
    }
  })
);
console.log("version fetch done:", ids.length);

// ---------- selection ----------
function loadVersions(id) {
  try { return JSON.parse(readFileSync(join(VDIR, `${id}.json`), "utf8"))?.data ?? null; } catch { return null; }
}
const target = parseV(TARGET);
const report = [];
for (const e of chosen.entries) {
  const entryName = draft.find((d) => d.idx === e.idx)?.entry ?? `idx-${e.idx}`;
  const row = { idx: e.idx, entry: entryName, flag: e.flag ?? null, selected: null, rejected: [], manual: false };
  for (const cid of e.candidates) {
    const vs = loadVersions(cid);
    if (!vs || !vs.length) { row.rejected.push({ id: cid, name: nameMap.get(cid) ?? "?", reason: "no-data" }); continue; }
    const ok = vs.filter((v) => satisfies(TARGET, v.spt_version_constraint));
    ok.sort((a, b) => String(b.published_at ?? "").localeCompare(String(a.published_at ?? "")) || (parseV(b.version)?.[2] ?? 0) - (parseV(a.version)?.[2] ?? 0));
    if (ok.length) {
      const v = ok[0];
      row.selected = {
        id: cid, name: nameMap.get(cid) ?? "?", version: v.version, constraint: v.spt_version_constraint,
        link: v.link, size: v.content_length, published: (v.published_at ?? "").slice(0, 10),
        deps: (v.dependencies ?? []).map((d) => d.mod_name ?? d.mod_id ?? d.name ?? "?"),
      };
      break;
    } else {
      const near = vs.slice(0, 3).map((v) => `${v.version}(${v.spt_version_constraint ?? "null"})`);
      row.rejected.push({ id: cid, name: nameMap.get(cid) ?? "?", reason: "no-4.1.6", near });
    }
  }
  if (!row.selected) row.manual = e.candidates.length === 0 || row.rejected.every((r) => r.reason !== "no-data");
  // 真正 manual: 无候选 或 所有候选都抓不到数据
  if (!row.selected) {
    row.manual = e.candidates.length === 0;
    if (!row.manual) {
      const allNoData = row.rejected.length > 0 && row.rejected.every((r) => r.reason === "no-data" || r.reason === "fetch-failed");
      if (allNoData) row.manual = "fetch-missing";
    }
  }
  report.push(row);
}

const tsv = report
  .map((r) => {
    if (r.selected) {
      const s = r.selected;
      return [String(r.idx), r.entry, String(s.id), s.name, s.version, s.constraint ?? "", s.published, s.size ?? "", s.link ?? "", r.flag ?? ""].join("\t");
    }
    return [String(r.idx), r.entry, "", "", "", "", "", "", "", r.manual === true ? "MANUAL" : r.manual === "fetch-missing" ? "FETCH-MISSING" : "NO-4.1.6"].join("\t");
  })
  .join("\n");
writeFileSync(join(SCRATCH, "selected-versions.tsv"), tsv, "utf8");
writeFileSync(join(SCRATCH, "selection-report.json"), JSON.stringify(report, null, 1), "utf8");

const sel = report.filter((r) => r.selected).length;
const no416 = report.filter((r) => !r.selected).length;
console.log(`selected: ${sel} / unresolved: ${no416}`);
console.log("unresolved list:");
for (const r of report.filter((x) => !x.selected)) console.log(`  #${r.idx} ${r.entry} -> ${r.manual === true ? "MANUAL(no-candidate)" : r.manual === "fetch-missing" ? "FETCH-MISSING" : "NO-4.1.6"} rejected=${r.rejected.map((x) => x.id).join(",")}`);
