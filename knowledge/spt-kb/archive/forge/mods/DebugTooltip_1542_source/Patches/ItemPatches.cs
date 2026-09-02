using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DebugTooltip
{
    internal class ItemPatches
    {
        public static void Enable()
        {
            new GridItemViewPatch().Enable();
            new TradingItemViewPatch().Enable();
            new SlotViewPatch().Enable();
            new ModSlotViewPatch().Enable();
            new ModSlotArmorPatch().Enable();
            new ModdingScreenSlotViewPatch().Enable();
        }

        private class GridItemViewPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(GridItemView), nameof(GridItemView.ShowTooltip));
            }

            [PatchPrefix]
            public static void Prefix(GridItemView __instance)
            {
                if (!Settings.ShowDebugInfo.Value)
                {
                    return;
                }

                DebugTooltip.SetDebugInfo(new ItemDebugInfo(__instance.Item));
            }
        }

        private class TradingItemViewPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(TradingItemView), nameof(TradingItemView.ShowTooltip));
            }

            [PatchPrefix]
            public static void Prefix(TradingItemView __instance)
            {
                if (!Settings.ShowDebugInfo.Value)
                {
                    return;
                }

                DebugTooltip.SetDebugInfo(new ItemDebugInfo(__instance.Item));
            }
        }

        public class SlotViewPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(SlotView), nameof(SlotView.Show));
            }

            [PatchPostfix]
            public static void Postfix(SlotView __instance)
            {
                var hoverTrigger = __instance.GetOrAddComponent<HoverTrigger>();

                void OnHoverStart(PointerEventData eventData)
                {
                    if (Settings.ShowDebugInfo.Value && __instance.Slot.ContainedItem == null)
                    {
                        DebugTooltip.SetDebugInfo(new EmptySlotDebugInfo(__instance.Slot));
                        ItemUiContext.Instance.Tooltip.Show(string.Empty);
                    }
                }

                hoverTrigger.OnHoverStart += OnHoverStart;
                new R.UIElement(__instance).UI.AddDisposable(() => hoverTrigger.OnHoverStart -= OnHoverStart);

                hoverTrigger.OnHoverEnd -= OnHoverEnd;
                hoverTrigger.OnHoverEnd += OnHoverEnd;
            }

            private static void OnHoverEnd(PointerEventData eventData)
            {
                ItemUiContext.Instance.Tooltip.Close();
            }
        }

        private class ModSlotViewPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(ModSlotView), nameof(ModSlotView.OnPointerEnter));
            }

            [PatchPrefix]
            public static void Prefix(ModSlotView __instance)
            {
                if (!Settings.ShowDebugInfo.Value)
                {
                    return;
                }

                if (__instance.Slot.ContainedItem == null)
                {
                    DebugTooltip.SetDebugInfo(new EmptySlotDebugInfo(__instance.Slot));
                }
            }

            [PatchPostfix]
            public static void Postfix(ModSlotView __instance)
            {
                if (!Settings.ShowDebugInfo.Value)
                {
                    return;
                }

                if (__instance.Slot.ContainedItem == null && !ItemUiContext.Instance.Tooltip.isActiveAndEnabled)
                {
                    ItemUiContext.Instance.Tooltip.Show(string.Empty);
                }
            }
        }

        private class ModSlotArmorPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ModSlotView), nameof(ModSlotView.method_15));
            }

            [PatchPostfix]
            public static void Postfix(ModSlotView __instance, ref bool __result)
            {
                if (!Settings.ShowDebugInfo.Value)
                {
                    return;
                }

                if (__result)
                {
                    DebugTooltip.SetDebugInfo(new ItemDebugInfo(__instance.Slot.ContainedItem));
                }
            }
        }

        private class ModdingScreenSlotViewPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod()
            {
                return AccessTools.Method(typeof(ModdingScreenSlotView), nameof(ModdingScreenSlotView.Start));
            }

            [PatchPostfix]
            public static void Postfix(ModdingScreenSlotView __instance, RectTransform ____tooltipHoverArea, Slot ___slot_0)
            {
                var hoverTrigger = ____tooltipHoverArea.GetComponent<HoverTrigger>();
                if (hoverTrigger == null)
                {
                    return;
                }

                void OnHoverStart(PointerEventData eventData)
                {
                    if (Settings.ShowDebugInfo.Value && ___slot_0.ContainedItem == null)
                    {
                        DebugTooltip.SetDebugInfo(new EmptySlotDebugInfo(___slot_0));
                        ItemUiContext.Instance.Tooltip.Show(string.Empty);
                    }
                }

                hoverTrigger.OnHoverStart += OnHoverStart;
                new R.UIElement(__instance).UI.AddDisposable(() => hoverTrigger.OnHoverStart -= OnHoverStart);

                hoverTrigger.OnHoverEnd -= OnHoverEnd;
                hoverTrigger.OnHoverEnd += OnHoverEnd;
            }

            private static void OnHoverEnd(PointerEventData eventData)
            {
                ItemUiContext.Instance.Tooltip.Close();
            }
        }
    }
}
