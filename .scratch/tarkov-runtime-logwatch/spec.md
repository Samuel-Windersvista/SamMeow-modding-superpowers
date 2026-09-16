# tarkov-runtime 错误/告警即时捕获（logwatch）Spec

Status: ready-for-agent

## Problem Statement

2026-09-15 雷达移植调试期间，三类故障全部靠**事后手动翻 1MB+ 日志**才发现：

| 故障 | 形态 | 发现方式 |
|------|------|----------|
| F12 ConfigurationManager ComboBox 崩溃 | 同一错误刷屏 **6,477 + 6,421 条**（正常输出被淹没） | 截图 + 逐行 grep |
| 战利品移除补丁 KeyNotFoundException | 单次闪退 | 日志尾部逐行回读 |
| `TrackableTransform` AccessViolation | **进程级崩溃**（桥随进程死亡，in-band 无痕） | 只有 `BepInEx/ErrorLog.log` 有栈 |

现状：桥（`tools/tarkov-runtime-bridge`）以 250ms 采样玩家/raid/bot 状态并暴露 raid 事件流，但**没有任何错误/告警可观测面**。后果：错误发现迟、重复错误淹没信号、进程级崩溃只能靠人肉考古。

## Solution

分层捕获，**事件驱动为主、周期聚合为辅**（沿用 ADR-0007 拉取模型，不引入推送通道）：

- **客户端（桥进程内）**：挂一个 BepInEx `ILogListener`（纯托管接口，零 IL2CPP 委托封送风险）→ 写入**有界环形缓冲**（复用 `RaidEventBuffer` 模式）→ 新增 `GET /logs/recent`（游标增量）与 `GET /logs/summary`（**归一化去重聚合**）两个端点。
- **服务器端**：**MCP 侧**增量 tail `SPT_Runtime/user/logs/spt/`、kestrel、requests 日志（按文件偏移游标，不重扫）—— 5–10 秒周期用在这一层。
- **进程级崩溃**：桥随进程死亡无法自报 → MCP 侧同时盯 `BepInEx/ErrorLog.log`（fatal 栈在此留痕）。

## User Stories

1. As a 调试中的开发者, I want 在错误发生后的数秒内看到它（含级别/来源/首末时间）, so that 不用翻日志文件考古
2. As a 调试中的开发者, I want 同一错误聚合为「1 条 + count + 首末时间」, so that 刷屏型错误（如 6,477 条 ComboBox 崩溃）不再淹没信号
3. As a 调试中的开发者, I want 客户端与服务端日志统一可见（source 区分）, so that 一个入口覆盖两侧
4. As a 调试中的开发者, I want 进程级崩溃的 fatal 栈被捕获（`ErrorLog.log` 通道）, so that 闪退不再是黑盒
5. As a 测试编写者, I want 增量游标语义（`since`/`dropped`）, so that 轮询不丢条、不重复
6. As a 维护者, I want 捕获成本恒定（回调只写内存、缓冲有界、无 I/O）, so that 不影响游戏性能
7. As a 维护者, I want MCP 侧 5–10 秒周期聚合 + 工具可即时拉取, so that 「及时」不依赖人工触发
8. As a 维护者, I want 多线程安全的写入路径, so that 任意线程的日志都不丢

## Implementation Decisions

1. **客户端捕获（桥侧）**：`BepInEx.Logging.Logger.Listeners.Add(ringBufferListener)`；实现 `ILogListener.LogEvent(object, LogEventArgs)`。选它而非 Unity `Application.logMessageReceived`：纯托管接口、零封送风险；且 Unity 日志已经由 BepInEx 转发（现有日志中的 `[Message: Unity]` 行即证）。**回调只写内存**（环形缓冲），绝不 I/O。
2. **环形缓冲**：沿用 `RaidEventBuffer` 模式——容量有界（默认 1000 条原始日志）、`seq` 单调、线程安全（锁或 Interlocked）；条目 `{seq, ts(UTC ISO), level, source, text}`；`level` 取 BepInEx 级别（Info/Debug 默认不采集，Warning/Error/Fatal 采集；阈值可配）。缓冲淘汰计数记 `dropped`。
3. **归一化去重聚合**：对文本做归一化（剥离易变 token：24 位 hex id、GUID、数字、文件偏移）得 group key；每组维护 `{key, level, source, count, firstTs, lastTs, sampleText}`；组数上限（默认 500），溢出按最久未更新淘汰并计入 `overflowDropped`。**聚合在桥侧**（进程内成本最低），端点直接返回聚合视图。
4. **端点契约**（JSON，字段序稳定）：
   - `GET /logs/recent?level=<min>&since=<seq>&limit=<n>` → `{seq, dropped, entries:[{seq, ts, level, source, text}]}`
   - `GET /logs/summary?since=<ts>` → `{groups:[{key, level, source, count, firstTs, lastTs, sampleText}], overflowDropped}`（`since` 按时间过滤新增/更新组）
   - 非 raid 时照常可用（日志与 raid 无关）。
5. **MCP 侧（tarkov-runtime-mcp）**：
   - 新增工具 `logs_recent` / `logs_summary`（拉桥端点；参数透传）。
   - 服务器日志 tail：按文件维护 `{path → offset}` 游标，5–10 秒周期增量读取 `user/logs/spt/`、`kestrel/`、`requests/`（新文件自动纳入；文件被截断/轮转时重置偏移）；产出与桥侧同构的聚合组，`source=server:<file>`。
   - **fatal 通道**：监视 `BepInEx/ErrorLog.log`（同偏移游标），产出 `source=fatal` 组（进程级崩溃栈）。
   - 周期聚合结果缓存在 MCP 进程内，工具调用即时返回（拉取模型不变）。
6. **配置**：桥侧 `LogWatchEnabled`（默认 true）、`LogWatchMinLevel`（默认 Warning）、`LogWatchRingSize`（默认 1000）；MCP 侧轮询间隔（默认 5s，可配 5–10s）。
7. **不做**：推送/WebSocket/SSE；日志持久化；UI；自动归因到具体 mod（按 source 前缀粗分即可）。

## Testing Decisions

- **好测试**：只测外部行为——归一化/去重键、环形淘汰与 `dropped`、`since` 增量语义、MCP 侧偏移游标 tail（fixture 日志文件驱动）、fatal 通道解析（fixture ErrorLog 样本，含本次 `TrackableTransform` AV 栈）。
- **接缝**：桥侧纯逻辑类（缓冲 + 归一化器）独立单测（xunit，沿用现有 85 用例模式）；MCP 侧 tail 器 + 聚合器纯函数化（vitest，沿用现有 236 用例模式）；**不新增 MCP-桥接缝**（复用 `BridgeConnection`）。
- **Live 验收**：进 raid 制造一条已知告警（如开一次 F12 触发 ComboBox 路径或人为 `Log.LogWarning`），断言 `logs_summary` 在 ≤10s 内出现且 count 正确；事后断言 `logs_recent` 的 `since` 增量不丢不重。

## Out of Scope

- 推送通道（ADR-0007 维持拉取）；日志文件轮转/清理策略；历史持久化（跨进程重启即失效，符合调试场景）；跨机器聚合；自动告警规则引擎（阈值/通知）。

## Further Notes

- **关联**：ADR-0007（拉取模型）；`.scratch/tarkov-runtime-wave2/spec.md`（环形缓冲 + 增量拉取先例）；`.scratch/tarkov-runtime-mcp/spec.md`（`BridgeConnection` 接缝）；今晚事故证据（ComboBox 刷屏计数、KeyNotFoundException 栈、`TrackableTransform` AV 栈与 `coreclr` 0xc0000005 事件日志）。
- **已知风险**：① 回调热路径成本（必须只写内存 + 有界；EFT 单会话日志量 1MB+）；② 多线程写入（BepInEx 日志来自任意线程）；③ 归一化过度会合并不同错误（先保守：仅剥离明显易变 token）；④ 桥进程死亡即失忆（fatal 通道兜底，其余接受丢失）。
- **开放问题**（实施前可再议）：`level` 默认阈值是否含 Info；`/logs/summary` 的 `since` 用时间还是组 id 游标；MCP 轮询是否常驻后台（进程内 setInterval）还是仅工具调用时惰性刷新。
