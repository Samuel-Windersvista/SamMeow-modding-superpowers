namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>历史消息的发言方。</summary>
public enum ChatRole
{
    /// <summary>玩家。</summary>
    Player = 0,

    /// <summary>商人（本 mod 生成的回复）。</summary>
    Trader,
}

/// <summary>一条历史对话消息（sessionId+traderId 维度内）。</summary>
public sealed record ChatHistoryMessage(ChatRole Role, string Text);
