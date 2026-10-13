using System.Text.Json;
using SPTarkov.Server.Core.Utils.Json.Converters;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// 内嵌 JSON 资源加载助手：按「类型 + 资源名」缓存，统一解析选项与异常文案。
/// 供各 ResourceLoader 复用（去重）。注册 SPT 的 <see cref="StringToMongoIdConverter"/>，
/// 使含 <c>MongoId</c> 字段的模型（如 HideoutProduction / Requirement）可直接反序列化。
/// </summary>
internal static class EmbeddedJsonResource<T> where T : class
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new StringToMongoIdConverter() }
    };

    private static readonly Dictionary<string, Lazy<T>> Cache = new(StringComparer.Ordinal);

    public static T Load(string resourceName)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(resourceName, out var lazy))
            {
                lazy = new Lazy<T>(() => Read(resourceName));
                Cache[resourceName] = lazy;
            }

            return lazy.Value;
        }
    }

    private static T Read(string resourceName)
    {
        var assembly = typeof(EmbeddedJsonResource<T>).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"缺少内嵌资源：{resourceName}");

        return JsonSerializer.Deserialize<T>(stream, Options)
            ?? throw new InvalidOperationException($"内嵌资源解析为空：{resourceName}");
    }
}
