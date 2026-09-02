using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.FieldMedicine.Patches;

internal class PersonalBuffPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(EFT.HealthSystem.EffectsSettings.StimulatorSettings), nameof(EFT.HealthSystem.EffectsSettings.StimulatorSettings.GetPersonalBuffSettings));
    }

    [PatchPostfix]
    public static void PostFix(EFT.HealthSystem.EffectsSettings.StimulatorSettings.StimulatorBuffSettings __result)
    {
        if (!SkillsPlugin.SkillData.FieldMedicine.Enabled) return;
        if (__result.BuffName == "Pain") return;
        
        var durationBuff = SkillManagerExt.Instance(EPlayerSide.Usec).FieldMedicineDurationBonus;
        var chanceBuff = SkillManagerExt.Instance(EPlayerSide.Usec).FieldMedicineChanceBonus;
        
        __result.Duration *= 1f + durationBuff;
        __result.Delay *= __result.Delay == 1f 
            ? __result.Delay
            : 1f + durationBuff;
        
        if (__result.Chance < 1f && !__result.Value.Negative())
        {
            __result.Chance *= 1f + chanceBuff;
        }
    }
}