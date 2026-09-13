# EFT 1.1.5 Assembly-CSharp 类名映射重建报告（尝试）

> 日期：2026-09-13 | 目标：为 SPT 5.0 客户端 mod 迁移重建 EFT 1.1.5 的类名映射
> 方法：对 SPT 5.0 实际安装中的 BepInEx interop 程序集做类型清单提取，与 4.1 客户端程序集对比
> 结论用途：评估「移植/编写 SPT5 客户端 mod」的可行性边界

---

## 0. 一句话结论

**名称不是障碍，逻辑才是。** EFT 1.1.5 虽是 IL2CPP，但其 BepInEx interop 程序集完整保留了**真实、未混淆的类名与命名空间**（16,435 个类型），可低成本提取并与 4.1 对比。真正的「难」在于**方法体**——interop 程序集只是调用原生 IL2CPP 的代理桩，不含游戏逻辑。因此：**类名映射可重建（本报告已产出制品），方法级逻辑逆向仍需原生逆向工程。**

---

## 1. 关键突破：名称未混淆

| 预期困难 | 实测结果 |
|----------|----------|
| EFT 1.1.5 反编译非常难 | 对**方法体**成立（IL2CPP 原生代码），对**类名**不成立 |
| 名称可能被混淆（`GClass123`） | **`GClass*`/`GStruct*`/`GInterface*` 计数为 0**——名称干净 |
| 命名空间可能丢失 | 完整保留（`EFT.BotOwner`、`EFT.ClientPlayer`、`EFT.InventoryLogic.*` 等） |

**证据**：
- EFT 1.1.5 确为 IL2CPP：`EscapeFromTarkov_Data\` 下**无 `Managed\`**，存在 `il2cpp_data\` 与根目录 `GameAssembly.dll`。
- `BepInEx\interop\Assembly-CSharp.dll`（54.1 MB）是 Il2CppInterop 生成的代理程序集，**保留了 IL2CPP 元数据中的真实类型名**。
- 抽查 `EFT.BotOwner` 反编译：字段为 `NativeFieldInfoPtr_*`，方法体为调用原生的桩——**证明是代理程序集，逻辑不在其中**。

---

## 2. 方法与工具链

| 项 | 值 |
|----|----|
| 工具 | `ilspycmd` 11.0.0.9375（ICSharpCode.Decompiler 11.0.0） |
| 1.1.5 源 | `E:\Game\EFT_Offline\SPT_5xx\BepInEx\interop\Assembly-CSharp.dll`（54.1 MB，`assembly-hash.txt` = `1821be36e697ca2a036990874dfaaee1`） |
| 4.1 源 | `E:\Game\EFT_Offline\SPT_41x\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`（15.5 MB，Mono，已去混淆） |
| 5.0 安装包 | `SPT-BLEEDINGEDGEMODS-5.0.0-47242-49aff99-20260910.7z`（同目录） |

```powershell
# 列出类型（c=class）
ilspycmd -l c "<Assembly-CSharp.dll>" > classes.txt
# 反编译单个类型
ilspycmd -t "EFT.BotOwner" "<Assembly-CSharp.dll>"
```

---

## 3. 结果统计

### 3.1 类型清单规模

| 指标 | 4.1（Mono） | 1.1.5（IL2CPP interop） |
|------|-------------|--------------------------|
| 类型总数 | 11,436 | 16,435 |
| 嵌套类型（含 `+`） | 4,419 | 7,420 |
| 顶层类型 | 7,016 | 8,889 |
| `MethodInfoStoreGeneric_*`（interop 伪影） | **0** | **664** |

> 注：4.1 是干净 Mono 程序集；1.1.5 是 interop 代理，含 Il2CppInterop 生成的泛型方法存根类型——这是两者**不可完全对齐**的根因。

### 3.2 顶层类型差异（清洗后：去嵌套/编译器生成/interop 伪影）

| 类别 | 数量 | 说明 |
|------|------|------|
| **两版相同** | **6,662** | 占 4.1 顶层类型的 95%——**绝大多数类名未变** |
| 仅 4.1 有 | 354 | 候选：改名 / 换命名空间 / 删除 |
| 仅 1.1.5 有 | 2,227 | 新增（EFT 1.0 内容 + interop 差异） |

### 3.3 「仅 4.1 有」的 354 个细分

| 细分 | 数量 | 说明 |
|------|------|------|
| 简单名匹配成功（换命名空间/改名） | **19** | 如 `EFT.InventoryLogic.SortingTableTemplate` → `SortingTableTemplate`；`LabelContentRow` → `EFT.UI.LabelContentRow`；`WaterRenderer` → `EFT.Water.WaterRenderer` |
| 简单名未匹配 | 335 | 以 `*RequestParams`N` / `*OperationParams`N` 为主 |

**335 个的解读**：4.1 有一批 `UpdateStatusRequestParams`7`、`ConfirmPurchaseOperationParams`7` 形式的后端请求参数类型，1.1.5 中**同名类型完全不存在**，而出现了泛型化的 `BackendRequestParams`——高度疑似 **EFT 1.0 对后端请求层做了泛型重构**（`OperationParams` 在 1.1.5 计数为 0）。这类「整层重构」用名字匹配无法还原，需签名级匹配。

### 3.4 1.1.5 新增类型示例（反映 1.0 新内容）

`ITODSky`、`ProfilingRecorderConfig`、`AICoreActionResult`2`、`AILayerDebugStruct`、`CoverpointIconController`、`ICoverSearchBot`、`PatrollingBlackDivisionSeason`（Black Division 赛季 AI）、`IBotDevelopService`、`AIVision`、`Debug*Struct` 系列、`CullingNode` 等。

---

## 4. 制品位置

已落盘到 `knowledge/spt-kb/archive/eft-1.1.5/`：

| 文件 | 内容 | 大小 |
|------|------|------|
| `classes-1.1.5.txt` | 1.1.5 全类型清单（16,435 行） | 1.6 MB |
| `classes-4.1.txt` | 4.1 全类型清单（11,436 行） | 993 KB |
| `common.txt` | 两版共有的顶层类型（6,662） | 208 KB |
| `only-in-4.1.txt` | 仅 4.1 有的顶层类型（354） | 12 KB |
| `only-in-1.1.5.txt` | 仅 1.1.5 有的顶层类型（2,227） | 79 KB |
| `rename-candidates/` | 成员指纹匹配产物（见第 5 节） | — |

---

## 5. 成员指纹匹配工具（eft-classmap）与结果

### 5.1 工具

位置：`tools/eft-classmap/`（.NET 10 控制台，`System.Reflection.Metadata` 读程序集，无第三方依赖）。

```powershell
# 构建
dotnet build tools/eft-classmap/eft-classmap.csproj -c Release
# 运行（A=旧版 4.1，B=新版 1.1.5）
dotnet tools/eft-classmap/bin/Release/net10.0/eft-classmap.dll <A.dll> <B.dll> <outDir>
# 诊断单类型
dotnet tools/eft-classmap/bin/Release/net10.0/eft-classmap.dll --dump <dll> EFT.BotOwner
```

**原理**：对每个类型提取**成员指纹**（方法/字段/属性/事件名集合），对「仅 A 有」× 「仅 B 有」做相似度匹配。关键是跨引擎归一化：

| 问题 | 处理 |
|------|------|
| Mono 自动属性后备字段 `<X>k__BackingField` | 归一为 `X` |
| Il2CppInterop 后备字段 `_X_k__BackingField` | 归一为 `X` |
| 访问器 `get_X`/`set_X`/`add_X`/`remove_X` | 归一为 `X` |
| Il2CppInterop 注入成员 `NativeFieldInfoPtr_*` 等 | 过滤 |
| 编译器生成类型（4.1 的 `CG_*`、1.1.5 的 `__c__DisplayClass*`） | 过滤 |
| 合成成员名 `method_N` | 过滤 |
| **相似度度量** | **F1**（非 Jaccard）——新版常新增成员，Jaccard 会被稀释；实测 `EFT.BotOwner` A=229/B=361/交集=227，F1 才能正确反映 |

### 5.2 结果（A=4.1，B=1.1.5）

| 指标 | 值 |
|------|-----|
| 清洗后类型数 | A=10,946 / B=14,293 |
| 同名 | 10,207 |
| 仅 A 有 / 仅 B 有 | 739 / 4,086 |
| **exact（成员集完全相同）** | **10** |
| **similar（F1 ≥ 0.70）** | **11** |
| possible（0.60 ≤ F1 < 0.70） | 21 |
| 未解析 | 697 |

**exact + similar 共 21 条，经抽样核对全部为真改名/换命名空间**，例：

| 4.1 | 1.1.5 | 性质 |
|-----|-------|------|
| `LabelContentRow` | `EFT.UI.LabelContentRow` | 补命名空间 |
| `UI.Trading_UI.Ragfair.NodeView.WishlistCategoryView` | `EFT.UI.WishlistCategoryView` | 迁移+改名 |
| `EFT.GlobalConfiguration+PveGlobalSettings` 等 6 个 | `EFT.PveGlobalSettings` 等 | 嵌套设置类提升为顶层 |
| `EFT.Quests.QuestDataClass` | `EFT.Quests.QuestStatusData` | 改名 |
| `EFT.UI.QuestListItem` | `EFT.UI.QuestListItemView` | 改名（33→46 成员） |
| `WaterSSR.WaterForSSRv3` | `EFT.Water.WaterContainer` | 迁移+改名 |
| `Audio.AudioCulling.SyncLoopSoundPlayer` | `CommonAssets.Scripts.Audio.SyncLoopSoundPlayer` | 迁移命名空间 |
| `PushAndSuppressLayer` | `PushAndSuppressAssaultLayer` | 改名 |

`possible` 桶（21 条）好坏混杂（如 `QuestLogger` → `EFT.Quests.QuestsLogger` 为真，`BitPacking.BitsHelper+EnumToInt`1` → `MinMaxVec2IntAttribute` 为假），**须人工核对**。

### 5.3 局限

- 697 个「仅 A 有」未解析：多为真删除、或成员改动过大（如 4.1 后端 `*RequestParams`N` 整层泛型重构）。
- 阈值（MinMatchMembers=4、similar≥0.70）是经验值；提高精度会降召回。
- 4.1 是 Mono 程序集、1.1.5 是 Il2CppInterop 代理，成员表示本质不同，只能逼近、不能等同。

---

## 6. 可行性评估（诚实分级）

| 目标 | 可行性 | 说明 |
|------|--------|------|
| **提取 1.1.5 全部类名/命名空间** | ✅ **已完成** | 本报告制品即成果 |
| **构建 4.1→1.1.5 名称对照** | ✅ **已完成** | 10,207 同名 + 21 条高置信改名/迁移 + 21 条待确认 |
| **自动识别改名** | ✅ **已实现** | 成员指纹 F1 匹配工具 `tools/eft-classmap/`（见第 5 节） |
| **还原方法体逻辑** | ❌ **需另建工具链** | interop 程序集是原生桩；需对 `GameAssembly.dll` + `global-metadata.dat` 做 IL2CPP 原生逆向（Il2CppDumper/IDA/Ghidra 路线）。**注意：这不等于不能改逻辑——见第 7 节** |

---

## 7. 后续建议

1. **立即可用**：类名清单（第 4 节）+ 改名对照（第 5 节）作为 1.1.5 客户端 mod 开发的符号参考。
2. **增量**：人工核对 `possible-match.csv` 的 21 条；如需更高召回，可放宽阈值并接受更多误报。
3. **改逻辑不必读逻辑**：见下节——Harmony 按签名挂钩即可，无需方法体。
4. **高成本**：仅在需要精确复刻/绕过某段算法时，才投入 IL2CPP 原生逆向。
5. **不建议**：现在就为 SPT 5.0 建客户端 mod 模板——5.0 仍在预发布，客户端 API 未冻结。

### 7.1 「看不到逻辑」≠「不能改逻辑」

方法体不可直接读，但改游戏逻辑**不需要**先读方法体：

| 手段 | 是否需要方法体 | 说明 |
|------|----------------|------|
| Harmony `Prefix`/`Postfix` | 否 | 按「类名+方法名+参数」挂钩，在方法前后插入/短路/改返回值 |
| Harmony `Transpiler` | 部分 | 改写 IL 需理解指令流，但**无需 C# 源码** |
| IL2CPP 原生逆向 | 是 | 精确复刻算法时才需要（`GameAssembly.dll` + IDA/Ghidra） |
| 运行时反射/日志/内存扫描 | 否 | 行为观测即可 |

多数 EFT 客户端 mod（SAIN、LootingBots、Waypoints 等）都靠签名挂钩实现。SPT 官方 `modules` 仓库（已有 `5.0x-dev` 分支）本身就是「如何 patch EFT」的活教材。

---

## 8. 与既有资料的关系

- 4.1 的官方映射（4.0 混淆名 → 4.1 真名）见 `wiki/SPT_41/modding/client/Class_Name_Mappings.md`；**本报告是其 4.1 → 1.1.5 的续篇**。
- 3.11→4.1 客户端迁移方法论见 `curated/migration/client-mod-311-to-41.md`，本报告复用其「先取清单、再建映射」思路。
- SPT 5.0 服务端 mod API 见 `docs/spt-5.0-mod-api-能力评估报告.md` 与 `curated/api-notes-5.0/`。
- 5.0 源码克隆记录见 `curated/operations/5xx-source-verification.md`。

---

## 附：复现命令

```powershell
# 1.1.5（interop）
ilspycmd -l c "E:\Game\EFT_Offline\SPT_5xx\BepInEx\interop\Assembly-CSharp.dll" > classes-1.1.5.txt
# 4.1（Mono）
ilspycmd -l c "E:\Game\EFT_Offline\SPT_41x\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll" > classes-4.1.txt
```
