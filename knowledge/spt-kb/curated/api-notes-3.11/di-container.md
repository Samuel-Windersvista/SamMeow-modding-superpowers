---
version: [3.11]
domain: server
topic: di
source: curated
---
# DI 容器笔记 [3.11]

> 状态：**已核实（源码实读 2026-09-07）** | 版本：[3.11]
> 源码：`server/project/src/di/Container.ts`、`server/project/src/di/Router.ts`、`server/project/src/di/OnLoad.ts`、`server/project/src/di/OnUpdate.ts`

## 内核

- **tsyringe**：`@injectable` / `@inject` / `@injectAll` 装饰器，`reflect-metadata` 在 `entry/run.ts:1` 引入。
- 注册中心 `Container`（`di/Container.ts:289`）是静态类，`registerTypes()` 按组注册（Utils→Routers→Generators→Helpers→Loaders→Callbacks→Servers→Services→Controllers→PrimaryDependencies，Container.ts:295-316）。
- **多标签注册**（`registerListTypes()`，Container.ts:331-426）：`depContainer.registerType(tag, impl)` 把多个实现绑定到同一 tag（token），供 `@injectAll(tag)` 集合注入 —— 这是 3.11 实现「list 服务」的机制。
- `PrimaryLogger→WinstonLogger`、`PrimaryCloner→RecursiveCloner` 绑定（Container.ts:318-329）。
- 子容器 `createChildContainer()` 供 mod 侧注册（Program.ts:23）。

## 关键 tag 常量（mod 开发者可见）

| tag（token） | 内容 | 位置 |
|---|---|---|
| `OnLoad` | DatabaseImporter、GameCallbacks、PostDBModLoader、HandbookCallbacks、HttpCallbacks、SaveCallbacks、TraderCallbacks、ModCallbacks、PresetCallbacks、RagfairPriceService、RagfairCallbacks | Container.ts:338-348 |
| `OnUpdate` | DialogueCallbacks、HideoutCallbacks、TraderCallbacks、RagfairCallbacks、InsuranceCallbacks、SaveCallbacks | Container.ts:349-354 |
| `StaticRoutes` | 23 个 `*StaticRouter` | Container.ts:356-378 |
| `DynamicRoutes` | 9 个 `*DynamicRouter` | Container.ts:379-387 |
| `IERouters` | 11 个 `*ItemEventRouter` | Container.ts:389-399 |
| `Serializer` | Image/Bundle/Notify | Container.ts:401-403 |
| `SaveLoadRouter` | Health/Inraid/Insurance/Profile 存档加载 | Container.ts:404-407 |
| `HttpListener` | SptHttpListener（registerPostLoadTypes，Container.ts:292） | Container.ts:290-293 |
| 其它 | `WebSocketConnectionHandler` / `SptWebSocketMessageHandler` / `DialogueChatBot` / `CommandoCommand` / `SptCommand` / `InventoryMagGen`（weapongen） | Container.ts 全文 |

## 路由基类继承链（`di/Router.ts`，95 行）

```
Router (顶层，getTopLevelRoute() = "spt")
├── StaticRouter (constructor(routes: RouteAction[]))    handleStatic: route.url === url 精确匹配
├── DynamicRouter (constructor(routes: RouteAction[]))   handleDynamic: url.includes(r.url) 包含匹配
├── ItemEventRouterDefinition (handleItemEvent)          注释：本应叫 ItemEventRouter，但名字被占用
└── SaveLoadRouter (handleLoad(profile): Promise<ISptProfile>)
```

- `HandledRoute(route: string, dynamic: boolean)`（Router.ts:83）
- `RouteAction(url, action: (url, info, sessionID, output) => Promise<any>)`（Router.ts:90）
- `canHandle(url, partialMatch)`（Router.ts:23-32）：`partialMatch=true` → 过滤 `dynamic` 路由 + `url.includes(r.route)`；`false` → 过滤 `!dynamic` 路由 + `r.route === url`（精确全等）
- **标准路由基类是 `Router`，不存在 `ApiRouter` / `BaseRouter`**（grep 全库无匹配）

## 生命周期接口（无 I 前缀）

```ts
// di/OnLoad.ts:1-4
export interface OnLoad {
    onLoad(): Promise<void>;
    getRoute(): string;
}
// di/OnUpdate.ts:1-4
export interface OnUpdate {
    onUpdate(timeSinceLastRun: number): Promise<boolean>;
    getRoute(): string;
}
```

## 序列化基类（`di/Serializer.ts`，11 行）

```ts
export class Serializer {
    public async serialize(sessionID, req, resp, body): Promise<void> { throw ... }
    public canHandle(something: string): boolean { throw ... }
}
```
三个内建 Serializer 的哨兵（响应体哨兵串，决定由哪个 serializer 写出）：`"BUNDLE"`（BundleSerializer）/ `"IMAGE"`（ImageSerializer）/ `"NOTIFY"`（NotifySerializer）。

## mod 侧的注册入口（`services/mod/` 子目录）

mod 在生命周期钩子里通过以下服务向容器登记（对应标签）：

| 服务 | 方法 | 绑定的 tag |
|---|---|---|
| `OnLoadModService` (services/mod/onLoad/OnLoadModService.ts:8) | `registerOnLoad(name, onLoad, getRoute)` | `OnLoad` |
| `OnUpdateModService` (services/mod/onUpdate/OnUpdateModService.ts:8) | `registerOnUpdate(name, onUpdate, getRoute)` | `OnUpdate` |
| `StaticRouterModService` (services/mod/staticRouter/StaticRouterModService.ts:9) | `registerStaticRouter(name, routes, topLevelRoute)` | `StaticRoutes` |
| `DynamicRouterModService` (services/mod/dynamicRouter/DynamicRouterModService.ts:9) | `registerDynamicRouter(...)` | `DynamicRoutes` |
| `HttpListenerModService` (services/mod/httpListener/HttpListenerModService.ts:10) | `registerHttpListener(name, canHandle, handle)` | `HttpListener` |
| `ImageRouter.addRoute(key, value)`（routers/ImageRouter.ts:16） | 图片路由 | 直接注册到 ImageRouteService |

**时机限制**：`OnLoad`/`OnUpdate` 的注册必须在 **preSptLoad 回调**里完成——因为 `App` 在 preSptModLoader.load() 之后才被构造（Program.ts:29→:32），`@injectAll` 在 App 构造时已收集。在 postSptLoad/postDBLoad 中注册的 OnLoad/OnUpdate 本轮不会被执行。

## 常用注入样例（内建服务）

```ts
constructor(
    @inject("ConfigServer") configServer: ConfigServer,
    @inject("DatabaseServer") databaseServer: DatabaseServer,
    @inject("SaveServer") saveServer: SaveServer,
    @inject("Logger") logger: ILogger,       // 或 "PrimaryLogger"
) {
    this.coreConfig = configServer.getConfig<ICoreConfig>(ConfigTypes.CORE);
}
```

## 已核实位置

- `di/Container.ts:331-426` 多标签注册（OnLoad/OnUpdate/StaticRoutes/DynamicRoutes/IERouters/Serializer/SaveLoadRouter）
- `di/Router.ts:5-95` 路由基类链
- `services/mod/onLoad/OnLoadMod.ts:3-18`、`services/mod/onUpdate/OnUpdateMod.ts:3-18` 包装类
- `utils/App.ts:29-30` 构造注入 `@injectAll("OnLoad")` / `@injectAll("OnUpdate")`
