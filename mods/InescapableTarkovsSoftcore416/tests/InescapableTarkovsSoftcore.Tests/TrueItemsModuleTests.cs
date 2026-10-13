using InescapableTarkovsSoftcore.Config;
using InescapableTarkovsSoftcore.Features;
using InescapableTarkovsSoftcore.Features.TrueItems;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>
/// G2 True Items Redux 应用逻辑测试：List 精确 / ParentList 批量 / medicals 空容器 /
/// Active 开关 / 未命中告警 / overrides 覆盖。
/// </summary>
public class TrueItemsModuleTests
{
    private const string AaBattery = "5672cb124bdc2d1a0f8b4568";
    private const string PhysicalBitcoin = "59faff1d86f7746c51718c9c";
    private const string Ledx = "5c0530ee86f774697952d952";
    private const string Crickent = "56742c284bdc2d98058b456d";
    private const string Propital = "5c0e530286f7747fa1419862";
    private const string Sj1 = "5c0e531286f7747fa54205c2";
    private const string Sj6 = "5c0e531d86f7747fa23f4d42";
    private const string KeycardParent = "5c164d2286f774194c5e69fa";
    private const string KeycardChildA = "5c164d2286f774194c5e69fb";
    private const string KeycardChildB = "5c164d2286f774194c5e69fc";
    private const string KeycardChildC = "5c164d2286f774194c5e69fd";
    private const string UnrelatedItem = "111111111111111111111111";
    private const string UnmatchedId = "000000000000000000000000";
    private const string BarrelParent = "555ef6e44bdc2de9068b457e";

    [Fact]
    public void Module_Metadata_IsTrueItemsOrder500()
    {
        var module = new TrueItemsModule(new RecordingLogger<TrueItemsModule>());

        Assert.Equal("trueItems", module.Id);
        Assert.Equal(500, module.Order);
    }

    [Fact]
    public void Apply_Barter_ExactId_SetsStackMaxSizeAndStackMinRandom()
    {
        var table = TestItemTables.Items((AaBattery, "", 15, null));

        var report = TrueItemsApplier.Apply(
            Templates(barter: ActiveList(1, ListEntry(AaBattery, 5, "AA Battery"))),
            table,
            new TrueItemsConfig());

        Assert.Equal(1, report.ChangedCount);
        Assert.Empty(report.Warnings);
        Assert.Equal(5, TestItemTables.Props(table, AaBattery).StackMaxSize);
        Assert.Equal(1, TestItemTables.Props(table, AaBattery).StackMinRandom);
    }

    [Fact]
    public void Apply_Barter_RealTemplates_ProduceDeltaTableSampleValues()
    {
        var table = TestItemTables.Items(
            (AaBattery, "", 15, null),
            (PhysicalBitcoin, "", 20, null),
            (Ledx, "", 5, null),
            (Crickent, "", 12, null));

        var report = TrueItemsApplier.Apply(TrueItemsResourceLoader.Load(), table, new TrueItemsConfig());

        Assert.Equal(5, TestItemTables.Props(table, AaBattery).StackMaxSize);
        Assert.Equal(5, TestItemTables.Props(table, PhysicalBitcoin).StackMaxSize);
        Assert.Equal(3, TestItemTables.Props(table, Ledx).StackMaxSize);
        Assert.Equal(12, TestItemTables.Props(table, Crickent).StackMaxSize);
        Assert.True(report.ChangedCount >= 4);
    }

    [Fact]
    public void Apply_Medicals_MultipliesByStackMult_OnlyForEmptyContainers()
    {
        var table = TestItemTables.Items(
            (Propital, "", 1, 0),      // MaxHpResource = 0 → 空容器，生效
            (Sj1, "", 1, 100),         // MaxHpResource > 0 → 非空，跳过
            (Sj6, "", 1, null));       // MaxHpResource 未定义 → 跳过

        var report = TrueItemsApplier.Apply(
            Templates(medicals: ActiveList(2,
                ListEntry(Propital, 4, "Propital"),
                ListEntry(Sj1, 4, "SJ1"),
                ListEntry(Sj6, 4, "SJ6"))),
            table,
            new TrueItemsConfig());

        Assert.Equal(1, report.ChangedCount);
        Assert.Equal(8, TestItemTables.Props(table, Propital).StackMaxSize); // 4 × 2
        Assert.Equal(1, TestItemTables.Props(table, Sj1).StackMaxSize);
        Assert.Equal(1, TestItemTables.Props(table, Sj6).StackMaxSize);
    }

    [Fact]
    public void Apply_Keycards_ParentList_SetsAllChildrenToOne()
    {
        var table = TestItemTables.Items(
            (KeycardChildA, KeycardParent, 5, null),
            (KeycardChildB, KeycardParent, 5, null),
            (KeycardChildC, KeycardParent, 5, null),
            (UnrelatedItem, "", 7, null));

        var report = TrueItemsApplier.Apply(
            Templates(keycards: ActiveParents(1, ParentEntry(KeycardParent, 1, "Keycard"))),
            table,
            new TrueItemsConfig());

        Assert.Equal(3, report.ChangedCount);
        Assert.Equal(1, TestItemTables.Props(table, KeycardChildA).StackMaxSize);
        Assert.Equal(1, TestItemTables.Props(table, KeycardChildB).StackMaxSize);
        Assert.Equal(1, TestItemTables.Props(table, KeycardChildC).StackMaxSize);
        Assert.Equal(1, TestItemTables.Props(table, KeycardChildA).StackMinRandom);
        Assert.Equal(7, TestItemTables.Props(table, UnrelatedItem).StackMaxSize);
    }

    [Fact]
    public void Apply_PartsnMods_Inactive_LeavesChildrenUntouched()
    {
        var table = TestItemTables.Items((KeycardChildA, BarrelParent, 3, null));
        var partsnmods = ActiveParents(1, ParentEntry(BarrelParent, 2, "Barrel"));
        partsnmods.Active = false;

        var report = TrueItemsApplier.Apply(
            Templates(partsnmods: partsnmods),
            table,
            new TrueItemsConfig());

        Assert.Equal(0, report.ChangedCount);
        Assert.Empty(report.Warnings);
        Assert.Equal(3, TestItemTables.Props(table, KeycardChildA).StackMaxSize);
    }

    [Fact]
    public void Apply_UnmatchedId_WarnsAndSkips()
    {
        var table = TestItemTables.Items((UnrelatedItem, "", 7, null));

        var report = TrueItemsApplier.Apply(
            Templates(barter: ActiveList(1, ListEntry(UnmatchedId, 5, "Ghost"))),
            table,
            new TrueItemsConfig());

        Assert.Equal(0, report.ChangedCount);
        Assert.Contains(report.Warnings, warning => warning.Contains(UnmatchedId, StringComparison.Ordinal));
    }

    [Fact]
    public void Apply_ListEntryWithoutStackMaxSize_SkipsSilently()
    {
        // 条目值缺失（源 undefined）→ 跳过：不告警、不计数、目标表不变。
        var table = TestItemTables.Items((AaBattery, "", 15, null));
        var barter = ActiveList(1, new TrueItemsListEntry { Id = AaBattery, Name = "AA Battery", Props = new TrueItemsPropEntry() });

        var report = TrueItemsApplier.Apply(Templates(barter: barter), table, new TrueItemsConfig());

        Assert.Equal(0, report.ChangedCount);
        Assert.Empty(report.Warnings);
        Assert.Equal(15, TestItemTables.Props(table, AaBattery).StackMaxSize);
    }

    [Fact]
    public void Apply_ParentEntryWithoutStackMaxSize_SkipsSilently()
    {
        var table = TestItemTables.Items(
            (KeycardChildA, KeycardParent, 5, null),
            (KeycardChildB, KeycardParent, 5, null));
        var keycards = ActiveParents(1, new TrueItemsParentEntry { Id = KeycardParent, Name = "Keycard" });

        var report = TrueItemsApplier.Apply(Templates(keycards: keycards), table, new TrueItemsConfig());

        Assert.Equal(0, report.ChangedCount);
        Assert.Empty(report.Warnings);
        Assert.Equal(5, TestItemTables.Props(table, KeycardChildA).StackMaxSize);
        Assert.Equal(5, TestItemTables.Props(table, KeycardChildB).StackMaxSize);
    }

    [Fact]
    public void Apply_NullProperties_SkipsSilently()    {
        var table = TestItemTables.Items((AaBattery, "", 15, null));
        table.Items[new SPTarkov.Server.Core.Models.Common.MongoId(AaBattery)].Properties = null!;

        var report = TrueItemsApplier.Apply(
            Templates(barter: ActiveList(1, ListEntry(AaBattery, 5, "AA Battery"))),
            table,
            new TrueItemsConfig());

        Assert.Equal(0, report.ChangedCount);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public void Apply_Overrides_WinOverTemplate()
    {
        var table = TestItemTables.Items((AaBattery, "", 15, null));
        var config = new TrueItemsConfig();
        config.Overrides[AaBattery] = 42;

        var report = TrueItemsApplier.Apply(
            Templates(barter: ActiveList(1, ListEntry(AaBattery, 5, "AA Battery"))),
            table,
            config);

        Assert.Equal(42, TestItemTables.Props(table, AaBattery).StackMaxSize);
        Assert.Equal(1, TestItemTables.Props(table, AaBattery).StackMinRandom);
        Assert.Equal("trueItems", report.Id);
    }

    [Fact]
    public void Apply_Overrides_ByParentId_AppliesToAllChildren()
    {
        var table = TestItemTables.Items(
            (KeycardChildA, KeycardParent, 5, null),
            (KeycardChildB, KeycardParent, 5, null));
        var config = new TrueItemsConfig();
        config.Overrides[KeycardParent] = 7;

        TrueItemsApplier.Apply(Templates(), table, config);

        Assert.Equal(7, TestItemTables.Props(table, KeycardChildA).StackMaxSize);
        Assert.Equal(7, TestItemTables.Props(table, KeycardChildB).StackMaxSize);
    }

    [Fact]
    public void Apply_Overrides_Unmatched_Warns()
    {
        var table = TestItemTables.Items((AaBattery, "", 15, null));
        var config = new TrueItemsConfig();
        config.Overrides[UnmatchedId] = 9;

        var report = TrueItemsApplier.Apply(Templates(), table, config);

        Assert.Equal(0, report.ChangedCount);
        Assert.Contains(report.Warnings, warning => warning.Contains(UnmatchedId, StringComparison.Ordinal));
    }

    [Fact]
    public void Apply_RealTemplates_PlaceholderStimIds_DoNotMatch_AndWarn()
    {
        var table = TestItemTables.Items((Propital, "", 1, 0));

        var report = TrueItemsApplier.Apply(TrueItemsResourceLoader.Load(), table, new TrueItemsConfig());

        // 占位 _id（非 24 位 hex）按「匹配不到 = 不生效」复刻：不抛异常，仅告警。
        Assert.Contains(report.Warnings, warning => warning.Contains("cheeta_stim", StringComparison.Ordinal));
        Assert.Equal(8, TestItemTables.Props(table, Propital).StackMaxSize);
    }

    [Fact]
    public void Module_Apply_AppliesRealTemplates_AndLogsWarnings()
    {
        var logger = new RecordingLogger<TrueItemsModule>();
        var module = new TrueItemsModule(logger);
        var table = TestItemTables.Items((AaBattery, "", 15, null), (Propital, "", 1, 0));
        var context = new ModContext(new SoftcoreConfig(), new ModTables(table, null!, null!, null!, null!, null!));

        var report = module.Apply(context);

        Assert.Equal("trueItems", report.Id);
        Assert.Equal(2, report.ChangedCount);
        Assert.Equal(5, TestItemTables.Props(table, AaBattery).StackMaxSize);
        Assert.Equal(8, TestItemTables.Props(table, Propital).StackMaxSize);
        Assert.NotEmpty(logger.WarningMessages);
    }

    [Fact]
    public void Orchestrator_DisabledTrueItems_LeavesTableUntouched()
    {
        var table = TestItemTables.Items((AaBattery, "", 15, null));
        var config = new SoftcoreConfig();
        config.TrueItems.Enabled = false;
        var orchestrator = new ModuleOrchestrator(
            [new TrueItemsModule(new RecordingLogger<TrueItemsModule>())],
            new RecordingLogger<ModuleOrchestrator>(),
            new ModTables(table, null!, null!, null!, null!, null!));

        var report = orchestrator.Run(config);

        Assert.Equal(15, TestItemTables.Props(table, AaBattery).StackMaxSize);
        Assert.Contains("trueItems", report.SkippedModuleIds);
    }

    [Fact]
    public void Orchestrator_EnabledTrueItems_AppliesThroughModule()
    {
        var table = TestItemTables.Items((AaBattery, "", 15, null));
        var orchestrator = new ModuleOrchestrator(
            [new TrueItemsModule(new RecordingLogger<TrueItemsModule>())],
            new RecordingLogger<ModuleOrchestrator>(),
            new ModTables(table, null!, null!, null!, null!, null!));

        var report = orchestrator.Run(new SoftcoreConfig());

        Assert.Equal(5, TestItemTables.Props(table, AaBattery).StackMaxSize);
        Assert.Equal(1, report.TotalChanged);
    }

    private static TrueItemsTables Templates(
        TrueItemsTable? barter = null,
        TrueItemsTable? clothing = null,
        TrueItemsTable? keycards = null,
        TrueItemsTable? medicals = null,
        TrueItemsTable? partsnmods = null,
        TrueItemsTable? provisions = null) => new()
        {
            Barter = barter ?? new TrueItemsTable(),
            Clothing = clothing ?? new TrueItemsTable(),
            Keycards = keycards ?? new TrueItemsTable(),
            Medicals = medicals ?? new TrueItemsTable(),
            PartsnMods = partsnmods ?? new TrueItemsTable(),
            Provisions = provisions ?? new TrueItemsTable()
        };

    private static TrueItemsTable ActiveList(int mult, params TrueItemsListEntry[] entries) =>
        new() { Active = true, StackMult = mult, List = [.. entries] };

    private static TrueItemsTable ActiveParents(int mult, params TrueItemsParentEntry[] entries) =>
        new() { Active = true, StackMult = mult, ParentList = [.. entries] };

    private static TrueItemsListEntry ListEntry(string id, int stack, string name) =>
        new() { Id = id, Name = name, Props = new TrueItemsPropEntry { StackMaxSize = stack } };

    private static TrueItemsParentEntry ParentEntry(string id, int stack, string name) =>
        new() { Id = id, Name = name, StackMaxSize = stack };
}
