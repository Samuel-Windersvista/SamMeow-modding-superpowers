using System.Text.Json;

namespace InescapableTarkovsSoftcore.Features.TrueItems;

/// <summary>
/// 内嵌查找表加载器：从主工程程序集读取六张 True Items JSON（源 = 旧包 IMM 覆盖层，
/// 随程序集嵌入，运行时无需外部文件）。首次调用后缓存。
/// </summary>
public static class TrueItemsResourceLoader
{
    private const string ResourcePrefix = "InescapableTarkovsSoftcore.trueitems.";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = false
    };

    private static readonly Lazy<TrueItemsTables> Cached = new(LoadCore);

    /// <summary>读取并缓存六张表。</summary>
    public static TrueItemsTables Load() => Cached.Value;

    private static TrueItemsTables LoadCore() => new()
    {
        Barter = Read("barter"),
        Clothing = Read("clothing"),
        Keycards = Read("keycards"),
        Medicals = Read("medicals"),
        PartsnMods = Read("partsnmods"),
        Provisions = Read("provisions")
    };

    private static TrueItemsTable Read(string name)
    {
        var resourceName = $"{ResourcePrefix}{name}.json";
        using var stream = typeof(TrueItemsResourceLoader).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"缺少内嵌资源：{resourceName}");

        return JsonSerializer.Deserialize<TrueItemsTable>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"内嵌资源解析为空：{resourceName}");
    }
}
