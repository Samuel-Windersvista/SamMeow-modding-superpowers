using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using BeltSlot.Helpers;
using InventoryInteractions = GClass3471; // There are two child versions?

namespace BeltSlot.Patches
{
    // Create the submenu options (inventory screen)
    public class ItemUiContextPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.SetGridWindowSelectedAsTarget));
        }

        [PatchPrefix]
        public static bool Prefix(ItemUiContext __instance, GClass3544 context, ItemContextAbstractClass itemContext, bool active)
        {
            if(Plugin.Instance == null)
            {
                return true; // If Plugin.Instance is null, we skip the patch
            }
            if(!Plugin.Instance.widowedGridWindow.activeSelf)
            {
                return false;
            }
            return true;
            //__instance.method_2(false);
        }

        [PatchPostfix]
        static void PostFix()
        {
            if(Plugin.Instance != null)
            {
                //Plugin.Instance.windowLoaded = true;
            }
        }
    }
}