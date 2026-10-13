using InescapableTarkovsSoftcore.Features.Softcore;
using InescapableTarkovsSoftcore.Features.Softcore.Changers;
using SPTarkov.Server.Core.Models.Enums;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class SoftcoreHideoutContainersTests
{
    [Fact]
    public void BiggerHideoutContainers_ApplySurvOverrideSizes()
    {
        var templates = SoftcoreTestData.NewTemplates();
        foreach (var template in new[]
                 {
                     ItemTpl.CONTAINER_MEDICINE_CASE, ItemTpl.CONTAINER_MR_HOLODILNICK_THERMAL_BAG,
                     ItemTpl.CONTAINER_MAGAZINE_CASE, ItemTpl.CONTAINER_ITEM_CASE,
                     ItemTpl.CONTAINER_WEAPON_CASE, ItemTpl.CONTAINER_KEY_TOOL,
                     ItemTpl.CONTAINER_THICC_WEAPON_CASE, ItemTpl.CONTAINER_THICC_ITEM_CASE
                 })
        {
            templates.Items[template] = SoftcoreTestData.NewContainer(template, 1, 1);
        }

        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());

        new HideoutContainersChanger().Apply(context, new SoftcoreChangeLog());

        // SURV 覆盖终态（cellsV, cellsH）：药品 10×10 / Holo 10×10 / 弹匣 7×10（V7·H10）/
        // 物品 6×6 / 武器 6×7（V6·H7）/ 钥匙工具 5×5 / THICC 武器与 THICC 物品 6×14（V6·H14）。
        SoftcoreTestData.AssertSize(templates, ItemTpl.CONTAINER_MEDICINE_CASE, cellsV: 10, cellsH: 10);
        SoftcoreTestData.AssertSize(templates, ItemTpl.CONTAINER_MR_HOLODILNICK_THERMAL_BAG, cellsV: 10, cellsH: 10);
        SoftcoreTestData.AssertSize(templates, ItemTpl.CONTAINER_MAGAZINE_CASE, cellsV: 7, cellsH: 10);
        SoftcoreTestData.AssertSize(templates, ItemTpl.CONTAINER_ITEM_CASE, cellsV: 6, cellsH: 6);
        SoftcoreTestData.AssertSize(templates, ItemTpl.CONTAINER_WEAPON_CASE, cellsV: 6, cellsH: 7);
        SoftcoreTestData.AssertSize(templates, ItemTpl.CONTAINER_KEY_TOOL, cellsV: 5, cellsH: 5);
        SoftcoreTestData.AssertSize(templates, ItemTpl.CONTAINER_THICC_WEAPON_CASE, cellsV: 6, cellsH: 14);
        SoftcoreTestData.AssertSize(templates, ItemTpl.CONTAINER_THICC_ITEM_CASE, cellsV: 6, cellsH: 14);
    }

    [Fact]
    public void SiccCaseBuff_MergesDocsFilterAndAllowsKeyTool()
    {
        const string docsA = "00000000000000000000a001";
        const string docsB = "00000000000000000000a002";
        const string siccC = "00000000000000000000a003";

        var templates = SoftcoreTestData.NewTemplates();
        templates.Items[ItemTpl.CONTAINER_DOCUMENTS_CASE] = SoftcoreTestData.NewContainer(
            ItemTpl.CONTAINER_DOCUMENTS_CASE, 3, 3, docsA, docsB);
        templates.Items[ItemTpl.CONTAINER_SICC] = SoftcoreTestData.NewContainer(
            ItemTpl.CONTAINER_SICC, 3, 3, docsB, siccC);
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig());

        new HideoutContainersChanger().Apply(context, new SoftcoreChangeLog());

        var filter = templates.Items[ItemTpl.CONTAINER_SICC].Properties!.Grids!.First().Properties!.Filters!.First().Filter!;
        var values = filter.Select(item => (string)item).ToHashSet();
        Assert.Equal(new HashSet<string> { docsA, docsB, siccC, ItemTpl.CONTAINER_KEY_TOOL }, values);
    }

    [Fact]
    public void Disabled_ProducesZeroChanges()
    {
        var templates = SoftcoreTestData.NewTemplates();
        templates.Items[ItemTpl.CONTAINER_MEDICINE_CASE] = SoftcoreTestData.NewContainer(ItemTpl.CONTAINER_MEDICINE_CASE, 7, 7);
        var config = new Config.SoftcoreModuleConfig();
        config.HideoutContainers.Enabled = false;
        var context = SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(), SoftcoreTestData.NewHideoutConfig(), config);

        var log = new SoftcoreChangeLog();
        new HideoutContainersChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Equal(7, templates.Items[ItemTpl.CONTAINER_MEDICINE_CASE].Properties!.Grids!.First().Properties!.CellsV);
    }
}
