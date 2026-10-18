# 02: 触点核验：4.1.6 实装审计（BB+DO）与修复

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** 对 4.1.6 Managed 实装做系统性触点核验（ilspycmd 反编译核实；方法沿用包内 pilot 经验），覆盖 BB 与 DO 两侧全部关键触点——命中链（BallisticCollider.ApplyHit）、交互菜单（InteractionContextHelper.GetAvailableActions）、门交互执行（StartInteraction / ExecuteDoorInteraction）、踹门（Door.KickOpen）、破门角度（IsBreachAngle）、门状态机（EDoorState 与 SetDoorState）、门体碰撞/遮挡（Portal）、PerfectCulling 防护点，以及 DO 侧破坏/视觉/物理触点。产出按模块分文件的触点核实表（`[OK]/[CHANGED]/[MISSING]` 逐项带证据），修复全部 `[CHANGED]/[MISSING]` 或记录降级方案。这是 DO 并入（03）前的技术闸门。

**Blocked by:** 01（工程奠基：合并基座与 BB 独立基线）

**Status:** ready-for-agent

- [x] 触点核实表产出（按模块分文件，逐项标注状态与反编译证据出处）：`mods/SamMeow-DoorBreachOverhaul/docs/api-touchpoints/{bb-breach,do-destruction,do-interactions}.md`
- [x] 全部 `[CHANGED]/[MISSING]` 完成修复，或在偏差表记录降级方案并经 Overseer 知情 —— 审计结论：**无 CHANGED/MISSING**；BB 侧三处 4.1 迁移绑定缺陷已在工单 01 修复并经运行时验证
- [x] 构建 0 error、MO2 加载无异常；日志无 `Failed to create patch` —— 构建 0 warning/0 error；战局实测日志：5 补丁 `Enabled patch`、零 patch 错误、零本插件异常
- [x] spec 风险面章节「触点核验第一批」条目闭环更新（已回填 spec.md）
- [x] WTT-ClientCommonLib 是否为编译必需依赖得到定论并记录：**非编译必需**（BB 与 DO 源码均 0 引用，无 `BepInDependency` 需求）

**上游参考（本仓归档）:** KB `curated/migration/pilot-experience-spt41-debugtoolkit.md`（触点闸门方法论）、`curated/migration/client-mod-311-to-41.md`；`docs/adr/0012`（上游对账义务）。

## Comments

- 2026-10-18（审计完成）：ilspycmd 对 4.1.6 `Assembly-CSharp.dll` 逐项反编译核验，BB 侧 5 补丁目标 + DO 侧 16 处补丁目标/高风险成员 **全部 [OK]**，无 [CHANGED]/[MISSING]。产物三表：
  - `docs/api-touchpoints/bb-breach.md`：5 补丁目标（ApplyHit 单重载 / GetAvailableActions 约 20 重载显式选择 / OnGameStarted / PerfectCulling.Toggle+字段真名）+ 门族/容器/容器类成员 + 绑定修复复核。
  - `docs/api-touchpoints/do-destruction.md`：KickOpen(Vector3,bool) / Grenade.Explosion（静态 8 参）/ CheckOcclusionPortal / OnGameStarted / 守卫三补丁；Effects._dict（private 字段存在）/ OpenAngle / _interaction（protected）/ InteractionState.ProgressCurve+Duration。
  - `docs/api-touchpoints/do-interactions.md`：GetAvailableActions / UpdateCommand(float) / InitializeSmoothOpenInteraction(EDoorState,bool) / PlaySound(EDoorState,float) / StateIsSuitableForHandInput 属性 / PlayerBones.ShiftWeaponRoot（全局类、12 参单方法）/ OnDestroy / EPlayerState.BreachDoor；InputKeyCombination.GameKey/InputKeys 成员。
  - 关键澄清：`PlayerBones` 为全局命名空间（非 `EFT.PlayerBones`）；`InputKeyCombination` 在 `EFT.InputSystem`；`EPlayerState` 为全局枚举；`KeycardDoor : Door`。
  - **WTT 定论**：BB 与 DO 源码对 `WTT|ClientCommonLib` 均 0 命中 → **非编译必需依赖**，不声明 `BepInDependency`（Forge 标注为虚）。
  - DO 侧证据补充：包内 `DoorOverhaul.dll`（131072 B，构建于 2026-10-13，meta `version=d2026.10.13.0`）为同源 v1.0.0 的 4.1.6 官方构建 → 源级编译兼容性有上游产物背书；合并编译在 03 首次执行（最终闸门）。
- 2026-10-18（环境观察）：`[8]门的大修-DoorOverhaul` 覆盖层当前为**禁用**状态（modlist 第 69 行为 `-`，2026-10-18 01:21 由 MO2 侧重写；同批次其它预设开关亦有调整）。本 mod 的两轮战局实测因此是**单独生效**（无 DO 干扰，基线证据更干净）。不影响源级审计；03 以源码快照为并入基准。
- 遗留行为注记（非触点缺失，已记入 `bb-breach.md` §4）：`IsValidDoorLockHit` 的本地空间比较存在坐标系疑点（碰撞体局部 vs 门局部），待合规弹药（12/70 AP-20）实测定性；`ApplyHit` 为 virtual，未来版本若出现子类覆写需扩大补丁面。
