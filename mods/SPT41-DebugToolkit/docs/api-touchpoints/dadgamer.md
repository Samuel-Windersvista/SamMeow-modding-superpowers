# DadGamerMode — SPT 4.1.6 移植 API 触点核实表（工单 02 子通道 C）

> 结论四态（纯文本）：**[OK]** 同名存在 / **[CHANGED]** 改名、签名或可见性变化（给出新形态）/ **[MISSING]** 缺失（必须给降级预案）/ **[UNCERTAIN]** 无法静态定位（给最佳候选 + 理由）。
> 本表逐条核实上游 DadGamerMode 1.9.3（3.8 世代混淆名）对 EFT 0.16.9.40743（已反混淆真名）的每一个补丁目标与反射触点。
> 与 5.0 线 `SamMeow-DebugToolkit/docs/api-touchpoints/invincibility.md` 的差异：本表为 **Mono / BepInEx 5 / netstandard2.1**——无 IL2CPP 代理约束，可托管直调、原生事件订阅、`GetOrAddComponent` 直接可用；上游 3.8 混淆名在此需逐一映射到 0.16.9 真名。

---

## 0. 头部

### 0.1 来源 provenance（只读）

| 项 | 值 |
|---|---|
| 上游仓库（本仓归档） | `knowledge/spt-kb/archive/forge/mods/DadGamerMode_686_source/` |
| 上游版本 | `1.9.3`（`Properties/AssemblyInfo.cs`） |
| License | MIT（上游 `LICENSE.txt` 为占位，按 Props/dvize 署名） |
| 上游混淆锚点 | 3.8 世代（`GClass*` / `GInterface*` / `method_N` 名式），需语义映射 |
| 主表覆盖源文件 | `Patches\ApplyDamage.cs`、`Patches\DestroyBodyPart.cs`、`Patches\InstantProductionPatch.cs`、`Patches\InstantConstructionPatch.cs`、`Patches\OnWeightUpdatedPatch.cs`、`Features\CODMode.cs`、`Features\Energy.cs`、`Features\Hydration.cs`、`Features\MaxStamina.cs`、`Features\MagReloadSpeed.cs`、`Features\FallingDamage.cs` |

### 0.2 目标环境

| 项 | 值 |
|---|---|
| SPT 安装 | `E:\Game\EFT_Offline\SPT_41x\`（SPT 4.1.6 / EFT 0.16.9.40743） |
| 运行时 | Mono（`EscapeFromTarkov_Data\Managed\`） |
| EFT 程序集 | `EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`（已反混淆真名；勿用同目录 `.spt-bak`） |
| 辅助程序集 | 同目录 `Comfort.dll` |
| 说明 | 0.16.9 的 `EBodyPart` / `EDamageType` / `Stamina` / `PhysicalBase` 均在 **Assembly-CSharp**（4.1.6 未拆到 `PlayerEnums.dll`，与 5.0 线不同） |

### 0.3 工具链与命令

```powershell
ilspycmd --version   # 11.0.0.9375
$A = "E:\Game\EFT_Offline\SPT_41x\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"

# 定向类型反编译
ilspycmd -t "EFT.HealthSystem.ActiveHealthController" $A
ilspycmd -t 'EFT.HealthSystem.BaseHealthController`1' $A
ilspycmd -t "EFT.Hideout.ItemsProducerBase" $A
ilspycmd -t "EFT.InventoryLogic.InventoryEquipment" $A

# 全量反编译后文本检索（用于定位旧混淆名的语义继任者）
ilspycmd -o <dir> --nested-directories $A

# 二次独立核实：.NET 反射（与 ILSpy 互为独立证据）
$asm = [Reflection.Assembly]::LoadFrom($A)
$asm.GetType("EFT.HealthSystem.ActiveHealthController").GetMethod("ApplyDamage")
```

---

## 1. 主表（触点核实）

### 1.1 无敌组 — 伤害补丁（`Patches\ApplyDamage.cs`）

| # | 来源文件 | 目标（旧名 → 0.16.9 名） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 1 | `ApplyDamage.cs:17,21` | `ActiveHealthController.ApplyDamage` | `public float ApplyDamage(EBodyPart bodyPart, float damage, DamageInfo damageInfo)`，**非 virtual**，唯一重载。Prefix 形参 `(ActiveHealthController __instance, ref float damage, EBodyPart bodyPart, DamageInfo damageInfo)` 成立，`ref float damage` 可改写，`return false` 短路 | 直用 | `return AccessTools.Method(typeof(ActiveHealthController), "ApplyDamage");` | [OK] |
| 2 | `ApplyDamage.cs:29` | `ActiveHealthController.GetBodyPartHealth` | 定义于基类 `BaseHealthController<ActiveHealthController.Effect>`：`public ValueStruct GetBodyPartHealth(EBodyPart bodyPart, bool rounded = false)`，返回 `EFT.HealthSystem.ValueStruct` | 直用 | `ValueStruct cur = healthController.GetBodyPartHealth(bodyPart, false);` | [OK] |
| 3 | `ApplyDamage.cs:62,70,77,84` | `ValueStruct.Current / .Minimum / .AtMinimum` | `public struct ValueStruct`：public 字段 `float Current / Maximum / Minimum`（另有 `OverDamageReceivedMultiplier` / `EnvironmentDamageMultiplier`），只读属性 `bool AtMinimum / AtMaximum`、`float Normalized` | 直用；**值类型语义**：`currentHealth = GetBodyPartHealth(...)` 得到的是副本，`currentHealth.Current = 3f;` 不会写回控制器（此为上游既有行为，非移植缺陷） | 读判用 `.Current` / `.Minimum` / `.AtMinimum`；`3f` 写回本就不生效，有效逻辑是 `damage = 0f; return false;` | [OK] |
| 4 | `ApplyDamage.cs:21` | `DamageInfo`（旧 3.8 名） | `EFT.Ballistics.DamageInfo`——**0.16.9 为 `public struct`**（非引用类型）；字段 `DamageType` / `Damage` / `PenetrationPower` / `HitCollider` / `Direction` / `HitPoint` 等 | 直用；`new DamageInfo()` 合法 | `using EFT.Ballistics;` 即可；不要按引用类型用 | [CHANGED] |
| 5 | `ApplyDamage.cs:25-28` | `ActiveHealthController.Player` / `Player.IsYourPlayer` / `Player.ActiveHealthController` | `public Player Player;`（public 字段）；`public bool IsYourPlayer { get; set; }`（属性，可读）；`public ActiveHealthController ActiveHealthController => _healthController as ActiveHealthController;` | 直用 | `if (__instance.Player != null && __instance.Player.IsYourPlayer) { ... }` | [OK] |

### 1.2 无敌组 — 部位摧毁（`Patches\DestroyBodyPart.cs`）

| # | 来源文件 | 目标（旧名 → 0.16.9 名） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 6 | `DestroyBodyPart.cs:18,22` | `ActiveHealthController.DestroyBodyPart` | `public void DestroyBodyPart(EBodyPart bodyPart, EDamageType damageType)`，**非 virtual**。Prefix `(ActiveHealthController __instance, EBodyPart bodyPart, EDamageType damageType)` + `return false` 成立 | 直用 | `return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.DestroyBodyPart));` | [OK] |
| 7 | `DestroyBodyPart.cs:13,27-28,53` | `EBodyPart.Head / .Chest / .Stomach`、`EDamageType` | `EBodyPart`（**全局命名空间**，Assembly-CSharp）、`EFT.EDamageType`（Assembly-CSharp，`[Flags]`）；枚举值包含 `Head, Chest, Stomach, LeftArm, RightArm, LeftLeg, RightLeg, Common` | 直用 | 引用 Assembly-CSharp 即可；`using EFT;` 供 `EDamageType` | [OK] |

### 1.3 CODMode（`Features\CODMode.cs`）

| # | 来源文件 | 目标（旧名 → 0.16.9 名） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 8 | `CODMode.cs:8,85` | 混淆 `AbstractIEffect = ActiveHealthController.GClass2429` | → `EFT.HealthSystem.ActiveHealthController.Effect`（嵌套 `public abstract class Effect : IHealthEffect`）；`Effect<TStore> : Effect` 为其泛型派生 | 别名替换 | `using AbstractIEffect = EFT.HealthSystem.ActiveHealthController.Effect;` | [CHANGED] |
| 9 | `CODMode.cs:63` | `healthController.EffectAddedEvent` / handler `IEffect` | `public sealed override event Action<IHealthEffect> EffectAddedEvent`；handler 形参 `EFT.HealthSystem.IHealthEffect` | 直用（Mono 可原生订阅） | `healthController.EffectAddedEvent += HealthController_EffectAddedEvent;`（形参写 `IHealthEffect`） | [CHANGED] |
| 10 | `CODMode.cs:77` | 混淆 `effect is GInterface252` / `is GInterface253`（排除项） | → `EFT.HealthSystem.IDesirable` / `EFT.HealthSystem.IPermanent`（两空标记接口，均存在）。**依据**：0.16.9 原生 `RemoveNegativeEffects` 谓词 = `!(effect is IDesirable) && !(effect is IPermanent)`，与上游 `!(252) && !(253)` 结构完全一致 | 命名接口复刻；或直接改用原生 `RemoveNegativeEffects`（首选） | `if (effect is not IDesirable && effect is not IPermanent) healthController.RemoveEffectFromList(effect as ActiveHealthController.Effect);` | [CHANGED] |
| 11 | `CODMode.cs:85` | 混淆 `healthController.RemoveEffectFromList(effect as AbstractIEffect)` | `BaseHealthController<ActiveHealthController.Effect>`：`public virtual bool RemoveEffectFromList(ActiveHealthController.Effect effect)` | 泛型实参收敛为 `ActiveHealthController.Effect` | 先 `var e = effect as ActiveHealthController.Effect; if (e != null) healthController.RemoveEffectFromList(e);` | [CHANGED] |
| 12 | `CODMode.cs:127,130` | 混淆 `healthController.Dictionary_0[limb].Health` / `.IsDestroyed` | → `healthController.BodyState[limb]`（`public Dictionary<EBodyPart, BodyPartState> BodyState => _bodyState;`，另有 public 字段 `_bodyState`）。`BodyPartState`（嵌套 public class）暴露 **public 字段** `Health : HealthValue` 与 `IsDestroyed : bool` | 直用；`HealthValue` 为**引用类型**，`Current` setter 会写回控制器，故 CODMode `currentHealth.Current += newHealRate` 生效 | `currentHealth = healthController.BodyState[limb].Health; if (!currentHealth.AtMaximum && !healthController.BodyState[limb].IsDestroyed) currentHealth.Current += newHealRate;` | [CHANGED] |
| 13 | `CODMode.cs:128`（注释） | 混淆 `healthController.FindActiveEffect<GInterface242/243>`（流血判定，已注释） | `public TEffect FindActiveEffect<TEffect>(EBodyPart bodyPart = EBodyPart.Common) where TEffect : IHealthEffect`（基类） | 直用（若启用注释逻辑） | `healthController.FindActiveEffect<IBleeding>(limb)` | [OK] |
| 14 | `CODMode.cs:130` | `HealthValue.AtMaximum` / `.Current` / `.Minimum` | `public class HealthValue`：`public virtual float Current { get; set; }`、`float Minimum`、`float Maximum`、`bool AtMinimum`、`bool AtMaximum`；另 public 字段 `ValueStruct Value` | 直用 | 同 #12 | [OK] |
| 15 | `CODMode.cs:59` | `player.BeingHitAction`（`Action<DamageInfo, EBodyPart, float>`） | `public event Action<DamageInfo, EBodyPart, float> BeingHitAction;`（`DamageInfo` 为 struct） | 直用；Mono 无 IL2CPP 代理封送问题 | `player.BeingHitAction += Player_BeingHitAction;` | [OK] |
| 16 | `CODMode.cs:58` | `player.OnPlayerDeadOrUnspawn` | `public event OnPlayerDeadOrUnspawn OnPlayerDeadOrUnspawn;`；委托 `public delegate void OnPlayerDeadOrUnspawn(Player player);` | 直用 | `player.OnPlayerDeadOrUnspawn += Player_OnPlayerDeadOrUnspawn;` | [OK] |
| 17 | `CODMode.cs:39,42` | `Singleton<IBotGame>.Instantiated` / `Singleton<GameWorld>.Instance` / `gameWorld.GetOrAddComponent<T>()` | `Comfort.Common.Singleton<T>` 存在；`GameObject.GetOrAddComponent<T>(this GameObject)` 与 `(this MonoBehaviour)` 扩展在 Assembly-CSharp 存在 | 直用 | `Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<CODModeComponent>();` | [OK] |
| 18 | `CODMode.cs:30` | 私有构造 `private CODModeComponent()` | Mono 下 `AddComponent` 需 public 无参构造（Unity 反射实例化） | 构造改 `public` | `public CODModeComponent() { ... }` | [CHANGED] |

### 1.4 无限能量 / 水分（`Features\Energy.cs`、`Features\Hydration.cs`）

| # | 来源文件 | 目标（旧名 → 0.16.9 名） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 19 | `Energy.cs:30,37,38` | `EnergyChangedEvent` / `ChangeEnergy(float)` / `Energy.Maximum` | `public sealed override event Action<float> EnergyChangedEvent`；`public void ChangeEnergy(float value)`（非 virtual）；`BaseHealthController.Energy` 现为 **`ValueStruct`**（`_energy.CurrentAndMaximum`），`ValueStruct.Maximum` 为 public 字段 | `.Maximum` 仍可解析（字段名未变）；无降级 | `player.ActiveHealthController.ChangeEnergy(player.ActiveHealthController.Energy.Maximum);` | [CHANGED] |
| 20 | `Hydration.cs:31,40` | `HydrationChangedEvent` / `ChangeHydration(float)` / `Hydration.Maximum` | `Action<float> HydrationChangedEvent`；`public void ChangeHydration(float value)`；`Hydration` 现为 `ValueStruct` | 同 #19 | `player.ActiveHealthController.ChangeHydration(player.ActiveHealthController.Hydration.Maximum);` | [CHANGED] |

### 1.5 无限体力（`Features\MaxStamina.cs`）

| # | 来源文件 | 目标（旧名 → 0.16.9 名） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 21 | `MaxStamina.cs:27,33-35` | `Player.Physical` | `public PhysicalBase Physical;`（public 字段；`Physical : PhysicalBase`） | 直用 | `player.Physical.Stamina.Current = player.Physical.Stamina.TotalCapacity.Value;` | [OK] |
| 22 | `MaxStamina.cs:33-35` | `Physical.Stamina / .HandsStamina / .Oxygen` | `PhysicalBase` public 字段：`public Stamina Stamina; public Stamina HandsStamina; public Stamina Oxygen;` | 直用 | 同上，三行分别赋值 | [OK] |
| 23 | `MaxStamina.cs:33-35` | `Stamina.Current` / `Stamina.TotalCapacity` | `public float Current;`（public 字段）；`public readonly Compute<float> TotalCapacity;`；`Compute<T>.Value` 为 virtual 属性（另有 `Compute<T>` → T 隐式转换） | 直用 | `...TotalCapacity.Value` | [OK] |

### 1.6 换弹 / 卸弹速度（`Features\MagReloadSpeed.cs`）

| # | 来源文件 | 目标（旧名 → 0.16.9 名） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 24 | `MagReloadSpeed.cs:22,23` | `Singleton<BackendConfigSettingsClass>.Instance.BaseLoadTime` | `BackendConfigSettingsClass` **已不存在**；真名 `EFT.GlobalConfiguration`（`Comfort.Common.Singleton<GlobalConfiguration>.Instance`），public 字段 `public float BaseLoadTime = 0.85f;`（与上游重置默认一致）；实际换弹路径即读 `Singleton<GlobalConfiguration>.Instance.BaseLoadTime`（`LoadMagazine`） | 类型替换 | `Singleton<GlobalConfiguration>.Instance.BaseLoadTime = dadGamerPlugin.ReloadSpeed.Value;` | [CHANGED] |
| 25 | `MagReloadSpeed.cs:22,23` | `BaseUnloadTime` | `public float BaseUnloadTime = 0.3f;`（默认与上游一致）；`UnloadMagazine` 读取之 | 类型替换 | `Singleton<GlobalConfiguration>.Instance.BaseUnloadTime = dadGamerPlugin.UnloadSpeed.Value;` | [CHANGED] |

### 1.7 无坠落伤害（`Features\FallingDamage.cs`）

| # | 来源文件 | 目标（旧名 → 0.16.9 名） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 26 | `FallingDamage.cs:32,53` | `ActiveHealthController.FallSafeHeight`（float） | `public override float FallSafeHeight { set { _fallSafeHeight = value; } }`（仅覆写 setter，virtual）。后备字段 `_fallSafeHeight` 现为 **`private new float`**（不可直写）；`HandleFall` 用 `_fallSafeHeight` | 直用属性即可；Mono 无 IL2CPP virtual 分派风险，无需走字段 | `player.ActiveHealthController.FallSafeHeight = dadGamerPlugin.NoFallingDamage.Value ? 999999f : 1.8f;` | [OK] |

### 1.8 瞬时生产（`Patches\InstantProductionPatch.cs`）— 混淆名映射

| # | 来源文件 | 目标（旧名 → 0.16.9 名） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 27 | `InstantProductionPatch.cs:17,21` | 混淆 `GClass1931.Update(float)` | → `EFT.Hideout.ItemsProducerBase.Update(float deltaTime)`（`public void`，实现 `IUpdate`）：遍历 `ProducingItems` 调 `value.Produce()`，`Progress >= 1.0` 则入 `_doneItems` → `ProcessDoneItems()` 完成产出 | 目标替换；瞬时化见 #28/#29 方案 | `AccessTools.Method(typeof(EFT.Hideout.ItemsProducerBase), "Update", new[] { typeof(float) });` | [CHANGED] |
| 28 | `InstantProductionPatch.cs:31` | 混淆 `GClass1931.ProducingItems`（`Dictionary<string, GClass1937>`） | → `ItemsProducerBase.ProducingItems : Dictionary<string, ProducingItemController>`（get-only 属性）；`Schemes : Dictionary<string, BaseHideoutScheme>` | 直用 | `foreach (var kv in __instance.ProducingItems) { ... }` | [CHANGED] |
| 29 | `InstantProductionPatch.cs:38,58-59,80` | 混淆 `GClass1937` / `GClass1937.Class1666` / `class1666_0` / `double_1`(进度) | → `ProducingItemController` / 嵌套 `ProducingItemController.ProducingProcess` / public 字段 `_producingProcess` / `ProducingProcess.Progress { get; set; }`（double）。`ProducingItemController.Progress` 为只读属性；`Produce()` 由 `EndTime` 重算进度 | 直用替换反射；瞬时化的稳健点 = 把 `_producingProcess.EndTime` 置为当前（`Produce()` 即算出 `Progress=1.0`） | `item._producingProcess.EndTime = DateTimeExtensions.UtcNowUnix;` 后再跑原 `Update` | [CHANGED] |
| 30 | `InstantProductionPatch.cs:38,102-104` | 混淆 `CompleteProduction` 链（`CreateCompleteItem` / `BeforeProductionComplete` / `CompleteItemsStorage.AddItem` / `SetDetailsData`） | 0.16.9 原生已具备该链：`ItemsProducerBase.CreateCompleteItem(BaseHideoutScheme)` / `BeforeProductionComplete(string)` / `CompleteItemsStorage.AddItem` / `SetDetailsData()`，由 `ProcessDoneItems()` 自动串起 | 不必自造扩展方法；让 `Progress` 到 1.0 即可 | 保留上游"排除比特币/燃料"语义：按 `ProducingItemController.SchemeId` 过滤 | [CHANGED] |

### 1.9 瞬时建造（`Patches\InstantConstructionPatch.cs`）— 混淆名映射

| # | 来源文件 | 目标（旧名 → 0.16.9 名） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 31 | `InstantConstructionPatch.cs:17,21` | 混淆 `AreaData."method_0"(int) : Task` | → `EFT.Hideout.AreaData.InitConstructing(int timestamp, bool alreadyUnderConstructing) : Task`。其 `!alreadyUnderConstructing` 分支在 `num` 秒后恰好设置：`Waiting=false; Status=(CurrentLevel>0?ReadyToInstallUpgrade:ReadyToInstallConstruct); ActionGoing=false; ActionReady=true`——与上游 `InstantCompleteConstruction` 体**逐字段一致**（识别依据见 §2.2） | 签名多一参 `bool`：Prefix 改 `(ref Task __result, AreaData __instance, int timestamp, bool alreadyUnderConstructing)`；瞬时化 = 直接复刻上游字段设置 | 见 §4.2 降级/适配代码片段 | [CHANGED] |
| 32 | `InstantConstructionPatch.cs:42-52` | `CurrentStage` / `Status` / `CurrentLevel` | `AreaData.CurrentStage`（`=> StageAt(CurrentLevel)`）、`AreaData.Status`（public get/set）、`AreaData.CurrentLevel` | 直用 | `__instance.CurrentStage.Waiting = false; __instance.Status = __instance.CurrentLevel > 0 ? EAreaStatus.ReadyToInstallUpgrade : EAreaStatus.ReadyToInstallConstruct;` | [OK] |
| 33 | `InstantConstructionPatch.cs:48-52` | `Stage.Waiting` / `Stage.ActionGoing` / `Stage.ActionReady` | `public class Stage`：`public bool ActionGoing { get; set; }` / `public bool Waiting { get; set; }` / `public bool ActionReady { get; set; }`（均 public get/set） | 直用 | `stage.ActionGoing = false; stage.ActionReady = true;` | [OK] |

### 1.10 负重百分比（`Patches\OnWeightUpdatedPatch.cs`）— 混淆名映射

| # | 来源文件 | 目标（旧名 → 0.16.9 名） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 34 | `OnWeightUpdatedPatch.cs:22` | 混淆 `EquipmentClass.method_10(IEnumerable<Slot>)` | → `EFT.InventoryLogic.InventoryEquipment.GetTotalWeight(IEnumerable<Slot> slots) : float`（**static**）：`slots.Sum(slot => slot.ContainedItem?.TotalWeight ?? 0f)` | 类型替换；改为 **static 方法 Prefix**（无 `__instance`） | `AccessTools.Method(typeof(InventoryEquipment), "GetTotalWeight", new[] { typeof(IEnumerable<Slot>) });` | [CHANGED] |
| 35 | `OnWeightUpdatedPatch.cs:30` | 混淆 `EquipmentClass.method_11(Item) : float` | → `Item.TotalWeight : float`（`public virtual float TotalWeight => Weight * (StackObjectsCount<=0?1:StackObjectsCount);`，`Weight => Template.Weight`） | 直接用属性 | `item.TotalWeight` | [CHANGED] |
| 36 | `OnWeightUpdatedPatch.cs:30` | 混淆 `Class2102.class2102_0.method_1(Slot) : Item`（槽→物品选择器） | → `Slot.ContainedItem : Item`（属性） | 直接用属性 | `slot.ContainedItem?.TotalWeight ?? 0f` | [CHANGED] |
| 37 | `OnWeightUpdatedPatch.cs:26,41` | Prefix 签名 `(EquipmentClass __instance, ref float __result, IEnumerable<Slot> slots)` | `GetTotalWeight` 为 static → Prefix 形参 `(ref float __result, IEnumerable<Slot> slots)`；`return false` 跳过原方法 | 改写 Prefix | `__result = slots.Sum(s => s.ContainedItem?.TotalWeight ?? 0f) * (weightPct / 100f); return false;` | [CHANGED] |

> 备选目标：`InventoryEquipment.GetEquipmentWeight()`（`public float`，`=> GetTotalWeight(GetAllSlots())`）——若需仅在"取装备总重"处生效可改挂此点，但覆盖面窄于 `GetTotalWeight`。

---

## 2. 三条混淆名映射结论（真名定位依据）

### 2.1 瞬时生产：`GClass1931.Update` → `EFT.Hideout.ItemsProducerBase.Update`

真名：**`EFT.Hideout.ItemsProducerBase.Update(float deltaTime)`**（`public void`，`ItemsProducerBase : IUpdate`）。

识别依据（语义指纹，逐项对齐上游字段/方法名）：
- `ItemsProducerBase` 持有 `ProducingItems : Dictionary<string, ProducingItemController>`、`Schemes : Dictionary<string, BaseHideoutScheme>`、`CompleteItemsStorage : IProductionStorage`、`MaxProducingItems`、`AreaData`，并定义 `CreateCompleteItem(BaseHideoutScheme)` / `BeforeProductionComplete(string)` / `SetDetailsData()` / `UpdateProductionTimeReduction(float)`——与上游 `GClass1931` 成员面逐一对应。
- `Update` 体即"制造进度推进"：`ProcessErrors() → PreUpdate() → foreach ProducingItems { Produce(); if (Progress>=1.0) _doneItems.Add } → ProcessDoneItems()`；`Produce()` 按 `EndTime` 重算 `Progress`（见 `ProducingItemController.Produce` L702128-702136）。
- 上游反射字段 `class1666_0` + `double_1` → `ProducingItemController._producingProcess`（public 字段）+ `ProducingProcess.Progress`（get/set）。
- 降级（无需）：真名可定位，完整可移植。

### 2.2 瞬时建造：`AreaData."method_0"(int)` → `EFT.Hideout.AreaData.InitConstructing`

真名：**`EFT.Hideout.AreaData.InitConstructing(int timestamp, bool alreadyUnderConstructing)`**（`public async Task`）。

识别依据（**方法体逐字段比对**，非名字猜测）：
- 上游 `InstantCompleteConstruction` 设置 `CurrentStage.Waiting=false`、`Status=(CurrentLevel>0?ReadyToInstallUpgrade:ReadyToInstallConstruct)`、`ActionGoing=false`、`ActionReady=true`。
- 0.16.9 `InitConstructing` 的 `!alreadyUnderConstructing` 分支（L705722-705731）设置**完全相同**的四个字段/属性（唯一差别是 0.16.9 用 `DecideStatus` 的另一分支、以及先 `await Delay(num)`）。
- 上游 `method_0` 的 `int timestamp` 形参与 0.16.9 `int timestamp` 一致；调用点 `InitProfileData` 传 `areaInfo.CompleteTime`（构造中）。
- 仍需在实现期以 F12 开关 + 游戏内观察二次确认（静态已充分，但行为确认在 S3 接缝）。
- 降级预案：若 `InitConstructing` 因 `alreadyUnderConstructing` 分支行为不符预期，可改挂 `AreaData.WaitUpgradeTime(float, CancellationToken)`（同样设置 `Waiting=false` + `DecideStatus`）作为替代；最坏情况按 spec「逐项降级」禁用瞬建 + README 偏差表记录。

### 2.3 负重：`EquipmentClass.method_10/11` + `Class2102.class2102_0.method_1` → `InventoryEquipment` 三件套

真名：
- `method_10(IEnumerable<Slot>) : float` → **`EFT.InventoryLogic.InventoryEquipment.GetTotalWeight(IEnumerable<Slot>)`**（static）。
- `method_11(Item) : float` → **`Item.TotalWeight`**（属性；内部即 `Weight * StackObjectsCount`）。
- `Class2102.class2102_0.method_1(Slot) : Item` → **`Slot.ContainedItem`**（属性）。

识别依据：
- 上游 Prefix 体 `slots.Select(slot→item).Sum(item→weight)` 与 `GetTotalWeight` 的 `slots.Sum(slot => slot.ContainedItem?.TotalWeight ?? 0f)` **语义逐字对应**（选择器 + 求和）。
- `InventoryEquipment.GetEquipmentWeight()` = `GetTotalWeight(GetAllSlots())`，即装备总重的实际计算入口；`TotalWeight`/`TotalWeightEliteSkill` 的 `Deferred` 即调用之（L822873-822874），故补 `GetTotalWeight` 可覆盖全部装备重量取用点。
- 3.8 `EquipmentClass` 现拆分为纯物品类型 `Equipment`（无重量计算）+ 计算类 `InventoryEquipment`。
- 降级（无需）：真名可定位。

---

## 3. 降级预案（与 spec「逐项降级」策略一致）

| 触点 | 触发条件 | 降级动作 | README 偏差表条目 |
|---|---|---|---|
| 瞬建（#31） | `InitConstructing` 行为不符 / 替换点失效 | 改用 `WaitUpgradeTime`；仍失败则禁用「瞬建」开关 + 日志警告 | 「瞬建不可用：0.16.9 建造计时点 `InitConstructing` 适配失败」 |
| 瞬产（#27–30） | `ItemsProducerBase.Update` 补丁致异常 | 回退为仅在 `ProducingItemController.Produce()` 上做 Prefix（置 `Progress=1.0`）；仍失败禁用「瞬产」 | 「瞬产不可用：制造计时链 `ItemsProducerBase` 适配失败」 |
| 负重（#34–37） | `GetTotalWeight` 补丁影响面过宽致异常 | 改挂 `GetEquipmentWeight()`（仅覆盖装备总重）；仍失败禁用「负重百分比」 | 「负重百分比不可用」 |

> 注：GodMode 坠落语义（spec「已知必录两条」之一）与逐项降级清单以实际复核结果为准，本表未新增语义偏离项——所有无敌组触点在 0.16.9 均同名存在或可直接改名适配。

---

## 4. 二次独立核实（≥5 条，双方法）

方法 A = ILSpy 反编译（`-t` 定向 / `-o` 全量文本检索）；方法 B = **.NET 反射**（`Assembly.LoadFrom` + `GetMethod/GetField/GetProperty/GetEvent`），与 ILSpy 互为独立元数据通路。以下均双方法一致：

| 触点 | 方法 A（ILSpy） | 方法 B（反射） | 一致 |
|---|---|---|---|
| #1 `ApplyDamage` | `public float ApplyDamage(EBodyPart, float, DamageInfo)`（AHC L3347，非 virtual） | `ret=Single params=EBodyPart bodyPart, Single damage, DamageInfo damageInfo; IsVirtual=False` | 是 |
| #6 `DestroyBodyPart` | `public void DestroyBodyPart(EBodyPart, EDamageType)`（AHC L3493） | `ret=Void params=EBodyPart bodyPart, EDamageType damageType; IsVirtual=False` | 是 |
| #2 `GetBodyPartHealth` | 基类返回 `ValueStruct`（BaseHC L808） | `ret=ValueStruct params=EBodyPart bodyPart, Boolean rounded` | 是 |
| #12 `BodyState` / `BodyPartState` | `BodyState => _bodyState`（BaseHC L615）；`BodyPartState.Health : HealthValue`(L23)/`IsDestroyed : bool`(L21) | `BaseHealthController<T>.BodyState`（Dictionary<EBodyPart,BodyPartState>）；`BodyPartState` 字段存在 | 是 |
| #10 `RemoveNegativeEffects`（IDesirable/IPermanent 依据） | 谓词 `!(effect is IDesirable) && !(effect is IPermanent)`（AHC L3265） | `EFT.HealthSystem.IDesirable` / `IPermanent` 类型存在 | 是 |
| #24/#25 `GlobalConfiguration.BaseLoadTime/BaseUnloadTime` | 字段 L543931/L543933（默认 0.85/0.3），使用点 L560133/L560155 | `GlobalConfiguration.BaseLoadTime : type=Single public=True` | 是 |
| #31 `AreaData.InitConstructing` | `public async Task InitConstructing(int, bool)`（L705712） | `ret=Task params=Int32 timestamp, Boolean alreadyUnderConstructing` | 是 |
| #27 `ItemsProducerBase.Update` | `public void Update(float)`（L699484） | `ret=Void params=Single deltaTime; ProducingItems=Dictionary<String,ProducingItemController>` | 是 |
| #34 `InventoryEquipment.GetTotalWeight` | `public static float GetTotalWeight(IEnumerable<Slot>)`（L824621） | `IEnumerable<Slot> slots; static=True; ret=Single` | 是 |
| #26 `FallSafeHeight` | `public override float FallSafeHeight { set; }`（L3015）；`_fallSafeHeight` private(L3004) | `FallSafeHeight: Single virtual=True` | 是 |
| #15 `Player.BeingHitAction` | `public event Action<DamageInfo, EBodyPart, float>`（Player L25070） | `Action<DamageInfo,EBodyPart,Single>` | 是 |
| #22 `Stamina` 三件套 | `PhysicalBase.Stamina/HandsStamina/Oxygen : Stamina`（L355/357/359）；`Stamina.TotalCapacity : Compute<float>`（L28） | `PhysicalBase.Stamina field: Stamina`；`Stamina.TotalCapacity: Compute<Single>` | 是 |

### 4.1 字节级旁证（元数据字符串堆）

`Assembly-CSharp.dll`（15.5 MB）内含成员名字符串 `BaseLoadTime`（offset 13209925）、`BaseUnloadTime`（13209973）、`FallSafeHeight`（14065929）；`BackendConfigSettingsClass` 字符串**不存在**（确证旧类名已移除）。此旁证用于"旧名消失 / 新名存在"的二分确认。

### 4.2 瞬时建造适配片段（供 05/06 引用，非最终实现）

```csharp
// 目标：EFT.Hideout.AreaData.InitConstructing(int timestamp, bool alreadyUnderConstructing)
[PatchPrefix]
private static bool Prefix(AreaData __instance, int timestamp, bool alreadyUnderConstructing)
{
    try
    {
        if (!dadGamerPlugin.InstantConstructionEnabled.Value) return true;
        if (__instance == null || __instance.CurrentStage == null) return true;
        Stage stage = __instance.CurrentStage;
        stage.Waiting = false;
        __instance.Status = (__instance.CurrentLevel > 0)
            ? EAreaStatus.ReadyToInstallUpgrade
            : EAreaStatus.ReadyToInstallConstruct;
        stage.ActionGoing = false;
        stage.ActionReady = true;
        return false; // 跳过原 async 计时
    }
    catch (Exception e) { dadGamerPlugin.Logger.LogError(e); return true; }
}
```

---

## 5. 验收自检

| 验收项 | 结论 |
|---|---|
| 每条触点含：目标签名、可达性结论、适配/降级方案、一行实现指引 | 满足（§1 主表 37 条，状态列覆盖） |
| 三点混淆名映射给出 0.16.9 真名或「无法定位」 | 三者均**定位成功**：瞬产 `ItemsProducerBase.Update`；瞬建 `AreaData.InitConstructing`；负重 `InventoryEquipment.GetTotalWeight`（+`Item.TotalWeight`+`Slot.ContainedItem`）。无 `[MISSING]` / `[UNCERTAIN]` |
| 至少 5 条二次独立核实并注明 | 满足：§4 列 12 条，均 ILSpy + .NET 反射双方法一致；另附字节级旁证（§4.1） |
| 状态分布 | [OK] 17 / [CHANGED] 20 / [MISSING] 0 / [UNCERTAIN] 0（主表 37 条） |
| 已知不确定项 | 瞬建的实际行为（`alreadyUnderConstructing` 分支）需在 S3 游戏内确认；静态映射充分。若失败，按 §3 降级 |

--- END OF TERMINAL ENTRY --- Vault-Tec：Preparing for the Future!
