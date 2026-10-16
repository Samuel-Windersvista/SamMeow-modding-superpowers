namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 对话历史访问抽象（只读）。
///
/// 引擎只读最近 N 条构造 prompt；**写入语义（追加回复、清空）留给接线（07）**，
/// 引擎不触碰 SPT 的对话/profile 结构。reset 由引擎识别后经
/// <see cref="TraderChatOutcome.ResetHistory"/> 通知接线执行。
/// </summary>
public interface IChatHistoryProvider
{
    /// <summary>
    /// 读取某（sessionId, traderId）对话最近 <paramref name="count"/> 条历史，按时间先后返回（旧 → 新）。
    /// 无历史时返回空列表。
    /// </summary>
    Task<IReadOnlyList<ChatHistoryMessage>> GetRecentAsync(
        string sessionId,
        string traderId,
        int count,
        CancellationToken cancellationToken = default);
}
