using System;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 日志级别归一化与严重度比较（纯逻辑，可独立单测）。
///
/// BepInEx 6 的 <c>LogLevel</c> 是 <c>[Flags]</c> 枚举，数值不反映严重度
/// （Fatal=1 &lt; Error=2 &lt; Warning=4 &lt; Message=8 &lt; Info=16 &lt; Debug=32），
/// 故过滤一律走本类的显式排名，绝不直接比较枚举数值。
/// 归一化级别名即 JSON <c>level</c> 字段的稳定字面量（小写）。
/// </summary>
internal static class LogWatchLevel
{
    internal const string Fatal = "fatal";

    internal const string Error = "error";

    internal const string Warning = "warning";

    internal const string Message = "message";

    internal const string Info = "info";

    internal const string Debug = "debug";

    /// <summary>
    /// 级别名归一化：去空白 + 大小写不敏感；未知 / 空 → <c>""</c>（视为「无有效阈值」）。
    /// </summary>
    internal static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        if (string.Equals(trimmed, Fatal, StringComparison.OrdinalIgnoreCase))
        {
            return Fatal;
        }

        if (string.Equals(trimmed, Error, StringComparison.OrdinalIgnoreCase))
        {
            return Error;
        }

        if (string.Equals(trimmed, Warning, StringComparison.OrdinalIgnoreCase))
        {
            return Warning;
        }

        if (string.Equals(trimmed, Message, StringComparison.OrdinalIgnoreCase))
        {
            return Message;
        }

        if (string.Equals(trimmed, Info, StringComparison.OrdinalIgnoreCase))
        {
            return Info;
        }

        if (string.Equals(trimmed, Debug, StringComparison.OrdinalIgnoreCase))
        {
            return Debug;
        }

        return string.Empty;
    }

    /// <summary>
    /// 严重度排名：0 = 最严重（fatal）… 5 = 最不严重（debug）；
    /// 未知 / 空级别返回 <see cref="int.MaxValue"/>（永不满足任何有效阈值）。
    /// </summary>
    internal static int Rank(string levelName)
    {
        var normalized = Normalize(levelName);
        switch (normalized)
        {
            case Fatal:
                return 0;
            case Error:
                return 1;
            case Warning:
                return 2;
            case Message:
                return 3;
            case Info:
                return 4;
            case Debug:
                return 5;
            default:
                return int.MaxValue;
        }
    }

    /// <summary>
    /// <paramref name="levelName"/> 是否达到（不轻于）<paramref name="minLevelName"/> 阈值。
    /// 阈值为空 / 未知时视为「无过滤」，一律返回 true。
    /// </summary>
    internal static bool IsAtLeast(string levelName, string minLevelName)
    {
        var normalizedMin = Normalize(minLevelName);
        if (normalizedMin.Length == 0)
        {
            return true;
        }

        return Rank(levelName) <= Rank(normalizedMin);
    }

    /// <summary>
    /// <c>level</c> 查询参数解析（宽松）：缺失 / 非法 → <c>""</c>（查询侧不做级别过滤，
    /// 返回全部已采集条目——采集阈值已在写入侧生效）。
    /// </summary>
    internal static string ParseMinLevel(string value)
    {
        return Normalize(value);
    }
}
