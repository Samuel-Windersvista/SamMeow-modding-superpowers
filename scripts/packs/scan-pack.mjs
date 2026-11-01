#!/usr/bin/env node
// scan-pack.mjs —— 扫描一个 MO2 形态的 SPT 整合包，产出可蒸馏的结构化快照。
//
// 输入：包根（含 mods/、profiles/、可选 ModOrganizer.ini）
// 输出：JSON（默认 stdout；--out 写文件）
//
// 用法：
//   node scripts/packs/scan-pack.mjs --pack-root "E:/Game/EFT_Offline/Norvinsk_Forever" --slug norvinsk-forever --out <file.json>
//
// 只读：不修改包内任何文件。
import { readFileSync, writeFileSync, readdirSync, existsSync, mkdirSync } from "node:fs";
import { join, basename, dirname } from "node:path";

function parseArgs(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (!a.startsWith("--")) continue;
    const eq = a.indexOf("=");
    if (eq !== -1) out[a.slice(2, eq)] = a.slice(eq + 1);
    else {
      const next = argv[i + 1];
      if (next !== undefined && !next.startsWith("--")) { out[a.slice(2)] = next; i++; }
      else out[a.slice(2)] = true;
    }
  }
  return out;
}

const opts = parseArgs(process.argv.slice(2));
const packRoot = String(opts["pack-root"] ?? "");
const slug = String(opts.slug ?? basename(packRoot));
if (!packRoot || !existsSync(packRoot)) {
  console.error("[scan-pack] 缺少或找不到 --pack-root: " + packRoot);
  process.exit(2);
}

const stripBom = (s) => (s.charCodeAt(0) === 0xfeff ? s.slice(1) : s);

/** 解析 MO2 meta.ini：[General] 段 key=value，去引号、还原 \n 转义 */
function parseMetaIni(file) {
  const raw = stripBom(readFileSync(file, "utf8"));
  const out = {};
  let section = "";
  for (const line of raw.split(/\r?\n/)) {
    const sec = /^\[([^\]]+)\]/.exec(line);
    if (sec) { section = sec[1]; continue; }
    if (section !== "General") continue;
    const m = /^([A-Za-z0-9_.-]+)=(.*)$/.exec(line);
    if (!m) continue;
    let value = m[2];
    if (value.length >= 2 && value.startsWith('"') && value.endsWith('"')) {
      value = value.slice(1, -1).replace(/\\n/g, "\n").replace(/\\"/g, '"');
    }
    out[m[1]] = value;
  }
  return out;
}

/**
 * modlist.txt → 条目 + 区块。
 *
 * 分组语义：SamMeow 的包用「分隔符作区块页脚」——_separator 标记其**上方**区块的结束，
 * 而不是下方区块的标题。所以遇到分隔符时结算它上面累积的 pending mods。
 * （此前的实现反了：把 mod 归给上方的分隔符，会把 [12]修复 组塞进 [11] 的条目。）
 */
function parseModlist(file) {
  const raw = stripBom(readFileSync(file, "utf8"));
  const entries = [];
  const blocks = [];
  let pending = [];
  for (const line of raw.split(/\r?\n/)) {
    if (!line || line.startsWith("#")) continue;
    const flag = line[0];
    if (flag !== "+" && flag !== "-") continue;
    const name = line.slice(1);
    if (name.endsWith("_separator")) {
      blocks.push({ separatorName: name, enabled: flag === "+", mods: pending });
      entries.push({ type: "separator", name, enabled: flag === "+" });
      pending = [];
      continue;
    }
    entries.push({ type: "mod", name, enabled: flag === "+" });
    pending.push({ name, enabled: flag === "+" });
  }
  if (pending.length > 0) blocks.push({ separatorName: "(未分组)", enabled: true, mods: pending });
  return { entries, blocks };
}

const docDirs = existsSync(join(packRoot, "docs")) ? readdirSync(join(packRoot, "docs"), { withFileTypes: true }).filter(d => d.isFile()).map(d => d.name) : [];
const modsDir = join(packRoot, "mods");
const modDirs = existsSync(modsDir) ? readdirSync(modsDir, { withFileTypes: true }).filter(d => d.isDirectory()).map(d => d.name) : [];
const modByDir = new Map();
for (const name of modDirs) {
  const metaPath = join(modsDir, name, "meta.ini");
  const meta = existsSync(metaPath) ? parseMetaIni(metaPath) : {};
  // 三套 id 体系不可混用：sp-mod.com/mod/<id>（Forge 现址）、hub.sp-tarkov.com/files/file/<id>（旧 Hub）、Nexus。
  const { forgeId, hubId, nexusId } = (() => {
    const u = String(meta.url ?? "");
    const f = /sp-mod\.com\/mod\/(\d+)/.exec(u);
    const h = /hub\.sp-tarkov\.com\/files\/file\/(\d+)/.exec(u);
    const n = /nexusmods\.com\/[^/]+\/mods\/(\d+)/.exec(u);
    return { forgeId: f ? Number(f[1]) : null, hubId: h ? Number(h[1]) : null, nexusId: n ? Number(n[1]) : null };
  })();
  const hasBepInEx = existsSync(join(modsDir, name, "BepInEx")) || existsSync(join(modsDir, name, "plugins"));
  const hasServerMod = existsSync(join(modsDir, name, "user")) || existsSync(join(modsDir, name, "SPT_Data"));
  modByDir.set(name, {
    dir: name,
    version: String(meta.version ?? ""),
    url: String(meta.url ?? ""),
    forgeId,
    hubId,
    nexusId,
    category: String(meta.category ?? ""),
    comments: String(meta.comments ?? ""),
    notes: String(meta.notes ?? ""),
    installationFile: String(meta.installationFile ?? ""),
    hasBepInEx,
    hasServerMod,
  });
}

const profilesDir = join(packRoot, "profiles");
const profiles = [];
if (existsSync(profilesDir)) {
  for (const d of readdirSync(profilesDir, { withFileTypes: true }).filter(d => d.isDirectory())) {
    const ml = join(profilesDir, d.name, "modlist.txt");
    if (!existsSync(ml)) continue;
    const parsed = parseModlist(ml);
    const mods = parsed.entries.filter(e => e.type === "mod");
    profiles.push({
      name: d.name,
      modlistPath: "profiles/" + d.name + "/modlist.txt",
      counts: {
        entries: parsed.entries.length,
        mods: mods.length,
        enabled: mods.filter(m => m.enabled).length,
        disabled: mods.filter(m => !m.enabled).length,
        separators: parsed.blocks.filter((b) => b.separatorName !== "(未分组)").length,
      },
      // 区块页脚式分组：block 名 = 其页脚分隔符，mods = 页脚上方累积的条目
      groups: parsed.blocks.map((b) => ({
        name: b.separatorName,
        enabled: b.enabled,
        mods: b.mods.map((m) => ({ name: m.name, enabled: m.enabled, meta: modByDir.get(m.name) ?? null })),
      })),
    });
  }
}

const iniPath = join(packRoot, "ModOrganizer.ini");
let mo2 = null;
if (existsSync(iniPath)) {
  const raw = stripBom(readFileSync(iniPath, "utf8"));
  const pick = (k) => { const m = new RegExp("^" + k + "=(.*)$", "m").exec(raw); return m ? m[1].replace(/^@ByteArray\((.*)\)$/, "$1") : null; };
  mo2 = { gameName: pick("gameName"), gamePath: pick("gamePath"), selectedProfile: pick("selectedProfile"), version: pick("version"), portable: pick("portable") };
}

const payload = {
  slug,
  packRoot,
  capturedAt: new Date().toISOString(),
  mo2,
  docs: docDirs,
  counts: {
    modDirs: modDirs.length,
    modDirsWithMeta: [...modByDir.values()].filter(m => m.version || m.url || m.comments).length,
    modsWithForgeId: [...modByDir.values()].filter(m => m.forgeId !== null).length,
  },
  profiles,
  mods: [...modByDir.values()].sort((a, b) => a.dir.localeCompare(b.dir, "zh")),
};

const json = JSON.stringify(payload, null, 2);
if (opts.out) { mkdirSync(dirname(String(opts.out)), { recursive: true }); writeFileSync(String(opts.out), json, "utf8"); console.log("[scan-pack] written " + opts.out + " | modDirs=" + payload.counts.modDirs + " profiles=" + profiles.length + " forgeIds=" + payload.counts.modsWithForgeId); }
else console.log(json);
