---
version: [4.1]
domain: client
topic: verification
title: "4.1.x 客户端移植 API 触点核验标准流程：反编译 + 反射双通路 / 四态判据 / 版本锚定"
keywords: [触点核验, ilspycmd, 反射, Assembly-CSharp, TarkovVersion, 混淆名映射, 全局命名空间, Mono, IL2CPP, 跨线污染, 四态判据, 二次独立核实, 版本锚定, 字符串堆扫描, FileVersionInfo, Harmony, BepInEx5, 可见性变化, 值类型语义, 降级预案]
summary: "SPT 4.1.x（Mono / BepInEx 5 / netstandard2.1）客户端移植前逐触点核验 API 的标准方法：以已反混淆真名的 Assembly-CSharp.dll 为目标，用 ilspycmd 定向反编译与 PowerShell .NET 反射两条独立元数据通路交叉取证，逐触点给出 [OK]/[CHANGED]/[MISSING]/[UNCERTAIN] 四态及来源行/目标签名/实测/降级预案/一行实现指引，随机抽取若干条做二次独立核实；以 FileVersionInfo.FilePrivatePart 做版本门禁并优先采信上游 TarkovVersion 锚点；坑位覆盖目标程序集误选 .spt-bak、全局命名空间类型被臆造 EFT. 前缀、运行期字符串不可静态核实、值类型/引用类型语义差异、可见性 protected↔public 变化、混淆名须按成员面与方法体语义指纹映射；并标出跨 5.0 线的污染陷阱（QuestListItem 改名仅属 1.1.5、UIContext→UIParent、BackendConfigSettingsClass→GlobalConfiguration、DamageInfo 为 struct、EBodyPart 在 4.1.6 仍留在 Assembly-CSharp）；适用于任何 4.1.x / 0.16.x 客户端移植，不适用于 IL2CPP 5.0 线或 server 端 mod。"
---

# 4.1.x 客户端移植 API 触点核验标准流程：反编译 + 反射双通路 / 四态判据 / 版本锚定

> 来源：2026-10-11 SamMeow.DebugToolkit 4.1.6 移植工程（spec 工单 02「API 触点核验」）；三份核实表：`mods/SPT41-DebugToolkit/docs/api-touchpoints/{tooltip.md, bot-overlay.md, dadgamer.md}`。
> 目标基准：SPT 4.1.6 / EFT `0.16.9.40743`（FilePrivatePart **40743**）/ Mono / BepInEx 5 / netstandard2.1；目标程序集 `EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`（已反混淆真名）。

## 一、流程与判据（四态 / 证据要求 / 二次独立核实 / 版本锚定）

移植动手前，先把上游源码对游戏 API 的每一个调用点（补丁目标 + 反射触点）列成表，逐条核实。

- **先固定核实表头部（provenance）**：上游仓库与本仓归档路径、上游版本、License、目标框架（netstandard2.1）、混淆/SPT 锚点。以 bot-overlay 为例：上游 `DrakiaXYZ/BotDebug 1.8.0`，MIT，锚点 `[assembly: TarkovVersion(40743)]`。
- **核实表骨架固定为 7 列**：`# | 来源文件:行 | 目标（类型.成员）| 0.16.9 实测 | 适配/降级 | 一行实现指引 | 状态`；逐行填满，不留空判。
- **四态判据**：
  - `[OK]` 同名存在（签名一致或兼容）；
  - `[CHANGED]` 改名、签名或可见性变化，必须给出新形态与适配；
  - `[MISSING]` 缺失，必须给降级预案；
  - `[UNCERTAIN]` 无法静态定位，给出最佳候选 + 理由，实现期再确认。
- **每条触点的证据要求**：来源 `file:行`、目标签名、0.16.9 实测、适配/降级预案、一行实现指引、状态。缺任一项视为未核实。
- **二次独立核实**：抽 ≥5 条用与主流程不同的元数据通路复核（本例 tooltip 7 条、bot-overlay 6 条、dadgamer 12 条），两条通路结论一致方可判 `[OK]`。
- **版本锚定**：以 `FileVersionInfo.FilePrivatePart` 门禁（== 40743）；上游若自带 `[assembly: TarkovVersion(40743)]` 且与本构建精确一致，则该子模块预期零 `[CHANGED]`——bot-overlay 50 触点全 `[OK]` 即由此而来。
- **规模记录**：tooltip 62 触点（OK 56 / CHANGED 5 / UNCERTAIN 1 / MISSING 0）；bot-overlay 50 全 OK；dadgamer 37（OK 17 / CHANGED 20 / MISSING 0，上游为 3.8 世代混淆名，须语义映射）。

## 二、工具链（ilspycmd / .NET 反射 / 字符串堆扫描 / FileVersionInfo）

- **ilspycmd 11.0.0.9375**：
  - `-t "<Full.Type.Name>"` 定向反编译单类型（取真实签名/字段/可见性）；
  - `-m "M:<Type>.<Method>(<ParamTypes>)"` 反编译单成员（doc-id，用于二次核实）；
  - `-l c/i/e` 列类/接口/枚举清单（独立于 `-t` 的存在性交叉验证）；
  - `-o <dir> --nested-directories` 全量反编译后做文本检索（定位旧混淆名的语义继任者）。
- **PowerShell .NET 反射**（独立于 ILSpy 的第二条元数据通路）：`[Reflection.Assembly]::LoadFrom($dll)` + `GetMethod/GetField/GetProperty/GetEvent`。dadgamer 12 条双方法一致。
- **DLL 字节级字符串堆扫描**：`[regex]::Matches([Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($dll)), <模式>)`，用于类型归属判断与「旧名消失 / 新名存在」二分（如 `BackendConfigSettingsClass` 字符串不存在，确证旧类名已移除）。
- **版本门禁**：`FileVersionInfo.FilePrivatePart`（本例 40743）。

代表性命令：

```powershell
# 单类型反编译（真实签名/字段/可见性）
ilspycmd -t "<Full.Type.Name>" "<Assembly-CSharp.dll>"
# 单成员反编译（二次独立核实骨架，doc-id）
ilspycmd -m "M:<Type>.<Method>(<ParamTypes>)" "<Assembly-CSharp.dll>"
# 类型清单（独立存在性交叉验证）
ilspycmd -l c <dll>        # 类
ilspycmd -l i <dll>        # 接口
ilspycmd -l e <dll>        # 枚举
# 全量反编译后文本检索（定位旧混淆名的语义继任者）
ilspycmd -o <dir> --nested-directories <dll>
# .NET 反射（第二条元数据通路）
$asm = [Reflection.Assembly]::LoadFrom("<dll>")
$asm.GetType("<Full.Type.Name>").GetMethod("<Method>")
```

- **依赖程序集同样入口**：若上游引用第三方前置（如 BigBrain），把该 DLL 也纳入同一套 `-t` / `-l` / 字符串扫描流程，避免把前置类型误记到游戏程序集。

## 三、常见坑（目标程序集选择 / 全局命名空间 / 运行期字符串 / 值类型语义 / 可见性变化 / 混淆名映射）

1. **目标程序集必须是已反混淆真名的 `Assembly-CSharp.dll`**；同目录 `.spt-bak` 是混淆备份，勿用。
2. **全局命名空间类型（无 namespace）容易被臆造 `EFT.` 前缀**：`DebugBotStruct` / `IAIData` / `EPlayerState` / `IBotGame` / `SSAA` / `BaseLogicLayerSimple` 等均在全局命名空间；`Player` / `BotOwner` / `GameWorld` 在 `EFT`。核实后按实际命名空间书写。
3. **运行期字符串不可静态核实**：`transform.Find("TaskShortInfo")` 之类只在运行时解析，静态只能判 `[UNCERTAIN]`，实现须空判（`.Find` 结果可能为 null）。
4. **值类型 vs 引用类型语义**：`DebugBotStruct` / `DamageInfo` 是 struct；`GetBodyPartHealth` 返回 `ValueStruct` **副本**，写其字段不生效；而 `BodyState[limb].Health`（`HealthValue`）为引用类型，`Current` setter 写回生效。误判会导致「赋值无效果」类隐性缺陷。
5. **可见性 `[CHANGED]`（protected↔public）不影响 Harmony 按名绑定**，但须记录：`GridItemView.ShowTooltip`、`ModSlotView.TryGetArmorSlot` 均为 4.1.6 由 protected/private 变 public。
6. **混淆名须按成员面 / 方法体语义指纹映射，不能猜名**：`GClass2429`→`ActiveHealthController.Effect`、`GInterface252`→`IDesirable`、`GClass1931.Update`→`ItemsProducerBase.Update`、`method_0(int)`→`AreaData.InitConstructing(int,bool)`（多一 `bool` 参）、`EquipmentClass.method_10`→`InventoryEquipment.GetTotalWeight`（**static**，故 prefix 无 `__instance`）、`method_11(Item)`→`Item.TotalWeight`、`method_1(Slot)`→`Slot.ContainedItem`；`method_9` / `GClass1583` 一类位置性混淆名**不可迁移**，须重定位。
7. **类型归属易错判**：`BaseLogicLayer` / `BaseLogicLayerSimple` 是 EFT 游戏端全局命名空间类型，**不是** BigBrain 类型（BigBrain 仅引用它们）；`DebugBotStruct.PlayerOwner` 的类型为 `IObserverToPlayerBridge` 而非 `Player`。核实归属须看类型声明在哪个程序集，不能按引用链推断。
8. **可空性属运行期问题，与类型存在性分开判**：`CameraManager.Instance.SSAA`（实例属性 `=> _sSAA`）在相机装配前可能为 null → 类型存在性判 `[OK]`，实现仍须空判。

## 四、Mono（4.1.6）与 IL2CPP（5.0）的差异红线

4.1.6 是 Mono，**无 IL2CPP 代理层**，下列 IL2CPP 适配在本线**不需要**，照抄 5.0 线代码反而引入多余依赖：

- `as` / `is` / LINQ / `foreach` 按托管原语义，无需 `TryCast`；对具体类型 `is/as` 直接可用。
- 原生事件 `+=` 无需 `DelegateSupport.ConvertDelegate`。
- virtual 属性直取，无 AccessViolation 风险。
- 自定义 `MonoBehaviour` 直接 `AddComponent`，无需 `ClassInjector` / `IntPtr` 构造。
- 上游 4.x 代码在 4.1.6 上几乎可逐行移植。

**BepInEx 5 配置约束**：用 `BaseUnityPlugin`、内置 `KeyboardShortcut`、无需 ConfigurationManager shim；`AcceptableValueList<T>` 有 `where T : IEquatable<T>` 约束，**裸枚举不满足** → 列表型配置改用 `ConfigEntry<string>` + `Enum.TryParse`（无效值回退）。

## 五、跨线污染陷阱实例（4.1.6 不得照抄 5.0 线改动）

| 触点 | 4.1.6（本线正确形态） | 5.0 线（不得照抄） |
|---|---|---|
| `QuestListItem` | 仍为 `EFT.UI.QuestListItem`，`Init(Quest, Action<QuestListItem>)` **2 参**、未改名 | 已改名 `QuestListItemView`（仅 1.1.5） |
| `UIContext` | 类型**不存在**；UI 字段真类型 `EFT.UI.UIParent : CompositeDisposable` | 正解 `UIContext`→`UIParent` |
| `BackendConfigSettingsClass` | 已移除 → `EFT.GlobalConfiguration`（`BaseLoadTime` / `BaseUnloadTime`） | 同 |
| `DamageInfo` | `EFT.Ballistics.DamageInfo` 为 **struct** | 代理类值语义不同 |
| `EBodyPart` / `EDamageType` / `Stamina` / `PhysicalBase` | 均在 **Assembly-CSharp**（4.1.6 未拆 `PlayerEnums.dll`） | 已拆至独立程序集 |
| `QuestObjectiveView.Show` | **6 参** | 1.1.5 为 7 参（多 `string prefix`） |

规则：涉及上述符号时，先确认「该改动属于哪条线」再照抄；跨线照搬是最常见的移植污染。

## 六、复用性与边界

- **方法锚定「反编译 + 反射双通路 + 四态判据」，与目标 DLL 无关**，适用于任何 4.1.x / 0.16.x 客户端移植，建议固化为标准核验流程；工单化时以本条目为方法模板。
- **适用**：Mono / BepInEx 5 / netstandard2.1 的 client 端移植（含混淆名上游、跨版本上游）。
- **不适用**：IL2CPP 5.0 线（须走 interop 桩与 `TryCast`，见 `curated/migration/pilot-experience-qol-consolidation.md`）；server 端 mod（无 Assembly-CSharp 触点）。
- **产物形态**：每模块一份核实表，含 provenance、命令清单、逐触点主表、混淆名映射依据、二次独立核实表、降级预案与偏差表。表在工单 02 交付，后续实现工单直接引用其「一行实现指引」。
- **核实节奏**：先做 `[MISSING]` / `[UNCERTAIN]` 与混淆名映射（决定可行性），再做 `[CHANGED]` 的签名/可见性适配；`[OK]` 批量确认即可，不必逐条重复举证。

## 相关

- `curated/migration/pilot-experience-spt41-debugtoolkit.md`（同批：本方法的实战项目复盘）
- `curated/operations/client-mod-compat-audit-playbook.md`（相邻：有源码时的 Harmony 冲突审计流程）
- `wiki/SPT_41/Client_40_to_41.md`（4.0→4.1 客户端变更参考）
