using System;
using System.Collections.Generic;
using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SamMeow.DebugToolkit.Modules.QoL.Patches
{
    /// <summary>
    /// <c>EFT.InventoryLogic.InventoryEquipment.GetTotalWeight(IEnumerable&lt;Slot&gt;)</c> static prefix：
    /// 按 <c>Item Total Weight %</c> 缩放装备总重。
    /// </summary>
    /// <remarks>
    /// 0.16.9 混淆名映射（见 <c>docs/api-touchpoints/dadgamer.md</c> §2.3）：旧
    /// <c>EquipmentClass.method_10(IEnumerable&lt;Slot&gt;)</c> → <c>InventoryEquipment.GetTotalWeight</c>（static）；
    /// 槽内物品取 <c>Slot.ContainedItem</c>，物品重量取 <c>Item.TotalWeight</c>。
    /// 因目标为 static，Prefix 无 <c>__instance</c>，直接置 <c>__result</c> 并 <c>return false</c>。
    /// 百分比为 100（正常）时不改写、直接放行（零副作用）。
    /// </remarks>
    internal sealed class OnWeightUpdatedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(
                typeof(InventoryEquipment),
                nameof(InventoryEquipment.GetTotalWeight),
                new[] { typeof(IEnumerable<Slot>) });

        [PatchPrefix]
        private static bool Prefix(ref float __result, IEnumerable<Slot> slots)
        {
            try
            {
                if (!QoLSettings.Ready)
                {
                    return true;
                }

                int percent = QoLSettings.ItemTotalWeight.Value;
                if (percent >= 100 || slots == null)
                {
                    return true;
                }

                float total = 0f;
                foreach (Slot slot in slots)
                {
                    Item item = slot?.ContainedItem;
                    if (item != null)
                    {
                        total += item.TotalWeight;
                    }
                }

                __result = total * (percent / 100f);
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[QoL] 负重 Prefix 异常（fail-open）：{ex.Message}");
                return true;
            }
        }
    }
}
