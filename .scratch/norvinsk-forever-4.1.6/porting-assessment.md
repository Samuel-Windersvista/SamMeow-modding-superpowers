# 《永远的诺文斯克》4.1.6 移植可行性评估

- 评估对象：未就绪清单 76 项中「本地已有源码」的候选（A 类自有仓库 13 组 / 16 仓库；B 类 Forge 归档快照 20 组 / 22 快照）
- 评估目标：SPT 4.1.6（服务端 net10.0；客户端 BepInEx 5 / net472 / netstandard2.1）
- 评估方法：对照 `knowledge/spt-kb/curated/migration/api-mapping-311-to-41.md`（3.11 TS → 4.1 C# 映射、4 大阻断器、难度占比低约 50% / 中约 35% / 高约 15%）逐候选核对；先例参考 `E:\云文件\GitHub\Moew-SAIN-For-4013`（3.x → 4.0.13 客户端+服务端移植样本）
- 评估性质：静态源码评估，未编译、未实机验证；只读，未改动任何被评估仓库
- 生成时间：2026-10-06

---

## 0. 关键结论摘要

1. **B 类「源码快照」名不符实的比例很高**：22 个快照中只有 5 个含真实源码（DadGamerMode_686、IOF-2960、Headshot-Damage-Redirection_2953、Item-Context-Menu-Extended_940、BRNVG_954），其余 17 个只剩 `bin/obj` 构建残件（无 .cs 源、无 DLL），**无法作为移植基础**。
2. **两个「疑似待移植」实际可能已就绪**：IOF 2960 源码 README 明示 "Ported to SPT 4.1.x by flir"；BRNVG 源码服务端直接引用 `SPTarkov.Server.Core 4.1.0`、元数据 `Range("~4.1.0")`。两者应先与 Forge/Hub 实况复核，再决定是否真的需要自建移植。
3. **A 类中约一半为「服务端 TS 小、客户端 C# 中」结构**（IDC：服务端仅 555 LOC；HomeComforts 465 LOC；LeaveItThere 496 LOC），此前清点脚本报出的「221/219/218 个 TS 文件」包含了 `types/*.d.ts` 与 node_modules，实际迁移量比表面小一个量级。
4. **重写型（高工作量）集中在 4 项**：Realism（客户端 28.5K + 服务端 33K TS）、PTT+IEAPI（5.1K 客户端 + 7.8K TS）、RaidOverhaul（5.2K + 10.8K TS）、Softcore 直译（5.3K TS）。
5. **Softcore-NG 是本批次最高性价比的战略资产**：`SamMeow-Inescapable-Tarkovs-Softcore` 是已完成 TS→C# 重写的 **SPT 5.0** 服务端整合 mod（52 文件 / 5260 LOC），同时覆盖 Softcore、True Items、noFIR、反重力臂章、更大背包 5 个未就绪条目；**重定向到 4.1.6 的可行性需要实测**（改 `SptVersion`、引用 4.1.6 运行时程序集、修 5.0→4.1 API 漂移），若可行则一次清 5 项。
6. **战略前提**：SPT 5.0 已发布为前进线，且用户已在 5.0 线重写（Inescapable 系列）。数千行级的重写型移植（Realism/PTT/RaidOverhaul）在排期前应先决定目标线，避免 4.1.6 资产短期废弃；低成本客户端/数据类移植可先行。

---

## 1. 总表

### A 类：自有 GitHub 仓库（E:\云文件\GitHub）

| # | 候选（pack 定位） | 仓库 | 技术栈 | 体量（真实源码） | 工作量 | 结论 | 一句话理由 |
|---|------|------|--------|------|--------|------|------------|
| A1 | 好莱坞第三人称镜头 [13]（#3） | SamMeow-HollywoodCam（无 git） | 客户端 C#，net472 | 22 文件 / 2129 LOC | 中 | 适合移植 | 纯客户端相机补丁，重编译 + 相机/玩家 API 漂移核对 |
| A2 | 白方块修复 [12]（#6） | WhiteBoxFix（无 git） | 客户端 C#，net471 | 4 文件 / 208 LOC | 低 | 适合移植 | 单 Harmony 补丁；先核实 4.1.6 是否仍需该修复 |
| A3 | 沉浸感时间循环 [8]（#23） | SamMeow-ImmersiveDaylightCycle | 服务端 TS + 客户端 C# | TS 5 文件/555 LOC；C# 12/710 LOC | 中 | 适合移植 | 服务端仅 5 条静态路由，TS→C# 边界清晰 |
| A4 | 更大的背包 [7]（#37） | JoshMate-BetterBackpacks（无 git） | 服务端 TS | 1 文件 / 356 LOC | 低 | 适合移植 | 纯 postDBLoad 数据修改，C# 改写直接 |
| A5 | AI 视线优化 ThatsLit [6]（#47） | SamMeow-ThatsLit-For-3114 | 客户端 C#，net472 | 33 文件 / 8353 LOC | 中-高 | 适合移植（有条件） | 反射型可见性重做，字段漂移需运行期探测 |
| A6 | 舒适家园系统 [5]（#48） | SamMeow-HomeComforts-for3114 | 服务端 TS + 客户端 C# | TS 3 文件/465 LOC；C# 19/1474 LOC | 中 | 适合移植 | 服务端小，客户端物品/UI 补丁中等 |
| A7 | 战局内存储 [5]（#49） | SamMeow-LeaveItThere-For3114 | 服务端 TS + 客户端 C# | TS 2 文件/496 LOC；C# 38/4384 LOC | 中-高 | 适合移植 | 客户端交互系统较大，Fika 部分可裁剪 |
| A8 | 以物易物经济 [5]（#50） | SamMeow-barter_economy（无 git） | 服务端 TS | 4 文件 / 1776 LOC | 中-高 | 适合移植 | 算法型经济 mod，全量 C# 重写但边界明确 |
| A9 | 武器操控大修 [5]（#51） | SamMeow-TarkovIRL_WeaponsHandlingMod（无 git） | 客户端 C#，netstandard2.1 | 41 文件 / 4816 LOC | 中 | 适合移植（被依赖阻断） | 硬依赖 RealismMod.dll 公开 API，须待 Realism 就绪 |
| A10 | 现实主义大修 [5]（#52） | LIN-Realism-Mod-Client + LIN-Realism-Server | 客户端 C# + 服务端 TS | C# 57/28553 LOC；TS 37/33147 LOC | 高 | 重写型 / 不建议 4.1.6 排期 | 双端巨型 overhaul，服务端含核心类扩展（阻断器 4） |
| A11 | 经济与制造大修 [5]（#53） | SamMeow--Softcore；SamMeow-Inescapable-Tarkovs-Softcore | 服务端 TS（3.11）；服务端 C#（**SPT 5.0**） | TS 29/5280 LOC；C# 52/5260 LOC（src） | 中（重定向）/ 高（重写） | 适合移植（推荐 NG 重定向） | NG 已覆盖 5 个未就绪条目；重定向 4.1.6 需实测 API 漂移 |
| A12 | 全地图移动 IEAPI+PTT [5]（#54） | SamMeow-PathToTarkov + SamMeow-InteractableExfilsAPI | 服务端 TS + 客户端 C# | PTT：C# 53/5106 + TS 63/7771；IEAPI：11/1884 | 高（PTT）；中（IEAPI） | 重写型（先做 IEAPI） | PTT 服务端体量大 + Fika 引用多（275 处），需专项排期 |
| A13 | 战局大修 RaidOverhaul [5]（#55） | SamMeow-Raid-Overhaul-For-3114 | 服务端 TS + 客户端 C# | C# 41/5178 + TS 24/10778（含枚举 4058） | 高 | 重写型 | 内容型大修，服务端多控制器迁移 + 客户端枚举预补丁重做 |

### B 类：Forge 归档源码快照（`knowledge/spt-kb/archive/forge/mods/`）

| # | 候选（pack 定位） | 快照 | 真实源码 | 版本/年代证据 | 工作量 | 结论 | 一句话理由 |
|---|------|------|----------|------------|--------|------|------------|
| B1 | 尸体可删除 [12]（#5） | SPTCorpseCleaner_2058_source | 无（仅 obj 残件） | v1.0.5，netstandard2.1 | - | 不建议 | 快照无源码、无 DLL，不可移植 |
| B2 | 我就是爹作弊工具 [11]（#7） | DadGamerMode_686_source | 有（16 .cs + Features/Patches + references） | net471，Plugin 1.9.3 | 低-中 | 适合移植 | 完整客户端源码，重编译 + 6 补丁点核对 |
| B3 | 好莱坞投掷视觉辅助 [11]（#9） | Janky-s-Visual-Assist_2213_source | 无（仅 LibSPT/obj 残件） | - | - | 不建议 | 快照无源码 |
| B4 | 更多界面信息 [11]（#10） | Game-Panel-HUD_456_source | 无（9 个子工程仅 obj 残件） | net472 | - | 不建议 | 快照无源码；若取上游源码属大工程 |
| B5 | 物品自动分类 [10]（#12） | InventoryOrganizingFeatures_856_source；IOF-...2960_source | 856 无；**2960 有（24 .cs + tests + CI）** | net472；CHANGELOG 1.9.0（2026-08-27）；README「Ported to SPT 4.1.x by flir」 | 低 | 适合移植 | 已有 4.1.x 移植源码，重编译到 4.1.6 即可；先复核 pending 判定 |
| B6 | 可拾取穿墙物品 [8]（#24） | Reach-Extender_1260_source | 无 | - | - | 不建议 | 快照无源码 |
| B7 | 坠落的补给直升机 [8]（#26） | HeliCrash_1292_source | 无 | Core 2.5.2 + Fika 1.0.4，netstandard2.1 | - | 不建议 | 快照无源码 |
| B8 | 有意义的武器精通 [8]（#31） | MeaningfulWeaponMasteries_1989_source | 无 | v1.0.0 | - | 不建议 | 快照无源码 |
| B9 | 超重也能练耐力 [8]（#32） | AlwaysLevelEndurance_697_source | 无 | v1.0.0，netstandard2.0 | - | 不建议 | 快照无源码 |
| B10 | 门可以锁 [8]（#33） | LockableDoors_1923_source | 无 | Core 2.0.0 + Server 0.0.1（**net9.0**） | - | 不建议 | 快照无源码；构件显示曾有 4.x 服务端组件 |
| B11 | 武器动画更新器 [7]（#45） | AnimatorUpdater_2006_source | 无 | v1.0.0，net472 | - | 不建议 | 快照无源码 |
| B12 | AI 锁头重定向 [6]（#46） | HeadshotDamageRedirect_2115_source；Headshot-Damage-Redirection_2953_source | 2115 无；**2953 有（3 .cs）** | 2953：v1.8.0，net472，单补丁 | 低 | 适合移植 | 单 Harmony 补丁 ApplyDamageInfo，重编译即可 |
| B13 | 武器破门 [8]（#28/#29） | BackdoorBandit_858_source | 无 | - | - | 不建议 | 快照无源码（客户端+服务端均缺） |
| B14 | 动态物品重量 [9]（#21） | DynamicItemWeights_2344_source | 无 | net472 | - | 不建议 | 快照无源码 |
| B15 | 项目上下文菜单扩展 [9]（#16，含 #75 CustomInteractions） | Item-Context-Menu-Extended_940_source | 有（28 .cs，多工程解决方案） | net472；ItemContextMenuExt 1.8.0 / CustomInteractions 1.8.0 | 中 | 适合移植 | 一次覆盖两个未就绪条目的完整源码 |
| B16 | 自定义任务加载器 VCQL [1]（#73/#74） | CustomQuestLoader_649_source | 无（仅 Client/VCQLQuestZones obj） | net472 | - | 不建议 | 快照无源码 |
| B17 | KmyTarkovApi [1]（#76） | KmyTarkovApi_898_source | 无（5 工程仅 obj 残件） | net472 | - | 不建议 | 快照无源码 |
| B18 | 禁用 Scav 模式 [8]（#27） | DisableScavMode_977_source | 无 | v1.1.0，netstandard2.1 | - | 不建议 | 快照无源码 |
| B19 | 平衡夜视装备 BRNVG [7]（#40/#41） | Borkel's-Realistic-Night-Vision-Goggles-(NVGs-and-T-7)_954_source | 有（48 .cs：客户端约 46 + 服务端 2） | 客户端 v3.0.3 netstandard2.1；**服务端 net10.0 + SPTarkov.Server.Core 4.1.0 包，Metadata SptVersion ~4.1.0** | 低 | 适合移植 | 源码已是 4.1 世代，重编译/重新打包即可；先复核 Hub 现状 |
| B20 | 存档编辑器 [13]（#4） | ProfileEditor_106_source | 无（57 个 .cs 全部在 obj，为生成代码） | 工程版本 4.1.0，TF net10.0-windows10.0.17763.0（来自 project.assets） | - | 不建议 | 快照无源码；工具类，建议直接核对上游发行版 |

> B 类说明：`_856`、`_858`、`_977`、`_1260`、`_1292`、`_1989`、`_2058`、`_2115`、`_2213`、`_2344`、`_649`、`_898`、`_1923`、`_697`、`_2006` 等快照经逐文件清点，不含任何真实 `.cs/.ts` 源文件与 DLL，只有 `obj` 下的 `AssemblyInfo/AssemblyAttributes` 与缓存（快照生成于 2026-08 至 09）。这些条目若仍要移植，需先从上游 GitHub/Hub 取源，不属于「本地已有源码」范围。

---

## 2. 分档详述

### 2.1 适合移植（低 / 中工作量）

#### A1 HollywoodCam（[13] 第三人称镜头）
- 事实：`SamMeow-HollywoodCam\LibSPT`，22 .cs / 2129 LOC，老式 csproj（net472，`SPT3114Path` 引用 3.11 客户端集）。核心：`Camera.cs`（472）、`Plugin.cs`（331）、`Patches\Camera.cs`（117）、`Patches\Player.cs`（77）、`CollisionSolver`（相机穿墙碰撞）。
- 工作内容：
  1. 建立 4.1.6 客户端引用集（Managed + `BepInEx\plugins\spt`）后重编译；
  2. 核对 `Patches\Camera/Player` 的补丁目标在 EFT 0.16 的签名（相机姿态、镜头切换、后坐力挂钩）；
  3. 核对 `AnimationSystem.Types` 相关调用（武器动画状态）；
  4. F12 配置项无服务端依赖，不需要服务端组件。
- 风险：0.16 相机/惯性改动可能触及核心补丁点；与已装 FOV Fix、BetterRearSights 等的兼容需要实机确认。
- 结论：适合移植 · 中（1-3 人天）。

#### A2 WhiteBoxFix（[12] 白方块修复）
- 事实：`MarsyApp-WhiteBoxFix`，4 .cs / 208 LOC；唯一补丁 `ItemFilter.CheckItem` 前缀，过滤 `acceptableNodes` 中的 null/空串。
- 工作内容：重编译到 4.1.6 客户端引用；**先验证 4.1.6 是否仍存在该缺陷**（ItemFilter 逻辑若已由 SPT/BSG 修复，则不需要移植）。
- 风险：低；唯一风险是重复修复。
- 结论：适合移植 · 低（数小时，含验证）。

#### A3 ImmersiveDaylightCycle（[8] 沉浸感时间循环）
- 事实：服务端 TS 5 文件 / 555 LOC（`mod.ts` 注册 5 条静态路由：console_command、host_raid_started、client_exited、request_time、profile/login 清理）；客户端 C# 12 文件 / 710 LOC（含 UI 补丁与 Fika 桥）。
- 工作内容：
  1. 服务端按映射表重写为 C#：`AbstractRouter` 子类 + 5 条路由；`TimeManager` 时间推进；`RegistrationManager` 会话注册（对应 4.1 的 `SaveProfileService` / `ProfileHelper` 注入）；
  2. 客户端重编译 + 路由 URL 协议保持一致；
  3. 删除 FikaModule（单机包不需要），解除 Fika.Core 引用。
- 风险：服务端使用 `container.resolve("SaveServer")`（3.11 语义）需改为 4.1 注入；时间写入点要与 4.1 的 `RaidTimeAdjustmentService` 组合验证。
- 结论：适合移植 · 中（2-4 人天）。

#### A4 BetterBackpacks（[7] 更大的背包）
- 事实：单文件 `src\mod.ts`，356 LOC；`IPostDBLoadMod` 中直接改 `tables.templates.items`：目标 itemID 列表（约 40 条）+ 网格宽高 + `removeFilters`。
- 工作内容：
  1. C# 重写为 `IOnLoad` 类，注入 `TemplateTable`；
  2. 核对全部 itemID 在 4.1.6 数据库仍存在（新版本增删背包；4.1 有新增背包可补充）；
  3. `Grid`/`_props` 字段路径按 4.1 模型核对。
- 风险：低；数据漂移用告警+跳过策略即可。
- 结论：适合移植 · 低（0.5-1 人天）。

#### A5 ThatsLit（[6] AI 视线优化）
- 事实：33 .cs / 8353 LOC，net472；核心 `SeenCoefPatch`（1287）、`ThatsLitPlayer`（1063）、`ScoreCalculator`（992）、`ThatsLitGameworld`（791）；全仓库 82 处反射调用、121 处配置项；含 `ThatsLit.Sync`（Fika 同步，可裁）。
- 工作内容：
  1. 重编译到 4.1.6 客户端引用；
  2. 建立字段探测清单：可见性系数、AI 感知、`Player`/`GameWorld` 相关字段在 0.16 的名称与类型（反射点失效不会编译报错）；
  3. 裁掉 Sync/Fika 组件；
  4. 实机验证：视野/能见度表现与性能。
- 风险：反射型 mod 的漂移是静默的，验证成本高于编译成本；若上游（thuynguyentrungdang）已有 4.x 版本，直接用上游更省。
- 结论：适合移植 · 中-高（3-6 人天）；建议先做字段探测再决定。

#### A6 HomeComforts（[5] 舒适家园系统）
- 事实：客户端 19 .cs / 1474 LOC（Safehouse 178、SpaceHeater 167、SafehouseSession 192）；服务端 3 文件 / 465 LOC（mod_helper 306 + mod 138）：自定义物品注册 + 路由 + 会话数据。
- 工作内容：
  1. 服务端 TS→C#：`CustomItemService` 物品克隆、路由、profile 数据读写（4.1 `SaveProfileService`）；
  2. 客户端重编译与补丁点核对（交互物品、UI、温度/需求逻辑）；
  3. 裁 Fika/Dissonance 引用（csproj 中 `Fika.Core`、`DissonanceVoip` 等）。
- 风险：客户端依赖服务端下发的自定义物品 ID，需成对验证；跨 mod 交互（LeaveItThere 引用在此仓库 csproj 中出现）注意顺序。
- 结论：适合移植 · 中（2-4 人天）。

#### A7 LeaveItThere（[5] 战局内存储）
- 事实：客户端 38 .cs / 4384 LOC（ItemMover 555、FakeItem 333、Settings 356、UI 265）；服务端 2 文件 / 496 LOC。
- 工作内容：同 A6 的结构（服务端小、客户端大）；额外核对物品搬运/伪造物品在 0.16 的物品控制器与网络事件；AssetBundle 加载点（13 处）核对路径；裁 Fika。
- 风险：物品交互是 0.16 变化较活跃的区域；实机回归工作量不小。
- 结论：适合移植 · 中-高（3-6 人天）。

#### A8 barter_economy（[5] 以物易物经济）
- 事实：4 .ts / 1776 LOC；`src\mod.ts`（958）为算法主体：为商人生成以物易物 assort，涉及 `RagfairOfferGenerator`、`HandbookHelper`、`ItemHelper`、商人 assort 定时更新、日志写盘（VFS 111 行）、Realism 兼容分支。
- 工作内容：
  1. 全量 C# 重写：注入 `TradersTable` / `ItemHelper` / `RagfairPriceService` 等；
  2. assort 数据形状对照 4.1 `AddCustomTraderHelper` 期望（阻断器 3）；
  3. 定时更新改为 `IOnUpdate`；
  4. VFS 日志改 `System.IO`。
- 风险：算法正确性需要与 4.1.6 的经济数值联合调参；Realism 兼容分支在 Realism 缺席时直接删除。
- 结论：适合移植 · 中-高（3-6 人天）。

#### A9 TarkovIRL WHM（[5] 武器操控大修）
- 事实：41 .cs / 4816 LOC，netstandard2.1；17 个 Harmony 补丁；`PrimeMover`（832）核心；csproj 直接引用 `..\LIN-Realism-Mod-Client\release\RealismMod.dll`；README 明示「依赖 Realism Mod」。`RealismWrapper` 只读 Realism 公开 API（WeaponStats/PlayerState/PluginConfig/StanceController）。
- 工作内容：Realism 客户端移植完成后，重编译 + 核对 17 个补丁点的 EFT 0.16 签名；核对 Realism API 面在新版本是否变化。
- 风险：**被 Realism 阻断**；若 Realism 不上 4.1.6，此 mod 视为不可移植（或需改造成去除 Realism 依赖，工作量升级为高）。
- 结论：适合移植 · 中（2-4 人天），前置条件 = Realism 客户端就绪。

#### B2 DadGamerMode（[11] 作弊工具）
- 事实：完整老式 csproj（net471）源代码 16 .cs；`Plugin.cs` + `Features`（GodMode/Energy/Hydration/FallingDamage/MagReloadSpeed/MaxStamina/CODMode）+ `Patches`（ApplyDamage/DestroyBodyPart/InstantConstruction/InstantProduction/OnWeightUpdated）；`references/` 自带 85 个 DLL（无 DLL 源码依赖风险）；Plugin 版本 1.9.3。
- 工作内容：
  1. 重编译到 4.1.6 客户端引用（BepInEx 5 / net471 不变）；
  2. 6 个补丁点签名核对（`Player.ApplyDamageInfo`、藏身处即时建造/生产、重量更新）；
  3. 删除或更新 `VersionChecker`（3.11 版本号硬校验）。
- 风险：低-中；补丁点均为常见 API，漂移可控。
- 结论：适合移植 · 低-中（1-2 人天）。

#### B5 IOF 2960（[10] 物品自动分类）
- 事实：完整源码工程（net472，24 .cs + tests + CI + towncrier CHANGELOG）；README 移植链明确写有 "Ported to SPT 4.1.x by flir"；CHANGELOG 1.9.0 / 2026-08-27。856 快照是旧版（Seion.Iof 1.3.0）且无源码。
- 工作内容：
  1. 先复核：pack 的 pending 判定针对的是哪个 IOF 条目（#12 名称「INVENTORY ORGANIZING FEATURES」与 Forge 2960 的关系）；
  2. 若需要自建：用 4.1.6 客户端引用重编译（`inventoryorganizingfeatures.SPT.References` NuGet 或本地引用），或直接采用作者 4.1.x 发行包。
- 风险：低；主要风险是重复劳动。
- 结论：适合移植 · 低。

#### B12 Headshot-Damage-Redirection 2953（[6] AI 锁头重定向）
- 事实：3 .cs 源（Plugin/Settings/ConfigurationManagerAttributes），v1.8.0，net472；单补丁 `Player.ApplyDamageInfo` 前缀，做头/胸伤害改判与减伤。2115 快照（旧）无源码。
- 工作内容：重编译到 4.1.6 引用；核对 `ApplyDamageInfo` 签名与 `DamageInfo` 结构；实机验证减伤/改判逻辑。
- 风险：低；注意 4.1 已有「面具修复」类 mod（KnightMaskFix 已装），功能边界勿重叠。
- 结论：适合移植 · 低（0.5-1 人天）。

#### B15 ItemContextMenuExt 940（[9] 上下文菜单，含 [1] CustomInteractions）
- 事实：完整解决方案（`ClientMods.sln`）：`ItemContextMenuExt` v1.8.0、`CustomInteractions` v1.8.0（+ `CustomInteractions.Prepatch` 客户端预补丁）、`ItemAttributeFix`、`ItemSellPrice`、`MagazineInspector`、`MunitionsExpert`；全部 net472。
- 工作内容：
  1. 重编译到 4.1.6 客户端引用；处理 `Shared\` 引用目录（Assembly-CSharp/Comfort/ItemComponent.Types/spt-reflection）；
  2. 核对 `CustomInteractions.Prepatch` 机制在 4.1 客户端预补丁管线下仍可用（3.11 时代技术，风险点）；
  3. ItemContextMenuExt 依赖 CustomInteractions，两个未就绪条目（#16/#75）需同批处理；
  4. `MunitionsExpert` 项目可对照 pack 中已装的 4.1 版本，作为 API 漂移的参照物。
- 风险：中；客户端预补丁是 0.15→0.16 迁移中最容易失效的一类。
- 结论：适合移植 · 中（2-4 人天）。

#### B19 BRNVG 954（[7] 平衡夜视装备视效，客户端+服务端）
- 事实：完整源码；客户端 `BorkelRNVG` v3.0.3（netstandard2.1，48 .cs + bundle/png 资源）；服务端 `BorkelRNVGServer` **net10.0，`PackageReference SPTarkov.Server.Core 4.1.0`**，`Metadata.SptVersion = Range("~4.1.0")`（Metadata 内版本 2.3.0，与客户端 csproj 3.0.3 不一致，需核对）。
- 工作内容：
  1. 服务端把包引用升到 4.1.6 后重编译（或直接引用 4.1.6 运行时程序集）；
  2. 客户端重编译到 4.1.6 引用（引用的是客户端 Managed 集）；
  3. 按 MO2 overlay 规范打包并实机验证。
- 风险：低；主要非技术风险——对照表将 BRNVG 归为「非 Forge（Hub 独占）」，需确认 Hub 版本与授权状况，可能根本不需要自建移植。
- 结论：适合移植 · 低。

### 2.2 重写型移植（高工作量，需专门排期）

#### A10 Realism（[5] 现实主义大修）
- 事实：客户端 57 .cs / 28553 LOC（HealthController 2365、StanceController 2161、StancePatches 1455、HealthPatches 1300、StatCalc 1213、Zones 1174；反射点 612、Bundle 28、Fika 21）；服务端 37 ts / 33147 LOC（ammo 4.9K、bots 3.7K、armor 2.1K、bot_gen 2.1K、traders 1.3K、mod.ts 1.3K；`extends` 11 处、ragfair 273 处、container.resolve 227 处）。
- 工作内容（若做）：客户端全量 API 漂移治理；服务端 TS→C# 重写并处理核心类扩展（阻断器 4）；弹道/护甲/bot/商人四套子系统重新校准。
- 风险：等同重做一个大型 overhaul；且与 pack 中 69 项已有 mod（SAIN、APBS、ABPS 等）存在深度耦合/冲突，需要重新做兼容矩阵。
- 结论：重写型；不建议在 4.1.6 排期。若保留 Realism 目标，应作为 5.0 线专项或等待上游。

#### A11 Softcore（[5] 经济与制造大修）+ Softcore-NG
- 事实：3.11 版 29 ts / 5280 LOC（fleamarket 785、productionAdjustments 698、recipes 650、各 changer）。NG（SPT 5.0 线）已完成重写：src 52 .cs / 5260 LOC，含 20 个 changer、EmbeddedResource 数据表、xUnit 测试、构建脚本；覆盖 Softcore + True Items + noFIR + 反重力臂章 + 更大背包 + 战局时长，`ModMetadata.SptVersion = "~5.0.0"`。
- 工作内容：
  - 路线 B（推荐评估）：NG 重定向 4.1.6——改 `SptVersion`、引用 4.1.6 运行时程序集、修 5.0→4.1 API 漂移（表/模型/加载钩子/required 语义），跑通单测与实机；同时把 4 个附带功能组拆出或保留；
  - 路线 A：按 3.11 源码直译 C#。
- 风险：NG 是 5.0 线资产，重定向会引入 5.0/4.1 双维护；4.1.6 与 5.0 的模型差异（如 required 成员、任务 name 语义）需要实测；直译路线则重复投入。
- 结论：重写型；推荐先做「NG 重定向可行性试验（半天）」再排期；一旦可行即清 5 个未就绪条目。

#### A12 PTT + IEAPI（[5] 全地图移动）
- 事实：PTT：客户端 53 .cs / 5106 LOC（Fika 引用 275 处，投票/自定义撤离服务）、服务端 63 ts / 7771 LOC（controller 1129、config 583、event-watcher 336、traders 访问控制）、`sptVersion ~3.11.3`；IEAPI：11 .cs / 1884 LOC（含示例与单例服务），serve PTT 的撤离交互 API。
- 工作内容：
  1. IEAPI 先行：重编译 + `GetActionsClass.GetAvailableActions` 补丁点核对（中等，1-3 人天）；
  2. PTT 服务端全量 C# 重写：撤离点/路径图、事件监听、商人访问控制、配置解析、route 注册；
  3. Fika 组件整体删除或重写（单机包）；
  4. 与 4.1 新撤离/transit 机制做功能对齐。
- 风险：PTT 是跨地图状态机，验证成本高；`loadBefore/loadAfter` → `TypePriority` 语义变化影响加载次序（映射表中）。
- 结论：重写型（1-3 周）；IEAPI 可独立先做。

#### A13 RaidOverhaul（[5] 战局大修）
- 事实：服务端 24 .ts / 10778 LOC（EventController 1689、ReqsController 711、ItemGenerator 592、ItemController 543、LegionController 561、WeatherController 381、PkController 456、Enums.ts 4058 为枚举数据）；客户端 41 .cs / 5178 LOC + `ROPrepatch`（WildSpawnType 枚举预补丁）。
- 工作内容：
  1. 服务端 TS→C#：多控制器重写（事件、天气、物品生成、商人、Legion 派系）；`registerStaticRouter` 13 条 + `DynamicRouter` 2 条按 4.1 路由重写；
  2. 客户端重编译 + 枚举预补丁核对 4.1 预补丁管线（阻断器 2 的对应机制）；
  3. 处理 locale 写入（27 处）走 `AddTransformer`（阻断器 1 的官方解法）。
- 风险：内容量大、控制器多；预补丁机制在 4.1 服务端/客户端均有变化；与 ABPS/APBS 等装好的 AI mod 的 bot 生成交互需回归。
- 结论：重写型（2-4 周，需专项）。

### 2.3 不建议

| 项 | 原因 |
|----|------|
| B1 SPTCorpseCleaner | 快照仅 obj 残件（v1.0.5），无源码无 DLL；如需，取上游仓库 |
| B3 Janky's Visual Assist | 同上（LibSPT/obj 残件） |
| B4 Game Panel HUD | 同上（9 个工程全部只有 AssemblyAttributes）；即使取源也是大工程 |
| B6 Reach Extender | 同上 |
| B7 HeliCrash | 同上（2.5.2 构件线索，无源） |
| B8 MeaningfulWeaponMasteries | 同上 |
| B9 Always-Level-Endurance | 同上 |
| B10 LockableDoors | 同上；obj 显示曾有 net9.0 服务端组件，若要追需上游源码 |
| B11 AnimatorUpdater | 同上 |
| B13 BackdoorBandit | 同上（客户端+服务端均无源） |
| B14 DynamicItemWeights | 同上 |
| B16 VCQL | 同上（Client\VCQLQuestZones 仅 obj）；作为前置框架，影响面大，不建议自研 |
| B17 KmyTarkovApi | 同上 |
| B18 DisableScavMode | 同上 |
| B20 ProfileEditor | 快照无源码（57 .cs 均在 obj，为生成代码）；工具类，直接核对上游发行版即可；且主工程版本 4.1.0 / net10 显示上游已覆盖 4.x |
| A10 Realism、A12 PTT、A13 RaidOverhaul | 见 2.2；不建议以「顺手移植」方式安排，应单独立项并先做 4.1.6/5.0 目标线决策 |

---

## 3. 建议移植顺序（性价比排序）

| 顺序 | 项目 | 理由（收益 / 成本 / 风险） |
|------|------|------|
| 1 | **BRNVG**（B19） | 源码已是 4.1 世代（服务端直接引用 4.1.0 包），近乎重编译；先复核 Hub 现状，可能零开发 |
| 2 | **IOF**（B5） | 上游已有 4.1.x 移植源码（flir），重编译即用；先复核 pending 判定 |
| 3 | **Headshot-Damage-Redirection**（B12） | 单补丁 3 文件，最低成本的功能补齐 |
| 4 | **ImmersiveDaylightCycle**（A3） | 自有仓库、服务端仅 5 条路由，边界最清晰的服务端 TS→C# 练习样本 |
| 5 | **BetterBackpacks**（A4） | 数据型 mod，改写直接、验证简单 |
| 6 | **HollywoodCam**（A1） | 自有客户端仓库、体量适中；pack 定位 [13]（高价值视觉项） |
| 7 | **DadGamerMode**（B2） | 完整客户端源码、6 个常规补丁点，半天到两天工作量 |
| 8 | **ItemContextMenuExt + CustomInteractions**（B15） | 一次清两个未就绪条目（#16/#75）；注意客户端预补丁风险 |
| 9（试验） | **Softcore-NG 重定向 4.1.6**（A11） | 半天试验：若 API 漂移可控，一次清 5 项（Softcore/True Items/noFIR/反重力/背包）；试验失败则转 5.0 线 |
| 10 | ThatsLit、HomeComforts、LeaveItThere、barter_economy、IEAPI（A5-A8、A12 前件） | 批次二：中等体量、成果稳定；ThatsLit 先做字段探测 |
| 11 | WhiteBoxFix（A2） | 最后做（先验证 bug 是否仍存在；若无则直接关闭条目） |

不建议进入本批：Realism（A10）、PTT（A12）、RaidOverhaul（A13）——重写型，先决策 4.1.6 与 5.0 的目标线；若目标线仍是 4.1.6，按专项立项（各自 1-4 周）。

---

## 4. 数据可信度备注

**经深入核对（逐文件/逐目录实读）**：
- A 类 16 个仓库的工程形态：csproj TF/引用（`SPT3114Path`、`D:\ref`、`Shared\` 等硬编码引用路径）、源码 LOC、入口文件与核心补丁/服务面计数（Harmony 点数、`getTables`/`container.resolve`/路由/反射调用计数）。
- B 类 22 个快照的源码真实性：逐目录清点 `.cs/.ts/.csproj/.sln/.dll`；构件版本从 `obj\**\AssemblyInfo.cs`（如 SPTCorpseCleaner 1.0.5、HeliCrash 2.5.2、LockableDoors-Server 0.0.1/net9.0）与 `project.assets.json`（ProfileEditor 4.1.0 / net10.0-windows）提取。
- 仓库近期状态：git 最后提交（IDC 2026-09-08、ThatsLit 2026-05-22、HomeComforts 2026-06-07、LeaveItThere 2026-06-25、Realism client 2026-07-29、PTT 2026-09-02、RaidOverhaul 2026-07-27、Softcore-NG 2026-09-26）；分支情况（均无 4.x 分支，除 Softcore-NG 的 5.0 线）。

**快速判断（未编译、未实机）**：
- 客户端 C# 的 0.15 → 0.16 API 漂移量：本地没有 4.1.6 客户端程序集对照，依「同代先例」（Moew-SAIN-For-4013 完成过 3.x→4.0.13 客户端+服务端移植；pack 中 69 项 4.1.6 客户端 mod 的既有生态）估计；真实漂移需以编译错误与实机日志为准。
- 服务端 TS→C# 工时：按迁移基准的难度占比（低约 50%/中约 35%/高约 15%）与各仓库 API 面计数估算，误差可能 ±50%。
- `类型代码 .d.ts` 与 node_modules 未计入迁移量；但仓库内存在 `-副本` 重复文件（PTT、IEAPI、RaidOverhaul 的 `-副本2026...`），实际源文件数量以有效文件为准（影响很小）。

**待复核事项（影响结论）**：
1. **IOF**：Forge 2960 的 README/CHANGELOG 显示已支持 4.1.x；需对照 pack 的 Forge 扫描口径（filter[spt_version]=4.1.6）确认 pending 判定是否仍成立。
2. **BRNVG**：对照表归其为「非 Forge（Hub 独占）」；需核对 Hub 当前版本对 4.1.6 的支持与授权，避免重复开发。
3. **B 类无源码快照**：若仍需要这些功能，应从上游 GitHub/Hub 取源重新评估（本次结论仅针对「本地快照」）。
4. **Softcore-NG 重定向**：5.0.0 → 4.1.6 的服务端 API 漂移量未知，需要一次编译试验（引用 4.1.6 运行时程序集）才能定性。
5. **前置条件**：客户端移植需先准备 4.1.6 引用集（Managed + `BepInEx\plugins\spt`，参照 Moew-SAIN-For-4013 的 `References/` 模式）；服务端移植以 KB 的 `IModMetadata + [Injectable] + IOnLoad` 骨架为准。

---

## 5. 附：本评估使用的关键事实来源

- `E:\Game\EFT_Offline\Norvinsk_Forever\docs\pending-mods-v0.1.0.md`（76 项未就绪）
- `E:\Game\EFT_Offline\Norvinsk_Forever\docs\对照表-v0.1.0.md`（69 项已安装 + 原因分类）
- `...\.scratch\norvinsk-forever-4.1.6\porting-candidates.md`（候选清点；B 类自动匹配含误报，本评估以人工过滤后的 20 组为准）
- `knowledge\spt-kb\curated\migration\api-mapping-311-to-41.md`（映射表与 4 大阻断器）
- `knowledge\spt-kb\curated\api-notes-4.1\`（mod-loading / di-container / database-structure / architecture-map）
- `knowledge\spt-kb\archive\forge\mods\`（22 个快照逐目录清点）+ `hot-index.json`（94 条 best_version/best_spt 证据）
- `E:\云文件\GitHub\Moew-SAIN-For-4013`（4.0.13 移植先例）

> 本报告只做静态评估；所有候选均未编译、未安装、未改动。实机验证由用户执行。
