using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SamMeow.LLMTraderChat.Shared;

/// <summary>
/// 客户端«可聊天商人»配置文件的纯解析/生成（不依赖 SPT、BepInEx、System.Text.Json）。
/// 约定文件形如 <c>{ "enabledTraderIds": ["54cb...", "5935..."] }</c>。
///
/// 解析为**宽松子串解析**（不做完整 JSON 语法校验）：定位 `enabledTraderIds` 后取其 `[...]` 区间内的
/// 引号字符串。语义：
/// <list type="bullet">
/// <item>找不到键 / 结构不完整（缺 `[` 或缺 `]`）/ 输入为空 → 回退 <c>defaults</c>。</item>
/// <item><b>显式空数组 <c>[]</c> → 返回空列表（不解锁任何商人）</b>，不回退默认。</item>
/// </list>
/// 绝不抛异常。
/// </summary>
public static class ClientTraderList
{
    private const string Key = "enabledTraderIds";

    private static readonly Regex QuotedStringRegex = new("\"([^\"]*)\"", RegexOptions.Compiled);

    /// <summary>
    /// 解析 JSON 文本中的 <c>enabledTraderIds</c> 字符串数组。
    /// 缺失/结构不完整 → <paramref name="defaults"/>；显式空数组 → 空列表。
    /// </summary>
    public static IReadOnlyList<string> Parse(string? json, IReadOnlyList<string> defaults)
    {
        if (json is null || json.Length == 0)
        {
            return defaults;
        }

        var keyIndex = json.IndexOf(Key, StringComparison.Ordinal);
        if (keyIndex < 0)
        {
            return defaults;
        }

        var open = json.IndexOf('[', keyIndex);
        if (open < 0)
        {
            return defaults;
        }

        var close = json.IndexOf(']', open + 1);
        if (close < 0)
        {
            return defaults;
        }

        var body = json.Substring(open + 1, close - open - 1);
        var ids = new List<string>();
        foreach (Match match in QuotedStringRegex.Matches(body))
        {
            var value = match.Value.Trim('"').Trim();
            if (value.Length > 0 && !ids.Contains(value))
            {
                ids.Add(value);
            }
        }

        // 数组区间存在即为「显式列表」：可以为空（[] = 不解锁任何商人）。
        return ids;
    }

    /// <summary>商人 id 是否在名单内（大小写不敏感）。</summary>
    public static bool Contains(IReadOnlyList<string> ids, string traderId)
    {
        for (var index = 0; index < ids.Count; index++)
        {
            if (string.Equals(ids[index], traderId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>生成默认配置文件文本。</summary>
    public static string BuildDefault(IReadOnlyList<string> ids)
    {
        var builder = new StringBuilder();
        builder.Append("{\n  \"").Append(Key).Append("\": [\n");
        for (var index = 0; index < ids.Count; index++)
        {
            builder.Append("    \"").Append(ids[index]).Append('"');
            if (index < ids.Count - 1)
            {
                builder.Append(',');
            }

            builder.Append('\n');
        }

        builder.Append("  ]\n}\n");
        return builder.ToString();
    }
}
