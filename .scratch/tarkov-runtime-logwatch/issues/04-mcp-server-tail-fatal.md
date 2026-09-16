# 04: MCP 服务器日志 tail + fatal 通道

**What to build:** MCP 进程内按 `{path → offset}` 游标增量 tail SPT server 日志（`SPT_Runtime/user/logs/` 下 spt / kestrel / requests 三个子目录；新文件自动纳入、文件截断或轮转时重置偏移；默认只纳入 Warning 及以上，与桥侧采集阈值语义一致），产出与桥侧同构的归一化聚合组（`source=server:<file>`）；同时以同游标监视 `BepInEx/ErrorLog.log` 产出 `source=fatal` 组（进程级崩溃栈；整文件视为错误输出，不过滤级别）。结果并入 `logs_summary` 输出（桥组 + 服务器组 + fatal 组统一可见、source 区分；合并后按桥侧同序排序：count 降序 → lastTs 降序 → key 升序）。**桥不可达/协议不匹配/旧桥端点缺失时，`logs_summary` 仍返回 ok 信封（服务器组 + fatal 组照常），`bridge.available=false` + reason——进程级崩溃会杀死桥进程，fatal 通道必须在该场景下仍可用（本通道存在的根本理由）。** 刷新策略默认惰性（调用时若距上次 ≥ 间隔即读增量），间隔可配 5–10s。

**Blocked by:** 03

**Status:** ready-for-human

- [x] 偏移游标 tail：增量不重不漏（半行不推进，防截断重复）；新文件纳入；截断/轮转重置（vitest fixture 驱动）
- [x] 行解析 `[ts][Level][Source] text`；无前缀续行（异常栈）归入上一条目；默认仅 Warning 及以上（Information/Debug 跳过）
- [x] 服务器日志聚合组与桥侧同构（key/level/source/count/firstTs/lastTs/sampleText；归一化规则同构移植，24hex/GUID/数字样本必合并）
- [x] fatal 通道解析 ErrorLog.log → `source=fatal` 组（fixture 用合成 AV 栈样本：AccessViolationException + il2cpp_runtime_invoke + TrackableTransform + coreclr.dll 0xc0000005；真实文件当前为空）
- [x] `logs_summary` 输出 = 桥组 + 服务器组 + fatal 组（source 区分；排序同桥侧）
- [x] 桥故障降级：unreachable / version_mismatch / endpoint_missing → ok 信封 + `bridge.available=false` + reason；服务器组与 fatal 组照常返回（logs_recent 保持严格门禁不变）
- [x] 刷新间隔可配（默认 5s，范围 5–10s）；惰性刷新语义（间隔内调用返回缓存）
- [x] 文件系统失败结构化降级、绝不抛出（沿用 log-reader 惯例）
- [x] 日志目录解析沿用环境变量优先级约定（TARKOV_RUNTIME_MCP_LOG_DIR / _SPT_DIR / cwd 回落；logs 根 = spt 目录父级或新增 LOGS_ROOT 覆盖）；fatal 路径显式 env（TARKOV_RUNTIME_MCP_ERRORLOG_PATH，或由 SPT_DIR 推导），缺失时静默降级
- [x] vitest 全绿（既有 279 + 新增；工单 03 中 logs_summary 桥故障测试期望按降级语义更新）

## Comments

### 2026-09-16 深夜 实现 + 验收（orchestrator）

- @fixer 实现（TDD）：字节游标 tail（半行不推进/文件变短重置/新文件纳入/失败静默）、行解析（续行归并）、TS 归一化同构移植、聚合器（500 上限 + overflowDropped）、惰性刷新（并发去重）、三通道合并视图。
- 桥故障降级：进程崩溃杀桥后 fatal 通道仍可用（本通道存在理由）；logs_recent 严格门禁不变。
- 评审修复轮并入：`server`/`fatal` 可用性元数据（`{available}` / `{available:false, reason}`，4 reason）消除静默降级不可见；工具描述与 README 同步（评审记录见工单 06）。
- 全波套件：MCP **395/395**（评审修复后 orchestrator 独立复跑）+ typecheck + build；live 验收（含 fatal 文件级验证）见工单 05。
