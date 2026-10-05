# 导言：素材、方法与本次补充

本文是「塔科夫剧情统一蒸馏」的开篇，任务是用 SPT 5.x 本地游戏数据（一手文本）补齐现有 lore 分区，并为后续章节建立引用口径。

**素材与方法**。本次蒸馏的主源是 SPT 运行时的任务与叙事数据，位于 `SPT_Runtime/SPT_Data/database/templates/` 与 `database/locales/global/ch.json`：`quests.json`（localization.ch 任务文本与条件文本）、`dialogue.json`（对话字幕）、`mainQuestNotes.json`（主线笔记结构）、`tapes.json`（音频日志）、`endings.json`（结局）、`subtitleTracks.json`、`questChains.json`（支线链）、`archivedQuests.json`。第一阶段提取覆盖 786 个任务（主线 192 / 支线 594），主线叙事并集覆盖 138/192，未覆盖 54 个（多为章节内无名称的分步任务）。方法为直接读取 localization.ch，缺失时以 en 兜底并标 `[en]`；不二次翻译，不补写原文没有的信息（口径见 `.scratch/lore-distill/coverage.md`、`SECTION-SPEC.md`）。

**与现有 lore 分区的关系**。仓库现存的 `knowledge/spt-kb/curated/lore/` 是社区整理的世界观底稿，上游为 Fandom Wiki、Wikipedia、NamuWiki 与 eftarkov.com；它给出了区域、阵营、时间线、TerraGroup、地图、商人、章节的骨架，但缺少任务级一手文本与支线全景。本文补的正是后者：以 KB 为骨架、以 SPT 客户端文本为血肉，并对外部简报中可回指官方的结论做交叉校验。凡 KB 未覆盖处标为缺口。

**引用体系**。全文使用四类标注：

- `quests_joined.json#<questId>`：任务级（名称/描述/条件/结语）。
- `quests.json#<noteId>`：主线笔记文本，抽取自 `<noteId> questNoteText`，汇总见 `.scratch/lore-distill/work/main-story-text.md`。
- `<file>#<id>`：其他叙事文件，如 `dialogue.json#<id>`、`tapes.json#<id>`、`endings.json#<系统名>`。
- KB 文件路径（如 `knowledge/spt-kb/curated/lore/01-world-overview.md`）或 `[外部: URL]`；外部结论按简报可信度附（官方）/（非官方），无法回指官方者标 `[未验证]`。

**可信度说明**。SPT 本地数据是客户端内文本，属一手但仅代表游戏呈现；KB curated 是社区整理，上游多为非官方；外部简报中仅官方论坛 patch notes 与官方认可 wiki 的结论标（官方），其余标（非官方）。

# 一、世界观与背景（整合与补充）

## 区域

《逃离塔科夫》发生在俄罗斯西北部虚构的 **Norvinsk 特别经济区（SEZ）**：它是俄罗斯与欧洲之间的门户地带，包含 Norvinsk 与 Tarkov 两城及其附属基础设施；该区域在俄罗斯境内享有特殊法律地位，并为国内外企业提供优惠经济条件。优惠条件既吸引了守法企业，也引来了动机可疑的跨国公司。Tarkov 的一家跨大西洋公司成为政治丑闻的 ground zero，六个月后政治对峙升级为涉及联合国维和部队、内务部内卫部队及两家 PMC 的武装冲突，区域边界随后被封锁。[外部: https://escapefromtarkov.fandom.com/wiki/Norvinsk_region]（官方认可 wiki，引游戏内描述）两座核心城市：物流与交通中心 **塔科夫（Tarkov）**（多数地图所在地），与通过芬兰湾大桥相连的 **诺文斯克（Norvinsk）**；陆地边界由内务部队把守、海岸由波罗的海舰队巡逻（此细节为社区整理）。[来源: `knowledge/spt-kb/curated/lore/01-world-overview.md`] 官网无静态文本，未能与官网原文逐字核对。

冲突的**起源地**是塔科夫市中心 **Ground Zero**：官方 patch notes 称其为 TerraGroup 的 "main Russian branch office"（俄罗斯主要分公司办公室），而游戏内描述与官方认可 wiki 则记为 "where TerraGroup was headquartered"（TerraGroup 总部所在地）；两种表述指向同一设施，可并存使用，USEC PMC 与 OMON（内务部特警）在此发生最激烈交火。[外部: https://forum.escapefromtarkov.com/topic/176591-patch-notes-for-01400/]（官方）[外部: https://escapefromtarkov.fandom.com/wiki/Ground_Zero]（官方认可 wiki）

## 阵营

塔科夫的冲突不是国家间战争，而是**俄罗斯政府（代理 BEAR）vs 跨国企业 TerraGroup（代理 USEC）**的代理战争，另有第三方混战。势力谱系如下：[来源: `knowledge/spt-kb/curated/lore/03-factions.md`]

| 势力 | 阵营 | 定位 |
|---|---|---|
| USEC | TerraGroup | 西方 PMC，销毁罪证、保护公司财产 |
| BEAR | 俄政府 | 前苏特种部队军官，收集 TerraGroup 罪证 |
| Scavs | 中立 | 本地幸存者武装，四大帮派 + 两独特帮派 |
| Rogues | 前 USEC | 盘踞灯塔水处理厂，对 BEAR 敌对 |
| Cultists | 中立 | 夜间活动、用毒刃的神秘教团 |
| UNTAR / RUAF | 俄方/维和 | 联合国维和部队 + 俄武装部队，执行封锁 |
| Black Division | TerraGroup | 集团内部精锐特种部队 |

官方已确认**派系敌对关系基于 lore**：AI BEAR 遇 AI USEC 比遇同阵营更可能交火。[外部: https://forum.escapefromtarkov.com/topic/182464-patch-notes-for-01500/]（官方）新 Boss **Kollontay**（前 MVD 军官，丑闻后组帮抢劫）与 **Partisan**（痴迷绊雷的老兵，与 Jaeger 气味相投）的背景亦出自官方 patch notes。[外部: https://forum.escapefromtarkov.com/topic/182464-patch-notes-for-01500/]（官方）官方认可 wiki 已证实 Scavs 的四个主要帮派（Grizzle / Pashutin「Fiend」/ Zhilnov「Yarik/Yaga」/ Stoporenko「Lawyer」）与两个独特帮派（由 Shturman 领导的 Svetloozersk Gang、由 Reshala 领导的 Zavodskoy Gang）；Cultists 正式名为「Witnesses of the Arrival」，由祭司（Zhrets）与 2-4 名信徒组成，夜间 22:00-7:00 活动，祭司总血量 850。[外部: https://escapefromtarkov.fandom.com/wiki/Scavs]、[外部: https://escapefromtarkov.fandom.com/wiki/Cultists]（官方认可 wiki）

## 时间线

可证实的冲突前史锚点：USEC 于 1999 年由 KerniSEC 与 Safe Sea 合并成立；2004 年 TerraGroup 国际控股与 USEC 建立联系，后者实质成为该集团的私人军队；BEAR 由俄罗斯联邦政府秘密法令组建，作为对 TerraGroup 在俄境内非法活动的反制，但未见官方年份。[外部: https://escapefromtarkov.fandom.com/wiki/USEC]、[外部: https://escapefromtarkov.fandom.com/wiki/BEAR]（官方认可 wiki）以下年份为社区整理推测，未见官方年份：2009 年化工厂 16 号土地被非法出售（工厂地图前身）；2015-2018 SEZ 繁荣期；2017-2018 BEAR 组建；2018-2019 丑闻爆发、政治对峙约六个月；2019-2020 升级为武装冲突（UNTAR + MVD + 两家 PMC）；约 2021 区域最终封锁、PMC 滞留、Scav 控制城区；此后游戏主线开始。[来源: `knowledge/spt-kb/curated/lore/02-timeline.md`] 官方对具体年份刻意模糊。

## TerraGroup 与关键组织

**TerraGroup** 是跨国控股集团，子公司 40+、分支遍布 120+ 国家，是冲突的核心反派；英国注册的 **TerraGroup Labs PLC** 名义上做农业生物技术，实际在 Norvinsk 深度卷入非法活动；格言 "Vires in Scientia"（力量源于科学）曾出现在 UNTAR 文件上，暗示对维和部队的渗透。[来源: `knowledge/spt-kb/curated/lore/04-terragroup.md`] 集团设施包括中心区总部（KB 作"总部"，层级待核）、市中心地下实验室（The Lab，官方层面"不存在"）、海岸线转场的"迷宫"、化工厂 16 号、灯塔新区。[来源: 同上]

SPT 客户端文本进一步补出两个关键组织与线索：**无名者（The Nameless Ones）**——Kerman 称其为 TerraGroup 背后的实际操纵者，办公纸上留有"无名者的意志"与用于"净化"的燃料留言，且已渗透政府机构；**A.P.**——来自 TerraGroup 总部、甚至可能是更高层级机构的专家，在"蓝冰"协议中获最高权限。[来源: `quests.json#6911dcd8466e028dca0d0d94`、`quests.json#6911dd02277e44fe9f0507cc`、`quests.json#6911dcc08b6f9666a8006fd4`、`quests.json#6911dede461b770ecd071606`] 音频日志录下 A.P. 主持的紧急撤离会议：与外界通讯中断、销毁敏感数据、员工经疗养院转场，且"到达疗养院后与 USEC 的合作立刻终止"，改由"新承包商"接手。[来源: `tapes.json#68bfea81ace495db5dd420fe`、`tapes.json#68c004eb1f214220734a84b8`、`tapes.json#68c00644a8cf72f81458aed6`、`tapes.json#68c006621e64ef2cfcc8a6fe`]

终局要求收集 TerraGroup 重大罪证（9 选 8）：犯罪证据录音带、守望者文件、间谍网络报告、"迷宫"研发报告、#1156 项目规格、ARRS 系统规格、燃料催化剂测试报告、A 女士谈话记录、破冰船数据库文件。[来源: `knowledge/spt-kb/curated/lore/07-story-chapters.md`] 其中"守望者"、A 女士的具体身份仍是缺口。
