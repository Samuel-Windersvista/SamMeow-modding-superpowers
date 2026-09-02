---
version: [4.1]
domain: server
topic: routes
source: curated
---
# HTTP 路由笔记 [4.1]

> 状态：已提炼（源自迁移文档 + 示例 10/13/24；内部实现待源码核对）
> 适用：[4.1] | 源码：`E:\云文件\GitHub\SamMeow_SPT410_source_code`

## 两类端点，两个体系（4.1 明确分工）

| 端点类型 | 体系 | 用途 |
|---------|------|------|
| 游戏流量（客户端发的 Tarkov 请求） | **Router**（`SPTarkov.Server.Core.Routers`） | 覆盖/新增游戏路由、item event |
| 网页/工具流量 | **MVC Controller**（Web 页面体系） | 你自己的 API、Blazor 页面调用 |

游戏客户端的一切请求必须走 Router；Controller 只服务你自己的 web 页面与外部工具。

## Router 规则（4.1）

1. **优先级生效**：`[Injectable(TypePriority = OnLoadOrder.Routers + n)]`
   - 新路由：`Routers + 1` 起
   - 覆盖 SPT 路由：`-1` = 你先处理；`+1` = SPT 先处理
   - 多 mod 撞同一路由：先注册者胜
2. **action 签名**：必须含 `CancellationToken`（来自 `HttpContext.RequestAborted`），不用也要声明
3. **强类型 body**：`RouteAction<TRequest>`，`TRequest : IRequestData`；body 到达即已反序列化
4. **Item Event**：`ItemEventRouter` 构造传 `ItemRouteAction<T>` 列表；不再 switch URL；`BaseInteractionRequestDataConverter` 移除

## 示例坐标

- 自定义静态路由：`E-Mod开发示例/server-mod-examples/10CustomRoute/CustomStaticRouter.cs`
- Item event：迁移文档第 6 节（4.0/4.1 对照）
- 商人图片路由：示例 13 中 `imageRouter.AddRoute(...)`（ImageRouter 也是可注入路由）

## 源码坐标（已核实 2026-08-02）

- 基类与实现：`Libraries/SPTarkov.Server.Core/Routers/`（`HttpRouter.cs` 是核心；`EventOutputHolder.cs` 处理事件输出）
- 内置路由清单（实读目录）：
  - 静态/动态路由：`ImageRouter.cs`、`BotDynamicRouter.cs`、`BundleDynamicRouter.cs`、`CustomizationDynamicRouter.cs`、`DataDynamicRouter.cs`、`InraidDynamicRouter.cs`、`NotifierDynamicRouter.cs`、`TraderDynamicRouter.cs`
  - Item Event 路由（独立类，非 switch）：`CustomizationItemEventRouter.cs`、`HealthItemEventRouter.cs`、`HideoutItemEventRouter.cs`、`InsuranceItemEventRouter.cs`、`InventoryItemEventRouter.cs`、`NoteItemEventRouter.cs`、`QuestItemEventRouter.cs`、`RagfairItemEventRouter.cs`、`RepairItemEventRouter.cs`、`TradeItemEventRouter.cs`
  - 存档加载：`Routers/SaveLoad/ProfileSaveLoadRouter.cs`
  - 加载器路由：`Routers/Static/ModLoaderRouter.cs`
- 路由收集/排序：DI 注入 `IEnumerable<AbstractRouter>`（多实例）→ 按 TypePriority 排序（见 di-container.md）
- 客户端枚举通道：`/singleplayer/customEnumEntries`（见 EnumExtensions.md）
