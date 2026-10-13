using InescapableTarkovsSoftcore.Config;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace InescapableTarkovsSoftcore.Features.Softcore;

/// <summary>
/// Softcore 子变换器执行上下文，按「逐次表」与「DI 单例服务」分组收纳依赖：
/// 新增依赖只需在分组对象上添加成员，子变换器按组取用，避免改动各子变换器签名。
/// </summary>
public sealed class SoftcoreContext
{
    public required SoftcoreModuleConfig Config { get; init; }

    /// <summary>逐次注入的数据库表。</summary>
    public required SoftcoreTables Tables { get; init; }

    /// <summary>DI 单例配置服务。</summary>
    public required SoftcoreServices Services { get; init; }
}

/// <summary>数据库表分组。Templates/Hideout/Traders 必填；Global（G6-B/C/D）与 Bots（G6-D）可空。</summary>
public sealed class SoftcoreTables
{
    public required TemplateTable Templates { get; init; }

    public required HideoutTable Hideout { get; init; }

    public required TradersTable Traders { get; init; }

    /// <summary>全局表（G6-B 健身效果；G6-C 跳蚤全局；G6-D 战局物品限制 / 技能经验）。</summary>
    public GlobalTable? Global { get; init; }

    /// <summary>机器人表（G6-D Reshala 金 TT）。</summary>
    public BotTable? Bots { get; init; }
}

/// <summary>DI 单例配置分组。HideoutConfig 必填；其余仅 G6-B/G6-C 使用（可空）。</summary>
public sealed class SoftcoreServices
{
    public required HideoutConfig HideoutConfig { get; init; }

    /// <summary>ScavCase 内部配置（G6-B；G6-A 子变换器不使用）。</summary>
    public ScavCaseConfig? ScavCase { get; init; }

    /// <summary>跳蚤市场内部配置（G6-C 经济）。</summary>
    public RagfairConfig? Ragfair { get; init; }

    /// <summary>商人内部配置（G6-C Fence）。</summary>
    public TraderConfig? Trader { get; init; }

    /// <summary>保险内部配置（G6-C）。</summary>
    public InsuranceConfig? Insurance { get; init; }
}
