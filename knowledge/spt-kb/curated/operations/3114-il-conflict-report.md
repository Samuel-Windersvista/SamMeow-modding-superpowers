---
version: [3.11]
domain: client
topic: conflict-analysis
source: curated
---
# 3114 整合包 IL 级 Harmony patch 冲突报告（2026-08-05）

> 状态：已验证（IL 反编译管线首跑：Mono.Cecil 读 108 启用 client DLL，106 成功，776 patch，566 唯一目标）
> 用途：诺文斯克 0.3.2 整合包的客户端 Harmony patch 冲突全景，整合包作者此前不可见的 IL 层冲突面

## 扫描概览

| 指标 | 值 |
|------|-----|
| 启用 client DLL | 108 |
| 成功扫描 | 106（2 失败：依赖解析/文件损坏） |
| Harmony patch 总数 | 776 |
| 唯一 patch 目标 | 566 |
| 跨插件 patch 碰撞（2+ 插件同目标） | **62** |

## 高碰撞目标（5+ 插件，3 个）

### 1. `EFT.GameWorld.OnGameStarted`（24 个插件）

几乎一半 mod 都在监听游戏启动事件。本身是事件订阅模式（可共存），但：
- **启动性能敏感**：24 个 patch 依次执行，任一慢 patch 拖慢启动
- **异常连锁**：任一 patch 抛异常可能阻断后续 patch（SPT 官方插件内一个 patch 抛异常 → 后续全部不加载）

参与插件：Janky's Visual Assist, Radar, DynamicMaps, acidphantasm-AccessibilityIndicators, MoxoPixel-MenuOverhaul, Jehree.ImmersiveDaylightCycle, BossNotifier, SamSWAT's HeliCrash: Arys Reloaded, dvize.BackdoorBandit, DrakiaXYZ-LootRadius, DrakiaXYZ-DoorRandomizer, Skills Extended, SPTBattleAmbience, Janky's HollywoodFX, Janky's HollywoodGraphics, LockableDoors, DrakiaXYZ-Waypoints, HomeComforts, RealismMod, InteractableExfilsAPI, Path To Tarkov, Raid Overhaul, WTT-Armory, VCQL-Zones

### 2. `EFT.Player/FirearmController._player`（5 个插件）

武器手感相关，**真正行为冲突**：多个 Transpiler/Prefix 按序叠加，后 patch 者看到前 patch 者的输出 IL，**最后一个 patch 实际生效**，前面被部分遮蔽。

参与插件：acidphantasm-AccessibilityIndicators, FOVFix, TarkovIRL - WHM, RealismMod, WTT-Armory

### 3. `GetActionsClass`（门交互，5 个插件）

**与刚完成的门交互重构直接相关**——5 个 mod 都在改门交互菜单。

参与插件：Tyfon.UIFixes, LockableDoors, LeaveItThere, RealismMod, InteractableExfilsAPI

## 中碰撞目标（2-4 插件，59 个，节选高价值）

| 目标 | 插件数 | 参与插件 |
|------|--------|---------|
| `EFT.Player.OnDead` | 4 | Visual Assist, DynamicMaps, HollywoodFX, Raid Overhaul |
| `EFT.GameWorld.UnregisterPlayer` | 4 | DynamicMaps, AccessibilityIndicators, Volcano-Subtitle, BotPlacementSystem |
| `EFT.Player.OnMakingShot` | 3 | Radar, FriendlyFireTeam-SAINAAddon, TarkovIRL |
| `EFT.GameWorld.RegisterPlayer` | 3 | HandsAreNotBusy, ContinuousLoadAmmo, Volcano-Subtitle |
| `GetActionsClass.smethod_9` | 2 | SPTCorpseCleaner, Skills Extended（**KeycardDoor 交互重构同目标**） |
| `EFT.UI.TraderScreensGroup.Show` | 2 | Kaeno-TraderScrolling, Path To Tarkov |
| `EFT.UI.TasksScreen.Show` | 2 | TaskListFixes, ContinuousLoadAmmo |

## 分析解读

### 为什么整合包能跑？

1. **事件订阅型碰撞（OnGameStarted 等）** 是**设计允许**的——Harmony 2 链式执行，Prefix 按序跑，任一返回 false 才截断。24 个 patch 共存是常态。
2. **真正危险的是 Transpiler 叠加**（修改 IL 的 patch）——FirearmController/_player 这种，**最后一个 patch 的 IL 是最终生效**，前面 patch 的修改可能被覆盖或失效。
3. **整合包作者的隐式优先级管理**（MO2 mod 顺序）在多数碰撞上恰好把工作做好了——但**这是不可见的**，如果哪天换顺序可能突然坏。

### 潜在风险点（值得整合包作者关注）

1. **`FirearmController._player` 5 插件**：FOVFix/TarkovIRL/RealismMod/WTT-Armory/AccessibilityIndicators 都改武器手感——**实际只有最后一个生效**，其余 mod 的武器手感修改可能被静默忽略
2. **`GetActionsClass` 5 插件**：门交互重构刚完成（4.1），5 个 mod 的 patch 顺序决定哪些门交互动作可见——**这是本次迁移刚处理的区域，值得复核**
3. **`OnGameStarted` 24 插件**：启动慢/异常连锁风险——如果哪天整合包启动异常，这里是首要排查点

## 工具与实现

- **IL helper**：`tools/spt-mcp/il-helper/`（Mono.Cecil 0.11.6，读 DLL 提取 BepInPlugin + ModulePatch 子类 + GetTargetMethod() IL 目标 + PatchPrefix/Postfix 特性）
- **扫描脚本**：`D:/Temp/opencode/il-scan-3114.js`（批量 108 DLL）
- **结果数据**：`D:/Temp/opencode/3114-il-scan.json`（全量）、`D:/Temp/opencode/3114-il-collisions.json`（62 碰撞）

## 关联

- `docs/wayfinder/findings/001-spt-conflict-taxonomy.md` § 2.2 Harmony 补丁冲突形态（本报告为其真实验证）
- `knowledge/spt-kb/curated/operations/conflict-analysis-input-model.md` — modlist 过滤 + 嵌套路径规则（本扫描复用同模型）
- wayfinder "Not yet specified" #1: IL decompilation pipeline（本报告证明可行，已完成基础版）
