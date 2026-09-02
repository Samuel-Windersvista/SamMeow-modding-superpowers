using System.Reflection;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class WeaponPreviewPatches
{
    public static void Enable()
    {
        new WeaponPreviewCameraNearClipPatch().Enable();
    }

    public class WeaponPreviewCameraNearClipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Called when WeaponPreview is opened and fully initialized;
            // WeaponPreview is used both by weapon modding screen, edit build screen, and item overview
            return AccessTools.Method(typeof(WeaponPreview), nameof(WeaponPreview.SetupItemPreview));
        }

        [PatchPostfix]
        public static void Postfix(WeaponPreview __instance)
        {
            if (__instance.WeaponPreviewCamera != null)
            {
                __instance.WeaponPreviewCamera.nearClipPlane = 0.01f;
            }
        }
    }
}
