# tarkov-runtime-mcp

SPT 5.x 运行时状态 MCP server。Phase 1 提供 server 握手/版本门禁与局外快照；
Phase 2 经本地 BepInEx Client Bridge 提供局内（in-raid）状态。

## 工具面

| 工具 | 说明 |
|---|---|
| `tarkov_server_status` | 握手：连接信息 + server 版本 + BEM tag 门禁 |
| `tarkov_instances` | 候选端口探测出的单实例信息 |
| `tarkov_snapshot` | 局外状态确定性快照（profile / traders / quests / hideout / inventory） |
| `tarkov_wait_for` | 对任意工具结果轮询求值谓词，超时返回结构化 `WAIT_TIMEOUT` |
| `raid_status` | raid 元数据 + 桥自报（经 BridgeConnection） |
| `raid_player` | 玩家局内全字段（位置/朝向/姿态/血量/新鲜度） |
| `raid_bots` | bot 摘要 / `detail=true` 明细 |

`tarkov_wait_for` 的谓词可直接作用于 raid 工具（如 `raid_bots` 的 `alive > 5`）。
若首轮观察即遇 `BRIDGE_UNREACHABLE` / `CLIENT_BRIDGE_NOT_INSTALLED`，立即返回该错误
信封，不把「桥没装」误报成 `WAIT_TIMEOUT`；`NOT_IN_RAID` **不**短路（等进 raid 是合法用法）。

## 配置（环境变量）

| 变量 | 默认 | 说明 |
|---|---|---|
| `TARKOV_RUNTIME_MCP_HOST` | `127.0.0.1` | SPT server 地址 |
| `TARKOV_RUNTIME_MCP_PORTS` | `6969` | 候选端口（逗号分隔） |
| `TARKOV_RUNTIME_MCP_ANCHOR_VERSION` | `5.0.0-BEM-20260914` | 锚定 BEM tag |
| `TARKOV_RUNTIME_MCP_USERNAME` / `_PASSWORD` | 无 | session 受限路由 |
| `TARKOV_RUNTIME_MCP_BRIDGE_HOST` | `127.0.0.1` | Client Bridge 地址 |
| `TARKOV_RUNTIME_MCP_BRIDGE_PORT` | `49777` | Client Bridge 端口 |
| `TARKOV_RUNTIME_MCP_BRIDGE_RECORD` | 无 | 录制输出 JSONL 路径；**缺省不录制** |

## 录制/回放（T07）

### 开关与磁盘行为

- **默认关**：未设置 `TARKOV_RUNTIME_MCP_BRIDGE_RECORD` 时，`createRuntime` 不包装
  录制器，不产生任何磁盘写入。
- 设置该变量为文件路径后，`createRuntime` 用 `RecordingBridgeConnection`
  （`src/bridge/recording.ts`）装饰默认 HTTP 连接；测试注入的连接同样会被包装。
- 每次桥调用追加一行 JSONL（`appendFileSync`，逐行 flush），**不覆盖**已有文件。
- 父目录不存在时自动创建；写盘失败只向 stderr 告警，不中断工具调用。

### 行结构

```json
{"ts":"2026-09-14T12:00:00.000Z","method":"getRaidStatus","args":null,"ok":true,"result":{"inRaid":true,"map":"Woods","status":"running","remainingSeconds":1234,"raidId":"raid-abc","sampleAgeMs":100}}
```

- `method` ∈ `getInfo | getRaidStatus | getRaidPlayer | getRaidBots`；
- `args`：`getRaidBots` 为 `{"detail":true|false}`，其余为 `null`；
- `ok:false` 时 `result` 为 `{"error":"<信息>"}`。

### 回放

`loadReplayConnection(path)`（`src/bridge/replay.ts`）从 JSONL 构造
`BridgeConnection`，供回归断言：工具层输出与录制逐字节一致（无时间戳噪声）。
桶内按序消费，耗尽后复用最后一条（`getInfo` 被每个 raid 工具各调用一次）。
录制与调用不匹配（缺 method 条目 / 失败条目）时抛 `BridgeUnreachableError`。

真实 raid 录制落 `tests/fixtures/live-raid/`（live 阶段补齐）。

## 开发

```sh
npm test        # vitest
npm run typecheck
npm run build
```
