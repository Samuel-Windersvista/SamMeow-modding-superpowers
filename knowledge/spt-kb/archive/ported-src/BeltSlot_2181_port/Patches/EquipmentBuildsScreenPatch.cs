using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace BeltSlot.Patches
{
    public class EquipmentBuildsScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // 4.1.2: EquipmentBuildsScreen.method_6 no longer exists; hook the
            // controller-driven Show overload instead.
            return AccessTools.Method(typeof(EquipmentBuildsScreen), nameof(EquipmentBuildsScreen.Show),
                new[] { typeof(EquipmentBuildsScreen.EquipmentBuildsScreenController) });
        }
        [PatchPostfix]
        static void Postfix(EquipmentBuildsScreen __instance)
        {
            Plugin.Instance.SetBuildsArmbandSlot();
        }
    }
}
