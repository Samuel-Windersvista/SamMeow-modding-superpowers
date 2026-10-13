// life-inventory.mjs —— 扫描 Life_in_Norvinsk_v0.3.2 实例，产出参照包 mod 全清单情报
// 用法: node life-inventory.mjs
import { readFileSync, writeFileSync, existsSync, readdirSync } from "node:fs";
import { join, relative } from "node:path";

const LIFE = "E:\\Game\\EFT_Offline\\Life_in_Norvinsk_v0.3.2";
const OUT = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const PROF = join(LIFE, "profiles", "单机运行整合包");

function readModlist(p) {
  if (!existsSync(p)) return null;
  const t = readFileSync(p, "utf8").replace(/^\uFEFF/, "");
  const res = [];
  for (const line of t.split(/\r?\n/)) {
    const m = line.match(/^([+-])(.+)$/);
    if (!m) continue;
    res.push({ enabled: m[1] === "+", name: m[2] });
  }
  return res;
}

function names(list) {
  return new Set((list ?? []).map((x) => x.name));
}

function listDlls(root, maxDepth = 7) {
  const out = [];
  (function walk(dir, depth) {
    if (depth > maxDepth) return;
    let ents;
    try {
      ents = readdirSync(dir, { withFileTypes: true });
    } catch {
      return;
    }
    for (const e of ents) {
      const p = join(dir, e.name);
      if (e.isDirectory()) walk(p, depth + 1);
      else if (e.name.toLowerCase().endsWith(".dll"))
        out.push(relative(root, p).replaceAll("\\", "/"));
    }
  })(root, 0);
  return out;
}

// ---- load modlists ----
const active = readModlist(join(PROF, "modlist.txt"));
const recover = readModlist(join(PROF, "modlist.txt.请恢复这个单机排序260920"));
const def = readModlist(join(LIFE, "profiles", "default", "modlist.txt"));
if (!active) {
  console.error("FATAL: active modlist not found");
  process.exit(1);
}

const activeNames = names(active);
const recoverNames = names(recover);
const defNames = names(def);

const diffs = {
  onlyInActive: [...activeNames].filter((n) => !recoverNames.has(n)),
  onlyInRecover: [...recoverNames].filter((n) => !activeNames.has(n)),
  activeCount: activeNames.size,
  recoverCount: recoverNames.size,
  defaultCount: defNames.size,
  activeEnabledCount: active.filter((x) => x.enabled).length,
};

// ---- scan mod dirs ----
const MODS = join(LIFE, "mods");
const modDirs = readdirSync(MODS, { withFileTypes: true }).filter((e) => e.isDirectory()).map((e) => e.name);

const mods = {};
for (const name of modDirs) {
  const dir = join(MODS, name);
  const rec = {
    name,
    enabled: activeNames.has(name),
    separator: name.includes("_separator"),
    custom: name.startsWith("《生活在诺文斯克》"),
    aiOptimized: name.includes("已AI优化"),
    server: [],
    clientDlls: [],
    hasBepInEx: existsSync(join(dir, "BepInEx")),
    hasUserMods: existsSync(join(dir, "user", "mods")),
    topEntries: [],
  };
  try {
    rec.topEntries = readdirSync(dir).slice(0, 12);
  } catch {}
  // server mods
  const um = join(dir, "user", "mods");
  if (existsSync(um)) {
    for (const sub of readdirSync(um, { withFileTypes: true })) {
      if (!sub.isDirectory()) continue;
      const pj = join(um, sub.name, "package.json");
      if (!existsSync(pj)) continue;
      try {
        const p = JSON.parse(readFileSync(pj, "utf8").replace(/^\uFEFF/, ""));
        rec.server.push({ dir: sub.name, name: p.name, version: p.version, sptVersion: p.sptVersion ?? null, guid: p.guid ?? null });
      } catch (e) {
        rec.server.push({ dir: sub.name, name: null, version: null, error: String(e).slice(0, 80) });
      }
    }
  }
  // client plugins dlls
  const pl = join(dir, "BepInEx", "plugins");
  if (existsSync(pl)) {
    rec.clientDlls = readdirSync(pl, { withFileTypes: true }).length >= 0 ? listDlls(pl).map((x) => x.replace(/^/, "")) : [];
  }
  mods[name] = rec;
}

const referenced = new Set([...activeNames, ...recoverNames, ...defNames]);
const unreferencedDirs = modDirs.filter((n) => !referenced.has(n));

// ---- outputs ----
const inv = { diffs, mods, unreferencedDirs, referencedMissing: [...new Set([...activeNames, ...recoverNames].filter((n) => !existsSync(join(MODS, n))))] };
writeFileSync(join(OUT, "life-inventory.json"), JSON.stringify(inv, null, 1), "utf8");

// TSV: order = active modlist order, then remaining dirs alphabetically
const ordered = [];
const seen = new Set();
for (const x of active) {
  const rec = mods[x.name];
  ordered.push({ line: x.enabled ? "+" : "-", ...(rec ?? { name: x.name, missing: true }) });
  seen.add(x.name);
}
for (const n of modDirs.sort()) {
  if (seen.has(n)) continue;
  ordered.push({ line: "?", ...mods[n] });
}

const tsvLines = ordered.map((r) => {
  const flags = [
    r.line ?? "?",
    r.separator ? "SEP" : "",
    r.custom ? "CUSTOM" : "",
    r.aiOptimized ? "AIOPT" : "",
    r.missing ? "MISSING" : "",
    r.hasBepInEx ? "CLI" : "",
    r.hasUserMods ? "SRV" : "",
  ]
    .filter(Boolean)
    .join("|");
  const srv = (r.server ?? []).map((s) => `${s.name ?? s.dir}@${s.version ?? "?"}`).join("; ");
  const dlls = (r.clientDlls ?? []).map((d) => d.split("/").pop()).join(",");
  return [r.name, flags, srv, dlls].join("\t");
});
writeFileSync(join(OUT, "life-inventory.tsv"), tsvLines.join("\n"), "utf8");

console.log("== summary ==");
console.log(JSON.stringify(diffs, null, 1));
console.log("modDirs:", modDirs.length);
console.log("unreferencedDirs:", unreferencedDirs.length);
console.log("activeEnabled:", active.filter((x) => x.enabled).length, "activeDisabled:", active.filter((x) => !x.enabled).length);
console.log("written: life-inventory.json / life-inventory.tsv");
