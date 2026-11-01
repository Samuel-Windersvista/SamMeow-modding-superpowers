using System.Reflection;
using EFT.InputSystem;
using DoorOverhaul.Helpers;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace DoorOverhaul.Patches
{
    internal class InteractKeyCapturePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InputKeyCombination), nameof(InputKeyCombination.UpdateCommand), new[] { typeof(float) });
        }

        [PatchPostfix]
        private static void Postfix(InputKeyCombination __instance)
        {
            if (__instance.GameKey == EGameKey.Interact)
            {
                InteractKey.Capture(__instance.InputKeys);
            }
        }
    }
}
