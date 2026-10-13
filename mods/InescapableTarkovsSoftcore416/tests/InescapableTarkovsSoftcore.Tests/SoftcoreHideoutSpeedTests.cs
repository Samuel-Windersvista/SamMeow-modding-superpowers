using InescapableTarkovsSoftcore.Features.Softcore;
using InescapableTarkovsSoftcore.Features.Softcore.Changers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class SoftcoreHideoutSpeedTests
{
    [Fact]
    public void FasterHideoutConstruction_DividesStageTimeByFifty()
    {
        var context = NewContext(out var hideout);
        hideout.Areas!.Add(SoftcoreTestData.NewAreaWithStage(HideoutAreas.Generator, 1000));

        new FasterHideoutConstructionChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(20d, hideout.Areas[0].Stages!["1"].ConstructionTime);
    }

    [Fact]
    public void FuelConsumption_MultipliesFlowRateByFour()
    {
        var context = NewContext(out var hideout, SoftcoreTestData.NewHideoutSettings(generatorFuelFlowRate: 1.0));

        new FuelConsumptionChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(4.0, hideout.Settings!.GeneratorFuelFlowRate);
    }

    [Fact]
    public void FasterBitcoinFarming_DividesBitcoinTime_AndSetsGpuEfficiency()
    {
        var context = NewContext(out var hideout, SoftcoreTestData.NewHideoutSettings(gpuBoostRate: 0.5));
        hideout.Production!.Recipes!.Add(
            SoftcoreTestData.NewRecipe("0000000000000000000000c1", ItemTpl.BARTER_PHYSICAL_BITCOIN, 1300));

        new FasterBitcoinFarmingChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(1000d, hideout.Production.Recipes![0].ProductionTime);
        Assert.Equal(1.0, hideout.Settings!.GpuBoostRate);
    }

    [Fact]
    public void FasterBitcoinFarming_NullProductionTime_IsSkipped_AndNotCounted()
    {
        // 4.1.6 ProductionTime 为可空 double?：null 跳过、保持 null、不计数（D7 表 3 之比特币路径）。
        // 不注入 hideout.Settings，隔离 GPU 效率站点的独立计数。
        var context = NewContext(out var hideout);
        hideout.Production!.Recipes!.Add(
            SoftcoreTestData.NewRecipe("0000000000000000000000c2", ItemTpl.BARTER_PHYSICAL_BITCOIN, null));
        var log = new SoftcoreChangeLog();

        new FasterBitcoinFarmingChanger().Apply(context, log);

        Assert.Null(hideout.Production.Recipes![0].ProductionTime);
        Assert.Equal(0, log.ChangedCount);
    }

    [Fact]
    public void FasterHideoutConstruction_NullConstructionTime_IsSkipped_AndNotCounted()
    {
        // 4.1.6 ConstructionTime 为可空 double?：null 跳过、保持 null、不计数（D7 表 4）。
        var context = NewContext(out var hideout);
        hideout.Areas!.Add(SoftcoreTestData.NewAreaWithStage(HideoutAreas.Generator, null));
        var log = new SoftcoreChangeLog();

        new FasterHideoutConstructionChanger().Apply(context, log);

        Assert.Null(hideout.Areas[0].Stages!["1"].ConstructionTime);
        Assert.Equal(0, log.ChangedCount);
    }

    [Fact]
    public void Construction_InvalidMultiplier_WarnsAndUnchanged()
    {
        var context = NewContext(out var hideout);
        context.Config.FasterHideoutConstruction.HideoutConstructionTimeMultiplier = 0;
        hideout.Areas!.Add(SoftcoreTestData.NewAreaWithStage(HideoutAreas.Generator, 1000));

        var log = new SoftcoreChangeLog();
        new FasterHideoutConstructionChanger().Apply(context, log);

        Assert.Equal(1000d, hideout.Areas[0].Stages!["1"].ConstructionTime);
        Assert.Contains(log.Warnings, warning => warning.Contains("非法", StringComparison.Ordinal));
    }

    [Fact]
    public void Disabled_ProducesZeroChanges()
    {
        var context = NewContext(out var hideout, SoftcoreTestData.NewHideoutSettings(generatorFuelFlowRate: 1.0));
        context.Config.FuelConsumption.Enabled = false;

        var log = new SoftcoreChangeLog();
        new FuelConsumptionChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Equal(1.0, hideout.Settings!.GeneratorFuelFlowRate);
    }

    [Fact]
    public void Construction_UsesJsHalfUpRounding()
    {
        var context = NewContext(out var hideout);
        context.Config.FasterHideoutConstruction.HideoutConstructionTimeMultiplier = 2;
        hideout.Areas!.Add(SoftcoreTestData.NewAreaWithStage(HideoutAreas.Generator, 1));

        new FasterHideoutConstructionChanger().Apply(context, new SoftcoreChangeLog());

        // JS Math.round(0.5) = 1（C# 银行家舍入会得 0）。
        Assert.Equal(1d, hideout.Areas[0].Stages!["1"].ConstructionTime);
    }

    [Fact]
    public void SetBitcoinPriceTo100k_Enabled_RewritesHandbookPrice()
    {
        var templates = SoftcoreTestData.NewTemplates(new HandbookBase
        {
            Categories = [],
            Items = [SoftcoreTestData.NewHandbookItem(ItemTpl.BARTER_PHYSICAL_BITCOIN, 225000)]
        });
        var config = new Config.SoftcoreModuleConfig();
        config.FasterBitcoinFarming.SetBitcoinPriceTo100k = true;
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig(), config);

        new FasterBitcoinFarmingChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(
            100000d,
            templates.Handbook.Items.Single(item => (string)item.Id == ItemTpl.BARTER_PHYSICAL_BITCOIN).Price);
    }

    private static SoftcoreContext NewContext(out HideoutTable hideout, HideoutSettingsBase? settings = null)
    {
        hideout = SoftcoreTestData.NewHideout(settings);
        return SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), hideout, SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());
    }
}
