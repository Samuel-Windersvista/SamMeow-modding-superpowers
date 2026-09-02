using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DrakiaXYZ.EquipFromWeaponRack.Patches
{
    internal class EquipItemWindowListPatch : ModulePatch
    {
        protected static FieldInfo _inventoryControllerField;
        protected static FieldInfo _itemAddressField;


        protected override MethodBase GetTargetMethod()
        {
            var targetClass = typeof(EquipItemWindow);

            _inventoryControllerField = AccessTools.Field(targetClass, "_inventoryController");
            _itemAddressField = AccessTools.Field(targetClass, "_itemAddress");

            return AccessTools.Method(targetClass, nameof(EquipItemWindow.GetAvailableItems));
        }

        [PatchPostfix]
        public static void PatchPostfix(GameObject ____placeHolder, EquipItemWindow __instance, ref IEnumerable<Item> __result)
        {
            var itemAddress = _itemAddressField.GetValue(__instance) as ItemAddress;
            var inventoryControllerClass = _inventoryControllerField.GetValue(__instance) as InventoryController;

            var inventory = inventoryControllerClass.Inventory;
            foreach (var area in new EAreaType[] { EAreaType.WeaponStand, EAreaType.WeaponStandSecondary })
            {
                if (!inventory.HideoutAreaStashes.ContainsKey(area)) continue;
                var areaItems = inventory.HideoutAreaStashes[area].GetNotMergedItems();
                __result = __result.Concat(areaItems);
            }

            __result = __result
                .Where(item => itemAddress != item.CurrentAddress)
                .Where(item => itemAddress.Container.CanAccept(item))
                .Where(item => {
                    Weapon weapon;
                    return (weapon = item as Weapon) == null || !weapon.MissingVitalParts.Any<Slot>();
                })
                .OrderByDescending(item => item.TemplateId);

            ____placeHolder.SetActive(!__result.Any<Item>());
        }
    }
}
