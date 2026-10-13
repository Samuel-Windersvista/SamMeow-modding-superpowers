using InescapableTarkovsSoftcore.Features.Softcore;
using InescapableTarkovsSoftcore.Features.Softcore.Changers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class SoftcoreEconomyTests
{
    [Fact]
    public void OtherFleaMarketChanges_OpenAtLevelOne_PristineItems_AndPriceIncrease()
    {
        var ragfair = SoftcoreTestData.NewRagfairConfig();
        ragfair.Dynamic.PriceRanges.Default.Min = 0.8;
        ragfair.Dynamic.PriceRanges.Default.Max = 1.2;
        ragfair.Dynamic.Condition[new MongoId("000000000000000000000101")] = new SPTarkov.Server.Core.Models.Spt.Config.Condition
        {
            ConditionChance = 1,
            Current = new MinMax<double>(),
            Max = new MinMax<double>()
        };
        var global = SoftcoreTestData.NewGlobalTable(0.25);
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), global: global, ragfair: ragfair);

        new EconomyOptionsChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(1d, global.Configuration.RagFair.MinUserLevel);
        Assert.Equal(0d, ragfair.Dynamic.Condition.Values.Single().ConditionChance);
        Assert.Equal(0.8 * 1.5, ragfair.Dynamic.PriceRanges.Default.Min);
        Assert.Equal(1.2 * 1.5, ragfair.Dynamic.PriceRanges.Default.Max);
    }

    [Theory]
    [InlineData(2.5, 3)] // AwayFromZero：中点向上取整（银行家舍入会得 2）
    [InlineData(2.4, 2)]
    [InlineData(2.6, 3)]
    [InlineData(1.0, 1)]
    public void FleaMarketOpenAtLevel_RoundsAwayFromZero(double level, int expected)
    {
        // 4.1.6 global RagFair.MinUserLevel 为 int；按 AwayFromZero 取整（D7 表 6）。
        var ragfair = SoftcoreTestData.NewRagfairConfig();
        var global = SoftcoreTestData.NewGlobalTable(0.25);
        var config = new Config.SoftcoreModuleConfig();
        config.EconomyOptions.OtherFleaMarketChanges.FleaMarketOpenAtLevel = level;
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), config, global: global, ragfair: ragfair);

        new EconomyOptionsChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(expected, global.Configuration.RagFair.MinUserLevel);
    }

    [Fact]
    public void BarterEconomy_AppliesSurvParameters()
    {
        var ragfair = SoftcoreTestData.NewRagfairConfig();
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), ragfair: ragfair);

        new EconomyOptionsChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(95d, ragfair.Dynamic.Barter.ChancePercent); // 100 - 5
        Assert.Equal(30d, ragfair.Dynamic.Barter.PriceRangeVariancePercent);
        Assert.Equal(4, ragfair.Dynamic.Barter.ItemCountMax);
        Assert.Equal(5, ragfair.Dynamic.OfferItemCount["default"].Min);
        Assert.Equal(13, ragfair.Dynamic.OfferItemCount["default"].Max);
        Assert.Equal(1, ragfair.Dynamic.NonStackableCount.Min);
        Assert.Equal(4, ragfair.Dynamic.NonStackableCount.Max);
        Assert.Equal(100d, ragfair.Dynamic.Barter.MinRoubleCostToBecomeBarter);
    }

    [Fact]
    public void PacifistWhitelist_EnablesItemAndMultipliesPrice()
    {
        var data = FleaMarketResourceLoader.Load();
        var whitelistId = data.Whitelist[0];

        var templates = SoftcoreTestData.NewTemplates(new SPTarkov.Server.Core.Models.Eft.Common.Tables.HandbookBase
        {
            Categories = [],
            Items = []
        });
        templates.Items[whitelistId] = SoftcoreTestData.NewTemplateItem(whitelistId, "00000000000000000000fa01");
        templates.Prices[whitelistId] = 100;
        var ragfair = SoftcoreTestData.NewRagfairConfig();
        ragfair.Dynamic.Blacklist.Custom.Add(whitelistId);
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), ragfair: ragfair);

        new EconomyOptionsChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(200d, templates.Prices[whitelistId]); // ×2
        Assert.True(templates.Items[whitelistId].Properties!.CanSellOnRagfair);
        Assert.DoesNotContain((MongoId)whitelistId, ragfair.Dynamic.Blacklist.Custom);
    }

    [Fact]
    public void Disabled_ProducesZeroChanges()
    {
        var ragfair = SoftcoreTestData.NewRagfairConfig();
        var config = new Config.SoftcoreModuleConfig();
        config.EconomyOptions.Enabled = false;
        var global = SoftcoreTestData.NewGlobalTable(0.25);
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), config, global: global, ragfair: ragfair);

        var log = new SoftcoreChangeLog();
        new EconomyOptionsChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Equal(0d, global.Configuration.RagFair.MinUserLevel);
        Assert.Equal(0d, ragfair.Dynamic.Barter.PriceRangeVariancePercent);
    }

    [Fact]
    public void OfferItemCount_ClearsAndCoversDefaultAndAmmoBox()
    {
        var ragfair = SoftcoreTestData.NewRagfairConfig();
        ragfair.Dynamic.OfferItemCount["543be5cb4bdc2deb348b4568"] = new MinMax<int> { Min = 1, Max = 1 };
        ragfair.Dynamic.OfferItemCount["other-parent"] = new MinMax<int> { Min = 1, Max = 1 };
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), ragfair: ragfair);

        new EconomyOptionsChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(2, ragfair.Dynamic.OfferItemCount.Count); // Clear 后仅 default + AmmoBox
        Assert.Equal(5, ragfair.Dynamic.OfferItemCount["default"].Min);
        Assert.Equal(13, ragfair.Dynamic.OfferItemCount["default"].Max);
        Assert.Equal(5, ragfair.Dynamic.OfferItemCount["543be5cb4bdc2deb348b4568"].Min);
        Assert.Equal(13, ragfair.Dynamic.OfferItemCount["543be5cb4bdc2deb348b4568"].Max);
        Assert.False(ragfair.Dynamic.OfferItemCount.ContainsKey("other-parent"));
    }

    [Fact]
    public void DisableFleaMarketCompletely_SetsLevel99_AndSkipsOthers()
    {
        var ragfair = SoftcoreTestData.NewRagfairConfig();
        var global = SoftcoreTestData.NewGlobalTable(0.25);
        var config = new Config.SoftcoreModuleConfig();
        config.EconomyOptions.DisableFleaMarketCompletely = true;
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), config, global: global, ragfair: ragfair);

        new EconomyOptionsChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(99d, global.Configuration.RagFair.MinUserLevel);
        Assert.Equal(0d, ragfair.Dynamic.Barter.PriceRangeVariancePercent); // 提前 return，barter 未改
    }

    [Fact]
    public void PriceRebalanceEnabled_WarnsButDoesNotThrow()
    {
        var ragfair = SoftcoreTestData.NewRagfairConfig();
        var config = new Config.SoftcoreModuleConfig();
        config.EconomyOptions.PriceRebalance.Enabled = true;
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), config, ragfair: ragfair);

        var log = new SoftcoreChangeLog { Changer = new EconomyOptionsChanger().Name };
        new EconomyOptionsChanger().Apply(context, log);

        Assert.Contains(log.Warnings, warning => warning.Contains("priceRebalance", StringComparison.Ordinal));
    }
}
