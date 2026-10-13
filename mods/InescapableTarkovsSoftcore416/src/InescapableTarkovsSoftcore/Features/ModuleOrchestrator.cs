using InescapableTarkovsSoftcore.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// 功能组编排器：按各模块 <see cref="IFeatureModule.Order"/> 升序执行，隔离单模块异常，
/// 逐模块输出 summary 行，末尾输出合计（前缀 [ITS]）。执行顺序语义（D5）：
/// softcore=100 → samuelTweaks=200 → antigravArmbands=300 → backpacks=400 →
/// trueItems=500 → noFirHideout=600 → raidDuration=700。
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class ModuleOrchestrator(
    IEnumerable<IFeatureModule> modules,
    ISptLogger<ModuleOrchestrator> logger,
    ModTables tables)
{
    /// <summary>执行一轮编排。general.enabled=false 时全部跳过。</summary>
    public OrchestrationReport Run(SoftcoreConfig config)
    {
        if (!config.General.Enabled)
        {
            logger.Info("[ITS] general.enabled=false，跳过全部功能模块");
            return new OrchestrationReport([], [], true, 0, 0, 0);
        }

        // 按注册顺序去重（保留先注册者）；Dictionary 枚举序无契约保证，故用 List 承载后再稳定排序。
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var registered = new List<IFeatureModule>();
        foreach (var module in modules)
        {
            if (module is null)
            {
                continue;
            }

            if (!seenIds.Add(module.Id))
            {
                logger.Warning($"[ITS] 检测到重复模块 id \"{module.Id}\"，保留先注册者，忽略后者");
                continue;
            }

            registered.Add(module);
        }

        var context = new ModContext(config, tables);
        var reports = new List<ModuleReport>();
        var skipped = new List<string>();

        // LINQ OrderBy 为稳定排序：同 Order 值保持注册顺序。
        foreach (var module in registered.OrderBy(module => module.Order))
        {
            try
            {
                // IsEnabled 亦置于异常隔离内：模块的配置段解析异常只影响本组（D15）。
                if (!module.IsEnabled(config))
                {
                    skipped.Add(module.Id);
                    logger.Info($"[ITS] {module.Id} 已在配置中禁用，跳过");
                    continue;
                }

                reports.Add(module.Apply(context));
            }
            catch (Exception ex)
            {
                logger.Error($"[ITS] {module.Id} 执行失败：{ex.Message}", ex);
                reports.Add(ModuleReport.Failed(module.Id, ex));
            }
        }

        var totalChanged = reports.Sum(report => report.ChangedCount);
        var totalWarnings = reports.Sum(report => report.Warnings.Count);
        var totalErrors = reports.Count(report => report.Error is not null);

        foreach (var report in reports)
        {
            logger.Info(FormatModule(report));

            // D15 警告汇总：summary 行后逐条输出该模块的告警正文。
            // 告警文本由各模块构造时已带 [ITS] {id} 前缀，此处原样转发，避免重复前缀；
            // 用 Info 输出以免与模块自身已发出的 Warning 行重复（本块只做汇总展示）。
            foreach (var warning in report.Warnings)
            {
                logger.Info(warning);
            }
        }

        logger.Info(
            $"[ITS] 合计：执行 {reports.Count} 个模块，变更 {totalChanged}，警告 {totalWarnings}，错误 {totalErrors}，跳过 {skipped.Count}");

        return new OrchestrationReport(reports, skipped, false, totalChanged, totalWarnings, totalErrors);
    }

    private static string FormatModule(ModuleReport report)
    {
        var line = $"[ITS] {report.Id}: changed={report.ChangedCount}, warnings={report.Warnings.Count}";
        return report.Error is null ? line : $"{line}, error={report.Error.Message}";
    }
}
