namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 一次性开场消息的「已开场」状态存储抽象（每 sessionId+traderId 一份）。
/// 测试用内存实现；生产由接线（07）落到 mod 目录状态文件。
/// </summary>
public interface ITraderChatStateStore
{
    /// <summary>
    /// 原子「认领」开场资格：未认领 → 置位并返回 true；已认领 → 返回 false。
    /// 引擎开场路径用本方法消除并发双发窗口。
    /// </summary>
    Task<bool> TryClaimOpeningAsync(string sessionId, string traderId, CancellationToken cancellationToken = default);

    /// <summary>该（sessionId, traderId）是否已发送过开场消息。</summary>
    Task<bool> IsOpeningSentAsync(string sessionId, string traderId, CancellationToken cancellationToken = default);

    /// <summary>标记该（sessionId, traderId）开场消息已发送。</summary>
    Task MarkOpeningSentAsync(string sessionId, string traderId, CancellationToken cancellationToken = default);
}
