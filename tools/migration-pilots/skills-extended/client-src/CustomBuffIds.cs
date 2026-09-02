// SkillsExtended 自定义 EBuffId 常量（对齐 3.11 Prepatcher 的 1000+ 区间）
// 4.1 的 EBuffId 是游戏原生枚举，mod 自定义成员通过数值强转引用
namespace SkillsExtended;

public static class CustomBuffIds
{
    // 3.11 Prepatcher 注入顺序（从 1000 开始）
    public const int FirstAidHealingSpeed = 1000;
    public const int FirstAidResourceCost = 1001;
    public const int FirstAidMovementSpeedElite = 1002;
    public const int FieldMedicineSkillCap = 1003;
    public const int FieldMedicineDurationBonus = 1004;
    public const int FieldMedicineChanceBonus = 1005;
    public const int UsecArSystemsRecoil = 1006;
    public const int UsecArSystemsErgo = 1007;
    public const int BearAkSystemsRecoil = 1008;
    public const int BearAkSystemsErgo = 1009;
    public const int LockpickingTimeIncrease = 1010;
    public const int LockpickingForgivenessAngle = 1011;
    public const int LockpickingUseElite = 1012;
    public const int SilentOpsIncMeleeSpeed = 1013;
    public const int SilentOpsRedVolume = 1014;
    public const int SilentOpsSilencerCostRed = 1015;
    public const int StrengthColliderSpeedBuff = 1016;
    public const int StrengthColliderSpeedBuffElite = 1017;
}
