using InescapableTarkovsSoftcore.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;

namespace InescapableTarkovsSoftcore.Features.TrueItems;

/// <summary>
/// G2 True Items Redux 物品真实堆叠（顺序 500）。由 SPT DI 自动聚合到
/// <see cref="ModuleOrchestrator"/>；<see cref="IsEnabled"/> 走 <c>trueItems.enabled</c>。
/// 查找表以内嵌资源形式随程序集分发，首次执行时惰性加载。
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class TrueItemsModule(ISptLogger<TrueItemsModule> logger) : FeatureModule<TrueItemsConfig>
{
    public const string ModuleId = "trueItems";

    public const int ModuleOrder = 500;

    private static readonly Lazy<TrueItemsTables> Tables = new(TrueItemsResourceLoader.Load);

    public override string Id => ModuleId;

    public override int Order => ModuleOrder;

    protected override TrueItemsConfig Section(SoftcoreConfig root) => root.TrueItems;

    protected override ModuleReport Apply(ModContext context, TrueItemsConfig config)
    {
        var report = TrueItemsApplier.Apply(Tables.Value, context.Tables.TemplateTable, config);

        foreach (var warning in report.Warnings)
        {
            logger.Warning(warning);
        }

        if (report.ChangedCount > 0)
        {
            logger.Info($"[ITS] {ModuleId}: 应用 {report.ChangedCount} 项堆叠变更");
        }

        return report;
    }
}
