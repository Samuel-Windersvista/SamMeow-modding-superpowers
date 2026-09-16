using System;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 桥路由判定结果（纯逻辑，与 HttpListener 解耦，可独立单测）。
/// </summary>
internal enum BridgeRouteKind
{
    /// <summary>未知路径：404 {"error":"not_found"}（无论方法）。</summary>
    NotFound,

    /// <summary>已知路径但非 GET：405 {"error":"method_not_allowed"}。</summary>
    MethodNotAllowed,

    /// <summary>GET /bridge/info。</summary>
    Info,

    /// <summary>GET /raid/player。</summary>
    Player,

    /// <summary>GET /raid/status。</summary>
    Status,

    /// <summary>GET /raid/bots（?detail=1 时含明细）。</summary>
    Bots,

    /// <summary>GET /raid/events（?since=&amp;limit= 增量拉取事件）。</summary>
    Events,

    /// <summary>GET /logs/recent（?level=&amp;since=&amp;limit= 增量拉取日志）。</summary>
    LogsRecent,

    /// <summary>GET /logs/summary（?since= 归一化聚合视图）。</summary>
    LogsSummary,
}

/// <summary>
/// 纯路由判定：路径大小写不敏感；未知路径一律 <see cref="BridgeRouteKind.NotFound"/>（含非 GET）；
/// 已知路径非 GET 为 <see cref="BridgeRouteKind.MethodNotAllowed"/>；已知路径 GET 映射到具体端点。
/// </summary>
internal static class BridgeRouter
{
    internal const string InfoPath = "/bridge/info";
    internal const string PlayerPath = "/raid/player";
    internal const string StatusPath = "/raid/status";
    internal const string BotsPath = "/raid/bots";
    internal const string EventsPath = "/raid/events";
    internal const string LogsRecentPath = "/logs/recent";
    internal const string LogsSummaryPath = "/logs/summary";

    internal static bool IsKnownRoute(string path)
    {
        return string.Equals(path, InfoPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, PlayerPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, StatusPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, BotsPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, EventsPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, LogsRecentPath, StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, LogsSummaryPath, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsGet(string method)
    {
        return string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判定请求路径 + 方法对应的路由结果。顺序即优先级：
    /// 未知路径 → 404；已知路径非 GET → 405；已知路径 GET → 具体端点。
    /// </summary>
    internal static BridgeRouteKind Resolve(string path, string method)
    {
        if (!IsKnownRoute(path))
        {
            return BridgeRouteKind.NotFound;
        }

        if (!IsGet(method))
        {
            return BridgeRouteKind.MethodNotAllowed;
        }

        if (string.Equals(path, InfoPath, StringComparison.OrdinalIgnoreCase))
        {
            return BridgeRouteKind.Info;
        }

        if (string.Equals(path, PlayerPath, StringComparison.OrdinalIgnoreCase))
        {
            return BridgeRouteKind.Player;
        }

        if (string.Equals(path, StatusPath, StringComparison.OrdinalIgnoreCase))
        {
            return BridgeRouteKind.Status;
        }

        if (string.Equals(path, BotsPath, StringComparison.OrdinalIgnoreCase))
        {
            return BridgeRouteKind.Bots;
        }

        if (string.Equals(path, EventsPath, StringComparison.OrdinalIgnoreCase))
        {
            return BridgeRouteKind.Events;
        }

        if (string.Equals(path, LogsRecentPath, StringComparison.OrdinalIgnoreCase))
        {
            return BridgeRouteKind.LogsRecent;
        }

        if (string.Equals(path, LogsSummaryPath, StringComparison.OrdinalIgnoreCase))
        {
            return BridgeRouteKind.LogsSummary;
        }

        // 防御性兜底：IsKnownRoute 已放行但无匹配分支（新路由只加进 IsKnownRoute
        // 而漏加级联分支）时显式 NotFound，绝不静默落到某个端点负载。
        return BridgeRouteKind.NotFound;
    }

    /// <summary>路由结果对应的 HTTP 状态码（200/404/405）。</summary>
    internal static int StatusCodeFor(BridgeRouteKind route)
    {
        switch (route)
        {
            case BridgeRouteKind.NotFound:
                return 404;
            case BridgeRouteKind.MethodNotAllowed:
                return 405;
            default:
                return 200;
        }
    }

    /// <summary>detail 查询参数判定：<c>1</c> / <c>true</c>（大小写不敏感）。</summary>
    internal static bool IsDetailRequested(string detailValue)
    {
        return string.Equals(detailValue, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(detailValue, "true", StringComparison.OrdinalIgnoreCase);
    }
}
