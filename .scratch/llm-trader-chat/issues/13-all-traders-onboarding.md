# 13: 全商人接入（原版 12 + mod 7）

**What to build:** 将《永远的诺文斯克》整合包内全部可聊天商人接入 LLM 对话：12 位原版商人（Prapor / Therapist / Fence / Skier / Peacekeeper / Mechanic / Ragman / Jaeger / Lightkeeper / BTR Driver / Ref / Storyteller）+ 7 位 mod 商人（Painter 既有 / Artem / Scorpion / Croupier / Hideout Harry / Badger / Courier）。沿用工单 08 验证过的「配置驱动、零代码接入」契约，不新增业务代码。

**Blocked by:** 08: mod 商人接入验证（零代码扩展）（已完成）

**Status:** in-progress

- [x] 服务端 config.jsonc / defaultConfig.jsonc 新增 16 条 per-trader 配置（persona / openingMessage / docsDir / resetKeywords）——两份 traderId 集合一致（单测守护）
- [x] knowledge/traders/<id>/ 每位商人 4 篇（背景 / 立场 / 业务 / FAQ），覆盖全部 16 位新增
- [x] SharedConstants.DefaultEnabledTraderIds 登记全部 19 位 + 新增 id 常量
- [x] 单测：19 位计数断言 + 客户端名单覆盖守卫（全绿）
- [x] pack + MO2 overlay 部署（保 apiKey）+ 客户端名单 JSON 同步（19 位）
- [x] 实机验收：服务器日志 `[LLMTC][route] 已接线商人` ×19；抽查若干新商人可对话（2026-10-15 通过，证据见评论）

## 商人清单与 MongoId（来源）

### 原版（12；来源 `SPT_Data/database/traders/<id>/base.json`，经 en/ch 本地化核对）

| 商人 | id | 备注 |
|---|---|---|
| Prapor | `54cb50c76803fa8b248b4571` | 弹药/军火/维修/保险 |
| Therapist | `54cb57776803fa99248b456e` | 既有（v1 试点） |
| Fence | `579dc571d53a0658a154fbec` | 销赃网络；货架运行时生成（无 assort.json） |
| Skier | `58330581ace78e27b8b10cee` | 走私中间人 |
| Peacekeeper | `5935c25fb3acc3127c3d8cd9` | 既有（v1 试点） |
| Mechanic | `5a7c2eca46aef81a7ca2145d` | 武器改装/维修 |
| Ragman | `5ac3b934156ae10c4430e83c` | 防具/服装定制 |
| Jaeger | `5c0647fdd443bc2504c2d371` | 狩猎/生存；非默认解锁 |
| Lightkeeper | `638f541a29ffd1183d187f57` | SPT nickname=caretaker；灯塔 |
| BTR Driver | `656f0f98d80a697f855d34b1` | 载具服务；无货架 |
| Ref | `6617beeaa9cfa777ca915b7c` | SPT nickname=Arena；竞技场裁判 |
| Storyteller | `6864e812f9fe664cb8b8e152` | SPT 特有（live 无对应）；无货架 |

### mod（7；来源各 mod 的 `db/base.json` / `data/base.json`，含 `Norvinsk_Forever` 启用名单核对）

| 商人 | id | 来源 mod |
|---|---|---|
| Painter | `668aaff35fd574b6dcc4a686` | 既有（工单 08，Painter-4.0） |
| Artem | `66bf757f27d0b097db0acea5` | WTT-Artem |
| Scorpion | `6688d464bc40c867f60e7d7e` | acidphantasm-scorpion |
| Croupier | `1337bb0dd843363fcd1be869` | Croupier |
| Hideout Harry | `67419e9d0d4541ce671543bb` | acidphantasm-harryhideout |
| Badger | `bd3a8b28356d9c6509966546` | Epic's All in One（TraderBadger） |
| Courier | `67d3a28a3d6f4f7dbd09ed13` | PIT Fireteam（补给线投递身份） |

## 范围排除（侦察结论，防漂移）

- `[4]竞技场商人的箱子-BARF`（箱子）、`[4]新商人-竞技场商人-ref-sptfriendly-quests`（给原版 Ref 加任务）、`[7]更好的枪匠界面-Tradermodding`（UI）、`TGC-NG`（给 Painter 加 assort）、`Russian-SF-Pack`/`Eco`/`WTT-Armory`（改原版 assort）、`Vagabond`（改商人位置；且已停用）、`BlackDiv`（敌人 mod）——均不新增商人。
- `WTT-ContentBackport`、`TushonkaTerritories`（停用）等无商人定义。

## 接入方式（沿用工单 08 契约）

1. 服务端 `config.jsonc` `traders[]` 追加对象（persona / openingMessage / docsDir / resetKeywords）
2. `knowledge/traders/<id>/` 放知识文档（4 篇/商人）
3. 客户端名单：已部署实例改 `SamMeow.LLMTraderChat.traders.json`；发布默认 = `defaultConfig.jsonc` + `SharedConstants.DefaultEnabledTraderIds`

## Comments

- 2026-10-15（实施会话）：接手（handoff `docs/handoff-llm-trader-chat-2026-10-15.md`）。侦察（MO2 127 mod / 100 启用、SPT 4.1.6 trader DB、各 mod db、服务器日志）确定 19 位商人全集；排除项已列。配置 16 条新增 + SharedConstants（19 位名单）+ 测试守卫已落地；文档 64 篇由 4 条 fixer lane 并行产出（lane A/C/D 已收口，lane B 进行中）。待办：lane B 收口 → 核对 → `dotnet test` → pack → overlay 部署（保 key）→ 客户端名单同步 → Overseer 实机验收。
- 2026-10-15（agent 侧收口）：4 条文档 lane 全部完成（64 篇；19 目录 × 4 文件，结构核查 + 抽查通过）。`dotnet test` **109/109**（含新增「客户端默认名单覆盖」守卫）；`pack.ps1` → `release\SamMeow.LLMTraderChat-0.1.0.zip`（137,701 bytes）。overlay 部署完成：robocopy（保 meta.ini）+ key 注入核验（`sk-75***c1e1`、placeholder=False）+ config traders=19 + knowledge 目录=19 + 客户端名单 JSON=19 位 + `downloads\llmtc-m1.zip` 同步 + overlay README/GLOSSARY 刷新。实机验收清单见 `docs/acceptance.md` §四；通过后勾选本单末项并收口。
- 2026-10-15（**实机验收通过，本单收口**）：验收会话证据（日志 `overwrite\SPT_Runtime\user\logs\spt\spt20261014.log`，17:15 起）——① `[LLMTC][config] 启用商人=19/19`；② `[LLMTC][route] 已接线商人` ×19（Prapor → Courier 全名单）；③ 16 位新商人开场消息全部发出（17:17:31–35，一次性）；④ 新商人真对话与检索命中：Mechanic（「你是谁？」→ Reply 48 字）、Croupier（「你是从哪来的？」→ `[kg] 命中数=1 来源=04-faq.md` → Reply 26 字）、Fence（「你据点在哪？」→ `来源=01-background.md` → Reply 75 字）、BTR Driver（「你现在车开到哪了？」→ 命中 2 来源；「你能来森林接我吗？就伐木场」→ Reply 73 字）；⑤ 验收窗口内无 `[LLMTC]` 告警；⑥ 客户端 `LogOutput.log`：`可聊天商人=` 19 位名单已解锁。Overseer 确认「测试没问题」。
