# Tarkov Runtime Bridge

tarkov-runtime-MCP 的客户端桥插件（C# / BepInEx 6 IL2CPP）。
读取 SPT 5.0 客户端（EFT 1.1.5）的玩家域（位置/朝向/姿态/血量/武器/装备）、raid 元数据（地图/状态/剩余时间/raidId）、
bot 域（摘要 + 明细）、事件域（受伤/死亡/撤离时间线）与日志域（进程内 Warning 及以上告警流），
经仅绑 `127.0.0.1` 的只读 HTTP 端点暴露。

## 安装（MO2 overlay）

本插件以 MO2 mod 形态交付：**mod 根目录 = 游戏根目录**，DLL 落在：

```
BepInEx/plugins/TarkovRuntimeBridge.dll
```

MO2 的 VFS（usvfs）在启动时将其投影进游戏目录。不要把 DLL 直接写进真实游戏安装目录。

## 配置

首次加载后在 `BepInEx/config/com.sammeow.tarkov-runtime-bridge.cfg` 生成：

| 段 | 键 | 默认 | 说明 |
|---|---|---|---|
| `Network` | `Port` | `49777` | HTTP 监听端口（仅 `127.0.0.1`） |
| `Sampling` | `SampleIntervalMs` | `1000` | 主线程采样间隔（毫秒，250–5000，即 0.25–5s） |
| `LogWatch` | `Enabled` | `true` | 是否注册 BepInEx 日志监听器（`/logs/recent` 的采集开关） |
| `LogWatch` | `MinLevel` | `Warning` | 日志采集最小级别；默认 Warning 即采集 Warning/Error/Fatal |
| `LogWatch` | `RingSize` | `1000` | 日志环形缓冲容量（条，16–10000；超容淘汰最旧） |

端口占用 / URL ACL 拒绝等启动失败只记 error，游戏照常运行；此时位置采样仍在内存中进行，仅 HTTP 端点不可用。

补丁常开：`LocalGame.Stop`（撤离）与 `ActiveHealthController.ApplyDamage`（受伤）两个 Harmony 补丁无独立开关——
二者只读观测、不修改游戏逻辑、无副作用，故不提供配置项。

`[LogWatch]` 段（`Enabled` / `MinLevel` / `RingSize`）为本波（logwatch）新增；`Port` 与 `SampleIntervalMs` 维持既有语义。
键名按段内去冗余惯例（对齐 `[Network] Port`）。日志监听器回调**只写内存**（零 I/O、绝不向游戏日志调用方外抛异常、
空 catch 内绝不记录以免递归），见 `src/LogWatchListener.cs`。

## 端点契约

所有端点仅 `GET`，仅绑 `127.0.0.1`，响应字段顺序稳定（可逐字节 diff），数值用 InvariantCulture，
NaN/Infinity 退化为 `0`。

| 方法 | 路径 | 响应 |
|---|---|---|
| GET | `/bridge/info` | `200 {"pluginVersion":<str>,"protocolVersion":1,"capabilities":{"endpoints":["/bridge/info","/raid/player","/raid/status","/raid/bots","/raid/events","/logs/recent","/logs/summary"],"sections":["player","raid","bots","events","logs"]},"sampling":{"intervalMs":<int>},"network":{"host":"127.0.0.1","port":<int>}}` |
| GET | `/raid/player` | 在 raid：`200 {"inRaid":true,"position":{"x":..,"y":..,"z":..},"rotation":{"x":..,"y":..},"pose":"Stand","health":{"alive":true,"total":<float>,"parts":{"Head":..,"Chest":..,"Stomach":..,"LeftArm":..,"RightArm":..,"LeftLeg":..,"RightLeg":..}},"weapon":{"tpl":<str>,"name":<str>,"ammoInMag":<int>,"ammoInChamber":<int>}\|null,"equipment":[{"slot":<str>,"tpl":<str>,"name":<str>}],"sampleAgeMs":<int>}` |
| GET | `/raid/player` | 不在 raid：`200 {"inRaid":false}` |
| GET | `/raid/status` | 在 raid：`200 {"inRaid":true,"map":<str>,"status":<GameStatus>,"remainingSeconds":<float>,"raidId":"<profileId>@<会话起点 UTC ISO>","sampleAgeMs":<int>}` |
| GET | `/raid/status` | 不在 raid：`200 {"inRaid":false}` |
| GET | `/raid/bots` | 在 raid 摘要：`200 {"inRaid":true,"total":<int>,"alive":<int>,"byCategory":{"pmc":n,"scav":n,"boss":n,"other":n},"spawner":{"aliveAndLoading":n,"delayed":n,"allWithDelayed":n},"sampleAgeMs":<int>}` |
| GET | `/raid/bots?detail=1` | 摘要 + `"bots":[{"x":..,"y":..,"z":..,"role":<WildSpawnType>,"side":<EPlayerSide>,"alive":<bool>}],"truncated":<bool>` |
| GET | `/raid/bots` | 不在 raid：`200 {"inRaid":false}` |
| GET | `/raid/events?since=<long>&limit=<int>` | `200 {"inRaid":<bool>,"seq":<long>,"dropped":<int>,"events":[{...}]}`（**非 raid 也返回缓冲**，`inRaid` 仅为状态字段；`since`/`limit` 缺省即增量起点 0 / 100 条，上限 1000） |
| GET | `/logs/recent?level=<str>&since=<long>&limit=<int>` | `200 {"seq":<long>,"dropped":<int>,"entries":[{"seq":<long>,"ts":<UTC ISO>,"level":<str>,"source":<str>,"text":<str>}]}`（**与 raid 状态无关**；`level` 为查询侧最小级别，缺省/非法即不过滤，**级别过滤先于 `limit` 截窗**，空 `entries` = 环内确实无匹配；`since`/`limit` 缺省与上限同 `/raid/events`，截断方向同为从 `since` 之后最旧一侧起算） |
| GET | `/logs/summary?since=<str>` | `200 {"groups":[{"key":<str>,"level":<str>,"source":<str>,"count":<long>,"firstTs":<UTC ISO>,"lastTs":<UTC ISO>,"sampleText":<str>}],"overflowDropped":<int>}`（**与 raid 状态无关**；`since` 为时间游标，只回 `lastTs` 晚于它的组，接受 ISO 8601 或整数 UTC Ticks，缺省即全量；组序 count 降序 → lastTs 降序 → key 升序） |
| ANY | 未知路径 | `404 {"error":"not_found"}` |
| 非 GET | 已知路径 | `405 {"error":"method_not_allowed"}` |

字段来源（见 `issues/01-signature-verification-report.md`）：

- `position`/`rotation`/`pose` ← `MainPlayer.Position` / `.Rotation` / `.Pose`
- `health.total` ← `ActiveHealthController.GetBodyPartHealth(EBodyPart.Common).Current`；各肢体同理
- `health.alive` ← `ActiveHealthController.IsAlive`
- `map`/`status`/`remainingSeconds` ← `Singleton<AbstractGame>.Instance.LocationId` / `.Status` / `.GameTimer.EscapeTimeSeconds()`（`EFT.GameTimerExtension` 扩展方法）
- `raidId` ← `<profileId>@<会话起点 UTC ISO 8601>`。profileId 取值链：`MainPlayer.ProfileId` →
  `GameWorld.CurrentProfileId` 字符串形式 → `unknown-profile`。
  会话起点由**桥自持**：`MainPlayer` 从无到有时记录桥进程墙钟 UTC（`DateTime.UtcNow`；桥在 raid 中途启动则为首次采样时刻），
  离开 raid 时清除；未记录起点时时间戳段记 `no-start`。
  **不使用 `GameTimer.StartDateTime`**——live 实测同一 raid 内它会在非墙钟值与 null 间翻转，不可作为会话标识。
  语义：raidId = 桥观测到的 raid 会话起点，同一 raid 内稳定、跨 raid 唯一（配合 profileId）。
- `bots` ← `GameWorld.AllAlivePlayersList` 排除 `IsYourPlayer`；`spawner` ← `Singleton<IBotGame>.Instance.BotsController.BotSpawner`
- bot 分类（**role 优先**）：`role` 含 `boss`（忽略大小写）→ boss；`role` 以 `pmc` 开头（忽略大小写）→ pmc；
  否则 `side == Savage` → scav；其余 → other。SPT 的 `pmcUSEC`/`pmcBEAR` 阵营为 `Savage`，
  仅凭 side 会把 PMC 误判为 scav（live 实测）。
- `weapon` ← `Player.HandsController` → `IFirearmHandsController.Item`（`StringTemplateId` / `Name`）+
  `GetCurrentMagazineCount()` / `ChamberAmmoCount`；非持枪（近战 / 投掷 / 空手）为 `null`
- `equipment` ← `Player.Profile.Inventory.Equipment.Slots`（仅已占用槽；`slot` 为 `Slot.Name`）
- `events`（`/raid/events`）← 三路采集，事件形状 `{seq, ts, type, raidId, payload}`（字段序稳定）：
  - `damage` ← Harmony prefix/postfix patch `ActiveHealthController.ApplyDamage(EBodyPart, float, DamageInfo)`；
    payload `{victimProfileId, victimIsLocal, part, amount, sourceType}`。**live 结论（2026-09-15）**：
    `IHealthController.ApplyDamageEvent` 委托订阅在 Il2CppInterop 下不可用（`DamageInfo` 为非 blittable struct，
    封送被拒且连带跳过死亡订阅），故改走补丁；生态先例：Deminvincibility / Miyako-Carry-Service 等均补丁该方法
  - `death` ← 订阅 `IHealthController.DiedEvent`；payload `{victimProfileId, victimIsLocal, damageType, killer}`，
    `killer` 由受伤前缀维护的 `victimProfileId → lastDamager` 映射（10s 时间窗）在死亡时消费，
    不可得为 `null`（不猜）。**击杀 = `death` 且 `killer != null`**（不重复出事件）
  - `extraction` ← Harmony postfix patch `LocalGame.Stop(string, ExitStatus, string, float)`；
    payload `{exitName, status}`（撤离 / 阵亡 / MIA 统一落此；阵亡时 `exitName` 为空、`status=Killed`）
- 事件增量语义：`seq` 桥进程内单调；`since` 应取**已消费的最后一条事件的 `seq`**（响应里的 `seq` 字段是最新序号，
  仅用于判断是否有新事件；`limit` 截断时用本次返回的最后一条事件的 `seq` 续拉），只回 `seq > since` 的事件；
  环形缓冲容量 1000、跨 raid 保留（事件带 `raidId`）；
  `since` 早于缓冲最旧事件 → 从最旧返回且 `dropped>0`；`limit` 缺省 100、上限 1000
- `entries`（`/logs/recent`）← BepInEx `ILogListener.LogEvent`（`Plugin.Load` 时 `Logger.Listeners.Add`，
  `Unload` 时 Remove 并置空引用）。条目形状 `{seq, ts, level, source, text}`（字段序稳定）；
  `level` 为归一化小写名（`fatal`/`error`/`warning`/`message`/`info`/`debug`），
  `source` 取 `ILogSource.SourceName`（如 `Unity` / `Assembly-CSharp`）。为什么用 `ILogListener` 而不是
  `Application.logMessageReceived`：纯托管接口、零 IL2CPP 封送风险，且 Unity 日志已由 BepInEx 转发
  （现有日志中的 `[Message: Unity]` 行即证）。**级别比较不走枚举数值**——BepInEx 6 的 `LogLevel` 是
  `[Flags]`（Fatal=1 < Error=2 < Warning=4 < Message=8 < Info=16 < Debug=32），严重度排序由
  `LogWatchLevel.Rank` 显式给出
- 日志增量语义：与 `/raid/events` 同款（`seq` 单调、`since` 独占、`dropped` 计淘汰、`limit` 缺省 100 /
  上限 1000 且从 `since` 之后最旧一侧截断），**但级别过滤先于 limit 截窗**：先取 `seq > since` 的全部条目做
  级别过滤，再取前 `limit` 条——否则窗口内无匹配时会返回空且游标无法推进（starvation）；
  因此「空 `entries`」严格等价于「环内确实无匹配条目」。`level` 为**查询侧**最小级别过滤，
  受采集阈值约束（采集侧未写入的级别不可能被查询出来）
- `groups`（`/logs/summary`）← 与 `/logs/recent` **并列**的第二条写路径（`LogSummaryStore`）：
  每条被采集日志同时进「原始条目环」与「归一化聚合」。组形状 `{key, level, source, count, firstTs, lastTs, sampleText}`：
  - `key` = 归一化文本（`LogSummaryNormalizer`）。**保守策略**：只替换形状明确的 token——
    GUID → `<guid>`、恰好 24 位 hex（EFT tpl / MongoId 形态）→ `<id>`、`0x…` 十六进制字面量（文件 / IL 偏移）→ `<hex>`、
    十进制数字 → `<n>`；24-hex 的左边界为非词字符、**右边界为「其后不得再有 hex 字符」**（真实告警样本会把 id 紧贴
    字面量 `s`，词边界会漏配）；25+ 位 hex 串与内嵌于更长标识符的片段不替换；不做大小写折叠、不做模糊匹配。
    宁可少合并，不可把不同错误误合并（误合并会把两个故障藏进同一组）。规则与 TS 侧
    `tools/tarkov-runtime-mcp/src/logs/log-normalizer.ts` **同构**（互为移植，改一侧必须同步另一侧）
  - `count` 为**全部**观测条数——不受 `/logs/recent` 环形缓冲容量影响（数千条刷屏始终显示完整计数）；
    `firstTs`/`lastTs` 为极值（多线程到达顺序不保证单调）；`level` 取组内**最高严重度**（组内出现 Error 不会仍标 Warning）；
    `source` 取首个来源；`sampleText` 取首个原始文本样本
  - 组数上限 500（`LogSummaryStore.DefaultMaxGroups`）；溢出时淘汰 `lastTs` 最小（最久未更新）的组，
    `lastTs` 并列取 key 序最小者（确定性），每次淘汰令 `overflowDropped` 累加 1
  - 组序固定：`count` 降序 → `lastTs` 降序 → `key` 序升序
  - `since` 为**时间游标**：只返回 `lastTs` 晚于它的组（新增 / 更新）；接受端点自己输出的 ISO 8601
    （`lastTs` 原样回填即可往返）或整数 UTC Ticks；缺省 / 非法即不过滤
- `name`（武器 / 装备）：live 实测（2026-09-15）在客户端 raid 上下文返回**本地化键**形态
  （如 `"5ac66d9b5acfc4001633997a Name"`）而非本地化值；`tpl` 为权威标识；本地化名解析列 backlog

`sampleAgeMs` 为快照相对当前时刻的年龄（单调毫秒）。`MainPlayer` 为空（未进 raid / 已撤离）时清除快照，所有 raid 端点返回 `inRaid:false`。
bot 明细上限 200 条；超出时 `truncated:true`（`total` 仍为完整计数）。

## 安全边界

- 仅绑定 `127.0.0.1`，不监听外部网卡，不接受远程连接。
- 只读：无写端点、无游戏状态修改。
- 不向真实游戏安装目录写入；仅经 MO2 overlay 投影。

## 故障排查

| 症状 | 排查 |
|---|---|
| MCP 报 `BRIDGE_UNREACHABLE` | 查 `BepInEx/LogOutput.log` 是否有 `Loading [Tarkov Runtime Bridge …]` 行；确认 MO2 覆盖层已启用、游戏经 MO2 启动 |
| 日志有 `failed to start HTTP listener` | 端口被占用（改 `Port`）或 URL ACL 拒绝（本机 `127.0.0.1` 实测免 ACL；受限策略下可改绑 `localhost` 或 `netsh http add urlacl`） |
| 端点可达但 `raidId` 为 `no-start` | 桥在 raid 中途启动（会话起点未被观测）——重新进出一次 raid 即获得完整 id |
| 配置改了不生效 | 配置可能被 usvfs 重定向到 MO2 `overwrite/BepInEx/config/`；改那份并重启游戏 |
| 端点可达但 `inRaid:false` | 当前不在 raid（菜单/藏身处）；进 raid 后自动恢复 |
| 无 `damage` / `death` 事件 | 查 `BepInEx/LogOutput.log` 的两补丁成功行 `Harmony patches applied (LocalGame.Stop extraction + ActiveHealthController.ApplyDamage hooks).`；若见 `Harmony patch failed (ActiveHealthController.ApplyDamage damage)` 或 `Bridge event collector: death subscribe failed`，对应采集被禁用 |
| 端点无响应且无日志行 | 插件未加载：检查覆盖层 `BepInEx/plugins/TarkovRuntimeBridge.dll` 是否存在、BepInEx 版本是否为 6（IL2CPP） |
| `/logs/recent` 恒为 `entries:[]` | 确认 `[LogWatch] Enabled=true`（改配置后需重启游戏）；`seq=0` 表示尚未捕获任何条目；默认只采集 Warning 及以上（Info/Debug 不采集，除非调低 `[LogWatch] MinLevel`）；`dropped>0` 表示轮询间隔内缓冲已绕圈（调大 `[LogWatch] RingSize` 或提高轮询频率）；`level` 过滤过严时也可能为空（空结果即环内无匹配，可放宽 `level` 或先拉全量） |
| `/logs/summary` 的 `count` 与 `/logs/recent` 条数不一致 | 预期行为：`count` 覆盖全部采集条目，`/logs/recent` 只保留最近 `[LogWatch] RingSize` 条；`overflowDropped>0` 表示聚合组数超过 500 上限（最久未更新的组被淘汰，计数只增不减） |
| 两个不同错误被并成一组 | 归一化把数字 / hex / GUID 视为易变 token。若确有区分意义，属已知取舍（保守策略下已要求词边界、不折叠大小写、不做模糊匹配）；如遇误合并请在工单里补样本 |

## 构建

```powershell
dotnet build "tools/tarkov-runtime-bridge/TarkovRuntimeBridge.csproj" -c Release
# SPT 安装根覆盖（STD-BUILD-006）：-p:SPTInstallPath="D:/SPT-5.0.0/DEV"（兼容 -p:GameDir=）
```

产物：`tools/tarkov-runtime-bridge/bin/Release/TarkovRuntimeBridge.dll`。

## 测试

纯逻辑单测（路由判定 / JSON 构造 / sampleAge / `RaidStateStore` / raidId 组装 / 事件缓冲 / 击杀归属 / 查询解析 /
日志环形缓冲 / 日志级别过滤 / 日志归一化 / 聚合组淘汰 / summary 时间游标）在
`tests/TarkovRuntimeBridge.Tests`（xunit，net6.0，引用主工程）；游戏耦合部分（IL2CPP /
`GameWorld` 读取、BepInEx `ILogListener` 适配）不做假接口，以 live 验收为准。

```powershell
dotnet test "tools/tarkov-runtime-bridge/tests/TarkovRuntimeBridge.Tests/TarkovRuntimeBridge.Tests.csproj" -c Release
```

> 测试工程构建主工程，故需本机存在 `Directory.Build.props` 所指向的 SPT 安装（引用其 interop 程序集）。
