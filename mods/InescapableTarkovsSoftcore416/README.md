# Inescapable Tarkov's Softcore（SPT 4.1.6 线）

单一、自包含的 SPT 4.1.6 服务端整合 mod。把旧整合包 Life in Norvinsk v0.3.2 的六组功能
（Samuel's Tweaks、True Items Redux 物品真实堆叠、藏身处免 FIR、反重力臂章、更大的背包、
Softcore 经济与制造系统大修）加战局时长控制，重写为一个 C# 服务端 mod，配置全中文。

## 目标与范围

- 游戏线：SPT 4.1.6（build 40743 / EFT 0.16.9 / net10.0）。
- 目标实例：MO2 便携实例 `Norvinsk_Forever`（profile Default），游戏根 `E:\Game\EFT_Offline\SPT_41x`。
- 部署单元：MO2 overlay `[5]软核大修-InescapableTarkovsSoftcore`（向目标实例投影）。
- 版本：`0.1.0`（GUID `com.sammeow.inescapable-softcore`）。
- 形态：单程序集服务端 mod，无 `package.json`；只作用于服务端，不写入游戏本体文件。
- 本项目从同一产品的 SPT 5.0 实现移植而来（见 `docs/adr/0001-port-from-5.0-codebase.md`），
  数值真相继承其 `docs/specs/delta-table.md`。

## 功能组

1. **Samuel's Tweaks（G1）**：护甲弹挂冲突修复 / 可掠夺臂章与近战 / 三格弹匣缩格。
2. **True Items Redux（G2）**：物品真实堆叠上限（多数 2–5、注射器 8、配件不堆叠、钥匙卡不堆叠）。
3. **noFirHideout（G3）**：藏身处建造/升级移除 FIR 要求。
4. **反重力臂章（G4）**：22 款臂章按色阶减重 6–25kg 且可堆叠 5。
5. **更大的背包（G5）**：主流背包网格扩容、容器过滤清空。
6. **Softcore 经济与制造系统大修（G6）**：20 个 changer（安全容器 / 仓库 / 藏身处容器 / 制造 /
   建设 / 燃料 / 比特币 / ScavCase / 经济 / 商人 / 保险 / 制造配方 / 杂项）。
7. **战局时长控制（G7）**：全局战局时长倍率（默认 1.0 = 原版）。

各组开关独立；内部固定应用顺序 G6 → G1 → G4 → G5 → G2 → G3 → G7（G2 靠后使逐物品显式值优先于
G6 通用倍率）。

### 本期移除项

- **启动器背景（`customBackground`）**：v0.1.0 不随移植源（用户裁决）；配置模板与配置模型中已删除该键，
  构建脚本不含相关逻辑。后续如需另立。

## 文档

- `GLOSSARY.md` — 术语表。
- `docs/adr/` — 架构决策记录。
- `docs/specs/spec-v0.1.0.md` — 规格书快照。
- `docs/port-notes.md` — 移植记录（4.1.6 适配执行结果、数据形态实测、部署验收登记）。

## 工程布局

```
InescapableTarkovsSoftcore.sln
Directory.Build.props                    项目自包含构建属性（SptRoot / SptRuntimeDir）
config/default-config.json               受控默认配置模板（JSONC，内嵌 + 随包分发）
data/                                    功能查找表（EmbeddedResource 源文件）
  trueitems/                             G2 True Items 六张表
  antigravArmbands/armbands.json         G4 反重力臂章表（22 款）
  backpacks/backpacks.json               G5 背包扩容表
  softcore/scavcase.json                 G6 ScavCase 数据表
  softcore/fleamarket.json               G6 经济数据表（白/黑名单等）
  softcore/crafting-rebalance.json       G6 配方重平衡表
  softcore/crafting-recipes.json         G6 新增配方表
src/InescapableTarkovsSoftcore/          主工程（net10.0，库，SPT 服务端 mod）
  Config/                                配置模型与加载器
  Features/                              变换层接缝与编排器（七组模块 + G6 的 20 个 changer）
tests/InescapableTarkovsSoftcore.Tests/  xUnit 测试工程
scripts/build.ps1                        构建 + overlay 组装脚本
build/overlay/                           构建产物（git 忽略）
```

查找表经 `EmbeddedResource` 嵌入主程序集（逻辑名见 `InescapableTarkovsSoftcore.csproj`），
运行时模块与测试经 `FeatureTables` 读取同一资源，无需随 overlay 拷贝。

> 说明：`scripts/build.ps1`（UTF-8 with BOM）为部署流水线入口：构建主工程 Release 并组装
> `build/overlay/`（DLL + config.json 逐字节拷贝；每次运行清空重建、幂等）。

## 构建与测试

### 环境要求

- .NET SDK 10.0.300+
- SPT 4.1.6 服务端运行时（提供引用程序集），默认 `E:\Game\EFT_Offline\SPT_41x\SPT_Runtime`

### 命令

```powershell
dotnet build -c Release          # Release 构建，产出 InescapableTarkovsSoftcore.dll
dotnet test                      # 运行单测（215 项）
powershell -File scripts/build.ps1     # 幂等组装 overlay 产物（DLL + config.json 逐字节拷贝）
```

### 路径覆盖

SPT 引用程序集目录由 MSBuild 属性 `SptRuntimeDir` 控制（默认值见 `Directory.Build.props`）。
`SptRoot` 为游戏根约定，默认 `E:\Game\EFT_Offline`；`SptRuntimeDir` 默认 `$(SptRoot)\SPT_41x\SPT_Runtime`。
若 SPT 安装在别处：

```powershell
dotnet build -c Release -p:SptRoot="D:\SPT"
dotnet build -c Release -p:SptRuntimeDir="D:\Other\SPT_Runtime"
powershell -File scripts/build.ps1 -SptRoot "D:\SPT"
```

### 产物布局

`scripts/build.ps1` 幂等产出（每次运行重建 `build/overlay/`）：

```
build/overlay/
└─ SPT_Runtime/
   └─ user/
      └─ mods/
         └─ com.sammeow.inescapable-softcore/
            ├─ InescapableTarkovsSoftcore.dll
            └─ config.json      # config/default-config.json 的逐字节拷贝
```

本期无任何静态件（启动器背景已移除）；查找表已内嵌进 DLL，不投影到 overlay。

## 配置

### 文件位置

- 模板（仓库内受控源）：`config/default-config.json`
- 运行时实例（随 overlay 分发）：`SPT_Runtime\user\mods\com.sammeow.inescapable-softcore\config.json`
  （`scripts/build.ps1` 从模板逐字节拷贝）

若运行时缺失 `config.json`，mod 会从内嵌默认模板重建该文件并输出告警（不崩溃）。

### 格式与容错

- 支持 `//` 行注释与尾随逗号（JSONC）。
- 未知键：输出告警并忽略该键。
- 缺键：使用内置默认值。
- 值类型非法（如 `enabled` 写成字符串、整数键写成小数）：输出告警并回落默认值。
- 字典型键（如 `trueItems.overrides`）须为 JSON 对象；写成标量同样告警并回落默认值。
- 改动配置后需重启 SPT 服务器生效。

### 结构（逐键说明）

```jsonc
{
  "general": {                                      // 全局
    "enabled": true,                                // 总开关：false 时跳过全部功能模块
    "debug": false                                  // 调试模式：输出更详细日志
  },
  "samuelTweaks": {                                 // G1 Samuel's Tweaks
    "enabled": true,                                // 组开关；false 时整组跳过（零变更）
    "armorConflictFix": true,                       // 护甲弹挂冲突修复：含 RigLayoutName 的弹挂甲 → BlocksArmorVest=false
    "lootableItems": {                              // 可掠夺物品（可分别控制）
      "armband": true,                              // 臂章（父类 5447e1d04bdc2dff2f8b4567）可掠夺
      "meleeWeapons": true                          // 近战武器（类目 5b3f15d486f77432d0509248）可掠夺
    },
    "magazineResize": {                             // 扩展弹匣缩格：宽 1 且高 >2 且容量在 [min,max] → 高 2、ExtraSizeDown−1
      "enabled": true,
      "minCapacity": 10,                            // 容量下界（含）
      "maxCapacity": 50                             // 容量上界（含）
    }
    // 注：启动器背景（customBackground）为 v0.1.0 移除项，模板中不存在该键
  },
  "trueItems": {                                    // G2 True Items Redux
    "enabled": true,
    "overrides": {}                                 // 逐物品/父类微调：id → 目标堆叠值（最后应用，覆盖内嵌查找表）
  },
  "noFirHideout": {                                 // G3 藏身处免 FIR
    "enabled": true
  },
  "antigravArmbands": {                             // G4 反重力臂章
    "enabled": true,
    "stackSize": 5,                                 // 全部臂章的堆叠上限
    "overrides": {}                                 // 单品覆盖：臂章 id → 重量（最后应用，覆盖内嵌查找表）
  },
  "backpacks": {                                    // G5 更大的背包
    "enabled": true
  },
  "softcore": {                                     // G6 Softcore 经济与制造系统大修
    "enabled": true,                                // 组开关；数值为 SURV 终态
    "secureContainersOptions": {                    // 安全容器
      "enabled": true,
      "progressiveContainers": {                    // 渐进式安全容器：新档 2×2 腰包起步，逐级升级到 Kappa
        "enabled": true,
        "collectorQuestRedone": true                // 收藏家任务重做：20 级可接、仅交 1 张 Gamma
      },
      "biggerContainers": true                      // 放宽容器尺寸：Alpha 3×3 / Beta 3×4 / Epsilon 3×5 / Gamma 4×5 / Kappa 5×5
    },
    "stashOptions": {                               // 仓库
      "enabled": true,
      "progressiveStash": true,                     // 新档从 LVL1 仓库起步
      "biggerStash": true,                          // 扩容至 56/100/150/200 行（标准仓 56 行以兼容 4.1.6 开局模板）
      "lessCurrencyForConstruction": true,          // 仓库建造现金需求 ÷10
      "easierLoyalty": false                        // 降低阶段建造忠诚度要求（SURV 关闭）
    },
    "hideoutContainers": {                          // 藏身处容器扩容（SURV 终态，cellsV×cellsH）
      "enabled": true,
      "biggerHideoutContainers": true,              // 药品 10×10 / Holodilnick 10×10 / 弹匣 7×10 / 物品 6×6 /
                                                    // 武器 6×7 / 钥匙工具 5×5 / THICC 武器 6×14 / THICC 物品 6×14
      "siccCaseBuff": true                          // SICC 增强：并入 Docs 允许清单并允许钥匙工具
    },
    "fasterCraftingTime": {                         // 制造加速（配方时间 = ceil(原时间 / 倍率)）
      "enabled": true,
      "baseCraftingTimeMultiplier": 3,              // 全局基础倍率
      "hideoutSkillExpFix": {                       // 藏身处管理技能经验修复（hoursForSkillCrafting ÷10）
        "enabled": true,
        "hideoutSkillExpMultiplier": 10
      },
      "fasterMoonshineProduction": {               // 月光酒制造时间倍率（÷0.3 ≈ 时间 ×3.33；名称 faster* 但时间变长，属源真实行为）
        "enabled": true,
        "baseCraftingTimeMultiplier": 0.3
      },
      "fasterPurifiedWaterProduction": {           // 纯净水制造时间倍率（÷0.3 ≈ 时间 ×3.33；同属源真实行为）
        "enabled": true,
        "baseCraftingTimeMultiplier": 0.3
      },
      "fasterCultistCircle": {                      // 邪教徒圈制造时间倍率（÷0.5 = 时间 ×2；同属源真实行为）
        "enabled": true,
        "baseCraftingTimeMultiplier": 0.5
      }
    },
    "fasterHideoutConstruction": {                  // 藏身处建设加速（阶段建造时间 = round(原时间 / 倍率)）
      "enabled": true,
      "hideoutConstructionTimeMultiplier": 50
    },
    "fuelConsumption": {                            // 发电机燃料流量倍率
      "enabled": true,
      "fuelConsumptionMultiplier": 4
    },
    "fasterBitcoinFarming": {                       // 比特币农场：GPU 效率 + 基础挖矿时间倍率
      "enabled": true,
      "setBitcoinPriceTo100k": false,               // 将比特币手册价改回 10 万（SURV 关闭）
      "baseBitcoinTimeMultiplier": 1.3,
      "gpuEfficiency": 1.0
    },
    "scavCaseOptions": {                            // ScavCase：奖励池过滤 + 价值区间/配方重做 + 启动速度
      "enabled": true,
      "betterRewards": true,                        // 奖励池过滤（父类黑名单 + 物品黑名单）
      "fasterScavcase": {
        "enabled": true,
        "speedMultiplier": 0.5                      // ÷0.5 = 时间 ×2（名称 faster* 但时间变长，属源真实行为）
      },
      "rebalance": true                             // 奖励价值区间 + ScavCase 配方重做
    },
    "allowGymTrainingWithMusclePain": true,          // 允许在严重肌肉疼痛时以 75% 效率健身
    "economyOptions": {                             // 经济：和平主义跳蚤 + 以物易物 + 其他跳蚤改动（SURV 终态）
      "enabled": true,
      "disableFleaMarketCompletely": false,         // 完全禁用跳蚤市场（关闭）
      "priceRebalance": {                           // SURV 关闭（itemFixes 仅在启用时生效）
        "enabled": false,
        "itemFixes": true
      },
      "pacifistFleaMarket": {                       // 和平主义跳蚤市场
        "enabled": true,
        "whitelist": { "enabled": true, "priceMultiplier": 2 },      // 白名单价格倍率
        "questKeys": { "enabled": true, "priceMultiplier": 3 },      // 任务钥匙价格倍率
        "markedKeys": { "enabled": true, "priceMultiplier": 5 }      // 标记钥匙价格倍率
      },
      "barterEconomy": {                            // 以物易物经济
        "enabled": true,
        "cashOffersPercentage": 5,                  // 现金购买比例（%）；内部以 100 − 该值 作为 barter 概率
        "barterPriceVariance": 30,                  // 价差
        "offerItemCount": { "min": 5, "max": 13 },  // 报价数区间
        "nonStackableCount": { "min": 1, "max": 4 },// 非堆叠物品数区间
        "itemCountMax": 4                           // 最多 4 换 1
      },
      "otherFleaMarketChanges": {                   // 其他跳蚤改动
        "enabled": true,
        "sellingOnFlea": false,                     // 关闭：不禁止上架
        "fleaMarketOpenAtLevel": 1,                 // 1 级开放
        "fleaPricesIncreased": 1.5,                 // 价格 ×1.5
        "fleaPristineItems": true,                  // 仅全新品
        "onlyFoundInRaidItemsAllowedForBarters": false
      }
    },
    "traderChanges": {                              // 商人：收价上调 / 类别调整 / Fence 15 报价 / 欧元 / 上限 ×2
      "enabled": true,
      "betterSalesToTraders": true,                 // 收价上调（忠诚 +5%/级）
      "alternativeCategories": true,                // 类别调整（Therapist/Ragman/Skier）
      "pacifistFence": { "enabled": true, "numberOfFenceOffers": 15 },
      "reasonablyPricedCases": true,                // 藏匿箱合理定价
      "skierUsesEuros": true,                       // Skier 欧元化（含任务奖励换算）
      "biggerLimits": { "enabled": true, "multiplier": 2.0 }   // 购买上限 ×2
    },
    "insuranceChanges": {                           // 保险
      "enabled": true,
      "praporInsuranceChanges": {
        "enabled": true,
        "returnChance": 70,                         // 返还几率（%）
        "returnTime": { "min": 240, "max": 360 },   // 返还时间（分钟）
        "insuranceCostPercentage": 80               // 保费比例（%）
      },
      "therapistInsuranceChanges": {
        "enabled": true,
        "returnChance": 60,
        "returnTime": { "min": 120, "max": 240 },
        "insuranceCostPercentage": 50
      }
    },
    "craftingChanges": {                            // 制造（数据内嵌 crafting-rebalance.json / crafting-recipes.json）
      "enabled": true,
      "craftingRebalance": true,                    // 配方重平衡（按 endProduct 定位）
      "additionalCraftingRecipes": true             // 新增 12 条配方（3-b-TG / 肾上腺素 / L1 / AHF1-M / CALOK-B 等）
    },
    "otherTweaks": {                                // 杂项（SURV 终值）
      "enabled": true,
      "skillExpBuffs": false,                       // 技能经验增益（关闭）
      "signalPistolInSpecialSlots": true,           // 信号手枪可放入特殊槽
      "unexaminedItemsAreBack": false,              // 撤销「默认已检视」（关闭）
      "fasterExamineTime": true,                    // 检视时间固定 0.2s
      "removeBackpackRestrictions": true,           // 移除背包/容器过滤限制
      "removeDiscardLimit": true,                   // 移除丢弃限制
      "reshalaAlwaysHasGoldenTT": true,             // Reshala 必带金色 TT
      "biggerAmmoStacks": { "enabled": true, "stackMultiplier": 5 },   // 弹药堆叠 ×5
      "vestsBlockArmor": false,                     // false = 弹挂与护甲不冲突（与 G1 修复同向）
      "questChanges": true,                         // 任务变更（仅 Crisis + Drip-Out）
      "removeRaidItemLimits": true,                 // 移除战局内物品限制
      "biggerCurrencyStacks": false,                // 货币堆叠（关闭）
      "smallContainersInSpecialSlots": false        // 小型容器可放入特殊槽（关闭）
    }
  },
  "raidDuration": {                                 // G7 战局时长控制
    "enabled": true,
    "multiplier": 1.0                               // 全局倍率；1.0 = 原版，2.0 = 翻倍
  }
}
```

### `faster*` 倍率方向语义（源真实行为，非笔误）

`softcore` 下的 `faster*` 时间参数实现为 **时间 ÷ 倍率**，故：

- `0.3` → 时间 ×3.33（时间变长）；`0.5` → 时间 ×2（时间变长）。
- 涉及 `fasterCraftingTime.fasterMoonshineProduction` / `fasterPurifiedWaterProduction` /
  `fasterCultistCircle`、`scavCaseOptions.fasterScavcase`。
- `baseCraftingTimeMultiplier` / `hideoutConstructionTimeMultiplier` 等 `>1` 的倍率为常规语义（时间变短）。

这是旧包最终游玩态的行为，忠实保留。

## G6 制造与杂项语义

- **顺序约束（制造）**：`CraftingChangesChanger` 注册在 `FasterCraftingTimeChanger` 之后，故新增配方的
  `productionTime` 保持源值，不被全局 ÷3（与源 `Softcore.ts` 应用顺序等效，测试已钉住）。
- **配方重平衡**：数据 = 内嵌 `crafting-rebalance.json`，按 `endProduct` 定位；`find` 语义 = 仅命中首条。
- **`vestsBlockArmor=false`**：`false` 表示含 `RigLayoutName` 的弹挂甲 `BlocksArmorVest=false`
  （弹挂与护甲不冲突，与 G1 修复同向）。置 `true` 则不改动。
- **弹药堆叠**：父类为 Ammo 且 `StackMaxSize≠0` → ×`stackMultiplier`。
- **任务变更（按 id 匹配）**：任务变更一律按稳定 id 定位（4.1.6 的任务 `name` 同为 locale-key，且存在同名任务）。
  仅含 Crisis 与 Drip-Out；收藏家任务由 `CollectorQuestChanger` 重做。
- **未迁移/默认关闭项**：`skillExpBuffs`、`unexaminedItemsAreBack`、`biggerCurrencyStacks`、
  `smallContainersInSpecialSlots`（SURV 终态）。

## 数据表

查找表已作为内嵌资源随 DLL 分发，运行时无需外部文件，也不投影到 overlay。

| 资源 | 语义 |
|---|---|
| `trueitems/barter.json` | 杂物/以物易物物品堆叠 |
| `trueitems/clothing.json` | 衣物堆叠 |
| `trueitems/keycards.json` | 门卡父类堆叠 = 1（不堆叠） |
| `trueitems/medicals.json` | 医疗容器堆叠（StackMult 2；注射器实得 8） |
| `trueitems/partsnmods.json` | 配件（源 IMM 层 `Active=false`，不生效） |
| `trueitems/provisions.json` | 食品/饮料堆叠 |
| `antigravArmbands/armbands.json` | G4 反重力臂章表（22 款，含减重值） |
| `backpacks/backpacks.json` | G5 背包扩容表 |
| `softcore/scavcase.json` | G6 ScavCase 数据表 |
| `softcore/fleamarket.json` | G6 经济数据表（白/黑名单等） |
| `softcore/crafting-rebalance.json` | G6 配方重平衡表 |
| `softcore/crafting-recipes.json` | G6 新增配方表 |

G2 应用规则：`StackMaxSize = 条目值 × StackMult`，并置 `StackMinRandom = 1`；未命中 `_id` 输出告警并跳过；
表 `Active=false` 时该文件零变更。`overrides` 在所有查找表之后应用：key 先按物品 `_id` 精确匹配，
匹配不到再按父类 `_parent` 批量匹配。

## 部署映射与回滚

| overlay 根 | 游戏实例 |
|---|---|
| `build/overlay/` | 游戏根 `E:\Game\EFT_Offline\SPT_41x` |
| `build/overlay/SPT_Runtime/user/mods/com.sammeow.inescapable-softcore/` | `SPT_Runtime\user\mods\com.sammeow.inescapable-softcore\` |

部署经 MO2 overlay 目录 `[5]软核大修-InescapableTarkovsSoftcore` 向目标实例投影；不直接写入游戏目录。

流程要点：

1. **部署前先做 profile 备份**（MO2 MCP `mo2_backup_profile`）。
2. 创建 overlay → 填充 `build/overlay/` 内容 → 启用 → 置于 `[5]` 组顶部。
3. 重启 SPT 服务器生效。
4. **回滚**：禁用或移除 overlay + 恢复 profile 备份。

## 来源与许可

- **新代码**：MIT。
- **数据表来源**：数值转录自旧整合包 Life in Norvinsk v0.3.2 的六件套源 mod；数据许可按移植源规格书
  口径为 MIT/NCSA（见移植源 `docs/specs/spec-v0.1.0.md` D13/D17 与 `data/MANIFEST.md`）。
  本项目为 4.1.6 线，沿革如下：源 TS（SPT 3.11）→ SPT 5.0 C# 实现（代码转录 + 数据表 + 差异表 + 单测）
  → 本 4.1.6 线（复用 5.0 代码底座，仅做 4.1.6 适配）。
- **移植源仓库**：`SamMeow-Inescapable-Tarkovs-Softcore`（SPT 5.0 线）。两目标线平行维护、互不同步；
  后续修复按需回搬。

## 状态

### 已达

- `dotnet build -c Release` 通过（0 错误）；src 范围零告警。
- 单测全绿（215 项）：配置层、G1–G7 样例、可空守卫专项断言、数据资源校验。
- 构建产物 `InescapableTarkovsSoftcore.dll` 生成。

### 待验证（需服务器启动与进游戏）

- 服务器启动日志出现各组 summary（变更计数与警告）。
- 玩家进游戏逐组验收（臂章重量与堆叠、背包尺寸与过滤、堆叠上限、藏身处建造、跳蚤入口与等级、
  商人/保险、战局时长）。
- 数据缺口告警计数（预期：背包 9 / barter 1）。
- 具体登记见 `docs/port-notes.md`。

版本自 `0.1.0` 起步。
