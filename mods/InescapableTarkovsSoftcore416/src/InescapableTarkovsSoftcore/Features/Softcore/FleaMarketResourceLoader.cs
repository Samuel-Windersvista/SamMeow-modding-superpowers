using System.Text.Json.Serialization;

namespace InescapableTarkovsSoftcore.Features.Softcore;

/// <summary>
/// 跳蚤市场数据表（源 TS assets/fleamarket.ts + keys.ts + itemBaseClasses.ts 迁移）。
/// 外部文件 config/softcore/fleamarket.json 优先，缺失从内嵌模板重建，解析失败回落内嵌。首次调用后缓存。
/// </summary>
public static class FleaMarketResourceLoader
{
    internal const string ResourceName = "InescapableTarkovsSoftcore.softcore.fleamarket.json";

    internal const string RelativePath = "config/softcore/fleamarket.json";

    private static readonly Lazy<FleaMarketTables> Cached =
        new(() => ExternalJsonFile.LoadProduction(typeof(FleaMarketResourceLoader).Assembly, LoadFrom));

    public static FleaMarketTables Load() => Cached.Value;

    /// <summary>测试与外部调用入口：从显式目录加载，告警写入调用方提供的列表。</summary>
    internal static FleaMarketTables LoadFrom(string modDirectory, List<string> warnings) =>
        ExternalJsonFile.ReadTable(
            modDirectory,
            RelativePath,
            ResourceName,
            json => ExternalJsonFile.ParseObjectTolerant<FleaMarketTables>(json, "跳蚤市场表", warnings),
            warnings);
}

/// <summary>跳蚤市场数据表根。</summary>
public sealed class FleaMarketTables
{
    [JsonPropertyName("whitelist")]
    public List<string> Whitelist { get; init; } = [];

    [JsonPropertyName("actualBaseClasses")]
    public List<string> ActualBaseClasses { get; init; } = [];

    [JsonPropertyName("fleaBarterRequestWhitelist")]
    public List<string> FleaBarterRequestWhitelist { get; init; } = [];

    [JsonPropertyName("requestWhitelist")]
    public Dictionary<string, double> RequestWhitelist { get; init; } = new(StringComparer.Ordinal);

    [JsonPropertyName("fleaListingsWhitelistHandBook")]
    public List<string> FleaListingsWhitelistHandBook { get; init; } = [];

    [JsonPropertyName("pacifistFenceItemBaseWhitelist")]
    public List<string> PacifistFenceItemBaseWhitelist { get; init; } = [];

    [JsonPropertyName("bsgBlacklist")]
    public List<string> BsgBlacklist { get; init; } = [];

    [JsonPropertyName("itemBaseClasses")]
    public List<string> ItemBaseClasses { get; init; } = [];

    [JsonPropertyName("questKeys")]
    public List<string> QuestKeys { get; init; } = [];

    [JsonPropertyName("markedKeys")]
    public List<string> MarkedKeys { get; init; } = [];
}
