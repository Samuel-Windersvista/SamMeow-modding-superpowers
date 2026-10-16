# 04: LLM 层（OpenAI 兼容）

**What to build:** LLM 调用层：抽象接口 + OpenAI 兼容实现（endpoint / key / model / temperature / maxTokens / timeout 均为配置项）。行为：async + CancellationToken；非 200、超时、坏 JSON 的失败语义明确（以异常或结果对象表达，供引擎统一兜底）；日志不输出 API key。示例配置同时给出本地 Ollama 与云端 DeepSeek 两种形态。实现不依赖 SPT 运行时，可在测试中直接实例化。

**Blocked by:** 01: 项目脚手架

**Status:** ready-for-agent

- [x] 单测（fake transport）：请求塑形（model/messages 与参数）、非 200、超时、坏 JSON
- [x] 实现不依赖 SPT 运行时；测试无网络
- [x] 示例配置涵盖 Ollama 与 DeepSeek
- [x] 日志与异常不含 API key

## Comments

- 2026-10-14：工单收口。交付：`src/Server/Llm/*`（ILlmClient / LlmCompletionResult + LlmFailureKind / OpenAiCompatibleLlmClient）。测试：OpenAiCompatibleLlmClientTests 9 条（含修订轮新增：畸形 endpoint 不抛异常返回失败态、body 读取中断映射失败态）。oracle 双轴评审：修订后收口——修订轮扩展失败面（URL 构造 + body 读取异常统一映射为结果对象；取消按契约传播），修复"异常逃逸违背接口契约"SHOULD。未提交。
