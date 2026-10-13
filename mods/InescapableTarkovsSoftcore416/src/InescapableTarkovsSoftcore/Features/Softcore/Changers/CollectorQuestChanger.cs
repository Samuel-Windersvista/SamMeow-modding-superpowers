using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils.Json;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-A 收藏家任务重做：SURV 终态为 20 级可接、仅需交 1 张 Gamma（覆盖 BASE 的 10 级 / 2 张）。
/// <para>
/// 任务按 <b>id</b> 匹配（SPT 5.0 生产 DB 的 quests.name 是本地化键「&lt;id&gt; name」、
/// 人类名在 locales，源 3.11 的 <c>QuestName === "Collector"</c> 在 5.0 会静默失效）。
/// </para>
/// </summary>
public sealed class CollectorQuestChanger : ISoftcoreChanger
{
    /// <summary>收藏家任务 id（SPT 5.0）。</summary>
    public const string CollectorQuestId = "5c51aac186f77432ea65c552";

    /// <summary>SURV 覆盖值：可接等级。</summary>
    public const int RequiredLevel = 20;

    /// <summary>SURV 覆盖值：需交 Gamma 数量。</summary>
    public const int RequiredGammaCount = 1;

    private const string LevelConditionId = "51d33b2d4fad9e61441772c0";
    private const string HandoverConditionId = "639135534b15ca31f76bc319";
    private const string HandoverParentId = "5448bf274bdc2dfc2f8b456a";

    public string Name => "collectorQuest";

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        if (!context.Config.SecureContainersOptions.ProgressiveContainers.CollectorQuestRedone)
        {
            return;
        }

        if (!context.Tables.Templates.Quests.TryGetValue(CollectorQuestId, out var quest))
        {
            log.Warn($"未找到 Collector 任务 {CollectorQuestId}，跳过");
            return;
        }

        quest.Conditions.AvailableForStart =
        [
            new QuestCondition
            {
                Id = LevelConditionId,
                ConditionType = "Level",
                CompareMethod = ">=",
                DynamicLocale = false,
                Value = RequiredLevel,
                Index = 0
            }
        ];

        // 4.1.6 AvailableForFinish 为可空列表；缺失时先建空列表再追加（原逻辑等价，且规避空引用）。
        var finish = quest.Conditions.AvailableForFinish ??= [];

        var handover = finish.FirstOrDefault(condition =>
            condition.ConditionType == "HandoverItem"
            && condition.Target?.List is { } targets
            && targets.Contains(ItemTpl.SECURE_CONTAINER_GAMMA));

        if (handover is null)
        {
            handover = new QuestCondition
            {
                Id = HandoverConditionId,
                ConditionType = "HandoverItem",
                ParentId = HandoverParentId,
                DynamicLocale = false,
                Index = 69,
                Target = new ListOrT<string>([ItemTpl.SECURE_CONTAINER_GAMMA], null)
            };
            finish.Add(handover);
        }

        handover.Value = RequiredGammaCount;
        log.Changed(2);
    }
}
