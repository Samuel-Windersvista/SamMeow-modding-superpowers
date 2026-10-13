# BotDebug（Bot 覆盖层）API 触点核实表 — BotDebug 1.8.0 → EFT 0.16.9.40743

> 工单 02 子通道 B。只做 API 触点核实：上游 BotDebug 1.8.0 的全部补丁目标与反射触点逐一对照
> EFT 0.16.9.40743 反混淆 Managed 程序集（及 BigBrain 1.5.0 DLL）。不含实现代码、不改工程。
> 每个触点给出四态之一：`[OK]` / `[CHANGED]` / `[MISSING]` / `[UNCERTAIN]`，并附一行可直接指导实现的适配指引。

---

## 0. Provenance / 目标环境 / 命令清单

### 0.1 Provenance（上游源码）

| 项 | 值 |
|---|---|
| 仓库 | DrakiaXYZ/BotDebug（Forge mod id 918） |
| 本地归档 | `knowledge/spt-kb/archive/forge/mods/DrakiaXYZ-BotDebug_918_source/` |
| 版本 | `1.8.0`（`BotDebugPlugin.cs:12` `[BepInPlugin("xyz.drakia.botdebug","DrakiaXYZ-BotDebug","1.8.0")]`） |
| 上游锚 | `VersionChecker/TarkovVersion.cs:11` `[assembly: TarkovVersion(40743)]` —— 与目标构建 `0.16.9.40743` 精确一致 |
| License | MIT（`LICENSE.txt`） |
| 上游工程 | `netstandard2.1` / `RootNamespace DrakiaXYZ.BotDebug`；引用 `spt-common` / `spt-reflection` / `0Harmony` / `Comfort` / `Unity.Postprocessing.Runtime` / `DrakiaXYZ-BigBrain` / `Managed\Assembly-CSharp.dll` |
| 上游文件 | `BotDebugPlugin.cs` / `BotInfo.cs` / `Components\BotDebugComponent.cs` / `Helpers\Settings.cs` / `Helpers\EnumExtension.cs` / `VersionChecker\TarkovVersion.cs` |

### 0.2 目标环境

| 项 | 值 |
|---|---|
| 游戏程序集 | `E:\Game\EFT_Offline\SPT_41x\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll` |
| 游戏版本 | `EscapeFromTarkov.exe` → FileVersion `0.16.9.40743`，FilePrivatePart = **40743** |
| 运行时 | Mono / BepInEx 5 / netstandard2.1（无 IL2CPP 代理层，`as`/`is`/LINQ 均为托管原生语义） |
| 游戏程序集状态 | 已反混淆真名真命名空间（不是 `.spt-bak`） |
| BigBrain | `Norvinsk_Forever\mods\[6]AI拟人MOD的前置框架-BigBrain\BepInEx\plugins\DrakiaXYZ-BigBrain.dll`，`[BepInPlugin("xyz.drakia.bigbrain","DrakiaXYZ-BigBrain","1.5.0")]` |
| SPT 反射库 | `SPT_41x\BepInEx\plugins\spt\spt-reflection.dll`（`SPT.Reflection.Patching`） |

### 0.3 命令清单（本次核实实际执行的命令）

```powershell
# 单类型反编译（真实签名/字段/可见性）
ilspycmd -t "<Full.Type.Name>" "E:\Game\EFT_Offline\SPT_41x\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"
# BigBrain 程序集
ilspycmd -t "<Type>" "…\[6]AI拟人MOD的前置框架-BigBrain\BepInEx\plugins\DrakiaXYZ-BigBrain.dll"
# 类型清单（独立于 -t 的存在性交叉验证）
ilspycmd -l c <dll>        # 类
ilspycmd -l i <dll>        # 接口
ilspycmd -l e <dll>        # 枚举
# DLL 内类型名字符串扫描（ASCII）+ BigBrain 归属判断
[regex]::Matches([Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($dll)), "[A-Za-z0-9_]*BaseLogic[A-Za-z0-9_]*")
```

### 0.4 结论汇总（先给结论，明细见 §1/§2）

- **补丁目标 `GameWorld.OnGameStarted` 在 0.16.9 存活且为 `virtual`**：`public virtual void OnGameStarted()`（`EFT.GameWorld`），可被 `ModulePatch` 前缀捕获；补丁体须 fail-open。
- **数据源 `BotSpawner.GetBotDebugData` + `DebugBotStruct` 全部消费字段存活**：方法同名同参同返回；`BotData`/`HeathsData`/`PlayerOwner` 为公共**字段**，`ProfileId`/`InitedBotData` 为**属性**；两侧内层结构体的每个被消费字段实测存在且类型一致。
- **零 `[MISSING]`、零 `[CHANGED]`**：因上游 1.8.0 锚定版本号与本构建精确相同，BotOverlay 段全部触点 `[OK]`。
- 需在实现期注意的 **4 处非 API 差异**（非改签名，属实现细节 / 归类纠偏）：
  1. `BaseLogicLayer` / `BaseLogicLayerSimple` 是 **EFT 游戏端类型（全局命名空间）**，不是 BigBrain 类型——BigBrain 仅引用它们。
  2. `DebugBotStruct.PlayerOwner` 的类型是 **`IObserverToPlayerBridge`**（非 `Player`）；上游源码本就按该接口形状书写，无需改。
  3. `DebugBotStruct` 在 0.16.9 是 **`struct`（值类型）**，与 5.0 线 IL2CPP 代理类的值语义不同，但上游写法在其上成立。
  4. `CameraManager.Instance.SSAA` 为**实例属性 `=> _sSAA`**，`_sSAA` 在相机装配前可能为 null → 需空判（`[UNCERTAIN]` 仅指运行时非空性，类型存在性为 `[OK]`）。
- 多处类型位于**全局命名空间**（`DebugBotStruct` / `DebugBotDataStructInner` / `DebugHeathsDataStructInner` / `IAIData` / `EPlayerState` / `IBotGame` / `BotEnemiesController` / `EnemyInfo` / `BaseLogicLayerSimple` / `AICoreLayer<T>` / `SSAA`），而 `Player` / `BotOwner` / `GameWorld` / `BotSpawner` / `BotsController` / `CameraManager` 在 `EFT`（含子命名空间）。上游已有 `using EFT;`，二者均可解析。

---

## 1. 主表（逐触点四态）

> 位置 = 上游文件名:行号。「实测」列来自 0.16.9（或 BigBrain 1.5.0）反编译。

### 1.1 `BotDebugPlugin.cs`（补丁装配）

| # | 上游位置 | 目标（类型.成员） | 0.16.9 实测（签名 / 可见性 / 补丁性质） | 状态 | 适配/降级 | 一行实现指引 |
|---|---|---|---|---|---|---|
| 1 | BotDebugPlugin.cs:39 | `EFT.GameWorld.OnGameStarted`（补丁目标） | `public virtual void OnGameStarted()`，`EFT.GameWorld : MonoBehaviour` 上实例虚方法 | `[OK]` | 无 | `GetTargetMethod() => typeof(EFT.GameWorld).GetMethod(nameof(EFT.GameWorld.OnGameStarted))`；前缀体内 try/catch fail-open |
| 2 | BotDebugPlugin.cs:37 | `SPT.Reflection.Patching.ModulePatch` | `public abstract class ModulePatch`；`Enable()` / `Disable()` / `protected abstract MethodBase GetTargetMethod()` | `[OK]` | 无 | 继承 `ModulePatch`，`Enable()` 在 `Awake` 装配 |
| 3 | BotDebugPlugin.cs:41 | `SPT.Reflection.Patching.PatchPrefixAttribute` | `public class PatchPrefixAttribute : Attribute` | `[OK]` | 无 | 前缀方法标 `[PatchPrefix]` |
| 4 | BotDebugPlugin.cs:44 | `BotDebugComponent.Enable()` | 组件静态入口（见 1.4） | `[OK]` | 无 | 前缀体调用 `BotDebugComponent.Enable()` |

### 1.2 `BotInfo.cs` — 数据源与 `DebugBotStruct`（最高优先级）

| # | 上游位置 | 目标（类型.成员） | 0.16.9 实测（签名 / 可见性） | 状态 | 适配/降级 | 一行实现指引 |
|---|---|---|---|---|---|---|
| 5 | BotSpawner（被 1.4 #33 调用） | `EFT.BotSpawner.GetBotDebugData` | `public DebugBotStruct GetBotDebugData(IPlayer currentPlayer, string profileId)`；第 1 参为 `IPlayer` | `[OK]` | 无（`Player : IPlayer` 隐式兼容） | 直接传 `(localPlayer, player.ProfileId)`，无需强转 |
| 6 | 返回类型 | `DebugBotStruct` | `public struct DebugBotStruct`（**值类型**），含公共字段 `BotData` / `HeathsData` / `PlayerOwner` 与属性 `ProfileId` / `InitedBotData` 等 | `[OK]` | 无 | 以值存储（`botMap` 内 `DebugBotStruct Data` 字段），勿假设引用语义 |
| 7 | BotInfo.cs:21 | `DebugBotStruct`（参数）/ `BotInfoMode` | 参数类型与枚举均存在（`BotInfoMode` 见 #39） | `[OK]` | 无 | `GetInfoText(DebugBotStruct, Player, BotInfoMode)` 签名不变 |
| 8 | BotInfo.cs:25 | `BepInEx.Logging.Logger.CreateLogSource` | `ManualLogSource CreateLogSource(string)` | `[OK]` | 无 | 惰性建日志源 |
| 9 | BotInfo.cs:29 | `DebugBotStruct.PlayerOwner` | `public IObserverToPlayerBridge PlayerOwner`（**公共字段**；类型为接口，**非 `Player`**） | `[OK]` | 无（上游用 `var` 承接） | `var playerOwner = debugBotStruct.PlayerOwner;` 后判 `!= null` |
| 10 | BotInfo.cs:33 | `IObserverToPlayerBridge.AIData` | `IAIData AIData { get; }` | `[OK]` | 无 | `IAIData aiData = playerOwner.AIData;` |
| 11 | BotInfo.cs:34 | `IAIData.BotOwner` → `BotOwner.EnemiesController` → `BotEnemiesController.EnemyInfos` | `IAIData.BotOwner : BotOwner`；`BotOwner.EnemiesController : BotEnemiesController`；`BotEnemiesController.EnemyInfos : Dictionary<IPlayer,EnemyInfo>`（托管字典） | `[OK]` | 无（Mono 下即 `System.Collections.Generic`，`.Values`/LINQ 均可用） | `foreach (var enemyInfo in …EnemyInfos.Values)` 直接遍历 |
| 12 | BotInfo.cs:36 | `EnemyInfo.ProfileId` / `Player.ProfileId` | `EnemyInfo.ProfileId : string`；`Player.ProfileId : string`（`=> Profile.Id`） | `[OK]` | 无 | 字符串相等比较判定标记色 |
| 13 | BotInfo.cs:79-83 | `DebugBotStruct.BotData.Name` / `.StrategyName` / `.LayerName` / `.CustomData` | `DebugBotDataStructInner` 公共字段：`string Name` / `string StrategyName` / `string LayerName` / `string CustomData` | `[OK]` | 无 | 直接读 `debugBotStruct.BotData.X` |
| 14 | BotInfo.cs:85-90 | `DebugBotStruct.PlayerOwner.Nickname` | `IObserverToPlayerBridge.Nickname : string` | `[OK]` | 无 | null 判后用 |
| 15 | BotInfo.cs:110-115 | `BotData.Name` / `.StrategyName` / `.IsInSpawnWeapon` / `.HaveAxeEnemy` / `.BotRole` / `.PlayerState` | 字段逐一致：`string` / `string` / `bool IsInSpawnWeapon` / `bool HaveAxeEnemy` / `int BotRole` / `EPlayerState PlayerState` | `[OK]` | 无 | 逐字段读取，`${playerState}` 走枚举 `ToString` |
| 16 | BotInfo.cs:120-129 | `DebugBotStruct.ProfileId` | `public string ProfileId => PlayerOwner?.iPlayer.ProfileId;`（**属性**，可空链返回 null） | `[OK]` | 无；写入前须判 null/空（见 §2.3） | 循环分块前先 `if (string.IsNullOrEmpty(...))` 短路 |
| 17 | BotInfo.cs:141 | `IObserverToPlayerBridge.CurrentStataName` | `EPlayerState CurrentStataName { get; }`（拼写 `Stata` 非笔误） | `[OK]` | 无 | `playerOwner.CurrentStataName.ToString()` |
| 18 | BotInfo.cs:150-157 | `BotData.NodeName` / `.Reason` / `.PrevNodeName` / `.PrevNodeExitReason` | 字段均存在（全 `string`） | `[OK]` | 无 | 空串判后输出 PrevNode / ExitBy |
| 19 | BotInfo.cs:173-174 | `IObserverToPlayerBridge.iPlayer` → `IPlayer.Position`；`Player.Transform.position` | `IPlayer iPlayer { get; }`；`IPlayer.Position : Vector3`；`Player.Transform : BifacialTransform`（`.position : Vector3`） | `[OK]` | 无 | `Mathf.RoundToInt((iPlayer.Position - localPlayer.Transform.position).magnitude)` |
| 20 | BotInfo.cs:183-190 | `BotData.Ammo` / `.HitsOnMe` / `.ShootsOnMe` / `.Reloading` / `.CoverIndex` | `int Ammo` / `int HitsOnMe` / `int ShootsOnMe` / `bool Reloading` / `int CoverIndex` | `[OK]` | 无 | 直接读 |
| 21 | BotInfo.cs:199-205 | `BotOwner.GetPlayer` → `Player.HandsController` → `Player.FirearmController.Item` → `Weapon.ChamberAmmoCount` / `Weapon.GetCurrentMagazineCount()` | `Player GetPlayer { get; set; }`；`Player.HandsController : AbstractHandsController`（`virtual`）；嵌套 `Player.FirearmController`（`: ItemHandsController`），`public new Weapon Item`；`Weapon.ChamberAmmoCount : int`；`Weapon.GetCurrentMagazineCount() : int` | `[OK]` | Mono 下 `as` 原生有效，无需 TryCast（5.0 线才需） | `var fc = botOwner.GetPlayer.HandsController as Player.FirearmController; if (fc != null) { fc.Item.ChamberAmmoCount; fc.Item.GetCurrentMagazineCount(); }` |
| 22 | BotInfo.cs:212-213 | `BotWeaponManager.Selector.IsWeaponReady` / `BotWeaponManager.Malfunctions.HaveMalfunction()` | `BotWeaponSelector Selector { get; set; }`；`bool IsWeaponReady { get; set; }`；`BotMalfunctionData Malfunctions { get; set; }`；`bool HaveMalfunction()` | `[OK]` | 无 | 保持 `?.` 空条件链 |
| 23 | BotInfo.cs:232 | `BotOwner.Memory.GoalEnemy` | `BotMemory Memory`（公共字段）；`EnemyInfo GoalEnemy { get; }` | `[OK]` | 无 | `aiData.BotOwner.Memory.GoalEnemy` 判 null |
| 24 | BotInfo.cs:233-237 | `EnemyInfo.Person` → `IPlayer.IsAI` / `IPlayer.AIData.BotOwner.name` | `EnemyInfo.Person : IPlayer`；`IPlayer.IsAI : bool`；`IPlayer.AIData : IAIData`；`IAIData.BotOwner : BotOwner`；`BotOwner : MonoBehaviour`（`.name`） | `[OK]` | 无 | AI 敌人显示 BotOwner.name，否则 `Nickname` |
| 25 | BotInfo.cs:242 | `EnemyInfo.Nickname` | `string Nickname { get; }`（`=> Person.Profile.Nickname`） | `[OK]` | 无 | 直接读 |
| 26 | BotInfo.cs:255-274 | `DebugBotStruct.HeathsData.*`（Head/Body/Stomach/Arms/Legs 的 `Health*` / `Health*BL` / `Health*Broken`） | `DebugHeathsDataStructInner` 公共字段：`int Health{Head,Body,Stomach,LeftArm,RightArm,LeftLeg,RightLeg}`、对应 `bool Health*BL`、`bool Health*Broken`——**全部命中** | `[OK]` | 无（上游 `Chest` 行误用 `healthHeadBroken` 是上游自身 bug，与原样移植一致即不改行为） | 逐部位读取；注意上游 Chest 行 Broken 标记取自 Head |
| 27 | BotInfo.cs:306-316 | `UnityEngine.Color.*` / `ColorUtility.ToHtmlStringRGB` | Unity 托管 API（`Color.white/yellow/green/grey…`；`ToHtmlStringRGB(Color) : string`） | `[OK]` | 无 | 富文本色标签纯字符串拼接 |
| 28 | BotInfo.cs:377-389 | `BotInfoMode`（枚举，含 BigBrainLayer/BigBrainLogic 档） | 上游自定义枚举，纯托管 | `[OK]` | 无 | 保留枚举与顺序（`Minimized=0…`），供 F10/F11 循环 |

### 1.3 `BotInfo.cs` — BigBrain 两模式（依赖 BigBrain 1.5.0）

> 上游 `#if !STANDALONE` 段。**重要归属纠正**：`BaseLogicLayerSimple` / `BaseLogicLayer` 定义在 **EFT 游戏程序集（全局命名空间）**，BigBrain 只是引用方（其 DLL 内仅出现 TypeRef 名字符串，无对应 TypeDef）。

| # | 上游位置 | 目标（类型.成员） | 实测（签名 / 归属） | 状态 | 适配/降级 | 一行实现指引 |
|---|---|---|---|---|---|---|
| 29 | BotInfo.cs:327,348 | `DrakiaXYZ.BigBrain.Brains.BrainManager.GetActiveLayer(BotOwner)` / `GetActiveLogic(BotOwner)` | `public static object GetActiveLayer(BotOwner botOwner)`；`public static object GetActiveLogic(BotOwner botOwner)`（BigBrain 1.5.0） | `[OK]` | 无 | `object activeLayer = BrainManager.GetActiveLayer(botOwnerObject);` |
| 30 | BotInfo.cs:332,367 | `DrakiaXYZ.BigBrain.Brains.CustomLayer.GetName()` / `.BuildDebugText(StringBuilder)` | `public abstract class CustomLayer`；`public abstract string GetName()`；`public virtual void BuildDebugText(StringBuilder)` | `[OK]` | 无 | `(activeLayer as CustomLayer)?.BuildDebugText(sb); customLayer.GetName()` |
| 31 | BotInfo.cs:352-354 | `DrakiaXYZ.BigBrain.Brains.CustomLogic.BuildDebugText(StringBuilder)` | `public abstract class CustomLogic : CustomLogic<CustomLayer.ActionData>`；`BuildDebugText` 为 `CustomLogic<T>` 的 `public virtual void`（被继承） | `[OK]` | 无 | `if (activeLogic is CustomLogic cl) cl.BuildDebugText(sb);` |
| 32 | BotInfo.cs:369-371 | `BaseLogicLayerSimple.Name()` | **EFT 类型**（全局命名空间）：`public abstract class BaseLogicLayerSimple : AICoreLayer<BotLogicDecision>`；`Name()` 继承自 `AICoreLayer<T>.Name() : string`（`public abstract string Name()`）；另有 `public abstract class BaseLogicLayer : BaseLogicLayerSimple` | `[OK]` | 无（但需引用 Assembly-CSharp，已引用） | `else if (activeLayer is BaseLogicLayerSimple logicLayer) AppendLabeledValue(…, logicLayer.Name(), …)`；`using EFT;` 不覆盖该类型（全局命名空间，直接可见） |

### 1.4 `Components\BotDebugComponent.cs`

| # | 上游位置 | 目标（类型.成员） | 0.16.9 实测 | 状态 | 适配/降级 | 一行实现指引 |
|---|---|---|---|---|---|---|
| 33 | BotDebugComponent.cs:43 | `Singleton<IBotGame>.Instance.BotsController.BotSpawner` | `Comfort.Common.Singleton<T>`（`static T Instance` / `static bool Instantiated`）；`IBotGame.BotsController : BotsController`（`IBotGame` 全局命名空间）；`EFT.BotsController.BotSpawner : BotSpawner` | `[OK]` | 无 | `botSpawner = Singleton<IBotGame>.Instance.BotsController.BotSpawner;` |
| 34 | BotDebugComponent.cs:44-45 | `Singleton<GameWorld>.Instance` / `GameWorld.MainPlayer` | `static T Instance`；`EFT.GameWorld.MainPlayer : Player`（公共字段） | `[OK]` | 无 | `gameWorld = Singleton<GameWorld>.Instance; localPlayer = gameWorld.MainPlayer;` |
| 35 | BotDebugComponent.cs:50 | `EFT.CameraControl.CameraManager.Instance.SSAA.isActiveAndEnabled` | `public static CameraManager Instance => instance ?? (instance = new CameraManager());`（**永不为 null**）；`public SSAA SSAA => _sSAA;`（实例属性，`_sSAA` 为公共字段 `SSAA _sSAA`）；`SSAA : MonoBehaviour`（继承 `Behaviour.isActiveAndEnabled`） | `[OK]`（类型/成员）/ 运行时空值 `[UNCERTAIN]` | 必须空判 `SSAA`（相机装配前 `_sSAA == null`）；建议包 try/catch | `var ssaa = CameraManager.Instance?.SSAA; if (ssaa != null && ssaa.isActiveAndEnabled) { … }` |
| 36 | BotDebugComponent.cs:52 | `SSAA.GetOutputWidth()` / `SSAA.GetInputWidth()` | `SSAA`（`Unity.Postprocessing.Runtime.dll`，全局命名空间）：`public int GetOutputWidth()` / `public int GetInputWidth()` | `[OK]` | 无 | `screenScale = (float)ssaa.GetOutputWidth() / ssaa.GetInputWidth();`（分母 > 0 由引擎保证，仍建议兜底） |
| 37 | BotDebugComponent.cs:106 | `GameWorld.AllAlivePlayersList` 遍历 | `public readonly List<Player> AllAlivePlayersList`（托管 `System.Collections.Generic.List`） | `[OK]` | 无 | `foreach (Player player in gameWorld.AllAlivePlayersList)` 原样可用 |
| 38 | BotDebugComponent.cs:108 | `BotSpawner.GetBotDebugData(localPlayer, player.ProfileId)` | 见 #5 | `[OK]` | 无 | 每帧节流（1s）后逐玩家取数，`:109` `botMap` 以 `ProfileId` 为键 |
| 39 | BotDebugComponent.cs:150-155 | `DebugBotStruct.InitedBotData` / `.PlayerOwner` / `PlayerOwner.AIData` / `aiData.BotOwner` | `InitedBotData` 为属性（`=> BotData.Inited`）；其余同 #9/#10/#11 | `[OK]` | 无 | 未 init 跳过；`aiData?.BotOwner == null` 视为死亡待清理 |
| 40 | BotDebugComponent.cs:179-181 | `IObserverToPlayerBridge.iPlayer.Position` / `Camera.main.WorldToScreenPoint` | 同 #19；`UnityEngine.Camera.main : Camera`（static）；`Vector3 WorldToScreenPoint(Vector3)` | `[OK]` | 无 | 头顶 +1.5f 世界坐标 → 屏幕坐标，`z > 0` 才绘制 |
| 41 | BotDebugComponent.cs:197-205 | `Mathf.RoundToInt` / `GUIStyle.CalcSize(GUIContent)` / `Screen.height` / `GUI.Box(Rect,GUIContent,GUIStyle)` | Unity IMGUI 托管 API | `[OK]` | 无 | 按 `screenScale` 补偿后计算 `GuiRect`，`GUI.Box` 绘制 |
| 42 | BotDebugComponent.cs:56,62 | `ConfigEntry<>.SettingChanged` 订阅 / `Destroy(this)` | `ConfigEntry<T>.SettingChanged`（BepInEx 托管事件）；`UnityEngine.Object.Destroy(Object)` | `[OK]` | 无 | 字号变更回调重建 `GUIStyle`；宿主是 `GameWorld.gameObject`，**只能 `Destroy(this)`**，禁用路径勿 `Destroy(gameObject)` |
| 43 | BotDebugComponent.cs:229-239 | `Disable()`/`Enable()`：`Singleton<IBotGame>.Instantiated` / `gameWorld.gameObject.AddComponent<BotDebugComponent>()` | `static bool Instantiated`；`GameObject.AddComponent<T>() where T : Component` | `[OK]` | 无 | 入战局时在 `GameWorld.gameObject` 挂组件；`GetComponent` 去重 |
| 44 | BotDebugComponent.cs:242-249 | `Disable()`：`GetComponent<BotDebugComponent>()?.Dispose()` | 同 #43 | `[OK]` | 无 | `Dispose()` 内 `Destroy(this)` |

### 1.5 `Helpers\Settings.cs` / `EnumExtension.cs` / `VersionChecker\TarkovVersion.cs`（Bot 覆盖层配置面）

| # | 上游位置 | 目标（类型.成员） | 0.16.9 实测 | 状态 | 适配/降级 | 一行实现指引 |
|---|---|---|---|---|---|---|
| 45 | Settings.cs:12-17 | `ConfigEntry<bool>` / `<BotInfoMode>` / `<KeyboardShortcut>` / `<int>` | BepInEx 5 托管配置类型均存在 | `[OK]` | 无 | `BotInfoMode` 枚举可直接作配置项（Mono + ConfigurationManager 原生下拉，无 IL2CPP ComboBox 崩溃问题） |
| 46 | Settings.cs:36-46 | `new KeyboardShortcut(KeyCode.F10/F9)` | BepInEx `KeyboardShortcut(KeyCode, params KeyCode[])`；`KeyCode` 来自 UnityEngine | `[OK]` | 无（上游 F9 与 DebugTooltip 冲突 → 本工程统一 F10/F11，见 spec） | 默认 NextModeKey=F10 / PrevModeKey=F11（按 spec 调整） |
| 47 | Settings.cs:48-58 | `ConfigDescription` + `AcceptableValueRange<int>(0,2000)` / `(12,36)` | `AcceptableValueRange<T>` | `[OK]` | 无 | `MaxDrawDistance`(1500) / `FontSize`(24) 用范围滑块 |
| 48 | Settings.cs:64 | `Singleton<IBotGame>.Instantiated` | 同 #33 | `[OK]` | 无 | 设置变更时按 `Instantiated` 决定是否挂/卸组件 |
| 49 | EnumExtension.cs:11-33 | `Next<T>()` / `Previous<T>()`（`Enum.GetValues` / `Array.IndexOf<T>`） | 纯托管 BCL | `[OK]` | 无 | 原样保留（可加 `using System.Linq` 清理） |
| 50 | TarkovVersion.cs:11,45-47 | `[assembly:TarkovVersion(40743)]` / `CheckEftVersion` / `FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FilePrivatePart` | 构建值 **40743 == 目标 FilePrivatePart**，门禁匹配 | `[OK]` | 本工程按 spec 改为「版本不符仅 LogWarning，不拒绝加载」；门禁自身 fail-open | 读 `EscapeFromTarkov.exe` FilePrivatePart 比对常量，异常吞掉并告警 |

---

## 2. 风险与适配（非 API 签名类）

### 2.1 命名空间归属（决定 `using` 面）

- **`EFT` 命名空间**：`Player` / `BotOwner` / `GameWorld` / `BotSpawner` / `BotsController` / `IObserverToPlayerBridge`（证据 E5 输出 `namespace EFT;`）；`CameraManager` 在 `EFT.CameraControl`。
- **全局命名空间（无命名空间）**：`DebugBotStruct` / `DebugBotDataStructInner` / `DebugHeathsDataStructInner` / `IAIData` / `EPlayerState` / `IBotGame` / `BotEnemiesController` / `EnemyInfo` / `BaseLogicLayerSimple` / `BaseLogicLayer` / `AICoreLayer<T>` / `SSAA`。对这些类型写 `EFT.` 限定名会编译失败。
- 上游 `using EFT;` 对全局命名空间类型无影响，二者均可直接引用；移植时不要臆造 `EFT.DebugBotStruct` 之类的限定名。

### 2.2 `DebugBotStruct` 为值类型

- 0.16.9 是 `public struct DebugBotStruct`（真实值类型），`botMap` 存 `DebugBotStruct` 字段；与 5.0 线 IL2CPP 代理类语义不同。上游写法在其上成立（`botData.SetData(data)` 值拷贝）。
- `PlayerOwner` 是引用字段（`IObserverToPlayerBridge`），`ProfileId` 是 `=> PlayerOwner?.iPlayer.ProfileId` 的属性，**可能为 null**；`Special` 模式对其做 `Length`/`Substring` 前必须短路。

### 2.3 `CameraManager.SSAA` 空值

- `CameraManager.Instance` 永不为 null（懒构造），但 `SSAA => _sSAA` 依赖相机装配（`_sSAA = Camera.GetComponent<SSAA>()`）。组件在 `OnGameStarted` 时挂载，理论上相机已就绪；但 DLSS/FSR 关闭或时序异常下 `_sSAA` 可能为 null → 上游无空判会 NRE。**移植必须空判**（见 #35 指引）。

### 2.4 组件宿主

- `BotDebugComponent` 挂在 `Singleton<GameWorld>.Instance.gameObject` 上；禁用/清理路径只能 `Destroy(this)`，绝不能 `Destroy(gameObject)`（会拆掉 GameWorld 宿主）。

### 2.5 Mono 语义红利

- 无 IL2CPP 代理层：`as`/`is` 类型判定、`System.Collections.Generic` 字典遍历、LINQ、`foreach` 均按托管原生语义工作；上游 1.8.0 的写法**几乎逐行可移植**，无需 5.0 线的 `TryCast`/手写索引循环/枚举配置降级等适配。

---

## 3. 证据附录

> 每条给出命令 + 关键输出摘录（均来自 §0.2 目标程序集，除注明者）。E1–E20 为常规核实；§4 为二次独立核实。

**E1 — GetBotDebugData 签名**
```powershell
ilspycmd -t "EFT.BotSpawner" Assembly-CSharp.dll
```
```
public DebugBotStruct GetBotDebugData(IPlayer currentPlayer, string profileId)
public DebugBotSpawnStruct GetDebugSpawnsData()
```

**E2 — DebugBotStruct 为 struct，字段/属性分离**
```powershell
ilspycmd -t "DebugBotStruct" Assembly-CSharp.dll
```
```
public struct DebugBotStruct
public DebugBotDataStructInner BotData;        // 字段
public DebugHeathsDataStructInner HeathsData;  // 字段
public IObserverToPlayerBridge PlayerOwner;    // 字段
public bool InitedBotData => BotData.Inited;   // 属性
public string ProfileId => PlayerOwner?.iPlayer.ProfileId;  // 属性
public EPlayerState PlayerState => BotData.PlayerState;
```

**E3 — DebugBotDataStructInner 消费字段全量存在**
```powershell
ilspycmd -t "DebugBotDataStructInner" Assembly-CSharp.dll
```
```
string Name / StrategyName / LayerName / NodeName / Reason / PrevNodeName / PrevNodeExitReason / CustomData
int Ammo / HitsOnMe / ShootsOnMe / CoverIndex / BotRole
bool Reloading / IsInSpawnWeapon / HaveAxeEnemy
EPlayerState PlayerState
```

**E4 — DebugHeathsDataStructInner 健康字段全量存在**
```powershell
ilspycmd -t "DebugHeathsDataStructInner" Assembly-CSharp.dll
```
```
int Health{Head,Body,Stomach,LeftArm,RightArm,LeftLeg,RightLeg}
bool Health*BL ; bool Health*Broken   （全部命中）
float Energy / Hydration ; EDebugBotEffect Effects
```

**E5 — IObserverToPlayerBridge 成员（`namespace EFT;`）**
```powershell
ilspycmd -t "EFT.IObserverToPlayerBridge" Assembly-CSharp.dll
```
```
IPlayer iPlayer { get; } ; IAIData AIData { get; } ; string Nickname { get; }
EPlayerState CurrentStataName { get; } ; bool IsAI { get; }
```

**E6 — IAIData.BotOwner（全局命名空间）**
```powershell
ilspycmd -t "IAIData" Assembly-CSharp.dll
```
```
public interface IAIData
BotOwner BotOwner { get; set; }
Player Player { get; }
```

**E7 — BotOwner / BotEnemiesController / EnemyInfo**
```powershell
ilspycmd -t "BotOwner" Assembly-CSharp.dll ; ilspycmd -t "BotEnemiesController" … ; ilspycmd -t "EnemyInfo" …
```
```
public class BotOwner : MonoBehaviour, IPlayer
public BotEnemiesController EnemiesController { get; set; }
public BotWeaponManager WeaponManager { get; set; }
public BotMemory Memory;                              // 公共字段
public Player GetPlayer { get; set; }
BotEnemiesController.EnemyInfos : Dictionary<IPlayer, EnemyInfo>
BotEnemiesController.HavePursuitableEnemy : bool
EnemyInfo.Person : IPlayer ; EnemyInfo.ProfileId : string ; EnemyInfo.Nickname : string
```

**E8 — 战斗数据链**
```powershell
ilspycmd -t "BotWeaponManager" … ; ilspycmd -t "BotWeaponSelector" … ; ilspycmd -t "BotMalfunctionData" … ; ilspycmd -t "BotMemory" …
```
```
BotWeaponManager: BotWeaponSelector Selector { get; set; } ; BotMalfunctionData Malfunctions { get; set; }
BotWeaponSelector: bool IsWeaponReady { get; set; }
BotMalfunctionData: bool HaveMalfunction()
BotMemory: EnemyInfo GoalEnemy { get; }
```

**E9 — Player 弹药链**
```powershell
ilspycmd -t "EFT.Player" Assembly-CSharp.dll ; ilspycmd -t "EFT.InventoryLogic.Weapon" …
```
```
public class Player : MonoBehaviour, IPlayer, …
public string ProfileId => Profile.Id;
public virtual AbstractHandsController HandsController
public BifacialTransform Transform => PlayerBones.BodyTransform;
public class FirearmController : ItemHandsController, …  →  public new Weapon Item
Weapon.ChamberAmmoCount : int ; int GetCurrentMagazineCount()
```

**E10 — 补丁目标与玩家集合**
```powershell
ilspycmd -t "EFT.GameWorld" Assembly-CSharp.dll
```
```
public abstract class GameWorld : MonoBehaviour, …
public virtual void OnGameStarted()
public Player MainPlayer;
public readonly List<Player> AllAlivePlayersList
```

**E11 — Singleton / IBotGame / BotsController**
```powershell
ilspycmd -t "Comfort.Common.Singleton`1" Comfort.dll ; ilspycmd -t "IBotGame" Assembly-CSharp.dll ; ilspycmd -t "EFT.BotsController" …
```
```
Singleton<T>: static T Instance ; static bool Instantiated
public interface IBotGame  →  BotsController BotsController { get; }
public class BotsController  →  public BotSpawner BotSpawner { get; }
```

**E12 — CameraManager / SSAA**
```powershell
ilspycmd -t "EFT.CameraControl.CameraManager" Assembly-CSharp.dll
ilspycmd -t "SSAA" "…\Managed\Unity.Postprocessing.Runtime.dll"
```
```
CameraManager: namespace EFT.CameraControl ; public static CameraManager Instance => …
CameraManager: public SSAA SSAA => _sSAA;   public SSAA _sSAA;
SSAA: public class SSAA : MonoBehaviour   （全局命名空间）
SSAA: public int GetInputWidth() ; public int GetOutputWidth()
```

**E13 — BigBrain 1.5.0 版本与 BrainManager**
```powershell
ilspycmd -t "DrakiaXYZ.BigBrain.BigBrainPlugin" DrakiaXYZ-BigBrain.dll
ilspycmd -t "DrakiaXYZ.BigBrain.Brains.BrainManager" …
```
```
[BepInPlugin("xyz.drakia.bigbrain", "DrakiaXYZ-BigBrain", "1.5.0")]
public static object GetActiveLayer(BotOwner botOwner)
public static object GetActiveLogic(BotOwner botOwner)
```

**E14 — BigBrain CustomLayer / CustomLogic**
```powershell
ilspycmd -t "DrakiaXYZ.BigBrain.Brains.CustomLayer" … ; ilspycmd -t "DrakiaXYZ.BigBrain.Brains.CustomLogic" …
```
```
public abstract class CustomLayer
public abstract string GetName();  public virtual void BuildDebugText(StringBuilder)
public abstract class CustomLogic : CustomLogic<CustomLayer.ActionData>
CustomLogic<T>: public virtual void BuildDebugText(StringBuilder)
```

**E15 — BaseLogicLayerSimple 归属（关键纠正）**
```powershell
# BigBrain DLL 内字符串扫描：仅出现类型名，无 TypeDef
[regex]::Matches(ascii(BigBrain.dll), "[A-Za-z0-9_]*BaseLogic[A-Za-z0-9_]*")  → BaseLogicLayerSimple, BaseLogicLayer
ilspycmd -t "DrakiaXYZ.BigBrain.Brains.BaseLogicLayerSimple" BigBrain.dll    → Could not find a type named …
# 归属到游戏程序集（全局命名空间）
ilspycmd -l c Assembly-CSharp.dll | grep BaseLogic  → Class BaseLogicLayer / Class BaseLogicLayerSimple
ilspycmd -t "BaseLogicLayerSimple" Assembly-CSharp.dll
ilspycmd -t "AICoreLayer`1" Assembly-CSharp.dll
```
```
public abstract class BaseLogicLayerSimple : AICoreLayer<BotLogicDecision>
public abstract class BaseLogicLayer : BaseLogicLayerSimple
AICoreLayer<T>: public abstract string Name();
```

**E16 — SPT 反射补丁基类**
```powershell
ilspycmd -t "SPT.Reflection.Patching.ModulePatch" spt-reflection.dll ; ilspycmd -t "SPT.Reflection.Patching.PatchPrefixAttribute" …
```
```
public abstract class ModulePatch   → public void Enable() / protected abstract MethodBase GetTargetMethod()
public class PatchPrefixAttribute : Attribute
```

**E17 — 游戏版本门禁**
```powershell
(Get-Item "…\SPT_41x\EscapeFromTarkov.exe").VersionInfo
```
```
FileVersion 0.16.9.40743 ; FilePrivatePart 40743
```

---

## 4. 二次独立核实记录（≥5）

> 均为对同一触点的**独立命令复核**（换用类型清单 `-l` 或换入口重新反编译）。

| V | 触点 | 独立命令 | 结果 |
|---|---|---|---|
| V1 | `BaseLogicLayerSimple` 类型存在性 | `ilspycmd -l c Assembly-CSharp.dll \| grep BaseLogicLayerSimple` | `Class BaseLogicLayerSimple`（与 E15 交叉一致） |
| V2 | `BotSpawner.GetBotDebugData` 签名与返回类型 | 重新 `ilspycmd -t "EFT.BotSpawner"` + `ilspycmd -t "DebugBotStruct"` | `public DebugBotStruct GetBotDebugData(IPlayer, string)`；`public struct DebugBotStruct` |
| V3 | BigBrain `GetActiveLayer`/`GetActiveLogic` | 重新 `ilspycmd -t "…BrainManager"` | 两方法均 `public static object` |
| V4 | `CustomLayer.GetName()` / `BuildDebugText` | 重新 `ilspycmd -t "…CustomLayer"` | `public abstract string GetName()`；`public virtual void BuildDebugText(StringBuilder)` |
| V5 | `CustomLogic<T>.BuildDebugText` | `ilspycmd -t "…CustomLogic`1"` | `public virtual void BuildDebugText(StringBuilder)` |
| V6 | `BotEnemiesController` / `EnemyInfo` / `BotsController` 归属 | `ilspycmd -l c Assembly-CSharp.dll`（类清单） | 三者均列出；`EnemyInfo` / `BotEnemiesController` 为全局命名空间，`EFT.BotsController` 在 `EFT` |

---

## 5. 遗留与下一步

- **无 `[MISSING]` / `[CHANGED]`**：BotOverlay 段全部触点类型与签名在 0.16.9.40743 存活，无需降级；上游 1.8.0 与目标构建版本号精确一致是主因。
- 待**运行时**确认（本工单不启动游戏）：
  1. `CameraManager.Instance.SSAA` 在 DLSS/FSR 关闭或相机装配前是否为空（决定 #35 空判的实际触发频率）。
  2. BigBrain 激活层对象在自定义层场景下是否确为 `CustomLayer` 实例（而非仅包装器），以确认 `#30` 分支命中。
  3. `DebugBotStruct.ProfileId`（`=> PlayerOwner?.iPlayer.ProfileId`）在 `PlayerOwner` 为 null 时短路（#16）——已给出空判指引，运行期验证告警是否出现。
- 供下游引用：本表 #5–#32 为 BotOverlay 实现的核心触点（数据源、`DebugBotStruct` 字段、BigBrain 两模式），#33–#44 为组件装配，均可按「一行实现指引」直接落地。

--- END OF TERMINAL ENTRY ---
