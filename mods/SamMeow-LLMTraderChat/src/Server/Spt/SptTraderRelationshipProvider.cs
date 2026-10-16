using SamMeow.LLMTraderChat.Server.Engine;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;

namespace SamMeow.LLMTraderChat.Server.Spt;

/// <summary>
/// 从 SPT profile 读取玩家与目标商人关系的 <see cref="ITraderRelationshipProvider"/> 生产实现（工单 14）。
///
/// 数据来源（源码实读 SPT 4.1）：<c>ProfileHelper.GetPmcProfile</c> 取 <c>PmcData</c>，
/// 其 <c>TradersInfo[traderId]</c> 给出解锁 / 好感度 / 忠诚等级；<c>Quests</c> 与
/// <c>TemplateTable.Quests</c> 交叉筛选出该商人名下任务（计数 / 活跃 / 最近完成）；
/// <c>Info.Side/Level</c> 给出玩家阵营与等级。任务中文名经 <c>LocaleService</c> 解析。
/// 只读；任何读取失败由引擎兜底降级，不在此抛错中断对话。
/// </summary>
public sealed class SptTraderRelationshipProvider : ITraderRelationshipProvider
{
    private readonly ProfileHelper _profileHelper;
    private readonly TemplateTable _templateTable;
    private readonly LocaleService _localeService;
    private readonly ISptLogger<SptTraderRelationshipProvider> _logger;

    /// <summary>构造（依赖由 DI 注入）。</summary>
    public SptTraderRelationshipProvider(
        ProfileHelper profileHelper,
        TemplateTable templateTable,
        LocaleService localeService,
        ISptLogger<SptTraderRelationshipProvider> logger)
    {
        _profileHelper = profileHelper;
        _templateTable = templateTable;
        _localeService = localeService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<TraderRelationshipSnapshot?> GetRelationshipAsync(
        string sessionId,
        string traderId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!MongoId.IsValidMongoId(sessionId) || !MongoId.IsValidMongoId(traderId))
        {
            return Task.FromResult<TraderRelationshipSnapshot?>(null);
        }

        var pmc = _profileHelper.GetPmcProfile(new MongoId(sessionId));
        if (pmc is null)
        {
            return Task.FromResult<TraderRelationshipSnapshot?>(null);
        }

        var targetTraderId = new MongoId(traderId);
        bool? unlocked = null;
        double? standing = null;
        int? loyaltyLevel = null;
        if (pmc.TradersInfo is { } tradersInfo && tradersInfo.TryGetValue(targetTraderId, out var traderInfo))
        {
            unlocked = traderInfo.Unlocked;
            standing = traderInfo.Standing;
            loyaltyLevel = traderInfo.LoyaltyLevel;
        }

        var completed = 0;
        var failed = 0;
        List<ActiveQuestInfo>? activeQuests = null;
        List<string>? recentCompleted = null;
        List<TraderQuestDetail>? activeQuestDetails = null;

        if (pmc.Quests is { Count: > 0 } quests && _templateTable.Quests is { Count: > 0 } questDb)
        {
            var active = new List<(QuestStatus Status, Quest Static, double SortTime, bool ReadyToFinish)>();
            var recent = new List<(QuestStatus Status, Quest Static, double SortTime)>();

            foreach (var quest in quests)
            {
                if (!questDb.TryGetValue(quest.QId, out var staticQuest) || staticQuest.TraderId != targetTraderId)
                {
                    continue;
                }

                switch (quest.Status)
                {
                    case QuestStatusEnum.Success:
                        completed++;
                        recent.Add((quest, staticQuest, CompletionTime(quest)));
                        break;
                    case QuestStatusEnum.Fail:
                    case QuestStatusEnum.FailRestartable:
                    case QuestStatusEnum.MarkedAsFailed:
                    case QuestStatusEnum.Expired:
                        failed++;
                        break;
                    case QuestStatusEnum.Started:
                    case QuestStatusEnum.AvailableForFinish:
                        active.Add((
                            quest,
                            staticQuest,
                            quest.StartTime,
                            quest.Status == QuestStatusEnum.AvailableForFinish));
                        break;
                }
            }

            // 活跃委托条数上限由本提供器单一负责：与态度段的 MaxActiveQuestsRendered（=5）共享同一常量，
            // 属刻意设计（非巧合）；渲染层不再设自身条数上限，仅做字数预算。
            var orderedActive = active
                .OrderByDescending(entry => entry.ReadyToFinish)
                .ThenByDescending(entry => entry.SortTime)
                .Take(TraderAttitudeEvaluator.MaxActiveQuestsRendered)
                .ToList();

            activeQuests = orderedActive
                .Select(entry => new ActiveQuestInfo(
                    ResolveQuestName(entry.Status.QId, questDb), entry.ReadyToFinish))
                .ToList();

            // 工单 16：同序同上限的委托详情（名 + 目标 + 描述 + 地区），供披露门控渲染。
            activeQuestDetails = orderedActive
                .Select(entry => BuildQuestDetail(entry.Status.QId, entry.Static, questDb))
                .ToList();

            recentCompleted = recent
                .OrderByDescending(entry => entry.SortTime)
                .Take(TraderAttitudeEvaluator.MaxRecentCompletedQuestsRendered)
                .Select(entry => ResolveQuestName(entry.Status.QId, questDb))
                .ToList();
        }

        var snapshot = new TraderRelationshipSnapshot(
            PlayerLevel: pmc.Info?.Level,
            PlayerSide: pmc.Info?.Side,
            TraderUnlocked: unlocked,
            Standing: standing,
            LoyaltyLevel: loyaltyLevel,
            CompletedQuestCount: completed,
            FailedQuestCount: failed,
            ActiveQuests: activeQuests,
            RecentlyCompletedQuestNames: recentCompleted,
            ActiveQuestDetails: activeQuestDetails);

        return Task.FromResult<TraderRelationshipSnapshot?>(snapshot);
    }

    /// <summary>
    /// 构建单条委托详情（工单 16）：名（解析链）、目标（<c>AvailableForFinish</c> 条件的裸条件 id locale 键）、
    /// 描述（<c>&lt;qid&gt; description</c>）、地区（<c>&lt;locationId&gt; Name</c>）。文本预算由渲染层负责。
    /// </summary>
    private TraderQuestDetail BuildQuestDetail(MongoId questId, Quest staticQuest, Dictionary<MongoId, Quest> questDb)
    {
        var objectives = new List<string>();
        if (staticQuest.Conditions?.AvailableForFinish is { Count: > 0 } conditions)
        {
            foreach (var condition in conditions)
            {
                var key = condition.Id.ToString();
                var text = TryGetLocaleValue("ch", key) ?? TryGetLocaleValue(null, key);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    objectives.Add(text);
                }
            }
        }

        var description = TryGetLocaleValue("ch", $"{questId} description")
            ?? TryGetLocaleValue(null, $"{questId} description");

        string? locationName = null;
        if (!string.IsNullOrWhiteSpace(staticQuest.Location))
        {
            var key = $"{staticQuest.Location} Name";
            locationName = TryGetLocaleValue("ch", key) ?? TryGetLocaleValue(null, key);
        }

        return new TraderQuestDetail(ResolveQuestName(questId, questDb), objectives, description, locationName);
    }

    /// <summary>完成任务时间：优先取 <c>StatusTimers[Success]</c>，缺失回退 <c>StartTime</c>。</summary>
    private static double CompletionTime(QuestStatus quest) =>
        quest.StatusTimers is { } timers && timers.TryGetValue(QuestStatusEnum.Success, out var done)
            ? done
            : quest.StartTime;

    /// <summary>任务中文名解析链：ch 本地化 → 默认本地化 → Quest.QuestName → Quest.Name → qId。</summary>
    private string ResolveQuestName(MongoId questId, Dictionary<MongoId, Quest> questDb)
    {
        var key = $"{questId} name";

        var chinese = TryGetLocaleValue("ch", key);
        if (chinese is not null)
        {
            return chinese;
        }

        var fallback = TryGetLocaleValue(null, key);
        if (fallback is not null)
        {
            return fallback;
        }

        if (questDb.TryGetValue(questId, out var staticQuest))
        {
            if (!string.IsNullOrWhiteSpace(staticQuest.QuestName))
            {
                return staticQuest.QuestName;
            }

            if (!string.IsNullOrWhiteSpace(staticQuest.Name))
            {
                return staticQuest.Name;
            }
        }

        return questId.ToString();
    }

    /// <summary>安全读取本地化文本；本地化服务异常时记录告警并回退下一来源，取消异常照常上抛。</summary>
    private string? TryGetLocaleValue(string? language, string key)
    {
        try
        {
            var locale = language is null ? _localeService.GetLocaleDb() : _localeService.GetLocaleDb(language);
            return locale.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Warning(
                $"[LLMTC][provider] 本地化读取失败（language={language ?? "default"}, key={key}），"
                + $"已回退下一来源：{ex.Message}");
            return null;
        }
    }
}
