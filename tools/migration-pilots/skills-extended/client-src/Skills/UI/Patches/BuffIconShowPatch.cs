using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine.UI;

namespace SkillsExtended.Skills.UI.Patches;

internal class BuffIconShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BuffIcon), nameof(BuffIcon.Show));
    }

    [PatchPostfix]
    public static void Postfix(
        BuffIcon __instance, 
        SkillManager.Buff buff,
        Image ____icon)
    {
        var staticIcons = EFTHardSettings.Instance.StaticIcons;

        switch (buff.Id)
        {
            case (EBuffId)CustomBuffIds.FirstAidHealingSpeed:
                ____icon.sprite = staticIcons.HealEffectSprites.GetValueOrDefault(EHealthFactorType.Energy);
                break;
            
            case (EBuffId)CustomBuffIds.FirstAidResourceCost:
                ____icon.sprite = staticIcons.HealEffectSprites.GetValueOrDefault(EHealthFactorType.Health);
                break;
            
            case (EBuffId)CustomBuffIds.FirstAidMovementSpeedElite:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.StressBerserk);
                break;
            
            case (EBuffId)CustomBuffIds.FieldMedicineSkillCap:
                ____icon.sprite = staticIcons.StimulatorBuffSprites.GetValueOrDefault(EStimulatorBuffType.SkillRate);
                break;
            
            case (EBuffId)CustomBuffIds.FieldMedicineDurationBonus:
                ____icon.sprite = staticIcons.StimulatorBuffSprites.GetValueOrDefault(EStimulatorBuffType.StaminaRate);
                break;
            
            case (EBuffId)CustomBuffIds.FieldMedicineChanceBonus:
                ____icon.sprite = staticIcons.ItemAttributeSprites.GetValueOrDefault(EItemAttributeId.MoneySum);
                break;
            
            case (EBuffId)CustomBuffIds.UsecArSystemsErgo:
            case (EBuffId)CustomBuffIds.BearAkSystemsErgo:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.WeaponErgonomicsBuff);
                break;
            
            case (EBuffId)CustomBuffIds.UsecArSystemsRecoil:
            case (EBuffId)CustomBuffIds.BearAkSystemsRecoil:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.WeaponRecoilBuff);
                break;
            
            case (EBuffId)CustomBuffIds.LockpickingTimeIncrease:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.CraftingContinueTimeReduce);
                break;
            
            case (EBuffId)CustomBuffIds.LockpickingForgivenessAngle:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.HideoutExtraSlots);
                break;
            
            case (EBuffId)CustomBuffIds.LockpickingUseElite:
                ____icon.sprite = staticIcons.ItemAttributeSprites.GetValueOrDefault(EItemAttributeId.KeyUses);
                break;
            
            case (EBuffId)CustomBuffIds.SilentOpsIncMeleeSpeed:
                ____icon.sprite = staticIcons.DamageEffectSprites.GetValueOrDefault(EDamageEffectType.Contusion);
                break;
            
            case (EBuffId)CustomBuffIds.SilentOpsRedVolume:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.CovertMovementSoundVolume);
                break;
            
            case (EBuffId)CustomBuffIds.SilentOpsSilencerCostRed:
                ____icon.sprite = staticIcons.ItemAttributeSprites.GetValueOrDefault(EItemAttributeId.Loudness);
                break;
            
            case (EBuffId)CustomBuffIds.StrengthColliderSpeedBuff:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.StrengthBuffSprintSpeedInc);
                break;
        }
        
        __instance.UpdateBuff();
    }
}