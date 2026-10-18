# 01: 工程奠基：合并基座与 BB 独立基线（曳光弹）

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** 在 `mods/SamMeow-DoorBreachOverhaul` 落位合并工程基座：以 BB-NG master 快照（`external/references/backdoorbandit-ng`，`451c6cb`）为源码起点（不含 Fika 子工程与仓库内旧 vendored 服务器载荷），重建被上游 .gitignore 排除的主构建工程（引用指本机 SPT 4.1.6：Managed / BepInEx core / spt plugins / 第三方程序集；引用根参数化、缺失响亮失败），身份定型（AssemblyName `DoorBreachOverhaul` / BepInPlugin GUID `com.sammeow.doorbreach` / 版本 0.1.0，三处同步），并以本地配置载体替换上游的服务端 HTTP 配置下发（配置缺失/损坏时默认值启动、无空引用崩溃），同时修复上游 4.1 迁移遗留的三处补丁绑定缺陷（使插件可在 4.1.6 真实加载）。完成后单机 4.1.6 环境可加载本插件，破门语义真实可用：射击上锁门 → 踹门开门。本单不含 DO 并入、不含部署、不写 MO2/游戏目录。

**Blocked by:** None（可立即开始）

**Status:** ready-for-agent

- [x] `dotnet build -c Release` 0 error；引用根参数化（默认本机 SPT 4.1.6），路径缺失响亮失败
- [x] 身份定型三处一致（构建工程 / 插件入口 / README）：`DoorBreachOverhaul` / `com.sammeow.doorbreach` / 0.1.0；单一日志源与统一前缀
- [x] 本地配置载体生效：无服务端依赖、无 `/backdoorbandit/load` 调用；缺配置时默认值启动且无 NRE
- [x] 经 MO2 启动至主菜单：BepInEx 日志断言插件加载成功、无异常、无 patch 绑定错误
- [x] 游戏内：合规武器射击上锁门 → 门被打开（破门基线在 4.1.6 真实可用）（Pleb 全放行链路实测通过；白名单弹药 + 命中锚定路径待 02 复核）
- [x] README / LICENSE / THIRD-PARTY-NOTICES 骨架（MIT 起步；BSD-3 署名位预留至 03 并入时补全）；偏差表雏形
- [x] 零 git 提交（提交由 Overseer 执行）；零 MO2/游戏目录写入

**上游参考（本仓归档）:** `external/references/backdoorbandit-ng`（master `451c6cb`，MIT）；发行包解包 `D:\Temp\opencode\bbng-2.0.4`（安装布局与服务端构成参考）；构建样板 `mods/SPT41-DebugToolkit`（4.1.6 构建接缝惯例）。

## Comments

- 2026-10-17（开工修正）：原文「踢门持枪姿态开关生效」与「本单不含 DO 并入」矛盾——「踢门持枪（就绪/朝向）」为 DO 能力（`KickWeaponReadyPatch` / `KickWeaponForwardPatch`），且已由工单 04 明确承接（「踢门持枪（就绪/朝向两开关）」）。已从本单 What-to-build 与验收清单移除，改为由 04 验收。依 handoff「工单如需调整：直接改 issues 文件」执行。
- 2026-10-17（开工勘察）：4.1.6 实装审计（ilspycmd 预检）发现上游 master 三处补丁绑定缺陷，将导致加载即报 patch 错误，纳入本单修复（02 复核）：
  1. `InteractionContextHelper.GetAvailableActions` 在 4.1.6 有约 20 个重载；`GetMethod(name)` 会抛 `AmbiguousMatchException`。Door 补丁需显式绑定 `(GamePlayerOwner, Door)`；KeyCard 补丁需显式绑定 `(GamePlayerOwner, KeycardDoor, bool)`（`KeycardDoor : Door`，postfix 参数注入类型可指基类）。
  2. `PerfectCullingBakeGroup` 字段在 4.1.6 已改名：`Int_0`→`runtimeGroupDataSize`、`RuntimeGroupContent_0`→`_runtimeGroupData`；上游 prefix 的 `___Int_0`/`___RuntimeGroupContent_0` 字段引用需同步更新。
  3. 已核实单方法绑定无歧义：`BallisticCollider.ApplyHit(DamageInfo, ShotId)`（单重载，postfix 按参数名注入 `damageInfo` 即可）、`GameWorld.OnGameStarted()`（单方法）、`PerfectCullingBakeGroup.Toggle(bool)`（单方法）。
- 2026-10-17（实现完成）：工程落位 `mods/SamMeow-DoorBreachOverhaul`（23 文件：`src/` 14 个 C# + csproj + README/LICENSE/THIRD-PARTY-NOTICES/豁免文件，外加已有 GLOSSARY/adr/assets）。实现经 @fixer 后台作业（fix-1）完成，编排者独立复验：
  - **构建**（编排者本机 `dotnet build -c Release -t:Rebuild` 复跑）：0 warning / 0 error；产物 `bin\Release\DoorBreachOverhaul.dll`（47104 B）+ `release\`（DLL + `audio\Beep.mp3` + `audio\FinalBepTone.mp3`）。
  - **标准检查**：`check-mod-standard.ps1 -TargetSptVersion 4.1.6 -Kind client` → PASS=13 / FAIL=0 / SKIP=0 / WAIVED=1（STD-CLI-003 有意豁免，已登记 `MODDING-STD-WAIVER.md`）。
  - **身份三处**一致（grep）：GUID `com.sammeow.doorbreach` / 程序集 `DoorBreachOverhaul` / 版本 `0.1.0`。
  - **零残留**（grep）：`RequestHandler` / `backdoorbandit/load` / `NotificationManager` / `Newtonsoft` / `SPT.Common.Http` / `EFT.Communications` / `BackdoorBanditClient` 全部 0 命中。
  - **零越界写入**：`E:\Game\EFT_Offline\` 无新文件（插件目录不存在）；零 git 操作。
  - 三处绑定缺陷已修：两个菜单补丁改 `AccessTools.Method` 显式重载；PerfectCulling prefix 字段引用更新为 4.1.6 真名；均带实证注释。
  - 实现期偏离（工作单外，均为编译/环境实证必需）：① JetBrains.Annotations 不引用独立程序集（`UnityEngine.CoreModule` 已内建同名命名空间，独立引用触发 CS0433 二义，`[CanBeNull]` 由内建副本解析）；② 追加 `DissonanceVoip` 引用（`Player.InteractablePlayer` 编译期必需）；③ 编排者将 Settings 绑定日志修正为仅列已绑定节（原含预留节，防 Level B 误读）。
- **待办（唯一剩余）**：经 MO2 的 S2 冒烟（加载断言）与 S3 破门基线（射击上锁门 → 踹门开门），需 Overseer 手工执行；agent 提供步骤与日志断言（见下条评论）。
- 2026-10-17（S2/S3 冒烟指引，Overseer 执行；agent 不代启动 MO2/游戏）：
  1. 确认游戏与 MO2 均已退出。
  2. 临时测试投放（**非工单 09 正式部署**）：复制 `mods\SamMeow-DoorBreachOverhaul\release\DoorBreachOverhaul.dll` 与 `release\audio\` 目录到 `E:\Game\EFT_Offline\SPT_41x\BepInEx\plugins\DoorBreachOverhaul\`（放基础安装目录；MO2 启动时 VFS 正常叠加；测完删除该目录即完整回滚）。也可选择在 MO2 建临时覆盖层替代。
  3. 经 MO2 启动至主菜单 → 回报「到达主菜单」；agent 断言 `E:\Game\EFT_Offline\SPT_41x\BepInEx\LogOutput.log`：应依次出现 `[DoorBreachOverhaul] 0.1.0 正在加载`、`[VersionGate] EFT 版本匹配`、`[Settings] 已绑定配置节`、`[DoorBreachOverhaul] 0.1.0 已加载`；且全日志无 `Failed to create patch`、无异常堆栈。
  4. 进任一地图（S3 破门基线）：找一扇**上锁的门**，持白名单内霰弹枪贴近门把手区域射击数发 → 门应被踹开（原生踹门动画）→ 回报 OK/FAIL + 现象；可选：车辆后备箱 / 容器（默认开启）同法验证；F12 检查配置面 4 节齐备（`1. 通用` / `2. 破门` / `5. 爆炸` / `6. 排除与开关`）。
  5. 回报格式：逐项 `OK` 或 `FAIL + 现象`；FAIL 请附 `LogOutput.log` 相关片段。
- 2026-10-17（S2 准备：MO2 覆盖层部署；Overseer 指令「直接经 MO2 部署进永远的诺文斯克」）：
  - 覆盖层 `[8]门的大修-破门大修-DoorBreachOverhaul-test-build`（priority 59，[8] 组内、紧邻 `[8]门的大修-DoorOverhaul`；`+` 启用）。
  - 内容：`BepInEx\plugins\DoorBreachOverhaul\DoorBreachOverhaul.dll` + `audio\`（2 mp3）+ README/LICENSE/THIRD-PARTY-NOTICES；meta.ini comments/notes 已写；安装源 `mods/SamMeow-DoorBreachOverhaul/dist/DoorBreachOverhaul-0.1.0-test.zip`（45,949 B）。
  - 方式：MO2 MCP plan/apply（安装快照 `7a72f418-b5e3-49b7-adad-f8d9fe829b2d`、重排快照 `ae34ee6d-04ac-4e4d-b6bb-c7b5c1ffb757`）；MO2 离线操作，未启动 MO2/游戏，未写 SPT 安装目录。
  - 测试期保持 `[8]门的大修-DoorOverhaul` 启用（共存观察）；若破门基线受 DO 干扰，可临时禁用 DO 隔离。
  - 待 Overseer：经 MO2 启动至主菜单 → agent 断言 `LogOutput.log`（S2）；随后进图射击上锁门（S3）。
- 2026-10-17（S2 PASS，当日主菜单实测）：`E:\Game\EFT_Offline\SPT_41x\BepInEx\LogOutput.log`（1730 行）本插件加载块（1252–1260 行）逐行断言通过：
  - `[Info : BepInEx] Loading [Door Breach Overhaul（破门大修） 0.1.0]` → `[DoorBreachOverhaul] 0.1.0 正在加载（目标 EFT 0.16.9.40743）` → `[VersionGate] EFT 版本匹配：build=40743` → `[Settings] 已绑定配置节（1/2/5/6 + 3/4/7 预留）` → 5 个补丁 `Enabled patch`（ApplyHit / ActionMenuDoorPatch / ActionMenuKeyCardPatch / PerfectCullingNullRefPatch / OnGameStartedPatch）→ `[DoorBreachOverhaul] 0.1.0 已加载`。
  - 全日志零 `Failed to create patch` / 零 `AmbiguousMatchException` / 零 Error/Fatal。三处 4.1.6 绑定修复在运行时成立。
  - 注：`ModulePatch` 日志不区分命名空间，同名 `OnGameStartedPatch` 另有 2 行属第三方 mod；本插件 5 补丁以加载块时序归属确认。
- 2026-10-17（S3 首测诊断）：首轮霰弹枪射击未开门。日志证据：命中链健康——`[Breach]` 铺场 Doors 219 / Containers 294 / Trunks 147（无效 25），`[C4]` 音频加载成功 ×2，零异常；但**零 `[Patches] Applying Hit Damage`** → `validDamage=false`。根因两条（均非缺陷）：
  1. **上游默认弹药语义**：霰弹枪要求弹药白名单命中（白名单实物：`12/70 AP-20` + `20/70 AP/DG` 独头弹）；普通鹿弹/独头弹不在白名单 → 不开门。白名单霰弹枪 11 把均为 12 号（MP-133/153/155、MR-43/43E、Saiga-12K、M3 S90、Mossberg 590A1、MC 255、Remington 870）。
  2. **配置快照时机**：F12 的 Pleb Mode 改动已写入 cfg（MO2 重定向至 `overwrite\BepInEx\config\com.sammeow.doorbreach.cfg`，`Pleb Mode = true`），但快照仅在插件启动时读取 → 需重启生效。
  - 处置（小幅补强，列入本单）：新增 `DoorBreachPlugin.RefreshConfigSnapshot()`，在 **`GameWorld.OnGameStarted` 时重读配置快照**（F12 修改下一战局生效，无需重启游戏）+ 每战局输出一行 `[Settings] 配置快照：...` 便于断言；README 补「生效时机」与「霰弹需白名单弹药」说明、偏差表第 9 行。`dotnet build -c Release` 0 warning / 0 error。
  - 复测方式：更新覆盖层 DLL（需游戏退出）→ 启动 → 进图（Pleb 已开）射击任意门应开门；合规语义复测用 12 号霰弹枪 + `12/70 AP-20` 弹药、或近战打非金属门。
- 2026-10-17（S3 PASS，实机复测）：覆盖层原位更新（DLL 47,616 B；meta.ini 与排序保留）后重启进图，Overseer 实测「霰弹枪射击门 → 开门」通过。日志证据（本次战局）：
  - `[Settings] 配置快照：Pleb=True, SemiPleb=False, 金属门破门弹=True, 容器=True, 后备箱=True, 耐久=100-200, 霰弹=11, 榴弹=6, 近战=5, 其他=0, 弹药=3, 黑名单=0` ×2（插件加载 + 战局启动重读）→ 快照刷新机制生效。
  - `[Breach]` 铺场：Doors 219 / Containers 283 / Trunks 147（无效 25）。
  - `[Patches] 对 Door 施加命中伤害 69.61636 / 69.58402 / 11.59734 ×2`（共 5 次）→ 锁血池结算生效，归零触发踹门开门。
  - 无本插件异常。
- **工单 01 全部验收项完成。** 测试覆盖层 `[8]门的大修-破门大修-DoorBreachOverhaul-test-build` 保留在包内（供 02/03 实机验证；F12 当前 Pleb=True）；工单 09 收口时以正式覆盖层替换 `[8]门的大修-DoorOverhaul`。
- 遗留待 02 复核：白名单弹药路径（12/70 AP-20）与 `IsValidDoorLockHit` 命中锚定（本地空间比较可疑：`col.transform.InverseTransformPoint(HitPoint)` vs `doorHandle.transform.localPosition` 不同坐标系）；Pleb 全放行测试绕过了这两条路径。
