---
version: [5.0]
domain: server
topic: operations
title: "SPT 5.0 服务端 mod 开发实战经验（Bot 生成管线 / 容器与 Upd / 耐久弹匣 / 数据库陷阱 / 契约配置 / Harmony / 日志运维 / 数据管线）"
keywords: ["SPT5", "服务端", "Bot 生成", "GenerateInventory", "容器缓存", "Upd", "耐久", "弹匣", "items.json", "契约", "Harmony", "日志接缝", "确定性管线", "ITBS"]
summary: "ITBS（Inescapable Tarkov's Bot System）服务端三段实战沉淀（P0 基座 / spec-2 服务端平面 / Progression E+ 票 01–18）：GenerateInventory 分段覆写与 ClearBotContainerCacheAfterGeneration 缓存语义（Postfix 材料化头号陷阱）；Item.Upd 生成路径与裸插后果；耐久读取域（Default/Pmc/BotDurabilities）与 GetDurabilityRole 透传；弹匣/转轮/装填方法族与 ReloadMode 映射；items.json 命名陷阱（armorClass 假判据、甲片槽清单、mod_equipment 语义）；QuestStatus.QId 十六进制死链与名称解析；ExtensionData 双写与单源读取；1e400→+∞ 与 int? 缺字段；服务端 Harmony（protected 解析、__state、fail-closed/fail-soft 分层、委派保真）；Preload vs PostLoad 配置写入窗口；多 mod 拓扑与零依赖契约；确定性数据管线（SHA 三方守卫、解锁式 tier、价格带）；日志接缝（UTC 文件名、overwrite 树、最新文件判据）与审查/提交纪律"
---

# SPT 5.0 服务端 mod 开发实战经验（Bot 生成 / 容器与 Upd / 耐久弹匣 / 数据库陷阱 / 契约配置 / Harmony / 日志运维 / 数据管线）

> 适用：[5.0] | 来源：ITBS（Inescapable Tarkov's Bot System）服务端三段实战——P0 基座（2026-09-27/28）、spec-2 服务端平面（2026-10-01/02）、Progression E+ 包（票 01–18，2026-10-04/05）；均经双轴审查，实机未证项已注明。
> 关联：`curated/api-notes-5.0/`、`curated/modding-standard/`、`curated/operations/5xx-client-mod-dev-lessons.md`（补丁面先读 §16/§12）、`curated/operations/5xx-source-verification.md`
> 完整实现与证据：`E:\云文件\GitHub\SamMeow-Inescapable-Tarkovs-Bot-System`（`.scratch/`：`p0-foundation-proof` / `spec-2-server-plane` / `progression-e-plus` 的 issues 与 evidence）

## 1. 服务端 mod 形态与多 mod 拓扑

- **mod 目录与装载**：一个 mod 目录 = net10.0 程序集 + 恰好一个 `IModMetadata` 实现；目录顶层多个 DLL 会被**同时加载**（契约库目录可放 DLL + 元数据壳）。装载经 `ModValidator`；`[Injectable]` + `OnLoadOrder` 参与排序。
- **多 mod 拆分模式**：契约库（单副本，被各消费者共享）+ 多个消费者；消费者经 `ModDependencies` 显式声明依赖（暴露加载顺序与缺失）。契约程序集保持**零依赖**（不引 SPTarkov / STJ）。
- **双工程模式**：`X`（SPT 薄壳）+ `X.Core`（纯逻辑，可 mock 单测）；契约模型与验证器分属不同程序集。
- **跨 mod DI 面**：经契约接口暴露服务并注册为 Singleton（`IServiceRegistry` 模式），供其它 mod 消费。
- **启动验收信号**：核对装载行否定信号（`[Critical]` / 「did you implement IModMetadata」提示 / 各 `event=*.load` 装载行）+ 路径字段（`contractsPath=`）直证单副本装配位置。
- **版本单一事实源**：版本号集中于 `Directory.Build.props`；跨工程一致性 + 依赖范围常量化用**守卫测试**锁定。

## 2. HTTP 面：请求体 DeShuffle 与响应压缩

- **请求体不可用明文体**：`SptHttpListener` 对非豁免路径请求体做 `DeShuffleAsync` → POST/PUT 明文体不可依赖；自建端点一律 **GET + 路径参数（base64url）**。
- 5.0 默认 **zlib 压缩响应**：读取侧发 `responsecompressed: 0` 取明文；否则解析失败（客户端握手 `parse-error` 的真根因）。
- **同 URL 只读观察**：非流式路由读 `output` 原样返回、不改变响应；**流式路由**（如 `/client/locations`）必须用 Router 事件钩子（`OnBeforeAction`），禁止注册同 URL `StaticRouter`（空串会**清空流式响应**）。
- **同步协议模式**：server → client 单向拉取，按域单文档 + `schemaVersion` + 内容哈希（SHA-256 小写 hex）；**「未知域」与「已知无内容」二分**（`unknown-domain` vs `empty-domain`），不要混为一类。
- **回环判定**用 `Uri.IsLoopback`——`Uri.Host` 对 IPv6 会归一为全展开形，`::1` 直接比较会失配。
- **大小写严格解析**：wire 字段名须精确（PascalCase 上报会静默变空报告）；解析异常 catch 应放宽为防御性超集（含 `JsonException` 包装链）。

## 3. Raid 上下文与存档访问

> 来源：spec-2 票 03/06。

- **上下文最小集与 fail-open**：`mapId` / `isNight` / `playerLevels(avg/max)` / `freshProfile`；缺失走 fallback warn 而非抛错（`isNight` 缺 → false）。
- **新档判定复用既有语义**：`ProfileInfo.IsWiped ?? false`（与 APBS 同源），勿自建判定。
- **跨线程诊断读写有竞态**：单锁快照 + `volatile` 字段（两次分别加锁会错配）。
- **边界与校验常量单源**：等级夹取 `1..79` 与校验器常量共用一个来源 + 守卫测试（`TierSeedDriftTests` 模式）。
- **等级双源优先级**：生成细节里的 `PlayerLevel` 为单机权威，raid 上下文是兜底；两者皆缺 → fallback warn（level=1）。

## 4. 契约与 schema 纪律

> 来源：spec-2 票 02/04/05（`ADR-0002`）。

- **additive-only**：新接口 / DTO 只增不改，不 bump 主版本；`ContractVersion` 为保留位、不参与协商。
- **零依赖线格式**：契约程序集不引 STJ——用 `IsExternalInit` 垫片 + camelCase 策略映射；服务端 shell DTO 用 `[JsonPropertyName]` 显式钉名。
- **STJ 静默缺口**：get-only `readonly struct` 回填会静默缺失 → 注册自定义 converter 并保证**两端对称实现**（`{"major":m,"minor":n}` 形态）。
- **golden 锁线格式**：迁移须补「反序列化迁移前字面 JSON → **逐字相等**」断言（样例 = 实机 curl 原文）。
- **职责分家**：schema 模型在 Contracts、验证器与默认值在服务端 Core；重名类及时改名（`*Validator`）消除歧义。
- 域 ID 用裸 `string` 可接受（记录在案）；跨端 wire 工具 / 路由常量**两端同源**（`ServerLinkRoutes`），以交叉引用注释纪律维持。

## 5. 配置系统与写入窗口

> 来源：spec-2 票 06/07 + E+ 票 01/05/13。

- **写入窗口二分**：策略值走 **Preload**（SPT `PostDbLoad` 落地前）；`LocationBase` 直写走 **PostLoad**（后写覆盖）——写错窗口会被 SPT 覆盖或抢读。
- **全局开关有他图副作用**：单图 / 单目标包**不写全局** `LocationConfig` 开关（`RogueLighthouse` / `AddOpenZones` / `AddCustomBotWaves` 等），替换语义走 PostLoad 后写覆盖。
- **「无意见 = 保留」**：用可空字段表达（`waves=null`），校验器把 null / 空视为「无意见」而非错误。
- **安装脚本保配置**：`pack.ps1 -Install` 保留既有 `config.json`；MO2 运行中按设计跳过安装（直装可用 `File.Copy` 绕通配符坑）。
- **能力门设计**：主门 + 各能力独立门、默认关；**门关 = 零读盘**、门开 + 默认 = 零行为；`enabled` 语义向后兼容。
- **Preload 写 BotConfig**：常量集（`DisableLootOnBotTypes` 幂等 `Add`）与武器耐久（§10）——懒解析 / fail-soft / 一次性 Critical / 幂等。
- 热切换探针全默认关 + `Unload` 清理；[WARN] SPT 5.0 配置解析链依赖编译期 `BuildType`，缺文件时启动崩点可能在 `try` 之外。

## 6. Spawn 数据面与服务端编排（spec-2）

> 来源：spec-2 票 07/08/09。

- **两套键空间勿混用**：`GetMappedKey("bigmap")` 返回属性键 `"Bigmap"`，而 schema `MapId="bigmap"` 按序数精确匹配——混用会致 `spawn.postload.miss reason=no-profile` 并**连带跳过后续决策与 verify**。修复 = 键空间分离 + `preserve` / `VerifyCap` 恒执行。
- **PostLoad 写入后必须读回 verify**（`event=spawn.verify ... ok=true`）证生效，不靠「写了就算」。
- **上限单源**：`SpawnState.Schema` / `SpawnCapResolver` 统一解析，勿同 mod 内双源。
- **枚举解析失败显式回退到确定值**：实例——注释声称的回退与实际死代码不符（TryParse 失败实回退 `marksman`）；显式 `EnumFallback.Resolve`。
- **late-start 补偿带 delegation-safe 分支**：门关或无作者数据 → 委托 vanilla；有作者数据才重建（elapsed 丢弃 / 平移）。
- **数据面无时间字段会致补偿空转**：`SpawnWave` 增可空 `TimeMin` / `TimeMax`（映射 null→-1、校验 ≥ -1）。
- **进图密度定性观察与数值证据分离归因**：preserve 零数值变化时应归因 modpack / 季候等其他来源。

## 7. Bot 生成管线与库存分段覆写

> 来源：E+ 票 07/08/10/15（`GenerateInventoryPatch`）。

- **覆写点**：`BotInventoryGenerator.GenerateInventory`；票面原定的 `Prefix(false)` 全替换经双轴裁定「有条件可接受」，落地为 **Postfix 分段覆写**（避免同建 base / 武器 / 战利品段的风险）。
- **Postfix 内段序（实测）**：栈随机化 → 装备段 → 武器段（含弹匣）→ 武器 mods → 装备 mods → 战利品池抽取 / 材料化。**材料化必须晚于装备 / 武器 / 配件替换**——曾因先材料化导致落位物品被同帧子树删除且日志误计成功。
- **守卫三判据**：角色在 `Equipment.Roles` 键表、通道含 `itbs.tier`、该 tier 含对应分节；任一不满足 → 交还 vanilla（全链实际仅 PMC 可达）。
- **签名限制**：`GenerateInventory` 签名不含 bot → Postfix 无 `BotBase` 引用；tier 回写只能写 `botGenerationDetails`（§15）。
- **挂点选择依据**：17 槽序 + 三类护甲回退链 `ArmorVest` → `TacticalVest` → `ArmouredRig`（后者必须落到 SPT 的 `TacticalVest` 槽）。
- **武器槽语义**：`FirstPrimaryWeapon` 长 / 短程合并同一池（设计事实）；`SecondPrimaryWeapon` / `Holster` / `Scabbard` 可能整体缺池（须存在性守卫）。
- [UNSTABLE] vanilla `GenerateInventory` 内部段序（equipment→modsForEquipment→weapons→magazines→loot）未实测——勿引用为事实。

## 8. Bot 容器缓存语义（Postfix 材料化头号陷阱）

> 来源：E+ 票 15（ora-3 硬违规复盘）。

- **默认与时机**：`ClearBotContainerCacheAfterGeneration` 默认 `True`；vanilla 在 `GenerateInventory` **末尾清缓存**——Postfix 运行时缓存已清，且容器字典 `TryGetValue` 失败后**不重建** → 四容器全 `NO_CONTAINERS`、材料化恒失败（头号功能空转，日志不炸）。
- **修复模式**：`Prefix` 置 `botGenerationDetails.ClearBotContainerCacheAfterGeneration = false`（缓存存活到 Postfix）+ Postfix `finally` 自行 `ClearCache`（维持 vanilla 卫生）。
- [UNSTABLE] 全程序集唯一写者 `GeneratePlayerScav`（置 false）——工单未载，仅代码注释可确认。
- **材料化落位**：逐容器 `TryAddItemToBotContainer`，首个 `ItemAddedResult.SUCCESS` 即止；容器优先级 `Pockets` → `TacticalVest` → `ArmorVest` → `Backpack`。
- **失败 reason 细分**（禁止 `catch {}` 吞并）：`tpl-missing` / `no-container` / `no-space` / `incompatible` / `error` / `partial`。
- **材料化 `Upd`**：须走完整 `GenerateUpd` 路径 + 按 `StackMaxSize` 收窄 + 超量切分（`LootStackChunker`）。
- [待实机] 容器键匹配与缓存 flag 链路（`Cache(botId)`）为本波登记复核项。

## 9. Item.Upd 生成路径与「裸插」后果

> 来源：E+ 票 07（Q1 硬违规）/ 票 15。

- **裸插物品不会自动获得 `Upd`**：vanilla `GenerateExtraPropertiesForItem` 路径不覆盖手动新增的物品 → 表现为无耐久、无堆叠、无折叠 / 手电 / NVG / 面罩属性；护甲耐久补丁对新装备**静默失效**。
- **修复 = 注入 `Upd`**（`GenerateUpd` 等价于 `GenerateExtraPropertiesForItem` 路径）——`GetRandomizedMaxArmorDurability` 只经该路径被调用，缺 `Upd` 即耐久逻辑不触发。
- 医疗物品 `Upd` 不能只写 `StackObjectsCount`（不完整）——须完整 `GenerateUpd` + `StackMaxSize` 收窄 / 切分。

## 10. 护甲 / 武器耐久读取域与写策略

> 来源：E+ 票 03 / 票 13。

- **护甲目标签名（5.0 实测）**：`DurabilityLimitsHelper.GetRandomizedMaxArmorDurability(TemplateItem?, string?)` → `Prefix(false)` 全替换。
- **武器读取域**：`GetRandomizedMaxWeaponDurability` 按 `Default` / `Pmc` / `BotDurabilities[role].Weapon` 分域读；`GetRandomizedWeaponDurability` 读 `MinDelta` / `MaxDelta` / `MinLimitPercent`。
- **未匹配角色键不落 default**：vanilla `GetDurabilityRole` 原样返回自身键并读自身 `BotDurabilities[key]`（如 `zombie` / `gifter` / `sectantoni` 等事件键）→ **保持 vanilla**；「Unknown → default」的常见假设被 IL 实测推翻。
- **分类顺序**：`Follower` 必须先于 `Boss` 判定（覆盖 `bossBoarsniper` 类随从）。
- **角色清单耦合警示**：分类器基于角色名清单——SPT `bot.json` 的 `BotDurabilities` 新增键而未收录会**静默漏配**（升级 SPT 时须全扫键表补映射）。
- **写策略二分**：护甲**不写** `BotConfig.Durability`（计算直入补丁，避免全局副作用）；武器**必须写**（vanilla 读配置）——经 Preload + 懒解析 + 幂等。
- 区间夹取 `1..100`；武器 `MinLimitPercent` 为 `double`、写入无取整。
- APBS 参照默认（可直接复用）：护甲 PMC 95–100 / Boss·Follower·Special 90–100 / Scav 70–90；武器 PMC 95–100·Δ0–5·下限 90、Boss 90–100·Δ0–5·85、Follower 90–100·Δ0–15·75、Scav 70–90·Δ5–20·50、Special 90–100·Δ0–10·80。
- [UNSTABLE] 护甲百分比路径仅 PMC 消费百分比、其余角色返满耐久（注释可确认，工单未载）。

## 11. 弹匣 / 转轮 / 装填

> 来源：E+ 票 08 / 票 09。

- **转轮判定**：取弹匣父类别名交 `BotWeaponGeneratorHelper.MagazineIsCylinderRelated`；父名 ∈ `{CylinderMagazine, SpringDrivenCylinder}` 为转轮。
- **可落地弹匣**：内弹匣 / 转轮计划必须有非空 `MagazineTpl`（`null` 会被壳层丢弃且日志失真；本 DB 有 23 + 15 型无弹匣）——`DefMagType` 必须记录供装填落地。
- **装填族**：有弹 `CreateMagazineWithAmmo`、无弹挂空弹匣（不整只丢弃）；上膛 `AddCartridgeToChamber`（首个 `Chambers` 槽、`StackObjectsCount=1`）。
- **`ReloadMode` → 4 策略映射**（优先级 0 / 10 / 20 / 99；外弹匣兜底 `CanHandle` 恒 true）；`ExternalMagazineWithInternalReloadSupport` 与 `OnlyBarrel` 在本 DB 为 0 实例（死分支）。
- `PickMagazine` 须显式返回 null（不静默回退）；`mod_magazine` 数据保留但消费侧排除（由 WeaponGen 拥有，避免重复挂载）。

## 12. items.json 命名陷阱与槽面推导（勿凭命名假设）

> 来源：E+ 票 02（M1）/ 票 14（AB1/AB3）+ `SCHEMA-NOTES.md` §2。

- **Vest 判据**：本 DB **全部 91 件 Vest 都带 `armorClass` 且值恒 0**——以「`armorClass` 存在性」判装甲会全数失实（502 条误入 `ArmouredRig`、`TacticalVest` 永不产出）。**正确判据** = `_props.ArmorType != "None"`（缺失视为 `None`）；实测分布 `Light` 44 / `None` 40 / `Heavy` 7；`BlocksArmorVest` 与本判据不一致、不采用。
- **装备 mod 槽判据** = 收模板 `Slots[]` 中**所有带非空 `filters[].Filter` 的槽**，不限 `mod_*` 前缀（仅按 `mod_*` 会滤掉板甲 / 软甲 / 头盔槽 262 处）；武器侧另按 `mod_*` 收窄（`weaponOnly: true`）。
- **槽命名清单（大小写混用，逐字）**：硬甲片 `Front_plate` / `Back_plate` / `Left_side_plate` / `Right_side_plate`；软甲 `Soft_armor_front` / `Soft_armor_back` / `Soft_armor_left` / `soft_armor_right` / `Collar` / `Groin` / `Groin_back` / `Shoulder_l` / `Shoulder_r`；装备件 / 头盔附件 `mod_equipment` / `mod_equipment_000..002` / `mod_mount` / `mod_nvg`；头盔壳体 `Helmet_top` / `Helmet_back` / `Helmet_ears` / `helmet_eyes` / `helmet_jaw`。
- **`mod_equipment*` = 头盔甲 / 装备件，不是甲片**；DB **不存在 `mod_plate`**——「甲片槽 = `mod_equipment*`」系样本误判（Headwear）。
- **槽全集锁定**：`EquipmentModSlots.cs`（24 项、大小写不敏感）+ 回归测试 `ShippedArtifact_EquipmentModSlotsMatchCanonical`。
- **类别树解析**：沿 `_parent` 上溯至 `_type == "Node"` 类别节点、按节点 `Name` 解析（不硬编码 tpl）；武器分类 = 祖先含 `Weapon` 且 `weapClass` 存在 → `Equipment`（`pistol` → `Holster`，否则 `FirstPrimaryWeapon`）；装备槽映射表 `Armor`→`ArmorVest` / `Visors`→`Eyewear` / `Headphones`→`Earpiece` 等。
- **装备 mod 抽取优先级**：`ModSlotPriority.EquipmentCanonical`（硬甲片 → 软甲 → 领 / 裆 / 肩 → 装备件 → 头盔壳体 → 头盔附件）。

## 13. 数据面事实（价格 / 稀有度 / 外观源 / 战利品类别）

> 来源：`SCHEMA-NOTES.md` §2b/§2e/§4 + E+ 票 17。

- **价格**：`SPT_Data/database/templates/handbook.json` 的 `Items[].Id → Price`（卢布）；无价格（≤0）跳过。
- **稀有度** `RarityPvE`：`Common` / `Rare` / `Superrare` / 缺失或 `Not_exist`；权重 1.0 / 0.5 / 0.25 / 1.0，未知视为放行。
- **外观源** `customization.json`：`_props.Side` 含 `Usec` / `Bear`（`Savage` 跳过）；`_props.BodyPart` ∈ `Body` / `Head` / `Feet` / `Hands`；DB 无季节字段 → 固定 `all`；`_props.Body` + `_props.Hands` 齐备者构成 `bodyHands` 套件。
- **战利品 8 类别**：`ammo` / `attachments` / `materials` / `meds` / `provisions` / `repair` / `throwables` / `valuables`；类别祖先节点表 `LootCategoryRules.NodeNamesByCategory`（meds: `Meds`/`Medical`/`MedicalSupplies`/`MedKit`/`Drugs`/`Stimulator`；provisions: `Food`/`Drink`/`FoodDrink`；repair: `RepairKits`/`PlantingKits`；materials: `BuildingMaterial`/`HouseholdGoods`/`Electronics`/`Battery`/`Lubricant`/`Fuel`/`Multitools`/`Tool`；valuables: `BarterItem`/`Jewelry`/`Info`/`Map`/`Key`/`KeyMechanical`/`Keycard`/`SpecItem`/`Tapes`/`Notes`/`Flyer`/`RadioTransmitter`；throwables: `ThrowWeap`/`VolumetricThrowWeapon`）。

## 14. 任务数据通道（QId 死链教训）

> 来源：E+ 票 15（ora-4 任务通道死链复盘）。

- **`QuestStatus.QId` 是十六进制 id，不可能含任务关键字**（字母仅 a–f）——用其做关键字子串匹配（如 `fishing`）**恒不命中**；合成 id 单测会掩盖该问题（须用真实档案数据验证）。
- **正确解法**：`pmc.Quests` 过滤 `quest.Status == QuestStatusEnum.Started` → `QuestHelper.GetQuestNameFromLocale(id)` 取名 → 两遍匹配（名称 + 显式标志）→ 写名称（含关键字）或显式标志。
- **单源写入**：`TaskExtensionWriter`（键 `itbs.task`）；时序 = `GenerateInventory` Postfix 内、战利品段读取之前；依赖缺（`ProfileHelper.GetPmcProfile` / `QuestHelper`）→ 一次性降级日志、通道不写。
- 按 `session` 记忆化（`_taskChannelBySession`）；跨任务 stale 为有界取舍。

## 15. ExtensionData 通道（双写与单源读取）

> 来源：E+ 票 01/10/15。

- **双写位置**：等级补丁同时写 `botGenerationDetails.ExtensionData` 与 `bot.Info.ExtensionData`；`GenerateInventory` Postfix 无 bot 引用 → 只写 details。
- **统一读取序（单源）**：先 details、再兜底 `bot.Info`（`TierDataLookup.TryResolveTier` 模式）；下游消费**不得裸读**。
- **键单源**：`itbs.tier`（`TierExtensionWriter.TierKey`）为唯一跨补丁 Tier 键——对照上游 APBS 裸 `Tier` 键，勿假设。
- **写入时序影响下游**：Poverty 降档在装备 / 武器 / 配件 / 战利品段读取**前**把有效 tier 回写 → 下游段可见降档后 tier（因签名限制只到 details）。
- 访问器 `TryGetExtensionData(out Dictionary<string, object>? data)` 须对 out 值空判（字典可能为空，勿依赖返回值——1.3+ 起读取即创建）。

## 16. JSON / 数值陷阱

> 来源：E+ 票 01（F1/F6）/ 票 15 / 票 17。

- **`1e400` 反序列化为 `double.PositiveInfinity` 且不抛错**——声明「有限数」必须用 `double.IsFinite` 真正兑现（补全 Inf 路径用例）。
- **缺字段 vs 默认值**：用 `int?` + 必填判定显式区分（缺 `tier` 静默默认 0 是真实事故类别）。
- **NaN 路径**：`Clamp01` 对 NaN 得 0 → 应 NaN 跳过 + 告警。
- **nullable 逐字段合并**（「无意见」= 不改），不可整条覆写（`LootItemResourceRandomization`）。
- **计数单位统一「条目」**（chunk 仅内部计数）——混用会致 `materialSkipped` 负值、reason 误判、告警被抑制。
- 权重须 > 0 且有限；价格带边界严格递增（不变量断言）。
- **确定性要求**：组内按 tpl ordinal、键按 ordinal、角色按声明序——输入乱序不得影响输出；抽取条目恒 `Count=1`、不参与栈随机化。

## 17. 服务端 Harmony 实践（托管面）

> 来源：E+ 票 03/06/07/09/13/14/15 + spec-2 票 06/08。

- **protected / 非公开目标解析**：`GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)` 显式解析（例：`HandlePostRaidPlayerScavAsync`）。
- **Postfix 按名增参**：`__state` 在 Prefix → Postfix 传元数据；`ref string role`（目标原参）、`ref BotBaseInventory __result`（改写结果）均按名绑定。
- **fail-closed / fail-soft 分层**：目标解析失败 → **fail-closed**（拒载 + 可读日志）；补丁体异常 → **fail-soft**（一次性 Critical，不崩）。统一骨架 `TryEnablePatch` + `IResolvablePatch`（`ResolveTarget`），禁止骨架克隆。
- **一次性日志守卫**：`Interlocked.Exchange(ref _errorLogged, 1) == 0`。
- **冗余补丁先 IL 实证再删**：vanilla `PlayerScavGenerator.Generate` 已自行清缓存 → 自建清缓存块冗余（实例）。
- **兼容判定复用 SPT 助手**：`BotGeneratorHelper.IsItemIncompatibleWithCurrentItems(items, MongoId, slot).Incompatible`（勿自写槽兼容逻辑）。
- **委派保真**：vanilla 已有 override 分支时，补丁在条件满足处直接 `return true` 让 vanilla 全程处理（零复刻风险，替代「自行实现区间抽取」）。
- **守卫闭包性**：接管前普扫**无 flag 守卫的调用路径**——实例：`SeasonalEventSettings.ReplaceBotHostility` 经 `EnableSummoning` 守卫后仍被无条件调用 → 须补 Prefix 定向阻断。
- **PatchManager 所有权兜底**（5.0 争议项）：托管启用 → 校验 `IsActive` → 未生效回退直连 `Enable()`（单路径兜底 + 日志确认实际生效路径）。
- **上线前签名核对**：补丁目标签名用**运行时 DLL 反射**核对可绑定（不是快照 / 反编译）。
- **触点纪律**：IL2CPP 触点隔离在入口层与 `Interop/` 目录；`Perf/` 只放纯托管逻辑。
- 多补丁各自独立 `try` / `catch`；**补丁面设计前先读客户端 KB §16 / §12**（结构体封送 / Nullable 参数）。

## 18. 黑名单 / 池 / 过滤

> 来源：E+ 票 05/16/18。

- **保护最后一项**：过滤后为空则保留最后一项（防清空池）；「池仅 1 项且被黑名单」时**保护语义优先**（carve-out）。
- **过滤层位**：候选池层（抽取 / 兼容 / 回退之前），不改数据快照；保序保权重建。
- **类↔池映射**：武器池 → `Weapon` / `Equipment`；弹匣候选 → `Attachment`（**不是** `Ammo`）；弹药 → `Ammo`；武器 / 装备 mod → `Attachment`；外观 → `Clothing`。
- tier **1-based**（1..7）；tier0 永久不可筛（成文）。
- 成员匹配统一 `Trim`（空白条目会绕过联动约束）；门关形态 `Empty` 不可污染（只读快照 + `ReadOnlyCollection`）；holder 注入**调用时读取**（DI 单例，规避装载顺序竞态）。
- 日志区分「保护未删」与「池空」（`blacklistProtected=` / `blacklistPools=` 等族）。
- **联动受黑名单约束**：forced 部件命中黑名单 → 回退过滤后池，绝不写黑名单条目；过滤后池空 → null。

## 19. 预设导入（Presets）

> 来源：E+ 票 11。

- **结构**：`Presets/<name>/` 顶层扁平数据文档 + `manifest.json`（版本闸 + SHA-256 文件清单）；文件集 = 顶层文件排除 manifest、**精确匹配**；子目录显式拒绝。
- **校验 fail-closed**：名字白名单 / 结构 / 路径安全 / hash 篡改检出；版本闸须**上界** + 清单 ↔ 数据内部 schema **交叉核对**（仅下界不够）。
- 非法预设 → Critical + 回退内置（拒绝原因 `firstError`，截断 160）；`presetName` 空 = 内置（避免重复大文件）。
- 数据文件升级覆盖 = 出厂基线；用户定制走预设（职责分离）。

## 20. 外观替换与 Player Scav 特化

> 来源：E+ 票 04 / 票 06。

- **外观全替换**：`BotGenerator.SetBotAppearance` `Prefix(false)`（仅 PMC）；写 `Bot.Customization` 的 `Head` / `Feet` / `Body` / `Hands`；**body→hands 联动**；季节 = `SeasonalEventService.GetActiveWeatherSeason()`，数据键回退 `all`；复合键 `{faction}:{season}` / `{faction}:all`（`AppearanceKeys` 单源）。
- **Player Scav 三补丁**：① `GeneratePlayerScav`（Boss 化概率，`__state` 传 Postfix；`allowedBosses` / `sectantWarrior` 守卫 / `useBossHealth`）；② `PlayerScavGenerator.Generate` Postfix（PMC skills 经 `ICloner` 复制）；③ `HandlePostRaidPlayerScavAsync` Prefix（skills 回写 clamp 5100）。
- **双层守卫**：`BotTable.Types` 不可得 → 不 Boss 化；`allowedBosses` 在 enable 期对 `BotTable.Types` 软校验（warn + 跳）。
- 随机选取用 `MongoId` ordinal 稳定序（`Head.Last()` 枚举序不稳）。

## 21. 库存实现杂项陷阱

- **子树递归移除**：替换装备必须递归移除旧 item 子树（ParentId），否则产生孤儿。
- **栈随机化边界**：只作用既有 vanilla 物品、不作用抽取条目；`IsChildStack`（父模板 `WeapClass` / `ReloadMagType`）判定的子堆叠不参与自由随机化（防弹匣内弹药越界）。
- **父链遍历带防环 guard**（`IsUnderNode` guard 16）。
- 嵌套 mod 链（`mod_mount` → scope 递归）仅一级近似——二级槽不生成（未实现）。
- **策略分支须以真实 DB 实例验证**：合成样本会掩盖死链 / 死分支（任务通道死链、本 DB 0 实例的 `OnlyBarrel` 分支）——分支可达性用现网数据全扫确认。

## 22. 确定性数据管线（DataGen）

> 来源：E+ 票 02/04/09/17 + `SCHEMA-NOTES.md` §1–§8。

- **结构**：工具工程只读 `SPT_Data/database/templates/items.json` + `handbook.json`（绝不写游戏目录）；规则逻辑在 Core（可单测）。
- **SHA 三方守卫**：生产产物 / 工具重跑 / 规约期望值三方 SHA256 逐字节一致（示例 `9132545B…A8E`，5.74MB）；乱序等价测试防漂移。
- **tier 差异化 = 解锁式渐进覆盖**：`N = clamp(ceil(count × (tier+1) / 8), 1, count)`；低 tier = 有序前 N 条、tier7 = 全集、随 tier 单调非降。
- **池序**：appearance = tpl ordinal；mods = （handbook 价格升、tpl ordinal）→ 低 tier 前缀 = 低价、高 tier 解锁高价。
- **价格带**：装备 / 弹药 / 战利品三带独立、边界严格递增；战利品 `LootEdges` = 500 / 1,500 / 4,000 / 10,000 / 25,000 / 60,000 / 120,000。
- **空池省略**（不写空数组）；每池下限硬不变量 = 1（产物断言 `>= 1`）。
- **`chances` 派生自 `loot` 池**（等权 1.0 + 白名单），不独立生成；一致性门禁交叉校验（`BodyHands` 键 / 值 / 池）。
- **fail-closed 校验**：`schemaVersion` + 非法拒载 + 门关零读盘（`TierDataValidator`）。
- 分类逻辑 = Core 纯函数 + fixture 测试（含现网条目抽样）；分节（appearance / mods / loot / chances）由本管线自产。
- **产物体积须评估分发面**：`tier-data.json` 逐波增长至 5.74MB（外观 + mods + 战利品池 / chances）——收口时评估体积与分发成本。
- **规约文档不随 mod 分发**：`SCHEMA-NOTES.md` 等放工具目录、不得混入 `Data/`（pack 白名单核对）。
- **重复解析抽共享 helper**（Tier / Blacklist / Preset 同形态数据）+ 共享错误结果类型防漂移；非法文件处理策略（整体丢弃 vs 部分解析）须显式抉择（默认整体丢弃）。

## 23. 日志接缝与运维事实

> 来源：spec-2 票 13 + E+ 票 01/12。

- **日志文件名用 UTC 日期**（`spt<yyyyMMdd(UTC)>.log`）：本地时区 00:00–08:00（CST）命中的是**前一 UTC 日**文件——判读一律取「最新 `spt*.log`（按 LastWriteTime）」提取 `[前缀] event=` 行，**不得以本地日文件名存在性判定**。
- **MO2 / usvfs 写重定向**：运行期落盘（日志 / 预设）写入 `实例\overwrite\SPT_Runtime\user\...`；物理 runtime 树是直启基线（停更属正常）。
- 结构化日志格式：空格可切分、`event=` 前缀（便于 grep / 判读）。
- `LogOutput.log`（客户端）每次启动覆写 → 里程碑即时快照；`ErrorLog.log` 长期 0B = fail-open「不抛」承诺的实证信号。
- [WARN] 开机崩点：配置解析链依赖编译期 `BuildType`，缺文件时可能在 `try` 之外启动即崩（§5）。
- **服务端改动批量合并**：每轮一次重启（实测成本约束）。

## 24. 审查 / 提交 / 协作纪律

> 来源：全项目（P0 / spec-2 / E+ 各票 Comments 与移交档）。

- **双轴独立审查**（Standards + Spec 各一次）实际捕获类型：命名假设 bug（槽名）/ 单位混用（chunk vs 条目）/ 死链（任务通道）/ 声明 no-op（清缓存）/ 覆盖盲区（保护×联动边界）——审查不是仪式。
- **单一实现者**：同一时刻只允许一个实现者改仓库；并行按**文件域**切分写权；他人半成品致编译红：等 60s 重试、绝不改域外文件。
- **幽灵会话纪律**：跟踪丢失但进程可能续跑——以**文件活动（mtime）+ build/test** 判定死活，未证实死亡前**禁止重派**（重派 = 双写撞车）。
- **失联恢复**：长会话失联（截断 / 空转）→ 先盘点工作树（残件可能实质完整）→ `task_revive` 收尾；再失联换新会话（专席不跨会话复用）。
- **提交流程**：不主动 commit；大改前与 owner 敲定基线；大段提交用**白名单制**（仅暂存本段路径，并发无关改动保持原状）。
- **cfg 仅启动读取**：手工改 cfg 须关游戏（F12 改内存 + 文件、退出写回可能覆盖手改）。
- 测试轮合并（减少重启）；agent 不启动 MO2 / 游戏 / 服务器，仅提示 owner。

## 25. 环境与工具链事实（ITBS 实测 pin）

- pin：BepInEx `6.0.0-be.788` / Unity `2022.3.43f2` / EFT `1.1.5.0-47242` / SPT `5.0.0 build 47242` / .NET SDK `10.0.300`；反编译 `ilspycmd`。
- 启动链 `sptvfsbridge.bat` → `SPT.Server.exe` → `SPT.Launcher.exe`；**直启 `EscapeFromTarkov.exe` 被拒**（须经 Launcher）——「无服务器进游戏」在此 fork 不可达。
- Runtime Bridge 只读端点：`http://127.0.0.1:49777` → `/raid/status`、`/raid/bots[?detail=1]`、`/raid/events`、`/logs/recent`。
- LSP 告警为陈旧噪声——构建 / 测试以 `dotnet build/test` 为权威。
- 云同步目录 mtime 失真——文件新旧判读以 git / dotnet 为准。
- 长会话内存：opencode 会话本体为 OOM 大户（committed 44GB 观测）——游戏闪退归因前先查系统 committed / `PagedMemorySize64`。
