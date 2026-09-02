---
version: [4.1]
domain: server
topic: recipe
recipe_task: custom-routes
source: curated
---
# 配方：挂自定义 HTTP 路由 [4.1]

> 状态：已提炼（源自迁移文档 Routers 节 + api-notes http-routing.md；基类细节以源码为准）
> 适用：[4.1] | 关联笔记：`../api-notes-4.1/http-routing.md`

## 目标

让客户端/外部工具能访问你的自定义端点（如 `/client/custom/something`），或覆盖 SPT 现有路由。

## 两条路线（先分清）

| 场景 | 用哪个 |
|------|--------|
| 游戏客户端发起的 Tarkov 请求 | **Router**（本文） |
| 你自己的网页/外部工具 API | **MVC Controller**（`IModBlazorMetadata` 体系，见配方 11） |

## 核心规则（4.1）

1. `[Injectable(TypePriority = OnLoadOrder.Routers + n)]` — 永远基于 `OnLoadOrder.Routers` 偏移
   - 新路由：`Routers + 1` 起
   - 覆盖 SPT 路由：`Routers - 1` = 你优先；`Routers + 1` = SPT 优先
2. **action 必须带 `CancellationToken` 参数**（源自 `HttpContext.RequestAborted`），不用也要声明：
   ```diff
   - (url, info, sessionId, output) => { ... }
   + (url, info, sessionId, output, cancellationToken) => { ... }
   ```
3. body 强类型：`RouteAction<TRequest>`（`TRequest : IRequestData`）
4. Item Event（游戏内物品操作）走 `ItemEventRouter`：构造传 `ItemRouteAction<T>` 列表，不 switch URL（示例见迁移文档第 6 节）

## 步骤（普通路由）

1. 元数据 `IModMetadata`
2. 继承路由基类（`AbstractRouter` 或其子类，以源码为准），构造函数注入你的服务
3. 实现 handled routes 与 action（示例 `10CustomRoute/CustomStaticRouter.cs`）
4. `TypePriority = OnLoadOrder.Routers + 1`
5. 客户端若调用你的路由：需客户端 mod 配合（请求路径由客户端 BepInEx 插件发出）

## 验证

- 启动日志无路由冲突
- 用浏览器/Postman 直接 GET 你的端点（服务端路由通常 HTTP 可直达）
- 覆盖路由场景：对比覆盖前后响应

## 坑

- **两个 mod 覆盖同一路由 = 先注册者胜**，无警告——设计时避开
- `RouteAction<TRequest>` 约束 `IRequestData`：body 类型不合 → 异常（含路由名与两类型名）
- 忘加 `CancellationToken` 参数 → 编译期通过但行为未知？不——签名不符会运行时报错，务必按迁移文档改
- WebSocket 类流量：`WebSocketRouter` 体系（示例 24），不走普通 Router

## 来源

- `wiki/SPT_41/Server_40_to_41.md`（Routers 节全文，含代码）
- `E-Mod开发示例/server-mod-examples/10CustomRoute/`、`24Websocket/`
- `../api-notes-4.1/http-routing.md`
