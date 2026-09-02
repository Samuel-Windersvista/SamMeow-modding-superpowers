using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using HarmonyLib;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.LockPicking.Patches;

public class KeyCardDoorActionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(EFT.InteractionContextHelper), "GetAvailableActions",
            new[] { typeof(GamePlayerOwner), typeof(KeycardDoor), typeof(bool) });

    [PatchPostfix]
    private static void Postfix(ref AvailableInteractionState __result, GamePlayerOwner owner, KeycardDoor door, bool isProxy)
    {
        if (__result is null || __result.Actions is null) return;

        if (WorldInteractionUtils.IsBotInteraction(owner)
            || !SkillsPlugin.SkillData.LockPicking.Enabled
            || Singleton<GameWorld>.Instance.MainPlayer.Side == EPlayerSide.Savage)
        {
            return;
        }

        door.AddInspectInteraction(__result, owner);
        door.AddKeyCardInteraction(__result, owner);
    }
}
