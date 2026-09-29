---
version: [5.0]
domain: both
topic: migration
title: "SPT5 客户端 QoL 整合研究实战经验：1.1.5 符号核验 / 合并冲突面 / 部署基线陷阱"
keywords: [SPT5, 客户端, IL2CPP, interop, ilspycmd, Harmony, transpiler, 字段漂移, 多前缀冲突, 许可合规, 分支基线, copy-if-missing, MO2部署, 排序合并, 弹药FiR, 排序先合栈, OperationResult直调, TryRunNetworkTransaction, await桥, simulate校准]
summary: "SPT5 客户端 QoL 整合实战沉淀：1.1.5 interop 符号核验工作流（类清单 + ilspycmd；方法体=原生桩）与原生反汇编取证路径；字段/名称漂移实例集（dictionary_0→_slotViews、containedGridsView_0→containedGridsView、GameSettingsGroup 命名空间迁移、method_9/GClass 消失）；transpiler 的 IL2CPP 实际形态（代理桩、须改 prefix/postfix）；同方法多前缀冲突面（Ctrl+Click 三岔口：QMTC/CactusPie/UIFixes-MultiSelect 同挂 QuickFindAppropriatePlace）；「客户端行为≠原版」再证（原版 Sort 从不合并；弹药 SpawnedInSession 恒 false）；许可矩阵前置（GPL 传染、快照-页面矛盾）；部署基线陷阱（错误分支回退 master 修复）与 copy-if-missing 数据陈旧（臂章 22 vs 37、git hash-object 判定升级）；MO2 overlay 离线部署流程；探针残缺声明与主控台直研回退；排序先合栈（StackFirst）移植全链路（镜像游戏自身 SortAsync 路径：值类型 OperationResult 直调链 + TryRun/await 桥 + simulate 以目标版本原身为准；执行策略先裁决再实现）。"
---

# SPT5 客户端 QoL 整合研究实战经验：1.1.5 符号核验 / 合并冲突面 / 部署基线陷阱

> 来源：2026-09-27/28 会话（Inescapable-Tarkovs-Softcore 臂章槽功能交付 + Betters Norvinsk QoL 整合预研）；2026-09-29/30 会话（Betters Norvinsk QoL M1：工单 02 Swap 全链路 + 工单 03 排序先合栈闭环，产物 `.scratch/m1-uifixes-subset/issues/03-sort-stack-first.md`）。
> 报告产物（toolkit `docs/research/`）：`ui-fixes-1342-spt5-port-analysis.md`、`spt5-sort-ammo-merge-mechanism.md`、`qol-mod-consolidation-feasibility.md`。
> 前置笔记：`pilot-experience-inescapable-softcore.md`（T1–T8 / W1–W4）。

## 一、客户端（1.1.5 / IL2CPP）域

### C1 符号核验工作流（移植物第一动作）
- 类清单：`knowledge/spt-kb/archive/eft-1.1.5/classes-1.1.5.txt`（16,435 类型）→ grep 类名先行。
- 单类型反编译：`ilspycmd -t "<Full.Type.Name>" "<SPT_5xx>\BepInEx\interop\Assembly-CSharp.dll"`。
- interop 事实：类名/命名空间保留（≈95% 与 4.1 同名）；**方法体为原生调用桩**；`ref/out/virtual/参数名` 在签名里可见——**这就是 Harmony 挂钩的依据**（prefix 参数按名绑定；`ref` 前缀参数可改写原方法 by-value 参数，QMTC 在 4.1/1.1.5 同构实证）。
- 旁路程序集独立：如 `BSG.GameSettings.dll` 提供 `StateGameSetting<T>`（`GameSettingsGroup` 本体则已迁入 Assembly-CSharp，见 C3）。

### C2 方法体取证（原生反汇编路径）
- 链路：Il2CppDumper `dump.cs`（取 RVA）→ `GameAssembly.dll` 按 RVA 反汇编（产物示例 `D:\Temp\opencode\il2cpp-re\`、`spt5-*-disasm.txt`）；判读「全函数 call 目标 RVA 集合」建立调用图；Cpp2IL ISIL 制品可读托管调用点（制品可能不完整——缺文件时回退到 RVA 反汇编）。
- 实例（排序链）：1.1.5 `ItemManipulator.Sort` 全函数 call 目标只有 `ItemSorter.Sort` + `Grid.AddAnywhere`——**不含任何 Merge/TransferOrMerge/TryFindMergeableItem/Ammo.ApplyToAmmo**（与 0.16 / 0.16.9.5 C# 反编译结论一致）。

### C3 字段/名称漂移实例（移植必查清单范式）
| 4.1 | 1.1.5 | 性质 |
|---|---|---|
| `ContainersPanel.dictionary_0` | `_slotViews` | 字段改名（AutoDeposit 注入参数必改） |
| `SearchableItemView.containedGridsView_0` | `containedGridsView` | 编译期后缀名消失 |
| `Bsg.GameSettings.GameSettingsGroup` | `EFT.Settings.Game.GameSettingsGroup` | 命名空间迁移（`EPriorityWindowMode` 随迁） |
| `ChatScreen.method_9` | 不存在（1.1.5 无 `method_*` 残留） | 位置性混淆名不可迁移 |
| `GClass1583`（`DestroyItemWarning`） | 未命中 | GClass 体系消解，需重定位 |
- 规则：**逐符号核验，勿信旧名**；`method_N` / `GClass*` / 编译期字段后缀（`_0`）/ 按类型 `Single()` 反射选成员——全部是版本升级危险源。

### C4 transpiler 在 IL2CPP 的实际形态
- 形式受支持（Il2CppInterop.HarmonySupport 复制「未空心化」方法体后套 Harmony），但**作用于代理桩而非游戏原生逻辑**：4.x 的 IL 模式改写（如 UIFixes 的 `Ldc_I4_4 → Bne_Un_S`）不可复刻。
- 移植对策：prefix/postfix 等价实现或重选评价点；风险面快速量化 = 统计目标 mod 的 `[PatchTranspiler]` 数量（UIFixes = 5 文件）。

### C5 同方法多前缀 = 合并 mod 的第一冲突面
- 实例：`ItemManipulator.QuickFindAppropriatePlace` 同挂 **QMTC（追加开窗目标到 `targets`）/ CactusPie（战局内 @loot 路由并短路）/ UIFixes-MultiSelect（改 `EMoveItemOrder` 位，`[HarmonyPriority(Priority.Last)]` 显式排后）**。
- 规则：合并咨询**先出「Harmony 目标矩阵」**（方法 × 挂载者 × 前后缀 × 优先级 × 短路语义），再把同方法的多前缀统一为单一决策链。

### C6 「客户端行为 ≠ 原版」再确认（T5 复现）
- 原版排序（0.16 → 0.16.9.5 → 1.1.5）**从不合并**；弹药 `SpawnedInSession` 自 4.1 起硬编码 false（1.1.5 原生 `xor al,al; ret`）→ 形成「子弹总能合并、他物受 FiR 门限」的判别式；**排序合并只能来自客户端插件**（UIFixes StackFirst / Seion IOF / Advanced-Stash-Sorting）。
- 排查手法：实例插件清单（`BepInEx\LogOutput.log` 的 Loading 行）+ 对插件 DLL 做字符串扫描（`GridSortPanel` / `TransferOrMerge` / `StackBeforeSort` / …）。

### C7 许可矩阵前置（合并的硬约束）
- 先做 license 矩阵再谈合并：本轮 = MIT×3（UIFixes / QMTC / AutoDeposit）+ NCSA（IOF；快照无 LICENSE 文件，须上游确认）+ **GPL**（CactusPie：快照正文 v2 vs Forge 页面 v3 矛盾）。
- 传染规则：GPL 代码并入 → 整包须 GPL 兼容发行；否则排除该件（独立插件共存或干净重写）。
- 页面标注与快照 LICENSE 冲突时：以快照/上游仓库为准并**把矛盾记录下来**。

### C8 UIFixes Swap 移植实战：CanAccept 危害族与 drop 侧重设计（三层根因，2026-09-29）

- **现象**：移植后拖动物品必闪退（coreclr 0xc0000005 同偏移；MO2 禁用 mod 即不崩）。探针点杀：崩点在「原方法在 detour 包装下被调用」环节（前缀 `return true` 放行后、后缀前）。
- **根因一（危害族）**：`OperationResult` 为非 blittable 值类型；对含其 **out 参数/返回值**的方法（`DragItemContext.CanAccept`、`GridView.CanAccept`、`Weapon.Apply`）打 Harmony detour → trampoline 结构体封送崩（`5xx-client-mod-dev-lessons.md` §16 同族实锤）。**修复：危害族不打 detour**；接受/执行语义改「drop 侧自执行」——`ItemView.OnEndDrag`（安全签名）+ 拖拽期采样的悬停目标 → `ItemManipulator.Swap` → `RollBack()` → `RunNetworkTransaction`。
- **根因二（松开时序）**：`OnEndDrag` 前游戏会发一次「清除更新」（`_currentContainerUnderCursor/_currentTargetItemContext → null`）；postfix 同步缓存会被覆盖为 null。**修复：拖拽期帧采样**（`DraggedItemView.Update` + `Input.GetMouseButton(0)` 门控；松开后残留帧不采样、不清除）。
- **根因三（合成上下文）**：`GridView.CalculateItemLocation(DragItemContext)` 依赖上下文的 `CursorPosition/ItemPosition/ItemRotation`；自建上下文未 `SetPosition` → 落点错 → 网格互换模拟必败（槽位路径无需落点故不显）。
- **通用教训**：① 接口/基类声明字段的运行时包装为 `Il2CppProxy`，`is/as` 对具体类型**恒失败**——一律 `TryCast<T>()`（原生判定，见 `Il2CppObjectBase.TryCast`）；② 「守卫级/相位级探针 + 同帧抑制」点杀法为最高效收敛手段（本例 3 轮精准定位）；③ `LogOutput.log` 每次启动覆写——**未重启的崩溃现场是金矿**；④ 修复顺序纪律（一次只变一个因子 + 探针兜底）避免多变量混淆。

### C9 排序先合栈（StackFirst）移植全链路：镜像游戏自身路径（2026-09-29/30）

- **背景**：上游 v6.0.3 `SortPatches.StackFirstPatch`（Mono 时代）→ 1.1.5 IL2CPP；产物 `BettersNorvinskQoL.Client/Modules/Stacking/`（`SortPatches` / `Sorter` / `StackingSettings`）；全链路闭环 = 执行策略裁决 → 实现 → 双轴审查 → 投影 → 游戏内验收 → 结项（工单 03 含符号矩阵与验收证据）。
- **决策法（本单最高价值经验）**：实现前**先裁决执行策略**，以「游戏自身怎么做」为权威蓝图——1.1.5 `GridSortPanel.SortAsync` ISIL 实证 = `ChangeProgress(true) → ItemManipulator.Sort(_item, _controller, simulate=1) → 虚调 TryRunNetworkTransaction → ChangeProgress(false)`（0.16 / 0.16.9.5 反编译同构）→ 定案「镜像」：合并与排序均 `simulate:true` → `Succeeded` → `await TryRun`。**上游的 `simulate:false` 系 Mono 时代债，修正为 `true`**。
- **执行面形态**（通用化见 `5xx-client-mod-dev-lessons.md` §18）：值类型 `OperationResult<T>` **只调不 detour**；泛型→非泛型隐式转换 + `await` Il2Cpp `Task<IResult>`（Il2CppInterop await 桥）**实机验证可用**；「跑一笔并等待」收敛为单点 `Execute<T>`（为降级预案预留：`TryRun(op, 回调)` + BCL `TaskCompletionSource`）。
- **替代方案裁剪**：不移植上游 `NetworkTransactionWatcher`（自建 detour 机械）与 `OperationQueuePatch`（范围外）；先求证「原生是否已有内建机制」——TryRun 即内建自持序列化（`<>c__DisplayClass148_0` = TCS + 回调）。
- **验收取证**：owner 实测通过 → 反查日志证据链（序列成对 + 插件域 0 错误 + Windows 事件日志窗口零崩溃）；陌生日志噪声（TMP `GenerateTextMesh` × 藏身处生产界面）经 `Player.log` 堆栈归因后挂观察项（见 lessons §19）。
- **流程沉淀**：① 高风险执行策略**先裁决再实现**（省一轮返工；oracle 会话复用效果佳）；② 每轮「构建门 → 投影（游戏关闭时）→ owner 验证」；③ 探针「首轮在、结项清」。

## 二、交付/部署域（Inescapable-Tarkovs-Softcore 延伸）

### D1 构建/部署前必须校验分支基线
- 事故案例：从 `softcore-chain`（master 的**祖先**）构建并部署 → 把 master 上 10 个提交的 R1 修复（安全箱 15 员家族 / 臂章 37 等）**整体回退**。
- 规则：部署前跑 `git merge-base --is-ancestor A B` + `git log A..B` + `git log B..A`（两边都看）；「当前检出分支 ≠ 最新线」是本类事故的根因（KB W1–W4 的延伸）。

### D2 copy-if-missing 的数据陈旧陷阱
- 机制：`data/**` 磁盘优先 + 部署 copy-if-missing → **旧数据文件永久压过新版内嵌表**（实测臂章表 22 条 vs master 37 条；涉 7 个文件）。
- 判定与升级：`git hash-object`（**带过滤器**，留意 autocrlf 差异）+ `git cat-file -e` 判定「历史原版 vs 玩家手改」；升级前整体备份；建议构建脚本内建「哈希感知升级」。
- 教训：**升级语义必须在部署清单里显式走一遍**，「文件已存在」≠「已是最新」。

### D3 MO2 overlay 离线部署流程（本会话已验证）
进程预检（游戏/服务器/启动器关闭）→ 备份（旧 DLL / 数据 / meta）→ 覆盖 DLL + 新增插件目录 → 数据按需升级 → `mo2_set_mod_notes`（plan→apply，原子写 + 快照）→ `mo2_modlist` 读回确认启用 → 哈希与条目全量校验。
- 基线切换技法：存量未提交改动先 `git stash`（tracked-only，untracked 随行），再 checkout 目标基线；stash 兼作回滚备份。
- **路径陷阱**：暂存目录名含 `[序号]`（如 `[5]核心-…`）时 PowerShell 方括号 = 通配符 → 文件操作一律 `-LiteralPath`（否则静默空结果/误匹配）；DLL 覆盖须游戏关闭（运行中锁文件），MO2 可保持开启。

## 三、研究/协作域
- **R1 探针模式**：explorer 本地考古 + **残缺快照显式声明**（QMTC 仅剩 bin/obj 的案例：声明 `[FAIL]`，主控台改从 GitHub 直取 `@039132f` 补全；不猜不编）。
- **R2 模型配额韧性**：librarian 模型周配额耗尽时任务即刻失败 → 主控台直研回退（本轮 2 次失败均如此）；「探针失败 ≠ 证据缺失」。
- **R3 验证纪律**：主控台抽验探针的关键证据文件（disasm / dump.cs / RVA 与原文比对）；报告区分「第一手核验 ✓ / 待逐验 ○」。

## 相关
- `pilot-experience-inescapable-softcore.md`（前置：T1–T8 / W1–W4）
- `curated/modding-standard/13-perf-security.md`（补丁工程原则：全 Prefix/Postfix、禁 Transpiler 的既有先例）
- `curated/migration/pilot-experience-lootingbots.md`（客户端混淆名映射方法论）
- toolkit `docs/research/` 三份报告（UIFixes 移植 / 排序合并机制 / QoL 整合可行性）
