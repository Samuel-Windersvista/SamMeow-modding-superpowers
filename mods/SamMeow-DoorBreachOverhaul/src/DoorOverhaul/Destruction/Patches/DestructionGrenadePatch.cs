using DoorBreach;
using System;
using System.Reflection;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace DoorOverhaul.Destruction.Patches
{
    internal class DestructionGrenadePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Grenade), nameof(Grenade.Explosion), new[]
            {
                typeof(Grenade), typeof(IExplosiveItem), typeof(Vector3), typeof(string), typeof(IBallisticsCalculator),
                typeof(Item), typeof(Vector3), typeof(bool)
            });
        }

        [PatchPostfix]
        private static void Postfix(Grenade grenade, IExplosiveItem grenadeItem, Vector3 grenadePosition, Vector3 shift)
        {
            if (grenade is SmokeGrenade)
            {
                return;
            }

            try
            {
                DoorBlast.Explode(grenadeItem, grenadePosition + shift);
            }
            catch (Exception ex)
            {
                DoorBreachPlugin.Logger.LogError($"[EDI-Blast] explosion handling failed: {ex}");
            }
        }
    }
}
