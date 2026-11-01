# 04: 交互并入与菜单合并

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** 把 DO 的门交互全套并入：Peek（点按快速开缝 / 按住逐步开到全开）、Open Further、Open Quietly（幅度/缓速/音量可配）、体推门与静音开门音效、踢门持枪（就绪/朝向两开关）、交互键捕获与部分开合动画相关补丁；交互菜单从多补丁收敛为**单点注入并去重**——Peek / Open Further / Open Quietly 与 Plant Explosive 同菜单共存、不重复、不冲突。游戏内可演示：门交互菜单动作齐全且无重复，逐项可用（含锁定状态表现）。

**Blocked by:** 03（破坏并入与上锁门豁免移除）

**Status:** ready-for-human

- [x] 交互动作全套可用：Peek（点按/按住语义）、Open Further、Open Quietly（静开参数）、体推门、踢门持枪（2026-10-20 Overseer 实测 1-6 项通过）
- [x] 菜单单点注入 + 去重守卫：动作不重复；Plant Explosive 与其他动作共存（代码侧 `DoorMenuMerge` 单入口；普通门实测通过；**KeyCard 单项未测**）
- [x] 锁定门状态的菜单表现符合预期（无非法动作）（2026-10-20 实测第 8 项通过）
- [x] 与门状态随机化（包内覆盖层）共存初检：菜单不崩、行为合理（2026-10-20 实测第 9 项通过）
- [x] 日志无异常；进出战局清理正常（2026-10-20 实测第 1/10 项通过；17 补丁全部 Enabled，DoorBreach 无异常）
- [x] DO 交互侧参数（静开幅度/缓速/音量、推门强度、踢枪开关）暂按上游默认运行（4. 交互 节已提前旋钮化，默认值与上游一致；规则层正式化仍在 05）

## Comments

- 2026-10-20（工单 04 代码侧完成）：
  - 移植 12 个 DO 交互文件到 `src/DoorOverhaul/`：`Actions/{DoorActionKind,DoorActions}.cs`、`Doors/{DoorOpenOverrides,DoorPusher,DoorRuntime,PeekDriver}.cs`、`Helpers/InteractKey.cs`、`Patches/{InteractKeyCapture,KickWeaponForward,KickWeaponReady,PartialOpenSwing,QuietOpenSound}Patch.cs`；未复制 `AddDoorActionsPatch` / `RaidEndCleanupPatch`（分别并入菜单与清理）。
  - 菜单合并：新增 `Patches/DoorMenuMerge.cs` 单入口；`ActionMenuDoorPatch` 与 `ActionMenuKeyCardPatch` 共用。DO 动作按 `ModInteractionAction.Kind` 去重，C4 按动作名去重；KeyCard 重载保留去重语义。
  - 配置：`Settings.cs` 新增 `4. 交互` 11 项（键名/默认值/范围按上游），交互代码实时读值；README 配置表与能力列表同步。
  - 生命周期：`DoorRuntime` 挂到现有 `_host`（逐帧 Kick/Pusher/Peek）；`DestructionCleanupPatch` 追加 `PeekDriver.Cancel` / `DoorPusher.Clear` / `KickWeaponForwardPatch.Clear` / `DoorOpenOverrides.Clear`。
  - 工程：`DoorBreachOverhaul.csproj` 增加 `UnityEngine.InputLegacyModule` 引用（`InteractKey` 使用 `UnityEngine.Input`）。
  - 验证：`dotnet build -c Release -t:Rebuild` 0 warning / 0 error；标准检查 PASS=12 / FAIL=0 / WAIVED=2；补丁注册数 17。
  - 待 Overseer 实测：菜单动作齐全且无重复（含 KeyCard）；锁定门不出现非法动作；Peek 点按/按住、Open Further、Open Quietly、体推门、踢门持枪；与 DoorRandomizer 共存；进出战局无残留、日志无异常。
- 2026-10-20（@oracle 双轴审查，Verdict: PASS WITH CHANGES）：
  - **Must fix 已修**：`DoorPusher.TryPush` 首行补 `door == null` Unity 假 null 守卫——门可能在 0.2s 扫描缓存窗口内被 C4 / 破坏管线销毁，原实现会在访问 `door.transform` 时抛 `MissingReferenceException` 并中断同帧 `PeekDriver.Tick()`。
  - **Should fix 已修**：`DoorMenuMerge` 两个入口加补丁边界 `try/catch`（记录日志、保留原生菜单）；`owner.Player.IsYourPlayer` 统一前置；`Settings.cs` 过期“预留”注释清理；README 偏差表第 12 行改为 7 节配置面并补交互键名规范化说明，许可节改为工单 03 / 04。
  - **Nice to have 已修**：删除 `PeekDriver.Finish()` 空 `if` 块；`DoorMenuMerge` 的上游 null-ref TODO 改为明确风险注记。
  - 审查确认：KeyCard postfix 的 `Door door` 绑定与上游一致（`KeycardDoor : Door`，Harmony 按名绑定）；EFT 内部 KeyCard 解锁路径会尾调用 Door 重载，C4 去重守卫有效；`PartialOpenSwingPatch` 的曲线拉伸不累计污染；`QuietOpenSoundPatch` 的 Prefix 短路安全；`UnityEngine.InputLegacyModule` 引用必要。
  - 复验：`dotnet build -c Release -t:Rebuild` 0 warning / 0 error；标准检查 PASS=12 / FAIL=0 / WAIVED=2。状态改为 `ready-for-human`（待部署 + 实机验证）。
  - 部署（2026-10-20）：已覆盖 MO2 覆盖层 `[8]门的大修-破门大修-DoorBreachOverhaul\BepInEx\plugins\DoorBreachOverhaul\DoorBreachOverhaul.dll`；新 DLL 164,352 B / SHA256 `F6F743955B9F0DF9221CF74CC6F371529B3CB9A6FA4499B5911579B067C21C4B`；旧 DLL 备份在 `.artifacts\doorbreach-ticket04-backup\DoorBreachOverhaul.pre-ticket04.dll`（149,504 B / `B6470127…`）。服务端组件未改动。
- 2026-10-20（Overseer 实机验证，按测试清单编号）：
  - **通过**：1 启动日志 / 2 菜单动作 / 3 Peek / 4 Open Quietly / 5 体推门 / 6 踢门持枪 / 8 上锁门菜单 / 9 DoorRandomizer 共存 / 10 进出战局清理。
  - **未测**：7 KeyCard（上锁钥匙卡门只显示 Plant Explosive；解锁后 DO 动作 + C4 不重复）。ticket 主路径已通过，KeyCard 单项保持待补测。
  - 日志证据：DoorBreachOverhaul 17 个补丁全部 `Enabled patch`（ApplyHit / ActionMenuDoorPatch / ActionMenuKeyCardPatch / 5 个交互补丁 / PerfectCulling / OnGameStarted / 7 个破坏补丁）；`[Settings] 已绑定配置节：1. 通用 / 2. 破门 / 3. 破坏 / 4. 交互 / 5. 爆炸 / 6. 排除与开关 / 7. 调试。`；DoorBreach 无 error/异常。仅有包内 `Fontaine-SPTRealismCommon-Lib` 既有 `ArgumentOutOfRangeException`（与本 mod 无关）。
  - 状态保持 `ready-for-human`，剩余 KeyCard 单项实测；其余可进入下一 frontier（工单 05）。
