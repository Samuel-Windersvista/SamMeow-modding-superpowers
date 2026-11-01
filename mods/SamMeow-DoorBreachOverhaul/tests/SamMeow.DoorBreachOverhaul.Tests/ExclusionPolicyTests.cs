using System;
using System.Collections.Generic;
using DoorBreach.Rules;
using Xunit;

namespace SamMeow.DoorBreachOverhaul.Tests;

/// <summary>
/// 排除策略（ExclusionPolicy）直测：门类 × 两个开关 → 受理/分类/原因。
/// 与 AttackPolicy 表驱动互补：这里只测纯策略，不经过攻击输入映射。
/// </summary>
public class ExclusionPolicyTests
{
    [Theory]
    [InlineData(DoorClassKind.Normal, ExclusionCategory.None)]
    [InlineData(DoorClassKind.Keycard, ExclusionCategory.Safety)]
    [InlineData(DoorClassKind.Sliding, ExclusionCategory.Safety)]
    [InlineData(DoorClassKind.DoorSwitch, ExclusionCategory.Safety)]
    [InlineData(DoorClassKind.NonOperatable, ExclusionCategory.Safety)]
    [InlineData(DoorClassKind.Double, ExclusionCategory.Safety)]
    [InlineData(DoorClassKind.Trigger, ExclusionCategory.HighRisk)]
    [InlineData(DoorClassKind.Extraction, ExclusionCategory.HighRisk)]
    [InlineData(DoorClassKind.Excluded, ExclusionCategory.Physical)]
    public void CategoryOf_MapsEveryDoorClass(DoorClassKind doorClass, ExclusionCategory expected)
    {
        Assert.Equal(expected, ExclusionPolicy.CategoryOf(doorClass));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Evaluate_Table(
        string scenario,
        DoorClassKind doorClass,
        bool allowSafety,
        bool allowHighRisk,
        bool expectedAllowed,
        ExclusionCategory expectedCategory,
        string expectedReason)
    {
        var config = new RuleConfig
        {
            AllowSafetyExclusions = allowSafety,
            AllowHighRiskExclusions = allowHighRisk,
        };

        ExclusionDecision decision = ExclusionPolicy.Evaluate(doorClass, config);

        Assert.True(expectedAllowed == decision.Allowed, $"{scenario}: Allowed={decision.Allowed}, reason={decision.Reason}");
        Assert.Equal(expectedCategory, decision.Category);
        if (expectedReason.Length == 0)
        {
            Assert.Empty(decision.Reason);
        }
        else
        {
            Assert.True(
                decision.Reason.Contains(expectedReason, StringComparison.OrdinalIgnoreCase),
                $"{scenario}: reason='{decision.Reason}' 不含 '{expectedReason}'");
        }
    }

    [Fact]
    public void Evaluate_NullConfig_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ExclusionPolicy.Evaluate(DoorClassKind.Normal, null!));
    }

    [Fact]
    public void Evaluate_UnknownDoorClass_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExclusionPolicy.Evaluate((DoorClassKind)999, new RuleConfig()));
    }

    public static IEnumerable<object[]> Cases()
    {
        // 默认关闭态：安全类与高危组全部拒绝，物理不可行项拒绝。
        yield return Case("normal / all off", DoorClassKind.Normal, false, false, true, ExclusionCategory.None, "");
        yield return Case("keycard / all off", DoorClassKind.Keycard, false, false, false, ExclusionCategory.Safety, "safety");
        yield return Case("sliding / all off", DoorClassKind.Sliding, false, false, false, ExclusionCategory.Safety, "safety");
        yield return Case("switch / all off", DoorClassKind.DoorSwitch, false, false, false, ExclusionCategory.Safety, "safety");
        yield return Case("non-operatable / all off", DoorClassKind.NonOperatable, false, false, false, ExclusionCategory.Safety, "safety");
        yield return Case("double / all off", DoorClassKind.Double, false, false, false, ExclusionCategory.Safety, "safety");
        yield return Case("trigger / all off", DoorClassKind.Trigger, false, false, false, ExclusionCategory.HighRisk, "high risk");
        yield return Case("extraction / all off", DoorClassKind.Extraction, false, false, false, ExclusionCategory.HighRisk, "high risk");
        yield return Case("excluded / all off", DoorClassKind.Excluded, false, false, false, ExclusionCategory.Physical, "physical");

        // 仅安全类总开关：安全类受理，高危组与物理仍拒绝。
        yield return Case("normal / safety on", DoorClassKind.Normal, true, false, true, ExclusionCategory.None, "");
        yield return Case("keycard / safety on", DoorClassKind.Keycard, true, false, true, ExclusionCategory.Safety, "");
        yield return Case("sliding / safety on", DoorClassKind.Sliding, true, false, true, ExclusionCategory.Safety, "");
        yield return Case("switch / safety on", DoorClassKind.DoorSwitch, true, false, true, ExclusionCategory.Safety, "");
        yield return Case("non-operatable / safety on", DoorClassKind.NonOperatable, true, false, true, ExclusionCategory.Safety, "");
        yield return Case("double / safety on", DoorClassKind.Double, true, false, true, ExclusionCategory.Safety, "");
        yield return Case("trigger / safety on", DoorClassKind.Trigger, true, false, false, ExclusionCategory.HighRisk, "high risk");
        yield return Case("extraction / safety on", DoorClassKind.Extraction, true, false, false, ExclusionCategory.HighRisk, "high risk");
        yield return Case("excluded / safety on", DoorClassKind.Excluded, true, false, false, ExclusionCategory.Physical, "physical");

        // 仅高危组子开关：高危组受理，安全类与物理仍拒绝。
        yield return Case("normal / high risk on", DoorClassKind.Normal, false, true, true, ExclusionCategory.None, "");
        yield return Case("keycard / high risk on", DoorClassKind.Keycard, false, true, false, ExclusionCategory.Safety, "safety");
        yield return Case("trigger / high risk on", DoorClassKind.Trigger, false, true, true, ExclusionCategory.HighRisk, "");
        yield return Case("extraction / high risk on", DoorClassKind.Extraction, false, true, true, ExclusionCategory.HighRisk, "");
        yield return Case("excluded / high risk on", DoorClassKind.Excluded, false, true, false, ExclusionCategory.Physical, "physical");

        // 两者都开：非物理项全部受理，物理不可行项仍拒绝。
        yield return Case("keycard / both on", DoorClassKind.Keycard, true, true, true, ExclusionCategory.Safety, "");
        yield return Case("double / both on", DoorClassKind.Double, true, true, true, ExclusionCategory.Safety, "");
        yield return Case("trigger / both on", DoorClassKind.Trigger, true, true, true, ExclusionCategory.HighRisk, "");
        yield return Case("extraction / both on", DoorClassKind.Extraction, true, true, true, ExclusionCategory.HighRisk, "");
        yield return Case("excluded / both on", DoorClassKind.Excluded, true, true, false, ExclusionCategory.Physical, "physical");
    }

    private static object[] Case(
        string scenario,
        DoorClassKind doorClass,
        bool allowSafety,
        bool allowHighRisk,
        bool expectedAllowed,
        ExclusionCategory expectedCategory,
        string expectedReason)
    {
        return new object[]
        {
            scenario, doorClass, allowSafety, allowHighRisk, expectedAllowed, expectedCategory, expectedReason,
        };
    }
}
