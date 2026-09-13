---
version: [5.0]
domain: server
topic: architecture
source: curated
---
# SPT 5.0 服务端架构图（源码实读）

> **[UNSTABLE-PREVIEW]** SPT 5.0 处于开发初期（`5.0x-dev` 分支，尚无正式版）。本笔记为 2026-09-13 源码快照（HEAD `ff0bf3281`）提炼，上游 API 可能变更；使用前请以源码复核，勿据此做长期承诺。

> 状态：**源码实读（2026-09-13）** | 版本：[5.0]
> 源码：`E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code`（`5.0x-dev`，HEAD `ff0bf3281`）
> 源码入口：`SPTushonka.Server/Program.cs`、`SPTushonka.Server/Helpers/ProgramHelpers.cs`、`SPTushonka.Server/Extensions/`、`Libraries/SPTushonka.Server.Core/`

## 总体结构

ASP.NET Core 主机（Kestrel + HTTPS），DI 用自研 `SPTarkov.DI`（非 tsyringe、非原生 `IServiceCollection` 特性扫描）。进程入口 `SPTushonka.Server/Program.cs`，宿主装配在 `SPTushonka.Server/Helpers/ProgramHelpers.cs`。

```
Program.Main
 └─ Program.StartServer
     ├─ ConfigLoader.Initialize                     （静态，读 SPT_Data/configs）
     ├─ ProgramHelpers.CreateEarlySptProvider       （早期 DI：config + locale + ModLoader/ModValidator）
     ├─ ModLoader.RunModLoader                      （加载 user/mods，可提前退出）
     └─ Program.StartServerAfterModLoading
         ├─ DatabaseImporter.LoadHashesAsync/LoadDatabaseAsync
         ├─ ProgramHelpers.CreateNewHostBuilder     （注册 config + DatabaseTables）
         ├─ ProgramHelpers.RegisterSptServicesAsync （Injectable 扫描 + IOnDIConstruct）
         ├─ builder.Build() → ConfigureWebApp
         ├─ RunPreSptLoadCallbacks                  （IOnLoad: Watermark ≤ p < GameCallbacks）
         └─ app.RunAsync("https://{ip}:{port}")
                └─ SPTStartupHostedService.StartAsync （IOnLoad: p ≥ GameCallbacks）
                     └─ ExecuteAsync 每 5s 轮询 IOnUpdate
```

## 启动链时序

### `Program.Main`（`Program.cs:31-176`）

| 步 | 动作 | 位置 |
|---|------|------|
| 1 | `RegisterSatelliteLocalizations()`（把 `.resources` 卫星程序集从根目录重定向到 `SPT_Data/dotnet/<culture>/`） | `Program.cs:33`、`:329-348` |
| 2 | `ProgramStatics.Initialize()`（按 `BuildType` 派生 `DEBUG`/`COMPILED`/`MODS` 静态常量） | `Program.cs:36`、`Utils/ProgramStatics.cs:13-52` |
| 3 | `SptLoggerProvider.Create(ProgramStatics.DEBUG())` 建早期 logger | `Program.cs:38` |
| 4 | `IsRunFromInstallationFolder()` 校验工作目录含 `sptLogger.json`/`sptLogger.Development.json` | `Program.cs:41`、`:318-324` |
| 5 | `StartServer(loggerFactory, args, startupCancellation)` | `Program.cs:54` |

异常兜底：`WebServerPortUnavailableException`（`:63-117`）、`ValidationErrorException`（`:118-134`）、程序集加载失败识别（`:135-171`）、`finally` 释放 logger（`:172-175`）。

### `Program.StartServer`（`Program.cs:178-200`）

| 步 | 动作 | 位置 |
|---|------|------|
| 1 | `ConfigLoader.Initialize(_earlyLogger, token)` → `IReadOnlyDictionary<Type, BaseConfig>` | `Program.cs:182` |
| 2 | `ProgramHelpers.CreateEarlySptProvider(loggerFactory, configuration, ProgramStatics.MODS())` | `Program.cs:183` |
| 3 | 若 `MODS()`：取 `ModLoader` 并 `RunModLoader(args, token)` | `Program.cs:187-197` |
| 4 | `runResult.ShouldStartServer == false` → 直接 return | `Program.cs:191-194` |
| 5 | `loadedMods = runResult.ValidRuntimeMods` | `Program.cs:196` |
| 6 | `StartServerAfterModLoading(...)` | `Program.cs:199` |

### `Program.StartServerAfterModLoading`（`Program.cs:205-267`）

| 步 | 动作 | 位置 |
|---|------|------|
| 1 | 从早期 provider 取 `DatabaseImporter` | `Program.cs:214` |
| 2 | 非 DEBUG 时 `LoadHashesAsync(token)`（校验 `checks.dat`） | `Program.cs:216-220` |
| 3 | `LoadDatabaseAsync(shouldVerify, token)` → `DatabaseTables` | `Program.cs:222-224` |
| 4 | `ProgramHelpers.CreateNewHostBuilder(loggerFactory, configuration, tables)` | `Program.cs:227` |
| 5 | `UseSptLoggerWithoutProvider`；`UseDefaultServiceProvider(ValidateOnBuild=true, ValidateScopes=true)` | `Program.cs:228-234` |
| 6 | `RegisterSptServicesAsync(builder, loadedMods, MODS(), token)` | `Program.cs:235` |
| 7 | `ConfigureKestrel(builder)`（证书 + TLS 1.2/1.3 + 无客户端证书） | `Program.cs:238`、`:287-302` |
| 8 | `builder.Build()` | `Program.cs:240` |
| 9 | `startupCancellation.LinkTo(IHostApplicationLifetime)` | `Program.cs:243` |
| 10 | `ConfigureWebApp(app)` | `Program.cs:246`、`:269-285` |
| 11 | `ForwardedHeaders`（X-Forwarded-For/Proto，清空 KnownProxies） | `Program.cs:249-256` |
| 12 | `RunPreSptLoadCallbacks(_earlyLogger, token)` | `Program.cs:258` |
| 13 | 取 `HttpConfig`，`VerifyWebServerPortAvailable()` | `Program.cs:260-264` |
| 14 | `app.RunAsync("https://{httpConfig.Ip}:{httpConfig.Port}")` | `Program.cs:266` |

`ConfigureWebApp`（`:269-285`）：`UseWebSockets()` → `SptLoggerMiddleware` → `GET /health` → `HttpServer.HandleRequestAsync` 中间件 → `UseSptBlazor()`。

### 宿主装配 `ProgramHelpers`（`SPTushonka.Server/Helpers/ProgramHelpers.cs`）

- `CreateNewHostBuilder`（`:32-62`）：`WebApplication.CreateBuilder(WebRootPath = "./SPT_Data/wwwroot")`（`:39`）；**每个 config 按其 `Type` 注册为单例** `AddSingleton(configEntry.Key, configEntry.Value)`（`:44-47`）；`databaseTables.AddToServices(builder.Services)`（`:59`）。
- `RegisterSptServicesAsync`（`:68-113`）：
  - `new DependencyInjectionHandler(builder.Services)`（`:75`）
  - 扫描 `typeof(Program)`、`typeof(PatchManager)`、`typeof(SPTWeb)` 所在程序集（`:78-80`）
  - `AddSingleton(new ClientEnumDefinitions())`（`:82`）
  - 若 `modsEnabled`：扫描全部已加载 mod 程序集 `AddInjectableTypesFromAssemblies(loadedMods.SelectMany(a => a.Assemblies))`（`:84-87`）
  - 扫描 `SPTStartupHostedService` 程序集（`:89`）
  - `diHandler.InjectAll()`（`:91`）——按 `Injectable.TypePriority` 升序注册，并登记 `IReadOnlyList<DependencyInjectionContainer>`（`DependencyInjectionHandler.cs:74,111`）
  - `builder.InitializeSptBlazor(loadedMods)`（`:93`）
  - `await builder.Services.AddModDIConstructorsAsync(modAssemblies, token)`（`:98`）——反射调用 mod 的静态 `IOnDIConstruct.OnDIConstructAsync`（`Extensions/ServiceCollectionExtensions.cs:11-23`）
  - 注册 `HttpContextAccessor`、`HttpClient`、具名 `"Github"` client（`:100-112`）
- `CreateEarlySptProvider`（`:115-141`）：早期 provider 只注册 `SPTStartupHostedService` 所在程序集的 Injectable + `ISemVer` + `DatabaseImporter`；`modsEnabled` 时额外 `AddSingleton<ModLoader>().AddSingleton<ModValidator>()`（`:135-138`）。这是为「mod 早于 Web 主机加载」做的分离（注释见 `Program.cs:202-204`）。

## `IOnLoad` 两阶段执行（关键机制）

`IOnLoad` 不在单点顺序执行，而由**两个执行器按 `Injectable.TypePriority` 分段**：

| 阶段 | 执行器 | 过滤条件 | 位置 |
|------|--------|----------|------|
| Pre-SPT-load | `ProgramExtensions.RunPreSptLoadCallbacks` | `TypePriority >= Watermark(0)` 且 `< GameCallbacks(200000)` | `Extensions/ProgramExtensions.cs:16-46` |
| Post-preload | `SPTStartupHostedService.StartAsync` | `TypePriority >= GameCallbacks(200000)` | `Services/Hosted/SPTStartupHostedService.cs:52-66` |

- 执行器从 `IReadOnlyList<DependencyInjectionContainer>` 里筛 `Type == typeof(IOnLoad)`，再按 `InjectableAttribute.TypePriority` 过滤（`ProgramExtensions.cs:22-25`、`SPTStartupHostedService.cs:52-54`）。
- 取 `onLoadContainer.ParentType` 解析实例并调 `OnLoadAsync`（`ProgramExtensions.cs:37-44`、`SPTStartupHostedService.cs:58-65`）。
- `SPTStartupHostedService` 是 `[Injectable(InjectionType.HostedService)] : BackgroundService`（`SPTStartupHostedService.cs:15,28`），即 `HostedService` 型注册会经 `TryAddEnumerable(IHostedService)` 挂到宿主（`DependencyInjectionHandler.cs:172-182`）。
- `StartAsync` 先 `bundleLoader.LoadBundlesAsync(loadedMods)`（`SPTStartupHostedService.cs:44-47`），再跑 Post-preload `IOnLoad`，然后 `base.StartAsync` 启动更新循环（`:74`）。

## `IOnUpdate` 循环（每 5 秒）

- `SPTStartupHostedService.ExecuteAsync`（`SPTStartupHostedService.cs:77-126`）：`PeriodicTimer(TimeSpan.FromSeconds(5))`（`:81`），循环遍历构造注入的 `IEnumerable<IOnUpdate> onUpdateComponents`（`:27,85`）。
- 对每个组件：用 `_onUpdateLastRun[fullName]` 与 `timeUtil.GetTimeStamp()` 求 `secondsSinceLastRun`（`:93-94`）；调 `OnUpdateAsync(secondsSinceLastRun, token)`（`:98`）；返回 `true` 才刷新时间戳（`:99-101`）；单组件异常被 `catch` 并记录，不中断其它组件（`:107-110`）；`await timer.WaitForNextTickAsync`（`:113`）。
- 组件可自行用 `secondsSinceLastRun` 做节流（例：`SaveCallbacks.OnUpdateAsync` 用 `CoreConfig.ProfileSaveIntervalInSeconds` 判间隔，`Callbacks/SaveCallbacks.cs:33-44`）。

## HTTP 请求链

```
Kestrel(HTTPS)
 → SptLoggerMiddleware
 → HttpServer.HandleRequestAsync                       Servers/HttpServer.cs:19-45
     ├─ WebSocket 请求 & webSocketServer.CanHandle → webSocketServer.OnConnectionAsync   :21-25
     └─ 找首个 IHttpListener.CanHandle(context)        :27
         → sessionId ← PHPSESSID cookie                :35-37
         → profileActivityService.SetActivityTimestamp :39-42
         → listener.HandleAsync(sessionId, context)    :44
             → SptHttpListener.HandleAsync              Servers/Http/SptHttpListener.cs:53-136
                 ├─ GET  → GetResponseObjectAsync       :57-69
                 └─ POST/PUT → DeShuffle + zlib 解压    :71-134
                     → HttpRouter.GetResponseObjectAsync Routers/HttpRouter.cs:20-35
                         ├─ StaticRouter 组（精确）      :28
                         └─ DynamicRouter 组（包含）     :29-32
                     → ISerializer（哨兵命中）或 zlib JSON Servers/Http/SptHttpListener.cs:192-201
                     → StreamedJsonBody / shuffle 帧      :147-156, :290-311
```

- **监听器抽象**：`IHttpListener { CanHandle(HttpContext); HandleAsync(MongoId, HttpContext, token) }`（`Servers/Http/IHttpListener.cs:6-9`）。内建实现：`SptHttpListener`（`Servers/Http/SptHttpListener.cs:20`）、`ImageRouter`（`Routers/ImageRouter.cs:11`）。
- **`SptHttpListener`**：`[Injectable]`（`SptHttpListener.cs:20`）；支持方法 `GET/PUT/POST`（`:32`）；`CanHandle = 方法命中 && httpRouter.CanHandle(context)`（`:48-51`）；请求体先 `RequestEncryptionUtil.DeShuffleAsync`（`:79-83`），再判 zlib（CMF/FLG 头，`:90-104`）；响应走 `SendResponseAsync`。
- **`HttpRouter`**：构造注入 `IEnumerable<StaticRouter>` + `IEnumerable<DynamicRouter>`（`Routers/HttpRouter.cs:10`）；`GetResponseObjectAsync` 先静态组、未命中再动态组（`:28-32`）；`HandleRouteAsync` 去掉 `?retry=` 后缀（`:55-58`），遍历 `route.CanHandle(url, dynamic)`（`:63`）并分派 `HandleDynamicAsync`/`HandleStaticAsync`（`:70-75`），**多个 router 可同时命中，后者覆盖 `wrapper.Output`**（`:83-87`）。
- **匹配语义**：`Router.CanHandle(url, partialMatch)`（`DI/Router.cs:51-59`）——`partialMatch=true` 取 `dynamic` 路由且 `url.Contains(route.route)`；`false` 取非 `dynamic` 路由且 `route.route == url`。
- **Serializer 哨兵**：`ISerializer.CanHandle(string route)`（`DI/ISerializer.cs:6-15`）；`BundleSerializer` 哨兵 `"BUNDLE"`（`Routers/Serializers/BundleSerializer.cs:40-43`）、`NotifySerializer` 哨兵 `"NOTIFY"`（`Routers/Serializers/NotifySerializer.cs:36-39`）。`SptHttpListener` 用 `serializers.FirstOrDefault(x => x.CanHandle(output))` 选路（`SptHttpListener.cs:192-196`）；未命中则 `SendZlibJsonAsync`（`:200`）。
- **帧格式/明文特例**：`ShouldShuffleRequest` 对 `/launcher`、`/client/metadata`、`/v2/shop`、`/files` 不 shuffle（`SptHttpListener.cs:399-405`）；`ShouldShuffleResponse` 再排除 `/singleplayer`（`:407-410`）；`SendsPlainJson` 对 `/v2/shop`、`/files` 直接写 UTF-8（`:412-415`）。
- **WebSocket**：`WebSocketServer`（`Servers/WebSocketServer.cs`）；`SptHttpListener` 之外由 `HttpServer` 优先分派（`HttpServer.cs:21-25`）。

## 目录职责与规模统计

统计口径：`Libraries/SPTushonka.Server.Core/` 下 `.cs` 文件数（HEAD `ff0bf3281`），**非类型数**；含子目录。

| 目录 | 文件数 | 职责 |
|------|-------:|------|
| `Controllers/` | 32 | 业务控制器（HTTP 端点实现层） |
| `Callbacks/` | 36 | URL→controller 绑定 + `IOnLoad`/`IOnUpdate` 回调 |
| `Routers/` | 53 | 路由与分发（见下细分） |
| `Generators/` | 33 | 数据生成（bot/战利品/跳蚤/任务/天气） |
| `Helpers/` | 70 | 业务辅助（Profile/Item/Trader/Quest/Dialogue 等） |
| `Models/` | 489 | 类型定义（`Eft/` 协议镜像、`Spt/` 内部模型、`Enums/` 等） |
| `Services/` | 60 | 业务服务（见下细分） |
| `Migration/` | 21 | profile 迁移（3 基类 + 18 迁移） |
| `Utils/` | 49 | 通用工具（Json/File/Hash/Importer/Time 等） |
| `Extensions/` | 23 | 扩展方法 |
| `Servers/` | 12 | 运行时服务（Http/Save/Ragfair/WebSocket） |
| `DI/` | 8 | 生命周期接口 + 路由基类 + `ISerializer` |
| `Loaders/` | 2 | `BundleLoader.cs`、`ConfigLoader.cs` |

`Routers/` 细分：`Static/` 24、`Dynamic/` 8、`ItemEvents/` 12、`SaveLoad/` 4、`Serializers/` 2，顶层 3（`HttpRouter.cs`、`EventOutputHolder.cs`、`ImageRouter.cs`）。

`Services/` 细分：`Commerce/` 9、`InRaid/` 9、`Bot/` 8、`Ragfair/` 7、`Modding/` 6、`Server/` 6、`Profile/` 5、`Locales/` 3、`Hideout/` 2、`Hosted/` 2、`Items/` 2、`Image/` 1。

`SPTushonka.Server/`（宿主程序集）顶层目录：`Exceptions/`、`Extensions/`、`Helpers/`、`Middleware/`、`Modding/`、`Properties/`。其中 `Modding/` 承载 `ModLoader.cs`/`ModValidator.cs`/`EnumPatcher.cs` 等，`Helpers/` 承载 `DatabaseImporter.cs`/`DatabaseTables.cs`/`ProgramHelpers.cs`。

### 32 个控制器（`Controllers/`）

`AchievementController`、`BattlePassController`、`BotController`、`BuildController`、`ClientLogController`、`CustomizationController`、`DialogueController`、`EndingController`、`GameController`、`HandBookController`、`HealthController`、`HideoutController`、`InRaidController`、`InsuranceController`、`InventoryController`、`LauncherV2Controller`、`LocationController`、`MatchController`、`ModdedTraderCustomizationController`、`NoteController`、`NotifierController`、`PresetController`、`PrestigeController`、`ProfileController`、`QuestController`、`RagfairController`、`RepairController`、`RepeatableQuestController`、`TradeController`、`TraderController`、`WeatherController`、`WishlistController`。

## 与 4.1 / 3.11 的关系

- 与 4.1：框架层无破坏性变更（`Models/Spt/Mod`、`Modding`、`DI` 目录 `git diff origin/4.1x-dev...5.0x-dev` 无输出）；本笔记是 4.1 架构笔记的 5.0 实读版本。
- 与 3.11：3.11 的 `Program.ts`/`utils/App.ts`/`ConfigServer`/`DatabaseServer` 机制在 5.0 **不存在**；5.0 用 `Program.cs` + ASP.NET Core 主机 + `SPTarkov.DI`。

## 已核实位置

- 启动链：`SPTushonka.Server/Program.cs:31-176,178-200,205-267,269-302`
- 静态常量：`Libraries/SPTushonka.Server.Core/Utils/ProgramStatics.cs:13-52`
- 宿主装配：`SPTushonka.Server/Helpers/ProgramHelpers.cs:32-62,68-113,115-141`
- Pre-load 回调：`SPTushonka.Server/Extensions/ProgramExtensions.cs:16-46`
- `IOnDIConstruct` 调用：`SPTushonka.Server/Extensions/ServiceCollectionExtensions.cs:11-53`
- DI 注册：`Libraries/SPTushonka.DI/DependencyInjectionHandler.cs:60-112,168-251`
- 更新循环：`Libraries/SPTushonka.Server.Core/Services/Hosted/SPTStartupHostedService.cs:42-126`
- 请求链：`Libraries/SPTushonka.Server.Core/Servers/HttpServer.cs:19-45`、`Servers/Http/SptHttpListener.cs:48-136,138-204,290-415`、`Routers/HttpRouter.cs:10-98`
- 路由基类：`Libraries/SPTushonka.Server.Core/DI/Router.cs:22-59,62-91,93-122,148-159`、`DI/Routing/ItemEventRouter.cs:8-42`
- 序列化：`Libraries/SPTushonka.Server.Core/DI/ISerializer.cs:6-15`、`Routers/Serializers/BundleSerializer.cs:40-43`、`Routers/Serializers/NotifySerializer.cs:36-39`
