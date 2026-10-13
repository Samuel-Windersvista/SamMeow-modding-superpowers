# 04: Bot 覆盖层模块移植（含 BigBrain 两模式）

**Spec:** `.scratch/spt41-debugtoolkit/spec.md`

**What to build:** 端到端编译级可用的 Bot 调试覆盖层（游戏内验证随 08）：战局内每个 bot 头顶绘制调试信息框；8 档监视模式（`Minimized`=0 隐藏 / `Behaviour` / `BattleState` / `Health` / `Specials` / `Custom` / `BigBrainLayer` / `BigBrainLogic`），F10/F11 循环切换；敌我颜色编码（白=普通、绿=玩家所属、红=把我当敌人）；SSAA 屏幕缩放补偿；死亡条目 3 秒移除、退出自动清理；`3. Bot 覆盖层` 配置节（`Enable`=false / `ActiveMode` 下拉 / `NextModeKey`=F10 / `PrevModeKey`=F11 / `MaxDrawDistance`=1500 / `FontSize`=24）；BigBrain 硬依赖 `xyz.drakia.bigbrain 1.5.0` + 编译引用（取 Norvinsk_Forever 覆盖层内 DLL，路径参数化、缺失响亮失败）；`STANDALONE` 条件符号保留。基线：BotDebug 1.8.0 源码（TarkovVersion 40743），触点适配照 02 核实表执行。

**Blocked by:** 02

**Status:** ready-for-agent

**Execution note:** 可与 03 / 05 并行（模块文件域隔离）；BigBrain 引用与依赖声明为 04 专属。

- [x] 8 档模式按上游语义；BigBrain 两模式编入（受 `STANDALONE` 符号保护，本线默认启用）
- [x] `[BepInDependency("xyz.drakia.bigbrain", "1.5.0")]` 声明；BigBrain 编译引用参数化可覆盖
- [x] 覆盖层默认关闭；热键/距离/字号可配；颜色编码与 SSAA 补偿按 02 结论
- [x] 战局生命周期：自动挂载、退出清理、死亡条目移除；`Destroy` 不摧毁 GameWorld 宿主
- [x] 补丁体 fail-open；构建 0 error；标准检查通过

**上游源码（本仓归档）:** `knowledge/spt-kb/archive/forge/mods/DrakiaXYZ-BotDebug_918_source/`（v1.8.0；MIT © DrakiaXYZ）。

## Comments

- 2026-10-11（编排者）: 前置就绪——工单 02 子通道 B 触点表已产出并经主控台抽验（`docs/api-touchpoints/bot-overlay.md`：50 触点全 [OK]；BaseLogicLayerSimple 归 EFT 全局命名空间、CameraManager 在 EFT.CameraControl 等断言独立复核命中）。csproj 已预置 `<BigBrainPath>` 属性 + DrakiaXYZ-BigBrain 编译引用；本单文件域 `src/Modules/BotOverlay/**` + `Plugin.cs` 仅一行 `[BepInDependency]`。施工（fix 通道，复用触点核实会话）。
- 2026-10-11（编排者）: **完成**（fix-3 施工 + 主控台复核）。
  - 交付：`src/Modules/BotOverlay/**` 6 文件（枚举/扩展/启动补丁/Settings/BotInfo/组件）；桩 → 真实启停；`Plugin.cs` +1 行 `[BepInDependency("xyz.drakia.bigbrain","1.5.0")]`；`SPT41DebugToolkit.csproj` +2 引用（TextRenderingModule/DissonanceVoip）。
  - 复核（编排者复跑）：`dotnet build -c Release` 0 warning / 0 error；标准检查 PASS=13 / FAIL=0 / WAIVED=1；产物符号含 `BrainManager/CustomLayer/BaseLogicLayerSimple/CustomLogic/GetBotDebugData`（BigBrain 编译路径确凿）。
  - 偏差/决策 7 条随单记录（ActiveMode 用 `ConfigEntry<string>`+`AcceptableValueList<string>`（BepInEx 5 的 `AcceptableValueList<T>` 泛型约束不接裸枚举）；`GetInfoText` 保持上游 3 参；struct 值语义；`ModulePatch.Disable()` 撤销；`#if !STANDALONE` 编入；供 07 汇总）。
  - **编排者裁定（记偏差表）**：Chest 行 Broken 标记由「忠实复刻上游 1.8.0（错取 Head）」改为取 `HealthBodyBroken`——与 5.0 线同一修正（用户故事「调试伤害结果」口径）。已改并复建绿（主控台）。
  - csproj 引用与 03 单重复追加，已由编排者去重（现各 1 条）。
  - 行为级确认（Level C）随 08；双轴代码审查在模块工单全数完成后统一进行（修订回填本单）。
- 2026-10-11（编排者）: **双轴审查闭环（Round 1 → 修订轮 → 复验）**。本单相关：`BotOverlayModule` 迁移共享 `PatchSet` + 配置订阅逐项退订；`BotData`→`BotOverlayEntry` 改名（命名歧义）；`BotDebugComponent` Awake-only 解析改惰性重试（Spec(c)2）；`BotOverlaySettings` 迁移 `ConfigDescriptions` 工厂。复验：构建 0 warning/0 error、标准检查 PASS=13/FAIL=0/WAIVED=1。记录：`reviews/round1-*.md` 三件。
