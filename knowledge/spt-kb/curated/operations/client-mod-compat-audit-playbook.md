---
version: [3.11]
domain: client
topic: conflict-analysis
source: curated
---
# 客户端 mod Harmony 冲突审计 playbook（9 仓源码实证，2026-08-17）

> 状态：已验证（对工作区 9 个 mod 源码仓全量审计，与 PerformanceTweaks 13 个补丁目标交叉比对）
> 前置：`3114-il-conflict-report.md`（IL 级扫描方法学：776 patch/62 碰撞）。本记录是有源码时的升级版流程。

## 审计流程（有源码时）

1. **全量 patch 目标提取（双通道 grep，缺一不可）**：
   - 通道 1：`AccessTools.Method(typeof(X), ...)` / `AccessTools.Constructor` / `AccessTools.PropertyGetter`
   - 通道 2：字符串字面量 `"MethodName"`（防动态定位/字符串查找绕过 typeof）
   - 形态判定：读 GetTargetMethod 周边，区分 Prefix / Postfix / **Transpiler**（唯一真危险形态）
2. **交叉比对**：与己方补丁目标表逐一对表；同目标时必读双方 patch 体判断语义。
3. **语义判定规则**（实证）：
   - 双 Prefix 链式共存：条件不相交即安全；**顺序敏感**（一方 return false 会跳过后续 prefix）→ 用 `HarmonyMethod(method, Priority.Low)` 让己方后执行即可消除
   - 双 Postfix：几乎总是安全（都读实例不写返回）
   - Transpiler 叠加：**禁止**；己方原则=全程 Prefix/Postfix
   - 整方法替换型 Prefix（return false 全替换，如 Realism 的 CreateShot、ABPS 的 NonWavesSpawnScenario.Update）：域相邻时需评估生命周期接管

## 9 仓审计实证事实（SPT 3.11.4 生态，可复用）

| 仓库 | GUID | 关键事实 |
|---|---|---|
| Moew-SAIN-For-3114 | `me.sol.sain` | SAIN **不是**整体替换感知链：只 patch 参数级方法（EnemyInfo.method_1/7/8、ShallKnowEnemy、LookSensor.Activate/method_2、BotMover.Sprint/ManualUpdate 等）；**不碰** CheckLookEnemy/UpdateLook/CalcPath/AddEnemy 语义（其 AddEnemy patch 仅 null 守卫）。"SAIN 接管一切感知"的直觉判断被证伪 |
| SamMeow-QuestingBots | `com.danw.questingbots` | patch `EnemyInfo.CheckLookEnemy`（睡眠 bot SetVisible(false) prefix，顺序敏感点）；**硬性不兼容 AIDisabler/AILimit/Phobos**（静态 GUID 黑名单）；自带 AI Limiter；与 LootingBots 经 `LootingBots.External` 反射互操作（配对设计）；脑层优先级 QB 18/19/26/99 vs LB 4/5/11/13 分层互斥 |
| Moew-LootingBot-For-3114 | `me.skwizzy.lootingbots` | 仅 5 个 patch（BotOwner.Dispose/Deactivate、GameWorld.Dispose、LocalGame.Stop、BotDifficultySettingsClass.ApplyPresetLocation）；依赖 BigBrain≥1.3.2（QB 要 ≥1.4.0，取高） |
| LIN-Realism-Mod-Client | `RealismMod` | 194 patch 类；弹道域用 **prefix+return false 全替换** `BallisticsCalculator.CreateShot`（自行 EftBulletClass.Create）——动弹道生命周期时必须知道它已接管；AI 感知域完全空白 |
| SamMeow-ThatsLit-For-3114 | `bastudio.thatslit` | 视距补偿走 `EnemyInfo.CheckPartLineOfSight` 的 **ref addSensorDistance**，**不改 VisibleDist 本身**；基于 VisibleDist 的裁剪类补丁（如距离修剪）会抵消其远距光照补偿——同装时裁剪阈值应放宽（1.5x→2.0+） |
| SamMeow-SWAG+Donuts | `com.dvize.Donuts` | 局末 `BaseLocalGame.smethod_4` 整方法替换：**绕过 GameWorld.UnregisterPlayer**——依赖 UnregisterPlayer 做清理的 mod 必须有全量重建兜底；注意该仓 Donuts 1.4.4 面向 SPT 3.8，与 3.11.4 不匹配 |
| Moew-ProgressiveBotSystem | 纯 server | 零 client patch；loadAfter SPTQuestingBots |
| Moew-botplacementsystem | `com.acidphantasm.botplacementsystem` | patch `GameWorld.UnregisterPlayer` postfix（仅 Boss 追踪回传，与索引维护类 postfix 正交）；其 despawn 主动调 UnregisterPlayer；`NonWavesSpawnScenario.Update` 整方法替换且每帧全量扫描——本身是潜在帧热点 |
| spt-singleplayer（官方） | spt 自带 | `Patches.Performance.RemoveStopwatchAllocationsEveryBotFramePatch`：**Transpiler NOP BotOwner.UpdateManual 固定 IL 索引（12-18/110-112）**——对该方法只能加 Prefix/Postfix，且 EFT 版本升级会先坏它 |

## spt-core/spt-singleplayer 官方 patch 目标速查（3.11.4，40 个方法）

反编译 `BepInEx/plugins/spt/*.dll` 提取（缓存在 `external/decompile-cache/spt-3.11.4-plugins/`）。
与性能/AI 相关的仅 2 个：BotOwner.UpdateManual（上述 transpiler）、CoverPointMaster.method_0。
其余为 RaidFix/ScavMode/离线化（GameWorld.OnGameStarted、Player.Init/OnMakingShot/ApplyDamageInfo、
BotsController.SetSettings 等）。**新 client mod 打补丁前先查此表确认非官方目标。**

## 关联

- IL 级（无源码）扫描：`tools/spt-mcp/il-helper/` + `3114-il-conflict-report.md`
- 本次审计对象与结论全文：`docs/eft-0.16-性能分析与优化mod可行性报告.md` §7.3
