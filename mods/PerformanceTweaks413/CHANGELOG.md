# PerformanceTweaks413 更新日志

> 2026-08-21：工程由 PerformanceTweaks412 更名（GUID/程序集名/命名空间同步更换为 spt413 系），
> 目标版本线改为 SP-Tushonka fork 的 4.1.3（与 4.1.2 客户端二进制同构，见
> `knowledge/spt-kb/curated/operations/413-fork-transition.md`）。

## v0.3.0（2026-08-21）

**主题**：第二批（重写）+ 第三批（4.1 新病灶）落地。

### 新增补丁（4 个，累计 16）

| # | 补丁 | 内容 | 默认 |
|---|---|---|---|
| P13 | 烟雾射线距离门限 | 4.1 新增每条 AI 视线都查烟雾体素（BotsSmokesVisionSystem.IsRayIntersectAnySmoke）。超过距离阈值（默认 150m 可配）直接返回"无遮挡"，跳过体素遍历；无烟短路保留。有烟战局感知开销不升反降 | 开 |
| P14 | 本地AI的IK隔帧 | 移植 3.11 P13：40m 内每帧 / 40-80m 隔2帧 / 80m 外隔3帧；主玩家不受影响 | 开 |
| P15 | carving 同帧去重 | **读码后判定病灶不成立**（门已有 1s 节流+2m 判定、切割 300s 节流、火车事件驱动、Unity 同值赋值本就是 no-op）。已实现为防御性同帧去重，**默认关闭** | 关 |
| P16 | AI扎堆斥力分帧 | 移植 3.11 P14：拥挤（≥4）时隔帧，跳帧手动调 OffsetDecrease 保持衰减连续；4.1.2 的 NearDoor 短路保留 | 开 |

### 工程变更

- 4 个新补丁文件 + 配置 4 组（全中文人话版）
- csproj 补 UnityEngine.PhysicsModule / AIModule 引用
- 编译 0 错误 0 警告，release/SamMeow.PerformanceTweaks413.dll v0.3.0

### 验证状态

- [x] 编译通过
- [ ] 部署与 16/16 加载验证（由 Overseer 决定部署时机）
- [ ] 战局实测（重点：烟雾战局 P13、近战多 bot P14、扎堆场景 P16）

---

## v0.2.0（2026-08-21）

**主题**：第一批移植——3.11 版 12 补丁全部落地 4.1.x（10 项默认开 + 2 项默认关）。

### 前置：4.x 生态兼容性审计（开工第一步，已完成）

| 对象 | 结论 |
|---|---|
| SAIN 4.x（Moew-SAIN-For-4013，GUID `me.sol.sain`，面向 4.0.13，98 个 patch） | **零真冲突**，3.11 结论"只 patch 参数级方法、互补"在 4.x 依然成立。唯一同方法：BotMemory.AddEnemy 双 prefix（SAIN 是 null 守卫，与 P5 条件正交）→ P5 默认关闭 |
| QuestingBots 4.x（SamMeow-QuestingBots，`com.danw.questingbots`，面向 4.0.2，~39 patch） | 仍 patch `EnemyInfo.CheckLookEnemy`（睡眠 bot SetVisible(false) prefix）→ **P1 prefix 用 Priority.Low 让 QB 先执行**（同 3.11 版处置） |

### 落地补丁（12 项）

默认开（10）：P1 感知距离修剪 / P3 睡眠bot跳过感知 / P4 寻路冷却 / P6 相机重复设置去重 /
P7 远距bot分帧降频 / P8 灯光闪烁分帧 / P9 玩家查找索引化 / P10 环境管理器降频 /
P11 绊线规划器预热 / P12 弹道可见性查询降频。

默认关（2，调 AI 行为，谨慎开启）：P2 感知调度放宽 / P5 初始感知距离门限。

### 与 3.11 版的实质差异（移植时处理）

- 全部目标类型/字段按 4.1.2 反编译树逐个核实（真名真命名空间，TargetMethod 可用 nameof 的日子到了）
- 关键改名：`BotMemoryClass→EFT.BotMemory`、`botOwner_0→_owner`（P5）、`BotsClass→BotsList`（字段 `_botOwners/_loggedErrors`）、`CameraClass→CameraManager`、`AITaskManager.Class279→AIRegularTaskGroup`、`GClass895.Class558.method_2→BasePlayerCulling.CullingStateToggle.CheckBallistic`
- **映射表修正 1 处**：P12 的参数类型在 4.1.2 实为 `IBallisticsCalculator`（EFT.Ballistics），不是报告里的 `ISharedBallisticsCalculator`（已按代码实况修正并注释）
- 配置全中文人话版（同 3.11 风格），P2/P5 注明默认关闭原因

### 验证状态

- [x] 编译 0 错误 0 警告，release DLL v0.2.0 已产出
- [x] 已部署至 `SPT_410/BepInEx/plugins/`
- [ ] BepInEx 日志确认 12/12（下次启动游戏自查 LogOutput.log）
- [ ] 战局冒烟 + AMD CSV 实测基线（建议并入迁移主线客户端验证批次）

### 后续批次（见 docs/PerformanceTweaks413-实施计划.md）

- 第二批：P1+烟雾合并拦截（重写项，距离门限前置到 IsRayIntersectAnySmoke 之前）
- 第三批：IK 隔帧（参考 3.11 P13）/ carving 合批 / 扎堆斥力（参考 3.11 P14）
- 第四批：Impostors/CullingManager（实测驱动）
