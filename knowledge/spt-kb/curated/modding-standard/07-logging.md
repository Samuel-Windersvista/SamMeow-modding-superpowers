---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 07 日志与错误处理（LOG）

> **Domain slug:** `LOG` · **规则 ID 前缀:** `STD-LOG-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 04，2026-09-14）。

## 维度范围

- `ISptLogger` 使用与日志级别
- 错误处理与降级
- 不吞异常

## 规则

### STD-LOG-001 — 服务端通过构造函数注入 ISptLogger<T> 记录日志

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`Libraries/SPTarkov.Common/Models/Logging/ISptLogger.cs`（EV-MECH-COORD）、`knowledge/spt-kb/curated/api-notes-4.1/di-container.md`；语料：`ISptLogger` 614 处（EV-CORPUS-MECH）
- **Rule:** 服务端 mod 的日志一律使用构造函数注入的 `ISptLogger<T>`（`T` 为当前类），不得绕过它使用 `Console.WriteLine` 或自建静态日志器。

```csharp
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;

[Injectable(InjectionType.Singleton)]
public class MyService(ISptLogger<MyService> logger)
{
    public void DoWork() => logger.Info("MyService 已就绪");
}
```

> 5.0 差异：命名空间前缀为 `SPTushonka.*`（如 `SPTushonka.Common.Models.Logging`），接口形态一致。
> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)

### STD-LOG-002 — 按语义选择日志级别而非一律 Info

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`Libraries/SPTarkov.Common/Models/Logging/ISptLogger.cs:7-15`（`Success/Info/Warning/Error/Critical/Debug`、`Log(LogLevel,…)`、`IsLogEnabled(LogLevel)`）、`knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md:179-181`（级别改用 `Microsoft.Extensions.Logging.LogLevel`：Fatal→Critical、Warn→Warning、Info→Information）；语料：`ISptLogger` 614 处（EV-CORPUS-MECH），无级别细分计数
- **Rule:** 按语义选级别：正常流程用 `Info` / `Success`，可恢复异常用 `Warning`，失败用 `Error`，致命用 `Critical`，诊断用 `Debug`；拼装昂贵消息前先用 `IsLogEnabled(LogLevel.Debug)` 判定。

```csharp
using Microsoft.Extensions.Logging;

if (logger.IsLogEnabled(LogLevel.Debug))
{
    logger.Debug($"详细数据: {JsonSerializer.Serialize(payload)}");
}

logger.Warning("配置项缺失，使用默认值");
logger.Error("写入失败", ex);
```

> 深入：[migration/api-mapping-311-to-41.md](../migration/api-mapping-311-to-41.md)

### STD-LOG-003 — 客户端使用 BepInEx BaseUnityPlugin.Logger 记录日志

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`templates/client-mod/src/Plugin.cs`、`knowledge/spt-kb/curated/modding-guide/03-client-mod-anatomy.md`；语料：`Logger.` 调用 4968 处、`BaseUnityPlugin` 275 处（EV-CORPUS-MECH）
- **Rule:** 客户端 mod 使用 `BaseUnityPlugin.Logger`（BepInEx）记录日志，不得引用服务端 `ISptLogger<T>`。

```csharp
public class MyPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo("MyPlugin 已加载");
        Logger.LogError("初始化失败");
    }
}
```

> 深入：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)

### STD-LOG-004 — 捕获异常必须记录并降级，不得静默吞异常

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`Libraries/SPTarkov.Common/Models/Logging/ISptLogger.cs:8-13`（`Success/Error/Warning/Info/Debug/Critical` 均接受 `Exception? ex`）；语料：无对应计数（机制推断，无语料先例）（EV-NOCORPUS）
- **Rule:** `catch` 块必须至少用 `logger.Error(message, ex)` / `logger.Warning(message, ex)` 记录异常并明确降级路径（跳过该可选功能而非中断加载）；禁止空 `catch` 或不记录异常。

```csharp
try
{
    ApplyOptionalPatch();
}
catch (Exception ex)
{
    // 记录完整异常并降级：本功能跳过，不影响其余加载
    logger.Error("ApplyOptionalPatch 失败，已跳过", ex);
}
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)

### STD-LOG-005 — 取消不是错误：让 OperationCanceledException 正常传播

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md:52-56`（生命周期方法带 `CancellationToken`、让 `OperationCanceledException` 正常传播）；语料：无对应计数（机制推断，无语料先例）（EV-NOCORPUS）
- **Rule:** 生命周期与异步 IO 应把 `CancellationToken` 传播给一切接受它的调用；长时间同步循环中定期调用 `ThrowIfCancellationRequested()`；不要捕获吞掉 `OperationCanceledException`，也不要把取消记为 `Error`。

```csharp
public async Task OnLoadAsync(CancellationToken cancellationToken)
{
    await using var stream = File.OpenRead(path);
    var config = await JsonSerializer.DeserializeAsync<MyConfig>(stream, cancellationToken: cancellationToken);

    // 长时间同步循环中定期检查
    cancellationToken.ThrowIfCancellationRequested();
}
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)
> 交叉引用：`STD-SRV-003`（token 传播）。
