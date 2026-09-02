using BepInEx.Logging;
using EFT.InventoryLogic;
using UnityEngine;

namespace Tosox.DynamicItemWeights.Helpers
{
    public class UsageHelper
    {
        private readonly ManualLogSource _logger;

        public UsageHelper(ManualLogSource logger)
        {
            _logger = logger;
        }

        public bool TryGetUsageFraction(Item item, out float fraction)
        {
            fraction = 1.0f;

            if (item.TryGetItemComponent<MedKitComponent>(out var medkit))
            {
                fraction = Mathf.Clamp(medkit.HpResource / medkit.MaxHpResource, 0.0f, 1.0f);
                return true;
            }

            if (item.TryGetItemComponent<FoodDrinkComponent>(out var food) && food.MaxResource > 1.0f)
            {
                fraction = Mathf.Clamp(food.HpPercent / food.MaxResource, 0.0f, 1.0f);
                return true;
            }

            if (item.TryGetItemComponent<ResourceComponent>(out var resource) && item is FuelItemClass)
            {
                fraction = Mathf.Clamp(resource.Value / resource.MaxResource, 0.0f, 1.0f);
                return true;
            }

            if (item.TryGetItemComponent<RepairKitComponent>(out var repairKit))
            {
                fraction = Mathf.Clamp(repairKit.Resource / repairKit.RepairKitsTemplateClass.MaxRepairResource, 0.0f, 1.0f);
                return true;
            }

            return false;
        }
    }
}
