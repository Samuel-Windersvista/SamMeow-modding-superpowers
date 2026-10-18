# Handoff: DoorBreachOverhaul（破门大修）—— 工单 01/02/03/08 已实施，等待实机测试结果

> 交接日期：2026-10-18 ｜ 交接方：实施会话（工单 01 → 02 → 03 + 08 双 lane） ｜ 接收方：新会话 agent
> 本文件按项目规则置于仓库 `docs/`（禁止临时目录）。**用户的测试结果将在新会话首条消息给出**——先收集结果，再按下文「新会话进法」继续。
> 上一份交接：`docs/handoff-doorbreach-overhaul-2026-10-17.md`（研究/决策/spec/工单全量已完成）。本文件只记录**实施期**新增事实，不重复 spec 与工单内容。

## 一、当前进度一句话

工单 **01（工程奠基）✅、02（触点核验）✅、03（破坏并入，代码侧完成，待实机复测）、08（C4 服务端，代码侧完成 + 已部署，待服务器/游戏内验证）**；下一步 frontier = **工单 04（交互并入 + 菜单合并）**，之后 05 → 06 → 07 → 09。

## 二、本轮实施交付物（全部已落盘）

| 产物 | 路径 | 状态 |
|---|---|---|
| 客户端工程 | `mods/SamMeow-DoorBreachOverhaul`（`src/` 24 文件 + 5 个原创文件） | 构建 0 warning/0 error；标准检查 13 PASS / 0 FAIL / 1 WAIVED |
| 触点核验表 | `mods/SamMeow-DoorBreachOverhaul/docs/api-touchpoints/{bb-breach,do-destruction,do-interactions}.md` | BB 5 补丁 + DO 16 触点全部 `[OK]`；无 CHANGED/MISSING |
| 服务端工程 | `mods/SamMeow-DoorBreachOverhaulServer`（csproj + src 3 文件 + bundle/db + 文档 + 豁免文件） | 构建 0 warning/0 error；标准检查 20 PASS / 0 FAIL / 3 WAIVED |
| 合并部署包 | `mods/SamMeow-DoorBreachOverhaul/dist/DoorBreachOverhaul-0.1.0-test.zip`（客户端 + 服务端两棵树） | 2,261,455 B |
| 工单状态 | `.scratch/bbng-dooroverhaul-416/issues/01..09` | 01/02 全勾；03 部分勾（游戏内待验）；08 部分勾（运行时待验）；04/05/06/07/09 未动 |

## 三、实施期关键决策与偏差（已在 README 偏差表 / 工单 Comments 登记）

1. **身份**：AssemblyName `DoorBreachOverhaul` / GUID `com.sammeow.doorbreach` / 0.1.0；服务端 `SamMeow.DoorBreachOverhaulServer` / ModGuid `com.sammeow.doorbreach.server` / 0.1.0。
2. **BB-NG 4.1 迁移三处绑定缺陷（工单 01 修复，运行时验证）**：`GetAvailableActions` 约 20 重载 → `AccessTools.Method` 显式签名；PerfectCulling 字段 4.1.6 改名（`runtimeGroupDataSize` / `_runtimeGroupData`）；音频路径改插件目录相对。
3. **配置载体**：本地 BepInEx（4+2 节；键名英文/显示名中文）。BB 类为**战局启动快照**（`RefreshConfigSnapshot` 挂 `OnGameStarted`）；破坏/调试项**实时读值**。破坏节 3 九项、调试节 7 三项。
4. **双轨语义（工单 03）**：唯一命中分派 = `BallisticCollider.ApplyHit` postfix → 先 `DispatchDestruction`（`DoorDestructionRegistry.ColliderMap` 映射、任意射手、独立 try/catch），后走 BB 门禁。DO 的 `OnHitAction` 订阅已全部移除（`DoorTakeover` 同步改为 `RegisterCollider/UnregisterCollider`）。
5. **上锁门豁免移除**：`DoorDamageIntake` 两处 `EDoorState.Locked` 拦截删除（全目录 0 残留）。
6. **Pleb/SemiPleb 与铰链实验冲突（实测后修复）**：上游两模式完全跳过锁区锚定 → 任意命中积攒锁血池，门在铰链打坏前被踹开。现改为：**放行武器/弹药/材质/黑名单，但锁血池只吃门把手附近（0.25m，门局部空间）命中**；`IsValidDoorLockHit` 顺带修正了上游「碰撞体局部空间 vs 把手 localPosition」的坐标系混用。README 偏差表第 13 行；工单 05 需将此语义正式化（可选旋钮默认关）。
7. **服务端加载阶段偏差（证据裁决）**：工单文字写 `PostLoad + 1`，实现用 **`Preload + 1`**。证据：`OnLoadOrder.PostLoad = 1000000`；`SaveCallbacks`（TypePriority=600000）置 `CustomItemService.ProfilesLoaded = true`，此后 `CreateItem` 抛异常。已采纳 `Preload + 1`。
8. **服务端数据契约（4.1 实证）**：`CustomItemService.CreateItem(NewItemDetails, Assembly?)` 直接写模板表（无 `db/CustomItems` 自动扫描）；bundle 由 `BundleLoader` 扫 mod 根 `bundles.json` + `bundles/<key>`（`IsBundleMod` 已移除）；配方 = 代码 append `HideoutTable.Production.Recipes`（Workbench=10）。
9. **服务端无配置系统**：STD-CFG-002/003/005 FAIL 属如实结果；`mods/SamMeow-DoorBreachOverhaulServer/MODDING-STD-WAIVER.md` 已登记三条豁免。
10. **单 mod 打包（Overseer 指令）**：客户端 + 服务端合并为**单一 MO2 覆盖层** `[8]门的大修-破门大修-DoorBreachOverhaul`（`BepInEx\plugins\DoorBreachOverhaul\` + `SPT_Runtime\user\mods\DoorBreachOverhaulServer\`）；原两个 `*-test-build` 覆盖层已删除。工程源码仍为两个 csproj（SPT 客户端/服务端本就两种二进制形态）。

## 四、部署与运行环境现状

- MO2 实例 `E:\Game\EFT_Offline\Norvinsk_Forever`（profile Default），游戏 `E:\Game\EFT_Offline\SPT_41x`（SPT 4.1.6 / EFT 0.16.9.40743），服务端根 `SPT_41x\SPT_Runtime`。
- 当前 modlist：`+[8]门的大修-破门大修-DoorBreachOverhaul`（priority 59，唯一启用覆盖层）；`-[8]门的大修-DoorOverhaul`（原 DO 保持禁用）。
- 覆盖层内：客户端 DLL **149,504 B**（2026-10-18 19:08，含锚定修复）+ audio；服务端 DLL 17,408 B + `bundles.json` + `bundles/C4Explosive.bundle`（SHA256 `471BFB5A…F1F19415`）+ `db/**`。
- 客户端 F12 现状（`Norvinsk_Forever\overwrite\BepInEx\config\com.sammeow.doorbreach.cfg`）：Pleb Mode=true、Breaching Rounds Open Metal Doors=true、容器/后备箱=true、耐久 100–200、黑名单空；破坏节默认 Destructible=true/ShowHoles=true/HingeCollapse=**false**（铰链实验需手动打开）。
- MO2 MCP 注意：① 会话绑定可能漂移到其他实例（如 `SPT-Organizer-v2.5.2-Novinsk`）→ 先 `mo2_session({mo2Root:"E:\\Game\\EFT_Offline\\Norvinsk_Forever"})`；② `mo2_install` 的整数 `target_priority` 不生效（落底部）→ 安装后用 `mo2_send_mod_to {target_mode:"above_separator", target_separator:"[8]战局内功能大修_separator"}`；③ `mo2_reinstall_mod` 需 MO2 在线（offline 更新覆盖层 = 直接复制文件到 `mods/<overlay>/...`，游戏关闭时）。
- agent 不启动 MO2/游戏/服务器；日志断言由 agent 读取（BepInEx：`SPT_41x\BepInEx\LogOutput.log`；服务器日志在 `SPT_41x\SPT_Runtime\user\logs\`，也可用 `tarkov_logs_summary`）。

## 五、新会话首条消息应做的事（用户会先给测试结果）

用户在本会话末尾已实测：**上锁门可被枪打坏 ✅**；并报告 Pleb 全放行与铰链实验互斥（已修复并部署，等待复测）。新会话将收到：

1. **锚定修复复测**（Pleb + 铰链实验开启）：铰链侧持续射击 → 应先悬挂/脱落而非提前开门；门把手附近射击 → 正常破门；门板中部 → 只破坏、不积攒锁耐。
2. **工单 03 游戏内项**：解锁门打孔/断片、调试面板、进出战局无残留。
3. **工单 08 运行时项**：服务器日志 `[SUCCESS] [DoorBreachOverhaulServer] 已加载：C4 物品 tpl=6636606320e842b50084e51a，工作台配方 id=665d4ce7e381d16c8676292b`；无 bundle 加载错误；图鉴 C4、工作台（≥2 级）配方与制造、图标/模型。

处理顺序：读日志断言（BepInEx + 服务器）→ 按结果勾工单 03/08 的待验项（FAIL 则先定位：`diagnosing-spt-problems` / `spt-conflict-audit`）→ 进入工单 04。

## 六、工单 04 开工提示（下一个 frontier）

- 范围：DO 交互全套并入（Peek / Open Further / Open Quietly / 体推门 / 踢门持枪（就绪+朝向）/ 交互键捕获 / 部分开合动画）+ **交互菜单单点注入并去重**（`InteractionContextHelper.GetAvailableActions(GamePlayerOwner, Door)` 合并为单 postfix：DO 三动作 + Plant Explosive；KeyCard 重载保留现有去重守卫）。
- 源：`external/references/door-overhaul/` 的 `Doors/{DoorPusher,DoorOpenOverrides,PeekDriver,DoorRuntime}.cs`（`DoorMath.cs` 已在 03 复制）+ `Patches/{AddDoorActionsPatch,InteractKeyCapturePatch,KickWeaponForwardPatch,KickWeaponReadyPatch,PartialOpenSwingPatch,QuietOpenSoundPatch,RaidEndCleanupPatch}.cs`；配置节 `4. 交互`（KickWeaponReady/KickWeaponForward/BodyPush/BodyPushStrength/SecondsToFullyOpen/MinimumPeekPercent/QuickPeekPercent/SilentPeek/QuietOpenPercent/QuietSwingSlowdown/QuietVolume）。
- 注意：`DoorRuntime` 是交互侧 Update 宿主（KickWeaponReady.Tick/DoorPusher.Tick/PeekDriver.Tick）——需挂到现有 `DoorBreachOverhaul` 宿主 GameObject；战局结束清理需并入现有 `DestructionCleanupPatch` 或新增交互清理补丁（PeekDriver.Cancel / DoorPusher.Clear / KickWeaponForward.Clear / DoorOpenOverrides.Clear）。DO 的 `RaidEndCleanupPatch` 与 `DestructionRaidStartPatch` 不整体复制（已分别并入/替代）。
- 触点已核验：`PlayerBones.ShiftWeaponRoot`（全局类、12 参单方法）、`InputKeyCombination.UpdateCommand(float)`、`Player.StateIsSuitableForHandInput`、`InitializeSmoothOpenInteraction(EDoorState,bool)`、`PlaySound(EDoorState,float)`、`EPlayerState.BreachDoor`（全局枚举）——见 `do-interactions.md`。

## 七、工单 05–09 备忘

- **05**：规则层纯函数化（武器/弹药/材质/锚定/模式/门类/配置快照 → 锁损伤/破坏受理/拒绝原因）+ S0 单测；将 Pleb/SemiPleb 锚定语义正式化（见三.6）；配置面扩至 7 节全旋钮。
- **06**：排除开关体系（总开关解除安全类；高危组子开关默认关；物理不可行项保留排除并在调试面板显示原因）。
- **07**：C4 客户端行为（引爆 → 完整破坏管线 + 不可破坏回退强制开门 + 优雅降级；替代当前 `Destroy(door.gameObject)`）；破门弹 `660249a0…` 为死分支（无物品，不崩溃），不复活。
- **08**：仅剩运行时验证与 bundle 兼容记录。
- **09**：正式覆盖层（已按单 mod 形态先行）、替换/保留 `[8]门的大修-DoorOverhaul`、BB 未就绪清单收口、`[15]` 配置对照迁移、Level C 全清单、README/许可/偏差表收口。
- **提交**：全部改动均未提交（用户执行）：`mods/SamMeow-DoorBreachOverhaul/**`、`mods/SamMeow-DoorBreachOverhaulServer/**`、`.scratch/bbng-dooroverhaul-416/**`、`docs/adr/0012-*.md`、两份 handoff。

## 八、环境教训（沿用 + 新增）

- 含中文 `.ps1` 用 UTF-8 BOM；overlay 全树物理路径 ≤260；部署前 profile 备份。
- BepInEx 游戏期写入被 MO2 重定向到 `overwrite\BepInEx\config\`；新 cfg 在 `overwrite` 下（不在 SPT 安装目录）。
- 游戏运行中覆盖层 DLL 被锁 → 更新前确认 `EscapeFromTarkov`/`SPT.Server` 进程已退出。
- `ModulePatch` 日志不区分命名空间（同名补丁需按加载块时序归属）。
- SPT 4.1.6 事实：`PlayerBones` 为全局类；`InputKeyCombination` 在 `EFT.InputSystem`；`EPlayerState` 为全局枚举；`KeycardDoor : Door`；`Grenade.Explosion` 为静态 8 参；`Door._occlusionPortal` 为 public 字段；`Effects._dict` 为 private 字典；`OnLoadOrder`：Preload=100000 / SaveCallbacks=600000 / PostLoad=1000000。
- 仓库噪声：`mods/InescapableTarkovsSoftcore416/tests` 的既有 LSP 报错与本项目无关，忽略。

## Suggested skills（新会话 Skill 工具）

- `implement` / `implement-spec` —— 按工单实现（工单 04 起）
- `tdd` —— 工单 05 规则层 S0 单测
- `code-review` —— 工单收口与最终双轴审查
- `diagnosing-spt-problems`、`spt-conflict-audit`、`spt-mcp-automation` —— 运行时排障与 MO2/日志断言
- `writing-spt-modpack-devlog` —— 按需登记
- `handoff` —— 下次会话边界
