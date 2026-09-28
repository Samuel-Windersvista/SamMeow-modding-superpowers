using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;
using {{ROOT_NAMESPACE}}.Shared;

namespace {{ROOT_NAMESPACE}}.Server.Routers;

/// <summary>
/// 示例静态路由（StaticRouter = URL 精确匹配）。
/// 路由形态取自 5.0 真实工程 tools/tarkov-active-probe/src/ActiveProbeRouter.cs：
/// 主构造函数注入 JsonUtil / HttpResponseUtil，基类构造接收 JsonUtil 与一个 RouteAction 列表。
///
/// 约定：
/// - [Injectable(TypePriority = OnLoadOrder.Routers)] 让 DI 容器收集本路由（Routers = 400000）。
/// - 路由路径是**完整绝对路径**；mod 自定义路由使用独立前缀 `/spt/...`，避免与内建路由冲突
///   （kb: api-notes-5.0/http-routing.md:71）。
/// - 路径取自 Shared 工程的 SharedConstants.ExampleRoute（值 `/spt/&lt;ModClassName&gt;/example`），
///   使两端/两工程使用同一来源，避免漂移（STD-STRUCT-004）。
/// - 同一组内多个 router 都可能命中，后者覆盖响应；静态路由先于动态路由执行。
/// - 返回值经 HttpResponseUtil 包装；NoBody / GetBody 的用法见 SPTarkov.Server.Core.Utils.HttpResponseUtil。
/// - 示例为最小内联处理；生产 mod 按 `STD-SRV-007` 把业务逻辑移入可注入的 Callbacks 类（见 modding-standard/04-server.md）。
/// </summary>
[Injectable(TypePriority = OnLoadOrder.Routers)]
public class {{MOD_CLASS_NAME}}Router(
    JsonUtil jsonUtil,
    HttpResponseUtil httpResponseUtil
) : StaticRouter(
    jsonUtil,
    [
        new RouteAction<EmptyRequestData>(
            SharedConstants.ExampleRoute,
            (_, _, _, _, _) =>
            {
                // 演示：返回一个 JSON 对象（NoBody 按内建响应包装序列化）。
                return new ValueTask<string>(
                    httpResponseUtil.NoBody(new { message = "Hello from {{MOD_NAME}}!" })
                );
            }
        ),
    ]
)
{ }
