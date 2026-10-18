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
- **Rule:** 服务端 mod 的日志一律使用构造函数注入的 `ISptLogger<T>`（`T` 为当前类），不得绕过它使用 `Console.WriteLine` 或自建静态日志器。本规则约束的是「记录日志时的方式」——无日志需求的 mod 不强制产生日志。

```csharp
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;

[Injectable(InjectionType.Singleton)]
public class MyService(ISptLogger<MyService> logger)
{
    public void DoWork() => logger.Info("MyService 已就绪");
}
```

> 5.0 差异：接口形态一致；服务端日志命名空间仍为 `SPTarkov.Common.Models.Logging`，`SPTushonka.Common` 是 NuGet 包名。
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
- **5.0 差异：** 属性名改为 `BasePlugin.Log`（`ManualLogSource`），调用形式 `Log.LogInfo(...)` 不变（见 [version-matrix.md](version-matrix.md) 与 [05-client.md](05-client.md) 的 STD-CLI-006 分支）。

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
- **Rule:** 生命周期与异步 IO 应把 `CancellationToken` 传播给一切接受它的调用；长时间同步循环中定期调用 `ThrowIfCancellationRequested()`；不要捕获吞掉 `OperationCanceledException`，也不要把取消记为 `Error`。纯同步实现（无 IO / 异步调用）没有可传播的 token，保持接口签名即可（见 `STD-SRV-003`），此情形下不要求额外的取消检查。

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

### STD-LOG-006 — `async void` 必须自带 try/catch 围栏或改为 `async Task`

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`async void` 的异常无法被调用方捕获，只能冒泡为 `AppDomain`/Unity 未处理异常（`knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md` 生命周期 async 约定、STD-SRV-003 的 `async Task` 签名、STD-LOG-004 的异常记录）；语料：129 个文件含 `async void`（EV-CORPUS-MECH）
- **Rule:** `async void` 方法必须在方法体内用 try/catch 完整包裹并记录异常（见 `STD-LOG-004`），或改为 `async Task` 交由调用方 await；仅事件处理器等不得不为 `async void` 的签名保留该形态，并由其自身承担异常围栏。

```csharp
// 正例：async void 自带 try/catch 围栏，异常不会逃逸
private async void OnSomething(object? sender, EventArgs e)
{
    try { await DoWorkAsync(); }
    catch (Exception ex) { Logger.LogError($"OnSomething 失败: {ex}"); }
}

// 更优：能等待就改成 async Task，由调用方捕获
private async Task DoWorkTrackedAsync() => await DoWorkAsync();
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)
> 交叉引用：`STD-LOG-004`（捕获必须记录并降级）。

### STD-LOG-007 — verbose / 性能诊断日志由配置门控且默认关闭

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：服务端 `ISptLogger.IsLogEnabled(LogLevel.Debug)` 与客户端 BepInEx 日志级别可门控诊断输出（`Libraries/SPTarkov.Common/Models/Logging/ISptLogger.cs:7-15`、STD-LOG-002）；语料：194 个仓库 / 2,171 命中含诊断相关代码（EV-CORPUS-MECH），样例 C11-True-North 的 Diagnostics 配置段（metadata-scan 诊断样例）
- **Rule:** verbose / 性能诊断日志（逐帧计时、逐 bot 细节、序列化 dump）必须由配置项或常量门控，并默认关闭；不要在热路径无条件拼装诊断字符串。

```csharp
if (configuration.VerboseDiagnostics.Value && logger.IsLogEnabled(LogLevel.Debug))
{
    logger.Debug($"bot {botId} goal={goal} t={elapsedMs}ms");
}
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)
> 交叉引用：`STD-LOG-002`（级别语义）、`STD-PERF-009`（热路径）。

### STD-LOG-008 — 对热路径/每包重复失败日志做去重，避免刷屏

- **Level:** MAY
- **Applies:** both
- **Evidence:** 机制：热路径或每包回调内的 `Error`/`Warning` 会刷屏并淹没其它信号（STD-LOG-002 级别语义）；语料：样例 Airburst 的 `_sendFailureLogged` 一次性标记（metadata-scan 诊断样例；EV-CORPUS-MECH；诊断/日志计数为本次语料内联）
- **Rule:** 对每帧 / 每包 / 每请求都可能重复触发的失败日志，用一次性标记或去重键抑制重复输出，避免刷屏。

```csharp
if (!_sendFailureLogged)
{
    _sendFailureLogged = true;
    logger.Error("首次发送失败，后续同类错误不再重复记录", ex);
}
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)
> 交叉引用：`STD-LOG-002`（级别语义）。
