---
version: [4.1]
domain: server
topic: di
source: curated
---
# DI 容器笔记 [4.1]

> 状态：**已核实（源码实读 2026-08-02）** | 适用：[4.1]
> 源码：`Libraries/SPTarkov.DI/DependencyInjectionHandler.cs`、`Annotations/Injectable.cs`

## 容器本质

- 底层 = **Microsoft.Extensions.DependencyInjection（MSDI）** → 标准 MSDI 能力全可用（含 `IEnumerable<T>` 多实例注入）
- 自定义封装：`DependencyInjectionHandler`（一次性使用，注册后抛异常防二次调用）
- 发现机制：反射扫描各程序集带 `[Injectable]` 的类 → 按 `TypePriority` 排序注册

## [Injectable] 特性（源码实读）

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class Injectable(InjectionType injectionType = InjectionType.Transient, int typePriority = int.MaxValue)
```

- **默认 `InjectionType.Transient`**、**默认 `typePriority = int.MaxValue`**（= 极晚加载）——不写 TypePriority 的类几乎最后才解析，重要类务必显式写
- `InjectionType` 枚举：`HostedService`（必须实现 `IHostedService`，否则抛 ArgumentException）/ `Singleton` / `Transient` / `Scoped`

## 注册规则（源码确认）

- 注册目标：具体类 + **实现的非 System 接口 + 基类**（递归）→ 支持接口注入、基类注入
- **泛型注册受支持**（RegisterGenericComponents：`Injectable` 泛型类可匹配构造参数 `IEnumerable<MyGeneric<T>>` 等）
- 构造器参数解析失败 → 容器构建失败（启动即炸，非运行时 null）
- 排序：`OrderBy(TypePriority)`；同值按 ModGuid 字母序（见 IModMetadata 注释）

## 关键注入面（mod 可直接要）

| 类别 | 类型 | 说明 |
|------|------|------|
| 表 | `GlobalTable` `BotTable` `HideoutTable` `LocaleTable` `LocationTable` `MatchTable` `TemplateTable` `TradersTable` `ServerTable` `SettingsTable` | `Models/Spt/Tables` |
| 配置 | 各 `*Config` 具体类型 | `InsuranceConfig`、`TraderConfig`、`RagfairConfig`、`QuestConfig`、`LootConfig`、`BotConfig`、`InventoryConfig` |
| 日志 | `ISptLogger<T>` | `SPTarkov.Common.Models.Logging` |
| 自定义服务 | `CustomItemService` `CustomQuestService` | `Services/Modding/Custom` |
| 客户端枚举 | `ClientEnumDefinitions` | `Services` 中（待定位精确文件） |
| Patch | `IEnumerable<IRuntimePatch>` | 多实例注入（MSDI 原生支持） |
| 迁移 | `IEnumerable<IProfileMigration>` | 存档迁移管线 |

## IOnDIConstruct（源码实读）

```csharp
public interface IOnDIConstruct
{
    static abstract Task OnDIConstructAsync(IServiceCollection serviceCollection, CancellationToken cancellationToken);
}
```

- 在 provider 构建前调用（带取消 token）；只应 Add 注册，**不要在此解析服务**
- 源码注释明确：普通注册用 `[Injectable]` 即可，此接口用于「容器无法自建」的对象（典型：config 实例）

## 已核实坐标

- `Libraries/SPTarkov.DI/Annotations/Injectable.cs` — 特性 + InjectionType 枚举
- `Libraries/SPTarkov.DI/DependencyInjectionHandler.cs` — 扫描/排序/注册（含接口/基类/泛型注册）
- `Libraries/SPTarkov.Server.Core/DI/IOnDIConstruct.cs` — 接口（带 CancellationToken）
- `Libraries/SPTarkov.Server.Core/DI/OnLoadOrder.cs` — 阶段常量
