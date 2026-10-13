using System.Text.Json.Serialization;

namespace InescapableTarkovsSoftcore.Features.Softcore;

/// <summary>
/// 内嵌跳蚤市场数据表（源 TS assets/fleamarket.ts + keys.ts + itemBaseClasses.ts 迁移）。
/// 数据文件以 EmbeddedResource 嵌入主工程，运行时与测试读同一资源。
/// </summary>
public static class FleaMarketResourceLoader
{
    internal const string ResourceName = "InescapableTarkovsSoftcore.softcore.fleamarket.json";

    public static FleaMarketTables Load() => EmbeddedJsonResource<FleaMarketTables>.Load(ResourceName);
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
