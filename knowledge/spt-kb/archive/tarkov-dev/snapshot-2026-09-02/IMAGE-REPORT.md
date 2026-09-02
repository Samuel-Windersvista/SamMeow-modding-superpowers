# tarkov.dev dump 数据画像报告（快照 2026-09-02）

> 数据源：https://json.tarkov.dev（REST dump 通道，GraphQL 故障替代方案）
> 快照目录：`knowledge/spt-kb/archive/tarkov-dev/snapshot-2026-09-02/`
> 抓取脚本：`knowledge/spt-kb/archive/tarkov-dev/fetch-dump.ps1`
> SPT 对照基准：`E:\Game\EFT_Offline\SPT_410\SPT_Runtime\SPT_Data\database\`（SPT 4.1.2，锁定 0.16.9.5.40743）

## 1. 各域计数

| 域 | 数量 | 说明 |
|---|---|---|
| items | 5312 | live 全物品（含藏身处/任务物品等） |
| tasks | 517 | tasks 任务 |
| questItems | 135 | 任务物品 |
| achievements | 123 | 成就 |
| prestige | 6 | 威望系统 |
| barters | 789 | 兑换交易（无翻译） |
| crafts | 214 | 藏身处制作（无翻译） |
| hideout stations | 26 | 藏身处设施模块 |
| maps | 17 | 地图（live 方） |
| traders | 16 | 商人 |
| itemCategories | 112 | 物品分类 |
| handbookCategories | 88 | 手册分类 |
| armorMaterials | 8 | 护甲材料 |
| skills | 49 | 技能 |
| mastering | 82 | 武器掌握 |
| specialItems | 37 | 特殊物品 |

## 2. items types 分布 Top 15（按数量降序）

| # | type | 物品数 |
|---|---|---|
| 1 | mods | 2295 |
| 2 | noFlea | 1386 |
| 3 | wearable | 747 |
| 4 | preset | 484 |
| 5 | barter | 339 |
| 6 | keys | 256 |
| 7 | ammoBox | 225 |
| 8 | ammo | 212 |
| 9 | gun | 171 |
| 10 | pistolGrip | 139 |
| 11 | rig | 91 |
| 12 | provisions | 89 |
| 13 | suppressor | 85 |
| 14 | armor | 70 |
| 15 | poster | 57 |

> 全量 types 种类数：26

## 3. items properties 覆盖与 propertiesType 分布 Top 10

- 带 properties 的物品：4164 / 5312（78.4%）

| # | propertiesType | 数量 |
|---|---|---|
| 1 | ItemPropertiesWeaponMod | 1638 |
| 2 | ItemPropertiesPreset | 484 |
| 3 | ItemPropertiesKey | 256 |
| 4 | ItemPropertiesScope | 230 |
| 5 | ItemPropertiesMagazine | 224 |
| 6 | ItemPropertiesAmmo | 200 |
| 7 | ItemPropertiesBarrel | 196 |
| 8 | ItemPropertiesWeapon | 171 |
| 9 | ItemPropertiesHelmet | 109 |
| 10 | ItemPropertiesInfoContent | 92 |

> 全量 propertiesType 种类数：28

## 4. _zh 字典映射验证（抽样 10 个 item id）

> dump 中 item 的 `name` 为占位 key（形如 `<itemId> Name`），需用 `items_zh.json` 字典 join 得到中文名。验证样例：

| # | item id | 占位 name | items_zh 映射中文名 | 映射命中 |
|---|---|---|---|---|
| 1 | 5447a9cd4bdc2dbd208b4567 | 5447a9cd4bdc2dbd208b4567 Name | 柯尔特 M4A1 5.56x45 卡宾枪 | 是 |
| 2 | 5447ac644bdc2d6c208b4567 | 5447ac644bdc2d6c208b4567 Name | 5.56x45mm M855弹药包（50发装） | 是 |
| 3 | 5448ba0b4bdc2d02308b456c | 5448ba0b4bdc2d02308b456c Name | 工厂紧急出口钥匙 | 是 |
| 4 | 5448bd6b4bdc2dfc2f8b4569 | 5448bd6b4bdc2dfc2f8b4569 Name | 马卡洛夫9x18PM 手枪 | 是 |
| 5 | 5448be9a4bdc2dfd2f8b456a | 5448be9a4bdc2dfd2f8b456a Name | RGD-5手榴弹 | 是 |
| 6 | 5448c12b4bdc2d02308b456f | 5448c12b4bdc2d02308b456f Name | 90-93 9x18PM 8发PM弹匣 | 是 |
| 7 | 5448c1d04bdc2dff2f8b4569 | 5448c1d04bdc2dff2f8b4569 Name | Magpul PMAG 20 GEN M3 5.56x45 STANAG 20发弹匣 | 是 |
| 8 | 5448fee04bdc2dbc018b4567 | 5448fee04bdc2dbc018b4567 Name | 0.6升瓶装水 | 是 |
| 9 | 5448ff904bdc2d6f028b456e | 5448ff904bdc2d6f028b456e Name | 军用饼干 | 是 |
| 10 | 5449016a4bdc2d6f028b456f | 5449016a4bdc2d6f028b456f Name | 卢布 | 是 |

> 结论：10/10 命中，字典 join 路径可用。

## 5. 各翻译字典条目数

| 字典 | 条目数 |
|---|---|
| items_zh | 16437 |
| items_en | 16437 |
| tasks_zh | 3657 |
| tasks_en | 3657 |
| hideout_zh | 123 |
| hideout_en | 123 |
| maps_zh | 430 |
| maps_en | 430 |
| traders_zh | 32 |
| traders_en | 32 |

## 6. SPT 本地数据库抽样对照

### 6.1 弹药字段对照（前 20，含指定口径）

> 字段映射：damage→_props.Damage，penetrationPower→_props.PenetrationPower，armorDamage→_props.ArmorDamage，initialSpeed→_props.InitialSpeed，fragmentationChance→_props.FragmentationChance
> 弹药名取自 items_zh 字典映射；normalizedName 见括号。

| 弹药名 | live id | 字段 | live 值 | SPT 值 | 一致？ |
|---|---|---|---|---|---|
| 5.56x45毫米 M855 | 54527a984bdc2d4e668b4567 | damage | 54 | 54 | 是 |
| 5.56x45毫米 M855 | 54527a984bdc2d4e668b4567 | penetrationPower | 31 | 31 | 是 |
| 5.56x45毫米 M855 | 54527a984bdc2d4e668b4567 | armorDamage | 37 | 37 | 是 |
| 5.56x45毫米 M855 | 54527a984bdc2d4e668b4567 | initialSpeed | 922 | 922 | 是 |
| 5.56x45毫米 M855 | 54527a984bdc2d4e668b4567 | fragmentationChance | 0.5 | 0.5 | 是 |
| 9x19毫米 Pst | 56d59d3ad2720bdb418b4577 | damage | 54 | 54 | 是 |
| 9x19毫米 Pst | 56d59d3ad2720bdb418b4577 | penetrationPower | 20 | 20 | 是 |
| 9x19毫米 Pst | 56d59d3ad2720bdb418b4577 | armorDamage | 33 | 33 | 是 |
| 9x19毫米 Pst | 56d59d3ad2720bdb418b4577 | initialSpeed | 457 | 457 | 是 |
| 9x19毫米 Pst | 56d59d3ad2720bdb418b4577 | fragmentationChance | 0.15 | 0.15 | 是 |
| 7.62x39毫米 PS | 5656d7c34bdc2d9d198b4587 | damage | 61 | 61 | 是 |
| 7.62x39毫米 PS | 5656d7c34bdc2d9d198b4587 | penetrationPower | 35 | 35 | 是 |
| 7.62x39毫米 PS | 5656d7c34bdc2d9d198b4587 | armorDamage | 52 | 52 | 是 |
| 7.62x39毫米 PS | 5656d7c34bdc2d9d198b4587 | initialSpeed | 717 | 717 | 是 |
| 7.62x39毫米 PS | 5656d7c34bdc2d9d198b4587 | fragmentationChance | 0.2 | 0.2 | 是 |
| 5.45x39毫米 PS | 56dff3afd2720bba668b4567 | damage | 56 | 56 | 是 |
| 5.45x39毫米 PS | 56dff3afd2720bba668b4567 | penetrationPower | 28 | 28 | 是 |
| 5.45x39毫米 PS | 56dff3afd2720bba668b4567 | armorDamage | 40 | 40 | 是 |
| 5.45x39毫米 PS | 56dff3afd2720bba668b4567 | initialSpeed | 890 | 890 | 是 |
| 5.45x39毫米 PS | 56dff3afd2720bba668b4567 | fragmentationChance | 0.4 | 0.4 | 是 |
| 7.62x54R 7N1狙击弹 | 59e77a2386f7742ee578960a | damage | 84 | 84 | 是 |
| 7.62x54R 7N1狙击弹 | 59e77a2386f7742ee578960a | penetrationPower | 45 | 45 | 是 |
| 7.62x54R 7N1狙击弹 | 59e77a2386f7742ee578960a | armorDamage | 84 | 84 | 是 |
| 7.62x54R 7N1狙击弹 | 59e77a2386f7742ee578960a | initialSpeed | 875 | 875 | 是 |
| 7.62x54R 7N1狙击弹 | 59e77a2386f7742ee578960a | fragmentationChance | 0.083 | 0.083 | 是 |
| 12/70 5.25毫米鹿弹 | 5d6e6772a4b936088465b17c | damage | 37 | 37 | 是 |
| 12/70 5.25毫米鹿弹 | 5d6e6772a4b936088465b17c | penetrationPower | 1 | 1 | 是 |
| 12/70 5.25毫米鹿弹 | 5d6e6772a4b936088465b17c | armorDamage | 15 | 15 | 是 |
| 12/70 5.25毫米鹿弹 | 5d6e6772a4b936088465b17c | initialSpeed | 330 | 330 | 是 |
| 12/70 5.25毫米鹿弹 | 5d6e6772a4b936088465b17c | fragmentationChance | 0 | 0 | 是 |
| 12/70 Express 6.5 毫米鹿弹 | 5d6e67fba4b9361bc73bc779 | damage | 35 | 35 | 是 |
| 12/70 Express 6.5 毫米鹿弹 | 5d6e67fba4b9361bc73bc779 | penetrationPower | 3 | 3 | 是 |
| 12/70 Express 6.5 毫米鹿弹 | 5d6e67fba4b9361bc73bc779 | armorDamage | 26 | 26 | 是 |
| 12/70 Express 6.5 毫米鹿弹 | 5d6e67fba4b9361bc73bc779 | initialSpeed | 430 | 430 | 是 |
| 12/70 Express 6.5 毫米鹿弹 | 5d6e67fba4b9361bc73bc779 | fragmentationChance | 0 | 0 | 是 |
| 12/70 7毫米鹿弹 | 560d5e524bdc2d25448b4571 | damage | 39 | 39 | 是 |
| 12/70 7毫米鹿弹 | 560d5e524bdc2d25448b4571 | penetrationPower | 3 | 3 | 是 |
| 12/70 7毫米鹿弹 | 560d5e524bdc2d25448b4571 | armorDamage | 26 | 26 | 是 |
| 12/70 7毫米鹿弹 | 560d5e524bdc2d25448b4571 | initialSpeed | 415 | 415 | 是 |
| 12/70 7毫米鹿弹 | 560d5e524bdc2d25448b4571 | fragmentationChance | 0 | 0 | 是 |
| 12/70 Magnum 8.5 毫米鹿弹 | 5d6e6806a4b936088465b17e | damage | 50 | 50 | 是 |
| 12/70 Magnum 8.5 毫米鹿弹 | 5d6e6806a4b936088465b17e | penetrationPower | 2 | 2 | 是 |
| 12/70 Magnum 8.5 毫米鹿弹 | 5d6e6806a4b936088465b17e | armorDamage | 26 | 26 | 是 |
| 12/70 Magnum 8.5 毫米鹿弹 | 5d6e6806a4b936088465b17e | initialSpeed | 385 | 385 | 是 |
| 12/70 Magnum 8.5 毫米鹿弹 | 5d6e6806a4b936088465b17e | fragmentationChance | 0 | 0 | 是 |
| 12/70 AP-20 穿甲独头弹 | 5d6e68a8a4b9360b6c0d54e2 | damage | 164 | 164 | 是 |
| 12/70 AP-20 穿甲独头弹 | 5d6e68a8a4b9360b6c0d54e2 | penetrationPower | 37 | 37 | 是 |
| 12/70 AP-20 穿甲独头弹 | 5d6e68a8a4b9360b6c0d54e2 | armorDamage | 65 | 65 | 是 |
| 12/70 AP-20 穿甲独头弹 | 5d6e68a8a4b9360b6c0d54e2 | initialSpeed | 510 | 510 | 是 |
| 12/70 AP-20 穿甲独头弹 | 5d6e68a8a4b9360b6c0d54e2 | fragmentationChance | 0.03 | 0.03 | 是 |
| 12/70 Copper Sabot Premier 空尖独头弹 | 5d6e68b3a4b9361bca7e50b5 | damage | 206 | 206 | 是 |
| 12/70 Copper Sabot Premier 空尖独头弹 | 5d6e68b3a4b9361bca7e50b5 | penetrationPower | 14 | 14 | 是 |
| 12/70 Copper Sabot Premier 空尖独头弹 | 5d6e68b3a4b9361bca7e50b5 | armorDamage | 46 | 46 | 是 |
| 12/70 Copper Sabot Premier 空尖独头弹 | 5d6e68b3a4b9361bca7e50b5 | initialSpeed | 442 | 442 | 是 |
| 12/70 Copper Sabot Premier 空尖独头弹 | 5d6e68b3a4b9361bca7e50b5 | fragmentationChance | 0.38 | 0.38 | 是 |
| 12/70 Dual Sabot 独头弹 | 5d6e68dea4b9361bcc29e659 | damage | 85 | 85 | 是 |
| 12/70 Dual Sabot 独头弹 | 5d6e68dea4b9361bcc29e659 | penetrationPower | 17 | 17 | 是 |
| 12/70 Dual Sabot 独头弹 | 5d6e68dea4b9361bcc29e659 | armorDamage | 65 | 65 | 是 |
| 12/70 Dual Sabot 独头弹 | 5d6e68dea4b9361bcc29e659 | initialSpeed | 415 | 415 | 是 |
| 12/70 Dual Sabot 独头弹 | 5d6e68dea4b9361bcc29e659 | fragmentationChance | 0.1 | 0.1 | 是 |
| 12/70 箭形弹 | 5d6e6911a4b9361bd5780d52 | damage | 25 | 25 | 是 |
| 12/70 箭形弹 | 5d6e6911a4b9361bd5780d52 | penetrationPower | 31 | 31 | 是 |
| 12/70 箭形弹 | 5d6e6911a4b9361bd5780d52 | armorDamage | 26 | 26 | 是 |
| 12/70 箭形弹 | 5d6e6911a4b9361bd5780d52 | initialSpeed | 320 | 320 | 是 |
| 12/70 箭形弹 | 5d6e6911a4b9361bd5780d52 | fragmentationChance | 0 | 0 | 是 |
| 12/70 FTX Custom Lite独头弹 | 5d6e68e6a4b9361c140bcfe0 | damage | 183 | 183 | 是 |
| 12/70 FTX Custom Lite独头弹 | 5d6e68e6a4b9361c140bcfe0 | penetrationPower | 20 | 20 | 是 |
| 12/70 FTX Custom Lite独头弹 | 5d6e68e6a4b9361c140bcfe0 | armorDamage | 50 | 50 | 是 |
| 12/70 FTX Custom Lite独头弹 | 5d6e68e6a4b9361c140bcfe0 | initialSpeed | 480 | 480 | 是 |
| 12/70 FTX Custom Lite独头弹 | 5d6e68e6a4b9361c140bcfe0 | fragmentationChance | 0.1 | 0.1 | 是 |
| 12/70 Grizzly 40独头弹  | 5d6e6869a4b9361c140bcfde | damage | 190 | 190 | 是 |
| 12/70 Grizzly 40独头弹  | 5d6e6869a4b9361c140bcfde | penetrationPower | 12 | 12 | 是 |
| 12/70 Grizzly 40独头弹  | 5d6e6869a4b9361c140bcfde | armorDamage | 48 | 48 | 是 |
| 12/70 Grizzly 40独头弹  | 5d6e6869a4b9361c140bcfde | initialSpeed | 390 | 390 | 是 |
| 12/70 Grizzly 40独头弹  | 5d6e6869a4b9361c140bcfde | fragmentationChance | 0.12 | 0.12 | 是 |
| 12/70 铅头弹 | 58820d1224597753c90aeb13 | damage | 167 | 167 | 是 |
| 12/70 铅头弹 | 58820d1224597753c90aeb13 | penetrationPower | 15 | 15 | 是 |
| 12/70 铅头弹 | 58820d1224597753c90aeb13 | armorDamage | 55 | 55 | 是 |
| 12/70 铅头弹 | 58820d1224597753c90aeb13 | initialSpeed | 370 | 370 | 是 |
| 12/70 铅头弹 | 58820d1224597753c90aeb13 | fragmentationChance | 0.2 | 0.2 | 是 |
| 12/70 .50 BMG 简易独头弹 | 5d6e68c4a4b9361b93413f79 | damage | 197 | 197 | 是 |
| 12/70 .50 BMG 简易独头弹 | 5d6e68c4a4b9361b93413f79 | penetrationPower | 26 | 26 | 是 |
| 12/70 .50 BMG 简易独头弹 | 5d6e68c4a4b9361b93413f79 | armorDamage | 57 | 57 | 是 |
| 12/70 .50 BMG 简易独头弹 | 5d6e68c4a4b9361b93413f79 | initialSpeed | 410 | 410 | 是 |
| 12/70 .50 BMG 简易独头弹 | 5d6e68c4a4b9361b93413f79 | fragmentationChance | 0.05 | 0.05 | 是 |
| 12/70 “食人鱼” | 64b8ee384b75259c590fa89b | damage | 25 | 25 | 是 |
| 12/70 “食人鱼” | 64b8ee384b75259c590fa89b | penetrationPower | 24 | 24 | 是 |
| 12/70 “食人鱼” | 64b8ee384b75259c590fa89b | armorDamage | 22 | 22 | 是 |
| 12/70 “食人鱼” | 64b8ee384b75259c590fa89b | initialSpeed | 310 | 310 | 是 |
| 12/70 “食人鱼” | 64b8ee384b75259c590fa89b | fragmentationChance | 0 | 0 | 是 |
| 12/70 Poleva-3 独头弹 | 5d6e6891a4b9361bd473feea | damage | 140 | 140 | 是 |
| 12/70 Poleva-3 独头弹 | 5d6e6891a4b9361bd473feea | penetrationPower | 17 | 17 | 是 |
| 12/70 Poleva-3 独头弹 | 5d6e6891a4b9361bd473feea | armorDamage | 40 | 40 | 是 |
| 12/70 Poleva-3 独头弹 | 5d6e6891a4b9361bd473feea | initialSpeed | 410 | 410 | 是 |
| 12/70 Poleva-3 独头弹 | 5d6e6891a4b9361bd473feea | fragmentationChance | 0.2 | 0.2 | 是 |
| 12/70 Poleva-6U 独头弹 | 5d6e689ca4b9361bc8618956 | damage | 150 | 150 | 是 |
| 12/70 Poleva-6U 独头弹 | 5d6e689ca4b9361bc8618956 | penetrationPower | 20 | 20 | 是 |
| 12/70 Poleva-6U 独头弹 | 5d6e689ca4b9361bc8618956 | armorDamage | 50 | 50 | 是 |
| 12/70 Poleva-6U 独头弹 | 5d6e689ca4b9361bc8618956 | initialSpeed | 430 | 430 | 是 |
| 12/70 Poleva-6U 独头弹 | 5d6e689ca4b9361bc8618956 | fragmentationChance | 0.15 | 0.15 | 是 |

### 6.2 一致率统计

- 参与对照的字段数：100
- 一致字段数：100
- **一致率：100.0%**（100/100）
- SPT 未收录的 live 弹药：0 条

### 6.3 差异样例（前 10 条）

| 弹药名 | 字段 | live 值 | SPT 值 | 差异 |
|---|---|---|---|---|

### 6.4 弹药总数对比

- tarkov.dev items 中 type 含 `ammo` 的弹药数：**212**
- SPT items.json 中 `_parent == 5485a8684bdc2da71d8b4567`（Ammo）的弹药数：**208**
- 总数差：4（live 多 4）

> 注意：两侧 id 集合并非包含关系。live 有而 SPT Ammo 分类无：**25** 个；SPT Ammo 分类有而 live types 无 `ammo`：**21** 个。
> 主要原因：榴弹 / 气枪弹 / 信号弹 / 弹药包在两侧分类口径不同（live 的 `types` 含 `ammo`，SPT 侧 `_parent` 指向其他节点），以及 live 1.1.0 新增弹药 SPT 0.16.9.5 尚未收录。

live 有而 SPT 无（前 10，中文名见 items_zh）：

- `5448be9a4bdc2dfd2f8b456a` RGD-5手榴弹（normalizedName=rgd-5-hand-grenade）
- `5710c24ad2720bc3458b45a3` F-1手榴弹（normalizedName=f-1-hand-grenade）
- `5737292724597765e5728562` 5.45x39mm BP弹药包（120发装）（normalizedName=545x39mm-bp-gs-ammo-pack-120-pcs）
- `57372a7f24597766fe0de0c1` 5.45x39mm BP弹药包（120发装）（normalizedName=545x39mm-bp-gs-ammo-pack-120-pcs-1）
- `57372c21245977670937c6c2` 5.45x39mm BT弹药包（120发装）（normalizedName=545x39mm-bt-gs-ammo-pack-120-pcs）
- `57372c56245977685e584582` 5.45x39mm BT弹药包（120发装）（normalizedName=545x39mm-bt-gs-ammo-pack-120-pcs-1）
- `58d3db5386f77426186285a0` M67手榴弹（normalizedName=m67-hand-grenade）
- `5a0c27731526d80618476ac4` Zarya震撼手榴弹（normalizedName=zarya-stun-grenade）
- `5a2a57cfc4a2826c6e06d44a` RDG-2B烟雾弹（normalizedName=rdg-2b-smoke-grenade）
- `5e32f56fcb6d5863cc5e5ee4` VOG-17 Khattabka 简易手榴弹（normalizedName=vog-17-khattabka-improvised-hand-grenade）

SPT 有而 live 无（前 10）：

- `5943d9c186f7745a13413ac9` 破片
- `5996f6cb86f774678763a6ca` RGD-5破片手榴弹
- `5996f6d686f77467977ba6cc` MON-50 Shrapnel
- `5996f6fc86f7745e585b4de3` M67破片手榴弹
- `5cde8864d7f00c0010373be1` 12.7x108毫米B-32
- `5d2f2ab648f03550091993ca` 12.7x108毫米 BZT-44M
- `5d70e500a4b9364de70d38ce` VOG-30 30x29毫米
- `5e85aac65505fa48730d8af2` !!!DO_NOT_USE!!!23x75 毫米 切廖穆哈-7M 催泪弹
- `5ede47641cf3836a88318df1` !!!DO NOT USE!!!40x46 毫米 M716 烟雾弹
- `5f647fd3f6e4ab66c82faed6` 23x75 毫米“波浪-R”橡胶弹

### 6.5 地图对照（SPT locations 目录 vs live maps）

- live maps 数量：**17**（id + 中文名见下表）
- SPT locations 目录数量：**19**

| live map id | live 中文名 | 对应 SPT 目录 | 说明 |
|---|---|---|---|
| `55f2d3fd4bdc2d5f408b4567` | 工厂 | factory4_day |  |
| `56f40101d2720b2a4d8b45d6` | 海关 | bigmap |  |
| `5704e3c2d2720bac5b8b4567` | 森林 | woods |  |
| `5704e4dad2720bb55b8b4567` | 灯塔 | lighthouse |  |
| `5704e554d2720bac5b8b456e` | 海岸线 | shoreline |  |
| `5704e5fad2720bc05b8b4567` | 储备站 | rezervbase |  |
| `5714dbc024597771384a510d` | 立交桥 | interchange |  |
| `5714dc692459777137212e12` | 塔科夫街区 | tarkovstreets |  |
| `59fc81d786f774390775787e` | 夜间工厂 | factory4_night |  |
| `5b0fc42d86f7744a585f9105` | 实验室 | laboratory |  |
| `653e6760052c01c1c805532f` | 中心区 | sandbox |  |
| `65b8d6f5cdde2479cb2a3125` | 中心区 21+ | sandbox_high |  |
| `65cc8f81a9aac3e77d0cfd3e` | 码头 | terminal |  |
| `6733700029c367a3d40b02af` | 迷宫 | labyrinth |  |
| `68236e8153654e8c1200798a` | Ground Zero 教程 | sandbox | 教程变体，并入 sandbox |
| `69af492a4819ea4ba10a69c5` | 破冰船 | - | live 特有（SPT 0.16.9.5 未收录） |
| `6a294a5b5eb5f9a1700417b7` | 实验室 (Dark) | - | live 特有（SPT 0.16.9.5 未收录） |


**SPT 特有目录（live maps 域无对应条目）**：

- `develop`
- `hideout` - 藏身处（live 侧在 hideout 域，不在地图域）
- `privatearea`
- `suburbs`
- `town`

**live 特有地图（SPT 未收录）**：

- `69af492a4819ea4ba10a69c5`（破冰船）
- `6a294a5b5eb5f9a1700417b7`（实验室 (Dark)）

> 注：live 1.1.0 新增"破冰船"地图与"实验室 Dark"变体，SPT 0.16.9.5 尚无；town / suburbs 目录在 SPT 存在但 live maps 域 17 张中无对应条目（可能为 SPT 预留/未开放区域）。

### 6.6 翻译对照抽样（SPT ch.json vs tarkov.dev items_zh）

> ch.json key 结构为 `<itemId> Name`（与 dump 字典一致）。抽样前 3 个对照弹药：

| item id | SPT ch.json | tarkov.dev items_zh | 一致？ |
|---|---|---|---|
| 54527a984bdc2d4e668b4567 | 5.56x45毫米 M855 | 5.56x45毫米 M855 | 是 |
| 56d59d3ad2720bdb418b4577 | 9x19毫米 Pst | 9x19毫米 Pst | 是 |
| 5656d7c34bdc2d9d198b4587 | 7.62x39毫米 PS | 7.62x39毫米 PS | 是 |

## 7. 附注 / 字段出入

- dump 中 item `name`/`shortName`/`description` 为占位 key，必须 join `_zh`/`_en` 字典；纯 `name` 字段不可直接展示。
- 对照字段 live 侧位于 `properties` 内（camelCase），SPT 侧位于 `_props`（PascalCase）；`caliber` 两边枚举字符串不同（live 如 `5.56x45`，SPT 如 `Caliber556x45NATO`），未纳入严格比对。
- 命名观察：`59e77a2386f7742ee578960a`（normalizedName=`762x54mm-r-ps-gzh`）在中文翻译中为"7.62x54R 7N1狙击弹"——tarkov.dev items_zh 与 SPT ch.json 完全一致（同源官方中文），而 items_en 为 "762x54mm R PS gzh"。normalizedName 与中文名并非总是字面对应。
- live 弹药 212 个 vs SPT Ammo 分类 208 个：总数差 4，但 id 集合差异更大（live-only 26 / spt-only 21），主因是榴弹/气枪弹/信号弹/弹药包的分类口径不同 + live 1.1.0 新增弹药未进 SPT 0.16.9.5。
- 对照的 20 个弹药 100 个字段全部一致，说明 tarkov.dev 数值与 SPT（同源于游戏文件 dump）在已收录弹药上完全吻合；live 新增弹药（如 6a072080 / 68c15a03 等 1.1.0 弹药）是后续版本跟踪的重点。
