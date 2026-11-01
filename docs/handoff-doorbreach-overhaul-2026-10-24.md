# Handoff: DoorBreachOverhaul（破门大修）—— 工单 01–05 完成，下一会话做 06

> 交接日期：2026-10-24 ｜ 交接方：实施会话（03 → 04 → 05） ｜ 接收方：新会话 agent
> 下一会话焦点：**工单 06（排除开关体系）**。本文件只记录实施期新增事实与 06 的开工接缝，不重复 spec / 工单 / ADR 内容。
> 项目规则：交接文档放仓库 `docs/`（禁止临时目录）；本文件即按此规则落盘。

## 一、当前进度一句话

工单 **01 ✅ / 02 ✅ / 03 ✅ / 04 ✅（主路径实测通过，仅 KeyCard 单项未测）/ 05 ✅（46 单测 + 实测通过）**；工单 **08** 服务端 C4 已部署、bundle 已注册，仅剩游戏内 C4 验证；**下一 frontier = 06**，之后 07 → 09。

## 二、当前交付与环境

| 项 | 值 |
|---|---|
| 客户端工程 | `mods/SamMeow-DoorBreachOverhaul`（`src/` + `src/Rules/` + `tests/`） |
| 服务端工程 | `mods/SamMeow-DoorBreachOverhaulServer`（未改动，仍为工单 08 版本） |
| MO2 实例 | `E:\Game\EFT_Offline\Norvinsk_Forever`（profile Default） |
| 游戏 / 服务端 | `E:\Game\EFT_Offline\SPT_41x`（SPT 4.1.6 / EFT 0.16.9.40743），服务端根 `SPT_41x\SPT_Runtime` |
| 覆盖层 | `[8]门的大修-破门大修-DoorBreachOverhaul`（客户端 + 服务端两棵树） |
| 当前客户端 DLL | **168,960 B** / SHA256 `A33D6E21325E7F4EA4BC7E7115DA5ECE6E9999AE70A21794CF022E8C49628CD1`（工单 05 版，与 `bin/Release`、`release` 一致） |
| 回滚备份 | `.artifacts\doorbreach-ticket04-backup\`（工单 03 版）、`.artifacts\doorbreach-ticket05-backup\`（工单 04 版） |
| 当前进程 | 交接时 `EscapeFromTarkov` / `SPT.Server` / `SPT.Launcher` / `ModOrganizer` 均在运行；**部署前必须先退出游戏与 Launcher**（agent 不代启动/关闭） |
| 当前 F12 cfg | `Norvinsk_Forever\overwrite\BepInEx\config\com.sammeow.doorbreach.cfg` |

**不要重置用户配置。** 当前 cfg 含用户新增的白名单项：
- 喷子 `6761b213607f9a6f79017d23` = `[WTT-Armory] Serbu Super Shorty 12ga`（运行时 `weapClass="shotgun"`）；
- 弹药 `5d6e6806a4b936088465b17e` = `12/70 8.5mm 鹿弹`（8 弹丸 / 单颗 50 伤害）。
- 当前 `Pleb=False / SemiPleb=False / 耐久 200–400 / 霰弹 12 / 弹药 4 / 黑名单空 / Destructible Doors=true`。
- 用户已确认白名单正/负向测试通过；不要覆盖此 cfg。

## 三、04 / 05 已完成的关键事实（只列 06 会碰到的）

### 工单 04（交互并入与菜单合并）
- 已移植 DO 交互全套：`src/DoorOverhaul/Doors/{DoorOpenOverrides,DoorPusher,DoorRuntime,PeekDriver}.cs`、`Actions/{DoorActionKind,DoorActions}.cs`、`Helpers/InteractKey.cs`、`Patches/{InteractKeyCapture,KickWeaponForward,KickWeaponReady,PartialOpenSwing,QuietOpenSound}Patch.cs`。
- 菜单：`src/Patches/DoorMenuMerge.cs` 单入口；`ActionMenuDoorPatch` = DO 三动作 + C4；`ActionMenuKeyCardPatch` = 仅 C4 去重守卫。EFT 内部：解锁 KeyCard 会尾调用 Door 重载；上锁 KeyCard 只走 KeyCard 重载。
- 配置节 `4. 交互` 11 项已落；`DoorRuntime` 挂在现有 `_host`；`DestructionCleanupPatch` 已并入交互清理。
- **待补**：KeyCard 上锁/解锁菜单单项实测（Overseer 未测；不影响 06）。

### 工单 05（规则层 + 单一命中分派）
- 纯规则层：`src/Rules/AttackTypes.cs`（`TargetKind` / `MaterialKind` / `DamageKind` / `DoorStateKind` / `DoorClassKind` / `RuleConfig` / `AttackInput` / `AttackDecision`）与 `src/Rules/AttackPolicy.cs`（纯函数，无 Unity/EFT 依赖）。
- 适配器：`src/AttackPolicyAdapter.cs` 只做 `DamageInfo` / `DoorEntry` → 纯输入映射。
- 单一命中分派：`src/Patches/ApplyHitPatch.cs` 的 `ApplyHit` postfix → `AttackPolicyAdapter` → `AttackPolicy`，同时产出锁损伤与破坏受理。
- 配置快照：`DoorBreachPlugin.RuleConfig`（`Settings.BuildRuleConfig(ModConfig)`，插件启动 + 每战局启动重建；列表转 `HashSet`，`OrdinalIgnoreCase`）。
- `DamageUtility` 已收敛为事实提取（`TryGetDoorAnchor` / `TryGetContainerAnchor` / `TryGetTrunkAnchor` / 弹药分类 / `IsShotgun`），不再做受理决策。
- S0 单测：`tests/SamMeow.DoorBreachOverhaul.Tests`（xUnit / net10.0，只编译 `src/Rules/`）；当前 **46/46**。主工程 `<Compile Remove="tests\**\*.cs" />`。
- 当前 `AttackPolicy.EvaluateDestruction` **只接受 `DoorClassKind.Normal`**；这正是 06 要扩展的点。

## 四、工单 06（排除开关体系）范围

来源：`.scratch/bbng-dooroverhaul-416/issues/06-exclusion-switch-system.md`；spec 第 111 / 113 行附近；决策 D6 / R2-Q4。

要做的事：
1. **安全类总开关**：KeycardDoor / SlidingDoor / DoorSwitch / 成对双门 / 非可操作门，开启后可被破坏与破门（含交互语义检查）。
2. **高危组子开关**：触发任务门（`door.TriggersMap`）与撤离门（`IsExtractionDoor`）独立开关，默认关、带警示文案；开启后参与破坏。
3. **物理不可行项**：无弹道碰撞体 / 无几何 / 异常尺寸 / 材质 `Other` 等保留硬排除，不可开关化；调试面板逐类可读原因。
4. **单测**：排除策略默认关闭回归 + 解除态行为。
5. **游戏内逐开关验证**：关闭态回归（尤其任务脚本与撤离交互不被误伤）+ 开启态；记录边界/偏差。
6. 边界/偏差写 README 偏差表 + 工单 06 Comments。

## 五、06 的代码接缝（已核对的入口）

### 5.1 运行时排除
- `src/DoorOverhaul/Destruction/DoorDestructionRegistry.cs`
  - `Build()`：`foreach entry in Entries.Values.Where(e => e.Eligible) RegisterCollider(...)`；未注册的碰撞体不会进入 `ColliderMap`，`ApplyHit` 不会派发破坏。
  - `ExclusionReason(Door, DoorEntry)`：当前顺序 Fika → KeycardDoor → SlidingDoor → DoorSwitch → `TriggersMap` → `IsExtractionDoor` → `!Operatable` → `Colliders.Count==0` → `Profile==null` → `Frame` 无效。返回字符串，未分类。
  - `MarkPossibleDoubleDoors()`：同 parent 或 latch edge < 0.25m → 两扇 `Eligible=false`，`Reason="possible double door (until surveyed)"`。
  - `DoorEntry`：`Eligible` / `Reason` / `Material` / `Frame` / `State` / `Colliders`；建议新增 `ExclusionCategory`（None/Safety/HighRisk/Physical）或等价枚举，避免解析字符串。
- `src/DoorOverhaul/Destruction/DestructionController.cs`
  - `DrawInfo(DoorEntry)` 当前对非 eligible 显示 `vanilla: {entry.Reason}`；06 要显示“物理不可行 / 安全类 / 高危组”分类 + 原因。
- `src/Rules/AttackPolicy.cs`
  - `EvaluateDestruction`：当前 `DoorClass != Normal` → `RejectDestruction("door class ...")`；`Material.None/Other` → reject；`DestructionEnabled` → reject。
- `src/Rules/AttackTypes.cs`
  - `DoorClassKind` 当前 `Normal / Keycard / Sliding / DoorSwitch / Excluded`；06 建议扩展 `Double / Trigger / Extraction`，`Excluded` 留给物理不可行。
  - `RuleConfig` 当前含 `DestructionEnabled` + 列表/血量；06 增加 `AllowSafetyExclusions` / `AllowHighRiskExclusions`（默认 false）。
- `src/Settings.cs`
  - 节 `6. 排除与开关` 当前只有 `Blacklisted Rooms`；06 加两个 bool 键（英文键名 / 中文显示名 / 高危警示文案），并加入 `BuildRuleConfig`。
- `src/AttackPolicyAdapter.cs`
  - `MapClass(Door)` 当前只映射 Keycard/Sliding/DoorSwitch/Normal；06 需从 `DoorEntry` 的排除分类/`TriggersMap`/`IsExtractionDoor` 映射 Double/Trigger/Extraction，或让 adapter 直接使用 `DoorEntry.ExclusionCategory`。
- `src/DoorBreachComponent.cs`
  - BB 铺场只看 `DoorState` / `Operatable` / Interactive layer，**没有**显式的 keycard/sliding/switch 排除。06 的“可被破门”要确认 BB 锁血池对这些门的现状：若它们本来就可操作，BB 可能已经能破门；不要把 DO 的排除开关与 BB 的锁血池排除混为一谈。需要实机/日志确认。
- `src/Patches/ActionMenuKeyCardPatch.cs` + `DoorMenuMerge`
  - 上锁 KeyCard 只走 KeyCard 重载（仅 C4）；解锁 KeyCard 尾调用 Door 重载（DO 三动作 + C4）。06 若解除 KeyCard 排除，需确认“破门/交互”是否要求上锁 KeyCard 也出现 DO 动作；若需要，必须显式改 KeyCard 重载，不能只改注册表。
- Fika 硬排除：`ExclusionReason` 第一条 `FikaPresent` 是“未同步”硬排除，**不要**并入安全类总开关。

### 5.2 推荐实现路线（二选一，先与 spec 对齐）
- **A（推荐）**：在 `src/Rules/` 增加 `ExclusionPolicy` 纯函数：输入 `DoorClassKind` + `RuleConfig`，输出 `Allowed` / `Category` / `Reason`。`DoorDestructionRegistry.Evaluate` 先用运行时事实判定物理不可行/类别，再调用它决定 `Eligible`；`AttackPolicy.EvaluateDestruction` 复用同一函数。这样注册表与规则层不会各写一套。
- **B**：直接把 `AllowSafetyExclusions` / `AllowHighRiskExclusions` 塞进 `AttackPolicy.EvaluateDestruction`，注册表用同样的布尔判断。代码少，但两处判定容易漂移。
- 无论 A/B：**物理不可行项永远不可开关化**；Fika 保持硬排除；配置在战局启动快照（与 1/2/5/6 节一致），战局中途改 F12 下局生效。
- 双门：`MarkPossibleDoubleDoors` 现在会把两扇都标为 ineligible；开启安全类后两扇都要变为 eligible，注意别只放行一扇导致“半扇可破坏”的异常。建议记录偏差。

### 5.3 单测扩展（S0）
在 `tests/SamMeow.DoorBreachOverhaul.Tests/AttackPolicyTests.cs` 增加表驱动用例：
- 默认关闭：Normal 受理；Keycard/Sliding/DoorSwitch/Double/Trigger/Extraction 拒绝；Physical/`Excluded` 拒绝。
- 仅安全类开：安全类受理；高危组仍拒绝；物理仍拒绝。
- 仅高危组开：高危组受理；安全类仍拒绝；物理仍拒绝。
- 两者都开：安全类 + 高危组受理；物理仍拒绝。
- 拒绝原因按类别可读（`safety` / `high risk` / `physical`）。
- 保留现有 46 用例；新增后更新 README 的测试计数。

## 六、构建 / 测试 / 部署

在 `mods/SamMeow-DoorBreachOverhaul`：
```
dotnet build -c Release
```
在测试工程：
```
dotnet test -c Release
```
标准检查：
```
powershell -NoProfile -ExecutionPolicy Bypass -File "E:\云文件\GitHub\SamMeow-modding-superpowers\scripts\check-mod-standard.ps1" -ModPath "E:\云文件\GitHub\SamMeow-modding-superpowers\mods\SamMeow-DoorBreachOverhaul" -TargetSptVersion 4.1.6 -Kind client
```
当前基线：build 0/0；单测 46/46；标准检查 PASS=12 / FAIL=0 / WAIVED=2（STD-CLI-003/007 既有豁免）。

部署（06 完成后）：
1. 先确认 `EscapeFromTarkov` / `SPT.Launcher` 已退出（agent 不代关）。
2. 复制 `bin\Release\DoorBreachOverhaul.dll` 到覆盖层 `...\BepInEx\plugins\DoorBreachOverhaul\DoorBreachOverhaul.dll`。
3. 校验 SHA256 与 `bin\Release` / `release` 一致；旧 DLL 备份到 `.artifacts\doorbreach-ticket06-backup\`。
4. 服务端组件（`SPT_Runtime\user\mods\DoorBreachOverhaulServer`）本单不需要改。
5. 请 Overseer 通过 MO2 启动；agent 读 `SPT_41x\BepInEx\LogOutput.log` 断言。

当前覆盖层 DLL 为工单 05 版 `A33D6E21…`；06 的 DLL 会不同，部署后重新记录哈希。

## 七、环境坑 / 已知基础设施问题

- **后台 subagent 证书错误**：本会话后台 `@fixer` 与 `@oracle` 均以 `unknown certificate verification error` 失败；前台 `@oracle` 正常。新会话优先**主会话直接实现**或**前台 @oracle**；不要反复重试后台 fixer/oracle。
- MO2 MCP：会话绑定可能漂移到其他实例；先 `mo2_session({mo2Root:"E:\\Game\\EFT_Offline\\Norvinsk_Forever"})`。`mo2_install` 的整数 `target_priority` 不生效；安装后用 `mo2_send_mod_to {target_mode:"above_separator", target_separator:"[8]战局内功能大修_separator"}`。
- `mo2_reinstall_mod` 需 MO2 在线；离线更新覆盖层 = 直接复制文件（游戏关闭时）。
- 游戏运行中覆盖层 DLL 被锁；更新前确认 `EscapeFromTarkov` / `SPT.Server` 进程已退出。
- BepInEx 游戏期写入被 MO2 重定向到 `overwrite\BepInEx\config\`；新 cfg 在 `overwrite` 下，不在 SPT 安装目录。
- 日志：`SPT_41x\BepInEx\LogOutput.log`；服务器日志在 `SPT_41x\SPT_Runtime\user\logs\`。`tarkov_logs_summary` 可能因 MCP 配置为 SPT 5.0 而版本门禁失败，直接读文件更稳。
- `Fontaine-SPTRealismCommon-Lib` 的 `ArgumentOutOfRangeException` 是包内既有噪声，与本 mod 无关；不要把 06 的新异常混进去。
- 仓库噪声：`mods/InescapableTarkovsSoftcore416/tests` 的既有 LSP 报错与本项目无关，忽略。
- 提交：本轮 03/04/05 的改动均未提交（用户执行）；新会话不要擅自 commit。

## 八、新会话首条消息怎么开始

1. 读本文件 → 读 `.scratch/bbng-dooroverhaul-416/issues/06-exclusion-switch-system.md` → 读 `spec.md` 第 111 / 113 / 128 行附近 → 读 `decisions.md` D6 / R2-Q4。
2. 读代码：`DoorDestructionRegistry.cs`、`DestructionController.cs`、`Rules/AttackTypes.cs`、`Rules/AttackPolicy.cs`、`AttackPolicyAdapter.cs`、`Settings.cs`、`DoorBreachComponent.cs`、`ActionMenuKeyCardPatch.cs` / `DoorMenuMerge.cs`。
3. 先确认 `EscapeFromTarkov` / `SPT.Launcher` 进程状态；不要在游戏运行时部署。
4. 按工单 06 实施：先补 S0 排除策略测试（TDD），再改注册表/规则层/配置/调试面板；跑 build + test + 标准检查；前台 @oracle 审查 Must fix。
5. 部署前请 Overseer 退出游戏；部署后请其逐开关实机验证，agent 读日志并记录偏差。
6. 若 06 收口，下一 frontier 为 07（C4 客户端行为）；08 的 C4 游戏内验证仍独立待办；04 的 KeyCard 单项也仍待补测。

## 九、Suggested skills（新会话 Skill 工具）

- `implement` / `implement-spec` —— 按工单 06 实现
- `tdd` —— 排除策略 S0 用例
- `code-review` / `@oracle`（前台）—— 06 收口审查
- `diagnosing-spt-problems`、`spt-conflict-audit`、`spt-mcp-automation` —— 任务/撤离门误伤排查、MO2/日志断言
- `handoff` —— 下次会话边界
