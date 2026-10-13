using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using InescapableTarkovsSoftcore.Config;
using InescapableTarkovsSoftcore.Features;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>
/// G7 战局时长倍率行为断言。
/// 字段核对结论：SPT 5.0.0 运行时的时限字段为 <c>LocationTable.&lt;map&gt;.Base.EscapeTimeLimit</c>
/// （<see cref="LocationBase.EscapeTimeLimit"/>，非空 <c>required double</c>）；服务器自身
/// <c>RaidTimeAdjustmentService.MakeAdjustmentsToMap</c> 亦写此字段。旧包 3.11 的字段名相同。
/// 模型 <c>required</c> 成员使 POCO 无法直接 new，夹具以 <see cref="RuntimeHelpers.GetUninitializedObject"/>
/// 构造 <see cref="LocationBase"/> 后仅写入时限。
/// </summary>
public class RaidDurationModuleTests
{
    [Fact]
    public void IdAndOrder_MatchSemanticOrder()
    {
        var module = new RaidDurationModule(new RecordingLogger<RaidDurationModule>());

        Assert.Equal("raidDuration", module.Id);
        Assert.Equal(700, module.Order);
    }

    [Fact]
    public void Apply_MultiplierTwo_DoublesEscapeTimeLimit_AcrossMaps()
    {
        var table = LocationTableWith(("bigmap", 40), ("woods", 45), ("shoreline", 60));

        var report = Apply(table, multiplier: 2.0);

        Assert.Equal(80, table.Bigmap.Base.EscapeTimeLimit);
        Assert.Equal(90, table.Woods.Base.EscapeTimeLimit);
        Assert.Equal(120, table.Shoreline.Base.EscapeTimeLimit);
        Assert.Equal(3, report.ChangedCount);
        Assert.Null(report.Error);
    }

    [Fact]
    public void Apply_MultiplierOne_LeavesEscapeTimeLimitUnchanged()
    {
        var table = LocationTableWith(("bigmap", 40), ("woods", 45));

        var report = Apply(table, multiplier: 1.0);

        Assert.Equal(40, table.Bigmap.Base.EscapeTimeLimit);
        Assert.Equal(45, table.Woods.Base.EscapeTimeLimit);
        Assert.Equal(0, report.ChangedCount);
    }

    [Fact]
    public void Apply_SkipsLocationsWithoutPositiveLimit()
    {
        var zeroLimit = LocationTableWith(("bigmap", 0));
        var nullBase = LocationTableWithBase(null);

        var zeroReport = Apply(zeroLimit, multiplier: 2.0);
        var nullBaseReport = Apply(nullBase, multiplier: 2.0);

        Assert.Equal(0, zeroLimit.Bigmap.Base.EscapeTimeLimit);
        Assert.Equal(0, zeroReport.ChangedCount);
        Assert.Equal(0, nullBaseReport.ChangedCount);
        Assert.Null(nullBaseReport.Error);
    }

    [Fact]
    public void Apply_NullEscapeTimeLimit_IsSkipped_AndStaysNull()
    {
        // 4.1.6 EscapeTimeLimit 为可空 double?：null → 不计数、不改写、无异常（D7 表 1/步骤 5-8）。
        var table = EmptyLocationTable();
        var locationBase = (LocationBase)RuntimeHelpers.GetUninitializedObject(typeof(LocationBase));
        SetLocation(table, "bigmap", new Location { Base = locationBase });

        var report = Apply(table, multiplier: 2.0);

        Assert.Null(table.Bigmap.Base.EscapeTimeLimit);
        Assert.Equal(0, report.ChangedCount);
        Assert.Null(report.Error);
    }

    [Fact]
    public void Apply_NonPositiveMultiplier_Warns_AndMakesNoChange()
    {
        var table = LocationTableWith(("bigmap", 40));
        var logger = new RecordingLogger<RaidDurationModule>();
        var module = new RaidDurationModule(logger);
        var config = new SoftcoreConfig();
        config.RaidDuration.Multiplier = 0;

        // 经基类公开入口 Apply(ModContext)：内部走 Section(context.Config) → 受保护的类型化 Apply。
        var report = module.Apply(new ModContext(config, Tables(table)));

        Assert.Equal(40, table.Bigmap.Base.EscapeTimeLimit);
        Assert.Equal(0, report.ChangedCount);
        Assert.NotEmpty(logger.WarningMessages);
    }

    [Fact]
    public void Disabled_ThroughOrchestrator_MakesNoChange_AndSkipsModule()
    {
        var table = LocationTableWith(("bigmap", 40));
        var config = new SoftcoreConfig();
        config.RaidDuration.Enabled = false;
        var orchestrator = new ModuleOrchestrator(
            [new RaidDurationModule(new RecordingLogger<RaidDurationModule>())],
            new RecordingLogger<ModuleOrchestrator>(),
            Tables(table));

        var report = orchestrator.Run(config);

        Assert.Equal(40, table.Bigmap.Base.EscapeTimeLimit);
        Assert.Empty(report.Modules);
        Assert.Contains("raidDuration", report.SkippedModuleIds);
    }

    private static ModuleReport Apply(LocationTable table, double multiplier)
    {
        var module = new RaidDurationModule(new RecordingLogger<RaidDurationModule>());
        var config = new SoftcoreConfig();
        config.RaidDuration.Multiplier = multiplier;
        return module.Apply(new ModContext(config, Tables(table)));
    }

    private static ModTables Tables(LocationTable table) => new(null!, null!, table, null!, null!, null!);

    private static LocationTable LocationTableWith(params (string JsonName, double Limit)[] maps)
    {
        var table = EmptyLocationTable();
        foreach (var (jsonName, limit) in maps)
        {
            SetLocation(table, jsonName, LocationWithLimit(limit));
        }

        return table;
    }

    private static LocationTable LocationTableWithBase(Location? location)
    {
        var table = EmptyLocationTable();
        SetLocation(table, "bigmap", location!);
        return table;
    }

    private static Location LocationWithLimit(double limit)
    {
        var locationBase = (LocationBase)RuntimeHelpers.GetUninitializedObject(typeof(LocationBase));
        typeof(LocationBase).GetProperty(nameof(LocationBase.EscapeTimeLimit))!.SetValue(locationBase, limit);
        return new Location { Base = locationBase };
    }

    private static void SetLocation(LocationTable table, string jsonName, Location? location)
    {
        // LocationTable 的 map 属性为 init-only（required init），编译期不可后赋；
        // 反射 SetValue 可越过该限制，用于在测试夹具中按 JSON 名装配地图。
        var property = typeof(LocationTable)
            .GetProperties()
            .Single(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == jsonName);
        property.SetValue(table, location);
    }

    private static LocationTable EmptyLocationTable() => new()
    {
        Bigmap = null!,
        Factory4Day = null!,
        Factory4Night = null!,
        Interchange = null!,
        Laboratory = null!,
        Lighthouse = null!,
        RezervBase = null!,
        Shoreline = null!,
        TarkovStreets = null!,
        Labyrinth = null!,
        Woods = null!,
        Sandbox = null!,
        SandboxHigh = null!,
        Base = new LocationsBase()
    };
}
