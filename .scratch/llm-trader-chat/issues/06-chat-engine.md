# 06: 引擎核心（TraderChatEngine）

**What to build:** 聊天引擎：输入 (sessionId, traderId, 玩家文本) 与历史访问接口，输出回复文本与延迟指令。编排责任：reset 指令识别；开场消息一次性判定；检索调用（可注入空实现）；prompt 组装（人格 + 检索片段 + 最近 N 条历史，N 默认 20 可配）；LLM 调用；失败兜底话术（可配）。除纯数据形状外不依赖 SPT 类型，全部依赖以接口注入。单测覆盖。

**Blocked by:** 03: 配置装载层；04: LLM 层（OpenAI 兼容）

**Status:** ready-for-agent

- [x] 单测：正常回复、检索片段注入、LLM 失败兜底、reset、开场一次性、超时路径
- [x] 历史注入条数受配置控制（默认 20）
- [x] 引擎不直接依赖 SPT 运行时类型；依赖均接口注入
- [x] 兜底话术可配置
- [x] prompt 语言模板（`config/prompts/system-zh.txt`）随包分发；缺模板时响亮兜底（不许静默空 prompt）
- [x] 数值参数（historyMessageCount / timeoutSeconds 等）的钳制归属明确为单一处（装载层或消费端）并有测试覆盖

## Comments

- 2026-10-14：工单收口。交付：`src/Server/Engine/*`（TraderChatEngine 六态 outcome：Reply/Fallback/NotReady/Reset/Opening/Ignored；只读历史接口 `IChatHistoryProvider`、状态接口 `ITraderChatStateStore`（at-most-once）、模板接口 `IPromptTemplateProvider` + 内置兜底）+ `Knowledge/IKnowledgeRetrieverFactory` + `knowledgeTopK` 配置字段 + `config/prompts/system-zh.txt`。测试：TraderChatEngineTests 28（20 + 修订 8）+ ConfigLoaderTests +8（钳制）= 73 全绿（主控台复验）。oracle 双轴评审：修订后收口（无 BLOCKER）；修订轮落地：E1 prompt 尺寸预算（历史 500 字/条、4000 字总量；知识 800 字/条、3000 字总量，截断入告警）+ E2 单趟替换 + E4 异常面扩展 + E5 空兜底回落 + E6/E7/E8 全部完成。未提交。
