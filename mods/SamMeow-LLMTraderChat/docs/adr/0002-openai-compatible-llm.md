# ADR-0002：LLM 接入采用 OpenAI 兼容单层

- 状态：已接受（2026-10-14）
- 决策人：Overseer（用户）

## 状态注记（2026-10-14）

默认端点于 2026-10-14 经 Overseer 决策切至 **DeepSeek**（随包 `config.jsonc`/`defaultConfig.jsonc` 的 `llm` 段：`endpoint=https://api.deepseek.com`、`model=deepseek-flash`）；隐私默认改为**显式配置**——代码兜底默认仍为本地 Ollama（不出网），但发布默认走云端且由配置文件显式声明，玩家需将 `apiKey` 填为真实密钥方可出网。本注记不改变下方「单层 OpenAI 兼容」的架构决策。

## 背景

服务端需调用 LLM 生成商人回复。候选形态有三条：单层 OpenAI 兼容 HTTP 实现；多 provider 抽象层（Miyako 式 `ILlmProvider` + 9 个服务商）；仅绑定本地 Ollama。先例中 Miyako 走多 provider，Trash Talk 走 OpenAI 兼容直连；OpenAI 兼容端点已能覆盖 Ollama、DeepSeek 及多数云服务商（研究依据 `docs/research/llm-trader-chat-feasibility.md` §3.4）。

## 决策

v1 采用单层 OpenAI 兼容实现：以 `ILlmClient` 抽象定义接缝，其下仅一个 OpenAI 兼容实现；endpoint / key / model / temperature / maxTokens / timeout 均可配置（示例配置同时给出 Ollama 与 DeepSeek）。调用必须 async + `CancellationToken`；失败时回兜底话术并记日志；未配置有效 LLM 时对该请求回「未就绪」提示而非静默。

## 备选方案

1. 多 provider 抽象 —— 延后：v1 收益低；OpenAI 兼容端点已覆盖主要目标，抽象面可待需要时从 `ILlmClient` 分叉。
2. 仅本地 Ollama —— 否决：排除云端 LLM，与「用玩家/服主自选端点」目标冲突；但本地 Ollama 保留为默认隐私优先选项。

## 后果

- 正面：实现面最小，单测接缝清晰（`ILlmClient` 的请求塑形/非 200/超时/坏 JSON 可脱离 SPT 运行时测试）；本地 Ollama 即「聊天内容不出网」的隐私选项。
- 负面：非 OpenAI 兼容的原生端点（如 Anthropic 原生协议）需另加适配；「单层」意味着不承诺多 provider 能力，跨商切换需后续演进。
