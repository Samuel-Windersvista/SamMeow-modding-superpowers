using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using InventoryInteractions = GClass3471; // There are two child versions?
using PackNStrap.Core.Items;
//using PackNStrap.Helpers;

namespace BeltSlot.Patches
{
    // Create the submenu options (inventory screen)
    public class GetPrioritizedContainersPackNStrapPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass3168), nameof(GClass3168.GetPrioritizedContainersForLoot));
        }

        [PatchPrefix]
        public static bool Prefix(InventoryEquipment equipment, Item item, ref IEnumerable<EFT.InventoryLogic.IContainer> __result)
        {
            Slot slot = equipment.GetSlot(EquipmentSlot.TacticalVest);
            Slot slot2 = equipment.GetSlot(EquipmentSlot.Backpack);
            Slot slot3 = equipment.GetSlot(EquipmentSlot.Pockets);
            Slot slot4 = equipment.GetSlot(EquipmentSlot.SecuredContainer);
            Slot slot5 = equipment.GetSlot(EquipmentSlot.ArmBand);
            VestItemClass vestItemClass = slot.ContainedItem as VestItemClass;
            BackpackItemClass backpackItemClass = slot2.ContainedItem as BackpackItemClass;
            PocketsItemClass pocketsItemClass = slot3.ContainedItem as PocketsItemClass;
            MobContainerItemClass mobContainerItemClass = slot4.ContainedItem as MobContainerItemClass;

            // Additional items for custom belt and tactical belt
            CustomBeltItemClass customBeltItemClass = slot5.ContainedItem as CustomBeltItemClass;
            VestItemClass tacticalBeltItemClass = slot5.ContainedItem as VestItemClass;
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable;
            if (vestItemClass != null)
            {
                if ((enumerable = vestItemClass.Containers) != null)
                {
                    goto IL_0064;
                }
            }
            enumerable = Enumerable.Empty<EFT.InventoryLogic.IContainer>();
            IL_0064:
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable2 = enumerable;
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable3;
            if (backpackItemClass != null)
            {
                if ((enumerable3 = backpackItemClass.Containers) != null)
                {
                    goto IL_007B;
                }
            }
            enumerable3 = Enumerable.Empty<EFT.InventoryLogic.IContainer>();
            IL_007B:
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable4 = enumerable3;
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable5;
            if (pocketsItemClass != null)
            {
                if ((enumerable5 = pocketsItemClass.Containers) != null)
                {
                    goto IL_0094;
                }
            }
            enumerable5 = Enumerable.Empty<EFT.InventoryLogic.IContainer>();
            IL_0094:
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable6 = enumerable5;
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable7;
            if (mobContainerItemClass != null)
            {
                if ((enumerable7 = mobContainerItemClass.Containers) != null)
                {
                    goto IL_00AD;
                }
            }
            enumerable7 = Enumerable.Empty<EFT.InventoryLogic.IContainer>();
            IL_00AD:
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable8 = enumerable7;

            // Additional items for custom belt and tactical belt
            // Custom belts from PackNStrap

            IEnumerable<EFT.InventoryLogic.IContainer> enumerable9;
            if(customBeltItemClass != null)
            {
                 if ((enumerable9 = customBeltItemClass.Containers) != null)
                 {
                     goto IL_00C6;
                 }
            }
            enumerable9 = Enumerable.Empty<EFT.InventoryLogic.IContainer>();
            IL_00C6:
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable10 = enumerable9;

            // Tactical belts from Tactical Item Component
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable11;
            if (tacticalBeltItemClass != null)
            {
                if ((enumerable11 = tacticalBeltItemClass.Containers) != null)
                {
                    goto IL_00DF;
                }
            }
            enumerable11 = Enumerable.Empty<EFT.InventoryLogic.IContainer>();
            IL_00DF:
            IEnumerable<EFT.InventoryLogic.IContainer> enumerable12 = enumerable11;
            // Belt slot containers come after the vest in looting priority
            if (item is AmmoItemClass || item is MagazineItemClass)
            {
                __result = enumerable2.Concat(enumerable10).Concat(enumerable12).Concat(enumerable6).Concat(enumerable4).Concat(enumerable8);
                return false;
            }
            if (item is MoneyItemClass)
            {
                __result = enumerable8.Concat(enumerable4).Concat(enumerable2).Concat(enumerable10).Concat(enumerable12).Concat(enumerable6);
                return false;
            }
            if (item is ThrowWeapItemClass)
            {
                __result = enumerable6.Concat(enumerable2).Concat(enumerable10).Concat(enumerable12).Concat(enumerable4).Concat(enumerable8);
                return false;
            }
            __result = enumerable4.Concat(enumerable2).Concat(enumerable10).Concat(enumerable12).Concat(enumerable6).Concat(enumerable8);
            return false;
        }
    }
}