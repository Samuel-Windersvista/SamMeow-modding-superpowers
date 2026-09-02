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
            return AccessTools.Method(typeof(Player.FirearmController.GClass1808), "OnMagAppeared");
        }

        [PatchPostfix]
        static void PatchPostfix(Player.FirearmController.GClass1808 __instance, Player.FirearmController ___firearmController_0)
        {
            if (___firearmController_0 != null)
            {
               var weaponPrefab = (WeaponPrefab) AccessTools.Field(___firearmController_0.GetType(), "weaponPrefab_0").GetValue(___firearmController_0);
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
            return AccessTools.Method(typeof(Player.FirearmController.GClass1785), "OnMagAppeared");
        }

        [PatchPostfix]
        static void PatchPostfix(Player.FirearmController.GClass1785 __instance, Player.FirearmController ___firearmController_0)
        {
            if (___firearmController_0 != null)
            {
                var weaponPrefab = (WeaponPrefab) AccessTools.Field(___firearmController_0.GetType(), "weaponPrefab_0").GetValue(___firearmController_0);
                var weapon = (Weapon)AccessTools.Field(weaponPrefab.GetType(), "weapon_0").GetValue(weaponPrefab);
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
            return AccessTools.Method(typeof(Player.FirearmController.GClass1821), "OnModChanged");
        }

        [PatchPostfix]
        static void PatchPostfix
        (Player.FirearmController.GClass1821 __instance,
            Player.FirearmController ___firearmController_0)
        {
            var weaponPrefab = (WeaponPrefab)AccessTools.Field(___firearmController_0?.GetType(), "weaponPrefab_0")
                .GetValue(___firearmController_0);
            weaponPrefab.UpdateAnimatorHierarchy();
        }
    }
    
    //This one has effect
    public class ModSetupPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player.FirearmController.Class1145), "OnModChanged");
        }

        [PatchPostfix]
        static void PatchPostfix
        (Player.FirearmController.Class1145 __instance,
            Player.FirearmController ___firearmController_0)
        {
            var weaponPrefab = (WeaponPrefab)AccessTools.Field(___firearmController_0?.GetType(), "weaponPrefab_0")
                .GetValue(___firearmController_0);
            var weapon = (Weapon)AccessTools.Field(weaponPrefab.GetType(), "weapon_0").GetValue(weaponPrefab);
            if (JsonLoader.IsExcluded(weapon.Template._id.ToString()))
            {
                return;
            }
            weaponPrefab.UpdateAnimatorHierarchy();
        }
    }
}
