namespace SamMeow.LLMTraderChat.Shared;

/// <summary>
/// 聊天输入字符校验放行策略（纯逻辑，可单测）。
///
/// 背景：游戏聊天输入框 <c>EFT.UI.ValidationInputField</c> 对输入做白名单正则校验
/// （<c>^[...]+$</c>，字符类仅含 ASCII 字母数字与半角符号；CJK 汉字经 <c>\w</c> 属 Unicode 单词字符可通过，
/// 但中文全角标点属 <c>\p{P}</c> 被 <c>_replacer</c> 剥离）。
///
/// 本策略决定**是否放行任意字符**：仅当目标对话在本 mod 解锁名单内时为 true；
/// 其余对话与界面为 false（保持游戏原版校验）。
/// </summary>
public static class ChatInputPolicy
{
    /// <summary>
    /// 是否应对该对话放行任意字符（含中文全角标点）。
    /// 未解锁 / 名单外 / 空 id / 设置缺失 → false。
    /// </summary>
    public static bool ShouldBypassCharacterValidation(ClientSettings settings, string? dialogueId)
    {
        if (settings is null || dialogueId is not { Length: > 0 })
        {
            return false;
        }

        return settings.ShouldUnlock(dialogueId);
    }
}
