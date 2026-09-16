using EFT;
using SPTushonka.Reflection.Patching;
using System;
using System.Reflection;

namespace Radar.Patches
{
    /// <summary>Installs the radar once a raid actually starts.</summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配：<c>SPT.Reflection.Patching.ModulePatch</c> 在 5.0 更名为
    /// <c>SPTushonka.Reflection.Patching.ModulePatch</c>，API 完全相同。
    /// </remarks>
    public class GameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        [PatchPostfix]
        private static void Postfix(GameWorld __instance)
        {
            RadarPlugin.Log.LogInfo("Game started, loading radar hud");

            // [CRITICAL] 补丁体绝不向游戏代码抛异常（异常穿透 = 游戏闪退）。
            try
            {
                __instance.gameObject.AddComponent<InRaidRadarManager>();
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogError($"Could not attach the radar manager: {e.Message}");
            }
        }
    }
}
