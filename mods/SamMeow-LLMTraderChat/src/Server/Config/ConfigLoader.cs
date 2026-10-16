using System.Text.Json;
using System.Text.Json.Nodes;

namespace SamMeow.LLMTraderChat.Server.Config;

/// <summary>
/// 配置装载器（纯函数 + 只读文件访问；不触碰 SPT 运行时，便于单测）。
///
/// 行为：
/// <list type="bullet">
/// <item>合理默认值：缺失的键一律回退到 <see cref="ModConfig"/> 的默认值，不产生告警。</item>
/// <item>非法 JSON / 根节点非对象：回退默认全局配置并停用全部商人，产出一条告警。</item>
/// <item>per-trader 非法（缺 traderId / 元素非对象 / 字段类型非法）：仅停用对应商人，其他商人不受影响；告警指明文件与字段。</item>
/// <item>装载结果携带告警列表，供调用方「响亮记录」。</item>
/// </list>
///
/// STD-CFG-002：默认文件为 <c>config/config.jsonc</c>（.jsonc 允许注释与尾随逗号）。
/// </summary>
public static class ConfigLoader
{
    /// <summary>配置子目录名。</summary>
    public const string ConfigFolderName = "config";

    /// <summary>玩家可改的运行时配置文件名。</summary>
    public const string ConfigFileName = "config.jsonc";

    /// <summary>随包分发的默认配置副本文件名（首启复制为 config.jsonc）。</summary>
    public const string DefaultConfigFileName = "defaultConfig.jsonc";

    /// <summary>config.jsonc 相对 mod 根目录的路径（日志/诊断用）。</summary>
    public const string ConfigRelativePath = ConfigFolderName + "/" + ConfigFileName;

    /// <summary>defaultConfig.jsonc 相对 mod 根目录的路径（日志/诊断用）。</summary>
    public const string DefaultConfigRelativePath = ConfigFolderName + "/" + DefaultConfigFileName;

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    // 数值参数的钳制契约（**唯一归属处为装载层**；消费端不再静默钳制）。
    private const int MinHistoryMessageCount = 1;
    private const int MaxHistoryMessageCount = 200;
    private const int MinKnowledgeTopK = 1;
    private const int MaxKnowledgeTopK = 20;
    private const int MinTimeoutSeconds = 1;
    private const int MaxTimeoutSeconds = 600;
    private const int MinMaxTokens = 1;
    private const int MaxMaxTokens = 32768;
    private const double MinTemperature = 0;
    private const double MaxTemperature = 2;
    private const double MinDelaySeconds = 0;
    private const double MaxDelaySeconds = 60;

    /// <summary>从 mod 目录读取配置（优先 config.jsonc，回退 defaultConfig.jsonc；都没有则回退默认值）。</summary>
    public static ConfigLoadResult LoadFromDirectory(string modDirectory)
    {
        var configPath = Path.Combine(modDirectory, ConfigFolderName, ConfigFileName);
        if (File.Exists(configPath))
        {
            return LoadFromFile(configPath);
        }

        var defaultPath = Path.Combine(modDirectory, ConfigFolderName, DefaultConfigFileName);
        if (File.Exists(defaultPath))
        {
            return LoadFromFile(defaultPath);
        }

        var warnings = new List<string>
        {
            $"[LLMTC] {ConfigRelativePath}: 未找到配置文件（config.jsonc / defaultConfig.jsonc 均缺失）；"
            + "已停用全部商人并回退默认全局配置"
        };
        return new ConfigLoadResult(new ModConfig { Traders = [] }, warnings);
    }

    /// <summary>读取单个配置文件。告警中的来源标识取文件名。</summary>
    public static ConfigLoadResult LoadFromFile(string configPath)
    {
        return Parse(File.ReadAllText(configPath), Path.GetFileName(configPath));
    }

    /// <summary>
    /// 解析 JSONC 文本（纯函数；sourceLabel 用于告警中的来源文件标识，缺省 config.jsonc）。
    /// </summary>
    public static ConfigLoadResult Parse(string json, string sourceLabel = ConfigFileName)
    {
        var warnings = new List<string>();

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json, nodeOptions: null, documentOptions: DocumentOptions);
        }
        catch (JsonException ex)
        {
            warnings.Add(
                $"[LLMTC] {sourceLabel}: JSON 解析失败（{ex.Message}）；已停用全部商人并回退默认全局配置");
            return new ConfigLoadResult(new ModConfig { Traders = [] }, warnings);
        }

        if (root is not JsonObject rootObject)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 配置根节点不是 JSON 对象；已停用全部商人并回退默认全局配置");
            return new ConfigLoadResult(new ModConfig { Traders = [] }, warnings);
        }

        var config = new ModConfig();
        BindLlm(rootObject, config.Llm, warnings, sourceLabel);
        BindReplyDelay(rootObject, config.ReplyDelay, warnings, sourceLabel);
        config.HistoryMessageCount = ClampInt(
            ReadInt(rootObject, "historyMessageCount", config.HistoryMessageCount, "historyMessageCount", warnings, sourceLabel),
            MinHistoryMessageCount, MaxHistoryMessageCount, "historyMessageCount", warnings, sourceLabel);
        config.KnowledgeTopK = ClampInt(
            ReadInt(rootObject, "knowledgeTopK", config.KnowledgeTopK, "knowledgeTopK", warnings, sourceLabel),
            MinKnowledgeTopK, MaxKnowledgeTopK, "knowledgeTopK", warnings, sourceLabel);
        config.FallbackMessage = ReadString(
            rootObject, "fallbackMessage", config.FallbackMessage, "fallbackMessage", warnings, sourceLabel);
        config.ResetConfirmationMessage = ReadString(
            rootObject, "resetConfirmationMessage", config.ResetConfirmationMessage, "resetConfirmationMessage", warnings, sourceLabel);
        config.SystemPromptPath = ReadString(
            rootObject, "systemPromptPath", config.SystemPromptPath, "systemPromptPath", warnings, sourceLabel);
        config.Traders = ParseTraders(rootObject, warnings, sourceLabel);

        return new ConfigLoadResult(config, warnings);
    }

    private static void BindLlm(JsonObject root, LlmConfig llm, List<string> warnings, string sourceLabel)
    {
        if (!root.TryGetPropertyValue("llm", out var node) || node is null)
        {
            return;
        }

        if (node is not JsonObject section)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 llm 类型非法（应为对象）；已回退默认 LLM 配置");
            return;
        }

        llm.Endpoint = ReadString(section, "endpoint", llm.Endpoint, "llm.endpoint", warnings, sourceLabel);
        if (!string.IsNullOrWhiteSpace(llm.Endpoint) && !IsAbsoluteHttpUri(llm.Endpoint))
        {
            warnings.Add(
                $"[LLMTC] {sourceLabel}: 字段 llm.endpoint 非法（应为绝对 http/https URI，"
                + "如 http://127.0.0.1:11434/v1）；已视同未配置");
            llm.Endpoint = "";
        }

        llm.ApiKey = ReadString(section, "apiKey", llm.ApiKey, "llm.apiKey", warnings, sourceLabel);
        WarnIfPlaceholderApiKey(llm.ApiKey, warnings, sourceLabel);
        llm.Model = ReadString(section, "model", llm.Model, "llm.model", warnings, sourceLabel);
        llm.Temperature = ClampDouble(
            ReadDouble(section, "temperature", llm.Temperature, "llm.temperature", warnings, sourceLabel),
            MinTemperature, MaxTemperature, "llm.temperature", warnings, sourceLabel);
        llm.MaxTokens = ClampInt(
            ReadInt(section, "maxTokens", llm.MaxTokens, "llm.maxTokens", warnings, sourceLabel),
            MinMaxTokens, MaxMaxTokens, "llm.maxTokens", warnings, sourceLabel);
        llm.TimeoutSeconds = ClampInt(
            ReadInt(section, "timeoutSeconds", llm.TimeoutSeconds, "llm.timeoutSeconds", warnings, sourceLabel),
            MinTimeoutSeconds, MaxTimeoutSeconds, "llm.timeoutSeconds", warnings, sourceLabel);
    }

    private static void BindReplyDelay(JsonObject root, ReplyDelayConfig delay, List<string> warnings, string sourceLabel)
    {
        if (!root.TryGetPropertyValue("replyDelay", out var node) || node is null)
        {
            return;
        }

        if (node is not JsonObject section)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 replyDelay 类型非法（应为对象）；已回退默认延迟范围");
            return;
        }

        delay.MinSeconds = ClampDouble(
            ReadDouble(section, "minSeconds", delay.MinSeconds, "replyDelay.minSeconds", warnings, sourceLabel),
            MinDelaySeconds, MaxDelaySeconds, "replyDelay.minSeconds", warnings, sourceLabel);
        delay.MaxSeconds = ClampDouble(
            ReadDouble(section, "maxSeconds", delay.MaxSeconds, "replyDelay.maxSeconds", warnings, sourceLabel),
            MinDelaySeconds, MaxDelaySeconds, "replyDelay.maxSeconds", warnings, sourceLabel);
        if (delay.MinSeconds > delay.MaxSeconds)
        {
            warnings.Add(
                $"[LLMTC] {sourceLabel}: replyDelay.minSeconds（{delay.MinSeconds}）大于 maxSeconds（{delay.MaxSeconds}），已交换");
            (delay.MinSeconds, delay.MaxSeconds) = (delay.MaxSeconds, delay.MinSeconds);
        }
    }

    /// <summary>逐元素解析 traders 数组；非法元素仅停用对应商人并告警，其余保留。</summary>
    private static List<TraderConfig> ParseTraders(JsonObject root, List<string> warnings, string sourceLabel)
    {
        var traders = new List<TraderConfig>();
        if (!root.TryGetPropertyValue("traders", out var node) || node is null)
        {
            return traders;
        }

        if (node is not JsonArray array)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 traders 类型非法（应为数组）；已停用全部商人");
            return traders;
        }

        for (var index = 0; index < array.Count; index++)
        {
            var path = $"traders[{index}]";
            if (array[index] is not JsonObject element)
            {
                warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path} 类型非法（应为对象）；该商人已禁用");
                continue;
            }

            if (!TryReadTraderId(element, path, warnings, sourceLabel, out var traderId))
            {
                continue;
            }

            var trader = new TraderConfig { TraderId = traderId };
            trader.Name = ReadString(element, "name", "", $"{path}.name", warnings, sourceLabel);
            trader.Enabled = ReadBool(element, "enabled", true, $"{path}.enabled", warnings, sourceLabel);
            trader.Persona = ReadString(element, "persona", "", $"{path}.persona", warnings, sourceLabel);
            trader.DocsDir = ReadString(element, "docsDir", "", $"{path}.docsDir", warnings, sourceLabel);
            trader.OpeningMessage = ReadString(element, "openingMessage", "", $"{path}.openingMessage", warnings, sourceLabel);
            trader.ResetKeywords = ReadStringList(
                element, "resetKeywords", trader.ResetKeywords, $"{path}.resetKeywords", warnings, sourceLabel);
            ReadAttitudePrompts(element, trader.AttitudePrompts, path, warnings, sourceLabel);
            traders.Add(trader);
        }

        return traders;
    }

    /// <summary>
    /// 读取必填的 traderId。缺失 / 空串 / 非字符串各只产出一条告警（不再先报类型非法再报缺失），
    /// 返回 false 表示该商人应被禁用。
    /// </summary>
    private static bool TryReadTraderId(
        JsonObject element, string path, List<string> warnings, string sourceLabel, out string traderId)
    {
        traderId = "";
        var field = $"{path}.traderId";

        if (!element.TryGetPropertyValue("traderId", out var node) || node is null)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 {field} 缺失；该商人已禁用");
            return false;
        }

        if (node is not JsonValue value
            || !value.TryGetValue<string>(out var parsed)
            || string.IsNullOrWhiteSpace(parsed))
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 {field} 非法（应为非空字符串）；该商人已禁用");
            return false;
        }

        traderId = parsed;
        return true;
    }

    /// <summary>
    /// 读取可选的 <c>attitudePrompts</c> 对象：未知档位键 / 非字符串值 / 空白值仅告警忽略，不禁用商人；
    /// 键规范为小写（大小写不敏感），值 trim 后存入 <paramref name="target"/>。缺失或 null 时不告警。
    /// </summary>
    private static void ReadAttitudePrompts(
        JsonObject element, Dictionary<string, string> target, string path, List<string> warnings, string sourceLabel)
    {
        var field = $"{path}.attitudePrompts";
        if (!element.TryGetPropertyValue("attitudePrompts", out var node) || node is null)
        {
            return;
        }

        if (node is not JsonObject section)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 {field} 类型非法（应为对象）；已忽略自定义态度文案");
            return;
        }

        foreach (var property in section)
        {
            var key = property.Key;
            if (!TraderAttitudeKeys.All.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                warnings.Add(
                    $"[LLMTC] {sourceLabel}: 字段 {field}.{key} 未知态度档位"
                    + "（允许值：hostile/cold/familiar/trusted/ally）；已忽略");
                continue;
            }

            if (property.Value is not JsonValue value
                || !value.TryGetValue<string>(out var parsed)
                || string.IsNullOrWhiteSpace(parsed))
            {
                warnings.Add($"[LLMTC] {sourceLabel}: 字段 {field}.{key} 类型非法（应为非空字符串）；已忽略");
                continue;
            }

            target[key.ToLowerInvariant()] = parsed.Trim();
        }
    }

    /// <summary>endpoint 必须是绝对 http/https URI（拒绝缺 scheme 的 host:port 串）。</summary>
    private static bool IsAbsoluteHttpUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    /// <summary>
    /// apiKey 非空但含非 ASCII 字符时告警（真实密钥为 ASCII；随包示例用中文占位符易被误留）。
    /// 空 key（如本地 Ollama）不告警。
    /// </summary>
    private static void WarnIfPlaceholderApiKey(string apiKey, List<string> warnings, string sourceLabel)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        foreach (var ch in apiKey)
        {
            if (ch < ' ' || ch > '~')
            {
                warnings.Add(
                    $"[LLMTC] {sourceLabel}: 字段 llm.apiKey 疑似占位符或含非 ASCII 字符，"
                    + "请替换为真实密钥（否则 LLM 调用将失败）");
                return;
            }
        }
    }

    /// <summary>整数钳制：越界时告警并取边界值；在区间内原样返回（不产生告警）。</summary>
    private static int ClampInt(int value, int min, int max, string path, List<string> warnings, string sourceLabel)
    {
        if (value < min)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path} 值 {value} 低于下限 {min}，已钳制为 {min}");
            return min;
        }

        if (value > max)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path} 值 {value} 超过上限 {max}，已钳制为 {max}");
            return max;
        }

        return value;
    }

    /// <summary>浮点钳制：越界时告警并取边界值；在区间内原样返回（不产生告警）。</summary>
    private static double ClampDouble(double value, double min, double max, string path, List<string> warnings, string sourceLabel)
    {
        if (value < min)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path} 值 {value} 低于下限 {min}，已钳制为 {min}");
            return min;
        }

        if (value > max)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path} 值 {value} 超过上限 {max}，已钳制为 {max}");
            return max;
        }

        return value;
    }

    private static bool ReadBool(JsonObject obj, string key, bool fallback, string path, List<string> warnings, string sourceLabel)
    {
        if (!obj.TryGetPropertyValue(key, out var node) || node is null)
        {
            return fallback;
        }

        if (node is JsonValue value && value.TryGetValue<bool>(out var parsed))
        {
            return parsed;
        }

        warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path} 类型非法（应为布尔）；已回退默认值 {fallback}");
        return fallback;
    }

    private static string ReadString(JsonObject obj, string key, string fallback, string path, List<string> warnings, string sourceLabel)
    {
        if (!obj.TryGetPropertyValue(key, out var node) || node is null)
        {
            return fallback;
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var parsed))
        {
            return parsed;
        }

        warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path} 类型非法（应为字符串）；已回退默认值");
        return fallback;
    }

    private static double ReadDouble(JsonObject obj, string key, double fallback, string path, List<string> warnings, string sourceLabel)
    {
        if (!obj.TryGetPropertyValue(key, out var node) || node is null)
        {
            return fallback;
        }

        if (node is JsonValue value && value.TryGetValue<double>(out var parsed) && double.IsFinite(parsed))
        {
            return parsed;
        }

        warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path} 类型非法（应为数值）；已回退默认值 {fallback}");
        return fallback;
    }

    private static int ReadInt(JsonObject obj, string key, int fallback, string path, List<string> warnings, string sourceLabel)
    {
        if (!obj.TryGetPropertyValue(key, out var node) || node is null)
        {
            return fallback;
        }

        if (node is JsonValue value)
        {
            if (value.TryGetValue<int>(out var intValue))
            {
                return intValue;
            }

            // 接受 5.0 这类整数值的浮点表示，但拒绝真正的小数。
            if (value.TryGetValue<double>(out var doubleValue) && double.IsFinite(doubleValue) && Math.Floor(doubleValue) == doubleValue)
            {
                return (int)doubleValue;
            }
        }

        warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path} 类型非法（应为整数）；已回退默认值 {fallback}");
        return fallback;
    }

    private static List<string> ReadStringList(
        JsonObject obj, string key, List<string> fallback, string path, List<string> warnings, string sourceLabel)
    {
        if (!obj.TryGetPropertyValue(key, out var node) || node is null)
        {
            return fallback;
        }

        if (node is not JsonArray array)
        {
            warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path} 类型非法（应为字符串数组）；已回退默认值");
            return fallback;
        }

        var result = new List<string>(array.Count);
        for (var index = 0; index < array.Count; index++)
        {
            if (array[index] is JsonValue value && value.TryGetValue<string>(out var item) && !string.IsNullOrWhiteSpace(item))
            {
                result.Add(item);
            }
            else
            {
                warnings.Add($"[LLMTC] {sourceLabel}: 字段 {path}[{index}] 类型非法（应为非空字符串）；已忽略该元素");
            }
        }

        return result;
    }
}
