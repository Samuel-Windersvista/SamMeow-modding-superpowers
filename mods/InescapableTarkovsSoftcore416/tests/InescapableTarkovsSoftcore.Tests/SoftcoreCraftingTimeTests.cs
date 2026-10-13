using InescapableTarkovsSoftcore.Features.Softcore;
using InescapableTarkovsSoftcore.Features.Softcore.Changers;
using SPTarkov.Server.Core.Models.Enums;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class SoftcoreCraftingTimeTests
{
    [Fact]
    public void GlobalMultiplier_DividesTime_ExcludingBitcoinMoonshineWater()
    {
        var context = NewContext(out var hideout);
        hideout.Production!.Recipes!.AddRange(
        [
            SoftcoreTestData.NewRecipe("0000000000000000000000a1", "0000000000000000000000e1", 1000),
            SoftcoreTestData.NewRecipe("0000000000000000000000a2", ItemTpl.BARTER_PHYSICAL_BITCOIN, 1000),
            SoftcoreTestData.NewRecipe("0000000000000000000000a3", ItemTpl.DRINK_BOTTLE_OF_FIERCE_HATCHLING_MOONSHINE, 1000),
            SoftcoreTestData.NewRecipe("0000000000000000000000a4", ItemTpl.DRINK_CANISTER_WITH_PURIFIED_WATER, 1000)
        ]);

        new FasterCraftingTimeChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(334d, hideout.Production.Recipes[0].ProductionTime);
        Assert.Equal(1000d, hideout.Production.Recipes[1].ProductionTime);
        Assert.Equal(Math.Ceiling(1000 / 0.3), hideout.Production.Recipes[2].ProductionTime);
        Assert.Equal(Math.Ceiling(1000 / 0.3), hideout.Production.Recipes[3].ProductionTime);
    }

    [Fact]
    public void GlobalMultiplier_NullProductionTime_IsSkipped_AndNotCounted()
    {
        // 4.1.6 ProductionTime 为可空 double?：null 跳过、保持 null、不计数（D7 表 3 之全局路径）。
        var context = NewContext(out var hideout);
        DisableOtherCraftingSubFeatures(context);
        hideout.Production!.Recipes!.Add(
            SoftcoreTestData.NewRecipe("0000000000000000000000c1", "0000000000000000000000e1", null));
        var log = new SoftcoreChangeLog();

        new FasterCraftingTimeChanger().Apply(context, log);

        Assert.Null(hideout.Production.Recipes[0].ProductionTime);
        Assert.Equal(0, log.ChangedCount);
    }

    [Fact]
    public void FasterMoonshineProduction_NullProductionTime_IsSkipped_AndNotCounted()
    {
        // 4.1.6 ProductionTime 为可空 double?：null 跳过、保持 null、不计数（D7 表 3 之单品路径）。
        var context = NewContext(out var hideout);
        DisableOtherCraftingSubFeatures(context);
        hideout.Production!.Recipes!.Add(SoftcoreTestData.NewRecipe(
            "0000000000000000000000c2", ItemTpl.DRINK_BOTTLE_OF_FIERCE_HATCHLING_MOONSHINE, null));
        var log = new SoftcoreChangeLog();

        new FasterCraftingTimeChanger().Apply(context, log);

        Assert.Null(hideout.Production.Recipes[0].ProductionTime);
        Assert.Equal(0, log.ChangedCount);
    }

    /// <summary>关闭会独立计数 log.Changed() 的其它制造子功能，隔离被测站点的计数。</summary>
    private static void DisableOtherCraftingSubFeatures(SoftcoreContext context)
    {
        context.Config.FasterCraftingTime.HideoutSkillExpFix.Enabled = false;
        context.Config.FasterCraftingTime.FasterPurifiedWaterProduction.Enabled = false;
        context.Config.FasterCraftingTime.FasterCultistCircle.Enabled = false;
    }

    [Fact]
    public void HideoutSkillExpFix_DividesHoursByTen()
    {
        var context = NewContext(out _);
        context.Services.HideoutConfig.HoursForSkillCrafting = 100;

        new FasterCraftingTimeChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(10, context.Services.HideoutConfig.HoursForSkillCrafting);
    }

    [Fact]
    public void CultistCircle_DividesRewardTimes()
    {
        var context = NewContext(out _);
        context.Services.HideoutConfig.CultistCircle!.HideoutTaskRewardTimeSeconds = 100;
        context.Services.HideoutConfig.CultistCircle.CraftTimeThresholds = [SoftcoreTestData.NewCraftTimeThreshold(100)];
        context.Services.HideoutConfig.CultistCircle.DirectRewards =
        [
            new SPTarkov.Server.Core.Models.Spt.Config.DirectRewardSettings
            {
                RequiredItems = [],
                Reward = [],
                CraftTimeSeconds = 100,
                Repeatable = false
            }
        ];

        new FasterCraftingTimeChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(200, context.Services.HideoutConfig.CultistCircle.HideoutTaskRewardTimeSeconds);
        Assert.Equal(200, context.Services.HideoutConfig.CultistCircle.CraftTimeThresholds[0].CraftTimeSeconds);
        Assert.Equal(200, context.Services.HideoutConfig.CultistCircle.DirectRewards[0].CraftTimeSeconds);
    }

    [Fact]
    public void InvalidMultiplier_WarnsAndLeavesRecipesUnchanged()
    {
        var context = NewContext(out var hideout);
        context.Config.FasterCraftingTime.BaseCraftingTimeMultiplier = 0;
        hideout.Production!.Recipes!.Add(SoftcoreTestData.NewRecipe("0000000000000000000000b1", "0000000000000000000000e2", 1000));

        var log = new SoftcoreChangeLog();
        new FasterCraftingTimeChanger().Apply(context, log);

        Assert.Equal(1000d, hideout.Production.Recipes[0].ProductionTime);
        Assert.Contains(log.Warnings, warning => warning.Contains("非法", StringComparison.Ordinal));
    }

    [Fact]
    public void Disabled_ProducesZeroChanges()
    {
        var context = NewContext(out var hideout);
        context.Config.FasterCraftingTime.Enabled = false;
        hideout.Production!.Recipes!.Add(SoftcoreTestData.NewRecipe("0000000000000000000000b2", "0000000000000000000000e3", 1000));

        var log = new SoftcoreChangeLog();
        new FasterCraftingTimeChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Equal(1000d, hideout.Production.Recipes[0].ProductionTime);
    }

    [Fact]
    public void HideoutSkillExpFix_ClampsToOne()
    {
        var context = NewContext(out _);
        context.Services.HideoutConfig.HoursForSkillCrafting = 10;
        context.Config.FasterCraftingTime.HideoutSkillExpFix.HideoutSkillExpMultiplier = 100000;

        new FasterCraftingTimeChanger().Apply(context, new SoftcoreChangeLog());

        // 10 / 100000 → 0；clamp ≥ 1 防止 core 除零。
        Assert.Equal(1, context.Services.HideoutConfig.HoursForSkillCrafting);
    }

    [Fact]
    public void Warning_UsesSoftcoreChangerPrefix()
    {
        var context = NewContext(out _);
        context.Config.FasterCraftingTime.BaseCraftingTimeMultiplier = 0;
        var log = new SoftcoreChangeLog { Changer = new FasterCraftingTimeChanger().Name };

        new FasterCraftingTimeChanger().Apply(context, log);

        Assert.Contains(
            log.Warnings,
            warning => warning.StartsWith("[ITS] softcore.fasterCraftingTime:", StringComparison.Ordinal));
    }

    private static SoftcoreContext NewContext(out SPTarkov.Server.Core.Models.Spt.Tables.HideoutTable hideout)
    {
        hideout = SoftcoreTestData.NewHideout();
        return SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), hideout, SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());
    }
}
