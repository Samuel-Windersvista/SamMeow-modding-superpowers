# Forge 4.1.x mod 生态速览与「颠覆潜力」清单

> 数据快照：**2026-10-11** ｜ 来源：**sp-mod.com API v0**（当前 Forge 主机；旧 forge.sp-tarkov.com 已下线）
> 配套数据：`knowledge/spt-kb/archive/forge/api/mods-catalog.json`（全量 1969）、`compat-4.1x.json`（4.1.x 兼容 600）
> 分析工作区：`.scratch/forge-4.1x-survey/`（脚本文档与中间数据）

---

## 0. 数据口径（先读）

- 全量 mod：**1969**。命中「4.1.x 兼容」（服务端 SemVer 求值 `filter[spt_version]=~4.1.0`，即 `>=4.1.0 <4.2.0`）的：**600 个**。
- 补丁分布（服务端权威求值，非猜测）：

| 集合 | 数量 |
|---|---|
| 支持 **4.1.5** | 502 |
| 支持 **4.1.6**（4.1 线最新版，2026-09-18 发布） | 547 |
| 两者都支持 | 486 |
| 仅支持 4.1.6（不支持 4.1.5） | 61 |
| 仅支持 4.1.5（未标 4.1.6） | 16 |
| 仅早期补丁（如 `~4.1.0 <4.1.4`） | 37 |

- **版本注意**：4.1.6 已成为社区主要目标（547/600）。若你的环境仍锚定 4.1.5 基线：本报告对「仅 4.1.6」或「仅 4.1.5」的特例做了显式标注，未标注条目默认两个补丁都支持（精确的逐条判定请查 `compat-4.1x.json`）。
- 其他字段说明：下载量 = Forge 记录累计下载（热度指标，不代表质量）；`fika` = 「任一已发布版本被确认兼容 Fika」（411/600）；`ads` 标记 25 个；档案绑定提示 183 个；`cheat_notice` 5 个。
- 数据限制：清单接口只返回每个 mod 的**最近若干个版本**（官方文档称最近 6 个，实测最多 10 个），非完整历史；本报告的兼容性判定用的是服务端补丁筛选（不受该限制影响）。
- 本报告只收录数据集内确认存在的条目。几个未跟进 4.1 的老牌 mod 供对照：MoreCheckmarks（约束 `~4.0 <4.1.0`）、KmyTarkovApi（`~3.11`）、ALP - Algorithmic Level Progression（止步 3.11）。

---

## 1. 「颠覆潜力」：能改变玩法形态的 mod

### 1.1 合作联机：SPT 变联机平台

- **Project Fika**（#2326，98k 下载）+ **Project Fika - Server**（#2357）——给 SPT 加大厅与联机能力：和朋友同局打 AI（PMC/Scav/Boss）、共享物品与任务进度；默认不改变原版体验。
- 生态位：#2449 **Fika Headless Launcher**（无头启动器）、#2583 Fika Discord Presence、#3122 **Fika Downed Spectator**（倒地观战）、#2857 Raid Settings Skipper、#2106 Quests Extended。
- 覆盖面：600 个 4.1.x mod 中 **411 个**带 Fika 兼容标记。
- 颠覆点：SPT 从「单机私服」变成「小型 coop 平台」。注意：联机对 mod 组合一致性要求高；Fika 有独立安装指南与支持渠道（Discord）。

### 1.2 内容大回移：把正作 1.0 搬进 SPT

- **WTT - Content Backport**（#2512，554k）——1.0 的新武器/配件/装备、角色定制（头/服装/语音）、藏身处升级与装饰、食物/医疗、新结局场景与 Miami 环境、WTT 成就体系。⚠ 带 ads 标记与档案绑定提示。
- **地图回移（Manimal 系列）**：#2866 **Icebreaker 整图回移**（70k，含解锁任务）、#3025 Interchange 扩展（Master of Ultra）、#3022 Lighthouse 返修（含 BTR 新路线与出租车）、#2937 Labs Office、#2681 装弹动画、#2745 看表动画、#2864 CS 毒气弹、#3129 序章教程回移。
- **任务回移**：#3026 **Quest Backport**（约 272 个任务的击杀/地图/交付条件对齐重写）、#2998 Original Story（从「Journey to Tarkov」到结局的剧情线 + 结局奖励）、#1538 Ref - SPT Friendly Quests（竞技场任务转 SPT，309k）。
- 颠覆点：4.1 与正作的内容代差被系统性抹平；代价是「内容层」的长期跟随维护。

### 1.3 开放世界 / 无限战局 / 领土战争（实验性）

- **Vagabond**（#2642）——开放世界化改造：只靠 transit 移动，商人与藏身处各自独立撤离点，从 Fence 低配起步（灵感：Path to... ）。7k 下载。
- **Raids Are Infinite**（#2903）——所有地图撤离时限拉到 99999 分钟，局内时钟改为墙钟。7k。
- **Open World Map**（#3073）——把地图物理拼接（作者自述早期艺术件：无 loot / bots / 任务）。168 下载，尝鲜。
- **Tushonka Territories**（#2942）——派系领土战争：Raiders/Rogues/Scavs 动态争夺地图控制权，有声望系统，玩家行为可影响战局。
- 颠覆点：「不是塔科夫」方向的实验。彼此都动 transit/撤离，组合需谨慎。

### 1.4 硬核 / Ironman / Roguelike 化

- **Ironman**（#2965）——三种 Ironman 档案（Standard/Ultimate/Hardcore）：死亡损失物品与货币、技能 XP 掉级、商人与治疗限制；Ultimate 以上每天一次「降级」抉择。
- **Bronzemanmode**（#1623）——真·零到英雄：商人/跳蚤商品靠撤离或任务解锁（注意：新版本因 Forge 政策转到 GitHub 维护）。
- **Hardcore Rules**（#784）——DeadlySlob 式硬核规则集，专用档案版本，切换档案即切换规则。
- **TrueHardcoreRules**（#3120）——无跳蚤、无保险、贵而稀的商人（4.1.6）。
- **Hardcore Standard Profile**（#3102）——STALKER 式开局：一把 AKS-74U + 一把 PM。
- 颠覆点：沙盒变成「有规则的游戏模式」，可与经济/季节/随机化 mod 叠出自己的生存赛季。

### 1.5 AI 模拟升级：让 raid「活」起来

- **ORBIT 2.0**（#2706，54k）——目标驱动 raid 智能：每队 bot 有清剿区/交战热点/任务点计划，沿途搜刮、升级装备、打完撤离；「Ghost Mode」让远处 bot 休眠省 CPU（Phobos 血统）。
- **PIT Fireteam**（#2676）——可自定义、可局内招募、可指挥的友方小队。
- **Miyako Carry Service**（#2709）——AI 队友生成器（兼容 SAIN/Fika）。
- **经典核心栈**：SAIN（#791，1.32M）、BigBrain（#902）、Waypoints（#827）、Looting Bots（#812）、Questing Bots（#1109）、APBS（#1594）、ABPS（#2097）、MoreBotsAPI（#2426）。
- **世界感**：#2210 Live Like Boss Spawn Chances（Boss 概率随等级/局内行为上升）、#814 **Late to the Party**（物资随战局时间消失、门窗状态变化、车撤离离开）、#2342 UNTAR Go Home / #2427 RUAF Come Home（新派系巡逻）。

### 1.6 技能与成长重做

- **Skills Extended**（#2383）——12 个技能扩展 + 撬锁/黑客/追踪三个小游戏 + 网页配置编辑器。
- **Fair Progression**（#2652）——装了自定义任务时自动平衡任务 XP，保住原版进度曲线。
- 配合项：#2144 Skill Distribution、#2162 Skill Multiplier、#2961 Weight Leveling。

### 1.7 事件化情景

- **Blackout**（#2843）+ **ebz7-BlackoutAddons**（#3096）——Labs 停电事件：断电摸黑、撤离点封锁、门锁密码；扩展包加入发电机重启、断路器维修、3 分钟紧急电力撤离、Black Division 猎人 + 突袭 Killa。
- **季节体系**：#2314 Season Rotator（自动换季）、#2479 Weighted Season Randomizer（加权随机、逐局换）、#2120 Time & Weather Changer NG。

### 1.8 经济完全可编程

- **SVM**（#236，1.33M）——一体机。
- **Ultimate Flea**（#2844）——本地供需经济 + tarkov.dev 价格 + 早期 wipe 缩放 + 等级锁定。
- **NUTS**（#1936，移除跳蚤）／**The Blacklist**（#755，解除跳蚤黑名单，169k）／**Live Flea Prices**（#1131，295k）。

---

## 2. 「有趣的」高质量 mod（按主题速查）

> 每行 = 名称（#id）— 一句话。带 ★ 为下载量头部或口碑项。

### 任务与商人
- ★ **Lotus**（#1232）— 340+ 任务、900+ 货品、无限 Labs 门卡；**仅 4.1.6**。188k。
- ★ **ISB Aishi**（#2478）— 110+ 任务的剧情商人，独特货架与装备。33k。
- ★ **Scorpion**（#1348）— 122 个开局即可做的任务（含限时活动任务）。140k。
- **WTT-Artem**（#1023）— 夜猎导向的剧情商人线。318k。
- **Painter**（#1025）— 涂装武器与独特配件商人。315k。
- **The Quartermaster**（#2817）— 「玩家驱动」二手市场（挂售/购买装备）。
- **VisitAPI Framework**（#2788）— 让商人有正作 1.0 风格剧情对话与过场（.DLG 脚本 + 编辑器）。
- **TraderGen**（#2749）/ **ItemGen**（#2803）/ **AmmoGen**（#2777）— 面向非程序员的商人/物品/弹药内容框架。
- **Quest Codex**（#3054）— 含自定义任务 mod 的任务百科 + Kappa 追踪。

### 地图与地点
- **Enable Labyrinth**（#2416）— 为 PMC/Scav 开放迷宫。60k。
- **Factory Classic**（#3041）— 重制前的旧工厂。
- **Map Variants**（#3040）— 原版/回移版地图择一。
- **Lots of Loot Redux**（#1619）— 战利品密度回调旧手感。252k。

### 战斗与武器
- ★ **WTT-Armory**（#2246，289k）/ ★ **Epic's All in One**（#1263，277k）。
- **Definitive Weapon Variants**（#2010）— 600+ 基于现有枪械的变体。
- **Weapon Customizer**（#1950）、**Trader Modding**（#1283）、**Modding Stats Helper**（#1300）——改枪三件套。
- **ScopeRangefinder**（#2735）、**Canted Aiming**（#2415）、**Airburst**（#2948，40mm 空爆）、**CookingGrenades**（#2951）、**ShrapnelFix**（#3118）。

### 生存与医疗
- **TraumaCore**（#2893）— 弹道-解剖伤害模型（伤口深度/器官/失血，AI 同样适用）。
- **Broken Limbs Don't Heal**（#3121）— 治好的骨折仍留后遗症。
- ★ **Continuous Healing**（#1884，142k）、★ **MergeConsumables**（#1657，87k）、**Quick Meds**（#2967）、**Make Meds Great Again**（#1818，仅 4.1.6）。

### 视听与沉浸
- ★ **Amands's Graphics**（#592，870k）、★ **HollywoodFX**（#2003，221k）。
- **CloudSix**（#2567）/ **SSRSix**（#3099）/ **POMSix**（#3100）/ **FogSix**（#3110）/ **AOSix**（#2569）——画质全家桶（体积云/SSR/视差/体积雾/环境光）。
- **BodyCamera**（#3075）— 运动相机式镜头（惯性 + 晃动）。
- **Battle Ambience**（#2264）、**Agony SFX**（#1831）、**S&M PRT**（#2825，局内无线电）、**SoulPlayer**（#2839，磁带放歌）、**Better Thermal & NV**（#2531）。

### UI 与信息
- ★ **Dynamic Maps**（#1431，810k）＋ **DynamicMaps Extended**（#3097）。
- ★ **UI Fixes**（#1342，457k）、**Quest Tracker**（#1140）、**All Quests Checkmarks**（#2025）、**Ammo Stats**（#167，213k）、**ODT's Item Info**（#2430）、**Show Me The Money**（#3011）、**Loot In Vicinity**（#2698）、**LootNET**（#2679）、**Career Log**（#2713）、**Better Keys NG**（#1888）。

### 工具与开发
- **Raid Review**（#1479，56k）— 战局回放 + 热图。
- **ModProfiler**（#2952）— 逐 mod CPU 开销分析（找卡顿元凶）。
- **WebOverlay**（#2879）— HTML HUD/面板框架（给 mod 开发者）。
- **NARCONet**（#2441，mod 同步）、**SPT Mod Manager**（#2851）、**Blacksite Mod Manager**（#3038）、**SPTChecker**（#2921，Forge 更新监视）、**Raid Recovery**（#3080，崩溃后回局）。

### 趣味
- **SPT Casino**（#2994）/ **Bookie**（#3032，赌场商人）/ **Roulette SPT**（#3089，CS2 式开箱）。
- **Trash Talk**（#2816，文字回怼击杀者）、**Tarkova-cola**（#3088，僵尸模式技能）、**Warzone Cat Ears**（#1280）、**Hideout Shootout**（#2705）、**Grappendix**（#3068，外置肠子攀爬）、**SOMF**（#3105）、**Hideout Cat**（#2908）、**BushWhacker**（#2329）。

---

## 3. 近期新面孔（2026-08 起发布；2026-09 起新建 120 个）

- **8 月**：Icebreaker #2866、CS Gas #2864、Skills Extended 回归 #2383、WebOverlay #2879、Task Item Indicator #2888、AutoKeycard #2881、Ammo Loading Animations #2681。
- **9 月**：Lotus 1.8 #1232、Interchange 扩展 #3025、Lighthouse #3022、ImpactCue #3029、Show Me The Money #3011、Map Variants #3040、Quest Backport #3026、SPT Casino #2994、Terminal Commander #2999（配音剧情商人）、Breakout Outlines #3007、Original Story #2998、Rook #3055、NATO Quartermaster #3030、Better Suppressors #3033、Ref Rework #3014、Kappa Tracker #2981、BodyCamera #3075、Raid Recovery #3080、PityLoot #3077、Blacksite Mod Manager #3038、Sage's Tutorial #3129。
- **最近一批（9 月末—10 月）**：Broken Limbs #3121、TrueHardcoreRules #3120、Tushonka Territories #2942、Open World Map #3073、Infinite Everything #3108、Cabinet Looter #3103（整柜翻找）、Extra-Lives #3002、Fika Downed Spectator #3122、DynamicMaps Extended #3097。

> 完整 120 条（2026-09-01 起新建）见 `.scratch/forge-4.1x-survey/data/view-newSinceSep.json`。

---

## 4. 注意事项汇总

1. **仅 4.1.6（61 个）** 例：Lotus、VRAM Cleaner、Healing Autocancel、Make Meds Great Again、Better Thermal & NV、Jero Backpack、Tripwire Improvement、Keycard Door Simplify Breach、Fair Progression——4.1.5 环境不可用。
2. **仅 4.1.5（16 个）** 例：**UnityToolkit**（客户端开发库，影响下游 mod）、BALANCED MEDS、BETTER ATTACHMENTS、USEFUL FOODS AND DRINKS、Full Automatic Guns、Better Suppressors、MassivesoftCore/Weapons、READABLE KEYS、Bookie——4.1.6 环境需等更新。
3. **cheat_notice（5 个）**：Open Sesame、Map Editor Lite、Dev Tools、MagicBullet、Cobra's SPT Explorer——开发者/作弊工具性质，正式整合包慎入。
4. **档案绑定提示（183 个）**：含 WTT Content Backport、Lotus、Scorpion、Skills Extended、Painter 等——装/卸前备份档案，部分改动不可逆。
5. **fika 标记（411/600）** 只表示「某个版本被确认兼容 Fika」，不等于「为联机设计」；合作玩法以 Fika 本体 + 明确标注 Fika 的 mod 为准。
6. 下载量只反映热度；补丁支持以权威数据集为准，本报告仅标注特例（「仅 4.1.6」/「仅 4.1.5」）。

---

## 5. 本次归档刷新与检索（2026-10-11）

**产出文件**（`knowledge/spt-kb/archive/forge/`）：

| 文件 | 内容 |
|---|---|
| `api/mods-catalog.json` | 全量 mod 目录刷新：**1969**（此前的 1830 快照已过期） |
| `api/compat-4.1x.json` | **新增**：4.1.x 兼容 600 条，含 versions（近期版本，最多 10 条）与 source_code_links |
| `mods/` 源码库 | 窗口 2026-09-14→10-11（272 mod / 296 源码条目）增量克隆 **172** 个仓库；`MANIFEST-sp-mod-2026-10.md` 记录（ok=291 / missing=5；缺失 5 条为非仓库形态链接） |
| `.incoming/` | 本次抓取页存档（6 页，已移入 `.incoming-2026-10/`） |

**工具**：

- `scripts/spt-kb/fetch-metadata.mjs`（**本次新增**）——刷新 catalog + compat 两份快照。
- 既有管线：`fetch-mods.mjs`（窗口抓取）→ `clone-sources.mjs`（浅克隆）→ `finalize-manifest.mjs`（MANIFEST）。
- 分析脚本：`.scratch/forge-4.1x-survey/scripts/`（补丁集抓取 / 兼容集分析 / 分类视图 / 详情抓取）。

**检索方法**：

- MCP `spt_forge_search`（读 catalog + hot-index，按下载量排序）。
- 直接过滤 `compat-4.1x.json`：`versions[].spt_version_constraint` 判补丁、`source_code_links` 找源码；补丁权威 ID 集在 `.scratch/forge-4.1x-survey/data/patch-4.1.5.ids.json` / `patch-4.1.6.ids.json`。
- Forge 页面：`https://sp-mod.com/mod/<id>`。

---

*报告生成：VT-OS/OPENCODE（2026-10-11）。数据可复现：见第 5 节工具链与分析工作区。*
