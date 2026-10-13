# 03: Tooltip 模块移植（DebugTooltip 全功能）

**Spec:** `.scratch/spt41-debugtoolkit/spec.md`

**What to build:** 端到端编译级可用的 Tooltip 调试信息模块（游戏内验证随 08）：悬停物品（库存/交易）、空装备槽、改装槽、护甲槽、任务列表项、任务详情、任务目标、商人卡片时，tooltip 出现对应调试 ID 块（Item ID/Tpl/Owner/Address、Slot/Parent、Quest ID+状态、Condition ID、Trader ID，格式与着色与上游一致）；三档剪贴板复制（ctrl-c / ctrl-alt-c / ctrl-shift-c）；F9 快速开关；`2. 提示信息` 配置节（`Show Debug Tooltip`=true / `Show Copy Instructions`=true / `Toggle Shortcut`=F9）；全部补丁体 fail-open。基线：DebugTooltip 5.0.0 源码（SPT\4.1 锚定），触点适配照 02 核实表执行。

**Blocked by:** 02

**Status:** ready-for-agent

**Execution note:** 可与 04 / 05 并行（模块文件域隔离；共享骨架改动最小化，由 07 收口复核）。

- [x] 8 类界面注入按上游语义与 02 表实现（不可达点按降级预案记录并禁用）
- [x] 三档复制可用（剪贴板取用方式按 02 结论）
- [x] F9 切换与配置项实时生效；调试块无延迟显示；复制提示开关可用
- [x] 全部补丁体 try/catch fail-open；构建 0 error；标准检查通过
- [x] 偏差条目随单记录（07 汇总入 README 偏差表）

**上游源码（本仓归档）:** `knowledge/spt-kb/archive/forge/mods/DebugTooltip_1542_source/`（v5.0.0；MIT © 2024 Tyfon）。

## Comments

- 2026-10-11（编排者）: 前置就绪——工单 02 子通道 A 触点表已产出并经主控台抽验（`docs/api-touchpoints/tooltip.md`：62 触点，[OK]56 / [CHANGED]5 / [UNCERTAIN]1 / [MISSING]0；QuestListItem 2 参未改名、UIContext→UIParent 等关键结论独立复核命中）。csproj 公共引用面已由编排者预置（UGUI/IMGUI/InputLegacy/UIModule/TextMeshPro/Sirenix/Postprocessing/ItemComponent.Types/Newtonsoft）；本单文件域 `src/Modules/Tooltip/**`。施工（fix 通道，复用触点核实会话）。
- 2026-10-11（编排者）: **完成**（fix-2 施工 + 主控台复核）。
  - 交付：`src/Modules/Tooltip/**` 11 文件（入口 + Settings + 3 数据/组件 + 6 补丁文件；共 14 个 `ModulePatch`）；桩 → 真实启停（装配/撤销、配置变更订阅/退订、克隆文本清理、幂等）。
  - 复核（编排者复跑）：`dotnet build -c Release` 0 warning / 0 error；标准检查 PASS=13 / FAIL=0 / WAIVED=1；模块域零 IL2CPP 形态。
  - 关键适配（照 02 表）：不移植 `R.cs`（`AddDisposable` 直调）；`_slot` 用 `FieldRefAccess` 实时读+空判；`QuestListItem` 2 参；`ReferenceEquals` 取代 `.Pointer`；ctrl 笔误修正为 `LeftControl || RightControl`；`ItemDebugInfo` 空安全。
  - 偏差 9 条随单记录（静态类命名 `DebugTooltipState`、csproj 引用追加等；供 07 汇总）。
  - 注：csproj 引用与 04 单重复追加，已由编排者去重（现各 1 条）。
  - 行为级确认（Level C）随 08；双轴代码审查在模块工单全数完成后统一进行（修订回填本单）。
- 2026-10-11（编排者）: **双轴审查闭环（Round 1 → 修订轮 → 复验）**。本单相关：`TooltipModule` 迁移共享 `PatchSet`（由自实现列表式统一）；`TooltipCopier` 停用残留修复（`RemoveAll()`，Spec(c)3）；死代码 `SetDebugInfo(string)`+`BasicDebugInfo` 删除；`TooltipSettings` 迁移 `ConfigDescriptions` 工厂。复验：构建 0 warning/0 error、标准检查 PASS=13/FAIL=0/WAIVED=1。记录：`reviews/round1-standards.md`、`round1-spec.md`、`round1-revision-result.md`。
