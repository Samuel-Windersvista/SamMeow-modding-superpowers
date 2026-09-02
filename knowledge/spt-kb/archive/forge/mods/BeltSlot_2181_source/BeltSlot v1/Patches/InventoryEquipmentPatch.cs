using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using InventoryInteractions = GClass3471; // There are two child versions?

namespace BeltSlot.Patches
{
    // Create the submenu options (inventory screen)
    public class InventoryEquipmentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryEquipment), nameof(InventoryEquipment.GetSlot));
        }

        [PatchPrefix]
        public static bool Prefix(InventoryEquipment __instance)
        {
            if(Plugin.Instance != null)
            {
                Plugin.Instance.inventoryEquipment = __instance;
            }
            return true;
        }
    }
}