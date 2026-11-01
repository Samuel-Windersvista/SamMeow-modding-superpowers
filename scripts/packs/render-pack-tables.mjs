#!/usr/bin/env node
// render-pack-tables.mjs —— 把 scan-pack.mjs 的 JSON 渲染成 markdown 表格片段。
//
// 用法：
//   node scripts/packs/render-pack-tables.mjs --scan <scan.json> [--profile <name>] [--mode summary|detail|all]
//
// 输出（stdout）：分组总览表（summary）+ 各分组明细表（detail）。
// 只读 scan.json，不碰实包。
import { readFileSync } from "node:fs";

function parseArgs(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (!a.startsWith("--")) continue;
    const eq = a.indexOf("=");
    if (eq !== -1) out[a.slice(2, eq)] = a.slice(eq + 1);
    else { const next = argv[i + 1]; if (next !== undefined && !next.startsWith("--")) { out[a.slice(2)] = next; i++; } else out[a.slice(2)] = true; }
  }
  return out;
}

const cell = (s, max) => String(s ?? "").replace(/\|/g, "／").replace(/\r?\n/g, " ").replace(/\s+/g, " ").trim().slice(0, max || 60);
const upstream = (m) => {
  if (!m) return "—";
  if (m.forgeId) return "Forge #" + m.forgeId;
  if (m.hubId) return "Hub #" + m.hubId;
  if (m.nexusId) return "Nexus #" + m.nexusId;
  if (m.url) { try { return new URL(m.url).hostname.replace(/^www\./, ""); } catch { return "链接"; } }
  return "—";
};

const opts = parseArgs(process.argv.slice(2));
const scan = JSON.parse(readFileSync(String(opts.scan), "utf8"));
const profileName = opts.profile ? String(opts.profile) : (scan.profiles.find(p => p.name.toLowerCase().includes("单机")) || scan.profiles[0]).name;
const profile = scan.profiles.find(p => p.name === profileName);
if (!profile) { console.error("[render] profile not found: " + profileName + " (have: " + scan.profiles.map(p => p.name).join(", ") + ")"); process.exit(2); }

const mode = String(opts.mode ?? "all");
const out = [];

if (mode === "summary" || mode === "all") {
  out.push("| 分组（MO2 分隔符） | 条目 | 启用 | 禁用 |");
  out.push("|---|---|---|---|");
  for (const g of profile.groups) {
    const enabled = g.mods.filter(m => m.enabled).length;
    const name = g.name.replace(/_separator$/, "").replace(/^《[^》]*》/, "").trim() || "(未分组)";
    out.push("| " + cell(name, 60) + " | " + g.mods.length + " | " + enabled + " | " + (g.mods.length - enabled) + " |");
  }
  out.push("");
  out.push("> 合计：" + profile.counts.mods + " 条 mod 条目（启用 " + profile.counts.enabled + " / 禁用 " + profile.counts.disabled + "）+ " + profile.counts.separators + " 个分隔符。");
  out.push("");
}

if (mode === "detail" || mode === "all") {
  for (const g of profile.groups) {
    const enabled = g.mods.filter(m => m.enabled).length;
    const name = g.name.replace(/_separator$/, "").replace(/^《[^》]*》/, "").trim() || "(未分组)";
    out.push("#### " + name + "（" + enabled + " / " + g.mods.length + " 启用）");
    out.push("");
    out.push("| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |");
    out.push("|---|---|---|---|---|");
    for (const m of g.mods) {
      const meta = m.meta ?? {};
      out.push("| " + cell(m.name, 70) + " | " + (m.enabled ? "✓" : "—") + " | " + cell(meta.version || "—", 22) + " | " + upstream(meta) + " | " + cell(meta.comments || meta.notes || "", 56) + " |");
    }
    out.push("");
  }
}

console.log(out.join("\n"));
