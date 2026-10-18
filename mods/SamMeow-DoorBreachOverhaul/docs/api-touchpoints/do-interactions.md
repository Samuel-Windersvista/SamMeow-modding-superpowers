# DO 交互侧触点核验（SPT 4.1.6 / EFT 0.16.9.40743）

> 工单 02 产物（03/04 并入前的技术闸门）。对象：`external/references/door-overhaul`（v1.0.0）。
> **结论：全部 [OK]，无 [CHANGED]/[MISSING]。**

## 1. 补丁目标（8 个）

| # | 目标 | 4.1.6 实装签名 | 状态 | 证据 |
|---|------|----------------|------|------|
| 1 | `InteractionContextHelper.GetAvailableActions(GamePlayerOwner, Door)`（AddDoorActionsPatch） | 存在（同类约 20 重载，需显式签名选择） | [OK] | 反编译 |
| 2 | `InputKeyCombination.UpdateCommand(float)`（InteractKeyCapturePatch） | `public override ECommand UpdateCommand(float deltaTime)`（`EFT.InputSystem`） | [OK] | 反编译 |
| 3 | `WorldInteractiveObject.InitializeSmoothOpenInteraction(EDoorState, bool)`（PartialOpenSwingPatch） | `public virtual void InitializeSmoothOpenInteraction(EDoorState state, bool confirmed = false)` | [OK] | 反编译 |
| 4 | `WorldInteractiveObject.PlaySound(EDoorState, float)`（QuietOpenSoundPatch） | `public void PlaySound(EDoorState state, float volume = 1f)` | [OK] | 反编译 |
| 5 | `Player.StateIsSuitableForHandInput` 属性 getter（KickWeaponReadyPatch） | `public bool StateIsSuitableForHandInput => ...`（expression-bodied 属性） | [OK] | 反编译 |
| 6 | `PlayerBones.ShiftWeaponRoot`（KickWeaponForwardPatch） | 全局类 `PlayerBones`：`public void ShiftWeaponRoot(float t, EPointOfView pv, float thirdPersonAuthority, bool armsupdated = false, ...)`（12 参、单方法；postfix 按名注入 `__instance` + `pv` 合法） | [OK] | 反编译 |
| 7 | `GameWorld.OnDestroy()`（RaidEndCleanupPatch） | `public virtual void OnDestroy()` | [OK] | 反编译 |
| 8 | `EPlayerState.BreachDoor`（踹门状态判定） | 全局枚举 `EPlayerState` 含 `BreachDoor` | [OK] | 反编译 |

## 2. 成员/字段

| 成员 | 4.1.6 实装 | 状态 | 证据 |
|------|------------|------|------|
| `InputKeyCombination.GameKey` | `public readonly EGameKey GameKey;` | [OK] | 反编译 |
| `InputKeyCombination.InputKeys` | 存在（`IGameKey[]`；`KeyCombination.InputKeys` 使用点） | [OK] | 反编译 + 官方 4.1.6 构建产物 |
| `InputKeyCombination` 继承链 | `InputKeyCombination : InputCombination`（`abstract ECommand UpdateCommand(float)` 为基类抽象方法） | [OK] | 反编译 |
| `EGameKey.Interact` | 存在（`InputKeyCombination(EGameKey gameKey, ECommand command, int priority = -1)`） | [OK] | 反编译 |
| `WorldInteractiveObject.OpenAngle` | `public float OpenAngle = 60f;` | [OK] | 反编译 |
| `WorldInteractiveObject._interaction`（`____interaction` 注入） | `protected InteractionState _interaction`；`ProgressCurve`（AnimationCurve 可读写）/ `Duration`（可写） | [OK] | 反编译 |

## 3. 注记

- `ShiftWeaponRoot` 在 4.1.6 为 12 参单方法（DO 源码只按方法名取目标，无二义风险）；postfix 仅注入 `__instance` 与 `pv`，不受其余参数变化影响。
- `UpdateCommand` 为 `InputCombination` 的抽象方法实现；`InteractKeyCapturePatch` 的 postfix 仅读 `GameKey` / `InputKeys`，签名稳定。
