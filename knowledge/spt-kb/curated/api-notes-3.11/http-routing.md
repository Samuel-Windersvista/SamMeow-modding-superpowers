---
version: [3.11]
domain: server
topic: routes
source: curated
---
# HTTP 路由笔记 [3.11]

> 状态：**已核实（源码实读 2026-09-07）** | 版本：[3.11]
> 源码：`server/project/src/routers/HttpRouter.ts`、`routers/ImageRouter.ts`、`routers/ItemEventRouter.ts`、`routers/EventOutputHolder.ts`、`di/Router.ts`、`di/Serializer.ts`、`servers/HttpServer.ts`、`servers/WebSocketServer.ts`

## HTTP 路由分发（`routers/HttpRouter.ts`，72 行）

`HttpRouter.getResponse(req, info, sessionID)`（:26-45）：
1. 移除 URL 中 `?retry=` 后缀（:30-33）
2. **先静态后动态**：`handleRoute(..., staticRouters, false)`（:34）；若**未命中**（`!handled`）再 `handleRoute(..., dynamicRoutes, true)`（:36）。静态路由优先级高于动态路由。
3. `handleRoute`（:47-67）：遍历该组所有 router，凡 `route.canHandle(url, dynamic)` 为真则执行并继续遍历（可能多个命中，后者覆盖 `wrapper.output`）。

## 匹配语法（`di/Router.ts:23-32`）

- **静态**（`partialMatch=false`）：`route.route === url` 精确全等，只考虑 `!dynamic` 的路由
- **动态**（`partialMatch=true`）：`url.includes(route.route)` 子串包含，只考虑 `dynamic` 的路由；动态路由因此常用后缀（`.jpg`/`.png`/`.ico`）或短 token 命中
- 静态/动态两组路由互不干扰；ItemEvent 路由走独立匹配（见下）

## ItemEvent 路由（`routers/ItemEventRouter.ts`，54 行）

`handleEvents(info, sessionID)`（:27-53）：
- 遍历请求 body 的 `info.data` 事件数组，按 `body.Action` 精确匹配 `itemEventRouters.find(r => r.canHandle(Action))`（:33）
- 命中后执行 `handleItemEvent`；若 `output.warnings.length > 0` 则中断后续事件（:37-39）
- 读取 `eventOutputHolder.getOutput(sessionID)` 组装响应，处理后克隆+`resetOutput`（:44-52）

`EventOutputHolder`（`routers/EventOutputHolder.ts`，213 行）：为每个 session 维护 `IItemEventRouterResponse`（经验/技能/藏身处生产/商人关系等），`resetOutput`（:43-64）初始化 `{ warnings, profileChanges }` 基座，`updateOutputProperties`（:70-92）从 profile 拉经验/健康/技能/生产/商人关系。`constructTraderRelations`（:106-121）、`getProductionsFromProfileAndFlagComplete`（:150-193）。

动作枚举：`models/enums/ItemEventActions.ts`（`ItemEventRouterDefinition.handleItemEvent` 覆盖）：

| 动作 | 值 |
|---|---|
| Move | `"Move"` |
| Remove | `"Remove"` |
| Split | `"Split"` |
| Merge | `"Merge"` |
| Transfer | `"Transfer"` |
| Swap | `"Swap"` |
| Fold | `"Fold"` |
| Toggle | `"Toggle"` |
| Tag | `"Tag"` |
| Bind | `"Bind"` |
| Unbind | `"Unbind"` |
| Examine | `"Examine"` |
| ReadEncyclopedia | `"ReadEncyclopedia"` |
| ApplyInventoryChanges | `"ApplyInventoryChanges"` |
| CreateMapMarker | `"CreateMapMarker"` |
| DeleteMapMarker | `"DeleteMapMarker"` |
| EditMapMarker | `"EditMapMarker"` |
| OpenRandomLootContainer | `"OpenRandomLootContainer"` |
| HideoutQteEvent | `"HideoutQteEvent"` |
| RedeemProfileReward | `"RedeemProfileReward"` |
| SetFavoriteItems | `"SetFavoriteItems"` |
| QuestFail | `"QuestFail"` |
| PinLock | `"PinLock"` |

## 图片路由（`routers/ImageRouter.ts`，36 行）

- `addRoute(key, valueToAdd)`（:16-18）→ `imageRouteService.addRoute`
- `sendImage(sessionID, req, resp, body)`（:20-31）：`ImageRouteService.existsByKey` → `httpFileUtil.sendFileAsync`
- `getImage(): string { return "IMAGE"; }`（:33-35）——**哨兵值**，供 `HttpDynamicRouter` 的 `.jpg`/`.png`/`.ico` 路由返回

## 序列化哨兵（`di/Serializer.ts`）

三个内建 Serializer 与哨兵：

| Serializer | 哨兵（canHandle 的 route 串） | 用途 |
|---|---|---|
| `BundleSerializer`（routers/serializers/BundleSerializer.ts:40-42） | `"BUNDLE"` | `/bundle/<key>` 文件应答 |
| `ImageSerializer` | `"IMAGE"` | 图片应答 |
| `NotifySerializer` | `"NOTIFY"` | 通知/轮询 |

## 服务端接线（`servers/HttpServer.ts`，199 行）

- `load()`（:45-73）：创建 HTTPS server、监听 `httpConfig.port/ip`（:55）、挂 WebSocket（:72）
- `createHttpsServer()`（:78-98）：cert/key（自读 `user/certs/localhost.*`，缺失则自签；sha256、keySize 4096、TLS 1.2-1.3）
- `handleRequest(req, resp)`（:130-162）：解析 `SESSION_ID` 写入 `ApplicationContext`，遍历 httpListeners 找 `canHandle` 的交给 `handle`

## WebSocket（`servers/WebSocketServer.ts`，73 行）

- `setupWebSocket(httpServer)`（:31-46）：`new Server({ server, WebSocket: SPTWebSocket })`
- `wsOnConnection(ws, req)`（:58-72）：按 `req.url` 匹配 handler 并 `onConnection`（`@injectAll("WebSocketConnectionHandler")`）

## mod 挂路由的推荐路径

```
preSptLoad 阶段（PreSptModLoader，before DB）:
  → StaticRouterModService.registerStaticRouter(name, routes, topLevelRoute)
  → DynamicRouterModService.registerDynamicRouter(name, routes, topLevelRoute)
  → HttpListenerModService.registerHttpListener(name, canHandle, handle)
  → ImageRouter.addRoute(key, value)   （图片/静态资源一般用这个）
```
- 路由注册必须在 preSptLoad 完成（App 构造前 by @injectAll 收集，见 di-container.md 时机限制）。
- 客户端访问路径：`topLevelRoute + "/" + route.url`（`getTopLevelRoute()` 默认 `"spt"`）。

## 已核实位置

- `routers/HttpRouter.ts:26-45` 先静态后动态
- `routers/static/GameStaticRouter.ts:20-171` 静态路由样例（URL→callback 绑定）
- `routers/dynamic/HttpDynamicRouter.ts:6-29` 动态路由样例（.jpg/.png/.ico 后缀）
- `routers/item_events/InventoryItemEventRouter.ts:10-104` ItemEvent 样例
- `routers/EventOutputHolder.ts:16-92` 事件响应基座
