using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;

namespace InescapableTarkovsSoftcore.Config;

/// <summary>
/// 配置加载器：定位 mod 目录 → 读取（缺失则从内嵌默认模板重建）→ 两遍解析。
/// 第一遍做键集与类型校验（键集由配置模型反射派生；未知键 / 类型非法 / 节值为 null
/// → 告警并从待绑定节点移除），第二遍用 System.Text.Json 绑定到 <see cref="SoftcoreConfig"/>
/// （缺键回落默认）。解析容忍 <c>//</c> 注释与尾随逗号。
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class ConfigLoader(ModHelper modHelper, ISptLogger<ConfigLoader> logger)
{
    public const string ConfigFileName = "config.json";

    private const string EmbeddedResourceName = "InescapableTarkovsSoftcore.default-config.json";

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = false
    };

    /// <summary>定位 mod 目录并加载配置（生产入口）。</summary>
    public ConfigLoadResult Load() => LoadFromDirectory(ResolveModDirectory());

    /// <summary>从指定目录加载 <c>config.json</c>；文件缺失时从内嵌模板重建并告警。</summary>
    public static ConfigLoadResult LoadFromDirectory(string modDirectory)
    {
        var warnings = new List<string>();
        var path = Path.Combine(modDirectory, ConfigFileName);
        string json;

        if (File.Exists(path))
        {
            json = File.ReadAllText(path);
        }
        else
        {
            warnings.Add($"[ITS] 未找到 {ConfigFileName}，已从内嵌默认模板重建：{path}");
            json = ReadDefaultConfigJson();

            try
            {
                Directory.CreateDirectory(modDirectory);
                File.WriteAllText(path, json, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                warnings.Add($"[ITS] 重建 {ConfigFileName} 失败：{ex.Message}");
            }
        }

        var result = Parse(json);
        return new ConfigLoadResult(result.Config, warnings.Concat(result.Warnings).ToList());
    }

    /// <summary>解析 JSONC 文本；纯函数，便于测试（不触碰文件系统或 SPT 运行时）。</summary>
    public static ConfigLoadResult Parse(string json)
    {
        var warnings = new List<string>();

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json, nodeOptions: null, documentOptions: DocumentOptions);
        }
        catch (JsonException ex)
        {
            warnings.Add($"[ITS] 配置 JSON 解析失败，使用全部默认值：{ex.Message}");
            return new ConfigLoadResult(new SoftcoreConfig(), warnings);
        }

        if (root is not JsonObject rootObject)
        {
            warnings.Add("[ITS] 配置根节点不是对象，使用全部默认值");
            return new ConfigLoadResult(new SoftcoreConfig(), warnings);
        }

        // 在副本上清洗，避免污染调用方传入的节点。
        var sanitized = (JsonObject)rootObject.DeepClone();
        ValidateRoot(sanitized, warnings);

        SoftcoreConfig config;
        try
        {
            config = sanitized.Deserialize<SoftcoreConfig>(SerializerOptions) ?? new SoftcoreConfig();
        }
        catch (JsonException ex)
        {
            warnings.Add($"[ITS] 配置绑定失败，使用默认值：{ex.Message}");
            config = new SoftcoreConfig();
        }

        return new ConfigLoadResult(config, warnings);
    }

    /// <summary>读取主工程内嵌的默认配置模板原文（JSONC）。</summary>
    public static string ReadDefaultConfigJson()
    {
        var assembly = typeof(ConfigLoader).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"缺少内嵌资源：{EmbeddedResourceName}");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /// <summary>优先用 ModHelper 定位 mod 目录；失败则回退程序集所在目录。</summary>
    private string ResolveModDirectory()
    {
        var assembly = typeof(ConfigLoader).Assembly;

        try
        {
            var resolved = modHelper.GetAbsolutePathToModFolder(assembly);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                return resolved;
            }
        }
        catch (Exception ex)
        {
            logger.Warning($"[ITS] ModHelper 定位 mod 目录失败，回退程序集目录：{ex.Message}");
        }

        var directory = Path.GetDirectoryName(assembly.Location);
        return string.IsNullOrWhiteSpace(directory) ? AppContext.BaseDirectory : directory;
    }

    /// <summary>根级键集与各节键集均从配置模型反射派生（读 [JsonPropertyName]），避免手写清单漂移。</summary>
    private static void ValidateRoot(JsonObject root, List<string> warnings)
    {
        var sections = typeof(SoftcoreConfig)
            .GetProperties()
            .Select(property => (Name: JsonName(property), Type: property.PropertyType))
            .ToList();

        var rootNames = sections.Select(section => section.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var (key, _) in root.ToList())
        {
            if (!rootNames.Contains(key))
            {
                warnings.Add($"[ITS] 未知配置键 \"{key}\"，已忽略");
                root.Remove(key);
            }
        }

        foreach (var (name, type) in sections)
        {
            ValidateSection(root, name, name, warnings, type);
        }
    }

    /// <summary>
    /// 校验单个配置节 / 子节：缺键保留默认；节值为 null 或非对象则以「类型非法」处理
    /// （告警 + 移除 → 回落默认）；未知叶键 / 叶键类型非法则移除该键（回落默认）并告警；
    /// 对象型键视为子节递归校验，路径以点号拼接用于告警定位。
    /// </summary>
    private static void ValidateSection(JsonObject parent, string key, string path, List<string> warnings, Type sectionType)
    {
        if (!parent.TryGetPropertyValue(key, out var node))
        {
            return;
        }

        if (node is not JsonObject section)
        {
            warnings.Add($"[ITS] 配置节 \"{path}\" 类型非法（应为对象），使用默认值");
            parent.Remove(key);
            return;
        }

        ValidateObject(section, path, warnings, sectionType);
    }

    /// <summary>逐键校验一个已确认是对象的配置节 / 子节。</summary>
    private static void ValidateObject(JsonObject section, string path, List<string> warnings, Type sectionType)
    {
        var boolKeys = new HashSet<string>(StringComparer.Ordinal);
        var numberKeys = new Dictionary<string, Type>(StringComparer.Ordinal);
        var dictionaryLeafKeys = new Dictionary<string, Type>(StringComparer.Ordinal);
        var objectKeys = new Dictionary<string, Type>(StringComparer.Ordinal);
        var allowed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var property in sectionType.GetProperties())
        {
            var name = JsonName(property);
            allowed.Add(name);

            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (type == typeof(bool))
            {
                boolKeys.Add(name);
            }
            else if (IsNumeric(type))
            {
                numberKeys[name] = type;
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                // 字典型叶键（如 overrides）：不递归进 Dictionary 反射面，作为键值对整体校验。
                dictionaryLeafKeys[name] = type;
            }
            else if (type != typeof(string) && type.IsClass)
            {
                objectKeys[name] = type;
            }
        }

        foreach (var (key, value) in section.ToList())
        {
            if (!allowed.Contains(key))
            {
                warnings.Add($"[ITS] 未知配置键 \"{path}.{key}\"，已忽略");
                section.Remove(key);
                continue;
            }

            if (dictionaryLeafKeys.TryGetValue(key, out var dictionaryType))
            {
                ValidateDictionaryLeaf(section, key, path, warnings, dictionaryType);
                continue;
            }

            if (objectKeys.TryGetValue(key, out var childType))
            {
                ValidateSection(section, key, $"{path}.{key}", warnings, childType);
                continue;
            }

            var valid = boolKeys.Contains(key)
                ? value is JsonValue boolValue && boolValue.TryGetValue<bool>(out _)
                : !numberKeys.ContainsKey(key) || IsValidNumber(value, numberKeys[key]);

            if (!valid)
            {
                warnings.Add($"[ITS] 配置键 \"{path}.{key}\" 类型非法，使用默认值");
                section.Remove(key);
            }
        }
    }

    /// <summary>校验字典型叶键（如 overrides）：值须为对象；内层值须符合字典值类型（整数型拒绝小数）。</summary>
    private static void ValidateDictionaryLeaf(JsonObject parent, string key, string path, List<string> warnings, Type dictionaryType)
    {
        if (parent[key] is not JsonObject map)
        {
            warnings.Add($"[ITS] 配置键 \"{path}.{key}\" 类型非法（应为对象），使用默认值");
            parent.Remove(key);
            return;
        }

        var valueType = dictionaryType.IsGenericType
            ? dictionaryType.GetGenericArguments()[1]
            : typeof(double);

        foreach (var (entryKey, entryValue) in map.ToList())
        {
            if (!IsValidNumber(entryValue, valueType))
            {
                warnings.Add($"[ITS] 配置键 \"{path}.{key}.{entryKey}\" 类型非法，已忽略");
                map.Remove(entryKey);
            }
        }
    }

    /// <summary>数值叶校验：整数型（int/long）拒绝非整数（如 5.5），浮点型接受任意 JSON 数字。</summary>
    private static bool IsValidNumber(JsonNode? value, Type type)
    {
        if (value is not JsonValue jsonValue)
        {
            return false;
        }

        if (type == typeof(int) || type == typeof(long))
        {
            return jsonValue.TryGetValue<int>(out _)
                   || jsonValue.TryGetValue<long>(out _)
                   || (jsonValue.TryGetValue<double>(out var number) && double.IsFinite(number) && Math.Floor(number) == number);
        }

        return jsonValue.TryGetValue<double>(out _);
    }

    private static bool IsNumeric(Type type) =>
        type == typeof(double) || type == typeof(float) || type == typeof(int) || type == typeof(long);

    private static string JsonName(PropertyInfo property) =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
}
