namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// 单个功能组的执行报告：变更计数、告警列表，以及（失败时的）异常。
/// </summary>
public sealed record ModuleReport(string Id, int ChangedCount, IReadOnlyList<string> Warnings, Exception? Error = null)
{
    /// <summary>正常完成。</summary>
    public static ModuleReport Ok(string id, int changedCount = 0, IReadOnlyList<string>? warnings = null) =>
        new(id, changedCount, warnings ?? [], null);

    /// <summary>执行失败（被编排器捕获）。</summary>
    public static ModuleReport Failed(string id, Exception error) => new(id, 0, [], error);
}
