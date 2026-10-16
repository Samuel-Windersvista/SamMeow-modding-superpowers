# 15: LLM 空响应诊断健壮性（推理型模型的 max_tokens 截断）

**What to build:** 让 LLM 客户端与引擎在「模型因推理耗尽 `max_tokens` 而返回 `finish_reason=length` + 空 `content`」时给出**明确、可判读的诊断**，而不是当前含糊的 `LLM 返回空内容`；并为运营者提供一眼可辨的处置指引（调大 `maxTokens`）。

**Blocked by:** 无（独立小改动；06 引擎与 04 LLM 客户端已收口）

**Status:** open

## 背景（2026-10-16 实机故障取证）

- 症状：玩家与 Therapist 对话两次均收到兜底话术「（商人暂时联系不上，请稍后再试。）」；日志链路 `[LLMTC][recv] → [LLMTC][route] → LLM 返回空内容 → 结果=Fallback`。
- 直连 API 对照实验（`deepseek-flash`）：
  - 最小提示词：`finish=stop`、`content` 非空、响应含 `reasoning_content`；
  - 近似真实 prompt + `max_tokens=512`：复现 `finish=length`、`contentLen=0`、`reasoningLen=2038`；
  - 同 prompt + `max_tokens=2048`：`finish=stop`、`contentLen=155`。
- 结论：`deepseek-flash` 属推理型模型，推理链计入 `max_tokens` 预算；预算耗尽即空内容。**根因属模型/参数侧**，当期以配置修复（`maxTokens: 4096`）解除。

## 问题（本工单要修的）

1. `OpenAiCompatibleLlmClient.ParseResponse` 只读取 `choices[0].message.content`；当其为空串时返回 `Ok("")`，引擎随后以 `LLM 返回空内容` 归入通用失败——**丢掉了 `finish_reason` 这一决定性线索**（`length` 与 `stop` 的含义完全不同）。
2. 可观测性不足：运营者无法从日志判断是「模型没说话」还是「被 token 上限截断」，排障需外部直连 API 复现。

## 期望行为（建议）

1. 解析响应时一并读取 `choices[0].finish_reason`；当 `content` 为空/空白且 `finish_reason == "length"` 时，返回一个**独立失败语义**（如新增 `LlmFailureKind.OutputTruncated`），`ErrorDetail` 形如：
   `"LLM 输出被 max_tokens 截断（finish_reason=length）：推理型模型的推理链会占用 max_tokens 预算，请调大 llm.maxTokens（当前 4096）"`
2. 引擎将 `<c>OutputTruncated</c>` 的 `ErrorDetail` 原样带入 warning（现有兜底路径已支持），并保持兜底话术行为不变（玩家侧仍收到可读提示）。
3. `content` 为空但 `finish_reason != "length"` 时，保留现有「LLM 返回空内容」语义，并附 `finish_reason` 值以便区分。
4. 单测：`finish_reason=length + content=""` → `OutputTruncated`；`finish_reason=stop + content=""` → 空内容；`finish_reason=length + 非空 content` → 正常 `Ok`（部分内容仍可用）。

## 注意事项

- 不改动 `maxTokens` 的装载层钳制范围（`ConfigLoader` 上限 32768）。
- 不新增 NuGet 依赖（继续 System.Text.Json）。
- README / `config/README.md` 增补一句运营提示：使用推理型模型（如 `deepseek-flash` / `deepseek-reasoner`）时应显著调大 `maxTokens`；非推理模型（如本地 Ollama `llama3.1`）512 通常够用。

## 验收清单

- [ ] `finish_reason` 被解析并进入失败诊断（含 `OutputTruncated` 分支）
- [ ] 单测覆盖 4 条分支；`dotnet test -c Release` 全绿
- [ ] 日志中可直接读出「被 max_tokens 截断」而非「空内容」
- [ ] README / config README 已补运营提示
