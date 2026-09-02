---
version: [live-ref]
domain: both
topic: reference-data
source: curated
---

# 弹药数据参考 [live-ref]

> 元信息
>
> - 快照日期：2026-09-02（tarkov.dev 全量 dump）
> - live EFT：1.1.0.1.46911
> - SPT：4.1.2（锁定 0.16.9.5.40743）
> - [live-ref] 声明：本表为 live 参考数据，数值权威以 SPT 数据库（`SPT_Data/database/templates/items.json`）为准
> - 数据源：`knowledge/spt-kb/archive/tarkov-dev/snapshot-2026-09-02/items.json`（+ items_zh.json / items_en.json 翻译字典）

## 1. 总览

- 弹药总数（types 含 `ammo`）：**212**
- 其中 propertiesType=ItemPropertiesAmmo：200；手榴弹/弹药包/火箭弹等（无弹药数值）：12
- 口径分组：32 组（含无口径 12 条）
- 中文名来源：items_zh.json `"<id> Name"` → items_en.json → normalizedName 回退
- 数值字段（damage/penetrationPower/armorDamage/initialSpeed/fragmentationChance/ricochetChance/stackMaxSize）取自 properties（ItemPropertiesAmmo），缺失留空；weight 取物品顶层字段
- fragmentationChance / ricochetChance 为 0~1 比例值（表内按百分数展示）

## 2. 全量弹药表（按口径分组）

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
### (无)

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| RGD-5手榴弹 | 5448be9a4bdc2dfd2f8b456a | - |  |  |  |  |  |  | 0.31 |  |
| F-1手榴弹 | 5710c24ad2720bc3458b45a3 | - |  |  |  |  |  |  | 0.6 |  |
| 5.45x39mm BP弹药包（120发装） | 5737292724597765e5728562 | - |  |  |  |  |  |  | 0.1 |  |
| 5.45x39mm BP弹药包（120发装） | 57372a7f24597766fe0de0c1 | - |  |  |  |  |  |  | 0.1 |  |
| 5.45x39mm BT弹药包（120发装） | 57372c21245977670937c6c2 | - |  |  |  |  |  |  | 0.1 |  |
| 5.45x39mm BT弹药包（120发装） | 57372c56245977685e584582 | - |  |  |  |  |  |  | 0.1 |  |
| M67手榴弹 | 58d3db5386f77426186285a0 | - |  |  |  |  |  |  | 0.4 |  |
| Zarya震撼手榴弹 | 5a0c27731526d80618476ac4 | - |  |  |  |  |  |  | 0.18 |  |
| RDG-2B烟雾弹 | 5a2a57cfc4a2826c6e06d44a | - |  |  |  |  |  |  | 0.6 |  |
| VOG-17 Khattabka 简易手榴弹 | 5e32f56fcb6d5863cc5e5ee4 | - |  |  |  |  |  |  | 0.28 |  |
| VOG-25 Khattabka 简易手榴弹 | 5e340dcdcb6d5863cc5e5efb | - |  |  |  |  |  |  | 0.25 |  |
| ShG-2 反人员火箭弹 | 67446fdd752be02c220f27b3 | - |  |  |  |  |  |  | 0.23 |  |

### Caliber556x45NATO

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 5.56x45毫米 M855 | 54527a984bdc2d4e668b4567 | Caliber556x45NATO | 54 | 31 | 37 | 922 | 50 | 40 | 0.01 | 80 |
| 5.56x45毫米 M855A1 | 54527ac44bdc2d36668b4567 | Caliber556x45NATO | 49 | 44 | 47 | 945 | 44 | 38 | 0.01 | 80 |
| 5.56x45毫米 M856 | 59e68f6f86f7746c9f75e846 | Caliber556x45NATO | 60 | 18 | 26 | 874 | 32.8 | 38 | 0.01 | 80 |
| 5.56x45毫米 M856A1 | 59e6906286f7746c9f75e847 | Caliber556x45NATO | 52 | 38 | 44 | 940 | 42.8 | 38 | 0.01 | 80 |
| 5.56x45毫米 M995 | 59e690b686f7746c9f75e848 | Caliber556x45NATO | 42 | 53 | 52 | 1013 | 42 | 36 | 0.01 | 80 |
| 5.56x45毫米 MK 255 Mod 0 (RRLP) | 59e6918f86f7746c9f75e849 | Caliber556x45NATO | 72 | 11 | 24 | 936 | 3 | 10 | 0.01 | 80 |
| 5.56x45毫米 FMJ | 59e6920f86f77411d82aa167 | Caliber556x45NATO | 57 | 23 | 33 | 957 | 50 | 26 | 0.01 | 80 |
| 5.56x45毫米 HP | 59e6927d86f77411da468256 | Caliber556x45NATO | 79 | 7 | 22 | 947 | 70 | 20 | 0.01 | 80 |
| 5.56x45毫米 Warmageddon | 5c0d5ae286f7741e46554302 | Caliber556x45NATO | 88 | 3 | 11 | 936 | 90 | 5 | 0.01 | 80 |
| 5.56x45毫米 Mk 318 Mod 0 (SOST) | 60194943740c5d77f6705eea | Caliber556x45NATO | 53 | 33 | 39 | 902 | 35 | 35 | 0.01 | 80 |
| 5.56x45毫米 SSA AP | 601949593ae8f707c4608daa | Caliber556x45NATO | 38 | 57 | 58 | 1013 | 30 | 48 | 0.01 | 80 |

### Caliber12g

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 12/70 7毫米鹿弹 | 560d5e524bdc2d25448b4571 | Caliber12g | 39 | 3 | 26 | 415 | 0 | 0 | 0.05 | 25 |
| 12/70 铅头弹 | 58820d1224597753c90aeb13 | Caliber12g | 167 | 15 | 55 | 370 | 20 | 10 | 0.05 | 25 |
| 12/70 RIP | 5c0d591486f7744c505b416f | Caliber12g | 265 | 2 | 11 | 410 | 100 | 1 | 0.04 | 25 |
| 12/70 5.25毫米鹿弹 | 5d6e6772a4b936088465b17c | Caliber12g | 37 | 1 | 15 | 330 | 0 | 0 | 0.04 | 25 |
| 12/70 Express 6.5 毫米鹿弹 | 5d6e67fba4b9361bc73bc779 | Caliber12g | 35 | 3 | 26 | 430 | 0 | 0 | 0.05 | 25 |
| 12/70 Magnum 8.5 毫米鹿弹 | 5d6e6806a4b936088465b17e | Caliber12g | 50 | 2 | 26 | 385 | 0 | 0 | 0.06 | 25 |
| 12/70 Grizzly 40独头弹  | 5d6e6869a4b9361c140bcfde | Caliber12g | 190 | 12 | 48 | 390 | 12 | 10 | 0.06 | 25 |
| 12/70 Poleva-3 独头弹 | 5d6e6891a4b9361bd473feea | Caliber12g | 140 | 17 | 40 | 410 | 20 | 10 | 0.04 | 25 |
| 12/70 Poleva-6U 独头弹 | 5d6e689ca4b9361bc8618956 | Caliber12g | 150 | 20 | 50 | 430 | 15 | 10 | 0.05 | 25 |
| 12/70 AP-20 穿甲独头弹 | 5d6e68a8a4b9360b6c0d54e2 | Caliber12g | 164 | 37 | 65 | 510 | 3 | 10 | 0.05 | 25 |
| 12/70 Copper Sabot Premier 空尖独头弹 | 5d6e68b3a4b9361bca7e50b5 | Caliber12g | 206 | 14 | 46 | 442 | 38 | 10 | 0.04 | 25 |
| 12/70 .50 BMG 简易独头弹 | 5d6e68c4a4b9361b93413f79 | Caliber12g | 197 | 26 | 57 | 410 | 5 | 10 | 0.06 | 25 |
| 12/70 SuperFormance 空尖独头弹 | 5d6e68d1a4b93622fe60e845 | Caliber12g | 220 | 5 | 12 | 594 | 39 | 10 | 0.03 | 25 |
| 12/70 Dual Sabot 独头弹 | 5d6e68dea4b9361bcc29e659 | Caliber12g | 85 | 17 | 65 | 415 | 10 | 10 | 0.05 | 25 |
| 12/70 FTX Custom Lite独头弹 | 5d6e68e6a4b9361c140bcfe0 | Caliber12g | 183 | 20 | 50 | 480 | 10 | 10 | 0.03 | 25 |
| 12/70 箭形弹 | 5d6e6911a4b9361bd5780d52 | Caliber12g | 25 | 31 | 26 | 320 | 0 | 0 | 0.04 | 25 |
| 12/70 “食人鱼” | 64b8ee384b75259c590fa89b | Caliber12g | 25 | 24 | 22 | 310 | 0 | 10 | 0.05 | 25 |

### Caliber762x54R

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 7.62x54R SNB | 560d61e84bdc2da74d8b4571 | Caliber762x54R | 75 | 62 | 87 | 875 | 8 | 28.5 | 0.02 | 40 |
| 7.62x54R LPS | 5887431f2459777e1612938f | Caliber762x54R | 81 | 42 | 78 | 865 | 18 | 39 | 0.02 | 40 |
| 7.62x54R 7N1狙击弹 | 59e77a2386f7742ee578960a | Caliber762x54R | 84 | 45 | 84 | 875 | 8.3 | 28.5 | 0.02 | 40 |
| 7.62x54R T-46M | 5e023cf8186a883be655e54f | Caliber762x54R | 82 | 41 | 83 | 800 | 18 | 30 | 0.03 | 40 |
| 7.62x54R 7BT1 | 5e023d34e8a400319a28ed44 | Caliber762x54R | 78 | 55 | 87 | 875 | 8.1 | 26.5 | 0.02 | 40 |
| 7.62x54R 7N37 | 5e023d48186a883be655e551 | Caliber762x54R | 72 | 70 | 88 | 785 | 8.3 | 34 | 0.03 | 40 |
| 7.62x54R FMJ | 64b8f7968532cf95ee0a0dbf | Caliber762x54R | 84 | 36 | 63 | 760 | 20 | 16 | 0.02 | 40 |
| 7.62x54R SP BT | 64b8f7b5389d7ffd620ccba2 | Caliber762x54R | 92 | 31 | 56 | 703 | 24 | 12 | 0.02 | 40 |
| 7.62x54R HP BT | 64b8f7c241772715af0f9c3d | Caliber762x54R | 102 | 26 | 37 | 807 | 40 | 10 | 0.02 | 40 |

### Caliber762x39

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 7.62x39毫米 PS | 5656d7c34bdc2d9d198b4587 | Caliber762x39 | 61 | 35 | 52 | 717 | 20 | 35 | 0.02 | 70 |
| 7.62x39毫米 BP | 59e0d99486f7744a32234762 | Caliber762x39 | 58 | 47 | 63 | 730 | 12 | 31.5 | 0.02 | 70 |
| 7.62x39毫米 T45M1 | 59e4cf5286f7741778269d8a | Caliber762x39 | 65 | 30 | 46 | 720 | 12 | 35 | 0.02 | 70 |
| 7.62x39毫米 US | 59e4d24686f7741776641ac7 | Caliber762x39 | 56 | 29 | 42 | 301 | 7.5 | 35.8 | 0.02 | 70 |
| 7.62x39毫米 HP | 59e4d3d286f774176a36250a | Caliber762x39 | 80 | 15 | 20 | 754 | 40 | 17.5 | 0.01 | 70 |
| 7.62x39毫米 MAI AP | 601aa3d2b2bcb34913271e6d | Caliber762x39 | 53 | 58 | 76 | 875 | 5 | 43.5 | 0.02 | 70 |
| 7.62x39毫米 PP | 64b7af434b75259c590fa893 | Caliber762x39 | 59 | 41 | 57 | 732 | 15 | 33 | 0.02 | 70 |
| 7.62x39毫米 FMJ | 64b7af5a8532cf95ee0a0dbd | Caliber762x39 | 63 | 26 | 33 | 775 | 30 | 25 | 0.01 | 70 |
| 7.62x39毫米 SP | 64b7af734b75259c590fa895 | Caliber762x39 | 68 | 20 | 27 | 772 | 35 | 20 | 0.01 | 70 |

### Caliber40mmRU

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 40毫米VOG-25榴弹 | 5656eb674bdc2d35148b457c | Caliber40mmRU | 199 | 0 | 68 | 76 | 0 | 0 | 0.26 | 1 |

### Caliber9x19PARA

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 9x19毫米 Pst | 56d59d3ad2720bdb418b4577 | Caliber9x19PARA | 54 | 20 | 33 | 457 | 15 | 5 | 0.01 | 100 |
| 9x19毫米 PSO gzh | 58864a4f2459770fcc257101 | Caliber9x19PARA | 59 | 10 | 32 | 340 | 25 | 6.5 | 0.01 | 100 |
| 9x19毫米 Luger CCI | 5a3c16fe86f77452b62de32a | Caliber9x19PARA | 70 | 10 | 38 | 420 | 25 | 6.5 | 0.02 | 100 |
| 9x19毫米 RIP | 5c0d56a986f774449d5de529 | Caliber9x19PARA | 102 | 2 | 11 | 381 | 100 | 0.2 | 0.01 | 100 |
| 9x19毫米 绿色曳光弹 | 5c3df7d588a4501f290594e5 | Caliber9x19PARA | 58 | 14 | 33 | 365 | 15 | 5 | 0.01 | 100 |
| 9x19毫米 AP 6.3 | 5c925fa22e221601da359b7b | Caliber9x19PARA | 52 | 30 | 48 | 392 | 5 | 20 | 0.01 | 100 |
| 9x19毫米 7N31 | 5efb0da7a29a85116f6ea05f | Caliber9x19PARA | 44 | 39 | 55 | 560 | 5 | 20 | 0.01 | 100 |
| 9x19毫米 QuakeMaker | 5efb0e16aeb21837e749c7ff | Caliber9x19PARA | 85 | 8 | 22 | 290 | 10 | 10 | 0.01 | 100 |
| 9x19毫米 FMJ M882 | 64b7bbb74b75259c590fa897 | Caliber9x19PARA | 56 | 18 | 35 | 385 | 20 | 10 | 0.01 | 100 |

### Caliber545x39

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 5.45x39毫米 BP | 56dfef82d2720bbd668b4567 | Caliber545x39 | 48 | 45 | 46 | 890 | 16 | 36 | 0.01 | 80 |
| 5.45x39毫米 BS | 56dff026d2720bb8668b4567 | Caliber545x39 | 45 | 54 | 57 | 830 | 17 | 38 | 0.01 | 80 |
| 5.45x39毫米 BT | 56dff061d2720bb5668b4567 | Caliber545x39 | 54 | 37 | 44 | 880 | 16.4 | 37 | 0.01 | 80 |
| 5.45x39毫米 FMJ | 56dff0bed2720bb0668b4567 | Caliber545x39 | 55 | 24 | 38 | 884 | 25 | 26 | 0.01 | 80 |
| 5.45x39毫米 HP | 56dff216d2720bbd668b4568 | Caliber545x39 | 76 | 9 | 15 | 884 | 35 | 20 | 0.01 | 80 |
| 5.45x39毫米 PP | 56dff2ced2720bb4668b4567 | Caliber545x39 | 51 | 34 | 42 | 886 | 17 | 38 | 0.01 | 80 |
| 5.45x39毫米 PRS | 56dff338d2720bbd668b4569 | Caliber545x39 | 70 | 13 | 24 | 866 | 30 | 4 | 0.01 | 80 |
| 5.45x39毫米 PS | 56dff3afd2720bba668b4567 | Caliber545x39 | 56 | 28 | 40 | 890 | 40 | 40 | 0.01 | 80 |
| 5.45x39毫米 SP | 56dff421d2720b5f5a8b4567 | Caliber545x39 | 67 | 15 | 31 | 873 | 45 | 15 | 0.01 | 80 |
| 5.45x39毫米 T | 56dff4a2d2720bbd668b456a | Caliber545x39 | 59 | 20 | 36 | 883 | 16 | 40 | 0.01 | 80 |
| 5.45x39毫米 US | 56dff4ecd2720b5f5a8b4568 | Caliber545x39 | 65 | 17 | 33 | 303 | 10 | 40 | 0.01 | 80 |
| 5.45x39毫米 7N39“针刺” | 5c0d5e4486f77478390952fe | Caliber545x39 | 37 | 62 | 59 | 905 | 2 | 38 | 0.01 | 80 |
| 5.45x39毫米 7N40 | 61962b617c6c7b169525f168 | Caliber545x39 | 55 | 42 | 45 | 915 | 2 | 30 | 0.01 | 80 |

### Caliber762x25TT

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 7.62x25毫米TT AKBS | 5735fdcd2459776445391d61 | Caliber762x25TT | 58 | 12 | 32 | 425 | 25 | 6.5 | 0.01 | 100 |
| 7.62x25毫米TT FMJ43 | 5735ff5c245977640e39ba7e | Caliber762x25TT | 60 | 11 | 29 | 427 | 25 | 6.5 | 0.01 | 100 |
| 7.62x25毫米TT LRN | 573601b42459776410737435 | Caliber762x25TT | 64 | 8 | 28 | 375 | 35 | 5 | 0.01 | 100 |
| 7.62x25毫米TT LRNPC | 573602322459776445391df1 | Caliber762x25TT | 66 | 7 | 27 | 385 | 35 | 5 | 0.01 | 100 |
| 7.62x25毫米TT P gl | 5736026a245977644601dc61 | Caliber762x25TT | 58 | 14 | 32 | 430 | 25 | 6.5 | 0.01 | 100 |
| 7.62x25毫米 TT Pst | 573603562459776430731618 | Caliber762x25TT | 50 | 25 | 36 | 430 | 20 | 10 | 0.01 | 100 |
| 7.62x25毫米 TT PT | 573603c924597764442bd9cb | Caliber762x25TT | 55 | 18 | 34 | 415 | 16.6 | 10 | 0.01 | 100 |
| 7.62x25毫米 TT M855A1 | 68c15a033173b556890b5959 | Caliber762x25TT | 34 | 31 | 33 | 900 | 2 | 6.5 | 0.01 | 100 |
| 7.62x25毫米 TT M856A1 | 68c15b4bb30038a118088bd6 | Caliber762x25TT | 36 | 27 | 31 | 900 | 8 | 6.5 | 0.01 | 100 |
| 7.62x25毫米 TT M995 | 68c15f77ed3d7df9220debd6 | Caliber762x25TT | 32 | 37 | 36 | 940 | 25 | 6.5 | 0.01 | 100 |

### Caliber9x18PM

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 9x18毫米 PM BZhT | 573718ba2459775a75491131 | Caliber9x18PM | 53 | 18 | 28 | 325 | 17 | 9.5 | 0.01 | 100 |
| 9x18毫米 PM P | 573719762459775a626ccbc1 | Caliber9x18PM | 50 | 5 | 16 | 302 | 25 | 6.5 | 0.01 | 100 |
| 9x18毫米 PM PBM | 573719df2459775a626ccbc2 | Caliber9x18PM | 40 | 28 | 30 | 519 | 16 | 9 | 0.01 | 100 |
| 9x18毫米 PMM PstM | 57371aab2459775a77142f22 | Caliber9x18PM | 58 | 24 | 33 | 420 | 17 | 7.5 | 0.01 | 100 |
| 9x18毫米 PM PPe | 57371b192459775a9f58a5e0 | Caliber9x18PM | 61 | 7 | 15 | 297 | 35 | 5 | 0.01 | 100 |
| 9x18毫米 PM PPT | 57371e4124597760ff7b25f1 | Caliber9x18PM | 59 | 8 | 22 | 301 | 16.6 | 10 | 0.01 | 100 |
| 9x18毫米 PM PRS | 57371eb62459776125652ac1 | Caliber9x18PM | 58 | 6 | 16 | 302 | 30 | 0.5 | 0.01 | 100 |
| 9x18毫米 PM PS PPO | 57371f2b24597761224311f1 | Caliber9x18PM | 55 | 6 | 16 | 330 | 25 | 3 | 0.01 | 100 |
| 9x18毫米 PM PSO | 57371f8d24597761006c6a81 | Caliber9x18PM | 54 | 5 | 13 | 315 | 35 | 6.5 | 0.01 | 100 |
| 9x18毫米 PM Pst | 5737201124597760fc4431f1 | Caliber9x18PM | 50 | 12 | 26 | 298 | 20 | 10 | 0.01 | 100 |
| 9x18毫米 PM PSV | 5737207f24597760ff7b25f2 | Caliber9x18PM | 69 | 3 | 5 | 280 | 40 | 1 | 0.01 | 100 |
| 9x18毫米 PM RG028 | 573720e02459776143012541 | Caliber9x18PM | 65 | 13 | 26 | 330 | 2 | 5 | 0.01 | 100 |
| 9x18毫米 PM SP7 | 57372140245977611f70ee91 | Caliber9x18PM | 77 | 2 | 5 | 420 | 2 | 5 | 0.01 | 100 |
| 9x18毫米 PM SP8 | 5737218f245977612125ba51 | Caliber9x18PM | 67 | 1 | 2 | 250 | 60 | 5 | 0.01 | 100 |

### Caliber9x39

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 9x39毫米 SP-5 | 57a0dfb82459774d3078b56c | Caliber9x39 | 71 | 28 | 39 | 290 | 20 | 40 | 0.02 | 70 |
| 9x39毫米 SP-6 | 57a0e5022459774d1673f889 | Caliber9x39 | 60 | 48 | 64 | 305 | 10 | 50 | 0.02 | 70 |
| 9x39毫米 SPP | 5c0d668f86f7747ccb7f13b2 | Caliber9x39 | 68 | 35 | 48 | 310 | 20 | 40 | 0.02 | 70 |
| 9x39毫米 BP | 5c0d688c86f77413ae3407b2 | Caliber9x39 | 58 | 54 | 69 | 295 | 10 | 50 | 0.02 | 70 |
| 9x39毫米 PAB-9 | 61962d879bb3d20b0946d385 | Caliber9x39 | 62 | 43 | 57 | 320 | 10 | 48 | 0.02 | 70 |
| 9x39毫米 FMJ | 6576f96220d53a5b8f3e395e | Caliber9x39 | 75 | 17 | 28 | 330 | 30 | 40 | 0.02 | 70 |

### Caliber762x51

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 7.62x51毫米 M80 | 58dd3ad986f77403051cba8f | Caliber762x51 | 80 | 43 | 67 | 820 | 17 | 38 | 0.02 | 50 |
| 7.62x51毫米 M61 | 5a6086ea4f39f99cd479502f | Caliber762x51 | 73 | 60 | 79 | 833 | 14 | 25 | 0.02 | 50 |
| 7.62x51毫米 M62 曳光弹 | 5a608bf24f39f98ffc77720e | Caliber762x51 | 82 | 42 | 65 | 820 | 14 | 38 | 0.02 | 50 |
| 7.62x51毫米 BPZ FMJ | 5e023e53d4353e3302577c4c | Caliber762x51 | 83 | 37 | 56 | 840 | 20 | 40 | 0.02 | 50 |
| 7.62x51毫米 TCW SP | 5e023e6e34d52a55c3304f71 | Caliber762x51 | 85 | 30 | 25 | 800 | 35 | 40 | 0.02 | 50 |
| 7.62x51毫米 Ultra Nosler | 5e023e88277cce2b522ff2b1 | Caliber762x51 | 105 | 15 | 40 | 800 | 70 | 20 | 0.02 | 50 |
| 7.62x51毫米 M993 | 5efb0c1bd79ff02a1f5e68d9 | Caliber762x51 | 70 | 65 | 85 | 930 | 13 | 28 | 0.02 | 50 |
| 7.62x51毫米 M80A1 | 6768c25aa7b238f14a08d3f6 | Caliber762x51 | 75 | 55 | 74 | 929 | 13 | 30 | 0.02 | 50 |

### Caliber366TKM

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| .366 TKM FMJ | 59e6542b86f77411dc52a77a | Caliber366TKM | 98 | 23 | 48 | 580 | 25 | 6.5 | 0.02 | 70 |
| .366 TKM EKO | 59e655cb86f77411dc52a77b | Caliber366TKM | 73 | 30 | 40 | 770 | 20 | 10 | 0.01 | 70 |
| .366 TKM Geksa | 59e6658b86f77411d949b250 | Caliber366TKM | 110 | 14 | 38 | 550 | 45 | 5 | 0.02 | 70 |
| .366 AP | 5f0596629e22f464da6bbdd9 | Caliber366TKM | 90 | 42 | 60 | 602 | 1 | 6.5 | 0.02 | 70 |

### Caliber9x21

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 9x21毫米 SP10 | 5a269f97c4a282000b151807 | Caliber9x21 | 59 | 22 | 39 | 410 | 20 | 40 | 0.01 | 100 |
| 9x21毫米 SP11 | 5a26abfac4a28232980eabff | Caliber9x21 | 65 | 18 | 44 | 413 | 30 | 20 | 0.01 | 100 |
| 9x21毫米 SP12 | 5a26ac06c4a282000c5a90a8 | Caliber9x21 | 80 | 15 | 63 | 415 | 35 | 20 | 0.01 | 100 |
| 9x21毫米 SP13 | 5a26ac0ec4a28200741e1e18 | Caliber9x21 | 52 | 32 | 42 | 410 | 20 | 40 | 0.01 | 100 |
| 9x21毫米 7N42“凿刀”穿甲手枪弹 | 6576f4708ca9c4381d16cd9d | Caliber9x21 | 49 | 38 | 47 | 400 | 10 | 50 | 0.01 | 100 |
| 9x21毫米 7U4 | 6576f93989f0062e741ba952 | Caliber9x21 | 53 | 27 | 44 | 300 | 25 | 30 | 0.02 | 100 |

### Caliber20g

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 20/70 7.5毫米鹿弹 | 5a38ebd9c4a282000d722a5b | Caliber20g | 25 | 3 | 14 | 430 | 0 | 0 | 0.04 | 35 |
| 20/70 5.6毫米鹿弹 | 5d6e695fa4b936359b35d852 | Caliber20g | 26 | 1 | 12 | 340 | 0 | 0 | 0.03 | 35 |
| 20/70 6.2毫米鹿弹 | 5d6e69b9a4b9361bc8618958 | Caliber20g | 22 | 2 | 13 | 410 | 0 | 0 | 0.03 | 35 |
| 20/70 7.3毫米鹿弹 | 5d6e69c7a4b9360b6c0d54e4 | Caliber20g | 23 | 3 | 13 | 475 | 0 | 0 | 0.03 | 35 |
| 20/70 Star独头弹 | 5d6e6a05a4b93618084f58d0 | Caliber20g | 154 | 16 | 42 | 415 | 10 | 10 | 0.03 | 35 |
| 20/70 Poleva-6U 独头弹 | 5d6e6a42a4b9364f07165f52 | Caliber20g | 135 | 17 | 40 | 445 | 15 | 10 | 0.03 | 35 |
| 20/70 Poleva-3 独头弹 | 5d6e6a53a4b9361bd473feec | Caliber20g | 120 | 14 | 35 | 425 | 20 | 10 | 0.03 | 35 |
| 20/70 Devastator独头弹 | 5d6e6a5fa4b93614ec501745 | Caliber20g | 198 | 5 | 13 | 405 | 100 | 10 | 0.03 | 35 |
| 20/70 TSS 穿甲独头弹 | 660137d8481cc6907a0c5cda | Caliber20g | 155 | 30 | 54 | 482 | 2 | 18 | 0.04 | 35 |
| 20/70“危险猎物”独头弹 (DGS) | 660137ef76c1b56143052be8 | Caliber20g | 143 | 25 | 47 | 476 | 3 | 15 | 0.04 | 35 |
| 20/70 箭形弹 | 6601380580e77cfd080e3418 | Caliber20g | 20 | 24 | 24 | 400 | 0 | 0 | 0.04 | 35 |

### Caliber46x30

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 4.6x30毫米 FMJ SX | 5ba2678ad4351e44f824b344 | Caliber46x30 | 43 | 40 | 41 | 620 | 20 | 40 | 0.01 | 100 |
| 4.6x30毫米 Action SX | 5ba26812d4351e003201fef1 | Caliber46x30 | 65 | 18 | 28 | 690 | 50 | 30 | 0.01 | 100 |
| 4.6x30毫米 AP SX | 5ba26835d4351e0035628ff5 | Caliber46x30 | 35 | 53 | 46 | 680 | 10 | 60 | 0.01 | 100 |
| 4.6x30毫米 Subsonic SX | 5ba26844d4351e00334c9475 | Caliber46x30 | 52 | 23 | 33 | 290 | 20 | 50 | 0.01 | 100 |
| 4.6x30毫米 JSP SX | 64b6979341772715af0f9c39 | Caliber46x30 | 46 | 32 | 37 | 579 | 30 | 35 | 0.01 | 100 |

### Caliber127x55

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 12.7x55毫米 PS12 | 5cadf6ddae9215051e1c23b2 | Caliber127x55 | 115 | 28 | 60 | 300 | 30 | 40 | 0.07 | 30 |
| 12.7x55毫米 PS12A | 5cadf6e5ae921500113bb973 | Caliber127x55 | 165 | 10 | 22 | 870 | 70 | 20 | 0.04 | 30 |
| 12.7x55毫米 PS12B | 5cadf6eeae921500134b2799 | Caliber127x55 | 102 | 46 | 57 | 570 | 30 | 50 | 0.06 | 30 |

### Caliber57x28

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 5.7x28毫米 SS190 | 5cc80f38e4a949001152b560 | Caliber57x28 | 49 | 37 | 43 | 715 | 20 | 60 | 0.01 | 100 |
| 5.7x28毫米 L191 | 5cc80f53e4a949000e1ea4f8 | Caliber57x28 | 53 | 33 | 41 | 715 | 20 | 60 | 0.01 | 100 |
| 5.7x28毫米 SB193 | 5cc80f67e4a949035e43bbba | Caliber57x28 | 59 | 27 | 37 | 299 | 35 | 30 | 0.01 | 100 |
| 5.7x28毫米 SS198LF | 5cc80f79e4a949033c7343b2 | Caliber57x28 | 70 | 17 | 19 | 792 | 80 | 20 | 0.01 | 100 |
| 5.7x28毫米 SS197SR | 5cc80f8fe4a949033b0224a2 | Caliber57x28 | 62 | 25 | 22 | 594 | 50 | 5 | 0.01 | 100 |
| 5.7x28毫米 R37.F | 5cc86832d7f00c000d3a6e6c | Caliber57x28 | 98 | 8 | 7 | 729 | 100 | 5 | 0.01 | 100 |
| 5.7x28毫米 R37.X | 5cc86840d7f00c002412c56c | Caliber57x28 | 81 | 11 | 9 | 724 | 70 | 10 | 0.01 | 100 |

### Caliber1143x23ACP

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| .45 ACP FMJ | 5e81f423763d9f754677bf2e | Caliber1143x23ACP | 72 | 25 | 36 | 340 | 1 | 6.5 | 0.02 | 100 |
| .45 RIP | 5ea2a8e200685063ec28c05a | Caliber1143x23ACP | 130 | 3 | 12 | 293 | 100 | 0.2 | 0.02 | 100 |
| .45 ACP AP | 5efb0cabfb3e451d70735af5 | Caliber1143x23ACP | 66 | 38 | 48 | 299 | 1 | 10 | 0.02 | 100 |
| .45 ACP Lasermatch FMJ | 5efb0d4f4bc50b58e81710f3 | Caliber1143x23ACP | 76 | 19 | 37 | 290 | 1 | 6.5 | 0.02 | 100 |
| .45 ACP Hydra-Shok | 5efb0fc6aeb21837e749c801 | Caliber1143x23ACP | 100 | 13 | 30 | 274 | 50 | 6.5 | 0.02 | 100 |

### Caliber23x75

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 23x75毫米“破片-10”霰弹 | 5e85a9a6eacf8c039e4e2ac1 | Caliber23x75 | 87 | 11 | 20 | 270 | 0 | 20 | 0.08 | 15 |
| 23x75毫米“红星”闪光弹 | 5e85a9f4add9fe03027d9bf1 | Caliber23x75 | 0 | 0 | 0 | 80 | 30 | 40 | 0.07 | 15 |
| 23x75毫米破障独头弹 | 5e85aa1a988a8701445df1f5 | Caliber23x75 | 192 | 39 | 75 | 420 | 20 | 40 | 0.08 | 15 |
| 23x75 毫米“破片-25”霰弹 | 5f647f31b6238e5dd066e196 | Caliber23x75 | 78 | 10 | 20 | 375 | 0 | 20 | 0.08 | 15 |

### Caliber40x46

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 40x46毫米 M406(HE) | 5ede4739e0350d05467f73e8 | Caliber40x46 | 199 | 1 | 95 | 76 | 0 | 0 | 0.23 | 1 |
| 40x46毫米 M441(HE) | 5ede47405b097655935d7d16 | Caliber40x46 | 199 | 1 | 95 | 76 | 0 | 0 | 0.23 | 1 |
| 40x46毫米 M381(HE) | 5ede474b0c226a66f5402622 | Caliber40x46 | 199 | 1 | 95 | 76 | 0 | 0 | 0.23 | 1 |
| 40x46毫米 M576(MP-APERS) | 5ede475339ee016e8c534742 | Caliber40x46 | 160 | 5 | 95 | 269 | 0 | 0 | 0.12 | 1 |
| 40x46毫米 M386(HE) | 5ede475b549eed7c6d5c18fb | Caliber40x46 | 199 | 1 | 95 | 76 | 0 | 0 | 0.23 | 1 |
| 40x46毫米 M433 (HEDP) | 5f0c892565703e5c461894e9 | Caliber40x46 | 199 | 1 | 95 | 76 | 0 | 0 | 0.23 | 1 |

### Caliber762x35

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| .300 Blackout BPZ FMJ | 5fbe3ffdf8b6a877a729ea82 | Caliber762x35 | 60 | 30 | 36 | 605 | 30 | 30 | 0.02 | 70 |
| .300 Blackout AP | 5fd20ff893a8961fc660a954 | Caliber762x35 | 51 | 48 | 65 | 635 | 10 | 30 | 0.01 | 70 |
| .300 Blackout V-Max | 6196364158ef8c428c287d9f | Caliber762x35 | 72 | 20 | 25 | 723 | 25 | 10 | 0.02 | 70 |
| .300 Whisper | 6196365d58ef8c428c287da1 | Caliber762x35 | 90 | 14 | 18 | 853 | 35 | 10 | 0.02 | 70 |
| .300 Blackout M62 曳光弹 | 619636be6db0f2477964e710 | Caliber762x35 | 54 | 36 | 40 | 442 | 20 | 37 | 0.02 | 70 |
| .300 Blackout CBJ | 64b8725c4b75259c590fa899 | Caliber762x35 | 58 | 43 | 57 | 725 | 15 | 20 | 0.02 | 70 |

### Caliber86x70

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| .338 Lapua Magnum FMJ | 5fc275cf85fd526b824a571a | Caliber86x70 | 122 | 47 | 83 | 900 | 20 | 40 | 0.05 | 30 |
| .338 Lapua Magnum AP | 5fc382a9d724d907e2077dab | Caliber86x70 | 115 | 79 | 89 | 849 | 13 | 30 | 0.05 | 30 |
| .338 Lapua Magnum TAC-X | 5fc382b6d6fa9c00c571bbc3 | Caliber86x70 | 196 | 18 | 55 | 880 | 50 | 40 | 0.04 | 30 |
| .338 Lapua Magnum UCW | 5fc382c1016cce60e8341b20 | Caliber86x70 | 142 | 32 | 70 | 849 | 60 | 40 | 0.05 | 30 |

### Caliber9x33R

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| .357 Magnum FMJ | 62330b3ed4dc74626d570b95 | Caliber9x33R | 70 | 35 | 43 | 385 | 1 | 6.5 | 0.02 | 60 |
| .357 Magnum Hollow Point | 62330bfadc5883093563729b | Caliber9x33R | 99 | 18 | 20 | 481 | 60 | 2.5 | 0.02 | 60 |
| .357 Magnum JHP | 62330c18744e5e31df12f516 | Caliber9x33R | 88 | 24 | 28 | 425 | 60 | 5.5 | 0.02 | 60 |
| .357 Magnum Soft Point | 62330c40bdd19b369e1e53d1 | Caliber9x33R | 108 | 12 | 15 | 455 | 20 | 3 | 0.02 | 60 |

### Caliber26x75

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 26x75 毫米照明信号弹（绿色） | 62389aaba63f32501b1b444f | Caliber26x75 | 37 | 0 | 60 | 80 | 0 | 100 | 0.04 | 1 |
| 26x75 毫米照明信号弹（红色） | 62389ba9a63f32501b1b4451 | Caliber26x75 | 37 | 0 | 60 | 80 | 0 | 100 | 0.04 | 1 |
| 26x75 毫米照明信号弹（白色） | 62389bc9423ed1685422dc57 | Caliber26x75 | 37 | 0 | 60 | 80 | 0 | 100 | 0.06 | 1 |
| 26x75燃烧照明信号弹（黄色） | 62389be94d5d474bf712e709 | Caliber26x75 | 37 | 0 | 60 | 80 | 0 | 100 | 0.05 | 1 |
| 26x75 毫米照明信号弹（酸绿） | 635267f063651329f75a4ee8 | Caliber26x75 | 37 | 0 | 60 | 80 | 0 | 100 | 0.04 | 1 |
| 信号弹（蓝） | 66d97834d2985e11480d5c1e | Caliber26x75 | 40 | 0 | 60 | 80 | 0 | 100 | 0.06 | 60 |
| 信号弹（新年） | 675ea4891b2579e8fe0250aa | Caliber26x75 | 40 | 0 | 60 | 80 | 0 | 100 | 0.06 | 60 |

### Caliber68x51

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 6.8x51毫米 SIG Hybrid | 6529243824cbe3c74a05e5c1 | Caliber68x51 | 72 | 47 | 58 | 914 | 12 | 30 | 0.02 | 50 |
| 6.8x51毫米 SIG FMJ | 6529302b8c26af6326029fb7 | Caliber68x51 | 80 | 36 | 49 | 899 | 18 | 27 | 0.02 | 50 |

### Caliber20x1mm

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 20x1毫米塑料片 | 6601546f86889319850bd566 | Caliber20x1mm | 1 | 0 | 0 | 20 | 0 | 0 | 0 | 50 |

### Caliber127x33

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| .50 AE FMJ | 668fe62ac62660a5d8071446 | Caliber127x33 | 85 | 40 | 50 | 440 | 1 | 8 | 0.03 | 50 |
| .50 AE JHP | 66a0d1c87d0d369e270bb9de | Caliber127x33 | 147 | 12 | 23 | 440 | 70 | 2 | 0.03 | 50 |
| .50 AE实心铜弹 | 66a0d1e0ed648d72fe064d06 | Caliber127x33 | 94 | 33 | 56 | 460 | 30 | 6 | 0.04 | 50 |
| .50 AE Hawk JSP | 66a0d1f88486c69fce00fdf6 | Caliber127x33 | 122 | 26 | 28 | 465 | 30 | 0.4 | 0.04 | 50 |
| .50 AE FMJ Bull | 6a60f36fb3d057447a095aee | Caliber127x33 | 113 | 57 | 50 | 635 | 1 | 8 | 0.04 | 50 |

### Caliber784x49

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| .308 ME | 67c540c3d0538d12ec036c08 | Caliber784x49 | 80 | 42 | 78 | 838 | 10 | 30 | 0.02 | 50 |
| .308 ME LOKT | 67c540cfb032bbdb530201b8 | Caliber784x49 | 96 | 24 | 55 | 838 | 35 | 20 | 0.02 | 50 |

### Caliber127x99

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| .50 BMG HP | 67d41936f378a36c4706eeb9 | Caliber127x99 | 260 | 34 | 22 | 880 | 12 | 2 | 0.11 | 15 |
| .50 BMG M21 | 67dc212493ce32834b0fa446 | Caliber127x99 | 220 | 45 | 55 | 867 | 7 | 15 | 0.12 | 15 |
| .50 BMG M33 | 67dc255ee3028a8b120efc48 | Caliber127x99 | 190 | 56 | 85 | 887 | 5 | 35 | 0.11 | 15 |
| .50 BMG M903 SLAP | 67dc2648ba5b79876906a166 | Caliber127x99 | 160 | 115 | 90 | 1220 | 1 | 45 | 0.1 | 15 |

### Caliber93x64

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 9.3x64毫米 SP | 68aeed8a8906b00bc800fdd6 | Caliber93x64 | 129 | 37 | 55 | 697 | 50 | 40 | 0.04 | 30 |
| 9.3x64毫米 FMJ | 68bac6ca653ee6b1e406d978 | Caliber93x64 | 115 | 44 | 55 | 793 | 50 | 40 | 0.04 | 30 |
| 9.3x64毫米 7N33 | 68bad8376cb22acf1107a586 | Caliber93x64 | 108 | 56 | 55 | 790 | 50 | 40 | 0.04 | 30 |

### Caliber58x42

| 中文名 | id（24位） | 口径 | 肉伤 damage | 穿透 penetrationPower | 甲伤 armorDamage | 初速 initialSpeed | 碎弹率% | 跳弹率% | 重量 weight | 叠放 stackMaxSize |
|---|---|---|---|---|---|---|---|---|---|---|
| 5.8x42毫米 DBP191 | 6a07208057b2695f9d001e63 | Caliber58x42 | 53 | 39 | 43 | 840 | 50 | 40 | 0.01 | 80 |
| 5.8x42毫米 DBX95 | 6a42661705016139300b2085 | Caliber58x42 | 57 | 33 | 40 | 910 | 50 | 40 | 0.01 | 80 |
| 5.8x42毫米 DVC12 | 6a42662fcb506840dd053827 | Caliber58x42 | 46 | 52 | 60 | 872 | 50 | 40 | 0.01 | 80 |
| 5.8x42毫米 DVX12 | 6a426637ddc63098d100ae67 | Caliber58x42 | 48 | 47 | 54 | 850 | 50 | 40 | 0.01 | 80 |

## 3. 额外属性（tracer / projectileCount / 出血修正）

> 仅列非空值。tracer=true 的弹药（26 条）全列；projectileCount>1（多弹丸）与出血修正非 0 的弹药数量较多，以计数说明。

### 3.1 tracer 弹药（26 条）

| 中文名 | id | tracerColor | projectileCount | penetrationChance | lightBleedModifier | heavyBleedModifier |
|---|---|---|---|---|---|---|
| 5.45x39毫米 BT | 56dff061d2720bb5668b4567 | tracerRed | 1 | 0.66 | 0 | 0 |
| 5.45x39毫米 T | 56dff4a2d2720bbd668b456a | tracerRed | 1 | 0.54 | 0 | 0 |
| 7.62x25毫米 TT PT | 573603c924597764442bd9cb | tracerRed | 1 | 0.18 | 0 | 0 |
| 9x18毫米 PM PPT | 57371e4124597760ff7b25f1 | red | 1 | 0.09 | 0.15 | 0 |
| 7.62x39毫米 T45M1 | 59e4cf5286f7741778269d8a | red | 1 | 0.37 | 0 | 0 |
| 5.56x45毫米 M856 | 59e68f6f86f7746c9f75e846 | red | 1 | 0.55 | 0 | 0 |
| 5.56x45毫米 M856A1 | 59e6906286f7746c9f75e847 | red | 1 | 0.55 | 0 | 0 |
| 9x21毫米 SP13 | 5a26ac0ec4a28200741e1e18 | red | 1 | 0.6 | 0 | 0 |
| 7.62x51毫米 M62 曳光弹 | 5a608bf24f39f98ffc77720e | green | 1 | 0.67 | 0.1 | 0.1 |
| 9x19毫米 绿色曳光弹 | 5c3df7d588a4501f290594e5 | tracerGreen | 1 | 0.2 | 0 | 0 |
| 5.7x28毫米 L191 | 5cc80f53e4a949000e1ea4f8 | red | 1 | 0.5 | 0 | 0 |
| 7.62x54R T-46M | 5e023cf8186a883be655e54f | green | 1 | 0.77 | 0 | 0 |
| 7.62x54R 7BT1 | 5e023d34e8a400319a28ed44 | green | 1 | 0.82 | 0 | 0 |
| .45 ACP Lasermatch FMJ | 5efb0d4f4bc50b58e81710f3 | red | 1 | 0.16 | 0 | 0 |
| .300 Blackout M62 曳光弹 | 619636be6db0f2477964e710 | red | 1 | 0.4 | 0 | 0 |
| 26x75 毫米照明信号弹（绿色） | 62389aaba63f32501b1b444f | yellow | 1 | 0 | 0 | 0 |
| 26x75 毫米照明信号弹（红色） | 62389ba9a63f32501b1b4451 | yellow | 1 | 0 | 0 | 0 |
| 26x75 毫米照明信号弹（白色） | 62389bc9423ed1685422dc57 | yellow | 1 | 0 | 0 | 0 |
| 26x75燃烧照明信号弹（黄色） | 62389be94d5d474bf712e709 | yellow | 1 | 0 | 0 | 0 |
| 26x75 毫米照明信号弹（酸绿） | 635267f063651329f75a4ee8 | yellow | 1 | 0 | 0 | 0 |
| 信号弹（蓝） | 66d97834d2985e11480d5c1e | yellow | 1 | 0 | 0 | 0 |
| 信号弹（新年） | 675ea4891b2579e8fe0250aa | yellow | 1 | 0 | 0 | 0 |
| .50 BMG M21 | 67dc212493ce32834b0fa446 | red | 1 | 0.6 | 0.5 | 0.3 |
| 7.62x25毫米 TT M856A1 | 68c15b4bb30038a118088bd6 | red | 1 | 0.26 | 0.26 | 0.07 |
| 5.8x42毫米 DBX95 | 6a42661705016139300b2085 | green | 1 | 0.5 | 0 | 0 |
| 5.8x42毫米 DVX12 | 6a426637ddc63098d100ae67 | green | 1 | 0.5 | 0 | 0 |

### 3.2 数量统计

- projectileCount > 1（霰弹/多弹丸）：**15** 条
- lightBleedModifier > 0：**77** 条
- heavyBleedModifier > 0：**86** 条
- 说明：上述字段为 0 或缺失的弹药未列出（如 M855 tracer=false、bleed=0）；需要全量值请直接查快照 items.json。

## 4. SPT 对照

### 4.1 批次 0 结论（详见 `archive/tarkov-dev/snapshot-2026-09-02/IMAGE-REPORT.md`）

- 抽样 20 种弹药、100 字段，与 SPT 数据库对照：**100% 一致**（damage / penetrationPower / armorDamage / initialSpeed / fragmentationChance 全部相同）
- 结论：tarkov.dev 数值与 SPT（同源于游戏文件 dump）在已收录弹药上完全吻合

### 4.2 覆盖差异

- live（types 含 ammo）：**212**；SPT（`_parent` = Ammo 分类 5485a8684bdc2da71d8b4567）：**208**
- live 有而 SPT 无：**25**（手榴弹、弹药包、1.1.0 新增弹药等）
- SPT 有而 live types 无 `ammo`：**21**（榴弹破片、12.7x108、23x75 催泪弹等，SPT 分类口径不同）
- 主要成因：手榴弹/气枪弹/信号弹/弹药包在两侧分类口径不同（live types 含 `ammo`，SPT `_parent` 指向其他节点）；live 1.1.0 新增弹药 SPT 0.16.9.5 未收录

## 5. 使用建议

- 写 mod 时以弹药 id 在 SPT 数据库（`SPT_Data/database/templates/items.json`）查 `_props` 为数值权威；本表用于快速浏览与口径分组
- live-only 弹药（如 1.1.0 新增）在 SPT 4.1 无对应物品，引用前须确认存在性
- 中文名以 items_zh.json 为准（与 SPT ch.json 同源），normalizedName 与中文名并非总是字面对应（如 59e77a2386f7742ee578960a）
- 碎弹率/跳弹率为 0~1 比例值，与 SPT `FragmentationChance`/`RicochetChance` 数值一致
