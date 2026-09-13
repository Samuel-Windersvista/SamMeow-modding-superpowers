using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Services.Profile;
using SPTarkov.Server.Core.Utils;

namespace TarkovActiveProbe;

/// <summary>
/// 活跃探针路由（ticket 08 B 部分）：POST /spt/runtime/active-profiles
///
/// 返回 {"activeProfiles": ["<profileId>", ...]}——近 30 分钟内活跃
/// （与 server 自带 Blazor 状态页同一窗口参数）的 profileId 列表。
/// 数据来自 server 内部 ProfileActivityService：玩家客户端每个带 cookie 的
/// 请求都会刷新其 LastActive 时间戳。
///
/// tarkov-runtime-MCP 握手时探测本路由：可用且恰有 1 个活跃 profile 时
/// 跟随玩家选择；本 mod 未安装时 MCP 静默退回自动选择规则（渐进增强）。
/// </summary>
[Injectable(TypePriority = OnLoadOrder.Routers)]
public class ActiveProbeRouter(
    JsonUtil jsonUtil,
    HttpResponseUtil httpResponseUtil,
    ProfileActivityService profileActivityService
) : StaticRouter(
    jsonUtil,
    [
        new RouteAction<EmptyRequestData>(
            "/spt/runtime/active-profiles",
            (_, _, _, _, _) =>
            {
                var activeProfiles = profileActivityService.GetActiveProfileIdsWithinMinutes(30);
                return new ValueTask<string>(
                    httpResponseUtil.NoBody(new { activeProfiles })
                );
            }
        ),
    ]
)
{ }
