using InescapableTarkovsSoftcore.Features.Softcore;
using InescapableTarkovsSoftcore.Features.Softcore.Changers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class SoftcoreScavCaseTests
{
    [Fact]
    public void BetterRewards_AppliesBuyableRules_AndAmmoBoxPricePatch()
    {
        const string keepId = "0000000000000000000000a1";
        const string junkId = "0000000000000000000000b1";
        const string whitelistParent = "5447b5f14bdc2d61278b4567"; // 源白名单项（AssaultRifle）
        const string whiteId = "0000000000000000000000c1";
        const string questId = "0000000000000000000000d1";
        const string nonBuyId = "0000000000000000000000e1";
        const string boxId = "0000000000000000000000f1";
        const string ammoId = "0000000000000000000000f2";

        var templates = SoftcoreTestData.NewTemplates(new HandbookBase
        {
            Categories = [],
            Items =
            [
                SoftcoreTestData.NewHandbookItem(keepId, 20000),
                SoftcoreTestData.NewHandbookItem(junkId, 5000),
                SoftcoreTestData.NewHandbookItem(whiteId, 5000),
                SoftcoreTestData.NewHandbookItem(questId, 5000),
                SoftcoreTestData.NewHandbookItem(nonBuyId, 5000),
                SoftcoreTestData.NewHandbookItem(boxId, 1000),
                SoftcoreTestData.NewHandbookItem(ammoId, 5000)
            ]
        });

        templates.Items[keepId] = SoftcoreTestData.NewTemplateItem(keepId, "00000000000000000000f001");
        templates.Items[junkId] = SoftcoreTestData.NewTemplateItem(junkId, "00000000000000000000f002");
        templates.Items[whiteId] = SoftcoreTestData.NewTemplateItem(whiteId, whitelistParent);
        templates.Items[questId] = SoftcoreTestData.NewTemplateItem(questId, "00000000000000000000f003", questItem: true);
        templates.Items[nonBuyId] = SoftcoreTestData.NewTemplateItem(nonBuyId, "00000000000000000000f004");

        var box = SoftcoreTestData.NewTemplateItem(boxId, ScavCaseChanger.AmmoBoxParent);
        box.Properties!.StackSlots =
        [
            new StackSlot
            {
                MaxCount = 30,
                Properties = new StackSlotProperties
                {
                    Filters = [new SlotFilter { Filter = [ammoId] }]
                }
            }
        ];
        templates.Items[boxId] = box;

        var traders = SoftcoreTestData.NewTraders();
        traders["0000000000000000000000aa"] = SoftcoreTestData.NewTraderWithAssort(keepId, junkId, whiteId, questId, boxId);

        var scavCase = SoftcoreTestData.NewScavCaseConfig();
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), traders, SoftcoreTestData.NewHideoutConfig(), scavCase: scavCase);

        new ScavCaseChanger().Apply(context, new SoftcoreChangeLog());

        var blacklist = scavCase.RewardItemBlacklist.Select(id => (string)id).ToHashSet();
        Assert.Equal(ScavCaseResourceLoader.Load().ParentBlacklist.Count, scavCase.RewardItemParentBlacklist.Count);
        Assert.Contains(junkId, blacklist);
        Assert.Contains(questId, blacklist);
        Assert.DoesNotContain(keepId, blacklist);
        Assert.DoesNotContain(whiteId, blacklist);
        Assert.DoesNotContain(nonBuyId, blacklist);
        Assert.DoesNotContain(boxId, blacklist);
        // 弹药箱补丁：手册价 = round(内容弹药价 5000 × 数量 30) = 150000。
        Assert.Equal(150000d, templates.Handbook.Items.Single(item => (string)item.Id == boxId).Price);
    }

    [Fact]
    public void Rebalance_AppliesValueRanges_AndReworkedRecipes()
    {
        var context = NewContext(out var hideout, out var scavCase);
        context.Config.ScavCaseOptions.FasterScavcase.Enabled = false;

        new ScavCaseChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(3, scavCase.RewardItemValueRangeRub.Count);
        Assert.Equal(1d, scavCase.RewardItemValueRangeRub["common"].Min);
        Assert.Equal(20000d, scavCase.RewardItemValueRangeRub["common"].Max);
        Assert.Equal(60001d, scavCase.RewardItemValueRangeRub["superrare"].Min);
        Assert.Equal(5, hideout.Production!.ScavRecipes!.Count);
        Assert.Equal(2500d, hideout.Production.ScavRecipes![0].ProductionTime);
    }

    [Fact]
    public void FasterScavcase_DividesProductionTime_SourceSemantics()
    {
        var context = NewContext(out var hideout, out _);
        hideout.Production!.ScavRecipes = [SoftcoreTestData.NewScavRecipe("0000000000000000000000d1", 2500)];

        new ScavCaseChanger().Apply(context, new SoftcoreChangeLog());

        // 忠于源 TS：round(时间 / speedMultiplier)；SURV speedMultiplier=0.5 → ×2（见报告不确定项）。
        Assert.Equal(5000d, hideout.Production.ScavRecipes![0].ProductionTime);
    }

    [Fact]
    public void FasterScavcase_NullProductionTime_IsSkipped_AndNotCounted()
    {
        // 4.1.6 ProductionTime 为可空 double?：null 跳过、保持 null、不计数（D7 表 3 之 ScavCase 路径）。
        // 关闭 betterRewards/rebalance 以保留自建配方并隔离计数。
        var context = NewContext(out var hideout, out _);
        context.Config.ScavCaseOptions.BetterRewards = false;
        context.Config.ScavCaseOptions.Rebalance = false;
        hideout.Production!.ScavRecipes = [SoftcoreTestData.NewScavRecipe("0000000000000000000000d2", null)];
        var log = new SoftcoreChangeLog();

        new ScavCaseChanger().Apply(context, log);

        Assert.Null(hideout.Production.ScavRecipes![0].ProductionTime);
        Assert.Equal(0, log.ChangedCount);
    }

    [Fact]
    public void Disabled_ProducesZeroChanges()
    {
        var context = NewContext(out var hideout, out var scavCase);
        context.Config.ScavCaseOptions.Enabled = false;

        var log = new SoftcoreChangeLog();
        new ScavCaseChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Empty(scavCase.RewardItemParentBlacklist);
        Assert.Empty(scavCase.RewardItemValueRangeRub);
        Assert.Empty(hideout.Production!.ScavRecipes!);
    }

    private static SoftcoreContext NewContext(out HideoutTable hideout, out SPTarkov.Server.Core.Models.Spt.Config.ScavCaseConfig scavCase)
    {
        hideout = SoftcoreTestData.NewHideout();
        hideout.Production!.ScavRecipes = [];
        scavCase = SoftcoreTestData.NewScavCaseConfig();
        return SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(),
            hideout,
            SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(),
            scavCase: scavCase);
    }
}
