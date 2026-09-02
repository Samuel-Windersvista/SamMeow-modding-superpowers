---
version: [live-ref]
domain: both
topic: reference-data
source: curated
---

# 商人库存数据参考 [live-ref]

> 元信息
>
> - 快照日期：2026-09-02（tarkov.dev 全量 dump）
> - live EFT：1.1.0.1.46911
> - SPT：4.1.2（锁定 0.16.9.5.40743）
> - [live-ref] 声明：本表为 live 参考数据，商人等级/收购/报价结构仅供对照；SPT 侧权威以 `SPT_Data/database/traders/<id>/base.json` 与 `assort.json` 为准（完整报价只存在于 SPT 数据库，live dump 未打包报价数据，见第 3、4 节）
> - 数据源（只读）：`knowledge/spt-kb/archive/tarkov-dev/snapshot-2026-09-02/traders.json`（16 条目）+ `traders_zh.json`（中文名字典）+ `items_zh.json`（物品中文名，join 用）
> - SPT 对照源（只读）：`E:\Game\EFT_Offline\SPT_410\SPT_Runtime\SPT_Data\database\traders\`（12 个 id 目录）

> dump 结构说明：`traders.json` 顶层为 `{ data: { <id>: Trader, ... }, translations: ["$.data.*.name", "$.data.*.description"] }`。条目内 `name`/`description` 为占位符（如 `"<id> Nickname"`），真实中文名/简介在 `traders_zh.json`，键格式为 `"<id> Nickname"` / `"<id> Description"`。条目**不含 `cashOffers`/`barters` 字段**（报价数据未打包进快照），故总览表中报价列为 N/A。id→商人对应关系经 SPT `base.json` nickname + SPT 任务归属（quests traderId）交叉验证，可信。

## 1. 商人总览（live dump 16 条目）

> 16 条目 = 11 个与 SPT 同 id 的标准商人 + 5 个 live 特有商人（Taran / 无线电台 / Kerman / Voevoda / 幸存者，SPT 4.1.2 无对应目录）。SPT 另有 1 个 live dump 没有的商人：`6864e812`（Storyteller，见第 5 节）。levels 数 = `levels` 数组长度；Fence 特殊：level 从 0 起共 3 级，且额外携带 `reputationLevels`（14 条 Scav 声望服务表，见第 2 节）。

| 中文名 | id | 货币 currency | 折扣 discount | 等级数 levels | 重置时间 resetTime | 现金报价 cashOffers | 易物数 barters |
|---|---|---|---|---|---|---|---|
| Prapor | `54cb50c76803fa8b248b4571` | RUB | 0 | 4 | 2026-09-01T21:09:40Z | N/A | N/A |
| Therapist | `54cb57776803fa99248b456e` | RUB | 0 | 4 | 2026-09-01T19:09:28Z | N/A | N/A |
| Fence | `579dc571d53a0658a154fbec` | RUB | 0 | 3（0/1/2） | 2026-09-01T18:58:36Z | N/A | N/A |
| Skier | `58330581ace78e27b8b10cee` | RUB | 0 | 4 | 2026-09-01T19:24:41Z | N/A | N/A |
| Peacekeeper | `5935c25fb3acc3127c3d8cd9` | USD | 0 | 4 | 2026-09-01T19:00:29Z | N/A | N/A |
| Mechanic | `5a7c2eca46aef81a7ca2145d` | RUB | 0 | 4 | 2026-09-01T20:07:20Z | N/A | N/A |
| Ragman | `5ac3b934156ae10c4430e83c` | RUB | 0 | 4 | 2026-09-01T21:07:59Z | N/A | N/A |
| Jaeger | `5c0647fdd443bc2504c2d371` | RUB | 0 | 4 | 2026-09-01T18:42:25Z | N/A | N/A |
| Lightkeeper（SPT nickname: caretaker） | `638f541a29ffd1183d187f57` | RUB | 0 | 1 | 2026-09-01T19:07:54Z | N/A | N/A |
| BTR 司机 | `656f0f98d80a697f855d34b1` | RUB | 0 | 1 | 2026-09-01T18:18:54Z | N/A | N/A |
| 竞技场裁判（Ref / SPT nickname: Arena） | `6617beeaa9cfa777ca915b7c` | RUB | 0 | 4 | 2026-09-01T19:04:19Z | N/A | N/A |
| Taran（live 特有） | `68fe15910f29ba3fdbba9d54` | RUB | 0 | 1 | 2026-09-01T19:05:54Z | N/A | N/A |
| 无线电台（Radio Station，live 特有） | `68fe15990f29ba3fdbba9d55` | RUB | 0 | 1 | 2026-09-01T19:05:55Z | N/A | N/A |
| Kerman（Mr. Kerman，live 特有） | `688246518448b05efd61d461` | RUB | 0 | 1 | 2026-09-01T18:36:54Z | N/A | N/A |
| Voevoda（live 特有） | `688246958448b05efd61d462` | RUB | 0 | 1 | 2026-09-01T18:59:54Z | N/A | N/A |
| 幸存者（Survivor，live 特有） | `69e0d6cc77b63940375b9173` | RUB | 0 | 1 | 2026-09-01T19:15:54Z | N/A | N/A |

## 2. 商人等级结构说明（levels）

> 字段以 traders.json 实际结构为准，`levels` 为对象数组，每级一个条目：

| 字段 | 类型 | 含义 |
|---|---|---|
| id | string | 形如 `<traderId>-<level>`（Fence 为 `<traderId>-0` 起） |
| level | int | 等级序号（Fence 为 0/1/2） |
| requiredPlayerLevel | int | 解锁该等级所需的玩家等级 |
| requiredReputation | float | 所需商人声望（rep） |
| requiredCommerce | int | 所需累计交易额（当前快照均为 0） |
| payRate | float | 收购折价率（商人回收物品付款比例） |
| insuranceRate | float | 保险返还率（仅提供保险服务的商人有值） |
| repairCostMultiplier | float | 维修费用倍率（仅提供维修服务的商人有值，如 Prapor 3.8→3.65） |

> 抽样：Prapor（`54cb50c7`）完整等级链

| level | requiredPlayerLevel | requiredReputation | requiredCommerce | payRate | insuranceRate | repairCostMultiplier |
|---|---|---|---|---|---|---|
| 1 | 0 | 0 | 0 | 0.4 | 0.21 | 3.8 |
| 2 | 6 | 0.7 | 0 | 0.4 | 0.2 | 3.75 |
| 3 | 21 | 2.7 | 0 | 0.4 | 0.19 | 3.7 |
| 4 | 36 | 7.9 | 0 | 0.4 | 0.18 | 3.65 |

> Fence 特有：`levels` 仅 3 级（0/1/2，玩家等级恒 1、payRate 0.24），其实际"等级"由 `reputationLevels`（14 条）承载——按 `minimumReputation` 区间（-7 至 6+）给出 Scav 冷却、scav 箱时间、撤离费、跟随/装备生成几率、`priceModifier`（1.2→递减）、boss/scav 敌对性、BTR 折扣（delivery -2000 / taxi -1000 / coveringFire -10000）等服务参数。其余商人 `reputationLevels` 为空数组。

## 3. 现金报价结构说明（TraderCashOffer）

> 本快照**未打包**报价数据（`cashOffers`/`barters` 字段不存在），下述字段结构来自 tarkov.dev 公开 schema（GraphQL `TraderCashOffer` 类型），供 SPT 商人 mod 字段设计参考：

| 字段 | 类型 | 含义 |
|---|---|---|
| item | Item | 报价物品（id 引用，可 join `items_zh.json` 得中文名） |
| minTraderLevel | int | 解锁该报价所需商人等级 |
| price | float | 原价（`currency` 货币单位） |
| priceRUB | float | 折算卢布价格 |
| currency | Currency | RUB / USD / EUR |
| taskUnlock | [Task] | 需要完成的任务解锁（可为空） |
| buyLimit | int | 每次补货周期的限购数量 |

> 抽样报价示例（每商人 3 条）：**live 快照无报价，以下抽样自 SPT 4.1.2 `assort.json`**（cashOffer = `barter_scheme` 中单一货币条目；物品中文名 join `items_zh.json`；限购 = `items[].upd.BuyRestrictionMax`；等级 = `loyal_level_items`）。货币代码为 SPT 报价原币种。

| 商人 | 物品中文名 | 价格 | 限购 | 等级 |
|---|---|---|---|---|
| Prapor | PP-91 9x18PM 标准 20 发弹匣 | 1638 RUB | 10 | 1 |
| Prapor | 9x18 毫米 PM Pst | 50 RUB | 1000 | 1 |
| Prapor | AK-74 TGP-A 5.45x39 消音器 | 45660 RUB | 3 | 3 |
| Therapist | Salewa 急救包 | 37061 RUB | 5 | 2 |
| Therapist | 物品箱 | 15192.74 USD | 1 | 3 |
| Therapist | 固定夹板 | 4283 RUB | 20 | 1 |
| Fence | （SPT 无 assort，报价运行时生成） | - | - | - |
| Skier | Zenit B-3 VSS/VAL 环形基座 | 3165 RUB | 3 | 3 |
| Skier | Saiga-9 9x19 卡宾枪 | 16198 RUB | 5 | 1 |
| Skier | Zenit B-11 AKS-74U 护木 | 5974 RUB | 4 | 1 |
| Peacekeeper | 美元（货币兑换） | 136 RUB | 无限制 | 1 |
| Mechanic | Magpul AFG 战术握把（橄榄绿） | 6326 RUB | 5 | 3 |
| Mechanic | Dead Ringer Snake Eye Glock 照门 | 29.97 USD | 3 | 2 |
| Mechanic | Glock 9x19 Lone Wolf AlphaWolf Bullnose 补偿器 | 20.44 USD | 3 | 1 |
| Ragman | FORT Kiver-M 防弹头盔 | 34545 RUB | 10 | 1 |
| Ragman | WARTECH TV-109 + TV-106 胸挂（A-TACS 橄榄绿迷彩） | 15820 RUB | 4 | 1 |
| Ragman | Peltor ComTac 2 耳机（橄榄绿） | 42236 RUB | 8 | 2 |
| Jaeger | MP-133 12 铅径 750 毫米枪管 | 7416 RUB | 3 | 1 |
| Jaeger | SOK-12 聚合物护木 Sb.7-1 | 1467 RUB | 3 | 1 |
| Jaeger | MP-133 12 铅径 660 毫米枪管 | 5256 RUB | 3 | 1 |
| Ref（Arena） | （SPT assort 707 条目均为非单货币易物，无纯现金报价） | - | - | - |
| Lightkeeper / BTR / Storyteller | （SPT 无 assort.json） | - | - | - |

## 4. 已知缺口

> tarkov.dev Issue #481：**商人枪械报价严重不完整**——live 全部 171 支枪中仅 18 支存在 trader 报价（其余武器报价缺失）。因此本参考（以及任何基于 tarkov.dev 报价数据的用法）**不可当作全量事实**：live 报价集合只适合做字段/结构参考，不适合做完整性断言。SPT 侧若要武器商人报价，以 SPT `assort.json` 实际条目为准。

## 5. SPT 对照

### 5.1 SPT 12 id vs live 16 条目

> 对应关系以 SPT `base.json` 的 `_id`/`nickname` 为权威（与 SPT quests 的 `traderId` 归属一致）。注意：**任务书写的 "54cb5777/Prapor、5ac3b934/Mechanic" 有误**——任务归属铁证：`Debut`→54cb50c7（Prapor）、`Shortage`/`Painkiller`→54cb5777（Therapist）、`Gunsmith - Part 1`→5a7c2eca（Mechanic）。

| SPT id | SPT nickname（base.json） | live dump | 结论 |
|---|---|---|---|
| `54cb50c76803fa8b248b4571` | Prapor（Романенко） | 有 | SAME |
| `54cb57776803fa99248b456e` | Therapist（Хабибуллина） | 有 | SAME |
| `579dc571d53a0658a154fbec` | Fence | 有 | SAME |
| `58330581ace78e27b8b10cee` | Skier（Киселёв） | 有 | SAME |
| `5935c25fb3acc3127c3d8cd9` | Peacekeeper（Пилсудский） | 有 | SAME |
| `5a7c2eca46aef81a7ca2145d` | Mechanic（Самойлов） | 有 | SAME |
| `5ac3b934156ae10c4430e83c` | Ragman（Абрамян） | 有 | SAME |
| `5c0647fdd443bc2504c2d371` | Jaeger（Егерь） | 有 | SAME |
| `638f541a29ffd1183d187f57` | caretaker（Смотритель，即 Lightkeeper） | 有 | SAME |
| `656f0f98d80a697f855d34b1` | БТР（BTR 司机） | 有 | SAME |
| `6617beeaa9cfa777ca915b7c` | Arena（竞技场裁判/Ref） | 有 | SAME |
| `6864e812f9fe664cb8b8e152` | Storyteller | **无** | SPT 特有 |
| `68fe15910f29ba3fdbba9d54`（Taran） | 无 | 有 | live 特有 |
| `68fe15990f29ba3fdbba9d55`（无线电台） | 无 | 有 | live 特有 |
| `688246518448b05efd61d461`（Kerman） | 无 | 有 | live 特有 |
| `688246958448b05efd61d462`（Voevoda） | 无 | 有 | live 特有 |
| `69e0d6cc77b63940375b9173`（幸存者） | 无 | 有 | live 特有 |

> 结论：11 个 id 完全一致（SAME）；SPT 特有 1 个（Storyteller `6864e812`）；live 特有 5 个（Taran/无线电台/Kerman/Voevoda/幸存者，均 1 级、SPT 4.1.2 无对应目录，推测为后续 live 版本新商人）。

### 5.2 抽样数量对照（SPT assort items vs live 报价总数）

> live 报价总数 = `cashOffers + barters`；本快照两个字段均不存在（= 0/无数据），故以下仅能给出 SPT 侧条目数与 live 侧"缺失"结论，不构成逐条比对。

| 商人 | SPT assort items 条目数 | SPT 现金报价数 | SPT 易物数 | live cashOffers+barters |
|---|---|---|---|---|
| Prapor `54cb50c7` | 951 | 357 | 63 | 无数据（dump 未打包） |
| Mechanic `5a7c2eca` | 1079 | 489 | 100 | 无数据（dump 未打包） |

> 全部 12 个 SPT 商人的 assort 概况（供上下文）：Therapist 85 items（31 现金/54 易物）、Skier 663（371/39）、Peacekeeper 1286（1/658，几乎全易物）、Ragman 440（115/53）、Jaeger 579（304/52）、Ref/Arena 707（0/167，全易物）、Fence/Lightkeeper/BTR/Storyteller 无 assort.json（0 items，报价由 SPT 运行时生成或为空）。

### 5.3 SPT base.json（4.1）字段 ↔ live Trader 字段映射

| SPT base.json 字段（4.1） | live Trader 字段 | 说明 |
|---|---|---|
| `_id` | `id` | 商人唯一 id，两者一致（24 位 hex） |
| `nickname` / `surname` / `name` | `normalizedName` / `description` | live `name`/`description` 为占位符，需翻译词典（`traders_zh.json`）还原；SPT 侧 nickname 即游戏内英文名 |
| `currency` | `currency` | 同义（RUB/USD/EUR；Peacekeeper=USD，其余 RUB） |
| `nextResupply`（unix 秒） | `resetTime`（ISO 8601） | 下次补给重置时间，格式不同、语义相同 |
| `loyaltyLevels` | `levels` | 等级解锁条件：SPT `minLevel`/`minStanding`/`minSalesSum` ↔ live `requiredPlayerLevel`/`requiredReputation`/`requiredCommerce`；SPT 额外含 `buy_price_coef`/`exchange_price_coef`/`insurance_price_coef`/`repair_price_coef` 价格系数（live 侧散落在 `payRate`/`insuranceRate`/`repairCostMultiplier`） |
| `items_buy`（`category` + `id_list`） | `buyAllowed`（`category` + `items`） | 允许收购的物品（live 用 `items` 键，SPT 用 `id_list` 键） |
| `items_buy_prohibited` | `buyProhibited` | 禁止收购的物品（键名同上差异） |
| `items_sell`（按等级 `{1..4: {category, id_list}}`） | （无直接对应） | live Trader 无出售权限表；SPT 侧按等级指定可售物品 |
| `insurance`（`availability`/`max_return_hour`/`min_payment` 等） | `levels[].insuranceRate` | SPT 为独立对象（含排除类别），live 仅等级内一列折率 |
| `repair`（`availability`/`quality`/`currency_coefficient`） | `levels[].repairCostMultiplier` | 同上，SPT 独立对象 vs live 等级内一列倍率 |
| `discount` / `discount_end` | `discount` | 一致（快照均为 0，无折扣） |
| `assort.json`：`items` + `barter_scheme` + `loyal_level_items` | `cashOffers` / `barters`（schema 概念） | SPT 侧 `barter_scheme` 单货币条目=现金报价（↔ live `TraderCashOffer`），多条目/非货币=易物（↔ live `TraderBarterOffer`）；`loyal_level_items` ↔ `minTraderLevel` |

## 6. 使用建议

1. **SPT 商人 mod 开发以 SPT traders 数据库为权威**：完整报价（assort.json）、等级与价格系数（loyaltyLevels）、收购/出售表（items_buy/items_buy_prohibited/items_sell）都在 `SPT_Data/database/traders/<id>/` 下；新增或修改商人报价直接读写该目录（遵循 MO2 overlay 规则，勿写 Stock Game）。
2. **live 结构作字段设计参考**：`Trader` 对象的 `levels`/`buyAllowed`/`buyProhibited`/`reputationLevels`（Fence 的 Scav 服务表）语义、`resetTime` 周期、`TraderCashOffer` 字段（item/minTraderLevel/price/priceRUB/currency/taskUnlock/buyLimit）可用于设计新商人数据模型；但**报价完整性不可信**（Issue #481，枪械仅 18/171 有报价）。
3. id 对应：SPT 与 live 共享 11 个商人 id（SAME），对接时直接按 id 映射；SPT 特有 Storyteller（`6864e812`）与 live 特有 5 个新商人互不对应，跨源集成时注意。
4. 中文名：live 侧商人名/简介经 `traders_zh.json` 按 `"<id> Nickname/Description"` 键取；物品名经 `items_zh.json` 按 `"<itemId> Name"` 键 join。
