using InescapableTarkovsSoftcore.Features.Softcore;
using InescapableTarkovsSoftcore.Features.Softcore.Changers;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class SoftcoreStashTests
{
    [Fact]
    public void ProgressiveStash_ResetsAreaToLevel1_AndRewritesStartingStash()
    {
        var templates = SoftcoreTestData.NewTemplates();
        templates.Profiles["standard"] = SoftcoreTestData.NewProfile(
            ItemTpl.SECURE_WAIST_POUCH, ItemTpl.STASH_EDGE_OF_DARKNESS_STASH_10X68, 4);
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());

        new StashChanger().Apply(context, new SoftcoreChangeLog());

        var side = templates.Profiles["standard"].Bear!;
        Assert.Equal(1, side.Character!.Hideout!.Areas!.Single(area => area.Type == HideoutAreas.Stash).Level);
        Assert.Equal(
            ItemTpl.STASH_STANDARD_STASH_10X30,
            (string)side.Character!.Inventory!.Items!.Single(item => item.Id == SoftcoreTestData.StashItemId).Template);
        Assert.Contains(side.Character!.Bonuses!, bonus => bonus.Type == BonusType.StashSize);
    }

    [Fact]
    public void BiggerStash_AppliesSurvRows()
    {
        var templates = SoftcoreTestData.NewTemplates();
        foreach (var (template, rows) in new[]
                 {
                     (ItemTpl.STASH_STANDARD_STASH_10X30, 30),
                     (ItemTpl.STASH_LEFT_BEHIND_STASH_10X40, 40),
                     (ItemTpl.STASH_PREPARE_FOR_ESCAPE_STASH_10X50, 50),
                     (ItemTpl.STASH_EDGE_OF_DARKNESS_STASH_10X68, 68),
                     (ItemTpl.STASH_THE_UNHEARD_EDITION_STASH_10X72, 72)
                 })
        {
            templates.Items[template] = SoftcoreTestData.NewStash(template, rows);
        }

        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());

        new StashChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(56, templates.Items[ItemTpl.STASH_STANDARD_STASH_10X30].Properties!.Grids!.First().Properties!.CellsV);
        Assert.Equal(100, templates.Items[ItemTpl.STASH_LEFT_BEHIND_STASH_10X40].Properties!.Grids!.First().Properties!.CellsV);
        Assert.Equal(150, templates.Items[ItemTpl.STASH_PREPARE_FOR_ESCAPE_STASH_10X50].Properties!.Grids!.First().Properties!.CellsV);
        Assert.Equal(200, templates.Items[ItemTpl.STASH_EDGE_OF_DARKNESS_STASH_10X68].Properties!.Grids!.First().Properties!.CellsV);
        Assert.Equal(250, templates.Items[ItemTpl.STASH_THE_UNHEARD_EDITION_STASH_10X72].Properties!.Grids!.First().Properties!.CellsV);
    }

    [Fact]
    public void LessCurrencyForConstruction_DividesCurrencyByTen_ButKeepsLoyalty()
    {
        var hideout = SoftcoreTestData.NewHideout();
        hideout.Areas!.Add(SoftcoreTestData.NewStashArea(
            new StageRequirement { TemplateId = Money.ROUBLES, Count = 5000, Type = "Item" },
            new StageRequirement { TemplateId = Money.EUROS, Count = 300, Type = "Item" },
            new StageRequirement { TemplateId = "5d235b4d86f7742e017bc88a", Count = 7, Type = "Item" },
            new StageRequirement { LoyaltyLevel = 3, Type = "Loyalty" }));
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), hideout, SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());

        new StashChanger().Apply(context, new SoftcoreChangeLog());

        var requirements = hideout.Areas.Single(area => area.Type == HideoutAreas.Stash).Stages!["1"].Requirements!;
        Assert.Equal(500, requirements.Single(r => r.TemplateId == Money.ROUBLES).Count);
        Assert.Equal(30, requirements.Single(r => r.TemplateId == Money.EUROS).Count);
        Assert.Equal(7, requirements.Single(r => (string)r.TemplateId == "5d235b4d86f7742e017bc88a").Count);
        // easierLoyalty 在 SURV 终态为 false，故忠诚度要求保持不变。
        Assert.Equal(3, requirements.Single(r => r.LoyaltyLevel.HasValue).LoyaltyLevel);
    }

    [Fact]
    public void Disabled_ProducesZeroChanges()
    {
        var templates = SoftcoreTestData.NewTemplates();
        templates.Profiles["standard"] = SoftcoreTestData.NewProfile(
            ItemTpl.SECURE_WAIST_POUCH, ItemTpl.STASH_EDGE_OF_DARKNESS_STASH_10X68, 4);
        var config = new Config.SoftcoreModuleConfig();
        config.StashOptions.Enabled = false;
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig(), config);

        var log = new SoftcoreChangeLog();
        new StashChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Equal(4, templates.Profiles["standard"].Bear!.Character!.Hideout!.Areas!.Single(a => a.Type == HideoutAreas.Stash).Level);
    }
}
