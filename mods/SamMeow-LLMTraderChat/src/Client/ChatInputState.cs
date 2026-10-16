using EFT.UI;

namespace SamMeow.LLMTraderChat.Client;

/// <summary>
/// 聊天输入相关运行时状态：当前聊天输入框实例与所选对话 id。
/// 供字符校验补丁判定「是否为聊天输入框 + 是否为已解锁对话」。
/// </summary>
internal static class ChatInputState
{
    /// <summary>当前 ChatScreen 的聊天输入框（Show 时记录）。</summary>
    public static ValidationInputField? ChatInputField { get; set; }

    /// <summary>当前所选对话 id（Show / 对话切换时记录）。</summary>
    public static string? SelectedDialogueId { get; set; }
}
