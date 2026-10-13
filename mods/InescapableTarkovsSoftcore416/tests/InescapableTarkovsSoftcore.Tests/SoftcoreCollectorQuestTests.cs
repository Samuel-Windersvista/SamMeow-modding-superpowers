using InescapableTarkovsSoftcore.Features.Softcore;
using InescapableTarkovsSoftcore.Features.Softcore.Changers;
using SPTarkov.Server.Core.Models.Enums;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class SoftcoreCollectorQuestTests
{
    [Fact]
    public void CollectorQuest_RequiresLevel20AndOneGamma()
    {
        var templates = SoftcoreTestData.NewTemplates();
        templates.Quests[SoftcoreTestData.CollectorQuestId] = SoftcoreTestData.NewCollectorQuest();
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());

        new CollectorQuestChanger().Apply(context, new SoftcoreChangeLog());

        var quest = templates.Quests[SoftcoreTestData.CollectorQuestId];
        var start = Assert.Single(quest.Conditions.AvailableForStart);
        Assert.Equal("Level", start.ConditionType);
        Assert.Equal(CollectorQuestChanger.RequiredLevel, start.Value);
        Assert.Equal(20d, start.Value);

        var handover = Assert.Single(quest.Conditions.AvailableForFinish, condition => condition.ConditionType == "HandoverItem");
        Assert.Equal(1d, handover.Value);
        var targets = handover.Target!.List!;
        Assert.Single(targets);
        Assert.Equal(ItemTpl.SECURE_CONTAINER_GAMMA, targets[0].ToString());
    }

    [Fact]
    public void CollectorQuest_Disabled_LeavesQuestUnchanged()
    {
        var templates = SoftcoreTestData.NewTemplates();
        templates.Quests[SoftcoreTestData.CollectorQuestId] = SoftcoreTestData.NewCollectorQuest();
        var config = new Config.SoftcoreModuleConfig();
        config.SecureContainersOptions.ProgressiveContainers.CollectorQuestRedone = false;
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig(), config);

        var log = new SoftcoreChangeLog();
        new CollectorQuestChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Empty(templates.Quests[SoftcoreTestData.CollectorQuestId].Conditions.AvailableForStart);
        Assert.Empty(templates.Quests[SoftcoreTestData.CollectorQuestId].Conditions.AvailableForFinish);
    }

    [Fact]
    public void CollectorQuest_MatchedById_NotByHumanName()
    {
        // 名为 "Collector"（旧式人类名）但 id 不是收藏家 → 不应被改动（按 id 匹配）。
        var templates = SoftcoreTestData.NewTemplates();
        var imposter = SoftcoreTestData.NewCollectorQuest();
        imposter.Id = "0000000000000000000000c1";
        imposter.Name = "Collector";
        templates.Quests[imposter.Id] = imposter;
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());

        var log = new SoftcoreChangeLog();
        new CollectorQuestChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Empty(imposter.Conditions.AvailableForStart);
        Assert.Contains(log.Warnings, w => w.Contains(CollectorQuestChanger.CollectorQuestId, StringComparison.Ordinal));
    }

    [Fact]
    public void CollectorQuest_MissingId_Warns()
    {
        var templates = SoftcoreTestData.NewTemplates();
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());
        var log = new SoftcoreChangeLog { Changer = new CollectorQuestChanger().Name };

        new CollectorQuestChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Contains(log.Warnings, w => w.Contains("未找到 Collector 任务", StringComparison.Ordinal));
    }
}
