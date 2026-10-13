# SPT41 DebugToolkit（SamMeow.DebugToolkit 4.1.6 版）Spec

> 目标版本线：SPT 4.1.6 / EFT 0.16.9.40743 / Mono / BepInEx 5 / netstandard2.1。
> 部署目标：MO2 实例 "Norvinsk_Forever"（SPT 4.1.6 / profile Default）。
> 工程落点：`mods/SPT41-DebugToolkit`（SamMeow-modding-superpowers 仓库内；沿用 `mods/Directory.Build.props` 的 SptRoot 约定）。
> 与 5.0 线关系：同身份版本线（见 GLOSSARY「Identity-Shared Version Lines」与 ADR-0010）——共享 AssemblyName / GUID，独立源码、独立演进。

Status: ready-for-agent

## Problem Statement

我在维护 SPT 4.1.6 的整合包（Norvinsk_Forever）并做 mod 开发与调试，需要三类能力：① 物品/任务/商人数据库 ID 的即视即取；② 战局内 bot 行为与状态的实时观察；③ 测试期间的免死与减负（无敌/部分无敌 + 便利组）。

现状碎片化且不全：

- DebugTooltip 5.0.0 与 BotDebug 1.8.0 是两个独立插件：热键冲突（F9 系双占用）、两份 F12 配置、两个日志点；BotDebug 硬依赖 BigBrain。
- DadGamerMode 上游停在 4.0.x 线路之前（Norvinsk_Forever 内已标记 NO-4.1.6），其无敌组与 QOL 组在 4.1.6 上整体缺席。
- 我已有的 SamMeow.DebugToolkit 是 **5.0 线** 产品（IL2CPP / BepInEx 6 / net6.0），无法在 4.1.6 的 Mono / BepInEx 5 运行时加载。

## Solution

交付单一 SPT 4.1.6 客户端 mod：**SamMeow.DebugToolkit**（assembly `SamMeow.DebugToolkit`，BepInPlugin GUID `com.sammeow.debugtoolkit`，版本 0.1.0），整合四组能力：

- **Tooltip 调试信息**（移植 DebugTooltip 5.0.0）：物品/空槽位/改装槽/护甲/任务/任务条件/商人 ID 注入 + 三档剪贴板复制 + F9 快速开关。
- **Bot 调试覆盖层**（移植 BotDebug 1.8.0）：8 档监视模式（含 `Minimized` 隐藏档与 BigBrainLayer / BigBrainLogic 两模式）+ 头顶覆盖层 + F10/F11 模式循环；硬依赖 BigBrain 1.5.0。
- **无敌 / 部分无敌组**（移植 DadGamerMode 1.9.3 健康组）：GodMode / Keep1Health / 忽略爆头 / 百分比伤害 / 无坠落伤害 + CODMode 自动治疗。
- **便利组 QOL**（移植 DadGamerMode 1.9.3 QOL 全组）：无限体力/能量/水分、瞬产、瞬建、换弹卸速、对敌伤害倍率、负重百分比。

统一 F12 配置（6 节、中文显示名、原生下拉/滑块）、统一热键（F9/F10/F11 可配）、版本门禁仅警告。以 MO2 覆盖层 `工具-SamMeow.DebugToolkit-4.1-0.1.0` 部署到 Norvinsk_Forever，替换该包内的 BotDebug / DebugTooltip 两个覆盖层（目录保留、一键回滚）。三源均 MIT，合规署名。

## User Stories

【Tooltip 调试信息】

1. 作为 mod 开发者，我想要悬停物品时在 tooltip 中看到 Item ID 与 Tpl ID，以便直接复制 ID 写配置与数据库补丁，不再靠外部工具翻找
2. 作为 mod 开发者，我想要物品 tooltip 显示 Owner 容器名/ID 与 Address（grid 坐标/grid ID 或 slot ID/父物品名与 ID），以便定位物品挂载结构
3. 作为 mod 开发者，我想要空槽位（装备槽/改装槽/改装界面槽位）显示槽位 ID 与父物品 ID，以便编写槽位相关补丁
4. 作为 mod 开发者，我想要护甲槽显示物品调试信息，以便核对护甲挂载
5. 作为 mod 开发者，我想要任务列表项与任务详情显示 Quest ID 与状态，以便编写任务补丁
6. 作为 mod 开发者，我想要任务目标显示 Condition ID，以便编写条件补丁
7. 作为 mod 开发者，我想要商人卡片显示 Trader ID，以便编写商人补丁
8. 作为 mod 开发者，我想要 ctrl-c 复制短 ID、ctrl-alt-c 复制备用 ID（Tpl/父 ID）、ctrl-shift-c 复制完整调试块，以便粘贴进代码与配置
9. 作为 mod 开发者，我想要一个开关隐藏「复制提示」文案，以便界面干净
10. 作为 mod 开发者，我想要 F9 免开 F12 即切换 tooltip 调试信息，以便测试中快速对照
11. 作为 mod 开发者，我想要 tooltip 调试块无延迟立即出现，以便快速扫视
12. 作为使用者，我想要全部 tooltip 补丁 fail-open（异常不穿透游戏调用链），以便调试工具永不以崩溃为代价

【Bot 调试覆盖层】

13. 作为 mod 开发者，我想要战局中每个 bot 头顶显示行为模式信息（Brain 名/策略、Layer、Node、EnterBy、PrevNode/ExitBy、距离），以便调试 AI 行为
14. 作为 mod 开发者，我想要战斗模式信息（弹药 Chamber/Mag/Total、Hits/Shoots、Reloading、CoverId、WeaponReady、Malfunction、GoalEnemy），以便调试战斗逻辑
15. 作为 mod 开发者，我想要健康模式信息（分部位 HP + 黑屏 (BL) / 骨折 (Broken) 标记），以便调试伤害结果
16. 作为 mod 开发者，我想要特殊模式信息（Profile ID 分块、WeapSpawn、AxeEnemy、Role、State、StateLc），以便调试 bot 生成与状态机
17. 作为 mod 开发者，我想要自定义数据模式（Custom Data 文本），以便查看 BigBrain 自定义层写入的调试数据
18. 作为 mod 开发者，我想要 BigBrainLayer / BigBrainLogic 两个监视模式（依赖 BigBrain 1.5.0），以便调试自定义 brain 层与逻辑
19. 作为 mod 开发者，我想要颜色编码（白=普通、绿=玩家所属、红=把我当敌人），以便一眼分辨敌对关系
20. 作为 mod 开发者，我想要 F10/F11 循环切换 8 档监视模式（含 Minimized 隐藏档）且键位可配，以便不打断游戏操作
21. 作为 mod 开发者，我想要覆盖层最大绘制距离与字号可配，以便按屏幕与场景调整
22. 作为使用者，我想要覆盖层默认关闭、按需开启；每局自动挂载、退出自动清理、死亡条目 3 秒内移除，以便长时间测试无泄漏
23. 作为使用者，我想要 DLSS/FSR 开启时覆盖层位置依然正确（屏幕缩放补偿），以便画质设置不影响调试

【无敌 / 部分无敌组】

24. 作为测试者，我想要 GodMode（除坠落外全伤害清零），以便验证其他功能时不被战斗打死
25. 作为测试者，我想要 Keep1Health 部分无敌，并可选择 All 或 头+胸 两种范围，以便保留最低限度的受伤反馈
26. 作为测试者，我想要部位摧毁（流血/骨折致死路径）与 Keep1Health/CODMode 一致地被抑制，以便部分无敌不会因部位摧毁而失效
27. 作为测试者，我想要忽略爆头伤害的独立开关，以便单独测试头部伤害流程
28. 作为测试者，我想要百分比伤害上限（全身、仅头部各自可配），以便把伤害压到可控比例而非完全清零
29. 作为测试者，我想要无坠落伤害开关（FallSafeHeight 拉满），以便地形测试不被摔死
30. 作为测试者，我想要 CODMode 受击后延迟自动治疗（含流血/骨折/疼痛清理），并可配治疗速率、等待时间、流血保留开关，以便长时测试自我维持
31. 作为测试者，我想要全部无敌类开关默认关闭且经 F12 实时生效，以便安全默认与即时调整

【便利组 QOL】

32. 作为测试者，我想要无限体力（身体体力 + 手部体力 + 氧气），以便长时间机动测试不被体力打断
33. 作为测试者，我想要无限能量与无限水分，以便长时间测试不需要进食饮水
34. 作为测试者，我想要瞬间生产（藏身处制造即刻完成），以便快速迭代配方与产出验证
35. 作为测试者，我想要瞬间建造（藏身处升级即刻完成），以便快速迭代藏身处建设验证
36. 作为测试者，我想要换弹/卸弹速度设置（开关 + 两个倍率值，关闭时恢复默认），以便调整武器操作节奏
37. 作为测试者，我想要对敌伤害倍率（1-20，默认 1），以便控制对敌交战时长
38. 作为测试者，我想要物品总重百分比（0-100，100 为正常；战局前设置），以便携带测试装备
39. 作为测试者，我想要 QOL 全部默认关闭（数值项取上游默认），以便安全默认

【配置与健壮性】

40. 作为使用者，我想要版本不匹配时只记录警告日志而不拒绝加载，以便调试工具在版本漂移时永不缺席
41. 作为使用者，我想要配置文件可读、键位可改；列表型配置用原生下拉、数值项用滑块（4.1.6 Mono 无 IL2CPP 限制），以便配置安全可靠
42. 作为使用者，我想要移植中无法定位或等价映射的补丁点逐项降级（日志警告 + README 偏差表），以便单个功能缺位不阻塞整体交付
43. 作为使用者，我想要单一日志源与统一前缀，加载/卸载顺序稳定、补丁全撤销、组件无残留，以便日志可读、反复进出无泄漏
44. 作为使用者，我想要 README 明确互斥警示（不可与三源 mod 并存）与 BigBrain 1.5.0 硬依赖，以便避免重复补丁与困惑

【部署与工程】

45. 作为使用者，我想要部署走 MO2 覆盖层（VFS 投影、可一键禁用回滚），替换 Norvinsk_Forever 内的 BotDebug/DebugTooltip，以便安全迁移
46. 作为开发者，我想要工程通过 `dotnet build -c Release` 与 modding standard 检查脚本（client / 4.1.6），以便质量门可重复
47. 作为开发者，我想要上游 MIT 版权与署名（LICENSE + THIRD-PARTY-NOTICES）随工程携带，以便合规分发

## Implementation Decisions

- **单一客户端插件**：无服务端组件；一个 BepInPlugin 承载全部补丁与组件；对三源 mod 为**替代关系**（不声明依赖）；对 BigBrain 声明**硬依赖** `xyz.drakia.bigbrain 1.5.0`（ADR-0011）。
- **模块划分**：
  - Plugin 入口：生命周期（加载/卸载）、补丁装配与撤销、版本门禁、日志源。
  - Tooltip 模块：调试信息模型（物品/空槽/商人/任务/任务条件）、静态传递、补丁集（物品视图、槽位视图、改装槽、任务、商人、快捷键）、剪贴板组件。
  - BotOverlay 模块：信息组装（8 模式）、监视组件（数据轮询 + 头顶投影 + 死亡清理）、模式枚举与循环、BigBrain 两模式。
  - Invincibility 模块：伤害补丁（ApplyDamage / DestroyBodyPart 等价物）、健康组件（Keep1Health 语义、无坠落伤害）、CODMode 组件。
  - QoL 模块：体力/能量/水分组件、瞬产/瞬建补丁、换弹卸速组件、负重补丁；对敌伤害倍率并入伤害补丁。
  - Settings：统一 F12 配置面 + ConfigurationManager 特性 + 中文显示名。
- **移植基线**（ADR-0010）：以 4.x 上游源件为行为基准——BotDebug 1.8.0（源码对应 EFT 40743）、DebugTooltip 5.0.0（源码锚 SPT\4.1）、DadGamerMode 1.9.3（3.8 世代混淆名，按 0.16.9 反混淆 API 逐点复核）；整合设计（配置面/生命周期/模块划分/README 结构/fail-open 原则）自 5.0 版平移。
- **运行时事实**：4.1.6 = Mono / BepInEx 5 / netstandard2.1；Managed 已反混淆（真名真命名空间）；无 IL2CPP 约束（可用托管 LINQ、原生事件订阅、virtual 属性直调；无 ComboBox 崩溃）。
- **配置面（F12，6 节；键名英文、显示名中文）**：
  - `1. 通用`：`Plugin Version`（只读）。
  - `2. 提示信息`：`Show Debug Tooltip`(true) / `Show Copy Instructions`(true) / `Toggle Shortcut`(F9)。
  - `3. Bot 覆盖层`：`Enable`(false) / `ActiveMode`（下拉：Minimized/Behaviour/BattleState/Health/Specials/Custom/BigBrainLayer/BigBrainLogic，默认 Behaviour）/ `NextModeKey`(F10) / `PrevModeKey`(F11) / `MaxDrawDistance`(1500; 0-2000) / `FontSize`(24; 12-36)。
  - `4. 无敌`：`God Mode` / `Keep 1 Health` / `Keep 1 Health Selection`（下拉：All/Head And Thorax）/ `Ignore Headshot Damage` / `Percentage Headshot Damage` / `% Damage Received (Only Head)`(100) / `% Damage Received (All Body)`(100) / `No Falling Damage`。
  - `5. 恢复`：`CODMode` / `CODMode Heal Rate`(10) / `CODMode Heal Wait`(10) / `Bleeding Damage`。
  - `6. 便利`：`Infinite Stamina` / `Infinite Energy` / `Infinite Hydration` / `Instant Production` / `Instant Construction` / `Enable Reload/Unload Mag Speed` / `Reload Mag Speed`(0.85) / `Unload Mag Speed`(0.3) / `Enemy Damage Multiplier`(1; 1-20) / `Item Total Weight %`(100; 0-100)。
  - 列表型配置用**原生下拉**（AcceptableValueList），数值项保留范围滑块；`1.–5.` 节名与键名直接采用 5.0 线现行中文形态；**无配置迁移器**（新线无历史配置）。
- **热键默认**：ToggleShortcut=F9；NextBotMode=F10；PrevBotMode=F11；全部可配置。
- **默认值**：tooltip 开；覆盖层关；无敌组全关；CODMode 关；QOL 全关（数值项取上游默认）。
- **GodMode 语义裁定**：除坠落伤害外全伤害清零（与 5.0 线一致；上游代码为无条件全清零、与其描述 "Makes You Invincible Except for Fall Damage" 矛盾——保留与 5.0 线相同的有意偏离并在偏差表记录）。
- **版本门禁**：读 `EscapeFromTarkov.exe` 的 `FileVersionInfo.FilePrivatePart`，比对 40743；不匹配仅 LogWarning；门禁自身出错 fail-open，绝不抛异常。
- **补丁与健壮性**：`SPT.Reflection.Patching.ModulePatch`；零 `[HarmonyPatch]` 特性；补丁体 try/catch fail-open；**补丁点无法定位/等价映射 → 逐项降级**（该功能禁用 + 日志警告 + README 偏差表），不阻塞其余功能与交付。
- **构建**：netstandard2.1；引用 BepInEx\core、Managed（Assembly-CSharp / Comfort / UnityEngine 模块）、BepInEx\plugins\spt（spt-common / spt-reflection），以及 BigBrain 程序集（默认取 `$(SptRoot)\Norvinsk_Forever\mods\[6]AI拟人MOD的前置框架-BigBrain\BepInEx\plugins\DrakiaXYZ-BigBrain.dll`，参数化可覆盖，路径缺失响亮失败）；扁平输出；`CopyToRelease` 到 `release/`（`-p:SkipCopyToRelease=true` 可关）。
- **身份与版本**：AssemblyName `SamMeow.DebugToolkit`、GUID `com.sammeow.debugtoolkit`、版本 0.1.0（与 5.0 线同身份）；版本号三处同步（csproj / `Plugin.cs` / README 与覆盖层名）。
- **部署**：MO2 覆盖层 `工具-SamMeow.DebugToolkit-4.1-0.1.0`（游戏根相对布局：`BepInEx\plugins\SamMeow.DebugToolkit.dll` + README + LICENSE + THIRD-PARTY-NOTICES）；meta.ini 写 comments + notes；modlist 单条；**禁用** `[13]MOD工具-AI刷新和行为检修-BotDebug` 与 `[13]战局外检修标签-DebugTooltip`（目录保留可回滚）；`[6]AI拟人MOD的前置框架-BigBrain` 保持启用；写入前确保游戏与 MO2 关闭。
- **打包**：`scripts/package.ps1` 产出 zip（dll + README + LICENSE + THIRD-PARTY-NOTICES），版本从 csproj 读取。
- **产出坐标**：工程 `mods/SPT41-DebugToolkit`；spec/工单 `.scratch/spt41-debugtoolkit/`；**实现期不产生 git 提交**（提交由 Overseer 执行）。

## Testing Decisions

- 好测试的标准：只测外部行为——构建可重复、加载日志可断言、游戏内可观察；无单元测试夹具可用，按既有三级验证阶梯执行。
- 接缝（沿用 5.0 线阶梯，不新开接缝）：
  - **S1 构建接缝（agent 执行）**：`dotnet build -c Release` 0 error；`scripts/check-mod-standard.ps1 -ModPath <mod> -TargetSptVersion 4.1.6 -Kind client` 通过（含豁免机制）。
  - **S2 加载接缝（Level B；Overseer 启动、agent 断言）**：经 MO2 启动至主菜单；agent 读 `BepInEx\LogOutput.log` 断言插件加载行、各模块装配行、无 Harmony 补丁错误。
  - **S3 功能接缝（Level C；Overseer 执行、agent 提供清单）**：tooltip 各面与三档复制；覆盖层 8 档模式与 Minimized；无敌组逐开关；QOL 逐开关（瞬产/瞬建、换弹速、负重需战局前设置的语义）。
  - **S4 辅助接缝（可选）**：tarkov-runtime bridge 运行时读数作旁证（当前不依赖、不阻塞）。
- 受测对象：整插件（集成级验证）。
- Prior art：SamMeow-DebugToolkit 5.0（Level B/C 流程与清单）、PerformanceTweaks413（构建接缝）、标准 STD-VERIFY-002/004/009。

## Out of Scope

- 服务端组件 / 服务端数据修改。
- Fika 多人兼容。
- Forge 发布（保持可发布形态，不执行发布动作）。
- 翻译/本地化工作流。
- 对 5.0 线仓库（SamMeow-DebugToolkit）的任何修改；跨线代码共享。
- BigBrain 本体修改；无 BigBrain 的 STANDALONE 构建（条件符号保留，不产出变体）。
- 自动化战局 UI 验证（由 Overseer 人工执行清单）。
- DadGamerMode 之外的新 QOL 扩展。

## Further Notes

- **上游 provenance（本地归档，均 MIT）**：
  - BotDebug 1.8.0 — `knowledge/spt-kb/archive/forge/mods/DrakiaXYZ-BotDebug_918_source`（© DrakiaXYZ；`TarkovVersion(40743)`）。
  - DebugTooltip 5.0.0 — `knowledge/spt-kb/archive/forge/mods/DebugTooltip_1542_source`（© Tyfon；引用 `ItemComponent.Types.dll`）。
  - DadGamerMode 1.9.3 — `knowledge/spt-kb/archive/forge/mods/DadGamerMode_686_source`（上游 LICENSE 占位，按 Props/dvize 署名）。
  - BigBrain 1.5.0 — 本地仅 obj 残件、无源码；运行期以 Norvinsk_Forever 覆盖层内 DLL 为依赖目标与编译引用。
- **环境快照**：SPT_41x = SPT-4.1.6-40743（EFT 0.16.9.40743）；Managed 已反混淆；BepInEx 5 + ConfigurationManager（spt 目录附带）；MO2 实例 Norvinsk_Forever（Default）已装 BotDebug 1.8.0 / DebugTooltip 5.0.0 / BigBrain 1.5.0。
- **已知风险面（工单中逐点复核）**：
  - DadGamerMode：`ApplyDamage` / `DestroyBodyPart` 签名、`FallSafeHeight` 后备字段、效果清理接口（EffectAddedEvent / RemoveEffectFromList / FindActiveEffect）、瞬产/瞬建/负重（旧混淆名需映射 0.16.9 真名）。
  - DebugTooltip：`UIElement."UI"` / `UIInputNode."UI"` / `UIContext."AddDisposable"` / `GUIUtility."systemCopyBuffer"` 反射点；私有字段注入参数 `____slot` / `____trader`；`QuestListItem.Init` 等 UI 类型在 0.16.9 的形状。
  - BotDebug：`BotSpawner.GetBotDebugData` / `DebugBotStruct` 字段、`CameraManager.SSAA`、BigBrain `BrainManager` / `CustomLayer` / `CustomLogic` API。
- **偏差记录制度**：README「上游偏差表」为唯一落点；已知必录至少两条：GodMode 坠落语义（同 5.0 线）与逐项降级清单（以实际复核结果为准）。
- **整合包影响**：部署后 Norvinsk_Forever 的 `[13]` 调试类由本插件替代；DadGamerMode 的 NO-4.1.6 缺口由本插件健康组 + QOL 组等价填补（该包台账的 DadGamerMode 项可否据此关闭，由 Overseer 裁定）。
