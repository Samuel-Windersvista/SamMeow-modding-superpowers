---
version: [live-ref]
domain: both
topic: reference-data
source: curated
---

# 易物/制作/藏身处数据参考 [live-ref]

> 元信息
>
> - 快照日期：2026-09-02（tarkov.dev 全量 dump）
> - live EFT：1.1.0.1.46911
> - SPT：4.1.2（锁定 0.16.9.5.40743）
> - [live-ref] 声明：本表为 live 参考数据，易物/制作配方与数值仅供对照；SPT 侧权威以 `SPT_Data/database/traders/<id>/assort.json`、`SPT_Data/database/hideout/production.json` 与 `areas.json` 为准。live 与 SPT 数量差异见第 5 节，勿将 live 数值直接写死进 mod 配置
> - 数据源（只读）：`knowledge/spt-kb/archive/tarkov-dev/snapshot-2026-09-02/barters.json`（789 条）、`crafts.json`（214 条）、`hideout.json`（26 站）、`hideout_zh.json`（藏身处中文名）、`items_zh.json`（物品中文名，join key 为 "<id> Name"）
> - SPT 对照源（只读）：`E:\Game\EFT_Offline\SPT_410\SPT_Runtime\SPT_Data\database\hideout\`、`database\traders\<id>\assort.json`
> - 物品名 join 命中率：barters 涉及 937 个唯一物品 id、crafts 涉及 360 个唯一物品 id，均 100% join 成功（无 raw id 泄漏）

## 1. 易物（barters）

### 1.1 统计

- 总数：**789** 条（live tarkov.dev 快照）
- 按商人分组（降序）：

| 商人 | trader id | 易物数 |
|---|---|---|
| 竞技场裁判 | `6617beeaa9cfa777ca915b7c` | 332 |
| Mechanic | `5a7c2eca46aef81a7ca2145d` | 108 |
| Prapor | `54cb50c76803fa8b248b4571` | 69 |
| Ragman | `5ac3b934156ae10c4430e83c` | 67 |
| Jaeger | `5c0647fdd443bc2504c2d371` | 61 |
| Peacekeeper | `5935c25fb3acc3127c3d8cd9` | 60 |
| Therapist | `54cb57776803fa99248b456e` | 58 |
| Skier | `58330581ace78e27b8b10cee` | 34 |

> 注意：live 商人含 竞技场裁判 Ref（332 条，占 42%）等后期商人；SPT 4.1 的 assort.json 为 SPT 自维护集合，与 live 数量不一一对应（见 5.2 节）。
> 任务解锁：66 条易物带 `taskUnlock`（需完成对应任务后解锁）。

### 1.2 全量易物表（按商人分组，组内按 minTraderLevel 升序）

> 列含义：换出（requiredItems 汇总）→ 换得（offeredItem）；等级=minTraderLevel（商人最低等级）；限购=buyLimit（每次刷新可购数量）；任务解锁=taskUnlock（— 表示无任务限制）。

| 换出 → 换得 | 商人 | 等级 | 限购 | 任务解锁 |
|---|---|---|---|---|
| GP币×0.42 → 抓痕巴拉克拉瓦头套×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → 半面巾（白色头套图样）×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → 黄色恶鬼巴拉克拉瓦头套×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → Cold Fear 隔热巴拉克拉瓦头套（热带复合迷彩）×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.62 → Work Peak 运动面罩 (鲨鱼嘴)×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → 氯丁橡胶面具（自由摔跤）×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → 半面巾（绿色头套图样）×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×8.88 → DevTaс 武士下颊 防弹面具 (白色)×1 | 竞技场裁判 | 1 | 2 | — |
| GP币×0.63 → 摔跤传奇假胡子×1 | 竞技场裁判 | 1 | 1 | — |
| GP币×0.42 → 绿色巴拉克拉瓦头套×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → 红鼻子巴拉克拉瓦头套×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → 氯丁橡胶面具（M90 沙漠迷彩）×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → Cold Fear 隔热巴拉克拉瓦头套（绿色）×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → 竞技场杯系列巴拉克拉瓦×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×1.25 → Cold Fear 隔热巴拉克拉瓦头套（Kukla 迷彩）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×1.25 → Cold Fear 隔热巴拉克拉瓦头套 (M90)×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×0.62 → Work Peak 运动面罩 (黄色)×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.62 → Work Peak 运动面罩×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.63 → Cold Fear 隔热巴拉克拉瓦头套（台风）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×0.63 → “Zaebtsa”巴拉克拉瓦头套×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×0.63 → Cold Fear 隔热巴拉克拉瓦头套（高原复合迷彩）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×0.62 → Work Peak 运动面罩 (健身房打卡)×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.62 → Work Peak 运动面罩 (灰色)×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.63 → Cold Fear 隔热巴拉克拉瓦头套（外星人）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×11.29 → Death Shadow (死亡阴影) 轻量化防弹面具 (灰色)×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.63 → 骠骑兵式小胡子×1 | 竞技场裁判 | 1 | 2 | — |
| GP币×0.63 → 独角兽巴拉克拉瓦头套×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×12.45 → 极寒天气面具×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.62 → 半面巾（圣诞骷髅）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×1.25 → Cold Fear 隔热巴拉克拉瓦头套（蓝色苔纹）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×1.25 → Cold Fear 隔热巴拉克拉瓦头套（沙漠防夜视迷彩）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×1.25 → Cold Fear 隔热巴拉克拉瓦头套（沙漠防夜视迷彩）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×67.67 → Wilcox Skull Lock头罩基座 PVS-14×1 | 竞技场裁判 | 1 | 2 | — |
| GP币×15.02 → NFM "HJELM" 头盔×1 | 竞技场裁判 | 1 | 2 | — |
| GP币×1.25 → 德国斑点迷彩棒球帽 (Zaebtsa)×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×1.25 → 棒球帽（波士顿）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×1.25 → 棒球帽（黑系复合迷彩）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×1.25 → 白桦迷彩棒球帽 (Zaebtsa)×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×0.63 → 竞技场棒球帽×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → 雷硼飞行员墨镜（绿色镜片）×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.42 → Dundukk 运动太阳镜（橙色镜片）×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.61 → 百叶窗太阳镜 (白色)×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×0.63 → 斐格力太阳镜 (绿色)×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.63 → 斐格力太阳镜 (红色)×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.63 → 斐格力太阳镜×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×0.61 → 大佬墨镜×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×1.24 → Revision ShadowStrike 战术眼镜（黄褐色）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×0.61 → 百叶窗太阳镜 (黑色)×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×1.24 → Revision ShadowStrike 战术眼镜（灰色）×1 | 竞技场裁判 | 1 | 5 | — |
| GP币×1.25 → 26x75 毫米照明信号弹（绿色）×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×228.28 → 钥匙收纳器×1 | 竞技场裁判 | 1 | 1 | — |
| GP币×22.53 → BNTI Zhuk（甲虫）防弹衣（战地记者版） 默认×1 | 竞技场裁判 | 1 | 2 | — |
| GP币×9.64 → NPP KlASS Kora-Kulon防弹衣×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×13.69 → BNTI Kirasa-N（胸甲-N）防弹衣（绿色）×1 | 竞技场裁判 | 1 | 2 | — |
| GP币×4.7 → IRBIS-GUN 45AL 前握把×1 | 竞技场裁判 | 1 | 3 | — |
| GP币×75.29 → 西蒙诺夫 OP-SKS 7.62x39 卡宾枪 TA11D×1 | 竞技场裁判 | 1 | 2 | — |
| GP币×57.59 → AKM 7.62x39 突击步枪 TA01NSN×1 | 竞技场裁判 | 1 | 2 | — |
| GP币×55.7 → AK-105 5.45x39 突击步枪 HHS-1×1 | 竞技场裁判 | 1 | 1 | — |
| GP币×41.22 → PP-19-01 勇士 9x19 冲锋枪 Zenit×1 | 竞技场裁判 | 1 | 1 | 5ac3462b86f7741d6118b983 |
| GP币×51.37 → SIG MPX 9x19 冲锋枪 Romeo8T×1 | 竞技场裁判 | 1 | 1 | — |
| 传奇奖章×1、GP币×100 → Shatun的藏身处钥匙×1 | 竞技场裁判 | 1 | 1 | — |
| GP币×18.75 → 野营燃料桶×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×0.42 → 氯丁橡胶面具（僵尸）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×0.42 → Cold Fear 隔热巴拉克拉瓦头套（生存迷彩）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×0.42 → Cold Fear 隔热巴拉克拉瓦头套（DPM 沙漠迷彩）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×0.42 → Cold Fear 隔热巴拉克拉瓦头套（SBEU）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×0.42 → 赤鬼巴拉克拉瓦头套×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×0.42 → 半面巾（红色头套图样）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×8.88 → DevTaс 武士下颊 防弹面具×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×0.42 → 半面巾（僵尸）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×0.42 → 疤脸巴拉克拉瓦头套×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×22.55 → MSA Gallet TC 800 High Cut 作战头盔×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×16.25 → Galvion 凯门鳄 复合防弹头盔（高原复合迷彩）×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×16.25 → Galvion 凯门鳄 复合防弹头盔（共生者）×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×16.25 → Galvion 凯门鳄 复合防弹头盔（复合迷彩）×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×2.49 → Wiley X SPEAR 防护眼镜（黑色）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×2.49 → Wiley X SPEAR 防护眼镜（黄褐色）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×4.85 → 7.62x51mm TCW SP 弹药包（20发装）×1 | 竞技场裁判 | 2 | 10 | — |
| GP币×12.95 → 5.56x45mm MK 318 Mod 0 (SOST) 弹药包（100发装）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×11.19 → 12.7x55mm PS12 弹药包（10发装）×1 | 竞技场裁判 | 2 | 12 | — |
| GP币×1.97 → 5.45x39mm FMJ弹药包（30发装）×1 | 竞技场裁判 | 2 | 15 | — |
| GP币×5.67 → 5.7x28mm SS197SR 弹药包（50发装）×1 | 竞技场裁判 | 2 | 6 | — |
| GP币×13.02 → 5.45x39mm PP弹药包（120发装）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×8.72 → Walker's Razor 数字耳机（ΜΟΛΩΝ ΛΑΒΕ）×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×3.73 → Opsmen Earmor M32 耳机（白色）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×11.22 → Safariland Liberator HP 2.0听力保护耳机（黑系复合迷彩）×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×7.5 → Hazard 4 Takedown 单肩背包（复合迷彩）×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×60.23 → ANA Tactical M2 插板胸挂（橄榄绿） 默认×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×9.16 → BlackRock 胸挂（卡其色）×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×7.46 → Direct Action Thunderbolt 紧凑型胸挂（丛林绿）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×8.74 → Haley Strategic D3CRX 胸挂（黑色）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×6.99 → WARTECH TV-115 插板胸挂（黑色）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×248.75 → 防弹插板箱×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×104.55 → 手榴弹箱×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×157.68 → 弹药箱×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×206.67 → 钥匙箱×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×355.92 → 医疗物品箱×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×190.87 → 弹匣箱×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×53.37 → Interceptor OTV防弹衣（通用迷彩） 默认×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×34.29 → NFM THOR 隐蔽型强化防弹背心（头眼）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×28.82 → Interceptor OTV 防弹衣（林地迷彩）×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×6.29 → Galvion 凯门鳄 复合防弹护颚（高原复合迷彩）×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×12.6 → Galvion 凯门鳄 复合防弹贴片（高原复合迷彩）×1 | 竞技场裁判 | 2 | 2 | — |
| GP币×4.99 → IRBIS-GUN 30AL 前握把×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×7.53 → LAS/TAC 2战术手电×1 | 竞技场裁判 | 2 | 5 | — |
| GP币×7.5 → Vortex Razor AMG UH-1全息瞄具×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×12.52 → FALKE LE 反射式瞄具×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×12.52 → SwampFox Justice 反射式瞄具×1 | 竞技场裁判 | 2 | 3 | — |
| GP币×11.27 → X Products HK MP5 9x19 X-5 50发弹鼓×1 | 竞技场裁判 | 2 | 4 | — |
| GP币×1.25 → DI Optical FC1 瞄具基座×1 | 竞技场裁判 | 2 | 5 | — |
| GP币×1.25 → DI Optical FC1 增高座×1 | 竞技场裁判 | 2 | 5 | — |
| GP币×2.49 → Phase5 AR-15 通用迷你枪托（红色）×1 | 竞技场裁判 | 2 | 5 | — |
| GP币×2.49 → Phase5 AR-15 通用迷你枪托（黄色）×1 | 竞技场裁判 | 2 | 5 | — |
| GP币×2.49 → Phase5 AR-15 通用迷你枪托×1 | 竞技场裁判 | 2 | 5 | — |
| GP币×117.56 → 柯尔特 M4A1 5.56x45 卡宾枪 SAI×1 | 竞技场裁判 | 2 | 1 | 5ae3280386f7742a41359364 |
| GP币×72.67 → SIG MCX .300 Blackout 突击步枪 T-1×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×81.36 → AK-104 7.62x39 突击步枪 RPKT mod.1×1 | 竞技场裁判 | 2 | 1 | 5ae3267986f7742a413592fe |
| GP币×89.18 → Rifle Dynamics RD-704 7.62x39突击步枪 T-1×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×106.59 → 奈特军械公司 (KAC) SR-25 7.62x51 精确射手步枪 TAC 30×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×76.66 → HK MP7A2 4.6x30 冲锋枪 T-1×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×67.68 → HK MP5 9x19 冲锋枪（海军三发点射） XPS3-0×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×70.05 → FN P90 5.7x28 冲锋枪 CWDG×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×57.7 → B&T MP9-N 9x19 冲锋枪 ACRO P-1×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×55 → ZiD SP-81 26x75信号枪×1 | 竞技场裁判 | 2 | 2 | — |
| 传奇奖章×3 → APOK ”荒原之地“ 战术短剑×1 | 竞技场裁判 | 2 | 1 | 69789418b2187365e70bb947 |
| GP币×1.2 → V40 微型手榴弹×1 | 竞技场裁判 | 2 | 4 | — |
| 传奇奖章×1、GP币×100 → Grumpy的藏身处钥匙×1 | 竞技场裁判 | 2 | 1 | — |
| GP币×9.97 → Steiner R1X 反射式瞄具×1 | 竞技场裁判 | 2 | 5 | — |
| GP币×8.72 → DI Optical FC1 反射式瞄具×1 | 竞技场裁判 | 2 | 5 | — |
| GP币×4.85 → 7.62x51毫米 TCW SP×20 | 竞技场裁判 | 2 | 10 | — |
| GP币×12.95 → 5.56x45毫米 Mk 318 Mod 0 (SOST)×100 | 竞技场裁判 | 2 | 3 | — |
| GP币×11.19 → 12.7x55毫米 PS12×10 | 竞技场裁判 | 2 | 12 | — |
| GP币×1.97 → 5.45x39毫米 FMJ×30 | 竞技场裁判 | 2 | 15 | — |
| GP币×5.67 → 5.7x28毫米 SS197SR×50 | 竞技场裁判 | 2 | 6 | — |
| GP币×13.02 → 5.45x39毫米 PP×120 | 竞技场裁判 | 2 | 3 | — |
| GP币×41.28 → 金属燃料桶×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×26.39 → Atomic Defense CQCM 防弹面具（微笑）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×0.42 → 半面巾（鬼魂）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×0.42 → 骷髅巴拉克拉瓦头套×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×0.42 → “为死而生”巴拉克拉瓦头套×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×0.42 → 半面巾（复合迷彩）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×0.42 → Cold Fear 隔热巴拉克拉瓦头套（虎纹迷彩）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×0.42 → 半面巾（苔藓迷彩）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×0.42 → “惹不起”巴拉克拉瓦头套×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×26.39 → Atomic Defense CQCM 防弹面具（骷髅）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×0.42 → “恐惧”巴拉克拉瓦头套×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×0.42 → Cold Fear 隔热巴拉克拉瓦头套（橡木纹）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×27.55 → Atomic Defense CQCM防弹面罩（冰爆）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×26.39 → Atomic Defense CQCM 防弹面具 (Stop Me)×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×26.39 → Atomic Defense CQCM 防弹面具（战疤）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×26.31 → Atomic Defense CQCM 防弹面具（恶魔獠牙）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×26.31 → Atomic Defense CQCM 防弹面具（路易伪登）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×26.31 → Atomic Defense CQCM 防弹面具（碰撞测试）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×26.39 → Atomic Defense CQCM 防弹面具（亡灵节）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×26.31 → Atomic Defense CQCM 防弹面具（生化警告）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×25.11 → HighCom Striker ULACH IIIA 头盔（黑色）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×28.85 → HighCom Striker ULACH IIIA 头盔（沙色）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×23.86 → Diamond Age NeoSteel 高切头盔 (橙斑)×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×23.86 → Diamond Age NeoSteel 高切头盔 (王牌)×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×24.88 → Crye Precision AirFrame 头盔（老派风格） 默认×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×27.61 → HighCom Striker ULACH IIIA 头盔（网格喷漆）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×27.61 → HighCom Striker ULACH IIIA 头盔（狼棕条纹）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×27.61 → HighCom Striker ULACH IIIA 头盔（冬季网格喷漆）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×22.45 → Crye Precision AirFrame 头盔（鲨鱼嘴）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×12.43 → Highcom Striker ACHHC IIIA 头盔（狼棕色）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×24.93 → Crye Precision AirFrame M-LOK 头盔（公牛）×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×28.85 → HighCom Striker ULACH IIIA 头盔（绿色条纹）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×0.42 → 圆框太阳镜（绿色镜片）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×42.5 → 26x75 毫米照明信号弹（红色）×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×11.76 → 5.7x28mm SS190弹药包（50发装）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×16.86 → .300 Blackout CBJ弹药包（50发装）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×17.24 → 9x21mm SP13 弹药包（30发装）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×15.32 → 7.62x51mm M80 弹药包（20发装）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×57.52 → 武器维修套件×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×16.2 → Peltor ComTac VI 耳机（丛林绿）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×14.93 → TW EXFIL Peltor ComTac VI 耳机（丛林绿）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×18.77 → Peltor TEP-300 战术耳塞（狼棕色）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×9.95 → Safariland Liberator HP 2.0听力保护耳机（黑色）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×14.93 → TW EXFIL Peltor ComTac VI 耳机（黑色）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×16.2 → Peltor ComTac VI 耳机（黑色）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×9.95 → Safariland Liberator HP 2.0听力保护耳机（复合迷彩）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×11.35 → Gruppa 99 T30背包（黑色）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×11.26 → ANA Tactical Alpha 胸挂（复合迷彩）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×62.59 → CQC 鱼鹰 MK4A 防弹胸挂（突击型，多地形迷彩） 默认×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×30.4 → FirstSpear Strandhogg 插板胸挂（ABU 迷彩）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×27.46 → Crye Precision AVS 插板胸挂（复合迷彩）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×11.26 → ANA Tactical Alpha 胸挂（A-TACS AU）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×24.23 → Stich Profi V2 插板胸挂（MARPAT 林地迷彩）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×25.4 → Stich Profi V2 插板胸挂（A-TACS FG）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×64.65 → FirstSpear Strandhogg 插板胸挂（黑系复合迷彩）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×8.81 → WARTECH MK3 TV-104 胸挂 (苔藓迷彩)×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×25.46 → Stich Profi V2 插板胸挂（灰褐色）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×7.45 → Stich Profi MK2胸挂（侦察型，数码丛林迷彩）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×32.7 → ANA Tactical M2 插板胸挂（高原复合迷彩）×1 | 竞技场裁判 | 3 | 2 | — |
| 传奇奖章×1、GP币×220 → 武器箱×1 | 竞技场裁判 | 3 | 1 | — |
| 传奇奖章×1、GP币×150 → 物品箱×1 | 竞技场裁判 | 3 | 1 | — |
| 传奇奖章×10、GP币×200 → Theta 安全箱×1 | 竞技场裁判 | 3 | 1 | — |
| 传奇奖章×5、GP币×100 → Theta 安全箱×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×23.98 → HighCom Trooper TFO 防弹背心（郊狼棕）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×45.42 → HighCom Trooper TFO 防弹背心（复合迷彩） 默认×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×8.72 → Monoclete三级PE防弹插板×1 | 竞技场裁判 | 3 | 4 | — |
| GP币×7.46 → Crye Precision AirFrame 护耳（复合迷彩）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×6.29 → Galvion 凯门鳄 复合防弹护颚（复合迷彩）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×6.29 → Galvion 凯门鳄 复合防弹护颚（共生者）×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×7.46 → Crye Precision AirFrame 护耳（灰褐色）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×7.46 → Crye Precision AirFrame 护耳（黑色）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×4.39 → Stark SE-5 Express Forward握把（FDE）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×3.47 → SE-5 Express握把×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×7.52 → EOTech EXPS3-0 全息瞄具（黄褐色）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×37.54 → EOTech Vudu 1-6x24 30 毫米 步枪瞄准镜×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×50.04 → Vortex Razor HD Gen.2 1-6x24 30 毫米步枪瞄准镜×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×71.29 → L3Harris GPNVG-18 夜视仪×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×7.51 → 6L31 5.45x39 AK-74 60发弹匣×1 | 竞技场裁判 | 3 | 4 | — |
| GP币×13.35 → RPK-16 5.45x39 95发弹鼓×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×1.25 → Strike Industries GRIDLOK AR-15 护木延长段 (黑色)×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×1.25 → Strike Industries GRIDLOK AR-15 护木延长段 (黄色)×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×1.25 → Strike Industries GRIDLOK AR-15 护木延长段 (红色)×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×2.51 → Daniel Defense Enhanced AR-15 伸缩式枪托（黑色）×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×1.26 → Daniel Defense Enhanced AR-15 伸缩式枪托 (FDE)×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×1.24 → Strike Industries GRIDLOK AR-15 护木框架式基座 (黑色)×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×2.5 → Strike Industries GRIDLOK 11 英寸 AR-15 护木×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×1.24 → Strike Industries GRIDLOK AR-15 护木框架式基座 (红色)×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×1.24 → Strike Industries GRIDLOK AR-15 护木框架式基座 (黄色)×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×1.23 → Strike Industries GRIDLOK 8.5 英寸 AR-15 护木×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×2.52 → Tyrant Designs MOD Chevron AR-15 镂空手枪式握把（红色）×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×2.52 → Tyrant Designs MOD Chevron AR-15 镂空手枪式握把（黑色）×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×2.54 → Gladman AK 镂空手枪式握把×1 | 竞技场裁判 | 3 | 3 | — |
| GP币×2.52 → Tyrant Designs MOD Chevron AR-15 镂空手枪式握把（黄色）×1 | 竞技场裁判 | 3 | 5 | — |
| GP币×83.61 → 雷电 Model 1 FA 5.56x45 突击步枪 默认×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×95.56 → CMMG Mk47 Mutant 7.62x39 突击步枪 EXPS3-0×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×90.56 → DS Arms SA-58 7.62x51 突击步枪 XPS3-0×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×77.4 → AK-12 5.45x39 突击步枪 GP-25×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×92.94 → 柯尔特 M4A1 5.56x45 卡宾枪 EXPS3-0×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×101.58 → AK-74N 5.45x39 突击步枪 PS320 1/6x×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×81.01 → FN SCAR-L 5.56x45突击步枪 （FDE） Contract Wars×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×124.34 → 雷明顿 R11 RSASS 7.62x51 精确射手步枪 TANGO6T×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×68.55 → Saiga-12K 12 铅径自动霰弹枪（红线） 默认×1 | 竞技场裁判 | 3 | 1 | — |
| 传奇奖章×1、GP币×100 → Voron的藏身处钥匙×1 | 竞技场裁判 | 3 | 1 | — |
| GP币×17.5 → TerraGroup实验室访问钥匙卡×1 | 竞技场裁判 | 3 | 2 | — |
| GP币×11.76 → 5.7x28毫米 SS190×50 | 竞技场裁判 | 3 | 3 | — |
| GP币×16.86 → .300 Blackout CBJ×50 | 竞技场裁判 | 3 | 3 | — |
| GP币×17.24 → 9x21毫米 SP13×30 | 竞技场裁判 | 3 | 3 | — |
| GP币×15.32 → 7.62x51毫米 M80×20 | 竞技场裁判 | 3 | 2 | — |
| GP币×155.1 → 显示卡×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×25.13 → Atomic Defense CQCM 防弹面具（恶魔）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×0.42 → 奢华巴拉克拉瓦头套×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×0.42 → 诡笑巴拉克拉瓦头套×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×0.42 → 氯丁橡胶面具（路易伪登）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×0.42 → 白色恶鬼巴拉克拉瓦头套×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×0.42 → 黄色巴拉克拉瓦头套×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×0.42 → 天狗恶魔巴拉克拉瓦头套×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×0.42 → 氯丁橡胶面具（鬼）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×0.42 → 半面巾（亡灵节）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×0.42 → 氯丁橡胶面具（非礼勿言）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×25.13 → Atomic Defense CQCM 防弹面具（标靶）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×149.06 → Altyn 防弹头盔（橄榄绿） Face shield×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×55.09 → DevTac 浪人面罩（永恒之绿）×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×98.88 → Vulkan-5 (火神) LShZ-5 重型防弹头盔 (丛林迷彩)×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×17.35 → Atlant Armour Titan（泰坦）芳纶头盔（复合迷彩）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×22.45 → Crye Precision AirFrame M-LOK 头盔（黑色）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×25 → Team Wendy EXFIL 防弹头盔（复合迷彩）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×98.88 → Vulkan-5 (火神) LShZ-5 重型防弹头盔 (8 号球)×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×22.44 → Atlant Armour Titan（泰坦）芳纶头盔 (Rudiarius)×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×98.88 → Vulkan-5 (火神) LShZ-5 重型防弹头盔 (烈焰)×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×55.09 → DevTac 浪人面罩（野兽）×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×22.45 → Atlant Armour Titan（泰坦）芳纶头盔（橄榄绿）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×42.63 → 冠军头盔×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×0.42 → Oakley SI M Frame 护目镜（橙色镜片）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×29.6 → .338 Lapua Magnum FMJ弹药包（20发装）×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×15.05 → 6.8x51mm SIG FMJ 弹药包（20发装）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×27.39 → 7.62x54mm R 7BT1 弹药包（20发装）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×17.14 → 9x39mm SP-6 弹药包（20发装）×1 | 竞技场裁判 | 4 | 5 | — |
| GP币×27.38 → 7.62x51mm M61 弹药包（20发装）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×12.5 → 12.7x55mm PS12B（10发装）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×19.62 → 7.62x39mm BP 弹药包（20发装）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×16.79 → .45 ACP AP弹药包（50发装）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×18.25 → 5.56x45mm M855A1 弹药包（50发装）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×99 → 防弹衣维修套件×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×25.03 → Eberlestock F4 终结者 承重背包（虎纹迷彩）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×62 → 5.11 Tactical TacTec 插板胸挂（风暴灰）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×69.79 → Ars Arma CPC MOD.1 插板胸挂（A-TACS FG 迷彩）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×107.89 → FORT Gladiator-S（格斗-S）插板胸挂（无惧死亡）×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×12.62 → LBT-1961A 承重胸挂（沙漠防夜视迷彩）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×16.25 → LBT-1961A 承重胸挂（AOR1）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×16.26 → LBT-1961A 承重胸挂（复合迷彩）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×92.41 → FORT Gladiator-S（格斗-S）轻型插板胸挂（维京）×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×87.59 → FORT Gladiator-S（格斗-S）轻型插板胸挂（复合迷彩）×1 | 竞技场裁判 | 4 | 1 | — |
| 传奇奖章×2、GP币×350 → THICC 武器箱×1 | 竞技场裁判 | 4 | 1 | — |
| 传奇奖章×3、GP币×500 → THICC 物品箱×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×12.55 → BNTI Zhuk（甲虫）防弹衣（数码丛林迷彩）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×51.56 → FORT Redut-M（堡垒-M）防弹衣（韩国林地迷彩）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×24.7 → Hexatac HPC 插板背心（复合迷彩）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×141.31 → IOTV Gen4 防弹衣（高机动型，复合迷彩） 默认×1 | 竞技场裁判 | 4 | 1 | 6615141bfda04449120269a7 |
| GP币×187.53 → 6B43 屏障-Sh 防弹衣（数码丛林迷彩） 默认×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×68.19 → FORT Redut-M（堡垒-M）防弹衣（竞技场之囚）×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×88.39 → FORT Gladiator-S（格斗-S）插板胸挂（灰色）×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×85.58 → FORT Defender-2 防弹衣（德国斑点迷彩）×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×32.77 → NPP KlASS Korund-VM（刚玉-VM）防弹衣（虎纹迷彩）×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×68.19 → FORT Redut-M（堡垒-M）防弹衣（黑色）×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×56.3 → Granit 4防弹插板（前部）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×50 → SAPI III+ 级防弹插板×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×12.6 → Galvion 凯门鳄 复合防弹贴片（共生者）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×11.29 → Diamond Age NeoSteel 头盔护颚 (黑色)×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×9.95 → Crye Precision AirFrame 护颚（复合迷彩）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×9.95 → Crye Precision AirFrame 护颚（黑色）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×12.6 → Galvion 凯门鳄 复合防弹贴片（复合迷彩）×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×11.29 → Diamond Age NeoSteel 头盔护颚 (王牌)×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×9.95 → Crye Precision AirFrame 护颚（鲨鱼嘴）×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×16.31 → Hexagon Wafflemaker 5.45x39 AK 消音器×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×13.8 → AAC 762 SDN-6 7.62x51声音抑制器 ×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×20 → MaxRounds Powermag 12/76 20发SOK-12兼容武器弹匣×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×25.02 → Magpul PMAG D-60 5.56x45 STANAG 60发弹鼓×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×5.04 → Strike Industries GRIDLOK 17 英寸 AR-15 护木×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×3.81 → Strike Industries GRIDLOK 15 英寸 AR-15 护木×1 | 竞技场裁判 | 4 | 3 | — |
| GP币×92.51 → SIG MCX SPEAR 6.8x51突击步枪 HHS-1褐×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×88.1 → AS VAL“巨浪” 9x39 特种突击步枪 BOSS Xe×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×74.19 → FN SCAR-H 7.62x51突击步枪 UH-1×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×114.03 → 柯尔特 M4A1 5.56x45 卡宾枪 XPS3-0×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×70.26 → ASh-12 12.7x55 突击步枪 TAC×1 | 竞技场裁判 | 4 | 2 | — |
| GP币×97.08 → 沙漠科技 MDR 7.62x51 突击步枪 HCO×1 | 竞技场裁判 | 4 | 1 | — |
| 传奇奖章×1、GP币×80 → SWORD International Mk-18 .338 LM 精确射手步枪 Thor PSR×1 | 竞技场裁判 | 4 | 1 | — |
| 传奇奖章×1、GP币×40 → FN GL-40 Mk.2 榴弹发射器 默认×1 | 竞技场裁判 | 4 | 1 | — |
| 传奇奖章×1、GP币×100 → Leon的藏身处钥匙×1 | 竞技场裁判 | 4 | 1 | — |
| GP币×29.6 → .338 Lapua Magnum FMJ×20 | 竞技场裁判 | 4 | 1 | — |
| GP币×15.05 → 6.8x51毫米 SIG FMJ×20 | 竞技场裁判 | 4 | 3 | — |
| GP币×27.39 → 7.62x54R 7BT1×20 | 竞技场裁判 | 4 | 2 | — |
| GP币×17.14 → 9x39毫米 SP-6×20 | 竞技场裁判 | 4 | 5 | — |
| GP币×27.38 → 7.62x51毫米 M61×20 | 竞技场裁判 | 4 | 2 | — |
| GP币×12.5 → 12.7x55毫米 PS12B×10 | 竞技场裁判 | 4 | 3 | — |
| GP币×19.62 → 7.62x39毫米 BP×20 | 竞技场裁判 | 4 | 3 | — |
| GP币×16.79 → .45 ACP AP×50 | 竞技场裁判 | 4 | 2 | — |
| GP币×18.25 → 5.56x45毫米 M855A1×50 | 竞技场裁判 | 4 | 3 | — |
| CPU风扇×1 → 气体分析仪×1 | Mechanic | 1 | 1 | — |
| 钳子×1、螺丝刀×1、扳手×1、绝缘胶带×1、施工用测量卷尺×1 → 一套工具×1 | Mechanic | 1 | 2 | — |
| 钳子×1、剪线钳×1、圆嘴钳×1 → Elite钳子×1 | Mechanic | 1 | 2 | — |
| 电子元件×1、充电宝×1 → WIFI摄像头×1 | Mechanic | 1 | 4 | — |
| 云尔斯顿香烟×1、阿波罗-联盟香烟×2 → 本地网络黑客入侵装置×1 | Mechanic | 1 | 3 | — |
| 电容×1、电磁铁×1、印制电路板×1 → 录音机×1 | Mechanic | 1 | 5 | — |
| 罐装铝热剂×2、“Eagle”火药×2 → 弹药箱×1 | Mechanic | 1 | 1 | 60e71d6d7fcf9c556f325055 |
| 螺母×1 → Silencerco Hybrid 46直接螺纹转接器×1 | Mechanic | 1 | 3 | — |
| 电钻×1 → SilencerCo Osprey 9 9x19毫米抑制器×1 | Mechanic | 1 | 2 | — |
| 罐装白盐×2 → Alpha Dog Alpha 9 9x19声音抑制器×1 | Mechanic | 1 | 5 | — |
| 损坏的GPhone×2、损坏的液晶显示屏×3 → L3Harris AN/PVS-14 单筒夜视仪×1 | Mechanic | 1 | 1 | — |
| 电钻×1、损坏的GPhone×1 → Armasight Vulcan MG 3.5x Bravo夜视瞄准镜×1 | Mechanic | 1 | 5 | — |
| 云尔斯顿香烟×1 → Badger Ordnance 战术掣子销 AR-15 拉机柄×1 | Mechanic | 1 | 3 | — |
| 马宝路香烟×1 → P226 9x19扩容弹匣×1 | Mechanic | 1 | 5 | — |
| USB适配器×1 → NCStar M1911A1 扳机保护基座×1 | Mechanic | 1 | 3 | — |
| 一号电池×1 → Magpul M-LOK 4.1英寸导轨×1 | Mechanic | 1 | 10 | — |
| 五号电池×2、一号电池×2 → M870 Magpul SGA 聚合物枪托×1 | Mechanic | 1 | 3 | — |
| 电脑CPU×1 → Tapco INTRAFUSE SKS枪托×1 | Mechanic | 1 | 2 | — |
| 损坏的硬盘×1 → MP-133有导轨的定制塑料下护木×1 | Mechanic | 1 | 5 | — |
| 可充电电池×1 → M870 Magpul MOE 护木×1 | Mechanic | 1 | 3 | — |
| 一号电池×2 → Zenit B-11 AKS-74U 护木×1 | Mechanic | 1 | 2 | — |
| 无彩香烟×1 → P226 Stainless Elite 木制握把贴片×1 | Mechanic | 1 | 3 | — |
| 钳子×1 → P226 Stainless Elite 手枪套筒×1 | Mechanic | 1 | 3 | — |
| 电灯泡×5 → Glock 17 9x19 手枪 Viper×1 | Mechanic | 1 | 2 | — |
| 电线×1、电容×2 → 托卡列夫 TT-33 7.62x25 手枪 Brunner×1 | Mechanic | 1 | 2 | — |
| 损坏的硬盘×5 → Glock 17 9x19 手枪 Hex×1 | Mechanic | 1 | 5 | — |
| Kinda牛仔帽×3 → Glock 17 9x19 手枪 Tac 3×1 | Mechanic | 1 | 2 | 5ac2428686f77412450b42bf |
| Tetriz便携式游戏机×1 → 20x1毫米玩具枪 默认×1 | Mechanic | 1 | 10 | — |
| 充电宝×2、武器零件×4 → 雷明顿 Model 870 12铅径泵动式霰弹枪 Breacher×1 | Mechanic | 1 | 1 | — |
| Elite钳子×2、螺丝刀×1 → MP-133 12 铅径泵动式霰弹枪 Tactical×1 | Mechanic | 1 | 2 | — |
| 完好的液晶显示屏×5、NIXXOR镜头×1 → HK MP7A1 4.6x30 冲锋枪 SEALS×1 | Mechanic | 1 | 1 | — |
| DVD光驱×5、紫外线灯泡×5、武器零件×2 → SIG MPX 9x19 冲锋枪 MQB×1 | Mechanic | 1 | 2 | — |
| DVD光驱×1、损坏的硬盘×1、电容×2 → HK UMP .45 ACP冲锋枪 默认×1 | Mechanic | 1 | 1 | — |
| 劳力土潜水金腕表×1 → PP-19-01 勇士 9x19 冲锋枪 Zenit×1 | Mechanic | 1 | 1 | 5ac3462b86f7741d6118b983 |
| Inseq燃气管扳手×1 → IWI UZI 9x19冲锋枪 SD×1 | Mechanic | 1 | 1 | — |
| 电磁铁×1、NIXXOR镜头×1 → HK MP5 9x19 冲锋枪（海军三发点射） SD×1 | Mechanic | 1 | 1 | — |
| 损坏的液晶显示屏×2 → 完好的液晶显示屏×1 | Mechanic | 2 | 10 | — |
| Tetriz便携式游戏机×2、GreenBat锂电池×2 → 实体比特币×1 | Mechanic | 2 | 1 | — |
| Kinda牛仔帽×1 → .308 ME 弹药包 (20发装)×1 | Mechanic | 2 | 1 | — |
| 实体比特币×5 → 武器箱×1 | Mechanic | 2 | 1 | — |
| 电磁铁×2 → Rotor 43 7.62x39 消音器×1 | Mechanic | 2 | 4 | — |
| 电线×3、电容×2 → Silencerco Hybrid 46多口径消音器×1 | Mechanic | 2 | 3 | — |
| 固态硬盘×2、长平头螺丝刀×1 → ELCAN SpecterDR 1x/4x瞄准镜 FDE×1 | Mechanic | 2 | 1 | — |
| 电线×3 → X Products HK MP5 9x19 X-5 50发弹鼓×1 | Mechanic | 2 | 3 | — |
| T形插座×6 → ProMag 7.62x39 AK-A-16 73发弹鼓×1 | Mechanic | 2 | 1 | — |
| Poxeram冷焊膏×2 → SGMT Glock 9x19 50发弹鼓×1 | Mechanic | 2 | 2 | — |
| 印制电路板×3 → UltiMAK M14 M8 Forward 光学瞄具基座×1 | Mechanic | 2 | 3 | — |
| DVD光驱×2 → I-E-A Mil Optics Magmount 34毫米一体式基座环×1 | Mechanic | 2 | 3 | — |
| 圆嘴钳×1、剪线钳×1 → M14 UTG 4点锁定式高级基座×1 | Mechanic | 2 | 1 | — |
| 技术指导文件×1、五号电池×1 → B&T MP5SD 三导轨环形基座×1 | Mechanic | 2 | 3 | — |
| 400毫升WD-40×1 → FAB Defense GL-SHOCK AR-15 枪托×1 | Mechanic | 2 | 3 | — |
| 充电宝×1 → M1A Archangel枪托×1 | Mechanic | 2 | 3 | — |
| 马宝路香烟×3、阿波罗-联盟香烟×2 → 10.3 英寸 AR-15 枪管 5.56x45×1 | Mechanic | 2 | 3 | — |
| CPU风扇×2 → Daniel Defence RIS II 9.5 英寸 AR-15 规格护木（狼棕色）×1 | Mechanic | 2 | 4 | — |
| 电子元件×1 → Zenit B-30 护木 + B-31S 上导轨×1 | Mechanic | 2 | 1 | — |
| 一盒牛奶×1 → AS VAL Rotor 43 手枪式握把附缓冲管转接器×1 | Mechanic | 2 | 3 | — |
| 武器零件×3 → 西蒙诺夫 OP-SKS 7.62x39 卡宾枪 UAS SKS×1 | Mechanic | 2 | 2 | — |
| Prokill带链奖章×2 → 柯尔特 M4A1 5.56x45 卡宾枪 SOPMOD II×1 | Mechanic | 2 | 2 | — |
| 电子元件×2、火花塞×2 → Steyr AUG A1 5.56x45突击步枪 默认×1 | Mechanic | 2 | 1 | — |
| 绝缘胶带×8 → 春田 M1A 7.62x51 步枪 EBR×1 | Mechanic | 2 | 3 | — |
| 电磁铁×2 → Glock 17 9x19 手枪 Spartan×1 | Mechanic | 2 | 2 | — |
| 400毫升WD-40×2 → Glock 17 9x19 手枪 PS9×1 | Mechanic | 2 | 2 | — |
| 损坏的GPhone×2、印制电路板×1 → Glock 17 9x19 手枪 Tac 2×1 | Mechanic | 2 | 2 | — |
| 莫辛步枪狙击型卡宾枪托×1、730毫米标准莫辛枪管×1、云尔斯顿香烟×1 → 莫辛-纳甘 7.62x54R 栓动式步枪（狙击型） Obrez M×1 | Mechanic | 2 | 1 | — |
| 军用电缆×2、节能灯泡×5 → 雷明顿Model 700 7.62x51 狙击步枪 AAC SD×1 | Mechanic | 2 | 1 | — |
| 管道扳手×1、Inseq燃气管扳手×1 → RSP-30 反应式信号弹（蓝色）×1 | Mechanic | 2 | 3 | 6744af0969a58fceba101fed |
| 管道扳手×1、Inseq燃气管扳手×1 → RSP-30 反应式信号弹（蓝色）×1 | Mechanic | 2 | 3 | 6744af0969a58fceba101fed |
| UZRGM手榴弹引信×1 → VOG-17 Khattabka 简易手榴弹×1 | Mechanic | 2 | 5 | — |
| CPU风扇×2、印制电路板×2、五号电池×4 → 疗养院东楼 328 房间钥匙×1 | Mechanic | 2 | 1 | — |
| 实体比特币×1 → Elektronik 的钥匙×1 | Mechanic | 2 | 5 | — |
| Kinda牛仔帽×1 → .308 ME×20 | Mechanic | 2 | 1 | — |
| 汽车蓄电池×7 → 显示卡×1 | Mechanic | 3 | 3 | 6744af0969a58fceba101fed |
| 汽车蓄电池×7 → 显示卡×1 | Mechanic | 3 | 3 | 6744af0969a58fceba101fed |
| 军用转速计×1 → .300 Blackout CBJ弹药包（50发装）×1 | Mechanic | 3 | 5 | 675c1ff1a757ddd00404f0aa |
| 电动马达×8、电线×15、损坏的液晶显示屏×4、相控阵单元×1 → 武器箱×1 | Mechanic | 3 | 1 | — |
| Poxeram冷焊膏×1 → UZI 9x19标准50发弹匣×1 | Mechanic | 3 | 5 | — |
| 武器零件×8 → DS Arms SA-58 7.62x51 突击步枪 X-FAL×1 | Mechanic | 3 | 5 | — |
| 马宝路香烟×4、军用电缆×1 → AK-74M 5.45x39 突击步枪 Zenitco×1 | Mechanic | 3 | 4 | — |
| 武器零件×12 → DS Arms SA-58 7.62x51 突击步枪 SPR×1 | Mechanic | 3 | 1 | — |
| UHF RFID固定式读取器 ×4、Virtex可编程处理器×2 → AK-104 7.62x39 突击步枪 T-SAW×1 | Mechanic | 3 | 1 | 5ac3464c86f7741d651d6877 |
| 加密磁带盒×2、长平头螺丝刀×1 → FN SCAR-H 7.62x51 突击步枪 (FDE) 默认×1 | Mechanic | 3 | 1 | — |
| 云尔斯顿香烟×8 → 春田 M1A 7.62x51 步枪 SASS×1 | Mechanic | 3 | 1 | — |
| 电容×8、武器零件×2 → RPK-16 5.45x39 轻机枪 默认×1 | Mechanic | 3 | 2 | — |
| 军用电路板×1、Cyclon 蓄电池组×1 → RPDN 7.62x39 轻机枪 OKP-7 DT×1 | Mechanic | 3 | 5 | — |
| Bulbex剪线器×1、管道扳手×1 → TerraGroup实验室访问钥匙卡×1 | Mechanic | 3 | 2 | — |
| 军用转速计×1 → .300 Blackout CBJ×50 | Mechanic | 3 | 5 | 675c1ff1a757ddd00404f0aa |
| 军用转速计×5、军用电缆×10、UHF RFID固定式读取器 ×5、军用COFDM无线信号发射器×8 → GPS信号放大单元×1 | Mechanic | 4 | 1 | 639670029113f06a7c3b2377 |
| Virtex可编程处理器×3、显示卡×1、损坏的GPhone X×5、VPX闪存模块×5 → 微控制器电路板×1 | Mechanic | 4 | 1 | 63966fbeea19ac7ed845db2e |
| 电子元件×30、相控阵单元×10、Virtex可编程处理器×10、VPX闪存模块×10 → 直流变压器×1 | Mechanic | 4 | 1 | 63967028c4a91c5cb76abd81 |
| 实体比特币×10、黄金骷髅指环×10 → THICC 武器箱×1 | Mechanic | 4 | 1 | 5c0bde0986f77479cf22c2f8 |
| 气体分析仪×1 → AK-50 .50 BMG 膛口制退器×1 | Mechanic | 4 | 1 | 684009026ceedc792c09b2a7 |
| Iridium军用热成像模块×2、军用转速计×2、军用电路板×3 → FLIR RS-32 2.25-9x 35毫米 60Hz热成像步枪瞄准镜×1 | Mechanic | 4 | 1 | — |
| Iridium军用热成像模块×4、军用电路板×4、军用电源滤波器×4 → Trijicon REAP-IR热成像步枪瞄准镜×1 | Mechanic | 4 | 1 | 64f83bcdde58fc437700d8fa |
| 盖革-穆勒计数器×1 → X Products 7.62x51 M14 X-14 50发弹鼓×1 | Mechanic | 4 | 5 | — |
| 损坏的GPhone×1 → X Products 7.62x51 SA58/FAL X-FAL 50发弹匣×1 | Mechanic | 4 | 10 | — |
| 圆嘴钳×4 → MaxRounds Powermag 12/76 20发SOK-12兼容武器弹匣×1 | Mechanic | 4 | 2 | — |
| 内存条×1、电子元件×3 → Beta C-Mag 5.56x45 AR-15 100发弹鼓×1 | Mechanic | 4 | 2 | — |
| Baddie的红胡子×1 → UZI 9x19 Beta C-Mag 100发弹鼓×1 | Mechanic | 4 | 3 | — |
| 缝纫锥×1、TP-200 砖型TNT×3 → X Products 7.62x51 AR-10 X-25 50发弹鼓×1 | Mechanic | 4 | 5 | — |
| 管道扳手×1、燃油添加剂×2 → 24 英寸 AK-50 .50 BMG 枪管×1 | Mechanic | 4 | 1 | 684009026ceedc792c09b2a7 |
| 压力表×1 → AK-50 M-LOK 护木与导气管组合×1 | Mechanic | 4 | 1 | 684009026ceedc792c09b2a7 |
| 损坏的硬盘×2 → AK-50 防尘盖×1 | Mechanic | 4 | 1 | 684009026ceedc792c09b2a7 |
| Prokill带链奖章×1 → AS“巨浪”MOD.4 9x39 特种突击步枪 默认×1 | Mechanic | 4 | 2 | 68db9c7557bc51a8c804c14b |
| NIXXOR镜头×2、LVNDMARK的老鼠药×1 → DS Arms SA-58 7.62x51 突击步枪 PBR×1 | Mechanic | 4 | 1 | — |
| 微控制器电路板×1、Iridium军用热成像模块×1 → 柯尔特 M4A1 5.56x45 卡宾枪 ECHO1×1 | Mechanic | 4 | 1 | — |
| VPX闪存模块×1、军用转速计×2 → Saiga-12K ver.10 12铅径半自动霰弹枪 Type340×1 | Mechanic | 4 | 1 | — |
| TerraGroup "蓝色文件夹" 材料×1 → PKM 7.62x54R 机枪 默认×1 | Mechanic | 4 | 1 | 64f83bd983cfca080a362c82 |
| 6-STEN-140-M军用电池×5、Cyclon 蓄电池组×8、UHF RFID固定式读取器 ×2、加密磁带盒×1 → 实验室钥匙卡·黑×1 | Mechanic | 4 | 1 | 676529af9c90953d090882e7 |
| 一盒牛奶×1 → UMTBS 6Sh112 Scout-Sniper胸挂（数码丛林迷彩）×1 | Prapor | 1 | 2 | — |
| 5升丙烷罐×1 → 6B23-1 防弹衣（数码丛林迷彩） 默认×1 | Prapor | 1 | 1 | — |
| 西柚汁×1 → Zhuk-3防弹插板（前部）×1 | Prapor | 1 | 3 | — |
| 火花塞×2 → BelOMO PSO-1M2-1 4x24 瞄准镜×1 | Prapor | 1 | 5 | — |
| 阿波罗-联盟香烟×2 → PNV-57E 夜视仪×1 | Prapor | 1 | 3 | — |
| Xenomorph发泡密封胶×1 → 6L18 5.45x39 AK-74 45发弹匣×1 | Prapor | 1 | 7 | — |
| 钳子×1 → 7.62x39 AK 30发弹匣（1955 年后配发）×1 | Prapor | 1 | 4 | — |
| 武器零件×1 → PPSh-41 7.62x25 71发弹鼓×1 | Prapor | 1 | 2 | — |
| 炖牛肉罐头×3 → AKM 7.62x39 突击步枪 默认×1 | Prapor | 1 | 1 | — |
| USB适配器×2、马宝路香烟×1 → AKS-74UB 5.45x39 短突击步枪 默认×1 | Prapor | 1 | 2 | — |
| AMG-10 液压油×2 → AK-308 7.62x51 突击步枪 Vudu 1-6×1 | Prapor | 1 | 2 | 69ce1de03e15cd80bd06f6c9 |
| 电脑CPU×1 → 雅利金 MP-443 “乌鸦” 9x19 手枪 默认×1 | Prapor | 1 | 2 | — |
| 螺母×1 → 马卡洛夫9x18PM 手枪 默认×1 | Prapor | 1 | 5 | — |
| 螺栓×1 → 斯捷奇金 APS 9x18PM 冲锋手枪 默认×1 | Prapor | 1 | 3 | — |
| Tarkovskaya瓶装伏特加×1 → 列别杰夫 PL-15 9x19 手枪 默认×1 | Prapor | 1 | 5 | — |
| T形插座×2 → PP-19-01 勇士 9x19 冲锋枪 默认×1 | Prapor | 1 | 2 | — |
| Clin 玻璃清洁剂×3 → PP-91-01 雪松-B 9x18PM 冲锋枪 默认×1 | Prapor | 1 | 2 | — |
| 一号电池×2、五号电池×2 → 莫辛-纳甘 7.62x54R 栓动式步枪（狙击型） PU 3.5x×1 | Prapor | 1 | 1 | — |
| 节能灯泡×1 → F-1手榴弹×1 | Prapor | 1 | 2 | — |
| 5升丙烷罐×2、燃油添加剂×2 → 金属燃料桶×1 | Prapor | 2 | 1 | — |
| 马宝路香烟×1 → 7.62x39mm PS弹药包（20发装）×1 | Prapor | 2 | 5 | — |
| 炖美味牛肉罐头（大）×2 → 5.45x39mm BT弹药包（30发装）×1 | Prapor | 2 | 2 | — |
| 模拟温度计×1 → 6L31 5.45x39 AK-74 60发弹匣×1 | Prapor | 2 | 6 | — |
| 技术指导文件×1 → VSS/VAL 9x39 6L25 20发弹匣（黑红色）×1 | Prapor | 2 | 5 | — |
| 卫生纸×2、牙膏×2 → AK-74 5.45x39 突击步枪 Plum×1 | Prapor | 2 | 1 | — |
| 阿波罗-联盟香烟×5 → AK-74M 5.45x39 突击步枪 默认×1 | Prapor | 2 | 2 | — |
| “Kite”火药×1 → 谢尔久科夫 SR1MP 9x21 “斑蝰蛇” 半自动手枪 默认×1 | Prapor | 2 | 2 | — |
| 阿波罗-联盟香烟×2、马宝路香烟×2 → SV-98 7.62x54 狙击步枪 默认×1 | Prapor | 2 | 2 | — |
| Tarkovskaya瓶装伏特加×2、炖牛肉罐头×3 → RPK-16 5.45x39 轻机枪 6L26×1 | Prapor | 2 | 1 | — |
| Aquamari带滤嘴水瓶×2、防毒面具滤罐×1 → 6Kh5 刺刀×1 | Prapor | 2 | 2 | — |
| 绒球毛线帽×2 → MPL-50挖掘工具×1 | Prapor | 2 | 2 | — |
| 节能灯泡×1 → RGD-5手榴弹×1 | Prapor | 2 | 3 | — |
| Zibbo打火机×2、一号电池×2 → 宿舍303房间钥匙×1 | Prapor | 2 | 1 | — |
| 马宝路香烟×1 → 7.62x39毫米 PS×20 | Prapor | 2 | 5 | — |
| 炖美味牛肉罐头（大）×2 → 5.45x39毫米 BT×30 | Prapor | 2 | 2 | — |
| 袖珍日记×1 → NPP KIASS Condor 护目镜×1 | Prapor | 3 | 4 | — |
| 绝缘胶带×2 → 9x39mm SPP弹药包（8发装）×1 | Prapor | 3 | 50 | — |
| 金蛋×1 → 刚玉-VM 防弹插板（前部）×1 | Prapor | 3 | 2 | 59ca1a6286f774509a270942 |
| Smoked Chimney下水道清洁剂×2 → AK-12 5.45x39消音器×1 | Prapor | 3 | 2 | — |
| Tarkovskaya瓶装伏特加×2 → Valday PS-320 1x/6x瞄准镜×1 | Prapor | 3 | 1 | — |
| 加密U盘×5、地形调查地图×2 → Cyclone Shakhin 3.7x 热成像瞄准镜×1 | Prapor | 3 | 1 | — |
| 情报文件夹×1、OFZ 30x165毫米炮弹×2 → GP-25“篝火”40 毫米下挂式榴弹发射器×1 | Prapor | 3 | 1 | 59ca1a6286f774509a270942 |
| 钳子×4 → VSS/VAL SR3M.130 9x39 30发弹匣×1 | Prapor | 3 | 5 | — |
| 碱性换热器表面洗涤剂×2 → RPK-16 5.45x39 95发弹鼓×1 | Prapor | 3 | 2 | — |
| 军用波纹软管×1 → SAG MK1 SVD 框架平台×1 | Prapor | 3 | 3 | — |
| Schaman洗发水×1 → Lynx Arms SVDS AK 手枪式握把转接器×1 | Prapor | 3 | 3 | — |
| 相位控制继电器×1 → SVDS定制防尘盖×1 | Prapor | 3 | 3 | — |
| GreenBat锂电池×1、可充电电池×2 → Tokarev SVT-40 7.62x54R步枪 Sniper×1 | Prapor | 3 | 1 | — |
| 古董茶壶×2 → Tokarev AVT-40 7.62x54R自动步枪 默认×1 | Prapor | 3 | 1 | 626bd75c71bd851e971b82a5 |
| “Eagle”火药×3、金属零件×4 → AK-12 5.45x39 突击步枪 默认×1 | Prapor | 3 | 5 | — |
| 棘轮扳手×1、“Kite”火药×1 → VSS “绞丝机” 特种狙击步枪  PSO-1M2-1×1 | Prapor | 3 | 1 | — |
| 军用电缆×1、五号电池×5 → RSh-12 12.7x55转轮手枪 默认×1 | Prapor | 3 | 2 | — |
| 施工用测量卷尺×5、T形插座×5、USB适配器×5 → TOZ KS-23M 23x75 毫米泵动式霰弹枪 Drozd×1 | Prapor | 3 | 1 | — |
| “Kite”火药×2、武器零件×3 → RPK-16 5.45x39 轻机枪 Drum×1 | Prapor | 3 | 2 | — |
| 绝缘胶带×2 → 9x39毫米 SPP×8 | Prapor | 3 | 50 | — |
| Cyclon 蓄电池组×1、军用转速计×2 → 6Sh118 突击背包（数码丛林迷彩）×1 | Prapor | 4 | 1 | 5e4d4ac186f774264f758336 |
| 地形调查地图×2、相位控制继电器×1 → 6B43 屏障-Sh 防弹衣（数码丛林迷彩）×1 | Prapor | 4 | 3 | — |
| USEC 狗牌×10 → 6B45 防弹衣 默认×1 | Prapor | 4 | 1 | — |
| 5升丙烷罐×2 → Maska-1SCh 面罩（橄榄绿）×1 | Prapor | 4 | 1 | — |
| Tarkovskaya瓶装伏特加×1 → AK AK-EVO枪托×1 | Prapor | 4 | 5 | — |
| USEC 狗牌×5 → SR-3M 9x39紧凑型突击步枪 FF3×1 | Prapor | 4 | 1 | — |
| 军用电路板×1、电线×1 → ASh-12 12.7x55 突击步枪 Silenced×1 | Prapor | 4 | 1 | — |
| Tarkovskaya瓶装伏特加×2 → ASh-12 12.7x55 突击步枪 默认×1 | Prapor | 4 | 2 | 63a9ae24009ffc6a551631a5 |
| 金公鸡塑像×2 → AK-12 5.45x39 突击步枪 Silenced×1 | Prapor | 4 | 1 | 675c1ff1a757ddd00404f0aa |
| 军用COFDM无线信号发射器×1 → AKS-74U 5.45x39 短突击步枪 Waffle×1 | Prapor | 4 | 1 | — |
| Prokill带链奖章×1 → TKPD 9.3x64 突击卡宾枪 XPS3-2×1 | Prapor | 4 | 2 | 68ee1c18b4e5bc9a68018cd7 |
| Arseniy包装荞麦×4 → 谢尔久科夫 SR1MP 9x21 “斑蝰蛇” 半自动手枪 Tactical 2×1 | Prapor | 4 | 1 | — |
| 波纹软管×1、军用波纹软管×1 → TOZ KS-23M 23x75 毫米泵动式霰弹枪 默认×1 | Prapor | 4 | 1 | — |
| Bulbex剪线器×1、管道扳手×1 → SR-2M 石楠 9x21 冲锋枪 FSB×1 | Prapor | 4 | 4 | 64e7b9a4aac4cd0a726562cb |
| 缝纫锥×1 → 肥皂×1 | Ragman | 1 | 1 | — |
| 马雕像×1 → 绒球毛线帽×1 | Ragman | 1 | 10 | — |
| Memento 服务器内存模块×2 → Rys-T 防弹头盔（黑色）×1 | Ragman | 1 | 5 | 69ce21e990144e437802b1e0 |
| A-2607 95Kh18 匕首×1 → 带耳罩的苏联毛帽×1 | Ragman | 1 | 3 | — |
| 项链×1 → 带耳罩的苏联毛帽×1 | Ragman | 1 | 10 | — |
| 卫生纸×3 → GSSh-01有源耳机×1 | Ragman | 1 | 2 | — |
| 遮脸半面巾×3 → PACA 软质防弹背心×1 | Ragman | 1 | 3 | — |
| 罐装 Max Energy 能量饮料×2 → MF-UNTAR防弹背心×1 | Ragman | 1 | 1 | — |
| 乌鸦雕像×1 → NPP KlASS Kora-Kulon防弹衣×1 | Ragman | 1 | 1 | — |
| BD 狗牌 •\| 铁灰×1 → Gentex Ops-Core SOTR 面罩×1 | Ragman | 2 | 2 | — |
| Ox漂白剂×2 → 6B47 Ratnik-BSh 头盔（数码迷彩盔罩）×1 | Ragman | 2 | 5 | — |
| 防破片护目镜×2 → UNTAR头盔×1 | Ragman | 2 | 3 | — |
| 施工用测量卷尺×1 → 战术羊绒帽（黄褐色）×1 | Ragman | 2 | 4 | — |
| Ox漂白剂×2 → 6B47 Ratnik-BSh 头盔（极地数码迷彩盔罩）×1 | Ragman | 2 | 2 | — |
| 经典火柴×2 → Peltor ComTac 2 耳机（橄榄绿）×1 | Ragman | 2 | 5 | — |
| Veritas吉他拨片×1 → MSA Sordin Supreme PRO-X/L有源耳机×1 | Ragman | 2 | 2 | — |
| Cordura聚酰胺面料×1 → LBT-1476A 3 日行军背包（林地迷彩）×1 | Ragman | 2 | 1 | — |
| 马雕像×1 → Hazard 4 Pillbox 背包 （黑色）×1 | Ragman | 2 | 1 | — |
| Cordura聚酰胺面料×1 → LBT-1476A 3 日行军背包 (高原复合迷彩)×1 | Ragman | 2 | 2 | — |
| 管道胶带×5、碱性换热器表面洗涤剂×1 → ANA Tactical Alpha胸挂×1 | Ragman | 2 | 1 | — |
| 6B3TM-01 防弹胸挂（卡其色）×2 → 6B3TM-01 防弹胸挂（卡其色）×1 | Ragman | 2 | 2 | — |
| 方便面×5、罐装热棒能量饮料×1 → Tac-Kek JayPC 插板胸挂（橄榄绿） 默认×1 | Ragman | 2 | 2 | — |
| 项链×3、古董茶壶×1 → 6B3TM-01 防弹胸挂（卡其色）×1 | Ragman | 2 | 1 | — |
| 士腻架能量棒×3、罐装热棒能量饮料×1 → Tac-Kek JayPC 插板胸挂（黑色） 默认×1 | Ragman | 2 | 2 | — |
| 袖珍日记×2 → NFM THOR 隐蔽型强化防弹背心 默认×1 | Ragman | 2 | 2 | — |
| 金公鸡塑像×1 → Interceptor OTV防弹衣（通用迷彩） 默认×1 | Ragman | 2 | 2 | — |
| 有机玻璃片×2 → ZSh-1-2M面罩×1 | Ragman | 2 | 3 | — |
| 宿舍楼 314 房间符号钥匙×1、疗养院西楼 112 办公室钥匙×1、尼龙绳索×5 → 冲击斧×1 | Ragman | 2 | 1 | — |
| KEKTAPE管道胶带×1 → Kinda牛仔帽×1 | Ragman | 3 | 3 | — |
| BD 狗牌 •\| 绿色×1 → Mystery Ranch 2 日突击包 (黑色)×1 | Ragman | 3 | 2 | — |
| BD 狗牌 •\| 铁灰×2 → Tasmanian Tiger Modular Pack 45 Plus 模块化背包 (黑系复合迷彩)×1 | Ragman | 3 | 2 | — |
| 狗牌×10 → Gruppa 99 T30背包 （复合迷彩）×1 | Ragman | 3 | 2 | — |
| 古董茶壶×1 → Poyas-A + Poyas-B 复合胸挂×1 | Ragman | 3 | 3 | — |
| “诺文斯基核动力”金牌格瓦斯600毫升装×2、Aquamari带滤嘴水瓶×1 → ANA Tactical M1 插板胸挂（橄榄绿） 默认×1 | Ragman | 3 | 3 | — |
| 炼乳罐头×3 → Velocity Systems多用途巡逻背心×1 | Ragman | 3 | 3 | — |
| 针线盒×1、Cordura聚酰胺面料×2、Tarkovskaya瓶装伏特加×1 → FirstSpear Strandhogg 插板胸挂（丛林绿） 默认×1 | Ragman | 3 | 2 | — |
| 《天雷地火》录像带×1、应急用水×1 → Shellback Tactical Banshee 插板胸挂（A-TACS AU 迷彩） 默认×1 | Ragman | 3 | 1 | — |
| Viibiin运动鞋×2 → Stich Profi V2 插板胸挂（黑色） 默认×1 | Ragman | 3 | 3 | — |
| BD 狗牌 •\| 绿色×1 → Ferro Concepts FCPC V5 插板胸挂 (黑色军团)×1 | Ragman | 3 | 2 | — |
| 金项链×1、罐装 Majaica 咖啡豆×2 → BNTI Gzhel-K（彩瓷-K）防弹衣×1 | Ragman | 3 | 3 | — |
| 有机玻璃片×3 → LSHZ-2DTM面罩×1 | Ragman | 3 | 3 | — |
| Altyn面罩×3 → Altyn面罩×1 | Ragman | 3 | 3 | — |
| 金公鸡塑像×1、项链×5 → 别墅后门钥匙×1 | Ragman | 3 | 1 | — |
| 显示卡×1 → Vulkan-5 (火神) LShZ-5 重型防弹头盔 (黑色)×1 | Ragman | 4 | 1 | — |
| 圆嘴钳×3、手摇钻×1、一包钉子×3 → Altyn 防弹头盔（橄榄绿）×1 | Ragman | 4 | 1 | 67d03be712fb5f8fd2096332 |
| Prokill带链奖章×1 → SSO Attack 2 突击背包（卡其色）×1 | Ragman | 4 | 1 | — |
| VPX闪存模块×1 → Mystery Ranch Blackjack 50 背包（复合迷彩）×1 | Ragman | 4 | 1 | 5e381b0286f77420e3417a74 |
| 炼乳罐头×2、一盒牛奶×1 → Tasmanian Tiger Trooper 35 背包（卡其色）×1 | Ragman | 4 | 2 | — |
| 金项链×2、托卡列夫 TT-33 7.62x25 黄金手枪×1 → Crye Precision AVS 插板胸挂（丛林绿） 默认×1 | Ragman | 4 | 1 | — |
| 狗牌×10 → ANA Tactical M2 插板胸挂（橄榄绿） 默认×1 | Ragman | 4 | 3 | — |
| Veritas吉他拨片×3、古董火镰×2 → Tasmanian Tiger MKIII 插板胸挂（狼棕色） 默认×1 | Ragman | 4 | 3 | — |
| Axel鹦鹉雕像×1、罐装白盐×2 → LBT-1961A 承重胸挂（MAS 灰色）×1 | Ragman | 4 | 2 | — |
| 木头钟×2、劳力土潜水金腕表×1 → Crye Precision JPC 插板胸挂（复合迷彩） 默认×1 | Ragman | 4 | 1 | — |
| Fierce Blow重击锤×1 → FORT Defender-2 防弹衣 默认×1 | Ragman | 4 | 1 | — |
| Axel鹦鹉雕像×3 → HighCom Trooper TFO 防弹背心（复合迷彩） 默认×1 | Ragman | 4 | 2 | — |
| 劳力土潜水金腕表×3 → Hexatac HPC 插板背心（黑系复合迷彩） 默认×1 | Ragman | 4 | 2 | — |
| VPX闪存模块×1 → IOTV Gen4 防弹衣（全面防护型，复合迷彩）×1 | Ragman | 4 | 2 | — |
| 青铜狮雕×1 → FORT Redut-T5（堡垒-T5）防弹衣（烟雾迷彩）×1 | Ragman | 4 | 2 | — |
| 破旧的古董书×2、技术指导文件×4 → FORT Redut-M（堡垒-M）防弹衣 默认×1 | Ragman | 4 | 1 | 6613f307fca4f2f386029409 |
| 破旧的古董书×1、技术指导文件×2 → FORT Redut-M（堡垒-M）防弹衣×1 | Ragman | 4 | 2 | — |
| 木头钟×3 → IOTV Gen4 防弹衣（全面防护型，复合迷彩） 默认×1 | Ragman | 4 | 1 | — |
| 青铜狮雕×3 → FORT Redut-T5（堡垒-T5）防弹衣（烟雾迷彩） 默认×1 | Ragman | 4 | 1 | — |
| 狗牌×6 → Altyn面罩×1 | Ragman | 4 | 3 | — |
| 有机玻璃片×5、瓶装 Pevko 淡啤酒×1 → Vulkan-5面罩×1 | Ragman | 4 | 1 | — |
| 有机玻璃片×4 → Altyn面罩×1 | Ragman | 4 | 3 | — |
| 有机玻璃片×4、螺栓×1 → Rys-T 面罩×1 | Ragman | 4 | 2 | — |
| 木头钟×1 → Granit Br4防弹插板×1 | Ragman | 4 | 4 | 5c1141f386f77430ff393792 |
| 经典火柴×8、Hunter火柴×5 → 金属燃料桶×1 | Jaeger | 1 | 1 | — |
| 剪线钳×1、扳手×1 → CMS手术包×1 | Jaeger | 1 | 2 | — |
| 士腻架能量棒×1 → Scav背心×1 | Jaeger | 1 | 10 | — |
| 《天雷地火》录像带×1、军用闪存装置×1 → 狗牌包×1 | Jaeger | 1 | 1 | — |
| 经典火柴×1、阿波罗-联盟香烟×1 → Kiba Arms International泵动式霰弹枪SPRM导轨×1 | Jaeger | 1 | 4 | — |
| Ortodontox牙膏×1 → Lobar Arms 30毫米瞄准镜基座×1 | Jaeger | 1 | 4 | — |
| 扳手×2、螺丝刀×2 → MP-153 12铅径半自动霰弹枪 默认×1 | Jaeger | 1 | 3 | — |
| Arseniy包装荞麦×1 → MP-133 12 铅径泵动式霰弹枪 默认×1 | Jaeger | 1 | 2 | — |
| Deadlyslob的胡须油×1 → 雷明顿Model 700 7.62x51 狙击步枪 默认×1 | Jaeger | 1 | 1 | — |
| 咸狗牛肉肠×2、Tarkovskaya瓶装伏特加×2、“凶狠跑刀崽”私酒×1 → Labrys 访问钥匙卡×1 | Jaeger | 1 | 2 | — |
| Vita果汁×2 → OLOLO瓶装复合维生素×1 | Jaeger | 2 | 2 | — |
| Emelya黑麦面包块×5 → 一包糖×1 | Jaeger | 2 | 1 | — |
| 咸狗牛肉肠×1、军用饼干×1 → Grizzly急救包×1 | Jaeger | 2 | 2 | — |
| 古董花瓶×2 → Surv12野战手术包×1 | Jaeger | 2 | 1 | 5d25aed386f77442734d25d2 |
| Aquapeps饮用水净化片×1 → 26x75 毫米照明信号弹（绿色）×1 | Jaeger | 2 | 1 | — |
| 碳酸氢钠×1 → 20/70 DGS 独头弹 弹药包（25发装）×1 | Jaeger | 2 | 4 | — |
| Hunter火柴×1 → Splav Tarzan M22 胸挂×1 | Jaeger | 2 | 2 | — |
| 罐装热棒能量饮料×10、罐装塔可乐汽水×5、鲱鱼罐头×5、罐装蔬菜泥×5 → Mr. Holodilnick保温箱×1 | Jaeger | 2 | 1 | — |
| 充电宝×1、电磁铁×2、紫外线灯泡×1 → Nightforce ATACR 7-35x56 34 毫米步枪瞄准镜×1 | Jaeger | 2 | 1 | — |
| “诺文斯基核动力”金牌格瓦斯600毫升装×3 → Burris FullField TAC30 1-4x24 30 毫米 步枪瞄准镜×1 | Jaeger | 2 | 1 | — |
| Arseniy包装荞麦×2 → Schmidt & Bender PM II 1-8x24 30 毫米步枪瞄准镜×1 | Jaeger | 2 | 2 | — |
| 一号电池×4 → UNV DLOC-IRD瞄准镜基座×1 | Jaeger | 2 | 4 | — |
| 5升丙烷罐×1 → Nightforce MagMount一体式基座环×1 | Jaeger | 2 | 3 | — |
| 压力表×1 → Magnum Research “沙漠之鹰”L5 .357 手枪 默认×1 | Jaeger | 2 | 5 | — |
| Repellent杀虫剂×2 → 雷明顿Model 700 7.62x51 狙击步枪 默认×1 | Jaeger | 2 | 1 | — |
| SP-8生存砍刀×1、6Kh5 刺刀×1、冲击斧×1 → SOG Voodoo Hawk战术斧×1 | Jaeger | 2 | 1 | 5d25bfd086f77442734d3007 |
| 技术指导文件×2、Tarkovskaya瓶装伏特加×4、炖牛肉罐头×2 → 军事基地检查站钥匙×1 | Jaeger | 2 | 1 | — |
| 工厂紧急出口钥匙×1、Iskra（“火花”）单兵口粮×3、MRE个人即食口粮×1 → 工厂紧急出口钥匙×1 | Jaeger | 2 | 1 | — |
| 碳酸氢钠×1 → 20/70“危险猎物”独头弹 (DGS)×25 | Jaeger | 2 | 4 | — |
| Repellent杀虫剂×1 → 20/70 TSS 穿甲独头弹 弹药包（25发装）×1 | Jaeger | 3 | 3 | — |
| 罐装白盐×1 → 20/70 箭形弹 弹药包（25发装）×1 | Jaeger | 3 | 2 | — |
| SurvL幸存者打火机×5、Hunter火柴×15、Repellent杀虫剂×8 → 手榴弹箱×1 | Jaeger | 3 | 1 | — |
| Iskra（“火花”）单兵口粮×2 → Bramit莫辛步枪消音器×1 | Jaeger | 3 | 3 | — |
| 一包糖×10、Aquamari带滤嘴水瓶×6 → FLIR RS-32 2.25-9x 35毫米 60Hz热成像步枪瞄准镜×1 | Jaeger | 3 | 1 | — |
| 邪教徒之刃×10 → Armasight Zeus-Pro 640 2-8x50 30Hz热成像瞄准镜×1 | Jaeger | 3 | 1 | 64ee99639878a0569d6ec8c9 |
| NIXXOR镜头×2 → MP-155 "Ultima"热成像摄像头×1 | Jaeger | 3 | 1 | 63a511ea30d85e10e375b045 |
| 瓶装 Pevko 淡啤酒×1 → Torrey Pines Logic T12W热成像反射式瞄具×1 | Jaeger | 3 | 1 | — |
| Crickent打火机×1 → REAP-IR瞄准镜眼罩×1 | Jaeger | 3 | 3 | — |
| 燃油添加剂×1 → Magnum Research “沙漠之鹰”L5 .50 AE手枪 默认×1 | Jaeger | 3 | 1 | — |
| 西鲱鱼罐头×10、Aquamari带滤嘴水瓶×10、罐装蔬菜泥×10 → Saiga-12K ver.10 12铅径半自动霰弹枪 NERFGUN×1 | Jaeger | 3 | 1 | — |
| 黑麦面包块×2、Emelya黑麦面包块×2 → Benelli M3 Super 90 12铅径双模式霰弹枪 直至黎明×1 | Jaeger | 3 | 1 | — |
| 马卡洛夫 PM(t) 9x18PM 手枪×3、托卡列夫 TT-33 7.62x25 手枪×2、“Eagle”火药×1 → ZiD SP-81 26x75信号枪×1 | Jaeger | 3 | 1 | — |
| 牙膏×5、卫生纸×5、过氧化氢溶液（双氧水）×2 → 野营斧×1 | Jaeger | 3 | 1 | — |
| 5升丙烷罐×15、燃油添加剂×10、固体燃料×15 → Red Rebel冰镐×1 | Jaeger | 3 | 1 | — |
| 卫生纸×6、Repellent杀虫剂×5、SurvL幸存者打火机×2、一包氯石灰×2 → Goshan收银机钥匙×1 | Jaeger | 3 | 1 | — |
| Hunter火柴×4、SurvL幸存者打火机×2、固体燃料×1 → OLI物流部钥匙×1 | Jaeger | 3 | 1 | — |
| RB-VO 符号钥匙×1、RB-PKPM 符号钥匙×1、RB-BK 符号钥匙×1 → 宿舍楼 314 房间符号钥匙×1 | Jaeger | 3 | 1 | — |
| Repellent杀虫剂×1 → 20/70 TSS 穿甲独头弹×25 | Jaeger | 3 | 3 | — |
| 罐装白盐×1 → 20/70 箭形弹×25 | Jaeger | 3 | 2 | — |
| 尼龙绳索×12、管道胶带×15、绝缘胶带×15、一包钉子×15 → SICC 收纳包×1 | Jaeger | 4 | 1 | — |
| 手枪收纳箱×5、野营燃料桶×5 → 武器箱×1 | Jaeger | 4 | 1 | — |
| 武器箱×2、弹药箱×4、金属零件×10 → THICC 武器箱×1 | Jaeger | 4 | 1 | — |
| 燃油添加剂×5 → Vortex Razor HD Gen.2 1-6x24 30 毫米步枪瞄准镜×1 | Jaeger | 4 | 1 | 5bc4856986f77454c317bea7 |
| 一罐酸黄瓜×2 → Leupold Mark 5HD 5-25x56mm 35毫米步枪瞄准镜 (FDE)×1 | Jaeger | 4 | 5 | — |
| LEDX皮肤透照仪×1 → Armasight Zeus-Pro 640 2-8x50 30Hz热成像瞄准镜×1 | Jaeger | 4 | 1 | 60e71e8ed54b755a3b53eb67 |
| 塔克肉干×1 → AI AXMC GTAC AR 规格手枪式握把转接器×1 | Jaeger | 4 | 5 | — |
| 手摇钻×1、SurvL幸存者打火机×1 → 雷明顿Model 700 7.62x51 狙击步枪 MRS×1 | Jaeger | 4 | 2 | 6a4532e48e82d8ffea0c3eae |
| 炖美味牛肉罐头（大）×3、炖牛肉罐头×3 → DVL-10 7.62x51 栓动式狙击步枪 直至黎明×1 | Jaeger | 4 | 1 | — |
| 银徽章×25 → 宿舍楼 314 房间符号钥匙×1 | Jaeger | 4 | 1 | 5eaaaa7c93afa0558f3b5a1c |
| 罐装六可乐×4、绒球毛线帽×8、带耳罩的苏联毛帽×4 → RB-PP钥匙×1 | Jaeger | 4 | 1 | — |
| Cyclon 蓄电池组×7、长平头螺丝刀×5 → RB-PSP2钥匙×1 | Jaeger | 4 | 1 | — |
| BD 狗牌 •\| 铁灰×1 → Gatorz Specter 军规防弹眼镜×1 | Peacekeeper | 1 | 2 | — |
| 损坏的硬盘×1 → Flyye MBSS 背包（通用迷彩）×1 | Peacekeeper | 1 | 3 | — |
| Ultralink 卫星通讯模块×1、IBX Gigachad 加密处理器×2 → SICC 收纳包×1 | Peacekeeper | 1 | 5 | 69e5583240c3e6c8ba0edbd5 |
| 气体分析仪×1 → MP9 9x19标准30发弹匣×1 | Peacekeeper | 1 | 3 | — |
| 应急用水×1 → Daniel Defence RIS II 9.5 英寸 AR-15 规格护木（狼棕色）×1 | Peacekeeper | 1 | 2 | — |
| 狗牌×5 → SIG MPX 9x19 冲锋枪 默认×1 | Peacekeeper | 1 | 1 | — |
| ER“支点”刺刀×1、A-2607 95Kh18 匕首×2 → HK MP5 9x19 冲锋枪（海军三发点射） 默认×1 | Peacekeeper | 1 | 2 | — |
| A-2607 大马士革钢匕首×4、ER“支点”刺刀×1 → HK UMP .45 ACP冲锋枪 默认×1 | Peacekeeper | 1 | 2 | — |
| BD 狗牌 •\| 铁灰×1 → Avon M53A1 防毒面具×1 | Peacekeeper | 2 | 2 | — |
| 地形调查地图×1、损坏的硬盘×1 → MSA Gallet TC 800 High Cut 作战头盔×1 | Peacekeeper | 2 | 3 | — |
| 损坏的硬盘×1、DVD光驱×1 → LBT-8005A Day Pack 背包（黑系复合迷彩）×1 | Peacekeeper | 2 | 4 | — |
| 6-STEN-140-M军用电池×1、Iridium军用热成像模块×3、Virtex可编程处理器×3、军用电缆×5 → Beta 安全箱×1 | Peacekeeper | 2 | 1 | — |
| USB适配器×2、施工用测量卷尺×1 → KAC QDC 5.56x45 AR-15 消焰器套件×1 | Peacekeeper | 2 | 1 | — |
| Dan Jackiel瓶装威士忌×1 → Sig BRAVO4 4X30瞄准镜×1 | Peacekeeper | 2 | 2 | — |
| Freeman撬棍×1 → ELCAN SpecterDR 1x/4x瞄准镜 FDE×1 | Peacekeeper | 2 | 2 | — |
| 燃油添加剂×1 → X Products HK MP5 9x19 X-5 50发弹鼓×1 | Peacekeeper | 2 | 3 | — |
| 碱性换热器表面洗涤剂×1 → Circle 10 5.56x45 SLR-106/AK 30发弹匣×1 | Peacekeeper | 2 | 10 | — |
| 武器零件×1、100毫升WD-40×1 → M14 UTG 4点锁定式高级基座×1 | Peacekeeper | 2 | 3 | — |
| 损坏的液晶显示屏×2 → 10.3 英寸 AR-15 枪管 5.56x45×1 | Peacekeeper | 2 | 1 | — |
| 波纹软管×1 → 11 英寸 HK416 枪管 5.56x45×1 | Peacekeeper | 2 | 3 | — |
| 5升丙烷罐×1 → HK 416 Midwest Industries 9 英寸 M-LOK 护木×1 | Peacekeeper | 2 | 3 | — |
| 电脑CPU×3、CPU风扇×1 → 柯尔特 M4A1 5.56x45 卡宾枪 Carbine×1 | Peacekeeper | 2 | 1 | — |
| 内存条×3 → DS Arms SA-58 7.62x51 突击步枪 AUT×1 | Peacekeeper | 2 | 1 | — |
| Cyclon 蓄电池组×1 → 奈特军械公司 (KAC) SR-25 7.62x51 精确射手步枪 默认×1 | Peacekeeper | 2 | 1 | — |
| USB适配器×4 → 春田 M1A 7.62x51 步枪 默认×1 | Peacekeeper | 2 | 1 | — |
| Pestily 瘟疫面具×1 → FN Five-seveN MK2 5.7x28 手枪 (FDE) 机械瞄具×1 | Peacekeeper | 2 | 3 | — |
| 军用电缆×2 → FN P90 5.7x28 冲锋枪 SBR×1 | Peacekeeper | 2 | 1 | — |
| Zibbo打火机×1 → M67手榴弹×1 | Peacekeeper | 2 | 8 | — |
| 串口硬盘×1、吗啡注射器×3、日记×1 → 疗养院东楼 306 房间钥匙×1 | Peacekeeper | 2 | 1 | — |
| BD 狗牌 •\| 铁灰×1 → Mystery Ranch NICE 框架式承重系统×1 | Peacekeeper | 3 | 1 | — |
| BD 狗牌 •\| 绿色×2 → Spiritus Systems LV-119 插板胸挂 (黑色军团 V2)×1 | Peacekeeper | 3 | 2 | — |
| 记者证（为NoiceGuy签发）×1 → Monoclete三级PE防弹插板×1 | Peacekeeper | 3 | 2 | — |
| 400毫升WD-40×1 → Ferfrans CRD 5.56x45 后座缓冲装置×1 | Peacekeeper | 3 | 3 | — |
| 100毫升WD-40×1 → Ferfrans CQB 5.56x45 AR-15 膛口制退器×1 | Peacekeeper | 3 | 3 | — |
| Humpback鲑鱼罐头×1、炼乳罐头×1、卫生纸×1 → KAC QDSS NT-4 5.56x45 消音器（黑色）×1 | Peacekeeper | 3 | 5 | — |
| 金公鸡塑像×1 → KAC PRS/QDC 7.62x51 消音器×1 | Peacekeeper | 3 | 2 | — |
| “凶狠跑刀崽”私酒×1 → L3Harris GPNVG-18 夜视仪×1 | Peacekeeper | 3 | 1 | — |
| BEAR 狗牌×9 → L3Harris GPNVG-18 夜视仪×1 | Peacekeeper | 3 | 1 | — |
| 情报文件夹×1、电子元件×3 → M203 40毫米下挂式榴弹发射器×1 | Peacekeeper | 3 | 5 | 63a9b229813bba58a50c9ee5 |
| 印制电路板×2、盖革-穆勒计数器×1 → SureFire MAG5-60 5.56x45 STANAG 60发弹匣×1 | Peacekeeper | 3 | 5 | — |
| BEAR 狗牌×8、USEC 狗牌×8 → 雷明顿 R11 RSASS 7.62x51 精确射手步枪 默认×1 | Peacekeeper | 3 | 3 | — |
| TerraGroup实验室访问钥匙卡×1 → HK G28 7.62x51 精确射手步枪 Patrol×1 | Peacekeeper | 3 | 1 | — |
| BakeEzy烹饪书×1 → IWI UZI PRO Pistol 9x19冲锋枪 EXPS3-0×1 | Peacekeeper | 3 | 1 | — |
| 日记×4、袖珍日记×4、地形调查地图×1 → 情报文件夹×1 | Peacekeeper | 3 | 1 | — |
| 军用COFDM无线信号发射器×1、军用转速计×1 → Ops-Core FAST MT 超级高切头盔（黑色） RAC×1 | Peacekeeper | 4 | 2 | — |
| 串口硬盘×3 → CQC 鱼鹰 MK4A 防弹胸挂（防护型，多地形迷彩） 默认×1 | Peacekeeper | 4 | 1 | — |
| BD 狗牌 •\| 绿色×1、BD 狗牌 •\| 红色×1 → First Spear Siege-R Optimized M.A.S.S. 插板胸挂 (黑色军团)×1 | Peacekeeper | 4 | 2 | — |
| BD 狗牌 •\| 绿色×2、BD 狗牌 •\| 红色×1 → Spiritus Systems LV-119 插板胸挂 (黑色军团 V1)×1 | Peacekeeper | 4 | 2 | — |
| TerraGroup "蓝色文件夹" 材料×20、加密磁带盒×30、加密U盘×30 → THICC 物品箱×1 | Peacekeeper | 4 | 1 | 60e71ce009d7c801eb0c0ec6 |
| 串口硬盘×1 → NFM THOR 一体式防弹护甲×1 | Peacekeeper | 4 | 1 | — |
| 串口硬盘×1 → SAPI III+ 级防弹插板×1 | Peacekeeper | 4 | 5 | 5c0bd01e86f7747cdd799e56 |
| 400毫升WD-40×3 → Steyr AUG Ase Utra S Series SL7i 5.56x45消音器×1 | Peacekeeper | 4 | 2 | — |
| 罐装塔可乐汽水×2、Dan Jackiel瓶装威士忌×1 → SilencerCo SAKER ASR 556 5.56x45 消音器×1 | Peacekeeper | 4 | 3 | — |
| 防毒面具滤罐×3 → Magpul PMAG D-60 5.56x45 STANAG 60发弹鼓×1 | Peacekeeper | 4 | 4 | 67a09724972c11a3f5077324 |
| Dan Jackiel瓶装威士忌×3、Tarkovskaya瓶装伏特加×1 → 柯尔特 M4A1 5.56x45 卡宾枪 SOPMOD II Flash×1 | Peacekeeper | 4 | 1 | — |
| VPX闪存模块×2、模拟温度计×1 → 沙漠科技 MDR 5.56x45 突击步枪 HHS-1褐×1 | Peacekeeper | 4 | 1 | — |
| 军用电路板×3、微控制器电路板×1 → FN SCAR-H 7.62x51突击步枪 FLIR RS-32×1 | Peacekeeper | 4 | 1 | — |
| LEDX皮肤透照仪×1 → 柯尔特 M4A1 5.56x45 卡宾枪 REAP-IR×1 | Peacekeeper | 4 | 1 | — |
| 损坏的GPhone X×1、充电宝×1 → HK MP7A2 4.6x30 冲锋枪 默认×1 | Peacekeeper | 4 | 2 | — |
| VPX闪存模块×1、军用电路板×5、军用电缆×5 → FN GL-40 Mk.2 榴弹发射器 默认×1 | Peacekeeper | 4 | 1 | 5a27bc8586f7741b543d8ea4 |
| Ortodontox牙膏×2 → 牙膏×1 | Therapist | 1 | 2 | — |
| Crickent打火机×3、经典火柴×2 → 一堆药×1 | Therapist | 1 | 4 | — |
| 阿波罗-联盟香烟×5 → OLOLO瓶装复合维生素×1 | Therapist | 1 | 5 | 5a68661a86f774500f48afb0 |
| 过氧化氢溶液（双氧水）×1 → 0.6升瓶装水×1 | Therapist | 1 | 2 | — |
| 经典火柴×1、Crickent打火机×1 → 太平洋秋刀鱼罐头×1 | Therapist | 1 | 5 | — |
| 一次性注射器×1 → Iskra（“火花”）单兵口粮×1 | Therapist | 1 | 2 | — |
| Vita果汁×1 → 士腻架能量棒×1 | Therapist | 1 | 5 | — |
| 牙膏×2 → Humpback鲑鱼罐头×1 | Therapist | 1 | 3 | — |
| 一堆药×1 → 军用饼干×1 | Therapist | 1 | 5 | — |
| 经典火柴×1 → 安乃近止痛药×1 | Therapist | 1 | 10 | — |
| Humpback鲑鱼罐头×1 → 车载急救包×1 | Therapist | 1 | 5 | — |
| 应急用水×1 → Salewa急救包×1 | Therapist | 1 | 2 | — |
| Paid杀蟑剂×1 → Salewa急救包×1 | Therapist | 1 | 1 | — |
| 一包螺钉×2 → CALOK-B止血剂×1 | Therapist | 1 | 2 | — |
| Gingy钥匙串×2、WZ钱包×2、6-STEN-140-M军用电池×1 → 钥匙箱×1 | Therapist | 1 | 1 | — |
| Nooby Shield 碘化钾片×3 → 钱箱×1 | Therapist | 1 | 5 | 69ce213a298a6529b30d7134 |
| T形插座×7、绝缘胶带×3 → 宿舍303房间钥匙×1 | Therapist | 1 | 1 | — |
| 一堆药×7 → 宿舍206房间钥匙×1 | Therapist | 1 | 1 | — |
| 罐装冰绿茶×1、军用饼干×2、罐装蔬菜泥×1 → 海关 Tarcone 主管办公室钥匙×1 | Therapist | 1 | 1 | — |
| Vita果汁×1、一盒牛奶×1、卫生纸×1 → 简易工棚钥匙×1 | Therapist | 1 | 1 | — |
| Arseniy包装荞麦×4 → 野营燃料桶×1 | Therapist | 2 | 1 | — |
| 五号电池×2 → 一次性注射器×1 | Therapist | 2 | 3 | — |
| 罐装冰绿茶×4 → 42 Signature Blend英式茶×1 | Therapist | 2 | 2 | — |
| Emelya黑麦面包块×1 → 黑麦面包块×1 | Therapist | 2 | 3 | — |
| 气体分析仪×1、紫外线灯泡×1 → Grizzly急救包×1 | Therapist | 2 | 2 | 5a68665c86f774255929b4c7 |
| 碳酸氢钠×1 → IFAK单兵急救包×1 | Therapist | 2 | 3 | — |
| 一次性注射器×2 → 铝固定夹板×1 | Therapist | 2 | 1 | — |
| Repellent杀虫剂×2 → CMS手术包×1 | Therapist | 2 | 1 | — |
| 苹果汁×3 → Propital×1 | Therapist | 2 | 2 | 5a68663e86f774501078f78a |
| 经典火柴×1、紫外线灯泡×1 → 肾上腺素注射器×1 | Therapist | 2 | 1 | — |
| 过氧化氢溶液（双氧水）×10、盐水溶液×10、一包氯石灰×10 → 钥匙收纳器×1 | Therapist | 2 | 1 | — |
| 一堆药×6、一次性注射器×4 → 手枪收纳箱×1 | Therapist | 2 | 1 | — |
| 猫雕像×1、青铜狮雕×1、马雕像×4 → 文件包×1 | Therapist | 2 | 1 | — |
| 狗牌×40 → 幸运Scav垃圾箱×1 | Therapist | 2 | 1 | — |
| 医用输血工具×10、一次性注射器×10、医疗工具×10 → 医疗物品箱×1 | Therapist | 2 | 1 | — |
| 金项链×4 → 海关 Tarcone 主管办公室钥匙×1 | Therapist | 2 | 1 | — |
| 一包氯石灰×8 → 疗养院西楼 221 房间钥匙×1 | Therapist | 2 | 1 | — |
| Arseniy包装荞麦×1、Aquapeps饮用水净化片×1、一堆药×5 → 宿舍220房间钥匙×1 | Therapist | 2 | 1 | — |
| 过氧化氢溶液（双氧水）×1、盐水溶液×1、一堆药×1 → 宿舍114房间钥匙×1 | Therapist | 2 | 1 | — |
| 狗牌×20、USEC 狗牌×140 → LEDX皮肤透照仪×1 | Therapist | 3 | 1 | — |
| 一次性注射器×1、过氧化氢溶液（双氧水）×1、碳酸氢钠×1 → SJ1 TGLabs战斗兴奋剂注射器×1 | Therapist | 3 | 1 | — |
| Schaman洗发水×4 → eTG-change再生兴奋剂注射器×1 | Therapist | 3 | 1 | — |
| OLOLO瓶装复合维生素×2 → Propital×1 | Therapist | 3 | 3 | — |
| 力百汀抗生素药片×2、凡士林×2、Surv12野战手术包×2、一堆药×3 → LBT-2670 小型野战医物包×1 | Therapist | 3 | 1 | — |
| 检眼镜×10、一堆药×25 → 物品箱×1 | Therapist | 3 | 1 | — |
| 狗牌×80 → 物品箱×1 | Therapist | 3 | 1 | — |
| 木头钟×4、古董茶壶×2、古董花瓶×1 → 海报筒×1 | Therapist | 3 | 1 | — |
| 纯净水×2、滤水器×2 → RB-AK钥匙×1 | Therapist | 3 | 1 | — |
| 一次性注射器×5、Iskra（“火花”）单兵口粮×3、检眼镜×1 → EMERCOM医疗区钥匙×1 | Therapist | 3 | 1 | — |
| 一堆药×4 → 疗养院西楼 112 办公室钥匙×1 | Therapist | 3 | 1 | — |
| 一堆药×6 → 一包糖×1 | Therapist | 4 | 2 | — |
| 医用输血工具×1、一堆药×3 → 布洛芬止痛药×1 | Therapist | 4 | 1 | — |
| 盐水溶液×2、过氧化氢溶液（双氧水）×1 → SJ6 TGLabs战斗兴奋剂注射器×1 | Therapist | 4 | 1 | — |
| 金项链×5、劳力土潜水金腕表×2、黄金骷髅指环×2 → 钱箱×1 | Therapist | 4 | 1 | — |
| 便携式除颤器×15、LEDX皮肤透照仪×15、布洛芬止痛药×15、牙膏×15 → THICC 物品箱×1 | Therapist | 4 | 1 | — |
| “凶狠跑刀崽”私酒×50、Tarkovskaya瓶装伏特加×50、Dan Jackiel瓶装威士忌×30 → THICC 物品箱×1 | Therapist | 4 | 1 | — |
| 医疗工具×20、一次性注射器×15、过氧化氢溶液（双氧水）×12 → 注射器收纳盒×1 | Therapist | 4 | 1 | — |
| 炼乳罐头×60、盒装燕麦片×10 → Kiba 商店外门钥匙×1 | Therapist | 4 | 1 | — |
| 马雕像×1、绒球毛线帽×1 → BNTI Zhuk（甲虫）防弹衣（战地记者版） 默认×1 | Skier | 1 | 1 | — |
| 项链×1 → ProMag SKS-A5 7.62x39 20发SKS弹匣×1 | Skier | 1 | 3 | — |
| 咸狗牛肉肠×1 → VPO-136 野猪-KM 7.62x39 卡宾枪 默认×1 | Skier | 1 | 3 | — |
| T形插座×1 → 12/70 RIP弹药包（5发装）×1 | Skier | 2 | 12 | 669fa39c64ea11e84c0642a6 |
| BEAR 狗牌×4 → AK-74 TGP-A 5.45x39消音器×1 | Skier | 2 | 4 | — |
| 罐装 Majaica 咖啡豆×1 → Rotor 43 9x19 消音器×1 | Skier | 2 | 2 | — |
| 木头钟×1 → Hexagon 12K声音抑制器×1 | Skier | 2 | 2 | — |
| 《天雷地火》录像带×1 → SureFire SOCOM556-MINI MONSTER 5.56x45 消音器×1 | Skier | 2 | 1 | — |
| 无彩香烟×1 → KRISS G30 MagEx Glock .45 ACP 30 发弹匣×1 | Skier | 2 | 6 | — |
| 马雕像×1 → KRISS Vector Mk.5 模块化导轨×1 | Skier | 2 | 3 | — |
| DVD光驱×1 → KRISS Vector 非可折叠枪托转接器×1 | Skier | 2 | 3 | — |
| 项链×1 → 6 英寸 KRISS Vector 枪管 9x19×1 | Skier | 2 | 3 | — |
| 气体分析仪×2 → Kel-Tec RFB 7.62x51 默认×1 | Skier | 2 | 1 | — |
| 盒装燕麦片×3、罐装六可乐×2 → Kel-Tec RFB 7.62x51 HHS-1×1 | Skier | 2 | 3 | — |
| 损坏的GPhone X×1、金色1GPhone×1、损坏的GPhone×2 → SIG MCX .300 Blackout 突击步枪 默认×1 | Skier | 2 | 2 | — |
| T形插座×1 → 12/70 RIP×5 | Skier | 2 | 12 | 669fa39c64ea11e84c0642a6 |
| 瓶装 Pevko 淡啤酒×6 → 金属燃料桶×1 | Skier | 3 | 1 | — |
| 无彩香烟×2 → Kiba Arms Titan防弹插板×1 | Skier | 3 | 2 | 5c0bc91486f7746ab41857a2 |
| NIXXOR镜头×1、五号电池×1 → VPO-101 Rotor 43 7.62x51 消音器×1 | Skier | 3 | 2 | — |
| 乌鸦雕像×1 → SIG Sauer SRD762-QD 7.62x51 消音器×1 | Skier | 3 | 1 | — |
| BOSS鸭舌帽×1、皮帽×2 → Thunder Beast Ultra 5声音抑制器 ×1 | Skier | 3 | 1 | — |
| USEC 狗牌×5 → Hensoldt FF 4-16x56 34 毫米步枪瞄准镜×1 | Skier | 3 | 1 | — |
| 马雕像×5、古董茶壶×1 → DVL-10 7.62x51 栓动式狙击步枪 Urbana×1 | Skier | 3 | 1 | — |
| 金项链×3 → .338 Lapua Magnum FMJ弹药包（20发装）×1 | Skier | 4 | 1 | — |
| “凶狠跑刀崽”私酒×10、Tarkovskaya瓶装伏特加×10、士腻架能量棒×5 → 武器箱×1 | Skier | 4 | 1 | — |
| 金公鸡塑像×1 → Cult Locust防弹插板×1 | Skier | 4 | 1 | 5c0d4c12d09282029f539173 |
| 损坏的GPhone X×2 → EOTech Vudu 1-6x24 30 毫米 步枪瞄准镜×1 | Skier | 4 | 1 | — |
| 木头钟×5、劳力土潜水金腕表×6、黄金骷髅指环×6 → Trijicon REAP-IR热成像步枪瞄准镜×1 | Skier | 4 | 1 | — |
| UHF RFID固定式读取器 ×1 → L3Harris GPNVG-18 夜视仪×1 | Skier | 4 | 2 | — |
| 地形调查地图×5、武器零件×3、军用电路板×1 → MPS Auto Assault-12 Gen 2 12铅径自动霰弹枪 默认×1 | Skier | 4 | 2 | 671a59e43d73dac1360765cc |
| Inseq燃气管扳手×1、Axel鹦鹉雕像×1、Dundukk运动太阳镜×10 → Saiga-12K 12 铅径自动霰弹枪 COMP M4×1 | Skier | 4 | 2 | 5c0bbaa886f7746941031d82 |
| 金属零件×4、罐装铝热剂×2 → DVL-10 7.62x51 栓动式狙击步枪 Saboteur×1 | Skier | 4 | 1 | — |
| 军用COFDM无线信号发射器×2、军事基地检查站钥匙×1、Prokill带链奖章×2、相控阵单元×2 → Miller Bros. Blades M-2 战术剑 ×1 | Skier | 4 | 1 | 5c0bdb5286f774166e38eed4 |
| 金项链×3 → .338 Lapua Magnum FMJ×20 | Skier | 4 | 1 | — |

## 2. 制作（crafts）

### 2.1 统计

- 总数：**214** 条（live tarkov.dev 快照）
- 按工作站分组（降序）：

| 工作站 | station id | 配方数 |
|---|---|---|
| 工作台 | `5d484fda654e7600681d9315` | 94 |
| 卫生间 | `5d484fba654e7600691aadf7` | 41 |
| 医疗站 | `5d484fcd654e7668ec2ec322` | 28 |
| 情报中心 | `5d484fdf654e7600691aadf8` | 26 |
| 营养部 | `5d484fd1654e76006732bf2e` | 19 |
| 集水器 | `5d484fc8654e760065037abf` | 4 |
| 酿酒处 | `5d494a3f5b56502f18c98a0e` | 1 |
| 比特币矿场 | `5d494a445b56502f18c98a10` | 1 |

> 9 条配方带 `requiredQuestItems`（需先完成任务/持有任务物品）。

### 2.2 全量制作表（按工作站分组，组内按 level 升序）

> 列含义：产物=productItem；工作站=station；等级=藏身处该站所需等级；时长=duration；原料摘要=requiredItems 前 3 项（超出标注总数）；任务解锁=requiredQuestItems（— 表示无）。

| 产物 | 工作站 | 等级 | 时长 | 原料摘要（前 3 项） | 任务解锁 |
|---|---|---|---|---|---|
| Gamma 安全箱×1 | 工作台 | 1 | 30分 | Gamma 安全箱×1、Poxeram冷焊膏×1 | — |
| 电容×6 | 工作台 | 1 | 2时4分 | 供电单元×1、螺丝刀×1 | — |
| 武器零件×5 | 工作台 | 1 | 1时18分 | AKM 7.62x39 突击步枪×1、Leatherman多功能工具钳×1 | — |
| 印制电路板×2 | 工作台 | 1 | 33分 | DVD光驱×1、平头螺丝刀×1 | — |
| 破冰船模型套件×1 | 工作台 | 1 | 1天 | 破冰船模型套件×1 | — |
| Gamma 安全箱×1 | 工作台 | 1 | 30分 | Gamma 安全箱×1、平头螺丝刀×1、绝缘胶带×1 | — |
| T形插座×3 | 工作台 | 1 | 57分 | 一包钉子×1、电源线×1 | — |
| 手摇钻×1 | 工作台 | 1 | 2时30分 | 螺母×1、螺栓×1、金属零件×2 | — |
| RGD-5手榴弹×6 | 工作台 | 1 | 5时5分 | TP-200 砖型TNT×2、UZRGM手榴弹引信×3 | — |
| 已解锁的贵重物品箱（稀有）×1 | 工作台 | 1 | 1分 | 手摇钻×1、上锁的贵重物品箱（稀有）×1 | — |
| 螺母罐×1 | 工作台 | 1 | 1时 | 罐装 Majaica 咖啡豆×1、平头螺丝刀×1 | — |
| “Kite”火药×2 | 工作台 | 1 | 15分 | 5.45x39毫米 HP×120、钳子×1 | — |
| 已解锁的装备箱（稀有）×1 | 工作台 | 1 | 1分 | 手摇钻×1、上锁的装备箱（稀有）×1 | — |
| 已解锁的武器箱（稀有）×1 | 工作台 | 1 | 1分 | 手摇钻×1、上锁的武器箱（稀有）×1 | — |
| 9x18毫米 PM SP8×150 | 工作台 | 1 | 55分 | 9x18毫米 PM RG028×150、电线×1 | — |
| 9x18毫米 PMM PstM×140 | 工作台 | 1 | 1时43分 | “Kite”火药×1 | — |
| 有机玻璃片×4 | 工作台 | 1 | 25分 | 损坏的液晶显示屏×2、Poxeram冷焊膏×1 | — |
| 汽车蓄电池×5 | 工作台 | 1 | 2时47分 | 6-STEN-140-M军用电池×1、管道扳手×1 | — |
| 印制电路板×2 | 工作台 | 1 | 48分 | 气体分析仪×1、螺丝刀×1 | — |
| 已解锁的物资箱（稀有）×1 | 工作台 | 1 | 5分 | 手摇钻×1、长平头螺丝刀×1、Elite钳子×1 等4项 | — |
| “Hawk”火药×3 | 工作台 | 1 | 36分 | 经典火柴×1、“Eagle”火药×1 | — |
| .366 AP×60 | 工作台 | 1 | 2时21分 | “Hawk”火药×2、7.62x39毫米 PS×100、钳子×1 | — |
| 电线×8 | 工作台 | 1 | 1时58分 | 电源线×2 | — |
| 一套工具×1 | 工作台 | 1 | 8分 | 钳子×1、螺丝刀×1、扳手×1 等6项 | — |
| 9x21毫米 SP11×150 | 工作台 | 1 | 1时30分 | “Kite”火药×1 | — |
| 已解锁的武器箱（稀有）×1 | 工作台 | 1 | 5分 | 手摇钻×1、长平头螺丝刀×1、Elite钳子×1 等4项 | — |
| PP-9 楔子 9x18PMM 冲锋枪 默认×1 | 工作台 | 1 | 30分 | PP-91 雪松 9x18PM冲锋枪×1、"Master"锉刀套装×1 | — |
| 7.62x39毫米 FMJ×200 | 工作台 | 1 | 1时13分 | “Eagle”火药×1、缝纫锥×1 | — |
| 已解锁的装备箱（稀有）×1 | 工作台 | 1 | 5分 | 手摇钻×1、长平头螺丝刀×1、Elite钳子×1 等4项 | — |
| 损坏的GPhone×1 | 工作台 | 1 | 25分 | 电脑CPU×1、印制电路板×1、电子元件×1 | — |
| 加固手提箱×1 | 工作台 | 1 | 1天1时 | 一套工具×1、加固手提箱×1 | 67bdea3ecf0cc29475092196 |
| 损坏的液晶显示屏×4 | 工作台 | 1 | 22分 | 完好的液晶显示屏×4、螺丝刀×1 | — |
| 电磁铁×1 | 工作台 | 1 | 43分 | 损坏的硬盘×1、螺丝刀×1 | — |
| 电源线×2 | 工作台 | 1 | 35分 | T形插座×2、电线×2、绝缘胶带×1 | — |
| 6-STEN-140-M军用电池×1 | 工作台 | 1 | 22时 | 汽车蓄电池×2、纯净水×1、Clin 玻璃清洁剂×2 等4项 | — |
| 已解锁的物资箱（稀有）×1 | 工作台 | 1 | 1分 | 手摇钻×1、上锁的物资箱（稀有）×1 | — |
| 实体比特币的左半边×1 | 工作台 | 1 | 10分 | 实体比特币×1、NKVD 芬卡工具刀×1 | — |
| 已解锁的装备箱（通行证 0 赛季）×1 | 工作台 | 1 | 1分 | 手摇钻×1、上锁的装备箱（通行证 0 赛季）×1 | — |
| 圆嘴钳×1 | 工作台 | 1 | 15分 | "Master"锉刀套装×1、钳子×1 | — |
| 气体分析仪×1 | 工作台 | 1 | 1时50分 | 五号电池×2、盖革-穆勒计数器×1、电磁铁×1 等5项 | — |
| 武器零件×1 | 工作台 | 1 | 52分 | 西蒙诺夫 SKS 7.62x39 卡宾枪×1、平头螺丝刀×1 | — |
| 12/70 “食人鱼”×120 | 工作台 | 1 | 1时28分 | 一包钉子×1、“Hawk”火药×1、Leatherman多功能工具钳×1 | — |
| 12/70 箭形弹×40 | 工作台 | 1 | 50分 | 一包钉子×3、“Eagle”火药×2、Fierce Blow重击锤×1 | — |
| 无线电中继器×1 | 工作台 | 1 | 1时30分 | 电子元件×1、军用COFDM无线信号发射器×1、气体分析仪×1 等6项 | — |
| PM 9x18PM 84发简易弹鼓×1 | 工作台 | 1 | 20分 | "Master"锉刀套装×1、钳子×1、PPSh-41 7.62x25 71发弹鼓×1 等5项 | — |
| AKM 7.62x39 突击步枪 默认×1 | 工作台 | 1 | 6分 | VPO-136 野猪-KM 7.62x39 卡宾枪×1、Leatherman多功能工具钳×1 | — |
| Zarya震撼手榴弹×5 | 工作台 | 1 | 1时18分 | UZRGM手榴弹引信×5、“Kite”火药×1 | — |
| 电灯泡×4 | 工作台 | 1 | 1时 | 电线×2、一包钉子×1、金属零件×1 | — |
| AK-74N 5.45x39 突击步枪 默认×1 | 工作台 | 1 | 1时22分 | 武器零件×1、AK-74 木制护木×1、AK-74 木制枪托×1 等5项 | — |
| 已解锁的贵重物品箱（稀有）×1 | 工作台 | 1 | 5分 | 手摇钻×1、长平头螺丝刀×1、Elite钳子×1 等4项 | — |
| 军用电路板×1 | 工作台 | 2 | 2时17分 | 长平头螺丝刀×1、螺丝刀×1、Elite钳子×1 等4项 | — |
| AK-74M 5.45x39 突击步枪 默认×1 | 工作台 | 2 | 1时23分 | 武器零件×1、AK-74 聚合物护木×1、AK-74M 聚合物枪托×1 等4项 | — |
| 9x39毫米 PAB-9×100 | 工作台 | 2 | 2时3分 | “Eagle”火药×3、SurvL幸存者打火机×1 | — |
| .300 Blackout M62 曳光弹×100 | 工作台 | 2 | 1时53分 | 5.56x45毫米 M856×120、固体燃料×1、SurvL幸存者打火机×1 | — |
| 可充电电池×2 | 工作台 | 2 | 1时13分 | 充电宝×1、平头螺丝刀×1 | — |
| 5.56x45毫米 Mk 318 Mod 0 (SOST)×90 | 工作台 | 2 | 1时44分 | “Kite”火药×2、“Eagle”火药×1、Elite钳子×1 | — |
| “Eagle”火药×2 | 工作台 | 2 | 1时38分 | M67手榴弹×2、M18烟雾弹 （绿色）×1、螺丝刀×1 | — |
| GreenBat锂电池×1 | 工作台 | 2 | 2时24分 | 可充电电池×1、电子元件×2、充电宝×2 | — |
| 7.62x54R LPS×50 | 工作台 | 2 | 4时 | 7.62x54R FMJ×70、“Hawk”火药×1、Leatherman多功能工具钳×1 | — |
| 12/70 .50 BMG 简易独头弹×60 | 工作台 | 2 | 1时10分 | 12/70 7毫米鹿弹×60、“Hawk”火药×1、100毫升WD-40×1 等4项 | — |
| 5.7x28毫米 L191×150 | 工作台 | 2 | 57分 | “Kite”火药×1、“Hawk”火药×1、Leatherman多功能工具钳×1 | — |
| 电动马达×1 | 工作台 | 2 | 1时20分 | 电线×2、电磁铁×1、电子元件×1 等4项 | — |
| Bulbex剪线器×1 | 工作台 | 2 | 6时57分 | 一套工具×1、100毫升WD-40×2、剪线钳×1 等6项 | — |
| 7.62x51毫米 BPZ FMJ×60 | 工作台 | 2 | 1时38分 | “Kite”火药×2 | — |
| 完好的液晶显示屏×1 | 工作台 | 2 | 31分 | 损坏的液晶显示屏×1、电容×1、电子元件×1 等4项 | — |
| NIXXOR镜头×2 | 工作台 | 2 | 2时32分 | WIFI摄像头×1、有机玻璃片×1、一套工具×1 等4项 | — |
| VOG-17 Khattabka 简易手榴弹×8 | 工作台 | 2 | 1时20分 | 缝纫锥×1、UZRGM手榴弹引信×5、TP-200 砖型TNT×1 | — |
| 5.45x39毫米 PP×120 | 工作台 | 2 | 2时22分 | 5.45x39毫米 PS×180、“Kite”火药×1、剪线钳×1 | — |
| “Hawk”火药×3 | 工作台 | 2 | 1时53分 | OFZ 30x165毫米炮弹×1、“Kite”火药×2、棘轮扳手×1 | — |
| 盖革-穆勒计数器×1 | 工作台 | 2 | 1时32分 | 气体分析仪×1、电线×1、电容×1 等5项 | — |
| VOG-25 Khattabka 简易手榴弹×5 | 工作台 | 2 | 1时40分 | UZRGM手榴弹引信×5、40毫米VOG-25榴弹×5、Leatherman多功能工具钳×1 | — |
| 9x19毫米 Luger CCI×180 | 工作台 | 2 | 1时33分 | 9x19毫米 FMJ M882×180、螺旋散热器×1、电线×1 | — |
| 罐装铝热剂×1 | 工作台 | 2 | 2时47分 | 宿舍308房间钥匙×1、“Kite”火药×1、金属零件×1 等5项 | — |
| 火花塞×1 | 工作台 | 2 | 1时57分 | 螺母×1、螺栓×2、SurvL幸存者打火机×1 等4项 | — |
| 4.6x30毫米 AP SX×60 | 工作台 | 3 | 3时27分 | 4.6x30毫米 FMJ SX×120、Elite钳子×1、紫外线灯泡×1 等5项 | — |
| 12/70 AP-20 穿甲独头弹×50 | 工作台 | 3 | 2时20分 | “Eagle”火药×3、12/70 7毫米鹿弹×50、400毫升WD-40×1 等4项 | — |
| 5.56x45毫米 M855A1×90 | 工作台 | 3 | 2时20分 | 5.56x45毫米 M855×180、“Kite”火药×1、“Eagle”火药×1 等4项 | — |
| 7.62x51毫米 M80A1×40 | 工作台 | 3 | 2时18分 | “Hawk”火药×2、螺旋散热器×1、Leatherman多功能工具钳×1 等4项 | — |
| 7.62x39毫米 BP×60 | 工作台 | 3 | 2时37分 | 管道扳手×1、电钻×1、7.62x39毫米 PP×90 等5项 | — |
| .45 ACP AP×100 | 工作台 | 3 | 3时27分 | .45 ACP Lasermatch FMJ×150、“Hawk”火药×1、Leatherman多功能工具钳×1 等6项 | — |
| .338 Lapua Magnum FMJ×30 | 工作台 | 3 | 4时10分 | .338 Lapua Magnum UCW×50、“Hawk”火药×2、Leatherman多功能工具钳×1 | — |
| 防弹衣维修套件×1 | 工作台 | 3 | 25分 | 防弹衣维修套件×1、Cordura聚酰胺面料×1、防撕布料×1 等5项 | — |
| 微控制器电路板×4 | 工作台 | 3 | 14时 | GPS信号放大单元×1、Elite钳子×1、长平头螺丝刀×1 等4项 | — |
| 23x75毫米“红星”闪光弹×7 | 工作台 | 3 | 4时35分 | “Eagle”火药×1、SurvL幸存者打火机×5、“Kite”火药×1 等6项 | — |
| TheAKGuy AK-50 .50 BMG 反器材步枪×1 | 工作台 | 3 | 1天4时 | "Master"锉刀套装×1、一套工具×1、RPK-16 5.45x39 轻机枪×1 等7项 | — |
| Trijicon REAP-IR热成像步枪瞄准镜×1 | 工作台 | 3 | 4时50分 | Iridium军用热成像模块×4、NIXXOR镜头×2、微控制器电路板×1 等7项 | — |
| 5.7x28毫米 SS190×60 | 工作台 | 3 | 2时25分 | 5.7x28毫米 SS198LF×100、“Eagle”火药×1、电容×4 等4项 | — |
| OFZ 30x165毫米炮弹×2 | 工作台 | 3 | 10时50分 | “Hawk”火药×2、“Eagle”火药×1、TP-200 砖型TNT×2 等5项 | — |
| 9x19毫米 AP 6.3×150 | 工作台 | 3 | 2时1分 | 9x19毫米 FMJ M882×150、“Kite”火药×2、一套工具×1 | — |
| 5.45x39毫米 BP×60 | 工作台 | 3 | 3时37分 | 5.45x39毫米 PS×180、“Eagle”火药×1、一套工具×1 | — |
| FLIR RS-32 2.25-9x 35毫米 60Hz热成像步枪瞄准镜×1 | 工作台 | 3 | 5时50分 | NIXXOR镜头×2、Iridium军用热成像模块×2、Virtex可编程处理器×1 等7项 | — |
| .300 Blackout AP×50 | 工作台 | 3 | 3时37分 | .300 Blackout CBJ×90、“Eagle”火药×1、一套工具×1 | — |
| 7.62x54R 7BT1×30 | 工作台 | 3 | 2时38分 | 7.62x54R 7N1狙击弹×50、罐装铝热剂×1、"Master"锉刀套装×1 等4项 | — |
| 武器维修套件×1 | 工作台 | 3 | 25分 | 武器维修套件×1、一套工具×1、#FireKlean牌枪润滑油×1 等5项 | — |
| 芳纶纤维布料×2 | 卫生间 | 1 | 33分 | PACA 软质防弹背心×1、Leatherman多功能工具钳×1 | — |
| 6L31 5.45x39 AK-74 60发弹匣×1 | 卫生间 | 1 | 1时23分 | 6L23 5.45x39 AK-74 30发弹匣×4、KEKTAPE管道胶带×1、长平头螺丝刀×1 | — |
| 波纹软管×2 | 卫生间 | 1 | 3时22分 | 硅胶管×1、电线×3、绝缘胶带×3 | — |
| 肥皂×6 | 卫生间 | 1 | 50分 | 碱性换热器表面洗涤剂×1、罐装白盐×1、咸狗牛肉肠×1 | — |
| 6B47 Ratnik-BSh 头盔（数码迷彩盔罩）×1 | 卫生间 | 1 | 20分 | 芳纶纤维布料×1、管道胶带×1、针线盒×1 | — |
| 防毒面具滤罐×1 | 卫生间 | 1 | 2分 | GP-7防毒面具×1、螺丝刀×1 | — |
| 无菌绷带×6 | 卫生间 | 1 | 32分 | 绒布布料×1 | — |
| 军用绷带×10 | 卫生间 | 1 | 17分 | 无菌绷带×5、Tarkovskaya瓶装伏特加×1 | — |
| Cordura聚酰胺面料×2 | 卫生间 | 1 | 48分 | 战术挎包×2、针线盒×1 | — |
| RDG-2B烟雾弹×6 | 卫生间 | 1 | 1时 | 一包氯石灰×1、Hunter火柴×1、管道胶带×1 等4项 | — |
| 卫生纸×2 | 卫生间 | 1 | 26分 | 打印纸×1 | — |
| Schaman洗发水×2 | 卫生间 | 1 | 35分 | 肥皂×1、瓶装 Pevko 淡啤酒×1 | — |
| BNTI Zhuk（甲虫）防弹衣（战地记者版）×1 | 卫生间 | 1 | 1时7分 | 芳纶纤维布料×1、防撕布料×1、Zhuk-3防弹插板（前部）×1 | — |
| 6B23-2 防弹衣（山地丛林迷彩）×1 | 卫生间 | 1 | 1时57分 | 6B33防弹插板（前部）×1、Cordura聚酰胺面料×1、Ox漂白剂×1 等4项 | — |
| Scav背包×1 | 卫生间 | 1 | 20分 | Cordura聚酰胺面料×1、绝缘胶带×1 | — |
| 防撕布料×2 | 卫生间 | 1 | 37分 | Scav背心×1、针线盒×1 | — |
| BNTI Module-3M 防弹背心×1 | 卫生间 | 1 | 54分 | 芳纶纤维布料×1、防撕布料×1 | — |
| 野营燃料桶×1 | 卫生间 | 2 | 1时2分 | Zibbo打火机×2、Crickent打火机×2、硅胶管×1 等4项 | — |
| KEKTAPE管道胶带×1 | 卫生间 | 2 | 1时20分 | 管道胶带×2、绝缘胶带×2、Clin 玻璃清洁剂×1 等4项 | — |
| 尼龙绳索×1 | 卫生间 | 2 | 4时18分 | 旅行包×2、电线×3、SurvL幸存者打火机×1 | — |
| 绒布布料×1 | 卫生间 | 2 | 43分 | UX PRO无檐小便帽×2、针线盒×1 | — |
| Eberlestock F5 弹簧刀 背包（干土色）×1 | 卫生间 | 2 | 42分 | 防撕布料×2、Cordura聚酰胺面料×2、针线盒×1 等4项 | — |
| 6B13定制防弹插板（黑色）×1 | 卫生间 | 2 | 27分 | 6B23-2防弹插板（黑色）×1、Poxeram冷焊膏×1、管道胶带×1 等5项 | — |
| BlackRock胸挂×1 | 卫生间 | 2 | 1时 | Splav Tarzan M22 胸挂×1、防撕布料×1、Cordura聚酰胺面料×1 等4项 | — |
| 手榴弹箱×1 | 卫生间 | 2 | 8时20分 | 金属燃料桶×2、螺母×5、螺栓×5 等5项 | — |
| FirstSpear Strandhogg 插板胸挂（丛林绿）×1 | 卫生间 | 2 | 33分 | 防撕布料×1、芳纶纤维布料×1、Cordura聚酰胺面料×2 等5项 | — |
| 滤水器×4 | 卫生间 | 2 | 2时13分 | 防毒面具滤罐×10、打印纸×5、金属切割剪刀×1 等4项 | — |
| 弹匣箱×1 | 卫生间 | 2 | 3时20分 | 野营燃料桶×2、螺栓×4、螺母×4 等4项 | — |
| Ox漂白剂×3 | 卫生间 | 2 | 39分 | 肥皂×1、碱性换热器表面洗涤剂×1、碳酸氢钠×1 | — |
| MSA ACH TC-2002 MICH系列头盔×1 | 卫生间 | 2 | 1时12分 | 管道胶带×2、芳纶纤维布料×1、针线盒×1 等5项 | — |
| 100毫升WD-40×2 | 卫生间 | 2 | 2时30分 | 400毫升WD-40×1、金属切割剪刀×1 | — |
| Ars Arma A18 Skanda 插板胸挂（复合迷彩）×1 | 卫生间 | 2 | 2时30分 | Cordura聚酰胺面料×2、芳纶纤维布料×3、防撕布料×1 等6项 | — |
| Clin 玻璃清洁剂×2 | 卫生间 | 2 | 41分 | Tarkovskaya瓶装伏特加×1、Schaman洗发水×1、硅胶管×1 | — |
| 幸运Scav垃圾箱×1 | 卫生间 | 2 | 5时 | 弹匣箱×3、螺栓×6、KEKTAPE管道胶带×3 等5项 | — |
| Blackhawk! Commando胸挂（黑）×1 | 卫生间 | 2 | 47分 | Cordura聚酰胺面料×1、防撕布料×1、缝纫锥×1 等4项 | — |
| FP-100过滤吸收器×1 | 卫生间 | 3 | 2时37分 | 防毒面具滤罐×3、滤水器×3、金属零件×3 等5项 | — |
| ANA Tactical M2 插板胸挂（橄榄绿）×1 | 卫生间 | 3 | 1时57分 | 芳纶纤维布料×1、防撕布料×2、Ox漂白剂×1 等5项 | — |
| Pilgrim旅行包×1 | 卫生间 | 3 | 42分 | 施工用测量卷尺×1、LolKek 3F Transfer 旅行背包×2、绝缘胶带×2 等4项 | — |
| Rys-T 防弹头盔（黑色）×1 | 卫生间 | 3 | 50分 | Altyn 防弹头盔（橄榄绿）×1、防撕布料×2、芳纶纤维布料×2 等6项 | — |
| 5.11 Tactical TacTec 插板胸挂（丛林绿）×1 | 卫生间 | 3 | 3时53分 | GAC 3s15m防弹插板×1、防撕布料×2、金属切割剪刀×1 等5项 | — |
| NPP KlASS Bagariy 防弹胸挂（数码丛林迷彩）×1 | 卫生间 | 3 | 2时13分 | Granit Br4防弹插板×2、芳纶纤维布料×1、防撕布料×2 等5项 | — |
| CALOK-B止血剂×1 | 医疗站 | 1 | 30分 | CALOK-B止血剂×1、一堆药×1 | — |
| 一次性注射器×4 | 医疗站 | 1 | 1时29分 | SurvL幸存者打火机×1、医疗工具×1、AI-2急救组合×4 等5项 | — |
| Salewa急救包×1 | 医疗站 | 1 | 39分 | 车载急救包×2、Esmarch止血带×2 | — |
| AI-2急救组合×3 | 医疗站 | 1 | 22分 | 一堆药×1 | — |
| CMS手术包×1 | 医疗站 | 1 | 49分 | 医疗工具×1、车载急救包×1、铝固定夹板×1 | — |
| 吗啡注射器×1 | 医疗站 | 2 | 1时13分 | 一堆药×1、安乃近止痛药×1、一次性注射器×1 | — |
| 2A2-(b-TG)兴奋剂注射器×1 | 医疗站 | 2 | 1时20分 | 肾上腺素注射器×1、OLOLO瓶装复合维生素×1、医疗工具×1 等4项 | — |
| xTG-12解毒剂×1 | 医疗站 | 2 | 1时10分 | 肾上腺素注射器×1、AI-2急救组合×2、一堆药×2 | — |
| AHF1-M 兴奋剂注射器×2 | 医疗站 | 2 | 47分 | 一堆药×5、一次性注射器×2、CALOK-B止血剂×1 等4项 | — |
| eTG-change再生兴奋剂注射器×1 | 医疗站 | 2 | 1时20分 | 一堆药×2、一次性注射器×2、Propital×2 等4项 | — |
| 铝固定夹板×2 | 医疗站 | 2 | 53分 | 固定夹板×5、Poxeram冷焊膏×1 | — |
| M.U.L.E. 兴奋剂×1 | 医疗站 | 2 | 1时32分 | Zagustin止血剂×2、吗啡注射器×1、罐装 Max Energy 能量饮料×2 等4项 | — |
| 便携式除颤器×1 | 医疗站 | 2 | 5时15分 | 电磁铁×2、电线×3、电容×4 等5项 | — |
| 2A2-(b-TG)兴奋剂注射器×1 | 医疗站 | 2 | 1时20分 | 肾上腺素注射器×1、OLOLO瓶装复合维生素×1、医疗工具×1 等4项 | — |
| IFAK单兵急救包×2 | 医疗站 | 2 | 50分 | 一堆药×2、军用绷带×2、Esmarch止血带×1 | — |
| 一堆药×3 | 医疗站 | 2 | 48分 | AI-2急救组合×1、无菌绷带×1、力百汀抗生素药片×1 | — |
| 凡士林×2 | 医疗站 | 2 | 55分 | 肥皂×3、Schaman洗发水×2、一堆药×1 | — |
| PNB (16 号化合物) 兴奋剂注射器×2 | 医疗站 | 2 | 1时3分 | AFAK单兵急救包×2、一次性注射器×2、过氧化氢溶液（双氧水）×1 等4项 | — |
| SJ1 TGLabs战斗兴奋剂注射器×5 | 医疗站 | 2 | 1时23分 | 一堆药×7、盐水溶液×2、Propital×3 等4项 | — |
| 医用输血工具×2 | 医疗站 | 2 | 28分 | 硅胶管×1、一次性注射器×1、医疗工具×1 | — |
| Grizzly急救包×2 | 医疗站 | 3 | 1时15分 | 一堆药×4、CAT止血带×2、铝固定夹板×1 | — |
| Surv12野战手术包×1 | 医疗站 | 3 | 1时17分 | 一堆药×3、医疗工具×1、铝固定夹板×1 等4项 | — |
| AFAK单兵急救包×1 | 医疗站 | 3 | 1时 | IFAK单兵急救包×2、军用绷带×1、CAT止血带×1 | — |
| SJ6 TGLabs战斗兴奋剂注射器×2 | 医疗站 | 3 | 1时25分 | SJ1 TGLabs战斗兴奋剂注射器×1、一堆药×2、盐水溶液×1 等4项 | — |
| 人造血（蓝血）注射器×1 | 医疗站 | 3 | 1时15分 | SurvL幸存者打火机×1、医疗工具×1、一堆药×2 等5项 | — |
| LEDX皮肤透照仪×1 | 医疗站 | 3 | 1天9时20分 | 检眼镜×2、电磁铁×3、UHF RFID固定式读取器 ×3 等7项 | — |
| Propital×4 | 医疗站 | 3 | 1时55分 | 布洛芬止痛药×1、金星软膏×1、一堆药×2 | — |
| Zagustin止血剂×1 | 医疗站 | 3 | 1时52分 | CALOK-B止血剂×1、Grizzly急救包×1、一堆药×2 等4项 | — |
| 加密磁带盒×1 | 情报中心 | 1 | 4时 | 一套工具×1、长平头螺丝刀×1、电磁铁×3 等5项 | — |
| 14-4 KORD 站点 K.Arshavin 钥匙卡 (修复)×1 | 情报中心 | 1 | 3时30分 | 加密U盘×1、军用闪存装置×1、VPX闪存模块×1 | — |
| 以 Kerman 提供的哈希代码重新编码的钥匙卡×1 | 情报中心 | 1 | 5时30分 | 装有 Kerman 哈希代码的 U 盘×1、空白 RFID 钥匙卡×1、已激活的克鲁格洛夫钥匙卡×1 | 67c0324acc87f341720274b6 |
| 加密U盘×3 | 情报中心 | 1 | 8时 | 电子元件×1、损坏的GPhone X×1、固态硬盘×1 | — |
| 串口硬盘×1 | 情报中心 | 1 | 3时 | 一套工具×1、长平头螺丝刀×1、损坏的硬盘×3 等4项 | — |
| 以 Prapor 提供的哈希代码重新编码的钥匙卡×1 | 情报中心 | 1 | 5时30分 | 装有 Prapor 哈希代码的 U 盘×1、已激活的克鲁格洛夫钥匙卡×1、空白 RFID 钥匙卡×1 | 67c0324acc87f341720274b6 |
| 地形调查地图×1 | 情报中心 | 1 | 2时 | 加密U盘×1、袖珍日记×1、日记×1 | — |
| 重新编码后的 A.P. 公寓门锁钥匙卡 (蓝)×1 | 情报中心 | 1 | 2时45分 | UHF RFID固定式读取器 ×1、实验室钥匙卡·蓝×1、TerraGroup "蓝色文件夹" 材料×1 | 68e7d1be6656898e78015bd9 |
| 硬盘恢复内容×1 | 情报中心 | 1 | 5时30分 | 打印纸×1、印制电路板×3、电脑CPU×2 | — |
| 从 Fence 笔记本提取的数据×1 | 情报中心 | 1 | 10分 | 打印纸×1、显示卡×1 | 6a5f9becfc12d937d50ae095 |
| 重新编码后的 A.P. 公寓门锁钥匙卡 (红)×1 | 情报中心 | 1 | 2时45分 | UHF RFID固定式读取器 ×1、实验室钥匙卡·红×1、TerraGroup "蓝色文件夹" 材料×1 | 68e7d1be6656898e78015bd9 |
| 破译的情报文件夹×1 | 情报中心 | 1 | 10分 | 军用数据随身碟×1 | — |
| 重新编码后的 A.P. 公寓门锁钥匙卡 (绿)×1 | 情报中心 | 1 | 2时45分 | UHF RFID固定式读取器 ×1、实验室钥匙卡·绿 ×1、TerraGroup "蓝色文件夹" 材料×1 | 68e7d1be6656898e78015bd9 |
| 已激活的克鲁格洛夫钥匙卡×1 | 情报中心 | 1 | 23时30分 | 空白 RFID 钥匙卡×1、克鲁格洛夫的钥匙卡×1 | 67bdf097600629c8f20322e5、67c0324acc87f341720274b6 |
| Digital secure DSP无线电收发器×1 | 情报中心 | 2 | 2时 | Digital secure DSP无线电收发器×1、螺丝刀×1、电子元件×1 | 6331bb0d1aa9f42b804997a6 |
| TerraGroup实验室访问钥匙卡×3 | 情报中心 | 2 | 40分 | UHF RFID固定式读取器 ×1、情报文件夹×1 | — |
| UHF RFID固定式读取器 ×1 | 情报中心 | 2 | 12时 | TerraGroup实验室访问钥匙卡×1、VPX闪存模块×1、先进电子材料教材×1 等4项 | — |
| TerraGroup "蓝色文件夹" 材料×1 | 情报中心 | 2 | 11时 | UHF RFID固定式读取器 ×1、加密U盘×2、军用闪存装置×1 等5项 | — |
| Virtex可编程处理器×1 | 情报中心 | 2 | 23时 | 军用电路板×2、电脑CPU×2、电容×5 等5项 | — |
| Object #11SR 钥匙卡×1 | 情报中心 | 2 | 2天17时33分 | 加密U盘×5、Object #21WS 钥匙卡×1、UHF RFID固定式读取器 ×1 等5项 | — |
| 实验室钥匙卡·紫×1 | 情报中心 | 3 | 5天12时13分 | TerraGroup实验室访问钥匙卡×10、实验室钥匙卡·黄 ×1、情报文件夹×4 等5项 | — |
| 实验室钥匙卡·红×1 | 情报中心 | 3 | 1天1时 | UHF RFID固定式读取器 ×1、TerraGroup实验室访问钥匙卡×10、TerraGroup "蓝色文件夹" 材料×5 等7项 | — |
| 显示卡×1 | 情报中心 | 3 | 15时 | 印制电路板×2、电脑CPU×2、CPU风扇×2 等7项 | — |
| 情报文件夹×1 | 情报中心 | 3 | 1天7时40分 | 军用闪存装置×2、打印纸×1 | — |
| 实验室钥匙卡·绿 ×1 | 情报中心 | 3 | 1天1时 | UHF RFID固定式读取器 ×1、TerraGroup实验室访问钥匙卡×10、TerraGroup "蓝色文件夹" 材料×5 等6项 | — |
| 军用闪存装置×1 | 情报中心 | 3 | 10时 | Virtex可编程处理器×1、加密U盘×2、地形调查地图×2 | — |
| 咸狗牛肉肠×3 | 营养部 | 1 | 33分 | 罐装白盐×1、炖牛肉罐头×1、卫生纸×1 等4项 | — |
| MRE个人即食口粮×2 | 营养部 | 1 | 40分 | 炖美味牛肉罐头（大）×1、军用饼干×2、士腻架能量棒×1 | — |
| 罐装 Max Energy 能量饮料×4 | 营养部 | 1 | 1时48分 | 罐装塔可乐汽水×1、罐装 Majaica 咖啡豆×1、0.6升瓶装水×1 等4项 | — |
| 应急用水×2 | 营养部 | 1 | 1时7分 | 0.6升瓶装水×1、KEKTAPE管道胶带×1、硅胶管×1 | — |
| Iskra（“火花”）单兵口粮×2 | 营养部 | 1 | 48分 | 军用饼干×2、炖牛肉罐头×1、罐装蔬菜泥×1 | — |
| “诺文斯基核动力”金牌格瓦斯600毫升装×2 | 营养部 | 1 | 1时3分 | 0.6升瓶装水×2、Emelya黑麦面包块×2 | — |
| 炖牛肉罐头×1 | 营养部 | 1 | 1时19分 | 炖美味牛肉罐头（大）×1、罐装蔬菜泥×1、黑麦面包块×1 | — |
| 云尔斯顿香烟×5 | 营养部 | 1 | 1时42分 | 阿波罗-联盟香烟×5、42 Signature Blend英式茶×1 | — |
| 炼乳罐头×3 | 营养部 | 2 | 1时22分 | 一包糖×1、一盒牛奶×1 | — |
| 罐装 Majaica 咖啡豆×2 | 营养部 | 2 | 45分 | 罐装 Dr. Lupo 咖啡豆×1、“Kite”火药×1 | — |
| 士腻架能量棒×5 | 营养部 | 2 | 22分 | Alyonka 巧克力×1、军用饼干×1 | — |
| Aquamari带滤嘴水瓶×3 | 营养部 | 2 | 4时52分 | 0.6升瓶装水×5、防毒面具滤罐×1、滤水器×1 等4项 | — |
| 一包糖×1 | 营养部 | 2 | 1时23分 | Alyonka 巧克力×2 | — |
| 炖美味牛肉罐头（大）×3 | 营养部 | 2 | 2时30分 | 炖牛肉罐头×4、Emelya黑麦面包块×2 | — |
| 罐装热棒能量饮料×20 | 营养部 | 3 | 12时 | 纯净水×1、一包糖×1、罐装 Majaica 咖啡豆×1 等6项 | — |
| Tarkovskaya瓶装伏特加×10 | 营养部 | 3 | 1时36分 | “凶狠跑刀崽”私酒×1、0.6升瓶装水×5 | — |
| 瓶装 Pevko 淡啤酒×10 | 营养部 | 3 | 10时 | 黑麦面包块×5、“诺文斯基核动力”金牌格瓦斯600毫升装×3、纯净水×1 | — |
| 0.6升瓶装水×10 | 营养部 | 3 | 1时51分 | 纯净水×1、硅胶管×1 | — |
| Dan Jackiel瓶装威士忌×3 | 营养部 | 3 | 1时47分 | 0.6升瓶装水×2、Tarkovskaya瓶装伏特加×2、Alyonka 巧克力×1 等5项 | — |
| 0.6升瓶装水×1 | 集水器 | 1 | 20分 | 0.6升瓶装水×1 | — |
| 应急用水×2 | 集水器 | 2 | 30分 | 西柚汁×1、硅胶管×1 | — |
| Aquamari带滤嘴水瓶×5 | 集水器 | 2 | 1时 | 0.6升瓶装水×5、Aquapeps饮用水净化片×1 | — |
| 纯净水×1 | 集水器 | 3 | 5时25分 | 滤水器×0.66 | — |
| “凶狠跑刀崽”私酒×1 | 酿酒处 | 1 | 3时3分 | 一包糖×2、纯净水×1 | — |
| 实体比特币×1 | 比特币矿场 | 1 | 3天11时20分 |  | — |

## 3. 藏身处（hideout）

### 3.1 站总览（26 站，按 areaType 升序）

> 等级数=levels 数组长度；最高级建造时间=最高级 constructionTime；加成摘要=各等级 bonuses 的 type 去重列表（— 表示无加成）。

| 中文名 | station id | 等级数 | 最高级建造时间 | 加成（bonuses type） |
|---|---|---|---|---|
| 通风 | `5d473c1e081959000e530190` | 3 | 10时 | — |
| 安保 | `5d484fb3654e7600681d9314` | 3 | 17时 | — |
| 卫生间 | `5d484fba654e7600691aadf7` | 3 | 7时 | UnlockArmorRepair、RepairArmorBonus |
| 仓库 | `5d484fc0654e76006657e0ab` | 4 | 4天 | StashSize、UnlockWeaponModification |
| 发电机 | `5d3b396e33c48f02b81cd9f3` | 3 | 16时 | AdditionalSlots |
| 供暖 | `5d388e97081959000a123acf` | 3 | 8时 | EnergyRegeneration、DebuffEndDelay |
| 集水器 | `5d484fc8654e760065037abf` | 3 | 16时 | HydrationRegeneration、AdditionalSlots |
| 医疗站 | `5d484fcd654e7668ec2ec322` | 3 | 12时 | HealthRegeneration |
| 营养部 | `5d484fd1654e76006732bf2e` | 3 | 14时 | EnergyRegeneration、HealthRegeneration、HydrationRegeneration |
| 休息区 | `5d484fd6654e76051d3cc791` | 3 | 2时 | DebuffEndDelay、EnergyRegeneration、HealthRegeneration、MaximumEnergyReserve、AdditionalSlots |
| 工作台 | `5d484fda654e7600681d9315` | 3 | 11时 | UnlockWeaponRepair、RepairWeaponBonus |
| 情报中心 | `5d484fdf654e7600691aadf8` | 3 | 1天 | ScavCooldownTimer、QuestMoneyReward、InsuranceReturnTime、RagfairCommission |
| 靶场 | `5d484fe3654e76006657e0ac` | 3 | 1天 | SkillGroupLevelingBoost |
| 图书馆 | `5d494a0e5b56502f18c98a02` | 1 | 2天6时 | ExperienceRate、SkillGroupLevelingBoost |
| Scav宝箱 | `5d494a175b56502f18c98a04` | 1 | 3天8时 | — |
| 照明 | `5d494a205b56502f18c98a06` | 3 | 6时 | — |
| 荣耀展柜 | `5d494a295b56502f18c98a08` | 3 | 1天 | SkillGroupLevelingBoost |
| 空气过滤单元 | `5d494a315b56502f18c98a0a` | 1 | 2天 | SkillGroupLevelingBoost、AdditionalSlots |
| 太阳能 | `5d494a385b56502f18c98a0c` | 1 | 3天 | FuelConsumption |
| 酿酒处 | `5d494a3f5b56502f18c98a0e` | 1 | 2天 | — |
| 比特币矿场 | `5d494a445b56502f18c98a10` | 3 | 4天10时 | AdditionalSlots |
| 易碎墙 | `637b39f02e873739ec490215` | 6 | 12时 | FuelConsumption、EnergyRegeneration、HealthRegeneration、ExperienceRate、SkillGroupLevelingBoost |
| 健身区 | `6377a9b9a93bde8fa30eb79a` | 1 | 4时 | — |
| 武器架 | `63db64cbf9963741dc0d741f` | 3 | 1天 | — |
| 装备架 | `65e5bb1713227bb7690cea0a` | 3 | 1天 | — |
| 仪式圈 | `667298e75ea6b4493c08f266` | 1 | 1时 | AdditionalSlots |

### 3.2 建造需求示例（抽样 3 站）

> 字段结构说明：每级建造需求包含 4 类字段——`itemRequirements`（物品需求，含 count、attributes.foundInRaid 战局外标记 / attributes.tool 工具标记）、`stationLevelRequirements`（前置站等级）、`skillRequirements`（技能等级）、`traderRequirements`（商人等级/声望）。bonuses 为生效加成（type + value）。

**工作台**（`5d484fda654e7600681d9315`，areaType 10，3 级）
- Lv1 建造 0秒：物品需求 螺母×2(战局外)、螺栓×2(战局外)、Leatherman多功能工具钳×1(战局外)；站需求 —；技能需求 —；商人需求 —；加成 UnlockWeaponRepair=1
- Lv2 建造 1时：物品需求 一套工具×3(战局外)、电钻×2(战局外)、螺栓×10(战局外)、武器零件×3(战局外)、"Master"锉刀套装×1(战局外)；站需求 照明 Lv1、工作台 Lv1；技能需求 —；商人需求 —；加成 RepairWeaponBonus=0.03
- Lv3 建造 11时：物品需求 卢布×395000(战局外)、Elite钳子×2、#FireKlean牌枪润滑油×1、罐装铝热剂×5(战局外)；站需求 发电机 Lv2、仓库 Lv2、工作台 Lv2；技能需求 —；商人需求 —；加成 RepairWeaponBonus=0.06

**比特币矿场**（`5d494a445b56502f18c98a10`，areaType 20，3 级）
- Lv1 建造 1天10时：物品需求 CPU风扇×15(战局外)、供电单元×10、电源线×15(战局外)、VPX闪存模块×2(战局外)、T形插座×10(战局外)；站需求 情报中心 Lv2；技能需求 —；商人需求 —；加成 AdditionalSlots=10
- Lv2 建造 2天2时：物品需求 CPU风扇×15(战局外)、供电单元×10(战局外)、印制电路板×15、相位控制继电器×10(战局外)、军用电源滤波器×5(战局外)；站需求 比特币矿场 Lv1、发电机 Lv3；技能需求 —；商人需求 —；加成 AdditionalSlots=15
- Lv3 建造 4天10时：物品需求 CPU风扇×25、硅胶管×15(战局外)、电动马达×10(战局外)、压力表×10(战局外)、6-STEN-140-M军用电池×2(战局外)；站需求 比特币矿场 Lv2、太阳能 Lv1、集水器 Lv3；技能需求 —；商人需求 —；加成 AdditionalSlots=25

**易碎墙**（`637b39f02e873739ec490215`，areaType 22，6 级）
- Lv1 建造 0秒：物品需求 —；站需求 医疗站 Lv1、集水器 Lv1；技能需求 —；商人需求 —
- Lv2 建造 12时：物品需求 —；站需求 易碎墙 Lv1；技能需求 —；商人需求 —；加成 FuelConsumption=0.05、EnergyRegeneration=-0.05、HealthRegeneration=-0.05、ExperienceRate=-0.03、SkillGroupLevelingBoost=-0.03
- Lv3 建造 1天：物品需求 —；站需求 易碎墙 Lv2；技能需求 —；商人需求 —；加成 FuelConsumption=0.05、EnergyRegeneration=-0.05、HealthRegeneration=-0.05、ExperienceRate=-0.03、SkillGroupLevelingBoost=-0.03
- Lv4 建造 3时：物品需求 Fierce Blow重击锤×1(战局外)；站需求 易碎墙 Lv3；技能需求 —；商人需求 —；加成 FuelConsumption=0.05、EnergyRegeneration=-0.05、HealthRegeneration=-0.05、ExperienceRate=-0.03、SkillGroupLevelingBoost=-0.03
- Lv5 建造 3时：物品需求 金属切割剪刀×1(战局外)、一套工具×1(战局外)；站需求 易碎墙 Lv4；技能需求 —；商人需求 —；加成 FuelConsumption=0.05、EnergyRegeneration=-0.05、HealthRegeneration=-0.05、ExperienceRate=-0.03、SkillGroupLevelingBoost=-0.03
- Lv6 建造 12时：物品需求 波纹软管×2(战局外)、管道胶带×1(战局外)、一套工具×1(战局外)、Elite钳子×1(战局外)、金属零件×5(战局外)、Xenomorph发泡密封胶×1(战局外)、电线×2(战局外)、电灯泡×2(战局外)；站需求 易碎墙 Lv5；技能需求 —；商人需求 —；加成 FuelConsumption=-0.2、EnergyRegeneration=0.2、HealthRegeneration=0.2、ExperienceRate=0.12、SkillGroupLevelingBoost=0.12

## 4. SPT 对照

### 4.1 藏身处结构对照

| 项目 | live 快照 | SPT 4.1.2 | 差异 |
|---|---|---|---|
| 站总数 | 26 | `areas.json` 27 条 + `customAreas.json` 1 条 = 28 区域 | SPT 多 2 个区域 |
| areaType 覆盖 | 0-27 缺 21、25 | 全部 0-27（含 21、25） | SPT 含 type 21（衣帽间 wardrobe，`customAreas.json`）与 type 25（`640b2d867f4185aa520d08ba`，areas.json 内） |
| 生产配方 | 214（crafts.json） | `production.json`：recipes 220 + scavRecipes 5 + cultistRecipes 1 = 226 | SPT 多 12（含 scav/教派配方，areaType 21 配方 17 条为 live 无对应站的产物） |

> SPT `areas.json` 字段结构：`_id` / `type` / `enabled` / `needsFuel` / `requirements` / `stages`（stages 每级含 requirements、bonuses、constructionTime、slots、container、autoUpgrade 等）；live hideout.json 字段结构：`levels[].{constructionTime, traderRequirements, stationLevelRequirements, itemRequirements, skillRequirements, bonuses, description}`。live 的"站前置"（stationLevelRequirements）在 SPT 中表现为 stages requirements 里 `type:"Area"` 的条目。
> SPT production.json 配方字段：`_id` / `areaType` / `requirements`（type: Area/Item/Resource）/ `productionTime` / `endProduct` / `count` / `locked` / `needFuelForAllProductionTime` / `continuous` / `productionLimitCount`。

### 4.2 易物对照（抽样）

> 任务描述点名的 `5ac3b934156ae10c4430e83c` 实为 **Ragman**（Mechanic 的 id 是 `5a7c2eca46aef81a7ca2145d`），故两个商人一并抽样。SPT 侧 barter_scheme 数量含货币（卢布/美元/欧元）购买条目，需剔除货币后才是物品易物。

| 商人 | SPT assort barter_scheme 总条目 | SPT 其中物品易物 | live barters 数 | 差异（live − SPT 物品易物） |
|---|---|---|---|---|
| Mechanic（`5a7c2eca46aef81a7ca2145d`） | 589 | 100 | 108 | +8 |
| Ragman（`5ac3b934156ae10c4430e83c`） | 168 | 53 | 67 | +14 |

> 结论：SPT 4.1 的商人 assort 是独立维护的子集，barter 数量与 live 有明显出入（且含货币条目）；做易物类 mod 时不要以 live 表为准覆盖 SPT assort，应按 SPT 实际文件增量修改。live 表的主要价值是"物品组合参考"（哪些物品可换什么、限购/等级设定量级）。

## 5. 使用建议

1. **易物 mod**：以 SPT `assort.json` 的 barter_scheme 为基线，用本表 live 数据作"组合灵感"与数值量级参考（minTraderLevel 1-4、buyLimit 常见 1-10、restockAmount 大额）。修改前先读 `xedit-automation`/商人相关 KB 配方，遵循 SPT 4.1 的 `assort` 结构（items + barter_scheme + loyal_level_items）。
2. **制作 mod**：SPT `production.json` 配方按 areaType 组织（workbench=10 92 条、lavatory=2 42 条、medstation=7 24 条、intelligence-center=11 23 条为 Top4），live 214 条配方可作对照，新增配方需在 `recipes`（或 scavRecipes/cultistRecipes）追加完整字段（含 requirements 的 Area 前置）。
3. **藏身处 mod**：SPT 以 `areas.json` stages + `production.json` 双文件驱动；live 26 站 vs SPT 28 区域的主要差异是 type 21（衣帽间）与 type 25。改站等级需求时用 SPT stages 的 requirements 数组（type:"Item"/"Area"/"Skill"/"Trader"），勿直接迁移 live 的字段结构。
4. **数据时效**：本表为 2026-09-02 快照；live 更新（如新任务解锁易物）后需重新 dump。join 用 `items_zh.json` 的 "<id> Name" key；新物品 id 若 join 失败会以 raw id 展示，属预期。
5. **只读约束**：本表为 curated 产物，勿回写 archive/ 原始快照；SPT 对照源勿直接修改。

## 附：生成信息

- 生成脚本：`D:\Temp\opencode\gen-barters-crafts-hideout.js`（一次性工具，未入库）
