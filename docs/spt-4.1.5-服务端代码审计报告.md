# SPT 4.1.5 服务端源代码审计报告

> 日期：2026-09-06 | 对象：`E:\云文件\GitHub\SamMeow_SP-Tushonka_source_code`（SP-Tushonka 4.1.5，`7d7add55`）
> 范围：SPTushonka.Server + Libraries/SPTushonka.*（Server.Core 为主战场），共 ~10.2 万行 C#（net10.0，Kestrel HTTP 服务端）
> 方法：结构地图（codemap）→ 全库反模式系统扫描（Random/序列化/阻塞/GC/锁/异步）→ 命中点逐一读码核实
> 约束说明：本次为直接执行审计（子代理配额当周耗尽，未走并行侦察）；所有发现均经读码验证，非模式猜疑。

---

## 1. Codemap（结构地图）

| 模块 | 文件数 | 行数 | 职责 |
|---|---|---|---|
| `Server.Core/Models/` | 438 | 27,692 | DTO/数据模型（纯数据，未审计） |
| `Server.Core/Services/` | 56 | 15,298 | 业务服务层（ragfair/商贸/hideout/bot/in-raid/profile/modding） |
| `Server.Core/Helpers/` | 68 | 14,782 | 业务助手（ragfair/dialogue/traders/items/quest/profile） |
| `Server.Core/Generators/` | 33 | 11,430 | 生成器（bot/loot/ragfair/weather/repeatable quest/scav case） |
| `Server.Core/Controllers/` | 30 | 9,231 | HTTP 控制器（inventory/hideout/bot/notifier/quest/weather） |
| `Server.Core/Utils/` | 44 | 3,813 | 工具（json/random/cloner/time/hash/collections） |
| `Server.Core/Callbacks/` | 34 | 3,105 | 路由回调（item event/game start/end/profile 等） |
| `Server.Core/Routers/` | 50 | 1,986 | 路由表 + serializer（含 Notify 热路径） |
| `Server.Core/Extensions/` | 23 | 2,182 | 扩展方法 |
| `Server.Core/Migration/` | 21 | 1,527 | profile 版本迁移 |
| `SPTushonka.Server/` | 16 | 1,740 | 启动入口/WebSocket/证书 |
| `Server.Web/` | 50 | 4,870 | Web UI（MudBlazor SIC 控制台） |
| `Common/DI/Reflection/` | 35 | 2,142 | 基础设施 |

**数据流**：Kestrel → Routers 路由 → Callbacks → Controllers/Services → Helpers/Generators → Utils（JSON/克隆/随机）→ 客户端响应。热点：notifier 轮询、item event 库存操作、ragfair 刷新、bot/loot 生成、raid 结束结算。

---

## 2. 发现总表

| # | 级别 | 类型 | 位置 | 摘要 |
|---|---|---|---|---|
| 1 | **高** | 性能 | RagfairOfferGenerator.cs:309-320 | ragfair 刷新时**每个商品一个 Task**（上千并发任务）+ WaitAll，线程池风暴 |
| 2 | **高** | Bug | RagfairSellHelper.cs:107 | 批量上架**循环内 new Random()**，种子重复→所有商品同时卖出（作者已在 :109 打补丁治标，未治本） |
| 3 | **高** | 性能/Bug | RagfairServer.cs:74 / LocationLifecycleService.cs:221 / GarbageMessageHandler.cs:27 | 三处**显式全代阻塞 GC.Collect**（ragfair 周期 / 每局结束 / **玩家发"garbage"消息即触发**——调试代码残留） |
| 4 | 中 | 性能 | NotifierController.cs:30-56 | 长轮询用 `Task.Factory.StartNew` + `Thread.Sleep(300)` 阻塞线程池线程 15 秒/请求 |
| 5 | 中 | 质量/性能 | NotifySerializer.cs:32-33, NotifierCallbacks.cs:40-41 | notifier 热路径 `.ContinueWith(x => x.Result)` 反模式 + 逐消息 `string.Join` 分配 |
| 6 | 中 | 性能 | BotController.cs:199-212 | 每波 bot `Task.Run`，无取消令牌、无并发上限 |
| 7 | 中 | 并发 | RagfairOfferHolder.cs（13 处 lock） | 单一大锁 `_ragfairOperationLock` 串行化全部 ragfair 操作（搜索与写入互斥） |
| 8 | 低 | Bug | RepairService.cs:253/601 | 修理技能点计算内 `new Random()`（同 tick 多请求种子冲突；应 `Random.Shared`） |
| 9 | 低 | Bug | CircleOfCultistService.cs:214 | 同上，`new Random()` |
| 10 | 低 | 质量 | BackupService.cs:82-98 | `Timer(async void)` 回调——async void 异常可炸进程（现有 try/catch 兜底，应改 PeriodicTimer） |
| 11 | 低 | 性能 | RepairHelper.cs:40-42 | 对 `double` 值类型调用 3 次 FastCloner 深克隆（反射开销纯浪费） |
| 12 | 低 | 一致性 | CircleOfCultistService.cs:825, SaveServer.cs:275 | MD5 残留：物品哈希、存档校验仍用 MD5（4.1.3 文件加载已迁 XxHash3，未跟进） |
| 13 | 低 | 性能 | FileUtil.cs:18, ConfigLoader.cs:57 | `Directory.GetFiles` 全量路径数组分配（低频，影响小） |
| 14 | 低 | 质量 | ReleaseCheckService.cs:21/58 | fire-and-forget `Task.Run` + 空 catch，版本检查失败完全静默 |

---

## 3. 详细发现

### [高] #1 Ragfair 动态报价生成任务风暴

```csharp
// RagfairOfferGenerator.cs:309-320
var tasks = new List<Task>();
foreach (var assortItemWithChildren in assortItemsToProcess)   // 商品数可达数千
{
    tasks.Add(Task.Factory.StartNew(() => CreateOffersFromAssort(...)));  // 每商品 1 个任务！
}
Task.WaitAll(tasks.ToArray());
```

**问题**：ragfair 每次刷新报价时，为每个商品创建独立线程池任务，无并发上限。数千任务同时争抢线程池 → 上下文切换风暴 + 线程池饥饿，直接阻塞服务器其它请求（客户端可能在这段时间请求超时）。`Task.Factory.StartNew` 的默认调度还绕过了 `ConfigureAwait` 语义。

**建议**：改 `Parallel.ForEachAsync(assortItemsToProcess, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, ...)`，或按 CPU 核数分批处理。`CreateOffersFromAssort` 内部还有大量 cloner 克隆，批量化收益叠加。

---

### [高] #2 Ragfair 批量上架卖出时间全相同（真 Bug）

```csharp
// RagfairSellHelper.cs:91-113
while (remainingCount > 0 && sellTimestamp < endTime)
{
    ...
    var random = new Random();          // ← 循环内实例化：同 tick 内所有实例同种子
    var newSellTime = Math.Floor(random.NextDouble() * (maximumTime.Value - minimumTime) + minimumTime);
    if (newSellTime == 0)
    // Ensure all sales don't occur the same exact time
    {
        newSellTime += 1;               // ← 作者已观察到"同时卖出"症状，只打补丁未治本
    }
    ...
}
```

**问题**：`Random` 默认种子基于 `Environment.TickCount`，循环在同一毫秒内完成 → 所有实例序列相同 → **批量上架的所有商品在同一时刻"售出"**。:109 的注释与 `+1` 补丁证明作者亲眼见过该症状但没找到根因。讽刺的是同一方法 :93-94 已经在用注入的 `randomUtil`（线程安全封装），此处却另行 `new Random()`。

**建议**：删除局部 `new Random()`，改用 `randomUtil.GetInt(minimumTime, maximumTime)`（该文件已有依赖注入的 RandomUtil）。

---

### [高] #3 显式全代阻塞 GC（三处，含玩家可触发的调试残留）

```csharp
// RagfairServer.cs:74 —— ragfair 每个刷新周期
GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, true, true);

// LocationLifecycleService.cs:221 —— 每局结束（结算高峰时刻）
GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

// GarbageMessageHandler.cs:27 —— 玩家给 SPTFriend 发 "garbage" 消息即触发！
public void Process(MongoId sessionId, UserDialogInfo sptFriendUser, ...)
{
    var beforeCollect = GC.GetTotalMemory(false) / 1024 / 1024;
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
    ...
}
```

**问题**：三处手动全代 GC，参数 `blocking: true` 直接阻塞服务器所有线程。`Aggressive` 是代价最高的模式（压缩 + 大量扫描）。后果：ragfair 刷新瞬间卡顿、**每局结束卡顿**（与存档序列化、结算日志叠加，正是 1% low 尖峰的服务器侧来源之一）、以及最离谱的——**任何玩家在游戏内向 SPTFriend 发消息"garbage"就能触发全服卡顿**（这是开发者调试内存用的功能，残留在生产路径，还通过邮件回显内存数字）。

**建议**：GarbageMessageHandler 直接删除或加调试开关；RagfairServer/LocationLifecycleService 的 GC.Collect 删除，让 GC 自行工作——若确需释放大对象图，改为 `GCSettings.LargeObjectHeapCompactionMode` 临时设置或把 profile/location 数据置于可回收作用域。

---

### [中] #4 Notifier 长轮询线程阻塞

```csharp
// NotifierController.cs:30-56
return Task.Factory.StartNew(() =>
{
    var counter = 0;
    while (counter < Timeout)              // 最多 15 秒
    {
        if (!notificationService.Has(sessionId))
        {
            counter += PollInterval;
            Thread.Sleep(PollInterval);    // ← 同步睡眠阻塞线程池线程
        }
        ...
    }
}, cancellationToken);
```

**问题**：notifier 是客户端**持续轮询**的端点（15 秒超时）。每个请求独占一个线程池线程做"sleep 轮询"。单机 1 玩家 = 1 线程常驻，看似无害；但这是经典反模式，且 Fika/局域网多人时线性放大；`Task.Factory.StartNew` 还占用默认调度队列。另外轮询语义本身可以事件化。

**建议**：用 `SemaphoreSlim.WaitAsync(timeout)` 或 `Channel<T>` + 事件通知替代轮询；若保留轮询，改 `await Task.Delay(PollInterval, cancellationToken)`。

---

### [中] #5 Notify 序列化热路径反模式

```csharp
// NotifySerializer.cs:30-33
await notifierController
    .NotifyAsync(tmpSessionID, cancellationToken)
    .ContinueWith(messages => messages.Result.Select(message => string.Join("\n", jsonUtil.Serialize(message))))
    .ContinueWith(text => httpServerHelper.SendTextJson(resp, text), cancellationToken);
```

**问题**：`ContinueWith` 链 + `.Result`（阻塞取已完成任务）+ 无 `TaskScheduler` 约束（ContinueWith 默认调度器可跳线程）+ 逐消息序列化后 `string.Join` 中间数组分配。notifier 是最高频轮询路径。同时 :23 `req.Path.Value.Split("/")` 每次请求分配字符串数组——小但可省。

**建议**：改写成 `var messages = await notifierController.NotifyAsync(...); var text = string.Join("\n", messages.Select(...)); await httpServerHelper.SendTextJson(resp, text);`。

---

### [中] #6 Bot 波次生成任务与 #7 Ragfair 单一大锁

```csharp
// BotController.cs:199-212
var waveGenerationTasks = request.Conditions.Select(condition =>
    Task.Run(() => GenerateBotWave(...)));    // 无取消令牌、无并发上限
```

波次数量有限（单局 10~30），风险低于 #1，但与 #1 叠加时线程池竞争。建议统一传 `cancellationToken` 并考虑 `MaxDegreeOfParallelism`。

```csharp
// RagfairOfferHolder.cs —— 同一把锁 13 处
lock (_ragfairOperationLock) { ... }   // 覆盖 GetOffers/GetOfferById/AddOffer/RemoveOffer 全部操作
```

**问题**：所有 ragfair 读操作（客户端搜索报价）与写操作（出售/过期处理）互斥。单机玩家搜索报价时会与服务器刷新流程互相阻塞。**建议**：读写分离（`ReaderWriterLockSlim`）或对 offer 字典用 `ConcurrentDictionary` + 细粒度锁。

---

### [低] #8-#14 杂项

- **#8/#9 `new Random()`**（RepairService.cs:253/601、CircleOfCultistService.cs:214）：低频场景种子冲突概率低，但 .NET 6+ 有现成 `Random.Shared`，零成本修复。
- **#10 `Timer(async void)`**（BackupService.cs:82-98）：async void 未捕获异常会直接崩进程；当前内部有 try/catch 兜底，风险已降，但模式应改为 `PeriodicTimer`。
- **#11 值类型 DeepClone**（RepairHelper.cs:40-42）：`cloner.Clone(itemToRepair.Upd.Repairable.MaxDurability)` 对 `double` 跑 FastCloner 反射克隆，纯浪费。
- **#12 MD5 残留**（CircleOfCultistService.cs:825 物品哈希、SaveServer.cs:275 存档校验）：与 4.1.3 的 XxHash3 文件哈希迁移不一致；MD5 碰撞已被实爆，物品哈希碰撞可导致存档/物品判定异常（概率极低但非零）。
- **#13 Directory.GetFiles**（FileUtil.cs:18、ConfigLoader.cs:57）：启动期一次性调用，影响可忽略，仅记入风格账。
- **#14 ReleaseCheckService 静默失败**（:21 `_ = Task.Run(CheckForUpdate)` + :58 空 catch）：版本检查失败无任何日志，用户永远不知道"为什么没有更新提示"。

---

## 4. 正面观察（做得好的部分）

1. **JsonUtil**：序列化选项静态缓存（NoIndent/Indented 两个实例复用），无 per-call options 坑。
2. **FileUtil 原子写**：`.bak` 临时文件 + `File.Move(overwrite)` 模式，断电/崩溃下不会产生半截 profile 文件（存档安全关键路径）。
3. **BotLootCacheService**：bot 战利品池按 bot 类型缓存 + 生成锁，避免每局重复生成战利品表。
4. **XxHash3 迁移**（4.1.3）：文件哈希已从 MD5 迁移，方向正确（本文 #12 是未迁移完全的尾巴）。
5. **TimeUtil 统一时间源**：全库经 TimeUtil 取时间（可测性 + 一致性），`DateTime.UtcNow` 直用仅 9 处且均在合理场景。
6. **并发意识**：RagfairOfferHolder、BotLootCacheService、TradeHelper、Ws 连接管理器等关键共享状态均有锁保护（虽有 #7 的粒度问题，但方向正确）。
7. **4.1.3 分配优化**：客户端启动路径、id lookup 等已做过一轮分配削减，说明团队有性能意识。

---

## 5. 修复优先级路线

| 批次 | 内容 | 工作量 | 收益 |
|---|---|---|---|
| P0（立刻） | #2 循环 Random（一行改 randomUtil）+ #3 删三处 GC.Collect（GarbageMessageHandler 直接删） | 30 分钟 | 修真 bug + 消除玩家可触发卡顿 |
| P1（本周） | #1 ragfair 任务风暴改 Parallel.ForEachAsync 限流 + #4 notifier 改 async 等待 | 半天 | 消除 ragfair 刷新与 notifier 轮询的线程池压力 |
| P2（择机） | #5/#6/#7 热路径重构（notify serializer 直写、bot 波次取消令牌、ragfair 读写锁） | 1-2 天 | 多人/Fika 场景扩展性 |
| P3（顺手） | #8-#14 杂项：Random.Shared、PeriodicTimer、删值类型克隆、MD5→XxHash3、版本检查日志 | 半天 | 质量账清零 |

> 与客户端侧的联系：`eft-0.16.9.5-spt412-性能复查报告.md` 记录的是客户端 12 病灶；本报告是**服务器侧**审计，两者互补。服务器侧发现集中在 ragfair/notifier/GC 三个主题——其中 #3 的"每局结束 GC.Collect"很可能贡献了你观察到的战局结算时刻顿卡（与客户端侧无关的独立来源）。

---
