# SPT5「自动排序合并子弹」机制报告 —— 为什么子弹会合并、其他物资不会

> 日期：2026-09-28
> 问题：SPT5 里用「自动排序」（Sort）时，**子弹堆叠会自动合并**，而其他物资不会——为什么？
> 方法：后台探针本地源码考古（0.16.x 双版本 C# 反编译 + **1.1.5 原生反汇编** + SPT5 服务端源码 + 目标实例插件清单/二进制扫描）+ 主控台逐条抽验
> 抽验记录（主控台）：`spt5-sort-disasm.txt` 的三条 call 与全部 merge-RVA 缺席一致；`dump.cs` 弹药成员 RVA 坐标一致；UIFixes `Settings.cs` 两项默认值一致；服务端 `SortInventory` 原码一致。

---

## 0. 一句话结论

1. **原版的「排序」从不合并**——客户端（0.16 / 0.16.9.5 / 1.1.5 三版实证）与 SPT5 服务端都不合并。所以「排序合并」在纯原版中**不可能发生**。
2. 「子弹（会）合并」的判别式 = 合并谓词里的 **FiR（`SpawnedInSession`）相等条件** + **弹药把 `SpawnedInSession` 硬编码为恒 false**（天然满足相等）→ 同型号任意两堆未满弹药在任何合并路径上都容易合并；而其他可堆叠物（药、打火机等）若一堆 FiR、一堆非 FiR，条件不成立 → 表现为「他物不合并」。
3. 但该谓词只在**拖拽/QuickMove（移动合并）路径**、以及**客户端插件重实现的排序**中被调用。若你确实在「点排序按钮」时看到合并：**几乎只能来自客户端插件**——最贴合你的描述的是 **UIFixes 的默认配置**（`Combine Stacks Before Sorting = true`、`Autostack Items with FiR Items = false`），Advanced Stash Sorting、旧包的 Seion.Iof 亦同类。
4. 实测核对：目标 SPT5 实例（`E:\Game\EFT_Offline\SPT_5xx` / MO2 源 `Inescapable Tarkov`）**未装任何此类插件**；但 **`Exclusive_Tarkov` 与 `Life_in_Norvinsk_v0.3.2` 两套包都实装了 `Tyfon.UIFixes.dll` 与 `Seion.Iof.dll`**。→ 观测来源需澄清（见 §5），这也正是本项目 KB T5 记录过的「客户端插件行为≠原版」模式。

---

## 1. 证据链一：原版 Sort 不合并（代码级，三版实证）

### 1.1 0.16.9.5（C# 反编译，方法体可读）
`ItemManipulator.Sort`（`external/decompile-cache/eft-0.16.9.5-spt412/EFT/InventoryLogic/ItemManipulator.cs:2882`）结构：
① 摘除所有 `PinLockState==Free` 物品 → ② `ItemSorter.Sort()` 纯重排 → ③ `Grid.AddAnywhere()` 逐个放回。**方法体内无任何 Merge/Transfer 调用。**

```
2900  … sortedItem.Grids.SelectMany(grid => grid.Items) where PinLockState == Free
2914  List<Item> list3 = ItemSorter.Sort(list2);
2925  OperationResult<GridAddResult> operationResult2 = grids[i].AddAnywhere(item, EErrorHandlingType.Ignore);
```

`ItemSorter.Sort`（同缓存 `ItemSorter.cs:183`）= `OrderBy(GetIndexOfItemType).ThenBy(comparer)`，纯排序。

### 1.2 0.16（SPT 3.11，混淆名同构）
`InteractionsHandlerClass.cs:2733`：同样「摘除 → `GClass3177.Sort` → `AddAnywhere`」。

### 1.3 1.1.5（**原生反汇编**，目标实例真实 `GameAssembly.dll`）
`ItemManipulator.Sort` @RVA `0x110A9C0` 的全函数 call 目标中**只有**：
- `0x10E56F0` = `ItemSorter.Sort`（`0x110B2EF` 处调用）
- `0x10543B0` = `Grid.AddAnywhere`（`0x110B3C8`、`0x110B498` 两处）

**不存在**：`Merge@0x1103800`、`TryFindMergeableItem@0x10F7D90`、`TransferMaxStackCount@0x1105A30`、`TransferOrMerge@0x110B9F0`、`Ammo.ApplyToAmmo@0x1073B90`。
`GridSortPanel.<SortAsync>`（ISIL 制品）唯一调用 `ItemManipulator.Sort`；`Grid.AddInternal` / `Stash.StashGrid.AddInternal` 反汇编亦无合并调用；1.1.5 `GridSortPanel` 的 lambda 集与 0.16.9.5 完全同构。
（证据文件：`D:\Temp\opencode\spt5-sort-disasm.txt`、`spt5-addinternal-disasm.txt`、`spt5-stashgrid-addinternal.txt`、`il2cpp-re/cpp2il_isil/IsilDump/.../GridSortPanel_NestedType__SortAsync_d__12.txt`）

### 1.4 SPT5 服务端
`SortInventory`（`SamMeow_SP-Tushonka_5xx_source_code/.../Controllers/InventoryController.cs:504-520`）**只回写 `ParentId/SlotId/Location`**，不合并。

> 小结：排序合并**没有**版本演进来源——0.16 到 1.1.5，原版 Sort 一路都不合并。

---

## 2. 证据链二：合并谓词 + 弹药特例（「为什么是子弹」）

### 2.1 合并谓词（拖拽/移动路径）
`ItemManipulator.TryFindMergeableItem`（0.16.9.5 `ItemManipulator.cs:1206`）四条件：

```
x != itemToMerge
x.TemplateId == itemToMerge.TemplateId      // 同型号
x.SpawnedInSession == itemToMerge.SpawnedInSession   // FiR 状态一致
x.StackObjectsCount < x.StackMaxSize        // 未满
剩余空间 ≥ overrideCount
```

### 2.2 弹药特例：`SpawnedInSession` 恒 false
- 0.16.9.5（`Ammo.cs:20-31`）：`ImportantForCheckSpawnedInSession => false`；`SpawnedInSession { get => false; set { } }`。
- **1.1.5 原生**（`dump.cs:448967-448974`，RVA 经主控台抽验）：`get_SpawnedInSession` @`0x6B6630`（`xor al,al; ret`——恒 false）、`set_SpawnedInSession` @`0x628110`（`ret 0`——空操作）。
- 历史注记：3.11 时代的 `AmmoItemClass` 仅 `ImportantForCheckSpawnedInSession=>false`，**未**硬编码 `SpawnedInSession`——「弹药永不 FiR」是 4.1/5.0 才成形；SPT 4.0 变更亦有记载（UIFixes v5.0.0 changelog：*"ammo is now never FiR"*）。

### 2.3 合成判别式
| 物品 | FiR 相等条件 | 结果 |
|---|---|---|
| **弹药** | 恒成立（双方都 false） | 同型号、未满 → **任意两堆可合并** |
| 其他可堆叠物 | 需相同 FiR 状态 | 一堆 FiR + 一堆非 FiR → **不合并**（这解释「他物不合并」） |

⚠️ 但该判别式只在**会调用合并的路径**产生效果（拖拽合并 / QuickMove / 插件重实现的排序）。**原版排序按钮本身不调用它。**

---

## 3. 「排序合并」的实际来源（客户端插件清单）

| 插件 | 机制 | 备注 |
|---|---|---|
| **UIFixes**（Tyfon.UIFixes） | `SortPatches.StackFirstPatch` 前缀重实现 `GridSortPanel.Sort`：先 `Sorter.FindStackForMerge` + `ItemManipulator.TransferOrMerge`，再 `ItemManipulator.Sort` | **默认 `Combine Stacks Before Sorting=true`、`Autostack Items with FiR Items=false`**——与「子弹合并、他物不合并」**完全同构**（主控台已抽验默认值） |
| Advanced Stash Sorting | 排序前按 `(TemplateId, SpawnedInSession)` 分组合并 | `SortPreparationPatch.cs:230-321` |
| Seion.Iof（旧包 INVENTORY ORGANIZING FEATURES） | 挂 `GridSortPanel` + `TransferOrMerge`（二进制实证） | 旧整合包（Life in Norvinsk / Exclusive_Tarkov）装有 |
| BarterItemsStacks / MergeConsumables | 非排序按钮路径（拖拽/消耗品合并） | 相邻参考 |

**实例核查**：
- `Inescapable Tarkov`（当前 SPT5 主实例）：插件清单（BepInEx 日志 10 plugins）与二进制扫描均**无**排序合并类插件。
- `Exclusive_Tarkov`、`Life_in_Norvinsk_v0.3.2`：**均实装 `Tyfon.UIFixes.dll` + `Seion.Iof.dll`**（含插件目录，主控台已核）。

---

## 4. 复现与归因（可执行步骤）

1. **对照实验**（判定谓词）：两堆**非 FiR** 的同类可堆叠物（如药）→ 点 Sort：
   - 若合并 = 该环境有排序合并插件（或 FiR 条件不阻断）；
   - 若弹药合并、药不合并 = 与 UIFixes 默认行为一致（药的 FiR 不一致）。
2. **归因步骤**：打开该实例 `BepInEx/LogOutput.log` 查 `Loading [UI Fixes …]` / `Seion` / `Advanced Stash Sorting`；在其 MO2 profile 中禁用对应插件后复测 Sort。
3. **警示**（本项目 KB T5 已记录）：把「客户端插件行为」记成「原版行为」是本项目高频误判模式——本次结论同样落在该模式。

---

## 5. 不确定性与边界

- **观测与主实例不符**：`Inescapable Tarkov` 实例复现不了「排序合并」（无此类插件）——需澄清观察来源（`Exclusive_Tarkov` / `Life_in_Norvinsk` / 其它），或其是否包含「拖拽/快速移动合并」的感知混同（那个是原版行为）。
- 1.1.5 证据为**原生反汇编**（RVA/IAT 解析），非可读 C#；Cpp2IL ISIL 制品不完整（缺 `ItemManipulator` 的 ISIL 文件）——故以 RVA + 反汇编取证；逐指令中文可读版需重跑 Cpp2IL/Ghidra。
- 未深挖 preset/UserItems 路径（`SortUserItems`，与 Sort 按钮无关）。
- 服务端 `SortInventory` 结论以 Tushonka 5.0 源码为准（与客户端行为一致）。

---

## 6. 补充（用户归因后核查，2026-09-28）

- 用户答复：观测环境 = **`Inescapable Tarkov` 主实例**。
- 主控台独立复扫（验证责任人复核）：扫描 **12 个客户端 DLL**（游戏 `BepInEx/plugins` + MO2 全部 mods 树 + `overwrite` 层；含 ITBS.Core/Perception、Tyrian-Radar、TrueRealTimeMap、DebugToolkit、RuntimeBridge、ActiveProbe、Inescapable 客户端插件等），关键字 `GridSortPanel` / `TransferOrMerge` / `StackBeforeSort` / `FindStackForMerge` / `Tyfon` / `Seion` —— **0 命中**。
- 由此：**静态证据与该观测相矛盾**（该实例无任何排序合并实现；原版 Sort 三版实证不合并）。在可检代码范围内，该现象**无法**用「Sort 按钮合并」解释。
- 最可能解释（待实验裁定）：观测到的是**拖拽/快速移动（QuickMove）时的合并**——原版传输路径**会**合并（谓词见 §2.1），且因弹药 `SpawnedInSession` 恒 false，「子弹总能合并」；其他可堆叠物一旦 FiR 状态不一致就被挡 → 主观印象恰为「子弹合并、他物不合并」。即：是**移动合并**被记为**排序合并**（与 T5 同类归因误差）。
- 对照实验矩阵（在 Stash 内即可完成；预期见括号）：
  1. 同容器放两堆**同型号、均非 FiR** 的非弹药堆叠物（商人购得）→ 点 **Sort** → 是否合并？（预期：**不合并**）
  2. 同容器放两堆同型号未满**弹药** → 点 **Sort** → 是否合并？（预期：**不合并**）
  3. **不做 Sort**：把一堆弹药拖到/快速移到另一堆上 → 是否合并？（预期：**合并** ← 原版传输合并）
  4. **不做 Sort**：把一堆非 FiR 的非弹药物拖到另一堆同型号非 FiR 堆上 → 是否合并？（预期：**合并**）
  - 判读：若 1/2 不合并而 3/4 合并 → 现象本质 = **移动合并**（原版），非排序合并；若 1/2 出现合并（与预期相反）→ 需现场采集（截图 / `BepInEx\LogOutput.log` / 操作录像）追加核查。
- 注：若需求本质是「**希望排序时合并**」——那是 UIFixes（或同类自研）的能力（本仓库已有 SPT5 客户端插件基建可复用）。
- **后续澄清（2026-09-28）**：用户说明该问题服务于 **UIFixes 移植评估**——本节的归因实验**无需执行**；「排序合并」属 UIFixes 特性，其 1.1.5 实现所需符号（`GridSortPanel.Sort/SortAsync`、`ItemManipulator.Sort/TransferOrMerge/TryFindMergeableItem`、`Item.IsSameItem`）已在本报告 §1 与报告 B（`docs/research/ui-fixes-1342-spt5-port-analysis.md`）§4.2/§7 全部验证。

---

## 附录：关键坐标速查

| 项 | 坐标 |
|---|---|
| 原版 Sort（0.16.9.5） | `eft-0.16.9.5-spt412/EFT/InventoryLogic/ItemManipulator.cs:2882-2971` |
| 原版 Sort（1.1.5 原生） | `ItemManipulator.Sort` RVA `0x110A9C0`（`D:\Temp\opencode\spt5-sort-disasm.txt`） |
| 合并谓词 | `ItemManipulator.TryFindMergeableItem` @0.16.9.5 `:1206`；1.1.5 RVA `0x10F7D90` |
| 弹药 FiR 恒 false | 0.16.9.5 `Ammo.cs:20-31`；1.1.5 `get_SpawnedInSession` RVA `0x6B6630`（`dump.cs:448967-448974`） |
| 服务端 Sort | `SamMeow_SP-Tushonka_5xx_source_code/.../Controllers/InventoryController.cs:504-520` |
| UIFixes 排序补丁 | `UI-Fixes_1342_source/src/Patches/SortPatches.cs:20-99`；默认值 `src/Settings/Settings.cs:943-959` |
