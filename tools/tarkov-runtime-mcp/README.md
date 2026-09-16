# tarkov-runtime-mcp

SPT 5.x 运行时状态 MCP server。Phase 1 提供 server 握手/版本门禁与局外快照；
Phase 2 经本地 BepInEx Client Bridge 提供局内（in-raid）状态与事件时间线（受伤/死亡/撤离）；
logwatch 波次提供错误/告警可观测面（客户端日志告警流 + 服务器日志 tail + 进程级崩溃 fatal 通道）。

## 工具面

| 工具 | 说明 |
|---|---|
| `tarkov_server_status` | 握手：连接信息 + server 版本 + BEM tag 门禁 |
| `tarkov_instances` | 候选端口探测出的单实例信息 |
| `tarkov_snapshot` | 局外状态确定性快照（profile / traders / quests / hideout / inventory） |
| `tarkov_wait_for` | 对任意工具结果轮询求值谓词，超时返回结构化 `WAIT_TIMEOUT` |
| `raid_status` | raid 元数据 + 桥自报（经 BridgeConnection） |
| `raid_player` | 玩家局内全字段（位置/朝向/姿态/血量/武器/装备/新鲜度） |
| `raid_bots` | bot 摘要 / `detail=true` 明细 |
| `raid_events` | raid 事件时间线（damage/death/extraction；`since`/`limit` 增量拉取，非 raid 也返回缓冲） |
| `logs_recent` | 桥进程内日志告警增量拉取（`since`/`level`/`limit`；与 raid 状态无关） |
| `logs_summary` | 统一日志聚合：桥组 + 服务器日志 tail + fatal 通道（桥故障时降级返回） |

`tarkov_wait_for` 的谓词可直接作用于 raid 工具（如 `raid_bots` 的 `alive > 5`）。
若首轮观察即遇 `BRIDGE_UNREACHABLE` / `CLIENT_BRIDGE_NOT_INSTALLED`，立即返回该错误
信封，不把「桥没装」误报成 `WAIT_TIMEOUT`；`NOT_IN_RAID` **不**短路（等进 raid 是合法用法）。

`raid_events` 返回 `{inRaid, seq, dropped, events}`：`seq` 字段是桥进程内最新序号（仅用于判断是否有新事件）；
`since` = 已消费的最后一条事件的 `seq`（只回 `seq > since` 的新事件）；`limit` 截断时用最后一条已返回事件的 `seq` 续拉；`since` 过旧时 `dropped>0`。
**非 raid 不返回 `NOT_IN_RAID`**——桥侧缓冲跨 raid 保留，赛后时间线（含撤离事件）仍可读；仅桥不可达 / 协议不符为错误。
事件语义：击杀 = `death` 且 `killer != null`；`damage` 载荷含 `part` / `amount` / `sourceType`；`wait_for` 可对其求值（如 `seq > N`）。

### 日志告警（logwatch）

日志与 raid 状态**无关**（非 raid 时照常可用），故两个 logs 工具都**不返回 `NOT_IN_RAID`**。
采集阈值在桥侧：`LogWatchMinLevel` 默认 `Warning`，即 Info/Debug 不入缓冲（未被采集的级别不可能被查询出来）。

`logs_recent` 返回 `{seq, dropped, entries:[{seq, ts, level, source, text}]}`（桥进程内原始条目环）：

| 入参 | 语义 |
|---|---|
| `since` | 已消费的最后一条条目的 `seq`（**独占**：只回 `seq > since`）；缺省从最旧开始；截断时用本次最后一条的 `seq` 续拉 |
| `level` | 查询侧最小级别，**必须**为 `fatal` / `error` / `warning` / `message` / `info` / `debug` 之一（大小写不敏感，归一化为小写后透传）；缺省即不过滤；未知取值（如 `Information`）返回 `INVALID_INPUT`（消息列出合法值），**不静默不过滤** |
| `limit` | 单次最多返回条数；桥侧缺省 100、上限 1000（从 `since` 之后最旧一侧截断） |

`seq` 字段是桥进程内当前最新序号（仅用于判断是否有新条目）；`dropped > 0` 表示 `since` 过旧、已被环形缓冲淘汰
（桥侧 `LogWatchRingSize`，默认 1000）。错误码：`level` 未知取值 / 其他入参非法 `INVALID_INPUT`；
桥不可达 `BRIDGE_UNREACHABLE`；协议不符 `BRIDGE_VERSION_MISMATCH`；
**旧版桥无 `/logs/*` 端点**（404）`LOGS_ENDPOINT_UNAVAILABLE`（消息提示更新桥 DLL，不误报版本门禁失败）。

`logs_summary` 返回 `{groups, overflowDropped, bridge, server, fatal}`，为**三通道合并视图**：

| source | 通道 | 说明 |
|---|---|---|
| 桥侧自报（如 `Unity` / `Assembly-CSharp`） | 桥组 | 客户端日志告警，归一化去重由桥侧完成（`GET /logs/summary`） |
| `server:<文件名>` | 服务器组 | MCP 侧增量 tail `<logs root>/spt` / `kestrel` / `requests`，默认仅 **Warning 及以上** |
| `fatal` | fatal 组 | MCP 侧监视 `BepInEx/ErrorLog.log` 的进程级崩溃栈；整文件视为错误输出，不过滤级别 |

- 组形状 `{key, level, source, count, firstTs, lastTs, sampleText}`：`key` 为归一化文本（GUID → `<guid>`、
  恰好 24 位 hex → `<id>`、`0x…` → `<hex>`、数字 → `<n>`），同一错误的数千个实例聚合为 1 组 + `count`
  （`count` 为全部观测条数，不受条目环容量影响）；`level` 取组内最高严重度；`source` 取首个来源。
- 组序固定：`count` 降序 → `lastTs` 降序 → `key` 序升序（桥组与 MCP 组合并后统一排序）。
- `overflowDropped` = 桥侧 + MCP 侧合计（各组聚合器上限 500，超限按「最久未更新」淘汰，只增不减）。
- `since` 为**时间游标**：只回 `lastTs` 晚于它的组；接受端点自己输出的 ISO 8601（`lastTs` 原样回填即可往返）
  或整数 UTC Ticks；缺省 / 非法即不过滤。桥侧原样透传（桥自行过滤），MCP 组按同语义本地过滤。
- **惰性刷新**：调用时距上次刷新 ≥ 间隔（默认 5000ms）才读增量，间隔内返回缓存；不常驻后台定时器。
- **三通道可用性（消除静默降级）**：`bridge` / `server` / `fatal` 各自报可用性，`available=false` 时附 `reason`，
  `available=true` 时不带 `reason`：
  - `bridge`：可用时 `{available:true, pluginVersion, protocolVersion, samplingIntervalMs}`；不可用
    `{available:false, reason}`，`reason` ∈ `unreachable` / `version_mismatch` / `endpoint_missing`；
  - `server` / `fatal`（MCP 侧通道）：`{available:true}` 或 `{available:false, reason}`，`reason` ∈
    `logs_root_missing`（日志根不存在 / 不是目录）/ `no_log_dirs`（根存在但 `spt`/`kestrel`/`requests`
    都不存在）/ `path_unresolved`（路径未配置 / 无法推导）/ `file_missing`（fatal 文件不存在）。
  **不可用通道的组恒为空**——输出里「`available=false` + 空组」保持一致，不会把陈旧组误读成该通道在产出数据；
  `overflowDropped` 为历史累计，不受可用性影响。
- **桥故障降级**：桥不可用时**仍返回 ok 信封**与服务器组 / fatal 组——**进程级崩溃会杀死桥进程，
  fatal 通道必须在该场景下仍可用**（本通道存在的根本理由）；`endpoint_missing` 的摘要提示更新桥 DLL。
  `logs_recent` 保持严格门禁，不受此降级影响。

> 状态：logwatch 各单元 / 集成测试已全绿，**实机 live 验收（工单 05）待进行**。

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
| `TARKOV_RUNTIME_MCP_LOGWATCH_INTERVAL_MS` | `5000` | logwatch 惰性刷新间隔（ms）；钳制 5000–10000，非法回默认 |
| `TARKOV_RUNTIME_MCP_LOGS_ROOT` | spt 日志目录父级 | 服务器日志根（其下 `spt/` `kestrel/` `requests/`） |
| `TARKOV_RUNTIME_MCP_ERRORLOG_PATH` | 由 `SPT_DIR` 推导 | fatal 通道 `ErrorLog.log` 路径；不可得时通道静默为空 |

本波（logwatch）新增 `TARKOV_RUNTIME_MCP_LOGWATCH_INTERVAL_MS` / `_LOGS_ROOT` / `_ERRORLOG_PATH`；
此前的 `raid_events` + `raid_player` 波次未新增环境变量。

### 日志路径解析优先级（对照 `src/config.ts`）

- **服务器日志根**（其下三个子目录）：`TARKOV_RUNTIME_MCP_LOGS_ROOT` → `TARKOV_RUNTIME_MCP_LOG_DIR`（取其父级）
  → `TARKOV_RUNTIME_MCP_SPT_DIR`（`<SPT_DIR>/user/logs`）→ `<cwd>/user/logs`。
- **fatal 通道路径**：`TARKOV_RUNTIME_MCP_ERRORLOG_PATH` → `<SPT_DIR>/../BepInEx/ErrorLog.log`（`..` 由 `path.join` 词法折叠，
  即 SPT_Runtime 同级的 `BepInEx/ErrorLog.log`）→ 未定义（通道静默为空）。
- 目录 / 文件缺失、读取失败一律**静默降级为空，绝不抛出**（沿用 `log-reader` 惯例）；
  文件按**字节偏移**游标增量读取，只消费到最后一个完整换行（半行留待下次，防截断重复），
  文件变短（轮转 / 截断）时偏移重置 0，新文件自动纳入。

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

- `method` ∈ `getInfo | getRaidStatus | getRaidPlayer | getRaidBots | getRaidEvents | getLogsRecent | getLogsSummary`；
- `args`：`getRaidBots` 为 `{"detail":true|false}`，`getRaidEvents` 为 `{"since":<n>|null,"limit":<n>|null}`，
  `getLogsRecent` 为 `{"since":<n>|null,"level":<str>|null,"limit":<n>|null}`，`getLogsSummary` 为 `{"since":<str|n>|null}`，其余为 `null`；
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
