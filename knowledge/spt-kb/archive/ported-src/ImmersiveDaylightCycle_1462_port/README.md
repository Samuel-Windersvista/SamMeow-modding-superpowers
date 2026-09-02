# ImmersiveDaylightCycle 1462 — SPT 3.11 -> 4.1.2 客户端移植

> 来源：SPT 3.11 客户端 mod DLL（`ImmersiveDaylightCycle.dll`，18KB，spt-reflection 3.11.0 + spt-common）
> 目标：SPT 4.1.2（EFT 0.16.9.40743）BepInEx 5.4.23.5 客户端引用集
> 移植日期：2026-08-10 | 方法：ilspycmd 10.1 反编译 -> 手改 -> 重编译

## 产物

- `bin/Release/ImmersiveDaylightCycle.dll` — 17408 字节，0 错误编译
- 部署：`E:\Game\EFT_Offline\Inescapable Tarkov\mods\[8]沉浸感时间循环-ImmersiveDaylightCycle\BepInEx\plugins\ImmersiveDaylightCycle\`

## 改动清单（3.11 -> 4.1）

| # | 文件 | 改动 | 依据 |
|---|------|------|------|
| 1 | `ImmersiveDaylightCycle_Fika_FikaBridge.cs` | `GetRaidId()` 回退路径：`((IPlayerAndPetProfile)((ClientApplication<ISession>)...).GetClientBackEndSession()).Profile.ProfileId` -> `ClientAppUtils.GetMainApp().Session.Profile.ProfileId` | 4.1 中 `TarkovApplication.Session`（IEftSession）直接暴露 Profile；`IPlayerAndPetProfile`/`GetClientBackEndSession` 已不存在 |
| 2 | `Jehree_ImmersiveDaylightCycle_Patches_OfflineRaidEndedPatch.cs` | Postfix 参数 `GClass1959 results` -> `EFT.SessionResult results` | 实测 4.1 `EftClientBackendSession.LocalRaidEnded(LocalRaidSettings, SessionResult, FlatItem[], Dictionary<string,FlatItem[]>)`；SessionResult 含 `result`/`playTime` 字段（Class_Name_Mappings 中 GClass1959->BindItemOperationDescriptor 为误标） |
| 3 | `Jehree_ImmersiveDaylightCycle_Helpers_Utils.cs` | 类名 `Utils` -> `ModUtils`（含全部静态调用点） | KB client-mod-311-to-41.md 5.1 节：4.1 Assembly-CSharp 存在全局顶层 `Utils` 类（90 方法）遮蔽 mod 自身 Utils |
| 4 | `ImmersiveDaylightCycle.csproj` | 经典格式 v4.7.2；引用 `Refs-410`；**新增 `bsg.console.core` 引用**（4.1 中 ConsoleCommandAttribute 定义于此，Refs-410 缺，从游戏 Managed 目录补齐）；新增 `Sirenix.Serialization` 引用 | LeaveItThere_1907 移植同样引用 bsg.console.core |
| 5 | `Jehree_ImmersiveDaylightCycle_Plugin.cs` | `LogSource = ((BaseUnityPlugin)this).Logger` -> `LogSource = Logger` | BepInEx 5.4.23 中 Logger 为 protected 成员，直接访问 |

## 未改动的 4.1 兼容点（已逐一验证）

- `LocationConditionsPanel.Set/Update` — 4.1 签名 `Set(IMatchmakerSession<RaidSettings>, RaidSettings, bool)`；TimeUIPanelPatch 用 `AccessTools.Method("Set")` 无参解析兼容；LocationConditionsPanelPatch 用 `FirstMethod(Set && param0.Name=="session")` 精确匹配 4.1 签名
- `LocationConditionsPanel` 字段（`_currentPhaseTime`/`_nextPhaseTime`/`_pmTimeToggle`/`_amTimeToggle`）— 4.1 全部 public，Harmony `____` 前缀注入规则一致
- `GameDateTime.Reset(DateTime, DateTime, float)` 3 参重载 — 4.1 存在
- `GameWorld.OnGameStarted` / `GameWorld.MainPlayer` — 4.1 存在（public field）
- `ConsoleScreen.Processor.RegisterCommandGroup<T>()` / `ConsoleScreen.Log` — 4.1 存在
- `RequestHandler.PostJson/PutJson`、`PatchConstants.EftTypes` — spt-common/spt-reflection 4.1.2 存在
- `FikaBridge` 事件委托解耦 — 单机无 Fika 时 `IAmHostEmitted` 为 null，回退返回 true；FikaModule 独立程序集跳过（单机包不需要）

## 验证

- 编译：`dotnet msbuild ImmersiveDaylightCycle.csproj /p:Configuration=Release` — 0 错误 0 警告
- 引用集指纹：spt-reflection 4.1.2.0 / spt-common 4.1.2.0 / BepInEx 5.4.23.5 / 0Harmony 2.9.0.0
- 部署：已复制到目标 mod 目录，17408 字节
- 运行时验证：待进游戏（BepInEx 日志确认插件 load + patch 绑定）
