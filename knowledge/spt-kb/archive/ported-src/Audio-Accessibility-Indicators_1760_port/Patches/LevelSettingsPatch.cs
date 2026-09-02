using acidphantasm_accessibilityindicators.IndicatorUI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace acidphantasm_accessibilityindicators.Patches
{
    internal class LevelSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LevelSettings), nameof(LevelSettings.Awake));
        }

        [PatchPostfix]
        public static void PatchPostfix(LevelSettings __instance)
        {
            if (__instance == null) return;

            Panel.northVector = __instance.NorthVector;
            Panel.northDirection = __instance.NorthDirection;
        }
    }
}
