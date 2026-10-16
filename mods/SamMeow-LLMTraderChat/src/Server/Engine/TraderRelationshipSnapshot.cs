namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>活跃任务摘要（渲染用）：中文名 + 是否可交付。</summary>
/// <param name="Name">任务中文名（已解析）。</param>
/// <param name="ReadyToFinish">是否处于「待交付」（AvailableForFinish）状态。</param>
public sealed record ActiveQuestInfo(string Name, bool ReadyToFinish);

/// <summary>
/// 玩家与目标商人的关系快照（工单 14）：单次生成回复前读取的关系状态，
/// 作为计算 <see cref="AttitudeTier"/> 与渲染态度块的输入。
/// 全部字段可选——缺失（null / 0 / 空）时按 <see cref="TraderAttitudeEvaluator"/> 的默认语义降级。
/// </summary>
/// <param name="PlayerLevel">玩家 PMC 等级（未知为 null）。</param>
/// <param name="PlayerSide">玩家 PMC 阵营（Usec / Bear 等，未知为 null）。</param>
/// <param name="TraderUnlocked">目标商人是否已解锁（未知为 null）。</param>
/// <param name="Standing">与该商人的好感度（未知为 null）。</param>
/// <param name="LoyaltyLevel">与该商人的忠诚等级（未知为 null，评估时视为 1）。</param>
/// <param name="CompletedQuestCount">该商人名下已完成委托数。</param>
/// <param name="FailedQuestCount">该商人名下失败委托数。</param>
/// <param name="ActiveQuests">进行中/待交付任务（已排序截断，最多 5 条）。</param>
/// <param name="RecentlyCompletedQuestNames">最近完成任务名（已排序截断，最多 3 条）。</param>
/// <param name="ActiveQuestDetails">进行中/待交付任务的详情（工单 16；与 <paramref name="ActiveQuests"/> 同序同上限）。</param>
public sealed record TraderRelationshipSnapshot(
    int? PlayerLevel = null,
    string? PlayerSide = null,
    bool? TraderUnlocked = null,
    double? Standing = null,
    int? LoyaltyLevel = null,
    int CompletedQuestCount = 0,
    int FailedQuestCount = 0,
    IReadOnlyList<ActiveQuestInfo>? ActiveQuests = null,
    IReadOnlyList<string>? RecentlyCompletedQuestNames = null,
    IReadOnlyList<TraderQuestDetail>? ActiveQuestDetails = null);
