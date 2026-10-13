using InescapableTarkovsSoftcore.Config;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// 功能组模块契约。每组功能实现一个模块，由 <see cref="ModuleOrchestrator"/>
/// 按其 <see cref="Order"/> 升序调用。实现应为纯逻辑：入参为配置与内存表，
/// 返回组报告，不触碰 I/O。
/// </summary>
public interface IFeatureModule
{
    /// <summary>模块标识，须与 <see cref="SoftcoreConfig"/> 中对应段名一致。</summary>
    string Id { get; }

    /// <summary>执行顺序数值（升序；同值保持注册序）。D5 语义见各模块注释。</summary>
    int Order { get; }

    /// <summary>本模块对应配置段是否启用（模块自持 id → 配置段绑定）。</summary>
    bool IsEnabled(SoftcoreConfig config);

    /// <summary>应用变换并返回组报告（变更计数 + 告警列表）。</summary>
    ModuleReport Apply(ModContext context);
}
