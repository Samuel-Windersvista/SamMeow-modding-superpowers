// append-addendum.mjs —— 复制评估报告到整合包 docs 并追加复核增录
import { readFileSync, writeFileSync, copyFileSync } from "node:fs";

const SRC = "E:\\云文件\\GitHub\\SamMeow-modding-superpowers\\.scratch\\norvinsk-forever-4.1.6\\porting-assessment.md";
const DST = "E:\\Game\\EFT_Offline\\Norvinsk_Forever\\docs\\porting-assessment.md";

copyFileSync(SRC, DST);
const addendum = [
  "",
  "---",
  "",
  "## 6. 复核结果（2026-10-06 补录）",
  "",
  "对第 4 节「待复核事项」1/2 的 Forge 实查结论：",
  "",
  "- **IOF #2960**：Forge 有且仅有 1 个 4.1.6 兼容版本 —— 1.9.0（`~4.1`，2026-08-26）。**可直接安装，无需移植。**",
  "- **BRNVG #954**：Forge 有 10 个 4.1.6 兼容版本（最新 ≥ 2.2.3，`~4.1`）。**可直接安装，无需移植。**",
  "- **Headshot Damage Redirection #2953**：1 个兼容版本 —— 1.8.0（`~4.1`，2026-08-23）。**可直接安装，无需移植。**",
  "",
  "> 结论修正：三项由「适合移植/待复核」转为「待增量补装」。此前漏查原因：BRNVG 的 Forge 条目名为全称（不含缩写），IOF 与 HDR 为社区重发的新条目（新 mod id）。",
  "",
];
writeFileSync(DST, readFileSync(DST, "utf8") + addendum.join("\n"), "utf8");
console.log("docs/porting-assessment.md written with addendum");
