---
version: [3.11]
domain: client
topic: conflict-analysis
source: curated
---
# 3114 整合包 IL 级冲突深度报告 v3（源码级定论 + 规则修正，2026-08-05）

> 状态：已验证（IL 反编译管线 v3：Mono.Cecil 修正目标提取（字段名不误当方法名）+ 修正 returnsFalse 仅 Prefix 统计）
> 用途：诺文斯克 0.3.2 整合包的客户端 Harmony patch 冲突全景，源码级逐 patch 定论
> 相关：`3114-il-conflict-report-v2.md`（v2，含规则盲区）、`3114-il-conflict-report.md`（v1 表层）

## 修正说明（v2 -> v3）

v2 报告有两个关键 bug，已修正：

1. **目标提取 bug**：`AccessTools.Field(X, "_player")` 的**字段名**被误当成**目标方法名**——
   导致"FirearmController._player 13 插件碰撞"误报。实际这些 patch 的**目标方法各不相同**
   （Look / LerpCamera / Shoot / method_61 / CalculateScaleValueByFov），
   只是都用 `AccessTools.Field(FirearmController, "_player")` 读玩家状态（辅助字段）。
2. **returnsFalse 误报**：Postfix 方法里的提前 `return`（void ret）被误算成 "Prefix 截断原方法"——
   AccessibilityIndicators 的 Postfix 只读 patch 被误算进截断竞争。

修正后规则：
- **高危 = 同一个具体目标方法（targetType + targetMethod）被 2+ 插件的 Prefix 返回 false 截断**
- 同字段访问但目标方法不同 → **不是冲突**（各管各的方法）
- Postfix / 不截断的 Prefix → **不冲突**（顺序执行可共存）

## 修正后碰撞全景（72 个跨插件碰撞）

| 级别 | 数量 | 规则 |
|------|------|------|
| HIGH（同方法 prefixRetFalse 截断竞争） | 7 | 同一目标方法被 2+ 插件截断 |
| MEDIUM（同方法 writesField/writesResult 2+） | 8 | 同一目标方法被 2+ 插件改字段/结果 |
| LOW（其他，事件订阅/只读/Postfix） | 57 | 可共存 |

## HIGH 高危碰撞（7 个，源码级定论）

### 1. `GetActionsClass`（5 插件，3 个 prefixRetFalse）

**涉及**：UIFixes / LockableDoors / LeaveItThere / RealismMod / InteractableExfilsAPI

**解读**：门交互重构区域（与刚完成的 4.1 门交互重构同区域）。5 个 mod 都改门交互菜单，
其中 3 个有截断意图的 Prefix patch。这是**与本次迁移直接相关的区域**，值得复核：
- UIFixes 的 `GetActionsClass` patch 是重构菜单构建逻辑
- LockableDoors/LeaveItThere 添加自定义门动作
- RealismMod/InteractableExfilsAPI 拦截交互流程

**真冲突点**：多个 mod 想往门菜单添加自定义动作 + 截断菜单构建——**谁后 patch 谁赢**，
前面 mod 的自定义动作可能被覆盖。但**各 mod 的动作名不同**（Pick lock / Inspect / Hack 等），
实际可能是"添加不同动作共存"而非"覆盖同一动作"。

### 2. `EFT.MovementContext.ClampSpeed`（2 插件，2 个 prefixRetFalse）

**涉及**：Skills Extended / RealismMod

**解读**：移动速度钳制。Skills Extended 的 FirstAid/Strength buff 改速度，RealismMod 的重量/体力系统也改速度。
**两 mod 都想改同一移动速度计算**——**真冲突**：谁后 patch 谁的速度模型生效，前面的被覆盖。
这是"能跑但速度计算可能不符合预期"的典型。

### 3. `BotTalk.Say` / `BotsGroup.AddEnemy`（friendlyPMC 系，各 2 个 prefixRetFalse）

**涉及**：FriendlyFireTeam-SAINAAddon / friendlyPMC

**解读**：同一作者系的两个 AI mod（friendlyPMC 本体 + 它的 SAIN 插件版）都截断 AI 对话/敌意管理。
**同一功能的两个版本共存**——重复实现，一个覆盖另一个。整合包里有"friendlypmc与PITFireTeam合并版本"
（启用）和"friendlypmc - 已AI优化"（禁用）——**实际只有合并版在跑**，但 IL 扫描把两个都扫了
（因为它们都在 MO2 里，只是 modlist 决定启用）。

### 4. `EFT.NonWavesSpawnScenario.Update`（2 插件，2 个 prefixRetFalse）

**涉及**：BotPlacementSystem / RealismMod

**解读**：bot 生成逻辑。BotPlacementSystem 重写生成调度，RealismMod 的 bot 数量/难度系统也想改。
**真冲突**：两 mod 都想接管 bot 生成，谁后 patch 谁赢。

### 5. `EFT.UI.DragAndDrop.GeneratedGridsView`（2 插件，2 个 prefixRetFalse）

**涉及**：Raid Overhaul / GildedKeyStorage

**解读**：UI 容器网格显示。两 mod 都想改背包/容器 UI 的网格逻辑。

### 6. 目标解析失败（3 个 prefixRetFalse，3 插件）

**涉及**：ContinuousLoadAmmo / Endurance / RealismMod

**解读**：IL 提取目标失败（混淆名 `method_X` 或复杂 AccessTools 模式）。需人工复核源码定位。

## MEDIUM 中危碰撞（8 个，节选关键）

### `EFT.Player.Look`（FOVFix + TarkovIRL）—— 源码级分析

| Mod | Patch | 类型 | 意图 |
|-----|-------|------|------|
| FOVFix | FreeLookPatch | **Prefix 返回 false** | **完全重写** Look 方法（自定义视角逻辑替代原实现） |
| TarkovIRL | Patch_Look | Postfix | 补充调整 HeadRotation（PrimeMover 小幅度效果） |

**解读**：FOVFix 用 Prefix 返回 false **完全接管** Look 方法；TarkovIRL 的 Postfix 在其后执行，
**基于 FOVFix 修改后的状态继续调整**。不是"同方法直接覆盖"，而是**FOVFix 的重写改变了 TarkovIRL 的运行前提**
（TarkovIRL 假设原 Look 执行了）。**行为可能互相影响**，不是直接冲突。

### `EFT.Player/FirearmController.UpdateSwayFactors`（TarkovIRL + RealismMod）

**解读**：武器摇摆因子更新。TarkovIRL 补充 UpdateSwayFactors，RealismMod 的武器摇摆系统也改。
两 mod 都改武器摇摆——**行为可能叠加/干扰**。

### `EFT.GameWorld.OnGameStarted`（24 插件，全部 Postfix/只读）

**解读**：事件订阅（启动回调），**全部 Postfix 或只读，无截断竞争**——**安全**，
只是启动时 24 个回调依次执行（性能敏感，异常连锁风险）。

## FirearmController._player 碰撞的源码级定论（v2 误报修正）

v2 报告的 `FirearmController._player` 13 插件"高危碰撞"**是误报**。修正后源码级定论：

| Mod | Patch | 类型 | 目标方法 | 冲突？ |
|-----|-------|------|---------|--------|
| FOVFix | FreeLookPatch | Prefix 返回 false | `Player.Look` | 与 TarkovIRL 同方法，行为互影响 |
| FOVFix | LerpCameraPatch | Prefix 返回 false | `ProceduralWeaponAnimation.LerpCamera` | 独立 |
| FOVFix | CalculateScaleValueByFovPatch | Prefix 条件返回 false | `Player.CalculateScaleValueByFov` | 独立 |
| RealismMod | FireratePitchPatch | Prefix 返回 false | `FirearmController.method_61`（射速） | 独立 |
| TarkovIRL | Patch_LerpCamera_ForceUpdateSway | Postfix | `ProceduralWeaponAnimation.LerpCamera` | 不冲突（补充） |
| WTT-Armory | ShootPatch | Prefix+Postfix（不截断） | `ProceduralWeaponAnimation.Shoot` | 不冲突（改 AN94 后坐力） |
| WTT-Armory | UpdateWeaponVariablesPatch | Postfix | `ProceduralWeaponAnimation.UpdateWeaponVariables` | 不冲突（只读） |
| AccessibilityIndicators | FirearmControllerPatch | Postfix | `FirearmController.InitiateShot` | 不冲突（只读 UI 指示） |

**结论**：这些 mod 都读 `FirearmController._player` 字段（辅助），但**目标方法各不相同**——
**没有"同一方法被多插件截断"的直接冲突**。真正的问题是 FOVFix 和 TarkovIRL 都 patch `Player.Look`
（行为互影响），以及 RealismMod 的武器逻辑重写在特定场景可能与 FOVFix 的视角重写叠加。

## 规则修正总结（v3 定稿）

```
正确规则：
  HIGH = 同一个 (targetType + targetMethod) 被 2+ 插件的 Prefix 返回 false 截断
  （必须目标方法完全相同，不是同字段访问）

错误规则（已废弃）：
  HIGH = 同字段被 2+ 插件访问（字段只是辅助读取，不是 patch 目标）
  HIGH = 同目标被 2+ 插件 patch（含 Postfix，Postfix 不截断可共存）
  HIGH = prefixRetFalse >= 2（Postfix 的 return 误算进截断）
```

## 工具

- IL helper：`tools/spt-mcp/il-helper/`（Mono.Cecil，修正后：字段名不误当方法名 + returnsFalse 仅 Prefix）
- 数据：`D:/Temp/opencode/3114-il-scan-v2.json`（修正后全量）、`D:/Temp/opencode/3114-il-collisions-v2.json`（72 碰撞）
- 源码定位：FOVFix（Fontaine-s-FOV-Fix_701_source）、TarkovIRL（SamMeow-TarkovIRL）、RealismMod（LIN-Realism-Mod-Client）、WTT-Armory-CLIENT（WelcomeToThursday/WTT-Armory-CLIENT-）、AccessibilityIndicators（Audio-Accessibility-Indicators_1760_source）

## 关联

- `3114-il-conflict-report-v2.md`（v2，含本次修正的两个 bug 记录）
- `curated/operations/conflict-analysis-input-model.md`（modlist 过滤 + 嵌套路径规则）
- `docs/wayfinder/findings/001-spt-conflict-taxonomy.md` § 2.2 Harmony 补丁冲突形态
