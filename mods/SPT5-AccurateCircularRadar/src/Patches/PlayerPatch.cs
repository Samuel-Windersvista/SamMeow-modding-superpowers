using EFT;
using EFT.InventoryLogic;
using SPTushonka.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace Radar.Patches
{
    /// <summary>
    /// Lights a contact up when it fires, which is what Fire Mode displays instead of live positions.
    /// </summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配：去掉 <c>[NotNull]</c>（JetBrains.Annotations 不在 5.0 引用集内），
    /// 参数语义不变；补丁框架改用 <c>SPTushonka.Reflection.Patching</c>。
    /// </remarks>
    internal class PlayerOnMakingShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(Player).GetMethod("OnMakingShot", BindingFlags.Public | BindingFlags.Instance);

        [PatchPostfix]
        private static void PostFix(Player __instance, IWeapon weapon, Vector3 force)
        {
            // [CRITICAL] 补丁体绝不向游戏代码抛异常（异常穿透 = 游戏闪退）。
            try
            {
                if (__instance == null)
                    return;

                InRaidRadarManager.LiveRadar?.UpdateFireTime(__instance.ProfileId);
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"Fire-time hook failed: {e.Message}");
            }
        }
    }
}
