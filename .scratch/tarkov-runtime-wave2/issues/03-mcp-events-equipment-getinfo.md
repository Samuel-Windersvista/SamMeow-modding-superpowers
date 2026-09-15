# 03: MCP 侧——`raid_events` 工具 + 装备 schema + `getInfo` 每次拉取

**What to build:** `BridgeConnection` 增加 `getRaidEvents(since?, limit?)`；新增 `raid_events` 工具（增量拉取，输出确定性）；`raid_player` schema 扩展 `weapon`/`equipment`；`getInfo` 去掉进程内缓存改为每次调用拉取（协议校验即时生效）。fake 驱动测试覆盖全部新路径。

**Blocked by:** 01, 02（契约以其为准）

**Status:** ready-for-agent

- [x] `raid_events`：`{since?, limit?}` 入参；输出 `{seq, dropped, events}`；NOT_IN_RAID/BRIDGE_UNREACHABLE 路径与既有工具一致
- [x] `raid_player` 新字段入 schema 与 fake；确定性断言
- [x] `getInfo` 缓存移除（含测试更新：连调两次 → 两次拉取）
- [x] `wait_for` 对 `raid_events` 可用（如 `seq > N`）有测试
- [x] `npm test` 全绿（237/237，基线 214 + 新增）+ typecheck + build

## Comments

### 2026-09-15 实施 + 评审修复

**落地**：`BridgeConnection.getRaidEvents(since?, limit?)`（含录制/回放方法名单源化 `BRIDGE_METHODS`）；`raid_events` 工具（**非 raid 返回缓冲，不返回 NOT_IN_RAID**）；`raid_player` schema 扩展 `weapon`/`equipment`；`getInfo` 去缓存。

**评审修复（Spec/Standards 轴）**：extraction 载荷解析宽容化（`normalizeString`：字段缺失/非字符串 → `""`，修 spec.md:41 违反项）；`raid_player` 工具描述补 weapon/equipment；`since` 语义文档精确化（= 已消费的最后一条事件的 seq）。

**验证**：`npm test` **237/237** + typecheck + build；live：`raid_events` 端到端正常（Sandbox 局 686+ 条，`since` 增量实证）。
