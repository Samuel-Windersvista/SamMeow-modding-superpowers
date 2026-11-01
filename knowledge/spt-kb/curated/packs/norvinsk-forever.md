---
version: [4.1]
domain: both
topic: pack-profile
source: curated
title: "《永远的诺文斯克》Norvinsk_Forever 包档案（SPT 4.1.6 · 主线）"
keywords: [Norvinsk_Forever, 永远的诺文斯克, SPT 4.1.6, 整合包档案, MO2, modlist, Forge 编号, 回滚点, 版本线, 覆盖层, Life_in_Norvinsk, 包构成]
summary: "以《生活在诺文斯克》v0.3.2（3.11.4，197 条目）为蓝本、在 SPT 4.1.6 上重建的主线整合包；版本线：3.11.4 参照 → v0.1.0 首建 72 → B8 批次 → 10-12/13 扩充与更新批 → 10-16/25 覆盖层迭代。2026-11-01 快照：mods 目录 129（114 条 mod + 15 分隔符），启用 100 / 禁用 14，93 条有 Forge 编号，MO2 2.5.2，profile Default，回滚点 6 处。"
---

# 《永远的诺文斯克》Norvinsk_Forever 包档案（SPT 4.1.6 · 主线）

> 用途：本页是仓库内可离线查询的**包构成档案**——不依赖 `E:` 实包即可回答「这包有什么、按什么顺序排、哪些禁用了、怎么回滚」。实包路径只用于溯源；所有数字以 `.artifacts/packs/norvinsk-forever/scan.json` 为准。快照时间 **2026-11-01 01:13（+08:00）**（`capturedAt=2026-10-31T17:13:14Z`）；包内文档最后更新：dev-log 2026-10-27、README / 对照表 2026-10-14，**文档滞后于实包**的部分见 §六。

## 一、身份与环境

| 项 | 值 | 来源 |
|---|---|---|
| 包名 | 《永远的诺文斯克》Norvinsk_Forever（主线） | scan `slug=norvinsk-forever` |
| SPT 版本线 | **4.1.6**（mod 本体按上游 4.1.x 重建，见 §一.2） | README / dev-log |
| 游戏根 `gamePath` | `E:\Game\EFT_Offline\SPT_41x`（4.1.x 布局，含 `SPT_Runtime\`） | scan `mo2.gamePath`（ModOrganizer.ini 中为双反斜杠转义） |
| MO2 `gameName` | `SPT4.1` | scan `mo2.gameName` |
| profile | `Default`（唯一 profile；modlist 129 行 = 114 mod + 15 分隔符） | scan `profiles[0]` |
| MO2 版本 | 2.5.2 | scan `mo2.version` |
| mods 目录数 | **129** = 114 条 mod + 15 个 `_separator` 目录 | scan `counts.modDirs` |
| 启用 / 禁用 | **100 / 14** | scan `profiles[0].counts` |
| meta 有 version/url/comments 的目录 | 109 | scan `counts.modDirsWithMeta` |
| Forge 编号覆盖 | **93 / 114** 条 mod（Hub 0、Nexus 0，见 §六 口径） | scan `counts.modsWithForgeId` |
| 包内文档 | `docs/` 6 份（README、dev-log、对照表、pending、porting-assessment、Vagabond 设置指南） | scan `docs` |
| 实包根 | `E:\Game\EFT_Offline\Norvinsk_Forever`（**仅溯源**） | scan `packRoot` |

### 一.1 版本线与回滚点

| 阶段 | 时间 | 内容 | 证据 |
|---|---|---|---|
| 3.11.4 参照 | — | `Life_in_Norvinsk_v0.3.2`（3.11.4 世代，197 条目）作为蓝本；3.11 世代 mod 本体无法直达 4.1.6，只能按上游 4.1.x 重建 | README §一/§二、对照表表头 |
| v0.1.0 首建 | 2026-10-06 | 69 项上游 mod（B1–B7 批次）+ 增量补装 3 项（BRNVG / IOF / HDR）= **72**；Level B 验证：服务端 39/39 已加载、客户端 56 插件 startup complete | dev-log 2026-10-06、对照表 §一/§四 |
| B8 批次 | 2026-10-11 | 4 装（Vagabond / TushonkaTerritories / ORBIT 2.0 / RaidsAreInfinite）+ 2 禁（LootingBots / SeparateHostility） | dev-log 2026-10-11、对照表 §五 |
| 10-12 扩充批 | 2026-10-12/13 | Softcore416 v0.1.0 部署 + 新装 26 项（上游 21 / 自定义层 3 / 汉化 2）+ 移除 brightlasers | dev-log 2026-10-12、对照表 §六 |
| 10-13 更新批 | 2026-10-13 | 就地更新 6 项（IOF / HDR / SkillMultiplier / SkillsExtended / UseItemsFromAnywhere / TushonkaTerritories），旧版隔离留档 | dev-log 2026-10-13、对照表 §七 |
| 10-14 追加 | 2026-10-14 | DoorOverhaul + BackpackGunSlot 装入；文档回写口径为 105 实体 / 120 行 | dev-log 2026-10-14、对照表 §十 |
| 覆盖层迭代（重建推定） | 2026-10-16 ~ 10-25 | 预设与 WTT 内容刷新（10-16/10-18）、LLM Trader Chat 落位（10-16）、BetterBepInEx（10-24）、DoorBreachOverhaul 换掉 DoorOverhaul（10-25） | 目录 mtime + dev-log 2026-10-25；逐项因由文档未全覆盖 → 待确认 |

回滚点（`.mo2-mcp/profile-backups/`，每份含 `modlist.txt` / `archives.txt` / `initweaks.ini` / `lockedorder.txt` / `settings.ini`）：

| 回滚点 | modlist 条目 | 时间（+08:00） | 对应节点 |
|---|---|---|---|
| `Default_pre-verify-0.1.0` | 69 | 2026-10-06 01:42 | 首建验证前 |
| `Default_pre-B8` | 87 | 2026-10-11 17:39 | B8 批次前 |
| `Default_pre-softcore416` | 96 | 2026-10-11 17:15（文件时间） | Softcore416 部署前；文档记为 10-12 |
| `Default_pre-llmtc-m1` | 126 | 2026-10-14 20:03 | LLM Trader Chat M1 前（文档未记，待确认） |
| `Default_pre-softcore416-v0.2.0` | 127 | 2026-10-16 00:11 | Softcore416 v0.2.0 前（文档未记，待确认） |
| `Default_pre-doorbreach-0.1.0-20261025` | 129 | 2026-10-25 14:18 | DoorBreachOverhaul 0.1.0 部署前 |

> 文档只登记前 3 处（README §五）；后 3 处为实包目录实列（2026-11-01 只读查看）。另外 `Default_pre-doorbreach-0.1.0-20261025` 部署后还有整目录备份 `.artifacts\doorbreach-ticket09-backup\2026-10-25-07-deployed\`（dev-log 2026-10-25）。

### 一.2 与《生活在诺文斯克》3.11.4 的参照关系

- **蓝本不是分支**：`Life_in_Norvinsk_v0.3.2`（3.11.4，197 条目）只提供玩法与内容覆盖的「目标清单」；4.1.6 的包体全部按上游 4.1.x 版本重装（dev-log 首建条），因此同名 mod 的版本、行为、可用性都可能不同。
- **交集**：NF 与 Life 的名称级交集 **59 组**；NF / Life / Exclusive 三包共有 **41 组**「公共底座」（判定口径见 [整合包矩阵](README.md) §二/§三）。
- **未复刻面**：3.11 专有内容（Realism、PTT+IEAPI、RaidOverhaul、Softcore 直译等）停在「未就绪清单」（72 项），部分由自建 mod 顶替（Softcore416 一次覆盖 5 项、DebugToolkit 覆盖 DadGamerMode）；评估结论见 [porting-candidate-assessment](../migration/porting-candidate-assessment.md)。
- **策略口径**：上游优先、自定义待定；每次变更留回滚点（README §一/§二）。

## 二、构成总览

| 分组（MO2 分隔符） | 条目 | 启用 | 禁用 |
|---|---|---|---|
| [15]《永远的诺文斯克》数值、沉浸感再平衡 | 7 | 6 | 1 |
| [14]汉化模组 | 2 | 2 | 0 |
| [13]MOD工具 | 6 | 4 | 2 |
| [12]修复、优化 | 4 | 4 | 0 |
| [11]破坏沉浸感MOD | 5 | 5 | 0 |
| [10]物品高效整理（告别华容道） | 8 | 8 | 0 |
| [9]全局功能大修 | 11 | 6 | 5 |
| [8]战局内功能大修 | 21 | 20 | 1 |
| [7]装备、物品功能大修 | 8 | 7 | 1 |
| [6]AI行为（吃帧数，帧数低可选择关这类，开丧尸模式请关闭整个大类） | 9 | 8 | 1 |
| [5]整合包核心MOD-基于原玩法的大修（开关此类需重开档） | 4 | 2 | 2 |
| [4]自定义商人、任务 | 8 | 8 | 0 |
| [3.1]新配件 | 6 | 6 | 0 |
| [2]新内容 | 8 | 8 | 0 |
| [1]前置MOD（不要关） | 7 | 6 | 1 |
| **合计** | **114** | **100** | **14** |

> 合计：114 条 mod 条目（启用 100 / 禁用 14）+ 15 个分隔符。
> 口径：`_separator` 是**区块页脚**——它标记其上方区块结束。所以 `[15]` 组收的是文件顶部 7 条 `[15]` 预设（第 8 行才是 `[15]…数值、沉浸感再平衡_separator`），`[1]前置` 组是文件末尾 7 条。15 个分隔符目录不计入 mod 条目；合计 = 114 条（启用 100 / 禁用 14）。

## 三、分组明细（完整 mod 清单，含禁用项）

本节表格由 `node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/norvinsk-forever/scan.json --mode detail --profile "Default"` 直接产出（2026-11-01 重扫；页脚式分组：`_separator` 标记其上方区块结束）。排查提示：每组的启用数/总数见小节标题；`启用` 列 ✓ = 启用、— = 禁用；`上游` 列的 `#id` 一律为 **Forge（sp-mod.com）编号**，本包 meta 未记录 Hub / Nexus 编号（§六）。表中 21 条非 Forge 项的自建 / Nexus / 自定义层归类见 §五。

#### [15]《永远的诺文斯克》数值、沉浸感再平衡（6 / 7 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [15]《永远的诺文斯克》腰包小容器-PackNStrap预设 | ✓ | d2024.5.3.0 | — | 增加更多可以放进保险箱的容器，并修正了臂包的售价。感谢群友：渗透者之心的定价计算 |
| [15]《永远的诺文斯克》Vagabond预设 | — | — | — |  |
| [15]《永远的诺文斯克》ORBIT预设 | ✓ | — | — |  |
| [15]《永远的诺文斯克》APBS预设 | ✓ | — | — |  |
| [15]《永远的诺文斯克》ABPS预设 | ✓ | — | — |  |
| [15]《永远的诺文斯克》SAIN预设-Norvinsk Forever's Preset | ✓ | — | — |  |
| [15]《永远的诺文斯克》-客户端MOD-F12设置cfg所在 | ✓ | — | — | 对游戏界面有自己习惯请关掉此项。否则进入游戏将只有左下角蹲姿体力槽。难度选项已包含此项目，启用难度会被部分覆盖。 |

#### [14]汉化模组（2 / 2 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [14]Simple TranslatorCS 文本汉化v1.4.1 | ✓ | 1.4.1 | Forge #1577 | 文本汉化工具（Simple Translator 简体中文）。 |
| [14]ConfigurationManager.zh-cn 汉化附属v2.13 Mono版本 | ✓ | 2.13 | — | ConfigurationManager（F12 配置管理器）简体中文汉化附属（Mono 版；适用 SPT 4. |

#### [13]MOD工具（4 / 6 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [13]MOD工具-mod性能监视器-ModProfiler | ✓ | d2026.10.14.0 | — |  |
| [13]MOD工具-快捷撤离-DebugExtractSurvived | ✓ | d2026.10.14.0 | — |  |
| [13]MOD工具-综合调试工具包-SamMeow.DebugToolkit | ✓ | 0.1.0 | — | SPT 4.1.6 调试工具包整合（Tooltip / Bot 覆盖层 / 无敌组 / QOL），替代 BotD |
| [13]更好的F12菜单-BetterBepInEx | ✓ | d2026.10.24.0 | — |  |
| [13]战局外检修标签-DebugTooltip | — | 5.0.0 | Forge #1542 | 在战局外界面显示检修与调试信息标签 |
| [13]MOD工具-AI刷新和行为检修-BotDebug | — | 1.8.0 | Forge #918 | 显示 AI 生成与行为相关调试信息 |

#### [12]修复、优化（4 / 4 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [12]商人列表滚动-TraderScrolling | ✓ | 4.1.0 | Forge #1089 | 让商人交易列表支持滚动浏览 |
| [12]任务列表修复-TaskListFixes | ✓ | 1.8.0 | Forge #824 | 修复任务列表相关问题 |
| [12]面具修复-KnightMaskFix(4.1.3+) | ✓ | 1.0.0 | Forge #3109 | 修复骑士面具的显示与使用问题 |
| [12]防卡手-HandsAreNotBusy | ✓ | 1.7.1 | Forge #1298 | 减少双手忙碌时的操作卡顿 |

#### [11]破坏沉浸感MOD（5 / 5 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [11]物品感应-AmandsSense | ✓ | 3.1.0 | Forge #2521 | 以视觉高亮辅助发现周围物品 |
| [11]任务跳过功能-Skipper | ✓ | 1.1.5 | Forge #1343 | 支持跳过任务中的部分步骤 |
| [11]内置动态地图-DynamicMaps | ✓ | 1.2.1 | Forge #1431 | 在战局内提供可查看的地图 |
| [11]声音可视化-accessibilityindicators | ✓ | 2.1.0 | Forge #1760 | 将声音事件转换为可视化提示 |
| [11]扫敌雷达-RadarStandalone | ✓ | 1.3.4 | Forge #1100 | 以圆形雷达显示周围敌人 |

#### [10]物品高效整理（告别华容道）（8 / 8 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [10]显示价格之快速出售-showmethemoney-quick-sell（Q+左Q+右） | ✓ | 4.2.0 | Forge #2955 | 为物品价格提示提供快速出售功能 |
| [10]显示价格-showmethemoney | ✓ | 3.0.2 | Forge #3011 | 在物品上显示其价格信息 |
| [10]物品容器间快速移动-QuickMoveToContainer | ✓ | 1.5.0 | Forge #1341 | 支持在容器间快速移动物品 |
| [10]自动存放-AutoDeposit | ✓ | 6.0.0 | Forge #1469 | 自动存放物品以减少手动整理 |
| [10]从武器架装备武器-EquipFromWeaponRack | ✓ | 1.6.0 | Forge #1136 | 允许从武器架直接装备武器 |
| [10]拾取时快速放入容器-ContainerQuickLoot | ✓ | 1.9.1 | Forge #3023 | 拾取物品时自动放入指定容器 |
| [10]UI大修-UIFixes | ✓ | 6.0.4 | Forge #1342 | 集中修复多项界面与交互问题 |
| [10]物品自动分类功能-IOF | ✓ | d2026.10.13.0 | Forge #2960 | 在仓库与容器中提供自动整理与排序功能 |

#### [9]全局功能大修（6 / 11 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [9]屏幕空间反射替换-SSRSix | — | 1.0.0 | Forge #3099 | 屏幕空间反射（SSR）替换。 |
| [9]环境光遮蔽替换-AOSix | — | 2.0.0 | Forge #2569 | 环境光遮蔽（AO）效果替换。 |
| [9]体积云替换-CloudSix | — | 4.0.0 | Forge #2567 | 体积云替换。 |
| [9]地面道路着色器替换-POMSix | — | 1.0.1 | Forge #3100 | 地面 / 道路视差遮蔽（POM）着色器替换。 |
| [9]体积雾效替换-FogSix | — | 1.0.0.0 | Forge #3110 | 体积雾效替换。 |
| [9]改进的自定义用户界面-ImprovedCustomizationUI | ✓ | 1.1.1 | Forge #3069 | 改进的自定义界面（服装 / 外观自定义 UI）。 |
| [9]子弹专家-MunitionsExpert | ✓ | 2.1.1 | Forge #3006 | 在物品提示中展示弹药详细属性 |
| [9]简单的锻炼QTE-simpleworkoutqte | ✓ | 2.2.2 | Forge #1437 | 为藏身处锻炼加入 QTE 小游戏 |
| [9]主菜单大修-MenuOverhaul | ✓ | 1.3.0 | Forge #1775 | 重制主菜单界面布局 |
| [9]全局功能大修-商人LLM对话-llm-trader-chat-test-build | ✓ | 0.1.0 | — | 商人 LLM 对话：在原生 Messenger 里与 19 位商人（12 原版 + 7 mod）文字聊天，回复由 |
| [9]预览窗口尺寸调整-previewsizer | ✓ | 1.1.0 | Forge #2339 | 调整物品与武器预览窗口的尺寸 |

#### [8]战局内功能大修（20 / 21 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [8]看表动作-WatchAnims | ✓ | 2.0.0 | Forge #2745 | 看表动作：查看剩余战局时间时的左手抬腕动画。 |
| [8]填装子弹动作-LoadAmmoAnim | ✓ | 2.0.0 | Forge #2681 | 填装子弹动作：往弹匣压弹的第一人称动画。 |
| [8]边走边装子弹-ContinuousLoadAmmo | ✓ | 1.2.0 | Forge #2112 | 支持移动中连续装填子弹 |
| [8]任务跟踪显示-QuestTracker | ✓ | 1.7.0 | Forge #1140 | 在界面显示任务目标进度 |
| [8]直接搜索已打开容器-SearchOpenContainers | ✓ | 1.5.0 | Forge #934 | 可直接搜索尚未搜刮完的容器 |
| [8]直接使用未捡物品-UseLooseLoot | ✓ | 1.6.0 | Forge #933 | 可直接使用地面上的未拾取物品 |
| [8]无须检视排障-InspectionlessMalfs | ✓ | 1.0.0 | Forge #465 | 无须检视即可直接排障（跳过故障检视）。 |
| [8]门的状态随机化-DoorRandomizer | ✓ | 1.8.0 | Forge #820 | 随机化战局中门的开关状态 |
| [8]身上任何物品均可绑定快捷键-UseItemsFromAnywhere | ✓ | d2026.10.13.0 | Forge #2386 | 可为身上任意物品绑定快捷键 |
| [8]人物技能扩展和撬锁-SkillsExtended | ✓ | d2026.10.13.0 | Forge #2383 | 扩展技能系统并加入撬锁内容 |
| [8]技能速度调节器-SkillMultiplier | ✓ | d2026.10.13.0 | Forge #2162 | 调节各技能的成长速度 |
| [8]可以攀爬更高的地方-Increase Climb Height | ✓ | 2.1.1 | Forge #1575 | 提高角色可攀爬的高度 |
| [8]Boss身上有lega徽章-bosseshavelegamedals | ✓ | 2.1.0 | Forge #1539 | 让 Boss 携带并掉落徽章 |
| [8]战场环境临场感增强-SPTBattleAmbience | ✓ | 2.4.0 | Forge #2264 | 增强战场环境音效与氛围 |
| [8]好莱坞级视觉系统合集-HollywoodFX+HollywoodGraphics | ✓ | 3.0.0 | Forge #2003 | 增强战斗视觉特效与画面表现 |
| [8]无限战局时间-RaidsAreInfinite | ✓ | 1.1.0 | Forge #2903 | 取消所有地图的战局倒计时：时间改为与真实时间同步的墙钟，逗留多久都可以。 |
| [8]背包枪槽-BackpackGunSlot | ✓ | d2026.10.14.0 | — |  |
| [8]武器死区-Deadzone | ✓ | d2026.10.14.0 | — |  |
| [8]门的大修-DoorOverhaul | — | d2026.10.13.0 | — |  |
| [8]门的大修-破门大修-DoorBreachOverhaul | ✓ | 0.1.0 | — | 破门大修（DoorBreachOverhaul）客户端 + C4 服务端：BB 4.1.6 破门 + DO 破坏 |
| [8]boss刷新通知-BossNotifier | ✓ | 1.2.1 | Forge #2543 | 在 Boss 刷新时发出通知 |

#### [7]装备、物品功能大修（7 / 8 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [7]平衡的夜视装备视效-BRNVG | ✓ | 3.0.4 | Forge #954 | 以真实感着色器重做夜视装备的成像效果 |
| [7]更好的枪匠界面-Tradermodding | ✓ | 2.1.2 | Forge #1283 | 改进商人处的武器改装界面 |
| [7]更好的手电筒光-BetterFlashlights | — | 1.0.1 | Forge #3084 | 更真实的手电筒光照与光斑。 |
| [7]光学镜前可叠加夜视热成像-MagicOpticMount | ✓ | 0.1.0 | Forge #3101 | 光学镜前可叠加夜视 / 热成像的镜架配件。 |
| [7]武器配件位置自定义-WeaponCustomizer | ✓ | 4.0.0 | Forge #1950 | 自定义武器配件的安装位置 |
| [7]更好的机瞄-BetterRearSights | ✓ | 1.8.0 | Forge #1591 | 改善武器机瞄准具的视野 |
| [7]瞄准视野大修-FOV-Fix | ✓ | 4.1.0 | Forge #701 | 修复并自定义瞄准时的视野 |
| [7]可以合并消耗品-MergeConsumables | ✓ | 1.6.3 | Forge #1657 | 允许合并同类消耗品 |

#### [6]AI行为（吃帧数，帧数低可选择关这类，开丧尸模式请关闭整个大类）（8 / 9 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [6]AI自动搜索物品-LootingBots | — | 1.8.0.0 | Forge #812 | 让 AI 在战局中主动搜索并拾取战利品（给Zen5玩家留的替代Orbit） |
| [6]AI锁头重定向-HeadshotDamageRedirection | ✓ | d2026.10.13.0 | Forge #2953 | 将头部/胸部致命伤害分摊到其他部位以增加生存机会 |
| [6]友方AI系统-PITFireteam | ✓ | 0.10.3 | Forge #2676 | 添加随玩家作战的友方 AI 火力小组 |
| [6]Acid的AI装备管理系统-progressivebotsystem | ✓ | 2.4.1 | Forge #1594 | 按进度调整 AI 的装备与配置 |
| [6]Acid的AI生成系统-botplacementsystem | ✓ | 2.1.0 | Forge #2097 | 控制 AI 在地图上的生成与分布 |
| [6]AI目标驱动行为-ORBIT2.0 | ✓ | 2.1.1.0 | Forge #2706 | 让 Bot 带着目标打战局：分头搜刮、追猎热点、实时撤离，附带省 CPU 的幽灵模式。（对Zen5CPU不友好， |
| [6]AI增强-SAIN | ✓ | 4.5.1 | Forge #791 | 全面重制 AI 的战斗行为 |
| [6]AI路径扩展-Waypoints | ✓ | 1.9.0 | Forge #827 | 扩展地图路径点以改善 AI 寻路 |
| [6]AI拟人MOD的前置框架-BigBrain | ✓ | 1.5.0 | Forge #902 | 为 AI 提供自定义行为层的公共前置框架 |

#### [5]整合包核心MOD-基于原玩法的大修（开关此类需重开档）（2 / 4 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [5]软核大修-InescapableTarkovsSoftcore | ✓ | 0.1.0 | — | Inescapable Tarkov's Softcore（SPT 4.1.6 线 v0.1.0）：《生活在诺文 |
| [5]现实主义系列-姿势大修-stance-overhaul | ✓ | 0.0.2 | Forge #3136 | 姿势系统大修（持枪 / 站姿 / 蹲姿姿态与镜头联动）。 |
| [5]开放世界大修-Vagabond | — | 0.8.3 | Forge #2642 | 把塔科夫改造成开放世界：靠过境点在地图间移动、以专属撤离点接触商人。 |
| [5]派系领土战争-TushonkaTerritories | — | d2026.10.13.0 | Forge #2942 | 为战局加入派系领土战争：Scav、流氓与掠夺者争夺各地图控制权，玩家可用声望与战果影响战局。 |

#### [4]自定义商人、任务（8 / 8 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [4]新敌人-WTT-黑色军团-BlackDiv | ✓ | 1.3.1 | Forge #2511 | WTT 黑色军团：新增敌人内容。 |
| [4]新商人-荷官-Croupier (random loadouts + flea quicksell) | ✓ | 2.0.7 | Forge #1971 | 新增随机装备商人，附带跳蚤市场快速出售 |
| [4]新商人-藏身处物资商人-acidphantasm-harryhideout | ✓ | 3.1.2 | Forge #1303 | 提供藏身处相关物资的商人 |
| [4]新商人-毒蝎-acidphantasm-scorpion | ✓ | 1.1.1 | Forge #1348 | 新增毒蝎商人并售卖特定商品 |
| [4]新商人-油漆匠-Painter | ✓ | 3.0.0 | Forge #1025 | 新增油漆匠商人及涂装相关物品 |
| [4]竞技场商人的箱子-Adam's Boxes at Ref (BARF) | ✓ | 3.0.0 | Forge #2483 | 在竞技场商人处添加储物箱 |
| [4]新商人-竞技场商人-ref-sptfriendly-quests | ✓ | 2.1.0 | Forge #1538 | 为竞技场商人添加适配单人模式的任务 |
| [4]新商人-Artem | ✓ | 3.0.2 | Forge #1023 | 新增商人 Artem 与其相关任务 |

#### [3.1]新配件（6 / 6 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [3]新配件-ConsortiumOfThings | ✓ | 2.0.1 | Forge #2195 | 新增大量武器配件 |
| [3]武器整合-Epic's All in One | ✓ | 5.0.1 | Forge #1263 | 整合多款武器与配件 |
| [3]新武器-AKResonant | ✓ | 3.0.0 | Forge #2170 | 新增 COD:MW2019 的 Resonant AK 步枪 |
| [3]新武器-俄罗斯特种部队武器包-Russian-SF-Pack | ✓ | 2.0.1 | Forge #3035 | 俄罗斯特种部队武器包（新武器系列）。 |
| [3]新武器-WTT武器库-WTT - Armory客户端 | ✓ | 3.0.0 | Forge #2246 | 扩展 WTT 武器与配件库 |
| [3]新配件-Eco-Attachment Emporium | ✓ | 3.0.0 | Forge #2882 | 扩展武器配件的获取与流通 |

#### [2]新内容（8 / 8 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [2]新衣服-MGS服装-RCTA - Metal Gear [FORTNITE] | ✓ | 1.0.4 | Forge #1768 | 新增 MGS 主题的服装 |
| [2]新衣服-RCTA - Synaptic Rig T1 (CODIW) | ✓ | 1.0.8 | Forge #1766 | 新增 COD:IW 主题的 Synaptic Rig 服装 |
| [2]WTT正式版内容 | ✓ | 2.0.5.0 | Forge #2512 | WTT 正式版内容回填包：新枪械、装备、配件等内容（服务端 + 客户端资源）。 |
| [2]新装备、衣服-TacticalGearComponent | ✓ | 3.0.0 | Forge #1125 | 新增战术装备与服装内容 |
| [2]腰包-WTT-PackNStrap | ✓ | d2026.10.19.0 | Forge #1278 | 新增腰包与腰带类容器 |
| [2]新酒水-MoreEnergyDrinks(近似替代) | ✓ | 1.3.0 | Forge #1688 | 新增更多能量饮料 |
| [2]新物品-镀金钥匙容器-GildedKeyStorage | ✓ | 2.1.1 | Forge #865 | 新增集中存放钥匙的容器 |
| [2]新物品-藏身处猫咪-Tarkin'sHideoutCat | ✓ | 1.1.1 | Forge #2908 | 在藏身处添加可互动的猫咪 |

#### [1]前置MOD（不要关）（6 / 7 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [1]前置框架-UnityToolkit | ✓ | 2.0.2 | Forge #1426 | Unity 资源与工具包前置：为依赖 Unity 资源加载的客户端模组提供共享工具集。 |
| [1]前置框架-游戏内交互界面公共库-WebOverlay | ✓ | d2026.10.14.0 | — |  |
| [1]前置框架-更多机器人API-MoreBotsAPI | ✓ | 2.1.1 | Forge #2426 | 机器人与 AI 模组 API 前置（BigBrain / SAIN 等生态依赖）。 |
| [1]前置框架-现实主义公共库-realism-common-lib | ✓ | 0.0.2 | Forge #3135 | SPT Realism 系列公共前置库。 |
| [1]前置框架-WTT公共库-WTT-ClientCommonLib | ✓ | 3.0.6 | Forge #2310 | WTT 系列模组的客户端公共前置库 |
| [1]前置-《永远的诺文斯克》好奇猫自用游戏设置及按键 | ✓ | — | — | 对游戏界面有自己习惯请关掉此项。否则进入游戏将只有左下角蹲姿体力槽 |
| [1]前置-《永远的诺文斯克》修改本体及MOD排序文件和整合包说明文档 | — | — | — | 利用Mo2的文件覆盖机制实现对本体文件的修改 |

## 四、排序规则与要点

- **前缀即优先级意图**：modlist 自上而下按 `[15]` → `[1]` 数字前缀降序排列；前缀同时是分组编号，与组尾的 `_separator` 同名（如 `[15]…数值、沉浸感再平衡` 收在 7 个 `[15]` 预设之后）。仓库既有口径记为「modlist.txt 越靠后优先级越高」，即覆盖关系由行序决定，改顺序 = 改覆盖关系（[整合包矩阵](README.md) §四）。
- **两组纪律写在分隔符名里**：`[5]整合包核心MOD-基于原玩法的大修（开关此类需重开档）` 整组开关需重开档；`[1]前置MOD（不要关）` 整组不要关。另 `[6]AI行为（吃帧数，帧数低可选择关这类，开丧尸模式请关闭整个大类）` 是性能/玩法二选一的提示。
- **底座沉底**：`[1]` 组是 5 个公共库（UnityToolkit / WebOverlay / MoreBotsAPI / realism-common-lib / WTT-ClientCommonLib）+ 2 个自定义前置（好奇猫设置、修改本体层），框架类放在默认加载底座位置。
- **覆盖方向的一个待确认观察**：`[15]腰包小容器-PackNStrap预设` 与 `[2]腰包-WTT-PackNStrap` 有 3 个同名文件（`config/config.jsonc`、`db/CustomItems/Gear_Belts.json`、`db/CustomItems/Item_Containers.json`；后两者预设版更大，实包文件树比对），预设需要「盖住」本体才生效；按上面口径顶部优先级最低，覆盖是否成立需实机按 MO2 覆盖标记复验。同类情形在 SAIN / ORBIT / APBS / ABPS 预设上无同名文件（纯新增文件），不受影响。

### 四.1 禁用项（14 条）与原因

| 禁用项 | 分组 | 版本 | 原因 | 证据 |
|---|---|---|---|---|
| `[15]Vagabond预设` | [15] | — | 随 Vagabond 一起停用，因由未记录 | 待确认 |
| `[13]战局外检修标签-DebugTooltip` | [13] | 5.0.0 | 被 DebugToolkit（Tooltip 模块）替换，目录保留可回滚 | dev-log 2026-10-11 |
| `[13]MOD工具-AI刷新和行为检修-BotDebug` | [13] | 1.8.0 | 被 DebugToolkit（Bot 覆盖层模块）替换，目录保留可回滚 | dev-log 2026-10-11 |
| `[9]屏幕空间反射替换-SSRSix` | [9] | 1.0.0 | 扩充批装后即禁；因由未记录（5 个 Six 同批） | 对照表 §八「待确认」 |
| `[9]环境光遮蔽替换-AOSix` | [9] | 2.0.0 | 同上 | 同上 |
| `[9]体积云替换-CloudSix` | [9] | 4.0.0 | 同上；另有 FikaSync 死目录隔离（F1） | 同上、dev-log 2026-10-12 |
| `[9]地面道路着色器替换-POMSix` | [9] | 1.0.1 | 同上 | 同上 |
| `[9]体积雾效替换-FogSix` | [9] | 1.0.0.0 | 同上 | 同上 |
| `[8]门的大修-DoorOverhaul` | [8] | d2026.10.13.0 | 破门覆盖层 `DoorBreachOverhaul` 0.1.0 接管，旧版目录保留作回滚 | dev-log 2026-10-25 |
| `[7]更好的手电筒光-BetterFlashlights` | [7] | 1.0.1 | 10-12 扩充批装入、10-14 后禁用；因由未记录 | 待确认（文档只列到 10-14 的 12 项） |
| `[6]AI自动搜索物品-LootingBots` | [6] | 1.8.0.0 | B8 冲突裁决：ORBIT 自带战利品引擎 | dev-log 2026-10-11、对照表 §五/§八 |
| `[5]开放世界大修-Vagabond` | [5] | 0.8.3 | B8 装入后禁用；启用需新建档案（旧档不受影响） | dev-log 2026-10-11、Vagabond 设置指南 |
| `[5]派系领土战争-TushonkaTerritories` | [5] | d2026.10.13.0 | 10-13 更新到 2.0.2 后禁用；因由未记录 | 待确认 |
| `[1]前置-修改本体…文档` | [1] | — | 「利用 MO2 覆盖机制修改本体」的自定义层，因由未记录 | README 待确认 #5 |

> 已被移出清单的旧禁用项：`SeparateHostility`（B8 冲突裁决，TT 双重敌意系统）、`HarryHead`（因由待确认）——两者目录已不在 `mods/`，modlist 无对应行（实包目录查看）。

## 五、本体修改 / 覆盖层 / 特殊资产

### 五.1 自定义层与预设（`[15]` / `[1]`）

| 层 | 内容 | 覆盖/作用对象 | 状态 |
|---|---|---|---|
| `[15]…-客户端MOD-F12设置cfg所在` | `BepInEx/config/` 下 **67 个 `.cfg`**（含 `com.sage.dooroverhaul.cfg`、`com.matsix.*six.cfg`、`com.acidphantasm.brightlasers.cfg` 等已禁用 mod 的 cfg 残留） | 客户端界面习惯、玩法开关；难度选项会覆盖其中部分项 | 启用 |
| `[15]…SAIN预设` | `BepInEx/plugins/SAIN/EditorDefaults.json` + 服务端 `Solarint-SAIN-ServerMod/CoreOverrides.json` 与 `Presets/Norvinsk Forever's Preset.json`（附 10-14 备份 `.20261014-010144.bak`） | SAIN 战斗参数与预设 | 启用 |
| `[15]…ORBIT预设` | 36 个文件：`plugins/ORBIT/Config/Maps/**` + `user/mods/ORBIT/config.json`、zones、`addon/Live-Like ORBIT – Full Config` | ORBIT 2.0 地图/区域/预设 | 启用 |
| `[15]…APBS预设` | 38 个文件：`acidphantasm-progressivebotsystem` 的 config / blacklists / `Presets/Norvinsk_Forever/{Ammo,Appearance,Chances,Equipment,…}/Tier1-7` | APBS 分档刷装参数 | 启用 |
| `[15]…ABPS预设` | 1 个 `acidphantasm-botplacementsystem/config.json` | ABPS 刷新配置 | 启用 |
| `[15]…PackNStrap预设` | 3 个文件：`WTT-PackNStrapServer` 的 config.jsonc / Gear_Belts.json / Item_Containers.json | 腰包容器与臂包售价（与 `[2]` 本体同名文件，见 §四） | 启用 |
| `[15]…Vagabond预设` | cfg 热键等随包预设 | Vagabond | **禁用** |
| `[1]前置-修改本体及MOD排序文件和整合包说明文档` | `EscapeFromTarkov_Data/Managed/Assembly-CSharp.dll` + `SPT_Runtime/SPT_Data/configs/core.json`、`locale.json` | 直接改本体文件（MO2 覆盖机制） | **禁用** |
| `[1]前置-好奇猫自用游戏设置及按键` | `SPT_Runtime/user/sptsettings/Control.ini` | 玩家按键/设置 | 启用 |

汉化层（`[14]`）：`Simple TranslatorCS 文本汉化 v1.4.1`（Forge #1577）+ `ConfigurationManager.zh-cn 汉化附属 v2.13（Mono）`（非 Forge；运行期在 `overwrite/API翻译日志/ConfigurationManager.zh-cn/` 落翻译日志）。

### 五.2 bot 补丁与工具链

- `[13]MOD工具-综合调试工具包-SamMeow.DebugToolkit` **v0.1.0**（自建）：Tooltip / **Bot 覆盖层** / 无敌组 / QOL 四模块合一，来源三件套 DebugTooltip 5.0.0 + BotDebug 1.8.0 + DadGamerMode 1.9.3 均为 MIT；部署后 DebugTooltip、BotDebug 禁用（目录保留），BigBrain 保持启用（硬依赖 ADR-0011）。
- 战局相关调试件：`ModProfiler`（性能监视）、`DebugExtractSurvived`（快捷撤离）、`BetterBepInEx`（F12 菜单增强）——均为 10-14 之后落位，meta url 空（§六）。
- `[8]破门大修-DoorBreachOverhaul` **0.1.0**（自建覆盖层，10-25 部署）：客户端 `DoorBreachOverhaul.dll` 175,616 B / SHA256 `4FF4DB6F…AAADE61C`；服务端 `SamMeow.DoorBreachOverhaulServer.dll` + `C4Explosive.bundle`；内容 = 06 排除开关体系（安全类总开关 + 高危组子开关 + 物理硬排除 + 调试面板分类/诊断日志）+ 07 C4 引爆接 DO 破坏管线。旧 `DoorOverhaul` 5 项调参待迁移（`Silent Peek`、`Quiet Volume`、薄/厚木、薄金属）。

### 五.3 自建 mod 与外部资产

| 类别 | 项 | 说明 |
|---|---|---|
| 自建（4） | Softcore416 `InescapableTarkovsSoftcore` 0.1.0 | 4.1.6 线自建，一次覆盖 Softcore / True Items / noFIR / 反重力臂章 / 更大背包 5 个未就绪条目；日志 7 模块 / 变更 10609 / 警告 32 / 错误 0 |
| 自建（4） | DebugToolkit 0.1.0 | 见 §五.2 |
| 自建（4） | DoorBreachOverhaul 0.1.0 | 见 §五.2 |
| 自建（4） | LLM Trader Chat 0.1.0（`llmtc-m1.zip`） | 原生 Messenger 商人对话；19 位商人（12 原版 + 7 mod）；随包 15 位商人任务手册（742 条委托）；运行期状态落 `overwrite/SPT_Runtime/user/mods/SamMeow.LLMTraderChat/state/` |
| Nexus 侧（7，dev-log 标注、meta 未填 url） | WebOverlay、BetterBepInEx、DebugExtractSurvived、ModProfiler、BackpackGunSlot、DoorOverhaul、Deadzone | 版本串形如 `d2026.10.14.0`（日期版本）；`installationFile` 有包名、无 Forge 编号 → Forge 覆盖 93/114 的差额来源之一 |
| 内容大件（对照表安装记录） | WTT-Armory 3.0.0 = 4640.7 MB、Eco-Attachment 3.0.0 = 2963.3 MB、Artem 3.0.2 = 1547.0 MB、Epic's All in One 5.0.1 = 1422.2 MB、ECOT 2.0.1 = 1161.0 MB | 体积集中在 `[3]` / `[4]` 组；WTT 内容包为 `WTT-ContentBackport-2.0.5.7z` |
| 离线工具产物 | `overwrite/SPT_Runtime/SPTarkov.Server.Core.Patched.dll` | 服务端核心的已打补丁副本（实包 overwrite 目录实列）；由哪个 mod/工具写入未记录 → 待确认 |
| 归档 | `downloads/` 15 个文件（含 `DoorBreachOverhaul-0.1.0-test.zip`、`llmtc-m1.zip`、`WTT-PackNStrap-2.1.1.7z` 及其 `.meta`）；`_quarantine/` 4 组（CloudSix FikaSync、APBS 两批、DoorBreach 部署备份）；`_quarantine/backups/` 5 组（含 `updates-20261013` 6 个旧版） | 回滚与审计素材，不建议清理 |

## 六、已知问题与维护要点

1. **文档落后于实包（最大风险）**：README / 对照表停在 10-14（105 实体 / 120 行 / 93 启用 + 12 禁用），dev-log 到 10-27；11-01 快照为 **129 实体 / 129 行 / 100 启用 + 14 禁用**。差值可定位：10-14 之后有 10 个 mod 目录被写入/新增（DoorBreachOverhaul、BetterBepInEx、WTT-PackNStrap、PackNStrap 预设、Vagabond、Vagabond 预设、LLM Trader Chat、SAIN 预设、ORBIT 预设、WTT正式版内容）；禁用项净变化 -SeparateHostility -HarryHead（目录已移除）+BetterFlashlights +DoorOverhaul +Vagabond预设 +TushonkaTerritories。除 DoorBreach 外均无 dev-log 条目 → 补记待办。
2. **scan 口径缺陷（读快照前必看）**：`hasServerMod` 全为 0——脚本只探 mod 根的 `user/` 或 `SPT_Data/`，而本包服务端文件在 `SPT_Runtime/user/` 下，**不能用该字段判断单/双端**；`forgeId=93`、`hubId=0`、`nexusId=0` 是因为 21 条非 Forge 项的 `url` 为空（Nexus 来源只在 dev-log / installationFile 里），不是「无上游」。
3. **分组口径已按页脚式对齐**：本包 `_separator` 是区块页脚（`[15]` 组 = 顶部 7 条、`[1]` 组 = 末尾 7 条）；`scan-pack.mjs` 已按此解析，2026-11-01 重扫（`capturedAt=2026-10-31T17:13:14Z`）后分组与实包一致。若引用 17:00 之前的旧快照，分组会整体错位一组，**不要混用**。
4. **覆盖方向的待确认**：PackNStrap 预设与 `[2]` 腰包 3 个同名文件（§四）。若按仓库口径「越靠后优先级越高」，顶部预设落在最低优先级，覆盖不成立；需在 MO2 左窗格看覆盖标记或进游戏核对容器/售价。**结论不宜照抄文档**（meta comments 声称该预设生效）。
5. **钉死版本风险**：B7 实验批的 Radar 1.3.4 / BossNotifier 1.2.1 / SeparateHostility 3.0.0 / Eco-Attachment 3.0.0 为精确小版本约束（4.1.x）；已知精确钉死会触发 ModValidator 全局拒载（`modloader-no_mods_loaded`），**优先回退 Eco（服务端）**（对照表 §一注）。
6. **部署期修复与长路径**：F1 CloudSix FikaSync 死目录隔离、F2 WTT bundle 长路径（overlay 更名 `[2]WTT正式版内容` 后 `Could not find bundle` 归零）、F4 Painter assort 残货隔离、F6 仓库标准行 50→56（新档 OOB=0）。机制见 [usvfs-long-path-silent-missing](../operations/usvfs-long-path-silent-missing.md)。
7. **存档门槛**：`[5]` 核心组整组开关需重开档；Vagabond 启用需新建档案；`[1]` 前置组不要关。仓库行 50→56 只对新档成立。
8. **未就绪面仍大**：pending 72 项（10-14 口径），已被自建/替代覆盖 6 项（Softcore416 连带 5 项 + DadGamerMode）；评估分档 14 项可移植 / 4 项重写 / 15 项不建议，建议顺序与目标线（4.1.6 vs 5.0）决策见 [porting-candidate-assessment](../migration/porting-candidate-assessment.md)。
9. **待确认清单（文档 7 项 + 本次新增）**：brightlasers 移除因由；Vagabond 与五个 Six 禁用因由；WeaponCustomizer / KnightMaskFix 是否更新；WTT 更名前的 overlay 名与首装；修改本体层为何禁用；downloads 清空策略；DebugToolkit 部署确证日期；**新增**：LLM Trader Chat 与 Softcore416 v0.2.0 的安装记录缺失、BetterFlashlights / DoorOverhaul / TushonkaTerritories / Vagabond 预设禁用因由、`SPTarkov.Server.Core.Patched.dll` 的写入方。

## 七、证据与复现

```powershell
# 1) 扫描实包（只读；产出结构化快照，数字的唯一权威来源）
node scripts/packs/scan-pack.mjs --pack-root "E:/Game/EFT_Offline/Norvinsk_Forever" --slug norvinsk-forever --out .artifacts/packs/norvinsk-forever/scan.json

# 2) 渲染分组表（本包唯一 profile 为 Default；不带 --profile 时脚本回落到第一个 profile）
node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/norvinsk-forever/scan.json --mode summary --profile "Default"
node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/norvinsk-forever/scan.json --mode detail  --profile "Default"
```

- **快照**：`.artifacts/packs/norvinsk-forever/scan.json`（`capturedAt=2026-10-31T17:13:14Z`；重跑会刷新）。长期副本：`knowledge/spt-kb/archive/packs/norvinsk-forever.json`（`capturedAt=2026-10-31T17:02:02Z`，计数一致 129 / 114 / 100 / 14 / 15，但分组是旧口径，**分组以 scan.json 为准**）。
- **口径说明（分组）**：`render-pack-tables.mjs` 按「分隔符 = 区块页脚」解析（2026-11-01 修复后），§二 / §三 就是上面两条 `--profile "Default"` 命令的直接输出（合计行除外），未做手工改表；任何数字差异以 `scan.json` 为准。
- **实包路径（仅溯源，页面不依赖）**：`E:\Game\EFT_Offline\Norvinsk_Forever\`；包内文档 `docs/{README.md, dev-log.md, 对照表-v0.1.0.md, pending-mods-v0.1.0.md, porting-assessment.md, Vagabond-设置指南.md}`；回滚点 `.mo2-mcp/profile-backups/`。本页所有非 scan 数字（回滚点条目数、mtime、cfg 数、文件重叠）均标注了来源，未标注者为**待确认**。
- **相关页**：[整合包矩阵](README.md)（四包关系 / 共有底座 41 组 / 上游 id 口径）· [porting-candidate-assessment](../migration/porting-candidate-assessment.md) · [pilot-experience-416-softcore-backport](../migration/pilot-experience-softcore-416-backport.md) · [pilot-experience-spt41-debugtoolkit](../migration/pilot-experience-spt41-debugtoolkit.md) · [416-softcore-v0.2.0-lessons](../operations/416-softcore-v0.2.0-lessons.md) · [usvfs-long-path-silent-missing](../operations/usvfs-long-path-silent-missing.md)
