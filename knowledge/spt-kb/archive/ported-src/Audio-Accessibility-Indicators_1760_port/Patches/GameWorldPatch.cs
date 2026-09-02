using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using acidphantasm_accessibilityindicators.IndicatorUI;
using acidphantasm_accessibilityindicators.Scripts;

namespace acidphantasm_accessibilityindicators.Patches
{
    internal class GameWorldOnGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        public static void PatchPostfix()
        {
            if (Panel.IndicatorHUD == null) Panel.CreateHUD();
        }
    }

    internal class GameWorldUnregisterPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.UnregisterPlayer));
        }

        [PatchPostfix]
        public static void PatchPostFix(IPlayer iPlayer)
        {
            Player player = iPlayer as Player;
            if (player.IsYourPlayer && Panel.IndicatorHUD != null) Panel.Dispose();
        }
    }
}
