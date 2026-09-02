using EFT;
using SkillsExtended.Models;

namespace SkillsExtended.Skills.Core;

public class SkillManagerExt
{
    private static SkillDataResponse SkillData => SkillsPlugin.SkillData;

    private static SkillManagerExt _playerInstance;
    private static SkillManagerExt _scavInstance;
    
    #region BUFFS

    public readonly SkillManager.FloatBuff FirstAidItemSpeedBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.FirstAidHealingSpeed,
    };
    
    public readonly SkillManager.FloatBuff FirstAidResourceCostBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.FirstAidResourceCost,
    };
    
    public readonly SkillManager.FloatBuff FirstAidMovementSpeedBuffElite = new()
    {
        Id = (EBuffId)CustomBuffIds.FirstAidMovementSpeedElite,
        BuffType = SkillManager.EBuffType.Elite
    };
    
    public readonly SkillManager.FloatBuff FieldMedicineSkillCap = new()
    {
        Id = (EBuffId)CustomBuffIds.FieldMedicineSkillCap,
    };
    
    public readonly SkillManager.FloatBuff FieldMedicineDurationBonus = new()
    {
        Id = (EBuffId)CustomBuffIds.FieldMedicineDurationBonus,
    };
    
    public readonly SkillManager.FloatBuff FieldMedicineChanceBonus = new()
    {
        Id = (EBuffId)CustomBuffIds.FieldMedicineChanceBonus,
    };
    
    public readonly SkillManager.FloatBuff UsecArSystemsErgoBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.UsecArSystemsErgo,
    };
    
    public readonly SkillManager.FloatBuff UsecArSystemsRecoilBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.UsecArSystemsRecoil,
    };
    
    public readonly SkillManager.FloatBuff BearAkSystemsErgoBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.BearAkSystemsErgo,
    };
    
    public readonly SkillManager.FloatBuff BearAkSystemsRecoilBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.BearAkSystemsRecoil,
    };
    
    public readonly SkillManager.FloatBuff LockPickingTimeBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.LockpickingTimeIncrease,
    };
    
    public readonly SkillManager.FloatBuff LockPickingForgiveness = new()
    {
        Id = (EBuffId)CustomBuffIds.LockpickingForgivenessAngle,
    };
    
    public readonly SkillManager.FloatBuff LockPickingUseBuffElite = new()
    {
        Id = (EBuffId)CustomBuffIds.LockpickingUseElite,
        BuffType = SkillManager.EBuffType.Elite
    };
    
    public readonly SkillManager.FloatBuff SilentOpsIncMeleeSpeedBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.SilentOpsIncMeleeSpeed,
    };
    
    public readonly SkillManager.FloatBuff SilentOpsReduceVolumeBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.SilentOpsRedVolume
    };
    
    public readonly SkillManager.FloatBuff SilentOpsSilencerCostRedBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.SilentOpsSilencerCostRed
    };
    
    public readonly SkillManager.FloatBuff StrengthBushSpeedIncBuff = new()
    {
        Id = (EBuffId)CustomBuffIds.StrengthColliderSpeedBuff
    };
    
    public readonly SkillManager.FloatBuff StrengthBushSpeedIncBuffElite = new()
    {
        Id = (EBuffId)CustomBuffIds.StrengthColliderSpeedBuffElite,
        BuffType = SkillManager.EBuffType.Elite
    };

    #endregion
    
    #region ACTIONS

    public readonly SkillManager.SkillAction FirstAidAction = new();
    public readonly SkillManager.SkillAction FieldMedicineAction = new();
    public readonly SkillManager.SkillAction UsecRifleAction = new();
    public readonly SkillManager.SkillAction BearRifleAction = new();
    public readonly SkillManager.SkillAction LockPickAction = new();
    public readonly SkillManager.SkillAction SilentOpsGunAction = new();
    public readonly SkillManager.SkillAction SilentOpsMeleeAction = new();

    #endregion
    
    /// <summary>
    /// Returns the SkillManagerExt instance for the respective side passed.
    /// For now for clarity always pass EPlayerSide.Usec for the PMC as
    /// there's no difference at the moment.
    /// The first time this is called it will also initialize the fields.
    /// </summary>
    /// <param name="playerSide">Side to get the SkillManagerExt for</param>
    /// <returns>SkillManagerExt</returns>
    public static SkillManagerExt Instance(EPlayerSide playerSide)
    {
        _playerInstance ??= new SkillManagerExt();
        _scavInstance ??= new SkillManagerExt();
        
        return playerSide == EPlayerSide.Savage ? _scavInstance : _playerInstance;
    }
    
    public SkillManager.FloatBuff[] FirstAidBuffs()
    {
        return
        [
            FirstAidItemSpeedBuff
                .Max(SkillData.FirstAid.ItemSpeedBonus)
                .Elite(SkillData.FirstAid.ItemSpeedBonusElite),

            FirstAidResourceCostBuff
                .Max(SkillData.FirstAid.MedkitUsageReduction)
                .Elite(SkillData.FirstAid.MedkitUsageReductionElite),
            
            FirstAidMovementSpeedBuffElite
        ];
    }
    
    public SkillManager.FloatBuff[] FieldMedicineBuffs()
    {
        return
        [
            FieldMedicineSkillCap
                .Max(SkillData.FieldMedicine.SkillBonus)
                .Elite(SkillData.FieldMedicine.SkillBonusElite),
            
            FieldMedicineDurationBonus
                .Max(SkillData.FieldMedicine.DurationBonus)
                .Elite(SkillData.FieldMedicine.DurationBonusElite),
            
            FieldMedicineChanceBonus
                .Max(SkillData.FieldMedicine.PositiveEffectChanceBonus)
                .Elite(SkillData.FieldMedicine.PositiveEffectChanceBonusElite)
        ];
    }
    
    public SkillManager.FloatBuff[] UsecArBuffs()
    {
        return
        [
            UsecArSystemsErgoBuff
                .Max(SkillData.NatoRifle.ErgoMod)
                .Elite(SkillData.NatoRifle.ErgoModElite),
            
            UsecArSystemsRecoilBuff
                .Max(SkillData.NatoRifle.RecoilReduction)
                .Elite(SkillData.NatoRifle.RecoilReductionElite)
        ];
    }
    
    public SkillManager.FloatBuff[] BearAkBuffs()
    {
        return
        [
            BearAkSystemsErgoBuff
                .Max(SkillData.EasternRifle.ErgoMod)
                .Elite(SkillData.EasternRifle.ErgoModElite),
            
            BearAkSystemsRecoilBuff
                .Max(SkillData.EasternRifle.RecoilReduction)
                .Elite(SkillData.EasternRifle.RecoilReductionElite)
        ];
    }
    
    public SkillManager.FloatBuff[] LockPickingBuffs()
    {
        return
        [
            LockPickingTimeBuff
                .PerLevel(SkillData.LockPicking.PickStrengthPerLevel),
            
            LockPickingForgiveness
                .PerLevel(SkillData.LockPicking.SweetSpotRangePerLevel),
            
            LockPickingUseBuffElite
        ];
    }
    
    public SkillManager.FloatBuff[] SilentOpsBuffs()
    {
        return
        [
            SilentOpsIncMeleeSpeedBuff
                .Max(SkillData.SilentOps.MeleeSpeedInc),
            
            SilentOpsReduceVolumeBuff
                .Max(SkillData.SilentOps.VolumeReduction),
            
            SilentOpsSilencerCostRedBuff
                .Max(SkillData.SilentOps.SilencerPriceReduction)
        ];
    }
}