using SPTarkov.Server.Core.Models.Enums;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-B 制造加速：全局配方时间 = ceil(原时间 / 倍率)（排除比特币/月光酒/纯净水），
/// 月光酒 0.3、纯净水 0.3、邪教徒圈 0.5，以及藏身处管理技能经验修复。
/// </summary>
public sealed class FasterCraftingTimeChanger : ISoftcoreChanger
{
    public string Name => "fasterCraftingTime";

    /// <summary>全局加速需要单独处理、不参与 divide 的成品。</summary>
    private static readonly HashSet<string> ExcludedFromGlobal =
    [
        ItemTpl.BARTER_PHYSICAL_BITCOIN,
        ItemTpl.DRINK_BOTTLE_OF_FIERCE_HATCHLING_MOONSHINE,
        ItemTpl.DRINK_CANISTER_WITH_PURIFIED_WATER
    ];

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var options = context.Config.FasterCraftingTime;
        if (!options.Enabled)
        {
            return;
        }

        ApplyAllRecipes(context, options.BaseCraftingTimeMultiplier, log);

        if (options.HideoutSkillExpFix.Enabled)
        {
            ApplyHideoutSkillExpFix(context, options.HideoutSkillExpFix.HideoutSkillExpMultiplier, log);
        }

        if (options.FasterMoonshineProduction.Enabled)
        {
            ApplyForEndProduct(context, ItemTpl.DRINK_BOTTLE_OF_FIERCE_HATCHLING_MOONSHINE, options.FasterMoonshineProduction.BaseCraftingTimeMultiplier, log);
        }

        if (options.FasterPurifiedWaterProduction.Enabled)
        {
            ApplyForEndProduct(context, ItemTpl.DRINK_CANISTER_WITH_PURIFIED_WATER, options.FasterPurifiedWaterProduction.BaseCraftingTimeMultiplier, log);
        }

        if (options.FasterCultistCircle.Enabled)
        {
            ApplyCultistCircle(context, options.FasterCultistCircle.BaseCraftingTimeMultiplier, log);
        }
    }

    private static void ApplyAllRecipes(SoftcoreContext context, double multiplier, SoftcoreChangeLog log)
    {
        if (!SoftcoreTime.TryMultiplier(multiplier, "baseCraftingTimeMultiplier", log)
            || !TryGetRecipes(context, log, out var recipes))
        {
            return;
        }

        foreach (var recipe in recipes!.Where(recipe => !ExcludedFromGlobal.Contains((string)recipe.EndProduct)))
        {
            // 4.1.6 ProductionTime 为可空 double?；null 跳过、不计数（不虚增变更数）。
            if (recipe.ProductionTime is { } time)
            {
                recipe.ProductionTime = SoftcoreTime.ScaleCeil(time, multiplier);
                log.Changed();
            }
        }
    }

    private static void ApplyForEndProduct(SoftcoreContext context, string endProduct, double multiplier, SoftcoreChangeLog log)
    {
        if (!SoftcoreTime.TryMultiplier(multiplier, $"fasterProduction[{endProduct}]", log)
            || !TryGetRecipes(context, log, out var recipes))
        {
            return;
        }

        foreach (var recipe in recipes!.Where(recipe => recipe.EndProduct == endProduct))
        {
            // 4.1.6 ProductionTime 为可空 double?；null 跳过、不计数（不虚增变更数）。
            if (recipe.ProductionTime is { } time)
            {
                recipe.ProductionTime = SoftcoreTime.ScaleCeil(time, multiplier);
                log.Changed();
            }
        }
    }

    private static void ApplyHideoutSkillExpFix(SoftcoreContext context, double multiplier, SoftcoreChangeLog log)
    {
        if (!SoftcoreTime.TryMultiplier(multiplier, "hideoutSkillExpMultiplier", log))
        {
            return;
        }

        // 源 TS：hoursForSkillCrafting /= multiplier；SPT5 字段为 int。
        // clamp ≥ 1：防止过大倍率得 0，触发 core 除零（Inf/NaN 经验损坏）。
        var hours = context.Services.HideoutConfig.HoursForSkillCrafting;
        context.Services.HideoutConfig.HoursForSkillCrafting = Math.Max(1, (int)(hours / multiplier));
        log.Changed();
    }

    private static void ApplyCultistCircle(SoftcoreContext context, double multiplier, SoftcoreChangeLog log)
    {
        if (!SoftcoreTime.TryMultiplier(multiplier, "fasterCultistCircle", log))
        {
            return;
        }

        var circle = context.Services.HideoutConfig.CultistCircle;
        if (circle is null)
        {
            log.Warn("未找到 cultistCircle 配置，跳过");
            return;
        }

        circle.HideoutTaskRewardTimeSeconds = (int)SoftcoreTime.ScaleCeil(circle.HideoutTaskRewardTimeSeconds, multiplier);

        if (circle.CraftTimeThresholds is not null)
        {
            foreach (var threshold in circle.CraftTimeThresholds)
            {
                threshold.CraftTimeSeconds = (int)SoftcoreTime.ScaleCeil(threshold.CraftTimeSeconds, multiplier);
            }
        }

        if (circle.DirectRewards is not null)
        {
            foreach (var reward in circle.DirectRewards)
            {
                reward.CraftTimeSeconds = (int)SoftcoreTime.ScaleCeil(reward.CraftTimeSeconds, multiplier);
            }
        }

        log.Changed();
    }

    private static bool TryGetRecipes(
        SoftcoreContext context,
        SoftcoreChangeLog log,
        out List<SPTarkov.Server.Core.Models.Eft.Hideout.HideoutProduction>? recipes)
    {
        recipes = context.Tables.Hideout.Production?.Recipes;
        if (recipes is not null)
        {
            return true;
        }

        log.Warn("未找到 hideout.production.recipes，跳过");
        return false;
    }
}
