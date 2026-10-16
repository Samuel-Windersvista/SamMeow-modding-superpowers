using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SamMeow.LLMTraderChat.Server.Config;

namespace SamMeow.LLMTraderChat.Server.Llm;

/// <summary>
/// OpenAI 兼容 LLM 客户端。POST <c>{endpoint}/chat/completions</c>，
/// <c>Authorization: Bearer {apiKey}</c>（key 为空则不带该头）。
///
/// 失败语义：非 200 / 超时 / 坏 JSON / 端点非法 / 传输与响应体读取失败一律返回
/// <see cref="LlmCompletionResult"/> 的失败态（<see cref="LlmFailureKind"/> 区分），
/// 不向调用方抛业务异常；**取消按契约传播**（<see cref="OperationCanceledException"/> 仅在
/// 外层 <c>cancellationToken</c> 已请求取消时抛出，绝不吞掉）。
/// 不新增 NuGet 依赖（System.Text.Json + HttpClient 内置）。日志与异常绝不包含 API key。
/// </summary>
public sealed class OpenAiCompatibleLlmClient : ILlmClient
{
    private const int ErrorBodyPreviewLength = 200;

    private readonly LlmConfig _config;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 构造客户端。<paramref name="httpClient"/> 缺省时自建（超时由每次请求的 linked token 控制，
    /// 故把 HttpClient.Timeout 设为无限以免与之冲突）；测试可注入带 fake handler 的实例。
    /// </summary>
    public OpenAiCompatibleLlmClient(LlmConfig config, HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        _config = config;
        _httpClient = httpClient ?? new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
    }

    /// <inheritdoc />
    public async Task<LlmCompletionResult> CompleteAsync(
        IReadOnlyList<LlmChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (string.IsNullOrWhiteSpace(_config.Endpoint))
        {
            return LlmCompletionResult.Fail(LlmFailureKind.NotConfigured, "llm.endpoint 未配置");
        }

        if (string.IsNullOrWhiteSpace(_config.Model))
        {
            return LlmCompletionResult.Fail(LlmFailureKind.NotConfigured, "llm.model 未配置");
        }

        // 端点必须是绝对 http/https URI；否则视为未配置（避免构造请求或 SendAsync 抛异常逃逸）。
        if (!TryBuildCompletionsUri(_config.Endpoint, out var endpointUri))
        {
            return LlmCompletionResult.Fail(
                LlmFailureKind.NotConfigured,
                "llm.endpoint 非法（应为绝对 http/https URI，如 http://127.0.0.1:11434/v1）");
        }

        var payload = JsonSerializer.Serialize(new ChatRequest(
            _config.Model, messages, _config.Temperature, _config.MaxTokens));

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // Math.Max(1, …) 仅为硬防御（防止 0/负数导致 CancelAfter 立即触发）；
        // timeoutSeconds 的取值范围契约由装载层（ConfigLoader）单一负责，此处不作为契约。
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _config.TimeoutSeconds)));

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpointUri)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };

            if (!string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);
            }

            response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // 用户取消：按契约传播，绝不吞。
        }
        catch (OperationCanceledException)
        {
            return LlmCompletionResult.Fail(LlmFailureKind.Timeout, $"LLM 请求超时（>{_config.TimeoutSeconds}s）");
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            return LlmCompletionResult.Fail(LlmFailureKind.HttpError, $"LLM 请求失败：{ex.Message}");
        }

        using (response)
        {
            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(timeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // 用户取消：按契约传播。
            }
            catch (OperationCanceledException)
            {
                return LlmCompletionResult.Fail(LlmFailureKind.Timeout, $"LLM 响应读取超时（>{_config.TimeoutSeconds}s）");
            }
            catch (Exception ex) when (ex is IOException or HttpRequestException)
            {
                return LlmCompletionResult.Fail(LlmFailureKind.HttpError, $"LLM 响应读取失败：{ex.Message}");
            }

            if (!response.IsSuccessStatusCode)
            {
                return LlmCompletionResult.Fail(
                    LlmFailureKind.HttpError,
                    $"LLM 返回 HTTP {(int)response.StatusCode}：{Truncate(body, ErrorBodyPreviewLength)}");
            }

            return ParseResponse(body);
        }
    }

    /// <summary>
    /// 由 endpoint 拼出 <c>{endpoint}/chat/completions</c>；仅接受绝对 http/https URI。
    /// 缺 scheme（如 <c>localhost:11434/v1</c>）、非 http(s) scheme 或非法串一律返回 false。
    /// </summary>
    private static bool TryBuildCompletionsUri(string endpoint, out Uri uri)
    {
        uri = null!;
        var candidate = $"{endpoint.TrimEnd('/')}/chat/completions";
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    /// <summary>传输层失败的异常集合（构造请求 / 发送阶段可能抛出的类型）。</summary>
    private static bool IsTransportFailure(Exception ex) =>
        ex is HttpRequestException or IOException or InvalidOperationException
            or NotSupportedException or UriFormatException or ArgumentException;

    /// <summary>从 OpenAI 兼容响应中提取 <c>choices[0].message.content</c>。</summary>
    private static LlmCompletionResult ParseResponse(string body)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            return LlmCompletionResult.Fail(LlmFailureKind.InvalidResponse, $"响应不是合法 JSON：{ex.Message}");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
            {
                return LlmCompletionResult.Fail(LlmFailureKind.InvalidResponse, "响应缺少 choices 数组");
            }

            var choice = choices[0];
            if (choice.ValueKind != JsonValueKind.Object
                || !choice.TryGetProperty("message", out var message)
                || message.ValueKind != JsonValueKind.Object
                || !message.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.String)
            {
                return LlmCompletionResult.Fail(LlmFailureKind.InvalidResponse, "响应缺少 choices[0].message.content");
            }

            return LlmCompletionResult.Ok(content.GetString() ?? string.Empty);
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";

    /// <summary>OpenAI 兼容请求体（snake_case 键）。</summary>
    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<LlmChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);
}
