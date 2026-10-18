using System.Reflection;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DoorOverhaul.Destruction.Patches
{
    internal class DestructionPortalPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Door), nameof(Door.CheckOcclusionPortal));
        }

        [PatchPostfix]
        private static void Postfix(Door __instance)
        {
            if (__instance._occlusionPortal != null && (DestructionVisuals.HasHole(__instance) || !DestructionOwnership.Free(__instance)))
            {
                __instance._occlusionPortal.open = true;
            }
        }
    }
}
