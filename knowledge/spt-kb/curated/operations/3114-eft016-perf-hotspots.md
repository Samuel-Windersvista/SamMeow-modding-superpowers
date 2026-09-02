---
version: [3.11]
domain: client
topic: performance
source: curated
---
# EFT 0.16 (SPT 3.11.4) 客户端性能热点地图（反编译实证，2026-08-16）

> 状态：已验证（ILSpy 11.0 全量反编译 Assembly-CSharp.dll，8340 文件，4 路侦察 + 逐点读码核实）
> 完整报告：`docs/eft-0.16-性能分析与优化mod可行性报告.md`；反编译缓存：`external/decompile-cache/eft-0.16-spt3114/`
> 产出 mod：`mods/PerformanceTweaks/`（P1-P12 已实现，v0.2.0）

## 反直觉总结论

EFT 0.16 每帧代码纪律**良好**：Update 体内 0 处集合/数组分配、装箱基本绝迹、静态对象池、
结构体闭包、网络插值已 Burst Job 化、FastAnimator 有可见性剔除、物理已手动同步
（simulationMode=Script、关 autoSyncTransforms）。BSG 自带 `GClass849` FindObjectsOfType
滥用检测器。**性能问题集中在 AI 链与少数未降频系统，SPT 离线局（bot 全客户端驱动）放大。**

## 实证热点（按收益排序）

| # | 热点 | 位置（反编译树） | 机制 |
|---|---|---|---|
| 1 | 感知射线风暴 | EnemyInfo.cs:494-571 CheckLookEnemy | 每 bot 对记忆敌人做多部位 Linecast（远1/中8/近6 部位）+可见时 2 条 Raycast；敌人名单只随死亡移除，无距离修剪 |
| 2 | Bot 全量串行更新 | EFT/BotOwner.cs:1014-1073 UpdateManual；BotsClass.cs:273-290 | 每 bot 每帧 ~25 子系统，仅 CalcGoal(3.3s)/出生检查(1s) 两处节流，无分帧 |
| 3 | 同步主线程寻路 | BotMover.cs:415-432 CalcPath | bot 不用 NavMeshAgent；目标一变即 NavMesh.CalculatePath + new NavMeshPath |
| 4 | 睡眠 bot 感知漏洞 | BotOwner.cs:1021（LookSensor 在 paused 判断前执行）；LookSensor.cs:374（任务只在 Dispose 注销） | 睡眠 bot 仍周期性做视线射线 |
| 5 | 弹道每帧物理查询 | EFT/Ballistics/BallisticsCalculator.cs:146-236 | 每活跃子弹每帧 2-10 条查询（正向 Linecast + LinecastPrecise 最多 8 次 + UseSpiritPlayer 1 条） |
| 6 | 观察玩家×子弹交叉扫描 | GClass895.cs:80-92 Class558.method_2 | 每观察玩家每帧 O(活跃子弹)，经 GClass897/GClass2762 每帧驱动 |
| 7 | GameWorld 三重遍历 + O(n) ID 查找 | GameWorld.cs:2085-2098 method_10（每 tick ×3）；:1141-1194 TryGetAlive/ObservedPlayer 字典全扫 | ID 查找被弹道命中等高频路径调用 |
| 8 | Streamer 每帧相机重置 | Streamer.cs:133 → CameraClass.SetCamera（CameraClass.cs:559-582 无同相机短路，内部 Release/Reset/委托累积） | 每帧全量重置 + 委托泄漏 |
| 9 | 灯光闪烁无节流 | EFT/Visual/LightFlicker.cs:19-23；派发链 FlickerSystem→ComponentSystem | 每帧每灯 PerlinNoise/Curve.Evaluate |
| 10 | EnvironmentManager 每帧赋值 | EFT/EnvironmentEffect/EnvironmentManager.cs:275-283 | 每帧 QualitySettings.shadowDistance 赋值 + SmoothDamp（SmoothDamp 依赖每帧推进，不能整帧抠） |
| 11 | 绊线同步加载 | EFT/Player.cs:26214 CreatePlantPlanner | 首次放置 Resources.Load+Instantiate，有懒加载守卫，可预热 |
| 12 | 每帧日志装箱残留 | Player.cs:28695、MovementContext.cs:2487、ClientPlayer.cs:200 | LogErrorFormat 在 tick 路径，错误门控但每帧判断 |

## 已知缺陷（bug 面）

- AICoreControllerClass.cs:49-60：agent 异常后进 hashSet 永久静默死亡，无恢复
- BotsClass.cs:277-287：异常 bot 留在遍历列表每帧重试（hashSet_1 只做日志去重）
- 全库仅 3 处显式帧交错（%2/%3/%20），100+ 处 `+= Time.deltaTime` 累加器本可降频

## 派发链关键事实（打补丁前必查）

- Flicker 不是 ComponentSystem 直连：`Flicker.OnEnable → GClass841.RegisterInSystem → Singleton<GInterface32<Flicker>> → FlickerSystem(ComponentSystem<Flicker,FlickerSystem>).UpdateComponent → callvirt ManualUpdate`
- LookSensor 的贵活不在 ManualUpdate（仅草穿透标志），在 AITaskManager 组调度的 `UpdateLook→CheckAllEnemies`；组参数 `new Class279(0.1f, 10, 0.6f)`（AITaskManager.cs:180-184），Class279 字段 readonly 只能整体换实例
- GameWorld 玩家字典 key 是 string ProfileId，int Id 查询全扫；GetAlivePlayerByProfileID 已是 O(1)
- GClass1828<T> 装备观察器是 Player 嵌套类、事件驱动（OnItemAddedOrRemoved），**非每帧热点**——初版侦察误判，读码后从补丁清单剔除

## 关联

- 完整报告与兼容性审计：`docs/eft-0.16-性能分析与优化mod可行性报告.md`（§7 为 9 仓源码审计）
- 审计方法学：`curated/operations/client-mod-compat-audit-playbook.md`
- 构建/反编译坑：`curated/operations/3114-client-mod-build-gotchas.md`
