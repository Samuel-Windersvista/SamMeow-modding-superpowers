using System.Reflection;
using DoorBreach;
using EFT.Interactive;
using DoorOverhaul.Doors;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DoorOverhaul.Patches
{
    internal class QuietOpenSoundPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WorldInteractiveObject), nameof(WorldInteractiveObject.PlaySound),
                new[] { typeof(EDoorState), typeof(float) });
        }

        [PatchPrefix]
        private static bool Prefix(WorldInteractiveObject __instance, EDoorState state, ref float volume)
        {
            if (state != EDoorState.Open || !DoorOpenOverrides.ConsumeQuiet(__instance))
            {
                return true;
            }

            volume *= Settings.QuietVolume.Value;
            return volume > 0.001f;
        }
    }
}
