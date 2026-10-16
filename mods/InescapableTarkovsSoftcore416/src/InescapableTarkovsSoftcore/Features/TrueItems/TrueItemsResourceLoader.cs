namespace InescapableTarkovsSoftcore.Features.TrueItems;

/// <summary>
/// 查找表加载器：六张 True Items JSON（源 = 旧包 IMM 覆盖层）以外部文件优先、
/// 内嵌资源兜底（缺失重建 / 解析失败回落）。首次调用后缓存；静态入口委托 <see cref="ExternalJsonFile"/>。
/// </summary>
public static class TrueItemsResourceLoader
{
    private const string ResourcePrefix = "InescapableTarkovsSoftcore.trueitems.";

    private static readonly Lazy<TrueItemsTables> Cached =
        new(() => ExternalJsonFile.LoadProduction(typeof(TrueItemsResourceLoader).Assembly, LoadTables));

    /// <summary>读取并缓存六张表。</summary>
    public static TrueItemsTables Load() => Cached.Value;

    /// <summary>测试与外部调用入口：从显式目录加载六张表，告警写入调用方提供的列表。</summary>
    internal static TrueItemsTables LoadTables(string modDirectory, List<string> warnings) => new()
    {
        Barter = Read(modDirectory, "barter", warnings),
        Clothing = Read(modDirectory, "clothing", warnings),
        Keycards = Read(modDirectory, "keycards", warnings),
        Medicals = Read(modDirectory, "medicals", warnings),
        PartsnMods = Read(modDirectory, "partsnmods", warnings),
        Provisions = Read(modDirectory, "provisions", warnings)
    };

    private static TrueItemsTable Read(string modDirectory, string name, List<string> warnings) =>
        ExternalJsonFile.ReadTable(
            modDirectory,
            $"config/trueitems/{name}.json",
            $"{ResourcePrefix}{name}.json",
            json => ExternalJsonFile.ParseObjectTolerant<TrueItemsTable>(json, $"True Items 表 {name}", warnings),
            warnings);
}
