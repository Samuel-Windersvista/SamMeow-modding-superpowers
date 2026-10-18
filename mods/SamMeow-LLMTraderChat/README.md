# LLM Trader Chat（商人 LLM 对话）

在 SPT 的游戏内 Messenger 里与**商人**直接打字对话：客户端解锁商人消息线程的输入，
服务端接住玩家文字、由聊天引擎（人格提示 + 本地知识文档检索 + LLM）生成回复，
以商人身份发回同一消息线程。

- 形态：成对 mod（客户端 BepInEx 5 插件 + 服务端 SPT server mod）+ 共享常量工程 + 测试工程。
- 目标环境：**SPT 4.1.6**（EFT 0.16.9.40743，Mono / BepInEx 5）。
- 许可：MIT。GUID：`sammeow.llmtraderchat`。
- 当前状态：**`0.1.0`（候选）**——M1 骨架 + 03–12 各层与接线已交付（配置 / LLM / 检索 / 引擎核心 / 服务端接线与试点内容 / 工单 08 mod 商人零代码接入 / 文档检索 / 中文标点修复）。
  真引擎已接入 chatbot（路线 A）；**全商人接入批次（工单 13）**：12 位原版商人 + 7 位 mod 商人（共 19 位）全部可聊天。
  原 3 位（Therapist、Peacekeeper、Painter）**M2/M3 实机验收已通过**；全商人批次已配置 / 构建 / 测试（109/109）并部署至整合包，实机验收待跑（见「验收状态」）。

> 规格与工单：`.scratch/llm-trader-chat/spec.md`、`.scratch/llm-trader-chat/issues/`。
> 可行性研究：`docs/research/llm-trader-chat-feasibility.md`。

## 目录结构

```
SamMeow-LLMTraderChat/
├── SamMeow.LLMTraderChat.sln          # 单解决方案（src/Client、src/Server、src/Shared、tests）
├── Directory.Build.props              # 集中版本号 + SptRoot 派生路径 + 运行时引用
├── src/
│   ├── Client/                        # netstandard2.1，BepInEx 5 插件（[BepInPlugin] + Awake）
│   ├── Server/                        # net10.0，SPT server mod（IModMetadata + IOnLoad）
│   └── Shared/                        # netstandard2.0，常量 / 协议，两端共同引用
├── tests/
│   └── SamMeow.LLMTraderChat.Tests/   # xunit + net10.0，引 Server + Shared
├── config/                            # 服务端运行时配置（config.jsonc + defaultConfig.jsonc，随包分发）
├── knowledge/                         # 商人知识文档（knowledge/traders/<traderId>/，随包分发）
├── scripts/pack.ps1                   # 构建 + 打包（单 zip 双端）
├── release/                           # 发行归档（zip 被 .gitignore 忽略）
├── README.md / GLOSSARY.md / LICENSE / CHANGELOG.md / .gitignore
```

## 构建（三步）

```powershell
# 1. 进入项目目录
cd mods\SamMeow-LLMTraderChat

# 2. 还原并构建整个解决方案（默认 SptRoot = E:\Game\EFT_Offline）
dotnet build -c Release

# 3. 跑测试（可选但建议）
dotnet test -c Release
```

- `SptRoot` 在 `Directory.Build.props` 中默认 `E:\Game\EFT_Offline`，可覆盖：
  `dotnet build -c Release -p:SptRoot="D:\SPT"`（或分别覆盖 `-p:SptClientDir=...` / `-p:SptRuntimeDir=...`）。
- 运行时 DLL（BepInEx / Assembly-CSharp / SPTarkov.*）以 `<Private>false</Private>` 引用，不进构建输出。

## 部署（三步）

```powershell
# 1. 打包（产出 release\SamMeow.LLMTraderChat-<version>.zip）
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/pack.ps1

# 2. 解压 zip 到 SPT 游戏根目录（或作为 MO2 overlay 安装）
#    BepInEx\plugins\SamMeow.LLMTraderChat\      <- 客户端半
#    SPT_Runtime\user\mods\SamMeow.LLMTraderChat\ <- 服务端半（含 config\、knowledge\）

# 3. 启动 SPT 服务器 / 客户端；日志关键字：
#    服务端：[LLMTC] LLM Trader Chat v0.1.0 服务端已加载
#    客户端（BepInEx\LogOutput.log）：LLM Trader Chat v0.1.0 客户端已加载（协议版本 1）
```

## 运行机制（M2）

- 客户端：一处 Harmony Postfix（`ChatShared.UpdatableChatDialogue.CanSendMessage`）解锁**名单内商人**消息线程的输入闸门，不新建 UI。名单与总开关为配置驱动（见下）。
- 服务端：路线 A —— 每个启用商人注册一个 `IDialogueChatBot`（DI 收编），`DialogueController.SendMessage` 按 dialogId 路由到 `TraderChatEngine`；不写 `DialogueController` 拦截、不改 `ChatbotFeatures.EnabledBots`。
- 编排：reset 指令识别 → 检索（`knowledgeTopK`）→ prompt 组装（persona + 态度段 `{{attitude}}` + 任务知识段 `{{quests}}` + 片段 + 最近 `historyMessageCount` 条）→ LLM 调用 → 延迟 `replyDelay` 秒后以该商人身份经 `MailSendService` 回信；失败走 `fallbackMessage`，LLM 未配置走「未就绪」提示（不静默、不刷屏）。
- 开场：`GenerateDialogueList` 前缀对启用商人首次发 `openingMessage`（一次性、跨重启 at-most-once）；对话不存在时该消息会创建对话（沿用 M1 补发形态）。
- reset：发送**整句** `reset` / `重置`（trim 后大小写不敏感等值匹配，如「请帮我重置一下」不触发）→ 重置对话记忆：移除无附件消息、**保留带附件的未领取邮件（保险/任务奖励）**并清零角标；随后回 `resetConfirmationMessage`（可空）。
- 日志前缀：`[LLMTC][recv] / [route] / [kg] 命中数 / 来源 / [engine] 耗时与结果 / [reply] / [fallback] / [notready] / [reset] / [opening]`。

## 动态态度（工单 14）

商人在对话中的语气与亲疏立场随玩家**实时关系**动态变化——只影响语气与谈资，不改交易/任务逻辑，也不在对话中报出内部数值。

- **5 档**：敌意/戒备（Hostile）、冷淡/生疏（Cold）、熟稔/认可（Familiar）、信任/器重（Trusted）、核心盟友/过命交情（Ally）。
- **判定输入**（单次回复前读取的关系快照）：目标商人 `Unlocked` / `Standing` / `LoyaltyLevel`；该商人名下任务（已完成 / 失败计数、活跃任务名 ≤5、最近完成任务名 ≤3）；玩家 PMC 阵营与等级。无记录或读取失败时降级为 `Cold` 并记 warning。
- **判定规则**：`Unlocked == false` 或 `Standing < 0` → Hostile；否则按忠诚等级（未知视为 1）：`<=1` Cold、`2` Familiar、`3` Trusted、`>=4` Ally。
- **注入**：默认模板（`config/prompts/system-zh.txt`）含 `{{attitude}}` 占位符；若使用**自定义模板且不含** `{{attitude}}`，态度块会自动拼到 `{{persona}}` 展开结果末尾（旧模板无缝兼容）。
- **反泄露**：注入块以禁令收尾——严禁报出好感度数值、忠诚等级数字（如「1级/LL2/皇冠」）、玩家等级数字或任务内部 ID；玩家背景仅以定性等级带（新手 / 有经验的佣兵 / 老手）和阵营表达。
- **可选覆盖**：`traders[i].attitudePrompts` 对象按档覆写关系指令，键 `hostile` / `cold` / `familiar` / `trusted` / `ally`（大小写不敏感）；未配置或留空的档位回退内置通用文案。非法键 / 非字符串值仅告警忽略，不禁用商人。
- **可观测性**：服务端日志 `[LLMTC][attitude] 档位=<tier>(unlocked=..,ll=..,standing=..,done=..,fail=..,active=..) 商人=<name>`。

```jsonc
{
  "traders": [
    {
      "traderId": "54cb57776803fa99248b456e",
      "attitudePrompts": {
        "hostile": "自定义的敌意档关系指令……",
        "ally": "自定义的盟友档关系指令……"
      }
    }
  ]
}
```

## 任务知识（工单 16）

商人在对话中能被问到「我名下进行中的委托」的细节，且**披露粒度随态度档位变化**，同时**严禁编造**。新增 prompt 段 `{{quests}}`（默认模板已含）。

- **注入范围**：仅该商人名下 `Started` + `AvailableForFinish` 的委托；委托名由 `{{attitude}}` 段承担，细节由本段承担，避免重复。
- **披露档位**（单一映射来源 `DisclosurePolicy.FromAttitudeTier`）：
  - `Hostile` / `Cold` → **None**：本段不含任何委托名或细节，但**仍保留反编造约束行**（**不空段** —— 实机 Cold 档缺陷要求 prompt 恒含反编造约束）。
  - `Familiar` → **Hint**：仅委托名 + 地图/地区级提示 + 一句通用引导，**不含**房间号 / 钥匙名 / 目标原文。
  - `Trusted` / `Ally` → **Full**：委托名 + 目标明细 + 描述摘要（≤300 字）。房间号/钥匙名等属玩家任务面板本就可见的信息，允许交底。
- **数据来源**：玩家进度 `PmcData.Quests` × 运行时已合并 mod 任务的 `TemplateTable.Quests`（`TraderId` 归属筛选）；文本取自 `LocaleService`——委托名 `<qid> name`、描述 `<qid> description`、目标为**裸 24-hex 条件 id** locale 键（`Quest.Conditions.AvailableForFinish[].Id`）、地区 `<locationId> Name`。
- **预算**：单条委托 ≤500 字、整段 ≤800 字（**独立于** `{{attitude}}` 的 600 字预算）；超预算丢弃尾部条目并记 `[LLMTC][engine]` 告警。条数上限由提供器单一负责（与态度段的 5 条共享），渲染层只做字数预算。段恒以反编造禁令收尾，**所有档位、含零委托与截断路径**皆然。
- **占位符兼容**：模板缺 `{{quests}}` 时自动把该段追加到 `{{persona}}` 展开结果之后（同 `{{attitude}}` 的旧模板兼容做法）；单趟替换不二次展开；模板缺 `{{persona}}`/`{{attitude}}`/`{{quests}}` 时告警。
- **反编造硬约束**：`{{quests}}` 段与默认模板均含「不知道就明说不知道、禁止用『不归我管』之类借口搪塞、不得报内部数值或任务内部 ID」。
- **可观测性**：服务端日志 `[LLMTC][quests] Disclosure=<None|Hint|Full> quests=<n> 商人=<name>`（与 `[LLMTC][attitude]` 并列）。

### 任务手册生成（工单 17 集成）

- **产物**：`knowledge/traders/<traderId>/05-quests.md`——每位有委托的商人的一份静态手册，每任务一节（`## 名称` + 类型 / 地点 / 目标 / 「委托人原话」摘要）。
- **生成器**：`scripts/gen-quest-manuals.mjs`（Node，无第三方依赖）。以任务表为唯一事实来源（**绝不反向从 locale 生成**，避免幽灵条目）；任务名 / 描述 / 目标取自 locale，目标以 `AvailableForFinish` 条件的**裸条件 id** 为键。
- **参数**：`--spt-data <dir>`（SPT_Data 根）、`--mods-root <dir>`（MO2 mods 根）、`--out <dir>`（输出 traders 目录）、`--no-mods`（仅生成原版）、`--split-bytes <n>`（单文件字节上限，超限拆为 `05-quests-1.md` …；默认 204800）。
- **幂等**：每次运行先删除目标目录内旧的 `05-quests*.md` 再全量重写（连续两次运行 SHA256 全同）；输出 UTF-8 无 BOM、LF 行尾。
- **零任务商人**：Storyteller / Croupier / Hideout Harry / Courier 名下无委托，**有意不产出** `05-quests.md`（其余 15 位各一份）。
- **已知降级（不修）**：Artem / Vagabond / WTT-Armory 三源任务文本仅英文；Mechanic 1 条 + Ref 9 条目标在 locale 中无文本被跳过（共 10 条）。实测未触发拆分（最大文件 Mechanic 约 72 KB，远低于阈值）。

## LLM 前置（随包默认：云端 DeepSeek）

随包 `config/config.jsonc` 的默认 `llm` 段为**云端 DeepSeek**：

- `endpoint`：`https://api.deepseek.com`（OpenAI 兼容 base，**无需 `/v1`**；对话端点为 `/chat/completions`）。
- `model`：`deepseek-flash`。
- `apiKey`：随包为占位符 `sk-在此填入你的DeepSeek密钥`——**不填则请求返回 401**，商人会回 `fallbackMessage` 兜底话术（不静默、不刷屏）。到 `https://platform.deepseek.com/api_keys` 申请后填入即可。

想要**不出网**的备选：改用本地 Ollama——安装 Ollama → `ollama serve` → `ollama pull llama3.1`，把 `llm` 段替换为配置注释中给出的三行（`endpoint=http://127.0.0.1:11434/v1`、`apiKey` 留空、`model=llama3.1`）。

> **隐私披露**：默认配置（DeepSeek）下**聊天内容会发送到云端端点**；仅当你显式改用本地 Ollama 时聊天内容才不出网。密钥与聊天内容由服务端持有与转发，日志绝不输出 `apiKey`。

> 说明：代码内 `LlmConfig` 的兜底默认仍是本地 Ollama（隐私优先），仅在配置缺 `llm` 段时生效；随包 `config.jsonc`/`defaultConfig.jsonc` 恒存在且显式给出 DeepSeek，故实机默认即云端。详见 `docs/adr/0002-openai-compatible-llm.md` 的状态注记。

## 客户端配置（工单 07 起）

- 总开关：`BepInEx/config/sammeow.llmtraderchat.cfg` → `[General] Enabled`（默认 true；false 时客户端不改变任何行为）。
- 商人名单：由 `[General] TraderListPath` 指向的 JSON 文件（默认 `BepInEx/config/SamMeow.LLMTraderChat.traders.json`）承载；发布默认名单＝12 位原版商人 + 7 位 mod 商人（共 19 位；权威列表见 `src/Shared/SharedConstants.cs` 的 `DefaultEnabledTraderIds`）。文件形如（节选）：
  ```json
  { "enabledTraderIds": ["54cb50c76803fa8b248b4571", "54cb57776803fa99248b456e", "668aaff35fd574b6dcc4a686", "..."] }
  ```
  **新增商人只需改此文件与服务端 `config.jsonc`，无需重编译客户端**；重启生效。

## 新增商人：最小接入契约（工单 08）

本 mod **配置驱动**：新增一位商人只动配置与文档，不新增业务代码。接入有两条路径——**发布默认路径**（随包，
含一处常量登记，需重编译）与**运行时零代码路径**（改两处配置文本文件，无需重编译）。工单 08 的现成 mod 商人
**Painter（油漆匠，id `668aaff35fd574b6dcc4a686`）** 走**发布默认路径**接入（证据见《永远的诺文斯克》包内
`Painter-4.0/db/base.json`）。最小操作清单：

| # | 文件（部署后路径） | 改什么 | 何时生效 |
|---|--------------------|--------|----------|
| 1 | 服务端 `SPT_Runtime/user/mods/SamMeow.LLMTraderChat/config/config.jsonc` 的 `traders[]` | 追加一个对象：`traderId`（必填）+`name`+`enabled`+`persona`+`docsDir`+`openingMessage`+`resetKeywords` | 重启服务器 |
| 2 | 服务端 `.../SamMeow.LLMTraderChat/knowledge/traders/<traderId>/` | 放入 `.md`/`.txt` 知识文档（目录须与 `docsDir` 一致） | 重启服务器 |
| 3 | 客户端名单 JSON（`[General] TraderListPath` 指向；默认 `BepInEx/config/SamMeow.LLMTraderChat.traders.json`） | 在 `enabledTraderIds` 数组加入该 `traderId` | 重启客户端 |

两条路径：

- **发布默认路径**（随包，含一处常量登记，需重编译）：把新商人写进 `config/defaultConfig.jsonc` 的 `traders[]`，
  并把其 id 加入 `src/Shared/SharedConstants.cs` 的 `DefaultEnabledTraderIds`（客户端默认名单来源）。
- **运行时零代码路径**（玩家/整合包就地扩展，无需重编译）：只改**两处配置文本文件**——服务端 `config.jsonc`
  的 `traders[]`（连同第 2 步放知识文档）与**客户端名单 JSON**（第 3 步）；重启服务器与客户端生效。
  > **升级 nuance**：客户端名单 JSON 首次运行即生成并持久化，**随包新增的编译默认不会回流到已有安装**。
  > 升级后需手动把新 `traderId` 加进该 JSON，或删除该文件使其按新默认重建；否则新商人不会在客户端解锁。

字段清单、目录约定与装载行为详见 `config/README.md`；完整字段语义见 `src/Server/Config/ConfigLoader.cs`
与 `.scratch/llm-trader-chat/spec.md` 的 Implementation Decisions #8。

## 知识文档与检索（M3）

- **目录结构**：每位商人的文档放在 `knowledge/traders/<traderId>/`（须与配置 `docsDir` 一致），支持 `.md` / `.txt`。
  检索按 Markdown 标题/段落分块，按关键词命中打分（中文按双字 bigram 兜底），取 `knowledgeTopK` 条注入 system prompt。
- **内容建议**：每商人 3–6 个主题文件（`01-background` 背景 / `02-stance` 立场 / `03-services` 业务 / `04-faq` 常见问答）；
  把常见问法（如「你都有什么」「你都卖什么」）写进问答，可提高命中率。
- **更新方式**：增改对应目录下的 md/txt 后**重启服务器生效**（检索器按 `docsDir` 缓存分块，运行中不热重载）。
  纯文本改动，无需改代码。
- **可观测性**：命中见服务端日志 `[LLMTC][kg] 命中数=N 来源=<命中文件名（去重）>`；无命中为 `来源=无`。
  无命中时 prompt 注入「（无相关知识片段）」，模型据此自然降级、不硬答。
- **随包文档**：`knowledge/traders/<traderId>/` 覆盖全部 19 位商人——12 位原版（Prapor / Therapist / Fence / Skier / Peacekeeper / Mechanic / Ragman / Jaeger / Lightkeeper / BTR Driver / Ref / Storyteller）＋ 7 位 mod（Painter / Artem / Scorpion / Croupier / Hideout Harry / Badger / Courier）。其中 15 位有委托的商人另含自动生成的 `05-quests.md` 任务手册（见「任务手册生成（工单 17 集成）」）；4 位零任务商人有意无此文件。

部署（不改游戏安装目录，仅解压）：

```powershell
# 1. 打包
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/pack.ps1
# 2. 将 release\SamMeow.LLMTraderChat-<version>.zip 解压到测试实例游戏根（合并目录）：
#    E:\Game\EFT_Offline\SPT_41x\        （内含 BepInEx\ 与 SPT_Runtime\）
# 3. 启动服务器 + 启动游戏，进入主菜单 Messenger；对 Therapist / Peacekeeper / Painter 对话打字。
```

日志关键字：服务端 `[LLMTC][recv]` / `[LLMTC][route]` / `[LLMTC][kg]` / `[LLMTC][engine]` / `[LLMTC][reply]` / `[LLMTC][opening]`；客户端 `BepInEx\LogOutput.log` 的 `[LLMTC] ... 客户端已加载`。

## 验收状态（M3）

- **spec 六条清单**（详细证据见 `docs/acceptance.md` §一）：① 两商人多轮对话—**通过**（工单 07/09 评论 M2 实测 + 服务器日志）；② 文档答案—**通过**（工单 10 评论问答清单；M3 实测「提问都没问题」）；③ 重启保留—**通过**（工单 09 评论重启实测 + profile 落库）；④ 日志无错—**通过**（`overwrite\SPT_Runtime\user\logs\spt\spt20261014.log` 无 `[LLMTC]` 报错）；⑤ 共存—**按「未同装」结案 + 代码侧冲突分析**；⑥ 未启用商人无变化—完成。
- **全商人接入批次（工单 13）**：配置 19 位（含 persona / 开场语）、文档 76 篇（19 × 4）随包；单测 **109/109**（含「客户端默认名单覆盖」守卫）；部署（overlay 刷新 + key 保留 + 客户端名单 19 位同步）已完成。实机验收（服务器日志 `[LLMTC][route]` ×19 + 抽测新商人对话）待 Overseer 跑通后补记。
- **共存**：同类聊天 mod（Trash Talk / Miyako）**未同装**（Overseer 决策）；代码侧冲突分析（补丁点 / 作用域 / 发送路径对比 + 残留观察点）见 `docs/acceptance.md` §二 —— 结论：点位/条件不重叠，预判可共存。
- **标准检查**：`check-mod-standard -TargetSptVersion 4.1.6` → `PASS=27 / FAIL=3`；3 条 FAIL 为检查器缺陷误报，豁免记录见 `docs/standards-waiver.md`（STD-BUILD-001 / STD-BUILD-006 / STD-META-003）。

## 隐私与 AI 声明

- **聊天内容出站**：玩家消息、对话历史与检索到的知识片段会组成 prompt 发送到 `config.jsonc` 配置的 LLM 端点。
  - 随包默认示例为**云端 DeepSeek**（`https://api.deepseek.com`，**出网**）——使用时须填入自己的 API key。
  - 备选**本地 Ollama**（`http://127.0.0.1:11434/v1`，**不出网**）——在意隐私请改用本地模型（配置注释已给出替换行）。
- **AI 生成回复**：商人回复由大语言模型生成，可能与游戏设定不符或产生虚构内容；本 mod 不保证回复的准确性。
- **密钥安全**：API key 仅存于服务端 `config.jsonc`；日志与异常不输出密钥。

## 里程碑

- **M1 骨架**：解锁输入 → 服务端接话 → 回显（已实机验证，路线 A 成立）。
- **M2**：接 LLM + 检索 + 引擎接线 + 试点内容（Therapist / Peacekeeper）+ mod 商人零代码接入验证（Painter，工单 08）——**已实机验收通过**。
- **M3**：文档驱动问答验收——**已实机验收通过**；共存按「未同装」结案（代码侧分析见 `docs/acceptance.md`）。
- **全商人接入（工单 13）**：《永远的诺文斯克》12 位原版 + 7 位 mod 商人全部接入（配置 / 文档 / 测试 / 部署收口；实机验收待跑）。

## 规则对照（要点）

- 客户端 netstandard2.1（Mono 可加载），服务端 net10.0；Shared netstandard2.0。
- 服务端 `user/mods/<Mod>/` 目录内**只允许一个** `IModMetadata` 实现（即 `Server.dll`；`Shared.dll` 不实现）。
- 版本号只定义一次（`Directory.Build.props` 的 `<Version>`），代码经生成的 `ModVersion.Value` 引用。
- 运行时引用一律 `HintPath` + `Private=false`，路径自 `SptRoot` 派生、可命令行覆盖。
