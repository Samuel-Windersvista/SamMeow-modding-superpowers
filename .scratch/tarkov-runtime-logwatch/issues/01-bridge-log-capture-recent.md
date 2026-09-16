# 01: 桥侧日志捕获 + /logs/recent

**What to build:** 桥进程内任意线程产生的 Warning/Error/Fatal 日志（阈值可配，默认 Warning）被 BepInEx ILogListener 捕获进有界环形缓冲（默认 1000 条；回调只写内存、零 I/O、绝不外抛异常），并经 `GET /logs/recent?level=<min>&since=<seq>&limit=<n>` 以与 /raid/events 同款的增量游标语义返回 `{seq, dropped, entries:[{seq, ts, level, source, text}]}`。非 raid 时照常可用；`/bridge/info` 的 capabilities 增列新端点与 section；插件 Unload 时解除 listener 注册。

**Blocked by:** None (can start immediately)

**Status:** ready-for-human

- [x] 环形缓冲：容量有界、seq 单调、多线程写入安全、since 过旧时从最旧返回并计 `dropped`（xunit）
- [x] 级别过滤：默认采集 Warning 及以上；`MinLevel` 可配（xunit）
- [x] 配置项 `[LogWatch] Enabled` / `MinLevel` / `RingSize` 经 BepInEx Config.Bind 声明（STD-CFG-006）
- [x] 捕获回调只写内存：无 I/O、异常绝不外抛（补丁体红线同款约束）
- [x] `GET /logs/recent` 增量语义与 /raid/events 一致（since/limit 缺省与上限、字段序稳定、数值 InvariantCulture；级别过滤先于 limit 截窗——评审修复轮）
- [x] 路由纳入既有判定（未知路径 404 / 已知路径非 GET 405）且非 raid 可用
- [x] `/bridge/info` capabilities（endpoints + sections）更新
- [x] 插件 Unload 解除 listener 注册（无泄漏）
- [x] 既有 xunit 全绿；`ProtocolVersion` 保持 1（纯增量端点，向后兼容）

## Comments

### 2026-09-16 深夜 实现 + 验收（orchestrator）

- @fixer 实现（TDD，红→绿）：`ILogListener` 适配层（预过滤掩码 Fatal|Error|Warning、回调吞异常零日志）→ 有界环形缓冲 → `GET /logs/recent`；配置 `[LogWatch] Enabled/MinLevel/RingSize`；Unload 摘 listener；ProtocolVersion 保持 1。
- 单测覆盖：缓冲淘汰/`dropped`/并发、级别归一化与阈值、路由、payload 字段序。
- 评审修复轮并入：级别过滤先于 limit 截窗（消除 starvation）、死成员清理（`DefaultMinLevel`）、注释纠偏、配置键去冗余（评审记录见工单 06）。
- 全波套件：桥 **295/295**（评审修复后 orchestrator 独立复跑，0 error）；live 验收见工单 05。
