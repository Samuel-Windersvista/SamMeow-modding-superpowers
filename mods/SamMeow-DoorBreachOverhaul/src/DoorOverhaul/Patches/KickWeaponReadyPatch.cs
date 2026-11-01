using System.Reflection;
using DoorBreach;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DoorOverhaul.Patches
{
    internal class KickWeaponReadyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Player), nameof(Player.StateIsSuitableForHandInput));
        }

        [PatchPostfix]
        private static void Postfix(Player __instance, ref bool __result)
        {
            if (!__result && KickWeaponReady.IsKicking(__instance))
            {
                __result = true;
            }
        }
    }

    internal static class KickWeaponReady
    {
        public static bool IsKicking(Player player)
        {
            return Settings.KickWeaponReady.Value
                && player != null
                && player.IsYourPlayer
                && player.CurrentState != null
                && player.CurrentState.Name == EPlayerState.BreachDoor;
        }

        public static void Tick(Player player)
        {
            if (IsKicking(player) && player.HandsController is Player.FirearmController firearm && firearm.IsAiming)
            {
                firearm.SetAim(false);
            }
        }
    }
}
