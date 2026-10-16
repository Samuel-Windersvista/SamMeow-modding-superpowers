using ChatShared;
using EFT;
using HarmonyLib;

namespace SamMeow.LLMTraderChat.Client.Patches;

/// <summary>
/// 解锁「可聊天商人」消息线程的输入闸门（配置驱动）。
///
/// 4.1.6 实测：<c>UpdatableChatDialogue.CanSendMessage(SocialNetwork, out string)</c> 对非 GroupChat 对话
/// 返回 <c>(!flag &amp;&amp; !ignored) &amp; flag2</c>，其中 <c>flag2 = socialNetwork.FriendsList.Contains(Profile)</c>。
/// 商人对话的 <c>Profile</c> 是玩家本人（对话 Users 为空时 ctor 回填 playerMember），永远不在好友列表，
/// 故原生返回 false 并提示「不在好友列表」。本 Postfix 仅对名单内商人对话把结果翻为 true。
///
/// 名单来源：客户端 JSON 文件（<c>LLMTraderChatClientPlugin.Settings</c>），新增商人只需改文件。
/// 形式沿用 Trash Talk 验证过的 Postfix（保留忽略名单判定，仅放行「不在好友列表」这一条）。
/// </summary>
[HarmonyPatch(typeof(UpdatableChatDialogue), nameof(UpdatableChatDialogue.CanSendMessage))]
internal static class CanSendMessagePatch
{
    [HarmonyPostfix]
    private static void Postfix(UpdatableChatDialogue __instance, ref bool __result, ref string? error)
    {
        if (__result)
        {
            return;
        }

        // 目标商人对话：服务端以商人 id 作为对话 id（MailSendService.GetDialog），故用 _id 识别。
        if (!LLMTraderChatClientPlugin.Settings.ShouldUnlock(__instance._id))
        {
            return;
        }

        // 保留忽略名单判定：任一命中则不解锁。
        if (__instance.Profile?.Info == null || __instance.Profile.Info.Ignored)
        {
            return;
        }

        if (SocialNetwork.IgnoringYouList.Contains(__instance.Profile))
        {
            return;
        }

        __result = true;
        error = null;
    }
}
