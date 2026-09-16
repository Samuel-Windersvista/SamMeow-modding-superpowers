using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 单条日志归一化规则（契约派生）：<see cref="Pattern"/> 已在加载期编译并校验，
/// <see cref="Replacement"/> 为占位符文本。规则顺序即应用顺序。
/// </summary>
internal sealed class BridgeNormalizationRule
{
    internal BridgeNormalizationRule(string id, Regex pattern, string replacement)
    {
        Id = id;
        Pattern = pattern;
        Replacement = replacement;
    }

    /// <summary>规则标识（如 guid / id24hex / hex / number）。</summary>
    internal string Id { get; }

    /// <summary>已编译正则（加载期校验；ECMAScript 语义，与 TS 端一致）。</summary>
    internal Regex Pattern { get; }

    /// <summary>替换文本（占位符，如 <c>&lt;guid&gt;</c>）。</summary>
    internal string Replacement { get; }
}

/// <summary>
/// 桥接契约解析结果（不可变）。字段与 <c>shared/bridge-contract/contract.json</c> 一一对应。
/// </summary>
internal sealed class BridgeContractData
{
    internal BridgeContractData(
        int protocolVersion,
        IReadOnlyList<BridgeNormalizationRule> normalizationRules,
        string whitespacePattern,
        bool trim,
        int maxGroups)
    {
        ProtocolVersion = protocolVersion;
        NormalizationRules = normalizationRules;
        WhitespacePattern = whitespacePattern;
        Trim = trim;
        MaxGroups = maxGroups;
    }

    /// <summary>wire 协议版本（握手门禁唯一源）。</summary>
    internal int ProtocolVersion { get; }

    /// <summary>归一化规则（顺序即应用顺序）。</summary>
    internal IReadOnlyList<BridgeNormalizationRule> NormalizationRules { get; }

    /// <summary>
    /// 空白收敛模式（显式字符类 + 量词，如 <c>[ \t…]+</c>）：规则应用后把连续空白
    /// 折叠为单空格，<see cref="Trim"/> 也使用同一集合。**不含 <c>\s</c>**——.NET 的
    /// ECMAScript <c>\s</c> 仅 ASCII，与 JS 集合不同（见 ADR-0009）。
    /// </summary>
    internal string WhitespacePattern { get; }

    /// <summary>是否按 <see cref="WhitespacePattern"/> 去除首尾空白。</summary>
    internal bool Trim { get; }

    /// <summary>聚合组数上限。</summary>
    internal int MaxGroups { get; }
}

/// <summary>
/// 桥接跨语言契约（C10 单一源）：编译期嵌入 <c>shared/bridge-contract/contract.json</c>，
/// 静态初始化时解析并校验；任何缺失 / 非法 = 响亮失败（<see cref="TypeInitializationException"/>）。
///
/// 契约取代了此前散落在源码中的三处字面量：<see cref="Plugin.ProtocolVersion"/>、
/// <see cref="LogSummaryNormalizer"/> 的规则集、<see cref="LogSummaryStore"/> 的组数上限。
/// 两端（C# / TS）消费同一数据；规则 pattern 限定 JS/.NET 共通子集，C# 侧以
/// <see cref="RegexOptions.ECMAScript"/> 编译以对齐 JS 的 <c>\b</c> / <c>\w</c> / <c>\d</c> 语义。
/// <c>\s</c> **不在共通子集内**（.NET ECMAScript 的 <c>\s</c> 仅 ASCII，JS 的 <c>\s</c> 是
/// 25 个码点的集合），故空白收敛由契约的显式字符类
/// <see cref="BridgeContractData.WhitespacePattern"/> 承担，规则与空白类内均不得出现 <c>\s</c>。
/// </summary>
internal static class BridgeContract
{
    /// <summary>嵌入资源逻辑名（与 csproj LogicalName 一致）。</summary>
    internal const string ResourceName = "TarkovRuntimeBridge.bridge-contract.json";

    /// <summary>
    /// 规则正则选项：ECMAScript 语义与 TS 端一致；Compiled 降低热路径开销。
    /// ECMAScript 与 CultureInvariant 可以组合（.NET 6 的白名单允许，已实测构造成功），
    /// 但此处无需——ECMAScript 语义本身即与区域设置无关，故不额外声明 CultureInvariant。
    /// </summary>
    private const RegexOptions RuleRegexOptions = RegexOptions.Compiled | RegexOptions.ECMAScript;

    /// <summary>静态初始化即加载并校验契约；失败 = 响亮报错。</summary>
    internal static BridgeContractData Current { get; } = Load();

    internal static int ProtocolVersion => Current.ProtocolVersion;

    internal static IReadOnlyList<BridgeNormalizationRule> NormalizationRules => Current.NormalizationRules;

    internal static string WhitespacePattern => Current.WhitespacePattern;

    internal static bool Trim => Current.Trim;

    internal static int MaxGroups => Current.MaxGroups;

    /// <summary>
    /// 解析契约 JSON 并校验（供单测直接驱动；非法输入抛
    /// <see cref="InvalidOperationException"/>，不静默降级）。
    /// </summary>
    internal static BridgeContractData Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("桥接契约内容为空");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"桥接契约 JSON 解析失败：{exception.Message}", exception);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("桥接契约根节点必须是 JSON 对象");
            }

            var protocolVersion = ReadInt(root, "protocolVersion");
            if (protocolVersion < 1)
            {
                throw new InvalidOperationException($"桥接契约 protocolVersion 必须 ≥ 1（实际 {protocolVersion}）");
            }

            if (!root.TryGetProperty("logNormalization", out var normalization)
                || normalization.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("桥接契约缺少 logNormalization 对象");
            }

            if (!normalization.TryGetProperty("rules", out var rulesElement)
                || rulesElement.ValueKind != JsonValueKind.Array
                || rulesElement.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("桥接契约 logNormalization.rules 必须是非空数组");
            }

            var rules = new List<BridgeNormalizationRule>(rulesElement.GetArrayLength());
            foreach (var ruleElement in rulesElement.EnumerateArray())
            {
                if (ruleElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException("桥接契约归一化规则必须是 JSON 对象");
                }

                var id = ReadString(ruleElement, "id");
                var pattern = ReadString(ruleElement, "pattern");
                var replacement = ReadString(ruleElement, "replacement");

                Regex compiled;
                try
                {
                    compiled = new Regex(pattern, RuleRegexOptions);
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException(
                        $"桥接契约规则 {id} 的 pattern 非法：{exception.Message}", exception);
                }

                rules.Add(new BridgeNormalizationRule(id, compiled, replacement));
            }

            var whitespacePattern = ReadString(normalization, "whitespacePattern");
            try
            {
                // 加载期编译校验：非法空白模式立即响亮报错（与 TS 端 parse 对齐）。
                _ = new Regex(whitespacePattern, RuleRegexOptions);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    $"桥接契约 whitespacePattern 非法：{exception.Message}", exception);
            }

            var trim = ReadBool(normalization, "trim");

            if (!root.TryGetProperty("logAggregation", out var aggregation)
                || aggregation.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("桥接契约缺少 logAggregation 对象");
            }

            var maxGroups = ReadInt(aggregation, "maxGroups");
            if (maxGroups < 1)
            {
                throw new InvalidOperationException($"桥接契约 logAggregation.maxGroups 必须 ≥ 1（实际 {maxGroups}）");
            }

            return new BridgeContractData(protocolVersion, rules, whitespacePattern, trim, maxGroups);
        }
    }

    private static BridgeContractData Load()
    {
        var assembly = typeof(BridgeContract).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream == null)
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new InvalidOperationException(
                $"桥接契约嵌入资源缺失：{ResourceName}。构建时 csproj 必须包含 " +
                $"<EmbeddedResource Include=\"..\\..\\shared\\bridge-contract\\contract.json\" " +
                $"LogicalName=\"{ResourceName}\" />（当前可用资源：{available}）。");
        }

        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>
    /// 读取整数字段：按 double 判定整值，故 JSON 的 <c>1.0</c> / <c>500.0</c> 形态同样接受
    /// （与 TS 端 <c>Number.isInteger</c> 语义一致，见 ADR-0009）。
    /// </summary>
    private static int ReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Number
            || !value.TryGetDouble(out var number)
            || double.IsNaN(number)
            || double.IsInfinity(number)
            || number != Math.Floor(number)
            || number < int.MinValue
            || number > int.MaxValue)
        {
            throw new InvalidOperationException($"桥接契约字段 {propertyName} 必须是整数");
        }

        return (int)number;
    }

    private static bool ReadBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value)
            || (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
        {
            throw new InvalidOperationException($"桥接契约字段 {propertyName} 必须是布尔值");
        }

        return value.GetBoolean();
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"桥接契约字段 {propertyName} 必须是字符串");
        }

        var result = value.GetString();
        if (string.IsNullOrEmpty(result))
        {
            throw new InvalidOperationException($"桥接契约字段 {propertyName} 不得为空");
        }

        return result;
    }
}
