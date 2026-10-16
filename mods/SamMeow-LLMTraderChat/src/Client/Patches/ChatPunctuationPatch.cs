using ChatShared;
using EFT;
using EFT.UI;
using EFT.UI.Chat;
using HarmonyLib;
using SamMeow.LLMTraderChat.Shared;

namespace SamMeow.LLMTraderChat.Client.Patches;

/// <summary>
/// 工单 12：放行已解锁商人对话中的中文全角标点输入。
///
/// 过滤点：<c>EFT.UI.ValidationInputField.ValidationError(string)</c>（注册于 <c>onValueChanged</c>）——
/// 白名单正则不匹配时用 <c>_replacer</c> 剥离非法字符（汉字经 <c>\w</c> 通过、全角标点被剥）。
/// 修法：仅当校验对象是聊天输入框且当前所选对话在解锁名单内时，跳过该校验（其余界面/对话不变）。
/// </summary>
[HarmonyPatch(typeof(ValidationInputField), nameof(ValidationInputField.ValidationError))]
internal static class ChatPunctuationPatch
{
    [HarmonyPrefix]
    private static bool Prefix(ValidationInputField __instance, ref ENicknameError __result)
    {
        // 只处理聊天输入框本身。
        if (!object.ReferenceEquals(__instance, ChatInputState.ChatInputField))
        {
            return true;
        }

        // 只处理本 mod 已解锁的对话；其他对话保持原版校验。
        if (!ChatInputPolicy.ShouldBypassCharacterValidation(LLMTraderChatClientPlugin.Settings, ChatInputState.SelectedDialogueId))
        {
            return true;
        }

        // 放行：强制判为合法，原方法不再执行（不做字符剥离）。
        __result = ENicknameError.ValidNickname;
        return false;
    }
}

/// <summary>记录 ChatScreen 的聊天输入框实例与当前所选对话 id。</summary>
[HarmonyPatch(typeof(ChatScreen), nameof(ChatScreen.Show))]
internal static class ChatScreenShowPatch
{
    [HarmonyPostfix]
    private static void Postfix(ChatScreen __instance, SocialNetwork social)
    {
        ChatInputState.ChatInputField = __instance._chatInputField;
        ChatInputState.SelectedDialogueId = social.SelectedDialogue?._id;
    }
}

/// <summary>对话切换时更新当前所选对话 id。</summary>
[HarmonyPatch(typeof(ChatScreen), nameof(ChatScreen.OnSelectedDialogChanged))]
internal static class ChatScreenSelectedPatch
{
    [HarmonyPrefix]
    private static void Prefix(UpdatableChatDialogue dialog)
    {
        ChatInputState.SelectedDialogueId = dialog?._id;
    }
}
