using System.Reflection;
using HarmonyLib;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Core.Patches;

public class SkillClassOnTriggerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(EFT.Skill), nameof(EFT.Skill.OnTrigger));
    }

    [PatchPrefix]
    public static void PatchPrefix(EFT.Skill __instance)
    {
        if (__instance.SkillManager.BonusController is not null) return;
        
        // BonusController is called in EFT.Skill.OnTrigger and must not be null,
        // otherwise it will trigger System.NullReferenceException.
        __instance.SkillManager.BonusController = GameUtils.GetProfile(true)?.BonusController;
    }
}