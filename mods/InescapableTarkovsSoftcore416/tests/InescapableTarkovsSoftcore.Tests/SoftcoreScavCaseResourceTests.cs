using InescapableTarkovsSoftcore.Features.Softcore;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>F1：外置 ScavCase 数据与源 TS（assets/scavcase.ts）逐项一致性的计数 + 抽查断言。</summary>
public class SoftcoreScavCaseResourceTests
{
    [Fact]
    public void Resource_CountsMatchSource()
    {
        var data = ScavCaseResourceLoader.Load();

        Assert.Equal(3, data.RewardItemValueRangeRub.Count);
        Assert.Equal(6, data.ParentBlacklist.Count);
        Assert.Equal(53, data.ItemBlacklist.Count);
        Assert.Equal(46, data.Whitelist.Count);
        Assert.Equal(5, data.Recipes.Count);
    }

    [Fact]
    public void Resource_SpotChecksMatchSource()
    {
        var data = ScavCaseResourceLoader.Load();

        // 价值区间
        Assert.Equal(1, data.RewardItemValueRangeRub["common"].Min);
        Assert.Equal(20000, data.RewardItemValueRangeRub["common"].Max);
        Assert.Equal(20001, data.RewardItemValueRangeRub["rare"].Min);
        Assert.Equal(60000, data.RewardItemValueRangeRub["rare"].Max);
        Assert.Equal(60001, data.RewardItemValueRangeRub["superrare"].Min);
        Assert.Equal(1200000, data.RewardItemValueRangeRub["superrare"].Max);

        // 父类黑名单（首/末）
        Assert.Equal("5485a8684bdc2da71d8b4567", data.ParentBlacklist[0]);
        Assert.Equal("65649eb40bf0ed77b8044453", data.ParentBlacklist[^1]);

        // 物品黑名单（首/末）
        Assert.Equal("660bbc47c38b837877075e47", data.ItemBlacklist[0]);
        Assert.Equal("5df8a77486f77412672a1e3f", data.ItemBlacklist[^1]);

        // 父类白名单（首/末）
        Assert.Equal("5447b5f14bdc2d61278b4567", data.Whitelist[0]);
        Assert.Equal("644120aa86ffbe10ee032b6f", data.Whitelist[^1]);

        // 配方（首/末）
        Assert.Equal("62710974e71632321e5afd5f", data.Recipes[0].Id);
        Assert.Equal("62a09f32621468534a797acb", data.Recipes[0].RequiredItem);
        Assert.Equal(2500, data.Recipes[0].ProductionTime);
        Assert.Equal(3, data.Recipes[0].Common.Min);
        Assert.Equal(3, data.Recipes[0].Common.Max);
        Assert.Equal("62710a0e436dcc0b9c55f4ec", data.Recipes[4].Id);
        Assert.Equal("5c12613b86f7743bbe2c3f76", data.Recipes[4].RequiredItem);
        Assert.Equal(19200, data.Recipes[4].ProductionTime);
        Assert.Equal(3, data.Recipes[4].Rare.Min);
        Assert.Equal(5, data.Recipes[4].Rare.Max);
        Assert.Equal(1, data.Recipes[4].Superrare.Max);
    }
}
