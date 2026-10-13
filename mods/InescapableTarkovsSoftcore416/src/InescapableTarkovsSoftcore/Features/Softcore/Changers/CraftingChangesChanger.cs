using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Common;
using Requirement = SPTarkov.Server.Core.Models.Eft.Hideout.Requirement;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-D 制造更改：47 条配方重平衡 + 12 条新增配方。数据来自内嵌
/// <see cref="CraftingResourceLoader"/>（源 TS assets/productionAdjustments.ts + recipes.ts）。
/// <para>
/// 顺序约束（与源 <c>Softcore.ts</c> 等效）：本变换器注册在 <see cref="FasterCraftingTimeChanger"/> 之后，
/// 故新增配方不在全局 ÷3 范围内，其 productionTime 保持源值。
/// </para>
/// <para>
/// 配方定位：优先按 <see cref="RecipeAdjustment.RecipeId"/> 钉定的配方 id（消除 SPT5 同 endProduct
/// 多配方时的首条漂移），否则按 endProduct；两者均排除 <c>ChristmasIllumination</c>。
/// </para>
/// <para>
/// B3 兜底：加载新增配方时检测重复 endProduct，告警并去重（仅约束本 mod 自带的配方资源；
/// SPT5 原版数据库中同一 endProduct 存在合法的多配方——例如同一物品在厨房/营养站各有一条）。
/// </para>
/// </summary>
public sealed class CraftingChangesChanger : ISoftcoreChanger
{
    public string Name => "craftingChanges";

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log) =>
        Apply(context, log, CraftingResourceLoader.LoadRebalance, CraftingResourceLoader.LoadRecipes);

    /// <summary>
    /// 测试接缝：注入外置数据表（生产路径经 <see cref="CraftingResourceLoader"/> 读内嵌资源）。
    /// </summary>
    public void Apply(
        SoftcoreContext context,
        SoftcoreChangeLog log,
        CraftingRebalanceTable rebalance,
        CraftingRecipesTable additionalRecipes) =>
        Apply(context, log, () => rebalance, () => additionalRecipes);

    private static void Apply(
        SoftcoreContext context,
        SoftcoreChangeLog log,
        Func<CraftingRebalanceTable> rebalance,
        Func<CraftingRecipesTable> additionalRecipes)
    {
        var options = context.Config.CraftingChanges;
        if (!options.Enabled)
        {
            return;
        }

        if (context.Tables.Hideout.Production?.Recipes is not { } recipes)
        {
            log.Warn("未找到 hideout.production.recipes，跳过 craftingChanges");
            return;
        }

        if (options.CraftingRebalance)
        {
            ApplyRebalance(recipes, rebalance(), log);
        }

        if (options.AdditionalCraftingRecipes)
        {
            ApplyAdditionalRecipes(recipes, additionalRecipes(), log);
        }
    }

    private static void ApplyRebalance(
        List<HideoutProduction> recipes,
        CraftingRebalanceTable rebalance,
        SoftcoreChangeLog log)
    {
        foreach (var adjustment in rebalance.RecipeAdjustments)
        {
            var craft = LocateRecipe(recipes, adjustment);
            if (craft is null)
            {
                var pinned = adjustment.RecipeId is { } id ? $"（钉定 id {id}）" : string.Empty;
                log.Warn($"未找到配方 {adjustment.Id}{pinned}，跳过");
                continue;
            }

            foreach (var op in adjustment.Ops)
            {
                switch (ApplyOp(craft, op))
                {
                    case CraftingOpResult.UnknownOp:
                        log.Warn($"配方 {adjustment.Id} 未知操作：{op.Op}");
                        break;
                    case CraftingOpResult.MissingField:
                        log.Warn($"配方 {adjustment.Id} 操作 {op.Op} 缺失必要字段，跳过");
                        break;
                }
            }

            log.Changed();
        }
    }

    /// <summary>定位目标配方：优先钉定 id，否则按 endProduct；均排除圣诞照明（源 CHRISTMAS_TREE）。</summary>
    private static HideoutProduction? LocateRecipe(List<HideoutProduction> recipes, RecipeAdjustment adjustment)
    {
        if (adjustment.RecipeId is { } pinned)
        {
            return recipes.FirstOrDefault(recipe =>
                recipe.Id == pinned && recipe.AreaType != HideoutAreas.ChristmasIllumination);
        }

        return recipes.FirstOrDefault(recipe =>
            recipe.EndProduct == adjustment.Id && recipe.AreaType != HideoutAreas.ChristmasIllumination);
    }

    private static CraftingOpResult ApplyOp(HideoutProduction craft, AdjustmentOp op)
    {
        // 4.1.6 HideoutProduction.Requirements 为可空；缺失时置空列表（与源「无需求」语义一致）。
        var requirements = craft.Requirements ??= [];

        switch (op.Op)
        {
            case "count":
                if (op.Value is not { } countValue)
                {
                    return CraftingOpResult.MissingField;
                }

                craft.Count = Scale(countValue);
                return CraftingOpResult.Applied;

            case "setAllCounts":
                if (op.Value is not { } allValue)
                {
                    return CraftingOpResult.MissingField;
                }

                foreach (var requirement in requirements.Where(r => r.Count is { } count && count != 0))
                {
                    requirement.Count = Scale(allValue);
                }

                return CraftingOpResult.Applied;

            case "setCount":
                if (op.TemplateId is not { } countTarget || op.Value is not { } setValue)
                {
                    return CraftingOpResult.MissingField;
                }

                // 源 TS 用 find → 仅首条命中。
                var countRequirement = requirements.FirstOrDefault(r => r.TemplateId is { } t && t == countTarget);
                if (countRequirement is not null)
                {
                    countRequirement.Count = Scale(setValue);
                }

                return CraftingOpResult.Applied;

            case "replaceTemplate":
                if (op.From is not { } from || op.To is not { } to)
                {
                    return CraftingOpResult.MissingField;
                }

                var replaceRequirement = requirements.FirstOrDefault(r => r.TemplateId is { } t && t == from);
                if (replaceRequirement is not null)
                {
                    replaceRequirement.TemplateId = to;
                }

                return CraftingOpResult.Applied;

            case "setAreaLevel":
                if (op.Value is not { } levelValue)
                {
                    return CraftingOpResult.MissingField;
                }

                var areaRequirement = requirements.FirstOrDefault(r => r.Type == "Area");
                if (areaRequirement is not null)
                {
                    areaRequirement.RequiredLevel = Scale(levelValue);
                }

                return CraftingOpResult.Applied;

            case "replaceRequirements":
                if (op.Requirements is not { } replacement)
                {
                    return CraftingOpResult.MissingField;
                }

                craft.Requirements = replacement.Select(Clone).ToList();
                return CraftingOpResult.Applied;

            case "pushRequirement":
                if (op.Requirement is not { } pushed)
                {
                    return CraftingOpResult.MissingField;
                }

                requirements.Add(Clone(pushed));
                return CraftingOpResult.Applied;

            default:
                return CraftingOpResult.UnknownOp;
        }
    }

    private static void ApplyAdditionalRecipes(
        List<HideoutProduction> recipes,
        CraftingRecipesTable additionalRecipes,
        SoftcoreChangeLog log)
    {
        // B3 兜底：先在资源内部按 endProduct 去重（保留首条），再逐条加入。
        var unique = CraftingRecipeGuard.DedupeByEndProduct(additionalRecipes.AdditionalRecipes, log);

        foreach (var recipe in unique)
        {
            // 幂等：同 Id 配方已存在则跳过（支持重复 Apply）。
            if (recipes.Any(existing => existing.Id == recipe.Id))
            {
                continue;
            }

            recipes.Add(Clone(recipe));
            log.Changed();
        }
    }

    private static int Scale(double value) => (int)SoftcoreTime.JsRound(value);

    private static Requirement Clone(Requirement requirement) => new()
    {
        TemplateId = requirement.TemplateId,
        Count = requirement.Count,
        IsEncoded = requirement.IsEncoded,
        IsFunctional = requirement.IsFunctional,
        AreaType = requirement.AreaType,
        RequiredLevel = requirement.RequiredLevel,
        Resource = requirement.Resource,
        QuestId = requirement.QuestId,
        IsSpawnedInSession = requirement.IsSpawnedInSession,
        GameVersions = requirement.GameVersions,
        Type = requirement.Type
    };

    private static HideoutProduction Clone(HideoutProduction recipe) => new()
    {
        Id = recipe.Id,
        AreaType = recipe.AreaType,
        Requirements = recipe.Requirements?.Select(Clone).ToList() ?? [],
        ProductionTime = recipe.ProductionTime,
        EndProduct = recipe.EndProduct,
        IsEncoded = recipe.IsEncoded,
        Locked = recipe.Locked,
        NeedFuelForAllProductionTime = recipe.NeedFuelForAllProductionTime,
        Continuous = recipe.Continuous,
        Count = recipe.Count,
        ProductionLimitCount = recipe.ProductionLimitCount,
        IsCodeProduction = recipe.IsCodeProduction
    };

    /// <summary>op 应用结果：用于区分「未知 op」与「字段缺失」的告警文案。</summary>
    private enum CraftingOpResult
    {
        Applied,
        UnknownOp,
        MissingField
    }
}
