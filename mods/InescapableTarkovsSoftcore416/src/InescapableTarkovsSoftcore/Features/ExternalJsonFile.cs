using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Utils.Json.Converters;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// 外部数据表加载助手（三套加载器共享的唯一接缝）。
/// 语义与 <c>config.json</c> 同构：外部文件优先 → 缺失时从内嵌模板重建（一行中文告警）→
/// 解析失败告警并回落内嵌（且不覆写用户文件）。支持 JSONC（<c>//</c> 注释与尾随逗号）。
/// 另提供：条目级容错解析（对象表数组/字典字段逐项、数组表逐元素，单条坏跳过）；
/// 生产加载引导样板（定位目录 + 入队告警）；静态 pending 告警队列，
/// 由 <see cref="ModuleOrchestrator"/> 在编排完成后统一经注入日志器输出（STD-LOG-001）。
/// </summary>
internal static class ExternalJsonFile
{
    /// <summary>与既有各解析器一致的 JSONC 绑定选项；含 SPT MongoId 转换器（HideoutProduction 等模型需要）。</summary>
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new StringToMongoIdConverter() }
    };

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>与 SerializerOptions 一致的大小写策略：JsonNode 字段定位（TryGetPropertyValue）不区分大小写。</summary>
    private static readonly JsonNodeOptions NodeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly object PendingLock = new();

    private static readonly List<string> Pending = [];

    /// <summary>把加载期告警入队（供编排器稍后统一输出）。</summary>
    public static void AddPendingWarnings(IEnumerable<string> warnings)
    {
        lock (PendingLock)
        {
            Pending.AddRange(warnings);
        }
    }

    /// <summary>排空并返回全部待输出告警；空队列返回空列表（no-op）。</summary>
    public static IReadOnlyList<string> DrainPendingWarnings()
    {
        lock (PendingLock)
        {
            if (Pending.Count == 0)
            {
                return [];
            }

            var drained = Pending.ToArray();
            Pending.Clear();
            return drained;
        }
    }

    /// <summary>
    /// 静态上下文无 DI，无法取 ModHelper；回退程序集所在目录（空则 <see cref="AppContext.BaseDirectory"/>）。
    /// 生产环境下即 DLL 所在 mod 目录（user/mods/com.sammeow.inescapable-softcore）。
    /// </summary>
    public static string ResolveModDirectory(Assembly assembly)
    {
        var directory = Path.GetDirectoryName(assembly.Location);
        return string.IsNullOrWhiteSpace(directory) ? AppContext.BaseDirectory : directory;
    }

    /// <summary>
    /// 生产加载入口样板：定位 mod 目录 → 执行 <paramref name="load"/> → 把加载期告警入 pending 队列。
    /// 收敛各加载器静态入口重复的「new List → ResolveModDirectory → AddPendingWarnings」引导逻辑。
    /// </summary>
    public static T LoadProduction<T>(Assembly assembly, Func<string, List<string>, T> load)
    {
        var warnings = new List<string>();
        var result = load(ResolveModDirectory(assembly), warnings);
        AddPendingWarnings(warnings);
        return result;
    }

    /// <summary>
    /// 获取数据表文本：外部存在 → 读取（JSON 语法非法则告警并回落内嵌，不覆写用户文件）；
    /// 缺失 → 读取内嵌模板 + 尝试写盘重建（一行告警），返回内嵌文本。
    /// </summary>
    public static string ReadTableText(
        string modDirectory,
        string relativePath,
        string embeddedResourceName,
        List<string> warnings)
    {
        var path = Path.Combine(modDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(path))
        {
            var external = File.ReadAllText(path);
            if (IsValidJson(external))
            {
                return external;
            }

            warnings.Add($"[ITS] 外置数据表 {relativePath} 解析失败，已回落内嵌默认（未覆写用户文件）：{path}");
            return ReadEmbeddedText(embeddedResourceName);
        }

        var template = ReadEmbeddedText(embeddedResourceName);
        warnings.Add($"[ITS] 未找到外置数据表 {relativePath}，已从内嵌默认模板重建：{path}");
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, template, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            warnings.Add($"[ITS] 重建外置数据表 {relativePath} 失败：{ex.Message}");
        }

        return template;
    }

    /// <summary>
    /// 泛型加载：取文本后按 <paramref name="parse"/> 绑定；若绑定失败（解析/格式非法）则告警并回落内嵌模板。
    /// </summary>
    public static T ReadTable<T>(
        string modDirectory,
        string relativePath,
        string embeddedResourceName,
        Func<string, T> parse,
        List<string> warnings)
        where T : class
    {
        var text = ReadTableText(modDirectory, relativePath, embeddedResourceName, warnings);
        try
        {
            return parse(text);
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            // 过滤即容错边界：仅吞「解析/格式非法」（含坏 hex 的 MongoId 转换）；其余异常（如 NullReference）继续向上抛出。
            warnings.Add($"[ITS] 外置数据表 {relativePath} 结构非法，已回落内嵌默认（未覆写用户文件）：{ex.Message}");
            return parse(ReadEmbeddedText(embeddedResourceName));
        }
    }

    /// <summary>
    /// 对象型数据表条目级容错解析：对象其余字段正常绑定；模型的 <c>List&lt;E&gt;</c> 字段逐元素、
    /// 字符串键 <c>Dictionary&lt;string,V&gt;</c> 字段逐键试绑定，单条/单键非法 → 告警并跳过，其余照常；
    /// 嵌套结构以一级元素为单位。根节点非对象则抛出（由 <see cref="ReadTable{T}"/> 兜底）。
    /// </summary>
    public static T ParseObjectTolerant<T>(string json, string label, List<string> warnings)
        where T : class
    {
        var node = JsonNode.Parse(json, NodeOptions, DocumentOptions)
            ?? throw new JsonException("根节点为空");
        if (node is not JsonObject obj)
        {
            throw new JsonException("根节点不是对象");
        }

        foreach (var property in typeof(T).GetProperties())
        {
            var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
            if (!obj.TryGetPropertyValue(name, out var child) || child is null)
            {
                continue;
            }

            var propertyType = property.PropertyType;
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (child is JsonArray array)
                {
                    PruneArrayElements(array, propertyType.GetGenericArguments()[0], $"{label} 字段 {name}", warnings);
                }
            }
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyType = propertyType.GetGenericArguments()[0];
                var valueType = propertyType.GetGenericArguments()[1];
                if (keyType == typeof(string) && child is JsonObject map)
                {
                    PruneDictionaryEntries(map, valueType, $"{label} 字段 {name}", warnings);
                }
            }
        }

        return obj.Deserialize<T>(SerializerOptions) ?? throw new JsonException("反序列化结果为空");
    }

    /// <summary>
    /// 数组型数据表条目级容错解析：逐元素反序列化，单条非法 → 告警并跳过，其余照常。
    /// 根节点非数组则抛出（由 <see cref="ReadTable{T}"/> 兜底）。
    /// </summary>
    public static List<T> ParseArray<T>(string json, string label, List<string> warnings)
        where T : class
    {
        var array = JsonNode.Parse(json, NodeOptions, DocumentOptions) as JsonArray
            ?? throw new JsonException("根节点不是数组");

        var list = new List<T>(array.Count);
        foreach (var node in array)
        {
            try
            {
                list.Add(node.Deserialize<T>(SerializerOptions) ?? throw new JsonException("条目为空"));
            }
            catch (Exception ex) when (ex is JsonException or FormatException)
            {
                // 过滤即容错边界：仅跳过数据非法的条目（解析/格式错误）；真 bug 继续向上抛出。
                warnings.Add($"[ITS] {label} 存在非法条目，已跳过：{ex.Message}");
            }
        }

        return list;
    }

    /// <summary>读取主工程内嵌资源原文。</summary>
    public static string ReadEmbeddedText(string embeddedResourceName)
    {
        var assembly = typeof(ExternalJsonFile).Assembly;
        using var stream = assembly.GetManifestResourceStream(embeddedResourceName)
            ?? throw new InvalidOperationException($"缺少内嵌资源：{embeddedResourceName}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>倒序删除数组中无法绑定到元素类型的条目（单条坏 → 告警并跳过）。</summary>
    private static void PruneArrayElements(JsonArray array, Type elementType, string label, List<string> warnings)
    {
        for (var index = array.Count - 1; index >= 0; index--)
        {
            try
            {
                _ = Bind(array[index], elementType);
            }
            catch (Exception ex) when (ex is JsonException or FormatException)
            {
                // 过滤即容错边界：仅数据非法的条目不参与整表回落。
                warnings.Add($"[ITS] {label} 存在非法条目，已跳过：{ex.Message}");
                array.RemoveAt(index);
            }
        }
    }

    /// <summary>删除对象中无法绑定到值类型的键（单键坏 → 告警并跳过）。</summary>
    private static void PruneDictionaryEntries(JsonObject map, Type valueType, string label, List<string> warnings)
    {
        foreach (var key in map.Select(pair => pair.Key).ToList())
        {
            try
            {
                _ = Bind(map[key], valueType);
            }
            catch (Exception ex) when (ex is JsonException or FormatException)
            {
                // 过滤即容错边界：仅数据非法的键不参与整表回落。
                warnings.Add($"[ITS] {label} 键 {key} 值非法，已跳过：{ex.Message}");
                map.Remove(key);
            }
        }
    }

    /// <summary>条目绑定：JSON 字面 null 与绑定结果为空一律视为该条目非法（抛 JsonException 由剪枝层跳过）。</summary>
    private static object? Bind(JsonNode? node, Type type)
    {
        if (node is null)
        {
            throw new JsonException("条目为空");
        }

        return JsonSerializer.Deserialize(node.ToJsonString(), type, SerializerOptions)
            ?? throw new JsonException("条目为空");
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json, DocumentOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
