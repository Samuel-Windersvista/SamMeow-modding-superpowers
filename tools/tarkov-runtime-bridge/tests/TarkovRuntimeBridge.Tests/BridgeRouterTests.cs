using System;
using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class BridgeRouterTests
{
    [Theory]
    [InlineData("/bridge/info", "Info")]
    [InlineData("/raid/player", "Player")]
    [InlineData("/raid/status", "Status")]
    [InlineData("/raid/bots", "Bots")]
    [InlineData("/raid/events", "Events")]
    [InlineData("/logs/recent", "LogsRecent")]
    [InlineData("/logs/summary", "LogsSummary")]
    public void Known_route_with_get_resolves_to_endpoint(string path, string expected)
    {
        Assert.Equal(expected, BridgeRouter.Resolve(path, "GET").ToString());
    }

    [Theory]
    [InlineData("/bridge/info")]
    [InlineData("/raid/player")]
    [InlineData("/raid/status")]
    [InlineData("/raid/bots")]
    [InlineData("/raid/events")]
    [InlineData("/logs/recent")]
    [InlineData("/logs/summary")]
    public void Every_known_route_resolves_to_an_explicit_endpoint(string path)
    {
        // 回归护栏：新路由若只加进 IsKnownRoute 而漏加级联分支，Resolve 会落到
        // 兜底 NotFound，此断言即失败（不会静默返回 Events 负载）。
        Assert.True(BridgeRouter.IsKnownRoute(path));
        Assert.NotEqual(BridgeRouteKind.NotFound, BridgeRouter.Resolve(path, "GET"));
    }

    [Fact]
    public void Unmatched_path_falls_back_to_not_found_not_events()
    {
        // 兜底必须是 NotFound：即使路径被 IsKnownRoute 放行却没有级联分支，
        // 也不能静默落到 Events 端点负载。
        Assert.Equal(BridgeRouteKind.NotFound, BridgeRouter.Resolve("/raid/events-typo", "GET"));
    }

    [Theory]
    [InlineData("/BRIDGE/INFO", "Info")]
    [InlineData("/Bridge/Info", "Info")]
    [InlineData("/RAID/BOTS", "Bots")]
    public void Path_matching_is_case_insensitive(string path, string expected)
    {
        Assert.True(BridgeRouter.IsKnownRoute(path));
        Assert.Equal(expected, BridgeRouter.Resolve(path, "GET").ToString());
    }

    [Theory]
    [InlineData("get")]
    [InlineData("Get")]
    [InlineData("gEt")]
    public void Method_matching_is_case_insensitive(string method)
    {
        Assert.True(BridgeRouter.IsGet(method));
        Assert.Equal(BridgeRouteKind.Info, BridgeRouter.Resolve("/bridge/info", method));
    }

    [Theory]
    [InlineData("/bridge/info")]
    [InlineData("/raid/player")]
    [InlineData("/raid/status")]
    [InlineData("/raid/bots")]
    [InlineData("/raid/events")]
    [InlineData("/logs/recent")]
    [InlineData("/logs/summary")]
    [InlineData("/raid/bots/")]
    public void Known_route_with_non_get_is_method_not_allowed(string path)
    {
        if (path == "/raid/bots/")
        {
            // 带尾斜杠不是已知路由：HttpListener 的 AbsolutePath 会原样保留。
            Assert.Equal(BridgeRouteKind.NotFound, BridgeRouter.Resolve(path, "GET"));
            return;
        }

        Assert.Equal(BridgeRouteKind.MethodNotAllowed, BridgeRouter.Resolve(path, "POST"));
        Assert.Equal(BridgeRouteKind.MethodNotAllowed, BridgeRouter.Resolve(path, "DELETE"));
        Assert.Equal(BridgeRouteKind.MethodNotAllowed, BridgeRouter.Resolve(path, null));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/unknown")]
    [InlineData("/raid")]
    [InlineData("/bridge")]
    [InlineData("")]
    [InlineData(null)]
    public void Unknown_path_is_not_found_regardless_of_method(string path)
    {
        Assert.False(BridgeRouter.IsKnownRoute(path));
        Assert.Equal(BridgeRouteKind.NotFound, BridgeRouter.Resolve(path, "GET"));
        Assert.Equal(BridgeRouteKind.NotFound, BridgeRouter.Resolve(path, "POST"));
        Assert.Equal(BridgeRouteKind.NotFound, BridgeRouter.Resolve(path, null));
    }

    [Theory]
    [InlineData("NotFound", 404)]
    [InlineData("MethodNotAllowed", 405)]
    [InlineData("Info", 200)]
    [InlineData("Player", 200)]
    [InlineData("Status", 200)]
    [InlineData("Bots", 200)]
    [InlineData("Events", 200)]
    [InlineData("LogsRecent", 200)]
    [InlineData("LogsSummary", 200)]
    public void Status_code_mapping(string route, int expected)
    {
        Assert.Equal(expected, BridgeRouter.StatusCodeFor(Enum.Parse<BridgeRouteKind>(route)));
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("True", true)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    [InlineData("yes", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Detail_query_flag(string value, bool expected)
    {
        Assert.Equal(expected, BridgeRouter.IsDetailRequested(value));
    }
}
