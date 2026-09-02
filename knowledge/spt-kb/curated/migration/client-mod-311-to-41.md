---
version: [3.11, 4.1]
domain: client
topic: migration
source: curated
---

# 3.11 -> 4.1 客户端 mod 迁移指南

> 状态：已提炼（2026-08-05）| 用途：自动迁移管线的客户端侧知识
> 方法：对比 3.11 与 4.1 游戏目录实测 + Class_Name_Mappings 对照表

---

## 1. 环境差异（实测）

| 组件 | 3.11 | 4.1 | 兼容性 |
|------|------|-----|--------|
| BepInEx | 5.4.22.0 | 5.4.23.5 | 同主版本，向后兼容 |
| 0Harmony | 2.9.0.0 | 2.9.0.0 | 完全相同 |
| Assembly-CSharp | 14.5 MB（混淆） | 15.3 MB（反混淆） | **不兼容，必须重编译** |
| 类命名 | GClass680 / GStruct80 / 扁平别名 | ABotProfileCreator / 真实命名空间 | **全部改名** |

## 2. 迁移步骤

### 2.1 重编译（必须）

3.11 客户端 mod DLL 无法在 4.1 加载 -- Assembly-CSharp 的类名和命名空间全部变化。必须：
1. 拿 3.11 mod 源码
2. 换引用：4.1 的 `Assembly-CSharp.dll` + `BepInEx.dll`（5.4.23）+ `0Harmony.dll`（2.9）
3. 按 `Class_Name_Mappings.md` 替换所有混淆类名
4. 重新编译

### 2.2 类名替换（核心工作）

对照表：`knowledge/spt-kb/wiki/SPT_41/modding/client/Class_Name_Mappings.md`（~9240 条映射）

替换对象：
- 直接类型引用：`GClass680` -> `ABotProfileCreator`
- Harmony patch 目标：`[HarmonyPatch(typeof(GClassXXX), "MethodName")]`
- 反射调用：`Type.GetType("GClassXXX")`
- 泛型参数：`typeof(GStructYYY)`
- 枚举：`GEnumZZZ`

**关键陷阱：**
- 类名替换后必须补 `using`（4.1 类型有真实命名空间）
- 同名类型可能映射到不同命名空间（同名不同类）
- Harmony patch 目标方法名可能也变了（不只是类名）

### 2.3 反混淆改名的自动化

类名映射表是机器可读的（markdown 表格），可以：
1. 解析映射表为 `oldName -> newName` 字典
2. 对 mod 源码做 AST 级查找替换（不能纯字符串替换 -- 防误伤 `GClass680` 出现在注释/字符串中）
3. 编译验证兜底（编译错误指出遗漏的替换）

**自动化率预估：80-90%** 的类型引用可以被机械替换；剩余的（同名歧义、方法名变化）需要 LLM 判断。

## 3. 无源码 client mod（只有 DLL）

如果只有编译好的 DLL 没有源码：
- **无法可靠自动化** -- 混淆名在 IL 层面已经是字符串/类型引用，改名需要 IL 级重写（Mono.Cecil），且方法体内部逻辑对类名的引用不总是可安全替换
- 可选路径：ILSpy/dnSpy 反编译 -> 修正源码 -> 重新编译（半自动）
- 风险评估：Harmony patch 的 `GetTargetMethod()` 反射特征在 4.1 反混淆后目标方法变了，纯 IL 替换不可靠

## 4. 验证

| 检查 | 方法 |
|------|------|
| 编译通过 | dotnet build 0 错误 |
| 插件加载 | BepInEx 控制台显示插件 load 成功 |
| Harmony patch 应用 | BepInEx 日志显示 patch 绑定到目标方法 |
| 运行时功能 | 进游戏验证 mod 功能 |
| patch 失败 | 日志 "Failed to create patch" -> 目标类/方法名仍错 |

## 5. 迁移工作量分级

| mod 类型 | 自动化率 | 说明 |
|----------|---------|------|
| 简单插件（1-2 个类引用） | ~95% | 机械替换 + 编译验证 |
| 中等插件（5-15 个引用） | ~85% | 部分需要 LLM 处理歧义 |
| 复杂插件（15+ 引用 + 反射） | ~60% | 反射/泛型/方法名变化需要人工 |
| 无源码 DLL | ~30% | 反编译后仍难自动处理 |

## 5.1 实测映射补充（2026-08-08，Wave 2/3 移植实战）

以下映射在 Wave 2（824/1089/1341/2058/2115/2344）与 Wave 3（2386/2521/1760/2162/865/2264）移植中实测触发，可直接复用：

### 类型改名（去 Class 后缀 / 重命名）
| 3.11 | 4.1 | 备注 |
|------|-----|------|
| `FuelItemClass` | `EFT.InventoryLogic.Fuel` | 通用规律：`XxxItemClass` → `EFT.InventoryLogic.Xxx` |
| `RepairKitsTemplateClass` | `RepairKitTemplate` | `_template.MaxRepairResource` |
| `MagazineItemClass` | `EFT.InventoryLogic.Magazine` | |
| `SkillClass` | `EFT.Skill` | 继承 `BaseSkill` |
| `MatchmakerPlayerControllerClass` | `EFT.UI.Matchmaker.MatchmakerPlayersController` | |
| `TraderControllerClass` | `EFT.InventoryLogic.ItemController` | |
| `DamageInfoStruct` | `EFT.Ballistics.DamageInfo` | |
| `NotificationManagerClass` | `EFT.Communications.NotificationManager` | |
| `PhraseSpeakerClass` | `EFT.BaseSpeaker` | 4.1 语音重构为 `Player.Speaker` 字段 |
| `TemplateIdToObjectMappingsClass.TypeTable` | `EFT.InventoryLogic.JsonTypes.TypeTable` | 字段同为 Dictionary<string,Type> |
| `GetActionsClass.smethod_8/9` | `EFT.InteractionContextHelper.GetAvailableActions/GetAvailableInteractionState` | |
| `ActionsReturnClass` | `EFT.UI.AvailableInteractionState` | |

### 成员改名 / 签名变化
| 3.11 | 4.1 |
|------|-----|
| `Item.TemplateId`（MongoID） | `Item.StringTemplateId`（string）——4.1 两者并存，仅改用处 |
| `GameWorld.MainPlayer` | 仍存在（public field，EFG.Player），无需改 `GamePlayerOwner.MyPlayer` |
| `Player.method_61` | `Player.StateChangedHandler`（冲刺脚步宿主） |
| `SkillClass.float_3`（Effectiveness） | `Skill._effectiveness` |
| `SkillClass.float_4`（Fatigue timer） | `Skill._fatigueTimer` |
| `InteractionsHandlerClass.Discard(item, owner)` | `ItemManipulator.Discard(item, (ItemController)item.Owner, false)` |
| `Item.Parent.GetOwner()` | `Item.Owner` |
| `Localized(null)` / `Localized(str)` | 需按签名补参 |
| `GetBodyPartHealth(bodyPart)` | `(bodyPart, false)` |
| `DisplayMessageNotification(msg, ENotificationDurationType.Long)` | `(msg, Long, ENotificationIconType.Default, null)` |
| `List<T>.Random()` | `EFT.EnumerableExtensions.PickRandom()` |
| `www.isHttpError \|\| www.isNetworkError` | `www.result != UnityWebRequest.Result.Success` |
| `TOD_Sky.Instance.Cycle.Hour` | `Singleton<GameWorld>.Instance.GameDateTime.StatedGameDateTime.Hour` |
| `OnBeenKilledByAggressor(Player, ...)` | `(EFT.IPlayer, EFT.Ballistics.DamageInfo, EBodyPart, EDamageType)`，用 `aggressor as Player` |

### 全局命名空间冲突陷阱（重要）
4.1 的 Assembly-CSharp.dll 存在**全局命名空间顶层 `Utils` 类**（90 个方法），会遮蔽 mod 自身的 `Xxx.Helpers.Utils`。实测 3 个 mod 触发（1760/2264/2521）——**直接把 mod 自己的 Utils 类改名为 ModUtils/AmbienceUtils**，并更新所有静态调用点。

### 框架/门禁
- `[BepInDependency("com.SPT.core", "3.11.0")]` → `"4.1.0"`
- 版本门禁（DrakiaXYZ 风格 `CheckEftVersion`）：SPT 4.1.2 = EFT 0.16.9.40743（FilePrivatePart 40743），**与 3.11 时代一致**，门禁常量匹配无需改
- 部分 mod 默认门禁常量是旧值（如 40087），需改为 40743（见 1760 实战）
- SDK 风格 csproj → 经典格式（ToolsVersion 15.0 / v4.7.2）是标准构建链改造；HintPath → `Refs-410\`

### 4.x 源码直编译（特殊情形）
Forge 存档中部分 mod（954 BorkelRNVG、1038 TraderModding）源码已是**作者适配的 4.x 版本**：服务端用 `SPTarkov.Server.Core` NuGet 包（net10.0 + `[Injectable]` + `IOnLoad`），客户端引用 `spt-*` 模块——零源码改动直编译（NuGet 自动还原）。判断方法：csproj 里出现 `SPTarkov.*` 或 `spt-core/spt-custom/spt-debugging` 引用即 4.x 基线。

## 6. 关键结论

1. **客户端迁移比服务端简单** -- 没有语言切换，只有改名 + 重编译
2. **BepInEx 5.x 兼容** -- 3.11 和 4.1 同版本线，插件框架无需迁移
3. **类名映射表是自动化关键** -- 机器可读，可驱动 AST 级自动替换
4. **无源码 DLL 是硬骨头** -- 无法可靠自动化，建议评估 mod 价值决定是否人工处理
5. **Harmony patch 目标是最高风险点** -- 方法名可能也变了，需要 LLM 逐一定位
