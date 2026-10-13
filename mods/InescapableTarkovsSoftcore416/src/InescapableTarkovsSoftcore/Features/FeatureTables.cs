using System.Text.Json;
using System.Text.Json.Serialization;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// 内嵌功能查找表访问入口。表文件以 EmbeddedResource 嵌入主工程，
/// 运行时模块与测试读同一资源，避免数值在代码与文档间漂移。
/// </summary>
public static class FeatureTables
{
    internal const string ArmbandsResourceName = "InescapableTarkovsSoftcore.antigravArmbands.armbands.json";

    internal const string BackpacksResourceName = "InescapableTarkovsSoftcore.backpacks.backpacks.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>G4：22 款反重力臂章（id → 减重后的重量）。</summary>
    public static IReadOnlyList<ArmbandEntry> Armbands { get; } = ReadList<ArmbandEntry>(ArmbandsResourceName);

    /// <summary>G5：43 条背包扩容目标（id → 网格宽高）。</summary>
    public static IReadOnlyList<BackpackEntry> Backpacks { get; } = ReadList<BackpackEntry>(BackpacksResourceName);

    private static IReadOnlyList<T> ReadList<T>(string resourceName)
    {
        var assembly = typeof(FeatureTables).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"缺少内嵌资源：{resourceName}");
        return JsonSerializer.Deserialize<List<T>>(stream, Options) ?? [];
    }
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
