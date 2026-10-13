using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-A 安全容器：新档 2×2 腰包起步、Peacekeeper 下架 Beta、邪教徒圈奖励改 Kappa、
/// 追加自定义升级配方，并把各安全容器放宽到 SURV 尺寸。
/// </summary>
public sealed class SecureContainersChanger : ISoftcoreChanger
{
    public string Name => "secureContainers";

    /// <summary>SURV 终态尺寸（cellsV = 高/行，cellsH = 宽/列）。</summary>
    private static readonly (string Template, int CellsV, int CellsH)[] Sizes =
    [
        (ItemTpl.SECURE_WAIST_POUCH, 2, 4),
        (ItemTpl.SECURE_CONTAINER_ALPHA, 3, 3),
        (ItemTpl.SECURE_CONTAINER_BETA, 3, 4),
        (ItemTpl.SECURE_CONTAINER_EPSILON, 3, 5),
        (ItemTpl.SECURE_CONTAINER_GAMMA, 4, 5),
        (ItemTpl.SECURE_CONTAINER_KAPPA, 5, 5)
    ];

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var options = context.Config.SecureContainersOptions;
        if (!options.Enabled)
        {
            return;
        }

        if (options.ProgressiveContainers.Enabled)
        {
            ApplyProgressive(context, log);
        }

        if (options.BiggerContainers)
        {
            ApplyBiggerContainers(context, log);
        }
    }

    private static void ApplyProgressive(SoftcoreContext context, SoftcoreChangeLog log)
    {
        // 新档起始安全容器 = 2×2 腰包。
        foreach (var profile in context.Tables.Templates.Profiles.Values)
        {
            SetStartingContainer(profile.Bear, log);
            SetStartingContainer(profile.Usec, log);
        }

        // Peacekeeper 下架 Beta：置零而非删除（删除 assort 项会导致问题）。
        if (context.Tables.Traders.TryGetValue(Traders.PEACEKEEPER, out var peacekeeper) && peacekeeper.Assort is not null)
        {
            foreach (var item in peacekeeper.Assort.Items.Where(item => item.Template == ItemTpl.SECURE_CONTAINER_BETA))
            {
                item.Upd ??= new Upd();
                item.Upd.UnlimitedCount = false;
                item.Upd.StackObjectsCount = 0;
                item.Upd.BuyRestrictionMax = 0;
                log.Changed();
            }
        }

        // 邪教徒圈：把「腰包」直接奖励替换为 Kappa。
        var rewards = context.Services.HideoutConfig.CultistCircle?.DirectRewards;
        if (rewards is not null)
        {
            foreach (var reward in rewards)
            {
                var index = reward.RequiredItems.IndexOf(ItemTpl.SECURE_WAIST_POUCH);
                if (index >= 0)
                {
                    reward.RequiredItems[index] = ItemTpl.SECURE_CONTAINER_KAPPA;
                    log.Changed();
                }
            }
        }

        // 追加自定义升级配方（幂等：按配方 Id 去重）。
        if (context.Tables.Hideout.Production?.Recipes is { } recipes)
        {
            foreach (var recipe in ContainerRecipes.All)
            {
                if (recipes.All(existing => existing.Id != recipe.Id))
                {
                    recipes.Add(recipe);
                    log.Changed();
                }
            }
        }
    }

    private static void ApplyBiggerContainers(SoftcoreContext context, SoftcoreChangeLog log)
    {
        foreach (var (template, cellsV, cellsH) in Sizes)
        {
            if (!SoftcoreGrids.TrySetCells(context.Tables.Templates, template, cellsV, cellsH))
            {
                log.Warn($"未找到安全容器 {template}，跳过");
                continue;
            }

            log.Changed();
        }
    }

    private static void SetStartingContainer(TemplateSide? side, SoftcoreChangeLog log)
    {
        var items = side?.Character?.Inventory?.Items;
        if (items is null)
        {
            return;
        }

        foreach (var item in items.Where(item =>
                     item.SlotId == "SecuredContainer" && item.Template != ItemTpl.SECURE_WAIST_POUCH))
        {
            item.Template = ItemTpl.SECURE_WAIST_POUCH;
            log.Changed();
        }
    }
}
