// append-duizhaobiao.mjs —— 对照表追加「增量补装」段
import { readFileSync, writeFileSync } from "node:fs";
const p = "E:\\Game\\EFT_Offline\\Norvinsk_Forever\\docs\\对照表-v0.1.0.md";
const add = [
  "", "---", "",
  "## 增量补装（2026-10-06）", "",
  "新增 3 项（复核发现上游已有 4.1.6 兼容版本，无需移植）：", "",
  "| # | 批次 | 名称 | Forge 来源 | 版本 | SPT 约束 |",
  "|---|------|------|-----------|------|---------|",
  "| 70 | B6-武器/配件/视觉 | [7]平衡的夜视装备视效-BRNVG | #954 Borkel's Realistic Night Vision Goggles | 3.0.4 | ~4.1 |",
  "| 71 | B3-修复/价格/容器 UI | [10]物品自动分类功能-IOF | #2960 IOF: Inventory Organizing Feature | 1.9.0 | ~4.1 |",
  "| 72 | B4-功能扩展 | [6]AI锁头重定向-HeadshotDamageRedirection | #2953 Headshot Damage Redirection | 1.8.0 | ~4.1 |",
  "",
  "> 合计：72 项。三项均通过安装读回校验（notes 转义格式 / url 含 slug / comments 齐备）。",
  "",
];
writeFileSync(p, readFileSync(p, "utf8") + add.join("\n"), "utf8");
console.log("对照表 addendum appended");
