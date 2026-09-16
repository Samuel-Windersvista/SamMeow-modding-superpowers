using System;
using System.Globalization;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// <c>/logs/summary</c> 查询参数解析（纯逻辑）：宽松解析——缺失 / 非法回退「不过滤」，
/// 不因客户端输入产生 4xx。
/// </summary>
internal static class LogSummaryQuery
{
    /// <summary>0 = 不过滤（返回全部组）。</summary>
    internal const long DefaultSinceTicks = 0;

    /// <summary>
    /// <c>since</c>：接受两种形态——(1) 端点自己输出的 ISO 8601（<c>lastTs</c> / <c>firstTs</c>
    /// 原样回填即可往返）；(2) 整数 UTC Ticks。缺失 / 非法 / 非正数 → 0（不过滤）。
    /// </summary>
    internal static long ParseSince(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return DefaultSinceTicks;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return DefaultSinceTicks;
        }

        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
        {
            return ticks > 0 ? ticks : DefaultSinceTicks;
        }

        if (DateTime.TryParse(
                trimmed,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces,
                out var parsed))
        {
            return parsed.Ticks > 0 ? parsed.Ticks : DefaultSinceTicks;
        }

        return DefaultSinceTicks;
    }
}
