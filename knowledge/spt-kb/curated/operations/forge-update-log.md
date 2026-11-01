---
version: [4.1]
domain: both
topic: operations
source: curated
title: Forge 更新日志 — sp-mod.com 增量追踪（4.1 线）
keywords: [forge, sp-mod, 更新日志, mod 增量, 4.1.6, 回填, fika, hot-index, 版本回退]
summary: 用 sp-mod.com API 的 updated_between 窗口追踪 Forge 增量。窗口 2026-10-18→11-01 共 187 条（71 个新 mod / 116 个已有 mod 更新，其中 60 条版本变化、45 条同版本重发）；4.1.6 约束占 80/187、Fika 兼容 107/187；「回填」与 Fika 是主线，版本号不再等于新鲜度。
---
# Forge 更新日志 — sp-mod.com 增量追踪（4.1 线）

> 用途：给整合包运维与 mod 移植决策留「这段时间 Forge 上发生了什么」的一手增量记录。每个窗口一节，最新在上。
> 数据源：`https://sp-mod.com/api/v0`（Forge 现址；`forge.sp-tarkov.com` 已死，HTTP 525）。抓取走 `scripts/spt-kb/fetch-mods.mjs`（curl 子进程 + 本地代理），窗口用 `filter[updated_between]`。
> 口径：「窗口内更新」= `updated_at` 落在窗口里；「新 mod」= `created_at` 也在窗口里；「版本变化」= 与本地 `archive/forge/api/compat-4.1x.json` 快照的最新版本号不同。
> 判读提醒：接口更新了描述、链接或兼容标注但没升版本时，同样会出现在窗口里（见下文「假更新」）——**版本号不等于新鲜度**。

## 窗口 2026-10-18 → 2026-11-01

### 概况

| 指标 | 数值 |
|------|------|
| 窗口内更新 mod | **187** |
| 其中新发布 mod | **71** |
| 已有 mod 更新 | **116** |
| 版本号发生变化 | **60** |
| 版本未变、仅描述被编辑（文件未换） | **45** |
| 本地 compat 快照查无此 id | **11** |
| SPT 约束分布 | 4.1.x 通用 100 / 含 4.1.6 **80** / 4.0.x 5 / 3.11.x 2 |
| 标注 Fika 兼容 | **107 / 187**（57%） |
| 本地 `archive/forge/mods/` 已有源码快照 | **94 / 187** |
| 在 `hot-index.json` 里（窗口内比对用 10-18 版本 636 条） | 105 / 187（11-01 重建后 **185 / 187**） |

> 全站 mod 数同时从 **2001**（本地 2026-10-18 快照）涨到 **2044**；4.1.x 兼容集从 **637** 涨到 **702**。平台新开分类 **id 18 = UI**（本地 9-17 的 `mod-categories.json` 还没有这个分类，本窗口 19 条落在 UI 下）。

### 值得跟进的已有 mod 更新

按下载量 × 对整合包的实际影响排序；要点为 changelog 的忠实中文摘录。

| Mod | ID | 版本 | SPT | 下载 | 要点 |
|-----|----|------|-----|------|------|
| UI Fixes | 1342 | 6.0.4（描述被编辑） | ~4.1 | 470k | 版本与文件都没动（9-29 发布），10-22 只改了 mod 描述 → 报告里的「更新」是假象；Edit Build 修复是 6.0.4 自带内容 |
| Questing Bots | 1109 | 1.1.0 → 1.2.0 | ~4.1.0 | 444k | Fika 增强：bot 生成状态在**所有客户端**显示（原本只在主机）；bot 调试 gizmo 全客户端可开 |
| Classic Movement | 1860 | 1.1.3（描述被编辑） | ~4.1.0 | 144k | 10-21 只编辑描述（作者在里面调侃同名作者），文件仍是 8-21 的 1.1.3 |
| Less Restricting Headwear | 922 | 4.0.0（描述被编辑） | ~4.1.0 | 138k | 内部重写发生在 10-15 发布 4.0.0 时；10-19 只补描述（按 4.1.6 构建、与任意内容包兼容） |
| MergeConsumables | 1657 | 1.6.3（描述被编辑） | >=4.1.3 | 91k | Fika 同步修复写在 9-21 发布的 1.6.3 描述里；10-31 只编辑描述，文件未换 |
| More Energy Drinks | 1688 | 1.3.1 → 1.3.2 | ~4.1 | 78k | 藏身处「名人堂」陈列修复；移除能量饮料触发心脏骤停的概率 |
| ORBIT 3.0 | 2706 | 2.1.1 → 3.1.0 | ~4.1.6 | 66k | 3.x 大版本：应用预设前可预览改动、自定义营地会找掩体/隐蔽位、撤离逻辑修复 |
| Raid Review | 1479 | 1.8.0 → 1.10.0 | ~4.1.6 | 59k | Fika 修复：复活后追踪、战利品、出生装备、撤离；修掉空 raid 列表与服务端报错 |
| Make Shotguns Great Again! | 1453 | 1.18.1 → 1.20.0 | ~4.1.6 | 55k | 新增 ATI Omni Hybrid Maxx .410 与三款 .410/65 弹、Iver Johnson Champion；屏蔽万圣节 Zryachiy 的强制哑火 |
| ISB Aishi | 2478 | 2.0.3 → 2.0.5 | 4.1.6 | 53k | Labs 小扩展：四个新小地点（钥匙卡 / 密码 / 机械钥匙 / 燃油维护区） |
| [SAIN] Twitch Players | 1895 | 3.1.1 → 3.1.2 | ~4.1 | 37k | 修正性格参数错位：Normal 曾比 GigaChad 瞄得更快；Goons 与 PMC 最大瞄准时间提到 0.8 |
| PIT Fireteam | 2676 | 0.10.3 → 1.0.0 | ~4.1.0 | 38k | 1.0 正式版：Guns for Hire / Allegiance 名册、友方 PMC 与 Scav 遭遇、招募与背叛后果 |
| ISB Special Forces | 2485 | 2.0.0 → 2.0.2 | 4.1.6 | 18k | 新增 Chaos Insurgency 入侵事件（承接 Labs 的 Black Division 破口剧情） |
| ECOTI | 2930 | 2.1.0 → 3.4.4 | ~4.1.3 | 18k | 窗口内连推 5 版：与 PiP-Disabler 组合时瞄准镜里透出草地（透明裁剪顺序问题）等修复 |
| CloudSix / AOSix / POMSix / FogSix / ReflectSix | 2567 / 2569 / 3100 / 3110 / 3099 | 4.0.0→4.0.2、2.0.0→2.0.1、1.0.1→1.0.2、1.0.0→1.0.1、1.0.0→1.0.1 | 4.1.6 | 22k / 11k / 2.9k / 2.1k / 2.4k | 画质套件集体更新：AOSix 新增极轻量 SSGI 与 TraceSix 联动；FogSix 修地图切换后玻璃材质残留；SSRSix 更名 ReflectSix（**需先删旧插件目录**） |
| TCF Mod Manager | 2945 | 1.24.0-beta → 1.26.1-beta | >=4.0.13 | 9.4k | 窗口内三个 beta：浏览/已安装页新增 Fika 过滤（不兼容 / 兼容性未知） |
| SPTChecker | 2921 | 3.4.6 → 3.5.0 | ~4.1 | 3.2k | **作者声明这是最后一个版本**，接受接手——工具类 mod 的维护风险信号 |
| Ultrawide Stash | 3053 | 1.1.1 → 1.1.4 | ~4.1.0 | 1.3k | Fika 下队友被套用主机 stash 宽度的问题；升级前需备份 `user/profiles` |
| Direct Reload & Painless Meds | 3074 | 1.3.0 → 1.4.1 | 4.1.6 | 1.6k | 食物/饮料提速一直无效的根因：服务端只缩放 `medUseTime`，食物走的是 `foodUseTime` |
| Configurable End Raid Status | 1978 | 1.0.0 → 1.0.1 | ~4.1 | 2.4k | 为通过版本检查重编译；此前约束是 `~4.0` 被 hot-index 跳过，本窗口因换约束回到 4.1 集合 |
| Black Exfil Revision / FIR Fence Purchases | 2380 / 2371 | 1.1.0 → 1.0.0+spt3.9 | ~3.11.0 | 17k / 12k | 为 SPT 3.11 发 backport，把「最新版本」指针指回 3.11 包——**版本号看起来是回退**，别当成降级事故 |

### 新 mod 精选（下载 ≥ 1000）

| Mod | ID | 版本 | SPT | 类别 | 下载 | 一句话 |
|-----|----|------|-----|------|------|--------|
| Interface 1.1 Backport | 3179 | 1.0.4 | ~4.1.6 | Other | 3789 | 把《逃离塔科夫》1.0–1.2 的新界面（菜单/设置/商人/角色/战局）回填到 SPT 4.1 |
| SCOURGE | 3196 | 1.0.3 | ~4.1.6 | Bots | 3766 | 把 Tushonka「Incursion」事件的 Scourge boss 引进 4.1 |
| Better BepInEx | 3207 | 1.0.3 | ~4.1.0 | UI | 2950 | 重做的 F12 配置菜单（窗口内连推三版，含 WASD 锁定热修） |
| Better Flea | 3177 | 2.4.1 | ~4.1 | Traders | 2490 | 复刻 live 跳蚤市场的挂单与价格行为 |
| Pause Continued | 3170 | 1.5.0 | ~4.1.0 | Other | 2406 | 一键暂停：玩家、bot、时间、音频、raid 计时全部冻结 |
| PPES (Past & Present Event Selector) | 3232 | 1.6.0 | ~4.1.6 | Overhauls | 2236 | 自选服务端跑哪些季节性/历史事件，带可编辑的战局预览与预设 |
| Arena Mode | 3175 | 0.4.1 | ~4.1.0 | Overhauls | 1627 | 竞技场玩法：二段跳、无装备焦虑 |
| Realistic Suppressors | 3171 | 1.0.0 | ~4.1.6 | Equipment | 1466 | 消音器按真实消声数据重做 |
| Gear Enhance | 3217 | 1.4.3 | 4.1.6 | Other | 1459 | 装备属性增强型改造 |
| Shorter Magazines | 3186 | 1.1.0 | ~4.1.0 | Equipment | 1443 | 3x1 弹匣改 2x1，腾格子 |
| TraceSix | 3221 | 1.0.1 | 4.1.6 | Other | 1419 | 体素 GI + 可选全光线追踪（RTGI / 反射 / AO） |
| Skill Effects Extended | 3180 | 1.1.0 | ~4.1.0 | Overhauls | 1328 | 技能等级附带更多实际效果 |
| Infil | 3202 | 2.1.0 | ~4.1.6 | Other | 1303 | 出战前规划 raid 入口点 |
| Transit Ram Cleaner | 3209 | 1.0.0 | 4.1.6 | Other | 1215 | 过境时释放刚离开地图的内存，避免多图常驻 |
| Kirisame Med Timing | 3219 | 1.1.5 | ~4.1.0 | Other | 1199 | 手术/医疗动画时长与真实治疗时间同步，修随机错位 |
| Lawnmower | 3231 | 1.1.0 | ~4.1 | Scripting | 1065 | 剃草（公告板式玩笑，效果是硬核的） |

### 其余新 mod（55 条，下载 < 1000）

| Mod | ID | 版本 | SPT | 类别 | 下载 | 一句话 |
|-----|----|------|-----|------|------|--------|
| Bring Back Artillery Strikes | 3182 | 1.1.0 | ~4.1.6 | Overhauls | 999 | Brings back artillery into your game. With sprinkles on top. |
| Unlock Extractions | 3192 | 1.0.1 | ~4.1.0 | Locations | 995 | Unlocks all extractions on all raids and makes coop extracts usable alone like a |
| All Weapons Flea | 3200 | 1.0.1 | 4.1.6 | Weapons | 967 | Unlock restricted weapons on the flea market while preserving SPT pricing and no |
| Killcam | 3174 | 1.0.1 | ~4.1.0 | Tools | 917 | Replays your last moments before death. |
| Wrist Accessories (WIP) | 3230 | 1.3.0 | 4.1.6 | Clothing | 905 | more customization :) |
| No FiR (Found in Raid) Hideout Upgrades | 3197 | 4.1.6 | 4.1.6 | Hideout | 879 | Removes FiR requirement for all hideout upgrades |
| Halloween Background Backport | 3235 | 1.2.0 | 4.1.x | UI | 839 | Backports the Halloween background from Live into SPT. |
| Make Magnifier Great Again | 3216 | 0.7.2 | ~4.1.6 | Weapons | 808 | Make Magnifier Great Again lets you pair standalone G33 magnifiers and the Skeet |
| LateJoin - Anytime Join for SPT Fika (PM | 3183 | 0.1.0 | 4.1.6 | Scripting | 781 | Connect to active Fika raids at any time as PMC or Scav |
| Price Index | 3228 | 1.1.0 | ~4.1 | UI | 778 | Flea and trader prices made easily viewable in the UI. Made for Better Flea. |
| Redeploy | 3173 | 1.1.1 | ~4.1.0 | Tools | 776 | Let's you actually respawn. |
| Pack A Punch Hideout Module | 3223 | 2.1.0 | 4.1.6 | Hideout | 744 | Bring the weapon-enhancing machine to your hideout! |
| Return Of Thunderstorms | 3236 | 1.2.4 | 4.1.6 | Overhauls | 701 | Bring thunderstorms back into SPT weather |
| Flogger | 3227 | 1.1.1 | ~4.1 | Items | 675 | One-click quick sell to the flea or the best trader, whichever pays more. Made f |
| OpenBarters | 3194 | 1.0.1 | 4.1.x | Overhauls | 648 | Pay for trader barters with any items the trader is willing to buy, as long as t |
| Breath FX | 3203 | 1.0.1 | ~4.1.6 | Overhauls | 627 | Brings back hidden oxygen mechanic and makes it worth. |
| BiC - Bind in Container | 3190 | 1.0.1 | ~4.1.0 | Equipment | 614 | Bind items in your secure container that you can normally bind in just your rig  |
| Euphoria-style ragdolls (WIP) | 3237 | 0.17.4 | 4.1.6 | Overhauls | 610 | Attempts to recreate Euphoria style Physics/Ragdolls in SPT. |
| Plate Durability | 3234 | 1.0.0 | ~4.1.0 | UI | 605 | Durability at a glance. |
| Item Upgrader | 3176 | 1.0.0 | ~4.1.6 | Items | 595 | Turn junk into loot, or lose it trying. Upgrade any stash item into a pricier on |
| Tushonka-800: Combat Augmentation System | 3169 | 1.0.1 | 4.1.6 | Scripting | 594 | Turn your PMC into an experimental combat machine with enemy scanning, a bullet- |
| Left hand rework | 3185 | 1.0.0 | 4.1.6 | Other | 577 | Let`s you peak by left hand without any troubles. |
| eXPonential! A Simple Global XP Multipli | 3201 | 2.0.0 | 4.1.6 | Overhauls | 552 | Multiply your XP gain! Default is 2x, but the sky (or the floor) is the limit! |
| Rippers Casino | 3212 | 1.0.1 | 4.1.6 | Other | 536 | Six casino games for your SPT hideout, featuring NPC opponents, a persistent dig |
| Found to FIR | 3198 | 1.0.1 | ~4.1.0 | Items | 532 | Makes any non FIR item in your inventory FIR after successfully extracting |
| Interface Beautification Hidden | 3184 | 1.0.0 | 4.1.X | Other | 515 | Now you can hidden those ugly things |
| Category Search Shortcut | 3172 | 1.2.0 | ~4.1.0 | UI | 504 | Like who actually uses categories? Now they open search window with selected cat |
| Tushonka Mapping SDK | 3193 | 1.9.3 | 4.1.6 | Locations | 486 | Adds a complete mapping SDK with support for almost everything normal Tushonka m |
| Loadout Manager | 3181 | 1.0.1 | 4.1.6 | Other | 486 | Save the gear you are wearing. Put it back on with one click. Loadouts for SPT,  |
| Server Room Backport | 3211 | 1.0.0 | 4.1.6 | UI | 452 | Backports the server room background from Live into SPT. |
| ebz7-TheStalker | 3213 | 1.0.43 | ~4.1.1 | Bots | 439 | A knife-only boss who stalks you in the dark. The lights die as he comes, he lau |
| Insurance Fraud | 3222 | 1.1.0 | ~4.1.0 | Scripting | 368 | Ditch it anywhere, Prapor knows where to look! This mod ensures that every insur |
| zezinho's Train Timer | 3238 | 1.0.1 | ~4.1.6 | UI | 330 | Shows when the armored train arrives and when it leaves, right in the extraction |
| Quest Glow | 3214 | 1.0.0 | ~4.1 | UI | 319 | Glowing outlines on your Kappa, Lightkeeper and lock-out quests, right in the qu |
| No Insurance Screen | 3206 | 2.0.0 | 4.1.6 | UI | 311 | Skips the insurance screen when queuing for a raid as a PMC. |
| transMOGGED | 3243 | 1.0.0 | ~4.1 | Clothing | 288 | Mog EVERYONE with your new transmogged gear. Or your pal, because it works with  |
| Forecaster | 3241 | 1.0.0 | ~4.1.0 | UI | 275 | Nobody asked for this! But I made it anyways. |
| ZM_Raygun | 3229 | 1.1.1 | 4.1.6 | Weapons | 263 | Do you like my glowing green balls? - Richtofen |
| DailyGunsmith | 3188 | 0.1.1 | 4.1.6 | Quests | 252 | Gunlovin` content for you everyday |
| ZM_Thundergun | 3226 | 1.1.0 | 4.1.6 | Weapons | 246 | Really.. in this game..? |
| Rescue NoClip | 3225 | 1.0.1 | ~4.1.6 | Tools | 222 | Got spawned out of bounds? Got stuck inbetween stuff? This mod will help you |
| FOV unlock | 3239 | 1.0.0 | 4.1.6 | Other | 198 | Unlock FOV settings, up to 150. No configuration, no additional content |
| SPT Barterizer | 3195 | 0.1.3 | ~4.1.6 | Traders | 196 | Turns cash trader offers into item barters while preserving existing barters. Op |
| Coop Quest Tracker | 3210 | 1.0.0 | ~4.1.6 | UI | 191 | Shows a window comparing your accepted tasks and your teammates |
| I'm Not Festive | 3244 | 1.0.0 | ~4.1 | Other | 181 | Disables all special seasonal events. Thats it. |
| PvP Spawner - Addon for Fika | 3205 | 1.1.0 | 4.1.x | Scripting | 166 | Did you want some real PvP with your friends? Well, here you go! |
| zezinho's Shopping List | 3242 | 1.1.3 | ~4.1.6 | Hideout | 163 | Buys what your hideout upgrades still need in one click, plus the ingredients fo |
| Custom Wishlist Categories | 3215 | 1.0.0 | ~4.1.0 | UI | 134 | Why use only 5 wishlist categories when you can have more? |
| Painkiller Duration Stacking | 3240 | 1.0.0 | ~4.1.0 | Overhauls | 130 | Stack painkiller durations. Multiple formulas available so you can tweak it to y |
| Gym Rat | 3247 | 1.0.0 | ~4.1.0 | Hideout | 91 | New options to fasten or reset gym cooldown: crafts, in-raid achievements |
| RailTime | 3246 | 1.0.0 | 4.1.6 | UI | 82 | Lightweight client-side plugin displaying the armored train countdown in SPT, wi |
| Hardcore Progression | 3208 | 2.0.0 | ~4.1.6 | Quests | 65 | Quest recovery barters unlocked through PMC raids, with persistent progress and  |
| DuckCam | 3245 | 1.0.0 | ~4.1 | Tools | 58 | All-in-One Photo Mode: Pose Editor, Bot Spawner, 16K Photos, Camera Paths etc |
| Botmon Trend | 3233 | 1.4.2 | 4.1.6 | UI | 47 | Trends and Bearing arrows on the stock BOTMON overlay |
| LookyLoot | 3191 | 1.1.1 | ~4.1.6 | Other | 20 | Look outside the box: Free-look around the in-raid inventory or container/corpse |

> 共 55 条（下载 < 1000 或已在精选表内）。完整 71 条见 `.artifacts/forge-update-2026-11-01/rows.json`。

### 其余版本变化（除上表之外）

| Mod | ID | 版本 | SPT | 下载 | changelog（原文首句） |
|-----|----|------|-----|------|------------------------|
| Picture in Picture Disabler | 2667 | 2.0.0 → 2.1.0 | ~4.1 | 30812 | Stance overhaul compat fix. |
| Compatibility Highlighter | 2859 | 1.3.0 → 1.3.2 | 4.1.x | 16574 | Fixed bug allowing highlight on unsearched/examined items. |
| DynamicMaps Extended | 3097 | 2.5.0 → 2.5.2 | >=4.1.6 | 15082 | DynamicMaps Extended 2.5.2 Labyrinth Satellite alignment fix |
| Smoothie | 3132 | 1.0.8 → 1.1.0 | 4.1.6 | 13816 | Smoothie 1.1.0: more objects culled, less pop-in, less work for your graphics card - Big buildings like the Cu |
| Cabinet Looter | 3103 | 0.5.2 → 0.5.3 | ~4.0.0 | 13793 | For SPT 4.0.x. |
| Map Variants | 3040 | 1.0.2 → 1.0.3 | ~4.1.3 | 13234 | Fixed airdrops when selecting vanilla raids |
| Bring Back Concussion | 2550 | 1.2.0 → 1.2.1 | ~4.1 | 13035 | Added - Verbose Logging option. |
| Repair Skill Extension | 2556 | 1.4.0 → 1.4.1 | 4.1.6 | 9787 | Fixed Max Durability Repairing. |
| Repeatable Quest Config | 1362 | 2.1.0 → 2.1.1 | ~4.1.4 | 9249 | This version will only work for SPT 4.1.4+ New Features - Config toggle to remove cost requirements to replace |
| female pmc voice (U***) | 2710 | 1.0.5 → 1.0.6 | 4.1.6 | 8324 | fixes - Bots now call out when they spot you again, not only on first contact. |
| Rook | 3055 | 1.2.1 → 1.2.2 | 4.1.6 | 6413 | Rook 1.2.2 Update -Major price changes over many of Rook's items (Weapon presets and a few weapon parts didnt  |
| Auto IFF | 2784 | 2.1.0 → 2.1.1 | ~4.1.0 | 6085 | - Fixed target identification staying blank for a whole raid after you had died in the previous coop raid (Fik |
| Craft Queue | 2860 | 2.4.1 → 2.4.2 | ~4.1.0 | 6014 | - Fixed: with some quest mods installed, the server log showed "Failed to prepare the crafting recipe cache at |
| Hj's Improved Customization UI | 3069 | 1.1.1 → 1.1.2 | ~4.1 | 5628 | 4.1x Version 1.1.2 --- Fixed - Customization tab no longer disappears with [Tushonka Territories](https://sp-m |
| Quest Codex | 3054 | 1.9.0 → 1.11.0 | 4.1.6 | 5360 | QuestCodex v1.11.0 Briefing Badges Update! |
| BackpackGunSlot | 3141 | 1.0.1 → 1.1.0 | 4.1.6 | 5210 | - Nested backpacks: a backpack opened in its own window (inside another backpack, in the stash, on the ground) |
| UnlockAllBackgrounds | 2978 | 1.0.0 → 1.0.3 | ~4.1.0 | 4999 | Fixes - No more "1 plugin failed to load" next to Server Room Backport. |
| Munitions Expert - Reloaded | 3006 | 3.0.0 → 3.1.0 | ~4.1.6 | 4549 | - Restore feature : ammo background coloration based on penetration, by popular demand. |
| RealTargets | 3165 | 1.0.0 → 1.2.0 | 4.1.6 | 4502 | Blood rework, Fika support and translatable text. |
| HideoutBackport | 3163 | 1.0.0 → 1.0.5 | ~4.1.6 | 3051 | - Crafted ammo packs (like 7N40 120 to 4x30) now come with their rounds instead of empty. |
| Usable Crafting | 3149 | 1.0.0 → 1.2.1 | ~4.1 | 2987 | Changes - Prevented the UI from suddenly shifting position when changing the selected craft or filter. |
| Sage's Incendiary Grenade | 3113 | 1.0.2 → 1.1.0 | 4.1.* | 2981 | v1.1.0 --- Added - SAGA-6 can now be planted as a tripwire with the Tripwire Installation Kit, like the other  |
| Music Extender | 2962 | 1.1.1 → 1.2.0 | ~4.1 | 2938 | 1.2.0 🎃 Added - Official support for custom music packs. |
| Deploy Screen Overhaul | 3047 | 1.0.0 → 1.1.0 | ~4.1.0 | 2808 | Your PMC looks more like he's standing in the map, not in front of a greenscreen. |
| GF2 Skin Pack | 3156 | 1.0.0 → 1.1.3 | 4.1.6 | 2681 | Improvements - Improved first-person hand models for Voymastina Erwin and Alva Antje. |
| Hj's Mod Source | 3083 | 0.9.0 → 1.1.0 | ~4.1 | 2535 | 4.1x Version 1.1.0 --- Added - Mod Filter Dropdown to The Handbook |
| Briefing | 3158 | 1.0.0 → 1.0.2 | 4.1.6 | 2335 | Briefing v1.0.2 Kappa & Lightkeeper Badges Update! |
| Armor Expert | 3106 | 3.0.0 → 3.1.0 | ~4.1.6 | 2193 | - Add BepinEx (F12) settings screen with toggles for all the item attributes the mod adds. |
| Realistic Insurance | 2949 | 0.0.93 → 0.0.94 | ~4.0.0 | 2169 | This is the SPT 4.0.x build. |
| Gunwright | 3128 | 1.0.1 → 1.0.2 | ~4.1.6 | 1768 | - BUG Fixes for Consistent Offer ID |
| Factory Classic | 3041 | 1.0.2 → 1.1.0 | ~4.1.3 | 1710 | Halloween Factory - Classic Factory now has a chance to be the Halloween version from EFT's 1.3.0 event: the s |
| HUD Breakout | 3148 | 1.1.1 → 1.4.0 | 4.1.6 | 1695 | - Effect View - Time Remaining of Effect will be visible where is applicable - Fix HUD elements position when  |
| Tarkova-cola | 3088 | 2.0.0 → 3.0.1 | 4.1.6 | 1299 | Hotfixes - APEX Core : dragging it in the inventory right after picking it up broke the UI. |
| Hideout Uncensored | 3167 | 1.2.3 → 1.3.0 | ~4.1 | 1259 | Changelog - New: "Wear your gear while holstered" (on by default). |
| ebz7-BlackoutAddons | 3096 | 1.26.6 → 1.27.10 | ~4.1.1 | 1240 | - The Stalker's dark presence now also kills the facility's own tube and panel lights (most of Labs at full po |
| Name Plate | 3131 | 1.0.0 → 1.1.0 | ~4.1.0 | 824 | New - Name Plate sharing (Fika). |
| Custom Loot Pool | 3161 | 1.3.1 → 1.5.2 | ~4.1.6 | 508 | Fixes - Loot quality: changing only the loot quality settings now enables Save. |
| Trader Level Requirements Editor | 3005 | 1.0.0 → 1.1.0 | ~4.1 | 141 | additions - added the ability to modify a trader's stock refresh timer note A wee-bit beyond the original scop |

> 共 38 条；changelog 保留英文原文，完整摘要见 `rows.json`。

### 版本未动、只有描述被编辑（45 条「假更新」）

这 45 条的 `mod.updated_at` 落在窗口里，但 `published_at` 与版本记录的 `updated_at` 都没变——**文件没换，玩家不需要重新下载**，作者只是编辑了 mod 描述 / 兼容标注 / 链接 / 分类。

判据（可复用）：拉 `versions` 时同时看 `mod.updated_at` 与 `versions[0].updated_at`。两者不一致 = 只改了描述；两者一致 = 真的重传了同版本号的包。本窗口 45 条**全部属于前者**（抽检 1342 / 1860 / 922 / 1657 / 566 的 `versions[0].updated_at` 均等于其 `published_at`）。

典型批量案例：**jbs4bmx 的 7 个 mod**（ArmBandCore 566、Holtzman Shield 319、Medical Attention 147、Remove The Dead 1551、Remove The Irremovables 2406、Special Slots 555、Olympus 505）在 **10-22 12:08–12:10 两分钟内**被逐一编辑——一次批量改描述，不是七次发版。

> 意义：这类变化会稀释每日更新报告的信噪比。读到 `prevVersion === newVersion` 的记录，先按上面的判据分类，再决定要不要关心。

以下是这些 mod 及其描述里写的内容（按下载量）：

| Mod | ID | 版本 | 更新日 | 观察 |
|-----|----|------|--------|------|
| UI Fixes | 1342 | 6.0.4 | 10-22 | Edit Build 修复，470k 下载的大 mod 也这么发 |
| Classic Movement | 1860 | 1.1.3 | 10-21 | 只有描述在动 |
| Less Restricting Headwear | 922 | 4.0.0 | 10-19 | 4.0.0 这个版本号下做了内部重写 |
| MergeConsumables | 1657 | 1.6.3 | 10-31 | Fika 同步热修 |
| Brighter Interiors | 2613 | 1.1.1 | 10-28 | 微优化 |
| Bot Callsigns - Reloaded | 1873 | 2.0.5 | 10-25 | 补 4.1 适配 |
| Camo And Stickers | 2658 | 1.19.0 | 10-27 | 材质编辑器支持海报正面与 UV 旋转 |
| Lacy's PvE Tweaks | 2395 | 1.3.2 | 10-18 | 只移除第一个过境目标、其余未清的 bug |
| Make Meds Great Again! | 1818 | 1.3.1 | 10-19 | 跳蚤挂单里医疗包显示原版 HP 的 bug |
| Discarder | 1745 | 5.0.0 | 10-31 | 标注「已适配 SPT 4.1」 |

| Mod | ID | 版本 | 更新日 | changelog（原文首句） |
|-----|----|------|--------|------------------------|
| Dynamic Item Weights | 2344 | 1.2.1 | 10-27 | Fixed log spam when an item has no tare weight entry (Thanks [@Liquidwarp](https://sp-mod.com/user/120411/liqu |
| NARCONet - Mod Sync | 2441 | 1.9.2 | 10-30 | Fix issues with the game GUI when updating a non-restart file or dismissing the sync dialog |
| Make PL-15 Great Again! | 1171 | 1.5.0 | 10-19 | Updated for SPT 4.1.6 . |
| Make VPO-101 Great Again! | 1175 | 1.5.0 | 10-19 | Updated for SPT 4.1.6 . |
| ImpactCue-UI-MOD | 3029 | 2.0.3 | 10-23 | Version 2.0.3 This update focuses on ammunition tracking, HUD customization, new customization options, and im |
| Weekend Drops Continued | 2747 | 1.0.9 | 10-24 | Russian Translation Update - Improved Russian grammar, plural forms, and Tarkov terminology. |
| Remove The Dead | 1551 | 4.1.3 | 10-22 | 🚧 24 August, 2026 - 4.1.3 (For SPT 4.1.3) Overview Version update. |
| Quest Backport | 3026 | 1.0.9 | 10-25 | v1.0.9 Fixes - Collector objective text : the 11 newer Collector items from live (Dunduk floppy disk, DesmondP |
| Quick Sell | 2955 | 4.3.0 | 10-18 | QuickSell v4.3.0 Changes from v4.2.0 : - Flea listing price range expanded - F12 slider now supports 25% to 20 |
| Special Slots | 555 | 4.1.3 | 10-22 | 🚧 24 August, 2026 - 4.1.3 (For SPT 4.1.3) Overview Version update. |
| Mag Retention Reload | 2589 | 1.3.0 | 10-27 | Automatically suppresses UI Fixes' magazine retention reload feature Added a notification when you lose a maga |
| Medical Attention | 147 | 4.1.3 | 10-22 | 🚧 24 August, 2026 - 4.1.3 (For SPT 4.1.3) Overview Version update. |
| Chamber Ammo Info | 2553 | 1.1.0 | 10-27 | Compatibility update for SPT 4.1 Show "Empty" for chamberless weapons with an empty magazine |
| Change Version Label | 2641 | 1.1.0 | 10-22 | - Ported to SPT 4.1. |
| MeleeMultiplier | 2763 | 0.0.4 | 10-18 | 4.1.0 |
| More Mannequin Pose | 2108 | 2.0.0 | 10-27 | Rework More Mannequin Poses has been reworked. |
| Hj's Task Item Indicator | 3082 | 1.0.1 | 10-28 | 4.1x Version 1.0.1 ------------- Added - Added "Point at standalone quest items" (no task needed, such as [Tut |
| Tushonka Territories | 2942 | 2.0.3 | 10-20 | Added strength values to location info screen. |
| SPT Stance Overhaul | 3136 | 0.0.2 | 10-20 | Very early alpha release, expect lots of issues and missing features and a lack of polish. |
| Ashley Schaeffers Gear and Clothing | 2889 | 4.0.2 | 10-31 | 4.1.5 update. |
| Realistic PMC Names | 3064 | 1.1.0 | 10-19 | Version 1.1.0 A major expansion of Realistic PMC Names, with a much larger PMC population and a new dedicated  |
| Holtzman Shield | 319 | 4.1.3 | 10-22 | 🚧 23 August, 2026 - 4.1.3 (For SPT 4.1.3) Overview Version update. |
| GFLVoices | 2738 | 2.5.1 | 10-22 | Removed some unused triggers, bleed and broken triggers are removed as all characters now use On being hit. |
| Remove The Irremovables | 2406 | 4.1.3 | 10-22 | 🚧 24 August, 2026 - 4.1.3 (For SPT 4.1.3) Overview Version update. |
| Strange Curios | 3059 | 1.1.0 | 10-21 | Added Fika support (Get addon for this to work). |
| ArmBandCore | 566 | 4.1.3 | 10-22 | 🚧 23 August, 2026 - 4.1.3 (For SPT 4.1.3) Overview Version update. |
| BodyCamera | 3075 | 0.3.0 | 10-26 | Аdded Recoil - Shot Kick Strength — Controls the overall intensity of the additional camera recoil effect when |
| Fair Equipment Restoration | 2575 | 1.1.0 | 10-31 | Update for SPT 4.1.5. |
| Effects Manager Framework | 3123 | 1.1.0 | 10-23 | Added - Many Helpers! |
| Roulette SPT | 3089 | 1.0.0 | 10-19 | Initial release |
| Hj's Custom UI Fonts | 2837 | 1.2.0 | 10-28 | 4.1x Version Added - Font Size Scaler New Fonts Atkinson Hyperlegible Atkinson Hyperlegible Next Black Ops 1 F |
| Olympus | 505 | 4.1.3 | 10-22 | 🚧 13 September, 2026 - 4.1.3.10 Changes: Add: Support for WTT firearms. |
| SPT Scaffold | 2633 | 0.3.0 | 10-19 | Features & Improvements - SPT 4.1.x Support : Updated the scaffold for stable SPT 4.1 releases starting with 4 |
| Plop Pro | 3168 | 1.0.0 | 10-19 | Plop Pro v1.0.0 Initial release. |
| Trader Only Part Markers | 3162 | 1.0.0 | 10-19 | Install as usual |

> 其余 35 条同样只是描述被编辑（文件未换），完整清单见 `rows.json` 中 `prev.version === latest.version` 的子集。

### 结构性观察

1. **4.1.6 已是硬门槛的一半**：187 条里 80 条约束含 4.1.6、100 条是 4.1.x 通用；只剩 5 条 4.0.x 与 2 条 3.11.x。整合包锁 4.1.6 的做法与生态一致。
2. **Fika 从加分项变成默认项**：107/187 标注兼容，还出现了专职 Fika 的新 mod（PvP Spawner 3205、LateJoin 3183、Coop Quest Tracker 3210），以及把 Fika 共享当卖点的（Name Plate 1.1.0 共享名牌、Ultrawide Stash 1.1.4 修 stash 串号）。
3. **「回填（Backport）」是这一波的主旋律**：Interface 1.1 Backport、Server Room Backport、Halloween Background Backport、HideoutBackport、Quest Backport、Tushonka-800、Tushonka Mapping SDK——SPT 5.0 / Tushonka 1.x 的内容在往 4.1 反向搬运。对整合包是内容红利，对移植是「上游已经有人做了」的信号。
4. **版本号不等于新鲜度，`updated_at` 也不等于**：45 条是「假更新」（`mod.updated_at` 变、`versions[0].updated_at` 不变，文件没换）；2 条 3.11 backport 把「最新版本」指针指回旧包（版本号回退）；1 条（1978）靠换 SPT 约束重新进入 4.1 集合。任何「比版本号」或「看 updated_at」的自动判断都要把 `versions[0].updated_at` 与约束一起看。
5. **画质/性能套件互相联动**：CloudSix / AOSix / POMSix / FogSix / ReflectSix（原 SSRSix）在窗口内全部动过，AOSix 会读 TraceSix 自动调 GI，ReflectSix 改名要求先删旧目录——这类「套装内互相感知」的更新最容易在整合包里踩雷。
6. **弃坑信号要记账**：SPTChecker 3.5.0 是作者最后一个版本并公开找接手人。

### 与本地档案的关系（本窗口已做的刷新）

- **94 / 187 的 mod 在 `archive/forge/mods/<id>_source/` 有源码快照**，可以直接对照源码判断变更影响，不必重抓。
- **元数据快照已全量刷新（2026-11-01）**：`api/mods-catalog.json` 2001 → **2044**、`api/compat-4.1x.json` 637 → **702**；`hot-index.json` 由 `build-hot-index.mjs` 重建 636 → **702**，窗口 187 条覆盖 **185**（71 个新 mod 全部纳入；未纳入的只有 2913、2875 两条 4.0 线）。
- **1978（Configurable End Raid Status）** 因约束从 `~4.0` 改为 `~4.1`，重建后已进入 hot-index（`best=1.0.1`）。
- 刷新前的三份基线留在 `api/updates/2026-10-18-baseline/`（catalog / compat / hot-index 各一份）；本页的「版本变化」判定就是用那份 compat 快照做的对比。
- 每日检查器 `archive/forge/tools/check-forge-updates.ps1`（计划任务 `ForgeUpdateCheck`，08:20）只盯 hot-index 里的 id：本次重建后新 mod 才进入视野；3.11 backport 的版本回退会在报告里显示成「1.1.0 → 1.0.0+spt3.9」，读报告时仍要按这两点过滤。

### 复现

```powershell
# 抓窗口增量（curl 走代理；-x 覆盖用 SP_MOD_PROXY）
node scripts/spt-kb/fetch-mods.mjs --since 2026-10-18 --until 2026-11-01 \
  --out-dir <输出目录>

# 过滤 4.1.x 兼容（可选，收紧到 spt_version 约束）
node scripts/spt-kb/fetch-mods.mjs --spt-version "~4.1.0" --since 2026-10-18 --until 2026-11-01
```

- 本窗口原始响应：4 页（`mods-p1..p4.json`，187 条），落盘在 `.artifacts/forge-update-2026-11-01/`（gitignored）。
- 分析中间产物：`rows.json`（每条含最新版本、前一版本、窗口内版本列表、changelog 摘要）。
- 与本地快照对比的基线：`archive/forge/api/updates/2026-10-18-baseline/compat-4.1x.json`（637 条，2026-10-18；现役就地文件已在 11-01 刷新为 702 条）。

<!-- 下一个窗口：在下面追加「## 窗口 YYYY-MM-DD → YYYY-MM-DD」，口径与结构复用本节。 -->
