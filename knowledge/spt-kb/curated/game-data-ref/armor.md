---
version: [live-ref]
domain: both
topic: reference-data
source: curated
---

# 护甲与头盔数据参考 [live-ref]

> 元信息
>
> - 快照日期：2026-09-02（tarkov.dev 全量 dump）
> - live EFT：1.1.0.1.46911
> - SPT：4.1.2（锁定 0.16.9.5.40743）
> - [live-ref] 声明：本表为 live 参考数据，数值权威以 SPT 数据库（`SPT_Data/database/templates/items.json`）为准
> - 数据源：`knowledge/spt-kb/archive/tarkov-dev/snapshot-2026-09-02/items.json`（+ items_zh.json / items_en.json 翻译字典）

## 1. 类型覆盖说明

覆盖 propertiesType ∈ {ItemPropertiesArmor, ItemPropertiesHelmet, ItemPropertiesChestRig, ItemPropertiesArmorAttachment}：

| propertiesType | 数量 | 本表处理 |
|---|---|---|
| ItemPropertiesArmor | 47 | 护甲表 |
| ItemPropertiesChestRig | 91 | 护甲/背心表（含携板背心） |
| ItemPropertiesHelmet | 109 | 头盔表 |
| ItemPropertiesArmorAttachment | 83 | 不展开（甲挂件/面罩/插板等；types 含 `armorPlate` 的插板物品 38 条） |

> 注：types 含 `armor` 的物品共 70（= 47 ItemPropertiesArmor + 23 也标 armor 的 ChestRig）；live 1.1.0 将护甲数值拆分到 armorSlots（插板/软甲块），顶层 class/durability 为等效聚合值。

## 2. 护甲/背心表（ItemPropertiesArmor + ItemPropertiesChestRig，按 class 降序）

> zones 为 live 1.x 新 ArmorZone 格式（`Collider Type ...` / `Armor Zone Plate_...`）；armorSlots 列列出插板槽 nameId（无插板标 `-`）。

| 中文名 | id（24位） | 类型 | class | 耐久 durability | 防护部位 zones | 插板槽 armorSlots |
|---|---|---|---|---|---|---|
| 6B43 屏障-Sh 防弹衣（数码丛林迷彩） | 545cdb794bdc2d3a198b456a | 护甲 | 6 | 510 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_low, Armor Zone Plate_Granit_SSAPI_side_right_low, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type LeftUpperArm, Collider Type RightUpperArm, Collider Type Pelvis | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Shoulder_l, Shoulder_r, Groin |
| BNTI Zhuk（甲虫）防弹衣（数码丛林迷彩） | 5c0e625a86f7742d77340f62 | 护甲 | 6 | 305 | Armor Zone Plate_Korund_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar |
| FORT Redut-T5（堡垒-T5）防弹衣（烟雾迷彩） | 5ca21c6986f77479963115a7 | 护甲 | 6 | 504 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type LeftUpperArm, Collider Type RightUpperArm, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Shoulder_l, Shoulder_r, Groin, Groin_back |
| LBT-6094A Slick 插板背心（黑色） | 5e4abb5086f77406975c9342 | 护甲 | 6 | 200 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| 5.11 Hexgrid 插板背心 | 5fd4c474dd870108a754b241 | 护甲 | 6 | 100 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back | front_plate, back_plate |
| LBT 6094A Slick 插板背心（黄褐色） | 6038b4b292ec1c3103795a0b | 护甲 | 6 | 200 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| LBT 6094A Slick 插板背心（橄榄绿） | 6038b4ca92ec1c3103795a0d | 护甲 | 6 | 200 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| Crye Precision AVS 插板胸挂（Tagilla 版） | 609e860ebd219504d8507525 | 携板背心 | 6 | 170 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| NFM THOR 一体式防弹护甲 | 60a283193cb70855c43a381d | 护甲 | 6 | 466 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type LeftUpperArm, Collider Type RightUpperArm, Collider Type Pelvis | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Shoulder_l, Shoulder_r, Groin |
| Tasmanian Tiger SK 插板胸挂（黑系复合迷彩） | 628cd624459354321c4b7fa2 | 携板背心 | 6 | 110 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back | Front_plate, Back_plate |
| Stich Profi Stich Defense mod.2防弹插板胸挂（多用途迷彩） | 66b6295178bbc0200425f995 | 携板背心 | 6 | 220 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| Spiritus Systems LV-119 插板胸挂 (黑色军团 V1) | 689479a4a733b1602007e2eb | 携板背心 | 6 | 220 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| 6B45 防弹衣 | 68948a95d8f2b85fb705e2a6 | 护甲 | 6 | 374 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_low, Armor Zone Plate_Granit_SSAPI_side_right_low, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar |
| 6B45 防弹胸挂 (突击型) | 68948b118c57a8a52301d7ae | 携板背心 | 6 | 414 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_low, Armor Zone Plate_Granit_SSAPI_side_right_low, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar |
| BNTI Gzhel-K（彩瓷-K）防弹衣 | 5ab8e79e86f7742d8b372e78 | 护甲 | 5 | 259 | Armor Zone Plate_Korund_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar |
| 5.11 Tactical TacTec 插板胸挂（丛林绿） | 5b44cad286f77402a54ae7e5 | 携板背心 | 5 | 170 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| IOTV Gen4 防弹衣（全面防护型，复合迷彩） | 5b44cd8b86f774503d30cba2 | 护甲 | 5 | 398 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type LeftUpperArm, Collider Type RightUpperArm, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Shoulder_l, Shoulder_r, Groin, Groin_back |
| IOTV Gen4 防弹衣（突击型，复合迷彩） | 5b44cf1486f77431723e3d05 | 护甲 | 5 | 362 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type LeftUpperArm, Collider Type RightUpperArm | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Shoulder_l, Shoulder_r |
| IOTV Gen4 防弹衣（高机动型，复合迷彩） | 5b44d0de86f774503d30cba8 | 护甲 | 5 | 320 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin, Groin_back |
| 6B13 M 突击甲（Killa 版） | 5c0e541586f7747fa54205c9 | 护甲 | 5 | 249 | Armor Zone Plate_Korund_chest, Armor Zone Plate_6B13_back, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin |
| FORT Redut-M（堡垒-M）防弹衣 | 5ca2151486f774244a3b8d30 | 护甲 | 5 | 358 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin, Groin_back |
| Ars Arma CPC MOD.1 插板胸挂（A-TACS FG 迷彩） | 5e4ac41886f77406a511c9a8 | 携板背心 | 5 | 240 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | front_plate, back_plate, left_side_plate, right_side_plate, soft_armor_front, soft_armor_back, soft_armor_left, soft_armor_right |
| FORT Defender-2 防弹衣 | 5e9dacf986f774054d6b89f4 | 护甲 | 5 | 320 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type Pelvis | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin |
| NPP KlASS Korund-VM（刚玉-VM）防弹衣（黑色） | 5f5f41476bdad616ad46d631 | 护甲 | 5 | 310 | Armor Zone Plate_Korund_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Korund_side_left_high, Armor Zone Plate_Korund_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin, Groin_back |
| CQC 鱼鹰 MK4A 防弹胸挂（防护型，多地形迷彩） | 60a3c68c37ea821725773ef5 | 携板背心 | 5 | 272 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type LeftUpperArm, Collider Type RightUpperArm | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Shoulder_l, Shoulder_r |
| S&S Precision PlateFrame 插板胸挂（Goons 特别版） | 628b9784bcf6e2659e09b8a2 | 携板背心 | 5 | 120 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high | Front_plate, Back_plate, Left_side_plate, Right_side_plate |
| Crye Precision CPC 插板胸挂（Goons 特别版） | 628b9c7d45122232a872358f | 携板背心 | 5 | 230 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| NPP KlASS Bagariy 防弹胸挂（数码丛林迷彩） | 628d0618d1ba6e4fa07ce5a4 | 携板背心 | 5 | 232 | Armor Zone Plate_Korund_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Korund_side_left_low, Armor Zone Plate_Korund_side_right_low, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| Hexatac HPC 插板背心（黑系复合迷彩） | 63737f448b28897f2802b874 | 护甲 | 5 | 90 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back | Front_plate, Back_plate |
| Tasmanian Tiger MKIII 插板胸挂（狼棕色） | 66b6295a8ca68c6461709efa | 携板背心 | 5 | 110 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back | Front_plate, Back_plate |
| 6B13 M 突击甲（圣诞特装版） | 674d91ce6e862d5a95059ed6 | 护甲 | 5 | 249 | Armor Zone Plate_Korund_chest, Armor Zone Plate_6B13_back, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin |
| Hexatac HPC 插板背心（复合迷彩） | 67ab2eecfe82855dcc0f2af6 | 护甲 | 5 | 90 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back | Front_plate, Back_plate |
| FORT Redut-M（堡垒-M）防弹衣（韩国林地迷彩） | 67ab2f5adafe3b22670c911f | 护甲 | 5 | 358 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin, Groin_back |
| 5.11 Tactical TacTec 插板胸挂（风暴灰） | 67ab4b2d6f7ae4aa550bbcf6 | 携板背心 | 5 | 170 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| Ferro Concepts FCPC V5 插板胸挂 (黑色军团) | 689479cb47e5acd1e10be986 | 携板背心 | 5 | 198 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| Spiritus Systems LV-119 插板胸挂 (黑色军团 V2) | 689479eb30cc5ba7be00f5ff | 携板背心 | 5 | 198 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| First Spear Siege-R Optimized M.A.S.S. 插板胸挂 (黑色军团) | 68947a4be4bf255d1b0ca746 | 携板背心 | 5 | 350 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type LeftUpperArm, Collider Type RightUpperArm, Collider Type Pelvis | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Shoulder_l, Shoulder_r, Groin |
| 6B45 防弹胸挂 (通用型) | 68948ad72c87773b9f06d73f | 携板背心 | 5 | 364 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_low, Armor Zone Plate_Granit_SSAPI_side_right_low, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar |
| 6B45 防弹胸挂 (医疗型) | 68948aebd8f2b85fb705e2b0 | 携板背心 | 5 | 364 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_low, Armor Zone Plate_Granit_SSAPI_side_right_low, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar |
| NPP KlASS Korund-VM（刚玉-VM）防弹衣（虎纹迷彩） | 68a97093431252e29a02dc06 | 护甲 | 5 | 310 | Armor Zone Plate_Korund_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Korund_side_left_high, Armor Zone Plate_Korund_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin, Groin_back |
| Crye Precision JPC 插板胸挂（复合迷彩） | 693fd0e9deee848f70054999 | 携板背心 | 5 | 110 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back | Front_plate, Back_plate |
| FORT Defender-2 防弹衣（德国斑点迷彩） | 69b11935f3783ec37c03a105 | 护甲 | 5 | 320 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type Pelvis | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin |
| FORT Redut-M（堡垒-M）防弹衣（竞技场之囚） | 69cf9696b96c8e8d3e002925 | 护甲 | 5 | 358 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin, Groin_back |
| FORT Redut-M（堡垒-M）防弹衣（黑色） | 69cfef0d6242b966d40803e7 | 护甲 | 5 | 358 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin, Groin_back |
| FORT Gladiator-S（格斗-S）插板胸挂（无惧死亡） | 69d26ff4b855150a70092b8c | 携板背心 | 5 | 440 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type Pelvis, Collider Type NeckFront, Collider Type NeckBack, Collider Type LeftUpperArm, Collider Type RightUpperArm | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, Soft_armor_right, Groin, Collar, Shoulder_l, Shoulder_r |
| FORT Gladiator-S（格斗-S）轻型插板胸挂（维京） | 69d27591df93e6952a031c84 | 携板背心 | 5 | 282 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type Pelvis | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Groin |
| FORT Gladiator-S（格斗-S）轻型插板胸挂（复合迷彩） | 69d27ebeb855150a70092ba3 | 携板背心 | 5 | 270 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type SpineTop, Collider Type SpineDown, Collider Type RibcageLow, Collider Type RibcageUp | Front_plate, Back_plate, Soft_armor_back, Soft_armor_front |
| FORT Gladiator-S（格斗-S）轻型插板胸挂（复合迷彩） | 69d28cdc274c032dd804afe0 | 携板背心 | 5 | 345 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type Pelvis | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, Soft_armor_right, Groin |
| FORT Gladiator-S（格斗-S）插板胸挂（灰色） | 69d3708c8d8009073d0a9df4 | 护甲 | 5 | 345 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckBack, Collider Type NeckFront | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar |
| Spiritus Systems LV-119 插板胸挂 (黑系复合迷彩) | 69e2441a18cb3157560855ec | 携板背心 | 5 | 198 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| Crye Precision AVS 插板胸挂（丛林绿） | 544a5caa4bdc2d1a388b4568 | 携板背心 | 4 | 212 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop, Collider Type Pelvis, Collider Type RibcageLow | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Groin |
| ANA Tactical M2 插板胸挂（橄榄绿） | 5ab8dced86f774646209ec87 | 携板背心 | 4 | 206 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| 6B5-15 Zh-86 Uley 防弹胸挂（丛林迷彩） | 5c0e446786f7742013381639 | 携板背心 | 4 | 110 | Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown, Collider Type NeckBack, Collider Type NeckFront, Collider Type Pelvis | Soft_armor_front, Soft_armor_back, Collar, Groin |
| 6B13 突击甲（丛林迷彩） | 5c0e51be86f774598e797894 | 护甲 | 4 | 203 | Armor Zone Plate_Korund_chest, Armor Zone Plate_6B13_back, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin |
| 6B13 突击甲（数码丛林迷彩） | 5c0e53c886f7747fa54205c7 | 护甲 | 4 | 203 | Armor Zone Plate_Korund_chest, Armor Zone Plate_6B13_back, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis | front_plate, back_plate, soft_armor_front, soft_armor_back, soft_armor_left, soft_armor_right, Collar, Groin |
| 6B23-2 防弹衣（山地丛林迷彩） | 5c0e57ba86f7747fa141986d | 护甲 | 4 | 246 | Armor Zone Plate_Korund_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckBack, Collider Type NeckFront, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin, Groin_back |
| HighCom Trooper TFO 防弹背心（复合迷彩） | 5c0e655586f774045612eeb2 | 护甲 | 4 | 180 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| ANA Tactical M1 插板胸挂（橄榄绿） | 5c0e722886f7740458316a57 | 携板背心 | 4 | 194 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| WARTECH TV-110 插板胸挂（灰褐色） | 5c0e746986f7741453628fe5 | 携板背心 | 4 | 160 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| 6B3TM-01 防弹胸挂（卡其色） | 5d5d646386f7742797261fd9 | 携板背心 | 4 | 86 | Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type Pelvis, Collider Type PelvisBack | Soft_armor_front, Soft_armor_back, Groin, Groin_back |
| Ars Arma A18 Skanda 插板胸挂（复合迷彩） | 5d5d87f786f77427997cfaef | 携板背心 | 4 | 186 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| NFM THOR 隐蔽型强化防弹背心 | 609e8540d5c319764c2bc2e9 | 护甲 | 4 | 170 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| CQC 鱼鹰 MK4A 防弹胸挂（突击型，多地形迷彩） | 60a3c70cde5f453f634816a3 | 携板背心 | 4 | 222 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type LeftUpperArm, Collider Type RightUpperArm | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Shoulder_l, Shoulder_r |
| Eagle Industries MMAC 插板胸挂 （丛林绿） | 61bc85697113f767765c7fe7 | 携板背心 | 4 | 144 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| FirstSpear Strandhogg 插板胸挂（丛林绿） | 61bcc89aef0f505f0c6cd0fc | 携板背心 | 4 | 198 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type RibcageLow | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Groin |
| ECLiPSE RBAV-AF 插板胸挂（丛林绿） | 628dc750b910320f4c27a732 | 携板背心 | 4 | 218 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type RibcageLow | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Groin |
| Shellback Tactical Banshee 插板胸挂（A-TACS AU 迷彩） | 639343fce101f4caa40a4ef3 | 携板背心 | 4 | 152 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| Interceptor OTV防弹衣（通用迷彩） | 64abd93857958b4249003418 | 护甲 | 4 | 222 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| Stich Profi V2 插板胸挂（黑色） | 66b6296d7994640992013b17 | 携板背心 | 4 | 198 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type RibcageLow | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Groin |
| HighCom Trooper TFO 防弹背心（郊狼棕） | 67ab2f94dafe3b22670c912c | 护甲 | 4 | 180 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| Crye Precision AVS 插板胸挂（复合迷彩） | 67ab49aab9c7a1e18c095686 | 携板背心 | 4 | 212 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop, Collider Type Pelvis, Collider Type RibcageLow | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Groin |
| FirstSpear Strandhogg 插板胸挂（ABU 迷彩） | 68a85ab8ef22d08bf401fa68 | 携板背心 | 4 | 198 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type RibcageLow | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Groin |
| NFM THOR 隐蔽型强化防弹背心（头眼） | 68a89146212dbbeead0d5636 | 护甲 | 4 | 170 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| Interceptor OTV 防弹衣（林地迷彩） | 68a89942431252e29a02dbf6 | 护甲 | 4 | 222 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| Stich Profi V2 插板胸挂（MARPAT 林地迷彩） | 68a99207aa809946e507c2f6 | 携板背心 | 4 | 198 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type RibcageLow | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Groin |
| ANA Tactical M2 插板胸挂（高原复合迷彩） | 69412e5573dcf473e50be464 | 携板背心 | 4 | 206 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| Stich Profi V2 插板胸挂（A-TACS FG） | 69cfff5920eac4535609a008 | 携板背心 | 4 | 198 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type Pelvis | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Groin |
| Stich Profi V2 插板胸挂（灰褐色） | 69cfff99cb69530af90a8279 | 携板背心 | 4 | 198 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type Pelvis | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Groin |
| FirstSpear Strandhogg 插板胸挂（黑系复合迷彩） | 69d36347705756116e0a901c | 携板背心 | 4 | 198 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type RibcageLow | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Groin |
| MF-UNTAR防弹背心 | 5ab8e4ed86f7742d8e50c7fa | 护甲 | 3 | 100 | Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| BNTI Kirasa-N（胸甲-N）防弹衣 | 5b44d22286f774172b0c9de8 | 护甲 | 3 | 240 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar |
| 6B5-16 Zh-86 Uley 防弹胸挂（卡其色） | 5c0e3eb886f7742015526062 | 携板背心 | 3 | 160 | Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis | Soft_armor_front, Soft_armor_back, Collar, Groin |
| 6B23-1 防弹衣（数码丛林迷彩） | 5c0e5bab86f77461f55ed1f3 | 护甲 | 3 | 206 | Armor Zone Plate_Korund_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack, Collider Type Pelvis, Collider Type PelvisBack | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar, Groin, Groin_back |
| BNTI Zhuk（甲虫）防弹衣（战地记者版） | 5c0e5edb86f77461f55ed1f7 | 护甲 | 3 | 185 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Korund_chest, Armor Zone Plate_Granit_SAPI_back, Armor Zone Plate_Granit_SSAPI_side_left_high, Armor Zone Plate_Korund_side_right_high, Armor Zone Plate_Granit_SSAPI_side_right_high, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckBack, Collider Type NeckFront | Front_plate, Back_plate, Left_side_plate, Right_side_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar |
| DRD防弹衣 | 62a09d79de7ac81993580530 | 护甲 | 3 | 120 | Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| WARTECH TV-115 插板胸挂（橄榄绿） | 64a536392d2c4e6e970f4121 | 携板背心 | 3 | 122 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageUp, Collider Type SpineTop | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| Eagle Allied Industries MBSS 插板胸挂（狼棕色） | 64a5366719bab53bd203bf33 | 携板背心 | 3 | 70 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back | Front_plate, Back_plate |
| NPP KlASS Kora-Kulon防弹衣 | 64be79c487d1510151095552 | 护甲 | 3 | 128 | Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown | Soft_armor_front, Soft_armor_back |
| NPP KlASS Kora-Kulon防弹衣（数码迷彩） | 64be79e2bf8412471d0d9bcc | 护甲 | 3 | 128 | Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown | Soft_armor_front, Soft_armor_back |
| BNTI Kirasa-N（胸甲-N）防弹衣（绿色） | 67ab2f28dafe3b22670c9116 | 护甲 | 3 | 240 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back, Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown, Collider Type NeckFront, Collider Type NeckBack | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right, Collar |
| WARTECH TV-115 插板胸挂（黑色） | 69b10ebfde4dda4a140bddb8 | 携板背心 | 3 | 122 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back | Front_plate, Back_plate, Soft_armor_front, Soft_armor_back |
| PACA 软质防弹背心 | 5648a7494bdc2d9d488b4583 | 护甲 | 2 | 100 | Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineDown, Collider Type SpineTop, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| BNTI Module-3M 防弹背心 | 59e7635f86f7742cbf2c1095 | 护甲 | 2 | 80 | Collider Type RibcageLow, Collider Type RibcageUp, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| 6B2 防弹衣（丛林迷彩） | 5df8a2ca86f7740bfe6df777 | 护甲 | 2 | 128 | Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown | Soft_armor_front, Soft_armor_back |
| PACA 软质防弹背心（Rivals 版本） | 607f20859ee58b18e41ecd90 | 护甲 | 2 | 100 | Collider Type RibcageUp, Collider Type RibcageLow, Collider Type SpineTop, Collider Type SpineDown, Collider Type LeftSideChestDown, Collider Type RightSideChestDown | Soft_armor_front, Soft_armor_back, Soft_armor_left, soft_armor_right |
| Tac-Kek JayPC 插板胸挂（黑色） | 693fd1200ec97e98040bd3f9 | 携板背心 | 1 | 180 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back | Front_plate, Back_plate |
| Tac-Kek JayPC 插板胸挂（橄榄绿） | 693fd13aa490096a05028cc8 | 携板背心 | 1 | 180 | Armor Zone Plate_Granit_SAPI_chest, Armor Zone Plate_Granit_SAPI_back | Front_plate, Back_plate |
| BlackRock胸挂 | 5648a69d4bdc2ded0b8b457b | 携板背心 | - | - | - | - |
| Scav背心 | 572b7adb24597762ae139821 | 携板背心 | - | - | - | - |
| UMTBS 6Sh112 Scout-Sniper胸挂（数码丛林迷彩） | 5929a2a086f7744f4b234d43 | 携板背心 | - | - | - | - |
| ANA Tactical Alpha胸挂 | 592c2d1a86f7746dbe2af32a | 携板背心 | - | - | - | - |
| WARTECH TV-109 + TV-106 胸挂（A-TACS 橄榄绿迷彩） | 59e7643b86f7742cbf2c109a | 携板背心 | - | - | - | - |
| WARTECH MK3 TV-104 胸挂 (复合迷彩) | 5ab8dab586f77441cd04f2a2 | 携板背心 | - | - | - | - |
| Blackhawk! Commando胸挂（Desert Tan） | 5b44c8ea86f7742d1627baf1 | 携板背心 | - | - | - | - |
| Poyas-A + Poyas-B 复合胸挂 | 5c0e6a1586f77404597b4965 | 携板背心 | - | - | - | - |
| Blackhawk! Commando胸挂（黑） | 5c0e9f2c86f77432297fe0a3 | 携板背心 | - | - | - | - |
| Triton M43-A胸挂 | 5ca20abf86f77418567a43f2 | 携板背心 | - | - | - | - |
| Haley Strategic D3CRX 胸挂（丛林绿） | 5d5d85c586f774279a21cbdb | 携板背心 | - | - | - | - |
| SOE微型胸挂 | 5d5d8ca986f7742798716522 | 携板背心 | - | - | - | - |
| Velocity Systems多用途巡逻背心 | 5df8a42886f77412640e2e75 | 携板背心 | - | - | - | - |
| Spiritus Systems Bank Robber 胸挂 | 5e4abc1f86f774069619fbaa | 携板背心 | - | - | - | - |
| Splav Tarzan M22 胸挂 | 5e4abfed86f77406a2713cf7 | 携板背心 | - | - | - | - |
| LBT-1961A 承重胸挂（MAS 灰色） | 5e9db13186f7742f845ee9d3 | 携板背心 | - | - | - | - |
| Direct Action Thunderbolt 紧凑型胸挂 | 5f5f41f56760b4138443b352 | 携板背心 | - | - | - | - |
| IDEA DIY胸挂 | 5fd4c4fa16cac650092f6771 | 携板背心 | - | - | - | - |
| 保安背心 | 5fd4c5477a8d854fa0105061 | 携板背心 | - | - | - | - |
| Gear Craft GC-BSS-MK1 胸挂 (A-TACS 橄榄绿迷彩) | 5fd4c60f875c30179f5d04c2 | 携板背心 | - | - | - | - |
| Umka М33-SET1 猎人背心 | 6034cf5fffd42c541047f72e | 携板背心 | - | - | - | - |
| CSA 胸挂（黑色） | 6034d0230ca681766b6a0fb5 | 携板背心 | - | - | - | - |
| Azimut SS "Zhuk"胸挂（黑） | 603648ff5a45383c122086ac | 携板背心 | - | - | - | - |
| Azimut SS "Zhuk"胸挂（SURPAT） | 6040dd4ddcf9592f401632d2 | 携板背心 | - | - | - | - |
| Stich Profi MK2胸挂（突击型，A-TACS FG迷彩） | 60a621c49c197e4e8c4455e6 | 携板背心 | - | - | - | - |
| Stich Profi MK2胸挂（侦察型，A-TACS FG迷彩） | 60a6220e953894617404b00a | 携板背心 | - | - | - | - |
| LBT-1961A 承重胸挂（Goons特别版） | 628baf0b967de16aab5a4f36 | 携板背心 | - | - | - | - |
| Azimut SS "Khamelion"胸挂（橄榄色） | 63611865ba5b90db0c0399d1 | 携板背心 | - | - | - | - |
| Zulu Nylon Gear M4 低特征胸挂（丛林绿） | 64be7095047e826eae02b0c1 | 携板背心 | - | - | - | - |
| 56式胸挂 | 64be7110bf597ba84a0a41ea | 携板背心 | - | - | - | - |
| Spiritus Systems Bank Robber 胸挂 (高原复合迷彩) | 674589d98dd67746010329e6 | 携板背心 | - | - | - | - |
| ANA Tactical Alpha 胸挂（复合迷彩） | 67ab3ea96d7ece17bf0096f6 | 携板背心 | - | - | - | - |
| BlackRock 胸挂（卡其色） | 67ab3f146d7ece17bf0096ff | 携板背心 | - | - | - | - |
| ANA Tactical Alpha 胸挂（A-TACS AU） | 68a85ac615a5769d50029cd9 | 携板背心 | - | - | - | - |
| WARTECH MK3 TV-104 胸挂 (苔藓迷彩) | 68a986615db8a4a66404ff9a | 携板背心 | - | - | - | - |
| LBT-1961A 承重胸挂（AOR1） | 68bee2b431902bb4d90c3a2a | 携板背心 | - | - | - | - |
| Direct Action Thunderbolt 紧凑型胸挂（丛林绿） | 69b11f46f3783ec37c03a116 | 携板背心 | - | - | - | - |
| LBT-1961A 承重胸挂（沙漠防夜视迷彩） | 69b124822c6ed131290d921f | 携板背心 | - | - | - | - |
| Stich Profi MK2胸挂（侦察型，数码丛林迷彩） | 69cfbf627eaa38bd240704ad | 携板背心 | - | - | - | - |
| LBT-1961A 承重胸挂（复合迷彩） | 69cfc2d7a9102bdca0080070 | 携板背心 | - | - | - | - |
| Haley Strategic D3CRX 胸挂（黑色） | 69d37050705756116e0a9037 | 携板背心 | - | - | - | - |

## 3. 头盔表（ItemPropertiesHelmet，按 class 降序）

| 中文名 | id（24位） | class | 耐久 | zones | deafening | blocksHeadset | ricochetX | ricochetY | ricochetZ |
|---|---|---|---|---|---|---|---|---|---|
| Tagilla 的“ZABEY”电焊面罩 | 678f84bb9e85556ca60f0362 | 6 | 100 | Collider Type HeadCommon, Collider Type ParietalHead, Collider Type Ears, Collider Type Eyes, Collider Type Jaw, Collider Type BackHead, Collider Type NeckFront, Collider Type NeckBack | Low | false | 0.4 | 0.9 | 70 |
| Altyn 防弹头盔（橄榄绿） | 5aa7e276e5b5b000171d0647 | 5 | 81 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.4 | 0.9 | 70 |
| Vulkan-5 (火神) LShZ-5 重型防弹头盔 (黑色) | 5ca20ee186f774799474abc2 | 5 | 75 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.3 | 0.85 | 75 |
| Rys-T 防弹头盔（黑色） | 5f60c74e3b85f6263c145586 | 5 | 90 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.4 | 0.9 | 65 |
| Tagilla的"UBEY"电焊面罩 | 60a7ad2a2198820d95707a2e | 5 | 40 | Collider Type HeadCommon, Collider Type ParietalHead, Collider Type Ears, Collider Type Eyes, Collider Type Jaw | Low | false | 0.4 | 0.9 | 70 |
| Tagilla的"Gorilla"电焊面罩 | 60a7ad3a0c5cb24b0134664a | 5 | 40 | Collider Type HeadCommon, Collider Type ParietalHead, Collider Type Ears, Collider Type Eyes, Collider Type Jaw | Low | false | 0.4 | 0.9 | 70 |
| Vulkan-5 (火神) LShZ-5 重型防弹头盔 (8 号球) | 68a986ca5c0073fa2d0d8cb8 | 5 | 75 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.3 | 0.85 | 75 |
| Vulkan-5 (火神) LShZ-5 重型防弹头盔 (烈焰) | 68a98762609a5cb2120ebd26 | 5 | 75 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.3 | 0.85 | 75 |
| Vulkan-5 (火神) LShZ-5 重型防弹头盔 (丛林迷彩) | 68a987a1609a5cb2120ebd2f | 5 | 75 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.3 | 0.85 | 75 |
| Ops-Core FAST MT 超级高切头盔（黑色） | 5a154d5cfcdbcb001a3b00da | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| ZSh-1-2M 头盔（橄榄绿） | 5aa7e454e5b5b0214e506fa2 | 4 | 63 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.3 | 0.85 | 75 |
| ZSh-1-2M 头盔（黑色盔罩） | 5aa7e4a4e5b5b000137b76f2 | 4 | 63 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.3 | 0.85 | 75 |
| Ops-Core FAST MT 超级高切 头盔（城市褐） | 5ac8d6885acfc400180ae7b0 | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| HighCom Striker ULACH IIIA 头盔（黑色） | 5b40e1525acfc4771e1c6611 | 4 | 66 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.3 | 0.7 | 85 |
| HighCom Striker ULACH IIIA 头盔（沙漠黄） | 5b40e2bc5acfc40016388216 | 4 | 66 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.3 | 0.7 | 85 |
| HighCom Striker ACHHC IIIA 头盔（黑色） | 5b40e3f35acfc40016388218 | 4 | 36 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| HighCom Striker ACHHC IIIA 头盔（橄榄绿） | 5b40e4035acfc47a87740943 | 4 | 36 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| DevTac 浪人面罩 | 5b4329f05acfc47a86086aa1 | 4 | 180 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Eyes, Collider Type Jaw, Collider Type HeadCommon, Collider Type Ears | High | false | 0.3 | 0.85 | 75 |
| Maska-1SCh 防弹头盔（橄榄绿） | 5c091a4e0db834001d5addc8 | 4 | 108 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.4 | 0.9 | 70 |
| Maska-1SCh 防弹头盔（Killa 版） | 5c0e874186f7745dc7616606 | 4 | 108 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.4 | 0.9 | 70 |
| Crye Precision AirFrame 头盔（黄褐色） | 5c17a7ed2e2216152142459c | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| MSA ACH TC-2001 MICH系列头盔 | 5d5e7d28a4b936645d161203 | 4 | 30 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| MSA ACH TC-2002 MICH系列头盔 | 5d5e9c74a4b9364855191c40 | 4 | 32 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| BNTI LShZ-2DTM 头盔（黑色） | 5d6d3716a4b9361bc8618872 | 4 | 99 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.3 | 0.85 | 75 |
| Team Wendy EXFIL 防弹头盔（黑色） | 5e00c1ad86f774747333222c | 4 | 54 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Team Wendy EXFIL 防弹头盔（狼棕色） | 5e01ef6886f77445f643baa4 | 4 | 54 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| MSA Gallet TC 800 High Cut 作战头盔 | 5e4bfc1586f774264f7582d3 | 4 | 36 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Diamond Age Bastion 头盔（黑色） | 5ea17ca01412a1425304d1c0 | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Diamond Age NeoSteel 高切头盔 (黑色) | 65709d2d21b9f815e208ff95 | 4 | 72 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.4 | 0.9 | 70 |
| Ballistic Armor Co. Bastion头盔（黑色） | 66b5f65ca7f72d197e70bcd6 | 4 | 50 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Ballistic Armor Co. Bastion头盔（橄榄绿） | 66b5f661af44ca0014063c05 | 4 | 50 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Ballistic Armor Co. Bastion头盔（复合迷彩） | 66b5f666cad6f002ab7214c2 | 4 | 50 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| MTEK FLUX 防弹头盔（高原复合迷彩） | 675956062f6ddfe8ff0e2806 | 4 | 50 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| MTEK FLUX 防弹头盔（橄榄绿） | 6759655674aa5e0825040d62 | 4 | 50 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| MTEK STRIKE 防弹头盔（灰褐色） | 67597ceea35600b4c10cea86 | 4 | 40 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| MTEK STRIKE 防弹头盔（灰褐/荒漠复合迷彩） | 67597d241d5a44f2f605df06 | 4 | 40 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Maska-1SCh 防弹头盔（圣诞特装版） | 6759af0f9c8a538dd70bfae6 | 4 | 108 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.4 | 0.9 | 70 |
| DevTac 浪人面罩（野兽） | 68a9a85c3e1ee5a70504c12e | 4 | 180 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Eyes, Collider Type Jaw, Collider Type HeadCommon, Collider Type Ears | High | false | 0.3 | 0.85 | 75 |
| DevTac 浪人面罩（永恒之绿） | 68a9a92b838d65bcb3050176 | 4 | 180 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Eyes, Collider Type Jaw, Collider Type HeadCommon, Collider Type Ears | High | false | 0.3 | 0.85 | 75 |
| Diamond Age NeoSteel 高切头盔 (橙斑) | 68a9be93f260f4e1c2038686 | 4 | 72 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.4 | 0.9 | 70 |
| Diamond Age NeoSteel 高切头盔 (王牌) | 68a9beecbba00932ed0bc256 | 4 | 72 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.4 | 0.9 | 70 |
| 冠军头盔 | 68bee28f79c8186398098e6f | 4 | 100 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Eyes, Collider Type HeadCommon, Collider Type Jaw | Low | false | 0.3 | 0.7 | 85 |
| HighCom Striker ULACH IIIA 头盔（沙色） | 68bee2ccd6da72c13f03db95 | 4 | 66 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.3 | 0.7 | 85 |
| HighCom Striker ULACH IIIA 头盔（绿色条纹） | 68bee2d9af253218c00ebbb4 | 4 | 66 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.3 | 0.7 | 85 |
| HighCom Striker ULACH IIIA 头盔（网格喷漆） | 68bee2e0ede5c8489f08e1b5 | 4 | 66 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.3 | 0.7 | 85 |
| HighCom Striker ULACH IIIA 头盔（狼棕条纹） | 68bee2e876e02b9e340ef113 | 4 | 66 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.3 | 0.7 | 85 |
| HighCom Striker ULACH IIIA 头盔（冬季网格喷漆） | 693be8f650fafa102607aed4 | 4 | 66 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.3 | 0.7 | 85 |
| Maska-1SCh 防弹头盔（Vida 版） | 697c79b1daf4828686033fcb | 4 | 108 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.4 | 0.9 | 70 |
| Team Wendy EXFIL 防弹头盔（复合迷彩） | 69c26722bf4ff19f50057643 | 4 | 54 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Highcom Striker ACHHC IIIA 头盔（狼棕色） | 69c26fa8add25b3623091e89 | 4 | 36 | Collider Type BackHead, Collider Type ParietalHead | None | false | 0.3 | 0.85 | 75 |
| Crye Precision AirFrame 头盔（网格绿色） | 69cbfe1f6dbaa9badb0c6b13 | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Crye Precision AirFrame 头盔（橄榄绿） | 69cbfe34c293038df7002963 | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Crye Precision AirFrame 头盔（鲨鱼嘴） | 69cbfe3a2f657c59da06e262 | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Crye Precision AirFrame 头盔（黄褐色） | 69cbfe44897389c1870b2337 | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Crye Precision AirFrame M-LOK 头盔（黑色） | 69cd423343c6b278a2076cca | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Crye Precision AirFrame M-LOK 头盔（公牛） | 69ce47ca49b447e32200cbe9 | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Crye Precision AirFrame 头盔（老派风格） | 69ce47f01bb66daf5b0d62fd | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Atlant Armour Titan（泰坦）芳纶头盔（橄榄绿） | 69ce70a321c1fc42cd01affa | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Atlant Armour Titan（泰坦）芳纶头盔（复合迷彩） | 69ce70df606696c00301a760 | 4 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Atlant Armour Titan（泰坦）芳纶头盔 (Rudiarius) | 69ce70ff49b447e32200cc1c | 4 | 78 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Jaw, Collider Type HeadCommon, Collider Type Ears | None | false | 0.3 | 0.85 | 75 |
| FORT Kiver-M 防弹头盔 | 5645bc214bdc2d363b8b4571 | 3 | 63 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.2 | 0.7 | 85 |
| 6B47 Ratnik-BSh 头盔（橄榄绿） | 5a7c4850e899ef00150be885 | 3 | 45 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.2 | 0.7 | 85 |
| 6B47 Ratnik-BSh 头盔（数码迷彩盔罩） | 5aa7cfc0e5b5b00015693143 | 3 | 45 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.2 | 0.7 | 85 |
| UNTAR头盔 | 5aa7d03ae5b5b00016327db5 | 3 | 45 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.2 | 0.7 | 85 |
| SSSh-94 SFERA-S 头盔 | 5aa7d193e5b5b000171d063f | 3 | 135 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.4 | 0.9 | 70 |
| LZSh 轻型头盔（橄榄绿） | 5b432d215acfc4771e1c6624 | 3 | 36 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.2 | 0.7 | 85 |
| SSh-68钢盔（橄榄绿） | 5c06c6a80db834001b735491 | 3 | 54 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | None | false | 0.4 | 0.9 | 70 |
| Galvion 凯门鳄 复合防弹头盔（灰色） | 5f60b34a41e30a4ab12a6947 | 3 | 60 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| NFM "HJELM" 头盔 | 61bca7cda0eae612383adf57 | 3 | 78 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Atomic Defense CQCM 防弹面具（黑色） | 657089638db3adca1009f4ca | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| NPP KlASS Tor-2 头盔（橄榄绿） | 65719f0775149d62ce0a670b | 3 | 81 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | None | false | 0.3 | 0.85 | 75 |
| Devtac 浪人防弹头盔 | 66bdc28a0b603c26902b2011 | 3 | 180 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Eyes, Collider Type Jaw, Collider Type HeadCommon, Collider Type Ears | High | false | 0.2 | 0.7 | 85 |
| 6B47 Ratnik-BSh 头盔（极地数码迷彩盔罩） | 6745895717824b1ec20570a6 | 3 | 45 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.2 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（微笑） | 67a4b71ad3228756b6088ee2 | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具 (Stop Me) | 67a5c5b6dfdf568c9009af66 | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（战疤） | 67a5c5df782ce4655104db14 | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（标靶） | 67a5c5f37f52620c5b05b4d6 | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（骷髅） | 67a5c6068fcd9fb73f0752cf | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（恶魔） | 67a5c61c7f52620c5b05b4d8 | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（亡灵节） | 67a5c657782ce4655104db16 | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（未读消息） | 688b3bfa1ed594eccd0c45ee | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（路易伪登） | 68a9a04373d52d47830759c7 | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（生化警告） | 68a9a0b01696fb8c1e0ee9cc | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（碰撞测试） | 68a9a1223e1ee5a70504c126 | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM 防弹面具（恶魔獠牙） | 68a9a15d73d52d47830759c9 | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Galvion 凯门鳄 复合防弹头盔（复合迷彩） | 68a9b3ca0a9c4f9398032c46 | 3 | 60 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Galvion 凯门鳄 复合防弹头盔（共生者） | 68a9b5a5863d2a71fa0494a6 | 3 | 60 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Atomic Defense CQCM 防弹面具（路易伪登） | 68d54d0525ac8590a8075ac3 | 3 | 35 | Collider Type HeadCommon, Collider Type Jaw, Collider Type Eyes, Collider Type ParietalHead | None | false | 0.3 | 0.7 | 85 |
| Atomic Defense CQCM防弹面罩（冰爆） | 6936ff8734029a096c06f95a | 3 | 35 | Collider Type HeadCommon, Collider Type ParietalHead, Collider Type Eyes, Collider Type Jaw | None | false | 0.3 | 0.7 | 85 |
| Galvion 凯门鳄 复合防弹头盔（高原复合迷彩） | 693be003582cc8870b090b41 | 3 | 60 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.85 | 75 |
| Kolpak -1S防暴头盔 | 59e7711e86f7746cae05fbe1 | 2 | 45 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | High | false | 0.2 | 0.7 | 85 |
| 杰克南瓜灯战术南瓜头盔 | 59ef13ca86f77445fd0e2483 | 2 | 40 | Collider Type HeadCommon, Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears, Collider Type Eyes, Collider Type Jaw, Collider Type NeckFront, Collider Type NeckBack | Low | false | 0.3 | 0.7 | 85 |
| SHPM消防盔 | 5c08f87c0db8340019124324 | 2 | 96 | Collider Type ParietalHead, Collider Type BackHead, Collider Type HeadCommon, Collider Type Eyes, Collider Type Jaw, Collider Type NeckFront, Collider Type Ears | High | false | 0.2 | 0.7 | 85 |
| PSH-97 "Jeta"头盔 | 5c0d2727d174af02a012cf58 | 2 | 156 | Collider Type ParietalHead, Collider Type BackHead, Collider Type HeadCommon, Collider Type Eyes, Collider Type Ears | High | false | 0.2 | 0.7 | 85 |
| Death Shadow (死亡阴影) 轻量化防弹面具 | 6570aead4d84f81fd002a033 | 2 | 30 | Collider Type Eyes, Collider Type Jaw, Collider Type HeadCommon | None | false | 0.3 | 0.7 | 85 |
| SHIELD 神中神 硬质安全帽 (橘色) | 68a6d95addf0111c2f04c9c3 | 2 | 96 | Collider Type ParietalHead, Collider Type BackHead, Collider Type HeadCommon, Collider Type Eyes, Collider Type Jaw, Collider Type NeckFront, Collider Type Ears | High | true | 0.2 | 0.7 | 85 |
| SHIELD 神中神 硬质安全帽 (白色) | 68a6d96fddf0111c2f04c9c9 | 2 | 96 | Collider Type ParietalHead, Collider Type BackHead, Collider Type HeadCommon, Collider Type Eyes, Collider Type Jaw, Collider Type NeckFront, Collider Type Ears | High | true | 0.2 | 0.7 | 85 |
| Death Shadow (死亡阴影) 轻量化防弹面具 (金色) | 68a9b821cdf661cc5a0626c6 | 2 | 30 | Collider Type Eyes, Collider Type Jaw, Collider Type HeadCommon | None | false | 0.3 | 0.7 | 85 |
| Death Shadow (死亡阴影) 轻量化防弹面具 (灰色) | 68a9b852cdf661cc5a0626c9 | 2 | 30 | Collider Type Eyes, Collider Type Jaw, Collider Type HeadCommon | None | false | 0.3 | 0.7 | 85 |
| Death Shadow (死亡阴影) 轻量化防弹面具 (白色) | 68a9b873a4b28d56c80a1818 | 2 | 30 | Collider Type Eyes, Collider Type Jaw, Collider Type HeadCommon | None | false | 0.3 | 0.7 | 85 |
| DevTaс 武士下颊 防弹面具 | 68bee22e79c8186398098e6d | 2 | 25 | Collider Type HeadCommon, Collider Type Jaw | None | false | 0.3 | 0.7 | 85 |
| DevTaс 武士下颊 防弹面具 (金色) | 68bee238a48c3c320808abc4 | 2 | 25 | Collider Type HeadCommon, Collider Type Jaw | None | false | 0.3 | 0.7 | 85 |
| DevTaс 武士下颊 防弹面具 (白色) | 68bee246ede5c8489f08e1b3 | 2 | 25 | Collider Type HeadCommon, Collider Type Jaw | None | false | 0.3 | 0.7 | 85 |
| 轻型加固破碎面具 | 5b432b2f5acfc4771e1c6622 | 1 | 40 | Collider Type HeadCommon, Collider Type ParietalHead, Collider Type Eyes, Collider Type Jaw | None | false | 0.2 | 0.7 | 85 |
| TSH-4M-L软质坦克乘员头盔 | 5df8a58286f77412631087ed | 1 | 105 | Collider Type ParietalHead, Collider Type BackHead, Collider Type Ears | Low | false | 0.2 | 0.7 | 85 |
| Tac-Kek Fast MT 头盔（不防弹的仿制品） | 5ea05cf85ad9772e6624305d | 1 | 48 | Collider Type ParietalHead, Collider Type BackHead | None | false | 0.3 | 0.7 | 85 |
| Bomber 无檐小便帽 | 60bf74184a63fc79b60c57f6 | 1 | 150 | Collider Type BackHead | None | false | 0.2 | 0.7 | 85 |
| Death Knight面具 | 62963c18dbc8ab5f0d382d0b | 1 | 55 | Collider Type ParietalHead, Collider Type Eyes | None | false | 0.2 | 0.7 | 85 |
| Glorious E轻型防弹面具 | 62a09e08de7ac81993580532 | 1 | 40 | Collider Type HeadCommon, Collider Type ParietalHead, Collider Type Eyes, Collider Type Jaw | None | false | 0.2 | 0.7 | 85 |

## 4. 插板系统说明

live 1.x 护甲/头盔的 `properties.armorSlots` 为插板/软甲块数组，每项结构：

```json
{
  "nameId": "Front_plate",          // 槽位名（与 SPT Slots[]._name 对应）
  "zones": ["Armor Zone Plate_Granit_SAPI_chest"],  // 该槽防护区域（新 ArmorZone 格式）
  "allowedPlates": ["656f9d59..."],  // 允许装入的插板物品 id 列表
  // 软甲块（Soft_armor_*）另有内嵌数值：class / durability / ricochetX/Y/Z / armorMaterial
}
```

- 硬板槽（Front_plate / Back_plate / Left_side_plate / Right_side_plate 等）：本体不存数值，数值在 `allowedPlates` 引用的插板物品（propertiesType=ItemPropertiesArmorAttachment，types 含 `armorPlate`）上
- 软甲块（Soft_armor_front/back/left/right、Collar、Shoulder_*、Groin、Helmet_top/back/ears 等）：class/durability 内嵌在 armorSlots 项内
- 顶层 `class`/`durability` 为该护甲的等效聚合值（写 mod 时优先用 armorSlots 与插板物品的明细值）
- 头盔额外字段：`deafening`（听觉压制 High/Low/None）、`blocksHeadset`（是否阻挡耳机）、`ricochetX/Y/Z`（跳弹参数）

## 5. SPT 对照

### 5.1 关键结构差异（重要）

SPT 4.1（锁定 0.16.9.5）与 live 1.1.0 的护甲数据组织方式不同：

| 维度 | live 1.1.0（tarkov.dev） | SPT 4.1（0.16.9.5） |
|---|---|---|
| 护甲主体数值 | properties.class / properties.durability（等效聚合值） | `_props.armorClass` / `_props.Durability` = **0**（清零） |
| 数值存放位置 | properties.armorSlots（内嵌）+ allowedPlates 插板物品 | `_props.Slots[].filters[].Plate` 引用的独立插板物品 |
| 防护区域 | zones（`Collider Type ...` / `Armor Zone Plate_...` 新格式） | 插板物品 `_props.armorColliders`（如 `RibcageUp` 旧格式） |
| 跳弹参数 | 顶层 ricochetX/Y/Z / 软甲块内嵌 | 插板物品 `_props.RicochetParams{x,y,z}` |

> live zones 的 `Collider Type X` / `Armor Zone Plate_X` 是 1.x 新 ArmorZone 格式；SPT 0.16 旧格式用 `armorColliders` 短名（如 `RibcageLow`）。这是版本演进证据，非数据错误，不做强行对齐。

### 5.2 抽样对照表（10 件护甲 class 2-6 + 1 头盔）

顶层对照：

| 物品 | 类型 | live class | SPT armorClass | live 耐久 | SPT Durability | 顶层一致？ |
|---|---|---|---|---|---|---|
| PACA 软质防弹背心 | 护甲 | 2 | 0 | 100 | 0 | 否（SPT 主体清零，数值在插板） |
| MF-UNTAR防弹背心 | 护甲 | 3 | 0 | 100 | 0 | 否（SPT 主体清零，数值在插板） |
| BNTI Kirasa-N（胸甲-N）防弹衣 | 护甲 | 3 | 0 | 240 | 0 | 否（SPT 主体清零，数值在插板） |
| 6B13 突击甲（丛林迷彩） | 护甲 | 4 | 0 | 203 | 0 | 否（SPT 主体清零，数值在插板） |
| 6B13 突击甲（数码丛林迷彩） | 护甲 | 4 | 0 | 203 | 0 | 否（SPT 主体清零，数值在插板） |
| BNTI Gzhel-K（彩瓷-K）防弹衣 | 护甲 | 5 | 0 | 259 | 0 | 否（SPT 主体清零，数值在插板） |
| IOTV Gen4 防弹衣（全面防护型，复合迷彩） | 护甲 | 5 | 0 | 398 | 0 | 否（SPT 主体清零，数值在插板） |
| 6B43 屏障-Sh 防弹衣（数码丛林迷彩） | 护甲 | 6 | 0 | 510 | 0 | 否（SPT 主体清零，数值在插板） |
| BNTI Zhuk（甲虫）防弹衣（数码丛林迷彩） | 护甲 | 6 | 0 | 305 | 0 | 否（SPT 主体清零，数值在插板） |
| FORT Redut-T5（堡垒-T5）防弹衣（烟雾迷彩） | 护甲 | 6 | 0 | 504 | 0 | 否（SPT 主体清零，数值在插板） |
| FORT Kiver-M 防弹头盔 | 头盔 | 3 | 0 | 63 | 0 | 否（SPT 主体清零，数值在插板） |

> 说明：SPT 4.1 所有护甲/头盔主体 `armorClass`/`Durability` 均为 0，数值迁移到 Slots→Plate 插板物品；因此顶层对照"不一致"是结构性差异，需看插板级对照。

插板槽级对照（软甲块 + 硬板槽）：

| 物品 | 可比插板槽数 | class 一致 | durability 一致 | 说明 |
|---|---|---|---|---|
| PACA 软质防弹背心 | 4 | 4 | 4 | 全一致 |
| MF-UNTAR防弹背心 | 4 | 4 | 4 | 全一致 |
| BNTI Kirasa-N（胸甲-N）防弹衣 | 7 | 7 | 5 | 全一致；durability 仅软甲块可比（5/7） |
| 6B13 突击甲（丛林迷彩） | 8 | 8 | 6 | 全一致；durability 仅软甲块可比（6/8） |
| 6B13 突击甲（数码丛林迷彩） | 8 | 8 | 6 | 全一致；durability 仅软甲块可比（6/8） |
| BNTI Gzhel-K（彩瓷-K）防弹衣 | 7 | 7 | 5 | SPT 无对应插板槽: Left_side_plate, Right_side_plate；durability 仅软甲块可比（5/7） |
| IOTV Gen4 防弹衣（全面防护型，复合迷彩） | 13 | 13 | 9 | 全一致；durability 仅软甲块可比（9/13） |
| 6B43 屏障-Sh 防弹衣（数码丛林迷彩） | 12 | 12 | 8 | 全一致；durability 仅软甲块可比（8/12） |
| BNTI Zhuk（甲虫）防弹衣（数码丛林迷彩） | 9 | 9 | 5 | 全一致；durability 仅软甲块可比（5/9） |
| FORT Redut-T5（堡垒-T5）防弹衣（烟雾迷彩） | 13 | 11 | 9 | class 不一致: Front_plate(SPT 5 vs live 6), Back_plate(SPT 5 vs live 6)；durability 仅软甲块可比（9/13） |
| FORT Kiver-M 防弹头盔 | 3 | 3 | 3 | 全一致 |
| **合计** | **88** | **86** | **64** | （durability 仅软甲块可对照） |

### 5.3 一致率统计

- 插板槽 class 一致率：**97.7%**（86/88）
- 插板槽 durability 一致率（软甲块，64 项可比）：**64/64**（无不一致项，差异项均为 SPT 无对应插板/硬板槽不可比）
- 软甲块 class + durability：**100% 一致**（SPT Plate 插板物品数值与 live armorSlots 内嵌数值完全相同）
- 硬板槽：live 侧数值在 allowedPlates 插板物品，SPT 侧在 Plate 插板物品；class 与护甲等效 class 对照。差异仅见 Redut-T5 Front/Back（SPT 默认 Plate class 5 vs live 等效 6）与 Gzhel-K 侧板（SPT 无对应 Plate）
- 插板物品本身（独立 items，如 `656f9d5900d62bcd2e02407c`）：live class/durability 与 SPT 插板物品 `armorClass`/`Durability` 可直接对照（抽样一致）

## 6. 使用建议

- 写 mod 时以 SPT 数据库 `_props` 为数值权威；注意 SPT 4.1 中护甲数值在插板物品上（Slots→filters→Plate），直接改主体 `armorClass` 无效
- live-only 物品（1.1.0 新增护甲/头盔）SPT 4.1 无对应，引用前确认存在性
- zones 跨版本不可直接字符串比对（`Collider Type X` vs `armorColliders` 短名）；做兼容判断时用短名归一化
- 插板槽 nameId 在 live armorSlots 与 SPT Slots[]._name 一致，可作为跨版本映射键
- 中文名以 items_zh.json 为准（与 SPT ch.json 同源）
