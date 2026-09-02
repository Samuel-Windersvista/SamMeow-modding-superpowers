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
    public class GridWindowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GridWindow), nameof(GridWindow.method_2));
        }

        [PatchPrefix]
        static void Prefix(GridWindow __instance)
        {
            
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