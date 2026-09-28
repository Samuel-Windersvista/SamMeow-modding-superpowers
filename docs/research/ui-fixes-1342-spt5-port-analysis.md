# SPT 4.1.6「UI Fixes」（Forge #1342）功能勘探与 SPT 5.0 移植变化分析

> 日期：2026-09-28
> 对象：**UI Fixes**（Forge id `1342`；GUID `com.tyfon.uifixes`；作者 Tyfon；链接 https://sp-mod.com/mod/1342/ui-fixes ⇔ https://forge.sp-tarkov.com/mod/1342/ui-fixes）
> 方法：本地 Forge 归档（元数据 + **全量 38 版本 changelog**）+ 归档内**完整源码快照**（`knowledge/spt-kb/archive/forge/mods/UI-Fixes_1342_source`，v6.0.1 / commit `dc5af6b`）+ GitHub（README / branches / releases）+ SPT 5.0 目标实例 **interop 一手核验**（`ilspycmd` + 类名清单）
> 快照锚定：源码 = **v6.0.1**（2026-08-09，目标 SPT 4.1）；GitHub 最新 = **v6.0.3**（2026-09-27）；**「4.1.6」对应 UIFixes 6.0.x 线**（hot-index：`best_spt: ~4.1`、`best_version: 6.0.1`）
> 用途：回答「这个 mod 具体做什么」+「移植到 SPT 5.0，功能上会有哪些变化」（仅报告，不做实现）。

---

## 1. 概览

| 项 | 值 |
|---|---|
| 性质 | 客户端 BepInEx 插件（`Tyfon.UIFixes.dll`）+ **配套服务端组件**（`UIFixes.Server`） |
| 规模 | 客户端 `src/` 136 个 .cs（其中 `src/Patches/` **87 个补丁文件**）；服务端 27 个 .cs（补丁 + 路由） |
| 依赖 | 硬：ConfigurationManager；软：**Fika**（`com.fika.core` ≥1.1.3）、PIT Fireteam（`xyz.pit.fireteam`）、MergeConsumables（`com.lacyway.mc`） |
| 许可 | MIT License（Copyright (c) 2025 Tyfon） |
| 下载量 | 411,820（归档 hot-index） |
| 定位 | 逐条修掉 Tarkov 界面/交互的痛点；changelog 长期执行「**BSG 已实现即移除**」的收敛策略 |

源码工程形态（快照 v6.0.1）：`netstandard2.1`（Mono / BepInEx 5 时代）、`AssemblyName=Tyfon.UIFixes`、引用 `spt-common` / `spt-reflection` / `Fika.Core`；服务端子工程为 `net10.0` + NuGet `SPTarkov.* 4.1.0`。

---

## 2. 功能清单（按域归纳，源自 README + 补丁清单）

**新增功能**
- **Multiselect**：框选/Shift 多选；组移动、组投放、组入格；Ctrl/Alt 快速移动/装备；右键批量（保险/装备/卸下/装弹药/装/卸配件/钉选/锁定）；与 Quick Move to Containers 兼容。
- **Swap in place**：拖动一个物品覆盖另一个 → 交换位置。
- **跳蚤历史返回**；**空槽位联动搜索**（给该槽找可用配件）；大部分右键操作可绑键。
- **Toggle/Hold 输入**：点按=“Press”机制、长按=“Continuous”机制（瞄准/冲刺/战术设备/头灯/护目面罩/交互与放置任务物品）。
- 快速移动/装备目标高亮；**窗口管理器**（记忆打开的容器与检视窗的位置/状态）。

**改进（Inventory）**
- 改装备武器；Home/End/PageUp/PageDown 行为重绑；滚轮速度可调；**整栈移动**；被 mod 变为可堆叠的物品遵循正常堆叠行为；仓库滚动位置同步；右键保险/维修；**战局内装弹**；装弹预设从背包取弹；多格马甲/背包网格重排（左→右、上→下）；马甲/背包可打标；**排序会堆叠并合并物品**；Open→All 递归开容器；改造/预设下拉易用性；搜索中可开右键（默认关）；**Search In Stash**；旋转键重绑。
- **检视窗**：含子配件总属性（可开关）；数值增减着色对比；拖拽目标空槽高亮；尺寸记忆/还原；左右吸附按钮+键位；描述自动展开；死亡保留物的 Quickbind 不被移除；兼容槽绿色描边；字体缩放。
- **商人**：买卖自动切换；上缴任务物品自动选择；维修窗记忆 + 实时刷新；头像图标（daily 新任务/可交付）；标签页/商人记忆。
- **跳蚤**：类别自动展开；解锁任务提示；保留 Add Offer 窗；点击 min/avg/max 定价（批量倍数）；Autoselect Similar 记忆；barter 图标实物化 + 持有量着色；无结果自动清筛选。
- **改枪/预设**：武器向左/上生长；滚轮缩放；免误报未保存；一键保存当前预设；隐藏默认预设过滤。
- **藏身处**：窗口状态记忆；**制造工具用后回原容器**（需服务端协作）。
- **战局内**：战术设备独立快捷键；未装备武器可改装（可选需工具）；**换弹原地换弹匣**（不落地）；手雷/消耗品快捷键接续；排队输入。
- **邮件**：窗口保持；附件图标跟踪。**杂项**：Enter/Space 确认；点外关弹窗；焦点行为；窗口化解锁光标；大量细节打磨。

**修复（不完整列举）**：Quest/FoundInRaid 图标旁 tooltip 消失（兼容 MoreCheckmarks）；离屏窗口；红框物品点击导致改造 UI 崩溃；商人“Compatible with Available”；barter 高亮；**拆解武器真正可用**；重复任务通知与音量；**左轮/多管枪的大部分弹药交互**；BTR 付款（含背包/安全箱内钱）；归还罗盘；邮件“稍后可返还”提示与 Receive All。

**Interop**：`src/Multiselect/MultiSelectInterop.cs`（反射接入，不硬依赖）；**GUID 自 4.1 起改为 `com.tyfon.uifixes`**。

---

## 3. 版本线（38 版；与移植相关的关键节点）

| 版本 | 目标 | 关键变化（摘要） |
|---|---|---|
| 1.3.4 → 5.x 前 | SPT 3.x | 多次「BSG 已实现即移除」（Add offer 右键菜单、Wishlist anywhere、可点击市场价等） |
| **5.0.0** | SPT 4.0 | Updated for SPT 4.0；**移除 FiR/非 FiR 弹药堆叠特性——「ammo is now never FiR」（SPT 4.0 起弹药永不 Found-in-Raid）**；任务物品警告移除（BSG 已实现） |
| 5.0.3 | 4.0 | 修复「合并弹药时交互异常」等 |
| 5.0.6 | 4.0 | 霰弹枪/左轮/多管枪整套弹药交互（装/卸/多选） |
| 5.1.x–5.2 | 4.0 | Toggle/Hold、Force Swap、Search in stash 等一批新功能 |
| 5.3.x | 4.0.1–~4.x | 玩家模型/武器平移缩放、PIT 联动、滚轮缩放开关、SAIN+Fika 兼容修复 |
| **6.0.0** | **SPT 4.1** | GUID 变更 `com.tyfon.uifixes`（依赖方需同步） |
| 6.0.1 | 4.1 | queued input reload 修复；双商服务页空白修复 |
| 6.0.2 / 6.0.3 | 4.1 | GitHub releases（2026-08-27 / 2026-09-27；本地归档快照未含其 changelog） |

> 「4.1.6」落在 6.0.x 线：选型应取 **6.0.3（最新）**。

---

## 4. 移植到 SPT 5.0 的功能变化分析

### 4.0 平台跨度（必改，但不改变功能语义）
| 4.1（现状） | 5.0（目标） |
|---|---|
| `BaseUnityPlugin` + `Awake` | `BepInEx.Unity.IL2CPP.BasePlugin` + `Load` |
| netstandard2.1 / Mono | net6.0 / **IL2CPP** |
| `spt-common` / `spt-reflection`（`SPT.Reflection.Patching`） | `SPTushonka.Common` / `SPTushonka.Reflection`（`SPTushonka.Reflection.Patching`） |
| Harmony 2 | HarmonyX（BepInEx 6 自带） |

参考先例（本仓库已跑通）：`mods/SPT5-AccurateCircularRadar`（4.1→5.0 全流程移植）+ `Inescapable-Tarkovs-Softcore` 客户端插件（armband 槽，2026-09-27 实机通过）。

### 4.1 类名/命名空间（已核：**40+ 个关键目标类全部命中**）
核验方式：`classes-1.1.5.txt`（16,435 类型）+ `ilspycmd`。抽样结果（全部 OK）：`EFT.UI.ItemUiContext`、`EFT.UI.DragAndDrop.GridView`、`EFT.InventoryLogic.ItemManipulator`、`EFT.UI.DragAndDrop.ItemView`、`EFT.UI.Ragfair.AddOfferWindow`、`EFT.InventoryLogic.ItemController`、`EFT.UI.ItemSpecificationPanel`、`EFT.UI.DragAndDrop.GridItemView`、`EFT.UI.ItemContextInteractionsSwitcher`、`EFT.UI.GridWindow`、`EFT.UI.DragAndDrop.SlotView`、`EFT.UI.EditBuildScreen`、`EFT.UI.TraderDealScreen`、`EFT.InventoryLogic.InventoryController`、`EFT.InventoryLogic.ItemContext`、`EFT.UI.Chat.DialogueView`、`EFT.UI.Ragfair.*`、`EFT.Hideout.*`、`EFT.Builds.MagBuildsStorage`、`EFT.UI.DragAndDrop.GridSortPanel`、`EFT.UI.InventoryScreen` 等（1.1.5 类名总体保留度约 95%）。
**改名关联（映射报告 §5.2，需改触点）**：`EFT.UI.QuestListItem → EFT.UI.QuestListItemView`——UIFixes 的 `TraderAvatarPatches.QuestListItemPatch` 引用前者（`typeof(QuestListItem)`），移植时须改指后者并重核 `UpdateView` 触点（成员数 33→46）；`WishlistCategoryView` / `LabelContentRow` 经检索**未被 UIFixes 直接引用**。

### 4.2 关键方法级核验（1.1.5 interop，一手实测）
| 符号 | 结果 |
|---|---|
| `ItemManipulator.TryFindMergeableItem(IEnumerable<IContainer>, Item, out StackableItem, int=0)` | ✓ |
| `ItemManipulator.Sort(CompoundItem, InventoryController, bool)` | ✓ |
| `ItemManipulator.TransferOrMerge(Item, Item, ItemController, bool)` | ✓ |
| `GridSortPanel.Sort()` + `SortAsync()`（含 `<SortAsync>d__12`） | ✓ |
| `Item.IsSameItem(Item)` | ✓（非虚） |
| `ItemContext.MergeAvailable` | ✓（**virtual** 属性） |
| `Diz.LanguageExtensions.Error/…` | ✓ |
| **`ChatScreen.method_9`**（MailPatches 目标） | **✗ 不存在**（1.1.5 无 `method_*` 残留；位置性混淆名不可迁移）→ 该补丁需重新定位目标 |

### 4.3 最高风险区：**5 个 transpiler 补丁文件**（IL 改写）
`GridHighlightPatches`（`PixelPerfectSpriteScaler.Awake`）、`InspectWindowStatsPatches`、`MailPatches`、`ScrollPatches`（`SimpleStashPanel.Update` / `TraderDealScreen.Update` / `OfferViewList.Update`）、`VariableScopeFixPatches`（`Firearms.*` / `PlayerCameraController.LateUpdate`）。
本仓库 IL2CPP 实测结论：interop 方法体为**原生调用桩**，transpiler 的可改写面与 Mono 时代不同 → 这些须**重新设计实现**（倾向 prefix/postfix 等价或改挂评价点）。对应功能（像素级网格高亮、检视属性对比条、邮件行为、滚动覆盖、镜变量作用域修复）**存在行为降级或需重写**——这是「功能层面变化」的最大来源。

### 4.4 IL2CPP 批量机械适配
61 个文件含 `System.Linq`（interop 集合无托管 LINQ → 改显式循环）；**0 处协程**（利好）；事件订阅受限（以补丁替代，如容器增删事件）；`TextMeshPro`/`TMP_InputField`/`EventSystem`/`UnityEngine.UI` 等引用面存在性良好；`Newtonsoft` 使用点需重核；`ItemContext.MergeAvailable` 为 virtual（挂钩可行，但注意 virtual 成员调用纪律）。

### 4.5 依赖与服务端
- **ConfigurationManager**：SPT5 目标实例已有（`BepInEx/plugins/sptushonka/ConfigurationManager`）✓。
- **Fika / PIT Fireteam / MergeConsumables**：SPT5 对应版本**未知**（Fika 同步、PIT 邀请面板联动、MC 交互的移植可行性待定）——移植前需先核生态。
- **服务端组件**（`UIFixes.Server`：LinkedSlotSearch 路由、KeepQuickbinds 持久化、PaginateMail、PutToolsBack、AssortUnlocks 等）：工程已是 `net10.0` + `SPTarkov.*` 包引用（4.1.0）→ 移植 = 改引 **SPT 5.0 运行时程序集** 并重核 API（路由注册/DI/存档字段；参考 KB `curated/api-notes-5.0/` 与本仓库 Inescapable mod 的 5.0 服务端实践）。

### 4.6 「功能变化」总表（用户可感知层）
| 类别 | 说明 |
|---|---|
| **A. 直接等价迁移** | 绝大多数前缀/后缀类补丁（右键菜单、窗口行为、商人/跳蚤/邮件 UI 细节）——只换命名空间/入口类 |
| **B. 触点需更新** | 改名类（`QuestListItemView`）、`ChatScreen.method_9` 重定位；个别成员签名需按 1.1.5 重核 |
| **C. 需改实现、行为可能变化** | 5 个 transpiler 文件所辖功能（4.3) |
| **D. 依赖外部 mod** | Fika/PIT/MC 相关功能——视 SPT5 生态 |
| **E. 可能已被原版覆盖、需重评** | 该 mod 历来「BSG 实现即删」（3.x 删 Add-offer/Wishlist；4.0 删弹药 FiR 堆叠）。EFT 1.1.5 / SPT 5.0 的 UI 与 SPT 功能面变化后，需**进游戏逐项重放**，确认哪些修复已无必要、哪些目标已改（不可离线完成） |

### 4.7 与「排序合并」课题的交叉注记
UIFixes 的「排序会堆叠并合并」是**该 mod 自己加装的行为**：`SortPatches.StackFirstPatch` 前缀重实现 `GridSortPanel.Sort`——先 `StackAll`（`Sorter.FindStackForMerge` + `ItemManipulator.TransferOrMerge`）再 `ItemManipulator.Sort`（源码注释自证：原版 `Sort` 只是 `SortAsync`，**不合并**）。「SPT5 原版弹药排序合并」的独立课题见 `docs/research/spt5-sort-ammo-merge-mechanism.md`（原版 Sort 不合并·三版实证；弹药 `SpawnedInSession` 恒 false 的判别式；UIFixes 默认值 `Combine Stacks Before Sorting=true` / `Autostack Items with FiR Items=false` 与该观测完全同构）。

---

## 5. 结论与建议（若后续立项移植）

1. **工程量判定**：这是一次「**机械适配为主 + 少数高风险重写**」的大型客户端移植（87 补丁 + 服务端组件）；类名面喜人（95% 保留），真正的坑集中在 **transpiler（5 文件）**、**外 mod 依赖**、**原版覆盖重评**三处。
2. 建议按域拆批：① Inventory/Multiselect → ② 商人/跳蚤 → ③ 检视/改枪/预设 → ④ 战局内 → ⑤ 邮件/杂项 → ⑥ 服务端组件；每批先清 transpiler 替代与改名触点。
3. 先做「1.1.5 原版行为基线」核对表（逐条对照 §4.6-E），避免移植已被原版修复的功能。
4. 许可：MIT，保留署名即可（源码级参照/重写均合规）。

## 6. 不确定性与边界
- 全部结论为**离线静态分析**；游戏内行为未验证（spec 建议：先小批实机验证再扩大）。
- 快照止于 v6.0.1；v6.0.2 / v6.0.3 变更未细读（GitHub releases 存在）。
- 1.1.5 方法体不可读：除 §4.2 已点验符号外，其余触点的「同名存在」**不保证**签名/行为兼容（需逐条重核）。
- Fika/PIT/MC 的 SPT5 可用性、SPT5 原版 UI 与 4.1 的差异面，均需运行时补证。

---

## 7. 可行性评估（增补，2026-09-28，面向「是否移植」决策）

> 本节用于回答本次评估的真实目标——**「移植 UIFixes 到 SPT5 的可能性」**。依据：本报告 §1–§6 + `docs/research/spt5-sort-ammo-merge-mechanism.md`（排序合并机制）。

### 7.1 分维评级

| 维度 | 评级 | 依据 |
|---|---|---|
| 符号可用性（类/命名空间） | **高** | 1.1.5 类名保留约 95%；40+ 关键类全部命中；核心方法逐一点验 ✓（`ItemManipulator.Sort/TryFindMergeableItem/TransferOrMerge`、`GridSortPanel.Sort/SortAsync`、`Item.IsSameItem`、`ItemContext.MergeAvailable`、`Diz.LanguageExtensions`） |
| 平台适配（BepInEx6 / IL2CPP / net6.0） | **高（有先例）** | 本仓库已完成两个 SPT5 客户端件（Radar 移植、armband 插件均实机通过）；87 补丁以 prefix/postfix 为主，属机械替换 + 逐条核签 |
| transpiler 补丁（5 文件） | **低 → 中（需重写）** | IL2CPP 无方法体 IL 可改（本仓库实测结论）；须改 prefix/postfix 等价或重选评价点；对应功能行为可能变化 |
| 外部依赖 | **中** | ConfigurationManager ✓ 已具备（sptushonka）；Fika / PIT Fireteam / MC 的 SPT5 版**未知**（同步/联动类功能视生态而定） |
| 服务端组件（UIFixes.Server） | **中** | 已是 net10.0 + `SPTarkov.*` 引用；改引 SPT5 运行时 + 核 API（路由/DI/存档字段），有本仓库 Inescapable mod 的 5.0 实践可循 |
| 运行时验证成本 | **中（需逐项重放）** | §4.6-E 类（可能被 1.1.5 原版覆盖）必须进游戏逐项确认；agent 不代跑游戏 |
| 工程规模 | **大（分批可解）** | 87 客户端补丁 + 服务端组件；工作量可观但可自然分域分批 |

### 7.2 与「排序合并」课题的关系（本次评估的动机问题）

原版 Sort 不合并（1.1.5 原生反汇编实证，见报告 A）→ **「排序时合并」必须由移植自行实现**。UIFixes 的实现（前缀 `GridSortPanel.Sort` → 先 `StackAll`（`Sorter.FindStackForMerge` + `ItemManipulator.TransferOrMerge`）→ 再 `ItemManipulator.Sort`）在 1.1.5 所需的**全部符号已逐一验证存在**（报告 A §1 / 本报告 §4.2）。行为预期：弹药因 `SpawnedInSession` 恒 false 总可合并；其他物品受 FiR 门约束（默认 `Autostack Items with FiR Items=false`）——与 mod 默认行为一致。

### 7.3 结论（决定视角）

- **可行**：无制度性/技术性阻断项；主要成本 = **机械适配 + 逐条核签**，主要风险 = **5 个 transpiler 补丁** + **外置依赖** + **原版覆盖重评**。
- 建议路径（若推进）：**M1** = 工程骨架 + Inventory/Multiselect + **排序合并**（动机特性，最先可见收益）→ **M2** = 商人/跳蚤/检视/改枪 → **M3** = 战局内/邮件/杂项 + 服务端组件；全程维护「1.1.5 原版行为基线」，对 E 类逐项重评。
- 量级参照：本仓库 armband 客户端插件（约 300 行 + 构建接线）≈ 一个工作会话；UIFixes 的 M1 ≈ 其 20–40 倍（Inventory 子集 + 基础设施）。

---

## 附录 A：客户端 87 个补丁文件全录（源：`src/Patches/`，v6.0.1）

```
AddOfferClickablePricesPatches / AddOfferRememberAutoselectPatches / AimToggleHoldPatches / AssortUnlocksPatch / AutofillQuestItemsPatch / BarrelOnlyPatches / BarterOfferPatches / BTRPaymentPatches / CompassGogglesPatch / ConfirmationDialogKeysPatches / ContextMenuPatches / ContextMenuShortcutPatches / CursorPatches / DropdownPatches / FilterOutOfStockPatches / FilterStockPresetsPatches / FixFleaPatches / FixGridPrepareItemsPatch / FixPlayerInspectPatch / FixTooltipPatches / FixTraderControllerSimulateFalsePatch / FixTraderFiltersPatch / FleaPrevSearchPatches / FleaSlotSearchPatches / FocusFleaOfferNumberPatches / GPCoinPatches / GridHighlightPatches / GridWindowButtonsPatch / HideInviteUIPatch / HideoutCameraPatches / HideoutLevelPatches / HideoutSearchPatches / InspectWindowResizePatches / InspectWindowStatsPatches / InternalMagPatches / KeepMessagesOpenPatches / KeepOfferWindowOpenPatches / LimitDragPatches / LoadAmmoInRaidPatches / LoadMagPresetsPatch / LoadMultipleMagazinesPatches / MailPatches / ModifyUnsearchedContainerPatch / MoveTaskbarPatch / MultiSelectPatches / OpenSortingTablePatches / OperationQueuePatch / PlayerModelViewPatches / PutToolsBackPatch / QuestKeysPatches / QueueInputPatches / QuickAccessPanelPatches / QuickMovePreviewPatches / RebindConsumablesPatches / ReloadInPlacePatches / RememberRepairerPatches / RemoveAdsPatch / RemoveDoorActionsPatch / ReorderGridsPatch / RevolverPatches / RotateKeybindPatch / ScrollPatches / ServicesPatches / SliderPatch / SortPatches / StackFirItemsPatches / StashSearchPatches / SwapPatches / SyncScrollPositionPatches / TacticalBindsPatches / TagPatches / TextboxPatches / TradeQuantityPatches / TraderAvatarPatches / TradingAutoSwitchPatches / TradingHighlightPatches / TransferConfirmPatch / TransferMergePatch / UnloadAmmoPatches / VariableScopeFixPatches / WeaponModdingPatches / WeaponPanPatches / WeaponPresetConfirmPatches / WeaponPreviewPatches / WeaponZoomPatches / WindowPatches / WishlistPatches
```

## 附录 B：来源与核验命令

- 本地归档：`knowledge/spt-kb/archive/forge/api/hot-mods/1342.json`（描述）、`1342.versions.json`（38 版 changelog）、`hot-index.json`（best_spt `~4.1` / best_version `6.0.1` / fika: true / downloads 411,820）；源码快照 `knowledge/spt-kb/archive/forge/mods/UI-Fixes_1342_source`（v6.0.1 / `dc5af6b`）。
- GitHub：`tyfon7/UIFixes`（README 全功能表；branches 仅 `main`——**无 5.0/IL2CPP 分支**；releases 至 v6.0.3）。
- 1.1.5 核验：`ilspycmd -t "<Type>" "E:\Game\EFT_Offline\SPT_5xx\BepInEx\interop\Assembly-CSharp.dll"`；类型清单 `knowledge/spt-kb/archive/eft-1.1.5/classes-1.1.5.txt`；改名对照 `docs/eft-1.1.5-类名映射重建报告.md` §5.2。
- 交叉证据：`Advanced-Stash-Sorting_2924_source/src/Patches/UIFixesCompatPatch.cs`、`BarterItemsStacks_2480_source/.../UIFixesStackAllPatch.cs`（生态对其 `SortPatches+StackFirstPatch`/`StackAll` 的挂钩方式）。
