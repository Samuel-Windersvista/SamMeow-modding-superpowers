# EFT 0.16 (SPT 3.11.4) 客户端性能与缺陷分析报告

> 分析对象: `E:\Game\EFT_Offline\SPT_3114\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`（14.54 MB，SPT 已补丁版）
> 分析方法: ILSpy 11.0 全量反编译（8340 个 .cs / 33.4 MB），4 路并行只读侦察 + 证据链核实；另反编译 spt-core / spt-singleplayer 提取官方 patch 目标清单
> 反编译缓存: `external/decompile-cache/eft-0.16-spt3114/`（本体）、`external/decompile-cache/spt-3.11.4-plugins/`（官方插件）；已 gitignore，可用 ilspycmd 重新生成
> 18 个方法因 ILSpy bug 未反编译成功（均为后端占位类，不影响分析）
> 日期: 2026-08-16（初版）；2026-08-16 v2 补充兼容性侦察（§7）；2026-08-17 v3 §7.3 升级为 9 仓源码实证审计
> **2026-08-19 实施状态：P1-P12 已落地为 `mods/PerformanceTweaks/`（v0.2.0，已部署），灯塔实测平均 FPS +20%（75.2→90.4），当前处于用户实战测试期。续作入口：`mods/PerformanceTweaks/STATUS.md`**

---

## 0. 总体结论（先看这个）

EFT 0.16 的客户端代码**并非普遍低质**——BSG 已经做了不少正确的性能工程：

- 自定义 PlayerLoop 只做轻量事件分发（`CustomPlayerLoopSystemsInjector`）
- 网络观察玩家插值已 Burst Job 化（GClass2781/2782）
- FastAnimator 有可见性剔除（GClass1269 Culler）
- 弹道命中检测用 RaycastNonAlloc、OverlapBox 大部分已 Job 化/异步化
- 每帧 Update 体内几乎无 LINQ、无集合/数组分配、无装箱
- 物理同步已改为手动（`Physics.simulationMode = Script`，关闭 autoSyncTransforms）
- BSG 甚至自己写了 `GClass849` 反模式检测器监控 FindObjectsOfType 滥用

**真正的问题集中在三处，且在 SPT 离线环境下被放大**：

1. **Bot AI 链是全客户端驱动的最大帧率杀手**（离线局所有 bot 由本机跑）
2. **每帧固定开销系统缺乏降频/错峰**（25 个 bot 子系统串行、三重玩家遍历、灯光闪烁系统）
3. **若干数据结构反模式**（O(n) 字典全扫、睡眠 bot 感知不注销、感知射线无距离修剪）

---

## 1. GC 分配与内存 — 评级: B+（健康，少量残留）

### 正面证据
- 静态预分配池（`Player.cs:11662` `_preallocatedAmmoList` 等）
- 每帧 Update 体内 **0 处** `new List/Dictionary/数组`
- 非泛型集合全树仅 3 处（装箱基本绝迹）
- 结构体捕获闭包（`<>8__locals`）规避堆分配——EFT 刻意的 GC 纪律
- 已有 NonAlloc API 家族（`GClass3176.GetItemComponentsInChildrenNonAlloc`）

### 残留问题
| 位置 | 问题 | 严重度 |
|---|---|---|
| `EFT/Player.cs:21302-21331` | 装备观察器 `GClass1828<T>.Update()` 每帧 LINQ（夜视/热成像/面罩），代码库已有 NonAlloc 版本却没用 | 中 |
| `Player.cs:28695` / `MovementContext.cs:2487` | 每帧 `ManualUpdate` 体内 `Debug.LogErrorFormat`（float 装箱），错误门控但每帧判断 | 低-中 |
| `ClientPlayer.cs:200` | 网络序列化循环内 LogErrorFormat，2×int 装箱 | 低 |
| 全树 1965 处 `Debug.Log*` | 多在事件路径；事件密集场景（交火/击杀）会累积字符串分配，Release 无级别门控审计 | 中 |
| `GameWorld.method_10(delegate)` 每帧委托 | 反编译无法确认编译器是否缓存为静态委托，需 IL 验证 | 待定 |

---

## 2. Unity API 反模式 — 评级: B（大多已治理，少数每帧泄漏）

| 模式 | 统计 | 结论 |
|---|---|---|
| FindObjectOfType 家族 | 89 处，仅 1 处在回调内（调试类） | 低危，GClass849 审计层已监控 |
| GetComponent 在每帧回调 | 1707 处总数中仅 20 处在 Update 体内，多数带脏检查 | 中低 |
| **Camera.main** | 24 处，其中 `Streamer.cs:133`、`ArmorDummy.cs:89-94`、`WinterScript.cs:434-439` **每帧 1-3 次 tag 扫描** | **中高** |
| Physics 设置 | autoSyncTransforms 已关闭，自定义 Script 模拟驱动 | 低（正确做法） |
| OnGUI (IMGUI) | 22 个，21 个是调试类；**`PlayerMotor.cs:135` 是生产路径** | 中 |
| Resources.Load 同步 | 56 处；**`Player.cs:26214` 绊线放置时同步加载+实例化**（放绊线瞬间卡顿） | 中 |
| 协程 | 326 处 StartCoroutine，多为单次/节流型 | 中低 |
| SendMessage | 7 处，全在第三方库（RootMotion/FinalIK） | 低 |
| `QualitySettings.shadowDistance` | `EnvironmentManager.cs:279` **Update 内每帧写全局属性** | 中低 |
| `Application.targetFrameRate` | Awake 硬编码 60，局内 -1 + Reflex 联动（GameGraphicsClass.cs:501-518） | 合理 |

---

## 3. 每帧执行系统 — 评级: C（主要问题区）

### 3.1 Bot AI 每帧链（P0，SPT 最大杀手）
```
BaseLocalGame.Update → UpdateByUnity 事件
 └─ BotsController.method_0 (BotsController.cs:300-307)
    ├─ AICoreController.Update()      所有 AI agent 决策
    ├─ AiTaskManager.Update()         感知任务分帧调度
    └─ Bots.UpdateByUnity() (BotsClass.cs:273-290)
       └─ foreach 全部活跃 BotOwner → UpdateManual()
```
- `BotOwner.UpdateManual`（BotOwner.cs:1014-1073）：**每 bot 每帧串行约 25 个子系统**（StandBy/LookSensor/SuppressShoot/ShootData/Tilt/NightVision/NearDoorData/DogFight/FriendChecker/Mover/AimingManager/Medecine/Boss/BotTalk/WeaponManager/BotRequestController/GrenadeToPortal/Tactic/Memory/WarnData/ArtilleryDangerPlace...）
- **仅 2 处节流**：CalcGoal 3.3s（CoreBotSettingsClass.cs:78）、PreActive 出生检查 1s
- 无按距离/ID 帧摊分，N 个 bot = 单帧 N 倍全量成本
- 休眠机制存在（BotStandBy：>130m 睡眠、<110m 激活、10s 检查间隔），**但有漏洞**：LookSensor.ManualUpdate 在 paused 判断之前执行，且睡眠 bot 的感知任务不从 AITaskManager 注销（只在 Dispose 注销）——**睡眠 bot 仍周期性做视线检查**

### 3.2 感知射线风暴（P0 并列）
- AITaskManager 分帧调度存在（LookSensor 组参数 0.1s 周期/10 任务/0.6s 上限），但单次感知成本高
- `EnemyInfo.CheckLookEnemy`（EnemyInfo.cs:494-571）：对每个记忆敌人做多部位 Linecast（远=1 部位、中=8、近=6）+ 可见时 2 条 Physics.Raycast（CheckCanShoot）
- **敌人名单只随死亡移除，无距离修剪**——玩家被全图 bot 记住后，感知射线直到玩家死亡才停止
- 可见时 `new GClass564(...)`（EnemyInfo.cs:560）GC 分配

### 3.3 同步 NavMesh 寻路（P0 并列）
- Bot **不用 NavMeshAgent**，移动 = `NavMesh.CalculatePath`（**同步、主线程、CPU 密集 A\***）
- `BotMover.CalcPath`（BotMover.cs:415-432）：每次 `new NavMeshPath()` + CalculatePath
- 决策 node 频繁触发 GoToPoint → 目标一变就主线程寻路
- 部分缓存存在（BotRun._cachePath static 等）但不系统

### 3.4 GameWorld 三重玩家遍历（P1）
- `GameWorld.method_10`（GameWorld.cs:2085-2098）：O(n) 遍历 AllAlivePlayersList，**每 tick 被调用 3 次**（PlayerTick/AfterPlayerTick/DoOtherWorldTick），每个玩家带 try/catch

### 3.5 弹道系统（P1）
- 每帧全量子弹 tick（BallisticsCalculator.cs:146-236），O(活跃子弹)
- **每颗活跃子弹每帧 2~10 条物理查询**：正向 Linecast + 反向 LinecastPrecise 最多 8 次 RaycastNonAlloc + UseSpiritPlayer（SPT 默认开）再 1 条
- 霰弹一枪 N 弹丸 = N 颗活跃子弹；SPT 多 bot 交火时线性放大
- **观察玩家 culling 每帧扫描全部激活子弹**（GClass895.cs:80-92）：O(子弹 × 观察玩家)

### 3.6 灯光闪烁系统（P2）
- `ComponentSystem<T,TS>` 框架（ComponentSystem.cs:26-62）：注册即每帧更新，无节流无距离剔除
- `LightFlicker.ManualUpdate`（LightFlicker.cs:19-23）：**每帧对每盏闪烁灯执行 PerlinNoise/AnimationCurve.Evaluate + intensity 写入**
- 同类子系统：MuzzleSystem、WeaponOverHeatSystem、LampSystem、DeferredDecalRenderer

### 3.7 其他每帧 O(n)
- SpeakerManager：每帧 foreach 全部说话者（SpeakerManager.cs:84-90）
- 观察玩家动画师：只有 cullingMode 切换，无按距离降频；IK 隔 3 帧是唯一显式错峰
- 全库仅 3 处显式帧交错（%2/%3/%20）——EFT 几乎不做降频设计
- 100+ 处 `+= Time.deltaTime` 累加器，游戏侧（bot 计时/炮击/测距）本该间隔化

### 3.8 数据结构反模式（P2）
- `GameWorld.TryGetAlivePlayer/TryGetObservedPlayer`（GameWorld.cs:1141-1194）：**O(n) 字典全扫**做 ID 查找，被弹道命中（GClass1397.cs:91）、BTR 炮塔等高频路径调用
- `BotDoorsController` 每帧遍历全部门

---

## 4. 已知缺陷模式（bug 面）

| 模式 | 位置 | 影响 |
|---|---|---|
| AI agent 静默死亡 | `AICoreControllerClass.cs:49-60` catch 后把失败 agent 丢进 hashSet 永久跳过，无恢复 | bot 决策可能悄悄停摆 |
| 卡死 bot 每帧重试 | `BotsClass.cs:277-287` catch 后仅记录 Id，异常 bot 留在遍历列表 | 每帧异常吞没+性能浪费 |
| 睡眠 bot 感知不注销 | BotOwner.cs:1021 paused 判断在 LookSensor 之后；LookSensor.cs:374 只在 Dispose 注销 | 睡眠 bot 仍耗感知射线 |
| EnemyInfo 无距离修剪 | BotMemoryClass.cs:787-790 只随死亡移除 | 感知射线随时间单调增长 |
| 绊线同步资源加载 | Player.cs:26214 Resources.Load + Instantiate | 放置瞬间卡顿 |
| 每帧 shadowDistance 写 | EnvironmentManager.cs:279 | 轻微全局属性写放大 |
| 生产路径 IMGUI | PlayerMotor.cs:135 OnGUI | 每帧 IMGUI 多 pass |

---

## 5. 优化 Mod 可行性评估（BepInEx + Harmony，SPT 3.11 客户端）

按 **收益/风险比** 排序：

### Tier 1 — 高收益、低风险（优先做）
| 补丁 | 目标 | 方式 | 预期收益 |
|---|---|---|---|
| Bot 感知距离修剪 | `EnemyInfo.CheckLookEnemy` | Prefix：Distance > VisibleDist 跳过部位检查 | 消灭全图射线风暴，大战场显著 |
| LookSensor 组参数放宽 | AITaskManager 构造的 LookSensor 组 (0.1s→0.25s+) | 构造器 postfix 改参数 | 感知开销减半以上 |
| 睡眠 bot 注销感知 | `BotOwner.UpdateManual` paused 分支 | 挂起 LookSensor 任务 | 睡眠 bot 零感知成本 |
| 寻路冷却 | `BotMover.CalcPath` | 0.5s 重算冷却 + 目标位移阈值 | 主线程 A* 尖峰消除 |
| 感知记忆距离门限 | `BotMemoryClass.AddEnemy` | >100m 不记录敌人 | 源头减少 EnemyInfo 膨胀 |

### Tier 2 — 中收益、低风险
| 补丁 | 目标 | 方式 |
|---|---|---|
| Camera.main 缓存 | Streamer.Update / ArmorDummy / WinterScript | Prefix 缓存相机引用 |
| Bot 分帧轮转 | `BotsClass.UpdateByUnity` | 按索引轮转，每帧只更新 N 个 bot（注意：会改变 bot 反应速度手感，需可配置） |
| 灯光闪烁节流 | LightFlicker.ManualUpdate | 隔 N 帧 + 相机距离剔除 |
| O(n) ID 查找索引化 | GameWorld.TryGetAlivePlayer 等 | 建立 Id→Player 反向索引字典 |
| shadowDistance 脏检查 | EnvironmentManager.Update | 值未变跳过赋值 |
| 绊线预加载 | Player.cs:26214 | 预热缓存 tripwire_planner prefab |
| 子弹 culling 快照 | GClass895.method_2 | 每 N 帧缓存 ActiveShots 快照 |
| GClass1828 NonAlloc 化 | 装备观察器 | 换用已有 NonAlloc API |

### Tier 3 — 高收益、中风险（需谨慎验证）
| 补丁 | 目标 | 风险 |
|---|---|---|
| 远距子弹降采样 tick | BallisticsCalculator.method_2 | 不改变命中判定，只降视觉更新率；需充分测试 |
| method_10 三重遍历合并 | GameWorld | 触碰核心 tick 链，需 IL 验证委托缓存问题 |
| MovementContext OverlapBox 降频 | 每 bot 每帧 2 次 → 隔帧 | 改变碰撞行为，不建议 |
| LinecastPrecise 精化次数 | 8 次 → 更少 | 改变穿透/弹道行为，**不建议动** |

### 不建议动的部分
- 网络插值 job 链（已 Burst 化，动了只可能更糟）
- FastAnimator Culler（已正确）
- 物理同步模式（已正确）
- GPUInstancer（GPU 侧，托管无问题）

---

## 6. 建议的实施路径

1. **先做 Tier 1 五个补丁**，打包为单一 BepInEx 客户端 mod（如 `SamMeow.PerformanceTweaks`），每项可独立开关（BepInEx ConfigurationManager）
2. 用 `testing-spt-modpack` Level B 标准验证：服务器启动 → 进主菜单 → 战局冒烟测试
3. 用性能计数对比（如 FPS/帧时间，bot 数量递增场景：工厂 vs 街区）量化收益
4. Tier 2 逐项追加；Tier 3 需要单独测试周期
5. 实施时走 `writing-spt-mod` skill 的客户端 mod 模板（BepInEx/Harmony）

> 注意：SPT 4.x 的 Assembly-CSharp 与 3.11 差异较大，本报告仅适用于 3.11.4（EFT 0.16）。若未来升级 SPT 版本需重新分析。

---

## 7. 兼容性侦察：与其他 mod 的相互作用（v2 补充）

### 7.1 当前现场盘点（E:\Game\EFT_Offline\SPT_3114）

- **server mod：0 个**
- **client DLL：仅 7 个**（ConfigurationManager + spt-common/core/custom/debugging/reflection/singleplayer），即裸 SPT 官方运行时，无第三方 mod
- 因此当前环境下本优化 mod **无现实冲突对象**；以下为前瞻性兼容性分析

### 7.2 与 SPT 官方运行时的目标重叠（实证：反编译 spt-core / spt-singleplayer 提取全部 patch 目标）

官方 patch 目标共 40 个（方法级），与 §5 全部补丁目标交叉比对结果：

| 重叠目标 | 官方 patch | 形态 | 对我们的影响 |
|---|---|---|---|
| `BotOwner.UpdateManual` | `RemoveStopwatchAllocationsEveryBotFramePatch`（spt-singleplayer，Patches.Performance 命名空间） | **Transpiler**，NOP 掉固定 IL 索引（12-18、110-112）的 Stopwatch 分配 | 我们计划中的"睡眠 bot 注销感知"也打该方法。**必须用 Prefix/Postfix，禁止 Transpiler**——Transpiler 叠加会因 SPT 硬编码索引产生不可预期结果。Prefix/Postfix 包裹在方法体外，与 IL 级 Transpiler 天然正交，可共存 |
| 其余 39 个目标（GameWorld.OnGameStarted、Player.Init、Player.OnMakingShot、BotsController.SetSettings 等） | 各类 RaidFix/ScavMode/修复 patch | Prefix/Postfix 为主 | **与我们的补丁目标零重叠** |

另注意：spt-singleplayer 已自带 `Patches.Performance` 命名空间（目前仅 2 个 patch：BotOwner.UpdateManual 与 CoverPointMaster.method_0 的 Stopwatch 分配移除）。说明 SPT 官方认可这条优化路线，但覆盖面极小，我们的 mod 是补全而非重复。

### 7.3 与生态 mod 的前瞻分析（v3：已升级为工作区源码实证审计）

> 2026-08-17 对 `E:\云文件\GitHub` 内用户自用的特制源码项目逐仓完成 patch 目标审计（双通道 grep + 碰撞点读码）。结论先行：**与本 mod 13 个补丁目标零真冲突**。

| Mod（本地源码仓） | GUID | patch 规模 | 与本 mod 碰撞 | 结论 |
|---|---|---|---|---|
| **SAIN**（Moew-SAIN-For-3114） | `me.sol.sain` | 21+ 目标 | 零方法级碰撞（不碰 CheckLookEnemy/UpdateLook/CalcPath/AddEnemy 语义；其 AddEnemy patch 仅 null 守卫） | 可共存，互补 |
| **Looting Bots**（Moew-LootingBot-For-3114） | `me.skwizzy.lootingbots` | 5 目标 | 零碰撞（只碰 BotOwner.Dispose/Deactivate、GameWorld.Dispose、LocalGame.Stop） | 可共存 |
| **Questing Bots**（SamMeow-QuestingBots） | `com.danw.questingbots` | ~36 目标 | **唯一碰撞：EnemyInfo.CheckLookEnemy 双 prefix**（QB 对睡眠 bot 做 SetVisible(false)）。条件不相交，但顺序敏感——**已修：本 mod P1 前缀设为 Priority.Low，保证 QB 先执行**。另注意 QB 硬性不兼容 AIDisabler/AILimit/Phobos，且 QB 自带 AI Limiter 与本 mod 领域重叠但语义互补 | 可共存（已消除顺序敏感） |
| **Realism**（LIN-Realism-Mod-Client） | `RealismMod` | 194 类/197 方法 | 仅 GameWorld.OnGameStarted 双 postfix（语义独立：它加载音频/区域，我们预热绊线）；其弹道全替换 patch（BallisticsCalculator.CreateShot）与未来 T3 子弹降采样相邻不同方法 | 可共存 |
| **That's Lit**（SamMeow-ThatsLit-For-3114） | `bastudio.thatslit` | 6 活跃 | 零方法级碰撞。**行为注意项**：它经 CheckPartLineOfSight 的 addSensorDistance 给光照视距补偿（可达百米级），本 mod P1 按 VisibleDist×1.5 裁剪可能抵消其远距补偿——同装时建议把 P1 视距倍率调到 2.0+ | 可共存（配置建议） |
| **SWAG+Donuts**（SamMeow-SWAG+Donuts） | `com.dvize.Donuts` | 6 目标 | BotMemoryClass.AddEnemy、OnGameStarted 双 prefix 链式共存无害。**注意：该仓 Donuts 1.4.4 面向 SPT 3.8，与 3.11.4 不匹配，需先升级**；其局末整方法替换会绕过 UnregisterPlayer——本 mod P9 索引已有脏检查重建兜底 | 可共存（版本需升级） |
| **ProgressiveBotSystem**（Moew-ProgressiveBotSystem） | 纯 server | 0 | 零交集（HTTP 路由层） | 可共存 |
| **BotPlacementSystem**（Moew-botplacementsystem） | `com.acidphantasm.botplacementsystem` | 11 目标 | GameWorld.UnregisterPlayer 双 postfix（它只回传 Boss 追踪数据，已读码确认正交）；其 despawn 走 UnregisterPlayer 会正常喂本 mod 索引 | 可共存 |

### 7.4 冲突形态结论（引用 KB 冲突报告方法学）

KB `curated/operations/3114-il-conflict-report.md`（108 DLL / 776 patch 实测）确立的规律：

1. **事件订阅型碰撞可共存**（Harmony 2 链式执行，Prefix 按序跑）
2. **Transpiler 叠加才是真正危险**（后 patch 者看到前 patch 者的 IL，最后生效者遮蔽前者）

据此定本 mod 的 **兼容性设计原则**：

1. **全部补丁用 Prefix/Postfix，禁止 Transpiler**（唯一例外是确需 IL 级时，先查 IL 冲突数据库）
2. **每个补丁独立配置开关**（BepInEx Config，F12 可调），出问题时用户可单点关闭而非卸载整个 mod
3. **软依赖检测**：启动时扫描 BepInEx 插件列表并记录日志（已实现 SAIN 文件级检测）；经 SAIN 源码审计确认零冲突后，当前版本**不做自动禁用**——若未来发现新的重叠 mod 再启用降级逻辑
4. **不改公开 API、不改数据结构、不序列化任何状态**——纯行为节流，加载/卸载均无副作用

> SAIN 兼容性审计记录（2026-08-16）：审计对象 `E:\云文件\GitHub\Moew-SAIN-For-3114`（SAIN for 3.11.4，net471，AssemblyName `SAIN`，GUID `me.sol.sain`）。方法：全源码 grep patch 目标（typeof + 字符串双通道），逐目标与本 mod 五补丁交叉比对。结论：零目标级冲突，无需互斥逻辑。

### 7.5 前置依赖问题（直接回答）

- **我们的 mod 需要的前置**：仅 BepInEx 5.x + 0Harmony + spt-common/spt-core 程序集（编译期引用）。**SPT 安装自带全部这些，无任何第三方 mod 前置**
- **其他 mod 是否需要把我们作前置**：**不需要**。本 mod 不提供 API、不改变任何其他 mod 依赖的行为契约，是纯增量优化
- **版本锁定**：本 mod 强绑定 EFT 0.16 / SPT 3.11.x 的混淆名与 IL 结构（尤其 BotOwner.UpdateManual 周边）。SPT 4.x 必须重新分析（反编译缓存与报告均需重建）

---

## 附：反编译缓存维护

| 路径 | 内容 | 再生成命令 |
|---|---|---|
| `external/decompile-cache/eft-0.16-spt3114/` | Assembly-CSharp.dll 全量反编译（8340 文件） | `ilspycmd -p --nested-directories -o <target> "E:\Game\EFT_Offline\SPT_3114\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"` |
| `external/decompile-cache/spt-3.11.4-plugins/` | spt-core / spt-singleplayer 反编译（patch 目标清单来源） | 对 `BepInEx\plugins\spt\*.dll` 逐 DLL 执行 `ilspycmd -p -o <target> <dll>` |

> 缓存已加入 .gitignore（派生产物，33MB+，可随时再生成，不入库）。
