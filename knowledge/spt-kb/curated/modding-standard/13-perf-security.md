---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 13 性能与安全（PERF）

> **Domain slug:** `PERF` · **规则 ID 前缀:** `STD-PERF-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 06，2026-09-14）。

## 维度范围

- 性能：避免已知热点模式
- 安全：路径遍历 / 输入校验

## 规则

### STD-PERF-001 — 不在 tick 路径做无修剪的多部位射线扫描与逐帧物理查询

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 报告：`curated/operations/3114-eft016-perf-hotspots.md`（热点 1 感知射线风暴 `EnemyInfo.cs:494-571`：每 bot 对记忆敌人做多部位 Linecast + 可见时 2 条 Raycast，敌人名单只随死亡移除、无距离修剪；热点 5 弹道每帧 2–10 条物理查询 `BallisticsCalculator.cs:146-236`；热点 6 观察玩家×子弹每帧 O(活跃子弹)）；语料：客户端 mod 类目 148 例（EV-CORPUS-TYPE）
- **Rule:** 客户端 patch 的每帧路径避免「每 bot × 每敌人 × 多部位」的射线/物理查询；按距离、可见性或频率修剪敌人名单与查询次数。

```csharp
// 反例：无修剪——每 bot 对全部记忆敌人逐帧做多部位射线
foreach (var enemy in rememberedEnemies) { CheckLookEnemy(enemy); }

// 正例：先按距离/可见性修剪名单（InRange 为示意），再降频
foreach (var enemy in rememberedEnemies.Where(e => InRange(e))) { CheckLookEnemy(enemy); }
```

> 深入：[operations/3114-eft016-perf-hotspots.md](../operations/3114-eft016-perf-hotspots.md)

### STD-PERF-002 — 对每帧遍历的 AI 子系统做分帧或降频

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 报告：`curated/operations/3114-eft016-perf-hotspots.md`（热点 2 `BotOwner.cs:1014-1073` 每 bot 每帧约 25 个子系统，仅 CalcGoal(3.3s)/出生检查(1s) 两处节流、无分帧；已知缺陷：全库仅 3 处显式帧交错 `%2/%3/%20`，100+ 处 `+= Time.deltaTime` 累加器本可降频）；语料：既有规范素材（EV-CORPUS-MATERIALS）
- **Rule:** 每帧遍历 bot/实体的 patch 应把非关键子系统用 `Time.deltaTime` 累加器分帧（如 `%2`/`%3`/`%N`），不要每帧全量串行更新。

```csharp
// 正例：用累加器分帧，非关键子系统每 N 帧才更新一次
_accumulator += Time.deltaTime;
if (_accumulator < Interval) { return; }   // 未到间隔直接返回
_accumulator = 0f;
RunExpensiveSubsystem();
```

> 深入：[operations/3114-eft016-perf-hotspots.md](../operations/3114-eft016-perf-hotspots.md)

### STD-PERF-003 — 把同步寻路等昂贵计算移出每帧主线程或加缓存

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 报告：`curated/operations/3114-eft016-perf-hotspots.md`（热点 3 `BotMover.cs:415-432`：bot 不用 NavMeshAgent，目标一变即 `NavMesh.CalculatePath` + `new NavMeshPath`，同步主线程）；语料：既有规范素材（EV-CORPUS-MATERIALS）
- **Rule:** 寻路/路径重建等昂贵计算按目标变化触发并缓存结果，避免在每帧 tick 中重复 `NavMesh.CalculatePath` 与 `new NavMeshPath`。

```csharp
// 反例：每帧重复分配并重建路径
navMesh.CalculatePath(from, to, mask, new NavMeshPath());

// 正例：仅目标变化时计算，结果缓存复用
if (targetChanged) { _cachedPath = new NavMeshPath(); navMesh.CalculatePath(from, to, mask, _cachedPath); }
```

> 深入：[operations/3114-eft016-perf-hotspots.md](../operations/3114-eft016-perf-hotspots.md)

### STD-PERF-004 — 客户端补丁用 Prefix/Postfix 并满足独立开关与 fail-open

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 经验：`curated/operations/pilot-experience-performancetweaks.md`（补丁工程四原则：全 Prefix/Postfix、禁 Transpiler——`spt-singleplayer` 在 `BotOwner.UpdateManual` 上有固定 IL 索引 Transpiler，叠加即死；每补丁独立开关；fail-open；顺序敏感用 `HarmonyMethod` Priority）；语料：客户端 `[BepInPlugin]` 251 处、`[BepInDependency]` 170 处（EV-CORPUS-MECH）
- **Rule:** 性能/行为 patch 优先用 Prefix/Postfix，避免与官方固定 IL 索引 Transpiler 叠加；每个 patch 独立可开关，且补丁自身异常必须放行原逻辑（fail-open）。

```csharp
[HarmonyPatch(typeof(BotOwner), nameof(BotOwner.UpdateManual))]
static class ExamplePatch
{
    [HarmonyPrefix]
    static bool Prefix()
    {
        try { /* 优化逻辑 */ return true; }
        catch { return true; } // fail-open：异常时不得阻断原逻辑
    }
}
```

> 深入：[operations/pilot-experience-performancetweaks.md](../operations/pilot-experience-performancetweaks.md)、[operations/client-mod-compat-audit-playbook.md](../operations/client-mod-compat-audit-playbook.md)

### STD-PERF-005 — 服务端避免在请求/启动热路径做阻塞式全量操作

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 报告：`curated/operations/415-source-review-report.md` §3（`LocationLifecycleService.cs:221` 每次 raid 开始 `GC.Collect` 阻塞式全量压缩；`TradeHelper.cs:42` `static readonly Lock` 包裹深克隆+I/O 致所有购买全局串行；`DisplaySkillNamesHandler.cs:40` `Thread.Sleep(500)` 同步阻塞约 5.5s）；语料：既有规范素材（EV-CORPUS-MATERIALS）
- **Rule:** mod 的 `IOnLoad`/路由处理不要引入阻塞式全量操作（`GC.Collect`、`Thread.Sleep`、全局锁包裹 I/O），以免放大服务器响应延迟。

```csharp
// 反例：请求/启动热路径上的阻塞式全量操作
GC.Collect();                        // 每次 raid 开始阻塞式全量压缩
lock (_gate) { DeepCloneAndIO(); }   // 全局锁包裹深克隆 + I/O，购买全局串行
Thread.Sleep(500);                   // 同步阻塞
```

> 深入：[operations/415-source-review-report.md](../operations/415-source-review-report.md)、[server-optimization-audit-report.md](../../../../docs/server-optimization-audit-report.md)

### STD-PERF-006 — 高频路径用索引/缓存替代 O(n²) 与逐次反射

- **Level:** MAY
- **Applies:** both
- **Evidence:** 报告：`curated/operations/415-source-review-report.md` §3（`PaymentService.cs:494-499` O(n²) 排序比较器 + 递归线性查找；`AbstractLocalisationService.cs:111` locale key miss 线性 `FirstOrDefault`；`EftEnumConverter.cs:41-59` 每次序列化枚举都反射扫字段）；语料：既有规范素材（EV-CORPUS-MATERIALS）
- **Rule:** 对高频调用的查找/排序/序列化路径，优先建立字典索引或缓存结果，避免 O(n²) 与逐次反射。

```csharp
// 反例：每次序列化枚举都反射扫字段
foreach (var field in type.GetFields()) { /* 逐次反射 */ }

// 正例：构建一次字典索引/缓存后复用
private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new();
```

> 深入：[operations/415-source-review-report.md](../operations/415-source-review-report.md)

### STD-PERF-007 — 校验外部输入路径，禁止路径遍历

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 报告：`curated/operations/415-source-review-report.md` §2.4（`ConfigEditorService.cs:262-283,311-335`：用户可手工编辑的预设 `Id` 未经校验即进 `GetPresetFilePath`，`DeletePreset`/`SavePreset` 可删除或覆盖任意 `*.json`）；语料：无（**机制推断，无语料先例**）（EV-NOCORPUS）
- **Rule:** 任何由配置、存档或网络请求提供的文件名/Id 在拼接路径前必须白名单校验（或限定在 mod 目录内），禁止直接拼接。

```csharp
// 反例：Path.Combine(dir, presetId + ".json")
string safeId = Path.GetFileName(presetId);            // 剥离目录分量
if (safeId != presetId || safeId.Contains("..")) return;
string path = Path.Combine(dir, safeId + ".json");
```

> 深入：[operations/415-source-review-report.md](../operations/415-source-review-report.md)

### STD-PERF-008 — 输入校验 fail-closed，不 log-then-continue

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 报告：`curated/operations/415-source-review-report.md` §2.4（`HttpServer.cs:35-37`：`PHPSESSID` cookie 未校验直接构造 MongoId，非法值抛异常被吞）、§3（`SptLoggerMiddleware.cs:38-50` catch 后不写响应不 rethrow，异常被吞返回 200 空 body）、§5（log-then-continue 无效防御反复出现）；语料：无（**机制推断，无语料先例**）（EV-NOCORPUS）
- **Rule:** 输入非法时应拒绝并返回明确错误（fail-closed）；不要只记日志后继续执行，也不要用空 body 或默认值掩盖失败。

```csharp
// 反例：log-then-continue——只记日志不 rethrow、不写响应，异常被吞后返回空 body
catch (Exception ex) { logger.LogError(ex); }

// 正例：fail-closed——输入非法即拒绝并返回明确错误
if (!IsValid(cookie)) { response.StatusCode = 400; return; }
```

> 深入：[operations/415-source-review-report.md](../operations/415-source-review-report.md)
