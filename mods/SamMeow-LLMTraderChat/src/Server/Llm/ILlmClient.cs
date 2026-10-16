namespace SamMeow.LLMTraderChat.Server.Llm;

/// <summary>
/// LLM 调用抽象（接缝：HTTP 边界）。OpenAI 兼容实现见 <see cref="OpenAiCompatibleLlmClient"/>。
/// 引擎只依赖本接口，便于用 fake 实现做单测与替换后端。
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// 请求一次补全。不抛业务异常：失败以 <see cref="LlmCompletionResult"/> 表达，供引擎兜底。
    /// 取消（<paramref name="cancellationToken"/>）按常规传播 OperationCanceledException。
    /// </summary>
    Task<LlmCompletionResult> CompleteAsync(
        IReadOnlyList<LlmChatMessage> messages,
        CancellationToken cancellationToken = default);
}
