---
version: [4.1, 5.0]
domain: server
topic: modding-standard
source: curated
---

# 04 服务端机制（SRV）

> **Domain slug:** `SRV` · **规则 ID 前缀:** `STD-SRV-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 03，2026-09-14）。

## 维度范围

- DI 注册：`[Injectable(TypePriority = OnLoadOrder.X + n)]`，禁止裸数字
- 生命周期：`IOnLoad` / `IOnUpdate`
- 路由注册：`StaticRouter` / `DynamicRouter`
- Callbacks 与 `ISptLogger<T>` 注入

## 规则

### STD-SRV-001 — 用 `[Injectable]` 标注服务端类，交由 DI 容器构造

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：[api-notes-4.1/di-container.md](../api-notes-4.1/di-container.md)（反射扫描 `[Injectable]` 类型，按 `TypePriority` 注册；默认 `InjectionType.Transient`）、[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)；语料：`[Injectable]` 1018（EV-CORPUS-MECH）
- **Rule:** 服务端所有服务、helper 与 mod 自有类（配置类除外，见 `STD-CFG-004`）都用 `[Injectable]` 标注，由 DI 容器反射发现并构造；需要跨类共享同一实例的服务显式声明 `InjectionType.Singleton`（默认是 `Transient`）。

```csharp
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;

[Injectable(InjectionType.Singleton)]
public class MyService(
    GlobalTable globalTable,
    TemplateTable templateTable,
    ISptLogger<MyService> logger)
{
    public int GetTemplateItemCount() => templateTable.Items.Count;
}
```

> 深入：[api-notes-4.1/di-container.md](../api-notes-4.1/di-container.md)

### STD-SRV-002 — 用 `OnLoadOrder.X + n` 表达 `TypePriority`，禁止裸数字

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)（永远基于 `OnLoadOrder.X` 写偏移，不要写裸数字）、[api-notes-4.1/mod-loading.md](../api-notes-4.1/mod-loading.md)（阶段常量 `OnLoadOrder`、默认 `int.MaxValue`）；语料：无（机制推断，无语料先例，登记 EV-NOCORPUS；EV-CORPUS-MECH 仅计 `[Injectable]` 总数 1018，未覆盖 `TypePriority` 写法）
- **Rule:** `TypePriority` 永远基于 `OnLoadOrder.X` 阶段常量写偏移（如 `OnLoadOrder.PostLoad + 1`），禁止写裸数字——裸数字在 SPT 调整阶段间隔后即失效。

```csharp
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

// 正确：基于阶段常量写偏移（PostLoad = 1000000）
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class MyModEntry : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// 反例：裸数字，SPT 调整阶段间隔后失效，禁止
// [Injectable(TypePriority = 1000001)]
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)

### STD-SRV-003 — 生命周期方法用 async 签名并传播 `CancellationToken`

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)（`OnLoadAsync(CancellationToken)` 替代 `OnLoad()`；token 传播给 IO/HTTP/Delay）、[api-notes-4.1/mod-loading.md](../api-notes-4.1/mod-loading.md)（生命周期由 DI 启动链调用）；语料：`IOnLoad` 279（EV-CORPUS-MECH）
- **Rule:** 实现 `IOnLoad` 时用 `Task OnLoadAsync(CancellationToken cancellationToken)`，把 token 传播给一切接受它的调用（文件 IO、HTTP、`Task.Delay`），长同步工作周期调用 `ThrowIfCancellationRequested()`。若实现为纯同步逻辑（无任何 IO / 异步调用），可不实际使用 token，但必须保留接口签名中的 `CancellationToken` 参数。

```csharp
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class MyModEntry(ISptLogger<MyModEntry> logger) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // 把 token 传播给一切接受它的调用（文件 IO、HTTP、Task.Delay）
        await LoadMyDataAsync(cancellationToken);

        // 长时间同步工作周期检查取消
        cancellationToken.ThrowIfCancellationRequested();

        logger.Info("MyMod 加载完成");
    }
}
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)
> 交叉引用：`STD-LOG-005`（取消异常处理）。

### STD-SRV-004 — 周期性任务实现 `IOnUpdate`，返回 `true` 表示本周期已处理

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：[api-notes-5.0/di-container.md](../api-notes-5.0/di-container.md)（`PeriodicTimer(5s)` 轮询，返回 `true` 才刷新时间戳）、[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)（`OnUpdateAsync(long secondsSinceLastRun, CancellationToken)` 返回 `Task<bool>`）；语料：`IOnUpdate` 6（EV-CORPUS-MECH，样本 <10）
- **Rule:** 周期性任务实现 `IOnUpdate.OnUpdateAsync(long secondsSinceLastRun, CancellationToken cancellationToken)`；返回 `true` 表示本周期已处理完毕（刷新时间戳），返回 `false` 则保留累计时间。

```csharp
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class MyPeriodicTask : IOnUpdate
{
    public Task<bool> OnUpdateAsync(long secondsSinceLastRun, CancellationToken cancellationToken)
    {
        // true = 本周期已处理（刷新时间戳）；false = 保留累计时间
        return Task.FromResult(true);
    }
}
```

> 深入：[api-notes-5.0/di-container.md](../api-notes-5.0/di-container.md)

### STD-SRV-005 — 用 `StaticRouter` / `DynamicRouter` 注册游戏路由

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：[api-notes-4.1/http-routing.md](../api-notes-4.1/http-routing.md) 与 [api-notes-5.0/http-routing.md](../api-notes-5.0/http-routing.md)（`StaticRouter` 精确匹配 / `DynamicRouter` 包含匹配；标 `[Injectable(TypePriority = OnLoadOrder.Routers + n)]` 由 DI 收集）、[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)；语料：`StaticRouter` 90、`DynamicRouter` 12（EV-CORPUS-MECH）
- **Rule:** 游戏客户端流量一律走 Router（网页/工具流量才用 MVC Controller）；精确路径用 `StaticRouter`，前缀/包含匹配用 `DynamicRouter`，并标 `[Injectable(TypePriority = OnLoadOrder.Routers + n)]`。偏移写法（含新路由 `+1`、覆盖 SPT 现有路由 `-1`）遵循 `STD-SRV-002` 的约定，不作为本规则的独立 MUST 要求。

```csharp
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Utils;

[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
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

[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
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

> 深入：[api-notes-4.1/http-routing.md](../api-notes-4.1/http-routing.md) · [api-notes-5.0/http-routing.md](../api-notes-5.0/http-routing.md)
> 交叉引用：`STD-SRV-002`（`TypePriority` 偏移写法）。

### STD-SRV-006 — Router action 签名必须包含 `CancellationToken`

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：[api-notes-4.1/http-routing.md](../api-notes-4.1/http-routing.md)（action 签名必须含 `CancellationToken`，源自 `HttpContext.RequestAborted`）、[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)；语料：无（机制推断，无语料先例，登记 EV-NOCORPUS；EV-CORPUS-MECH 计 `StaticRouter` 90，未覆盖 action 签名）
- **Rule:** 所有路由 action 显式声明 `CancellationToken` 参数（即使不用也要声明），并在后续调用中继续传递，使请求中止能传播。若本 action 确实不消费该 token，参数名可写为 `_`（丢弃），但签名仍必须包含 `CancellationToken` 参数。

```csharp
new RouteAction<EmptyRequestData>(
    "/spt/mymod/ping",
    async (url, info, sessionID, output, cancellationToken)
        => await callbacks.Ping(url, info, sessionID, cancellationToken)
);
```

> 深入：[api-notes-4.1/http-routing.md](../api-notes-4.1/http-routing.md)

### STD-SRV-007 — Router 只声明路由，业务逻辑放入可注入的 Callbacks 类

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：[api-notes-5.0/http-routing.md](../api-notes-5.0/http-routing.md)（官方路由示例均构造注入 `MyCallbacks` 并转发）、[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)；语料：无（机制推断，无语料先例，登记 EV-NOCORPUS）
- **Rule:** Router 子类只声明路由表并把 action 转发给注入的 Callbacks（或 Service）；业务逻辑写在 Callbacks 类里，便于被多个 Router / ItemEvent 复用。

```csharp
[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
public class MyRouter(JsonUtil jsonUtil, MyCallbacks callbacks)
    : StaticRouter(jsonUtil, [
        new RouteAction<EmptyRequestData>(
            "/spt/mymod/ping",
            async (url, info, sessionID, output, cancellationToken)
                => await callbacks.Ping(url, info, sessionID)
        ),
    ])
{ }

// 业务逻辑集中在 Callbacks，可被路由与 ItemEvent 共用
[Injectable]
public class MyCallbacks(ISptLogger<MyCallbacks> logger)
{
    public Task<string> Ping(string url, EmptyRequestData info, string sessionId)
    {
        logger.Info("ping");
        return Task.FromResult("pong");
    }
}
```

> 深入：[api-notes-5.0/http-routing.md](../api-notes-5.0/http-routing.md)

### STD-SRV-008 — 用注入的 `ISptLogger<T>` 记录服务端日志

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：[api-notes-4.1/di-container.md](../api-notes-4.1/di-container.md)（`ISptLogger<T>` 属可注入面）、[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)（`ISptLogger` 移至 `SPTarkov.Common.Models.Logging`，级别用 `Microsoft.Extensions.Logging.LogLevel`）；语料：`ISptLogger` 614（EV-CORPUS-MECH）
- **Rule:** 服务端日志通过构造函数注入的 `ISptLogger<T>`（`using SPTarkov.Common.Models.Logging;`）输出，级别用 `Microsoft.Extensions.Logging.LogLevel`。

```csharp
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;

[Injectable]
public class MyService(ISptLogger<MyService> logger)
{
    public void DoWork() => logger.Info("MyService 开始工作");
}
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)
> 交叉引用：`STD-LOG-001`、`STD-LOG-004`（日志与错误处理约定）。

### STD-SRV-009 — 服务端 Harmony 补丁用 `IRuntimePatch` 经 DI 收集并在 `IOnLoad` 中启用

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)（Patch 由 DI 管理：标 `[Injectable]`、注入 `IEnumerable<IRuntimePatch>` 统一启用，`AbstractPatch` 实现 `IRuntimePatch`，在 `Preload` 阶段启用）、[api-notes-4.1/di-container.md](../api-notes-4.1/di-container.md)（`IEnumerable<IRuntimePatch>` 多实例注入）、[wiki-tushonka/modding/SPT_41_Modding/Server_40_to_41.md](../../wiki-tushonka/modding/SPT_41_Modding/Server_40_to_41.md)（`[PatchPostfix]` + `Enable()`、`ServiceLocator` 已移除）；语料：约 28 个仓库含服务端补丁管理器（如 `ABPS-Acid's-Bot-Placement-System_2097-2_source/Server/PatchManager.cs`、Strange-Curios、EternalCycle、Ironman、WTT-CommonLib）（EV-CORPUS-MECH；EV 表暂无 server patch-manager 专列，计数为本次语料内联）
- **Rule:** 服务端 Harmony 补丁用 `IRuntimePatch`（经 `AbstractPatch`）表达，标 `[Injectable]` 由 DI 容器构造；在入口 `IOnLoad` 中注入 `IEnumerable<IRuntimePatch>` 并逐个 `Enable()`，在 `OnLoadOrder.Preload + n` 阶段启用（早于被打补丁的代码运行）。不要在服务端用 `Harmony.PatchAll()` 扫程序集。

```csharp
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

// 补丁类：继承 AbstractPatch，标 [Injectable] 由容器构造
[Injectable]
public class MyServerPatch : AbstractPatch
{
    private static ISptLogger<MyServerPatch> _logger = default!;

    public MyServerPatch(ISptLogger<MyServerPatch> logger)
    {
        // Harmony 补丁方法必须是 static，依赖经 static 字段传递
        _logger = logger;
    }

    protected override MethodBase GetTargetMethod()
        => AccessTools.DeclaredMethod(typeof(SomeTarget), nameof(SomeTarget.SomeMethod));

    [PatchPostfix]
    public static void PatchPostfix()
    {
        _logger.Info("MyServerPatch 已生效");
    }
}

// 入口：注入所有补丁，在 Preload 阶段统一 Enable
[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class MyModPatchLoader(IEnumerable<IRuntimePatch> patches) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        foreach (var patch in patches)
        {
            patch.Enable();
        }
        return Task.CompletedTask;
    }
}
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md) · [api-notes-4.1/di-container.md](../api-notes-4.1/di-container.md)
> 交叉引用：`STD-SRV-001`（`[Injectable]` DI 构造）、`STD-SRV-002`（`OnLoadOrder.X + n` 偏移写法）。
