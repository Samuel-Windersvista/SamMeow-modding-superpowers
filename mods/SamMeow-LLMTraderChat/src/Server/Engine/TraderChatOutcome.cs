namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 引擎输出：要发送的文本 + 延迟调度指令 + 需要接线执行的副作用 + 可诊断信息。
///
/// 副作用（清空历史）不在引擎内执行——引擎只置 <see cref="ResetHistory"/> 标记，由接线（07）
/// 落地；历史写入语义整体留给接线，引擎仅读历史。
/// </summary>
/// <param name="Kind">结果类别。</param>
/// <param name="Text">要发给玩家的文本（<see cref="ChatOutcomeKind.Ignored"/> / <see cref="ChatOutcomeKind.Reset"/> 可能为空）。</param>
/// <param name="DelaySeconds">发送前延迟（秒）；仅需发送文本的类别 &gt; 0，其余为 0。</param>
/// <param name="ResetHistory">是否请求接线清空该（sessionId, traderId）对话历史。</param>
/// <param name="Warnings">非致命诊断（模板缺失、检索失败、渲染截断、LLM 失败详情等），供接线响亮记录。</param>
/// <param name="KnowledgeHitCount">本次检索命中的知识片段数（可观测性；<see cref="ChatOutcomeKind.Ignored"/> / <see cref="ChatOutcomeKind.Reset"/> / <see cref="ChatOutcomeKind.Opening"/> 为 0）。</param>
/// <param name="KnowledgeSources">命中片段的来源文件名（去重、按命中顺序）；供日志显示「来源」。无命中为 null/空。</param>
/// <param name="AttitudeTier">本次判定的动态态度档位（可观测性；<see cref="ChatOutcomeKind.Ignored"/> / <see cref="ChatOutcomeKind.Reset"/> / <see cref="ChatOutcomeKind.Opening"/> 为 null）。</param>
/// <param name="AttitudeSummary">态度判定摘要串（供服务端日志判读）；无判定为 null。</param>
/// <param name="QuestSummary">任务知识披露摘要串（工单 16，如 <c>Disclosure=Hint quests=2</c>）；无判定为 null。</param>
public sealed record TraderChatOutcome(
    ChatOutcomeKind Kind,
    string Text,
    double DelaySeconds,
    bool ResetHistory,
    IReadOnlyList<string> Warnings,
    int KnowledgeHitCount = 0,
    IReadOnlyList<string>? KnowledgeSources = null,
    AttitudeTier? AttitudeTier = null,
    string? AttitudeSummary = null,
    string? QuestSummary = null)
{
    /// <summary>无动作结果。</summary>
    public static TraderChatOutcome Ignored() => new(ChatOutcomeKind.Ignored, string.Empty, 0, false, []);

    /// <summary>reset 指令结果：请求清空历史；<paramref name="confirmation"/> 为确认文案（空则不发）。</summary>
    public static TraderChatOutcome Reset(string confirmation, double delaySeconds) =>
        new(ChatOutcomeKind.Reset, confirmation, delaySeconds, true, []);
}
