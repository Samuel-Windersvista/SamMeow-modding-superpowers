namespace InescapableTarkovsSoftcore.Features.Softcore;

/// <summary>
/// Softcore 子变换器的变更/告警/错误累加器。
/// 告警统一为 <c>[ITS] softcore.&lt;changer名&gt;: ...</c>（编排器按「已带 [ITS] {id} 前缀」原样转发）。
/// </summary>
public sealed class SoftcoreChangeLog
{
    /// <summary>当前子变换器名（由 SoftcoreModule 在每个子变换器执行前设置）。</summary>
    public string Changer { get; set; } = "module";

    public int ChangedCount { get; private set; }

    public List<string> Warnings { get; } = [];

    public List<Exception> Errors { get; } = [];

    public void Changed(int count = 1) => ChangedCount += count;

    public void Warn(string message) => Warnings.Add($"[ITS] softcore.{Changer}: {message}");

    public void Error(Exception exception) => Errors.Add(exception);
}
