using System.Text.Json.Serialization;

namespace InescapableTarkovsSoftcore.Features.Softcore;

/// <summary>
/// 内嵌 ScavCase 数据表（源 TS assets/scavcase.ts 迁移）。
/// 数据文件以 EmbeddedResource 嵌入主工程，运行时与测试读同一资源。
/// </summary>
public static class ScavCaseResourceLoader
{
    internal const string ResourceName = "InescapableTarkovsSoftcore.softcore.scavcase.json";

    public static ScavCaseTables Load() => EmbeddedJsonResource<ScavCaseTables>.Load(ResourceName);
}

/// <summary>ScavCase 数据表根。</summary>
public sealed class ScavCaseTables
{
    [JsonPropertyName("rewardItemValueRangeRub")]
    public Dictionary<string, ScavCaseRange> RewardItemValueRangeRub { get; init; } = new(StringComparer.Ordinal);

    [JsonPropertyName("parentBlacklist")]
    public List<string> ParentBlacklist { get; init; } = [];

    [JsonPropertyName("itemBlacklist")]
    public List<string> ItemBlacklist { get; init; } = [];

    [JsonPropertyName("whitelist")]
    public List<string> Whitelist { get; init; } = [];

    [JsonPropertyName("recipes")]
    public List<ScavCaseRecipeEntry> Recipes { get; init; } = [];
}

/// <summary>价值区间（含 min/max）。</summary>
public sealed class ScavCaseRange
{
    [JsonPropertyName("min")]
    public double Min { get; init; }

    [JsonPropertyName("max")]
    public double Max { get; init; }
}

/// <summary>ScavCase 配方条目（扁平的 common/rare/superrare 数量）。</summary>
public sealed class ScavCaseRecipeEntry
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("requiredItem")]
    public string RequiredItem { get; init; } = string.Empty;

    [JsonPropertyName("productionTime")]
    public double ProductionTime { get; init; }

    [JsonPropertyName("common")]
    public ScavCaseRange Common { get; init; } = new();

    [JsonPropertyName("rare")]
    public ScavCaseRange Rare { get; init; } = new();

    [JsonPropertyName("superrare")]
    public ScavCaseRange Superrare { get; init; } = new();
}
