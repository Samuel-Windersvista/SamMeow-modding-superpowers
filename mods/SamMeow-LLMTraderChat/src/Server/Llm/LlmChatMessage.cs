using System.Text.Json.Serialization;

namespace SamMeow.LLMTraderChat.Server.Llm;

/// <summary>
/// 一条聊天消息（角色 + 内容），与 OpenAI 兼容接口的 message 对象一一对应。
/// 序列化键为小写 role/content。
/// </summary>
public sealed record LlmChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content)
{
    /// <summary>system 消息（人格 + 知识片段 + 行为约束）。</summary>
    public static LlmChatMessage System(string content) => new("system", content);

    /// <summary>user 消息（玩家发言）。</summary>
    public static LlmChatMessage User(string content) => new("user", content);

    /// <summary>assistant 消息（商人历史回复）。</summary>
    public static LlmChatMessage Assistant(string content) => new("assistant", content);
}
