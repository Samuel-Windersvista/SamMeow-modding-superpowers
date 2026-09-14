# Tarkov Runtime Bridge

tarkov-runtime-MCP Phase 2 的客户端桥插件（C# / BepInEx 6 IL2CPP）。
读取 SPT 5.0 客户端（EFT 1.1.5）的玩家域（位置/朝向/姿态/血量）、raid 元数据（地图/状态/剩余时间/raidId）
与 bot 域（摘要 + 明细），经仅绑 `127.0.0.1` 的只读 HTTP 端点暴露。

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

端口占用 / URL ACL 拒绝等启动失败只记 error，游戏照常运行；此时位置采样仍在内存中进行，仅 HTTP 端点不可用。

## 端点契约

所有端点仅 `GET`，仅绑 `127.0.0.1`，响应字段顺序稳定（可逐字节 diff），数值用 InvariantCulture，
NaN/Infinity 退化为 `0`。

| 方法 | 路径 | 响应 |
|---|---|---|
| GET | `/bridge/info` | `200 {"pluginVersion":<str>,"protocolVersion":1,"capabilities":{"endpoints":["/bridge/info","/raid/player","/raid/status","/raid/bots"],"sections":["player","raid","bots"]},"sampling":{"intervalMs":<int>},"network":{"host":"127.0.0.1","port":<int>}}` |
| GET | `/raid/player` | 在 raid：`200 {"inRaid":true,"position":{"x":..,"y":..,"z":..},"rotation":{"x":..,"y":..},"pose":"Stand","health":{"alive":true,"total":<float>,"parts":{"Head":..,"Chest":..,"Stomach":..,"LeftArm":..,"RightArm":..,"LeftLeg":..,"RightLeg":..}},"sampleAgeMs":<int>}` |
| GET | `/raid/player` | 不在 raid：`200 {"inRaid":false}` |
| GET | `/raid/status` | 在 raid：`200 {"inRaid":true,"map":<str>,"status":<GameStatus>,"remainingSeconds":<float>,"raidId":"<profileId>@<会话起点 UTC ISO>","sampleAgeMs":<int>}` |
| GET | `/raid/status` | 不在 raid：`200 {"inRaid":false}` |
| GET | `/raid/bots` | 在 raid 摘要：`200 {"inRaid":true,"total":<int>,"alive":<int>,"byCategory":{"pmc":n,"scav":n,"boss":n,"other":n},"spawner":{"aliveAndLoading":n,"delayed":n,"allWithDelayed":n},"sampleAgeMs":<int>}` |
| GET | `/raid/bots?detail=1` | 摘要 + `"bots":[{"x":..,"y":..,"z":..,"role":<WildSpawnType>,"side":<EPlayerSide>,"alive":<bool>}],"truncated":<bool>` |
| GET | `/raid/bots` | 不在 raid：`200 {"inRaid":false}` |
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
| 端点无响应且无日志行 | 插件未加载：检查覆盖层 `BepInEx/plugins/TarkovRuntimeBridge.dll` 是否存在、BepInEx 版本是否为 6（IL2CPP） |

## 构建

```powershell
dotnet build "tools/tarkov-runtime-bridge/TarkovRuntimeBridge.csproj" -c Release
# SPT 安装根覆盖（STD-BUILD-006）：-p:SPTInstallPath="D:/SPT-5.0.0/DEV"（兼容 -p:GameDir=）
```

产物：`tools/tarkov-runtime-bridge/bin/Release/TarkovRuntimeBridge.dll`。

## 测试

纯逻辑单测（路由判定 / JSON 构造 / sampleAge / `RaidStateStore` / raidId 组装）在
`tests/TarkovRuntimeBridge.Tests`（xunit，net6.0，引用主工程）；游戏耦合部分（IL2CPP /
`GameWorld` 读取）不做假接口，以 live 验收为准。

```powershell
dotnet test "tools/tarkov-runtime-bridge/tests/TarkovRuntimeBridge.Tests/TarkovRuntimeBridge.Tests.csproj" -c Release
```

> 测试工程构建主工程，故需本机存在 `Directory.Build.props` 所指向的 SPT 安装（引用其 interop 程序集）。
