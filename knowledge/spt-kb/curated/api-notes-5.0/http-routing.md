---
version: [5.0]
domain: server
topic: routes
source: curated
---
# HTTP 路由笔记 [5.0]

> **[UNSTABLE-PREVIEW]** SPT 5.0 处于开发初期（`5.0x-dev` 分支，尚无正式版）。本笔记为 2026-09-13 源码快照（HEAD `ff0bf3281`）提炼，上游 API 可能变更；使用前请以源码复核，勿据此做长期承诺。

> 状态：**源码实读（2026-09-13）** | 版本：[5.0]
> 源码：`Libraries/SPTushonka.Server.Core/DI/Router.cs`、`DI/Routing/ItemEventRouter.cs`、`DI/ISerializer.cs`、`Routers/HttpRouter.cs`、`Servers/HttpServer.cs`、`Servers/Http/SptHttpListener.cs`、`Routers/Serializers/`、`Routers/Static/`、`Routers/Dynamic/`、`Routers/ItemEvents/`、`Routers/SaveLoad/`

## 路由基类

| 基类 | 匹配 / 职责 | 配套 action | 位置 |
|------|------------|-------------|------|
| `Router`（abstract） | 顶层基类；`CanHandle` + `OnBeforeAction`/`OnAfterAction` 事件 | — | `DI/Router.cs:22-60` |
| `StaticRouter`（abstract） | 精确匹配（`route.url == url`） | `RouteAction` / `RouteAction<T>` | `DI/Router.cs:62-91` |
| `DynamicRouter`（abstract） | 包含匹配（`url.Contains(route.url)`） | `RouteAction` / `RouteAction<T>` | `DI/Router.cs:93-122` |
| `ItemEventRouter`（abstract） | ItemEvent 路由（按 Action 精确匹配） | `ItemRouteAction` / `ItemRouteAction<T>` | `DI/Routing/ItemEventRouter.cs:8-42` |
| `SaveLoadRouter`（abstract） | Profile 加载钩子 | — | `DI/Router.cs:148-159` |

- `HandledRoute(route, dynamic)`（`DI/Router.cs:161`）：`CanHandle` 内部用 `GetHandledRoutes()` 产出的路由描述。
- `Router.CanHandle(url, partialMatch)`（`DI/Router.cs:51-59`）：
  - `partialMatch=true` → 只取 `dynamic` 路由且 `url.Contains(r.route)`（`:53-56`）；
  - `partialMatch=false` → 只取非 `dynamic` 路由且 `r.route == url`（`:58`）。

## Action 记录类型（签名）

`DI/Router.cs`：

```csharp
public record RouteAction(
    string url,
    Func<string, IRequestData, MongoId, string?, CancellationToken, ValueTask<object>> action,
    Type? bodyType = null);                                    // :179-183

public record RouteAction<TRequest>(
    string url,
    Func<string, TRequest, MongoId, string?, CancellationToken, ValueTask<string>> typedAction)
    : RouteAction(...) where TRequest : class, IRequestData;   // :200-209

public record StreamedRouteAction<TRequest>(
    string url,
    Func<string, TRequest, MongoId, CancellationToken, ValueTask<StreamedJsonBody>> typedAction)
    : RouteAction(...) where TRequest : class, IRequestData;   // :227-239
```

`DI/Routing/ItemEventRouter.cs`：

```csharp
public abstract record ItemRouteAction(
    string Url,
    Func<string, PmcData, BaseInteractionRequestData, MongoId, ItemEventRouterResponse, CancellationToken,
         ValueTask<ItemEventRouterResponse>> Action,
    Type BodyType);                                            // :44-56

public sealed record ItemRouteAction<TRequest>(...)            // :58-77（运行时校验 body 类型，不匹配抛 InvalidOperationException）
    where TRequest : BaseInteractionRequestData;
```

- `RouteAction<T>` 反序列化 body 时用 `action.bodyType`；body 为空则 `info ??= new EmptyRequestData()`（`DI/Router.cs:72-84`）。
- `StreamedRouteAction<T>` 返回 `StreamedJsonBody`，`SptHttpListener` 直接流式序列化（`Servers/Http/SptHttpListener.cs:147-156`）。

## URL 组装 / top-level route

- 5.0 **没有** 3.11 的 `getTopLevelRoute()` / `topLevelRoute` 概念（全库未找到该标识符）。
- 路由匹配直接对 `HttpContext.Request.Path.Value`（`Routers/HttpRouter.cs:14,47`）；`RouteAction.url` 即**完整绝对路径**（例：`/client/achievement/list`，`Routers/Static/AchievementStaticRouter.cs:14-18`）。
- `HttpRouter.HandleRouteAsync` 只做一处 URL 规整：去掉 `?retry=` 后缀（`Routers/HttpRouter.cs:54-58`）。
- mod 自定义路径建议使用独立前缀（如 `/spt/mymod/...`），避免与内建路由冲突。

## HTTP 分发流程

```
Kestrel(HTTPS)
 → SptLoggerMiddleware                                   SPTushonka.Server/Program.cs:273
 → HttpServer.HandleRequestAsync                          Servers/HttpServer.cs:19-45
     ├─ WebSocket & webSocketServer.CanHandle → OnConnectionAsync   :21-25
     └─ 首个 IHttpListener.CanHandle(context)             :27
         → sessionId ← PHPSESSID cookie                    :35-37
         → listener.HandleAsync(sessionId, context, token) :44
             → SptHttpListener.HandleAsync                 Servers/Http/SptHttpListener.cs:53-136
                 ├─ GET → GetResponseObjectAsync            :57-69
                 └─ POST/PUT → DeShuffle + zlib 解压         :71-134
                     → HttpRouter.GetResponseObjectAsync    Routers/HttpRouter.cs:20-35
                         ├─ StaticRouter 组（精确，先）      :28
                         └─ DynamicRouter 组（包含，后）     :29-32
                     → ISerializer 命中哨兵 或 zlib JSON     Servers/Http/SptHttpListener.cs:192-201
                     → StreamedJsonBody / shuffle 帧          :147-156,290-311
```

- `HttpServer` 是 `[Injectable(InjectionType.Singleton)]`，构造注入 `HttpConfig`、`WebSocketServer`、`ProfileActivityService`、`IEnumerable<IHttpListener>`（`Servers/HttpServer.cs:11-17`）。
- `SptHttpListener` 是 `[Injectable]`，支持 `GET/PUT/POST`（`Servers/Http/SptHttpListener.cs:20,32`）；`CanHandle = 方法命中 && httpRouter.CanHandle(context)`（`:48-51`）。
- `HttpRouter` 构造注入 `IEnumerable<StaticRouter>` + `IEnumerable<DynamicRouter>`（`Routers/HttpRouter.cs:10`）；`GetResponseObjectAsync` 先静态后动态（`:28-32`）；同一组内多个 router 可命中，后写覆盖 `wrapper.Output`（`:60-88`）。
- 未命中响应：`SptHttpListener.GetResponseObjectAsync` 用 `HttpResponseUtil.GetBody(null, BackendErrorCodes.HTTPNotFound, ...)` 兜底（`SptHttpListener.cs:320-330`）。

## Serializer / `ISerializer` 机制

```csharp
// DI/ISerializer.cs:6-15
public interface ISerializer
{
    Task SerializeAsync(MongoId sessionID, HttpRequest req, HttpResponse resp, object? body, CancellationToken ct = default);
    bool CanHandle(string route);
}
```

- `SptHttpListener` 构造注入 `IEnumerable<ISerializer> serializers`（`Servers/Http/SptHttpListener.cs:23`），响应时 `serializers.FirstOrDefault(x => x.CanHandle(output))`（`:192`）；命中则 `SerializeAsync`，否则走 zlib JSON（`:193-201`）。
- 内建 serializer 与哨兵串：

| Serializer | 哨兵（`CanHandle`） | 位置 |
|-----------|---------------------|------|
| `BundleSerializer` | `"BUNDLE"` | `Routers/Serializers/BundleSerializer.cs:40-43` |
| `NotifySerializer` | `"NOTIFY"`（忽略大小写） | `Routers/Serializers/NotifySerializer.cs:36-39` |

- 图片不走 serializer，而是独立的 `IHttpListener`：`ImageRouter : IHttpListener`（`Routers/ImageRouter.cs:11-44`），`AddRoute(key, value)` + 按 key 命中后 `HttpFileUtil.SendFileAsync`（`:13-43`）。
- 帧/明文特例：`ShouldShuffleRequest` 对 `/launcher`、`/client/metadata`、`/v2/shop`、`/files` 不 shuffle（`SptHttpListener.cs:399-405`）；`ShouldShuffleResponse` 再排除 `/singleplayer`（`:407-410`）；`SendsPlainJson` 对 `/v2/shop`、`/files` 直接 UTF-8 写（`:412-415`）。

## mod 注册路由：完整示例

### 静态路由（精确匹配）

```csharp
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

[Injectable(TypePriority = OnLoadOrder.Routers)]
public class MyStaticRouter(JsonUtil jsonUtil, MyCallbacks callbacks)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/spt/mymod/ping",
                async (url, info, sessionID, output, cancellationToken)
                    => await callbacks.Ping(url, info, sessionID)
            ),
        ]
    )
{ }
```

（模板源自 `Routers/Static/AchievementStaticRouter.cs:9-25`；`StaticRouter` 构造签名 `DI/Router.cs:62`。）

### 动态路由（包含匹配）

```csharp
[Injectable(TypePriority = OnLoadOrder.Routers)]
public class MyDynamicRouter(JsonUtil jsonUtil, MyCallbacks callbacks)
    : DynamicRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/spt/mymod/asset/",
                async (url, info, sessionID, output, cancellationToken)
                    => await callbacks.GetAsset(url, info, sessionID)
            ),
        ]
    )
{ }
```

（模板源自 `Routers/Dynamic/BotDynamicRouter.cs:9-36`；`DynamicRouter` 构造签名 `DI/Router.cs:93`。）

### ItemEvent 路由

```csharp
using SPTarkov.Server.Core.DI.Routing;
using SPTarkov.Server.Core.Models.Enums;

[Injectable(TypePriority = OnLoadOrder.Routers)]
public sealed class MyItemEventRouter(MyCallbacks callbacks)
    : ItemEventRouter([
        new ItemRouteAction<MyActionRequest>(
            ItemEventActions.ADD_NOTE,   // 用实际 Action 常量
            async (url, pmcData, body, sessionID, output, cancellationToken)
                => await callbacks.HandleMyAction(pmcData, body, sessionID)
        ),
    ])
{ }
```

（模板源自 `Routers/ItemEvents/NoteItemEventRouter.cs:10-26`；`ItemEventRouter` 构造签名 `DI/Routing/ItemEventRouter.cs:8`。）

### Profile 加载钩子（`SaveLoadRouter`）

```csharp
[Injectable(TypePriority = OnLoadOrder.Routers)]
public class MySaveLoadRouter : SaveLoadRouter
{
    protected override List<HandledRoute> GetHandledRoutes() => [new HandledRoute("spt-mymod", false)];
    protected override SptProfile HandleLoadInternal(SptProfile profile)
    {
        // 在 profile 加载后补/清洗数据
        return profile;
    }
}
```

（模板源自 `Routers/SaveLoad/ProfileSaveLoadRouter.cs:8-21`；`SaveLoadRouter` 基类 `DI/Router.cs:148-159`。）

### 另一种方式：MVC Controller

mod 也可用 ASP.NET Core MVC controller 注册端点（示例 `Testing/TestMod/Controllers/TestController.cs:5-11`）：

```csharp
public class TestController : Controller
{
    [HttpGet("/test/ping")]
    public IActionResult Ping() => Content("Pong from MVC!");
}
```

- 注册条件：`StaticRouter`/`DynamicRouter`/`ItemEventRouter`/`SaveLoadRouter` 均标 `[Injectable(TypePriority = OnLoadOrder.Routers)]`，由 DI 自动收集（`HttpRouter` 注入 `IEnumerable<...>`；`DependencyInjectionHandler.InjectAll` 扫描 mod 程序集，见 di-container.md）。
- 路由注册阶段常量：`OnLoadOrder.Routers = 400000`（`DI/OnLoadOrder.cs:9`）。

## 5.0 新增 / 变化

- `/client/quest/list` 改用 `StreamedRouteAction`（`RouteAction` 家族新增流式变体，`DI/Router.cs:227-239`）。
- `/client/dialogue` 改用 `EmptyRequestData`（原 `GetClientDialogueRequestData` 已删除）。
- 删除的请求模型：`GetClientDialogueRequestData`、`GetAchievementListRequest` → 引用者需改代码。
- 新增 `ISerializer` 之外，图片改为 `ImageRouter : IHttpListener` 承载。

## 与 4.1 的关系

- `Router` / `RouteAction` 基类**完全相同** → 路由写法不变（`git diff origin/4.1x-dev...5.0x-dev` 对 `DI` 目录无输出）。

## 已核实位置

- 基类与 action：`Libraries/SPTushonka.Server.Core/DI/Router.cs:22-60,62-91,93-122,148-159,161,179-239`
- ItemEvent：`Libraries/SPTushonka.Server.Core/DI/Routing/ItemEventRouter.cs:8-42,44-77`
- 分发：`Libraries/SPTushonka.Server.Core/Routers/HttpRouter.cs:10-98`、`Servers/HttpServer.cs:11-45`、`Servers/Http/SptHttpListener.cs:20-136,147-204,290-415`
- 序列化：`Libraries/SPTushonka.Server.Core/DI/ISerializer.cs:6-15`、`Routers/Serializers/BundleSerializer.cs:40-43`、`Routers/Serializers/NotifySerializer.cs:36-39`、`Routers/ImageRouter.cs:11-44`
- 模板：`Routers/Static/AchievementStaticRouter.cs:9-25`、`Routers/Dynamic/BotDynamicRouter.cs:9-36`、`Routers/ItemEvents/NoteItemEventRouter.cs:10-26`、`Routers/SaveLoad/ProfileSaveLoadRouter.cs:8-21`、`Testing/TestMod/Controllers/TestController.cs:5-11`
- 阶段常量：`Libraries/SPTushonka.Server.Core/DI/OnLoadOrder.cs:9`
