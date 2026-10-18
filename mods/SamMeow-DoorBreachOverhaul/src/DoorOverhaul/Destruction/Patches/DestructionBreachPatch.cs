using DoorBreach;
using System;
using System.Reflection;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace DoorOverhaul.Destruction.Patches
{
    internal class DestructionBreachPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Door), nameof(Door.KickOpen), new[] { typeof(Vector3), typeof(bool) });
        }

        [PatchPostfix]
        private static void Postfix(Door __instance)
        {
            try
            {
                DestructionVisuals.OnLeafSwapped(__instance);
            }
            catch (Exception ex)
            {
                DoorBreachPlugin.Logger.LogError($"[EDI-Visual] {__instance?.Id}: breach hand-over failed: {ex}");
            }
        }
    }
}
