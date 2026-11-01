---
version: [3.11, 4.1, 5.0]
domain: both
topic: pack-profile
source: curated
title: 整合包矩阵 — 四个包的关系、共有底座与排序框架
keywords: [整合包, 矩阵, Norvinsk_Forever, Life_in_Norvinsk, Exclusive_Tarkov, Inescapable_Tarkov, 共有底座, 排序框架, MO2, 包档案]
summary: 四个整合包的索引与关系页：Inescapable（SPT 5.0 自建线）、Norvinsk_Forever（4.1.6 主线，以 Life 3.11.4 为蓝本重建）、Life_in_Norvinsk（3.11.4 主力）、Exclusive_Tarkov（3.11.4 入门，Life 的子集版）；三包共有 41 组组件，Life 与 Exclusive 名称交集 95 组，NF 与 Life 交集 59 组。
---
# 整合包矩阵 — 四个包的关系、共有底座与排序框架

> 用途：四个整合包的**索引页**——哪条 SPT 线、彼此什么关系、共用哪些组件、排序怎么排。详细构成见同目录各包档案（不依赖 E: 实包即可回答包间问题）。
> 数据来源：`scripts/packs/scan-pack.mjs` 对四个包 `mods/` + `profiles/*/modlist.txt` + 每 mod `meta.ini` 的只读扫描（2026-11-01）。实包路径只作溯源。

## 一、矩阵速览

| 包 | SPT 线 | 定位 | gamePath | 主 profile | mods 目录 | 条目（启用/禁用） |
|----|--------|------|----------|------------|-----------|-------------------|
| [Inescapable Tarkov](inescapable-tarkov.md) | **5.0**（Alpha） | 自建为主的新线：ITBS 机器人 + Softcore + QoL | E:/Game/EFT_Offline/SPT_5xx | Default | 12 | 12（11/1） |
| [Norvinsk_Forever](norvinsk-forever.md) | **4.1.6** | 以 Life 3.11.4 为蓝本重建的主线 | E:/Game/EFT_Offline/SPT_41x | Default | 129 | 114（100/14） |
| [Life_in_Norvinsk](life-in-norvinsk.md) | **3.11.4** | 现役主力包（同时是 NF 的参照基准） | E:/Game/EFT_Offline/SPT_3114 | 单机运行整合包 | 196 | 179（169/10） |
| [Exclusive_Tarkov](exclusive-tarkov.md) | **3.11.4** | 面向新手入门版（Life 的子集） | E:/Game/EFT_Offline/SPT_3114 | 单机运行整合包 | 130 | 114（114/0） |

> Life 另有 `default`（165/13）与 `生化危机配置`（161/15）两个分支 profile；NF 的 15 条、Life 与 Exc 的 16–17 条分隔符不计入 mod 条目。

## 二、血统与关系

- **Life_in_Norvinsk → Exclusive_Tarkov**：Exclusive 是 Life 的新手入门版，v1.0 / v1.2 分两次「下放」Life 的 AI 优化（BigBrain / Waypoints / SAIN / botplacementsystem / progressivebotsystem / LootingBots 等）。名称级交集 **95 组**，是四个包里最像的一对。
- **Life_in_Norvinsk → Norvinsk_Forever**：NF 是 4.1.6 上按 Life 的玩法与内容覆盖**重建**出来的（3.11.4 的 mod 本体无法直达 4.1.6）。名称级交集 **59 组**；差异主要是 3.11 专有内容未移植，以及 4.1 侧新增的性能/画质套件。
- **Inescapable_Tarkov**：与 3.11/4.1 两代**没有组件交集**（12 个 mod 全部自建或工具链），是 SPT 5.0 的新线；只有 `AccurateCircularRadar` 一件与旧包生态相关。
- 三包（NF / Life / Exclusive）共有的组件是 **41 组**，见下一节——这基本就是「SamMeow 整合包公共底座」。

## 三、共有底座（NF ∩ Life ∩ Exclusive，41 组）

判定口径：以 `modlist.txt` 中启用项的名字 token 做交集（不依赖 id 体系，见第五节）。清单如下，按名称排序：

| 组件（可辨识名） | NF | Life | Exc | 说明 |
|---|---|---|---|---|
| 声音可视化-accessibilityindicators | ✓ | ✓ | ✓ | 将声音事件转换为可视化提示 |
| 新商人-毒蝎-acidphantasm-scorpion | ✓ | ✓ | ✓ | 提供藏身处相关物资的商人 |
| 竞技场商人的箱子-Adam's Boxes at Ref (BARF) | ✓ | ✓ | ✓ | 在竞技场商人处添加储物箱 |
| 新武器-AKResonant | ✓ | ✓ | ✓ | 新增 COD:MW2019 的 Resonant AK 步枪 |
| 物品感应-AmandsSense | ✓ | ✓ | ✓ | 以视觉高亮辅助发现周围物品 |
| 新武器-WTT武器库-WTT - Armory服务端 | ✓ | ✓ | ✓ | 扩展 WTT 武器与配件库 |
| 新商人-Artem | ✓ | ✓ | ✓ | 新增商人 Artem 与其相关任务 |
| 新配件-Eco-Attachment Emporium | ✓ | ✓ | ✓ | 扩展武器配件的获取与流通 |
| 自动存放-AutoDeposit - 已AI优化 | ✓ | ✓ | ✓ | 自动存放物品以减少手动整理 |
| 更好的机瞄-BetterRearSights | ✓ | ✓ | ✓ | 改善武器机瞄准具的视野 |
| AI拟人MOD的前置框架-BigBrain - 已AI优化 | ✓ | ✓ | ✓ | 为 AI 提供自定义行为层的公共前置框架 |
| Boss身上有lega徽章-bosseshavelegamedals | ✓ | ✓ | ✓ | 让 Boss 携带并掉落徽章 |
| boss刷新通知-BossNotifier | ✓ | ✓ | ✓ | 在 Boss 刷新时发出通知 |
| Acid的AI生成系统-botplacementsystem - 已AI优化 | ✓ | ✓ | ✓ | 控制 AI 在地图上的生成与分布 |
| 平衡的夜视装备视效-BRNVG服务器文件 | ✓ | ✓ | ✓ | 以真实感着色器重做夜视装备的成像效果 |
| 新配件-ConsortiumOfThings | ✓ | ✓ | ✓ | 新增大量武器配件 |
| 拾取时快速放入容器-ContainerQuickLoot - 已AI优化 | ✓ | ✓ | ✓ | 拾取物品时自动放入指定容器 |
| 内置动态地图-DynamicMaps | ✓ | ✓ | ✓ | 在战局内提供可查看的地图 |
| 从武器架装备武器-EquipFromWeaponRack | ✓ | ✓ | ✓ | 允许从武器架直接装备武器 |
| -单独-客户端MOD-F12设置cfg所在 | ✓ | ✓ | ✓ | 客户端 F12 配置载体（界面习惯与难度选项会覆盖此项） |
| 新物品-镀金钥匙容器-GildedKeyStorage服务器文件 | ✓ | ✓ | ✓ | 新增集中存放钥匙的容器 |
| 防卡手-HandsAreNotBusy | ✓ | ✓ | ✓ | 减少双手忙碌时的操作卡顿 |
| SAIN增强版预设-Live in Norvinsk AI preset | ✓ | ✓ | ✓ | 整合包专属 SAIN 预设（持续更新） |
| 腰包-WTT-PackNStrap服务器端文件 | ✓ | ✓ | ✓ | 增加更多可放进保险箱的容器，并修正臂包售价 |
| 新商人-油漆匠-Painter | ✓ | ✓ | ✓ | 新增油漆匠商人及涂装相关物品 |
| Acid的AI装备管理系统-progressivebotsystem - 已AI优化 | ✓ | ✓ | ✓ | 按进度调整 AI 的装备与配置 |
| 任务跟踪显示-QuestTracker | ✓ | ✓ | ✓ | 在界面显示任务目标进度 |
| 物品容器间快速移动-QuickMoveToContainer | ✓ | ✓ | ✓ | 支持在容器间快速移动物品 |
| 新商人-荷官-Croupier (random loadouts + flea quicksell) | ✓ | ✓ | ✓ | 新增随机装备商人，附带跳蚤市场快速出售 |
| 扫敌雷达-RadarStandalone | ✓ | ✓ | ✓ | 以圆形雷达显示周围敌人 |
| 直接搜索已打开容器-SearchOpenContainers | ✓ | ✓ | ✓ | 可直接搜索尚未搜刮完的容器 |
| 显示价格-showmethemoney | ✓ | ✓ | ✓ | 为物品价格提示提供快速出售功能 |
| 新商人-竞技场商人-ref-sptfriendly-quests | ✓ | ✓ | ✓ | 为竞技场商人添加适配单人模式的任务 |
| 新装备、衣服-TacticalGearComponent | ✓ | ✓ | ✓ | 新增战术装备与服装内容 |
| 任务列表修复-TaskListFixes | ✓ | ✓ | ✓ | 修复任务列表相关问题 |
| 更好的枪匠界面-Tradermodding服务器文件 | ✓ | ✓ | ✓ | 改进商人处的武器改装界面 |
| 商人列表滚动-TraderScrolling | ✓ | ✓ | ✓ | 让商人交易列表支持滚动浏览 |
| UI大修-UIFixes | ✓ | ✓ | ✓ | 集中修复多项界面与交互问题 |
| 身上任何物品均可绑定快捷键-UseItemsFromAnywhere | ✓ | ✓ | ✓ | 可为身上任意物品绑定快捷键 |
| 直接使用未捡物品-UseLooseLoot | ✓ | ✓ | ✓ | 可直接使用地面上的未拾取物品 |
| AI路径扩展-Waypoints - 已AI优化 | ✓ | ✓ | ✓ | 扩展地图路径点以改善 AI 寻路 |

> 读法：这 41 组是跨版本稳定的骨架。要判断「换包后体验差在哪」，先看某个新包缺了这张表里的哪几项，再谈版本差异。

## 四、排序框架（三包共用，Inescapable 例外）

- **前缀即优先级**：MO2 `modlist.txt` 里越靠后优先级越高；包内用 `[15]` → `[2]` 的数字前缀从高到低排列，`[1]前置` 沉底。改顺序 = 改覆盖关系，不是审美问题。
- **分隔符**：以 `_separator` 结尾的伪 mod 把清单切成「数值再平衡 / 汉化 / MOD工具 / 修复优化 / 破坏沉浸感 / 物品整理 / 全局大修 / 战局内大修 / 装备物品大修 / AI行为 / 核心大修 / 商人任务 / 新配件 / 新内容」等组。
- **开关代价**：`[5]整合包核心MOD`（Life 另有 `[5.5]`）整组开关**需要重开档**；`[1]前置` 组不要关。这两条写在分隔符名字里，是包作者给定的纪律。
- **Inescapable 例外**：体量小（12 项），沿用 `[1]前置` / `[5]核心` / `工具` / `界面` 语义但没采用数字降序的 87 overlay 规则（那套规则属于 3.11→4.1 迁移期，见 `docs/wayfinder/tickets/009-migrate-3114-to-spt41.md`，已标历史）。

## 五、上游 id 口径（别混用）

`meta.ini` 的 `url` 有**三套互不兼容的编号**，本目录档案统一标注：

| 体系 | URL 形态 | 出现在 | 备注 |
|------|----------|--------|------|
| Forge（现址） | `sp-mod.com/mod/<id>` | NF（4.1.6） | 4.1 时代下载源 |
| Hub（旧站） | `hub.sp-tarkov.com/files/file/<id>` | Life / Exclusive（3.11.4） | 同一 mod 的编号与 Forge 不同（例：UI Fixes = Hub 1860 / Forge 1342） |
| Nexus | `nexusmods.com/.../mods/<id>` | 少量 | 需要 Nexus 账号 |

> 所以「某个 mod 是不是同一个」不能用 id 直接比。4.1.6 包对 Forge id 可以直接用；3.11.4 包的 Hub id 只能用于旧站溯源。

## 六、数据与复现

```powershell
# 扫描一个包（只读；输出结构化快照）
node scripts/packs/scan-pack.mjs --pack-root "E:/Game/EFT_Offline/Norvinsk_Forever" --slug norvinsk-forever --out .artifacts/packs/norvinsk-forever/scan.json

# 渲染 markdown 表格（分组总览 / 分组明细）
node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/norvinsk-forever/scan.json --mode summary
node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/norvinsk-forever/scan.json --mode detail
```

- 四个包的扫描快照：`.artifacts/packs/<slug>/scan.json`（gitignored，可随时重跑）。
- 长期快照副本：`knowledge/spt-kb/archive/packs/<slug>.json`。

## 七、相关页

- 各包完整档案：同目录 [inescapable-tarkov.md](inescapable-tarkov.md) / [norvinsk-forever.md](norvinsk-forever.md) / [life-in-norvinsk.md](life-in-norvinsk.md) / [exclusive-tarkov.md](exclusive-tarkov.md)
- Forge 生态增量：[Forge 更新日志](../operations/forge-update-log.md)
- 4.1.6 重建的移植评估：[porting-candidate-assessment](../migration/porting-candidate-assessment.md)
- 3.11→4.1 迁移规则：[413-fork-transition](../operations/413-fork-transition.md)
