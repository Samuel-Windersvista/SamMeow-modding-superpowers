using EFT;
using EFT.Interactive;
using SPT.Reflection.Patching;
using System.Reflection;

namespace SPTCorpseCleaner.AssemblyPatches__GetActionsClass {
    /// <summary>allow search zombies</summary>
    public class Smethod8Patch : ModulePatch {
        protected override MethodBase GetTargetMethod () {
            return typeof(GetActionsClass).GetMethod(nameof(GetActionsClass.smethod_8));
        }

        [PatchPostfix]
        public static void Postfix (GamePlayerOwner owner, LootItem lootItem, ref ActionsReturnClass __result) {
            if(__result!=null){return;}
            __result = GetActionsClass.smethod_9(owner,lootItem.ItemOwner.RootItem,lootItem.ItemOwner,lootItem.ItemId,lootItem.Name,lootItem.LastOwner);
        }
    }
}
