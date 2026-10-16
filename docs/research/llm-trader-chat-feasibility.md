# LLM 商人聊天 Mod 可行性研究 —— 在游戏内 Messenger 与商人文字聊天（SPT 4.1.6）

> 日期：2026-10-14
> 问题：能否做一个 mod——在游戏信息面板（消息/Messenger 区域）中与商人文字聊天；玩家输入文字，商人回复由 LLM API + 指定知识库/文档库生成；目标 SPT 4.1.6，并可扩展到第三方 mod 商人。立项前判断："现在是都不行的"。
> 方法：三路后台只读侦察汇总——(1) 本地知识库/模板/官方示例合并仓库（服务端对话机制、客户端 UI 面、商人扩展）；(2) 本地 Forge 归档 570 个 mod 源码快照先例搜索；(3) 外部一手资料（Forge live API、GitHub 公开源码、SPT 发行说明）。高风险结论由主控台磁盘抽查复核（源码行 grep + 目录核验）。
> 关联材料：`docs/forge-4.1x-mod-survey-2026-10.md`（生态速览）、`docs/research/ui-fixes-1342-spt5-port-analysis.md`（messenger 类名交叉参考）。

---

## 1. 结论摘要（先读这段）

**技术判定：可行——且已有整例前作；"都不行"的判断不成立。**

三条硬事实：

1. **游戏内文字聊天载体已存在。** SPT 原生 Messenger 内置聊天机器人通道（SPT Friend、Commando），交互路径为 `Messenger > Friends > 点击对象 > Start Chat`，玩家输入经 `SendMessageRequest{dialogId,type,text,replyTo}` 送达服务端。**不需要自建聊天面板。**（证据：`knowledge/spt-kb/wiki/SPT_and_Commando_Bots.md:12-16`）
2. **服务端对话管线是官方公开扩展点。** 实现 `IDialogueChatBot`（DI 注册）→ `HandleMessage(sessionId, request)` 接住玩家文本 → 用 `MailSendService` 回发消息（在原生 Messenger 中显示为对方回复）。官方示例 `server-mod-examples/20CustomChatBot`、`23CustomAbstractChatBot` 即最小实现。（证据：`external/spt-archive/server-mod-examples/20CustomChatBot/CustomChatBot.cs:29-58`、`23CustomAbstractChatBot/CustomAbstractChatBot.cs:29-62`）
3. **LLM 接入已有两个 4.1.x 整例**（详见 §2）：Miyako Carry Service（自定义商人 + LLM 多服务商对话）、Trash Talk（Messenger LLM 回复 + 客户端输入解锁），均适配到 4.1.6。

与设想的差距（=真正要做的事）：

- 先例各自绑定自己的对象（Miyako 只服务它的商人；Trash Talk 只服务 PMC 击杀对话），**没有"把 LLM 聊天泛化到任意第三方 mod 商人"的注册机制**；
- **没有 RAG/知识库先例**：现有实现是静态 system prompt + 最近 N 条对话历史，知识库检索需自建（轻量组合可行，§3.4）；
- 客户端需要一小步：把目标对话对象的输入解锁/接线（Trash Talk 已证明路径：`UpdatableChatDialogue.CanSendMessage` 一处 Postfix）。

**推荐路线**：复用原生 Messenger（Trash Talk 模式）+ 服务端 per-trader chatbot 注册层（Miyako 模式）+ LLM provider 抽象 + RAG 层（新增）。备选自建面板（WebOverlay 路线）不建议首选，理由见 §4.2。

> 对"信息栏"落点的理解说明：本报告按主菜单 Messenger（消息/邮件/聊天面板）理解，并同时覆盖"商人屏"注入先例（VisitAPI）；两处均有实证路径。若所指为其他界面（如战局内通知），服务端机制不变，客户端落点需另行核验。

---

## 2. 先例（均已公开发布，源码可得）

| Mod | ID | 作者 | 兼容 | 下载量 | 相关度 | 本地源码快照 | 仓库 |
|---|---|---|---|---|---|---|---|
| Miyako Carry Service（宫子护航店） | #2709 | Himesamanoyume | `~4.1.2`（页面标至 4.1.6） | ~13.1K | **极高**：自定义商人 + chatbot + LLM 多服务商 | `knowledge/spt-kb/archive/forge/mods/Miyako-Carry-Service_2709_source/` | github.com/himesamanoyume/MiyakoCarryService |
| Trash Talk | #2816 | Vultify | `~4.1.0`（页面标至 4.1.6） | ~4.6K | **极高**：Messenger LLM 回复 + 客户端输入解锁 | `knowledge/spt-kb/archive/forge/mods/Trash-Talk_2816_source/` | github.com/Vultify/TrashTalk |
| VisitAPI Framework | #2788 | TricolourLM7（TricolourSky） | 4.1.x / Fika 兼容 | — | 高：商人屏/对话 UI 注入框架（非 LLM） | `knowledge/spt-kb/archive/forge/mods/VisitAPI-Framework_2788_source/` | github.com/TricolourSky/VisitAPI |
| Terminal Commander | #2999 | MissinginAction（ReplayDEVYT） | 4.1.x | — | 中高：剧情商人（章节 + 配音 + Visit 对话） | `knowledge/spt-kb/archive/forge/mods/Terminal-Commander_2999_source/` | github.com/ReplayDEVYT/TerminalCommander |
| WebOverlay | #2879 | Anvil（maschine） | 4.1.x | ~10.1K | 高（备选面板路线：WebView2 HTML 叠加） | `knowledge/spt-kb/archive/forge/mods/WebOverlay_2879_source/` | github.com/maschine34675/WebOverlay |
| ReceiveAllChats | #2785 | maschine | 4.1.x | ~5.0K | 中：Messenger 补丁触点参考 | `knowledge/spt-kb/archive/forge/mods/ReceiveAllChats_2785_source/` | github.com/maschine34675/ReceiveAllChats |
| UI Fixes | #1342 | tyfon7 | 4.1.x | — | 低-中：邮件/messenger 行为补丁 | `knowledge/spt-kb/archive/forge/mods/UI-Fixes_1342_source/` | github.com/tyfon7/UIFixes |

### 2.1 Miyako Carry Service（#2709）——"自定义商人 + chatbot + LLM"完整范本

- 服务端 chatbot：`Server/ChatBot/MiyakoChatBot.cs`（`class MiyakoChatBot : IDialogueChatBot`，主控台已 grep 核验 :29）。`HandleMessage()` 先走命令前缀（`mcs order` 等），未命中交 `LlmDispatcherService.TryDispatchAsync(sessionId, text)` 做 LLM 自由文本兜底。
- LLM 层：`Server/Providers/Llm/*`——`ILlmProvider` + `BaseLlmProvider` + 9 个 provider（OpenAICompatible / Anthropic / GoogleGemini / DashScope / Zhipu / Spark / Qianfan / MiniMax 等）；`OpenAICompatibleProvider.cs` 用 `response_format=json_object` 解析意图 JSON（缺省 baseUrl 指向 `api.deepseek.com`）。
- 回信：`MailSendService.SendLocalisedNpcMessageToPlayer` / `SendDirectNpcMessageToPlayer`；`Server/Patches/Dialogue/SendLocalisedNpcMessageToPlayerPatch.cs` 用 Harmony 替换发信商人 ID。
- 客户端触点：`Client/Patches/RefreshQuests/MessageReceivedHandlerPatch.cs`（patch `SocialNetwork.MessageReceivedHandler` 刷新任务）、`TraderScreensGroupShowPatch.cs`、`MainMenuControllerClassPatch.cs`。
- 已知边界：只服务"宫子"一个商人；无 RAG。

### 2.2 Trash Talk（#2816）——距目标最近的最小架构（可直接参考的骨架）

- 代码量极小：客户端 1 文件 + 服务端 4 文件。
- 客户端 `TrashTalk/TrashTalk.cs`：BepInEx 5 插件，Harmony patch `UpdatableChatDialogue.CanSendMessage` Postfix 解锁聊天输入（源码注释：PMC 对话框只卡在 friends-list 检查上）。主控台已核验 `:23-32`。
- 服务端 `TrashTalkServer/Patches.cs`：
  - `PmcChatResponseService.SendVictimResponse` / `SendKillerResponse` Postfix 捕获击杀上下文（武器/部位/距离/地图）；
  - `DialogueController.SendMessage` Postfix —— **所有玩家打字消息的唯一收口**，`Task.Run` 异步转 `LlmService.HandlePlayerMessage`；反射读取 `DialogueController.DialogueChatBots` 跳过 SPT 自带 bot。
- LLM `TrashTalkServer/LlmService.cs`：HttpClient 直连 OpenAI 兼容 `/chat/completions`（默认 Ollama `llama3.2`，可换 Groq/OpenRouter/Gemini/OpenAI）；从 `DialogueHelper.GetDialogueFromProfile` 取整段历史做多轮上下文；随机 15–75 秒"打字延迟"；失败静默。
- 回信投递：`MailSendService.SendUserMessageToPlayer`，回复显示在原生 Messenger。
- 已知限制：**不支持 Fika**；仅复用现成对话，不自建 UI。

### 2.3 其余参考

- **VisitAPI Framework（#2788）**：商人屏注入范式——`Client/Ui/TalkButton.cs` patch `TraderScreensGroup.SelectTrader` Postfix 运行时创建"访问"按钮；`Client/ChapterUI/ChapterTab.cs` patch `TasksScreen.Awake` 注入 STORY 页签；`.dlg` 对话脚本 + 自绘对话框；进度经 `Client/Core/VisitHttp.cs` 自定义路由通信。补丁逐类登记（`Client/Core/Patches.cs`），失败只损失单功能。
- **Terminal Commander（#2999）**：基于 Visit 的剧情商人（章节 + 配音），证明"商人 + 剧情对话"在 4.1.x 可行。
- **WebOverlay（#2879）**：WebView2 HTML 面板框架（`examples/PanelPlugin.cs`、`docs/API.md`）；若自建聊天面板是唯一现成的窗口载体；自述限制：独占全屏不支持、光标/焦点有坑。
- **ReceiveAllChats（#2785）**：Messenger 客户端精确触点清单——`ChatScreen.TransferAll`、`ChatMessageSendBlock.*`、`SocialNetwork.UpdateDialogueList`、`DialogueView._dialogue`（反射字段）。
- **UI Fixes（#1342）**：4.1 侧 `ChatScreen.method_9` 等邮件行为补丁（5.0 侧该方法已消失，移植需重定位）。

---

## 3. 机制证据（一手来源）

### 3.1 服务端：对话与消息

- **`IDialogueChatBot`（核心扩展点）**：`external/spt-archive/server-mod-examples/20CustomChatBot/CustomChatBot.cs:29-58`——`[Injectable]` DI 注册；`GetChatBot()` 返回 `UserDialogInfo{Id, Nickname, Side, Level, MemberCategory}`；`HandleMessage(MongoId sessionId, SendMessageRequest request) → ValueTask<string>`；回发 `mailSendService.SendUserMessageToPlayer(sessionId, GetChatBot(), text)`。
- **复杂形态**：`23CustomAbstractChatBot/CustomAbstractChatBot.cs:29-62`（`AbstractDialogChatBot` 基类 + `IChatCommand`/`IChatMessageHandler` DI 集合）；`Commands/MyCoolCommand.cs:9-44`；变体 `21CustomCommandoCommand`（`Helpers.Dialog.Commando` 命名空间）、`22CustomSptCommand`（`ISptCommand`）。
- **发信出口 `MailSendService`**：4.1 命名空间为 `SPTarkov.Server.Core.Services.Commerce.MailSendService`（4.0 为 `Services.MailSendService`，迁移记录 `knowledge/spt-kb/wiki/SPT_41/Server_40_to_41.md:381`）；方法面契约 `external/spt-archive/mod-examples/TypeScript/9RouterHooks/types/services/MailSendService.d.ts:43-91`（NPC/系统/用户三类发信 + outbox/inbox）。
- **玩家消息契约**：`ISendMessageRequest.d.ts:2-7`（`{dialogId,type,text,replyTo}`）；`ISendMessageDetails.d.ts:7-34`；`IUserDialogInfo.d.ts:2-13`。
- **全局收口**：`DialogueController.SendMessage` 可拦截全部玩家打字（Trash Talk 实测，`TrashTalkServer/Patches.cs`）。
- **对话路由回调面**：`IDialogueCallbacks.d.ts:18-33`（`getMailDialogList/getMailDialogView/sendMessage/...`）。
- **客户端→服务端自定义路由**：`10CustomRoute/CustomStaticRouter.cs:37-87`（`StaticRouter` + `RouteAction<TRequest>`）；4.1 硬约束 `curated/recipes/09-custom-http-routes.md:24-35`（action 必须带 `CancellationToken`；`TypePriority` 基于 `OnLoadOrder.Routers ± n`；同路由先注册者胜）。路由体系总览 `curated/api-notes-4.1/http-routing.md:37-42`。
- **主动推送**：WebSocket 通道 `24Websocket/WebsocketConnectionHandler.cs:27-62`；现网仅 notifierServer 在用（`docs/research/spt-runtime-state-export.md:181`）。
- **游戏内入口**：`knowledge/spt-kb/wiki/SPT_and_Commando_Bots.md:12-16`（Messenger > Friends > Start Chat）；:96-121（Commando 命令列表）。
- **现成 handler 先例**：`docs/spt-4.1.5-服务端代码审计报告.md:106-107`（`GarbageMessageHandler.Process` —— 玩家向 NPC 发字符串即触发服务端逻辑）。

### 3.2 客户端：UI 面与输入

- **4.1 客户端已反混淆**（真名真命名空间；4.0 的 `GClass*` 名一律失效）：`knowledge/spt-kb/curated/modding-guide/03-client-mod-anatomy.md:20-35`。
- **插件形态**：`templates/paired-mod/Client/src/Plugin.cs:24-50`（`[BepInPlugin] : BaseUnityPlugin`，`Awake()` 中 `new Harmony(...).PatchAll()`）；引用面与 TFM 见 `templates/client-mod/ClientModTemplate.csproj`（`netstandard2.1`）；两端共享路由常量样例 `templates/paired-mod/Shared/src/SharedConstants.cs:11`。
- **关键类名（`knowledge/spt-kb/wiki/SPT_41/modding/client/Class_Name_Mappings.md`）**：
  - `UpdatableChatDialogue.CanSendMessage` —— 聊天输入解锁点（Trash Talk 实测验证）；
  - `ChatShared.*`（:717-739：`ChatMessagesList`、`DialogueChatMessage`、`UpdatableChatDialogue` 等）；
  - `EFT.UI.Chat.*`（:4117-4125：`ChatMembersPanel`、`DialogueData`、`MessageData` 等）；
  - `EFT.SocialNetwork`（:3987）、`SendMessageRequestParams`（:5709）；
  - 另：`EFT.UI.TraderDialogScreen` / `EFT.UI.EventDialogScreen` / `EFT.Dialogs.*`（lib-1 交叉引用）。
- **输入解锁证据**：`knowledge/spt-kb/archive/forge/mods/Trash-Talk_2816_source/TrashTalk/TrashTalk.cs:23-32`（主控台 grep 核验）。
- **UI 叠加层范式（本仓）**：`mods/SPT41-DebugToolkit/src/Modules/BotOverlay/BotDebugComponent.cs:25,259-389`（`MonoBehaviour` + `OnGUI`，挂到 `GameWorld.gameObject`）。
- **版本锚点**：4.1.6 目标客户端 EFT `0.16.9.40743`，Mono + BepInEx 5（`mods/SPT41-DebugToolkit/src/Plugin.cs:16-32`）；服务端 .NET 10 运行时（`SP-Tushonka/build` 发行说明，未在本轮逐项复核）。

### 3.3 mod 商人扩展

- **商人表**：`Tables.Traders`（`Dictionary<MongoId, Trader>`，含 `GetTrader(id)`）——`external/spt-archive/server-mod-examples/13AddTraderWithAssortJson/AddCustomTraderHelper.cs:57-76,114-125`；注册全流程 `curated/recipes/01-add-custom-trader.md:46-52`（含 `Trader.Dialogue = []` 字段）。
- **已实作遍历**：`mods/InescapableTarkovsSoftcore416/src/InescapableTarkovsSoftcore/Features/Softcore/Changers/TraderChangesChanger.cs`（`context.Tables.Traders` 全量遍历 + 逐商人改写；其白名单注释"忽略自定义商人"证明自定义商人共存于同一表）。
- **结论**：商人本体（展示/交易/库存）扩展成熟；"让任意商人具备 LLM 聊天" = 把 traderId 映射到 chatbot 注册（新增薄层），无现成 service。`Trader.Dialogue` 字段存在但 schema 未在 KB 展开（缺口，§6）。
- **生态佐证**：VisitAPI / Terminal Commander 证明"商人 + 剧情对话"在 4.1.x 可行（`docs/forge-4.1x-mod-survey-2026-10.md:96-102`）。

### 3.4 LLM 与知识库（RAG）

- **先例 LLM 层**：Miyako `Server/Providers/Llm/*`（`ILlmProvider` 抽象 + 9 服务商 + JSON 意图解析）；Trash Talk `TrashTalkServer/LlmService.cs`（HttpClient 直连 OpenAI 兼容端点，默认本地 Ollama）。
- **.NET 选型**：`OpenAI` 官方 SDK（netstandard2.0+）；`OllamaSharp`（netstandard2.0/net8/net9/net10，实现 `Microsoft.Extensions.AI` 的 `IChatClient` / `IChatEmbeddingGenerator`）；统一抽象 `Microsoft.Extensions.AI` 便于切换本地/云端。（来源：github.com/openai/openai-dotnet、github.com/awaescher/ollamasharp）
- **RAG**：SPT 生态无现成中间件。轻量组合 = embedding（本地 ONNX / Ollama / OpenAI）+ SQLite/LiteDB 向量表；更重的选项 `Microsoft.KernelMemory`。[推断/建议]
- **异步纪律**：LLM 等待必须 async + `CancellationToken` + 超时兜底，避免 SPT 服务端已知反模式（同步阻塞路由线程、热路径 `.Result`、手动 `GC.Collect`）——`docs/spt-4.1.5-服务端代码审计报告.md:38-40,115,121-142`。

---

## 4. 目标 mod 的组成与方案岔路

### 4.1 推荐路线：复用原生 Messenger

链条：**客户端解锁输入 → 服务端 bot 接话 → LLM/RAG 生成 → `MailSendService` 回投**。

1. **客户端**（BepInEx 5 / Mono / `netstandard2.1`）：一条 `UpdatableChatDialogue.CanSendMessage` 类 Postfix 解锁目标对话输入（Trash Talk :31 同款）；可选 `SocialNetwork.MessageReceivedHandler` 刷新补丁（Miyako 同款）。
2. **服务端 chatbot 注册层**：per-trader 注册 `IDialogueChatBot`；从 `Tables.Traders` 枚举 + 配置 opt-in；首个概念验证 = 把 `20CustomChatBot` 改造成"按 traderId 查 persona/文档配置的 bot"。
3. **LLM 层**：`ILlmProvider` 抽象（同构于 Miyako 面）→ OpenAICompatible / Ollama；默认本地 Ollama 可作隐私优先选项。
4. **RAG 层（新增）**：per-trader 文档目录 → 分块 + 嵌入 → 本地向量存储；回复前 top-k 片段注入 system prompt；文档更新走重建索引。
5. **扩展面**：第三方 mod 商人由配置声明（traderId + persona + 文档路径）或 opt-in API 接入；未声明者不受影响。

### 4.2 备选：自建面板（不建议首选）

WebOverlay（WebView2 HTML）或 VisitAPI 式运行时造 UI 均可行，但：工作量大、输入/光标/全屏有坑（WebOverlay 文档自述）、与原生体验割裂。Miyako 与 Trash Talk 都选择复用原生 Messenger——这是最强的工程判据。

### 4.3 一个尚未验证的产品问题

原生 Messenger 的"好友列表"可见性模型：bot 需作为可点击对象出现（Trash Talk 解锁的是已存在对话；Miyako 是自建商人 + 对话对象）。**任意 mod 商人是否自动获得可聊天入口，需运行时实测**（§6-2）。

---

## 5. 风险与版本注意

- **服务端示例是 4.0 语法**：`AbstractModMetadata` + `override` + `SptVersion "~4.0.0"` 需改写为 4.1 形式——`IModMetadata`（无 override）、`SptVersion new("~4.1.0")`、`OnLoadAsync(CancellationToken)`（`curated/recipes/01:27-31`；`templates/server-mod/src/ModEntry.cs:15-25`）。
- **命名空间迁移未收敛**：对话相关同时存在 `Helpers.Dialogue` 与 `Helpers.Dialog.Commando`；`MailSendService` 4.1 在 `Services.Commerce`。写代码前以 4.1.6 源码（fork tag `731d7a2a`）为准。
- **4.1 路由硬约束**：`CancellationToken` 必带；`TypePriority` 基于 `OnLoadOrder.Routers ± n`；同路由先注册者胜、无警告（`curated/recipes/09`）。
- **mod 间无官方消息总线**：跨 mod 只能 DI 单例 / 元数据依赖 / 自建通道（`curated/recipes/10`）。
- **4.1.6 成员级签名未核**：KB 类名映射表为 4.0→4.1 语义映射，不保证 4.1.6 成员级签名；5.0 侧 `ChatScreen.method_9` 消失证明该类面会漂移——开工第一步用 ILSpy/dnSpy 对 4.1.6 程序集现查（流程参考 `curated/operations/client-api-touchpoint-verification.md`）。
- **Fika**：Trash Talk 明确不支持；Miyako/VisitAPI 兼容——若后续面向联机需按 Fika 兼容方式编写。
- **许可证**：借鉴先例代码前核对各 mod 许可证（本地快照含 LICENSE；Miyako/Trash Talk 许可证本轮未核）。
- **API 密钥与隐私**：外部 LLM 意味着聊天内容出站；配置需明示，默认本地 Ollama 可作为隐私优先选项。
- **语言**：俄/英/中并存——LLM prompt 语言与游戏本地化（`SendLocalisedNpcMessageToPlayer`）需一并设计。
- **通知链脆弱**：`NotificationService` 队列存在丢消息先例（`docs/SPT413-fork-源码审查报告.md:29-34`）——若用通知做聊天提示需容错。

---

## 6. 证据缺口 / 开工前实测清单

1. **4.1.6 客户端成员签名**：`UpdatableChatDialogue.CanSendMessage`、`ChatScreen`、`EFT.UI.Chat.*` 逐符号 ILSpy/dnSpy 核验（映射表不保证成员级精确）。
2. **mod 商家在 Messenger 的可见性模型**：任意第三方商人如何获得可聊天入口（好友列表显示条件）——需运行时实测或读 4.1.6 源码确认。
3. **`Trader.Dialogue` 字段 schema**：未在 KB 展开；VisitAPI 源码可作参考。
4. **多轮会话持久化形态**：对话历史在 profile dialogue 中的存储结构（影响"历史消息数/清空对话"设计）。
5. **自建面板路线**（若选）：WebOverlay 在 4.1.6 的实测状态未验证。
6. **Fika 兼容性**：目标 mod 的联机行为未评估。
7. **live Forge 增量**：本地快照为 2026-10；lib-1 在线复查未见相关新先例，后续可在开工时再刷一次。

---

## 7. 来源清单

**本地（仓库内）**

- `external/spt-archive/server-mod-examples/20CustomChatBot/CustomChatBot.cs`、`23CustomAbstractChatBot/`、`21CustomCommandoCommand/`、`22CustomSptCommand/`、`10CustomRoute/CustomStaticRouter.cs`、`24Websocket/WebsocketConnectionHandler.cs`、`13AddTraderWithAssortJson/AddCustomTraderHelper.cs`
- `external/spt-archive/mod-examples/TypeScript/9RouterHooks/types/`（MailSendService / ISendMessageRequest / ISendMessageDetails / IDialogueCallbacks）、`10ScopesAndTypes/types/helpers/Dialogue/IDialogueChatBot.d.ts`
- `knowledge/spt-kb/wiki/SPT_and_Commando_Bots.md`、`knowledge/spt-kb/wiki/SPT_41/Server_40_to_41.md`、`knowledge/spt-kb/wiki/SPT_41/modding/client/Class_Name_Mappings.md`
- `knowledge/spt-kb/curated/recipes/01-add-custom-trader.md`、`09-custom-http-routes.md`、`10-*`；`curated/api-notes-4.1/http-routing.md`；`curated/modding-guide/03-client-mod-anatomy.md`
- `knowledge/spt-kb/archive/forge/mods/`：`Miyako-Carry-Service_2709_source/`、`Trash-Talk_2816_source/`、`VisitAPI-Framework_2788_source/`、`Terminal-Commander_2999_source/`、`WebOverlay_2879_source/`、`ReceiveAllChats_2785_source/`、`UI-Fixes_1342_source/`
- `templates/client-mod/`、`templates/paired-mod/`、`templates/server-mod/`
- `mods/SPT41-DebugToolkit/`、`mods/InescapableTarkovsSoftcore416/`
- `docs/spt-4.1.5-服务端代码审计报告.md`、`docs/SPT413-fork-源码审查报告.md`、`docs/forge-4.1x-mod-survey-2026-10.md`、`docs/research/ui-fixes-1342-spt5-port-analysis.md`

**外部（URL）**

- Forge：sp-mod.com/mod/2709（Miyako Carry Service）、/2816（Trash Talk）、/2788（VisitAPI）、/2999（Terminal Commander）、/2879（WebOverlay）、/2785（ReceiveAllChats）、/1342（UI Fixes）
- GitHub：github.com/himesamanoyume/MiyakoCarryService；github.com/Vultify/TrashTalk；github.com/TricolourSky/VisitAPI；github.com/ReplayDEVYT/TerminalCommander；github.com/maschine34675/WebOverlay、/ReceiveAllChats；github.com/tyfon7/UIFixes
- 运行时与库：github.com/SP-Tushonka/build（releases）；github.com/openai/openai-dotnet；github.com/awaescher/ollamasharp

> 核验说明：本报告高风险结论由主控台磁盘抽查复核——`TrashTalk.cs:31-32`（`CanSendMessage` 补丁）、`MiyakoChatBot.cs:29`（`IDialogueChatBot` 实现）、`external/spt-archive/server-mod-examples/` 目录（`20CustomChatBot/` 等）均已逐项确认存在（2026-10-14）。

---

## 8. 核验补记（2026-10-14，ILSpy 双通路 + 4.1.6 源码）

开工前触点核验已完成（原始产物：`.artifacts/llm-trader-chat-ilspy/`）。结论：

**客户端（`Assembly-CSharp.dll`；ilspycmd + .NET 反射双通路一致）**

- 全部目标触点存在且签名一致：`SocialNetwork.UpdateDialogueList/MessageReceivedHandler/SendMessage`；`ChatScreen.TransferAll/method_9`；`ChatMessageSendBlock` 五成员；`DialogueView._dialogue`；`EFT.UI.Chat.*` 四类型；`EFT.UI.TraderDialogScreen`/`EventDialogScreen`/`EFT.Dialogs.*`；`ChatShared.*`。
- 两处修正：`ChatMessageSendBlock` 字段实为 `_receiveAllButton`（单下划线）；`CG_TransferAll` 是方法（非嵌套类）。
- 发送链已核通：`ChatScreen._sendButton → ChatScreen.method_10 → SocialNetwork.SendMessage → ISocial.ChatSendMessage → ClientBackendSession`，路由常量 `/client/mail/msg/send`（Messaging 基址）。

**服务端（源码 tag `4.1.6`）**

- `IDialogueChatBot`/`AbstractDialogChatBot`/`IChatCommand`/`IChatMessageHandler` 位于 `SPTarkov.Server.Core.Helpers.Dialogue*`；`MailSendService` 位于 `Services.Commerce`，三个发信方法签名一致；`DialogueController.SendMessage` + `DialogueChatBots` + `RegisterChatBot`/`GetActiveChatBots` 存在；`DialogStaticRouter` 与客户端路由对齐。
- 关键新事实：mod 程序集被 DI 扫描（`AddInjectableTypesFromAssemblies`），`[Injectable] IDialogueChatBot` 自动收编；**但 `GetActiveChatBots()` 受 `CoreConfig.Features.ChatbotFeatures.EnabledBots` 门禁**（任意商人 Id 是否需预登记 → 实现期确认，或绕开门禁走拦截路线）。

**对设计的影响**：聊天入口形态（并入商人现有消息线程 vs 独立联系人）由里程碑 1 骨架实测裁定；两条路线均有对应机制支撑。
