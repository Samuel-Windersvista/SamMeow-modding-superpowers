namespace MeaningfulWeaponMasteries
{
    using BepInEx;
    using HarmonyLib;
    using EFT.InventoryLogic;
    using EFT;
    using System;

    [BepInPlugin(GUID, NAME, VERSION)]
    public class MeaningfulWeaponMasteries : BaseUnityPlugin
    {
        public const string GUID = "com.ehaugw.meaningfulweaponmasteries";
        public const string VERSION = "1.0.8";
        public const string NAME = "Meaningful Weapon Masteries";

        internal void Awake()
        {
            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(SkillManager), "GetWeaponInfo")]
        public class SkillManager_GetWeaponInfo
        {
            [HarmonyPostfix]
            public static void Postfix(SkillManager __instance, ref SkillManager.GClass2017 __result, Item weapon)
            {
                int mastering = __instance?.GetMastering(weapon.TemplateId)?.Level ?? 0;
                __result.AimSpeed += 0.05f * mastering;
                __result.ReloadSpeed += 0.05f * mastering;
                __result.FixSpeed += 0.05f * mastering;
            }
        }

        [HarmonyPatch(typeof(Weapon), nameof(Weapon.GetTotalCenterOfImpact))]
        public class AccuracyPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Weapon __instance, ref float __result)
            {
                if (__instance.Owner is InventoryController inventoryController && inventoryController.Profile is Profile profile && profile.SkillsInfo is SkillManager manager)
                {
                    int projectileCount;

                    if ((__instance?.FirstLoadedChamberSlot?.ContainedItem ?? __instance?.GetCurrentMagazine()?.FirstRealAmmo()) is AmmoItemClass ammoClassChamber)
                    {
                        projectileCount = ammoClassChamber.ProjectileCount;
                    }
                    else
                    {
                        projectileCount = __instance.CurrentAmmoTemplate.ProjectileCount;
                    }

                    if (projectileCount > 1)
                    {
                        return;
                    }
                    const float moa_bonus_per_mastery = 0.05f;
                    var capped_moa = Math.Min(__result, 2.9089f / 100);
                    
                    int mastering = manager.GetMastering(__instance.TemplateId)?.Level ?? 0;
                    __result -= capped_moa * mastering * moa_bonus_per_mastery;
                }
            }
        }
    }
}
