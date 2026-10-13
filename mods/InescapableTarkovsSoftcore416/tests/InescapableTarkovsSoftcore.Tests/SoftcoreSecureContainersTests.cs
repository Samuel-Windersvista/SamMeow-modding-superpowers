using InescapableTarkovsSoftcore.Features.Softcore;
using InescapableTarkovsSoftcore.Features.Softcore.Changers;
using SPTarkov.Server.Core.Models.Enums;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class SoftcoreSecureContainersTests
{
    [Fact]
    public void ProgressiveContainers_StartFromWaistPouch()
    {
        var templates = SoftcoreTestData.NewTemplates();
        templates.Profiles["standard"] = SoftcoreTestData.NewProfile(
            ItemTpl.SECURE_CONTAINER_GAMMA, ItemTpl.STASH_STANDARD_STASH_10X30, 1);
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());

        new SecureContainersChanger().Apply(context, new SoftcoreChangeLog());

        var sides = new[] { templates.Profiles["standard"].Bear!, templates.Profiles["standard"].Usec! };
        foreach (var side in sides)
        {
            var secured = side.Character!.Inventory!.Items!.Single(item => item.SlotId == "SecuredContainer");
            Assert.Equal(ItemTpl.SECURE_WAIST_POUCH, (string)secured.Template);
        }
    }

    [Fact]
    public void ProgressiveContainers_RemoveBetaFromPeacekeeperWithoutDeleting()
    {
        var templates = SoftcoreTestData.NewTemplates();
        var traders = SoftcoreTestData.NewTraders();
        traders[Traders.PEACEKEEPER] = SoftcoreTestData.NewPeacekeeper(ItemTpl.SECURE_CONTAINER_BETA);
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), traders, SoftcoreTestData.NewHideoutConfig());

        new SecureContainersChanger().Apply(context, new SoftcoreChangeLog());

        var beta = traders[Traders.PEACEKEEPER]!.Assort!.Items!.Single();
        Assert.False(beta.Upd!.UnlimitedCount);
        Assert.Equal(0d, beta.Upd.StackObjectsCount);
        Assert.Equal(0, beta.Upd.BuyRestrictionMax);
    }

    [Fact]
    public void ProgressiveContainers_ReplaceCultistPouchRewardWithKappa_AndAddRecipes()
    {
        var templates = SoftcoreTestData.NewTemplates();
        var hideout = SoftcoreTestData.NewHideout();
        var hideoutConfig = SoftcoreTestData.NewHideoutConfig(ItemTpl.SECURE_WAIST_POUCH);
        var context = SoftcoreTestData.NewContext(
            templates, hideout, SoftcoreTestData.NewTraders(), hideoutConfig);

        new SecureContainersChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(
            ItemTpl.SECURE_CONTAINER_KAPPA,
            (string)hideoutConfig.CultistCircle.DirectRewards[0].RequiredItems[0]);
        Assert.Equal(4, hideout.Production.Recipes.Count);
    }

    [Fact]
    public void BiggerContainers_ApplySurvSizes()
    {
        var templates = SoftcoreTestData.NewTemplates();
        foreach (var template in new[]
                 {
                     ItemTpl.SECURE_WAIST_POUCH, ItemTpl.SECURE_CONTAINER_ALPHA, ItemTpl.SECURE_CONTAINER_BETA,
                     ItemTpl.SECURE_CONTAINER_EPSILON, ItemTpl.SECURE_CONTAINER_GAMMA, ItemTpl.SECURE_CONTAINER_KAPPA
                 })
        {
            templates.Items[template] = SoftcoreTestData.NewContainer(template, 1, 1);
        }

        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());

        new SecureContainersChanger().Apply(context, new SoftcoreChangeLog());

        // SURV 终态（cellsV, cellsH）：腰包 2×4（源 TS 注释「腰包是 2x4」）。
        SoftcoreTestData.AssertSize(templates, ItemTpl.SECURE_WAIST_POUCH, cellsV: 2, cellsH: 4);
        SoftcoreTestData.AssertSize(templates, ItemTpl.SECURE_CONTAINER_ALPHA, cellsV: 3, cellsH: 3);
        SoftcoreTestData.AssertSize(templates, ItemTpl.SECURE_CONTAINER_BETA, cellsV: 3, cellsH: 4);
        SoftcoreTestData.AssertSize(templates, ItemTpl.SECURE_CONTAINER_EPSILON, cellsV: 3, cellsH: 5);
        SoftcoreTestData.AssertSize(templates, ItemTpl.SECURE_CONTAINER_GAMMA, cellsV: 4, cellsH: 5);
        SoftcoreTestData.AssertSize(templates, ItemTpl.SECURE_CONTAINER_KAPPA, cellsV: 5, cellsH: 5);
    }

    [Fact]
    public void Disabled_ProducesZeroChanges()
    {
        var templates = SoftcoreTestData.NewTemplates();
        templates.Profiles["standard"] = SoftcoreTestData.NewProfile(
            ItemTpl.SECURE_CONTAINER_GAMMA, ItemTpl.STASH_STANDARD_STASH_10X30, 1);
        templates.Items[ItemTpl.SECURE_CONTAINER_GAMMA] = SoftcoreTestData.NewContainer(ItemTpl.SECURE_CONTAINER_GAMMA, 1, 1);
        var config = new Config.SoftcoreModuleConfig();
        config.SecureContainersOptions.Enabled = false;
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig(), config);

        var log = new SoftcoreChangeLog();
        new SecureContainersChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Equal(
            ItemTpl.SECURE_CONTAINER_GAMMA,
            (string)templates.Profiles["standard"].Bear!.Character!.Inventory!.Items!.Single(i => i.SlotId == "SecuredContainer").Template);
    }
}
