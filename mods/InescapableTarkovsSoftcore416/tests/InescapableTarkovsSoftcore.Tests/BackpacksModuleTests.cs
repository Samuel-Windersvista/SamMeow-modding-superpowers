using InescapableTarkovsSoftcore.Config;
using InescapableTarkovsSoftcore.Features;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class BackpacksModuleTests
{
    private const string SixSh118Id = "5df8a4d786f77412672a1e3b";

    /// <summary>仅基础层（BASE）独有、旧包 IMM 终态移除的 9 条；v0.2.0 按旧包原版值补档，现已在查找表内。</summary>
    private static readonly string[] FormerlyBaseOnlyIds =
    [
        "656f198fb27298d6fd005466",
        "674da9cf0cb4bcde7103c07b",
        "674da107c512807d1a0e7436",
        "656e0436d44a1bb4220303a0",
        "67458730df3c1da90b0b052b",
        "5f5e46b96bdad616ad46d613",
        "5c0e805e86f774683f3dd637",
        "6034d2d697633951dc245ea6",
        "5d5d940f86f7742797262046"
    ];

    [Fact]
    public void Table_HasFiftySixBackpacks_WithPinnedAnchors()
    {
        var backpacks = FeatureTables.Backpacks;

        Assert.Equal(56, backpacks.Count);
        Assert.Equal(56, backpacks.Select(entry => entry.Id).Distinct().Count());
        var sixSh118 = backpacks.Single(entry => entry.Id == SixSh118Id);
        Assert.Equal(8, sixSh118.CellsH);
        Assert.Equal(9, sixSh118.CellsV);

        // v0.2.0 追加条目抽查锚点（旧包来源值 / 同类风格值，见工单 10 Comments 核查表）
        var terraframe = backpacks.Single(entry => entry.Id == "674da107c512807d1a0e7436");
        Assert.Equal(2, terraframe.CellsH);
        Assert.Equal(5, terraframe.CellsV);
        var paratus = backpacks.Single(entry => entry.Id == "5c0e805e86f774683f3dd637");
        Assert.Equal(5, paratus.CellsH);
        Assert.Equal(5, paratus.CellsV);
        var ttModular45P = backpacks.Single(entry => entry.Id == "68947a8ce4bf255d1b0ca759");
        Assert.Equal(5, ttModular45P.CellsH);
        Assert.Equal(8, ttModular45P.CellsV);
    }

    [Fact]
    public void Apply_6Sh118_SetsEightByNine_AndClearsFilters()
    {
        var table = TestTemplateTables.Create((SixSh118Id, TestItems.Backpack(6, 13, filters: [new GridFilter()])));

        var report = new BackpacksModule(new RecordingLogger<BackpacksModule>())
            .Apply(Context(table));

        var grid = table.Items[new MongoId(SixSh118Id)].Properties.Grids!.Single().Properties!;
        Assert.Equal(8, grid.CellsH);
        Assert.Equal(9, grid.CellsV);
        Assert.Empty(grid.Filters!);
        Assert.Equal(1, report.ChangedCount);
    }

    [Fact]
    public void Apply_FormerlyBaseOnlyBackpacks_AreNowAligned()
    {
        var table = TestTemplateTables.Create(
            FormerlyBaseOnlyIds.Select(id => (id, TestItems.Backpack(2, 2, filters: [new GridFilter()]))).ToArray());

        var report = new BackpacksModule(new RecordingLogger<BackpacksModule>())
            .Apply(Context(table));

        Assert.Equal(9, report.ChangedCount);
        // 表格仅含这 9 条，其余查找表条目缺席 → 告警；此处只关注这 9 条被对齐。
        Assert.Equal(FeatureTables.Backpacks.Count - FormerlyBaseOnlyIds.Length, report.Warnings.Count);
        foreach (var id in FormerlyBaseOnlyIds)
        {
            var entry = FeatureTables.Backpacks.Single(candidate => candidate.Id == id);
            var grid = table.Items[new MongoId(id)].Properties.Grids!.Single().Properties!;
            Assert.Equal(entry.CellsH, grid.CellsH);
            Assert.Equal(entry.CellsV, grid.CellsV);
            Assert.Empty(grid.Filters!);
        }
    }

    [Fact]
    public void Apply_MissingBackpack_WarnsAndSkips()
    {
        var table = TestTemplateTables.Create();
        var logger = new RecordingLogger<BackpacksModule>();

        var report = new BackpacksModule(logger).Apply(Context(table));

        Assert.Equal(0, report.ChangedCount);
        Assert.Equal(56, report.Warnings.Count);
        Assert.Contains(report.Warnings, warning => warning.Contains(SixSh118Id, StringComparison.Ordinal));
        Assert.Equal(56, logger.WarningMessages.Count);
    }

    [Fact]
    public void Apply_AllFiftySix_AreChanged()
    {
        var table = TestTemplateTables.Create(
            FeatureTables.Backpacks.Select(entry => (entry.Id, TestItems.Backpack(1, 1, filters: [new GridFilter()]))).ToArray());

        var report = new BackpacksModule(new RecordingLogger<BackpacksModule>())
            .Apply(Context(table));

        Assert.Equal(56, report.ChangedCount);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public void Disabled_LeavesTableUnchanged()
    {
        var table = TestTemplateTables.Create((SixSh118Id, TestItems.Backpack(6, 13, filters: [new GridFilter()])));
        var config = new SoftcoreConfig();
        config.Backpacks.Enabled = false;
        var orchestrator = new ModuleOrchestrator(
            [new BackpacksModule(new RecordingLogger<BackpacksModule>())],
            new RecordingLogger<ModuleOrchestrator>(),
            TestModTables.With(table));

        var report = orchestrator.Run(config);

        var grid = table.Items[new MongoId(SixSh118Id)].Properties.Grids!.Single().Properties!;
        Assert.Equal(6, grid.CellsH);
        Assert.Equal(13, grid.CellsV);
        Assert.NotEmpty(grid.Filters!);
        Assert.Equal(0, report.TotalChanged);
        Assert.Contains("backpacks", report.SkippedModuleIds);
    }

    private static ModContext Context(TemplateTable table, SoftcoreConfig? config = null) =>
        new(config ?? new SoftcoreConfig(), TestModTables.With(table));
}
