#nullable disable
using System;

namespace DoorBreach.Rules
{
    /// <summary>排除策略输出：是否受理、分类（调试面板可读）、拒绝原因。</summary>
    public sealed class ExclusionDecision
    {
        public bool Allowed;
        public ExclusionCategory Category;
        public string Reason = string.Empty;
    }

    /// <summary>
    /// 排除策略（纯函数）：门类 + 配置快照 → 破坏受理 / 分类 / 拒绝原因。
    /// 注册表与 AttackPolicy 共用本函数，避免两处判定漂移（工单 06 路线 A）。
    /// 物理不可行项（DoorClassKind.Excluded）永不可开关化；Fika 由注册表在环境层硬排除。
    /// </summary>
    public static class ExclusionPolicy
    {
        public static ExclusionDecision Evaluate(DoorClassKind doorClass, RuleConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            ExclusionCategory category = CategoryOf(doorClass);
            bool allowed;
            switch (category)
            {
                case ExclusionCategory.None:
                    allowed = true;
                    break;
                case ExclusionCategory.Safety:
                    allowed = config.AllowSafetyExclusions;
                    break;
                case ExclusionCategory.HighRisk:
                    allowed = config.AllowHighRiskExclusions;
                    break;
                case ExclusionCategory.Physical:
                    allowed = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(doorClass), doorClass, "unknown door class");
            }

            return new ExclusionDecision
            {
                Allowed = allowed,
                Category = category,
                Reason = allowed ? string.Empty : RefusalReason(doorClass),
            };
        }

        /// <summary>门类 → 排除分类（无需配置即可读取，调试面板与注册表共用）。</summary>
        public static ExclusionCategory CategoryOf(DoorClassKind doorClass)
        {
            switch (doorClass)
            {
                case DoorClassKind.Normal:
                    return ExclusionCategory.None;
                case DoorClassKind.Keycard:
                case DoorClassKind.Sliding:
                case DoorClassKind.DoorSwitch:
                case DoorClassKind.NonOperatable:
                case DoorClassKind.Double:
                    return ExclusionCategory.Safety;
                case DoorClassKind.Trigger:
                case DoorClassKind.Extraction:
                    return ExclusionCategory.HighRisk;
                case DoorClassKind.Excluded:
                    return ExclusionCategory.Physical;
                default:
                    throw new ArgumentOutOfRangeException(nameof(doorClass), doorClass, "unknown door class");
            }
        }

        /// <summary>拒绝原因带分类前缀（safety / high risk / physical），供日志、单测与调试面板逐类可读。</summary>
        private static string RefusalReason(DoorClassKind doorClass)
        {
            switch (doorClass)
            {
                case DoorClassKind.Keycard:
                    return "safety: keycard door";
                case DoorClassKind.Sliding:
                    return "safety: sliding door";
                case DoorClassKind.DoorSwitch:
                    return "safety: door switch";
                case DoorClassKind.NonOperatable:
                    return "safety: not operatable";
                case DoorClassKind.Double:
                    return "safety: possible double door";
                case DoorClassKind.Trigger:
                    return "high risk: fires map/quest triggers";
                case DoorClassKind.Extraction:
                    return "high risk: extraction door";
                case DoorClassKind.Excluded:
                    return "physical: not destructible";
                default:
                    throw new ArgumentOutOfRangeException(nameof(doorClass), doorClass, "unknown door class");
            }
        }
    }
}
