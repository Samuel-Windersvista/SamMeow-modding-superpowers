using System;
using System.Globalization;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// <c>/raid/events</c> 查询参数解析（纯逻辑）：宽松解析——缺失 / 非法 / 越界回退默认，
/// 不因客户端输入产生 4xx。
/// </summary>
internal static class RaidEventsQuery
{
    internal const long DefaultSince = 0;

    internal const int DefaultLimit = RaidEventBuffer.DefaultLimit;

    internal const int MaxLimit = RaidEventBuffer.DefaultCapacity;

    /// <summary>since：缺失 / 非法 / 非正数 → 0（从头拉取）。</summary>
    internal static long ParseSince(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return DefaultSince;
        }

        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return DefaultSince;
        }

        return parsed > 0 ? parsed : DefaultSince;
    }

    /// <summary>limit：缺失 / 非法 / 非正数 → 默认；超过容量 → 钳制到容量。</summary>
    internal static int ParseLimit(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return DefaultLimit;
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return DefaultLimit;
        }

        if (parsed <= 0)
        {
            return DefaultLimit;
        }

        return parsed > MaxLimit ? MaxLimit : parsed;
    }
}
