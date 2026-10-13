using InescapableTarkovsSoftcore.Features.Softcore;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>T11：外置 crafting-rebalance.json / crafting-recipes.json 与源 TS 数据的一致性（计数 + 抽查）。</summary>
public class SoftcoreCraftingResourceTests
{
    [Fact]
    public void Resource_CountsMatchSource()
    {
        var rebalance = CraftingResourceLoader.LoadRebalance();
        var recipes = CraftingResourceLoader.LoadRecipes();

        Assert.Equal(47, rebalance.RecipeAdjustments.Count);
        Assert.Equal(12, recipes.AdditionalRecipes.Count);
    }

    [Fact]
    public void Resource_AdjustmentIds_AreUnique()
    {
        var rebalance = CraftingResourceLoader.LoadRebalance();

        Assert.Equal(
            rebalance.RecipeAdjustments.Count,
            rebalance.RecipeAdjustments.Select(a => a.Id).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Resource_AdditionalRecipeEndProducts_AreUnique()
    {
        var recipes = CraftingResourceLoader.LoadRecipes();

        Assert.Equal(
            recipes.AdditionalRecipes.Count,
            recipes.AdditionalRecipes.Select(r => (string)r.EndProduct).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Resource_PinnedRecipeIds_MatchDeltaTable()
    {
        var rebalance = CraftingResourceLoader.LoadRebalance();

        Assert.Equal(
            "61c77cc6fcc1673f08540e9b",
            rebalance.RecipeAdjustments.Single(a => a.Id == "60098b1705871270cd5352a1").RecipeId);
        Assert.Equal(
            "5dc1f4d9e078d303d91b44c7",
            rebalance.RecipeAdjustments.Single(a => a.Id == "5448fee04bdc2dbc018b4567").RecipeId);
        Assert.Equal(
            "5dd3c5a67da3785e63275437",
            rebalance.RecipeAdjustments.Single(a => a.Id == "5d6fc87386f77449db3db94e").RecipeId);
        // 590a3b04：5.0 首条与源一致，无需钉定。
        Assert.Null(rebalance.RecipeAdjustments.Single(a => a.Id == "590a3b0486f7743954552bdb").RecipeId);
        Assert.Equal(3, rebalance.RecipeAdjustments.Count(a => a.RecipeId is not null));
    }

    [Fact]
    public void Resource_SpotChecksMatchSource()
    {
        var rebalance = CraftingResourceLoader.LoadRebalance();
        var recipes = CraftingResourceLoader.LoadRecipes();

        // 1) 卫生纸：count = 1
        var toiletPaper = rebalance.RecipeAdjustments.Single(a => a.Id == "5c13cef886f774072e618e82");
        Assert.Equal("count", toiletPaper.Ops.Single().Op);
        Assert.Equal(1d, toiletPaper.Ops[0].Value);

        // 2) 军用闪存盘：count + replaceTemplate + setAreaLevel + setAllCounts（顺序）
        var flashDrive = rebalance.RecipeAdjustments.Single(a => a.Id == "62a0a16d0b9d3c46de5b6e97");
        Assert.Equal(new[] { "count", "replaceTemplate", "setAreaLevel", "setAllCounts" }, flashDrive.Ops.Select(o => o.Op));
        Assert.Equal("590c621186f774138d11ea29", flashDrive.Ops[1].From);
        Assert.Equal("5c05300686f7746dce784e5d", flashDrive.Ops[1].To);
        Assert.Equal(2d, flashDrive.Ops[2].Value);

        // 3) UHF RFID 读取器：整段替换（含 QuestComplete）
        var uhf = rebalance.RecipeAdjustments.Single(a => a.Id == "5c052fb986f7746b2101e909");
        var replacements = uhf.Ops.Single(o => o.Op == "replaceRequirements").Requirements!;
        Assert.Equal(6, replacements.Count);
        Assert.Equal("63966fccac6f8f3c677b9d89", (string)replacements[^1].QuestId!);

        // 4) 9x18 PM PSTM：pushRequirement（140 发）
        var pstm = rebalance.RecipeAdjustments.Single(a => a.Id == "57371aab2459775a77142f22");
        Assert.Equal("pushRequirement", pstm.Ops.Single().Op);
        Assert.Equal(140, pstm.Ops[0].Requirement!.Count);

        // 5) 3-b-TG 新配方：productionTime 31 / count 2 / 医疗站
        var threebTg = recipes.AdditionalRecipes.Single(r => (string)r.EndProduct == "5ed515c8d380ab312177c0fa");
        Assert.Equal(31d, threebTg.ProductionTime);
        Assert.Equal(2, threebTg.Count);
        Assert.Equal(7, (int)threebTg.AreaType);

        // 6) 肾上腺素：productionTime 23 / count 1
        var adrenaline = recipes.AdditionalRecipes.Single(r => (string)r.EndProduct == "5c10c8fd86f7743d7d706df3");
        Assert.Equal(23d, adrenaline.ProductionTime);
        Assert.Equal(1, adrenaline.Count);

        // 7) Obdolbos：count 8
        Assert.Equal(8, recipes.AdditionalRecipes.Single(r => (string)r.EndProduct == "5ed5166ad380ab312177c100").Count);

        // 8) OLOLO：营养站（areaType 8）
        Assert.Equal(8, (int)recipes.AdditionalRecipes.Single(r => (string)r.EndProduct == "62a0a043cf4a99369e2624a5").AreaType);

        // 9) Zagustin：count 3
        Assert.Equal(3, recipes.AdditionalRecipes.Single(r => (string)r.EndProduct == "5c0e533786f7747fa23f4d47").Count);
    }
}
