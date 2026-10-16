namespace SamMeow.LLMTraderChat.Server.Llm;

/// <summary>LLM 调用失败类别，供引擎统一兜底时区分处理与记日志。</summary>
public enum LlmFailureKind
{
    /// <summary>未失败。</summary>
    None = 0,

    /// <summary>端点或模型未配置（不应发起请求）。</summary>
    NotConfigured,

    /// <summary>网络错误或非 2xx 响应。</summary>
    HttpError,

    /// <summary>请求/读取超时。</summary>
    Timeout,

    /// <summary>响应不是合法 JSON 或缺少预期字段。</summary>
    InvalidResponse,
}

/// <summary>
/// LLM 调用结果（结果对象而非异常——引擎据 <see cref="Success"/> 统一兜底，
/// 失败时 <see cref="ErrorDetail"/> 供日志；**绝不含 API key**）。
/// </summary>
public sealed record LlmCompletionResult(bool Success, string Content, LlmFailureKind FailureKind, string? ErrorDetail)
{
    /// <summary>成功结果。</summary>
    public static LlmCompletionResult Ok(string content) => new(true, content, LlmFailureKind.None, null);

    /// <summary>失败结果。</summary>
    public static LlmCompletionResult Fail(LlmFailureKind kind, string detail) => new(false, string.Empty, kind, detail);
}
