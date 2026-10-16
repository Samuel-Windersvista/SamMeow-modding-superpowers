---
version: [4.1]
domain: both
topic: recipe
recipe_task: trader-chat-pipeline
source: curated
---
# 配方：玩家↔商人文字聊天管线（原生 Messenger + LLM 接入基线）[4.1]

> 状态：已验证（SamMeow-LLMTraderChat 0.1.0：M1 链路 / M2 真对话 / M3 文档问答三轮实机验收通过，2026-10-14/15；证据见 `mods/SamMeow-LLMTraderChat/docs/acceptance.md`）
> 适用：[4.1]（成员级签名按 4.1.6 双端核验；ilspy dumps 在 `.artifacts/llm-trader-chat-ilspy/`）
> 关联：配方 01（自定义商人）、09（自定义路由）、10（mod 通信）；LLM/RAG 层经验见 `../operations/llm-mod-integration-lessons.md`

## 目标

让玩家在**原生 Messenger** 与商人（含 mod 商人）进行真实文字对话。本文给出最小管线与已验证的坑位。

## 管线总览（三环）

1. **客户端输入解锁**：原版 `ChatShared.UpdatableChatDialogue.CanSendMessage(SocialNetwork, out string)` 对非好友对话恒 false → Postfix 对"目标对话名单"翻转 `__result`。发送走原生 `/client/mail/msg/send`，**无需自定义路由**。
2. **服务端接话（路线 A，已实机验证）**：实现 `[Injectable] IDialogueChatBot`（mod 程序集经 DI 自动收编）→ `HandleMessage(MongoId sessionId, SendMessageRequest request)`。**dialogue id == 商人 id**；回复用 `MailSendService.SendDirectNpcMessageToPlayer(sessionId, traderId, MessageType.NpcTraderMessage, …)` 落回同一线程。
   - `CoreConfig.Features.ChatbotFeatures.EnabledBots` 门禁**不拦** `DialogueController.SendMessage` 的 bot 路由（它只影响 `GetActiveChatBots()` → 好友列表）；**有意不开启**，避免商人被塞进好友列表。
3. **开场/建线程**：`DialogueController.GenerateDialogueList` 前缀——首次为启用商人发一条 NPC 消息即创建对话。

> 备选路线 B：`DialogueController.SendMessage` Postfix 收口拦截（Trash Talk 先例）。路线 A 更干净（不动核心路由），优先推荐。

## 关键细节（逐条已验证）

- **落库时序**：`SendMessage` 先 `SendPlayerMessageToNpc` 再调 `chatBot.HandleMessage` → 读历史时需排除"末尾当前玩家消息"（快速连发存在窄竞态，属观察点）。
- **历史来源**：`SptProfile.DialogueRecords[dialogId].Messages`（`DialogueHelper` 读取）；`MessageType.UserMessage`=玩家，其余非空文本=商人（勿把空文本模板消息计入）。
- **reset**：选择性清空——移除无 `Items.Data` 的消息、**保留带附件**（未领取保险/任务奖励）+ 清零 `AttachmentsNew`/`New`（防幽灵角标）；关键词整句匹配（trim + 大小写不敏感）。
- **全角标点被剥根因**：`EFT.UI.ValidationInputField` 字符白名单只含 ASCII 字母数字/半角符号；汉字经 `\w`（Unicode）通过、全角标点（`\p{P}`）被 `_replacer` 剥离 → 前缀跳过 `ValidationError`（限定"聊天输入框实例 + 目标对话名单"）；发送路径无二次过滤。
- **延迟回复**：`Task.Delay` 后台发送（不阻塞路由线程）；延迟到达消息的 UI 刷新=客户端轮询/重开对话（体验设计要点）。
- **每商人一 bot**：注册按配置遍历（`ConfigRegistration`），bot Id=traderId；未启用商人零影响。

## 最小步骤清单（新 mod 落地顺序）

1. 客户端：Harmony 补丁 `CanSendMessage`；名单放**运行时 JSON 文件**（扩展商人免重编译）
2. 服务端：`IDialogueChatBot` 实现（`GetChatBot()` 返回该商人 `UserDialogInfo`）
3. 回信：`MailSendService.SendDirectNpcMessageToPlayer`
4. 开场：`GenerateDialogueList` 前缀 + 一次性标记（键 `sessionId|traderId`，跨档隔离）
5. （可选）标点放行补丁（`ValidationInputField` 面）
6. 验证：`[recv]/[route]/[reply]` 日志 + 实机对聊

## 共存（聊天类 mod，未同装 + 代码侧分析）

Trash Talk（PMC 对话域）与 Miyako（自有商人 bot）与本管线补丁点/条件不重叠：`CanSendMessage` 双 Postfix 条件互斥；Trash Talk 的 `SendMessage` Postfix 按实现跳过全部已注册 bot；Miyako 只 patch `SendLocalisedNpcMessageToPlayer`（不同重载）。残留观察点见 mod `docs/acceptance.md` §二。

## 参考实现

- 成品：`mods/SamMeow-LLMTraderChat/`（客户端 `src/Client/Patches/`；服务端 `src/Server/Chatbots|Patches|Engine`）
- 先例源码：`archive/forge/mods/Trash-Talk_2816_source/`、`Miyako-Carry-Service_2709_source/`
