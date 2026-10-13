using InescapableTarkovsSoftcore.Config;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// 功能组模块泛型基类：封装「id → 配置段 → enabled」绑定与配置段解析。
/// 子类只需实现 <see cref="Section"/>（解析本模块段）与
/// <see cref="Apply(ModContext, TConfig)"/>（纯逻辑）。
/// 新增一组功能 = 一个模块文件 + 一段配置属性。
/// </summary>
/// <typeparam name="TConfig">本模块对应的配置段类型。</typeparam>
public abstract class FeatureModule<TConfig> : IFeatureModule
    where TConfig : ModuleConfig
{
    public abstract string Id { get; }

    public abstract int Order { get; }

    public bool IsEnabled(SoftcoreConfig config) => Section(config).Enabled;

    public ModuleReport Apply(ModContext context) => Apply(context, Section(context.Config));

    /// <summary>从根配置解析本模块的配置段。</summary>
    protected abstract TConfig Section(SoftcoreConfig root);

    /// <summary>模块纯逻辑入口。</summary>
    protected abstract ModuleReport Apply(ModContext context, TConfig config);
}
