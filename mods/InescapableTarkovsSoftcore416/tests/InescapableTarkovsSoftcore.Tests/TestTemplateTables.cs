using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>
/// 构造真实 <see cref="TemplateTable"/> 的测试夹具。SPT 5 的表模型把每个属性标为
/// required + init，故此处显式置空全部必需成员，只填充 <see cref="TemplateTable.Items"/>。
/// </summary>
internal static class TestTemplateTables
{
    public static TemplateTable Create(params (string Id, TemplateItem Item)[] items)
    {
        var table = new TemplateTable
        {
            Character = null!,
            CustomisationStorage = null!,
            Items = new Dictionary<MongoId, TemplateItem>(),
            Prestige = null!,
            Quests = null!,
            RepeatableQuests = null!,
            Handbook = null!,
            Customization = null!,
            Dialogue = null!,
            Profiles = null!,
            Prices = null!,
            DefaultEquipmentPresets = null!,
            Achievements = null!,
            CustomAchievements = null!,
            LocationServices = null!
        };

        foreach (var (id, item) in items)
        {
            table.Items[new MongoId(id)] = item;
        }

        return table;
    }
}

/// <summary>合成物品构造器：只搭出被测模块读取的属性。</summary>
internal static class TestItems
{
    public static TemplateItem Armband(double weight, int stackMaxSize) => new()
    {
        Properties = new TemplateItemProperties
        {
            Weight = weight,
            StackMaxSize = stackMaxSize
        }
    };

    public static TemplateItem Backpack(int cellsH, int cellsV, IEnumerable<GridFilter>? filters = null) => new()
    {
        Properties = new TemplateItemProperties
        {
            Grids =
            [
                new Grid
                {
                    Properties = new GridProperties
                    {
                        CellsH = cellsH,
                        CellsV = cellsV,
                        Filters = filters ?? Array.Empty<GridFilter>()
                    }
                }
            ]
        }
    };
}
