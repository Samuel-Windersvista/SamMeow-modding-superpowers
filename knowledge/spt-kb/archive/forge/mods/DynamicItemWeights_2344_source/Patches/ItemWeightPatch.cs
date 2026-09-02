using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using Tosox.DynamicItemWeights.Helpers;
using System.Reflection;
using UnityEngine;

namespace Tosox.DynamicItemWeights.Patches
{
    public class ItemWeightPatch : ModulePatch
    {
        private static UsageHelper _usageHelper;
        private static EmptyWeightDB _emptyWeightDB;

        public ItemWeightPatch(UsageHelper usageHelper, EmptyWeightDB emptyWeightDB)
        {
            _usageHelper = usageHelper;
            _emptyWeightDB = emptyWeightDB;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Item), nameof(Item.Weight));
        }

        [PatchPostfix]
        public static void PatchPostfix(Item __instance, ref float __result)
        {
            if (!Plugin.IsPluginEnabled.Value)
                return;

            try
            {
                // Only modify items with consumable usage
                if (!_usageHelper.TryGetUsageFraction(__instance, out float usage))
                    return;

                // Ignore full items
                if (usage >= 0.9999f)
                    return;

                float original = __result;

                // Determine empty weight
                if (!_emptyWeightDB.TryGet(__instance.TemplateId, out float emptyWeight))
                {
                    emptyWeight = Mathf.Max(0.0f, Plugin.DefaultEmptyFraction.Value * original);
                    Logger.LogWarning($"Item '{__instance.LocalizedName()}' ({__instance.TemplateId}) not found in empty weight DB");
                }

                // Clamp sanity
                emptyWeight = Mathf.Clamp(emptyWeight, 0.0f, original);

                // Interpolate
                float adjusted = emptyWeight + (original - emptyWeight) * usage;

                __result = adjusted;

                Logger.LogDebug($"Item '{__instance.LocalizedName()}' weight adjusted: {original} -> {adjusted} (empty: {emptyWeight}, usage: {usage})");
            }
            catch { /* Ignore */ }
        }
    }
}
