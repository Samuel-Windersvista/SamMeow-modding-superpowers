namespace SamMeow.LLMTraderChat.Server.Spt;

/// <summary>
/// reset 的选择性清空规则（纯逻辑，抽出来便于单测——bot 本身依赖 SPT 运行时不可直测）。
/// </summary>
public static class DialogueResetPolicy
{
    /// <summary>
    /// 该消息在 reset 时是否应被移除：**无附件**（附件条目数 &lt;= 0）→ 移除（属于对话记忆）；
    /// 带附件（未领取保险/任务奖励）→ 保留。
    /// </summary>
    public static bool ShouldRemoveOnReset(int attachmentItemCount) => attachmentItemCount <= 0;
}
