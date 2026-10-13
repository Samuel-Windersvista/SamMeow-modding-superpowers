namespace InescapableTarkovsSoftcore.Features.Softcore;

/// <summary>
/// Softcore 子变换器契约。每个子变换器负责一组相关改动，接收上下文与累加器，
/// 只做数据变更与计数/告警汇报；异常由 <see cref="SoftcoreModule"/> 统一隔离。
/// 新增一批功能 = 新增一个子变换器并在 <see cref="SoftcoreModule"/> 注册。
/// </summary>
public interface ISoftcoreChanger
{
    /// <summary>子变换器名（用于日志与告警前缀）。</summary>
    string Name { get; }

    /// <summary>应用本组改动。</summary>
    void Apply(SoftcoreContext context, SoftcoreChangeLog log);
}
