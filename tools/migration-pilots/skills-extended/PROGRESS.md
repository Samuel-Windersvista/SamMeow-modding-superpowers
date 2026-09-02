# 会话进度存档：SkillsExtended 客户端迁移（4.1）-- 完成版

> 更新：2026-08-05（第二轮）| 编译 All-Clear：0 错误 0 警告
> 编译命令：`cd tools/migration-pilots/skills-extended; dotnet build SkillsExtendedClient.csproj -c Release`

## 已完成（勿重复）

1. **混淆名解析 8/8** -- 核心成果。映射表：
   `knowledge/spt-kb/curated/migration/client-obfuscation-mapping-skills-extended.md`
2. **类名/API 替换**：68 稳定类 + 35 EBuffId + 5 Item + Unity modules
3. **逻辑重写**：GetBarterPricePatch（Assortment/ItemPrice）、MovementContextSetSpeedLimitPatch（SetPhysicalCondition）、Notification/Prone/Stats/Camera/Silencer/Keycard 映射
4. **服务端 TS->C# 完整迁移实测通过**
5. **客户端 8 类 4.1 架构错误全部修复（30 个编译错误 -> 0）**：

| # | 3.11 | 4.1 迁移方案 | 涉及文件 |
|---|------|-------------|---------|
| 1 | `GetActionsClass.smethod_5/smethod_9` + `ActionsTypesClass` + `InteractionResult.Actions` | 门交互菜单重构：挂载 `EFT.InteractionContextHelper.GetAvailableActions(owner, Door)` / `(owner, KeycardDoor, bool)`（static），向 `AvailableInteractionState.Actions` 添加 `EFT.UI.InteractionAction`（Name/Disabled/Action 字段） | WorldInteractionUtils.cs、DoorActionPatch.cs、KeyCardDoorActionPatch.cs |
| 2 | `KeycardComponent` 子类模式匹配 + `KeyComponent` | 组件化：`item.GetItemComponent<KeyComponent>()`，字段直改 `NumberOfUsages`/`Template.MaximumNumberOfUsage` | LockPickActionHandler.cs |
| 3 | `InteractionResult` 交互动作 | 同 #1，全部改 `AvailableInteractionState` | 同上 |
| 4 | `IHealthEffect.DamageEffects` + `MedicalItem` 类型检查 | 组件化：patch `HealthEffectsComponent` ctor `(Item, IHealthEffectsComponentTemplate)`，postfix 用 `__instance.DamageEffects`（`Dictionary<EDamageEffectType, JsonType.DamageEffectSpecification>`，Cost 字段直接改）；删除 MedicalItem 检查 | HealthEffectComponentPatch.cs |
| 5 | `BarterVariant.count/_tpl` | 4.1 `BarterScheme = List<BarterVariant>`、`BarterVariant = List<BarterTemplate>`：`barterScheme.Sum(v => v.Sum(bt => bt.count))`，`scheme[0][0]._tpl`（BarterTemplate 才有 _tpl/count）；`ItemPrice(MongoID?, int)` ctor，string 可隐式转 MongoID? | GetBarterPricePatch.cs |
| 6 | `IAbstractSession.Profile` | Session 返回 `IEftSession`（继承 IProfileSession，Profile 在 IProfileSession 上）；`ClientAppUtils.GetClientApp().Session` 即 `ClientApplication<IEftSession>.Session` | GameUtils.cs |
| 7 | `Utils.Get<T>` / `Utils.RandomizePercentage` | 4.1 全局 `Utils` 类遮蔽命名空间 Utils（CS0117 不报歧义）：用全限定 `SkillsExtended.Helpers.Utils.Get`（using 别名会与全局类型 CS0576 冲突，须全限定） | SkillsPlugin.cs、LockPickingGame.cs |
| 8 | `LockPickingUseBuffElite.Value`（3.11 bool） | 4.1 `SkillManager.FloatBuff.Value` 是 `float` 字段：`Value > 0` | LockPickActionHandler.cs |

## 关键 4.1 API 事实（AsmResolver 实测）

- `InteractionResult` 只剩 `EInteractionType InteractionType` 字段 + ctor；`Actions` 列表机制已删除
- 交互菜单构建：`GamePlayerOwner.InteractionsChangedHandler` -> `InteractionContextHelper.GetAvailableActions(owner, IInteractive)` 分发 -> 各具体重载返回 `EFT.UI.AvailableInteractionState { List<InteractionAction> Actions; ... }`
- `InteractionAction` 字段：`Name`(string)、`Disabled`(bool)、`TargetName`(string)、`Action`(System.Action) -- 与 3.11 ActionsTypesClass 结构几乎一致
- `WorldInteractiveObject.Interact(InteractionResult)` / `Door.Interact(Player, EInteractionType)`（static，返回 Option<InteractionResult>）-- 未被本 mod 使用
- `Item.GetItemComponent<T>()` / `TryGetItemComponent<T>(out T)` 是 4.1 组件获取标准 API
- `KeycardComponent`（门禁卡）有 `Key`(KeyComponent) + `Template`(IKeyComponentTemplate)；`KeyComponent` 有 `NumberOfUsages`(int) + `Template`(IKeyComponentTemplate{KeyId, MaximumNumberOfUsage})
- `SkillManager.FloatBuff.Value` 是 `float` 字段；`BooleanBuff.Value` 才是 bool
- `HealthEffectsComponent` ctor `(Item, IHealthEffectsComponentTemplate)`；`DamageEffects` 属性在组件上（不在 IHealthEffect）
- `EDamageEffectType`：HeavyBleeding/LightBleeding/Fracture/Contusion/...（与 3.11 同名）
- `MongoID` 有 `op_Implicit(string) -> MongoID?`，可直接传 `_tpl`

## 验证状态

- [x] 服务端迁移：0 错误 0 警告，dbdump 实测（lockpick 物品 + 配方 + locale）
- [x] 客户端编译：0 错误 0 警告（93 类型入 DLL，72KB）
- [ ] 客户端实机：未验证（需 SPT 4.1 启动 + 进 raid 测试 lockpick/医疗/消音器功能）

## 续接建议

- 实机验证路径：`E:\Game\EFT_Offline\SPT_410\` 启动 SPT，BepInEx 控制台确认 SkillsExtended 加载 + Harmony patch 绑定
- 高优先级实机点：DoorActionPatch 目标方法名（GetAvailableActions 有 20+ 重载，AccessTools.Method 按参数类型精确匹配已过编译，运行时需确认绑定成功）
- 原 3.11 源码：`E:\云文件\GitHub\SamMeow-Skills-Extended\Plugin\`
- 混淆名映射方法论文档：`knowledge/spt-kb/curated/migration/client-obfuscation-mapping-skills-extended.md`
