# 03: 破坏并入与上锁门豁免移除

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** 把 Door Overhaul 的破坏系统并入同一插件（以 DO v1.0.0 源码为基准，`external/references/door-overhaul`）：破坏注册与构建、命中入线、弹孔/断片/碎片视觉、铰链与门闩损伤、门板脱落/接管（含实验开关）、调试面板（命中标记/门信息/破坏图谱）、战局装配与清理；同时**移除上锁门豁免**——`locked: untouchable` 不再生效，上锁门成为完整攻击目标。双轨语义生效：同一命中同时喂锁血池（BB）与破坏模拟（DO），互不阻塞、不吞伤害。游戏内可演示：对上锁门持续射击，门板逐步破洞/掉件、锁耐久同步消耗——「打锁开门」与「打烂门板」两条路径都可达成。保留 DO 既有工程性排除与守卫补丁现状（开关化改造留 06）。

**Blocked by:** 02（触点核验：4.1.6 实装审计（BB+DO）与修复）

**Status:** ready-for-agent

- [x] 破坏系统装配完整：注册/构建、命中入线、视觉（孔洞/碎片/门板）、调试面板、清理闭环
- [x] 上锁门豁免移除（代码侧：`DoorDamageIntake` 两处 `EDoorState.Locked` 拦截删除，全目录 0 残留；游戏内回归待 S3）
- [x] 双轨各自消费同一命中（单一 `ApplyHit` postfix：先派发破坏（任意射手），后走 BB 门禁；两段独立 try/catch，旁路不吞伤害）
- [x] 游戏内：解锁门打孔/断片正常；**上锁门**同样可打孔、锁耐久可消耗至踹门开门（2026-10-19 Overseer 实测通过）
- [x] 铰链实验开关：悬挂/脱落行为符合上游预期（含默认值语义）（2026-10-19 实测通过：单铰 FAILED → `leaf Hanging`；继续射击 → 整扇脱框；上游实验粗糙度见 README 偏差表第 14 行）
- [x] 进出战局无残留、日志无异常；双状态机交叠的已知边界记录进偏差表（2026-10-19 Overseer 实测通过；BepInEx 日志 0 异常）
- [x] LICENSE/THIRD-PARTY-NOTICES 补 BSD-3（Sage）署名

**上游参考（本仓归档）:** `external/references/door-overhaul`（v1.0.0，BSD-3）；豁免点：`Destruction/DoorDamageIntake.cs` 的 Locked 分支（158/218 行注记见 `research.md`）。

## Comments

- 2026-10-18（实现完成，代码侧）：@fixer 后台作业（fix-2）按工作单并入 DO v1.0.0 破坏系统。
  - 复制 23 个 DO 源文件至 `src\DoorOverhaul\`（Destruction 10 + Core 8 + Patches 4 + Doors\DoorMath 1），新增 `DestructionCleanupPatch`；保持 `DoorOverhaul.*` 命名空间便于上游对账。未复制 `DestructionRaidStartPatch`（并入 `OnGameStartedPatch`）、交互侧与 DO 的 Settings/Plugin。
  - 接缝改造：① 唯一命中分派 = `ApplyHit` postfix 先 `DispatchDestruction`（`ColliderMap` 映射查询、任意射手、独立 try/catch）后走 BB 门禁；② `DoorDestructionRegistry` 以 `ColliderMap` + `RegisterCollider/UnregisterCollider` 取代 `OnHitAction` 订阅（`DoorTakeover` 同步改造，全目录 `OnHitAction` 0 残留）；③ 战局装配单点（RefreshConfigSnapshot → Intake.Clear → 条件 Registry.Build → 两组件 Enable）；④ 战局结束清理新补丁（GameWorld.OnDestroy）；⑤ 宿主 `DoorBreachOverhaul` GameObject + `DestructionController`（Awake 创建 / OnDestroy 销毁）。
  - 配置面：节 `3. 破坏` 9 项 + 节 `7. 调试` 3 项（public static 实时读值；键名英文、显示名中文）。
  - 上锁门豁免：`DoorDamageIntake` 命中拦截块与 `ApplyBlast` 守卫删除，`EDoorState.Locked` 在 `DoorOverhaul\` 0 残留。
  - 编排者复验（新鲜证据）：`dotnet build -c Release -t:Rebuild` 0 warning / 0 error（DLL 148,992 B）；标准检查 PASS=13 / FAIL=0 / WAIVED=1；12 补丁注册（5 BB + 7 破坏）；`DoorOverhaulPlugin`/`OnHitAction` 0 残留；git 状态仅两个工程目录（无越界写入）。
  - 文档：THIRD-PARTY-NOTICES 补 BSD-3（Sage）全文与修改声明；README 状态/破坏能力/F12 3 与 7 节表/偏差表第 10–12 行更新。
- 待 Overseer（S3）：解锁门打孔/断片、上锁门破坏受理、铰链实验开关、调试面板、进出战局无残留。
- 2026-10-18（打包调整，Overseer 指令）：客户端与服务端合并为**单一 MO2 覆盖层** `[8]门的大修-破门大修-DoorBreachOverhaul`（`BepInEx\plugins\DoorBreachOverhaul\` + `SPT_Runtime\user\mods\DoorBreachOverhaulServer\` 两棵树）；原两个 `*-test-build` 覆盖层已删除。测试清单不变。
- 2026-10-18（实测反馈修复：Pleb/SemiPleb 与铰链实验互斥）：
  - 现象（Overseer 实测）：上锁门可破坏 ✓；但 Pleb 全放行下**任意命中都在积攒锁血池**（每发霰弹 ~69 伤害，2–3 发即开门），门在铰链被破坏前就被破门打开。
  - 修复（三处）：① `IsValidDoorLockHit` 统一到门局部空间比较（修正上游「碰撞体局部空间 vs 把手 localPosition」混用）并补空保护；② `HandleDoorDamage` 的 PlebMode 分支不再短路为 true，改为按锁区锚定判定（武器/弹药/材质/黑名单仍放行，仅保留锚定）；③ `CheckWeaponAndAmmo` 的 SemiPleb 分支同样保留锚定。
  - 语义结果：锁血池只吃门把手附近（0.25m）命中；打门板/铰链只喂破坏模拟 → 铰链实验可在门未破门前完成。
  - 复验：build 0 warning / 0 error；标准检查 PASS=13 / FAIL=0 / WAIVED=1；README 偏差表第 13 行 + F12 破门语义说明更新。
  - 待 Overseer 复测：Pleb + 铰链实验开启 → 对铰链侧持续射击，应先出现门扇悬挂/脱落而不是提前开门；对门把手射击仍应破门。
- 2026-10-19（Overseer 实测结果）：
  - 解锁门打孔/断片、上锁门可破坏、调试面板、进出战局无残留：**全部通过**。BepInEx 日志无异常，客户端 DLL 哈希与最新构建一致（`B647012…`，149,504 B）。
  - **铰链实验 FAIL**：对门铰持续射击后门扇未悬挂/未脱落，门仍正常开合。本轮 `Pleb=False`、`SemiPleb=False`、`Hanging And Falling Doors=true`、`Show Holes=true`、耐久 200–400（`overwrite\BepInEx\config\com.sammeow.doorbreach.cfg`）。
  - 待定位方向：① 锚点坐标是否落在实际铰链侧（`hingeX=0.03` 假设门轴在 bbox 最小边）；② 铰链碰撞体是否在 `ColliderMap` 内；③ 金属门 `HingeHealth=1500` + 非破门弹 `AnchorScale=1` 的伤害是否低到不可达。诊断用 F12 `7. 调试` 的 `Show Door Info` + `Show Damage Map` 读取 hinge 百分比。
  - 2026-10-19（诊断结论，工单 03 铰链项收口）：调试面板实测（`custom_Obshchezhitie_2_indoor_00007`，WoodThin，1.00×2.09×0.05 m，AK-105 PS）：36 发后 `Hinge (0.03,0.20)=44%`、`(0.03,1.89)=89%`、`Latch=53%`，`leaf Attached`；锚点位置与 damage map 青色点均在实际铰链侧，命中计数正常（`hits 36, ignored 0`）。继续射击后单铰 `FAILED` → `leaf Hanging`，再打另一铰 → 整扇脱框。结论：锚点坐标/碰撞体映射无缺陷，仅上游数值下需要持续集火；**沿用上游数值**，不引入未复测的平衡改动。上游实验粗糙度（面板 `Hanging` 与视觉已脱框短暂不一致）登记 README 偏差表第 14 行；铰链伤害倍数旋钮列入工单 05 候选。
