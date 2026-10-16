# 14: v2 动态态度（好感度 + 忠诚等级 + 任务进度）

**What to build:** 让 19 位商人在聊天中的语气与立场随玩家的实时存档状态（与该商人的好感度 `Standing`、忠诚等级 `LoyaltyLevel`、解锁状态 `Unlocked`、该商人名下的任务进度、以及玩家 PMC 阵营与等级）动态变化，并在服务端日志中输出态度判定摘要以供验收，同时严禁向玩家泄露内部数值或系统术语。

**Blocked by:** 06: 聊天引擎（已完成）, 07: 服务端接线（已完成）, 13: 全商人接入（已完成）

**Status:** in-progress

## 决策记录（2026-10-15 grilling 确认）

1. **状态信号范围（Q1 = A）**：
   - 目标商人 `TradersInfo[traderId]`：`Unlocked`（`bool?`）、`Standing`（`double?`）、`LoyaltyLevel`（`int?`）。
   - 目标商人名下任务进度（`PmcData.Quests` × `TemplateTable.Quests[qid].TraderId == traderId`）：已完成数（`Success`）、失败数（`Fail` / `FailRestartable` / `MarkedAsFailed` / `Expired`）、当前活跃任务中文名（`Started` / `AvailableForFinish`，最多前 5 个并标注「进行中」或「待交付」）、最近完成任务中文名（`Success`，按完成时间戳/开始时间降序最多前 3 个）。
   - 玩家 PMC 基本身份（`PmcData.Info`）：阵营 `Side`（`Usec` / `Bear`）与等级 `Level`（`int?`）。
   - 任务中文名获取链：`LocaleService.GetLocaleDb("ch")["{qid} name"]` → `LocaleService.GetLocaleDb()["{qid} name"]` → `Quest.QuestName` → `Quest.Name`。
2. **5 档态度分级模型（Q2 = A）**：
   - `Hostile`（敌意/戒备）：`Unlocked == false` 或 `Standing < 0`
   - `Cold`（冷淡/生疏）：`LoyaltyLevel <= 1`（或存档无该商人 `TradersInfo` 记录 / 字段为空时的默认档）
   - `Familiar`（熟稔/认可）：`LoyaltyLevel == 2`
   - `Trusted`（信任/器重）：`LoyaltyLevel == 3`
   - `Ally`（核心盟友/过命交情）：`LoyaltyLevel >= 4`
3. **通用档位语义 + per-trader 可选覆写（Q3 = A）**：
   - 内置 5 档通用中文关系指令，交由商人既有 `persona` 自然染色。
   - `TraderConfig` 新增可选字段 `attitudePrompts`（`Dictionary<string, string>`，键支持 `hostile` / `cold` / `familiar` / `trusted` / `ally`，大小写不敏感）；未配置或某档留空时回退内置通用指令。
   - `ConfigLoader` 校验 `attitudePrompts`：非对象或未知档位键/非字符串值时产生告警并忽略非法项，不禁用商人。
4. **任务进度注入粒度与预算（Q4 = A）**：
   - 计数摘要 + 活跃任务名（≤5）+ 最近完成任务名（≤3）；单任务名超长截断（≤60 字），`{{attitude}}` 总段预算上限（≤600 字），超预算截断并记录告警。
5. **反泄露边界（Q5 = A）**：
   - 在注入块与默认模板中明确要求：严禁在回复中报出好感度小数、忠诚等级数字（如“1级忠诚度/LL2/皇冠”）、玩家等级数字或任务内部 ID，只允许用符合人设的自然口吻表达亲疏与委托记忆。
6. **Prompt 占位符与向后兼容（Q6 = A）**：
   - `config/prompts/system-zh.txt` 与 `SystemPromptDefaults.Template` 新增 `{{attitude}}` 占位符。
   - 单趟替换正则扩展为 `\{\{(persona|attitude|knowledge|history)\}\}`（防二次展开）。
   - 若用户自定义模板不含 `{{attitude}}`，自动将态度块拼入 `{{persona}}` 展开结果末尾，保证旧模板无缝获得动态态度。
   - 一次性开场白（`openingMessage`）保持静态配置不变。
7. **可观测性**：
   - `TraderChatOutcome` 携带本次判定的 `AttitudeTier` 与简要诊断串（如 `Cold(unlocked=true,ll=1,standing=0.20,done=2,active=1)`），`TraderChatBot` 输出 `[LLMTC][attitude] 档位=... 商人=...` 日志供实机验收判读。

## 测试接缝（Pre-agreed TDD Seams）

1. **`TraderAttitudeEvaluator` / `TraderChatEngine`（引擎接缝）**：
   - 5 档判定边界（`Unlocked == false`、`Standing < 0`、`LL1`/`null`、`LL2`、`LL3`、`LL4+`）。
   - 玩家阵营/等级、活跃任务（进行中/待交付）、最近完成任务、已完成/失败计数的渲染格式与反泄露禁令注入。
   - `TraderConfig.AttitudePrompts` 覆写特定档位文案 vs 回退内置默认文案。
   - 自定义模板不含 `{{attitude}}` 时的自动追加兼容行为；单趟替换不二次展开 `{{...}}`。
   - 提供器抛异常或返回 `null` 时优雅降级为默认 `Cold` 档并记录 warning，不中断对话。
2. **`ConfigLoader`（配置装载接缝）**：
   - `traders[i].attitudePrompts` 正常解析（大小写不敏感键）、未知档位键告警忽略、非对象/非字符串值告警回退。
   - `ShippedSystemPromptTemplate_MatchesBuiltInDefault` 包含 `{{attitude}}`。
3. **SPT 适配层（`SptTraderRelationshipProvider`）**：
   - 构造注入 `ProfileHelper`、`TemplateTable`、`LocaleService`；在 `ConfigRegistration` 注册为 `ITraderRelationshipProvider` 单例。

## 审查记录（2026-10-15 双轴 code-review，@oracle 并行）

- **Standards 轴**：3 项硬性/判断项 → 已修订（`SptTraderRelationshipProvider` 静默吞异常 → 记日志；`AttitudeTier` 注释「商店」→「商人」；档位键清单单一归属 `TraderAttitudeKeys`；`config/README.md` 补齐 `{{attitude}}`/`attitudePrompts`；测试魔数 600 → 常量）。
- **Spec 轴**：Q5 默认模板禁令半项缺失 + Q6 自定义模板双占位符皆缺的静默路径 → 已修订（默认模板追加「不报内部数值/系统术语」条款；引擎在模板缺 `{{persona}}` 且缺 `{{attitude}}` 时告警）。
- **已驳回（附证据）**：Spec 轴提出 `MongoId.IsValidMongoId(string)` 疑为「非空检查、形同虚设」——已核对 4.1.6 源码 `Libraries/SPTarkov.Server.Core/Extensions/MongoIdExtensions.cs:48-69`，实为 **24 位十六进制格式校验**，守卫有效，保留原样。
- **有意偏离（记录在案）**：Q1 的字面「注入 Level (int?)」改为**定性等级带**（`<15` 新手 / `15–34` 有经验 / `>=35` 老手）+ 阵营规范化——为配合反泄露边界，避免向模型提供可直接复述的原始等级数字；档位/委托/阵营信号均保留。

## 验收清单

- [x] 新增与更新单测全部通过（`dotnet test -c Release`：**144/144**，0 失败；orchestrator 独立复验）
- [x] `Server` / `Client` / `Shared` Release 构建 0 错误 0 警告（orchestrator 独立复验）
- [x] `@oracle` 双轴审查（Spec / Standards）通过 + 修订回归（R1–R7；144/144）
- [x] 打包 `release\SamMeow.LLMTraderChat-0.1.0.zip`（149,679 B）并同步部署至整合包 overlay（最小保 key 集：`Server.dll` + `system-zh.txt` + `config/README.md` + 根 `README.md`/`GLOSSARY.md`；`config.jsonc` 未动以保真实 key；`downloads\llmtc-m1.zip` 已同步）
- [x] **实机验收（2026-10-16 通过，日志取证）**：每条消息输出 `[LLMTC][attitude] 档位=Cold(unlocked=True,ll=1,standing=0.24,done=2,active=2)`；修复 `maxTokens`（见下）后 6/6 轮均为真实回复（93–150 字，含 `[kg]` 命中 `02-stance.md` / `04-faq.md`），无告警、无空响应。
  - 未做（可选）：与高忠诚商人（`ll>=2` → Familiar/Trusted/Ally）的语气体感对比——档位映射已由 23 条评测器单测与 `Cold` 实机档位覆盖；如需体感证据，可用老主顾商人复核。
  - [WARN] 实机曾出现全量兜底话术，根因**不在本工单**：`deepseek-flash` 为推理型模型，`maxTokens=512` 被推理链吃满 → API 返回 `finish_reason=length` + 空 `content`。已按配置修复（`maxTokens: 4096`）；诊断取证与代码层健壮性另见工单 15。

## 部署记录（2026-10-15）

- 备份：覆盖前 5 个将被替换的 overlay 文件已存入 `.artifacts/overlay-backup-llmtc-v2-20261015\`（回滚用）。
- 运行态：部署时游戏与 SPT 服务器均未运行（MO2 开着，按规程允许）。
- 核验：overlay `SamMeow.LLMTraderChat.Server.dll` 102,400 B，二进制含 `EvaluateTier` / `RenderAttitudeBlock` 符号；`system-zh.txt` 含 `{{attitude}}`；`config.jsonc` 保留真实 apiKey（placeholder=False）。
