using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BeltSlot.Patches
{
    // Create the submenu options (inventory screen)
    public class GetPrioritizedContainersPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // 4.1.2: GetPrioritizedContainersForLoot moved from GClass3168 (instance)
            // to the static extension class InventoryEquipmentExtension.
            return AccessTools.Method(typeof(InventoryEquipmentExtension), nameof(InventoryEquipmentExtension.GetPrioritizedContainersForLoot));
        }

        [PatchPrefix]
        public static bool Prefix(InventoryEquipment equipment, Item item, ref IEnumerable<EFT.InventoryLogic.IContainer> __result)
        {
            Slot slot = equipment.GetSlot(EquipmentSlot.TacticalVest);
            Slot slot2 = equipment.GetSlot(EquipmentSlot.Backpack);
            Slot slot3 = equipment.GetSlot(EquipmentSlot.Pockets);
            Slot slot4 = equipment.GetSlot(EquipmentSlot.SecuredContainer);
            Slot slot5 = equipment.GetSlot(EquipmentSlot.ArmBand);
            Vest vestItemClass = slot.ContainedItem as Vest;
            Backpack backpackItemClass = slot2.ContainedItem as Backpack;
            Pockets pocketsItemClass = slot3.ContainedItem as Pockets;
            MobContainer mobContainerItemClass = slot4.ContainedItem as MobContainer;

            // Additional items for tactical belt
            Vest tacticalBeltItemClass = slot5.ContainedItem as Vest;

            // Tactical Rig Location
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

            // Backpack Location
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

            // Pockets Location
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

            // Secured Container Location
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

            // Additional items for tactical belt
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
            if (item is Magazine)
            {
                // enumerable2 is chestrig, enumerable4 is backpack, enumerable6 is pockets,
                // enumerable8 is secured container, and enumerable12 is tactical belt
                __result = enumerable2.Concat(enumerable12).Concat(enumerable6).Concat(enumerable4).Concat(enumerable8);
                return false;
            }
            if (item is Ammo)
            {
                __result = enumerable12.Concat(enumerable2).Concat(enumerable6).Concat(enumerable4).Concat(enumerable8);
                return false;
            }
            if (item is Money)
            {
                __result = enumerable8.Concat(enumerable4).Concat(enumerable2).Concat(enumerable12).Concat(enumerable6);
                return false;
            }
            if (item is ThrowWeap)
            {
                __result = enumerable6.Concat(enumerable12).Concat(enumerable2).Concat(enumerable4).Concat(enumerable8);
                return false;
            }
            __result = enumerable4.Concat(enumerable2).Concat(enumerable12).Concat(enumerable6).Concat(enumerable8);
            return false;
        }
    }
}