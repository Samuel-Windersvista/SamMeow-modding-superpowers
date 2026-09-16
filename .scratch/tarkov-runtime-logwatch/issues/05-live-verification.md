# 05: Live 验收（借雷达复测战局）

**What to build:** 新桥 DLL 构建并部署到 MO2 覆盖层（游戏关闭时执行）后，借雷达复测的实机战局完成 logwatch live 验收：进 raid 制造一条已知告警（人为 Log.LogWarning 或 F12 路径），断言 `logs_summary` 在 ≤10s 内出现且 count 正确；`logs_recent` 的 since 增量连续拉取不丢不重；fatal 通道以文件级验证。

**Blocked by:** 01, 02, 03, 04

**Status:** ready-for-human

- [x] 桥 Release 构建 + 部署至 MO2 覆盖层（`工具-tarkov-runtime-client-bridge-0.1.0` 的 `BepInEx/plugins/`，游戏必须关闭）——2026-09-16 深夜预置（游戏未运行；SHA256 一致；旧版已备份）
- [x] 启动后 `/bridge/info` 可见新端点；`/logs/recent` 实机可达——实机确认 endpoints 含 `/logs/recent`、`/logs/summary`；启动日志 `loaded (log watch: min level Warning, ring 1000)`
- [x] 进 raid 制造已知告警 → `logs_summary` ≤10s 出现且 count 正确——自然告警实测：字体刷屏组 **count=5610 == 文件 5610 行**；新鲜度 5×3s 轮询 age=1–4s
- [x] `logs_recent` since 增量：连续两次拉取不丢不重——`5625..5649` → `5650..5674` 连续、0 重复
- [x] fatal 通道文件级验证——fixture 文件经真实 tail 路径解析（+2 组）；真实 `ErrorLog.log` 0 字节（本次无崩溃），路径 `available=true`
- [x] 验收证据（日志行 / 响应片段）记录至本工单 Comments
- [x] 同场战局顺带完成雷达复测（见下方雷达复测记录；收尾落档由 fixer 执行中）

## Comments

### 2026-09-16 深夜 预置 + 真数据冒烟（orchestrator）

**部署预置**：游戏未运行（`Get-Process EscapeFromTarkov` 空）时部署评审修复版 DLL：
- 源：`tools/tarkov-runtime-bridge/bin/Release/TarkovRuntimeBridge.dll`（64,512 bytes，SHA256 `D6C4C2B3…DF3536`）
- 目标：MO2 覆盖层 `工具-tarkov-runtime-client-bridge-0.1.0/BepInEx/plugins/`（哈希核验一致）
- 旧版备份：`D:\Temp\opencode\TarkovRuntimeBridge-pre-logwatch-20260916.dll`（52,736 bytes）

**真数据冒烟**（脚本 `D:\Temp\opencode\logwatch-smoke.mjs`，输出 `logwatch-smoke-out.txt`；只读，桥未运行）：
- `logs_summary` → `ok=true`；`bridge={available:false,reason:"unreachable"}`（降级路径生效）；`server={available:true}`、`fatal={available:true}`
- 真实日志（SPT 20260913/0914）：28 组；`Fixed item: <24hex>s …` **141 条合并为 1 组**（右边界放宽在真实数据生效）；本地时间→UTC 转换正确（本地 13:34 +07:00 → `06:34Z`）
- `logs_recent` → `BRIDGE_UNREACHABLE`（严格门禁保持）
- fatal 通道：fixture AV 栈真实文件 → +2 组（错误块合并 + Fatal 块）；真实 `ErrorLog.log` 当前为空（available=true、0 组）
- 未覆盖（待实机）：`/bridge/info` 新端点、`/logs/recent` 实机增量、告警 ≤10s 断言、真崩溃样本
- 执行方式备注：`tarkov` MCP 未在 OpenCode 会话暴露（`.opencode` 插件仅注册 `mo2`/`spt`；根 `.mcp.json` 的 `tarkov` 条目为 bgs 时代路径，未被 OpenCode 加载）——本票验证以驱动脚本（`D:\Temp\opencode\logwatch-smoke.mjs`）进行；注册修复（按插件既有 `config.mcp.* ??=` 模式 + 路径 env）列为 follow-up，待 Overseer 决定。

### 2026-09-16 上午 实机验收：logwatch 全部断言通过（orchestrator）

游戏经 MO2 启动（桥加载新版：`Tarkov Runtime Bridge 0.1.0 loaded (log watch: min level Warning, ring 1000).`）。

1. **端点上线**：`/bridge/info` → `endpoints:[…,"/logs/recent","/logs/summary"]`、`sections:[…,"logs"]`。
2. **`/logs/recent` 实机**：返回 `{seq:5681, dropped:4681, entries:[{seq,ts,level,source,text}]}`；增量连续两批 `5625..5649` → `5650..5674`（since 独占、批间无缝、跨批 0 重复）。
3. **告警可见性 ≤10s**：5 次轮询（3s 间隔）实测最新组 `age=1–4s`（`Can't find skill to upgrade: RecoilControl` / `Quest. Can't find condition…` / `Calling Animator.GotoState on Synchronize layer`）。
4. **计数正确性（刷屏聚合）**：`Unable to add characters to font asset [DON'T USE! SimSun-Chinese SDF] …` 组 **count=5610 == `LogOutput.log` 实际行数 5610**（逐字吻合；对照工单动机中的 6,477 条 ComboBox 刷屏场景）。
5. **level 查询过滤（评审修复实机验证）**：`?level=error&limit=5` 只回 4 条 error；`?level=warning&limit=3` 只回 3 条 warning（先过滤后截窗生效）。
6. **MCP 全链（驱动脚本，桥在线）**：`bridge={available:true,pluginVersion:0.1.0,protocolVersion:1}`；三通道合并视图 33 组（桥组 + `server:spt20260913.log` 组按统一排序交错）。
7. **fatal 通道**：真实 `ErrorLog.log` 0 字节（无崩溃）；fixture 样本经真实 tail 路径解析 +2 组；真实路径 `available=true`。

### 2026-09-16 上午 雷达复测（同场 Sandbox 战局；桥探测驱动，无需人工回报）

- 桥探测：`inRaid:true, map:Sandbox, raidId 6aa408bf3c427c14241039f5@2026-09-16T02:48:35.8656996Z`；玩家域正常（alive / total 440 / 13 装备槽）。
- 价格表：`Local flea price table loaded: 4719 entries.` ✓
- **价格源隔离修复（76D8C6E3）复验通过**：`Loot scan: owners=1067, tracked=69, maxPrice=139000, threshold=30000`（修复前 tracked=0）；商人价熔断 `Trader pricing disabled after failure` Warning 出现但**未毒化整价** ✓
- **F12 阈值即时性**：阈值 30000 → 55634/54718/…/52887 连续改动，`tracked` 69 → 17，每次改动均触发新 `Loot scan` 行 ✓
- 稳定性：无闪退（游戏存活 pid 37592）；F12 路径无异常（唯一 `ConfigurationManager` 匹配为 `Il2CppInterop` Info 注册行）✓
- logwatch 联动：雷达两条 Warning 实时进入 `/logs/summary`（`Accurate Circular Radar - SPT5` 组）✓
- 收尾（README 第 11 项 + 运行期验证结果 + 归档刷新 + PROVENANCE + devlog）：**已完成**（fix-3；归档 DLL 哈希与 MO2 部署副本一致，orchestrator 独立复核；MANIFEST 行已同步 11 项）。

**残余**：真崩溃样本无法主动制造（不阻塞）；`tarkov` MCP 注册修复为 follow-up。
