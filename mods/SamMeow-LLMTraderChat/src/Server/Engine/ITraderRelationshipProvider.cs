namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 玩家关系状态读取抽象（工单 14）：返回某（sessionId, traderId）的关系快照，供引擎计算动态态度。
///
/// 引擎不触碰 SPT 运行时类型；生产实现（<c>SptTraderRelationshipProvider</c>）由 wiring 注入。
/// 返回 <c>null</c> 表示无记录 / 无法读取，引擎据此降级为默认 <see cref="AttitudeTier.Cold"/>。
/// </summary>
public interface ITraderRelationshipProvider
{
    /// <summary>
    /// 读取玩家与目标商人的关系快照；无记录或不可读返回 <c>null</c>。
    /// </summary>
    Task<TraderRelationshipSnapshot?> GetRelationshipAsync(
        string sessionId,
        string traderId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 空实现：始终返回 <c>null</c>（引擎默认态度降级）。用于未接线关系提供器时保持向后兼容。
/// </summary>
public sealed class NullTraderRelationshipProvider : ITraderRelationshipProvider
{
    /// <summary>无状态单例。</summary>
    public static readonly NullTraderRelationshipProvider Instance = new();

    /// <inheritdoc />
    public Task<TraderRelationshipSnapshot?> GetRelationshipAsync(
        string sessionId,
        string traderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<TraderRelationshipSnapshot?>(null);
}
