using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using EFT.UI;

namespace DoorBreach.Patches
{
    /// <summary>
    /// 钥匙卡门交互菜单补丁：复用单点合并入口，保证 DO 三动作与 Plant Explosive 不重复。
    /// </summary>
    internal class ActionMenuKeyCardPatch : ModulePatch
    {
        // 4.1.6：显式选择 (GamePlayerOwner, KeycardDoor, bool isProxy) 重载。
        // KeycardDoor : Door，故 postfix 的 Door door 参数注入合法。
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(InteractionContextHelper), nameof(InteractionContextHelper.GetAvailableActions),
                new[] { typeof(GamePlayerOwner), typeof(KeycardDoor), typeof(bool) });


        [PatchPostfix]
        public static void Postfix(ref AvailableInteractionState __result, GamePlayerOwner owner, Door door)
        {
            // KeyCard 重载只保留 C4 去重守卫；DO 三动作由 Door 重载单点注入。
            DoorMenuMerge.AppendPlantExplosiveOnly(__result, owner, door);
        }
    }
}
