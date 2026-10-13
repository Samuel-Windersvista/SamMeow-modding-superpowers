using System;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine.EventSystems;

namespace SamMeow.DebugToolkit.Modules.Tooltip.Patches
{
    /// <summary>库存/网格物品视图：悬停时准备物品调试信息。</summary>
    internal sealed class GridItemViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(GridItemView), nameof(GridItemView.ShowTooltip));

        [PatchPrefix]
        private static void Prefix(GridItemView __instance)
        {
            try
            {
                if (!TooltipSettings.ShowDebugInfoValue)
                {
                    return;
                }

                DebugTooltipState.SetDebugInfo(new ItemDebugInfo(__instance.Item));
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] GridItemView.ShowTooltip prefix 失败：{e.Message}");
            }
        }
    }

    /// <summary>交易物品视图：悬停时准备物品调试信息。</summary>
    internal sealed class TradingItemViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(TradingItemView), nameof(TradingItemView.ShowTooltip));

        [PatchPrefix]
        private static void Prefix(TradingItemView __instance)
        {
            try
            {
                if (!TooltipSettings.ShowDebugInfoValue)
                {
                    return;
                }

                DebugTooltipState.SetDebugInfo(new ItemDebugInfo(__instance.Item));
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] TradingItemView.ShowTooltip prefix 失败：{e.Message}");
            }
        }
    }

    /// <summary>槽位视图：后置挂 HoverTrigger，悬停空槽时显示槽位调试信息。</summary>
    internal sealed class SlotViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(SlotView), nameof(SlotView.Show));

        [PatchPostfix]
        private static void Postfix(SlotView __instance)
        {
            try
            {
                HoverTrigger hoverTrigger = __instance.GetOrAddComponent<HoverTrigger>();
                TooltipHelpers.AttachDebugHover(__instance, hoverTrigger, () =>
                {
                    Slot slot = __instance.Slot;
                    return slot != null && slot.ContainedItem == null ? new EmptySlotDebugInfo(slot) : null;
                });
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] SlotView.Show postfix 失败：{e.Message}");
            }
        }
    }

    /// <summary>改装槽视图：进入时准备空槽调试信息，必要时补一个显示调用。</summary>
    internal sealed class ModSlotViewEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.DeclaredMethod(typeof(ModSlotView), nameof(ModSlotView.OnPointerEnter));

        [PatchPrefix]
        private static void Prefix(ModSlotView __instance)
        {
            try
            {
                if (!TooltipSettings.ShowDebugInfoValue)
                {
                    return;
                }

                Slot slot = __instance.Slot;
                if (slot != null && slot.ContainedItem == null)
                {
                    DebugTooltipState.SetDebugInfo(new EmptySlotDebugInfo(slot));
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] ModSlotView.OnPointerEnter prefix 失败：{e.Message}");
            }
        }

        [PatchPostfix]
        private static void Postfix(ModSlotView __instance)
        {
            try
            {
                if (!TooltipSettings.ShowDebugInfoValue)
                {
                    return;
                }

                Slot slot = __instance.Slot;
                if (slot == null || slot.ContainedItem != null)
                {
                    return;
                }

                var context = ItemUiContext.Instance;
                var tooltip = context?.Tooltip;
                if (tooltip != null && !tooltip.isActiveAndEnabled)
                {
                    TooltipHelpers.ShowTooltip(string.Empty);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] ModSlotView.OnPointerEnter postfix 失败：{e.Message}");
            }
        }
    }

    /// <summary>护甲槽：成功取到护甲槽时准备物品调试信息。</summary>
    internal sealed class ModSlotArmorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ModSlotView), nameof(ModSlotView.TryGetArmorSlot));

        [PatchPostfix]
        private static void Postfix(ModSlotView __instance, ref bool __result)
        {
            try
            {
                if (!TooltipSettings.ShowDebugInfoValue || !__result)
                {
                    return;
                }

                Slot slot = __instance.Slot;
                Item contained = slot?.ContainedItem;
                if (contained != null)
                {
                    DebugTooltipState.SetDebugInfo(new ItemDebugInfo(contained));
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] ModSlotView.TryGetArmorSlot postfix 失败：{e.Message}");
            }
        }
    }

    /// <summary>
    /// 改装界面槽位：在 _tooltipHoverArea 上订阅悬停，显示空槽调试信息。
    /// </summary>
    /// <remarks>
    /// 触点表 R2：<c>_slot</c> 在 0.16.9 为私有字段，且 <c>Start()</c> 早于 <c>Show()</c> 执行
    /// （此时 _slot 尚为 null）。故不用 Harmony 参数注入按值捕获，而是用
    /// <c>AccessTools.FieldRefAccess</c> 在悬停回调时实时读取 + 空判。
    /// 0.16.9 的 <c>Start()</c> 自身已为 _tooltipHoverArea 添加 HoverTrigger，
    /// 这里 GetOrAddComponent 命中既有组件并追加自身订阅。
    /// </remarks>
    internal sealed class ModdingScreenSlotViewPatch : ModulePatch
    {
        private static AccessTools.FieldRef<ModdingScreenSlotView, Slot> _slotRef;

        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ModdingScreenSlotView), nameof(ModdingScreenSlotView.Start));

        [PatchPostfix]
        private static void Postfix(ModdingScreenSlotView __instance)
        {
            try
            {
                var hoverArea = __instance._tooltipHoverArea;
                if (hoverArea == null)
                {
                    return;
                }

                HoverTrigger hoverTrigger = hoverArea.gameObject.GetOrAddComponent<HoverTrigger>();

                AccessTools.FieldRef<ModdingScreenSlotView, Slot> slotRef = GetSlotRef();
                if (slotRef == null)
                {
                    return;
                }

                TooltipHelpers.AttachDebugHover(__instance, hoverTrigger, () =>
                {
                    Slot slot = slotRef(__instance);
                    return slot != null && slot.ContainedItem == null ? new EmptySlotDebugInfo(slot) : null;
                });
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] ModdingScreenSlotView.Start postfix 失败：{e.Message}");
            }
        }

        /// <summary>惰性解析私有字段 _slot 的 FieldRef；解析失败返回 null（该触点降级，其余功能不受影响）。</summary>
        private static AccessTools.FieldRef<ModdingScreenSlotView, Slot> GetSlotRef()
        {
            if (_slotRef != null)
            {
                return _slotRef;
            }

            try
            {
                _slotRef = AccessTools.FieldRefAccess<ModdingScreenSlotView, Slot>("_slot");
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 解析 ModdingScreenSlotView._slot 失败（改装界面空槽提示降级）：{e.Message}");
                return null;
            }

            return _slotRef;
        }
    }
}
