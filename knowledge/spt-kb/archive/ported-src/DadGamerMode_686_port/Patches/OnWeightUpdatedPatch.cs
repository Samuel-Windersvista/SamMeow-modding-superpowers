using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using Comfort.Common;
using dvize.GodModeTest;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace dvize.DadGamerMode.Patches
{

    internal class OnWeightUpdatedPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            // SPT 4.1: EquipmentClass was deobfuscated/merged into InventoryEquipment;
            // the per-slot weight sum is the static InventoryEquipment.GetTotalWeight
            return AccessTools.Method(typeof(InventoryEquipment), nameof(InventoryEquipment.GetTotalWeight));
        }

        [PatchPrefix]
        internal static bool Prefix(IEnumerable<Slot> slots, ref float __result)
        {

            //original functionality
            __result = slots.Sum(new Func<Slot, float>(s => s.ContainedItem != null ? s.ContainedItem.TotalWeight : 0f));

            // Get the total weight reduction setting
            float totalWeightReduction = dadGamerPlugin.totalWeightReductionPercentage.Value;

            // Convert it into a reduction factor: 0% -> full reduction (factor = 0), 100% -> no reduction (factor = 1)
            float reductionFactor = totalWeightReduction / 100f;

            // Apply the reduction factor
            __result *= reductionFactor;

            return false; // false to skip original method after prefix
        }
    }


}
