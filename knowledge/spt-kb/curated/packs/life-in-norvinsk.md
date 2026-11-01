---
version: [3.11]
domain: both
topic: pack-profile
source: curated
title: 《生活在诺文斯克》Life in Norvinsk 整合包档案（SPT 3.11.4 主力线）
keywords: [Life-in-Norvinsk, 生活在诺文斯克, SPT 3.11.4, 整合包档案, MO2 modlist, 排序规则, order.json, bot修复补丁, 版本线, Hub编号, 生化危机配置]
summary: "《生活在诺文斯克》SPT 3.11.4 主力线档案：196 个 mods 目录（179 真 mod + 17 分隔符），主档 179 条（启用 169 / 禁用 10），另两档 default 165/13、生化 161/15；73 条带旧 Hub 编号，forgeId 0；order.json 107 条；bot 补丁 v1.0 六项修复；版本线 v0.3.2.2→v0.3.3(+补丁)→3.11.5 特别优化版。"
---

# 《生活在诺文斯克》Life in Norvinsk 包构成档案（SPT 3.11.4 主力线）

> **用途**：本页是仓库内**可离线查询**的包构成档案 —— 要回答「这包有什么 mod、怎么排的、哪个 profile 开了什么、本体被谁改过」时，不必打开 E: 实包。实包路径只作溯源用途，不作为阅读前提。
>
> **数据快照**：2026-11-01 01:13（Asia/Shanghai，即 scan.json 的 capturedAt 2026-10-31T17:13:15Z），快照文件 .artifacts/packs/life-in-norvinsk/scan.json（526,808 B）。除明确标注「实测」（现场读 ini/文件头）或「包内文档」（docs/README.md、docs/bot修复补丁-v1.0-安装说明.md）外，**本页所有数字均来自该快照**；无法取证处一律写「待确认」并给线索，不估算。

## 一、身份与环境

| 项 | 值 | 来源 |
|---|---|---|
| 包名 | 《生活在诺文斯克》Life in Norvinsk（包根目录 Life_in_Norvinsk_v0.3.2；MO2 实例名 SPT3.11.4《生活在诺文斯克》0.3.2.2） | scan.packRoot / 实例 ini（实测） |
| SPT 版本线 | 3.11.4（EFT 0.16.0.35392）；同一 MO2 mod 库下另有 3.11.5 服务端安装 | gamePath 的 SPT_Data/Server/configs/core.json（实测） |
| 游戏根 gamePath | E:\Game\EFT_Offline\SPT_3114 | 实例 ModOrganizer.ini（实测） |
| MO2 gameName | SPT3 | 实例 ModOrganizer.ini（实测） |
| profile | 单机运行整合包（主档 / selected_profile）；并存 default、生化危机配置 | 实例 ini（实测）+ scan.profiles |
| MO2 版本 | 2.5.2（实例 ini version=2.5.2；配套 SPT特别优化版 MO2 v2.5.2） | 实例 ini + E: 根资产（实测） |
| mods 目录数 | 196（179 个真 mod + 17 个分隔符伪目录） | scan.counts.modDirs |
| 启用 / 禁用 | 169 / 10（主档）；另 default 165/13、生化危机配置 161/15 | scan.profiles[].counts |

**同一个 MO2 mod 库、两份服务端**（本包最容易被忽略的结构）：

| MO2 实例（C:\Users\Winde\AppData\Local\ModOrganizer\） | gamePath | core.json 实测 sptVersion | 主 profile |
|---|---|---|---|
| SPT3.11.4《生活在诺文斯克》0.3.2.2 | E:\Game\EFT_Offline\SPT_3114 | 3.11.4 | 单机运行整合包 |
| SPT3.11.4《生活在诺文斯克》0.3.3 | E:\Game\EFT_Offline\SPT_3114_Novinsk_Edition | 3.11.5 | 单机运行整合包 |

- 两个实例的 base_directory 同为 E:/Game/EFT_Offline/Life_in_Norvinsk_v0.3.2 —— **mods/ 与 profiles/ 共用一份，只有游戏本体/服务端不同**。所以「升 3.11.5」在这台机器上表现为换一个实例的 gamePath，而不是复制一份 mod 库。
- 注意：scan.json 的 mo2 字段为 null（扫描器不读 MO2 配置），上表的 gamePath / gameName / profile / MO2 版本是现场读实例 ini 得到的「实测」值，不是快照字段。

## 二、构成总览

主档「单机运行整合包」的分组总览（由 render-pack-tables.mjs --mode summary 直接产出，逐字未改）：

| 分组（MO2 分隔符） | 条目 | 启用 | 禁用 |
|---|---|---|---|
| [15]《现实化诺文斯克》数值、沉浸感再平衡 | 18 | 15 | 3 |
| [14]汉化模组 | 2 | 2 | 0 |
| [13]MOD工具 | 5 | 4 | 1 |
| [12]修复、优化 | 6 | 6 | 0 |
| [11]破坏沉浸感MOD | 11 | 9 | 2 |
| [10]物品高效整理（告别华容道） | 12 | 11 | 1 |
| [9]全局功能大修 | 12 | 12 | 0 |
| [8]战局内功能大修 | 26 | 25 | 1 |
| [7]装备、物品功能大修 | 19 | 19 | 0 |
| [6]AI行为（吃帧数，帧数低可选择关这类，开丧尸模式请关闭整个大类） | 12 | 11 | 1 |
| [5.5]整合包核心MOD-改变游戏玩法的大修（开关此类需重开档） | 6 | 5 | 1 |
| [5]整合包核心MOD-基于原玩法的大修（开关此类需重开档） | 4 | 4 | 0 |
| [4]自定义商人、任务 | 11 | 11 | 0 |
| [3.1]新配件 | 6 | 6 | 0 |
| [3]新武器 | 8 | 8 | 0 |
| [2]新内容 | 14 | 14 | 0 |
| [1]前置MOD（不要关） | 7 | 7 | 0 |

> 合计：179 条 mod 条目（启用 169 / 禁用 10）+ 17 个分隔符。


> 口径：render 未指定 --profile 时自动选择名字含「单机」的 profile，即主档；分隔符伪 mod 不计入 mod 条目。合计 179 = 启用 169 + 禁用 10，另 17 个分隔符，故 modlist.txt 共 196 条 + 1 行文件头 = 197 行。

### 2.1 三个 profile 的计数（均含禁用项）

| profile | modlist.txt | 条目 | mod 条目 | 启用 | 禁用 | 分隔符 | 定位 |
|---|---|---|---|---|---|---|---|
| default | profiles/default/modlist.txt | 195 | 178 | 165 | 13 | 17 | 遗留档（与主档差异最小） |
| 单机运行整合包 | profiles/单机运行整合包/modlist.txt | 196 | 179 | 169 | 10 | 17 | **主档**，selected_profile |
| 生化危机配置 | profiles/生化危机配置/modlist.txt | 193 | 176 | 161 | 15 | 17 | 分支玩法：丧尸模式向 |

### 2.2 三档行级差异（三档合计 183 个不同 mod 名：19 条状态/存在性不一致，其余 164 条在出现的各档中状态一致）

图例：✓ 启用 / ✗ 禁用 / — 该 profile 无此条目。

| Mod | default | 单机运行整合包 | 生化危机配置 | 依据 / 说明 |
|---|---|---|---|---|
| [13]好莱坞第三人称镜头-HollywoodCam | ✗ | ✓ | ✓ | 无 comments；default 未勾选，原因待确认 |
| [13]战局外检修标签-DebugTooltip | ✗ | ✓ | ✗ | 检修类工具，仅主档启用 |
| [13]MOD工具-AI刷新和行为检修-BotDebug | ✗ | ✓ | ✗ | 同上（默认关闭的排查工具） |
| [10]小火山的仓库搜索框修复-SearchBarFix | ✗ | ✗ | — | 两档均禁用；生化档无该条目 |
| [7]可以合并消耗品-MergeConsumables | ✓ | ✓ | — | 生化档无该条目 |
| [6]AI增强-SAIN-Moew-v4.4.0增强版 | ✓ | ✓ | ✗ | 生化档按组纪律关 AI 大类（组名：开丧尸模式请关闭整个大类） |
| [6]AI自动搜索物品-LootingBots - 已AI优化 | ✓ | ✓ | ✗ | 同上 |
| [6]AI路径扩展-Waypoints - 已AI优化 | ✓ | ✓ | ✗ | 同上 |
| [6]AI拟人MOD的前置框架-BigBrain - 已AI优化 | ✓ | ✓ | ✗ | 同上（BigBrain 系全家桶在生化档整体关闭） |
| [6]AI视线优化-thuynguyentrungdang版That.s.Lit - 已AI优化 | ✓ | ✓ | ✗ | 同上 |
| [6]友方AI系统-friendlypmc - 已AI优化（未合并版） | ✗ | — | — | 主档改用了「friendlypmc与PITFireTeam合并版本」，旧条目只留在 default 且禁用 |
| [6]AI修复-friendlyPMC护栏-FriendlyPmcGuard | — | ✓ | — | bot 补丁新增项，仅主档登记并启用 |
| [6]AI修复-护卫NRE护栏-FollowerGuard | — | ✗ | — | 补丁新增项，但主档 modlist 行为「-」（见第六节） |
| [5]丧尸模式开关 | ✗ | ✗ | ✓ | 生化档开丧尸模式；其余两档关闭 |
| [5]战局大修-RaidOverhaul - 已AI增强版本 | ✓ | ✓ | — | 生化档改用下面的「已AI优化」变体 |
| [5]战局大修-RaidOverhaul - 已AI优化 | — | — | ✓ | 同名变体，档间选择不同 |
| [1]前置框架-WTT公共库-WTT-ClientCommonLib | ✓ | ✓ | — | 生化档无该条目（WTT 系组件在生化档的取舍待确认） |
| [9]工具提示中显示售价-LootValue | — | — | ✓ | 仅生化档存在并启用 |
| [9]游戏画质改善-阿曼达的图形-Amanda's Graphics | — | — | ✗ | 仅生化档存在且禁用；与 HollywoodGraphics 不兼容（见 [8]好莱坞级视觉系统合集 comments） |

## 三、分组明细（完整 mod 清单）

> 本节表格由 render-pack-tables.mjs --mode detail 直接产出（仅把渲染出的 4 级标题降为 3 级，其余逐字未改），**179 条 mod 条目全量在表，启用与禁用都保留，未删行**。
>
> 读表要点：
> - **「上游」列的 Hub #N 指旧站 hub.sp-tarkov.com/files/file/N**（3.11 线）。本包 forgeId 全为 null（0 条），hubId 73 条，范围 Hub #184–#2796；另有 1 条只有 GitHub 链接（AmandsSense），合计 74 条带 url。**旧 Hub 编号与 4.1.6 包用的 Forge 编号是两套体系，不能互相比较**（例：UI Fixes 在旧 Hub 是 1860、在 Forge 是 1342）。
> - 「说明」列取自每个 mod 的 meta.ini comments，为功能摘要，非完整介绍。
> - **分组是「页脚式」的**：_separator 标记它上方区块的结束，组名即该页脚名。所以 [14]汉化模组 组装的是它上方那 2 条 [14] 开头的 mod；文件顶部没有前置页脚的那一块（18 条无 [N] 前缀的自定义设置/难度项）归给第一个页脚 [15]；文件末尾的页脚 [1]前置MOD（不要关）独自结算它上方的 7 条前置。详见 4.2。

### [15]《现实化诺文斯克》数值、沉浸感再平衡（15 / 18 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| 《生活在诺文斯克》背包容量扩展-BetterBackpacks沉浸感增强设置 | ✓ | d2024.8.7.0 | Hub #772 | 增加了对MOD背包的支持 |
| 《生活在诺文斯克》物品堆叠-True Items沉浸感增强设置 | ✓ | d2023.12.9.0 | — | 增加了更多物品的堆叠 |
| 《生活在诺文斯克》更多的酒水MOD-CornerStore+HoodsEnergyDrinks沉浸感增强设置 | ✓ | d2024.5.16.0 | — | mod酒水吃喝效果全部改成缓慢增加 |
| 《生活在诺文斯克》技能扩展-SkillsExtended沉浸感增强设置-已AI优化 | ✓ | d2024.5.16.0 | — | 在config里增加了BackdoorBandit的C4炸弹的工作台配方 |
| 《生活在诺文斯克》反重力袖章-AntigravArmbands沉浸感增强设置 | ✓ | d2023.11.2.0 | — | 增加更多的反重力臂章和减重等级 |
| 《生活在诺文斯克》腰包小容器-WTT-PackNStrap沉浸感增强设置 | ✓ | d2024.5.3.0 | — | 增加更多可以放进保险箱的容器，并修正了臂包的售价。感谢群友：渗透者之心的定价计算 |
| 《生活在诺文斯克》战局大修-RaidOverhaul沉浸感增强设置 | ✓ | d2025.5.5.0 | — |  |
| 《生活在诺文斯克》AI生成-botplacementsystem沉浸感增强设置 | ✓ | d2025.4.27.0 | — |  |
| 《生活在诺文斯克》难度：容易-主家：中心区Nakatani地下室楼梯 | — | d2024.9.16.0 | — | 难度四选一。最容易。 |
| 《生活在诺文斯克》难度：中等-主家：森林山间仓库 | — | d2024.9.16.0 | — | 难度四选一。中等难度。 |
| 《生活在诺文斯克》难度：困难-主家：海关17号仓库 | — | d2024.9.16.0 | — | 难度四选一。较难。 |
| 《生活在诺文斯克》难度：生存-重生地：中心区-主家：立交桥安全屋 | ✓ | d2024.9.16.0 | — | 难度四选一。最难。 |
| 《生活在诺文斯克》-单独-软核-Softcore沉浸感增强设置 | ✓ | d2023.11.18.0 | — | 软核的设置目前跟1.1.8一致。难度选项已包含此项目，启用难度会被完全覆盖。 |
| 《生活在诺文斯克》-单独-现实主义-Realism数值重制+小火山汉化+兼容补丁 | ✓ | d2025.2.7.0 | — | 现实主义的本整合包所有自定义参数均在此。难度选项已包含此项目，启用难度会被部分覆盖。 |
| 《生活在诺文斯克》-单独-商人以物易物-barter_economy-经济系统设置 | ✓ | d2025.3.9.0 | — | 所有商人的以物易物设置均在此设置。难度选项已包含此项目，启用难度会被完全覆盖。 |
| 《生活在诺文斯克》-单独-客户端MOD-F12设置cfg所在 | ✓ | — | — | 对游戏界面有自己习惯请关掉此项。否则进入游戏将只有左下角蹲姿体力槽。难度选项已包含此项目，启用难度会被部分覆盖。 |
| 《生活在诺文斯克》SAIN增强版预设-Live in Norvinsk AI preset | ✓ | — | — | 整合包专属SAIN预设-持续更新 |
| 《生活在诺文斯克》Samuel's Tweaks | ✓ | — | — | 自制MOD，优化客户端底层/护甲弹挂不冲突/可以掠夺臂章近战武器/三格弹匣变两格 |

### [14]汉化模组（2 / 2 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [14]本体缺失翻译 | ✓ | — | — | 翻译者：群友✘✘✘✘。现包括Artem、Painter、scorpion三个商人的任务文本翻译。 |
| [14]客户端ModF12通用翻译器及菜单汉化文本 | ✓ | d2024.12.15.0 | — | 这个mod会将所有F12的选项全部翻译且不影响原mod文件。 |

### [13]MOD工具（4 / 5 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [13]瞄准点坐标-AimPointCoordinates | — | d2025.7.28.0 | — |  |
| [13]好莱坞第三人称镜头-HollywoodCam | ✓ | d2026.6.23.0 | — |  |
| [13]战局外检修标签-DebugTooltip | ✓ | d2025.3.16.0 | Hub #2112 | 可以按\\键关了 |
| [13]MOD工具-AI刷新和行为检修-BotDebug | ✓ | d2025.3.16.0 | Hub #1242 | 检查PMC生成情况的工具，默认关闭。F9/F10可实现类似透视的效果，但主要是显示各AI的行为信息，用来检查AI |
| [13]存档编辑器-SPT-AKI Profile Editor | ✓ | d2025.5.3.0 | Hub #184 |  |

### [12]修复、优化（6 / 6 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [12]商人列表滚动-TraderScrolling | ✓ | d2025.5.3.0 | Hub #1508 | 提供商人界面的滚动选择功能 |
| [12]任务列表修复-TaskListFixes | ✓ | d2025.3.31.0 | — |  |
| [12]尸体可删除-SPTCorpseCleaner | ✓ | d2025.4.2.0 | — |  |
| [12]骑士面具修复-FacetheKnight-MaskFix | ✓ | d2025.10.11.0 | — |  |
| [12]防卡手-HandsAreNotBusy | ✓ | d2025.3.16.0 | Hub #1810 | 防卡手快捷键：SHIFT+L，手卡了就按一下 |
| [12]白色方块bug修复-WhiteBoxFix | ✓ | d2025.4.22.0 | — |  |

### [11]破坏沉浸感MOD（9 / 11 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [11]新任务线-迎新礼物任务-EFCL-WelcomeGifts | — | d2025.9.19.0 | Hub #993 | 用任务的方式将礼品分派给了不同的商人 |
| [11]多种可选准心-SimpleCrosshair | — | d2025.3.17.0 | Hub #1920 | 感谢Volcano的汉化工作及使用授权：https://sns.oddba.cn/139847.html |
| [11]我就是爹作弊工具-DadGamerMode | ✓ | d2025.3.17.0 | Hub #926 | 其实就是无敌mod，也提供类COD呼吸回血，感谢Volcano的汉化工作及使用授权：https://sns.od |
| [11]新商人-可购买自己创建的预设枪械-Hephaestus | ✓ | d2025.6.16.0 | — |  |
| [11]好莱坞投掷武器视觉辅助-VisualAssist | ✓ | d2025.8.24.0 | — |  |
| [11]扫敌雷达-RadarStandalone | ✓ | d2025.9.13.0 | Hub #1522 | 挂！光环雷达，可以标识周围120米内的AI分布。 |
| [11]物品感应-AmandsSense | ✓ | d2025.5.4.0 | github.com | 附近可拾取物扫描，使用方法：游戏内双击F。感谢Volcano的汉化工作及使用授权：https://sns.odd |
| [11]任务跳过功能-Skipper | ✓ | d2025.3.18.0 | Hub #1861 | 在任务条件项后面添加一个跳过按钮，可以一键完成任务。 |
| [11]内置动态地图-DynamicMaps | ✓ | d2025.4.19.0 | Hub #1981 | 战局内按M，或查看地图可显示。 |
| [11]声音可视化-accessibilityindicators | ✓ | d2025.4.7.0 | — | 顾名思义，感谢Volcano的汉化工作及使用授权：https://sns.oddba.cn/139847.htm |
| [11]更多界面信息-GamePanelHUD | ✓ | d2025.3.23.0 | Hub #652 | 多功能用户界面 |

### [10]物品高效整理（告别华容道）（11 / 12 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [10]显示价格之快速出售-showmethemoney-quick-sell（Q+左Q+右） | ✓ | d2025.10.6.0 | — |  |
| [10]显示价格-showmethemoney | ✓ | d2025.10.10.0 | — |  |
| [10]物品容器间快速移动-QuickMoveToContainer | ✓ | d2025.3.16.0 | Hub #1859 | 打开容器后，按Ctrl+点击物品即可自动将物品放到已打开的对应容器内 |
| [10]自动存放-AutoDeposit - 已AI优化 | ✓ | d2025.3.16.0 | — |  |
| [10]物品真实堆叠-True Items Redux | ✓ | d2024.8.2.0 | Hub #2206 | 提供其他物品的堆叠功能 |
| [10]物品自动分类功能-INVENTORY ORGANIZING FEATURES | ✓ | d2025.5.4.0 | Hub #2122 | 使用特定的标签来实现自动物品分类和容器锁定位置。 |
| [10]从武器架装备武器-EquipFromWeaponRack | ✓ | d2025.3.16.0 | Hub #1567 | 顾名思义 |
| [10]拾取时快速放入容器-ContainerQuickLoot - 已AI优化 | ✓ | d2025.4.12.0 | — |  |
| [10]小火山的仓库搜索框修复和扩展-SearchBarExtended | ✓ | d2026.1.3.0 | — |  |
| [10]小火山的仓库搜索框修复-SearchBarFix | — | d2026.9.6.0 | — |  |
| [10]仓库搜索框-StashSearch | ✓ | d2025.3.22.0 | Hub #1769 | 在仓库上增加一个搜索栏方便搜索物品，感谢Volcano的汉化工作及使用授权：https://sns.oddba. |
| [10]UI大修-UIFixes | ✓ | d2025.10.6.0 | Hub #1860 | 对NJT的垃圾UI操作进行大量改进，让体验更好，感谢Volcano的汉化工作及使用授权：https://sns. |

### [9]全局功能大修（12 / 12 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [9]解锁所有藏身处装饰-UnlockHideoutCustomization | ✓ | d2025.7.18.0 | — |  |
| [9]预览窗口尺寸调整-previewsizer | ✓ | d2025.10.6.0 | — |  |
| [9]项目上下文菜单扩展-ItemContextMenuExt | ✓ | d2025.5.8.0 | Hub #1283 |  |
| [9]子弹专家-MunitionsExpert | ✓ | d2025.3.25.0 | Hub #1324 | 顾名思义 |
| [9]简单的锻炼QTE-simpleworkoutqte | ✓ | d2025.5.8.0 | Hub #1989 |  |
| [9]物品打勾标记提供更多信息-MoreCheckmarks | ✓ | d2025.3.16.0 | Hub #1159 | 顾名思义，感谢Volcano的汉化工作及使用授权：https://sns.oddba.cn/139847.htm |
| [9]任务交接自动化-TaskAutomation | ✓ | d2025.8.21.0 | — |  |
| [9]藏身处不用打勾-nofirhideout | ✓ | d2025.10.21.0 | — |  |
| [9]腰带槽-BeltSlot | ✓ | d2025.7.12.0 | — |  |
| [9]动态物品重量-DynamicItemWeights | ✓ | d2025.10.9.0 | — |  |
| [9]小火山的全界面中文字体切换 | ✓ | d2026.8.11.0 | — |  |
| [9]主菜单大修-MenuOverhaul | ✓ | d2025.6.26.0 | Hub #2412 |  |

### [8]战局内功能大修（25 / 26 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [8]自动奔跑-AutoRun-alt+W自动向前 | — | d2025.5.4.0 | — |  |
| [8]沉浸感时间循环-ImmersiveDaylightCycle | ✓ | d2026.6.23.0 | — |  |
| [8]边走边装子弹-ContinuousLoadAmmo - 已AI优化 | ✓ | d2026.3.5.0 | — | 边走边填装子弹快捷键：K，会导致AutoDeposit的容器按钮消失 |
| [8]可拾取穿墙物品-ReachExtender | ✓ | d2025.5.16.0 | Hub #1758 |  |
| [8]boss刷新通知-BossNotifier | ✓ | d2025.5.16.0 | Hub #1737 |  |
| [8]塔科夫天气系统-Tarkov-Weather-System | ✓ | d2026.1.25.0 | Hub #2796 |  |
| [8]坠落的补给直升机-HeliCrash.ArysReloaded | ✓ | d2025.4.19.0 | — |  |
| [8]禁用Scav模式-DisableScavMode | ✓ | d2025.3.25.0 | Hub #1815 | 因核心玩法需要，禁用Scav |
| [8]武器破门-BackdoorBandit客户端文件 | ✓ | d2025.5.13.0 | Hub #2575 | 整合包默认所有武器可破门，已汉化 |
| [8]武器破门-BackdoorBandit服务器文件 | ✓ | d2025.5.13.0 | Hub #2575 | 整合包默认所有武器可破门，已汉化 |
| [8]任务跟踪显示-QuestTracker | ✓ | d2025.3.17.0 | Hub #1574 | 感谢Volcano的汉化工作及使用授权：https://sns.oddba.cn/139847.html |
| [8]增加拾取距离-LootRadius | ✓ | d2025.3.31.0 | Hub #1870 |  |
| [8]直接搜索已打开容器-SearchOpenContainers | ✓ | d2025.3.16.0 | Hub #1265 | 可以直接搜索打开的文件柜等，不用关了再开。 |
| [8]直接使用未捡物品-UseLooseLoot | ✓ | d2025.3.16.0 | Hub #1264 | 对于未拾取的吃喝药品，可以直接使用，而不用先捡进包里 |
| [8]门的状态随机化-DoorRandomizer | ✓ | d2025.3.16.0 | Hub #1107 | 现在所有的门的开关状态不是一成不变的。 |
| [8]身上任何物品均可绑定快捷键-UseItemsFromAnywhere | ✓ | d2025.3.16.0 | Hub #1416 | 顾名思义 |
| [8]更有意义的武器精通-MeaningfulWeaponMasteries | ✓ | d2025.10.6.0 | — |  |
| [8]超重也能练耐力-Always-Level-Endurance | ✓ | d2025.8.11.0 | — |  |
| [8]人物技能扩展和撬锁-SkillsExtended-已AI优化 | ✓ | d2025.4.2.0 | Hub #1722 | 顾名思义，它会修改部分技能的效果，目前处于初期测试阶段，但和现实主义没有冲突 |
| [8]技能速度调节器-SkillMultiplier | ✓ | d2026.1.28.0 | — |  |
| [8]可以攀爬更高的地方-Increase Climb Height | ✓ | d2025.3.17.0 | — |  |
| [8]Boss身上有lega徽章-bosseshavelegamedals | ✓ | d2025.5.4.0 | — |  |
| [8]战场环境临场感增强-SPTBattleAmbience | ✓ | d2025.9.30.0 | — |  |
| [8]好莱坞级视觉系统合集-HollywoodFX+HollywoodGraphics-已AI优化 | ✓ | d2025.10.10.0 | Hub #2683 | HollywoodGraphics与阿曼达的图形不兼容，二选一，吃帧数，比较好看。 |
| [8]门可以锁-LockableDoors | ✓ | d2025.3.31.0 | — |  |
| [8]火山家的实时字幕-Volcano-subtitle | ✓ | d2026.8.11.0 | — |  |

### [7]装备、物品功能大修（19 / 19 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [7]更好的枪匠界面-Tradermodding客户端文件 | ✓ | d2025.3.22.0 | Hub #1795 |  |
| [7]更好的枪匠界面-Tradermodding服务器文件 | ✓ | d2025.3.22.0 | Hub #1795 |  |
| [7]真实的热成像-realisticThermalScopes | ✓ | d2025.5.14.0 | — |  |
| [7]更亮的镭射线-brightlasers | ✓ | d2025.3.16.0 | Hub #1880 |  |
| [7]反重力臂章-AntigravArmbands | ✓ | d2024.7.10.0 | — | 让所有袖章可以减少5-25KG的负重，并可堆叠5个，力量满级后袖章请放在背包或者保险箱 |
| [7]更大的背包-BetterBackpacks | ✓ | d2025.4.5.0 | Hub #772 |  |
| [7]反射镜视野重做-ReflexSightsRework客户端文件 | ✓ | d2025.4.22.0 | Hub #1753 | 反射镜红点大修，不挡视野 |
| [7]反射镜视野重做-ReflexSightsRework服务器文件 | ✓ | d2025.4.22.0 | Hub #1753 | 反射镜红点大修，不挡视野 |
| [7]可以合并消耗品-MergeConsumables | ✓ | d2026.7.12.0 | — |  |
| [7]平衡的夜视装备视效-BRNVG客户端文件 | ✓ | d2025.8.21.0 | Hub #1303 | 更真实的夜视装备视野效果 |
| [7]平衡的夜视装备视效-BRNVG服务器文件 | ✓ | d2025.8.21.0 | Hub #1303 | 更真实的夜视装备视野效果 |
| [7]武器配件位置自定义-WeaponCustomizer | ✓ | d2025.4.23.0 | — |  |
| [7]更好的机瞄-BetterRearSights | ✓ | d2025.4.30.0 | — |  |
| [7]弹匣优化管理系统-MagazineManagementSystem | ✓ | d2025.3.22.0 | — |  |
| [7]面具修复-mask-fix | ✓ | d2025.6.8.0 | — | 解除头部装备的冲突 |
| [7]MOD武器配件自动兼容框架-autocompatframework-已AI优化 | ✓ | d2025.9.24.0 | — |  |
| [7]武器动画更新-P90弹匣动画-transparentp90mag | ✓ | d2025.5.14.0 | — |  |
| [7]武器动画更新器-AnimatorUpdater | ✓ | d2025.4.9.0 | — |  |
| [7]瞄准视野大修-FOV-Fix | ✓ | d2025.12.5.0 | Hub #942 | 给部分光学倍镜添加Ctrl+鼠标滚轮的无极变焦功能，F12中可修改 |

### [6]AI行为（吃帧数，帧数低可选择关这类，开丧尸模式请关闭整个大类）（11 / 12 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [6]分离敌意-SeparateHostility | ✓ | d2026.2.24.0 | — |  |
| [6]AI修复-friendlyPMC护栏-FriendlyPmcGuard | ✓ | — | — | FriendlyPmcGuard 0.1.0 - friendlyPMC follower lifecycle  |
| [6]AI修复-护卫NRE护栏-FollowerGuard | — | — | — | AI修复-护卫NRE护栏：修补 BotFollower.Dispose 释放缺口，消除 boss 死亡后跟班 N |
| [6]友方AI系统-friendlypmc与PITFireTeam合并版本 | ✓ | d2025.9.16.0 | — | [6]友方AI系统-friendlypmc - 已AI优化的增强版本，吸收了4013的PITFireTeam的部 |
| [6]AI锁头重定向-Headshot Damage Redirection | ✓ | d2025.6.2.0 | Hub #1809 | 防锁头MOD，这个mod的作用是将原本锁头带来的伤害重分配到其他身体部位，减少一枪喷子白头眼情况发生 |
| [6]AI视线优化-thuynguyentrungdang版That.s.Lit - 已AI优化 | ✓ | d2025.9.30.0 | — |  |
| [6]AI增强-SAIN-Moew-v4.4.0增强版 | ✓ | d2025.8.6.0 | Hub #1062 | 增强的AI |
| [6]AI自动搜索物品-LootingBots - 已AI优化 | ✓ | d2025.4.15.0 | Hub #1096 | AI现在会搜索捡拾他看见的所有东西，包括你骗保的装备。感谢Volcano的汉化工作及使用授权：https://s |
| [6]Acid的AI装备管理系统-progressivebotsystem - 已AI优化 | ✓ | d2026.1.17.0 | — |  |
| [6]Acid的AI生成系统-botplacementsystem - 已AI优化 | ✓ | d2025.4.27.0 | — | 管理AI的波次生成 |
| [6]AI路径扩展-Waypoints - 已AI优化 | ✓ | d2025.6.20.0 | Hub #1119 | 更多的路径点，运行AI走得更远，更没有限制范围。 |
| [6]AI拟人MOD的前置框架-BigBrain - 已AI优化 | ✓ | d2025.3.16.0 | Hub #1219 | 整套BigBrain系AI扩展的必备前置MOD，依赖它的：SAIN\nSWAG\nWaypoints\nLoot |

### [5.5]整合包核心MOD-改变游戏玩法的大修（开关此类需重开档）（5 / 6 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [5]丧尸模式开关 | — | — | — | 利用Mo2的文件覆盖机制实现对丧尸模式的精准控制 |
| [5]舒适家园系统-HomeComforts - 已AI优化 | ✓ | d2025.9.7.0 | — | 基于LeaveItThere运行，也是配套MOD，提供战局内道具功能。 |
| [5]战局内存储-LeaveItThere - 已AI优化 | ✓ | d2025.9.7.0 | Hub #2572 | PTT的配套MOD，任何物品都可以放在突袭中不消失，直到您将其捡回仓库！ |
| [5]以物易物经济系统-barter_economy-已AI优化 | ✓ | d2025.6.22.0 | Hub #1760 | 让商人变成以物易物 |
| [5]武器操控大修-TarkovIRL_W.H.M(关现实主义这个也一起关） - 已AI优化 | ✓ | d2026.3.5.0 | Hub #2016 | 完全改变操控枪械的手感，感觉难受的话可以关了。 |
| [5]现实主义大修-Realism - 已AI优化 | ✓ | d2025.12.5.0 | Hub #606 | 让整个塔科夫现实化的关键MOD，感谢Volcano的汉化工作及使用授权：https://sns.oddba.cn |

### [5]整合包核心MOD-基于原玩法的大修（开关此类需重开档）（4 / 4 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [5]经济与制造系统大修-Softcore - 已AI优化 | ✓ | d2025.2.1.0 | Hub #998 | 经济系统改造MOD，整合包核心MOD |
| [5]全地图间移动套装-IEAPI+PTT - AI优化版 | ✓ | d2025.7.28.0 | — | 关闭后要重开档案 |
| [5]战局大修-RaidOverhaul - 已AI增强版本 | ✓ | d2025.5.5.0 | — |  |
| [5]新增5个开局角色-CustomProfiles | ✓ | d2025.5.3.0 | — |  |

### [4]自定义商人、任务（11 / 11 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [4]新商人-AES | ✓ | d2025.10.10.0 | — |  |
| [4]新商人-VolkovTrader | ✓ | d2025.3.25.0 | — | 狗牌换购商人 |
| [4]新商人-Epakim1-1 | ✓ | d2025.5.18.0 | — |  |
| [4]新商人-不明生还者-UnknownSurvivor | ✓ | d2025.4.27.0 | — |  |
| [4]新商人-荷官-Croupier (random loadouts + flea quicksell) | ✓ | d2025.3.22.0 | — |  |
| [4]新商人-藏身处物资商人-acidphantasm-harryhideout | ✓ | d2025.3.21.0 | Hub #1817 |  |
| [4]新商人-毒蝎-acidphantasm-scorpion | ✓ | d2025.3.31.0 | Hub #1869 | 一大堆任务，群友✘✘✘✘对任务本文进行了翻译 |
| [4]新商人-Artem | ✓ | d2025.6.7.0 | — |  |
| [4]新商人-油漆匠-Painter | ✓ | d2025.7.9.0 | Hub #1412 | 需要MagTape和BlackCore作为前置，已翻译 |
| [4]竞技场商人的箱子-Adam's Boxes at Ref (BARF) | ✓ | d2025.5.9.0 | — |  |
| [4]新商人-竞技场商人-ref-sptfriendly-quests | ✓ | d2025.3.16.0 | Hub #2108 | 已翻译 |

### [3.1]新配件（6 / 6 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [3]新配件-ConsortiumOfThings | ✓ | d2025.7.8.0 | — | 汉化进行中 |
| [3]新配件-Eco-Attachment Emporium | ✓ | d2025.9.21.0 | — | 汉化进行中 |
| [3]新配件-Echoes.of.Tarkov.-.Requisitions | ✓ | d2025.7.12.0 | — | 汉化进行中 |
| [3]新配件-EpicRangeTime-Weapons | ✓ | d2025.6.26.0 | — | 内包含商人Badger |
| [3]新配件-BlackCore | ✓ | d2025.4.23.0 | Hub #1345 | 新商人-油漆匠-CivicPainter的前置，已有现实主义兼容 |
| [3]新配件-MagTape | ✓ | d2025.4.19.0 | Hub #1404 | 新商人-油漆匠-CivicPainter的前置，已有现实主义兼容，已汉化 |

### [3]新武器（8 / 8 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [3]新武器功能-其他武器可使用超速射 - BabaYaga x Hyperburst | ✓ | d2025.9.15.0 | — | [3]新武器-WTT武器库-WTT - Armory是前置 |
| [3]新武器-AKResonant | ✓ | d2025.6.7.0 | — |  |
| [3]新武器-国产武器-1SD-QBZ191 | ✓ | d2025.12.6.0 | — |  |
| [3].50BMG重制-Epics 50 BMG Remaster-Expansion | ✓ | d2025.7.22.0 | — |  |
| [3]新武器-AK50 | ✓ | d2025.7.16.0 | — |  |
| [3]新武器-SIG_MCX_VIRTUS | ✓ | d2025.12.27.0 | — |  |
| [3]新武器-WTT武器库-WTT - Armory客户端 | ✓ | d2025.8.22.0 | — |  |
| [3]新武器-WTT武器库-WTT - Armory服务端 | ✓ | d2025.12.21.0 | — |  |

### [2]新内容（14 / 14 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [2]高清材质-TarkovHDRework | ✓ | d2025.10.10.0 | — | 会增加显存负担 |
| [2]自定义配方-tarkovcraft | ✓ | d2026.1.12.0 | — | 可用https://vinihns.github.io/TarkovCraft/生成配方文件 |
| [2]新衣服-MGS服装-RCTA - Metal Gear [FORTNITE] | ✓ | d2025.7.18.0 | — |  |
| [2]新衣服-RCTA - Synaptic Rig T1 (CODIW) | ✓ | d2025.6.11.0 | — |  |
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

### [1]前置MOD（不要关）（7 / 7 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [1]前置框架-WTT公共库-WTT-ClientCommonLib | ✓ | d2026.8.6.0 | — |  |
| [1]前置框架-自定义任务加载器-Virtual's Custom Quest Loader客户端文件 | ✓ | d2025.3.16.0 | Hub #885 |  |
| [1]前置框架-自定义任务加载器-Virtual's Custom Quest Loader服务器文件 | ✓ | d2025.3.16.0 | Hub #885 |  |
| [1]前置框架-自定义交互-CustomInteractions | ✓ | d2025.5.8.0 | Hub #1278 | [8]人物技能扩展和撬锁-SkillsExtended和[9]项目上下文菜单扩展-ItemContextMenu |
| [1]前置框架-KmyTarkovApi | ✓ | d2025.3.23.0 | Hub #1215 | GamepanelHUD的前置 |
| [1]前置-《生活在诺文斯克》修改本体及MOD排序文件和整合包说明文档 | ✓ | — | — | 利用Mo2的文件覆盖机制实现对本体文件的修改 |
| [1]前置-《生活在诺文斯克》好奇猫自用游戏设置及按键 | ✓ | — | — | 对游戏界面有自己习惯请关掉此项。否则进入游戏将只有左下角蹲姿体力槽 |

## 四、排序规则与要点

### 4.1 MO2 加载序的语义

- modlist.txt 的行序 = MO2 左栏自上而下的顺序；**越靠后优先级越高**（后加载者覆盖前者）。所以本页分组从 [15] 一路排到 [1]前置，读起来是「低优先级 → 高优先级」，而不是重要性从高到低。
- 包作者用 **[N] 数字前缀降序**表达这条语义：文件顶部是无前缀的《生活在诺文斯克》自定义设置组，随后 [15] 一路降到 [2]，最后把 **[1]前置沉底**——前置框架因此拿到**最高**优先级，其文件覆盖最优先。
- 由此可推：**改排序 = 改覆盖关系**。想覆盖某个 mod 的文件，必须把它放到目标 mod 之后；改前先看本节的禁用名单与 4.3 的重开档纪律。

### 4.2 分组机制：分隔符是「区块页脚」

- SamMeow 的包把 *_separator 当**页脚**用：一个分隔符标记**它上方区块的结束**，组名就是该分隔符的名字。所以 [14]汉化模组 组装的是它上方那 2 条 [14] 开头的 mod（不是它下方的内容）。
- 文件顶部没有前置页脚的那一块（18 条无 [N] 前缀的《生活在诺文斯克》自定义设置 / 难度四选一 / 单独设置项）归给第一个页脚 **[15]《现实化诺文斯克》数值、沉浸感再平衡**。
- 文件最末尾的页脚 **[1]前置MOD（不要关）** 结算它上方的 7 条前置条目 —— 前置因此独立成组，不再并入 [2]新内容。三档末尾都有这个页脚，所以三档都不会出现「(未分组)」组。
- 主档各页脚结算的条数（启用/总数）：[15] 15/18、[14] 2/2、[13] 4/5、[12] 6/6、[11] 9/11、[10] 11/12、[9] 12/12、[8] 25/26、[7] 19/19、[6] 11/12、[5.5] 5/6、[5] 4/4、[4] 11/11、[3.1] 6/6、[3] 8/8、[2] 14/14、[1]前置 7/7；合计 179 = 启用 169 + 禁用 10。
- 页脚只负责「归组 / 命名」，**覆盖优先级仍由行位置决定**（见 4.1）：行越靠后越优先，与它落在哪个页脚组里无关。

### 4.3 开关代价（哪些分组一动就要重开档）

| 分组 | 纪律 | 依据 |
|---|---|---|
| [5.5]整合包核心MOD-改变游戏玩法的大修 | **开关需重开档** | 分隔符名自带「开关此类需重开档」 |
| [5]整合包核心MOD-基于原玩法的大修 | **开关需重开档** | 同上 |
| （上述组内）[5]全地图间移动套装-IEAPI+PTT | **关闭后要重开档案** | 该 mod comments |
| [1]前置MOD（不要关） | 不要关 | 分隔符名 |
| [15]《现实化诺文斯克》内的难度四选一 | 只开一档；启用「难度」会覆盖「单独」设置项 | 难度项 comments「难度四选一」；软核/现实主义/以物易物项 comments 均写「难度选项已包含此项目，启用难度会被（部分/完全）覆盖」 |
| [6]AI行为（吃帧数…） | 开丧尸模式请关闭整个大类 | 分隔符名；生化档即按此执行（见 2.2） |

### 4.4 主档 10 条禁用项与依据（有据才写）

| 分组 | 禁用项 | 依据 | 结论 |
|---|---|---|---|
| [15]《现实化诺文斯克》数值、沉浸感再平衡 | 《生活在诺文斯克》难度：容易-主家：中心区Nakatani地下室楼梯 | comments「难度四选一。最容易。」 | 四档只开一档，本档选「生存」 |
| [15]《现实化诺文斯克》数值、沉浸感再平衡 | 《生活在诺文斯克》难度：中等-主家：森林山间仓库 | 同上 | 同上 |
| [15]《现实化诺文斯克》数值、沉浸感再平衡 | 《生活在诺文斯克》难度：困难-主家：海关17号仓库 | 同上 | 同上 |
| [13]MOD工具 | [13]瞄准点坐标-AimPointCoordinates | comments 为空 | **待确认**（无任何说明） |
| [11]破坏沉浸感MOD | [11]新任务线-迎新礼物任务-EFCL-WelcomeGifts | comments 只述功能 | **待确认** |
| [11]破坏沉浸感MOD | [11]多种可选准心-SimpleCrosshair | comments 只述功能 | **待确认** |
| [10]物品高效整理（告别华容道） | [10]小火山的仓库搜索框修复-SearchBarFix | 无 comments；同作者扩展版 SearchBarExtended 已启用 | 疑为同功能二选一（**推断，待确认**） |
| [8]战局内功能大修 | [8]自动奔跑-AutoRun-alt+W自动向前 | comments 为空 | **待确认** |
| [6]AI行为（吃帧数，帧数低可选择关这类，开丧尸模式请关闭整个大类） | [6]AI修复-护卫NRE护栏-FollowerGuard | bot 补丁文档要求新增并生效（自检锚 [FollowerGuard] applied 8/8） | **与补丁预期冲突，待确认**（见第六节） |
| [5.5]整合包核心MOD-改变游戏玩法的大修（开关此类需重开档） | [5]丧尸模式开关 | 组名「开丧尸模式请关闭整个大类」；comments「利用Mo2的文件覆盖机制实现对丧尸模式的精准控制」 | 非丧尸档关闭（推断） |

> 其余两档各自多出的禁用项（default 13 条、生化 15 条）除 2.2 差异表所列外，与主档同名单一致，不重复列举。

### 4.5 服务端加载序 user/mods/order.json

| 项 | 值 | 来源 |
|---|---|---|
| 位置 | mods\[1]前置-《生活在诺文斯克》修改本体及MOD排序文件和整合包说明文档\user\mods\order.json | 实测 |
| 体积 | 3,489 B | 实测 |
| 结构 | 单个对象，键 order，值为 mod 名数组 | 实测（JSON.parse） |
| 条目数 | **107 条**（106 个唯一名，Hephaestus 出现 2 次） | 实测解析 + 包内 README 记载一致 |
| 首 / 末条目 | friendlyPMC / zzzzz-simple-translator | 实测 |
| 语义 | 数组顺序即服务端 mod（user/mods 下各 mod 目录名）的加载先后；未出现在数组里的 mod 由加载器按默认规则补齐（**具体规则待确认**） | 结构推断，未读加载器源码 |
| 冲突 | 包内 README 记 107 条，另有「历史盘点记 110 条」的说法；本次直接解析为 107。**110 的来源与差额待确认** | 实测 + 包内文档 |

> 用法：服务端 mod 之间的覆盖/补丁关系看这个文件，不是看 modlist.txt（后者只管客户端 BepInEx 侧）。新增服务端 mod 若要改变加载先后，改这里；只增不改时通常追加到末尾。

## 五、本体修改 / 覆盖层 / 特殊资产

### 5.1 本体覆盖层（第一个 [1]前置 目录）

位置：mods\[1]前置-《生活在诺文斯克》修改本体及MOD排序文件和整合包说明文档\（comments：利用 Mo2 的文件覆盖机制实现对本体文件的修改）

| 相对路径 | 字节 | 作用 |
|---|---|---|
| meta.ini | 494 | MO2 元数据 |
| EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll | 15,243,264 | 改写后的客户端程序集，本体行为修改（≈15 MB） |
| SPT_Data\Server\configs\core.json | 8,136 | 服务端核心配置；实测 sptVersion=3.11.5、compatibleTarkovVersion=0.16.0.35392、commit=96e5b73f… |
| SPT_Data\Server\configs\locale.json | 597 | 本地化配置 |
| SPT_Data\Server\configs\location.json | 16,201 | 地图/地点配置 |
| SPT_Data\Server\configs\ragfair.json | 13,923 | 跳蚤市场配置 |
| SPT_Data\Server\configs\seasonalevents.json | 495,251 | 季节活动（体积最大的一项配置） |
| SPT_Data\Server\database\settings.json | 1,704 | 服务端数据库设置 |
| SPT_Data\Server\database\templates\handbook.json | 505,066 | 手册/图鉴条目 |
| SPT_Data\Server\database\traders\579dc571d53a0658a154fbec\base.json | 5,781 | 单个商人的基表 |
| user\mods\order.json | 3,489 | 服务端加载序（见 4.5） |
| STARTHeadless.bat / STARTHeadless test.bat | 1,059 / 2,367 | 无头/服务端启动脚本 |

- 合计 13 个文件（表中 12 行，其中 .bat 一行含 2 个文件）、16,297,332 B（实测），无成篇说明文本。
- 覆盖层里 core.json 写的是 **3.11.5**，而包名与 docs 都称 3.11.4 —— 与第一节「两份服务端」一致：覆盖层是面向 3.11.5 特别优化版线准备的，3.11.4 安装是否被它覆盖到需按实例逐个确认（**待确认**）。

### 5.2 设置载体（第二个 [1]前置 目录）

位置：mods\[1]前置-《生活在诺文斯克》好奇猫自用游戏设置及按键\（comments：对游戏界面有自己习惯请关掉此项，否则进入游戏将只有左下角蹲姿体力槽）

| 内容 | 数量 | 说明 |
|---|---|---|
| user\sptsettings\ | 5 个 ini（Control / Game / Graphics / PostFx / Sound） | 按键 / 游戏 / 画面 / 后处理 / 声音 |
| BepInEx\config\ | 7 个 GamePanelHUD cfg（Core / Compass / Grenade / Health / Hit / Kill / Weapon） | 界面 HUD 布局与开关 |
| meta.ini | 1 | MO2 元数据 |

> 这一层是「个人习惯」性质：与界面/按键习惯冲突时整项关掉，不影响玩法 mod；关掉后界面会退回默认布局。

### 5.3 bot 修复补丁 v1.0（2026-09-20，覆盖更新，不动存档结构）

补丁包：E:\Game\EFT_Offline\【生活在诺文斯克】v0.3.3-bot行为优化补丁_2026-09-20\【生活在诺文斯克】bot行为修复补丁_v1.0_2026-09-20\（实测存在：mods\、profiles\、安装说明.md 3,658 B、SHA256SUMS.txt 1,244 B）

| # | 修复项 | 关键数字 / 证据 |
|---|---|---|
| 1 | LootingBots 空手扫描死循环（bot 站桩） | 修复后恢复普通 bot 巡逻与搜刮 |
| 2 | boss 护卫每帧 NRE 刷屏与滑步 | 单场约 4.5 万次；新增护栏 mod FollowerGuard |
| 3 | 价格链 | 堆叠计价、武器缓存错价、市场价未就绪兜底、弹药可拾取、空扫描冷却、每格价值阈值 PMC 1500 / Scav 2000 |
| 4 | SAIN 协同加固 | 互操作闩锁改造、GetItemPrice 类型错配修复、搜刮遇威胁中断（vigilance）全局启用 |
| 5 | friendlyPMC 护栏 | 死跟班解雇清理 + 死 bot 招募拦截；新增 FriendlyPmcGuard |
| 6 | 性能 | 扫描并发默认 3、检测距离 尸体 120 / 容器 100 / 散落 150、空扫描 180s 冷却 |

覆盖文件清单（据补丁安装说明）：

| 包内文件 | 覆盖目标 |
|---|---|
| mods\[6]AI自动搜索物品-LootingBots…\BepInEx\plugins\skwizzy.LootingBots.dll | 原同名文件（覆盖） |
| mods\[6]AI增强-SAIN…\BepInEx\plugins\SAIN\SAIN.dll | 原同名文件（覆盖） |
| mods\[6]AI修复-护卫NRE护栏-FollowerGuard\（整目录） | 新增 mod |
| mods\[6]AI修复-friendlyPMC护栏-FriendlyPmcGuard\（整目录） | 新增 mod |
| mods\《生活在诺文斯克》-单独-客户端MOD-F12设置cfg所在\BepInEx\config\me.skwizzy.lootingbots.cfg | 原同名文件（覆盖） |
| profiles\单机运行整合包\modlist.txt | 可选（与基线一致的档才可直接覆盖） |

自检锚点（BepInEx 日志）：应出现 [FollowerGuard] applied 8/8 patches 与 [FriendlyPmcGuard] applied 2/2 patches；Exception in Agent Update（NRE）计数应为 0。F12 新增项 Maximum concurrent scans 默认 3（弱机 2 / 强机 5，下一场 raid 生效）。

回滚法：删除 FollowerGuard 与 FriendlyPmcGuard 两个 mod 文件夹 → 从 modlist.txt 移除对应行 → 用备份还原 LootingBots.dll / SAIN.dll / me.skwizzy.lootingbots.cfg。

### 5.4 回滚点与外部资产

| 资产 | 字节 | 时间（实测） | 用途 |
|---|---|---|---|
| 3.11.4升级3.11.5-Live-In-Norvinsk特别优化版补丁包.zip | 47,129,481 | 2026-10-13 22:31 | 3.11.4 → 3.11.5 特别优化版 |
| SPT-3.11.4-服务端（升3115后后悔用）.7z | 108,221,807 | 2026-10-13 22:33 | **回滚点**：退回 3.11.4 服务端 |
| SPT特别优化版MO2-Organizer-v2.5.2-Novinsk.zip | 218,159,739 | 2026-10-13 22:24 | 配套 MO2 特别优化版 v2.5.2 |

- 游戏资料（包内 docs/README.md 记载，实测 4 个 PNG 存在）：所有角色通用路线图（2,061,744 B）、腰包价格定价明细（3,169,553 B）、针剂效果明细介绍（1,707,658 B）、难度分级明细表（239,203 B）。
- 工具链（包内 docs/README.md 记载，未复核）：E:\Game\EFT_Offline\整合包制作用的工具\ 下的 Questline generator based on WTTCommonLib、Realism-patch-Generator、Realism-patch-Generator-Csharp、模板MOD、对比、spt-scaffold.exe、spt4服务器mod框架生成器.zip。
- 版本线：v0.3.2.2（现役，MO2 实例 0.3.2.2）→ v0.3.3（+bot 修复补丁 v1.0，2026-09-20，MO2 实例 0.3.3）→ 3.11.5 特别优化版（2026-10-13 补丁包 + 独立 gamePath SPT_3114_Novinsk_Edition）。
- 客户端资产规模（交叉引用）：curated/migration/bundle-311-to-41.md 记 Life_in_Norvinsk 实测 2622 个 bundle，做 3.11→4.1 迁移时以此为基线。

## 六、已知问题与维护要点

1. **包内 docs/README.md 的计数与快照冲突，以快照为准。** 该文档（2026-10-14 盘点）写主档「条目 197（启用 170 / 禁用 26）、分隔符 7」；实测 modlist.txt 为 197 行（含 1 行文件头）→ 196 条目 = 179 mod + 17 分隔符，启用 169 / 禁用 10。差异（分隔符 7→17、禁用 26→10、启用 170→169）疑为早期盘点口径错误或旧档状态，**待确认**。
2. **order.json 107 条已复核**（106 唯一 + Hephaestus 重复一次）；但「历史盘点 110 条」的来源与差额仍**待确认**，不要再用 110。
3. **FollowerGuard 在主档 modlist 为禁用**（单机运行整合包 line 124 以「-」开头），而补丁的另一个新增项 FriendlyPmcGuard 为启用。后果：补丁自检锚点 [FollowerGuard] applied 8/8 不会出现，boss 护卫 NRE 护栏实际未生效。是「有意关闭」还是「勾选遗漏」**待确认**；若按补丁说明行事，应把该行改为「+」。
4. **覆盖层 core.json 是 3.11.5**，与包名/文档的 3.11.4 并存。与第一节两份服务端对照后，合理解释是覆盖层面向 3.11.5 线；但 3.11.4 安装（SPT_3114）是否也被该覆盖层改写，需按实例核验（**待确认**）。
5. **196 个 mods 目录里只有 179 个是真 mod**，另 17 个是 *_separator 伪目录（其 meta.ini 的 version/url/comments 三字段全空；scan 的 modDirsWithMeta=179 正是指这一点）。检索「无描述 mod」时不要把这 17 个分隔符算进去。
6. **一个 mod 名可能横跨两档不同变体**：[5]战局大修-RaidOverhaul 有「已AI增强版本」（default/主档）与「已AI优化」（生化档）两条同功能条目；[6]友方AI系统-friendlypmc 也有未合并版/合并版之分。按名字做交集比对时必须带变体后缀，否则会误判。
7. **生化危机配置 = 丧尸模式分支**：它开 [5]丧尸模式开关、按组纪律关掉 SAIN / LootingBots / Waypoints / BigBrain / That's Lit 等 AI 大类，并缺 [1]前置框架-WTT公共库。与主档共 19 条状态差异（见 2.2），不重复列。
8. **无注释的禁用项不要瞎猜原因**：AimPointCoordinates、EFCL-WelcomeGifts、SimpleCrosshair、AutoRun、SearchBarFix 五条在 meta.ini 里没有任何禁用理由，统一标待确认；其中 SearchBarFix 的「同功能二选一」只是从命名推断。
9. **bot 补丁的适用边界**：补丁说明明确只针对 SPT 3.11.4 / EFT 0.16 基线，不同版本勿混用；补丁本身不改存档结构，但 3.11.5 特别优化版的覆盖层与它并非同一批次（2026-09-20 vs 2026-10-13），混用时按 5.3 的自检锚点验一遍。
10. **id 体系不要混用**：本包 73 条 hubId 全属旧站 hub.sp-tarkov.com，forgeId 为 0 条；与 4.1.6《永远的诺文斯克》的 Forge 编号不可互比（见 curated/packs/README.md 第五节）。
11. **2.2 差异表的统计口径**：以 modlist.txt 的 mod 名（含变体后缀）+ 启用状态为准，不含分隔符；「—」表示该 profile 根本没有这一行。

## 七、证据与复现

```powershell
# 1) 扫描实包 → 结构化快照（只读；本页数字的唯一权威来源）
node scripts/packs/scan-pack.mjs --pack-root "E:/Game/EFT_Offline/Life_in_Norvinsk_v0.3.2" --slug life-in-norvinsk --out .artifacts/packs/life-in-norvinsk/scan.json

# 2) 渲染主档（单机运行整合包）表格
node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/life-in-norvinsk/scan.json --mode summary
node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/life-in-norvinsk/scan.json --mode detail

# 3) 另两个 profile 的完整明细（本页未内联，需要时现场生成）
node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/life-in-norvinsk/scan.json --mode detail --profile "default"
node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/life-in-norvinsk/scan.json --mode detail --profile "生化危机配置"
```

**溯源路径（仅用于核对，不依赖它们阅读本页）**

- 包根：E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2\（mods\ 196 目录、profiles\ 三档）
- 覆盖层：mods\[1]前置-《生活在诺文斯克》修改本体及MOD排序文件和整合包说明文档\{user\mods\order.json、SPT_Data、EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll、STARTHeadless*.bat}
- 设置层：mods\[1]前置-《生活在诺文斯克》好奇猫自用游戏设置及按键\{user\sptsettings\*.ini、BepInEx\config\*.cfg}
- 包内文档：E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2\docs\{README.md 8,727 B、bot修复补丁-v1.0-安装说明.md 3,658 B}
- MO2 实例：C:\Users\Winde\AppData\Local\ModOrganizer\SPT3.11.4《生活在诺文斯克》0.3.2.2\ModOrganizer.ini（gameName=SPT3、gamePath=SPT_3114、version=2.5.2、selected_profile=单机运行整合包）
- 快照：.artifacts/packs/life-in-norvinsk/scan.json（capturedAt 2026-10-31T17:13:15Z；526,808 B）

**交叉引用**

- [整合包矩阵索引](README.md) —— 四包关系、共有底座 41 组、Life↔Exclusive 名称交集 95 组、Life↔NF 59 组、三套 id 口径
- [3.11→4.1 bundle 分布](../migration/bundle-311-to-41.md) —— Life_in_Norvinsk 实测 2622 个 bundle
- [移植候选评估](../migration/porting-candidate-assessment.md) —— 4.1.6 侧的重建/移植判据
- [MO2 SPT 版本误判](../operations/mo2-spt-version-misdetection.md) —— MO2 识别 SPT 版本的坑
- [Forge 更新日志](../operations/forge-update-log.md) —— 上游 id 生态变化