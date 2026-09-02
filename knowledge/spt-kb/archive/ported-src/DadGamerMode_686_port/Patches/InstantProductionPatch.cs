using System;
using System.Collections.Generic;
using System.Reflection;
using dvize.GodModeTest;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace dvize.DadGamerMode.Patches
{

    // Patch for the Update method
    internal class InstantUpdatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemsProducerBase), nameof(ItemsProducerBase.Update));
        }

        [PatchPrefix]
        private static bool Prefix(ItemsProducerBase __instance, float deltaTime)
        {
            if (dadGamerPlugin.InstantProductionEnabled.Value)
            {
                if (__instance == null || __instance.ProducingItems == null)
                {
                    return false;
                }

                // Filter itemsToComplete by removing bitcoin farm
                List<KeyValuePair<string, ProducingItemController>> itemsToComplete = new List<KeyValuePair<string, ProducingItemController>>(__instance.ProducingItems);
                itemsToComplete.RemoveAll(x => x.Key == "5d5589c1f934db045e6c5492" || x.Key == "5d5c205bd582a50d042a3c0e"); //bitcoin and fuel?

                foreach (var kvp in itemsToComplete)
                {
                    if (__instance.Schemes != null && __instance.Schemes.TryGetValue(kvp.Key, out BaseHideoutScheme scheme))
                    {
                        __instance.CompleteProduction(kvp.Value, scheme);
                    }
                }

                // Allow normal update processing for Bitcoin items
                return true;
            }

            return true;
        }
    }

    // Extension method to handle CompleteProduction
    internal static class ItemsProducerExtensions
    {
        private static readonly FieldInfo ProducingProcessField;
        private static readonly PropertyInfo EndTimeProperty;
        private static readonly PropertyInfo ProgressProperty;

        static ItemsProducerExtensions()
        {
            // SPT 4.1: ProducingItemController._producingProcess (ProducingProcess) drives progress
            ProducingProcessField = AccessTools.Field(typeof(ProducingItemController), "_producingProcess");
            if (ProducingProcessField != null)
            {
                EndTimeProperty = AccessTools.Property(ProducingProcessField.FieldType, "EndTime");
                ProgressProperty = AccessTools.Property(ProducingProcessField.FieldType, "Progress");
            }
        }

        public static void CompleteProduction(this ItemsProducerBase __instance, ProducingItemController producingItem, BaseHideoutScheme scheme)
        {
            if (__instance == null || producingItem == null || scheme == null)
            {
                dadGamerPlugin.Logger.LogError("CompleteProduction: __instance, producingItem, or scheme is null.");
                return;
            }

            try
            {
                var producingProcess = ProducingProcessField?.GetValue(producingItem);
                if (producingProcess == null)
                {
                    dadGamerPlugin.Logger.LogError("CompleteProduction: producingProcess is null.");
                    return;
                }

                // Force the production process to complete: the normal Update flow of the producer
                // (ProducingItemController.Produce) recomputes Progress from EndTime, so move EndTime to
                // now and pin Progress at 1.0. The game then moves the item into the done/complete pipeline.
                if (EndTimeProperty != null)
                {
                    EndTimeProperty.SetValue(producingProcess, (double)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                }
                if (ProgressProperty != null)
                {
                    ProgressProperty.SetValue(producingProcess, 1.0);
                }
            }
            catch (Exception ex)
            {
                dadGamerPlugin.Logger.LogError($"Unexpected error during CompleteProduction: {ex.Message}");
            }
        }
    }
}
