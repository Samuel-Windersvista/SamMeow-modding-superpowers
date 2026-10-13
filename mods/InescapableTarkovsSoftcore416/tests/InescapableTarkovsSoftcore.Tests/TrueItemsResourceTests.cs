using InescapableTarkovsSoftcore.Features.TrueItems;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>
/// G2 True Items Redux 物品堆叠：内嵌查找表资源加载测试。
/// 数据源 = 旧包 IMM 覆盖层（《生活在诺文斯克》物品堆叠-True Items沉浸感增强设置）。
/// </summary>
public class TrueItemsResourceTests
{
    [Fact]
    public void Load_ReturnsSixTables_WithExpectedEntryCountsAndFlags()
    {
        var tables = TrueItemsResourceLoader.Load();

        Assert.True(tables.Barter.Active);
        Assert.Equal(1, tables.Barter.StackMult);
        Assert.Equal(175, tables.Barter.List.Count);
        Assert.Empty(tables.Barter.ParentList);

        Assert.True(tables.Clothing.Active);
        Assert.Equal(1, tables.Clothing.StackMult);
        Assert.Equal(22, tables.Clothing.List.Count);

        Assert.True(tables.Keycards.Active);
        Assert.Equal(1, tables.Keycards.StackMult);
        Assert.Empty(tables.Keycards.List);
        Assert.Single(tables.Keycards.ParentList);

        Assert.True(tables.Medicals.Active);
        Assert.Equal(2, tables.Medicals.StackMult);
        Assert.Equal(43, tables.Medicals.List.Count);

        // 源文件 List 实为 104 项 + 10 父类；delta-table.md / 工单所记 107 为陈旧数字（见工单报告不确定项）。
        Assert.False(tables.PartsnMods.Active);
        Assert.Equal(1, tables.PartsnMods.StackMult);
        Assert.Equal(104, tables.PartsnMods.List.Count);
        Assert.Equal(10, tables.PartsnMods.ParentList.Count);

        Assert.True(tables.Provisions.Active);
        Assert.Equal(1, tables.Provisions.StackMult);
        Assert.Equal(19, tables.Provisions.List.Count);
    }

    [Fact]
    public void Load_BarterSamples_MatchDeltaTable()
    {
        var barter = TrueItemsResourceLoader.Load().Barter;

        Assert.Equal(5, ListValue(barter, "5672cb124bdc2d1a0f8b4568"));  // AA Battery
        Assert.Equal(5, ListValue(barter, "59faff1d86f7746c51718c9c"));  // Physical Bitcoin
        Assert.Equal(3, ListValue(barter, "5c0530ee86f774697952d952"));  // LEDX Skin Transilluminator
        Assert.Equal(12, ListValue(barter, "56742c284bdc2d98058b456d")); // Crickent lighter
    }

    [Fact]
    public void Load_MedicalsSyringeEntryValue_IsFour_AndStackMultIsTwo()
    {
        var medicals = TrueItemsResourceLoader.Load().Medicals;

        Assert.Equal(4, ListValue(medicals, "5c0e530286f7747fa1419862")); // Propital injector
        Assert.Equal(2, medicals.StackMult);
    }

    [Fact]
    public void Load_PartsnMods_SampleParentAndListEntries_ArePresent()
    {
        var partsnmods = TrueItemsResourceLoader.Load().PartsnMods;

        Assert.Contains(partsnmods.ParentList, entry => entry.Id == "555ef6e44bdc2de9068b457e"); // Barrel
        Assert.Contains(partsnmods.List, entry => entry.Id == "628120c21d5df4475f46a337");       // AI AXMC AT X Top Rail
    }

    private static int ListValue(TrueItemsTable table, string id) =>
        table.List.Single(entry => entry.Id == id).Props.StackMaxSize
        ?? throw new InvalidOperationException($"条目 {id} 缺少 StackMaxSize");
}
