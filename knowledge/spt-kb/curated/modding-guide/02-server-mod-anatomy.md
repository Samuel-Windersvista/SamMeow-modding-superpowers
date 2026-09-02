---
version: [4.1]
domain: both
topic: server-anatomy
source: curated
---
# Server Mod 解剖 [4.1]

> 适用版本：[4.1] | 主要来源：`wiki/SPT_41/Server_40_to_41.md`（迁移文档）、`wiki/SPT_41/modding/EnumExtensions.md`、`wiki/SPT_41/modding/server/Mod_Web_Pages.md`、`E-Mod开发示例/server-mod-examples/`

一个 SPT 4.1 服务端 mod = 一个 .NET 类库 DLL，放在 `user/mods/<ModGuid>/`，被服务器的 DI 容器扫描加载。本文按「一个 mod 的完整构成」组织。

## 1. 元数据（Metadata）

4.1 中 `IModMetadata` 是接口（4.0 是抽象 record `AbstractModMetadata`），去掉所有 `override`：

```csharp
public record MyModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.example.my-mod";   // 反向域名，全局唯一
    public string Name { get; init; } = "My Mod";
    public string Author { get; init; } = "Me";
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0"); // 兼容范围
    // 可选：Contributors, Incompatibilities, ModDependencies, Url, License
    // 4.1 移除：IsBundleMod（改由 bundles.json 存在性判断）
    // 4.1 新增：HasPrepatcher（仅枚举扩展需要）、IModBlazorMetadata 成员（Web 页面用）
}
```

## 2. 生命周期与加载顺序（OnLoadOrder）

加载顺序从低到高，各阶段间隔 100000，可用 `+n` 插队：

```
Watermark → Preload → GameCallbacks → TraderRegistration → Routers
→ HandbookCallbacks → SaveCallbacks → TraderCallbacks → PresetCallbacks
→ RagfairCallbacks → PostLoad
```

- 数据库与配置文件在一切开始前已加载完毕——不再需要 `PostDBModLoader` 阶段，原用它的 mod 改挂 `PostLoad`
- 往数据库**添加自己的数据**（物品/商人），在 `Preload` 做（此时数据库已完全加载，避免 profile 因物品不存在而加载失败）

```csharp
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class MyMod(ISptLogger<MyMod> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken) { ... }
}
```

生命周期方法全部 async 且带 `CancellationToken`（服务端关机时取消）：
- `OnLoadAsync(CancellationToken)` — 替代 `OnLoad()`
- `OnUpdateAsync(long secondsSinceLastRun, CancellationToken)` — 替代 `OnUpdate(long)`，返回 `Task<bool>`

规则：把 token 传播给一切接受它的调用（文件 IO、HTTP、Delay）；长时间同步工作周期调用 `cancellationToken.ThrowIfCancellationRequested()`；让 `OperationCanceledException` 正常传播——取消不是错误。

## 3. DI 容器（[Injectable]）

- 所有服务、helper、mod 自己的类都靠 `[Injectable]` 让容器发现并构造
- `TypePriority` 控制解析顺序，永远基于 `OnLoadOrder.X` 写偏移，不要写裸数字
- 自己的类照旧用 `[Injectable]`；只有容器无法自己构造的对象（最典型：config 文件实例）才用 `IOnDIConstruct`

```csharp
public class MyModConfigRegistration : IOnDIConstruct
{
    public static async Task OnDIConstructAsync(IServiceCollection serviceCollection)
    {
        MyModConfig config = await LoadConfigFromDiskAsync();
        serviceCollection.AddSingleton(config);
    }
}
```

注册后任何类都能注入 `MyModConfig`。注意：config 类上**不要**加 `[Injectable]`（否则容器用默认值新建实例，你的 JSON 永远不会被读）。

## 4. 表与配置：全部可注入（4.1 最大变化）

`DatabaseServer`、`DatabaseService`、`ConfigServer` **全部移除**。每个数据库表和每个配置都是 DI 单例，构造函数直接要：

```csharp
[Injectable]
public class MyService(GlobalTable globalTable, TemplateTable templateTable, InsuranceConfig insuranceConfig)
{
    public void DoThing()
    {
        var items = templateTable.Items;
    }
}
```

数据库表（位于 `SPTarkov.Server.Core.Models.Spt.Tables`）：

| 4.0 取法 | 4.1 注入 |
|---------|---------|
| `GetBots()` | `BotTable` |
| `GetGlobals()` | `GlobalTable` |
| `GetHideout()` | `HideoutTable` |
| `GetLocales()` | `LocaleTable` |
| `GetLocations()` | `LocationTable` |
| `GetMatch()` | `MatchTable` |
| `GetTemplates()` | `TemplateTable` |
| `GetTraders()` | `TradersTable` |
| `GetServer()` | `ServerTable` |
| `GetSettings()` | `SettingsTable` |

TemplateTable 属性捷径（替代原 `GetXxx()`）：`Items`、`Quests`、`Handbook`、`Prices`、`Customization`、`Achievements`、`CustomAchievements`、`Profiles`、`LocationServices`

按 ID 查：`locationTable.GetLocation(id)`、`tradersTable.GetTrader(id)`。`GetTables()` 无替代，需要多个就全部列参数。

配置按具体类型注入：`configServer.GetConfig<InsuranceConfig>()` → 直接参数 `InsuranceConfig`。类型没映射到配置文件时，服务器**启动即失败**（而不是运行时才炸）。

## 5. 路由（Router）

- 路由优先级在 4.1 真正生效。所有内置 router 坐在 `OnLoadOrder.Routers` 上，你的位置相对它偏移
- 自定义新路由：`Routers + 1` 起（随便你偏移多少，但基于 Routers 写）
- 覆盖 SPT 现有路由：`Routers - 1` = 你优先处理（替换响应）；`Routers + 1` = SPT 先处理（在结果上动手）
- 两个 mod 覆盖同一路由时，谁先注册谁赢

```csharp
[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
public class MyRouter : AbstractRouter { ... }
```

所有路由 action 增加 `CancellationToken` 参数（源自 `HttpContext.RequestAborted`），必须带上：

```diff
- (url, info, sessionId, output) => { ... }
+ (url, info, sessionId, output, cancellationToken) => { ... }
```

`RouteAction<TRequest>` 现在约束 `TRequest : IRequestData`，body 到达即已强类型。

## 6. Item Event Router（4.1 重写）

`ItemEventRouterDefinition` → `ItemEventRouter`（`SPTarkov.Server.Core.DI.Routing`）。不再 override 两个方法 + switch URL，改为把路由记录传给基类构造函数：

```csharp
[Injectable(TypePriority = OnLoadOrder.Routers)]
public sealed class MyItemEventRouter(MyCallbacks callbacks)
    : ItemEventRouter([
        new ItemRouteAction<MyActionRequest>(
            "MyAction",
            async (url, pmcData, body, sessionID, output, cancellationToken) =>
                await callbacks.DoThing(pmcData, body, sessionID)
        ),
    ]) { }
```

- `BaseInteractionRequestDataConverter` 与 `RegisterModDataHandler` 已删除——类型声明在路由上，转换自动
- body 类型不匹配时抛出命名路由和两个类型的异常，而不是 null 悄悄蔓延

## 7. Patch（Harmony）

- Patch 由 DI 管理：标 `[Injectable]`，注入 `IEnumerable<IRuntimePatch>` 统一启用
- `ServiceLocator` 已移除——patch 的依赖走构造函数，Harmony 方法必须是 static，把依赖存到 static 字段
- 在 `Preload` 阶段启用（`OnLoadOrder.Preload + 1`），确保在被打补丁的代码运行前生效
- `Enable()`/`Disable()` 只有创建者程序集能调（`IsYourPatch` 判断归属）
- patch 失败抛 `PatchException`（含目标方法名），而非裸 Exception
- `AbstractPatch` 实现新的 `IRuntimePatch` 接口

```csharp
[Injectable]
public class MyPatch(ISptLogger<MyPatch> logger, ItemHelper itemHelper) : AbstractPatch
{
    private static ISptLogger<MyPatch> _logger = default!;
    private static ItemHelper _itemHelper = default!;
    // 构造函数里 _logger = logger; _itemHelper = itemHelper;

    protected override MethodBase GetTargetMethod() { ... }

    [PatchPostfix]
    public static void PatchPostfix() { /* 用 static 字段 */ }
}
```

## 8. 日志

- `ISptLogger` 移到 `SPTarkov.Common`：`using SPTarkov.Common.Models.Logging;`
- 级别改用 `Microsoft.Extensions.Logging.LogLevel`：`Fatal→Critical`、`Warn→Warning`、`Info→Information`；Trace/Debug/Error 不变
- 数字比较注意方向反转（SPT 从 Fatal 起数，Microsoft 从 Trace 起数）
- 颜色用 Spectre.Console：`logger.LogWithColor("Loaded", Color.Green, Color.Black)`

## 9. Bundle

- `IsBundleMod` 没了：服务器检测 mod 文件夹里的 `bundles.json` 决定是否加载 bundle

## 10. Web 页面（Mod Web Pages，可选）

实现 `IModBlazorMetadata`（`SPTarkov.Server.Web`，原名 `IModWebMetadata`）+ `IModMetadata`：

```csharp
public sealed class MyModMetadata : IModMetadata, IModBlazorMetadata
{
    public string? WWWRootUrl { get; init; }          // null = 用程序集名
    public string? HomePage { get; init; } = "/my-mod";           // SIC 卡片入口
    public string? HomePageDescription { get; init; } = "Settings";
}
```

- 项目需 `Microsoft.NET.Sdk.Web` + `<OutputType>Library</OutputType>`，引用 `SPTarkov.Server.Web`
- Blazor 页面（`@page` 路由要匹配 `HomePage`）、`wwwroot` 静态文件（两 mod 撞 URL = 启动硬失败）、MVC Controller（只供你自己的页面/工具调用；游戏流量仍走 Router）
- 配置编辑器：config 类（全部 get/set，用 `[JsonPropertyName]` 稳定文件名）+ `IConfigEditorConfigProvider` 返回 `ConfigEditorConfigRegistration.Create(id, 显示名, config实例, 相对路径)`；apply 与 save 相互独立

详见 `wiki/SPT_41/modding/server/Mod_Web_Pages.md`。

## 11. 枚举扩展（Prepatcher，可选）

声明式，不再写 DLL / Mono.Cecil：
- 服务端：JSON 数组放 `user/patchers/<ModGuid>/任意名.json`，元数据 `HasPrepatcher = true`；服务器重启时写 `SPTarkov.Server.Core.Patched.dll`
- 客户端：服务端 mod 注入 `ClientEnumDefinitions` 注册 `EnumEntryDefinition`，客户端内建 prepatcher 启动时经 `/singleplayer/customEnumEntries` 拉取并写入 `Assembly-CSharp`
- 数值必须适配枚举底层类型、名字与数值全局唯一、服务端/客户端定义各自独立注册且值要同步

详见 `wiki/SPT_41/modding/EnumExtensions.md`。

## 12. 命名空间迁移速查（常用）

| 4.0 | 4.1 |
|-----|-----|
| `Helpers.ItemHelper` | `Helpers.Items.ItemHelper` |
| `Helpers.ProfileHelper` | `Helpers.Profile.ProfileHelper` |
| `Helpers.InventoryHelper` | `Helpers.Profile.InventoryHelper` |
| `Helpers.QuestHelper` | `Helpers.Quest.QuestHelper` |
| `Helpers.TraderHelper` | `Helpers.Traders.TraderHelper` |
| `Helpers.BotHelper` | `Helpers.Bot.BotHelper` |
| `Helpers.ModHelper` | `Helpers.Server.ModHelper` |
| `Services.Mod.CustomItemService` | `Services.Modding.Custom.CustomItemService` |
| `Services.Mod.CustomQuestService` | `Services.Modding.Custom.CustomQuestService` |
| `Services.ServerLocalisationService` | `Services.Locales.ServerLocalisationService` |

完整迁移表见 `wiki/SPT_41/Server_40_to_41.md`（Generators/Helpers/Services/Models 四组全表）。

## 13. 其他小变化

- `ModHelper` 可读你自己 mod 文件夹内的文件（`modHelper.GetAbsolutePathToModFolder(Assembly)` + `GetJsonDataFromFile<T>`）
- 自定义物品可跳过手册与跳蚤价格条目
- `MongoId` 重构，存量用法不受影响
- Prepatching 独立页面在 wiki 中为死链（404），内容由 EnumExtensions 页承接

## 关键文件坐标（4.1 源码）

- DI 注解：`SPTarkov.DI.Annotations`（[Injectable]）
- 加载阶段枚举：`SPTarkov.Server.Core.DI`（OnLoadOrder）
- 表模型：`SPTarkov.Server.Core.Models.Spt.Tables`
- Item Event 路由：`SPTarkov.Server.Core.DI.Routing`
- 日志模型：`SPTarkov.Common.Models.Logging`
- Web 元数据接口：`SPTarkov.Server.Web`
