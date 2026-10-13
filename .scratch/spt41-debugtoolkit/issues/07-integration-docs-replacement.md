# 07: 集成、文档与源 mod 替换

**Spec:** `.scratch/spt41-debugtoolkit/spec.md`

**What to build:** 全量集成收口：全量构建 + 标准检查；README 定稿（项目定位、功能总览、F12 配置面、互斥警示——不可与 DebugTooltip / BotDebug / DadGamerMode 并存、BigBrain 1.5.0 硬依赖、上游偏差表汇总、安装/卸载/回滚说明）；THIRD-PARTY-NOTICES 定稿（三上游 MIT 全文 + 版本/版权行 + BigBrain 引用说明）；LICENSE；`scripts/package.ps1` 产出 zip 并校验内容；**MO2 部署到 Norvinsk_Forever（Default）**：覆盖层 `工具-SamMeow.DebugToolkit-4.1-0.1.0`（游戏根相对布局、meta.ini comments/notes）、**禁用** `[13]MOD工具-AI刷新和行为检修-BotDebug` 与 `[13]战局外检修标签-DebugTooltip`（目录保留，一键回滚）、`[6]AI拟人MOD的前置框架-BigBrain` 保持启用；modlist 校验与部署记录。写入前确保游戏与 MO2 关闭。

**Blocked by:** 03, 04, 05, 06

**Status:** ready-for-agent

- [x] 全量构建 0 error + 标准检查（4.1.6 / client）通过
- [x] README 定稿（含偏差表汇总、互斥警示、BigBrain 依赖、回滚路径）；THIRD-PARTY-NOTICES / LICENSE 定稿
- [x] zip 产出并校验内容（dll + README + LICENSE + THIRD-PARTY-NOTICES）
- [x] 覆盖层部署：modlist 单条唯一启用、meta.ini 就绪、DLL 仅存在于覆盖层（游戏目录无副本，VFS 佐证）
- [x] BotDebug / DebugTooltip 已禁用（目录保留）；BigBrain 保持启用；回滚路径写入 README

**上游源码（本仓归档）:** 三源目录（署名以各自 LICENSE/README 为准）；MCP 部署惯例：`<category>-<mod-name>-<version>`。

## Comments

- 2026-10-11（编排者）: 前置就绪——03/04/05/06 全部收口（含双轴审查闭环），`reviews/round1-*.md` 三件为文档事实来源；`package.ps1` 就绪可跑。**执行拆分**：文档（README/NOTICES/waiver）+ zip 由 fixer 通道施工；**MO2 部署（覆盖层安装 + 禁用两源 + modlist 校验）由编排者经 MO2 MCP 执行**（需游戏/MO2 关闭）。
- 2026-10-11（编排者）: **完成**（文档/打包 by fix-6 + MO2 部署 by 编排者经 MCP plan/apply）。
  - 文档：README 定稿（偏差表 21 行、行为注记 4 条）；waiver STD-CLI-003 理由重写；THIRD-PARTY-NOTICES 补齐三上游 MIT 全文 + BigBrain 依赖说明；LICENSE 核验未动。
  - 打包：`dist\SamMeow.DebugToolkit-0.1.0.zip`（4 条目：`BepInEx\plugins\SamMeow.DebugToolkit.dll` + README/LICENSE/THIRD-PARTY-NOTICES）；package.ps1 exit 0。
  - 部署（MO2 MCP 全链 plan/apply）：安装覆盖层 `工具-SamMeow.DebugToolkit-4.1-0.1.0`（plan `5a3aa772` → apply；snapshot `dbe4aa61`）；禁用 `[13]MOD工具-AI刷新和行为检修-BotDebug`（plan `3b53e2eb` → apply；snapshot `b22fd9fe`）；禁用 `[13]战局外检修标签-DebugTooltip`（plan `0297615c` → apply；snapshot `1337c7c3`）。
  - 读回校验：modlist = `+工具-SamMeow.DebugToolkit-4.1-0.1.0` / `-[13]BotDebug` / `-[13]DebugTooltip` / `+[6]BigBrain`；meta.ini comments/notes UTF-8 存证；游戏目录无 DLL 副本（VFS 佐证）；两源目录保留（一键回滚）。
  - 下一步：工单 08（Level B/C 验证；Overseer 启动游戏）。
- 2026-10-11（编排者）: **命名规范整改**（Overseer 指令：MO2 覆盖层一律沿用目标整合包既有命名结构，例 `[5]现实主义系列-姿势大修-stance-overhaul-test-build`；不再用 `工具-…-版本` 形）。
  - 覆盖层重命名：`工具-SamMeow.DebugToolkit-4.1-0.1.0` → **`[13]MOD工具-综合调试工具包-SamMeow.DebugToolkit`**（MCP rename plan `0782114b` → apply；snapshot `257bb0c4`；modlist 读回已更新）。
  - 同步整改命名指引落点：`skills/building-spt-modpack/SKILL.md`（2 处）与 `skills/porting-spt-mod-to-spt5/SKILL.md`（1 处）；`tools/mo2-mcp` 工具文案（`mo2-install` / `mo2-create-mod`）——`npm run build` + `npm test`（499 passed / 19 skipped）通过，dist 已重建（下次 MCP 重启生效）；本工程 `README.md`（3 处）与 `docs/level-c-checklist.md`（2 处）；zip 已按新 README 重打包（校验含新名）；覆盖层内 README 已刷新。
  - 旧名全库残留扫描（排除历史台账与构建目录）：零（仅 `docs/dev-log.md` 历史条目按「不改历史」保留）。
