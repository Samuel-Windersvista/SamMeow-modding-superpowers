using InescapableTarkovsSoftcore.Config;
using InescapableTarkovsSoftcore.Features;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>
/// G3 藏身处免 FIR 的行为断言。
/// 注意：本工程编译针对真实 SPT 5.0.0 运行时程序集，其模型以 <c>required</c> 成员约束构造
/// （与 SP-Tushonka 源码快照的修饰符不一致），故夹具需显式填充全部 required 成员。
/// </summary>
public class NoFirHideoutModuleTests
{
    [Fact]
    public void IdAndOrder_MatchSemanticOrder()
    {
        var module = new NoFirHideoutModule(new RecordingLogger<NoFirHideoutModule>());

        Assert.Equal("noFirHideout", module.Id);
        Assert.Equal(600, module.Order);
    }

    [Fact]
    public void Apply_SetsIsSpawnedInSessionFalse_WhereKeyIsTrue()
    {
        var withTrueKey = new StageRequirement { Type = "Item", IsSpawnedInSession = true };
        var withFalseKey = new StageRequirement { Type = "Item", IsSpawnedInSession = false };
        var withoutKey = new StageRequirement { Type = "Item" };
        var improvementWithTrueKey = new StageImprovementRequirement
        {
            Type = "Item",
            Count = 1,
            IsEncoded = false,
            IsFunctional = false,
            IsSpawnedInSession = true
        };
        var table = HideoutTableWith(StageWith(
            [withTrueKey, withFalseKey, withoutKey],
            [ImprovementWith([improvementWithTrueKey])]));

        var report = Apply(table);

        Assert.False(withTrueKey.IsSpawnedInSession);
        Assert.False(withFalseKey.IsSpawnedInSession);
        Assert.Null(withoutKey.IsSpawnedInSession);
        Assert.False(improvementWithTrueKey.IsSpawnedInSession);
        Assert.Equal(2, report.ChangedCount);
        Assert.Null(report.Error);
    }

    [Fact]
    public void Apply_RequirementWithoutKey_IsNotTouched()
    {
        var withoutKey = new StageRequirement { Type = "Item" };
        var table = HideoutTableWith(StageWith([withoutKey]));

        var report = Apply(table);

        Assert.Null(withoutKey.IsSpawnedInSession);
        Assert.Equal(0, report.ChangedCount);
    }

    [Fact]
    public void Apply_NullIsSpawnedInSession_Unchanged_AndTrueBecomesFalse()
    {
        // 4.1.6 IsSpawnedInSession 为可空 bool?：null 不动作；true → false 并计数（D7 表 7）。
        var nullKey = new StageRequirement { Type = "Item" };
        var trueKey = new StageRequirement { Type = "Item", IsSpawnedInSession = true };
        var table = HideoutTableWith(StageWith([nullKey, trueKey]));

        var report = Apply(table);

        Assert.Null(nullKey.IsSpawnedInSession);
        Assert.False(trueKey.IsSpawnedInSession);
        Assert.Equal(1, report.ChangedCount);
    }

    [Fact]
    public void Apply_EmptyHideout_YieldsZeroChanges()
    {
        var report = Apply(HideoutTableWith());

        Assert.Equal(0, report.ChangedCount);
        Assert.Null(report.Error);
    }

    [Fact]
    public void Disabled_ThroughOrchestrator_MakesNoChange_AndSkipsModule()
    {
        var withTrueKey = new StageRequirement { Type = "Item", IsSpawnedInSession = true };
        var table = HideoutTableWith(StageWith([withTrueKey]));
        var config = new SoftcoreConfig();
        config.NoFirHideout.Enabled = false;
        var orchestrator = new ModuleOrchestrator(
            [new NoFirHideoutModule(new RecordingLogger<NoFirHideoutModule>())],
            new RecordingLogger<ModuleOrchestrator>(),
            Tables(table));

        var report = orchestrator.Run(config);

        Assert.True(withTrueKey.IsSpawnedInSession);
        Assert.Empty(report.Modules);
        Assert.Contains("noFirHideout", report.SkippedModuleIds);
    }

    private static ModuleReport Apply(HideoutTable table)
    {
        var module = new NoFirHideoutModule(new RecordingLogger<NoFirHideoutModule>());
        return module.Apply(new ModContext(new SoftcoreConfig(), Tables(table)));
    }

    private static ModTables Tables(HideoutTable table) => new(null!, table, null!, null!, null!, null!);

    private static Stage StageWith(
        List<StageRequirement> requirements,
        List<StageImprovement>? improvements = null) => new()
    {
        AutoUpgrade = false,
        Bonuses = [],
        ConstructionTime = 0,
        Container = default,
        Description = string.Empty,
        DisplayInterface = false,
        Improvements = improvements ?? [],
        Requirements = requirements,
        Slots = 0
    };

    private static StageImprovement ImprovementWith(List<StageImprovementRequirement> requirements) => new()
    {
        Bonuses = [],
        ImprovementTime = 0,
        Requirements = requirements
    };

    private static HideoutTable HideoutTableWith(params Stage[] stages) => new()
    {
        Areas =
        [
            new HideoutArea
            {
                Type = default,
                IsEnabled = false,
                NeedsFuel = false,
                Requirements = [],
                IsTakeFromSlotLocked = false,
                CraftGivesExperience = false,
                DisplayLevel = false,
                EnableAreaRequirements = false,
                Stages = stages
                    .Select((stage, index) => (Key: index.ToString(), Stage: stage))
                    .ToDictionary(pair => pair.Key, pair => pair.Stage)
            }
        ],
        CustomAreas = null,
        Customisation = new HideoutCustomisation { Globals = [], Slots = [] },
        Production = new HideoutProductionData { Recipes = [], ScavRecipes = [], CultistRecipes = [] },
        Settings = new HideoutSettingsBase(),
        Qte = []
    };
}
