# 05: 配置迁移 + 异步/并发规则

**What to build:** 补上配置 schema 迁移规则与异步/并发规则。配置侧要求隐藏 `Internal` 版本字段、显式重置或迁移、schema 版本独立于发布版本、服务端持久化记录带 `SchemaVersion`；异步侧要求 client spawn/tick/patch 路径禁用 `Thread.Sleep` 轮询、共享状态用 CAS / `SemaphoreSlim` / 窄锁、`async void` 必须有异常围栏、verbose 诊断默认关闭、失败日志去重。

**Blocked by:** 02（证据基线刷新）

**Status:** done

- [x] 新增 `STD-CFG-007`（隐藏 Internal schema 版本）、`STD-CFG-008`（显式重置/迁移，MUST）、`STD-CFG-009`（schema 版本独立于发布版本）、`STD-CFG-010`（服务端持久化记录版本），Level / Applies / Evidence 完整。
- [x] 新增 `STD-PERF-009`（client spawn/tick/patch 禁 `Thread.Sleep` 轮询）、`STD-PERF-010`（共享状态 CAS / SemaphoreSlim / 窄锁）。
- [x] 新增 `STD-LOG-006`（`async void` 必须 try/catch 或改 `async Task`）、`STD-LOG-007`（verbose 诊断默认关闭）、`STD-LOG-008`（失败日志去重，MAY）。
- [x] `STD-PERF-009` 如可稳定检查，补 checker 规格与 fixture；`async void`、Fika 程序集隔离等脆弱检查保持 `check: null`。
- [x] 不把 `.Result` / `.Wait()` 的原始命中数当死锁数；规则文本只禁止“在未完成 Task 上阻塞”。
- [x] `validate-mod-standard.ps1` 与 `run-fixtures.ps1` 全绿。

**执行说明：** 与 04、06 采用 worktree 并行；共享注册表 / README / 证据索引冲突由 09 收口。依据 spec 的 D5、D6。

## Comments

- 2026-10-18：实现并合并。新增 CFG-007..010（CFG-008 MUST）、PERF-009/010、LOG-006..008；PERF-009 保持 check:null（文件级正则对主线程热路径误报，ticket 允许 fallback）；证据锚点已补齐。最终集成 103 条 / MUST 31/31。
