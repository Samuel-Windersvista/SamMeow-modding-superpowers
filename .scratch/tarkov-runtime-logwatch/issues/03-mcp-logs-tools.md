# 03: MCP 工具 logs_recent / logs_summary

**What to build:** tarkov-runtime-mcp 新增 `logs_recent` / `logs_summary` 工具，经既有 BridgeConnection 拉取桥端点（参数透传、输出字段序稳定）；信封语义与 raid_* 一致；桥为旧版（无 /logs/* 端点）时返回结构化「端点缺失」错误并提示更新桥 DLL，不误报版本门禁失败。

**Blocked by:** 01, 02

**Status:** ready-for-human

- [x] 两工具按既有工具惯例注册（名称/schema/描述/错误码）
- [x] `logs_recent` 透传 since/level/limit，输出 `{seq, dropped, entries}`（fake-bridge vitest）
- [x] `logs_summary` 透传 since，输出 `{groups, overflowDropped}`（fake-bridge vitest）
- [x] 桥不可达 → BRIDGE_UNREACHABLE；协议不匹配 → VERSION_MISMATCH（logs_summary 的桥故障降级语义于工单 04 调整）
- [x] 旧桥 404 → 结构化端点缺失错误（含更新提示）
- [x] 无效输入 → INVALID_INPUT
- [x] 不新增 MCP-桥接缝（复用 BridgeConnection）；vitest 全绿

## Comments

### 2026-09-16 深夜 实现 + 验收（orchestrator）

- @fixer 实现（TDD）：两工具 + BridgeConnection 接缝扩展（HTTP/录制/回放/fake 同步，`BRIDGE_METHODS` +2）；旧桥 404 → `LOGS_ENDPOINT_UNAVAILABLE`（三态 NotFoundPolicy，既有端点行为不变）。
- 评审修复轮并入：`level` 严格值域校验（六级别名，大小写不敏感，未知 → `INVALID_INPUT` 且不调用桥，消除「静默不过滤」footgun）。
- 注意：`logs_summary` 的桥故障语义在工单 04 调整为降级（ok 信封 + `bridge.available=false`）；`logs_recent` 保持严格门禁。
- 全波套件：MCP **395/395**（评审修复后 orchestrator 独立复跑）+ typecheck + build；live 验收见工单 05。
