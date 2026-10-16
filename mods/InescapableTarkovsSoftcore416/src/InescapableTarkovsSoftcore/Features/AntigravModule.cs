using InescapableTarkovsSoftcore.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// G4 反重力臂章（顺序 300）。按查找表把各臂章的
/// <c>_props.Weight</c> 置为色阶减重值、<c>_props.StackMaxSize</c> 置为配置堆叠上限；
/// 配置 <c>overrides</c> 最后应用，可逐款覆盖重量。缺失臂章 id → 告警并跳过。
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class AntigravModule(ISptLogger<AntigravModule> logger) : FeatureModule<AntigravArmbandsConfig>
{
    public override string Id => "antigravArmbands";

    public override int Order => 300;

    protected override AntigravArmbandsConfig Section(SoftcoreConfig root) => root.AntigravArmbands;

    protected override ModuleReport Apply(ModContext context, AntigravArmbandsConfig config)
    {
        var items = context.Tables.TemplateTable.Items;
        var overrides = config.Overrides ?? new Dictionary<string, double>();
        var warnings = new List<string>();
        var overriddenIds = new HashSet<string>(StringComparer.Ordinal);
        var changed = 0;

        foreach (var entry in FeatureTables.Armbands)
        {
            var weight = entry.Weight;
            if (overrides.TryGetValue(entry.Id, out var overrideWeight))
            {
                weight = overrideWeight;
                overriddenIds.Add(entry.Id);
            }

            if (!items.TryGetValue(new MongoId(entry.Id), out var item))
            {
                ModuleWarnings.Add(logger, Id, warnings, $"未找到臂章 \"{entry.Name}\"（{entry.Id}），已跳过");
                continue;
            }

            if (item.Properties is not { } properties)
            {
                ModuleWarnings.Add(logger, Id, warnings, $"臂章 \"{entry.Name}\"（{entry.Id}）缺少 Properties，已跳过");
                continue;
            }

            properties.Weight = weight;
            properties.StackMaxSize = config.StackSize;
            changed++;
        }

        // overrides 的最后应用：允许指向查找表之外的其他臂章。
        foreach (var (id, weight) in overrides)
        {
            if (overriddenIds.Contains(id))
            {
                continue;
            }

            if (!items.TryGetValue(new MongoId(id), out var item))
            {
                ModuleWarnings.Add(logger, Id, warnings, $"overrides 未找到臂章 \"{id}\"，已跳过");
                continue;
            }

            if (item.Properties is not { } props)
            {
                ModuleWarnings.Add(logger, Id, warnings, $"overrides 臂章 \"{id}\" 缺少 Properties，已跳过");
                continue;
            }

            props.Weight = weight;
            changed++;
        }

        return ModuleReport.Ok(Id, changed, warnings);
    }
}
