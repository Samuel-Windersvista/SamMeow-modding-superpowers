// porting-inventory.mjs —— 清点：未就绪 76 项 × 本地源码（GitHub 仓库 + Forge 归档源码快照）
import { readFileSync, writeFileSync, readdirSync, existsSync, statSync } from "node:fs";
import { join } from "node:path";

const PENDING_MD = "E:\\Game\\EFT_Offline\\Norvinsk_Forever\\docs\\pending-mods-v0.1.0.md";
const GITHUB = "E:\\云文件\\GitHub";
const ARCHIVE = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\knowledge\\spt-kb\\archive\\forge\\mods";
const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";

// ---- pending list ----
const pending = [];
for (const line of readFileSync(PENDING_MD, "utf8").split(/\r?\n/)) {
  const m = line.match(/^\|\s*\d+\s*\|\s*(.+?)\s*\|\s*$/);
  if (m) pending.push(m[1].trim());
}

// ---- explicit repo→mod alias map (curated from earlier repo scan) ----
const REPO_MAP = {
  "SamMeow--Softcore": ["Softcore"],
  "SamMeow-barter_economy": ["barter_economy"],
  "SamMeow-HomeComforts-for3114": ["HomeComforts"],
  "SamMeow-LeaveItThere-For3114": ["LeaveItThere"],
  "SamMeow-Raid-Overhaul-For-3114": ["RaidOverhaul"],
  "SamMeow-ThatsLit-For-3114": ["ThatsLit", "That.s.Lit"],
  "SamMeow-PathToTarkov": ["Path to Tarkov", "PTT", "path-to-tarkov"],
  "SamMeow-ImmersiveDaylightCycle": ["ImmersiveDaylightCycle"],
  "SamMeow-HollywoodCam": ["HollywoodCam"],
  "SamMeow-TarkovIRL_WeaponsHandlingMod": ["TarkovIRL"],
  "SamMeow-InteractableExfilsAPI": ["IEAPI", "InteractableExfilsAPI"],
  "JoshMate-BetterBackpacks": ["BetterBackpacks"],
  "WhiteBoxFix": ["WhiteBoxFix"],
  "LIN-Realism-Mod-Client": ["Realism"],
  "LIN-Realism-Server": ["Realism"],
  "Moew-SAIN-For-4013": ["SAIN"],
  "Moew-SAIN-For-3114": ["SAIN"],
  "Moew-botplacementsystem": ["botplacementsystem"],
  "Moew-ProgressiveBotSystem": ["progressivebotsystem"],
  "Moew-LootingBot-For-3114": ["LootingBots"],
  "SamMeow-Skills-Extended": ["SkillsExtended"],
  "SamMeow-UIfixes": ["UIFixes"],
  "SamMeow-AutoDeposit": ["AutoDeposit"],
  "SamMeow-ContainerQuickLoot": ["ContainerQuickLoot"],
  "SamMeow-ContinuousLoadAmmo": ["ContinuousLoadAmmo"],
  "SamMeow-HollywoodFX": ["HollywoodFX"],
  "SamMeow-QuestTracker": ["QuestTracker"],
  "SamMeow-friendlypmc": ["friendlypmc"],
  "SamMeow-Inescapable-Tarkovs-Softcore": ["Softcore"],
};

function pendingMatches(p) {
  const hits = [];
  const low = p.toLowerCase();
  for (const [repo, aliases] of Object.entries(REPO_MAP)) {
    for (const a of aliases) {
      if (low.includes(a.toLowerCase()) || p.includes(a)) { hits.push(repo); break; }
    }
  }
  return [...new Set(hits)];
}

// ---- archive source matching ----
const archiveDirs = readdirSync(ARCHIVE).filter((d) => d.endsWith("_source"));
const archMatch = [];
for (const p of pending) {
  // 提取英文关键词（含驼峰拆分）
  const ascii = p.replace(/[^\x20-\x7E]/g, " ").replace(/[\[\]\(\)+]/g, " ");
  const toks = new Set();
  for (const t of ascii.split(/[-_\s.]+/)) {
    for (const part of t.replace(/([a-z])([A-Z])/g, "$1 $2").split(" ")) {
      if (/^[a-zA-Z]{4,}$/.test(part)) toks.add(part.toLowerCase());
    }
  }
  const keys = [...toks].filter((t) => !/^(spt|the|for|and|with|mod|mods|core)$/.test(t));
  if (!keys.length) continue;
  const hits = archiveDirs.filter((d) => {
    const dl = d.toLowerCase();
    return keys.some((k) => dl.includes(k));
  });
  if (hits.length) archMatch.push({ pending: p, keys, hits: hits.slice(0, 4) });
}

// ---- repo facts ----
function repoFacts(name) {
  const dir = join(GITHUB, name);
  if (!existsSync(dir)) return null;
  const facts = { name, exists: true, sptVersion: [], tf: [], langs: { ts: 0, cs: 0 }, has4xHint: false };
  const walk = (d, depth) => {
    if (depth > 3) return;
    let ents = [];
    try { ents = readdirSync(d, { withFileTypes: true }); } catch { return; }
    for (const e of ents) {
      if (e.isDirectory()) {
        if (/^(\.git|node_modules|\.vs|obj|bin)$/i.test(e.name)) continue;
        walk(join(d, e.name), depth + 1);
      } else {
        const pth = join(d, e.name);
        if (e.name === "package.json" && facts.sptVersion.length < 3) {
          try { const j = JSON.parse(readFileSync(pth, "utf8").replace(/^\uFEFF/, "")); if (j.sptVersion) facts.sptVersion.push(String(j.sptVersion)); } catch {}
        } else if (e.name.endsWith(".csproj") && facts.tf.length < 4) {
          try { const t = readFileSync(pth, "utf8"); const m = t.match(/<TargetFramework[s]?>([^<]+)</); if (m) facts.tf.push(m[1]); } catch {}
        } else if (e.name.endsWith(".ts")) facts.langs.ts++;
        else if (e.name.endsWith(".cs")) facts.langs.cs++;
      }
    }
  };
  walk(dir, 0);
  if (facts.sptVersion.some((v) => /^~?4\./.test(v))) facts.has4xHint = true;
  if (facts.tf.some((v) => v.includes("net9") || v.includes("net10"))) facts.has4xHint = true;
  return facts;
}

const lines = [];
lines.push("# 未就绪 × 本地源码 —— 4.1.6 移植候选清点");
lines.push("");
lines.push(`未就绪 ${pending.length} 项；以下为「本地存在源码」的交集。`);
lines.push("");
lines.push("## A. 自有 GitHub 仓库命中");
lines.push("");
const seen = new Set();
for (const p of pending) {
  const repos = pendingMatches(p);
  if (!repos.length) continue;
  for (const r of repos) {
    if (seen.has(r + "|" + p)) continue;
    seen.add(r + "|" + p);
    const f = repoFacts(r);
    if (!f) continue;
    lines.push(`- ${p}`);
    lines.push(`  - 仓库: ${r} | sptVersion: [${f.sptVersion.join(", ")}] | csproj TF: [${f.tf.join(", ")}] | .ts/.cs: ${f.langs.ts}/${f.langs.cs} | 4.x迹象: ${f.has4xHint}`);
  }
}
lines.push("");
lines.push("## B. Forge 归档源码快照命中");
lines.push("");
for (const m of archMatch) {
  lines.push(`- ${m.pending}  → [${m.hits.join(" ; ")}]`);
}
writeFileSync(join(SCRATCH, "porting-candidates.md"), lines.join("\n"), "utf8");
console.log(lines.join("\n"));
