namespace InescapableTarkovsSoftcore.Features;

/// <summary>整轮编排的结果汇总。</summary>
public sealed record OrchestrationReport(
    IReadOnlyList<ModuleReport> Modules,
    IReadOnlyList<string> SkippedModuleIds,
    bool GeneralDisabled,
    int TotalChanged,
    int TotalWarnings,
    int TotalErrors);
