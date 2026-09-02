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
    internal class ItemSpecificationPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemSpecificationPanel), nameof(ItemSpecificationPanel.method_4));
        }

        [PatchPrefix]
        static bool Prefix(ItemSpecificationPanel __instance)
        {
            return __instance == null || !Plugin._actionPanel;
        }
    }
}
