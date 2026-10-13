using InescapableTarkovsSoftcore.Config;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// 功能模块执行上下文：根配置 + 数据库表载体。
/// 日志由各模块自身的 <c>ISptLogger&lt;TModule&gt;</c> 提供，不在此硬编码。
/// </summary>
public sealed class ModContext(SoftcoreConfig config, ModTables tables)
{
    public SoftcoreConfig Config { get; } = config;

    public ModTables Tables { get; } = tables;
}
