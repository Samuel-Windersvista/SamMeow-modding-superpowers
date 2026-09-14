# Live raid 录制 fixtures

本目录存放**真实 raid** 的桥响应录制（JSONL），由 `TARKOV_RUNTIME_MCP_BRIDGE_RECORD`
在 live 验收阶段产出，供回放回归断言使用。

## 现有 fixtures

- `raid-sandbox-2026-09-14.jsonl` —— 2026-09-14 live 验收录制（Sandbox 真实 raid，修复版桥，
  含稳定 `raidId` 与 role 优先分类后的 bot 数据）：
  `getInfo ×5 / getRaidStatus ×1 / getRaidPlayer ×2 / getRaidBots ×2`。
  **profileId 已匿名化**（替换为 `000000000000000000000000`），其余字段为原始录制值。
  使用方：`tests/bridge/live-raid-replay.test.ts`。

## 约定

- 命名建议：`<map>-<yyyymmdd>-<short-sha>.jsonl`（如 `woods-20260914-ff0bf32.jsonl`）。
- 回放：`loadReplayConnection(path)`（`src/bridge/replay.ts`）从文件构造
  `BridgeConnection`，工具层输出应与录制逐字段一致。
- 行结构见 `tools/tarkov-runtime-mcp/README.md`「录制/回放」一节。
