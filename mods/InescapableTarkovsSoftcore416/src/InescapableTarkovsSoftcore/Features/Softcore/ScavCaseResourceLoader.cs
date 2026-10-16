using System.Text.Json.Serialization;

namespace InescapableTarkovsSoftcore.Features.Softcore;

/// <summary>
/// ScavCase 数据表（源 TS assets/scavcase.ts 迁移）。外部文件 config/softcore/scavcase.json 优先，
/// 缺失从内嵌模板重建，解析失败回落内嵌。首次调用后缓存。
/// </summary>
public static class ScavCaseResourceLoader
{
    internal const string ResourceName = "InescapableTarkovsSoftcore.softcore.scavcase.json";

    internal const string RelativePath = "config/softcore/scavcase.json";

    private static readonly Lazy<ScavCaseTables> Cached =
        new(() => ExternalJsonFile.LoadProduction(typeof(ScavCaseResourceLoader).Assembly, LoadFrom));

    public static ScavCaseTables Load() => Cached.Value;

    /// <summary>测试与外部调用入口：从显式目录加载，告警写入调用方提供的列表。</summary>
    internal static ScavCaseTables LoadFrom(string modDirectory, List<string> warnings) =>
        ExternalJsonFile.ReadTable(
            modDirectory,
            RelativePath,
            ResourceName,
            json => ExternalJsonFile.ParseObjectTolerant<ScavCaseTables>(json, "ScavCase 表", warnings),
            warnings);
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
