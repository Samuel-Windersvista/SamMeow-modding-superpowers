---
version: [4.1]
domain: server
topic: architecture
source: curated
---
# SPT 4.1.5 服务端架构图（Codemap 提炼）

> 状态：已核实（源码实读 2026-09-06，4.1.5 fork `SamMeow_SP-Tushonka_source_code` commit `7d7add55`）
> 适用：[4.1] | 完整审查见 `curated/operations/415-source-review-report.md`

> [!IMPORTANT] 目录 vs 命名空间：4.1.3 起 fork 把**项目文件夹**从 `SPTarkov.*` 重命名为 `SPTushonka.*`（`Libraries/SPTushonka.Server.Core/` 等），但**代码命名空间仍是 `SPTarkov.*`**（`RootNamespace=SPTarkov.Server`）。写 mod 引用命名空间时仍用 `SPTarkov.*`；找源码文件时用 `SPTushonka.*` 目录。

## 系统本质

.NET 10 单进程**整体式**架构（非微服务），核心是**自研反射 DI 容器** + **内存数据库**。`[Injectable(TypePriority)]` 注解驱动扫描注册，`TypePriority` 同时决定依赖装配顺序与 `IOnLoad` 启动回调顺序。

## 启动链

```
Program.Main
└─ StartServer
   ├─ ConfigLoader.Initialize         # SPT_Data/configs/*.{json,jsonc} 强类型反序列化（8 个 JsonConverter）
   ├─ ProgramHelpers.CreateEarlySptProvider  # 一次性 WebApplication，只拿 ModLoader/ModValidator
   ├─ ModLoader.RunModLoader          # enum prepatch 内存补丁 + PrepatchLoadContext 隔离宿主 + 读 user/mods/*.dll
   └─ StartServerAfterModLoading
      ├─ DatabaseImporter.LoadDatabaseAsync  # XxHash3 校验 checks.dat + 递归灌库
      ├─ CreateNewHostBuilder                # DatabaseTables 10 张表单例注册
      ├─ RegisterSptServicesAsync            # 扫描 Program/PatchManager/SPTWeb + mod 程序集注入 DI
      ├─ ConfigureKestrel                    # TLS1.2/1.3 自签名证书
      └─ app.RunAsync
```

## 请求链

```
Kestrel → UseWebSockets → SptLoggerMiddleware → /health → HttpServer.HandleRequestAsync
  → SptHttpListener (ZLib 解压) → HttpRouter → Router(Static/Dynamic) → Callbacks → Controller
  → Service/Helper → 内存表/SaveServer → HttpResponseUtil 包装 → ISerializer 特判 → ZLib 压缩回写
```

四层解耦：URL → RouteAction → Callback（适配层）→ Controller → Service/Helper。

## 数据流

- **内存数据库**：`ImporterUtil.LoadRecursiveAsync` 递归灌 JSON → `DatabaseTables`（Bots/Hideout/Locales/Locations/Match/Templates/Traders/Globals/Server/Settings 十张单例表）。访问方注入对应 `*Table` 单例。
- **Profile**：`SaveServer` 用 `ConcurrentDictionary<MongoId, SptProfile>` 内存主副本 + `SaveLoadRouter` 钩子 + `Migration` 迁移链 + MD5 写盘去重。

## 关键接口

| 接口 | 作用 |
|---|---|
| `[Injectable(InjectionType, TypePriority)]` | 自研 DI 注解，TypePriority 决定注册排序 = IOnLoad 加载顺序 |
| `IOnLoad.OnLoadAsync` | 启动加载钩子，`SPTStartupHostedService` 按 `OnLoadOrder` 依次执行 |
| `IOnUpdate.OnUpdateAsync` | 周期更新钩子，5s 一循环，异常记为 scheduled_events_failed |
| `Router` / `StaticRouter` / `DynamicRouter` | 路由抽象；Dynamic 前缀匹配，Static 精确匹配 |
| `RouteAction` / `RouteAction<T>` / `StreamedRouteAction<T>` | 路由动作；泛型版强类型 request；Streamed 版大响应直写流 |
| `SaveLoadRouter` | Profile 加载/保存钩子 |
| `ISerializer` | 响应特判（IMAGE/BUNDLE/NOTIFY） |
| `IHttpListener` | HTTP 请求处理器抽象 |
| `IOnDIConstruct` | mod 专属 DI 构造钩子 |

## 模块职责地图

| 目录 | 职责 |
|---|---|
| `Libraries/SPTushonka.Server.Core/Controllers`（30） | 业务逻辑层，每 Controller 一领域 |
| `.../Callbacks`（34） | 请求适配层，Router→Callbacks→Controller 桥接 |
| `.../Services`（56） | 核心业务服务（Bot/Commerce/Hideout/InRaid/Profile/Ragfair…） |
| `.../Helpers`（68） | 无状态逻辑助手 |
| `.../Generators`（33） | 数据生成器（Bot/Loot/Ragfair/RepeatableQuests/Weather） |
| `.../Models`（438） | DTO 数据模型（Spt/Config、Spt/Tables、Eft/…） |
| `.../Routers`（50） | 路由注册（static/dynamic/itemEvent/saveLoad/serializers） |
| `.../Servers`（11） | 服务宿主（HttpServer/SaveServer/RagfairServer/WebSocketServer） |
| `.../DI`（8） | IOnLoad/IOnUpdate/ISerializer/Router/OnLoadOrder 等 |
| `.../Migration`（21） | Profile 迁移链 |
| `.../Utils`（44） | JsonUtil/HashUtil/ImporterUtil/RandomUtil 等 |
| `Libraries/SPTushonka.Server.Web` | Blazor UI（Mod 配置页/数据库浏览器） |
| `Libraries/SPTushonka.DI` | 自研 DI 容器（DependencyInjectionHandler） |
| `Libraries/SPTushonka.Reflection` | 反射补丁基座（PatchManager/Prepatcher） |
| `Libraries/SPTushonka.Common` | 日志/通用模型（ISptLogger/Semver） |
| `SPTushonka.Server/Modding` | ModLoader/ModValidator/EnumPatcher/Prepatch* |

## 高风险区（审查定位）

1. **ModLoader/ModValidator**：程序集反射热加载 + 内存补丁 + 隔离上下文
2. **DependencyInjectionHandler**：反射接口/基类/泛型注册
3. **SaveServer**：Profile 并发/持久化核心（saveMd5 去重 + saveLocks 加锁）
4. **RagfairServer + RagfairOfferGenerator**：动态市场生成/过期/刷新
5. **ImporterUtil**：递归目录并行灌库 + 反射 GetSetMethod
6. **ConfigLoader**：DEBUG 下 UnmappedMemberHandling.Disallow 严格反序列化

## 源码坐标（4.1.5 fork）

- 启动：`SPTushonka.Server/Program.cs`
- DI：`Libraries/SPTushonka.DI/DependencyInjectionHandler.cs`、`Annotations/Injectable.cs`
- 路由：`Libraries/SPTushonka.Server.Core/DI/Router.cs`、`Routers/HttpRouter.cs`
- 存档：`Libraries/SPTushonka.Server.Core/Servers/SaveServer.cs`
- 灌库：`Libraries/SPTushonka.Server.Core/Utils/ImporterUtil.cs`
- 配置：`Libraries/SPTushonka.Server.Core/Loaders/ConfigLoader.cs`
- Mod 加载：`SPTushonka.Server/Modding/ModLoader.cs`
- HTTP：`Libraries/SPTushonka.Server.Core/Servers/Http/SptHttpListener.cs`
