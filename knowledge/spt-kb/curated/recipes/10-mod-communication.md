---
version: [4.1]
domain: server
topic: recipe
recipe_task: mod-communication
source: curated
---
# 配方：Mod 间通信 [4.1]

> 状态：已提炼（源自 4.1 源码检索结论——Core 中**无**专用 ModRegistry/EventHub/MessageBroker 类；机制为 DI 共享 + 元数据声明）
> 适用：[4.1] | 结论基于源码实搜，若后续发现官方通信组件再更新

## 机制总览

SPT 4.1 **没有官方的 mod 间消息总线**。mod 间通信靠三条路：

### 1. DI 共享服务（推荐，唯一官方支持的跨 mod 注入通道）

所有 mod 的程序集都被同一容器扫描，因此 **A mod 的 `[Injectable]` 类可以被 B mod 构造注入**：

```csharp
// A mod：注册一个共享单例（提供数据/服务）
[Injectable(InjectionType.Singleton)]
public class SharedState
{
    public string SomeData { get; set; } = "";
}

// B mod：直接用
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class Consumer(SharedState sharedState) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken ct) { ... }
}
```

要点：
- 共享类必须 `InjectionType.Singleton`（否则每处注入都是新实例）
- 时序：`TypePriority` 决定谁先加载——A 要在 `Preload` 写入、B 在 `PostLoad` 读取
- config 类共享：A 用 `IOnDIConstruct` 注册实例，B 注入同一实例（见 api-notes config-system.md）

### 2. 元数据声明（依赖/冲突）

`IModMetadata` 的 `ModDependencies`（`Dictionary<string, SemanticVersioning.Range>`）与 `Incompatibilities`（`List<string>`）——声明依赖关系，加载器按版本范围校验。这不是运行时通信，但让加载顺序与冲突可预期。

### 3. 静态/文件通道（自己造的轮子）

- 静态类：程序集静态字段跨 mod 共享（但容器外，无生命周期管理）
- 文件：`user/mods/<guid>/` 下 JSON（ModHelper 可读自己文件夹；读别人的文件夹路径约定需双方协商）

## 判断标准

| 需求 | 选 |
|------|-----|
| A 提供数据/能力，B 消费 | DI 单例（机制 1） |
| 依赖版本约束 | ModDependencies（机制 2） |
| 双向实时消息（事件推送） | 自建（如 B 轮询共享单例状态；或 WebSocket/文件）——无官方事件总线 |

## 坑

- 忘写 `InjectionType.Singleton` = 数据不同步（最常见错误）
- 循环依赖：A 注入 B 且 B 注入 A → 容器解析失败，启动即炸
- 引用对方程序集：B 项目要引用 A 的 DLL 或共享接口程序集——SPT 不提供接口注册中心，**建议把共享接口放独立小程序集**，双方只依赖接口
- 两个 mod 若一方未装，另一方注入会失败——用 `ModDependencies` 强制或 `IEnumerable<T>` 注入容忍缺失

## 待核实（已销账 2026-08-02）

- [x] ~~Server.Web 或加载器程序集中是否有官方通信组件~~ — Core/DI 全检索无 ModRegistry/EventHub；`SPTarkov.DI` 即 MSDI 封装，无消息总线
- [x] ~~4.1 是否支持 `IEnumerable<T>` 注入~~ — **支持**：底层是 MSDI，`DependencyInjectionHandler` 也用它注册多实例（如 `IEnumerable<IRuntimePatch>`、`IEnumerable<IProfileMigration>` 在源码中直接注入）
- [x] 接口注入 — **支持**：`DependencyInjectionHandler` 会把实现的非 System 接口与基类一并注册（递归），配方 10 的「共享接口程序集」建议有官方机制支撑

## 已核实源码坐标

- `Libraries/SPTarkov.DI/DependencyInjectionHandler.cs` — MSDI 封装、接口/基类注册、泛型注册
- `Libraries/SPTarkov.DI/Annotations/Injectable.cs` — InjectionType 枚举（HostedService/Singleton/Transient/Scoped）
- `IModMetadata.cs` — 加载顺序 tiebreaker（TypePriority → ModGuid 字母序）

## 来源

- 源码检索：`Libraries/SPTarkov.Server.Core`（无 ModRegistry/EventHub 类）
- `SPTarkov.DI.Annotations`（[Injectable] 的 InjectionType 定义）
- 迁移文档第 3 节（DI）、第 2 节（元数据）
