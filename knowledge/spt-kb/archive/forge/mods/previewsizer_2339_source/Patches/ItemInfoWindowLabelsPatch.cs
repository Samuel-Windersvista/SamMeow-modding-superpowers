using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using EFT;
using EFT.UI;
using EFT.UI.WeaponModding;
using UnityEngine.UI;
using UnityEngine;

namespace acidphantasm_previewsizer.Patches
{
    internal class ItemInfoWindowLabelsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemInfoWindowLabels), nameof(ItemInfoWindowLabels.Show));
        }

        [PatchPostfix]
        static void Postfix(ItemInfoWindowLabels __instance, GameObject ____previewPanel)
        {
            if (__instance == null) 
                return;

            if (!____previewPanel.TryGetComponent<LayoutElement>(out var layoutElement)) 
                return;
            
            layoutElement.preferredHeight = Plugin._previewMinHeight;
            layoutElement.minHeight = Plugin._previewMinHeight;
        }
    }
}
