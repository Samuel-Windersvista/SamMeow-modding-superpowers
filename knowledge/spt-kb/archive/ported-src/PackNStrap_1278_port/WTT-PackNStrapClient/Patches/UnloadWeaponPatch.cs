using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using PackNStrap.Core.Items;
using SPT.Reflection.Patching;
using PackNStrap.Helpers;

namespace PackNStrap.Patches;

internal class UnloadWeaponPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnloadWeapon));
    }

    [PatchPrefix]
    public static bool UnloadWeaponPrefix(ItemUiContext __instance, ref Weapon weapon, ref Task __result)
    {
        if (!EFT.InGameStatus.InRaid)
        {
            return true;
        }
        
        #if DEBUG
        Console.WriteLine($"Starting CustomUnloadWeapon for weapon: {weapon.StringTemplateId}");
        #endif
        try
        {
            // Set the result to the Task returned by CustomUnloadWeapon
            __result = CustomUnloadWeapon(__instance, weapon);
            return false;
        }
        catch (Exception ex) 
        {
            Console.WriteLine(ex.ToString());
            return true;
        }
    }

    private static async Task CustomUnloadWeapon(ItemUiContext __instance, Weapon weapon)
    {
        ItemController itemController = (ItemController)
            AccessTools.Field(typeof(ItemUiContext),
                    "_itemController")
                .GetValue(__instance);
        CompoundItem[] rightPanelItems = (CompoundItem[])
            AccessTools.Field(typeof(ItemUiContext),
                    "_rightPanelItem")
                .GetValue(__instance);
        InventoryEquipment inventoryEquipment = (InventoryEquipment)
            AccessTools.Field(typeof(ItemUiContext),
                    "_equipment")
                .GetValue(__instance);
        Magazine currentMagazine = weapon.GetCurrentMagazine();
        if (currentMagazine != null)
        {
            // MagDumpPouch logic
            List<CustomContainerItemClass> magDumpPouches = Common.GetMagDumpPouches(inventoryEquipment, false);
            
#if DEBUG
            Console.WriteLine($"Found {magDumpPouches?.Count ?? 0} MagDumpPouches");
#endif
            IEnumerable<CompoundItem> enumerable = new CompoundItem[] { inventoryEquipment };
            if (rightPanelItems != null && rightPanelItems.Length > 0)
            {
                enumerable = enumerable.Concat(rightPanelItems);
            }
            IEnumerable<CompoundItem> containers;
            if (magDumpPouches != null && magDumpPouches.Count > 0)
            {
                containers = magDumpPouches.Concat(enumerable);
            }
            else
            {
                containers = enumerable;
            }

#if DEBUG
            Console.WriteLine("[AFTER] Final search order:");
            LogContainers(containers);
#endif
            ItemManipulator.QuickFindAppropriatePlace(currentMagazine, itemController, containers, ItemManipulator.EMoveItemOrder.PrioritizeTargetsOrder, false);
        }
    }
    private static void LogContainers(IEnumerable<CompoundItem> containers)
    {
        if (containers == null)
        {
            Console.WriteLine("No containers available");
            return;
        }

        foreach (var container in containers)
        {
            Console.WriteLine($"- Container: {container.Name} ({container.Id})");
        }
    }
}
