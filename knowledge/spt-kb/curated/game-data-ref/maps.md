---
version: [live-ref]
domain: both
topic: reference-data
source: curated
---

# 地图点位数据参考 [live-ref]

> 元信息
>
> - 快照日期：2026-09-02（tarkov.dev 全量 dump）
> - live EFT：1.1.0.1.46911
> - SPT：4.1.2（锁定 0.16.9.5.40743）
> - [live-ref] 声明：本表为 live 参考数据，点位数值与刷新概率仅供对照，SPT 侧权威以 `SPT_Data/database/locations/<图>/base.json` 及配套 loot 文件为准
> - 数据源（只读）：`knowledge/spt-kb/archive/tarkov-dev/snapshot-2026-09-02/maps.json`（17 张地图 + goonReports/mobs/lootContainers/stationaryWeapons）+ `maps_zh.json`（中文翻译字典）
> - SPT 对照源（只读）：`E:\Game\EFT_Offline\SPT_410\SPT_Runtime\SPT_Data\database\locations\`（19 个目录）

## 1. 地图总览（17 张）

> enemies 为地图敌人种类列表（scav 阵营 + boss/follower 波次）；boss 数为 `bosses` 数组长度（含多条目波次项，不代表不同 boss 种类数）。

| 中文名 | id（normalizedName） | 玩家数 players | 局时长 raidDuration | enemies | boss 数（bosses 长度） |
|---|---|---|---|---|---|
| 工厂 | `55f2d3fd4bdc2d5f408b4567`（factory） | 7-8 | 20 | scavs、bossTagilla | 1 |
| 海关 | `56f40101d2720b2a4d8b45d6`（customs） | 10-12 | 35 | scavs、bossBully、followerBully、bossKnight、followerBigPipe、followerBirdEye、bossPartisan、sectantPriest、sectantWarrior | 4 |
| 森林 | `5704e3c2d2720bac5b8b4567`（woods） | 10-14 | 35 | scavs、bossKojaniy、followerKojaniy、bossKnight、followerBigPipe、followerBirdEye、bossPartisan、sectantPriest、sectantWarrior | 4 |
| 灯塔 | `5704e4dad2720bb55b8b4567`（lighthouse） | 10-12 | 40 | scavs、bossZryachiy、followerZryachiy、bossKnight、followerBigPipe、followerBirdEye、bossPartisan、ExUsec | 10 |
| 海岸线 | `5704e554d2720bac5b8b456e`（shoreline） | 10-14 | 45 | scavs、bossSanitar、followerSanitar、bossKnight、followerBigPipe、followerBirdEye、bossPartisan、sectantPriest、sectantWarrior、Sentry | 6 |
| 储备站 | `5704e5fad2720bc05b8b4567`（reserve） | 9-11 | 40 | scavs、bossGluhar、followerGluharAssault、followerGluharSecurity、followerGluharScout、PmcBot | 5 |
| 立交桥 | `5714dbc024597771384a510d`（interchange） | 11-15 | 40 | scavs、bossKilla、bossTagilla | 2 |
| 塔科夫街区 | `5714dc692459777137212e12`（streets-of-tarkov） | 12-16 | 40 | scavs、sniper、bossBoar、followerBoar、followerBoarClose1、followerBoarClose2、bossBoarSniper、bossKolontay、followerKolontayAssault、followerKolontaySecurity | 2 |
| 夜间工厂 | `59fc81d786f774390775787e`（night-factory） | 5-6 | 25 | scavs、bossTagilla、sectantPriest、sectantWarrior | 2 |
| 实验室 | `5b0fc42d86f7744a585f9105`（the-lab） | 8-10 | 30 | PmcBot | 16 |
| 中心区 | `653e6760052c01c1c805532f`（ground-zero） | 9-10 | 30 | （无） | 0 |
| 中心区 21+ | `65b8d6f5cdde2479cb2a3125`（ground-zero-21） | 9-12 | 30 | sectantPriest、sectantWarrior | 1 |
| 码头 | `65cc8f81a9aac3e77d0cfd3e`（terminal） | 1-5 | 50 | scavs、blackDivision、vsRFSniper、vsRF、bossKilla、bossGluhar、followerGluharAssault、followerGluharSecurity、followerGluharScout、bossBully、followerBully、bossSanitar、followerSanitar、bossTagilla | 28 |
| 迷宫 | `6733700029c367a3d40b02af`（the-labyrinth） | 5-5 | 30 | bossTagillaAgro、bossKillaAgro、scavs | 1 |
| Ground Zero 教程 | `68236e8153654e8c1200798a`（ground-zero-tutorial） | 9-12 | 30 | scavs | 0 |
| 破冰船 | `69af492a4819ea4ba10a69c5`（icebreaker） | 1-3 | 50 | scavs、bossKnight、ExUsec、bossBullyBlackDiv、followerBullyBlackDiv、pmcBotBlackDiv、bossWedge | 40 |
| 实验室 (Dark) | `6a294a5b5eb5f9a1700417b7`（the-lab-dark） | 8-10 | 30 | PmcBot、pmcBotBlackDiv、bossWedgeLab、followerWedgeLab | 9 |

## 2. 各图点位统计

> 字段名以 maps.json 实际结构为准：spawns（出生点）/ extracts（撤离点，含 scav 变体与秘密撤离）/ lootContainers（容器）/ lootLoose（松散物）/ transits（转场）/ locks（锁门）/ switches（开关）/ stationaryWeapons（固定武器）/ btrStops（BTR 站点）/ hazards（危险区）。

| 地图中文名 | spawns | extracts | lootContainers | lootLoose | transits | locks | switches | stationaryWeapons | btrStops | hazards |
|---|---|---|---|---|---|---|---|---|---|---|
| 工厂 | 119 | 9 | 167 | 144 | 3 | 4 | 0 | 0 | 0 | 0 |
| 海关 | 278 | 27 | 551 | 416 | 4 | 34 | 1 | 4 | 0 | 5 |
| 森林 | 327 | 20 | 428 | 387 | 4 | 4 | 0 | 2 | 8 | 64 |
| 灯塔 | 211 | 13 | 533 | 1003 | 3 | 26 | 2 | 8 | 0 | 344 |
| 海岸线 | 282 | 14 | 756 | 611 | 3 | 38 | 0 | 1 | 0 | 23 |
| 储备站 | 167 | 11 | 991 | 766 | 3 | 33 | 3 | 8 | 0 | 4 |
| 立交桥 | 232 | 6 | 823 | 595 | 2 | 101 | 6 | 0 | 0 | 0 |
| 塔科夫街区 | 420 | 17 | 1282 | 1313 | 3 | 58 | 0 | 8 | 6 | 197 |
| 夜间工厂 | 119 | 9 | 167 | 163 | 3 | 4 | 0 | 0 | 0 | 0 |
| 实验室 | 134 | 7 | 319 | 434 | 1 | 14 | 15 | 0 | 0 | 0 |
| 中心区 | 192 | 6 | 523 | 208 | 1 | 6 | 0 | 2 | 0 | 9 |
| 中心区 21+ | 194 | 6 | 523 | 250 | 1 | 6 | 0 | 2 | 0 | 9 |
| 码头 | 109 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 迷宫 | 14 | 2 | 35 | 162 | 0 | 11 | 12 | 0 | 0 | 19 |
| Ground Zero 教程 | 17 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 破冰船 | 56 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 实验室 (Dark) | 145 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

> 注：码头/破冰船/实验室 (Dark)/Ground Zero 教程为事件或开发中内容，快照内未携带 loot/extract 数据（破冰船为事件波次图，码头为 PvE 剧情图）。

## 3. 撤离点（MapExtract）结构说明

live 的 MapExtract 实际字段（与部分资料描述的 exitStatus/zoneNames/minSurvTime 不同）：

| 字段 | 类型 | 说明 |
|---|---|---|
| id | string(40) | 撤离点唯一 id |
| name | string | 撤离点名（对应 `maps_zh.json` 中 `"<id> Name"` 键为撤离点名的本地化） |
| faction | string | 可用阵营：`pmc` / `scav` / `shared` |
| switch | boolean | 是否需要开关/电源激活 |
| switches | array | 关联开关（引用 switches 数据） |
| position | {x,y,z} | 中心坐标 |
| size | {x,y,z} | 触发体积 |
| outline | [{x,y,z}...] | 轮廓多边形顶点（4-8 个） |
| top / bottom | number | 垂直范围 |
| transferItem | {item, count} | 可选：付费撤离（V-Ex 卢布 5449016a4bdc2d6f028b456f x20000）或秘密撤离道具 |

典型示例：

```
// 工厂 Cellars（PMC 基础撤离，无需条件）
{
  "id": "ffb8b37a6390d2c3b00baeee3295492ea1e19a93",
  "name": "Cellars", "faction": "pmc", "switch": false,
  "position": {"x": 73.89, "y": -3.29, "z": -29.08},
  "size": {"x": 6.03, "y": 4.74, "z": 7.73}
}
```

```
// 海关 Dorms V-Ex（付费撤离：20,000 卢布，transferItem）
{
  "name": "Dorms V-Ex", "faction": "pmc", "switch": false,
  "transferItem": {"item": "5449016a4bdc2d6f028b456f", "count": 20000}
}
```

```
// 工厂 Gate 3（同一点位有 pmc 与 scav 两个变体条目，scav 变体独立列出）
{ "name": "Gate 3", "faction": "pmc" }
{ "name": "Gate 3", "faction": "scav" }
```

> 同名撤离点常见多阵营变体：live 按 faction 拆条（如工厂 9 条 extract 含 4 条 scav-only），秘密撤离带独立道具（如 `factory_secret_ark` 需特殊物品 x1）。

## 4. Boss 刷新（BossSpawn）结构说明

| 字段 | 类型 | 说明 |
|---|---|---|
| mob | string | 波次/首领模板名（如 bossTagilla、PmcBot、ExUsec） |
| spawnChance | number | 0~1 出现概率 |
| spawnLocations | array | 候选出生区：`{name, chance, spawnKey, positions[{x,y,z}...]}` |
| escorts | array | 护卫：`{mob, amount[{chance, count}]}` |
| supports | array | 支援波次 |
| spawnTime | number | 出生时间（秒）；-1 = 局开始即刷；>0 = 开赛后秒数 |
| spawnTimeRandom | boolean | 出生时间是否随机 |
| spawnTrigger | string/null | 触发条件（如 `"Switch"` = 拉闸触发；储备站 D2 为 `"autoId_00000_D2_LEVER"`） |
| switch / switch_id | string | 触发开关引用（id / 开关名） |

### 4.1 各图 boss 一览（地图 | boss 名 | spawnChance）

常规图（条目数 ≤ 6，逐条列出）：

| 地图 | boss（mob） | spawnChance |
|---|---|---|
| 工厂 | Tagilla（bossTagilla） | 0.35 |
| 海关 | Reshala（bossBully） | 0.60 |
| 海关 | Knight（bossKnight，Rogues 集团） | 0.20 |
| 海关 | Partisan（bossPartisan） | 0.15 |
| 海关 | 邪教徒祭司（sectantPriest） | 0.20 |
| 森林 | Shturman（bossKojaniy） | 0.60 |
| 森林 | Knight（bossKnight） | 0.20 |
| 森林 | Partisan（bossPartisan） | 0.15 |
| 森林 | 邪教徒祭司（sectantPriest） | 0.20 |
| 海岸线 | Sanitar（bossSanitar） | 0.60 |
| 海岸线 | Knight（bossKnight） | 0.20 |
| 海岸线 | Partisan（bossPartisan） | 0.15 |
| 海岸线 | 邪教徒祭司（sectantPriest） | 0.17 |
| 海岸线 | 守军（Sentry）x2 条目 | 1.00 |
| 储备站 | Glukhar（bossGluhar） | 0.35 |
| 储备站 | 掠夺者（PmcBot，局开始刷） | 0.40 |
| 储备站 | 掠夺者（PmcBot，拉闸触发 EXFIL 撤离刷） | 0.30 |
| 储备站 | 掠夺者（PmcBot，D2 拉杆触发刷） | 0.30 |
| 储备站 | 掠夺者（PmcBot，开赛 3 秒刷） | 0.30 |
| 立交桥 | Killa（bossKilla） | 0.60 |
| 立交桥 | Tagilla（bossTagilla） | 0.35 |
| 塔科夫街区 | Kaban（bossBoar） | 0.60 |
| 塔科夫街区 | Kollontay（bossKolontay） | 0.60 |
| 夜间工厂 | Tagilla（bossTagilla） | 0.60 |
| 夜间工厂 | 邪教徒祭司（sectantPriest） | 0.08 |
| 中心区 | （无 boss） | - |
| 中心区 21+ | 邪教徒祭司（sectantPriest） | 0.02 |
| 迷宫 | Shadow of Tagilla（bossTagillaAgro） | 1.00 |
| Ground Zero 教程 | （无 boss） | - |

条目较多图（按 mob 汇总，完整条目数见括号）：

| 地图 | boss（mob） | spawnChance / 条目说明 |
|---|---|---|
| 灯塔（10 条） | Zryachiy（bossZryachiy） | 1.00 x1 |
| 灯塔 | Knight（bossKnight，Rogues） | 0.20 x1 |
| 灯塔 | Partisan（bossPartisan） | 0.15 x1 |
| 灯塔 | 游荡者（ExUsec） | 0.80 x4 / 0.50 x2 / 0.20 x1（共 7 条，各处游荡者营地） |
| 实验室（16 条） | 掠夺者（PmcBot） | 0.60 x2 / 0.45 x4 / 0.40 x1 / 0.35 x9（电梯/撤离拉闸触发，spawnTime 300-1200s） |
| 码头（28 条） | 黑色军团（blackDivision） | 1.00 x10（固定波次，spawnTime 810-2845s） |
| 码头 | 俄军（vsRF / vsRFSniper） | 1.00 x7（固定波次，spawnTime 40-2100s） |
| 码头 | Killa / Glukhar / Reshala / Sanitar / Tagilla | 0.20 x5（局末 5790s 轮换） |
| 破冰船（40 条） | Knight / 游荡者 / Black Div. Boss（bossBullyBlackDiv）/ Black Div. PMC（pmcBotBlackDiv）/ The Wedge（bossWedge） | 1.00 为主（0.70 x1），spawnTime 9999（事件固定波次） |
| 实验室 (Dark)（9 条） | 掠夺者（PmcBot）/ Black Div. PMC（pmcBotBlackDiv）/ The Wedge Labs（bossWedgeLab）/ followerWedgeLab | 事件波次（0.2-1.0） |

> 实验室 16 条 PmcBot 中多条为同一撤离点开关（switch_id）的不同时间窗波次；码头/破冰船为固定时间轴波次脚本（spawnTime 明确秒数），spawnChance 基本为 1，非概率刷新。

## 5. SPT 对照（结构对照，不比对数值）

### 5.1 SPT 19 目录 vs live 17 图对应关系

| SPT 目录（Id/Name） | live 地图（normalizedName） | 说明 |
|---|---|---|
| bigmap（Customs） | customs（海关） | 对应 |
| factory4_day（Factory） | factory（工厂） | 对应 |
| factory4_night（Factory） | night-factory（夜间工厂） | 对应 |
| interchange（Interchange） | interchange（立交桥） | 对应 |
| laboratory（Laboratory） | the-lab（实验室） | 对应 |
| labyrinth（Labyrinth） | the-labyrinth（迷宫） | 对应 |
| lighthouse（Lighthouse） | lighthouse（灯塔） | 对应 |
| rezervbase（ReserveBase） | reserve（储备站） | 对应 |
| sandbox（Sandbox） | ground-zero（中心区） | 对应（Id=Sandbox） |
| sandbox_high（Sandbox） | ground-zero-21（中心区 21+） | 对应（Id=Sandbox_high） |
| shoreline（Shoreline） | shoreline（海岸线） | 对应 |
| tarkovstreets（Streets of Tarkov） | streets-of-tarkov（塔科夫街区） | 对应 |
| terminal（Terminal） | terminal（码头） | 对应（SPT 为空壳：exits=0/BossLocationSpawn=0） |
| woods（Woods） | woods（森林） | 对应 |

SPT 特有（live 快照无对应图）：

| SPT 目录 | Name | 性质 |
|---|---|---|
| develop | Arena | 竞技场（EscapeTimeLimit=60000 特殊值） |
| privatearea | Private Sector | 私人区（未开放，exits=0） |
| suburbs | Suburbs | 郊区（未开放，exits=0） |
| town | Town | 小镇（未开放，exits=0） |
| hideout | Hideout | 藏身处（exits=0） |

live 特有（SPT 4.1.2 未收录）：

| live 地图 | 说明 |
|---|---|
| icebreaker（破冰船） | 事件图，SPT 无对应目录 |
| the-lab-dark（实验室 Dark） | 实验室事件模式，SPT 无独立目录 |
| ground-zero-tutorial（Ground Zero 教程） | 教程变体，SPT 无独立目录（并入 sandbox 体系） |

### 5.2 抽样对照（工厂 factory4_day、森林 woods）

| 维度 | live 工厂 | SPT factory4_day | live 森林 | SPT woods |
|---|---|---|---|---|
| 撤离点 exits 数 | 9（extracts，含 3 scav 变体 + 1 secret） | 5 + secretExits 1（`factory_secret_ark`） | 20（含 10 scav 变体 + 1 secret） | 9 + secretExits 1（`woods_secret_minefield`） |
| 静态容器 | 167（lootContainers） | 166（staticContainers） | 428（lootContainers） | 429（staticContainers） |
| 松散 loot | 144（lootLoose） | 503 候选点（spawnpoints，mean=16.3/局） | 387（lootLoose） | 1689 候选点（spawnpoints，mean=162.9/局） |
| 固定武器 | 0 | 0（staticWeapons） | 2 | 2（staticWeapons） |

对照结论：

1. **静态容器高度一致**：SPT `staticContainers` 数量与 live `lootContainers` 几乎相同（工厂 166 vs 167、森林 429 vs 428），SPT 容器表基本复刻 live，可直接作为 loot mod 容器基线。
2. **撤离点口径可精确对应**：live `extracts` 按 faction 拆条，SPT `exits` + `secretExits` 与 live 的 `faction=pmc` 条目（非 secret）逐名一一对应（工厂 5+1=6、森林 9+1=10），scav-only 变体被剔除（工厂 3 条、森林 10 条）。做点位对照时按 `faction=pmc` 过滤并分离 secret 即可直接映射。
3. **松散 loot 模型不同**：live `lootLoose` 是实际点位；SPT `spawnpoints` 是概率候选池（`spawnpointCount.mean/std` 决定每局抽样数），数量远大于 live（503/144、1689/387）。SPT 侧权威配置在 `looseLoot.json` 的 `spawnpoints` + `spawnpointCount`。
4. **boss 配置口径不同**：live `bosses` 数组含多条目波次脚本（拉闸触发、定时波次），SPT 侧在 `base.json` 的 `BossLocationSpawn`（含 BossName/BossChance/BossZone 等字段）；SPT 还附 PMC 波次（pmcUSEC/pmcBEAR）与事件项（gifter/arenaFighterEvent）。

## 6. 使用建议

- SPT 的 loot/spawn 配置独立且为运行时权威：`database/locations/<图>/base.json`（exits/BossLocationSpawn/waves/limits）+ `looseLoot.json`（spawnpoints）+ `staticContainers.json`（staticContainers/staticWeapons），改 loot/刷怪 mod 一律以此为准。
- live 点位数据用途：
  - 静态容器数量级核对（live lootContainers ≈ SPT staticContainers，差异即潜在遗漏/多余容器）。
  - 撤离点/转场/锁门/开关清单作为功能点位参照（如拉闸撤离、BTR 站点、密室钥匙位）。
  - Boss 刷新语义参考（spawnChance、spawnTime、spawnTrigger 的 live 行为），但 SPT 以 `BossLocationSpawn` 独立配置为准，不直接搬数值。
  - 坐标字段（position/outline/top/bottom）可直接用于地图工具/叠加层；SPT loot 文件中坐标为 Unity 场景坐标，与 live 同源。
- 事件/未开放图（码头、破冰船、实验室 Dark、Ground Zero 教程、develop/privatearea/suburbs/town/hideout）live 侧数据缺失或 SPT 侧为空壳，开发时不要依赖这些图的点位参考。
