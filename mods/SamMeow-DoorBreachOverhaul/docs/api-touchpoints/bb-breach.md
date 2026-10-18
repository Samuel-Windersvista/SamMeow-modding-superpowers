# BB 侧触点核验（SPT 4.1.6 / EFT 0.16.9.40743）

> 工单 02 产物。核验方式：ilspycmd 反编译 `SPT_41x\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll` + 本工程构建/运行时实证。
> 状态口径：`[OK]` 签名/成员与源码假设一致；`[CHANGED]` 存在但签名变化；`[MISSING]` 不存在。
> **结论：全部 [OK]，无 [CHANGED]/[MISSING]。** 上游 4.1 迁移遗留的三处绑定缺陷已在工单 01 修复并经运行时验证（见第 3 节）。

## 1. 补丁目标（5 个）

| # | 目标 | 4.1.6 实装签名 | 状态 | 证据 |
|---|------|----------------|------|------|
| 1 | `BallisticCollider.ApplyHit` | `public virtual PlayerHitInfo ApplyHit(DamageInfo damageInfo, ShotId shotID)`（单重载；postfix 按名注入 `damageInfo`） | [OK] | 反编译；运行时 `Enabled patch ApplyHit` + 战局命中行 `[Patches] 对 Door 施加命中伤害 ...` |
| 2 | `InteractionContextHelper.GetAvailableActions(GamePlayerOwner, Door)` | 存在；同类共约 20 个重载 | [OK] | 反编译重载清单；运行时 `Enabled patch ActionMenuDoorPatch` |
| 3 | `InteractionContextHelper.GetAvailableActions(GamePlayerOwner, KeycardDoor, bool isProxy)` | 存在；`KeycardDoor : Door`（postfix 的 `Door` 参数注入合法） | [OK] | 同上；`Enabled patch ActionMenuKeyCardPatch` |
| 4 | `GameWorld.OnGameStarted()` | `public virtual void OnGameStarted()`（单方法） | [OK] | 反编译；运行时铺场 `Doors 219 / Containers 283 / Trunks 147` |
| 5 | `PerfectCullingBakeGroup.Toggle(bool)` | `public void Toggle(bool rendererEnabled)`（单方法）；字段真名 `runtimeGroupDataSize` / `_runtimeGroupData` | [OK] | 反编译；`Enabled patch PerfectCullingNullRefPatch`（字段注入成功） |

## 2. 非补丁依赖成员

| 成员 | 4.1.6 实装 | 状态 | 证据 |
|------|------------|------|------|
| `WorldInteractiveObject` 家族 | `DoorState`（virtual 属性）、`interactWithoutAnimation`、`Operatable`、`KeyId`、`Id`、`Handle`、`OnEnable()` | [OK] | 反编译 |
| `Door.KickOpen` | `KickOpen(bool)` / `KickOpen(Vector3, bool = false)` | [OK] | 反编译 |
| `Door.IsBreachAngle` | `public bool IsBreachAngle(Vector3 yourPosition)` | [OK] | 反编译（C4 菜单 Disabled 判定） |
| `ExecuteDoorInteraction` | `Player.CurrentManagedState.ExecuteDoorInteraction(door, interactionResult, null, this)` 调用点存在 | [OK] | 反编译调用点 + 运行时踹门开门 |
| `StartInteraction` | 两侧源码均不直接调用（BB 经 `ExecuteDoorInteraction`；DO 经 `Door.Interact` 守卫） | [OK]（无依赖） | 全仓 grep 0 命中 |
| `DoorHandle` | 全局类 `DoorHandle` 存在 | [OK] | 反编译类型清单 + 编译 |
| `Trunk.Open()` / `LootableContainer.Open()` | 编译通过 | [OK] | 构建 0 error |
| `ItemFactory.ItemTemplates` / `ItemManipulator.Discard` / `OperationResult`（`Diz.LanguageExtensions`） | 编译通过 | [OK] | 构建 0 error |
| `GameWorld.SetupItem` / `FindItemById` | `public LootItem SetupItem(Item, IPlayer, Vector3, Quaternion)` / `public Option<Item> FindItemById(string)` | [OK] | 反编译（C4 路线） |
| `Systems.Effects.Effects.EmitGrenade` | 编译通过 | [OK] | 构建 0 error |
| `DamageInfo` 字段面（`Damage` / `HittedBallisticCollider` / `HitCollider` / `HitPoint` / `Weapon` / `SourceId` / `DamageType` / `Player.iPlayer`） | 编译 + 运行 | [OK] | 构建 0 error + 战局命中日志 |
| `AmmoTemplate._id` / `ExplosionStrength` / `ProjectileCount` / `FragmentType`、`Weapon.WeapClass` | 编译通过 | [OK] | 构建 0 error |

## 3. 绑定修复（工单 01 已完成，本单复核）

| 缺陷 | 修复 | 运行时证据 |
|------|------|------------|
| `GetMethod(nameof(GetAvailableActions))` 二义（约 20 重载） | `AccessTools.Method` 显式签名选择（Door / KeycardDoor） | 两个菜单补丁 `Enabled patch`，零 `AmbiguousMatchException` |
| PerfectCulling 4.0 混淆字段名 | 映射为 4.1.6 真名 | `Enabled patch`，字段注入成功 |
| 音频路径硬编码（上游仓库布局与运行布局不一致） | 插件目录相对解析 + 缺失静音降级 | `[C4] 音频加载成功` ×2 |

## 4. 遗留行为注记（非触点缺失）

- **`IsValidDoorLockHit` 坐标系疑点**：`col.transform.InverseTransformPoint(HitPoint)`（碰撞体局部空间）与 `doorHandle.transform.localPosition`（门局部空间）比较——两者不保证同一坐标系；有把手门的「0.25m 内」判定可能失真。Pleb/SemiPleb 模式绕过该判定。待合规弹药（12/70 AP-20）实测后决定是否修正；修正将登记偏差表。
- **`ApplyHit` 为 virtual**：当前未发现门/容器碰撞体的子类覆写（战局命中日志证明实际路径经过补丁）。若后续版本出现覆写子类，需对全部实现打补丁。
- `HandleDamageForEntity` 在 `hasHitPoints` 缺失时直接返回——铺场失败时表现为「能打但不掉耐久」，排查时先看 `[Breach]` 铺场统计。
