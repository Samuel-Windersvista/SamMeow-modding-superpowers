using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Request;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended;

/// <summary>
/// SkillsExtended 迁移版路由（3.11 RouteManager 对应）。
/// 2 条静态路由：/skillsExtended/GetSkillsConfig + /skillsExtended/GetKeys
/// </summary>
[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
public class SkillsRoutes(JsonUtil jsonUtil) : StaticRouter(
    jsonUtil,
    new[]
    {
        new RouteAction("/skillsExtended/GetSkillsConfig",
            (_, _, _, _, _) => new ValueTask<object>("{}")),
        new RouteAction("/skillsExtended/GetKeys",
            (_, _, _, _, _) => new ValueTask<object>("{}")),
    })
{
}
