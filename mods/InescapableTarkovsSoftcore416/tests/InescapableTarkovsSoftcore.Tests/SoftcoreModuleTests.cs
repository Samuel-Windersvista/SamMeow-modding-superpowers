using InescapableTarkovsSoftcore.Config;
using InescapableTarkovsSoftcore.Features;
using InescapableTarkovsSoftcore.Features.Softcore;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class SoftcoreModuleTests
{
    [Fact]
    public void Module_IsSoftcoreAtOrder100()
    {
        var module = NewModule(SoftcoreTestData.NewHideoutConfig(), out _);

        Assert.Equal("softcore", module.Id);
        Assert.Equal(100, module.Order);
    }

    [Fact]
    public void Module_AppliesBatch_WithoutErrors()
    {
        var module = NewModule(SoftcoreTestData.NewHideoutConfig(ItemTpl.SECURE_WAIST_POUCH), out var context);

        var report = module.Apply(context);

        Assert.Null(report.Error);
        Assert.True(report.ChangedCount > 0);
        Assert.Equal("softcore", report.Id);
    }

    [Fact]
    public void Module_DisabledSoftcore_ReportsDisabled()
    {
        var module = NewModule(SoftcoreTestData.NewHideoutConfig(ItemTpl.SECURE_WAIST_POUCH), out var context);
        var config = context.Config;
        config.Softcore.Enabled = false;

        // 禁用判定归编排器（见 ModuleOrchestrator）；此处断言模块自持开关绑定正确。
        Assert.False(module.IsEnabled(config));
    }

    [Fact]
    public void Module_Warnings_CarrySoftcoreChangerPrefix()
    {
        var module = NewModule(SoftcoreTestData.NewHideoutConfig(), out var context);
        context.Config.Softcore.FasterCraftingTime.BaseCraftingTimeMultiplier = 0;

        var report = module.Apply(context);

        Assert.Contains(
            report.Warnings,
            warning => warning.StartsWith("[ITS] softcore.fasterCraftingTime:", StringComparison.Ordinal));
    }

    [Fact]
    public void Module_RegistrationOrder_KeepsNewRecipesUndivided()
    {
        // 经真实 SoftcoreModule.Apply 断言注册序：fasterCraftingTime（÷3）先于 craftingChanges（新增配方）。
        var module = NewModule(SoftcoreTestData.NewHideoutConfig(), out var context);
        var recipes = context.Tables.HideoutTable.Production!.Recipes!;
        recipes.Add(SoftcoreTestData.NewRecipe("0000000000000000000000e1", "0000000000000000000000ee", 900));

        var report = module.Apply(context);

        Assert.Null(report.Error);
        Assert.Equal(300d, recipes.Single(r => r.EndProduct == "0000000000000000000000ee").ProductionTime);
        Assert.Equal(23d, recipes.Single(r => r.EndProduct == "5c10c8fd86f7743d7d706df3").ProductionTime);
    }

    private static SoftcoreModule NewModule(HideoutConfig hideoutConfig, out ModContext context)
    {
        var templates = BuildTemplates();
        var hideout = BuildHideout();
        var traders = BuildTraders();

        var tables = new ModTables(templates, hideout, null!, traders, null!, null!);
        context = new ModContext(new SoftcoreConfig(), tables);

        return new SoftcoreModule(
            new TestSptLogger<SoftcoreModule>(),
            hideoutConfig,
            SoftcoreTestData.NewScavCaseConfig(),
            SoftcoreTestData.NewRagfairConfig(),
            SoftcoreTestData.NewTraderConfig(),
            SoftcoreTestData.NewInsuranceConfig());
    }

    private static TemplateTable BuildTemplates()
    {
        var templates = SoftcoreTestData.NewTemplates();
        templates.Items[ItemTpl.SECURE_WAIST_POUCH] = SoftcoreTestData.NewContainer(ItemTpl.SECURE_WAIST_POUCH, 1, 1);
        templates.Items[ItemTpl.SECURE_CONTAINER_GAMMA] = SoftcoreTestData.NewContainer(ItemTpl.SECURE_CONTAINER_GAMMA, 1, 1);
        templates.Profiles["standard"] = SoftcoreTestData.NewProfile(
            ItemTpl.SECURE_CONTAINER_GAMMA, ItemTpl.STASH_STANDARD_STASH_10X30, 1);
        templates.Quests[SoftcoreTestData.CollectorQuestId] = SoftcoreTestData.NewCollectorQuest();
        return templates;
    }

    private static HideoutTable BuildHideout()
    {
        var hideout = SoftcoreTestData.NewHideout();
        hideout.Areas!.Add(SoftcoreTestData.NewStashArea());
        return hideout;
    }

    private static TradersTable BuildTraders()
    {
        var traders = SoftcoreTestData.NewTraders();
        traders[Traders.PEACEKEEPER] = SoftcoreTestData.NewPeacekeeper(ItemTpl.SECURE_CONTAINER_BETA);
        return traders;
    }
}
