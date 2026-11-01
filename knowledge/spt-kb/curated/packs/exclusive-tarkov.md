---
version: [3.11]
domain: both
topic: pack-profile
source: curated
title: "《独享的塔科夫》Exclusive Tarkov 包档案（SPT 3.11.4 · 入门线）"
keywords: [Exclusive_Tarkov, 独享的塔科夫, SPT 3.11.4, 入门整合包, "Life_in_Norvinsk 血统", 单机运行整合包, MO2 modlist, 本体覆盖层, Assembly-CSharp, CustomProfiles, SamMeow.PerformanceTweaks, 包档案]
summary: "《生活在诺文斯克》3.11.4 入门线：mods 目录 130（114 mod + 16 分隔符），唯一 profile「单机运行整合包」114 条全启用、0 禁用；本体靠 [1]前置覆盖层（Assembly-CSharp.dll 15,243,264 B + core.json/settings.json/order.json）；上游 51 条旧 Hub、0 条 Forge；v1.0/v1.2 两次下放 Life 的 AI 优化。"
---

# 《独享的塔科夫》Exclusive Tarkov 包档案（SPT 3.11.4 · 入门线）

> **用途**：本页是仓库内可离线查询的**包构成档案**——回答「这个包里有什么、谁覆盖谁、本体被改了什么、有哪些已知坑」，不依赖 E: 实包即可查阅；实包路径只作溯源用。数字唯一权威来源是只读扫描快照 `.artifacts/packs/exclusive-tarkov/scan.json`（capturedAt 2026-10-31T17:13:16Z，北京时间 2026-11-01 01:13；分组按「分隔符作区块页脚」口径结算），包内文档只用于定位与史实，两者冲突时以 scan.json 为准并在正文标注。
> 现场核对：2026-11-01 对 `E:\Game\EFT_Offline\Exclusive_Tarkov` 与 `E:\Game\EFT_Offline\SPT_3114` 做了只读复核（目录枚举、哈希、JSON 差异），标注为「实测」的行即来自该次核对。

## 一、身份与环境

| 项 | 值 | 依据 |
|---|---|---|
| 包名（中文 / 英文） | 《独享的塔科夫》/ Exclusive Tarkov | 包目录名、docs/README.md |
| 项目标识（core.json） | `Exclusive-Tarkov 1.2 AI-Enhanced Based on SPT` | 覆盖层 core.json（实测） |
| SPT 版本线 | **3.11.4**（sptVersion=3.11.4，本体与覆盖层两处一致） | core.json |
| EFT 客户端版本 | compatibleTarkovVersion=`0.16.0.35392`；客户端日志目录名记 `0.16.1.3.35392` → 以何为准**待确认** | core.json / 文档 |
| 游戏根 gamePath | `E:/Game/EFT_Offline/SPT_3114`（实例 ini 中以 ByteArray 转义存储） | MO2 实例 ModOrganizer.ini |
| MO2 gameName | `SPT3` | 同上 |
| MO2 实例名 / 注册位置 | `SPT3.11.4《独享的塔科夫》`，注册在 `%LOCALAPPDATA%\ModOrganizer\` 下 | 同上 |
| MO2 版本 | `2.5.3`（实例 ini 的 version 字段；包内文档记「2.5.3beta2 世代」） | ini / 文档 |
| profile | **单机运行整合包**（唯一 profile） | `profiles/单机运行整合包/modlist.txt` |
| MO2 数据目录 | `E:\Game\EFT_Offline\Exclusive_Tarkov`（仅 docs / downloads / mods / overwrite / profiles / webcache，**无根级 ModOrganizer.ini**） | 目录枚举（scan.json 的 mo2 字段为 null 即因缺 ini） |
| mods 目录数 | **130** = 114 个实 mod + 16 个 `_separator` 占位目录 | 目录枚举 + modlist |
| modlist 条目 | 130 条 = `+` 114（全为 mod）+ `-` 16（全为分隔符） | modlist.txt（实测） |
| 启用 / 禁用 | **114 / 0** | scan.json |
| 服务端 mod 目录（合并后唯一名） | 57 个 | 实测（`mods/*/user/mods` 枚举） |
| 包内文档 | 仅 `docs/README.md`（2026-10-14 建档的档案卡，非作者原文） | 目录枚举 |
| 扫描快照 | `.artifacts/packs/exclusive-tarkov/scan.json`，capturedAt `2026-10-31T17:13:16Z` | scan.json |

### 1.1 血统：Life_in_Norvinsk 的新手入门版

- **定位**：与《生活在诺文斯克》同一 SPT 3.11.4 线、同一 MO2 世代、同一 profile 名（`单机运行整合包`），本包是其**新手入门版/子集**：面向塔科夫萌新，转正版或继续离线都适用（文档「注」）。体量对比：本包 114 条 mod，Life 主线 179 条（见 [整合包矩阵](README.md)）。
- **AI 优化下放**：v1.0 下放 Life v0.3 的 AI 优化，v1.2 下放 v0.3.3 的 AI 优化。落点在本包里可数：**9 条** mod 名带「- 已AI优化」标记（[6]AI行为 组 6 条、[10] 组 2 条、[7] 组 1 条），另有整合包专属 `SAIN增强版预设-Live in Norvinsk AI preset`。
- **名称级交集**：与 Life 的名称交集为 **95 组**（矩阵页口径），是四个包里最像的一对；差异集中在内容量（新商人/新武器/新配件）与性能层。
- **继承痕迹**：覆盖层的 `order.json` 是 107 条超集，只对应 57 个实际服务端目录（见 §4.3），其中 `revival-mod`、`SkillsExtended`、`SPT-Realism` 等条目在本包并无目录——典型的「从更大包沿用排序表」。

### 1.2 更新史（依据 docs/README.md，日期为包作者记法）

| 版本 | 日期 | 要点 |
|---|---|---|
| v0.3 | 25-11-12 | MO2 内 SPT 识别插件更新（区分 AkiSPT / SPT3 / SPT4）；新增 nofirhideout、PlayerLives（重生）、Volcano-subtitle；关闭 revival-mod 改用 PlayerLives；调整专属开局技能与库存；WTT-PackNStrap 安全容器全部禁用以排查爆红 |
| v0.5 | 26-01-31 | 跟随 Live_in_Norvinsk_Beta v0.1.8；暂时关闭 CustomProfiles 的老兵与测试账号开局（等下版修复） |
| v1.0 AI-Enhanced | 26-06-16 | 下放 Life v0.3 的 AI 优化（BigBrain、Waypoints、SAIN v4.2.0 增强版、botplacementsystem、progressivebotsystem、LootingBots、That.s.Lit、friendlypmc、autocompatframework、HollywoodFX/Graphics、ContainerQuickLoot、AutoDeposit 等）；修复关游戏无响应、加快服务器启动、补全汉化 |
| v1.2 AI-Enhanced | 26-09-03 | 下放 Life v0.3.3 的 AI 优化；新增自制 `SamMeow.PerformanceTweaks`（针对本体缺陷的帧数优化）；修复 CustomProfiles 老兵开局报错；腰带包 / WTT 武器配件可刷进静态容器；避免腰包套腰包 |
| 特别优化版 | 26-09-08 | 发布 SPT3.11.5-Live-In-Norvinsk 特别优化版（藏身处与任务界面更快、交任务卡顿减轻、错误提示更明确、设置更不易丢失、启动器多项修复）。**产物不在本包目录内**，本包仍为 3.11.4 |

> 现场与文档的偏差：v1.0 记 SAIN 为 v4.2.0 增强版，包内现为 `SAIN-Moew-v4.4.0增强版`（后续跟进升级）；文档未记录这次升级的日期。

## 二、构成总览

| 分组（MO2 分隔符） | 条目 | 启用 | 禁用 |
|---|---|---|---|
| [15]《现实化诺文斯克》数值、沉浸感再平衡 | 6 | 6 | 0 |
| [14]汉化模组 | 1 | 1 | 0 |
| [13]MOD工具 | 1 | 1 | 0 |
| [12]修复、优化 | 7 | 7 | 0 |
| [11]作弊MOD | 3 | 3 | 0 |
| [10]物品高效整理（告别华容道） | 10 | 10 | 0 |
| [9]全局功能大修 | 8 | 8 | 0 |
| [8]战局内功能大修 | 12 | 12 | 0 |
| [7]装备、物品功能大修 | 12 | 12 | 0 |
| [6]AI行为 | 9 | 9 | 0 |
| [5]整合包核心MOD-基于原玩法的大修（开关此类需重开档） | 1 | 1 | 0 |
| [4]自定义商人、任务 | 13 | 13 | 0 |
| [3.1]新配件 | 6 | 6 | 0 |
| [3]新武器 | 9 | 9 | 0 |
| [2]新内容 | 10 | 10 | 0 |
| [1]前置MOD（不要关） | 6 | 6 | 0 |
| **合计** | **114** | **114** | **0** |

**读法与口径**

- **130 不是 mod 数**：`mods/` 下 130 个目录 = 114 个实 mod + 16 个 `_separator` 占位目录（每个占位目录内只有 meta.ini，实测体积 438–448 B）。真实条目以 `modlist.txt` 的 `+` 行为准：114 条，全部启用。
- **禁用数为 0**：16 条 `-` 条目全部是分隔符（MO2 的分隔符默认写 `-`，不可勾选），文件里没有任何被禁用的实际 mod。唯一 `+` 的分隔符是 `[14]汉化模组`。
- **分组形态**：16 个分隔符把清单切成 16 个区块，从 `[15]` 递减到 `[1]`（文件自上而下），每块全部启用。
- **形态构成**（114 条实 mod，实测）：BepInEx 客户端 61 条、服务端（`user/` 或 `SPT_Data/`）63 条、两端兼有 10 条（61+63−10=114）；109 条带 `version` 字段、5 条无；58 条 `meta.ini` 有 `comments` 说明。
- **上游编号口径**（`meta.ini` 的 `url`，三套编号不可混用；3.11.4 两包用旧 Hub，4.1.6 的 Norvinsk_Forever 才是 Forge）：

| 上游来源 | 条数 | 形态 |
|---|---|---|
| 旧 Hub（`hub.sp-tarkov.com/files/file/<id>`） | **51** | `Hub #id`，仅可回旧站溯源，**不是** Forge id |
| Forge（`sp-mod.com/mod/<id>`） | **0** | 本包没有 Forge 编号 |
| Nexus | 0 | — |
| 仅 URL（GitHub release） | 1 | `[11]物品感应-AmandsSense` |
| 无 url / 无编号 | 62 | 自制、合并文件或原始自用条目 |

## 三、分组明细（完整 114 条，按 MO2 文件顺序）

> 分组语义：包作者把分隔符写在区块**末尾**（页脚式）——某条 mod 属于它**下方**那条 `_separator` 的区块，文件最后一行的分隔符就是全表最后一个区块的页脚。`scan-pack.mjs` 已按此口径切片，下列小节与明细表直接取自 `render-pack-tables.mjs --mode detail` 的现成输出（16 组、114 行，启用 114 / 禁用 0，不删行）。

#### [15]《现实化诺文斯克》数值、沉浸感再平衡（6 / 6 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| 《独享的塔科夫》更多的酒水MOD-CornerStore+HoodsEnergyDrinks沉浸感增强设置 | ✓ | d2024.5.16.0 | — | mod酒水吃喝效果全部改成缓慢增加 |
| 《独享的塔科夫》腰包小容器-WTT-PackNStrap沉浸感增强设置 | ✓ | d2024.5.3.0 | — | 增加更多可以放进保险箱的容器，并修正了臂包的售价。感谢群友：渗透者之心的定价计算 |
| 《独享的塔科夫》AI生成-botplacementsystem沉浸感增强设置 | ✓ | d2025.4.27.0 | — |  |
| 《独享的塔科夫》客户端MOD-F12设置cfg所在 | ✓ | — | — | 对游戏界面有自己习惯请关掉此项。否则进入游戏将只有左下角蹲姿体力槽。难度选项已包含此项目，启用难度会被部分覆盖。 |
| 《独享的塔科夫》SAIN增强版预设-Live in Norvinsk AI preset | ✓ | — | — | 整合包专属SAIN预设-持续更新 |
| 《独享的塔科夫》Samuel's Tweaks | ✓ | — | — | 自制MOD，护甲弹挂不冲突/可以掠夺臂章近战武器/三格弹匣变两格 |

#### [14]汉化模组（1 / 1 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [14]客户端ModF12通用翻译器及菜单汉化文本 | ✓ | d2024.12.15.0 | — | 这个mod会将所有F12的选项全部翻译且不影响原mod文件。 |

#### [13]MOD工具（1 / 1 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [13]存档编辑器-SPT-AKI Profile Editor | ✓ | d2025.5.3.0 | Hub #184 |  |

#### [12]修复、优化（7 / 7 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [12]商人列表滚动-TraderScrolling | ✓ | d2025.5.3.0 | Hub #1508 | 提供商人界面的滚动选择功能 |
| [12]任务列表修复-TaskListFixes | ✓ | d2025.3.31.0 | — |  |
| [12]尸体可删除-SPTCorpseCleaner | ✓ | d2025.4.2.0 | — |  |
| [12]内存清理-RamCleanerInterval | ✓ | d2025.3.22.0 | Hub #1827 |  |
| [12]骑士面具修复-FacetheKnight-MaskFix | ✓ | d2025.10.11.0 | — |  |
| [12]防卡手-HandsAreNotBusy | ✓ | d2025.3.16.0 | Hub #1810 | 防卡手快捷键：SHIFT+L，手卡了就按一下 |
| [12]白色方块bug修复-WhiteBoxFix | ✓ | d2025.4.22.0 | — |  |

#### [11]作弊MOD（3 / 3 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [11]扫敌雷达-RadarStandalone | ✓ | d2025.9.13.0 | Hub #1522 | 挂！光环雷达，可以标识周围120米内的AI分布。 |
| [11]我就是爹作弊工具-DadGamerMode | ✓ | d2025.3.17.0 | Hub #926 | 其实就是无敌mod，也提供类COD呼吸回血，感谢Volcano的汉化工作及使用授权：https://sns.od |
| [11]物品感应-AmandsSense | ✓ | d2025.5.4.0 | github.com | 附近可拾取物扫描，使用方法：游戏内双击F。感谢Volcano的汉化工作及使用授权：https://sns.odd |

#### [10]物品高效整理（告别华容道）（10 / 10 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [10]显示价格之快速出售-showmethemoney-quick-sell（Q+左Q+右） | ✓ | d2025.10.6.0 | — |  |
| [10]显示价格-showmethemoney | ✓ | d2025.10.10.0 | — |  |
| [10]物品容器间快速移动-QuickMoveToContainer | ✓ | d2025.3.16.0 | Hub #1859 | 打开容器后，按Ctrl+点击物品即可自动将物品放到已打开的对应容器内 |
| [10]自动存放-AutoDeposit - 已AI优化 | ✓ | d2025.3.16.0 | — |  |
| [10]物品自动分类功能-INVENTORY ORGANIZING FEATURES | ✓ | d2025.5.4.0 | Hub #2122 | 使用特定的标签来实现自动物品分类和容器锁定位置。 |
| [10]从武器架装备武器-EquipFromWeaponRack | ✓ | d2025.3.16.0 | Hub #1567 | 顾名思义 |
| [10]拾取时快速放入容器-ContainerQuickLoot - 已AI优化 | ✓ | d2025.4.12.0 | — |  |
| [10]小火山的仓库搜索框修复和扩展-SearchBarExtended | ✓ | d2026.1.3.0 | — |  |
| [10]仓库搜索框-StashSearch | ✓ | d2025.3.22.0 | Hub #1769 | 在仓库上增加一个搜索栏方便搜索物品，感谢Volcano的汉化工作及使用授权：https://sns.oddba. |
| [10]UI大修-UIFixes | ✓ | d2025.10.6.0 | Hub #1860 | 对NJT的垃圾UI操作进行大量改进，让体验更好，感谢Volcano的汉化工作及使用授权：https://sns. |

#### [9]全局功能大修（8 / 8 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [9]工具提示中显示售价-LootValue | ✓ | d2026.1.15.0 | — |  |
| [9]项目上下文菜单扩展-ItemContextMenuExt | ✓ | d2025.5.8.0 | Hub #1283 |  |
| [9]内置动态地图-DynamicMaps | ✓ | d2025.4.19.0 | Hub #1981 | 战局内按M，或查看地图可显示。 |
| [9]物品打勾标记提供更多信息-MoreCheckmarks | ✓ | d2025.3.16.0 | Hub #1159 | 顾名思义，感谢Volcano的汉化工作及使用授权：https://sns.oddba.cn/139847.htm |
| [9]藏身处不用打勾-nofirhideout | ✓ | d2025.10.21.0 | — |  |
| [9]腰带槽-BeltSlot | ✓ | d2025.7.12.0 | — |  |
| [9]动态物品重量-DynamicItemWeights | ✓ | d2025.10.9.0 | — |  |
| [9]小火山的全界面中文字体切换 | ✓ | d2026.8.11.0 | — |  |

#### [8]战局内功能大修（12 / 12 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [8]可拾取穿墙物品-ReachExtender | ✓ | d2025.5.16.0 | Hub #1758 |  |
| [8]boss刷新通知-BossNotifier | ✓ | d2025.5.16.0 | Hub #1737 |  |
| [8]任务跟踪显示-QuestTracker | ✓ | d2025.3.17.0 | Hub #1574 | 感谢Volcano的汉化工作及使用授权：https://sns.oddba.cn/139847.html |
| [8]声音可视化-accessibilityindicators | ✓ | d2025.4.7.0 | — | 顾名思义，感谢Volcano的汉化工作及使用授权：https://sns.oddba.cn/139847.htm |
| [8]增加拾取距离-LootRadius | ✓ | d2025.3.31.0 | Hub #1870 |  |
| [8]直接搜索已打开容器-SearchOpenContainers | ✓ | d2025.3.16.0 | Hub #1265 | 可以直接搜索打开的文件柜等，不用关了再开。 |
| [8]直接使用未捡物品-UseLooseLoot | ✓ | d2025.3.16.0 | Hub #1264 | 对于未拾取的吃喝药品，可以直接使用，而不用先捡进包里 |
| [8]身上任何物品均可绑定快捷键-UseItemsFromAnywhere | ✓ | d2025.3.16.0 | Hub #1416 | 顾名思义 |
| [8]Boss身上有lega徽章-bosseshavelegamedals | ✓ | d2025.5.4.0 | — |  |
| [8]好莱坞投掷武器视觉辅助-VisualAssist | ✓ | d2025.8.24.0 | — |  |
| [8]火山家的实时字幕-Volcano-subtitle | ✓ | d2026.8.11.0 | — |  |
| [8]更多界面信息-GamePanelHUD | ✓ | d2025.3.23.0 | Hub #652 | 多功能用户界面 |

#### [7]装备、物品功能大修（12 / 12 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [7]更好的枪匠界面-Tradermodding客户端文件 | ✓ | d2025.3.22.0 | Hub #1795 |  |
| [7]更好的枪匠界面-Tradermodding服务器文件 | ✓ | d2025.3.22.0 | Hub #1795 |  |
| [7]真实的热成像-realisticThermalScopes | ✓ | d2025.5.14.0 | — |  |
| [7]更亮的镭射线-brightlasers | ✓ | d2025.3.16.0 | Hub #1880 |  |
| [7]反射镜视野重做-ReflexSightsRework客户端文件 | ✓ | d2025.4.22.0 | Hub #1753 | 反射镜红点大修，不挡视野 |
| [7]反射镜视野重做-ReflexSightsRework服务器文件 | ✓ | d2025.4.22.0 | Hub #1753 | 反射镜红点大修，不挡视野 |
| [7]平衡的夜视装备视效-BRNVG客户端文件 | ✓ | d2025.8.21.0 | Hub #1303 | 更真实的夜视装备视野效果 |
| [7]平衡的夜视装备视效-BRNVG服务器文件 | ✓ | d2025.8.21.0 | Hub #1303 | 更真实的夜视装备视野效果 |
| [7]更好的机瞄-BetterRearSights | ✓ | d2025.4.30.0 | — |  |
| [7]弹匣优化管理系统-MagazineManagementSystem | ✓ | d2025.3.22.0 | — |  |
| [7]面具修复-mask-fix | ✓ | d2025.6.8.0 | — | 解除头部装备的冲突 |
| [7]MOD武器配件自动兼容框架-autocompatframework-已AI优化 | ✓ | d2025.9.24.0 | — |  |

#### [6]AI行为（9 / 9 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [6]分离敌意-SeparateHostility | ✓ | d2026.2.24.0 | — |  |
| [6]AI锁头重定向-Headshot Damage Redirection | ✓ | d2025.6.2.0 | Hub #1809 | 防锁头MOD，这个mod的作用是将原本锁头带来的伤害重分配到其他身体部位，减少一枪喷子白头眼情况发生 |
| [6]AI视线优化-thuynguyentrungdang版That.s.Lit - 已AI优化 | ✓ | d2025.9.30.0 | — |  |
| [6]AI自动搜索物品-LootingBots - 已AI优化 | ✓ | d2025.4.15.0 | Hub #1096 | AI现在会搜索捡拾他看见的所有东西，包括你骗保的装备。感谢Volcano的汉化工作及使用授权：https://s |
| [6]AI增强-SAIN-Moew-v4.4.0增强版 | ✓ | d2025.8.6.0 | Hub #1062 | 增强的AI |
| [6]Acid的AI装备管理系统-progressivebotsystem - 已AI优化 | ✓ | d2026.1.17.0 | — |  |
| [6]Acid的AI生成系统-botplacementsystem - 已AI优化 | ✓ | d2025.4.27.0 | — | 管理AI的波次生成 |
| [6]AI路径扩展-Waypoints - 已AI优化 | ✓ | d2025.6.20.0 | Hub #1119 | 更多的路径点，运行AI走得更远，更没有限制范围。 |
| [6]AI拟人MOD的前置框架-BigBrain - 已AI优化 | ✓ | d2025.3.16.0 | Hub #1219 | 整套BigBrain系AI扩展的必备前置MOD，依赖它的：SAIN\nSWAG\nWaypoints\nLoot |

#### [5]整合包核心MOD-基于原玩法的大修（开关此类需重开档）（1 / 1 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [5]《独享的塔科夫》5个开局角色-CustomProfiles | ✓ | d2025.5.3.0 | — |  |

#### [4]自定义商人、任务（13 / 13 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [4]新任务线-迎新礼物任务-EFCL-WelcomeGifts | ✓ | d2025.9.19.0 | Hub #993 | 用任务的方式将礼品分派给了不同的商人 |
| [4]新商人-AES | ✓ | d2025.10.10.0 | — |  |
| [4]新商人-VolkovTrader | ✓ | d2025.3.25.0 | — | 狗牌换购商人 |
| [4]新商人-Epakim1-1 | ✓ | d2025.5.18.0 | — |  |
| [4]新商人-不明生还者-UnknownSurvivor | ✓ | d2025.4.27.0 | — |  |
| [4]新商人-可购买自己创建的预设枪械-Hephaestus | ✓ | d2025.6.16.0 | — |  |
| [4]新商人-荷官-Croupier (random loadouts + flea quicksell) | ✓ | d2025.3.22.0 | — |  |
| [4]新商人-藏身处物资商人-acidphantasm-harryhideout | ✓ | d2025.3.21.0 | Hub #1817 |  |
| [4]新商人-毒蝎-acidphantasm-scorpion | ✓ | d2025.3.31.0 | Hub #1869 | 一大堆任务，群友✘✘✘✘对任务本文进行了翻译 |
| [4]新商人-Artem | ✓ | d2025.6.7.0 | — |  |
| [4]新商人-油漆匠-Painter | ✓ | d2025.7.9.0 | Hub #1412 | 需要MagTape和BlackCore作为前置，已翻译 |
| [4]竞技场商人的箱子-Adam's Boxes at Ref (BARF) | ✓ | d2025.5.9.0 | — |  |
| [4]新商人-竞技场商人-ref-sptfriendly-quests | ✓ | d2025.3.16.0 | Hub #2108 | 已翻译 |

#### [3.1]新配件（6 / 6 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [3]新配件-ConsortiumOfThings | ✓ | d2025.7.8.0 | — | 汉化进行中 |
| [3]新配件-Eco-Attachment Emporium | ✓ | d2025.9.21.0 | — | 汉化进行中 |
| [3]新配件-Echoes.of.Tarkov.-.Requisitions | ✓ | d2025.7.12.0 | — | 汉化进行中 |
| [3]新配件-EpicRangeTime-Weapons | ✓ | d2025.6.26.0 | — | 内包含商人Badger |
| [3]新配件-BlackCore | ✓ | d2025.4.23.0 | Hub #1345 | 新商人-油漆匠-CivicPainter的前置，已有现实主义兼容 |
| [3]新配件-MagTape | ✓ | d2025.4.19.0 | Hub #1404 | 新商人-油漆匠-CivicPainter的前置，已有现实主义兼容，已汉化 |

#### [3]新武器（9 / 9 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [3]新武器-AKResonant | ✓ | d2025.6.7.0 | — |  |
| [3]新武器-国产武器-1SD-QBZ191 | ✓ | d2025.12.6.0 | — |  |
| [3].50BMG重制-Epics 50 BMG Remaster-Expansion | ✓ | d2025.7.22.0 | — |  |
| [3]新武器-AK50 | ✓ | d2025.7.16.0 | — |  |
| [3]新武器-SIG_MCX_VIRTUS | ✓ | d2025.12.27.0 | — |  |
| [3]新武器功能-其他武器可使用超速射 - BabaYaga x Hyperburst客户端文件 | ✓ | d2025.9.15.0 | — | [3]新武器-WTT武器库-WTT - Armory是前置 |
| [3]新武器功能-其他武器可使用超速射 - BabaYaga x Hyperburst服务器文件 | ✓ | d2025.9.15.0 | — | [3]新武器-WTT武器库-WTT - Armory是前置 |
| [3]新武器-WTT武器库-WTT - Armory客户端 | ✓ | d2025.8.22.0 | — |  |
| [3]新武器-WTT武器库-WTT - Armory服务器 | ✓ | d2025.12.21.0 | — |  |

#### [2]新内容（10 / 10 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [2]新物品-竞技场赛季奖励-SPT Battlepass | ✓ | d2025.8.21.0 | — |  |
| [2]新装备、衣服-TacticalGearComponent | ✓ | d2025.7.9.0 | Hub #1555 | Civic系列服饰装备，自带现实主义兼容，已汉化 |
| [2]腰包-WTT-PackNStrap客户端文件 | ✓ | d2025.9.30.0 | Hub #1790 | 多款可放在臂章位置的腰包，属于第二个安全箱，已有现实主义兼容 |
| [2]腰包-WTT-PackNStrap服务器端文件 | ✓ | d2025.9.30.0 | Hub #1790 | 多款可放在臂章位置的腰包，属于第二个安全箱，已有现实主义兼容 |
| [2]新酒水-HoodsEnergyDrinks | ✓ | d2025.5.21.0 | — |  |
| [2]新酒水-WTT - CornerStore | ✓ | d2025.9.25.0 | Hub #2334 | 在《现实化诺文斯克》v0.9.0版药品酒水沉浸感增强设置有做针对性调整 |
| [2]新物品-地图容器-SecureMapbookMod | ✓ | d2025.3.26.0 | — |  |
| [2]新物品-镀金钥匙容器-GildedKeyStorage客户端文件 | ✓ | d2025.3.22.0 | Hub #1166 | 方便存放钥匙和钥匙卡，可帮助辨别捡到的钥匙是否重复 |
| [2]新物品-镀金钥匙容器-GildedKeyStorage服务器文件 | ✓ | d2025.3.22.0 | Hub #1166 | 方便存放钥匙和钥匙卡，可帮助辨别捡到的钥匙是否重复 |
| [2]新物品-藏身处猫咪-hideoutcat | ✓ | d2025.3.18.0 | — |  |

#### [1]前置MOD（不要关）（6 / 6 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [1]前置框架-自定义任务加载器-Virtual's Custom Quest Loader客户端文件 | ✓ | d2025.3.16.0 | Hub #885 |  |
| [1]前置框架-自定义任务加载器-Virtual's Custom Quest Loader服务器文件 | ✓ | d2025.3.16.0 | Hub #885 |  |
| [1]前置框架-自定义交互-CustomInteractions | ✓ | d2025.5.8.0 | Hub #1278 | [8]人物技能扩展和撬锁-SkillsExtended和[9]项目上下文菜单扩展-ItemContextMenu |
| [1]前置框架-KmyTarkovApi | ✓ | d2025.3.23.0 | Hub #1215 | GamepanelHUD的前置 |
| [1]前置-《独享的塔科夫》修改本体及MOD排序文件和整合包说明文档 | ✓ | — | — | 利用Mo2的文件覆盖机制实现对本体文件的修改 |
| [1]前置-《独享的塔科夫》好奇猫自用游戏设置及按键 | ✓ | — | — | 对游戏界面有自己习惯请关掉此项。否则进入游戏将只有左下角蹲姿体力槽 |

## 四、排序规则与要点

### 4.1 优先级语义：越靠后越高

- MO2 `modlist.txt` 自上而下 = 优先级由低到高：**靠后的条目覆盖靠前的条目**（文件冲突以高优先级为准）。
- 本包的排法是「数字前缀降序 + 前置沉底」：文件顶部是 `[15]` 区块（最先被覆盖），往下依次 `[14] … [3.1] [3] [2]`，最后是 `[1]前置MOD（不要关）` 区块（**最高优先级**），再以该分隔符收尾。
- 前置沉底是刻意的：`[1]` 区块里既有框架（Virtual's Custom Quest Loader、CustomInteractions、KmyTarkovApi），也有**本体覆盖层**（Assembly-CSharp.dll / core.json / settings.json / order.json）。让它们最后生效，才能保证所有功能 mod 的文件不把这些关键文件盖掉。
- 因此「拖一下顺序」不是审美动作：任何跨组移动都直接改变覆盖关系。

### 4.2 分隔符归属：页脚式（scan 已按此口径切片）

- **判定规则**：每个 `_separator` 是它**上方连续条目**的组名（页脚写法）；文件最后一行的 `[1]前置MOD（不要关）_separator` 就是全表最后一个区块（6 条前置）的页脚，而不是下一块的开头。
- **量化证据**：108 条带 `[N]` 数字前缀的 mod 中，**102 条**其前缀数字等于所属分隔符的数字（94.4%）；余下 6 条全部是 `[3]新配件-*` 落在 `[3.1]新配件` 区块（作者把配件拆成 `[3]`/`[3.1]` 两级编号）。按旧口径（条目归给其上方分隔符）切片时该匹配率为 0/108，因此这一口径可直接用前缀数字自检。
- **口径已落地**：`scripts/packs/scan-pack.mjs` 的 `parseModlist` 在遇到 `_separator` 时结算上方 pending 块（文件末尾残余归「(未分组)」，本包无残余，16 组恰好覆盖 114 条）；`render-pack-tables.mjs --mode summary|detail` 的现成输出即本页 §2 / §3 的表。
- **复现**：见 §7 的骨架片段，可与 `scan.json` 的 `profiles[0].groups` 交叉核对。

### 4.3 服务端加载序：order.json（107 条 / 57 个实际目录）

| 项 | 值 | 依据 |
|---|---|---|
| 位置（覆盖层内） | `[1]前置-...` mod 的 `user\mods\order.json`，3,489 B | 实测 |
| 条目数 | **107 条 / 106 个唯一名**（`Hephaestus` 出现两次） | 实测 |
| 首 / 末 | 首 `friendlyPMC`；末尾依次 `Tarkov Weather System`、`zz_guiltyman-addmissingquestweaponrequirements`、`zzzzz-simple-translator`（`zz*` 前缀用于压后） | 实测 |
| 实际服务端目录 | `mods/*/user/mods` 合并后 **57 个唯一目录名**，全部被 order.json 收录 | 实测 |
| 无对应目录的条目 | **49 条**（如 `revival-mod`、`SkillsExtended`、`SPT-Realism`、`gunsmith`、`RaidOverhaul` 等） | 实测 |
| 游戏根对应文件 | 本体 `user\mods\order.json` 为 `{"order": []}`（19 B），`user\mods` 下 0 个目录 | 实测 |

- 读法：服务端 mod **全部经 MO2 覆盖层**提供（游戏根 user/mods 为空），因此 57 个目录才是真正被加载的服务端 mod；49 条多余条目属于「继承超集」，加载器找不到目录时按常见行为跳过（**日志侧待确认**，本次未取 SPT 服务端日志）。
- 常见多份打包：`Virtual's Custom Quest Loader` 被至少 9 个 mod 各自打包一份（`[3]WTT武器库`、`[4]` 多个商人、`[4]迎新礼物任务` 等），MO2 的 VFS 会把同名目录合并 —— 版本一致性靠高优先级副本决定，是本包最容易出隐性问题的位置之一。

### 4.4 开关代价与禁用项

| 分组 | 开关代价 | 依据 |
|---|---|---|
| `[5]整合包核心MOD-基于原玩法的大修` | **需重开档**（组只有 1 项：`CustomProfiles` 5 个开局角色） | 分隔符名自带提示 |
| `[1]前置MOD（不要关）` | **请不要关**（含本体覆盖层与全部框架） | 分隔符名自带提示 |
| `《独享的塔科夫》客户端MOD-F12设置cfg所在` | 关掉后「只有左下角蹲姿体力槽」；难度选项会部分覆盖此项 | meta.ini comments |
| `[6]AI行为` / `[15]` 的调参层 | 关 [15] 的「沉浸感增强设置」只回退数值、不丢 AI 本体 | 组内三层结构（§5.3） |
| 禁用项 | **无**：114 条 mod 全部启用，0 禁用；16 条 `-` 全是分隔符 | scan.json |

## 五、本体修改 / 覆盖层 / 特殊资产

### 5.1 覆盖层清单（`[1]前置-《独享的塔科夫》修改本体及MOD排序文件和整合包说明文档`）

| 文件 | 大小 | 作用 |
|---|---|---|
| `EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll` | **15,243,264 B** | 改写后的本体程序集（具体改动**无清单，待确认**） |
| `SPT_Data\Server\configs\core.json` | 8,175 B | 项目名、存档间隔、fixes 开关 |
| `SPT_Data\Server\database\settings.json` | 1,704 B | 服务端内存管理 |
| `user\mods\order.json` | 3,489 B | 服务端加载序（107 条） |
| `meta.ini` | — | comments：「利用Mo2的文件覆盖机制实现对本体文件的修改」 |

- 生效方式：MO2 的文件覆盖（VFS/USVFS 投射）把这些文件覆盖到 `gamePath`，从而「不改真实游戏目录」地改本体；游戏根目录另存在 `sptvfsbridge.bat`、`winhttp.dll`（doorstop），是 SPT + MO2 组合的常规布局。
- 该 mod 目录内**总共只有 5 个文件**（实测），没有任何说明文本——目录名里的「整合包说明文档」文件名不副实。

### 5.2 覆盖层实际改了什么（2026-11-01 实测差异）

| 文件 | 本体（gamePath） | 覆盖层 | 差异性质 |
|---|---|---|---|
| Assembly-CSharp.dll | 15,243,264 B，SHA256 前 16 位 `12d5fd1728c58a9c` | **同大小、同哈希** | 两者**字节一致**：该 DLL 已同时存在于本体位置，无法区分「VFS 投射」与「安装时已物理替换」；改动的 IL/常量**待确认** |
| core.json | projectName=`SPT`；profileSaveIntervalSeconds=15；fixes 四项仅 `fixShotgunDispersion` 为 true | projectName=`Exclusive-Tarkov 1.2 AI-Enhanced Based on SPT`；profileSaveIntervalSeconds=**5**；fixes **四项全 true**（removeModItemsFromProfile / removeInvalidTradersFromProfile / fixProfileBreakingInventoryItemIssues 由 false 改 true） | 真改动（另有 commit / buildTime 不同） |
| settings.json | MemoryManagementSettings：HeapPreAllocationEnabled=false、OverrideRamCleanerSettings=false、RamCleanerEnabled=false | 同三项全部 **true** | 只改 3 个内存开关，与 [12]内存清理-RamCleanerInterval 呼应 |
| order.json | `{"order": []}`（19 B） | 107 条（3,489 B） | 真改动，且是唯一「加载序」载体 |

> 结论：覆盖层对**服务端配置**是真改动（存档间隔 15→5 秒、fixes 全开、内存策略全开、加载序注入）；对**客户端 DLL** 则是「本体位置已是同一份文件」，所以排查客户端问题时不要假设「关掉覆盖层就回到原版」。

### 5.3 bot 补丁（AI 三层结构）

- **本体层（[6]AI行为，9 条）**：`SAIN-Moew-v4.4.0增强版`、前置框架 `BigBrain`、路径 `Waypoints`、搜刮 `LootingBots`、波次生成 `botplacementsystem`、装备管理 `progressivebotsystem`、视线 `That.s.Lit`、敌意分离 `SeparateHostility`、锁头伤害重定向 `Headshot Damage Redirection`。
- **调参层（[15] 组）**：3 条「沉浸感增强设置」（`更多酒水MOD`、`腰包小容器`、`AI生成-botplacementsystem`）+ `SAIN增强版预设-Live in Norvinsk AI preset`。它们不装插件、只改数值/预设，覆盖上面本体层的参数。
- **下放标记**：9 条 mod 名带「- 已AI优化」，其中本组 6 条（BigBrain / Waypoints / LootingBots / botplacementsystem / progressivebotsystem / That.s.Lit），另 [10] `ContainerQuickLoot`、[10] `AutoDeposit`、[7] `autocompatframework`。这 9 条即 v1.0 / v1.2 两次下放 Life AI 优化的可见痕迹。
- 依赖关系（meta.ini 记载）：BigBrain 是 SAIN / SWAG 等 BigBrain 系扩展的必备前置；KmyTarkovApi 是 GamePanelHUD 的前置；CustomInteractions 是 SkillsExtended / ItemContextMenuExt 一类的前置；MagTape + BlackCore 是油漆匠（Painter）的前置。

### 5.4 回滚点与外部资产

| 资产 | 位置 | 状态 |
|---|---|---|
| 排序回滚点 | `profiles/单机运行整合包/modlist.txt.请恢复这个单机排序260903` | **存在**（实测），用户命名的整份排序备份 |
| profile 其它文件 | `archives.txt` / `initweaks.ini` / `lockedorder.txt`（空表头）/ `settings.ini` | 存在 |
| overwrite 目录 | `BepInEx` / `Logs` / `SPT_Data` / `user` | 四个子目录 mtime 均为 2026-09-02（实测）；文档记 2026-09-03 跑档。属运行残留，非 mod 本体 |
| downloads / webcache | 空（0 文件） | 无待装队列 |
| 游戏根 | `EscapeFromTarkov.exe` / `SPT.Server.exe` / `SPT.Launcher.exe` / `sptvfsbridge.bat` / `winhttp.dll` / `SPT_Data` / `BepInEx` | 3.11.4 本体 |
| 原版备份 | **未发现**任何 `Assembly-CSharp.dll` / `core.json` 原版副本 | 回滚到原版只能重装本体（风险点） |

### 5.5 自制 / 专属资产

- `《独享的塔科夫》Samuel's Tweaks`：自制 mod，结构为 `BepInEx\plugins\SamMeow.PerformanceTweaks.dll` + `user\mods\Samuel'sAIO`；meta.ini 记功能「护甲弹挂不冲突 / 可以掠夺臂章近战武器 / 三格弹匣变两格」。v1.2 新增的 `SamMeow.PerformanceTweaks` 是**帧数优化**（针对本体缺陷），详见 [PerformanceTweaks 试点记录](../operations/pilot-experience-performancetweaks.md)。
- `《独享的塔科夫》好奇猫自用游戏设置及按键`：作者自用界面/按键覆盖，含蹲姿体力槽等 HUD 布局；可关。
- `[5]《独享的塔科夫》5个开局角色-CustomProfiles`（服务端 `EFCL-CustomProfiles`）：5 个开局角色，属「开关需重开档」的核心组。
- 3 条「沉浸感增强设置」调参 mod 与 `SAIN增强版预设`：包作者自有数值层，非上游发布物。

## 六、已知问题与维护要点

| # | 问题 | 影响 | 依据 / 线索 |
|---|---|---|---|
| 1 | **跳蚤市场搜索某些物品时没有交易条目** | 不崩服，仅是搜索结果为空 | v1.2 已知问题（文档） |
| 2 | v0.3 遗留待查：钱包交易爆红、切刀再切枪卡手、商人处购买无限加载、进藏身处报错 | 多数无法复现；无复现步骤 | 文档「v0.3 遗留待查」 |
| 3 | EFT 客户端版本两处记录不一致（`0.16.0.35392` vs 日志目录 `0.16.1.3.35392`） | 排障时对不上号 | core.json / 文档，**待确认** |
| 4 | DLL 计数口径不一致：实测 `mods/` 树内 86 个 `.dll`，文档记 89 | 只影响统计，不影响运行 | 实测 5,885 文件 / 16.86 GB（与文档一致） |
| 5 | `order.json` 49 条无对应目录（继承超集） | 通常无感；若某条目在旧包有副作用需逐条核 | 实测，加载器行为**待确认** |
| 6 | `Virtual's Custom Quest Loader` 等多份同名目录靠 VFS 合并 | 版本冲突时症状隐蔽（任务/商人异常） | 实测 `mods/*/user/mods` |
| 7 | 「130 个 mods 目录」易被误读成 130 个 mod | 统计/交接时高估 16 条 | 实测：16 个 `_separator` 占位目录只有 meta.ini |
| 8 | 覆盖层无原版备份，且客户端 DLL 与本体同位文件字节相同 | 想「关掉覆盖层回原版」做不到 | 实测哈希 |
| 9 | 包内无作者原文说明（`docs/README.md` 是 2026-10-14 的建档档案卡） | 更新史只能靠文档转述 + 外部日志 | 目录枚举 / 文档自述 |

**维护要点**

- 改排序请在 MO2 界面内做，改完用回滚点对照；`modlist.txt` 手改会破坏分隔符页脚关系。
- 动 `[5]整合包核心MOD` 前先备份角色存档（需重开档）；`[1]前置` 组任何一项都不建议关。
- 服务端 mod 的真实清单看 `mods/*/user/mods`（57 个），不要只看 `order.json`（107 条）。
- 升级 Life 线时优先比对：`[6]AI行为` 的 9 条、`order.json` 的 `zz*` 尾部、以及 `[15]` 调参层——这三处是「下放」最常改的地方。

## 七、证据与复现

**扫描与渲染（只读，均可重跑）**

```powershell
# 1) 采集快照（数字唯一权威）
node scripts/packs/scan-pack.mjs --pack-root "E:/Game/EFT_Offline/Exclusive_Tarkov" --slug exclusive-tarkov --out .artifacts/packs/exclusive-tarkov/scan.json

# 2) 分组总览 / 分组明细
node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/exclusive-tarkov/scan.json --mode summary
node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/exclusive-tarkov/scan.json --mode detail
```

> 分组口径：`--mode summary` / `--mode detail` 的现成输出就是 §2 / §3 的表（页脚式切片，16 组、114 条、启用 114 / 禁用 0），可直接复制。下面留一段独立核对骨架，用来验证 `scan.json` 的 `profiles[0].groups` 切片正确。

分组切片的可复现骨架（与 scan.json 的 mod 表 join 后即可得到 §3 的 16 个小节）：

```js
const markdown = readFileSync("profiles/单机运行整合包/modlist.txt", "utf8").replace(/^\uFEFF/, "");
const entries = markdown.split(/\r?\n/).filter(l => l && !l.startsWith("#"))
  .map(l => ({ enabled: l[0] === "+", name: l.slice(1) }));
const groups = []; let pending = [];                       // 页脚语义：分隔符结算其上方的条目
for (const e of entries) {
  if (e.name.endsWith("_separator")) {
    groups.push({ name: e.name.replace(/_separator$/, ""), mods: pending }); pending = [];
  } else pending.push(e);                                  // 用 scan.json 的 mods 表补 meta
}
// groups: 16 组 / 114 条，[15] 在前、[1]前置MOD（不要关）在最后
```

**核对要点（可复算的小片）**

| 断言 | 核对方式 | 本次结果 |
|---|---|---|
| 114 条 mod 全启用 | 数 `+` 行与 `_separator` 行 | 114 / 0 |
| 分隔符 16 个 | 数 `_separator` 结尾条目 | 16（15 个 `-`、1 个 `+`） |
| 130 个目录 = 114 + 16 | 枚举 `mods/` 子目录 | 114 非 separator + 16 separator |
| 分组为页脚式 | 按行序把分隔符上方的 pending 块结算成一组，与 `scan.json` 的 groups 比对 | 16 组 / 114 条；`[15]` 6 条（启用 6）、`[1]前置` 6 条（启用 6，位于文件末尾） |
| DLL 与本体一致 | 对两处 `Assembly-CSharp.dll` 取 SHA256 | 均为 `12d5fd1728c58a9c…` |
| order.json 超集 | 把 `order.json` 名单与 `mods/*/user/mods` 目录名求差 | 107 条 / 57 目录 / 49 条无目录 |

**实包路径（仅溯源用，本页结论不依赖它们存在）**

- 客户端排序：`E:\Game\EFT_Offline\Exclusive_Tarkov\profiles\单机运行整合包\modlist.txt`
- 排序回滚点：`...\profiles\单机运行整合包\modlist.txt.请恢复这个单机排序260903`
- 服务端加载序：`...\mods\[1]前置-《独享的塔科夫》修改本体及MOD排序文件和整合包说明文档\user\mods\order.json`
- 版本字段：`...\[1]前置-...\SPT_Data\Server\configs\core.json`
- 覆盖层说明：`...\[1]前置-...\meta.ini`
- 本体 DLL：`...\[1]前置-...\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`
- MO2 实例注册：`C:\Users\Winde\AppData\Local\ModOrganizer\SPT3.11.4《独享的塔科夫》\ModOrganizer.ini`

**缺口清单（明确没验证的事）**

- `Assembly-CSharp.dll` 的具体改写点（IL/常量/方法）——无改动清单，本次只确认大小与哈希。
- 实际 EFT 客户端版本以 core.json 还是日志目录为准。
- SPT 加载器对 49 条「无目录」order 条目的实际处理（是否告警/跳过）。
- `[5]` 组关闭后对现有存档的实际破坏程度（文档只给「需重开档」的结论）。
- MO2 版本世代（ini 写 2.5.3，文档写 2.5.3beta2）——是否同一构建待确认。

**相关页**

- 包矩阵与共有底座：[packs/README.md](README.md)
- 血统主线：[life-in-norvinsk.md](life-in-norvinsk.md)
- 4.1.6 重建的同源包：[norvinsk-forever.md](norvinsk-forever.md)
- 3.11 性能与排障：[3114-eft016-perf-hotspots](../operations/3114-eft016-perf-hotspots.md)、[MO2 SPT 版本误判](../operations/mo2-spt-version-misdetection.md)
