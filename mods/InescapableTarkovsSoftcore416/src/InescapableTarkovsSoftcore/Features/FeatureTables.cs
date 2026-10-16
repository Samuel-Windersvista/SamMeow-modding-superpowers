using System.Text.Json.Serialization;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// 功能查找表访问入口。表数据以外部文件（<c>config/</c> 子树）优先，缺失时从内嵌资源重建；
/// 运行时模块与测试读同一资源，避免数值在代码与文档间漂移。
/// </summary>
public static class FeatureTables
{
    internal const string ArmbandsResourceName = "InescapableTarkovsSoftcore.antigravArmbands.armbands.json";

    internal const string ArmbandsRelativePath = "config/armbands.json";

    internal const string BackpacksResourceName = "InescapableTarkovsSoftcore.backpacks.backpacks.json";

    internal const string BackpacksRelativePath = "config/backpacks.json";

    /// <summary>G4：反重力臂章查找表（id → 减重后的重量）。</summary>
    public static IReadOnlyList<ArmbandEntry> Armbands { get; } =
        ExternalJsonFile.LoadProduction(typeof(FeatureTables).Assembly, ReadArmbands);

    /// <summary>G5：背包扩容查找表（id → 网格宽高）。</summary>
    public static IReadOnlyList<BackpackEntry> Backpacks { get; } =
        ExternalJsonFile.LoadProduction(typeof(FeatureTables).Assembly, ReadBackpacks);

    /// <summary>测试与外部调用入口：从显式目录加载臂章表，告警写入调用方提供的列表。</summary>
    internal static IReadOnlyList<ArmbandEntry> ReadArmbands(string modDirectory, List<string> warnings) =>
        ExternalJsonFile.ReadTable(
            modDirectory,
            ArmbandsRelativePath,
            ArmbandsResourceName,
            json => ExternalJsonFile.ParseArray<ArmbandEntry>(json, $"臂章表 {ArmbandsRelativePath}", warnings),
            warnings);

    /// <summary>测试与外部调用入口：从显式目录加载背包表，告警写入调用方提供的列表。</summary>
    internal static IReadOnlyList<BackpackEntry> ReadBackpacks(string modDirectory, List<string> warnings) =>
        ExternalJsonFile.ReadTable(
            modDirectory,
            BackpacksRelativePath,
            BackpacksResourceName,
            json => ExternalJsonFile.ParseArray<BackpackEntry>(json, $"背包表 {BackpacksRelativePath}", warnings),
            warnings);
}

/// <summary>反重力臂章条目。</summary>
public sealed class ArmbandEntry
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("weight")]
    public double Weight { get; init; }
}

/// <summary>背包扩容条目。</summary>
public sealed class BackpackEntry
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("cellsH")]
    public int CellsH { get; init; }

    [JsonPropertyName("cellsV")]
    public int CellsV { get; init; }
}
