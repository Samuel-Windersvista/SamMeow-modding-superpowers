# 01 侦查核验报告 — EFT 1.1.5 interop 成员签名 + 参考源登记

> 日期：2026-09-14 | 对应工单：`issues/01-signature-verification-references.md`
> 方法：直读本地 SPT 5.0 客户端 `BepInEx/interop`（ilspycmd 11.0.0.9375 + `tools/eft-classmap`），并与 SPT 官方 `modules@5.0x-dev` 的既有用法交叉验证。
> 证据约定：`<类型>@<行>` 指反编译产物（缓存于 `D:\Temp\opencode\interop-1.1.5\`，可按 §8 命令重建）；「modules」指 `SP-Tushonka/modules` 分支 `5.0x-dev`（本地已 fetch，tip `b5513e6`）中的真实用法。
> **v2 勘误（2026-09-14，@oracle 抽查后修正）**：① `EscapeTimeSeconds` 实为扩展方法（原判「不存在」有误）；② `StopDateTime` 非 Nullable；③ 总血量路径补 `EBodyPart.Common`；④ `ValueStruct` 取值明确为 `.Current`；⑤ modules 行号校正。

## 0. 结论速览

| 域 | 结论 |
|---|---|
| 访问链（GameWorld / AbstractGame / IBotGame） | **全部可读**，且 modules 官方代码在用 |
| 玩家：位置 / 朝向 / 姿态 / 存活 | **全部可读**（姿态首选 `Player.Pose`） |
| 玩家：血量（总 + 肢体） | 可读；`GetBodyPartHealth` 返回 `ValueStruct`，取值 `.Current`（§6-1）；总血量 = `GetBodyPartHealth(EBodyPart.Common)` |
| raid 元数据（地图 / 状态 / 剩余时间） | 可读；剩余时间 = `GameTimer.EscapeTimeSeconds()`（**扩展方法**，`EFT.GameTimerExtension`） |
| raidId | 无现成单字段 → 采用组合方案（§3.4） |
| bot 域（枚举 / 计数 / 类型 / 阵营 / 位置 / 存活） | **全部可读**，modules 的 `BotMonitor` 是完整范例 |

> 无「读不到」项；跨程序集/包装类注意点集中在 §6。

## 1. 访问链

| 目标 | 签名 | 证据 |
|---|---|---|
| 游戏世界单例 | `Comfort.Common.Singleton<GameWorld>.Instance` → `GameWorld` | modules: `BotMonitor.cs:47`（另 :32）、`NoclipCommand.cs:49`、`GodModeCommand.cs:24` |
| 游戏会话 | `Singleton<AbstractGame>.Instance`（+ `.TryCast<LocalGame>()`） | modules: `DebugExtractCommand.cs:20`、`ServerActionPatches.cs:82`、`TimelineBankPatches.cs:28`；`EFT.LocalGame` 在类型清单 @4690 |
| bot 系统 | `Singleton<IBotGame>.Instance?.BotsController?.BotSpawner` | modules: `BotMonitor.cs:85`；`Class IBotGame` 在类型清单 @1466 |

- `GameWorld.MainPlayer` → `Player`（GameWorld@4581；modules 多处）
- `GameWorld.AllAlivePlayersList` → `List<Player>`（GameWorld@4359；modules: `BotMonitor.cs:96`、`TeleportCommands.cs:94`）
- `GameWorld.OnPersonAdd` → `Il2CppSystem.Action<IPlayer>`（事件，可用于增量订阅；GameWorld@4093）
- `GameWorld.LocationId` → `string`（GameWorld@5589）

## 2. 玩家域

### 2.1 位置与朝向

| 字段 | 签名 | 证据 / 备注 |
|---|---|---|
| 位置 | `Player.Position` → `Vector3` | Player@76395；modules: `bot.Position`、`world.MainPlayer.Position`（`BotMonitor.cs:94,107`） |
| 位置（备选） | `Player.Transform` → `BifacialTransform`，成员含 `position` / `rotation` / `forward` / `up` / `right` / `eulerAngles` | Player@78820（classmap dump）；modules: `NoclipCommand.cs:57,145` 用 `player.Transform.position` 读写 |
| 朝向（水平角） | `Player.Rotation` → `Vector2` | Player@76203 |
| 朝向（向量） | `Player.LookDirection` → `Vector3` | Player@76805 |
| 视点 | `Player.CameraPosition` → `Transform`（取 `.position`） | Player@76732；modules: `BotMonitor.cs:93` |
| 速度 | `Player.Velocity` → `Vector3` | Player@76274 |

### 2.2 姿态

| 字段 | 签名 | 证据 / 备注 |
|---|---|---|
| 姿态（**首选**） | `Player.Pose` → `EPlayerPose { Prone=0, Duck=1, Stand=2 }` | Player@76161；枚举直查（Assembly-CSharp interop） |
| 俯卧布尔 | `Player.IsInPronePose` → `bool` | Player@76244 |
| 冲刺布尔 | `Player.IsSprintEnabled` → `bool` | Player@76435 |
| 二线（细状态） | `Player.MovementContext` → `MovementContext`（@76094）→ `CurrentState` → `BaseMovementState`；其成员 `Name` → `EPlayerState`、`Type` → `EStateType`、`AnimatorStateName` → `string` | BaseMovementState@77/64/90（decompile）；modules 未用（备选路径） |

### 2.3 血量与存活

| 字段 | 签名 | 证据 / 备注 |
|---|---|---|
| 健康控制器 | `Player.ActiveHealthController` → `ActiveHealthController` | Player@78848；modules: `GodModeCommand.cs:25` |
| 存活 | `IsAlive` → `bool`（`BaseHealthController<TEffect>` 虚属性） | BaseHealthController`1@2688；接口 `IHealthController` 亦有 `IsAlive` |
| 单肢体血量 | `GetBodyPartHealth(EBodyPart bodyPart, bool rounded = false)` → `EFT.HealthSystem.ValueStruct`（取值 `.Current`） | BaseHealthController`1@3848；`IBaseHealthController` 亦声明同名方法 |
| **总血量** | `GetBodyPartHealth(EBodyPart.Common)`（native 对 `Common` 走聚合分支） | 同上；T04 实现「总血量」用此路径 |
| 肢体破坏/摧毁 | `IsBodyPartBroken(EBodyPart)` / `IsBodyPartDestroyed(EBodyPart)` → `bool` | BaseHealthController`1 native 方法名 @2101/2103；`IHealthController` 亦有 |
| 危险肢体 | `GetBodyPartsInCriticalCondition(...)` | `IHealthController` 成员 |
| 事件（第二波） | `HealthChangedEvent` / `DiedEvent` / `BodyPartDestroyedEvent` / `BodyPartRestoredEvent` | `IHealthController` 成员（事件流预留） |

- 肢体枚举：`EBodyPart { Head, Chest, Stomach, LeftArm, RightArm, LeftLeg, RightLeg, Common }` — **声明在 `PlayerEnums.dll`**（非 Assembly-CSharp，见 §6-2）

### 2.4 身份与阵营（枚举直查）

| 枚举 | 取值 |
|---|---|
| `EPlayerSide` | `Usec = 1, Bear = 2, Savage = 4` |
| `EPlayerPose` | `Prone = 0, Duck = 1, Stand = 2` |
| `EBotState` | `NonActive, PreActive, Active, ActiveFail, Disposed` |
| `GameStatus` | `Stopped, Running, Runned, Starting, Started, Stopping, SoftStopping` |
| `EGameTimerStatus` | `Unknown, Started, Stopped` |

| 字段 | 签名 | 证据 |
|---|---|---|
| 是否本地玩家 | `Player.IsYourPlayer` → `bool` | Player@79106；modules: `BotMonitor.cs:100` |
| profile id | `Player.ProfileId` → `string` | Player@78291 |
| 存档 | `Player.Profile` → `Profile`（@78411）：`Profile.Side` → `EPlayerSide`（@4493）、`Profile.Nickname`（@4411）、`Profile.Info` → `ProfileInfo`（@3713） | modules: `BotMonitor.cs:107` |
| 阵营/类型 | `ProfileInfo.Side` → `EPlayerSide`（@669）；`ProfileInfo.Level` → `int`（@745）；`ProfileInfo.Settings` → `ProfileSettings`（@694）：`.Role` → `WildSpawnType`（@28）、`.BotDifficulty` → `BotDifficulty`（@41） | modules: `bot.Profile.Info.Settings.Role` / `.BotDifficulty` / `bot.Profile.Side`（`BotMonitor.cs:107`） |

## 3. raid 元数据

### 3.1 状态

- `AbstractGame.Status` → `GameStatus { Stopped, Running, Runned, Starting, Started, Stopping, SoftStopping }`（AbstractGame@470）
- `AbstractGame.GameTimer` → `GameTimer`（AbstractGame@494）

### 3.2 地图

- `AbstractGame.LocationId` → `string`（AbstractGame@652）；`GameWorld.LocationId` → `string`（GameWorld@5589）

### 3.3 时间

| 字段 | 签名 | 证据 |
|---|---|---|
| 计时器状态 | `GameTimer.Status` → `EGameTimerStatus` | GameTimer@188 |
| **剩余时间** | `GameTimer.EscapeTimeSeconds()` → `float`（**扩展方法**，`EFT.GameTimerExtension`） | GameTimerExtension（ilspycmd 直读）；同族：`SessionSeconds` / `PastTimeSeconds` / `EscapeTimeTimeSpan` / `Started` / `TryStop` |
| 已过时间 | `GameTimer.PastTime` → `TimeSpan`；`PastTimeSeconds()` → `float`（扩展） | GameTimer@264；GameTimerExtension |
| 会话时长 | `GameTimer.SessionTime` → `Nullable<TimeSpan>` | GameTimer@212 |
| 起止时刻 | `StartDateTime` / `EscapeDateTime` → `Nullable<DateTime>`；`StopDateTime` → `Il2CppSystem.DateTime`（**非 Nullable**） | GameTimer@236/250/279 |

- 主路径用扩展方法（`using EFT;` 即可）；兜底 `SessionTime − PastTime`。⚠ 移植注意：`EscapeTimeSeconds` 不在 `GameTimer` 本类上，IDE 提示时注意扩展类。
- `StopDateTime` 与另两者可空性不同，判空逻辑需区别对待。

### 3.4 raidId（组合方案）

- `AbstractGame` / `GameWorld` 均**无**现成 `SessionId` 属性（已核查：类清单与两处反编译均未命中）。
- 采用确定性组合：`raidId = <GameWorld.CurrentProfileId> + "@" + <GameTimer.StartDateTime>`（`CurrentProfileId` → `Nullable<MongoID>`，GameWorld@4064）。断言只需稳定可 diff；如 T03 发现 `LocalGame` 上有原生会话字段可替换。

## 4. bot 域

| 字段 | 签名 | 证据 |
|---|---|---|
| 全部存活角色 | `GameWorld.AllAlivePlayersList` → `List<Player>` | GameWorld@4359；modules: `BotMonitor.cs:96` |
| bot 计数（存活+装载） | `BotSpawner.AliveAndLoadingBotsCount` → `int` | BotSpawner@2772；modules: `BotMonitor.cs:88` |
| bot 计数（含延迟） | `BotSpawner.AllBotsWithDelayed` / `BotsDelayed` → `int` | BotSpawner@2787/2802；modules: `BotMonitor.cs:89-90` |
| 控制器/生成器 | `BotsController.BotSpawner` → `BotSpawner`（@1255）；`BotsController.Bots` → `BotsList`（@690） | modules: `BotMonitor.cs:85` |
| 逐 bot 入口 | `Player.AIData` → `IAIData`（Player@78606；**全局命名空间**，见 §6-3）→ `IAIData.BotOwner` → `BotOwner` | classmap: IAIData 成员含 `BotOwner`；modules: `bot.AIData?.BotOwner`（`BotMonitor.cs:105`） |
| bot 状态 | `BotOwner.BotState` → `EBotState`（@5907）；`.IsDead` → `bool`（@6516） | BotOwner 反编译 |
| bot 位置 | `BotOwner.Position` → `Vector3`（@6000）；或 `Player.Position` | modules 用 `bot.Position`（Player 层，`BotMonitor.cs:107`） |
| bot 归属 | `BotOwner.GetPlayer` → `Player`（@6272） | BotOwner@6272 |
| 分区 | `BotOwner.BotsGroup.BotZone.NameZone` → `string` | modules: `BotMonitor.cs:105` |
| 类型/阵营 | `Player.Profile.Info.Settings.Role` → `WildSpawnType`；`Player.Profile.Side` → `EPlayerSide` | modules: `BotMonitor.cs:107` |

- 分类策略（T04 用）：PMC = `Side ∈ {Usec, Bear}`；Scav = `Side == Savage` 且 `Role == assault`（按需细化）；Boss = `Role` 为 boss* 系（以 `WildSpawnType.ToString()` 输出，不硬编码全枚举）。

## 5. 字段映射表（spec → 访问路径，T04/T05 直接可用）

| spec 字段 | 访问路径 |
|---|---|
| raid.地图名 | `Singleton<AbstractGame>.Instance.LocationId` |
| raid.状态 | `AbstractGame.Status`（GameStatus） |
| raid.剩余时间 | `AbstractGame.GameTimer.EscapeTimeSeconds()`（扩展方法；兜底 `SessionTime − PastTime`） |
| raid.raidId | `GameWorld.CurrentProfileId` + `GameTimer.StartDateTime`（组合） |
| player.位置 | `GameWorld.MainPlayer.Position` |
| player.朝向 | `MainPlayer.Rotation`（水平）/ `LookDirection`（向量） |
| player.姿态 | `MainPlayer.Pose`（EPlayerPose） |
| player.血量（总） | `MainPlayer.ActiveHealthController.GetBodyPartHealth(EBodyPart.Common).Current` |
| player.血量（肢体） | `MainPlayer.ActiveHealthController.GetBodyPartHealth(<EBodyPart>)`（`.Current`） |
| player.存活 | `MainPlayer.ActiveHealthController.IsAlive` |
| bots.总数/分类 | `AllAlivePlayersList` 过滤 `!IsYourPlayer` + `Profile.Side` / `Settings.Role`；生成器计数见 §4 |
| bots.明细 | 每 `Player`：`Position`、`Profile.Info.Settings.Role`、`Profile.Side`、`ActiveHealthController.IsAlive` |

## 6. 不确定项与运行时验证清单（T02/T04 实测）

1. **`GetBodyPartHealth` 返回 `EFT.HealthSystem.ValueStruct`**（显式布局 struct：字段 `Current` / `Maximum` / `Minimum` / `OverDamageReceivedMultiplier` / `EnvironmentDamageMultiplier`，属性 `Normalized` / `AtMinimum` / `AtMaximum`）——取值即 `.Current`；**降级为一条冒烟验证**（不再列为最高风险）。
2. **`EBodyPart` 跨程序集**：声明在 `PlayerEnums.dll`；桥工程需引用该 interop 程序集（csproj 引用注意）。
3. **`IAIData` 在全局命名空间**（无 `EFT.` 前缀）：using/限定名注意。
4. **raidId 组合方案**：T03 复核 `LocalGame` 是否有更原生字段。
5. **`BotDifficulty` 枚举**（全局 `BotDifficulty`）取值集合未展开——低风险，`ToString()` 输出即可。
6. **远端 bot 的 `Pose` 同步可靠性**：`Player.Pose` 对 bot 是否实时同步需 live 验证（T04）；不可靠则退回 `IsInPronePose` + `MovementContext.CurrentState.Name`。
7. **`BifacialTransform.position` 的模仿层语义**：本地玩家读写正常（modules 在用）；bot 位置建议统一用 `Player.Position`（T04 实测比对）。
8. **扩展方法可用性**：`GameTimerExtension` 为静态类，桥代码 `using EFT;` 后按实例方法调用即可（T02 冒烟覆盖）。

## 7. 参考源登记

### 7.1 SPT 官方 modules 分支 `5.0x-dev`（本地已 fetch，tip `b5513e6`）

路径：`E:\云文件\GitHub\SamMeow_SP-Tushonka_modules_source_code`（工作树在 `master`；分支来自 remote `tushonka` = `SP-Tushonka/modules`）

| 文件 | 用途 |
|---|---|
| `SPTushonka.Debugging/Scripts/BotMonitor.cs` | **bot 域完整范例**：存活列表枚举、生成器计数、角色/阵营/难度/分区/距离 |
| `SPTushonka.Debugging/Commands/NoclipCommand.cs` | `player.Transform.position` 读写的标准写法 |
| `SPTushonka.Debugging/Commands/GodModeCommand.cs` | `player.ActiveHealthController` 访问 + Harmony patch 范式 |
| `SPTushonka.Debugging/Commands/TeleportCommands.cs` | `Singleton<GameWorld>` / `AllAlivePlayersList` / `MainPlayer` 用法 |
| `SPTushonka.Debugging/Commands/DebugExtractCommand.cs` | `Singleton<AbstractGame>` + `TryCast<LocalGame>` 用法 |
| `SPTushonka.Debugging/Patches/BsgLogPatch.cs` | 日志 patch 参考 |

### 7.2 生态先例（本地 clone，`external/references/`，gitignored）

| 项目 | 关键文件 | 用途 |
|---|---|---|
| `SPTarkovWebMinimap/`（NNThomasL/SPTarkovWebMinimap） | `TechHappy.MapLocation/Services/MapDataServer.cs`；`Services/Bots/BotDataService.cs`；`Services/Airdrop/AirdropService.cs`；`Patches/MapLocationPatch.cs`；`MapLocationPlugin.cs` | 插件内嵌 HTTP 导出玩家/bot 位置的同构先例 |
| `bepinex-mcp/`（rkuhn153/bepinex-mcp） | `plugins/BepInExMCP.IL2CPP/HttpBridgeServer.cs`；`MainThreadDispatcher.cs`；`Plugin.cs`；`Protocol.cs`；`WatcherService.cs` | BepInEx 6 IL2CPP 的 HttpListener + 主线程调度模式 |

### 7.3 仓库内资产

- `knowledge/spt-kb/archive/eft-1.1.5/classes-1.1.5.txt`（16,435 类型清单）、改名对照（`rename-candidates/`）
- `tools/eft-classmap/`（成员指纹匹配 / `--dump` 单类型成员清单）

## 8. 复现命令

```powershell
# 单类型反编译（缓存到 D:\Temp\opencode\interop-1.1.5\）
ilspycmd -t "EFT.Player" "E:\Game\EFT_Offline\SPT_5xx\BepInEx\interop\Assembly-CSharp.dll"

# 成员清单（快速、无反编译）
dotnet tools/eft-classmap/bin/Release/net10.0/eft-classmap.dll --dump "<interop>\Assembly-CSharp.dll" EFT.Player

# 跨程序集类型（如 EBodyPart）
ilspycmd -t "EBodyPart" "E:\Game\EFT_Offline\SPT_5xx\BepInEx\interop\PlayerEnums.dll"
```
