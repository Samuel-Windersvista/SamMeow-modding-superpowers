using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Hideout;

namespace InescapableTarkovsSoftcore.Features.Softcore;

/// <summary>
/// 制造数据表（源 TS assets/productionAdjustments.ts + assets/recipes.ts 迁移）。
/// 外部文件 config/softcore/crafting-*.json 优先，缺失从内嵌模板重建，解析失败回落内嵌。首次调用后缓存。
/// 再生成命令见 data/softcore/MANIFEST.md（scripts/tools/gen-crafting.mjs）。
/// </summary>
public static class CraftingResourceLoader
{
    internal const string RebalanceResourceName = "InescapableTarkovsSoftcore.softcore.crafting-rebalance.json";

    internal const string RebalanceRelativePath = "config/softcore/crafting-rebalance.json";

    internal const string RecipesResourceName = "InescapableTarkovsSoftcore.softcore.crafting-recipes.json";

    internal const string RecipesRelativePath = "config/softcore/crafting-recipes.json";

    private static readonly Lazy<CraftingRebalanceTable> CachedRebalance =
        new(() => ExternalJsonFile.LoadProduction(typeof(CraftingResourceLoader).Assembly, LoadRebalanceFrom));

    private static readonly Lazy<CraftingRecipesTable> CachedRecipes =
        new(() => ExternalJsonFile.LoadProduction(typeof(CraftingResourceLoader).Assembly, LoadRecipesFrom));

    /// <summary>配方重平衡表（47 条，按 endProduct 定位）。</summary>
    public static CraftingRebalanceTable LoadRebalance() => CachedRebalance.Value;

    /// <summary>新增配方表（12 条）。</summary>
    public static CraftingRecipesTable LoadRecipes() => CachedRecipes.Value;

    /// <summary>测试与外部调用入口：从显式目录加载重平衡表，告警写入调用方提供的列表。</summary>
    internal static CraftingRebalanceTable LoadRebalanceFrom(string modDirectory, List<string> warnings) =>
        ExternalJsonFile.ReadTable(
            modDirectory,
            RebalanceRelativePath,
            RebalanceResourceName,
            json => ExternalJsonFile.ParseObjectTolerant<CraftingRebalanceTable>(json, "制造配方重平衡表", warnings),
            warnings);

    /// <summary>测试与外部调用入口：从显式目录加载新增配方表，告警写入调用方提供的列表。</summary>
    internal static CraftingRecipesTable LoadRecipesFrom(string modDirectory, List<string> warnings) =>
        ExternalJsonFile.ReadTable(
            modDirectory,
            RecipesRelativePath,
            RecipesResourceName,
            json => ExternalJsonFile.ParseObjectTolerant<CraftingRecipesTable>(json, "新增制造配方表", warnings),
            warnings);
}

/// <summary>配方重平衡根。</summary>
public sealed class CraftingRebalanceTable
{
    [JsonPropertyName("recipeAdjustments")]
    public List<RecipeAdjustment> RecipeAdjustments { get; init; } = [];
}

/// <summary>单条配方调整：按 endProduct 定位 + 有序 ops。</summary>
public sealed class RecipeAdjustment
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 钉定的目标配方 id（可空）。非空时按 id 定位，消除「同 endProduct 多配方」的首条漂移；
    /// 见 data/softcore/MANIFEST.md 与 delta-table §4c。
    /// </summary>
    [JsonPropertyName("recipeId")]
    public string? RecipeId { get; init; }

    [JsonPropertyName("ops")]
    public List<AdjustmentOp> Ops { get; init; } = [];
}

/// <summary>
/// 声明式调整操作（源 TS 闭包的录制结果）。op 取值：
/// count / setAllCounts / setCount / replaceTemplate / setAreaLevel / replaceRequirements / pushRequirement。
/// </summary>
public sealed class AdjustmentOp
{
    [JsonPropertyName("op")]
    public string Op { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public double? Value { get; init; }

    [JsonPropertyName("templateId")]
    public string? TemplateId { get; init; }

    [JsonPropertyName("from")]
    public string? From { get; init; }

    [JsonPropertyName("to")]
    public string? To { get; init; }

    [JsonPropertyName("requirements")]
    public List<Requirement>? Requirements { get; init; }

    [JsonPropertyName("requirement")]
    public Requirement? Requirement { get; init; }
}

/// <summary>新增配方根。</summary>
public sealed class CraftingRecipesTable
{
    [JsonPropertyName("additionalRecipes")]
    public List<HideoutProduction> AdditionalRecipes { get; init; } = [];
}
