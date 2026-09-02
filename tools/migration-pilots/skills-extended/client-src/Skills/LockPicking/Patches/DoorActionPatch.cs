using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace SkillsExtended.Skills.LockPicking.Patches;

internal class DoorActionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(EFT.InteractionContextHelper), "GetAvailableActions",
            new[] { typeof(GamePlayerOwner), typeof(Door) });

    [PatchPostfix]
    private static void Postfix(ref AvailableInteractionState __result, GamePlayerOwner owner, Door door)
    {
        if (__result is null || __result.Actions is null) return;

        if (WorldInteractionUtils.IsBotInteraction(owner)
            || !SkillsPlugin.SkillData.LockPicking.Enabled
            || Singleton<GameWorld>.Instance.MainPlayer.Side == EPlayerSide.Savage)
        {
            return;
        }

        door.AddLockpickingInteraction(__result, owner);
        door.AddInspectInteraction(__result, owner);
    }
}
