---
version: [5.0]
domain: server
topic: di
source: curated
---
# DI 容器笔记 [5.0]

> **[UNSTABLE-PREVIEW]** SPT 5.0 处于开发初期（`5.0x-dev` 分支，尚无正式版）。本笔记为 2026-09-13 源码快照（HEAD `ff0bf3281`）提炼，上游 API 可能变更；使用前请以源码复核，勿据此做长期承诺。

> 状态：**源码实读（2026-09-13）** | 版本：[5.0]
> 源码：`Libraries/SPTushonka.DI/Annotations/Injectable.cs`、`Libraries/SPTushonka.DI/DependencyInjectionHandler.cs`、`Libraries/SPTushonka.DI/Extensions/DependencyInjectionExtensions.cs`、`SPTushonka.Server/Helpers/ProgramHelpers.cs`、`SPTushonka.Server/Extensions/ServiceCollectionExtensions.cs`、`Libraries/SPTushonka.Server.Core/DI/`

## 注入注解 `[Injectable]`

```csharp
// Libraries/SPTushonka.DI/Annotations/Injectable.cs:5-20
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class Injectable(InjectionType injectionType = InjectionType.Transient, int typePriority = int.MaxValue) : Attribute
{
    public InjectionType InjectionType { get; init; } = injectionType;
    public int TypePriority { get; init; } = typePriority;
}

public enum InjectionType { HostedService, Singleton, Transient, Scoped }
```

- 仅可标注 **class**，且 `Inherited = false`（`Injectable.cs:5`）。
- **默认值**：`InjectionType = Transient`、`TypePriority = int.MaxValue`（`Injectable.cs:7`）。即不写参数时是瞬态服务、最晚注册。
- 语义：`TypePriority` 越低越先注册/执行；`int.MaxValue` 表示最后（`DependencyInjectionHandler.cs:74`）。

## 注册流程：`DependencyInjectionHandler`

`Libraries/SPTushonka.DI/DependencyInjectionHandler.cs`，构造注入 `IServiceCollection`（`:9`）。内部状态：`_injectableAssemblies`（`:11`）、`_injectedTypeNames`（`:14`）、`_injectedValues`（`:16`）、`_oneTimeUseFlag`（`:19`）。

### 1. 收集可注入类型

- `AddInjectableTypesFromAssembly(Assembly)`（`:21-24`）→ `AddInjectableTypesFromTypeList`（`:39-58`）。
- 过滤条件：`Attribute.IsDefined(type, typeof(Injectable))` **且**尚未以 `{Namespace}.{Name}` 记录（`:48-50`）；命中则以该 key 记入 `_injectedTypeNames`（`:53-56`）。
- 入口封装：`AddInjectableTypesFromAssemblies`（`:26-32`）、`AddInjectableTypesFromTypeAssembly`（`:34-37`）。

### 2. `InjectAll()` 注册（一次性）

`InjectAll`（`:60-112`）：
1. 二次调用抛 `Exception("... one time use service!")`（`:62-66`）。
2. 每个类型构造 `DependencyInjectionContainer(attribute, type, type)`（`:67-71`）。
3. `OrderBy(tRef => tRef.InjectableAttribute.TypePriority)` 排序（`:74`）。
4. 对每个类型，把自身 + 其实现的**非 System 命名空间接口** + 基类入队（`:80-96`）；泛型走 `RegisterGenericComponents`（`:98-101,114-147`），否则 `RegisterComponent`（`:104`）。
5. 最后 `serviceCollection.AddSingleton<IReadOnlyList<DependencyInjectionContainer>>(dependencyInjectionContainers)`（`:111`）——这是启动链筛选 `IOnLoad` 的凭据。

### 3. 按 `InjectionType` 落地（`RegisterComponent`，`:168-222`）

| `InjectionType` | 注册方式 | 位置 |
|-----------------|----------|------|
| `HostedService` | 要求实现 `IHostedService`，`TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), impl))` | `:172-182` |
| `Singleton` | `HandleSingletonRegistration`（接口别名时用工厂 + 锁缓存） | `:183-193,224-251` |
| `Transient` | `AddTransient(registrableInterface, implementationType)` | `:194-204` |
| `Scoped` | `AddScoped(registrableInterface, implementationType)` | `:205-215` |

- 对 `HostedService`/`Transient`/`Scoped` 误把 `IHostedService` 当作普通接口注册会抛 `ArgumentException`（`:184-190,195-201,207-212`）。
- `HandleSingletonRegistration`（`:224-251`）：接口别名时注册工厂，用 `_injectedValues`（加锁）缓存同一实例，保证多个接口解析到同一单例（`:229-246`）；`registrableInterface == implementationType` 时直接 `AddSingleton`（`:249`）。
- `DependencyInjectionContainer` 记录：`InjectableAttribute` / `Type` / `ParentType`（`:254-266`）。

## 解析流程与公开 API

- **未找到名为 `Resolve<T>` 的公开方法**（全 `Libraries/` 检索 `Resolve<` 无命中）。解析走标准 `IServiceProvider`：
  - `app.Services.GetRequiredService<HttpConfig>()`（`SPTushonka.Server/Program.cs:260`）；
  - `serviceProvider.GetRequiredService(onLoadContainer.ParentType)`（`SPTushonka.Server.Core/Services/Hosted/SPTStartupHostedService.cs:58`）；
  - 内部单例别名解析 `serviceProvider.GetService(implementationType)`（`DependencyInjectionHandler.cs:238`）。
- 组件获取自己依赖的**常规方式**是构造函数注入（DI 自动解析参数）；需要手动解析时可注入 `IServiceProvider`（内建 `SPTStartupHostedService` 即如此，`:25`）。
- `IReadOnlyList<DependencyInjectionContainer>` 注册为单例（`DependencyInjectionHandler.cs:111`），启动链用它按 `Type`/`TypePriority` 筛组件（`ProgramExtensions.cs:20-25`、`SPTStartupHostedService.cs:18,52-54`）。
- `GetTypePriority<T>()` 扩展：读类型上的 `[Injectable]` 并返回 `TypePriority`，无特性时 `int.MaxValue`（`DependencyInjectionExtensions.cs:7-17`）。

## mod 自定义服务注册入口

两条路：

1. **声明式（推荐）**：给类标 `[Injectable(...)]`，mod 程序集会被扫描并注册。
   - 扫描入口：`ProgramHelpers.RegisterSptServicesAsync` 在 `modsEnabled` 时 `diHandler.AddInjectableTypesFromAssemblies(loadedMods.SelectMany(a => a.Assemblies))`（`SPTushonka.Server/Helpers/ProgramHelpers.cs:84-87`），随后 `diHandler.InjectAll()`（`:91`）。
2. **手动（特殊场景）**：实现 `IOnDIConstruct` 的**静态** `OnDIConstructAsync(IServiceCollection, CancellationToken)`。

```csharp
// Libraries/SPTushonka.Server.Core/DI/IOnDIConstruct.cs:24-36
public interface IOnDIConstruct
{
    static abstract Task OnDIConstructAsync(IServiceCollection serviceCollection, CancellationToken cancellationToken);
}
```

- 调用链：`ProgramHelpers.cs:98` → `builder.Services.AddModDIConstructorsAsync(modAssemblies, token)`（`SPTushonka.Server/Extensions/ServiceCollectionExtensions.cs:11-23`）；反射找 public static 方法并调用，方法必须返回 `Task`（`:26-53`）。
- 接口文档要点：该钩子在**服务提供程序构建前**触发，实现中**不应从容器解析服务**，只应 `Add*` 注册；常见用途是注册 mod 专属配置或无法经 `[Injectable]` 注册的服务（`IOnDIConstruct.cs:9-23`）。
- 示例：`TestModPreload : IOnDIConstruct, IOnLoad` 在 `OnDIConstructAsync` 里 `serviceCollection.AddSingleton(new TestDIClass())`（`Testing/TestMod/TestMod.cs:30-47`）。

## 生命周期接口

```csharp
// DI/IOnLoad.cs:5-28 —— 启动阶段（一次性）
public interface IOnLoad { Task OnLoadAsync(CancellationToken cancellationToken); }

// DI/IOnUpdate.cs:3-36 —— 周期（每 5s 轮询）
public interface IOnUpdate { Task<bool> OnUpdateAsync(long secondsSinceLastRun, CancellationToken cancellationToken); }

// DI/IOnDIConstruct.cs:24-36 —— 容器构建前手动注册（静态）
public interface IOnDIConstruct { static abstract Task OnDIConstructAsync(IServiceCollection serviceCollection, CancellationToken cancellationToken); }
```

- `IOnLoad` 分两阶段按 `TypePriority` 执行：
  - `TypePriority ∈ [Watermark(0), GameCallbacks(200000))` → `ProgramExtensions.RunPreSptLoadCallbacks`（`SPTushonka.Server/Extensions/ProgramExtensions.cs:16-46`）；
  - `TypePriority >= GameCallbacks(200000)` → `SPTStartupHostedService.StartAsync`（`Services/Hosted/SPTStartupHostedService.cs:52-66`）。
  - 详见 architecture-map.md。
- `IOnUpdate`：`PeriodicTimer(TimeSpan.FromSeconds(5))`，遍历 `IEnumerable<IOnUpdate>` 调 `OnUpdateAsync(secondsSinceLastRun, token)`；返回 `true` 才刷新时间戳（`SPTStartupHostedService.cs:81,85-101`）。
- `OnLoadOrder` 阶段常量（`DI/OnLoadOrder.cs:3-15`）：

| 常量 | 值 |
|------|-----|
| `Watermark` | 0 |
| `Preload` | 100000 |
| `GameCallbacks` | 200000 |
| `TraderRegistration` | 300000 |
| `Routers` | 400000 |
| `HandbookCallbacks` | 500000 |
| `SaveCallbacks` | 600000 |
| `TraderCallbacks` | 700000 |
| `PresetCallbacks` | 800000 |
| `RagfairCallbacks` | 900000 |
| `PostLoad` | 1000000 |

- mod 常用写法：`OnLoadOrder.Preload + 1`（`Testing/TestMod/TestMod.cs:30`）、`OnLoadOrder.PostLoad + 1`（`TestMod.cs:49,60`）、`OnLoadOrder.Routers`（各路由，`Routers/Static/AchievementStaticRouter.cs:9`）、`OnLoadOrder.SaveCallbacks`（`Callbacks/SaveCallbacks.cs:11`）。

## `TypePriority` 取值与默认值语义（汇总）

- 声明默认：`int.MaxValue`（`Injectable.cs:7`）。
- 排序方向：升序（小者先注册/先执行，`DependencyInjectionHandler.cs:74`）。
- mod 与 mod 之间：`IModMetadata` 文档说明「类先按 `TypePriority` 排，再以 `ModGuid` 为 tiebreaker」（`Models/Spt/Mod/IModMetadata.cs:22-26`）；而 `ModLoader` 的 mod 列表本身按 `ModGuid` 升序（`Modding/ModLoader.cs:143`）。
- `IOnLoad` 阶段门槛由 `TypePriority` 与 `GameCallbacks(200000)` 比较决定（`ProgramExtensions.cs:24-25`、`SPTStartupHostedService.cs:54`）。

## 与 4.1 的关系

- `Injectable` 特性完全相同：`[Injectable(InjectionType, typePriority)]` → DI 写法不变（`git diff origin/4.1x-dev...5.0x-dev` 对 `DI` 目录无输出）。

## 已核实位置

- 注解：`Libraries/SPTushonka.DI/Annotations/Injectable.cs:5-20`
- 处理器：`Libraries/SPTushonka.DI/DependencyInjectionHandler.cs:9-19,21-58,60-112,114-147,168-222,224-251,254-266`
- 扩展：`Libraries/SPTushonka.DI/Extensions/DependencyInjectionExtensions.cs:7-17`
- mod 扫描：`SPTushonka.Server/Helpers/ProgramHelpers.cs:68-113`（尤其 `:84-87,91,98`）
- `IOnDIConstruct` 调用：`SPTushonka.Server/Extensions/ServiceCollectionExtensions.cs:11-53`
- 生命周期：`Libraries/SPTushonka.Server.Core/DI/IOnLoad.cs:5-28`、`IOnUpdate.cs:3-36`、`IOnDIConstruct.cs:9-36`、`OnLoadOrder.cs:3-15`
- 执行器：`SPTushonka.Server/Extensions/ProgramExtensions.cs:16-46`、`Services/Hosted/SPTStartupHostedService.cs:42-126`
- 解析示例：`SPTushonka.Server/Program.cs:260`、`SPTushonka.Server.Core/Services/Hosted/SPTStartupHostedService.cs:25,58`
- 示例 mod：`Testing/TestMod/TestMod.cs:11-47`
