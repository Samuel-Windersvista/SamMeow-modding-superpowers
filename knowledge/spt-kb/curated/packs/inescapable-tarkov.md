---
version: [5.0]
domain: both
topic: pack-profile
source: curated
title: "《无法逃离塔科夫》Inescapable Tarkov 包构成档案（SPT 5.0 · Alpha）"
keywords: [Inescapable Tarkov, 无法逃离塔科夫, SPT5, pack-profile, ITBS, Softcore, BettersNorvinskQoL, DebugToolkit, MO2, 自建mod, Alpha, 覆盖层]
summary: "《无法逃离塔科夫》Inescapable Tarkov（Alpha）· SPT 5.0 自用包（MO2 2.5.2/SPT5/EFT 1.1.5）：12 mod 目录、12 条条目，启用 11 禁用 1（TrueRealTimeMap），0 分隔符；上游 id 全缺；核心 ITBS 三件 + Softcore + QoL + MCP 工具链；待办 Barter Economy、Samuel's Tweaks、Traveler-PTT。"
---

# 《无法逃离塔科夫》Inescapable Tarkov 包构成档案（SPT 5.0 · Alpha）

> **用途**：本页是仓库内**可离线查询**的包构成档案——不打开 `E:` 实包也能回答「这包里有什么、怎么排序、哪些禁用、覆盖了什么」。全部数字取自 `.artifacts/packs/inescapable-tarkov/scan.json`（扫描快照 `2026-10-31T17:02:01Z`，即北京时间 2026-11-01 01:02），由 `scripts/packs/scan-pack.mjs` 对包根只读扫描产出；表格由 `scripts/packs/render-pack-tables.mjs` 直出，不手抄。实包路径 `E:\Game\EFT_Offline\Inescapable Tarkov` 只作溯源，不在本页承担数据职责；包内文档只有 `docs/README.md` 一篇。

## 一、身份与环境

| 项 | 值 | 证据 |
|---|---|---|
| 包名 | 《无法逃离塔科夫》Inescapable Tarkov（代号 `Inescapable Tarkov_Alpha`） | 包内 `docs/README.md` |
| SPT 版本线 | 5.0（IL2CPP / BepInEx 6；EFT 1.1.5，启动日志 `1.1.5.0.47242`） | scan.json `mo2.gameName` + 包内 README |
| 游戏根 gamePath | `E:\Game\EFT_Offline\SPT_5xx` | scan.json `mo2.gamePath` |
| MO2 gameName | `SPT5` | scan.json `mo2.gameName` |
| profile | `Default`（`profiles/Default/modlist.txt`，12 条）；scan 未记当前选中 profile（`selectedProfile: null`） | scan.json / 实包 modlist |
| MO2 版本 | 2.5.2（`portable` 字段未记录） | scan.json `mo2.version` |
| mods 目录数 | 12（12 个目录均带 `meta.ini`） | scan.json `counts.modDirs` / `modDirsWithMeta` |
| 启用 / 禁用 | 11 / 1（12 条 modlist 条目 + 0 个 MO2 分隔符） | scan.json `profiles[0].counts` |
| 实例根 | `E:\Game\EFT_Offline\Inescapable Tarkov` | scan.json `packRoot` |
| 第二注册 | `%LOCALAPPDATA%\ModOrganizer\SPT5.x《测试》`，指向同一 `SPT_5xx`（是否并存使用**待确认**） | 包内 README |

补充：本包是 SamMeow 系自用包，代号 Alpha，以自建 mod 为主；游玩基调见包内 README「本整合包鼓励玩家扮演丹尼斯」。12 条中 11 条可归为自建 / 本仓库工具链（ITBS 三件、Softcore、BettersNorvinskQoL、DebugToolkit、MCP 活跃探针、tarkov-runtime-client-bridge、两条 `[1]前置` 覆盖层、TrueRealTimeMap）；`界面-AccurateCircularRadar` 是上游 `Tyrian-Radar-Standalone` 1.3.4 的 SPT 5.0 移植（是否由本包作者自行适配**待确认**）。

## 二、构成总览

| 分组（MO2 分隔符） | 条目 | 启用 | 禁用 |
|---|---|---|---|
| (未分组) | 12 | 11 | 1 |

> 合计：12 条 mod 条目（启用 11 / 禁用 1）+ 0 个分隔符。

口径说明：

- 分组 = MO2 分隔符（modlist.txt 中以 `_separator` 命名的伪条目）。本包**一个分隔符都没有**，12 条全部平铺在「(未分组)」里，因此不存在「整组启用/禁用」的操作面。
- 分组名称经过 render 脚本归一化（去 `《…》` 前缀、去 `_separator` 后缀），故显示为 `(未分组)`。
- 上游 id 覆盖：`forgeId` 0 / `hubId` 0 / `nexusId` 0（scan.json `counts.modsWithForgeId = 0`，且三列在全部 12 条上均为 `null`）。**本包不存在「Forge 编号 vs 旧 Hub 编号」混淆问题**——它一个编号都没有；唯一外链是雷达条目的 GitHub 仓库 URL。
- `hasServerMod` 在 12 条上全为 `false`，但其中 Softcore 与 MCP 活跃探针明确部署到 `SPT_Runtime\user\mods\`。原因是扫描口径只探测 mod 根下是否存在 `user\` 或 `SPT_Data\`（`scripts/packs/scan-pack.mjs` 第 93–94 行），而部署路径在 `SPT_Runtime\` 子层。**该字段不可用作「有无服务端 mod」的判据**（详见第六节）。

## 三、分组明细（完整 mod 清单）

下表由 `render-pack-tables.mjs --mode detail` 直出，行序 = `modlist.txt` 行序，**未删行**（启用与禁用都在）。「说明」列由脚本截断至 56 字，完整 `comments` / `notes` 见 `.artifacts/packs/inescapable-tarkov/scan.json`，要点抽取见本文第五、六节。

#### (未分组)（11 / 12 启用）

| Mod（MO2 名） | 启用 | 版本 | 上游 | 说明 |
|---|---|---|---|---|
| [5]核心-《无法逃离塔科夫》QoL-BettersNorvinskQoL-0.1.0 | ✓ | 0.1.0 | — | [5]核心-《无法逃离塔科夫》QoL-BettersNorvinskQoL-0.1.0：UIFixes 物品管理 |
| [5]核心-《无法逃离塔科夫》机器人系统-ITBS.Spawn-0.1.0 | ✓ | — | — | [5]核心-《无法逃离塔科夫》机器人系统-ITBS.Spawn-0.1.0：ITBS.Spawn 客户端（Bep |
| [5]核心-《无法逃离塔科夫》机器人系统-ITBS.Perception-0.1.0 | ✓ | — | — | [5]核心-《无法逃离塔科夫》机器人系统-ITBS.Perception-0.1.0：ITBS 感知 PoC（视 |
| [5]核心-《无法逃离塔科夫》机器人系统-ITBS.Core-0.1.0 | ✓ | — | — | [5]核心-《无法逃离塔科夫》机器人系统-ITBS.Core-0.1.0：ITBS 宿主骨架（Contracts |
| [1]前置-《无法逃离塔科夫》修改本体及MOD排序文件和整合包说明文档 | ✓ | — | — | 利用Mo2的文件覆盖机制实现对本体文件的修改 |
| [1]前置-《无法逃离塔科夫》好奇猫自用游戏设置及按键 | ✓ | — | — | 对游戏界面有自己习惯请关掉此项。否则进入游戏将只有左下角蹲姿体力槽 |
| [5]核心-Inescapable-Tarkovs-Softcore-0.1.0 | ✓ | 0.1.0 | — | SPT5 服务端整合 mod：六组功能（Samuel's Tweaks / True Items Redux / |
| 工具-MCP活跃探针-0.1.0 | ✓ | 0.1.0.0 | — | tarkov-runtime-MCP 配套探针：暴露活跃 profile 只读路由，供 MCP 跟随玩家当前使用 |
| 工具-tarkov-runtime-client-bridge-0.1.0 | ✓ | 0.1.0.0 | — | tarkov-runtime-MCP Phase 2 客户端桥：只读暴露局内玩家位置（GET /raid/pla |
| 界面-AccurateCircularRadar-1.3.4-spt5.1 | ✓ | 1.3.4-spt5.1 | github.com | Accurate Circular Radar 1.3.4（SPT 5.0 移植版）：圆形雷达 HUD，显示敌人 |
| 界面-TrueRealTimeMap-0.1.0 | — | 0.1.0 | — | TrueRealTimeMap client plugin (SPT 5.0 skeleton). GUID c |
| 工具-SamMeow.DebugToolkit-0.1.0 | ✓ | 0.1.0.0 | — | SPT5 调试工具包：统一 F12 配置，整合 Tooltip 调试信息 + Bot 覆盖层 + 无敌组；整合自 |

## 四、排序规则与要点

### 4.1 加载序语义：越靠后优先级越高

- MO2 `modlist.txt` 的行序即优先级序：**越靠后的行优先级越高**，同名文件由后写者覆盖先写者。依据：本仓库工单 `docs/wayfinder/tickets/009-migrate-3114-to-spt41.md` 固化规则「`modlist.txt` 顺序 = 无前缀(0) → `[14]` → … → `[2]` → `[1]` 数字降序；`[1]` 前置在最底部 = 最高 priority」。本包沿用同一读法；**本包自身未用 MO2 界面逐条复核**（包内 README 亦标「待确认」），故该语义在本实例上仍标**待确认**。
- 本包事实（scan.json 分组顺序 = modlist 行序，已与实包 `profiles/Default/modlist.txt` 核对一致）：首行（`#` 注释之后）是 `[5]核心-…QoL-BettersNorvinskQoL`（最低优先级），末行是 `工具-SamMeow.DebugToolkit`（最高优先级）。
- 功能性后果：12 条中唯一共享同一程序集的是 ITBS 三件——Perception 的 `comments` 写明「`ITBS.Contracts.dll` 由 ITBS.Core overlay 提供」，而 Core 在行序上排在 Spawn / Perception **之后**（优先级更高），与该声明同向。若将来把 Core 前移，需同步核对 Contracts 的覆盖来源。
- QoL 位于最低优先级，打包时不会覆盖其后任何条目；反过来，后续所有条目都可覆盖 QoL 的路径。

### 4.2 `[N]` 前缀：只剩分类语义，不再是层级规则

- 前缀实际只有两档：`[1]前置`（2 条）、`[5]核心`（5 条）；另有 `工具`（3 条）、`界面`（2 条）不带数字。
- 本包**未采用** 4.1 时代 87 overlay 的「`[14]`→`[1]` 数字降序」层级排序（该规则见工单 009，且在那里已明标为历史）。这里 `[N]` 只标注类别，真实优先级完全由 modlist 行序决定。
- 行序与 `[N]` 数值**不单调**：`[5]核心` 被 `[1]前置` 从中间切开（QoL/ITBS 三件 → `[1]`×2 → Softcore），`工具` / `界面` 又穿插其间。按前缀数值推断优先级会得到错误结论。

### 4.3 前置沉底：本包未遵循

- 工单 009 规约是「`[1]` 前置写在最底部 = 最高优先级」。本包 2 条 `[1]前置` 落在条目位次 5–6（文件第 6–7 行，首行为 MO2 自动生成的注释行；中部），既不在底也不在顶。
- 后果：两个本体覆盖层不会「最后压上」，其后的 `[5]核心`（Softcore）、`工具`、`界面` 条目在文件树同名路径上可以覆盖它们。若出现「本体文件被 mod 反向覆盖」类症状，先查这一点。
- 是否有意为之，包内文档无记载（**待确认**）。

### 4.4 分组开关与重开档

- 分隔符 0 → **没有分组级开关**。要停一个功能只能逐条 toggle（改 modlist 行前缀 `+` / `-`），无「一键停核心组」的余地。
- 启动生效层面：改完 `modlist.txt` 必须重开 MO2 注入（重开游戏 / 服务器）才生效，仅保存文件不重载 USVFS——这是 MO2 通用语义，包内文档未复述。
- 服务端 Softcore 的配置改动，按该条 `notes` 为「单一 JSON 配置、**重启生效**」；哪些改动需要**重开存档**（经济 / 藏身处免 FIR / 战局时长是否只对新档或新战局生效），`meta.ini` 与包内文档均无记载（**待确认**）。验证面清单可参照 `curated/migration/pilot-experience-inescapable-softcore.md`（服务端 mod 只有上真服务器才能验证的那批点）。
- 12 条中无 Fika / 联机相关条目，也无需重开档的独立「开局」类 mod；ITBS 的编排门按注释默认全关，不改正常游戏。

### 4.5 禁用项：仅 1 条，原因未记载

- `界面-TrueRealTimeMap-0.1.0`（modlist 行前缀 `-`）是本包唯一禁用项。
- `meta.ini` 只给出：「TrueRealTimeMap client plugin (**SPT 5.0 skeleton**)，GUID `com.sammeow.truerealtime.map.client`」，`notes` 记「Deployed by `scripts/build-deploy.ps1` → `BepInEx/plugins/TrueRealTimeMap.Client/`」，`url` 与三个 id 皆空。
- 禁用原因在 `meta.ini` 与包内文档中**均无记载 → 待确认**。可确认的线索只有两条：版本 0.1.0 + `skeleton` 自述，即它本来就是骨架态部署件（未完工），这是「被禁用」的合理背景，但**不是记载过的原因**。

### 4.6 `category` 字段（与排序无关）

- `meta.ini` 的 `category` 只有三种取值：`-1,`（7 条）、`0`（2 条：QoL / Softcore）、空（3 条：ITBS 三件）。按 MO2 惯例 `-1` 表示未分类，本包未逐项复核实包分类名（**待确认**）。
- 该字段是 MO2 左栏的分组标签，与 modlist 优先级是两套独立机制；改它不会改加载顺序。

## 五、本体修改 / 覆盖层 / 特殊资产

| 层位 | 具体内容 | 证据 |
|---|---|---|
| 本体覆盖层 A：`[1]前置-…修改本体及MOD排序文件和整合包说明文档` | 以 MO2 文件覆盖机制改本体文件；该 mod 同时被当作「排序文件 + 整合包说明文档」的载体，但目录内**没有文档文本**（名不副实，见第六节） | `meta.comments`「利用Mo2的文件覆盖机制实现对本体文件的修改」；包内 README |
| 本体覆盖层 B：`[1]前置-…好奇猫自用游戏设置及按键` | 覆盖游戏界面设置与按键绑定；启用后进入游戏「只有左下角蹲姿体力槽」，有自定界面习惯者应关掉此项 | `meta.comments` 原文 |
| 服务端整合层：`Inescapable-Tarkovs-Softcore-0.1.0` | 部署 `SPT_Runtime\user\mods\com.sammeow.inescapable-softcore`（DLL + `config.json`）；六组功能（Samuel's Tweaks / True Items Redux / 反重力臂章 / 更大的背包 / 藏身处免 FIR / Softcore 经济与制造大修）+ 战局时长控制；另有客户端插件 `com.sammeow.inescapable-softcore.client` | `meta.notes` v0.1.0 / r2 |
| 启动器背景（已弃） | v0.1.0 首发曾带 `SPT_Runtime\SPT_Data\images\launcher\bg.png`（G1 背景，`customBackground` 开关）；r2 已移除，master 弃 bg 链 | `meta.notes` r2 第 ④ 条 |
| ITBS 共享程序集 | `ITBS.Contracts.dll` 由 ITBS.Core overlay 提供；Spawn / Perception 只带自身程序集，BepInEx / Harmony / interop **由运行时提供**（覆盖层不重复发货依赖） | 三条 ITBS 的 `comments` / `notes` |
| Bot 相关补丁面 | ① ITBS.Perception：视觉系数 Postfix + 单容器交易（独立补丁面，不依赖宿主）；② ITBS.Spawn：生成面 PoC + 生成编排第一段（只读点位缓存 + Scav 点位距离过滤，R12 门后、编排门默认全关）；③ Softcore：PMC 尸体 ArmBand 槽（服务端三字段解锁 + PMC 臂章强制 100%）；④ DebugToolkit：Bot 覆盖层 5 模式（F10/F11、敌我颜色、Minimized 隐藏档） | 各条 `comments` / `notes` |
| 物品 / 经济数据面 | Softcore r2 刷新 7 个数据文件：`armbands`（臂章 22→37）、`barter`、`clothing`、`medicals`、`provisions`、`backpacks`（38）、`scavcase`；安全箱 15 员家族映射、true items 扩展、data 盘优先 | `meta.notes` r2 第 ①③ 条 |
| 只读观测端点（外部工具链） | MCP 活跃探针：`POST /spt/runtime/active-profiles`（返回近 30 分钟活跃 profileId）；client-bridge：`GET http://127.0.0.1:49777/raid/player`（仅绑 127.0.0.1，只读） | 两条 `notes` |
| 二进制指纹（可校验） | QoL：`BepInEx\plugins\com.sammeow.betters-norvinsk-qol\BettersNorvinskQoL.Client.dll` SHA256 `A8989C6BC18D069800072BB498325715DD1FC53A18557A9D5F9EA15A63DEC7BB`；Softcore server `615B236A…` / client `1AD694FB…`（`meta.notes` 只留前 8 位） | `meta.notes` |
| 回滚点 | ① Softcore 旧数据备份：`D:\Temp\opencode\its-deploy-backup-20260928-003604\data-old`；② QoL 历史版本按提交重建（`40fe316`→`91708D55` / `de580ec`→`94B0AFFA` / `0e2198b`→`F9472DC0` / `e457e9e`→`B162CD76`）；③ 部署规约：**DLL 始终覆盖，`config` / `data` 不覆盖玩家编辑**（本次数据升级属修复陈旧文件，旧版已备份） | `meta.notes` |
| 外部资产 | 雷达上游 `https://github.com/Leonana69/Tyrian-Radar-Standalone`（1.3.4，SPT 4.1.3 → 5.0 移植）；QoL 源码仓 `E:\云文件\GitHub\SamMeow-Inescapable-Tarkov-Betters-Norvinsk-QoL`（`m1` @ `e4c60a1`）；Softcore 构建源 GitHub `SamMeow-Inescapable-Tarkovs-Softcore`（master @ `db65411`） | `meta.url` / `meta.notes` / 包内 README |
| 未落盘项 | 覆盖层 A 具体改了哪些本体文件、B 具体改了哪些设置键：scan.json 只记 `meta.ini`，**不含文件清单 → 待确认**（核对方式：列实包该 mod 目录树） | scan.json 口径 |

## 六、已知问题与维护要点

| # | 问题 | 影响 / 风险 | 依据 | 处置建议 |
|---|---|---|---|---|
| 1 | 实例内零说明文档：`[1]前置-…整合包说明文档` 承诺文档，目录内无任何文档文本 | 新人/离线无法就包内自述了解构成；名不副实 | 包内 README「已知缺口」 | 以本页 + scan.json 顶替；补文档或改名 |
| 2 | 笔记侧版本标签过时：两篇笔记 `version` 标 `[3.11]`，正文写 SPT4.0.x，环境实为 SPT5 | 检索与版本判断失真 | 包内 README 第 3 节 | 修正笔记 frontmatter 标签，正文改 5.0 |
| 3 | 版本号仍为 `Inescapable Tarkov_Alpha` | 无稳定发版语义，难以对齐变更 | 包内 README | 转正式版前先定版号与变更记录 |
| 4 | 上游 id 覆盖 0 / 12（`forgeId`、`hubId`、`nexusId` 全空） | 无法自动追踪上游更新；第三方件与自建件不可机读区分 | scan.json `counts.modsWithForgeId = 0` | 为雷达等第三方条目补 id；自建件在 `meta.url` 记源码仓 |
| 5 | 5 条 `version` 为空（2 条 `[1]前置` + ITBS 三件）；ITBS 版本只存在于目录名与 `notes` 里的「版本 0.1.0」 | 版本不可机读，脚本无法比对 | scan.json `mods[].version` | 回填 `meta.ini` 的 `version`（ITBS 记 0.1.0、契约 ContractVersion 0.0） |
| 6 | `工具-SamMeow.DebugToolkit` 的 `notes` 被存成 Qt 富文本 HTML（`<!DOCTYPE HTML…`） | 元数据不可读，污染档案与 diff | scan.json `meta.notes` 原文 | 清洗为纯文本三模块清单（Tooltip / Bot 覆盖层 / 无敌组） |
| 7 | `hasServerMod` 全 12 条为 `false`，但 Softcore、MCP 探针实为服务端 mod | 任何按该字段筛「服务端 mod」的脚本会漏 | 扫描口径只查 mod 根 `user\` / `SPT_Data\`（`scan-pack.mjs` 93–94 行），实际部署在 `SPT_Runtime\` 子层 | 判服务端面看部署路径，不看该布尔 |
| 8 | 单组无分隔符，12 条平铺 | 无组级开关；批量调整时易误伤 | scan.json `separators = 0` | 改动前备份 `modlist.txt`；按 `[N]` 前缀补分隔符 |
| 9 | QoL 待复测：本版 = 工单 05（组拖动 + 框选双修）+ 04 + 02，8 类已知降级项 | 拖拽预览 / 局部成功门等行为可能不达预期 | QoL `meta.notes` | 按工单 05 的复测清单跑：组拖动 / 槽位 / 检视页 / 回归 |
| 10 | Softcore 曾有构建事故：r1 基线误用 `softcore-chain`（落后 master）、`copy-if-missing` 未刷新数据 | 首版数据陈旧；已被 r2 取代 | Softcore `meta.notes` r1/r2 | 构建前校验 master 基线（`db65411`）；数据升级走「先备份再覆盖」 |
| 11 | 双注册：`%LOCALAPPDATA%\ModOrganizer\SPT5.x《测试》` 与实例根同指 `SPT_5xx` | 两个实例可能交替改同一游戏目录，状态互相污染 | 包内 README | 确认是否并存使用（**待确认**）；建议只保留一个入口 |
| 12 | 待办功能缺口（更新日志 260319）：Barter Economy、Samuel's Tweaks、Traveler 或 PTT 尚未移植 | 包定位的未完成项；旧清单里已勾选的「反重力袖章」已随 Softcore 生效，而「CustomProfiles（4.0 线）」在 12 条 modlist 中**找不到对应条目**（是否另有载体待确认） | 包内 README 第 4 节；scan.json 12 条清单 | 移植候选评估见 `curated/migration/porting-candidate-assessment.md` |

维护要点（一句话版）：**改 modlist 先备份、改 DLL 先关游戏/服务器/启动器、每次数据升级先备份旧版、判服务端面看部署路径而不是 `hasServerMod`。**

## 七、证据与复现

扫描（只读，产出本页唯一数字来源）：

    node scripts/packs/scan-pack.mjs --pack-root "E:/Game/EFT_Offline/Inescapable Tarkov" --slug inescapable-tarkov --out .artifacts/packs/inescapable-tarkov/scan.json

渲染（本页第二、三节表格的实际来源，未手抄）：

    node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/inescapable-tarkov/scan.json --mode summary
    node scripts/packs/render-pack-tables.mjs --scan .artifacts/packs/inescapable-tarkov/scan.json --mode detail

快照元信息：`slug=inescapable-tarkov`、`capturedAt=2026-10-31T17:02:01.009Z`、`counts={modDirs:12, modDirsWithMeta:12, modsWithForgeId:0}`、`profiles=[Default]`。

实包路径（**仅溯源用**，本页不依赖其在线可读）：

- 包根：`E:\Game\EFT_Offline\Inescapable Tarkov`（含 `ModOrganizer.ini`、`.mo2-mcp`）
- 排序与启用态：`E:\Game\EFT_Offline\Inescapable Tarkov\profiles\Default\modlist.txt`（13 行 = 1 行注释 + 12 条）
- 版本/路径：`E:\Game\EFT_Offline\Inescapable Tarkov\ModOrganizer.ini`（`gameName=SPT5` / `gamePath=E:\Game\EFT_Offline\SPT_5xx`）
- 随包文档：`E:\Game\EFT_Offline\Inescapable Tarkov\docs\README.md`（本页唯一包内文字来源）

相关页：

- `curated/migration/pilot-experience-inescapable-softcore.md` — Softcore 六组 TS→C# 整合经验与部署期验证点
- `curated/migration/pilot-experience-qol-consolidation.md` — QoL 整合（UIFixes 收敛）经验
- `curated/migration/pilot-experience-spt41-debugtoolkit.md` — DebugToolkit 迁移经验
- `curated/operations/5xx-client-mod-dev-lessons.md`、`curated/operations/5xx-server-mod-dev-lessons.md` — 5.x 客户端/服务端开发教训
- `curated/api-notes-5.0/` — 5.0 API 面（mod 加载 / 配置 / 路由 / DI）
- `docs/wayfinder/tickets/009-migrate-3114-to-spt41.md` — 排序规则与 `[N]` 前缀语义的原始出处（明标历史）
