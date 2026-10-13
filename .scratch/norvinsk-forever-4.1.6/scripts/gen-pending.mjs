// gen-pending.mjs —— 生成 docs/pending-mods-v0.1.0.md（未就绪 80 项跟踪清单）
import { readFileSync, writeFileSync } from "node:fs";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const rows = JSON.parse(readFileSync(SCRATCH + "\\final-report.json", "utf8"));
// 钉死实验批 4 项已安装（实验），从待跟踪中剔除；#104 Softcore 为误配修正后回列
const PINNED_INSTALLED = new Set([16, 48, 89, 120]);
// 2026-10-06 增量补装（IOF/BRNVG/HDR）后从清单剔除
const INSTALLED_SUBSTR = ["INVENTORY ORGANIZING", "BRNVG", "Headshot Damage Redirection"];
const un = rows.filter((r) => !r.selected && !PINNED_INSTALLED.has(r.idx) && !INSTALLED_SUBSTR.some((s) => r.entry.includes(s)));
un.push({ idx: 104, entry: "[5]经济与制造系统大修-Softcore - 已AI优化" });
un.sort((a, b) => a.idx - b.idx);

const lines = [];
lines.push("# 未就绪清单 v0.1.0 —— 待上游 4.1.6 更新后增量补装");
lines.push("");
lines.push(`共 ${un.length} 项（对照 Life_in_Norvinsk_v0.3.2 完整复刻目标中尚无 4.1.6 兼容版本的上游 mod）。`);
lines.push("");
lines.push("已知原因类别：上游止步 4.0.x / 3.11 时代停更 / 精确钉死不覆盖 / 非 Forge（Hub 独占）。");
lines.push("重扫方法：对插件仓库运行 Forge 检索（filter[spt_version]=4.1.6），对照本清单逐项确认。");
lines.push("");
lines.push("| # | 条目 |");
lines.push("|---|------|");
let i = 0;
for (const r of un) { i++; lines.push(`| ${i} | ${r.entry} |`); }
lines.push("");
writeFileSync("E:\\Game\\EFT_Offline\\Norvinsk_Forever\\docs\\pending-mods-v0.1.0.md", lines.join("\n"), "utf8");
console.log("pending written:", un.length);
