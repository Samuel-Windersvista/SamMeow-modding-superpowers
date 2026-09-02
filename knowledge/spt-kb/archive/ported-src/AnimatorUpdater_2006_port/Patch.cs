using EFT;
using SPT.Reflection.Patching;
using System.Reflection;
using HarmonyLib;
using EFT.InventoryLogic;

namespace UpdateHierarchy
{
    //This patch does not have effect
    public class UpdateAnimatorPatch1 : ModulePatch
    {
        
        //i have 0 clue on what this guy does. But seems like not used
        protected override MethodBase GetTargetMethod()
        {
            // PORT-NOTE 4.1.2: GClass1808 -> ReloadInternalMagOperation（作者注释"no effect"，且此 patch 在 Plugin.cs 中被禁用）
            return AccessTools.Method(typeof(Player.FirearmController.ReloadInternalMagOperation), "OnMagAppeared");
        }

        [PatchPostfix]
        static void PatchPostfix(Player.FirearmController.ReloadInternalMagOperation __instance, Player.FirearmController ___Controller)
        {
            if (___Controller != null)
            {
               var weaponPrefab = (WeaponPrefab) AccessTools.Field(___Controller.GetType(), "_weaponPrefab").GetValue(___Controller);
                weaponPrefab.UpdateAnimatorHierarchy();
            }
        }
    }
    
    //this patch is the one that has effect on reloading, but also desync the hands on certain weapons. 
    //Weird bundle behaviour
    //Note for future: if you want a if check that uses array, do not use a loop. Thanks CJ for helping me out
    public class UpdateAnimatorPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // PORT-NOTE 4.1.2: GClass1785 -> ReloadExternalMagOperation（弹匣插入重载，作者确认"有生效"）
            return AccessTools.Method(typeof(Player.FirearmController.ReloadExternalMagOperation), "OnMagAppeared");
        }

        [PatchPostfix]
        static void PatchPostfix(Player.FirearmController.ReloadExternalMagOperation __instance, Player.FirearmController ___Controller)
        {
            if (___Controller != null)
            {
                var weaponPrefab = (WeaponPrefab) AccessTools.Field(___Controller.GetType(), "_weaponPrefab").GetValue(___Controller);
                var weapon = (Weapon)AccessTools.Field(weaponPrefab.GetType(), "_weaponData").GetValue(weaponPrefab);
                //If the id from ExclusionList matches with the weapon _id, return. If not then run the method
                if (JsonLoader.IsExcluded(weapon.Template._id.ToString()))
                {
                    return;
                }
                weaponPrefab.UpdateAnimatorHierarchy();
            }
        }
    }

    //This one does not have effect
    public class ModSetupPatch1 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // PORT-NOTE 4.1.2: GClass1821 -> RemoveModOperation（作者注释"no effect"，且此 patch 在 Plugin.cs 中被禁用）
            return AccessTools.Method(typeof(Player.FirearmController.RemoveModOperation), "OnModChanged");
        }

        [PatchPostfix]
        static void PatchPostfix
        (Player.FirearmController.RemoveModOperation __instance,
            Player.FirearmController ___Controller)
        {
            var weaponPrefab = (WeaponPrefab)AccessTools.Field(___Controller?.GetType(), "_weaponPrefab")
                .GetValue(___Controller);
            weaponPrefab.UpdateAnimatorHierarchy();
        }
    }
    
    //This one has effect
    public class ModSetupPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // PORT-NOTE 4.1.2: Class1145 -> AddModOperation（作者确认"有生效"）
            return AccessTools.Method(typeof(Player.FirearmController.AddModOperation), "OnModChanged");
        }

        [PatchPostfix]
        static void PatchPostfix
        (Player.FirearmController.AddModOperation __instance,
            Player.FirearmController ___Controller)
        {
            var weaponPrefab = (WeaponPrefab)AccessTools.Field(___Controller?.GetType(), "_weaponPrefab")
                .GetValue(___Controller);
            var weapon = (Weapon)AccessTools.Field(weaponPrefab.GetType(), "_weaponData").GetValue(weaponPrefab);
            if (JsonLoader.IsExcluded(weapon.Template._id.ToString()))
            {
                return;
            }
            weaponPrefab.UpdateAnimatorHierarchy();
        }
    }
}
