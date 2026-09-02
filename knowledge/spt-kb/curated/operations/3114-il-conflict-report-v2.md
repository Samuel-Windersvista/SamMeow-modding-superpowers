---
version: [3.11]
domain: client
topic: conflict-analysis
source: curated
---
# 3114 整合包 IL 级 Harmony patch 冲突深度报告（行为分析版，2026-08-05）

> 状态：已验证（IL 反编译管线 v2：Mono.Cecil 读 108 启用 client DLL，106 成功，776 patch + IL 行为特征分析）
> 用途：诺文斯克 0.3.2 整合包的客户端 Harmony patch 冲突全景，含 patch 行为特征（截断/写字段/只读）+ 危险分级 + 规则盲区说明
> 相关：`3114-il-conflict-report.md`（v1 表层版，仅同目标碰撞）

## 扫描概览

| 指标 | 值 |
|------|-----|
| 启用 client DLL | 108 |
| 成功扫描 | 106 |
| Harmony patch 总数 | 776 |
| 唯一 patch 目标 | 566 |
| 跨插件碰撞（2+ 插件同目标） | 62 |
| **含行为特征的碰撞** | 62 |

## patch 行为分布（全量 776）

| 行为 | 数量 | 含义 |
|------|------|------|
| `returnsFalse` | 216 | Prefix 返回 false（截断原方法执行） |
| `isReadOnly` | 621 | 只读观察（不改行为，日志/计数） |
| `writesField` | 154 | 修改字段（stfld） |
| `callsOriginal` | 67 | 调用原方法（base./__original） |
| `writesResult` | 1 | 改返回值（__result） |

## 危险分级（启发式）

| 级别 | 规则 | 数量 | 说明 |
|------|------|------|------|
| **HIGH** | `prefixRetFalse >= 2`（多插件争相截断同一方法） | 9 | 多个 mod 都想让原方法**不执行**，先执行者截断后，后续 patch 的修改可能失效 |
| **MEDIUM** | `writesField >= 2` 或 `writesResult >= 2` | 6 | 多插件改同一字段/结果，可能互相覆盖 |
| **LOW** | 其他 | 47 | 事件订阅/只读，可共存 |

## HIGH 高危碰撞（9 个，prefixRetFalse >= 2）

> **高危语义**：多个插件对同一方法都有 `Prefix 返回 false` 的 patch（意图截断原方法）。
> Harmony 2 的链式语义：所有 Prefix 按序执行，**任一返回 false 则跳过原方法**——
> 后执行的 Prefix 仍跑，但原方法已不执行，**后 patch 的修改可能基于"原方法会跑"的假设而失效**。
> 这是"能跑但行为不符合预期"的真实隐患区。

| 目标 | 插件 | prefixRetFalse | 解读 |
|------|------|----------------|------|
| `EFT.Player/FirearmController._player` | AccessibilityIndicators, FOVFix, TarkovIRL, RealismMod, WTT-Armory | **13** | 武器手感核心，5 个 mod 共 13 个截断 patch 争抢——最危险 |
| `GetActionsClass` | UIFixes, LockableDoors, LeaveItThere, RealismMod, InteractableExfilsAPI | 3 | 门交互（与 4.1 重构同区域） |
| `EFT.MovementContext._player` | Skills Extended, RealismMod | 8 | 移动上下文，2 mod 各 8 个截断 patch |
| `BotTalk.Say` | FriendlyFireTeam-SAINAAddon, friendlyPMC | 2 | AI 对话 |
| `BotsGroup.AddEnemy` | FriendlyFireTeam-SAINAAddon, friendlyPMC | 2 | AI 敌意管理 |
| `EFT.NonWavesSpawnScenario.Update` | BotPlacementSystem, RealismMod | 2 | bot 生成逻辑 |
| `EFT.MovementState.MovementContext` | TarkovIRL, RealismMod | 3 | 移动状态 |
| `EFT.UI.DragAndDrop.GeneratedGridsView.Show` | Raid Overhaul, GildedKeyStorage | 2 | UI 容器显示 |
| 未知目标 `?\|?` | ContinuousLoadAmmo, Endurance, RealismMod | 3 | 目标解析失败（3 个截断 patch，目标未识别） |

## MEDIUM 中危碰撞（6 个，writesField/writesResult 2+）

| 目标 | 插件 | 解读 |
|------|------|------|
| `EFT.GameWorld.OnGameStarted` | 24 个插件 | **全部 isReadOnly/事件订阅，无截断竞争**——实际安全，只是启动事件订阅多 |
| `EFT.UI.ContainersPanel.Show` | AutoDeposit, BeltSlot | UI 容器显示，2 mod 写字段 |
| `EFT.UI.MenuScreen.Show` | MenuOverhaul, SkillMultiplier | 菜单显示 |
| `GetActionsClass.smethod_14` | BackdoorBandit, Raid Overhaul | 门交互另一目标 |
| `System.String.Name` | SearchOpenContainers, UseLooseLoot | 字符串名 |
| `EFT.Player.ApplyDamageInfo` | Headshot Damage Redirection, RealismMod | 伤害应用，2 mod 改字段 |

## 规则盲区（诚实声明）

当前危险分级是**启发式**，存在以下盲区，可能高估或低估：

1. **prefixRetFalse 检测浅**：只识别 `ldc.i4.0 + ret` 模式，**条件返回 false**（`if (x) return false`）可能漏判——实际截断意图可能更多
2. **阈值 2 是任意的**：2 个截断 patch 可能是"各截断不同子行为"（不冲突），也可能"争相改同一行为"（真冲突）——规则不区分意图
3. **不读 patch 实际做什么**：只知道"想截断"，不知道"截断是为了什么"——两 mod 都想截断 _player 可能是替换武器手感（冲突），也可能是拦截不同触发条件（不冲突）
4. **callsOtherPatch = 0 全部**：检测"调用别的 Harmony patch"的逻辑（找 HarmonyLib.Harmony 调用）在 patch 方法体内找不到——patch 是从插件 `Awake()` 调用 `new PatchClass().Enable()` 应用的，不是 patch 内互相调用。这个特征无用
5. **isReadOnly 分类粗**：621/776 判定为只读，但很多改行为的 patch 调复杂方法（内部改状态），不直接 stfld/starg，仍被判为只读——**实际改行为的 patch 可能更多**

## 建议的验证方法

对高危碰撞，**人工/源码级复核**每个 patch 的 `GetTargetMethod()` 目标是否语义相同：
- 若两 mod 的 patch 都 patch 同一**具体方法**（如同一个 `FirearmController._player` 的同一重载）→ 真冲突
- 若 patch 的是**同一方法的不同重载/不同触发条件** → 可能不冲突

## 工具

- IL helper：`tools/spt-mcp/il-helper/`（Mono.Cecil，提取 patch 目标 + IL 行为特征）
- 结果数据：`D:/Temp/opencode/3114-il-scan-v2.json`（全量含 behavior）、`D:/Temp/opencode/3114-il-collisions-v2.json`（62 碰撞）

## 关联

- `3114-il-conflict-report.md`（v1 表层版，62 碰撞同目标列表）
- `curated/operations/conflict-analysis-input-model.md`（modlist 过滤 + 嵌套路径规则）
- `docs/wayfinder/findings/001-spt-conflict-taxonomy.md` § 2.2 Harmony 补丁冲突形态
