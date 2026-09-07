---
version: [3.11]
domain: server
topic: architecture
source: curated
---
# SPT 3.11 服务端架构图（源码提炼）

> 状态：**已核实（源码实读 2026-09-07）** | 版本：[3.11]
> 源码：`server/project/src/`（`@spt/*` 别名下）顶层目录 + `Program.ts` + `utils/App.ts`

## 总体结构

单进程 Node 应用（Node 22.12.0，`package.json engines`），tsyringe DI 容器 + `@spt/*` 路径别名。启动入口 `src/entry/run.ts`，主装配类 `src/Program.ts`。

```
run.ts → Program → di/Container（注册）→ PreSptModLoader（preSptLoad）→ App.load()（OnLoad/OnUpdate 调度）
                                    └──→ childContainer（mod 复用）
```

## 启动链时序（`Program.start()`，`Program.ts:20-36`）

| 步 | 动作 | 位置 |
|---|---|---|
| 1 | `Container.registerTypes(container)` 全量注册单例 | Program.ts:22 |
| 2 | `container.createChildContainer()` 子容器 | Program.ts:23-24 |
| 3 | `Watermark.initialize()` | Program.ts:24-25 |
| 4 | `Container.registerListTypes(childContainer)` 注册多标签集合（OnLoad/OnUpdate/StaticRoutes 等） | Program.ts:28 → Container.ts:331-426 |
| 5 | `preSptModLoader.load(childContainer)` → preSptLoad 阶段 | Program.ts:29 |
| 6 | `Container.registerPostLoadTypes(container, childContainer)` 注册 SptHttpListener | Program.ts:31 → Container.ts:290-293 |
| 7 | `childContainer.resolve<App>("App").load()` → OnLoad/OnUpdate 调度 | Program.ts:32 -->
| 8 | HttpServer（HTTPS 监听端口/`httpConfig.port`、挂 WebSocket） | HttpServer.ts:45-73 |
| 9 | 全局错误兜底 | Program.ts:33-35 `errorHandler.handleCriticalError` |

`HttpServer` 在 `App` 之外由外部 base 入口接线（本分支 `App.load()` 不含 `httpServer.load()` 显式调用—— `App.ts:35-70` 只做 OnLoad + OnUpdate 调度）。

## App.load()（`utils/App.ts:35-70`）

- 打印 OS/CPU/RAM/版本；Node 版本核对，不符则 `process.exit(1)`（App.ts:44-53）
- 顺序执行所有 `@injectAll("OnLoad")` 的 `onLoad()`（App.ts:63-65）
- `setInterval(5000)` 每 5 秒执行 `update()`（App.ts:67-69）
- `update()`（App.ts:72-102）：`httpServer.isStarted()` 与 `databaseService.isDatabaseValid()` 任一为假则跳过；每个 OnUpdate 调 `onUpdate(secondsSinceLastRun)`，try-catch（L83-87），返回 `true` 才更新 `onUpdateLastRun[route]`（L89-90），否则 20 分钟 debug 日志

## 请求链（HTTP）

```
HttpServer.handleRequest (解析 SESSION_ID → ApplicationContext, HttpServer.ts:130-162)
  → SptHttpListener (注册于 HttpListener tag)
    → HttpRouter.getResponse (HttpRouter.ts:26-45)
      → StaticRouter (精确匹配, StaticRoutes) → DynamicRouter (包含匹配, DynamicRoutes)
        → Callbacks (URL → controller)
          → Controller → Services → Helpers
    → Serializer (ImageRouter/Bundle/Notify 哨兵: "IMAGE"/"BUNDLE"/"NOTIFY")
  → WebSocketServer (setupWebSocket, 独立通道)
```

## 目录职责（`server/project/src/`）

| 目录 | 职责 |
|---|---|
| `entry/` | 进程引导（run.ts、build.json 元数据） |
| `di/` | tsyringe 注册中心 + 路由/生命周期基类（Container/Router/OnLoad/OnUpdate/Serializer） |
| `loaders/` | mod 加载链（PreSpt/PostSpt/PostDB ModLoader、ModLoadOrder、ModTypeCheck、BundleLoader） |
| `servers/` | 运行时服务（HttpServer/WebSocketServer/DatabaseServer/SaveServer/ConfigServer/RagfairServer） |
| `routers/` | HTTP 分发（HttpRouter/ImageRouter/ItemEventRouter/EventOutputHolder + static/dynamic/item_events/serializers 分组） |
| `callbacks/` | URL → controller 回调绑定层 |
| `controllers/` | 29 个业务控制器（见下） |
| `services/` | 业务编排层（存档/经济/保险/Fence/邮件/修理/支付 + `mod/` 子目录的 mod 注册服务 + `cache/`） |
| `generators/` | 数据生成层（bot/战利品/跳蚤/武器/Scav Case 奖励/天气） |
| `helpers/` | 业务辅助工具（ItemHelper/InventoryHelper/ProfileHelper/TradeHelper/QuestHelper 等） |
| `models/` | 类型定义层（`eft/` 协议镜像、`spt/` 内部模型、`enums/`、`external/` mod 生命周期接口、`common/`） |
| `utils/` | 通用工具（JsonUtil/HashUtil/RandomUtil/FileSystem/ImporterUtil/DatabaseImporter/App/Watermark + logging/、cloners/、collections/） |
| `context/` | 上下文/会话状态（ApplicationContext: Map<ContextVariableType, LinkedList<ContextVariable>>，holderMaxSize=10；枚举 SESSION_ID/RAID_CONFIGURATION/CLIENT_START_TIMESTAMP/REGISTER_PLAYER_REQUEST/RAID_ADJUSTMENTS/TRANSIT_INFO） |

## 29 个控制器（`controllers/`）

Achievement / Bot / Build / ClientLog / Customization / Dialogue / Game（最重）/ Handbook / Health / Hideout / Inraid / Insurance / Inventory（最复杂）/ Launcher（登录注册 wipe）/ Location / Match / Note / Notifier / Preset / Prestige / Profile / Quest / Ragfair / Repair / RepeatableQuest / Trade / Trader / Weather / Wishlist

## 关键引用

- `Program.ts:20-32` 启动装配
- `ProgramStatics.ts:18-57` 静态常量（ENTRY_TYPE/DEBUG/COMPILED/MODS/SPT_VERSION），从 `entry/build.json` 读取
- `Container.ts:295-316` registerTypes；`:331-426` registerListTypes（多标签）；`:290-293` registerPostLoadTypes
- `App.ts:35-70` OnLoad/OnUpdate 调度
- `HttpServer.ts:45-73,130-162` HTTPS 服务与请求分发
