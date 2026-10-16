# LLM 商人聊天（LLM Trader Chat）— 实施规格

> **状态（Status）**: ready-for-agent ｜ ACTIVE（2026-10-14）
> 来源：grilling 三轮回合全部决策（2026-10-14）+ 4.1.6 双端触点核验 + `docs/research/llm-trader-chat-feasibility.md`（含 §8 核验补记）。
> 目标环境：SPT 4.1.6（EFT 0.16.9.40743，Mono / BepInEx 5）。
> 项目位置：`mods/SamMeow-LLMTraderChat/`（本仓库内普通目录，无嵌套 git）。

## Problem Statement

SPT 中商人只能单向给玩家发消息（任务、保险、系统通知），玩家无法回复或与之对话；想与商人聊天只能使用外部工具，或根本没有任何入口。玩家期望的体验是：在游戏内 Messenger 的消息线程里直接与商人打字对话，商人以符合其身份的口吻回复，且回复内容可以来自玩家/服主给定的知识文档；未来的 mod 商人也应能接入同一能力。

## Solution

一个成对 mod（客户端 + 服务端），复用原生 Messenger：

- **客户端**（BepInEx 5 插件）：解锁商人消息线程的输入闸门，让玩家能对商人打字发送。
- **服务端**（SPT server mod）：接住玩家发往商人的文字，由聊天引擎生成回复（人格提示 + 本地知识文档检索 + LLM），以商人身份发回同一消息线程。
- **配置驱动扩展**：每个商人一份配置（traderId、人格、文档目录、开关、开场消息），第三方 mod 商人无需写代码即可接入。
- **试点商人**：Peacekeeper 与 Therapist；mod 商人接入验证在 M2。
- **里程碑**：M1 骨架（解锁输入 → 服务端接话 → 回显，打通端到端）→ M2 接 LLM → M3 接检索增强。

## User Stories

1. 作为玩家，我想在 Messenger 的商人消息线程里直接打字发送，以便不离开游戏就能与商人对话。
2. 作为玩家，我想让商人用符合其身份的口吻回复（Peacekeeper 的美式承包商语气、Therapist 的冷静医生语气），以便沉浸感更强。
3. 作为玩家，我想让商人记得最近若干轮对话，以便多轮聊天连贯。
4. 作为玩家，我想在重启游戏与服务器后对话历史仍然保留，以便延续聊天。
5. 作为玩家，我想用聊天内指令（reset）清空某商人的对话历史，以便重新开始。
6. 作为玩家，我想在首次启用时收到商人的一条开场消息，以便知道该商人已可聊天。
7. 作为玩家，我想让商人回复有一段自然的打字延迟（默认 2–6 秒，可配），以便不显机械。
8. 作为玩家，我想在 LLM 不可用时收到一句兜底话术（而不是无响应或刷屏报错），以便知道服务异常。
9. 作为玩家，我想让回复默认使用中文（system prompt 模板可替换），以便符合我的语言偏好。
10. 作为玩家，我想让未启用的商人保持原版行为（不出现任何变化），以便安装无风险。
11. 作为玩家，我想让商人的回复基于知识文档（涉及设定/FAQ 的问题能答对），以便它不只是闲聊。
12. 作为玩家，我想知道聊天内容会发送到所配置的 LLM 端点（默认可用本地 Ollama 不出网），以便知情控制隐私。
13. 作为玩家，我想让商人只被动回复（除一次性开场外不主动发消息），以便不被打扰。
14. 作为玩家，我想在同时安装其他聊天类 mod（如 Trash Talk / Miyako）时互不干扰，以便共存。
15. 作为玩家，我想让多个商人的对话各自独立（互不串线），以便分别聊天。
16. 作为 mod 作者，我想为我自己的 mod 商人提供一份配置（traderId + 人格 + 文档目录）即可让商人可聊，以便零代码接入。
17. 作为 mod 作者，我想知道接入所需的最小配置契约（字段与目录约定），以便编写兼容配置。
18. 作为服主，我想统一配置 LLM 端点/密钥/模型/超时，以便只设置一次。
19. 作为服主，我想按商人单独开关与指定文档目录，以便细粒度控制。
20. 作为服主，我想通过日志看到聊天关键事件（收到、检索命中、LLM 耗时、失败原因），以便排障。
21. 作为服主，我想让 API key 只存在于服务端配置文件，且配置非法时响亮失败或禁用对应商人，以便安全与可诊断。
22. 作为服主，我想调整延迟范围、历史注入条数、兜底话术、检索条数等参数，以便调优。
23. 作为维护者，我想核心引擎可以脱离 SPT 运行时做单元测试，以便快速回归。
24. 作为维护者，我想项目遵循仓库 mod 惯例（src/、tests/、release/、README、GLOSSARY、MIT），以便与既有 mod 一致。
25. 作为维护者，我想通过 `check-mod-standard`（4.1.6）门禁并在发布说明中注明 LLM 前置条件，以便符合规范。
26. 作为维护者，我想让 M1 骨架先回答三个实测问题（输入解锁是否对商人线程生效、玩家消息如何路由回本 mod、回复能否落回同一线程），以便后续开发建立在已验证链路上。

## Implementation Decisions

1. **形态**：成对 mod——客户端 BepInEx 5 插件（Mono / netstandard2.1）+ 服务端 SPT server mod（net10.0，`IModMetadata`，`OnLoadAsync`）+ 共享常量/协议工程；结构仿仓库 `templates/paired-mod`。
2. **客户端单一职责**：只解锁目标对话的输入（Harmony 补丁 `UpdatableChatDialogue.CanSendMessage`，可在 4.1.6 上核实存在），不新建任何 UI；消息发送沿用原生链路（`/client/mail/msg/send`）。
3. **服务端接入路线由 M1 骨架裁定**：候选 A＝注册 `IDialogueChatBot`（DI 自动收编，但须处理 `CoreConfig.Features.ChatbotFeatures.EnabledBots` 门禁）；候选 B＝在 `DialogueController.SendMessage` 收口拦截并直接经 `MailSendService` 回信（绕过 bot 门禁）。优先形态＝"并入商人现有消息线程"；若该形态不可行，回退"独立联系人"形态。
4. **引擎边界（主接缝）**：`TraderChatEngine`——输入 (sessionId, traderId, 玩家文本) 与历史访问接口，输出回复文本（含延迟调度指令）。编排：reset 指令识别 → 开场消息判定 → 检索 → prompt 组装 → LLM 调用 → 失败兜底。引擎不依赖 SPT 具体实现（接口注入）。
5. **LLM 层**：`ILlmClient` 抽象 + OpenAI 兼容实现（endpoint / key / model / temperature / maxTokens / timeout 可配；示例配置同时给出 Ollama 与 DeepSeek）。异步 + CancellationToken；失败回兜底话术并记日志；未配置有效 LLM 时聊天功能对该请求回"未就绪"提示而非静默。
6. **检索层**：`IKnowledgeRetriever` 抽象 + 关键词/子串实现（支持 md/txt；按标题/段落分块；top-k 可配；中文按字符级/双字匹配兜底）；该抽象即"检索后端适配口"，嵌入实现留待后续。
7. **历史**：复用 SPT profile 的对话消息记录（回复本就落进对话，天然持久化）；构造 prompt 时注入最近 N 条（默认 20，可配）；reset＝清空该商人对话消息（含对话式 welcome 的消息可保留或一并清除，按实现取简单者并在 README 说明）。
8. **配置**：全局配置（LLM、延迟范围、历史条数、兜底话术、语言模板路径）+ per-trader 配置（traderId、enabled、persona、docsDir、开场消息、reset 关键词）。非法 JSON/缺必填 → 对对应商人禁用并响亮记录；不影响其他商人。
9. **节奏**：回复延迟为配置范围内随机值（默认 2–6 秒）。
10. **语言**：默认中文 system prompt 模板（可替换为文件）；人格以中文口吻表达。
11. **开场消息**：每个商人一次性（已发过则不再发；可配开关与文案）。
12. **v1 试点内容**：Peacekeeper 与 Therapist 的人格与知识文档由 agent 起草、Overseer 审改后入库（随 mod 的 knowledge 目录分发）。
13. **共存**：仅处理本 mod 启用的商人对话对象；不触碰 SPT 自带 bot 与其他 mod 的对象；与 Trash Talk / Miyako 的 Harmony 触点错开并做一次共存实测。
14. **身份与位置**：GUID `sammeow.llmtraderchat`；display name `LLM Trader Chat`（中文名「商人 LLM 对话」）；MIT；项目目录 `mods/SamMeow-LLMTraderChat/`；发行归档 `release/`。
15. **里程碑**：M1 骨架（无 LLM 回显，端到端可实测）→ M2 接 LLM（+ mod 商人接入验证）→ M3 接检索（+ 文档驱动问答验收）。
16. **可观测性**：关键路径输出带固定前缀的结构化日志（收到消息 / 路由判定 / 检索命中数 / LLM 耗时与结果 / 兜底触发），供实机验收与排障。
17. **ADR 候选**（脚手架阶段补写至项目 docs/adr/）：载体选择（复用原生 Messenger）、LLM 接入形态（OpenAI 兼容单层）、检索策略（v1 关键词 + 适配口）。

## Testing Decisions

- **好测试**：只测外部行为（引擎输入输出、HTTP 请求形态、检索结果集、配置解析结果），不测实现细节；SPT 运行时（游戏/服务器进程）不进单测；测试不依赖网络与游戏安装。
- **被测试模块（四个单测接缝）**：
  1. `TraderChatEngine`：正常回复、检索片段注入、LLM 失败兜底、reset 指令、开场消息、超时路径（fake LLM + fixture 文档 + fake 历史）。
  2. `ILlmClient`：请求塑形（model/messages 等）、非 200、超时、坏 JSON（fake transport）。
  3. `IKnowledgeRetriever`：命中/排序/条数上限/空库/中文匹配（fixture 文档目录）。
  4. 配置装载：默认值、缺字段、非法值、禁用行为。
- **测试工程先例**：`mods/InescapableTarkovsSoftcore416/tests/`（xunit + net10.0 + `TestSptLogger`/`RecordingLogger`/`TestModTables` 辅助类、`ConfigLoaderTests` 风格）——新项目按同型组织。
- **非自动部分（实机）**：SPT 适配层（bot/拦截 + MailSendService + 历史读写）与客户端补丁不做单测；由 M1 实机骨架与后续里程碑验收覆盖（agent 读服务器/BepInEx 日志 + Overseer 在游戏内操作并回报）。
- **验收清单（试点完成定义）**：① 两个试点商人各完成 ≥3 轮对话；② 至少一轮的答案必须来自知识文档（问一个只有文档里有的细节）；③ 重启后历史保留；④ 服务器日志无报错（白名单警告除外）；⑤ 与同类聊天 mod（若同期安装）共存一测通过；⑥ 未启用商人无行为变化。
- **门禁**：`check-mod-standard`（`-TargetSptVersion 4.1.6`）通过；构建 0 错误、0 新增警告。

## Out of Scope

- 语音（STT/TTS）与任何音频交互。
- 商人交易/任务行为改动（纯对话，不触发任何游戏功能，不做"下单"类联动）。
- 自建聊天面板（复用原生 Messenger；VisitAPI 式商人屏注入与 WebOverlay 均不采用）。
- SPT 5.0 / IL2CPP 适配（仅 4.1.6；代码避免写死单玩家假设以留余地）。
- Fika 联机兼容承诺（不保证；消息按 sessionId 隔离以免未来挡路）。
- mod 间注册 API（v1 纯配置文件驱动；API 设计后置到试点稳定之后）。
- 实时游戏状态注入（v1 纯文档知识库；**v2 已由工单 14 演进开放：注入玩家与该商人的好感度/忠诚等级/任务进度/PMC 阵营与等级以驱动动态态度，其余实时库存/价格仍不在范围内**）。
- 嵌入向量检索（v1 关键词；适配口预留）。
- 事件性主动消息（只允许一次性开场；任务完成/定时问候等不做）。
- Forge 公开发布的执行动作（发布资料与流程另行安排；本 spec 只要求"可发布标准"的工程质量）。

## Further Notes

- **研究依据**：`docs/research/llm-trader-chat-feasibility.md`（可行性、先例、机制证据；§8 为 4.1.6 双端触点核验补记）；原始核验产物在 `.artifacts/llm-trader-chat-ilspy/`。
- **可参考先例**（本仓库归档源码）：Miyako Carry Service（per-trader chatbot + 多 provider）、Trash Talk（输入解锁 + `DialogueController.SendMessage` 收口 + 兜底）、VisitAPI（备选 UI 路线的注入范式）。
- **已核实关键事实**：客户端 `UpdatableChatDialogue.CanSendMessage(SocialNetwork, out string)` 存在；发送链至 `/client/mail/msg/send`；服务端 `[Injectable] IDialogueChatBot` 自动收编、但 `GetActiveChatBots()` 有 `ChatbotFeatures.EnabledBots` 门禁。
- **仍待实测**（M1 骨架负责回答）：① 商人消息线程的输入解锁是否生效；② 玩家消息的路由归属（bot 注册 vs 收口拦截）；③ 回复落回同一线程的可行形态。
- **项目脚手架时补**：项目级 `GLOSSARY.md`（商人对话、检索后端适配口、开场消息等术语）与 ADR（见 Implementation Decisions #17）。
- **发布注意**（后续里程碑）：README 写明 LLM 前置条件与隐私说明（聊天内容出站、默认本地 Ollama 选项）；MIT 许可证；Forge 描述中标注 AI 生成回复。

## v3 扩展：任务知识（2026-10-16 追加）

> **状态**: accepted（grilling Q1–Q7 全部决策已定，Overseer 采纳）。

**触发缺陷（实机取证）**：玩家问 Therapist「水瓶座行动」细节，模型**自信编造**——先答地点为「储备站水处理区」（实际为**海关宿舍楼**），被玩家纠正后未承认不知道，反而以「我知道但不细说」的姿态补圆（「编号我不背」「带盥洗室那间」「夹克口袋能摸到」）。根因双条：① 知识库对任务内容覆盖为零（Therapist 四篇文档关键词命中全 0、检索 `[kg] 命中数=0`）；② 无「不知道就说不知道」的行为约束。

**核心决策**
1. 注入范围＝该商人名下 `Started` + `AvailableForFinish` 任务**详情**（名字仍归 `{{attitude}}`，避免重复）。
2. 披露门控＝`AttitudeTier` → `DisclosureLevel{None, Hint, Full}`：Hostile/Cold→None；Familiar→Hint；Trusted/Ally→Full（含房间/钥匙名）。
3. 独立占位符 `{{quests}}` + 独立预算（单任务 ≤500 字、整段 ≤800 字），旧模板自动追加兼容。
4. 静态层＝**自动生成**的「商人任务手册」（覆盖 15 位有任务的商人；跳过 Storyteller/Croupier/Hideout Harry/Courier）；非人工起草；以任务表为准，禁止反向从 locale 生成（避免 `664ca9f5…` 这类 locale 孤儿键产出幽灵条目）。
5. 反编造硬约束写入 `{{quests}}` 段与默认模板。
6. 剧透边界：Full 档允许报房间号/钥匙名（游戏内任务面板本就可见）。

**任务单**：`issues/16-runtime-quest-knowledge.md`（运行时注入，代码）、`issues/17-trader-quest-manuals.md`（静态任务手册，生成脚本 + 文档）。

**关键数据事实（exp-2 实证）**：任务目标文本＝**裸 24-hex 条件 id 作为独立 locale 键**（链路 `Quest.Conditions.AvailableForFinish[]._props.id` → `locale[裸键]`）；实机 profile 位于 MO2 profile 的 `saves\`（`profiles\Default\saves\<sessionId>.json`），非游戏目录 `SPT_Runtime\user\profiles`。
