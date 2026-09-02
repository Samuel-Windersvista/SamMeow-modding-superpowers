using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace BeltSlot.Patches
{
    public class InventoryControllerPatch : ModulePatch
    {
        private static GClass3180 baseType;
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(InventoryController), 
                    nameof(InventoryController.ReplaceInventory));
        }
        [PatchPrefix]
        public static bool Prefix(InventoryController __instance, Inventory newInventory)
        {
            baseType = __instance as GClass3180;

            if (__instance.Inventory != null)
            {
                baseType.RemoveItemEvent -= new Action<GEventArgs3>(__instance.Inventory.UpdateTotalWeight);
                baseType.AddItemEvent -= new Action<GEventArgs2>(__instance.Inventory.UpdateTotalWeight);
                baseType.RefreshItemEvent -= new Action<GEventArgs18>(__instance.Inventory.UpdateTotalWeight);
                __instance.Inventory = null;
            }
            __instance.Inventory = newInventory;
            // Skip original logic removing armband slots from scavs
            //if (__instance.Profile.Side == EPlayerSide.Savage)
            //{
            //    __instance.Inventory.Equipment.GetSlot(EquipmentSlot.ArmBand).Deleted = true;
            //}
            baseType.RemoveItemEvent += new Action<GEventArgs3>(__instance.Inventory.UpdateTotalWeight);
            baseType.AddItemEvent += new Action<GEventArgs2>(__instance.Inventory.UpdateTotalWeight);
            baseType.RefreshItemEvent += new Action<GEventArgs18>(__instance.Inventory.UpdateTotalWeight);
            if (__instance.Inventory.Stash != null && __instance.Inventory.Stash.CurrentAddress == null)
            {
                __instance.Inventory.Stash.CurrentAddress = baseType.CreateItemAddress();
            }
            return false; // Skip the original method execution
        }
    }
}