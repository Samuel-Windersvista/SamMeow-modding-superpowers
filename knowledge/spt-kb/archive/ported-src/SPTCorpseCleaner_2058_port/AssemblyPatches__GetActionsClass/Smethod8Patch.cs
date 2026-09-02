using EFT;
using EFT.Interactive;
using EFT.UI;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace SPTCorpseCleaner.AssemblyPatches__GetActionsClass {
    /// <summary>allow search zombies</summary>
    public class Smethod8Patch : ModulePatch {
        protected override MethodBase GetTargetMethod () {
            return typeof(EFT.InteractionContextHelper).GetMethod(nameof(EFT.InteractionContextHelper.GetAvailableActions), new Type[] { typeof(GamePlayerOwner), typeof(LootItem) });
        }

        [PatchPostfix]
        public static void Postfix (GamePlayerOwner owner, LootItem lootItem, ref AvailableInteractionState __result) {
            if (__result != null) { return; }
            __result = EFT.InteractionContextHelper.GetAvailableInteractionState(owner, lootItem.ItemOwner.RootItem, lootItem.ItemOwner, lootItem.ItemId, lootItem.Name, lootItem.LastOwner);
        }
    }
}
