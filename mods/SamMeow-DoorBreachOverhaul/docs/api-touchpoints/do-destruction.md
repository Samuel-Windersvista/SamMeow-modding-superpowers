# DO 破坏侧触点核验（SPT 4.1.6 / EFT 0.16.9.40743）

> 工单 02 产物（03 并入前的技术闸门）。对象：`external/references/door-overhaul`（v1.0.0，BSD-3，Sage；包内 DLL 构建于 2026-10-13，meta `version=d2026.10.13.0`）。
> **结论：补丁目标全部 [OK]，无 [CHANGED]/[MISSING]。** DO 源码 v1.0.0 的官方 4.1.6 构建产物（`DoorOverhaul.dll`，131072 B）为本表提供源级编译背书；合并编译在工单 03 首次执行（最终闸门）。

## 1. 补丁目标（5 个）

| # | 目标 | 4.1.6 实装签名 | 状态 | 证据 |
|---|------|----------------|------|------|
| 1 | `Door.KickOpen(Vector3, bool)`（DestructionBreachPatch） | `public void KickOpen(Vector3 yourPosition, bool confirmed = false)` | [OK] | 反编译 |
| 2 | `Grenade.Explosion`（DestructionGrenadePatch） | `public static void Explosion(Grenade grenade, IExplosiveItem grenadeItem, Vector3 grenadePosition, string playerProfileIDWhoThrew, IBallisticsCalculator grenadeBallisticsCalculator, Item originalWeaponItem, Vector3 shift, bool isPlanted = false)`（静态 8 参，首参 `Grenade`） | [OK] | 反编译 |
| 3 | `Door.CheckOcclusionPortal()`（DestructionPortalPatch） | `public void CheckOcclusionPortal()` | [OK] | 反编译 |
| 4 | `GameWorld.OnGameStarted()`（DestructionRaidStartPatch） | `public virtual void OnGameStarted()` | [OK] | 反编译 |
| 5 | 守卫三补丁（DestructionOwnershipPatches） | `public override void Door.Interact(InteractionResult)`；`public void SetDoorState(EDoorState state, bool force = false)`；`KickOpen` 同上 | [OK] | 反编译 |

## 2. 破坏系统依赖的高风险私有成员

| 成员 | 4.1.6 实装 | 状态 | 证据 |
|------|------------|------|------|
| `Systems.Effects.Effects._dict`（DestructionFragments） | `private Dictionary<MaterialType, Effect> _dict;`（`AccessTools.FieldRefAccess` 可读私有字段） | [OK] | 反编译 |
| `WorldInteractiveObject.OpenAngle` | `public float OpenAngle = 60f;` | [OK] | 反编译 |
| `WorldInteractiveObject._interaction` | `protected InteractionState _interaction = new InteractionState();`（Harmony 字段注入可命中） | [OK] | 反编译 |
| `InteractionState.ProgressCurve` / `Duration` | 公共属性（`ProgressCurve` getter 回退 `EFTHardSettings.Instance.DoorCurve`；`Duration` 可写） | [OK] | 反编译 |
| 门体几何/材质（Mesh / Renderer / Material / shader 读写） | Unity 标准 API | [OK] | 官方 4.1.6 构建产物背书；03 合并编译复核 |

## 3. 合并期注意

- 破坏系统内部（FractureState / LeafVisuals / MeshReadback / DoorTakeover 等）大量访问门体网格与 EFT 内部状态；本表按「补丁目标 + 高风险私有成员」核验。完整编译兼容以 03 合并构建为最终闸门（`dotnet build` 失败即阻断并入）。
- 上锁门豁免点：`Destruction/DoorDamageIntake.cs:158/218`（03 移除）。
- 包内 `[8]门的大修-DoorOverhaul` 覆盖层截至 2026-10-18 为**禁用**状态（Overseer 侧 modlist 调整）；不影响本源级审计，03 以源码快照为并入基准。
