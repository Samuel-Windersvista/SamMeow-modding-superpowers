# 移植记录（SPT 4.1.6 线 v0.1.0）

本文件登记本项目从 SPT 5.0 实现移植到 SPT 4.1.6 的适配执行结果、数据形态实测，以及部署与验收登记。
内容以已核实事实与代码佐证为准。

## 1. 适配执行

### 1.1 可空性修正（9 处）

4.1.6 运行时数据模型相对 5.0 引入可空性差异。以下站点按「空值守卫后计算 / 判真处理」落实：

| # | 组件 | 4.1.6 差异 | 落点 |
|---|---|---|---|
| 1 | `OtherTweaksChanger`（弹药堆叠） | `StackMaxSize` 为 `int?` | `is not { StackMaxSize: { } size }` 守卫；null 与 0 均跳过 |
| 2 | `SamuelTweaksModule`（可掠夺） | `Unlootable` 为 `bool?` | `props.Unlootable != true` 跳过；仅 true 处理 |
| 3 | `FasterCraftingTimeChanger.ApplyAllRecipes` | `ProductionTime` 为 `double?` | `recipe.ProductionTime is { } time` 守卫 |
| 4 | `FasterCraftingTimeChanger.ApplyForEndProduct` | `ProductionTime` 为 `double?` | 同上（月光酒 / 纯净水） |
| 5 | `FasterBitcoinFarmingChanger` | `ProductionTime` 为 `double?` | 同上 |
| 6 | `ScavCaseChanger`（fasterScavcase） | `ProductionTime` 为 `double?` | 同上 |
| 7 | `FasterHideoutConstructionChanger` | `ConstructionTime` 为 `double?` | `stage.ConstructionTime is { } time` 守卫 |
| 8 | `TraderChangesChanger`（Skier 欧元） | `MinSalesSum` 为 `long?` | `(loyaltyLevel.MinSalesSum ?? 0)` 兜底 0 后换算 |
| 9 | `NoFirHideoutModule` | `IsSpawnedInSession` 为 `bool?` | `== true` 才置 false（含 `StageImprovementRequirement` 分支） |

第 3–7 项为时间字段可空守卫，合计 5 个站点：`ProductionTime`（Crafting 全局 + Crafting 单项 + Bitcoin +
ScavCase）+ `ConstructionTime`（建设）。

### 1.2 类型取整修正

| # | 组件 | 4.1.6 差异 | 落点 |
|---|---|---|---|
| 6' | `EconomyOptionsChanger`（跳蚤等级） | `MinUserLevel` 为 `int`（5.0 为 `double`） | `(int)Math.Round(level, MidpointRounding.AwayFromZero)` 赋值，保持原取整语义 |

### 1.3 工程项

- **SptVersion**：`ModMetadata.SptVersion = new("~4.1.6")`（否则 `ModValidator` 拒载）。
- **运行时引用**：`Directory.Build.props` 自包含声明 `SptRoot`（默认 `E:\Game\EFT_Offline`）与
  `SptRuntimeDir`（默认 `$(SptRoot)\SPT_41x\SPT_Runtime`）；引用 DLL 清单与 5.0 线一致。本项目级
  `Directory.Build.props` 遮蔽 `mods\` 根同名文件，故两属性在此自包含声明，均可经命令行覆盖。
- **警告纪律**：未启用 `WarningsAsErrors`；src 范围告警审查清零（见 §3）。

### 1.4 清单外额外修复

- **RaidDuration 时限字段守卫**：4.1.6 `LocationBase.EscapeTimeLimit` 为可空 `double?`（5.0 为不可空
  `double`）。`RaidDurationModule` 对 null / ≤0 一律**跳过且不计数**（视为未设时限），避免写入畸形值。
  此项不在 D7 清单内，系实现期对照运行时程序集发现并修正。
- **部署期修正：仓库标准行数 50→56（4.1.6 开局模板兼容，修复新档 [OOB] 越界）**：4.1.6 开局模板
  （`SPT_Data\database\templates\profiles.json`，含 Unheard）起始物品铺至 y=51、物品高 2 → 标准仓需
  ≥53 行；渐进仓库将新档仓库统一转 `STASH_STANDARD_STASH_10X30` 时原 50 行越界（[OOB] 实测复现）。
  `StashChanger.StashSizes` 首项提升至 56；其余 100/150/200/250 不变。

## 2. 计数语义修正（工单 02 收尾）

- 上述 5 个时间字段站点统一为「**null 跳过不计数**」：可空值为 null 时不产生变更、不虚增变更数
  （`log.Changed()` 仅在成功写入后调用）。
- **背景**：4.1.6 现数据**零 null `ProductionTime`**（见 §4），故该守卫属**防御性**，不影响当前计数实况；
  其价值在于数据形态变化或异常数据下不误报、不崩溃。

## 3. 测试与警告

- **单测**：`dotnet test` 全绿，共 **215 项**（配置层、G1–G7 样例、可空守卫专项断言、数据资源校验）。
- **src 告警**：`dotnet build -c Release` 0 错误；src 范围零告警。
- **测试工程警告**：`tests/` 设 `NoWarn`（CS8602 / CS8604 / CS8629）——测试夹具按生产形态构造可空 POCO，
  解引用告警为已知噪声；产品代码 `src\` 不抑制。

## 4. 数据形态实测（4.1.6 DB 探针）

以 4.1.6 运行时数据库为真值对关键集合实测：

- **臂章**：24 款；`Unlootable=true` 23 款 / `=false` 1 款；`UnlootableFromSide` 非空 24 款。
  （G4 查找表为 22 款减重子集；此处为 DB 全量父类盘点。）
- **近战武器**：26 款，全部 `Unlootable=true`。
- **制造配方**：`production.recipes` = 220 条；`scavRecipes` = 5 条；`ProductionTime` **零 null**。
- **任务**：
  - Collector `5c51aac186f77432ea65c552`：`AvailableForStart` 原为 136 条 Quest 前置；`AvailableForFinish`
    含 39 条 `HandoverItem`。
  - Crisis `60e71c48c1bfa3050473b8e5`：`AvailableForStart` 含 `Level` 条件（值 48）。
  - Drip-Out：两组同名（`6613f300…` / `6613f307…` 与 `66151401…` / `6615141b…`）——印证按 **id** 匹配的必要性。

## 5. 部署与验收登记（工单 05–07）

### 5.1 部署与启动核验（工单 05 / 06，2026-10-12）

| 项目 | 结果 |
|---|---|
| profile 备份标识 | `Default_pre-softcore416`（`.mo2-mcp\profile-backups\`） |
| 部署单元 | MO2 overlay `mods\[5]软核大修-InescapableTarkovsSoftcore`（启用；DLL SHA256 头部 `91B9C9E9…`） |
| 服务器日志摘要 | `[ITS] Inescapable Tarkov's Softcore v0.1.0 loaded`；合计：执行 7 个模块，变更 10609，警告 32，错误 0，跳过 0 |
| 数据缺口计数实况 | 背包 9 条（id 未命中）+ trueItems 23 条（22 个 stim 占位 + 1 条 Vinyl record），均属已知静默跳过 |
| 未处理异常 | 无（唯一 Error 为良性 `Unable to add bundle: barrel_vpo215_600mm_366tkm.bundle`——与 BetterRearSights 的先到先得注册冲突） |

模块级计数（启动日志）：softcore 9900 / samuelTweaks 126 / antigravArmbands 22 / backpacks 34（警告 9）/
trueItems 250（警告 23）/ noFirHideout 277 / raidDuration 0（倍率 1.0 无变化即正确）。

### 5.2 部署期修复登记（2026-10-12/13，四项）

| # | 问题 | 修复 | 复验结果 |
|---|---|---|---|
| F1 | CloudSix FikaSync 死目录导致启动 Critical | 整体隔离至 `_quarantine\CloudSix-OptionalFikaSync-20261012-2307` | 重启后 Critical 清零 |
| F4 | Painter 商人 assort 4 条残货报错 | 移除残货条目（原件隔离于 `_quarantine\backups`） | 重启后报错清零 |
| F6 | 4.1.6 开局模板起始物品越界（新档 [OOB]） | 仓库标准行数 50→56 并重新部署（见 §1.4） | 新档 [OOB]=0 |
| F2 | WTT Content Backport 3 个 bundle 静默缺失 | overlay 目录更名 `[2]WTT正式版内容`（物理路径全树 ≤252）；modlist 同步修正 | **2026-10-13 重启复验 PASS：`Could not find bundle` 0 条** |

**F2 根因（重要新知）**：MO2/USVFS 对物理全路径 **>260 字符**的 overlay 文件不投映给经 MO2 启动的进程
（本例 SPT.Server），表现为 `Could not find bundle …（文件实际存在）` 的静默缺失。实证：3 个 bundle 物理路径
263/268/273 字符 → 缩短 overlay 目录名后归零。**约定：overlay 目录命名与层级须保证全树物理路径 ≤260。**

### 5.3 进游戏核对（工单 07）

- 核对清单已交付用户（11 项：臂章 / 背包 8×9 / 堆叠上限 / 藏身处免 FIR / 跳蚤 / 商人 / 保险 /
  藏身处加速 / 战局时长 / Collector / 新档仓库 56 行）。
- 状态：**用户游玩期间渐进验证中**（非阻塞）；异常项将登记于本节。
- 已完成子项：F2 服务器复验（§5.2）；新档 [OOB] 复验（§5.2 F6）。
- 待复核子项：Crisis「+30」/ Drip-Out / Collector 实际行为（§6 开放项）。

## 6. 开放项

- **Crisis「+30」在 4.1.6 的可复现性**：4.1.6 `AvailableForStart` 已含 `Level`（值 48）条件；源 TS 的
  `AvailableForStart[1].value=30` 依赖原结构位置，能否复现以**运行日志**定论。
- **Collector 接取/完成/存档**：A4S 重组为单条 Level 20、A4F 追加 Gamma 交付（Value=1），需进游戏复核
  接取、完成与存档兼容。（2026-10-13 起随游玩渐进验证，见 §5.3）
- **旧存档超上限堆叠**：旧存档中超过新堆叠上限的物品在 `InvalidStackObjectsCountFix` 迁移路径下的行为。
- **各 Config 游戏表现**：barter / 报价数 / 保险 / Fence 参数等的实际游戏表现。

## 7. v0.2.0（配置外置化与数据全量对齐）

本节登记 v0.2.0 相对 v0.1.0 的数据与阶段变更结果，引用各工单 Comments；部署与服务器日志验收待
工单 17 实测填充（见 §7.6）。上游规格 `docs/specs/spec-v0.2.0.md`。

### 7.1 配置外置化（工单 08）

- 产品版本 0.1.0 → **0.2.0**（GUID / Name 不变）。
- 12 张数据表随包分发于 overlay `config/` 子树（外部文件优先）；内嵌资源仅作**缺失重建 / 解析失败回落**兜底。
- 唯一共享缝 `Features/ExternalJsonFile.cs`；支持 JSONC（注释 + 尾随逗号）与条目级容错；`config.json` 保持单文件不拆分。
- 覆盖验证：外部覆盖生效 / 缺失重建 / 损坏回落 / JSONC / 条目级容错（`ExternalJsonFileTests` 等）。

### 7.2 数据全量对齐（工单 09–13）

A/B/C 三类终态（以数据为准）：

- **A 删除**：
  - medicals 删 22 条旧包自定义 stim 占位（`cheeta_stim`、`boar_stim` … `radioactiveblood_stim`，均非 24-hex、全整合包无提供者）：43 → **21**（工单 11）。
  - 背包删 1 条 `6621b28d9411498998d408c3`（全整合包不存在，工单 10）。
  - softcore 四表 **31 条 neither 引用逐条核实后全部保留**（非死引用）：scavcase 5 条 = 配方自身 id；fleamarket 20 条 = 4.1.6 `handbook.json` 类目节点 Id；crafting-rebalance 5 条 = 配方 `_id` / 任务 `_id`；crafting-recipes 12 条 = 附加配方自身 `_id`。核对基准 `SPT_41x\SPT_Runtime\SPT_Data\database\`（工单 13 Comments）。
- **B 阶段后移生效**（不删）：8 条背包（WTT-Artem 2 / TGC-NG 6）+ Vinyl 记录 1 条（Epic AIO）——见 §7.3。
- **C 补档**：
  - 臂章 22 → **37**（+15：基础库 4 + WTT-ContentBackport 11）；按既有减重档位（-6/-8/-10/-15/-20/-25）分配，终态分组 `-6=5 / -8=6 / -10=9 / -15=11 / -20=1 / -25=5`（工单 09）。
  - 背包 43 → **56**（删 1 + 补 14：基础库 11 + WTT 3）；对旧包曾移除的 9 条先做体积/网格核查，旧包原版直系值优先，WTT 3 条采纳作者原值（工单 10）。
  - trueItems（工单 12）：barter +44（175 → **219**）、clothing +11（22 → **33**）、provisions +4（19 → **23**）；medicals 21（不变）、keycards ParentList 1（不变）、partsnmods List 104 + ParentList 10（`Active=false` 不变）。
  - **barter/clothing 裁定与 86 条暂缓**（工单 12 Comments）：候选 barter 85 / clothing 56，经**保守口径**（仅纳入既有替换源可证同类别；排除自造、来袭固定、未落地模板、硬错帽）裁定采纳 **55 条**（barter 44 + clothing 11）；剩余 **86 条暂缓**（barter 41 + clothing 45），待用户裁决后续工单。
- **规格笔误登记**：`spec-v0.2.0.md` Testing Decisions 记「barter 175 不变」，与 D3-C（trueItems 同类新增补条目）自相矛盾；实况**按 D3-C 执行**，barter 终值 **219**（以数据为准）。

### 7.3 处理阶段后移（工单 14）

- 入口 `TypePriority` 由 `OnLoadOrder.Preload + 1` 改为 `OnLoadOrder.PostLoad + 1`（n=1）。
- 证据：v0.1.0 于 14:39:02 完成处理时，包内内容注入 mod（Croupier 14:39:05 / TGC 14:39:08）尚未完成装载 → B 类物品不可见。PostLoad 为 4.1 阶段链最末窗口，n=1 保证排在全部内容注入之后（同相位按 ModGuid 字母序）。
- 内部模块固定顺序（G6→G1→G4→G5→G2→G3→G7）不变。

### 7.4 缺陷修复：臂章 / 近战父类互换（工单 15）

- 纠正常量：`ArmbandParentId = 5b3f15d486f77432d0509248`、`MeleeWeaponParentId = 5447e1d04bdc2dff2f8b4567`（来源 mod 标反，按 DB 真值纠正；已加防回归字面值断言）。
- 默认全开时两族均处理、变更总数不变；纠正后单开关语义与日志标签一一对应。README / `config.json` 注释同步。

### 7.5 测试与构建（v0.2.0）

- 单测：**235 项**全绿（v0.1.0 基线 215 → 工单 08 +13、复验 +2、审查收口 +3、工单 09/10 钉值同步、工单 14 改名、工单 15 +1）。
- `scripts/build.ps1` 幂等产出 overlay：DLL + `config.json` + `config/` 12 外置表（内置存在性断言）。

### 7.6 部署与验收登记（工单 17 实测，2026-10-17）

| 项目 | 结果 |
|---|---|
| profile 备份 | `Default_pre-softcore416-v0.2.0`（5 文件：modlist / settings.ini / archives / initweaks.ini / lockedorder） |
| overlay 更新 | `[5]软核大修-InescapableTarkovsSoftcore`：DLL + config.json + config/ 12 表，逐文件 SHA256 核对 **14/14 一致（mismatch=0）**；meta.ini 未动 |
| DLL | SHA256 `C2E0DE2927F714F5449623EE4051BF7A4930841A016B3FC0900463D095846255`（334336 B） |
| 服务器日志 | `Norvinsk_Forever\overwrite\SPT_Runtime\user\logs\spt\spt20261016.log`（MO2 启动的服务器日志写权重定向至实例 `overwrite/`） |
| 加载确认 | `[ITS] Inescapable Tarkov's Softcore v0.2.0 loaded`（2026-10-16 16:12:10） |
| 同类告警（未找到 / 未命中 / 跳过） | **0**（全模块 `warnings=0`；外置表 0 重建、0 回落消息；全文件 ITS Error / Critical = 0） |
| 模块 summary（变更 / 警告） | softcore 16905/0；samuelTweaks 247/0（护甲冲突修复 162、臂章可掠夺 **37**、近战可掠夺 27、弹匣缩格 21）；antigravArmbands **37**/0；backpacks **56**/0；trueItems **328**/0；noFirHideout 277/0；raidDuration 0/0；**合计 17850 变更，警告 0，错误 0，跳过 0** |
| B 类覆盖 | 8 背包（WTT-Artem 2 / TGC-NG 6）全部纳入（backpacks 56/56 命中）；Vinyl 记录随 trueItems 328 项应用（0 告警） |
| 阶段后移实证 | 内容注入 mod 物品在 `PostLoad + 1` 全部可见：samuelTweaks 覆盖面 67→162、弹匣 10→21、backpacks 34→56、trueItems 250→328、softcore 9900→16905（均为「此前不可见项」现被处理） |
| 用户抽查（进游戏渐进确认，非阻塞） | 清单已交付：新增臂章重量（Discord −10 / Prestige 5 −25 等 15 款）、WTT 背包尺寸（2 Day 5×6 / NICE Sling 5×7 / TT 45P 5×8）、Vinyl 堆叠、Developer Pack 归一 8×9、新增 barter / clothing / provisions 堆叠抽验；状态：进行中 |

> 回退预案：禁用 / 移除 overlay + 恢复 `Default_pre-softcore416-v0.2.0` profile 备份。
> overlay 路径纪律（F2 教训）：本产物全树物理路径远低于 260 阈值。
> 存档兼容：本版为服务端数据/加载方式变更，不破坏存档，无需开新档（spec User Story 28）。

