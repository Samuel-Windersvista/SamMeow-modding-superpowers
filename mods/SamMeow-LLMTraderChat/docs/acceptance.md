# 验收记录 —— LLM Trader Chat（M3 收口）

> 版本：`0.1.0`（候选）。**M2/M3 实机验收已通过**；剩余 = 正式发布动作。
> 规格：`.scratch/llm-trader-chat/spec.md`；工单：`.scratch/llm-trader-chat/issues/`。

## 一、spec 六条验收清单

| # | 验收项 | 状态 | 证据指针 |
|---|--------|------|----------|
| 1 | 两个试点商人各完成 ≥3 轮对话 | **通过**（M2 实测） | 工单 07 评论（引擎接线 `TraderChatBot`）、09 评论（M2 实测）；服务器日志 `overwrite\SPT_Runtime\user\logs\spt\spt20261014.log`（`[LLMTC][recv]/[route]/[reply]`） |
| 2 | 至少一轮答案来自知识文档 | **通过**（M3 实测） | 工单 10 评论（验收问答清单已归档；M3 实测「提问都没问题」）；日志 `[LLMTC][kg] 命中数=N 来源=<文件名>` |
| 3 | 重启后历史保留 | **通过**（M2 实测） | 工单 09 评论（重启实测）；`SptChatHistoryProvider` 读 profile `dialogueRecords`、回复经 `MailSendService` 落库 |
| 4 | 服务器日志无报错（白名单警告除外） | **通过**（M2 实测） | 日志 `overwrite\SPT_Runtime\user\logs\spt\spt20261014.log` 无 `[LLMTC]` 相关报错 |
| 5 | 与同类聊天 mod 共存一测 | **按「未同装」结案 + 代码侧冲突分析（见下）** | Overseer 决策：Trash Talk / Miyako 不安装 |
| 6 | 未启用商人无行为变化 | 完成 | 仅对 `traders[]` 中 `enabled=true` 者注册 bot（`ConfigRegistration`）；客户端仅解锁名单内对话（`ClientSettings.ShouldUnlock`）；测试 `HandleMessage_DisabledTrader_IsIgnored` / `HandleMessage_UnknownTrader_IsIgnored` |

## 二、共存冲突分析（未实机同装；对照归档源码）

对照源码：`knowledge/spt-kb/archive/forge/mods/Trash-Talk_2816_source/`、`Miyako-Carry-Service_2709_source/`。

### 补丁点 / 作用域对比

| 维度 | Trash Talk | Miyako Carry Service | 本 mod（LLM Trader Chat） |
|---|---|---|---|
| 客户端 | `UpdatableChatDialogue.CanSendMessage` Postfix；仅 `Type == EMessageType.UserMessage`（PMC 对话域） | 无客户端聊天输入补丁 | `UpdatableChatDialogue.CanSendMessage` Postfix；仅 `_id ∈ 客户端解锁名单`（商人对话域） |
| 服务端接入 | `DialogueController.SendMessage` Postfix（`IsChatBot(dialogId)` 为真则跳过） | 自有 `IDialogueChatBot`（联系人 Miyako，独立 trader id） | 每个启用商人一个 `IDialogueChatBot`（`SendMessage` 按 dialogId 路由） |
| 服务端补丁 | —（以 `SendMessage` 收口） | `MailSendService.SendLocalisedNpcMessageToPlayer` Prefix（仅重映射 TempOrderTraderId→MiyakoId）；`DialogueController.GenerateDialogueView` Prefix（仅 Miyako dialog） | `DialogueController.GenerateDialogueList` Prefix（仅启用商人，发开场）；发送用 `SendDirectNpcMessageToPlayer` |

### 结论：点位/条件不重叠，预判可共存

- **客户端 `CanSendMessage`**：两者均为 Postfix（Harmony 默认链式执行、互不 skip）。Trash Talk 只处理 `UserMessage`（PMC）对话，本 mod 只处理名单内商人对话 —— 条件互斥，各自只翻自己那一类。
- **服务端路由**：Trash Talk 的 `SendMessage` Postfix 检测到 dialogId 属于任一已注册 `IDialogueChatBot`（含本 mod 商人 bot）即跳过，不会重复回复；本 mod bot 与 Miyako bot 的 dialog id 互不相同，按 id 路由互不串台。
- **发送路径**：Miyako 的 `SendLocalisedNpcMessageToPlayer` Prefix 只重映射它自己的临时商人 id；**本 mod 使用 `SendDirectNpcMessageToPlayer`（非 localised 重载）**，故 Miyako 补丁不触及本 mod 发送；本 mod 亦不触碰 localised 重载。
- **列表/开场补丁**：Miyako 补 `GenerateDialogueView`、本 mod 补 `GenerateDialogueList` —— 不同方法，且各自以自身 dialog id 为条件。

### 残留观察点（若将来同装）

1. Trash Talk 的 `_chatBotIds` 在首次 `SendMessage` 时缓存；本 mod bot 于 DI 构建期注册（早于任何请求）→ 应已被收录（低风险；同装时确认首次收信无重复回复）。
2. 两 mod 均向 `IEnumerable<IDialogueChatBot>` 注册多实例（MSDI 支持多注册）；同装时确认 bot 集合无冲突。
3. 客户端同装时 `CanSendMessage` 有两个 Postfix；确认 Trash Talk 对本 mod 商人对话不改 `__result`（其条件已排除），反之亦然。

> 未同装实机验证（Overseer 决策）。以上为**代码侧静态分析**：预判可共存，保留观察点。

## 三、评审 NIT 决策（工单 12 复审）

- **NIT-2 输入框文本跨对话外溢**：**不改**。因果链：① 原版 `ChatScreen` 切换对话时**不清空** `_chatInputField.text`（vanilla 行为）；② 本 mod 的字符放行补丁使「解锁对话内可输入原版会剥离的字符」——两者叠加形成**理论外溢路径**（在对话 A 输入原版会被剥的字符 → 切到对话 B 仍留在框中）。因影响可忽略（需特定操作顺序、且残留字符无害）而**不修**，以避免引入「切换对话时复验/清空」这一新的耦合点。
- **NIT-3 PatchAll 全有或全无**：**明确不改**。曾实现逐补丁类 `CreateClassProcessor(...).Patch()` + try/catch，但 **STD-CLI-007 明确要求存在 `PatchAll` + 撤销的生命周期**（改为逐类会使该规则 FAIL）；且本 mod 各补丁目标在当前 SPT 4.1.6 上均存在（已实机验证），全有或全无风险可接受。已在 `Plugin.cs` 注释说明。
- **NIT-5 `ChatPunctuationPatch` using 清理**：已改（工单 10）。

## 四、全商人接入批次（工单 13）——部署记录（实机验收**已通过**）

> 2026-10-15：在原 3 位（Therapist / Peacekeeper / Painter）基础上，接入《永远的诺文斯克》全部商人（12 位原版 + 7 位 mod，共 19 位）。

- **配置**：`config.jsonc` / `defaultConfig.jsonc` 各 19 条（traderId 集合由单测守护一致）；每位含 persona / openingMessage / docsDir / resetKeywords。
- **文档**：`knowledge/traders/` 19 目录 × 4 篇（背景 / 立场与态度 / 业务范围 / 常见问答），共 76 篇随包。
- **测试**：109/109（含新增「随包启用商人 ⊆ 客户端默认名单」守卫）。
- **部署（已完成）**：overlay 文件级刷新（robocopy，保留 meta.ini）——保 key `sk-75***c1e1`（注入核验通过、placeholder=False）、config traders=19、knowledge 目录=19；运行时客户端名单 JSON = 19 位；`downloads\llmtc-m1.zip` 同步；overlay README/GLOSSARY 刷新。
- **实机验收（2026-10-15 通过）**：`[LLMTC][config] 启用商人=19/19`；`[LLMTC][route] 已接线商人` ×19；16 位新商人开场消息全部发出（一次性）；新商人真对话与 `[kg]` 命中（Mechanic / Croupier / Fence / BTR Driver）；验收窗口内无 `[LLMTC]` 告警；客户端 `可聊天商人=` 19 位。证据详见工单 13 评论。
