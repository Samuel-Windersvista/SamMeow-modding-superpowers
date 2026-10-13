using SPTarkov.Common.Models.Logging;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// 模块告警构造助手：统一「[ITS] {moduleId}: {message}」形态，同时写入报告告警列表与日志。
/// 供各功能模块复用；G4/G5 原先各自持有重复的私有 Warn 实现，此处收敛为单一共享形态。
/// </summary>
internal static class ModuleWarnings
{
    public static void Add<T>(ISptLogger<T> logger, string moduleId, List<string> warnings, string message)
    {
        var line = $"[ITS] {moduleId}: {message}";
        warnings.Add(line);
        logger.Warning(line);
    }
}
