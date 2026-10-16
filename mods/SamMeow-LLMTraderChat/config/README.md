# config —— 服务端运行时配置

本目录存放 LLM Trader Chat 服务端半的运行时配置，随 mod 部署在
`SPT_Runtime/user/mods/SamMeow.LLMTraderChat/config/`。

## 文件

- `config.jsonc` —— 玩家可改的运行时配置（全局 + per-trader）。
- `defaultConfig.jsonc` —— 随包分发的默认副本；首启或 `config.jsonc` 被删除时由服务端复制为
  `config.jsonc`，绝不覆盖已存在的 `config.jsonc`。

两者结构一致，均支持 `//` 注释与尾随逗号（JSONC）。装载实现见
`src/Server/Config/ConfigLoader.cs`，经 `ConfigRegistration`（`IOnDIConstruct` + `AddSingleton`）注册为 DI 单例。

> MO2 说明：包内已含 `config.jsonc`，正常部署无需首启复制；若运行时生成/复制配置（首启恢复场景），
> 因 USVFS 重定向，文件会落在 MO2 的 `overwrite\` 下并保持相对路径（`overwrite\SPT_Runtime\user\mods\
> SamMeow.LLMTraderChat\config\`）。

## 字段（节选）

- `llm`：OpenAI 兼容端点/密钥/模型/温度/maxTokens/超时（示例给出本地 Ollama 与云端 DeepSeek 两种形态）。
  - `apiKey` 校验为**启发式**：非空且含非 ASCII 字符时启动告警「疑似占位符或含非 ASCII 字符，请替换为真实密钥」
    （真实密钥为 ASCII）；空 key（如本地 Ollama）不告警。
- `replyDelay`：回复前随机延迟范围（秒）。
- `historyMessageCount`：构造 prompt 时注入的最近对话条数。
- `knowledgeTopK`：检索取用的知识片段条数（top-k）。
- `fallbackMessage`：LLM 不可用时的兜底话术。
- `resetConfirmationMessage`：reset 生效后回给玩家的确认文案；空则不发送确认。
- `systemPromptPath`：中文 system prompt 模板路径（相对 mod 根目录；模板见 `config/prompts/system-zh.txt`，
  占位符 `{{persona}}` / `{{attitude}}` / `{{quests}}` / `{{knowledge}}` / `{{history}}`）。
- `traders[]`：per-trader 配置（`traderId` 必填；`name`、`enabled`、`persona`、`docsDir`、`openingMessage`、`resetKeywords`、`attitudePrompts`）。

非法 JSON 或某商人缺必填字段时，仅停用对应商人并输出日志（指明文件与字段），不影响其他商人。
数值参数（`historyMessageCount` / `knowledgeTopK` / `llm.timeoutSeconds` / `llm.maxTokens` / `llm.temperature` /
`replyDelay.*`）的范围钳制**由装载层单一负责**：越界即告警并取边界值。

## reset 语义

玩家发送**整句** reset 关键词（`resetKeywords`，trim 后大小写不敏感等值匹配；如「请帮我重置一下」不触发）后，
服务端**重置对话记忆**：移除无附件消息，**保留带附件的未领取邮件（保险/任务奖励）**，并清零角标；
随后（若配置了 `resetConfirmationMessage`）发送确认。日志记 `[LLMTC][reset]`。

## 新增商人：最小接入契约（零代码）

本 mod 的商人为**配置驱动**：新增一位商人只动配置文件与文档，**不需要改一行 C#、不需要重编译**。
最小操作清单如下（三处；以 Painter / 油漆匠「工单 08 验证商人」为例）：

| # | 文件 | 改什么 | 何时生效 |
|---|------|--------|----------|
| 1 | 服务端 `config/config.jsonc` 的 `traders[]` | 追加一个对象：`traderId`（必填）+ `name` + `enabled` + `persona` + `docsDir` + `openingMessage` + `resetKeywords` | 重启服务器 |
| 2 | 服务端 `knowledge/traders/<traderId>/` | 放入 `.md` / `.txt` 知识文档（目录路径须与 `docsDir` 一致） | 重启服务器 |
| 3 | 客户端名单 JSON（`[General] TraderListPath` 指向；默认 `BepInEx/config/SamMeow.LLMTraderChat.traders.json`） | 在 `enabledTraderIds` 数组加入该 `traderId` | 重启客户端 |

### 字段清单（per-trader）

- `traderId`（必填，字符串）：商人的 MongoId，必须与游戏内商人 id 完全一致；缺失/空串 → 该商人被禁用并记日志，其他商人不受影响。
- `name`（字符串）：显示名，用于对话挂载与日志；留空回退为 `traderId`。
- `enabled`（布尔，默认 `true`）：是否注册该商人的聊天机器人。
- `persona`（字符串）：人格提示，参与 system prompt 组装；建议 150–300 字，写清身份、语气、立场与对玩家的态度。
- `docsDir`（字符串）：知识文档目录，**相对 mod 根目录**（如 `knowledge/traders/<traderId>`）。
- `openingMessage`（字符串）：首次可聊天时主动发往该消息线程的一次性欢迎语；空串表示不发。
- `resetKeywords`（字符串数组）：重置对话记忆的关键词，整句等值匹配（trim 后大小写不敏感）。
- `attitudePrompts`（对象，可选）：按态度档位覆写关系指令，键 `hostile` / `cold` / `familiar` / `trusted` / `ally`
  （大小写不敏感），值为该档自定义文案；未配置或留空的档位回退内置通用文案。未知键 / 非字符串值仅告警忽略，不禁用商人。
  详见 `README.md` 的「动态态度（工单 14）」。

### 目录约定

- 知识文档一律放 `knowledge/traders/<traderId>/`，每商人建议 3–6 篇（背景 / 立场 / 业务范围 / 常见问答），格式 `.md` 或 `.txt`。
- 文档目录名与 `traderId` 对齐，`docsDir` 指向之；上表第 1、2 步的目录必须一致，否则检索命中为空（不报错）。

### 两条路径

- **发布默认路径**（随包分发，含一处常量登记）：发布者把新商人写进 `config/defaultConfig.jsonc` 的 `traders[]`，
  并把其 id 加入 `src/Shared/SharedConstants.cs` 的 `DefaultEnabledTraderIds`（客户端默认名单来源）——此步需重编译。
  工单 08 的 Painter 即走此路径。`config.jsonc` 与 `defaultConfig.jsonc` 的 `traders[]` traderId 集合必须一致（由单测守护）。
- **运行时零代码路径**（玩家/整合包就地扩展）：玩家只改**两处文本文件**——服务端 `config.jsonc` 的 `traders[]`（连同
  第 2 步放文档）与**客户端名单 JSON**（第 3 步），**不重编译**即接入一位商人；重启服务器与客户端生效。

> **升级 nuance**：客户端名单 JSON 首次运行即生成并持久化，**随包新增的编译默认不会回流到已有安装**。
> 从旧版本升级后，需手动把新 `traderId` 加进该 JSON，或删除该文件让其按新默认重建；否则新商人不会在客户端解锁。

字段语义与装载行为详见 `src/Server/Config/ConfigLoader.cs`；接缝与决策见
`.scratch/llm-trader-chat/spec.md` 的 Implementation Decisions #8。
