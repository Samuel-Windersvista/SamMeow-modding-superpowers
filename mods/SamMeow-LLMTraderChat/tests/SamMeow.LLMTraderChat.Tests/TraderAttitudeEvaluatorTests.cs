using SamMeow.LLMTraderChat.Server.Config;
using SamMeow.LLMTraderChat.Server.Engine;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>
/// 动态态度评测与渲染单测（工单 14）：5 档判定边界、玩家背景/任务进度渲染、反泄露禁令注入、
/// 自定义档位文案覆写、超预算截断。纯函数，不碰 SPT 运行时。
/// </summary>
public class TraderAttitudeEvaluatorTests
{
    [Fact]
    public void EvaluateTier_NullSnapshot_IsCold()
    {
        Assert.Equal(AttitudeTier.Cold, TraderAttitudeEvaluator.EvaluateTier(null));
    }

    [Theory]
    [InlineData(false, null, null, AttitudeTier.Hostile)] // 未解锁
    [InlineData(true, null, -0.5, AttitudeTier.Hostile)] // 好感度为负
    [InlineData(true, null, 0.2, AttitudeTier.Cold)] // 无忠诚等级 → 视为 1
    [InlineData(true, 1, 0.2, AttitudeTier.Cold)]
    [InlineData(true, 2, 0.2, AttitudeTier.Familiar)]
    [InlineData(true, 3, 0.2, AttitudeTier.Trusted)]
    [InlineData(true, 4, 0.2, AttitudeTier.Ally)]
    [InlineData(true, 5, 0.2, AttitudeTier.Ally)]
    public void EvaluateTier_Boundaries(bool unlocked, int? loyaltyLevel, double? standing, AttitudeTier expected)
    {
        var snapshot = new TraderRelationshipSnapshot(
            TraderUnlocked: unlocked, Standing: standing, LoyaltyLevel: loyaltyLevel);

        Assert.Equal(expected, TraderAttitudeEvaluator.EvaluateTier(snapshot));
    }

    [Theory]
    [InlineData(AttitudeTier.Hostile, "hostile")]
    [InlineData(AttitudeTier.Cold, "cold")]
    [InlineData(AttitudeTier.Familiar, "familiar")]
    [InlineData(AttitudeTier.Trusted, "trusted")]
    [InlineData(AttitudeTier.Ally, "ally")]
    public void GetTierConfigKey_ReturnsCanonicalKey(AttitudeTier tier, string expected)
    {
        Assert.Equal(expected, TraderAttitudeEvaluator.GetTierConfigKey(tier));
    }

    [Fact]
    public void FormatDiagnosticSummary_NullSnapshot_IsCompact()
    {
        var summary = TraderAttitudeEvaluator.FormatDiagnosticSummary(AttitudeTier.Cold, null);

        Assert.Equal("Cold(snapshot=null)", summary);
    }

    [Fact]
    public void FormatDiagnosticSummary_PopulatedSnapshot_CarriesSignals()
    {
        var snapshot = new TraderRelationshipSnapshot(
            TraderUnlocked: true,
            Standing: 0.2,
            LoyaltyLevel: 1,
            CompletedQuestCount: 2,
            ActiveQuests: [new ActiveQuestInfo("甲", false)]);

        var summary = TraderAttitudeEvaluator.FormatDiagnosticSummary(AttitudeTier.Cold, snapshot);

        Assert.Contains("Cold(", summary, StringComparison.Ordinal);
        Assert.Contains("unlocked=True", summary, StringComparison.Ordinal);
        Assert.Contains("ll=1", summary, StringComparison.Ordinal);
        Assert.Contains("standing=0.20", summary, StringComparison.Ordinal);
        Assert.Contains("done=2", summary, StringComparison.Ordinal);
        Assert.Contains("active=1", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderAttitudeBlock_NullSnapshot_UsesColdBuiltIn_NoQuestRecord_AndAntiLeak()
    {
        var block = TraderAttitudeEvaluator.RenderAttitudeBlock(null, new TraderConfig(), []);

        Assert.Contains("关系阶段：【冷淡/生疏】", block, StringComparison.Ordinal);
        Assert.Contains("委托记录：该玩家在你这里暂无任务委托记录。", block, StringComparison.Ordinal);
        Assert.Contains("表达禁令：严禁在回复中说出好感度数值", block, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderAttitudeBlock_FullSnapshot_RendersPlayerAndQuestProgress()
    {
        var snapshot = new TraderRelationshipSnapshot(
            PlayerLevel: 20,
            PlayerSide: "Bear",
            TraderUnlocked: true,
            Standing: 0.5,
            LoyaltyLevel: 2,
            CompletedQuestCount: 4,
            FailedQuestCount: 1,
            ActiveQuests:
            [
                new ActiveQuestInfo("急救训练", true),
                new ActiveQuestInfo("线下交易", false),
            ],
            RecentlyCompletedQuestNames: ["首次出勤"]);

        var block = TraderAttitudeEvaluator.RenderAttitudeBlock(snapshot, new TraderConfig(), []);

        Assert.Contains("关系阶段：【熟稔/认可】", block, StringComparison.Ordinal);
        Assert.Contains("玩家背景：BEAR 阵营佣兵（有一定经验的佣兵）", block, StringComparison.Ordinal);
        Assert.Contains("委托记录：已完成 4 项委托，失败 1 项。", block, StringComparison.Ordinal);
        Assert.Contains("《急救训练》（待交付）", block, StringComparison.Ordinal);
        Assert.Contains("《线下交易》（进行中）", block, StringComparison.Ordinal);
        Assert.Contains("近期刚完成：《首次出勤》。", block, StringComparison.Ordinal);
        Assert.Contains("表达禁令", block, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Usec", "USEC")]
    [InlineData("Bear", "BEAR")]
    [InlineData("Savage", "Savage")]
    public void RenderAttitudeBlock_PlayerSide_IsMappedOrRaw(string side, string expected)
    {
        var snapshot = new TraderRelationshipSnapshot(PlayerLevel: 40, PlayerSide: side);

        var block = TraderAttitudeEvaluator.RenderAttitudeBlock(snapshot, new TraderConfig(), []);

        Assert.Contains($"玩家背景：{expected} 阵营佣兵（身经百战的老手）", block, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderAttitudeBlock_TierOverride_ReplacesBuiltInPrompt()
    {
        var config = new TraderConfig();
        config.AttitudePrompts["trusted"] = "自定义信任文案";
        var snapshot = new TraderRelationshipSnapshot(LoyaltyLevel: 3);

        var block = TraderAttitudeEvaluator.RenderAttitudeBlock(snapshot, config, []);

        Assert.Contains("关系阶段：自定义信任文案", block, StringComparison.Ordinal);
        Assert.DoesNotContain("【信任/器重】", block, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderAttitudeBlock_OverBudget_TruncatesButKeepsAntiLeakDirective()
    {
        var config = new TraderConfig();
        config.AttitudePrompts["cold"] = new string('甲', 400);
        var longName = new string('乙', 60);
        var snapshot = new TraderRelationshipSnapshot(
            LoyaltyLevel: 1,
            ActiveQuests:
            [
                new ActiveQuestInfo(longName, false),
                new ActiveQuestInfo(longName, false),
                new ActiveQuestInfo(longName, false),
                new ActiveQuestInfo(longName, false),
                new ActiveQuestInfo(longName, false),
            ]);
        var warnings = new List<string>();

        var block = TraderAttitudeEvaluator.RenderAttitudeBlock(snapshot, config, warnings);

        Assert.True(
            block.Length <= TraderAttitudeEvaluator.MaxAttitudeTotalChars,
            $"态度块长度 {block.Length} 超出预算 {TraderAttitudeEvaluator.MaxAttitudeTotalChars}");
        Assert.Contains("表达禁令：严禁在回复中说出好感度数值", block, StringComparison.Ordinal);
        Assert.Contains(
            warnings,
            warning => warning.Contains("态度渲染截断", StringComparison.Ordinal)
                       && warning.Contains(
                           TraderAttitudeEvaluator.MaxAttitudeTotalChars.ToString(),
                           StringComparison.Ordinal));
    }
}
