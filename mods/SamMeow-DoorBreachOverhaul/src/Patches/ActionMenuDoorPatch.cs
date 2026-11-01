using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using EFT.UI;

namespace DoorBreach.Patches
{
    /// <summary>
    /// 门交互菜单补丁：单点注入 DO 三动作（Peek / Open Further / Open Quietly）与 Plant Explosive。
    /// </summary>
    internal class ActionMenuDoorPatch : ModulePatch
    {
        // 4.1.6：InteractionContextHelper.GetAvailableActions 有约 20 个重载，
        // 必须显式按签名选择 (GamePlayerOwner, Door)，否则 GetMethod(name) 抛 AmbiguousMatchException。
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(InteractionContextHelper), nameof(InteractionContextHelper.GetAvailableActions),
                new[] { typeof(GamePlayerOwner), typeof(Door) });


        [PatchPostfix]
        public static void Postfix(ref AvailableInteractionState __result, GamePlayerOwner owner, Door door)
        {
            DoorMenuMerge.AppendAll(__result, owner, door);
        }
    }
}
