# SPT 运行时状态导出可行性研究

> 研究任务：核实"能否在 SPT (Single Player Tarkov) 运行时从外部进程实时读取游戏状态"，为"升级现有 spt-MCP vs 新建独立 tarkov-runtime-MCP"的架构决策提供证据。
> 目标版本：SPT 4.1（最终锁定版本）。
> 报告存放：本仓库尚无 `docs/research/` 惯例，本次按用户指定路径新建 `docs/research/` 存放。

---

## 一句话结论

**技术上可行，但不存在单一"运行时 MCP"桥；必须按状态生命周期拆成两座桥：**

1. **局外状态（菜单/仓库/商人/任务/存档）**：通过 SPT server 的自定义 HTTP Router 或复用现有 `/client/*` 路由，由外部进程向 server 轮询/订阅即可读取，延迟取决于轮询间隔。
2. **局内实时状态（raid 中玩家位置、血量、AI）**：SPT server 不掌握这些状态，必须写 BepInEx client mod，用 Harmony patch 访问 `Singleton<GameWorld>.Instance.MainPlayer` 等对象，再自建 IPC（本地 HTTP/websocket/命名管道/写文件）导出；本地归档中**未发现**此类实时导出到外部进程的先例。

---

## A. Server 侧可行性：外部进程能否读取局外游戏状态

### A.1 自定义 server mod 可注册任意 HTTP endpoint

SPT 4.1 server 是 .NET 10 + Kestrel + 自研反射 DI 的整体式架构。所有游戏流量走 `Router` 体系，网页/工具流量走 MVC Controller 体系。

- 自定义**游戏路由**：继承 `StaticRouter` / `DynamicRouter` / `ItemEventRouter`，标 `[Injectable(TypePriority = OnLoadOrder.Routers + n)]`，由 DI 注入 `IEnumerable<StaticRouter>` / `IEnumerable<DynamicRouter>` 收集排序。
- 自定义**外部工具 API**：实现 `IModBlazorMetadata` 后使用标准 ASP.NET Core MVC Controller，自动注册。

源码证据（4.1.0 server）：

```csharp
// E:\云文件\GitHub\SamMeow_SPT410_source_code\Libraries\SPTarkov.Server.Core\DI\Router.cs:61-90
public abstract class StaticRouter(JsonUtil jsonUtil, IEnumerable<RouteAction> routes) : Router
{
    public async ValueTask<object> HandleStaticAsync(
        string url, string? body, MongoId sessionId, string output,
        CancellationToken cancellationToken = default)
    {
        var action = routes.Single(route => route.url == url);
        ...
        var result = await action.action(url, info, sessionId, output, cancellationToken);
        return result;
    }
}

public record RouteAction(
    string url,
    Func<string, IRequestData, MongoId, string?, CancellationToken, ValueTask<object>> action,
    Type? bodyType = null);
```

```csharp
// E:\云文件\GitHub\SamMeow_SPT410_source_code\Libraries\SPTarkov.Server.Core\Routers\HttpRouter.cs:8-15
[Injectable]
public class HttpRouter(IEnumerable<StaticRouter> staticRouters, IEnumerable<DynamicRouter> dynamicRoutes)
{
    public bool CanHandle(HttpContext context)
    {
        return staticRouters.Any(sr => sr.CanHandle(context.Request.Path.Value, false))
            || dynamicRoutes.Any(dr => dr.CanHandle(context.Request.Path.Value, true));
    }
}
```

本地迁移示例 `SkillsRoutes.cs` 已实践自定义静态路由：

```csharp
// E:\云文件\GitHub\SamMeow-modding-superpowers\tools\migration-pilots\skills-extended\src\SkillsRoutes.cs:13-22
[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
public class SkillsRoutes(JsonUtil jsonUtil) : StaticRouter(
    jsonUtil,
    new[]
    {
        new RouteAction("/skillsExtended/GetSkillsConfig", (_, _, _, _, _) => new ValueTask<object>("{}")),
        new RouteAction("/skillsExtended/GetKeys", (_, _, _, _, _) => new ValueTask<object>("{}")),
    })
{
}
```

### A.2 现有 SPT server 路由可被外部进程复用

SPT server 已暴露大量 `/client/*`、`/singleplayer/*`、`/launcher/*` 路由，外部进程只要持有 `PHPSESSID` cookie 即可调用（`HttpServer.cs:35-37` 从 cookie 构造 `MongoId sessionId`）。

可复用的局外状态路由示例（4.1.0 server）：

| 状态 | 路由 | 来源 |
|------|------|------|
| 存档列表/创建/状态 | `/client/game/profile/list`, `/client/profile/status`, `/launcher/profiles` | `ProfileStaticRouter.cs:21,45,65` |
| 仓库/角色数据 | `/client/game/profile/items/moving` (Item Event) | `ItemEventStaticRouter.cs:15` |
| 商人设置 | `/client/trading/api/traderSettings` | `TraderStaticRouter.cs:15` |
| 商人详情/报价 | `/client/trading/api/getTrader/{id}`, `/client/trading/api/getTraderAssort/{id}` | `TraderDynamicRouter.cs:15,19` |
| 任务列表 | `/client/quest/list` | `QuestStaticRouter.cs:16` |
| 藏身处 | `/client/hideout/production/recipes`, `/client/hideout/settings`, `/client/hideout/areas`, `/client/hideout/qte/list` | `DataStaticRouter.cs:40,44,48,56` |
| 全局设置/物品/手册 | `/client/settings`, `/client/items`, `/client/handbook/templates` | `DataStaticRouter.cs:16,24,28` |
| 地图 | `/client/locations` | `LocationStaticRouter.cs:16` |
| raid 菜单设置 | `/singleplayer/settings/raid/menu` | `InraidStaticRouter.cs:20` |

**关键限制**：这些路由主要用于客户端-服务器通信，响应大多经过 zlib 压缩/shuffle（`SptHttpListener.cs:57-104,215-224`）；外部进程调用需处理压缩与 cookie，且多数为**轮询式请求-响应**，不是 server 主动推送。

### A.3 Server 内存中的状态对 mod 完全可读

SPT 4.1 把数据库表和配置全部作为 DI 单例暴露：

```csharp
// E:\云文件\GitHub\SamMeow_SPT410_source_code\Libraries\SPTarkov.Server.Core\Servers\SaveServer.cs:19-35
[Injectable(InjectionType.Singleton)]
public sealed class SaveServer(...)
{
    private const string profileFilepath = "user/profiles/";
    private readonly ConcurrentDictionary<MongoId, SptProfile> profiles = new();
    ...
    public SptProfile GetProfile(MongoId sessionId) { ... }
    public Dictionary<MongoId, SptProfile> GetProfiles() { ... }
}
```

十张表单例（来自 KB `database-structure.md`）：`GlobalTable`、`TemplateTable`（含 `Items/Quests/Handbook/Prices/Customization/Achievements/Profiles/LocationServices`）、`BotTable`、`HideoutTable`、`LocationTable`、`TradersTable`、`LocaleTable`、`MatchTable`、`ServerTable`、`SettingsTable`。

因此，写一个 server mod 注册 `/spt/mymod/profile` 之类路由，把 `SaveServer.GetProfile(sessionId)` 或任意表序列化后返回，是**完全可行且被官方机制支持的**。

---

## B. Client 侧可行性：局内实时状态导出的技术路径

### B.1 Server 不掌握局内实时状态

SPT server 的 `Inraid` 服务（`LocationLifecycleService`、`RaidTimeAdjustmentService`、`MatchLocationService` 等）只处理 raid 启动/结束、时间调整、bot 配置、空投等**规则层**数据。玩家实时位置、血量、AI 行为、当前武器状态等**模拟层**数据全部在 EFT 客户端内存中**，不在 server 内存里。

证据：`InraidDynamicRouter.cs` 唯一动态路由是 `/client/location/getLocalloot`（raid 开始时注册玩家并返回本地战利品），没有查询"当前战局中玩家血量/位置"的路由。

### B.2 Client mod 可以访问局内状态

本地反编译缓存中的 SPT 3.11.4 官方 client mod 展示了通过 `Singleton<GameWorld>.Instance.MainPlayer`、玩家事件、`ActiveHealthController` 等访问局内状态的标准做法：

```csharp
// E:\云文件\GitHub\SamMeow-modding-superpowers\external\decompile-cache\spt-3.11.4-plugins\spt-singleplayer\SPT.SinglePlayer.Patches.RaidFix\EmptyInfilFixPatch.cs:64
Vector3 position = Singleton<GameWorld>.Instance.MainPlayer.Transform.position;
```

```csharp
// E:\云文件\GitHub\SamMeow-modding-superpowers\external\decompile-cache\spt-3.11.4-plugins\spt-singleplayer\SPT.SinglePlayer.Patches.MainMenu\AmmoUsedCounterPatch.cs:17-22
[PatchPostfix]
public static void PatchPostfix(Player __instance)
{
    if (__instance.IsYourPlayer)
    {
        __instance.Profile.EftStats.SessionCounters.AddLong(1L, SessionCounterTypesAbstractClass.AmmoUsed);
    }
}
```

```csharp
// E:\云文件\GitHub\SamMeow-modding-superpowers\external\decompile-cache\spt-3.11.4-plugins\spt-singleplayer\SPT.SinglePlayer.Patches.MainMenu\ArmorDamageCounterPatch.cs:19-20
[PatchPostfix]
public static void PatchPostfix(DamageInfoStruct damageInfo)
{
    if (damageInfo.Player != null && damageInfo.Player.iPlayer != null && damageInfo.Player.iPlayer.IsYourPlayer ...)
```

4.1 客户端已反混淆，类型名/命名空间可见（`wiki/SPT_41/modding/client/Class_Name_Mappings.md`），所以上述路径在 4.1 中更易实现。

### B.3 导出 IPC 路径

Client mod 拿到状态后，要传给外部进程，可选技术路径：

| 路径 | 优点 | 缺点 | 先例（本地） |
|------|------|------|-------------|
| **HTTP client 回 SPT server** | 复用现有 server 路由/自定义路由；server 再暴露给外部 | 每帧/高频数据会大量回环；server 需做桥 | 未发现 |
| **WebSocket client 连外部服务** | 低延迟、server 主动推送 | 需外部 websocket 服务；client 需自己建 ws 客户端 | 未发现 |
| **命名管道 / TCP / UDP** | 低延迟、本地直接 | 需额外 listener 进程；权限/防火墙 | 未发现 |
| **写日志/JSON 文件 + FileSystemWatcher** | 实现简单 | IO 开销大、实时性差、文件锁风险 | 未发现 |

**本地未发现任何将局内实时状态导出到外部进程的 mod 先例**。SPT 官方 client mod 仅把状态用于修正本地计数器、修复 bug、与 server 做常规请求（如 `RequestHandler.PostJson("/singleplayer/settings/getRaidTime", ...)`），没有外部 overlay/实时面板/Discord Rich Presence 类实现。

---

## C. 实时性评估

### C.1 Server 侧：轮询 vs 推送

- **HTTP 轮询**：外部进程每隔 N 秒向自定义路由或现有路由发请求。延迟 = 轮询间隔 + 请求处理时间（Kestrel + router + 序列化，通常 < 50 ms）。适合库存、商人、任务等**变化不频繁**的状态。
- **WebSocket 推送**：SPT server 已有 WebSocket 基础设施（`WebSocketServer.cs`、`SptWebSocketConnectionHandler.cs`），但当前仅用于 `notifierServer/getwebsocket/` 通知通道。mod 可实现 `IWebSocketConnectionHandler` 注册自己的 hook URL，并调用 `SptWebSocketConnectionHandler.SendMessageAsync(sessionID, ...)` 主动向已连接的外部客户端推送事件。源码证据：

```csharp
// E:\云文件\GitHub\SamMeow_SPT410_source_code\Libraries\SPTarkov.Server.Core\Servers\Ws\IWebSocketConnectionHandler.cs:5-8
public interface IWebSocketConnectionHandler
{
    string GetHookUrl();
    Task OnConnectionAsync(WebSocket ws, HttpContext context, string sessionIdContext);
    Task OnMessageAsync(byte[] receivedMessage, WebSocketMessageType messageType, WebSocket ws, HttpContext context);
    Task OnCloseAsync(WebSocket ws, HttpContext context, string sessionIdContext);
}
```

```csharp
// E:\云文件\GitHub\SamMeow_SPT410_source_code\Libraries\SPTarkov.Server.Core\Servers\Ws\SptWebSocketConnectionHandler.cs:183-211
public Task SendMessageAsync(MongoId sessionID, WsNotificationEvent output)
{
    WebSocket[] targets;
    lock (_socketsLock)
    {
        targets = _sockets.GetValueOrDefault(sessionID)?.Values.Where(s => s.State == WebSocketState.Open).ToArray() ?? [];
    }
    var payload = Encoding.UTF8.GetBytes(jsonUtil.Serialize(output, output.GetType())!);
    return SendRawToSocketsAsync(targets, payload);
}
```

**注意**：KB `415-source-review-report.md` 指出 `Helpers/Server/HttpServerHelper.cs:82-91` 的 `SendTextJson` 是空实现，导致 NOTIFY HTTP 长轮询通道失效；但 WebSocket 通道本身独立，不依赖该空方法。

### C.2 Client 侧：取决于 IPC 方式

- Harmony patch 采集状态：可做到每帧/事件级（< 16 ms）。
- 命名管道/TCP/UDP：本地 loopback 通常在 1 ms 内。
- WebSocket client：取决于外部服务，本地通常 < 10 ms。
- 写文件 + watcher：受磁盘/刷新间隔影响，通常 50 ms~数秒。

---

## D. 结论：运行时 MCP 是否可行及推荐形态

### D.1 可行性判断

- **局外状态（server 侧）**：高度可行，推荐作为第一阶段。
- **局内实时状态（client 侧）**：可行，但工作量显著更大，需要同时维护 client mod + 外部 IPC；且无本地先例可参考。
- **统一"运行时 MCP"**：不存在一座桥同时覆盖 server 局外状态与 client 局内状态；必须拆两座桥。

### D.2 推荐桥形态

| 状态域 | 推荐形态 | 理由 |
|--------|---------|------|
| 局外状态 | **Server mod HTTP bridge** | 官方支持、无需 client 改动、可复用现有路由、MCP server 用普通 HTTP client 即可访问 |
| 局内实时状态 | **Client BepInEx mod + 本地 websocket/命名管道 bridge** | client 是唯一拥有实时状态的地方；websocket/命名管道延迟最低；外部 MCP server 可做一个本地 proxy 转发 |
| 事件通知 | 可选 **server-side WebSocket hook** | 若 MCP 需要 server 事件（如商人补货、任务完成），可注册 `IWebSocketConnectionHandler` 主动推送 |

### D.3 与"升级 spt-MCP vs 新建 tarkov-runtime-MCP"决策的关系

- **server 侧状态**与现有 `spt-mcp`（文件级 mod 分析、metadata、冲突审计）**属于不同生命周期**：现有 spt-MCP 是静态/离线工具；运行时状态需要 server 进程在线、需要 session cookie、需要处理压缩/认证。
- **client 侧状态**完全超出 spt-MCP 当前范围，必须新建组件。
- 因此：
  - 若只读局外状态：可在现有 spt-MCP 中新增"连接到本地 SPT server"的 runtime 模式。
  - 若还要读局内状态：**强烈建议新建独立 tarkov-runtime-MCP**，因为传输方式、状态生命周期、依赖组件（BepInEx client mod）与现有 spt-MCP 差异过大，合并会导致职责混乱。

---

## E. 顺带证据：影响合并 vs 独立决策的技术事实

1. **传输方式差异**
   - spt-MCP 当前是文件系统操作（`docs/internal/mcp-specs/spt-mcp-design.md:26`："SPT mod 分析是纯文件系统操作，所有工具同步返回"）。
   - 运行时状态需要 HTTP/WebSocket/named pipe 等 IPC，且 SPT server 使用 HTTPS 自签名证书 + zlib 压缩/shuffle（`SptHttpListener.cs:57-104,215-224`）。

2. **状态生命周期差异**
   - spt-MCP 可在 SPT 未启动时工作。
   - 运行时 MCP 必须等待 SPT server 启动；局内状态还必须等待 EFT 客户端进入 raid。

3. **身份验证差异**
   - spt-MCP 无 session 概念。
   - 运行时 MCP 需要持有 `PHPSESSID` cookie（`HttpServer.cs:35-37`）；外部进程需要从 launcher/profile 流程或 server 日志中获取 session id。

4. **扩展点差异**
   - Server 侧扩展点是 `[Injectable]` Router/Controller/WebSocket handler，与 spt-MCP 的 DLL 扫描完全不同。
   - Client 侧扩展点是 BepInEx plugin + Harmony patch，需要引用 EFT 程序集并随 EFT 版本重新编译（`client-mod-anatomy.md`）。

5. **MO2 overlay 规则仍然适用**
   - 任何 game-local 改动（server mod DLL、client mod DLL）都必须以 MO2 mod overlay 形式输出，不得直接写入 SPT 安装目录（仓库硬规则 1）。

---

## 未找到证据的问题

- 本地归档中**未发现**任何已存在的 client mod 将局内实时状态导出到外部进程（overlay/实时面板/Discord Rich Presence/websocket client 等）。
- 本地归档中**未发现** SPT server 有现成端点可直接查询"当前 raid 中玩家血量/位置/AI 状态"；此类状态确实不在 server 侧。
- SPT server 的 `/singleplayer/clientmods` 等端点具体字段细节未在本次研究中展开；若需外部进程复用，建议进一步测试实际请求格式与 cookie 要求。

---

## 主要来源索引

- SPT 4.1.0 server 源码：`E:\云文件\GitHub\SamMeow_SPT410_source_code\`
  - `Libraries/SPTarkov.Server.Core/DI/Router.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/HttpRouter.cs`
  - `Libraries/SPTarkov.Server.Core/Servers/SaveServer.cs`
  - `Libraries/SPTarkov.Server.Core/Servers/HttpServer.cs`
  - `Libraries/SPTarkov.Server.Core/Servers/Http/SptHttpListener.cs`
  - `Libraries/SPTarkov.Server.Core/Servers/WebSocketServer.cs`
  - `Libraries/SPTarkov.Server.Core/Servers/Ws/SptWebSocketConnectionHandler.cs`
  - `Libraries/SPTarkov.Server.Core/Servers/Ws/IWebSocketConnectionHandler.cs`
  - `Libraries/SPTarkov.Server.Core/Servers/Ws/Message/DefaultSptWebSocketMessageHandler.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/Static/ProfileStaticRouter.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/Static/TraderStaticRouter.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/Static/QuestStaticRouter.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/Static/DataStaticRouter.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/Static/InraidStaticRouter.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/Static/LocationStaticRouter.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/Dynamic/TraderDynamicRouter.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/Dynamic/InraidDynamicRouter.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/Serializers/NotifySerializer.cs`
  - `Libraries/SPTarkov.Server.Core/Routers/SaveLoad/ProfileSaveLoadRouter.cs`
- 本地知识库：`E:\云文件\GitHub\SamMeow-modding-superpowers\knowledge\spt-kb\`
  - `curated/api-notes-4.1/http-routing.md`
  - `curated/api-notes-4.1/architecture-map.md`
  - `curated/api-notes-4.1/database-structure.md`
  - `curated/api-notes-4.1/save-profile.md`
  - `curated/api-notes-4.1/di-container.md`
  - `curated/modding-guide/02-server-mod-anatomy.md`
  - `curated/modding-guide/03-client-mod-anatomy.md`
  - `curated/recipes/09-custom-http-routes.md`
  - `curated/recipes/10-mod-communication.md`
  - `wiki/SPT_41/modding/server/Mod_Web_Pages.md`
- 本地迁移示例：`E:\云文件\GitHub\SamMeow-modding-superpowers\tools\migration-pilots\skills-extended\src\SkillsRoutes.cs`
- SPT 3.11.4 client mod 反编译缓存：`E:\云文件\GitHub\SamMeow-modding-superpowers\external\decompile-cache\spt-3.11.4-plugins\`
  - `spt-singleplayer/SPT.SinglePlayer.Patches.MainMenu/AmmoUsedCounterPatch.cs`
  - `spt-singleplayer/SPT.SinglePlayer.Patches.MainMenu/ArmorDamageCounterPatch.cs`
  - `spt-singleplayer/SPT.SinglePlayer.Patches.RaidFix/EmptyInfilFixPatch.cs`
  - `spt-singleplayer/SPT.SinglePlayer.Utils.InRaid/RaidTimeUtil.cs`
  - `spt-singleplayer/SPT.SinglePlayer.Utils.InRaid/RaidChangesUtil.cs`
