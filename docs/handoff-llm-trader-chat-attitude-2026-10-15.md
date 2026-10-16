# 交接文档：LLM 商人聊天 v2 —— 动态态度（任务进度 + 好感度）— 2026-10-15

> 状态：**全商人接入批次（工单 13）已收口**——19 位商人（12 原版 + 7 mod）实机验收通过、已部署至整合包。
> 新会话目标：**v2 特性——商人根据玩家的任务进度与好感度（standing）等状态，动态改变对玩家的态度**。
> 本文只放「接手动线」与新增事实；环境与部署细节一律引用既有工件，不复制。

## 1. 一页索引（建议阅读顺序）

| 工件 | 路径 | 说明 |
|---|---|---|
| Mod 源码 | `mods/SamMeow-LLMTraderChat/` | `README.md`＝入口（19 位商人清单/最小接入契约/隐私与 AI 声明）；`docs/acceptance.md`（§四＝全商人接入验收记录） |
| Spec 与工单 | `.scratch/llm-trader-chat/` | `spec.md` + `issues/01–13`（13＝全商人接入，已收口；含部署与验收证据） |
| 上一份交接 | `docs/handoff-llm-trader-chat-2026-10-15.md` | **环境状态（§2）与关键操作手册（§4：保 key 部署循环、PowerShell 坑、日志位置）仍然有效** |
| 时间线 | `docs/dev-log.md` | 「2026-10-15（续 2）」（全商人接入）、「（续 3）」（验收通过）两条 |
| ilspy 触点 dump | `.artifacts/llm-trader-chat-ilspy/` | 4.1.6 双端 dumps；新特性涉及 Profile/Quest API 时先在此查触点 |
| 知识库 | `knowledge/spt-kb/`（`index.json` → 过滤） | 4.1 服务端 API / profile / quest / trader 数据事实；`curated/api-notes-4.1/` |

## 2. 环境状态（相对上一份交接的增量）

- **部署 overlay**：`[9]全局功能大修-商人LLM对话-llm-trader-chat-test-build`——全商人批次最终构建（`Server.dll` 78,848B / `Shared.dll` 11,264B @ 2026-10-15 00:57:09；zip `release\SamMeow.LLMTraderChat-0.1.0.zip` 137,701B）。
- **运行时客户端名单**：`Norvinsk_Forever\overwrite\BepInEx\config\SamMeow.LLMTraderChat.traders.json` = **19 位**（权威列表 = `src/Shared/SharedConstants.cs` 的 `DefaultEnabledTraderIds`）。
- **实机验收**：2026-10-15 通过（服务器日志 `spt20261014.log` 17:15 起：`启用商人=19/19`、`[route]`×19、16 位新商人开场、4 位新商人真对话含 `[kg]` 命中、无告警；证据在工单 13 评论 + `acceptance.md` §四）。
- **测试基线**：**109/109**（`dotnet test mods/SamMeow-LLMTraderChat/SamMeow.LLMTraderChat.sln -c Release`）。
- **LLM**：DeepSeek `deepseek-flash`；key 仅存于部署副本 `config.jsonc`（mask `sk-75***c1e1`；仓库无 key）。
- **仓库**：仍**全部未提交**（按 Overseer 规则）。发布动作（commit / Forge / release notes）仍未执行。

## 3. 下一工作：v2 动态态度（任务进度 + 好感度）

### 目标（一句话）
商人在对话中的**语气与立场**随玩家的任务进度、与该商人的好感度（standing）/ 忠诚等级等状态变化——低好感冷淡敷衍，高好感信任亲近；且**不向玩家泄露内部数值**。

### 已知接缝与事实（供 grilling 用，细节以 KB/ilspy 为准）
- **引擎主接缝**：`src/Server/Engine/TraderChatEngine.cs`——`HandleMessageAsync(sessionId, traderId, text)` 组装 prompt（persona + 知识片段 + 历史）；模板 `config/prompts/system-zh.txt` 现有占位符 `{{persona}}` / `{{knowledge}}` / `{{history}}`。
- **数据获取先例**：`src/Server/Spt/SptChatHistoryProvider.cs`（profile 读写模式，含 sessionId → profile 路径）；服务端可注入 `ProfileHelper`（`SPTarkov.Server.Core.Helpers.Profile`）。
- **候选数据源**（待 grilling 核实）：`Profile.TradersInfo[traderId]`（standing / loyaltyLevel / salesSum / unlocked）、`Profile.Quests`（qid + status；任务↔商人归属映射）；mod 商人的 `TradersInfo` 同样存在（默认值需实测）。
- **预算约束**：engine 有分段 prompt 预算（见 dev-log 评审记录）；新注入段需纳入预算与截断策略。
- **接缝风格**：项目偏好「接口 + 工厂」形态（参照 `IKnowledgeRetriever` / `IKnowledgeRetrieverFactory`）——态度计算器可类比（决策待定）。
- **测试先例**：`tests/SamMeow.LLMTraderChat.Tests/TraderChatEngineTests.cs` 的 Fake provider 风格（新增 provider 沿此写 Fake）。

### 开放问题（新会话开局 grilling 清单）
1. **数据面**：只用 standing + loyalty？是否叠加任务进度（按商人归属？全局完成度？）与其他维度（等级/时长）？
2. **分级模型**：几档态度（如 陌生/熟客/信任/过命）？阈值按 standing + loyalty 还是 per-trader 自定义？
3. **表达方式**：prompt 新占位符（如 `{{attitude}}`）vs 拼进 persona？影响模板兼容与协议版本（`SharedConstants.ProtocolVersion`）。
4. **per-trader 覆盖**：19 位商人是否各自可配档位文案，或统一档位语义交 persona 吸收？
5. **反泄露边界**：明确「不报数值」的产品语气（或允许模糊提及？）。
6. **缓存/时机**：每条消息实时读 profile（内存操作）是否足够；是否需要失效策略。
7. **验收标准**：如何构造「低好感 vs 高好感」对比实测（改档前后语气差异可判读）。

## 4. 关键操作手册（增量提示）

- **部署 / 保 key / 打包 / 测试 / 日志位置**：沿用上一份交接 §4，无变化（PowerShell 对 `[9]` 等含方括号路径必须 `-LiteralPath`；部署＝zip → overlay robocopy + 注入旧 key + 核验 placeholder=False + 客户端名单同步 + `downloads\llmtc-m1.zip` 同步）。
- 本轮验收日志在 `spt20261014.log`（17:15 起）；服务器日志按日期滚动（`sptYYYYMMDD.log`）。
- 新特性改动后：跑 `dotnet test`（基线 109）并确认既有验收不回退（「未启用商人零影响」「重置选择清空」等）。

## 5. Suggested skills（新会话按需调用 Skill 工具）

- 开局钉需求：`grill-with-docs`（动态态度的开放问题见 §3；存在多个需要人机对话的设计决策）。
- 单窗口路径：`implement`（内驱 `tdd` → `code-review`）；多窗口（预计 >150K token）：`to-spec` → `to-tickets` → 每张工单 `implement`。
- 查 SPT API / profile / quest 事实：`spt-mcp-automation`（KB 优先，必要时 Forge/API 文档）。
- 发布动作（仍未做，可与新特性并行或先行，Overseer 决定）：`code-review`（提交批次复审）。
- 再次交接：`handoff`。`using-spt-modding-superpowers` 由插件自动注入。

## 6. 安全与红线

- 不 commit（除 Overseer 明确指令）；**不把 DeepSeek key 写入仓库或任何文档**（本文仅记 mask）。
- 游戏安装目录（`SPT_41x`）不做直写物理修改——一律 overlay（或既定「文件级覆盖 + 备份」流程）。
- agent 不代启动 MO2 / 游戏 / 服务器；实机验收由 Overseer 操作并回报，agent 读日志核验。
