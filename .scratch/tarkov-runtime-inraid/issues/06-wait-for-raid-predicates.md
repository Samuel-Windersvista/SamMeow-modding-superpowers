# 06: wait_for 扩展——raid 谓词

**What to build:** `tarkov_wait_for` 谓词空间扩展至 raid 状态（如 `raid.bots.alive_count > 5`、`raid.player.alive == true`），复用现有谓词引擎与超时结构化失败（WAIT_TIMEOUT 含谓词/最后观测值/耗时）；bridge 缺席或不在 raid 时给出明确错误码，而非静默超时。

**Blocked by:** 04, 05

**Status:** ready-for-agent

- [x] raid 谓词满足路径 + 超时路径可用（fake bridge 测试）
- [x] 超时返回结构化 `WAIT_TIMEOUT`；bridge 缺席返回 `CLIENT_BRIDGE_NOT_INSTALLED`（不误报超时）
- [x] 与既有局外谓词共存无回归（基线 + 新增全绿）
- [x] live 验收：raid 内等待条件（如 bot 出现）成功一次

## Comments

### 2026-09-14 实施 + live 核验（T06 完成）

- **MCP 侧（fix-5）**：首轮观察为 `BRIDGE_UNREACHABLE` / `CLIENT_BRIDGE_NOT_INSTALLED` → 立即返回该错误（不误报超时）；`NOT_IN_RAID` 不短路（「等进 raid」继续轮询）；谓词引擎复用（无新语法）
- **live 实证**：`wait_for` 谓词 `pose equals Duck` 在 **11.2s / 23 次轮询**后满足（下蹲实时同步，真实 raid）
- 干跑实证：bridge 缺席 → 立即 `BRIDGE_UNREACHABLE`；首轮 `NOT_IN_RAID` → 继续轮询至 `WAIT_TIMEOUT`（结构化含最后观测值）
- 测试：新增 5 条 + 既有不回归（214/214）
- 注：验收原文「bridge 缺席返回 CLIENT_BRIDGE_NOT_INSTALLED」——T03 拆分后实际为 `BRIDGE_UNREACHABLE`（语义等价；`CLIENT_BRIDGE_NOT_INSTALLED` 保留给未注册 raid.* 兜底）
