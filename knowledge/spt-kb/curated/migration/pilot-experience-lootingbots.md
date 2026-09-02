---
version: [4.1]
domain: client
topic: migration
source: curated
---

# 客户端混淆名映射（LootingBots 实战解析）

> 状态：已验证（2026-08-07，Mono.Cecil 从 4.1.1 Assembly-CSharp.dll 实读解析 + raid 冒烟验证通过）
> 用途：客户端 mod 迁移（3.11 -> 4.1）的类型映射实战，覆盖 LootingBots 全部 20+ 混淆名
> 方法：Mono.Cecil 反射脚本 + 成员签名匹配（与 SkillsExtended 案例互补）

---

## 1. 核心映射表（LootingBots 已验证）

### 1.1 交易/背包核心

| 3.11 混淆名 | 4.1 真名 | 判定依据 |
|------------|---------|---------|
| `TraderControllerClass` | `EFT.InventoryLogic.InventoryController` | 基类链 InventoryController→PersonItemController→ItemController；不是 ItemController |
| `List_0` | `ItemController.ActiveEvents` | 类型 `List<ItemEventArgs>`（List_0 是字段名不是类型） |
| `GStruct153` | `Diz.LanguageExtensions.OperationResult` | `TryRunNetworkTransaction(OperationResult, Callback)` 返回 `Task<IResult>`；OperationResult<T> 有到非泛型隐式转换 |
| `GEventArgs9` | `SetInHandsEventArgs` | 实现 `IItemInHandsEventArgs` |
| `GEventArgs10` | `RemoveFromHandsEventArgs` | 同上 |
| `GInterface418` | `IItemInHandsEventArgs` | 接口名 |
| `InteractionsHandlerClass` | `EFT.InventoryLogic.ItemManipulator` | 静态 Move/Merge/Swap，返回 `OperationResult<MoveResult>` 等 |
| `ItemFactoryClass` | `EFT.ItemFactory` | CreateItem(String,String,UnparsedData)→Item |

### 1.2 物品类型（全部在 EFT.InventoryLogic）

| 3.11 | 4.1 | | 3.11 | 4.1 |
|------|-----|-|------|-----|
| `KnifeItemClass` | `Knife` | | `ArmorPlateItemClass` | `ArmorPlate` |
| `ThrowWeapItemClass` | `ThrowWeap` | | `OtherItemClass` | `SpecItem` |
| `VestItemClass` | `Vest` | | `HeadphonesItemClass` | `Headphones` |
| `BackpackItemClass` | `Backpack` | | `ArmBandItemClass` | `ArmBand` |
| `HeadwearItemClass` | `Headwear` | | `MedsItemClass` | `Meds` |
| `ArmorItemClass` | `Armor` | | `MoneyTemplateClass` | `MoneyTemplate` |
| `FaceCoverItemClass` | `FaceCover` | | `PocketsItemClass` | `Pockets` |
| `VisorsItemClass` | `Visors` | | `GClass3125` | `Slot` |

### 1.3 AI/机器人

| 3.11 混淆名 | 4.1 真名 | 判定依据 |
|------------|---------|---------|
| `BotDifficultySettingsClass` | `BotSettings` | `ApplyPresetLocation(BotLocationModifier)` 确认 |
| `GClass266` | `PeacefulNode` | ctor(BotOwner) + `UpdateNodeByBrain(CoreActionResultParams)` |
| `AirdropLogicClass` | `EFT.Airdrop.AirdropManager` | 抽象基类；`InitContainerLoot(AirdropSynchronizableObject, String)` 是落地后初始化容器钩子 |
| `AirdropSynchronizableObject_0` | `InitContainerLoot` 的 `container` 参数 | 继承 SynchronizableObject→SerializedMonoBehaviour，GetComponentInChildren 可行 |

### 1.4 会话/价格（结构性变化，见 §3）

| 3.11 混淆名 | 4.1 真名 | 说明 |
|------------|---------|------|
| `ISession` | `EFT.IEftSession` | RagfairGetPrices(Callback<Dictionary<string,float>>) 签名匹配 |
| `HandbookClass` | `EFT.HandBook.Handbook` | **无 Items 属性**，价格结构变了（见 §3.2） |
| `GClass2340.InRaid`（静态） | `Singleton<GameWorld>.Instance != null` | 4.1 无静态 InRaid；GameWorld 存在性判断是标准做法 |

### 1.5 杂项

| 3.11 混淆名 | 4.1 真名 | 判定依据 |
|------------|---------|---------|
| `LayerMaskClass.HighPolyWithTerrainMask` | `LayersMaskController.HighPolyWithTerrainMask` | 静态字段，类型 LayerMask |
| `Player.vmethod_0(wio, ir, null)` | `Player.StartInteraction(wio, ir, Action)` | 参数匹配 |
| `Player.vmethod_1(wio, ir)` | `Player.ExecuteInteraction(wio, ir)` | 参数匹配 |
| `InventoryEquipment.CachedSlots` | `CompoundItem.AllSlots` | IEnumerable<Slot>，装备槽全量 |
| `BotWeaponSelector.ErrorCounter`（公共属性） | `_errorCounter`（私有字段） | 4.1 移除公共属性，改本地计数器 |
| `BotSettings.WildSpawnType_0`（Harmony 注入字段） | `BotSettings._role`（public） | 见 §4.1 |

## 2. 关键陷阱

### 2.1 全局命名空间 ObjectPool 遮蔽 UnityEngine.Pool

**症状**：编译报 ObjectPool 构造/方法不匹配（7 参 vs 5 参）。
**根因**：Assembly-CSharp.dll 有**全局命名空间**的 `ObjectPool<T>`/`ListPool<T>`（ctor(Int32,Func,Action,Action)，方法 Withdraw/Return），遮蔽 `UnityEngine.Pool.ObjectPool<T>`（7 参 ctor，Get/Release）。
**修复**：全限定 `UnityEngine.Pool.ObjectPool<`。
**注意**：批量替换 `ObjectPool<` → `UnityEngine.Pool.ObjectPool<` 会误伤 `IObjectPool<`（变成 `IUnityEngine.Pool.ObjectPool<`），替换后需检查 IObjectPool 用法。

### 2.2 C# using 别名不递归

`using ItemControllerResultStruct = Diz.LanguageExtensions.OperationResult;` 别名只对当前文件生效，且不参与其他 using 的解析。跨文件使用时必须全限定。

### 2.3 PowerShell 5.1 编码

UTF-8 无 BOM 脚本含中文路径会被 ANSI 解析破坏。脚本必须全 ASCII + 运行时 `(Get-Location).Path` 解析仓库根。

## 3. 结构性 API 变化

### 3.1 价格获取（Handbook 重构）

3.11：`Singleton<HandbookClass>.Instance.Items` → `IEnumerable<HandbookData>`，`HandbookData.Price`（float）。

4.1：
- `Singleton<EFT.HandBook.Handbook>.Instance` 仍注册（游戏代码 AILootPointsCluster.EvaluatePrice 等大量使用）
- 但 **无 Items 属性**；有 `AllNodes : Dictionary<string, HandbookNode>`
- `HandbookNode.Data : HandbookData`，`HandbookData.Item : Item`（**无 Price 字段**）
- 价格来源 = `Item.Template.CreditsPrice`（ItemTemplate 公共 int 字段）

构建价格字典：
```csharp
HandbookData = Singleton<Handbook>.Instance.AllNodes.Values
    .Where(node => !node.IsDummy && node.Data?.Item != null)
    .GroupBy(node => node.Data.Item.TemplateId)
    .ToDictionary(group => group.Key, group => group.First().Data);
```
注意 ToDictionary 对重复 key 抛异常 → 必须 GroupBy/First 去重。

### 3.2 会话接口

3.11 `ISession` → 4.1 `EFT.IEftSession`（接口内 RagfairGetPrices 签名不变：`Callback<Dictionary<string,float>>` → void）。`ClientApplication<IEftSession>.Instance.GetClientBackEndSession()` 返回 IEftSession。

### 3.3 InRaid 判断

3.11 `GClass2340.InRaid` 静态属性。4.1 `AbstractGame.InRaid` 是**实例**属性且 AbstractGame 无静态单例；GameWorld 也不是 MonoBehaviourSingleton。标准做法：`Singleton<GameWorld>.Instance != null`（GameWorld 在 raid 中存在；菜单中为 null）。

## 4. Harmony 补丁适配

### 4.1 字段注入改名

3.11 补丁用 `ref WildSpawnType ___WildSpawnType_0`（Harmony 注入 BotSettings 私有字段）。4.1 字段改名为 `_role`（且是 public）。**修复**：直接 `__instance._role` 访问，删掉注入参数。Harmony 对不存在的 `___Field` 注入会在 patch 时报 "No such field defined in class"（运行时失败，编译不报）。

### 4.2 验证方式

启动后查 `BepInEx/LogOutput.log`：
- `Loading [LootingBots 1.7.0]` — DLL 加载
- `Enabled N patches` — 期望 N=全部 patch 数（7/7）
- `Failed to init [X]` — 某 patch Harmony 失败（编译期查不到，必须启动验证）
- 菜单中应有 `Updating item appraiser` — Update() 正常跑

## 5. Mono.Cecil 反射脚本模式

工具：`E:\Game\EFT_Offline\SPT_410\SPT_Runtime\Mono.Cecil.dll`
目标：`E:\Game\EFT_Offline\SPT_410\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`

```powershell
Add-Type -Path $cecil
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($asmPath)
$all = $asm.MainModule.Types
# 按字段名找类型
$all | Where-Object { $_.Fields -and ($_.Fields | Where-Object { $_.Name -eq 'HighPolyWithTerrainMask' }) }
# 按方法签名找类型
# 嵌套类型匹配：$all 包含嵌套类型吗？不全——Player/PlayerInventoryController 需用 .NestedTypes 递归
# 接口检查：$t.Interfaces
# 静态判断：$_.GetMethod.IsStatic / $_.IsStatic
# 继承链：$_.BaseType（Resolve() 可能因 UnityEngine 引用失败，用 try/catch）
```

要点：
- 类型全名精确匹配用 `$_.FullName -eq 'EFT.X.Y'`
- 找混淆类映射用**唯一成员名**（方法/属性/字段名）跨全程序集搜索
- 属性静态性看 `GetMethod.IsStatic`；字段看 `IsStatic`/`IsPublic`

## 6. 移植验证流水线（LootingBots 完整走通）

1. 编译到 0 错误（类型映射全清）
2. `dotnet build -c Release`
3. 部署 `skwizzy.LootingBots.dll` → `SPT_410\BepInEx\plugins\`（依赖 DrakiaXYZ-BigBrain.dll 已存在）
4. 删 LogOutput.log → 跑 `sptvfsbridge.bat`（起 server+launcher）
5. 等游戏进程 → 查日志：`Enabled 7 patches` + 无 `Failed to init`
6. 菜单出现 `Updating item appraiser` = 价格系统工作

**最终结果**：7/7 patches 启用，无错误，raid 冒烟前菜单验证通过。

## 7. 可复用结论

1. **3.11→4.1 客户端移植成本**：LootingBots 约 25 个混淆名/API 点，类型映射全可脚本化；剩余是 Harmony 字段注入改名 + 结构性 API（Handbook 价格）需人工判断。
2. **同类 mod 参考**：SAIN（106 patches）在同一环境正常加载，说明 4.1 的 Bot 相关 API 稳定，LootingBots 移植后可与之共存。
3. **验证必须启动**：Harmony 字段注入问题编译不报错，必须跑游戏看日志。
