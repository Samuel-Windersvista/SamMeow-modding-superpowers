using InescapableTarkovsSoftcore.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// G5 更大的背包（顺序 400）。按查找表把各背包的
/// <c>_props.Grids[0]._props.cellsH/cellsV</c> 置为目标尺寸，并清空容器过滤
/// （<c>filters = []</c>）。缺失背包 id / 无网格 → 告警并跳过。
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class BackpacksModule(ISptLogger<BackpacksModule> logger) : FeatureModule<BackpacksConfig>
{
    public override string Id => "backpacks";

    public override int Order => 400;

    protected override BackpacksConfig Section(SoftcoreConfig root) => root.Backpacks;

    protected override ModuleReport Apply(ModContext context, BackpacksConfig config)
    {
        var items = context.Tables.TemplateTable.Items;
        var warnings = new List<string>();
        var changed = 0;

        foreach (var entry in FeatureTables.Backpacks)
        {
            if (!items.TryGetValue(new MongoId(entry.Id), out var item))
            {
                ModuleWarnings.Add(logger, Id, warnings, $"未找到背包 \"{entry.Name}\"（{entry.Id}），已跳过");
                continue;
            }

            var gridProperties = item.Properties?.Grids?.FirstOrDefault()?.Properties;
            if (gridProperties is null)
            {
                ModuleWarnings.Add(logger, Id, warnings, $"背包 \"{entry.Name}\"（{entry.Id}）无网格，已跳过");
                continue;
            }

            gridProperties.CellsH = entry.CellsH;
            gridProperties.CellsV = entry.CellsV;
            gridProperties.Filters = [];
            changed++;
        }

        return ModuleReport.Ok(Id, changed, warnings);
    }
}
