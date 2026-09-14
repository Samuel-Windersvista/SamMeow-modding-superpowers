# 07: 录制/回放——JSONL + fixtures 回归

**What to build:** MCP 侧录制（bridge 原始响应 + 时间戳 → JSONL；默认关，env/配置开启；live 验收时开启）与回放器（录制文件驱动 fake `BridgeConnection`，供回归断言）；至少一段真实 raid 录制落 fixtures 并在测试中使用。

**Blocked by:** 04, 05

**Status:** ready-for-agent

- [x] 录制默认关；开启后 JSONL 含时间戳 + 原始响应；live 录制成功
- [x] 回放器以录制文件驱动 fake bridge，回归断言确定性
- [x] 至少一段真实 raid 录制进 fixtures 并用于测试
- [x] 录制开关/磁盘行为在 README 或工具描述中有说明

## Comments

### 2026-09-14 实施 + live 录制 + 回归（T07 完成）

- **MCP 侧（fix-5）**：`RecordingBridgeConnection`（JSONL 追加、逐行 flush、默认关、env `TARKOV_RUNTIME_MCP_BRIDGE_RECORD`）；`replay.ts` 回放器（method+detail 分桶、顺序消费、耗尽复用最后一条）
- **live 录制**：真实 raid（Sandbox，修复版桥）录制 10 条（getInfo×5 / getRaidStatus×1 / getRaidPlayer×2 / getRaidBots×2）→ profileId 匿名化（`000000000000000000000000`）→ 入仓 `tests/fixtures/live-raid/raid-sandbox-2026-09-14.jsonl`
- **回归**：`tests/bridge/live-raid-replay.test.ts`（4 条：status 逐字段 / player 顺序消费 / bots 摘要+明细 / 两次独立回放逐字节一致）——**214/214 全绿**
- README（MCP）记录录制开关与磁盘行为；fixtures README 记录命名与匿名化约定
