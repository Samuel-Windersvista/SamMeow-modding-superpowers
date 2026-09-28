# QoLMOD 整合可行性研究 —— UIFixes + QuickMove + AutoDeposit + IOF + CactusPieTransfer → 单一 SPT5 mod

> 日期：2026-09-28
> 问题：把 **UIFixes（#1342）** 与 **Quick Move To Containers（#1341）**、**AutoDeposit（#1469）**、**IOF - Inventory Organizing Feature（#2960）**、**CactusPie's Transfer Loot Into Container Automatically（#3023）** 合并成一个 SPT5 mod：可能性、哪些功能还能用、哪些已不合时宜、若不能则为什么。
> 方法：本地 Forge 归档源码快照（4/5 完整；QMTC 快照残缺 → 主控台从 GitHub `DrakiaXYZ/SPT-QuickMoveToContainer` @`039132f` 直取源码）+ 后台探针勘探（exp-3/exp-4）+ **SPT 5.0 目标实例 1.1.5 interop 逐符号核验**（ilspycmd；UIFixes 部分见另一报告）。
> 关联报告：`docs/research/ui-fixes-1342-spt5-port-analysis.md`（UIFixes 功能谱/移植分级）、`docs/research/spt5-sort-ammo-merge-mechanism.md`（排序合并机制）。

---

## 1. 结论摘要（先读这段）

**技术判定：可行（有条件）** —— 五个 mod 全部可在 SPT5 合并为**单一客户端插件**（`BasePlugin` + 分区段配置 + 统一决策层），类名面 95% 保留、四 mod 的全部关键符号已在 1.1.5 逐一命中（§5）。

**三条硬约束（决定「怎么合并」而非「能不能合并」）：**

1. **Ctrl+Click 三岔口**：`ItemManipulator.QuickFindAppropriatePlace` 上同时挂着 **QMTC（append 开窗目标）+ CactusPie（战局内 @loot 路由）+ UIFixes-MultiSelect（改 EMoveItemOrder 位）** 三个前缀（UIFixes 用 `Priority.Last` 明确约定「在 QMTC 之后跑」）。合并版必须把这三个前缀**统一为单一决策函数**（明确优先级与短路语义），否则按键行为不可预期。
2. **许可混合**：UIFixes / QMTC / AutoDeposit = **MIT**；IOF = Forge 标 **NCSA**（快照无 LICENSE 文件，**需向 GitLab 上游核实**）；**CactusPie = GPL**（快照 LICENSE 正文为 **GPLv2**，Forge 页面标注 **GPLv3** —— 版本矛盾需裁定）。**GPL 代码并入 = 整包需以 GPL 兼容方式发行**。二选一：整包 GPL（署名保留）/ 排除 CactusPie（保持其独立插件或对行为做干净重写）。
3. **全体无 SPT5 版本**：五个 mod 上游分支均只有 `master`/`main`（无 4.1 之后分支、无 IL2CPP 工程）→ 全部**需要自行移植**，无现成 5.0 件可直接取用。

**功能存废速览**（详见 §6）：
- **仍值得要（保留）**：QMTC 的「Ctrl+Click 进已开容器」；AutoDeposit 的「按钮式归位到同物容器」；IOF 的 `@o` 富语法整理 / `@sl`/`@ml` 锁 / ALL / Take Out；CactusPie 的「战局内 @loot 自动归位 + 拾取归位」；UIFixes 的完整 QoL 面（含排序先合栈）。
- **需重写/重构（不是废）**：AutoDeposit 的反射层（`GClass1583` 在 1.1.5 消失、`containedGridsView_0`→`containedGridsView`、`dictionary_0`→`_slotViews`、按类型 `Single()` 选成员）；UIFixes 的 5 个 transpiler 补丁；IOF 的 StackTrace 调用方嗅探与 UI 运行时克隆。
- **待运行时重评（可能已不合时宜）**：QMTC 的「禁用 PriorityWindowMode」补丁（1.1.5 是否仍需）；IOF 的标签字符上限提升（原版上限是否已变）；UIFixes 各「BSG/SPT 已实现即删」候选（见报告 B §4.6-E）。

---

## 2. 对象与来源

| Mod | Forge | GUID（运行期） | 作者 | 许可 | 快照版本 / 目标 | 源码可得性 |
|---|---|---|---|---|---|---|
| UI Fixes | #1342 | `com.tyfon.uifixes` | Tyfon | MIT | v6.0.1 / SPT 4.1（4.1.6 用 6.0.x，最新 6.0.3） | 本地完整 ✓ |
| Quick Move To Containers | #1341 | `xyz.drakia.quickmovetocontainer` | DrakiaXYZ | MIT | v1.5.0 / SPT 4.1.0+（页面标 4.1.6） | 本地**残缺**（仅 bin/obj）→ GitHub `DrakiaXYZ/SPT-QuickMoveToContainer`@`039132f` 直取 |
| AutoDeposit | #1469 | **`Tyfon.AutoDeposit`**（Forge 标 `com.tyfon.autodeposit`，不一致） | Tyfon | MIT | v5.0.0 / SPT 4.0（页面标 4.1.6 有 6.0.0） | 本地完整 ✓（commit `4060a12`） |
| IOF - Inventory Organizing Feature | #2960 | `flir.iof` | flir（原作 Nightingale 系） | **NCSA**（Forge 标注；快照无 LICENSE 文件） | v1.9.0 / SPT 4.1.3–4.1.6 | 本地完整 ✓（GitLab `flir063-spt/inventoryorganizingfeatures`） |
| CactusPie's Transfer… | #3023 | `com.cactuspie.containerquickloot.cqlv4` | hayk4500（原作者 CactusPie） | **GPL**（快照正文 v2 / Forge 标 v3） | v1.9.0–1.9.1 / SPT 4.1.5 | 本地完整 ✓ |

> 风险提示（来自 Forge 页面）：**IOF 与 CactusPie 都会对 profile 做持久化改动**（IOF 的标签存于物品 tag；CactusPie 的 `@loot` 标签同理）——整合 mod 需继承「可安全移除/回滚」的说明义务。

---

## 3. 各 mod 功能谱（要点）

**QMTC（Ctrl+Click 定向入容器）**
- `Ctrl+Click` 把物品移入**已打开的容器窗口**（默认遍历全部开窗；可配仅最顶层）；两个开容器间也可互移（弹药箱互倒）。
- 机制：前缀 `ItemManipulator.QuickFindAppropriatePlace`——当 order 含 `MoveToAnotherSide` 时，把开着的 `GridWindow` 的 `CompoundItem` 追加进 `targets`；另有补丁禁用 `PriorityWindowMode` 设置。
- 配置：`Target All Open Containers`（默认 true）。

**AutoDeposit（Terraria Quick Stack 式归位）**
- 在**装备栏**（pockets/rig/backpack/secure）与**转移屏**上加按钮；点击后把物品移入**仓库中已含有同 `TemplateId` 物品的容器**；递归处理嵌套容器；空容器可整体转移、非空跳过；**仅局外**。
- 机制：`ContainersPanel.Show` postfix（注入按钮）+ `TransferItemsScreen.Show` postfix；重反射层（R.cs）；0 transpiler。
- 配置：5 个按钮开关（默认全开）。

**IOF（整理/锁定富功能）**
- `@sl` 排序锁（排序时钉住）、`@ml` 移动锁（含拖拽与快捷移动）、`@o` 整理（类别/名称/取反/次序/`--fir`/`--not-fir`/布尔表达式）+ `ORG.` 按钮 + `ALL` 整仓整理 + `Take Out` 倒出 + 标签字符上限提升。
- 机制：10 个补丁/12 个 Harmony 方法（`ItemManipulator.Sort` 钉锁、`ItemUiContext.QuickFindAppropriatePlace` 拦 `@ml`、`GridSortPanel.Show` 注入按钮、`ItemView.OnPointerDown/OnBeginDrag` 拦拖拽、多处 Show/Close 挂接）；0 transpiler、无 LINQ、无 GClass 实引用（4.1 起移除反射层）。

**CactusPie Transfer（战局内自动归位）**
- 战局内 `Ctrl+Click` → 物品投进带 `@loot` 标签的匹配容器（后缀数字越小越优先）；散落拾取（`PickUp`）同规则；任务物品不拦；堆叠可自动合并（**不检查 FiR**）；另有「非 loot 容器堆叠合并」。仅战局内生效（`GameWorld.LocationId`）。
- 机制：单一前缀 `ItemManipulator.QuickFindAppropriatePlace`（字符串名反射取方法）。

**UIFixes**：87 补丁全谱见报告 B §2；与本整合相关的高频面 = Multiselect/快速移动与高亮、**排序先合栈**（`GridSortPanel.Sort` 前缀）、`Item.IsSameItem` 放宽（FiR 可配忽略）、大量 UI/商人/跳蚤/检视/邮件修复。

---

## 4. 技术整合面

### 4.1 Harmony 目标矩阵（同方法冲突总览）

| 目标方法 | 挂载者（前缀/后缀） | 冲突等级 |
|---|---|---|
| **`ItemManipulator.QuickFindAppropriatePlace`** | **QMTC（改 targets）＋ CactusPie（战局内拦截并自处理）＋ UIFixes-MultiSelect（改 order 位，`Priority.Last`）** | **高——必须统一为单一决策链** |
| `GridSortPanel.Sort` | UIFixes（StackFirst：先 StackAll 再 Sort） | 中（与 IOF 的语义交互，见 4.2-②） |
| `ItemManipulator.Sort` | IOF（`@sl` 钉锁 prefix+finalizer） | 低（UIFixes 只是调用方；链式共存可行） |
| `ItemUiContext.QuickFindAppropriatePlace`（UI 层） | IOF（`@ml` 拦快捷移动） | 低 |
| `GridSortPanel.Show` | IOF（克隆按钮注入 ORG./ALL/T-O） | 中（与 UIFixes/ AutoDeposit 的 UI 注入面相邻） |
| `ContainersPanel.Show` / `TransferItemsScreen.Show` | AutoDeposit（注入按钮） | 低 |
| `ItemView.OnPointerDown` / `OnBeginDrag` | IOF（拖拽拦截） | 低（UIFixes 也有拖拽类补丁，不同方法） |
| `Item.IsSameItem` | UIFixes（FiR 合并放宽） | 低 |
| `MenuTaskBar.InitHandbook` / `MenuScreen.Init` / `EditTagWindow.Show` / `SimpleStashPanel.Close` / `TraderScreensGroup.Close` | IOF | 低 |
| `GameSettingsGroup..ctor` | QMTC（禁 PriorityWindowMode） | 低（1.1.5 命名空间已迁移，见 §5） |

### 4.2 合并必须解决的四项行为交互

1. **Ctrl+Click 单键多语义**（最高优先）：现状 = QMTC「进开着的容器」、CactusPie「战局内进 @loot 容器」、UIFixes MultiSelect「批量快速移动/装弹」、IOF「@ml 禁止移动」。合并版需定义**单一决策顺序**（建议：`@ml 拦截` → `战局内 @loot 优先` → `开窗容器目标` → `默认快速移动/多选`），并以补丁优先级显式钉死。
2. **排序管线语义**：UIFixes 的 StackAll 在 `ItemManipulator.Sort` **之前**、且**不读 `PinLockState``**——IOF 的 `@sl` 只在 Sort 内生效 → 合并版须**让 StackAll 尊重 `@sl`/`@ml`**（否则排序锁用户可感知地被绕过）。
3. **堆叠/FiR 政策统一**：三套并存——UIFixes `MergeFIROther`（默认 false，FiR 相同才合）、IOF `CanBeStacked`（强制 FiR 相等，防 FiR 污染）、CactusPie（**无 FiR 判定**）。合并版应有**单一 FiR 政策开关**，并在 CactusPie 路径补上守卫。
4. **UI 注入协作**：IOF 克隆 `GridSortPanel` 按钮（名字去重已有）、AutoDeposit 克隆面板按钮、UIFixes 有 `GridWindowButtonsPatch`/高亮 → 统一排序键（sibling index）与去重约定，避免按钮打架。

### 4.3 建议架构（单插件多模块）

```
QoL 合并 mod（单 DLL）
├─ Core：统一配置（单 JSONC/ConfigFile，分 section）、统一日志、统一按键语义决策层（Ctrl+Click Router）
├─ Modules/
│  ├─ MultiSelectQuickMove（UIFixes 系：多选/高亮/快速移动）
│  ├─ OpenContainerTargeting（QMTC）
│  ├─ StashAutoDeposit（AutoDeposit：按钮+归位）
│  ├─ OrganizeLocks（IOF：@o/@sl/@ml、ORG/ALL/T-O 按钮）
│  └─ RaidLootRouting（CactusPie：@loot/拾取归位）
└─ Patches/：按 §4.1 矩阵声明，Ctrl+Click 相关三个前缀合并为 1 个
```
- 二选一发行策略：**全 GPL 合包**（含 CactusPie）或 **MIT 合包 + CactusPie 独立/重写**。

---

## 5. SPT5（1.1.5）符号核验（第一手）

> 核验方式：`ilspycmd` 对 `BepInEx\interop\Assembly-CSharp.dll`（+`BSG.GameSettings.dll`）反编译，逐符号比对；○ = 未逐验/需运行时复核。

| 符号（4.1 目标） | 1.1.5 状态 | 备注 |
|---|---|---|
| `ItemManipulator.QuickFindAppropriatePlace(item, controller, targets, order, simulate)` | ✓ | 参数名一致（`ref` 前缀绑定可行） |
| `ItemManipulator.Sort` / `Merge` / `TransferOrMerge` / `TryFindMergeableItem` | ✓ | 排序链核心（报告 A 亦证） |
| `EMoveItemOrder.MoveToAnotherSide` | ✓ | `= TryTransfer \| PrioritizeTargetsOrder` |
| `ItemUiContext._windows`（List<WindowData>）/ `WindowData.Window` / `ItemUiContext.Instance` / `ContextType` | ✓ | QMTC 读窗依赖 |
| `GridWindow._item` / `_sortPanel` | ✓ | |
| `GameSettingsGroup`（**命名空间迁移：`Bsg.GameSettings` → `EFT.Settings.Game`**）/ `EPriorityWindowMode.Disabled` / 公开 ctor | ✓ | QMTC 第二补丁需改 `using` |
| `StateGameSetting<T>`（`BSG.GameSettings.dll`）+ 匹配 ctor + 可覆写 `SetValue` | ✓ | QMTC 自定义设置类可保留 |
| `ContainersPanel.Show(...)` | ✓ | AutoDeposit 挂钩点 |
| `ContainersPanel` 槽位字典字段 | **改名：`dictionary_0` → `_slotViews`** | AutoDeposit 注入参数需改 |
| `TransferItemsScreen._itemsToTransferGridView` / `Show(...)` | ✓ | |
| `SearchableSlotView._searchableItemView` | ✓ | |
| `SearchableItemView` 网格视图字段 | **改名：`containedGridsView_0` → `containedGridsView`** | AutoDeposit 需改 |
| `ItemUiContext._gridWindowTemplate` / 按类型取 InventoryController 字段（1 处命中） | ✓ / ○ | 按类型 `Single()` 需按 1.1.5 成员数复核；ItemContextAbstractClass 属性本次未命中（改由运行时/复核确认） |
| `DestroyItemWarning`（4.1 `GClass1583`） | **✗ 未命中** | 需重定位替代（或经 `UIExtensions+_TryShowDestroyItemsDialog` 路径改造） |
| `ItemUiContext.QuickFindAppropriatePlace`（UI 层） | ✓ | IOF `@ml` 拦截点 |
| `ItemView.OnPointerDown` / `OnBeginDrag`（virtual） | ✓ | IOF 拖拽拦截 |
| `Item.PinLockState` /（`EItemPinLockState`） | ✓ / ○ | IOF `@sl` 机制依赖 |
| `MenuTaskBar.InitHandbook(Handbook)` / `MenuScreen.Init(EnvironmentUI)` / `EditTagWindow.Show(TagComponent)` / `TagComponent` | ✓ | IOF 挂点 |
| `SimpleStashPanel.Close()` / `TraderScreensGroup.Close()` | ✓（override） | IOF 挂点 |
| `GridSortPanel`（类）/ `Show` 方法 | ✓ / ○ | IOF 按钮注入 |
| `Grid.ContainedItems` / `FindLocationForItem` | ✓ | CactusPie 合并路径 |
| `GameWorld.LocationId` | ✓ | CactusPie 战局判定 |
| `MergeResult` / `MoveResult` / `IItemOperationResult` / `ITransferOrMergeResult` / `InventoryEquipment` | ✓ | |
| `EOwnerType` / `UIElement.UI` / `UIInputNode.UI` / `AddDisposable` | ○ | 细节待逐验（低风险） |
| UIFixes 全量符号 | 见报告 B §4.2（40+ 全命中；`ChatScreen.method_9` ✗ 需重定位） | |

---

## 6. 功能存废清单（还能用 / 需重写 / 已不合时宜）

**A. 直接有价值、保留（移植后可用）**
- QMTC：Ctrl+Click 进开窗容器（+ 全开窗扫描选项）。
- AutoDeposit：装备栏/转移屏按钮 + 同物归位（含嵌套容器）。
- IOF：`@o` 富语法整理、`@sl`/`@ml`、ALL、Take Out、（标签上限提升默认关或更新）。
- CactusPie：战局内 @loot 路由、拾取归位、堆叠合并（需补 FiR 守卫）。
- UIFixes：除「已由原版覆盖」候选外的全部 QoL（报告 B §2）。

**B. 需重写/重构（保留功能、改实现）**
- AutoDeposit：反射层整体重写（`GClass1583` 消失、`containedGridsView_0`/`dictionary_0` 改名、按类型选成员改按名/签名）；net471→net6.0、BepInEx5→6。
- UIFixes：5 个 transpiler 补丁改 prefix/postfix 等价（报告 B §4.3）。
- IOF：StackTrace 调用方嗅探改显式传参；UI 运行时克隆需按 IL2CPP 重验；`PinLockState` 翻转机制按 1.1.5 排序语义复核（报告 A 已证 Sort 只收 Free 项——机制方向一致）。
- QMTC：`_windows` 泛型列表在 IL2CPP 下的读取方式（`Il2CppSystem.Collections.Generic.List` vs 托管 IList 反射）。
- CactusPie：字符串名取方法改 `AccessorTools`/签名定位；LINQ→显式循环（iof 风格）可选。

**C. 待运行时重评（可能「已不合时宜」）**
- QMTC `DisablePriorityWindowPatch`：1.1.5 的 PriorityWindowMode 行为是否仍需要禁用（若原版已改，可删）。
- IOF 标签字符上限提升：1.1.5 原版上限是否仍为 16。
- UIFixes 修复类中「BSG 已实现」项（该 mod 历史上多次删功能；1.1.5 需逐项重放，尤其与 1.1.5 新 UI 冲突的）。
- AutoDeposit 的 `WaitForEndOfFrame` 时机（IL2CPP 帧序）。
- 任一 mod 的功能如被 SPT5 原版或更现代的 QoL 实现覆盖 → 砍掉，减小合并面。

**D. 建议不并入（或需特别决策）**
- CactusPie 的**代码并入**（GPL 传染）——除非整包接受 GPL；否则保持独立插件或干净重写。
- IOF 的 `ReferencePackage.csproj` CI 私源机制（与本仓库构建体系无关，勿带）。

---

## 7. 许可合规（合并发行前必办）

| 组合路径 | 结论 |
|---|---|
| **整包 GPL**（含 CactusPie 代码） | 可行：MIT/NCSA 均可并入 GPL 作品；须保留全部署名（Tyfon/DrakiaXYZ/flir/hayk4500+CactusPie/Nightingale 系）。**须先裁定 GPLv2 还是 v3**（快照 vs Forge 矛盾——建议向上游仓库确认 LICENSE 全文）。 |
| **整包 MIT**（排除 CactusPie） | 可行：MIT×3 + NCSA（确认后）兼容；CactusPie 以独立插件共存（其 GPL 不传染独立进程组件）或干净重写（仅按行为描述重写、不参考其代码实现）。 |
| IOF (NCSA) 确认 | **必办**：快照无 LICENSE 文件；从 GitLab 上游取 LICENSE 全文并归档。 |

---

## 8. 建议路径（若立项）

- **M1（整合骨架 + Ctrl+Click 统一）**：单插件骨架（BepInEx6/net6.0/SPTushonka）、统一配置与日志、**三岔口决策层**、QMTC 全功能 + CactusPie 行为（先按 GPL 或重写二选一）——最高风险面最先收敛。
- **M2**：IOF 整理/锁体系 + UIFixes Inventory/Multiselect 子集（含「StackAll 尊重 @sl」行为修正）。
- **M3**：AutoDeposit + UIFixes 其余域（商人/跳蚤/检视/邮件）+ 服务端组件（若 UIFixes.Server 纳入）。
- 全程维护「1.1.5 原版行为基线」，对 §6-C「待重评」逐项进游戏定夺。

## 9. 不确定性与边界

- 全部为**离线静态分析**；无任何一项在 SPT5 实机运行。合并后的按键语义/排序行为/UI 注入均需实机验证。
- QMTC 本地快照残缺（以 GitHub master 为准；快照对应 4.1.0 基线，页面标注 4.1.6 兼容）。
- IOF 许可、AutoDeposit 的 Forge/运行期 GUID 不一致（`com.tyfon.autodeposit` vs `Tyfon.AutoDeposit`）均待上游确认。
- 1.1.5 方法体不可读：§5 的「✓」代表符号/签名存在，不保证行为等价；标 ○ 项未逐验。
- UIFixes 部分的移植面见报告 B（含 5 transpiler 与 dep 风险）；本报告不重复。

---

## 附录：证据坐标速查

- QMTC 源码：`github.com/DrakiaXYZ/SPT-QuickMoveToContainer` master @`039132f`（`QuickMovePlugin.cs` / `Helpers/Settings.cs` / csproj）；Forge 页 `sp-mod.com/mod/1341/…`。
- AutoDeposit：`…/mods/AutoDeposit_1469_source/`（`Patches/AddInventoryButtonsPatch.cs:28`、`Patches/AddTransferButtonPatch.cs`、`R.cs:36-133`、`AutoDepositPanel.cs:95`）。
- IOF：`…/mods/IOF-Inventory-Organizing-Feature_2960_source/`（`Patches/PreItemManipulatorSort.cs:34`、`Patches/PostGridSortPanelShow.cs:19`、`Features/OrganizedContainer.cs:99-160`、`CHANGELOG.md`）。
- CactusPie：`…/mods/CactusPie's-Transfer-…-3023_source/`（`QuickTransferPatch.cs:18-120`、`LICENSE`）。
- 1.1.5 核验产物（临时目录 `D:\Temp\opencode\`）：`v2-itemuicontext.cs`、`v2-gridwindow.cs`、`v-itemmanipulator.cs`、`v-item.cs`、`v2-gsg2.cs`、`v2-sgs.cs`、`v3-*.cs` 系列。
