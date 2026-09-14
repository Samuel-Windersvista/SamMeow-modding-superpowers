# 03: 采样与错误模型加固——框架正式化

**What to build:** 把首刀最小实现加固为正式框架。采样循环参数化（默认 1s，可配 0.25–5s）且响应携带 `sample_age_ms`；新增 `/bridge/info` 自报（插件版本/协议版本/能力清单/采样配置），MCP 握手校验协议版本；错误模型落地——`BRIDGE_UNREACHABLE`（未运行）、`NOT_IN_RAID`（未进 raid）、`BRIDGE_VERSION_MISMATCH`、`UNSUPPORTED_SECTION`；端口冲突可诊断；MCP 侧 fake-bridge 测试覆盖全部错误路径。

**Blocked by:** 02

**Status:** ready-for-agent

- [x] 采样间隔可配且生效（age 观测随配置变化）
- [x] `/bridge/info` 返回协议版本与能力清单；协议不匹配返回 `BRIDGE_VERSION_MISMATCH`（fake 测试）
- [x] 未安装 / 未运行 / 不在 raid 三类错误码可区分（fake 测试 + live 各一条）
- [x] 端口冲突场景有可诊断输出（日志或错误码），不崩溃
- [x] 既有 150 测试基线不回归，新增测试全绿

## Comments

### 2026-09-14 双 lane 实施 + live 核验（T03 完成）

- **桥侧（fix-3）**：`/bridge/info`（pluginVersion/protocolVersion=1/capabilities/sampling/network）；启动日志含协议版本、采样间隔与端口；端口冲突记 error 不抛
- **MCP 侧（fix-4）**：`getInfo()`（成功后缓存）+ `EXPECTED_BRIDGE_PROTOCOL_VERSION=1`；错误码拆分 `BRIDGE_UNREACHABLE` / `BRIDGE_VERSION_MISMATCH` / `NOT_IN_RAID`（`CLIENT_BRIDGE_NOT_INSTALLED` 保留为未注册 raid.* 兜底）
- **live 实证**：`/bridge/info` 返回 `sampling.intervalMs=250`（配置 1000→250 生效）；BepInEx 日志 `(protocol 1) … sampling interval 250ms`；菜单态 `raid_status → NOT_IN_RAID`；游戏退出后 → `BRIDGE_UNREACHABLE`；`BRIDGE_VERSION_MISMATCH` 由 fake 测试覆盖
- **已知行为**：MCP 进程内 `getInfo` 缓存——同进程内桥升级不会重新校验协议（跨会话/进程重启自然刷新；记录备查）
- 测试：214/214（含错误路径新增）
