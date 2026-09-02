using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using PackNStrap.Core.Items;
using PackNStrap.Helpers;
using SPT.Reflection.Patching;

namespace PackNStrap.Patches;

internal class GetPrioritizedGridsForUnloadedObjectPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(InventoryEquipmentExtension), nameof(InventoryEquipmentExtension.GetPrioritizedGridsForUnloadedObject));
    }

    [PatchPrefix]
    public static bool PatchPrefix(ref InventoryEquipment equipment, bool backpackIncluded, ref IEnumerable<Grid> __result)
    {
        // Retrieve slots
        Slot tacticalVestSlot = equipment.GetSlot(EquipmentSlot.TacticalVest);
        Slot pocketsSlot = equipment.GetSlot(EquipmentSlot.Pockets);
        Slot backpackSlot = equipment.GetSlot(EquipmentSlot.Backpack);
        Slot armbandSlot = equipment.GetSlot(EquipmentSlot.ArmBand);

        // Handle contained items
        Vest tacticalVestItem = tacticalVestSlot?.ContainedItem as Vest;
        Pockets pocketsItem = pocketsSlot?.ContainedItem as Pockets;
        Backpack backpackItem = backpackSlot?.ContainedItem as Backpack;
        CustomBeltItemClass armbandItem = armbandSlot?.ContainedItem as CustomBeltItemClass;

        // Retrieve grids or empty arrays if items are null
        Grid[] tacticalVestGrids = tacticalVestItem?.Grids ?? Array.Empty<Grid>();
        Grid[] pocketsGrids = pocketsItem?.Grids ?? Array.Empty<Grid>();
        Grid[] backpackGrids = backpackItem?.Grids ?? Array.Empty<Grid>();
        Grid[] armbandGrids = armbandItem?.Grids ?? Array.Empty<Grid>();

        // Find all instances of magDumpPouch
        List<CustomContainerItemClass> magDumpPouches = Common.GetMagDumpPouches(equipment, backpackIncluded);

        // Retrieve grids for all found magDumpPouches that can accept items
        List<Grid> magDumpPouchGrids = magDumpPouches
            .SelectMany(pouch => pouch.Grids ?? Array.Empty<Grid>())
            .Where(Common.CanAcceptItems) // Check if grid can accept items
            .ToList();

        if (magDumpPouchGrids.Count > 0)
        {
#if DEBUG
            Console.WriteLine("Returning only MagDumpPouch grids that can accept items.");
#endif
            __result = magDumpPouchGrids; // Return only MagDumpPouch grids if valid
            return false;
        }
#if DEBUG
        Console.WriteLine("No valid MagDumpPouch grids found.");
#endif
        // Fall back to returning other grids if no valid MagDumpPouch grids
        __result = backpackIncluded
            ? tacticalVestGrids.Concat(pocketsGrids).Concat(backpackGrids).Concat(armbandGrids)
            : tacticalVestGrids.Concat(pocketsGrids).Concat(armbandGrids);

        return false; 
    }

}