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
