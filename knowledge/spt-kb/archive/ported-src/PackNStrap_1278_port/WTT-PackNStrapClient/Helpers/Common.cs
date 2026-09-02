

using System;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using PackNStrap.Core.Items;

namespace PackNStrap.Helpers;

public abstract class Common
{
    public static List<CustomContainerItemClass> GetMagDumpPouches(InventoryEquipment equipment, bool backpackIncluded)
    {
        if (equipment == null)
        {
            Console.WriteLine("Equipment is null.");
            return null;
        }

        List<CustomContainerItemClass> magDumpPouches = new List<CustomContainerItemClass>();
        var magDumpPouchItemId = "440de5d056825485a0cf3a19";

        // Function to search first-level items for the pouch
        void FindMagDumpPouchInItem(Item item)
        {
            if (item == null) return;

            foreach (var itemInGrid in item.GetAllItems())
            {
                if (itemInGrid is CustomContainerItemClass potentialMagDumpPouch 
                    && potentialMagDumpPouch.StringTemplateId == magDumpPouchItemId)
                {
                    if (potentialMagDumpPouch.IsChildOf(item))
                        magDumpPouches.Add(potentialMagDumpPouch);
                }
            }
        }

        // Retrieve slots
        Slot tacticalVestSlot = equipment.GetSlot(EquipmentSlot.TacticalVest);
        Slot pocketsSlot = equipment.GetSlot(EquipmentSlot.Pockets);
        Slot backpackSlot = equipment.GetSlot(EquipmentSlot.Backpack);
        Slot armbandSlot = equipment.GetSlot(EquipmentSlot.ArmBand);

        // Check each slot for MagDumpPouches
        FindMagDumpPouchInItem(tacticalVestSlot?.ContainedItem as Vest);
        FindMagDumpPouchInItem(pocketsSlot?.ContainedItem as Pockets);
        if (backpackIncluded)
            FindMagDumpPouchInItem(backpackSlot?.ContainedItem as Backpack);
        FindMagDumpPouchInItem(armbandSlot?.ContainedItem as CustomBeltItemClass);

        // Cast magDumpPouches to CompoundItem and return
        return magDumpPouches;
    }

    public static bool CanAcceptItems(Grid grid)
    {
        Player player = PackNStrap.Player;
        // Example condition, replace with actual logic as needed
        Item handsItem = (player?.HandsController as IHandsController)?.Item;
        if (handsItem?.GetCurrentMagazine() != null)
        {
            return grid.CanAccept(handsItem.GetCurrentMagazine()); // Assuming `CanAcceptItems` is a property or method on `Grid`
        }
        return false;
    }
}