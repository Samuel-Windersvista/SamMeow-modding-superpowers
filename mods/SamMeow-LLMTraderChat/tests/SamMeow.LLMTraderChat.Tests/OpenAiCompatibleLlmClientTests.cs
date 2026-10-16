using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using SamMeow.LLMTraderChat.Server.Config;
using SamMeow.LLMTraderChat.Server.Llm;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>
/// LLM 层单测（工单 04）：请求塑形、非 200、超时、坏 JSON。
/// 全程用 fake transport，不触网、不依赖 SPT 运行时。
/// </summary>
public class OpenAiCompatibleLlmClientTests
{
    [Fact]
    public async Task CompleteAsync_ShapesRequest_AndParsesContent()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                """{"choices":[{"message":{"role":"assistant","content":"你好，这里是治疗师。"}}]}""")));

        var client = CreateClient(handler, endpoint: "https://api.example.com/v1", apiKey: "secret-key", model: "deepseek-chat");
        var messages = new List<LlmChatMessage>
        {
            LlmChatMessage.System("你是治疗师。"),
            LlmChatMessage.User("你好"),
        };

        var result = await client.CompleteAsync(messages);

        Assert.True(result.Success);
        Assert.Equal("你好，这里是治疗师。", result.Content);

        var request = Assert.IsType<HttpRequestMessage>(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.example.com/v1/chat/completions", request.RequestUri!.ToString());
        Assert.Equal(
            new AuthenticationHeaderValue("Bearer", "secret-key"),
            request.Headers.Authorization);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        Assert.Equal("deepseek-chat", root.GetProperty("model").GetString());
        Assert.Equal(0.7, root.GetProperty("temperature").GetDouble());
        Assert.Equal(512, root.GetProperty("max_tokens").GetInt32());

        var sentMessages = root.GetProperty("messages");
        Assert.Equal(2, sentMessages.GetArrayLength());
        Assert.Equal("system", sentMessages[0].GetProperty("role").GetString());
        Assert.Equal("你是治疗师。", sentMessages[0].GetProperty("content").GetString());
        Assert.Equal("user", sentMessages[1].GetProperty("role").GetString());
        Assert.Equal("你好", sentMessages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task CompleteAsync_EmptyApiKey_OmitsAuthorizationHeader()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse("""{"choices":[{"message":{"content":"ok"}}]}""")));

        var client = CreateClient(handler, apiKey: "");
        await client.CompleteAsync([LlmChatMessage.User("hi")]);

        Assert.Null(handler.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task CompleteAsync_Non200_ReturnsHttpError_WithoutLeakingApiKey()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse(
                """{"error":"boom"}""", HttpStatusCode.InternalServerError)));

        var client = CreateClient(handler, apiKey: "super-secret");
        var result = await client.CompleteAsync([LlmChatMessage.User("hi")]);

        Assert.False(result.Success);
        Assert.Equal(LlmFailureKind.HttpError, result.FailureKind);
        Assert.Contains("500", result.ErrorDetail!, StringComparison.Ordinal);
        Assert.DoesNotContain("super-secret", result.ErrorDetail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAsync_Timeout_ReturnsTimeoutFailure()
    {
        // handler 遵循取消令牌；linked token 到点后 SendAsync 抛出取消。
        var handler = new FakeHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return FakeHttpMessageHandler.JsonResponse("{}");
        });

        var client = CreateClient(handler, timeoutSeconds: 1);
        var result = await client.CompleteAsync([LlmChatMessage.User("hi")]);

        Assert.False(result.Success);
        Assert.Equal(LlmFailureKind.Timeout, result.FailureKind);
    }

    [Fact]
    public async Task CompleteAsync_BadJson_ReturnsInvalidResponse()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse("this is not json")));

        var client = CreateClient(handler);
        var result = await client.CompleteAsync([LlmChatMessage.User("hi")]);

        Assert.False(result.Success);
        Assert.Equal(LlmFailureKind.InvalidResponse, result.FailureKind);
    }

    [Fact]
    public async Task CompleteAsync_EmptyChoices_ReturnsInvalidResponse()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse("""{"choices":[]}""")));

        var client = CreateClient(handler);
        var result = await client.CompleteAsync([LlmChatMessage.User("hi")]);

        Assert.False(result.Success);
        Assert.Equal(LlmFailureKind.InvalidResponse, result.FailureKind);
    }

    [Fact]
    public async Task CompleteAsync_EndpointMissing_ReturnsNotConfigured_WithoutHttpCall()
    {
        var called = false;
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            called = true;
            return Task.FromResult(FakeHttpMessageHandler.JsonResponse("{}"));
        });

        var client = CreateClient(handler, endpoint: "");
        var result = await client.CompleteAsync([LlmChatMessage.User("hi")]);

        Assert.False(result.Success);
        Assert.Equal(LlmFailureKind.NotConfigured, result.FailureKind);
        Assert.False(called);
    }

    [Fact]
    public async Task CompleteAsync_MalformedEndpoint_ReturnsFailure_WithoutThrowing()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(FakeHttpMessageHandler.JsonResponse("{}")));

        // 缺 scheme 的 host:port 串（Uri 会误把 localhost 当 scheme）：必须返回失败态而非抛异常。
        var client = CreateClient(handler, endpoint: "localhost:11434/v1");
        var result = await client.CompleteAsync([LlmChatMessage.User("hi")]);

        Assert.False(result.Success);
        Assert.Equal(LlmFailureKind.NotConfigured, result.FailureKind);
    }

    [Fact]
    public async Task CompleteAsync_BodyReadInterrupted_ReturnsFailure_WithoutThrowing()
    {
        var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ThrowingContent() }));

        var client = CreateClient(handler);
        var result = await client.CompleteAsync([LlmChatMessage.User("hi")]);

        Assert.False(result.Success);
        Assert.Equal(LlmFailureKind.HttpError, result.FailureKind);
    }

    /// <summary>响应体读取时抛 IOException 的自定义内容，模拟头已到、体传输重置。</summary>
    private sealed class ThrowingContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            throw new IOException("模拟响应体传输中断");

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private static OpenAiCompatibleLlmClient CreateClient(
        FakeHttpMessageHandler handler,
        string endpoint = "https://api.example.com/v1",
        string apiKey = "test-key",
        string model = "test-model",
        int timeoutSeconds = 30)
    {
        var config = new LlmConfig
        {
            Endpoint = endpoint,
            ApiKey = apiKey,
            Model = model,
            Temperature = 0.7,
            MaxTokens = 512,
            TimeoutSeconds = timeoutSeconds,
        };

        return new OpenAiCompatibleLlmClient(config, new HttpClient(handler));
    }
}
