// gen-report.mjs —— 生成《永远的诺文斯克》安装对照表文档
import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const SCRATCH = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6";
const targets = JSON.parse(readFileSync(join(SCRATCH, "install-targets.json"), "utf8"));
const excluded = JSON.parse(readFileSync(join(SCRATCH, "excluded-entries.json"), "utf8"));
const finalRows = JSON.parse(readFileSync(join(SCRATCH, "final-report.json"), "utf8"));

const BATCH_DESC = {
  B1: "前置与工具", B2: "AI 全家", B3: "修复/价格/容器 UI", B4: "功能扩展",
  B5: "商人/内容/服装/腰包", B6: "武器/配件/视觉（含大件）", B7: "钉死版本实验批",
};

const lines = [];
lines.push("# 《永远的诺文斯克》v0.1.0 —— 安装对照表");
lines.push("");
lines.push(`- 生成时间: ${new Date().toISOString().slice(0, 16).replace("T", " ")}`);
lines.push("- 目标版本: SPT 4.1.6（服务端+客户端已实装）");
lines.push("- MO2 实例: SPT4.1.6《永远的诺文斯克》/ 数据目录 Norvinsk_Forever / profile Default");
lines.push("- 参照基准: Life_in_Norvinsk_v0.3.2（3.11.4 世代，197 条目）");
lines.push("- 回滚点: `.mo2-mcp/profile-backups/Default_pre-verify-0.1.0` + 每次安装的 snapshot");
lines.push("");
lines.push("## 一、已安装（69 项）");
lines.push("");
lines.push("| # | 批次 | 名称 | Forge 来源 | 版本 | SPT 约束 | 大小 |");
lines.push("|---|------|------|-----------|------|---------|------|");
let i = 0;
for (const t of targets) {
  i++;
  const mb = t.size ? (t.size / 1048576).toFixed(1) + " MB" : "-";
  const pin = t.pinned ? " ⚠️" : "";
  lines.push(`| ${i} | ${t.batch} ${BATCH_DESC[t.batch] ?? ""} | ${t.overlayName}${pin} | #${t.forgeId} ${t.forgeName} | ${t.version} | ${t.constraint} | ${mb} |`);
}
lines.push("");
lines.push("> ⚠️ = 钉死小版本（4.1.x 精确约束）：Radar / BossNotifier / SeparateHostility 为纯客户端；Eco-Attachment 为服务端。");
lines.push("> 若服务端启动出现全局拒载（日志 `modloader-no_mods_loaded`），优先回退 Eco-Attachment。");
lines.push("");
lines.push("## 二、未就绪（80 项，暂缓跟踪）");
lines.push("");
lines.push("等待上游发布 4.1.6 版本后增量补装。主要类别：");
lines.push("");
lines.push("- 上游止步 4.0.x: MoreCheckmarks、TaskAutomation、GamePanelHUD、LootRadius、BeltSlot、DynamicItemWeights、HomeComforts、LeaveItThere、barter_economy、AES、VolkovTrader、Epakim1、Echoes、BlackCore、MagTape、tarkovcraft、Battlepass、SecureMapbook、CornerStore、ItemContextMenuExt、ReachExtender、HeliCrash、StashSearch、True Items、UnlockHideoutCustomization、DadGamerMode 等");
lines.push("- 3.11 时代及更早: Realism、Softcore、TarkovIRL、ThatsLit、PTT、VCQL、KmyTarkovApi、CustomInteractions、RaidOverhaul 等");
lines.push("- 精确钉死不覆盖: Radar(客户端)、BossNotifier(客户端)、SeparateHostility(客户端)、Eco(服务端) → 已作实验批装入");
lines.push("- 非 Forge（Hub/社区独占）: ReflexSightsRework、BRNVG、Antigrav、Volcano 字体/字幕、SearchBarExtended、F12 汉化、本体缺失翻译、QBZ191、UnknownSurvivor、EFCL-CustomProfiles");
lines.push("");
lines.push("## 三、排除项（45 条）");
lines.push("");
const byReason = {};
for (const e of excluded) byReason[e.reason] = (byReason[e.reason] ?? 0) + 1;
for (const [r, c] of Object.entries(byReason)) lines.push(`- ${r}: ${c}`);
lines.push("");
lines.push("> 自定义内容（《生活在诺文斯克》配置/难度档案/已AI优化等）按既定策略「上游优先，自定义待定」。");
lines.push("");
writeFileSync(join(SCRATCH, "对照表.md"), lines.join("\n"), "utf8");
console.log("对照表.md written,", lines.length, "lines");
