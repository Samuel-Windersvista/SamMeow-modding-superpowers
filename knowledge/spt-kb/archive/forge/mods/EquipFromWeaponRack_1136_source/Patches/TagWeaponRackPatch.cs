using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace DrakiaXYZ.EquipFromWeaponRack.Patches
{
    internal class TagWeaponRackPatch : ModulePatch
    {
        protected static FieldInfo _inventoryControllerField;
        protected static FieldInfo _viewListField;

        protected override MethodBase GetTargetMethod()
        {
            var targetClass = typeof(EquipItemWindow);

            _inventoryControllerField = AccessTools.Field(targetClass, "_inventoryController");
            _viewListField = AccessTools.Field(targetClass, "_viewList");

            return AccessTools.Method(targetClass, nameof(EquipItemWindow.RefreshItemViews));
        }

        [PatchPostfix]
        public static void PatchPostfix(object __instance)
        {
            var inventoryControllerClass = _inventoryControllerField.GetValue(__instance) as InventoryController;
            var inventory = inventoryControllerClass.Inventory;

            var items = _viewListField.GetValue(__instance) as ViewList<Item, ItemView>;
            foreach (var (item, itemView) in items)
            {
                if (item == null || itemView == null) continue;

                var tagPanel = itemView.transform.Find("TagPanel");
                if (tagPanel == null) continue;

                var tagName = tagPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (tagName == null) continue;

                bool inRack = false;
                foreach (var area in new EAreaType[] { EAreaType.WeaponStand, EAreaType.WeaponStandSecondary })
                {
                    if (!inventory.HideoutAreaStashes.ContainsKey(area)) continue;
                    var areaStash = inventory.HideoutAreaStashes[area];

                    if (item.IsChildOf(areaStash))
                    {
                        inRack = true;
                        break;
                    }
                }

                tagName.text = inRack ? "Rack" : "Stash";

                tagPanel.RectTransform().sizeDelta = new Vector2(60, 14);
                tagPanel.gameObject.SetActive(true);
            }
        }
    }
}
