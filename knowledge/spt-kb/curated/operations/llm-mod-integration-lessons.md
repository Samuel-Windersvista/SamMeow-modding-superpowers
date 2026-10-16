---
version: [4.1]
domain: both
topic: llm-integration
title: "LLM 接入 SPT mod 的工程复盘（商人聊天项目 0.1.0，2026-10-14/15 实证）"
keywords: [LLM, OpenAI兼容, DeepSeek, apiKey, ASCII header, prompt预算, 截断告警, RAG, 关键词检索, 检索适配口, bigram, IDialogueChatBot, MailSendService, MO2, overwrite, 文件级覆盖, 保key, 人设蒸馏, 延迟回复, 失败面, 结果对象, 占位符]
summary: "SamMeow-LLMTraderChat 全流程复盘的可复用经验：① LLM 失败面以结果对象表达（不抛业务异常；catch 面须覆盖 URL 构造与 body 读取）；② apiKey 进 HTTP 头，非 ASCII 占位符触发 Request headers must contain only ASCII characters——装载层做『非 ASCII=占位符』启动警告（空 key 不警告以兼容本地 Ollama）；③ endpoint 用绝对 http/https（DeepSeek 2026-10 文档 base_url 不带 /v1、模型 deepseek-flash）；④ prompt 需『逐条+总量』字符预算与截断告警（防小上下文溢出被误判为网络错）；⑤ 轻量 RAG 走适配口 IKnowledgeRetriever + 关键词实现（CJK bigram；常见问法命中 0 优先文档侧补问法而非调参），命中数/来源进日志；⑥ 一次性状态键含 sessionId 防跨档串扰，文件落 MO2 overwrite；⑦ 开发循环用手动『文件级覆盖+保 key』替代需 live broker 的 reinstall；⑧ 人设蒸馏自 lore sections+任务文案，输出 2–5 句注入 system prompt。"
---
# LLM 接入 SPT mod 的工程复盘：商人聊天项目（0.1.0，2026-10-14/15 实证）

> 状态：已验证（12 工单闭环、108 单测、三轮实机验收；成品 `mods/SamMeow-LLMTraderChat/`）
> 定位：跨项目可复用的"LLM 接入 mod"经验清单；SPT 侧聊天管线本身见 `../recipes/13-trader-chat-pipeline.md`

## LLM 接入层（OpenAI 兼容）

- **失败面=结果对象**：NotConfigured / HttpError / Timeout / InvalidResponse，不抛业务异常；调用方取消按契约传播。catch 面须覆盖 **URL 构造**（畸形 endpoint 的 `UriFormatException`/`InvalidOperationException`）与 **body 读取**（`IOException`/`HttpRequestException`）——只 catch 超时会漏。
- **apiKey 坑（实测事故）**：key 进 `Authorization` 头 → 含非 ASCII（如未替换的中文占位符）直接抛 `Request headers must contain only ASCII characters`（请求未出即败，落兜底话术）。装载层启发式：非 ASCII 即"疑似占位符"启动警告；**空 key 不警告**（本地 Ollama 合法）。
- **endpoint/model**：绝对 http/https 校验；DeepSeek 文档（2026-10）base_url=`https://api.deepseek.com`（**不带 /v1**）、模型名 `deepseek-flash`（旧 `deepseek-chat` 已弃用）；示例配置给 Ollama/DeepSeek 双形态、key 留占位符并注明"不填 → 401 → 兜底"。
- **prompt 尺寸预算**：历史/知识片段"逐条 + 总量"字符上限 + 截断告警——否则小上下文本地模型溢出会被归类为网络错，玩家观感是"商人联系不上"（诊断语义误导）。
- **延迟与体验**：回复延迟数值应与客户端刷新行为共同设计（延迟到达靠轮询/重开对话刷新；可选 UI 刷新补丁）。

## 知识库 / RAG（轻量起步）

- **适配口模式**：`IKnowledgeRetriever(query, topK) → 片段{来源/标题/正文/分}`；v1 关键词实现（CJK bigram + 整段权重；已知边界：跨词误报、单字符/数字漏报），嵌入后端可同构替换。
- **调优顺序**：常见问法命中 0 → 先**文档侧补问法/条目**（便宜、可审），再考虑停用词/权重，最后才换检索后端。
- **可观测性**：命中数与**来源**进日志（`[kg] 命中数=N 来源=a.md,…`）——验收与排障的第一证据面。

## 状态与持久化（SPT 侧）

- 一次性状态（开场标记等）键含 `sessionId`（=profile id）→ 跨档天然隔离；文件写 mod 目录时，MO2 下实际落 `overwrite\`（路径保真）。
- 运行时不热重载（检索缓存、配置装载在启动）：**改文档/配置=重启生效**（README 写明）。

## MO2 开发循环（部署经验）

- **手动"文件级覆盖"**替代 MCP `reinstall`（后者需 live broker；broker 插件未装即不可用）：robocopy 到 overlay 目录；前提=关游戏+服务器（DLL 锁），MO2 可开着。
- **保用户数据**：覆盖前提取部署副本 `config.jsonc` 的 `apiKey` → 覆盖后注入 → 核验无占位符残留；同步 `downloads\` 安装档案；保留 profile 备份/快照作回滚锚点。
- **升级 nuance**：运行时生成的名单 JSON 等一旦存在即持久，随包新增默认**不回流**——升级后需手改或删档重建。
- 命名沿用目标整合包结构（`[N]分类-中文名-英文标识`）；meta.ini 的 comments/notes 用中文记来源/回滚方法。

## 人设蒸馏（LLM 角色）

- 素材：repo 根 `.scratch/lore-distill/sections/*`（分商人）+ `curated/lore/11-trader-questlines.md` / `13-quest-text-distillation.md`；mod 商人用其自身任务文案。
- 输出 2–5 句 persona（可注入 system prompt 的信息密度：说话风格 / 立场 / 生意观 / 与玩家关系 / 关键叙事事实）；**文档与 persona 身份须互洽**（先核对再写）。

## 工具链注意

- PowerShell：路径含 `[..]` 用 `-LiteralPath`（否则被当通配符）；中文输出乱码时写 UTF-8 文件后阅读。
- 检查器（check-mod-standard）对 paired 布局 / 命名族 / 常量间接引用有 3 条已知误报（STD-BUILD-001/006、STD-META-003）——处置：豁免记录 + 检查器修复（详见 `mods/SamMeow-LLMTraderChat/docs/standards-waiver.md`）。

## 相关工件

- 配方：`../recipes/13-trader-chat-pipeline.md`
- 成品与验收：`mods/SamMeow-LLMTraderChat/`（README、docs/acceptance.md、docs/standards-waiver.md）
- 研究：`docs/research/llm-trader-chat-feasibility.md`
