using System.Reflection;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace DoorOverhaul.Destruction.Patches
{
    internal static class DestructionOwnership
    {
        public static bool Free(WorldInteractiveObject door)
        {
            return !(door is Door d) || !DoorDestructionRegistry.TryGet(d, out DoorEntry entry) || entry.Takeover == null;
        }
    }

    internal class DestructionInteractGuardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Door), nameof(Door.Interact), new[] { typeof(InteractionResult) });
        }

        [PatchPrefix]
        private static bool Prefix(Door __instance)
        {
            return DestructionOwnership.Free(__instance);
        }
    }

    internal class DestructionSetStateGuardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WorldInteractiveObject), nameof(WorldInteractiveObject.SetDoorState), new[] { typeof(EDoorState), typeof(bool) });
        }

        [PatchPrefix]
        private static bool Prefix(WorldInteractiveObject __instance)
        {
            return DestructionOwnership.Free(__instance);
        }
    }

    internal class DestructionKickGuardPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Door), nameof(Door.KickOpen), new[] { typeof(Vector3), typeof(bool) });
        }

        [PatchPrefix]
        private static bool Prefix(Door __instance)
        {
            return DestructionOwnership.Free(__instance);
        }
    }
}
