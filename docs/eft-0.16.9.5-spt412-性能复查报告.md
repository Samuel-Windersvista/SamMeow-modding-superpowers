# EFT 0.16.9.5 (SPT 4.1.2) 客户端性能复查报告 — PerformanceTweaks 可移植性评估

> 日期：2026-08-19 | 对象：`E:\Game\EFT_Offline\SPT_410\...\Assembly-CSharp.dll`（15.47MB，4.1.2 已反混淆）
> 方法：ILSpy 11.0 全量反编译（8620 文件 / 35MB / 零错误）→ 12 个补丁目标逐一与 3.11.4 版对照核实
> 缓存：`external/decompile-cache/eft-0.16.9.5-spt412/`（gitignore）
> 姊妹文档：`docs/eft-0.16-性能分析与优化mod可行性报告.md`（3.11.4 分析 + 兼容性审计）

---

## 0. 总结论

**BSG 什么都没修。** 12 个病灶在 4.1.2 全部存活，逻辑与 3.11.4 逐字节等价——4.1 的差异只是反混淆改名 + 少量方法重命名，没有任何一处性能逻辑改进。

**PerformanceTweaks 可整体移植**：12 个补丁的宿主类型/签名在 4.1.2 均存在（仅改名），无结构性障碍。移植工作量主要是"换名字"，不是"重新设计"。

## 1. 病灶存活对照（12/12）

| # | 补丁 | 4.1.2 状态 | 4.1.2 位置 |
|---|---|---|---|
| P1 | 感知距离修剪 | 仍存在（结构微调：HashSet\<BodyPartType\> + CalculatePartVisibility，仍无距离修剪） | EnemyInfo.cs:493 |
| P2 | 感知调度放宽 | 仍存在（构造器参数逐行相同：0.1f/10/0.6f） | AITaskManager.cs:180-184 |
| P3 | 睡眠 bot 跳过感知 | 仍存在（UpdateLook 守卫无 StandBy 检查，注销仅在 Dispose） | BotOwner.cs:1024-1030, LookSensor.cs:307 |
| P4 | 寻路冷却 | 仍存在（每次 new NavMeshPath + 同步 CalculatePath，逐字节相同） | BotMover.cs:457-474 |
| P5 | 初始感知距离门限 | 仍存在（onActivation 初始登记仍无距离上限） | EFT/BotMemory.cs:609 |
| P6 | 相机重复设置去重 | 仍存在（SetCamera 无同相机短路，Streamer.Update 每帧调用） | CameraControl/CameraManager.cs:386, Streamer.cs:135 |
| P7 | 远距 bot 分帧降频 | 仍存在（每帧 foreach 全部 bot） | BotsList.cs:271 |
| P8 | 灯光闪烁分帧 | 仍存在（FlickerSystem/LightFlicker 逐字节一致） | EFT/Visual/ |
| P9 | 玩家 ID 索引化 | 仍存在（int 查询仍 O(n) 全扫） | GameWorld.cs:907/921 |
| P10 | 环境管理器降频 | 仍存在（每帧 shadowDistance 赋值） | EnvironmentManager.cs:222-230 |
| P11 | 绊线预热 | 仍存在（懒加载同步 Resources.Load） | Player.cs:26489-26501 |
| P12 | 弹道可见性缓存 | 仍存在（每观察玩家每帧 O(活跃子弹)） | BasePlayerCulling.cs:81-93 |

## 2. 改名映射表（移植时直接查）

| 3.11.4（混淆） | 4.1.2（真名） |
|---|---|
| `AITaskManager.Class279` | `AITaskManager.AIRegularTaskGroup` |
| `GClass589`（lookAll） | `LookAllData` |
| `BotMemoryClass` | `EFT.BotMemory` |
| `BotSettingsClass` | `BotGroupEnemyInfo` |
| `CameraClass` | `EFT.CameraControl.CameraManager` |
| `BotsClass` | `BotsList` |
| `GClass895.Class558.method_2` | `BasePlayerCulling.CullingStateToggle.CheckBallistic` |
| `GClass359.CanShoot` | `AIUtility.CanShoot` |
| `method_2`（EnvironmentManager） | `GetLongShadowCorrectionFactor` |
| `hashSet_0` / `hashSet_1`（BotsList） | `_botOwners` / `_loggedErrors` |
| `action_1`（CameraManager） | `_onCameraChanged` |

## 3. 移植注意点

1. **P1 需要小重写**：4.1.2 的部位容器从 `Dictionary<EnemyPart, EnemyPartData>` 换成 `HashSet<BodyPartType>` + 数组索引，若补丁触碰部位结构需适配；只做"距离超阈值则跳过整个 CheckLookEnemy"则不受影响
2. **目标框架不同**：4.1 客户端 mod 用 netstandard2.1（BepInEx 5，与 3.11 的 net471 不同），csproj 与引用路径需按 `templates/client-mod/` 调整
3. **兼容性审计需在 4.1 版本重做**：SAIN/QB 等的 4.x 版 patch 目标可能与 3.11 版不同（工作区已有 Moew-SAIN-For-4013 可审）；methodology 直接复用 `client-mod-compat-audit-playbook.md`
4. **4.1 是反混淆真名**：补丁代码可读性大幅提升，TargetMethod 可用 `nameof`，维护成本低于 3.11 版
5. **建议路径**：不要双版本共源码硬撑——复制 `mods/PerformanceTweaks/` 为 `mods/PerformanceTweaks412/`，按映射表改名 + 换框架，独立演进

## 4. 决策建议

- 若迁移主线（ticket #9）推进到"60 客户端插件游戏内验证"阶段，PerformanceTweaks412 可作为其中一员同批验证
- 也可以等 3.11.4 版实战测试期结束、参数定版后再移植，避免双线调参
- 收益预期参考 3.11 实测：平均 FPS +20%（灯塔），4.1.2 病灶相同，预期同量级

---

# 第二部分：扩大审计 + 议会复审（2026-08-19 v2）

> 方法：四路侦察（GC/Unity API/每帧系统/AI物理，聚焦"12 项之外的新问题"）→ 多模型议会复审裁决。
> 议会出席：gamma（完整响应）+ 主持人补证定案；alpha/beta 超时。置信度：多数意见 + 硬证据定案。

## 5. 扩大审计新发现（四路汇总）

| # | 新发现 | 位置 | 侦察评级 |
|---|---|---|---|
| A | 观察玩家管线固定 255 槽：每帧 255 次 ContainsKey + [255,64] 快照矩阵 + [255,512] 命令矩阵（13 万结构体驻留）+ 主线程 16,320 次 bool 读取 | ObservedPlayerMessageJobs.cs:42-71、ObservedPlayerMessageReceiver.cs:124-176 | 高（后被议会推翻，见 §6） |
| B | 烟雾体素遮挡挂进每条 LOS 射线（4.1 新增，无烟零成本短路，有烟叠加射线风暴） | EnemyPartVision.cs:182、BotsSmokesVisionSystem.cs:122-146 | 中 |
| C | CommandMessageAutoSender 每帧轮询 + async 双跑 | CommandMessageAutoSender.cs:258-295 | 中（后被推翻，同 A） |
| D | 散点：WinterScript 每帧 Camera.main（:447）；HairRenderer 每帧 GetComponent（:524）；BTR 生成同步 Resources.Load×3（BtrController.cs:374）；BackblastModel JobHandle 轮询；PlayerMotor 生产路径 OnGUI；BotLocalAvoidance 同体素 O(k²)（4.1 新增）；BaseLightSystem 空遍历；ObservedPlayer spawn 静默失败无重试；Job 内 Commands[256] 无边界写 | 各文件 | 中低 |
| E | 共识：内存纪律持平略好（每帧分配点 146→149）；弹道/寻路/物理同步无变化；UI 纪律良好；NextObservedPlayer 工程质量高但带 255 槽硬编码 | — | — |

## 6. 议会裁决（关键翻转）

1. **A/C 在 SPT 单机不激活 —— 侦察"高"评级推翻**：ObservedPlayer 管线唯一构造点 NetworkGame.cs:567，LocalGame 全文无引用；离线局 bot 是完整 LocalPlayer 实例，allObservedPlayersByID 为空。→ **A 转为 Fika 专用补丁立项（单机零成本），C 为死代码排除**
2. **P12 单机确认激活，维持"高"**（议会最重要翻转）：`LocalPlayer.cs:89-93` 给每个离线 bot 挂 `OfflinePlayerCulling : BasePlayerCulling`，每帧 ManualUpdate 遍历全部活跃子弹——单机成本 O(bot × 活跃子弹)/帧。议员"单机 O(0)"的怀疑被主持人补证证伪
3. **"1% low 不变"指向卡顿类病灶**：3.11 补丁全是每帧类优化 → 剩余病根是同步加载/尖峰类（BTR、绊线、NavMesh 加载、烟雾尖峰、LocalAvoidance 平方项）——这批在 412 版整体升格
4. P2/P5（调 AI 参数有行为影响）降为**默认关闭的可选开关**

## 7. 终版移植清单（议会裁决后）

| 批次 | 内容 |
|---|---|
| **第一批：直接改名移植（10 项）** | P1、P3、P4、P6、P7、P8、P9、P10、P11、P12（改名表见 §2；KB `Class_Name_Mappings` 可直接用） |
| **第二批：重写（1 项）** | P1+B 统一拦截：距离门限前置到 IsRayIntersectAnySmoke 之前，无烟短路 + 有烟先按距剔除，单点覆盖 3.11 热点与 4.1 新增调用点 |
| **第三批：新纳入（3 项）** | BotLocalAvoidance 距离预筛/空间哈希；BTR 生成资源异步化（治 1% low）；HairRenderer/WinterScript 一行级缓存 |
| **排除（5 项）** | A（转 Fika 补丁单独立项）、C（死代码）、P2/P5（默认关可选开关）、Commands[256] 越界（转上游 bug 报告渠道） |

## 8. 议会指出的盲区（下一轮侦察清单）

1. SPT/Harmony 补丁层自身开销（多 mod 叠 prefix 后原版零成本路径不再零成本）
2. FastAnimatorSystem：20-40 bot 的 Animator 每帧求值 + IK LOD 分级（四路恰好绕开）
3. NavMesh 切片同步加载/carving（1% low 家族）
4. BetterAudio 音频遮挡的增长曲线（SPT 历史敏感区）
5. 任务系统每帧轮询 + PerfectCulling 覆盖盲区定点抽查

## 9. 终版结论

4.1.2 的性能账 = **3.11 的 12 项全存活 + 4.1 新增 1 项实质加重（B 烟雾）+ 1 项新平方项（LocalAvoidance）+ 若干散点**。议会纠偏后，移植清单为"10 项改名 + 1 项重写 + 3 项新纳入"，排除 5 项。A 项（255 槽）是 Fika 联机环境的高价值补丁候选，但与 SPT 单机无关，单独建档不混入主线。
