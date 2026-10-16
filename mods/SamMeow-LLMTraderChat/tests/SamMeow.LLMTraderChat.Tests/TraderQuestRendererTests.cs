using SamMeow.LLMTraderChat.Server.Engine;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>
/// 任务知识渲染单测（工单 16 + 修订 R1/R3/R4）：披露门控映射、Hint/Full/None 渲染差异、预算截断、
/// 反编造禁令在所有路径恒存。纯函数，不碰 SPT 运行时。
/// </summary>
public class TraderQuestRendererTests
{
    [Theory]
    [InlineData(AttitudeTier.Hostile, DisclosureLevel.None)]
    [InlineData(AttitudeTier.Cold, DisclosureLevel.None)]
    [InlineData(AttitudeTier.Familiar, DisclosureLevel.Hint)]
    [InlineData(AttitudeTier.Trusted, DisclosureLevel.Full)]
    [InlineData(AttitudeTier.Ally, DisclosureLevel.Full)]
    public void FromAttitudeTier_MapsFiveTiersToThreeLevels(AttitudeTier tier, DisclosureLevel expected)
    {
        Assert.Equal(expected, DisclosurePolicy.FromAttitudeTier(tier));
    }

    [Fact]
    public void RenderQuestBlock_NoneLevel_EmitsAntiFabricationDirective_WithoutQuestDetails()
    {
        var detail = new TraderQuestDetail("水瓶座行动 - 1", ["在海关找到被藏在宿舍楼里的水"], "描述", "海关");

        var block = TraderQuestRenderer.RenderQuestBlock([detail], DisclosureLevel.None, []);

        Assert.Contains("严禁编造", block, StringComparison.Ordinal);
        Assert.Contains("不归我管", block, StringComparison.Ordinal);
        Assert.DoesNotContain("水瓶座行动", block, StringComparison.Ordinal);
        Assert.DoesNotContain("宿舍楼", block, StringComparison.Ordinal);
        Assert.DoesNotContain("当前进行中的委托", block, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderQuestBlock_NoQuests_EmitsAntiFabricationDirective()
    {
        var nullBlock = TraderQuestRenderer.RenderQuestBlock(null, DisclosureLevel.Full, []);
        var emptyBlock = TraderQuestRenderer.RenderQuestBlock([], DisclosureLevel.Full, []);

        Assert.Contains("严禁编造", nullBlock, StringComparison.Ordinal);
        Assert.Contains("不归我管", nullBlock, StringComparison.Ordinal);
        Assert.Contains("严禁编造", emptyBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderQuestBlock_HintLevel_ShowsNameAndRegion_ButNoObjectiveRoomOrKey()
    {
        var detail = new TraderQuestDetail(
            "水瓶座行动 - 1",
            ["在海关找到被藏在宿舍楼里的水", "以幸存状态撤离该区域"],
            "详细描述文本",
            "海关");

        var block = TraderQuestRenderer.RenderQuestBlock([detail], DisclosureLevel.Hint, []);

        Assert.Contains("《水瓶座行动 - 1》", block, StringComparison.Ordinal);
        Assert.Contains("（地区：海关）", block, StringComparison.Ordinal);
        // 目标原文 / 房间号 / 描述细节一律不得泄露。
        Assert.DoesNotContain("宿舍楼", block, StringComparison.Ordinal);
        Assert.DoesNotContain("在海关找到", block, StringComparison.Ordinal);
        Assert.DoesNotContain("详细描述文本", block, StringComparison.Ordinal);
        // 通用引导句（不含地点细节）。
        Assert.Contains("拿不准再来问我", block, StringComparison.Ordinal);
        Assert.Contains("严禁编造", block, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderQuestBlock_HintLevel_WithoutLocation_OmitsParenthetical()
    {
        var detail = new TraderQuestDetail("水瓶座行动 - 1", ["在海关找到被藏在宿舍楼里的水"], null, null);

        var block = TraderQuestRenderer.RenderQuestBlock([detail], DisclosureLevel.Hint, []);

        Assert.Contains("《水瓶座行动 - 1》", block, StringComparison.Ordinal);
        Assert.DoesNotContain("（地区", block, StringComparison.Ordinal);
        Assert.DoesNotContain("未知", block, StringComparison.Ordinal);
        Assert.Contains("拿不准再来问我", block, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderQuestBlock_FullLevel_IncludesObjectivesAndDescription()
    {
        var detail = new TraderQuestDetail(
            "水瓶座行动 - 1",
            ["在海关找到被藏在宿舍楼里的水", "以幸存状态撤离该区域"],
            "这是委托描述。",
            "海关");

        var block = TraderQuestRenderer.RenderQuestBlock([detail], DisclosureLevel.Full, []);

        Assert.Contains("地区：海关", block, StringComparison.Ordinal);
        Assert.Contains("- 在海关找到被藏在宿舍楼里的水", block, StringComparison.Ordinal);
        Assert.Contains("- 以幸存状态撤离该区域", block, StringComparison.Ordinal);
        Assert.Contains("说明：这是委托描述。", block, StringComparison.Ordinal);
        Assert.Contains("严禁编造", block, StringComparison.Ordinal);
        // Full 档不追加 Hint 通用引导。
        Assert.DoesNotContain("拿不准再来问我", block, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderQuestBlock_FullLevel_TruncatesDescriptionToBudget()
    {
        var longDescription = new string('描', 400);
        var detail = new TraderQuestDetail("委托", [], longDescription, null);

        var block = TraderQuestRenderer.RenderQuestBlock([detail], DisclosureLevel.Full, []);

        Assert.Contains("说明：", block, StringComparison.Ordinal);
        // 摘要恰被截断到 MaxDescriptionChars 字（其后即段收尾禁令）。
        Assert.Contains(new string('描', TraderQuestRenderer.MaxDescriptionChars), block, StringComparison.Ordinal);
        Assert.DoesNotContain(
            new string('描', TraderQuestRenderer.MaxDescriptionChars + 1), block, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderQuestBlock_SingleQuestOverItemBudget_TruncatesAndWarns_DirectiveSurvives()
    {
        var objectives = Enumerable.Range(0, 10).Select(_ => new string('目', 80)).ToList();
        var detail = new TraderQuestDetail("超长委托", objectives, null, null);
        var warnings = new List<string>();

        var block = TraderQuestRenderer.RenderQuestBlock([detail], DisclosureLevel.Full, warnings);

        Assert.Contains(warnings, warning => warning.Contains("单任务预算", StringComparison.Ordinal));
        Assert.Contains("严禁编造", block, StringComparison.Ordinal);
        Assert.True(block.Length <= TraderQuestRenderer.MaxQuestTotalChars);
    }

    [Fact]
    public void RenderQuestBlock_TotalOverBudget_DropsTailAndWarns_DirectiveSurvives()
    {
        var quests = Enumerable.Range(0, 5)
            .Select(index => new TraderQuestDetail($"任务{index}", [new string('标', 150)], null, null))
            .ToList();
        var warnings = new List<string>();

        var block = TraderQuestRenderer.RenderQuestBlock(quests, DisclosureLevel.Full, warnings);

        // 精确告警：字数预算导致，丢弃尾条（而非条数上限）。
        Assert.Contains(
            warnings,
            warning => warning.Contains("已丢弃", StringComparison.Ordinal)
                       && warning.Contains(TraderQuestRenderer.MaxQuestTotalChars.ToString(), StringComparison.Ordinal));
        Assert.True(block.Length <= TraderQuestRenderer.MaxQuestTotalChars);
        Assert.Contains("严禁编造", block, StringComparison.Ordinal);
        Assert.Contains("任务0", block, StringComparison.Ordinal);
        Assert.DoesNotContain("任务4", block, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderQuestBlock_SectionBudget_IsIndependentOfAttitudeBudget()
    {
        // 4 × 约 150 目标 → 段长超过态度段 600 字预算，但仍应在任务知识段 800 字预算内。
        var quests = Enumerable.Range(0, 4)
            .Select(index => new TraderQuestDetail($"T{index}", [new string('x', 150)], null, null))
            .ToList();

        var block = TraderQuestRenderer.RenderQuestBlock(quests, DisclosureLevel.Full, []);

        Assert.True(
            block.Length > TraderAttitudeEvaluator.MaxAttitudeTotalChars,
            $"任务知识段长度 {block.Length} 应超过态度段预算 {TraderAttitudeEvaluator.MaxAttitudeTotalChars}");
        Assert.True(block.Length <= TraderQuestRenderer.MaxQuestTotalChars);
    }

    [Fact]
    public void RenderQuestBlock_NoCountCap_AllShortQuestsRendered()
    {
        // 修订 R4：渲染层不设自身条数上限（条数上限由提供器单一负责）——短委托都应渲染且不告警。
        var quests = Enumerable.Range(0, 7)
            .Select(index => new TraderQuestDetail($"委托{index}", ["短目标"], null, null))
            .ToList();
        var warnings = new List<string>();

        var block = TraderQuestRenderer.RenderQuestBlock(quests, DisclosureLevel.Full, warnings);

        Assert.Empty(warnings);
        Assert.Contains("委托6", block, StringComparison.Ordinal);
    }
}
