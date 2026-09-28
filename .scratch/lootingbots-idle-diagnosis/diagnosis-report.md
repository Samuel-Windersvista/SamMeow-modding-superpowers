# SPT 3.11.4 Novinsk Edition — bot 非战发呆 / 不搜刮 / 不游荡 诊断报告

- 日期：2026-09-19
- 诊断对象：SPT 3.11.4 整合包（Novinsk Edition，MO2 profile：单机运行整合包）
- 证据范围：运行日志单场 raid 全量实证 + 三处源码库 + 部署生态 mod 反编译 + 本仓库 SPT 3.11 知识库
- 判定方式：每条结论附证据与置信度；[高]≥0.85 / [中]0.5-0.85 / [低]<0.5

---

## 0. 摘要（先读这段）

症状由**两类独立缺陷叠加**造成，**不是**三处特制化"互相冲突"：

1. **[高] boss 护卫永久僵死（运行时实证）**：vanilla 的 boss-follower 清理缺口（boss 死亡/处置后 `BossToFollow` 引用悬垂）在本整合包环境被触发。悬垂护卫每帧访问已处置 boss 的 `Mover` 抛 NRE，被 BigBrain（stock 版）逐帧记录并放行重抛，该 bot 本帧 AI 决策被彻底中断。单场 raid 44,790 次，覆盖整场、无空档 —— 这就是"护卫类 bot 原地发呆"的直接机制。
2. **[中高，已修订——见文末"修订记录"] 普通 bot 不游荡（修正机制）**：LB `LootingLayer` 因扫描窗口永续而永久激活（空手扫描后不重置 ScanTimer → 无限扫描循环），压制原生游荡层 `PatrolAssault`；原"移除和平层 → 无层驱动"表述经二次反编译否证。
3. **原始假设裁决**：SPT 源码特制化 → **不成立**（近 stock）；SAIN fork → **部分成立**（协同面偏窄、有设计缺陷，但本次日志显示其实际在工作）；LB fork → **成立（普通 bot 维度）**（移除和平层 + 版本滞后缺上游修复）；"三方互不联调产生冲突" → **不成立**（未发现互相破坏证据，属各自独立缺陷叠加）。

---

## 1. 症状与口径

- 用户报告：游戏内 LootingBots 对 bot 的影响异常 —— 非战斗状态 bot 原地发呆、很难触发 looting 状态、不会游荡。
- 用户确认范围：**所有地图、各类 bot 普遍如此；boss / 护卫类尤为明显**。
- 用户原始假设：SPT 3.11.4 源码 / Moew-LootingBots fork / Moew-SAIN fork 在不同时间各自特制化，未经联调，集成失效。
- 日志样本：单次游戏进程 + 单场 raid（Ground Zero，Boss Kollontay），`BepInEx\LogOutput.log` 363,894 行，99% 体量为同一异常刷屏。

---

## 2. 根因模型（按因果贡献排序）

| # | 根因 | 归属 | 置信度 | 作用范围 | 关键证据 |
|---|------|------|--------|----------|----------|
| R1 | **vanilla 悬垂 boss 缺口**：`BotFollower.Dispose` 仅在 `BossToFollow.IsAlive` 时清引用；boss 已死则提前 `return false`，`BossToFollow` 悬垂。悬垂护卫继续跑 `GClass545.Update → method_3` 访问 `BossToFollow.MoveSpeed`，而死 boss 的 `BotOwner.Mover` 已置 null → 每帧 NRE | vanilla 缺陷，本环境被触发/放大 | 0.90 | 仅 boss 护卫（KolonSec / Gluhar 系 / Tagilla 系）；普通 bot 不涉及 | 反编译逐行实证 + NRE 栈逐帧吻合 |
| R2 | **BigBrain 无节流 catch + `return true`**：每帧 LogError，原方法重跑再抛（被 AICoreController 静默吞）→ 该 bot 本帧 AI 决策中断 → 护卫永久僵死；日志被淹没（44,790 条 × 约 8 行 ≈ 全量 98%） | BigBrain stock 设计（非特制化引入） | 代码 0.95 / 行为后果 0.70 | 所有 NRE 源 bot | `BotAgentUpdatePatch.cs:62-66` + 日志全量统计 |
| R3【已修订——见文末"修订记录"】 | **（原表述，已否证）LB 移除原版和平层 → 扫描间隙无层驱动**；修正后机制 = LB 扫描窗口永续 → LootingLayer 永久激活压制 PatrolAssault | LB fork（回归：删 ScanScheduler + 900m 配置） | 机制 0.85 / 后果 0.6（待坐实） | **普通 bot 不游荡（修正机制）** | `LootFinder.cs:26-29`、`FindLootLogic.cs`、归档 1.6.1 `FindLootLogic.cs:25` |
| R4 | **LB fork 缺失上游修复**：`InteractContainer` 未设 `InteractingPlayer`（上游 1.6.3 修"SAIN 启用时搜刮容器后停摆"）；`ForceBrainEnabled` 只 set true 永不复位（上游 1.7 修） | LB fork 版本滞后 | 0.80 | 曾进入过搜刮的 bot 二次停摆 | 与上游 1.6.3/1.7 diff |
| R5 | **SAIN fork `IsAvailable` 单向闩锁**：7 方法任一 invoke 异常 → 整局静默失效；`TryPreventBotFromLooting(10s)` 仅在 ExtractLayer 激活时运行 | SAIN fork | 0.50 | 撤离-搜刮协同面；对发呆非主因 | `LootingBotsInterop.cs:132-162`；本次日志无 invoke failed → 闩锁未触发 |
| R6 | **friendlyPMC 清理缺口 4 处**（Init 无保护可抛→AddBotFollower catch→未注册且悬垂残留；Dismiss 提前 return；InitPlayer 不复位；延迟 onActivate 未执行） | friendlyPMC 合并版 | 缺口实证 0.85 / 对 NRE 贡献 0.30 | 其招募 follower 行为异常；非 NRE 源 | `pitAIBossPlayer.cs:326-340`、`BotFollowerPlayer.cs:582-660` |
| R7 | **SAIN fork PeacefulLayer 兴趣点探索死代码**（`RecordPoint` 无调用者）→ SAIN 侧无和平探索兜底（与 R3 叠加） | SAIN fork | 0.90 | 普通 bot | 全仓 grep 仅定义处 |

**"NRE 为何 raid 开始后立即出现且全程恒定"**：Kollontay 于行 4559 生成、4670 最后定位、4734 首条 NRE。boss 死亡帧起，其 KolonSec 护卫的 `BossToFollow` 悬垂，下一帧 AI 更新即抛；vanilla 中本应把护卫切出 bossPatrol 的路径位于 `Dispose()` 内，恰好被提前 return 跳过 → 护卫无法切出 → 每帧一条、从 4734 持续到关服前（363261），无空档。

---

## 3. 证据链

### 3.1 运行时日志（单会话 / 单 raid / Ground Zero）

- 插件加载：SAIN 4.4.0（113 patches）、LootingBots 1.6.1（5 patches）、BigBrain 1.3.2、friendlyPMC 4.4.12、FFT 1.3.0 + SAINAddon 1.4.0、BossNotifier 1.6.0；**QuestingBots 未安装**；Waypoints 1.7.0 正常。
- 异常统计：`Exception in Agent Update` NRE **44,790 条**（minGap=8 / median=8 / maxGap=55 行），首条 4734、末条 363261，恒定速率无空档。
- `MissingReferenceException` = 0（"组件销毁竞态"假说排除）。
- LootingBots 运行期仅 5 条日志（1 条 `[pmcBEAR] Bot3 stuck trying to reach`）；SAIN 仅 6 条（2 条枚举告警 + 4 条关服 Dispose NRE）；无 `Interrupted looting`。
- FFT/friendlyPMC 失败行全部集中在加载期（见 3.3），运行期无失败痕迹。

### 3.2 NRE 精确机理（反编译逐行实证）

调用链（自内向外）：

```
BotBoss.get_MoveSpeed()                      ← NullReferenceException
  GClass545.method_3(v, withSprint)          // CloseCover follower 的移动执行
  GClass545.Update()
  PatrolDataFollower.ManualUpdate()
  GClass241.UpdateNodeByBrain()              // followerPatrol 脑节点
  GClass169<T>.UpdateNodeByMain()
  DrakiaXYZ.BigBrain.Patches.BotAgentUpdatePatch.PatchPrefix()
```

- `BotBoss.MoveSpeed => botOwner_0.Mover.DestMoveSpeed`；`Mover` 仅在 `BotOwner.PreActivate()` 创建（BotOwner.cs:591）、仅在 `Dispose()` 置 null（:813）→ NRE 时 boss 的 BotOwner 已被处置。
- `GClass545.method_3` 取 `botOwner_0.BotFollower.BossToFollow.MoveSpeed`（GClass545.cs:108-117）。
- vanilla 清理缺口：`BotFollower.Dispose()`（BotFollower.cs:246-261）在 boss 已死时不清空 `BossToFollow`；`PatrolDataFollower.Dispose()`（:175-181）不复位 `IsInited`/`followerAIBase`。
- CloseCover follower 来自 `PatrolMode.bossCoverScouts`（GClass495.cs:138-165，Gluhar 系近卫）；`followerPatrol` 节点由 GClass522.cs:118-119 创建。
- BigBrain（stock）catch 后 `return true` → 原方法重跑并再次抛出 → 被 `AICoreController.Update` 静默吞掉 → **该 bot 本帧决策彻底中断**。

### 3.3 生态责任排查（BossNotifier / FFT / friendlyPMC）

- **BossNotifier 排除[高]**：`BotBossPatch` = `BotBoss` 构造函数 postfix，仅只读 Role/Position 写日志；DLL 无 `PatrolDataFollower` 字符串。
- **FFT 排除[中高]**：`BrainReplacePatch` 的目标（`BotFollowerPlayer.Activate`、`BossPlayers.SpawnGroupBots`）在 friendlyPMC 4.4.12 中不存在 → 静默跳过；FFT 全程不写 `BossToFollow`；其 BigBrain 层只读且带空判。SAINAddon 失败点=语音静音补丁未挂（无害）。
- **friendlyPMC[中]**：唯一写 `BossToFollow` 的 mod，4 条清理缺口（见 R6）。注意：其写入指向 `pitAIBossPlayer`（`MoveSpeed=0.7f` 常量，**不 NRE**）→ friendlyPMC 不是 NRE 源，但会制造"半接管/无人管理"的 follower 状态。
- 关键日志行：`Hooked BossPlayers.AddFollower ... (EFT.BotOwner, friendlyPMC.Components.pitAIBossPlayer, ...)`（行 1397，钩子成功）；`pitTeam.*` 旧命名空间查找失败（行 1412/1413/4671，FFT 自身反射陈旧）。

### 3.4 LootingBots fork 面（部署 DLL 已确认 = 仓库 `bin\Release\netstandard2.1` 构建）

- 与上游 v1.6.1 近乎逐字一致；fork 特有：`CleanupBotComponentsPatch`/`BotCleanupHelper`（仅销毁自身 `LootingBrain`/`LootFinder`，与 NRE 无关）、`LocalGameStopPatch`、LootCache 重写、价格缓存、扫描计时 ±20% 抖动、按兵种分级的武器切换、服务端默认 `SpawnWithLoot=true`。
- 缺失上游 1.6.3 `InteractingPlayer` 修复；`ForceBrainEnabled` 永不复位。
- 结构事实：移除 `Utility peace`/`LootPatrol` 层；`PeacefulLogic` 死分支；扫描间隔 10s、`MaxActiveLootingBots=20`；**配置实测检测距离 900m**（代码默认 80m，性能开销未测量）。
- BigBrain 优先级语义：数字越大越优先；LB=4/5/11/13，SAIN=20-24/80/99，无数字冲突。

### 3.5 SAIN fork 面（部署 DLL 已确认 = 仓库 `release\SAIN.dll`）

- LB 协同代码实际落在提交 `a1dbe449`（`2b0280b8` 仅改版本号/CHANGELOG）。
- `LootingBotsInterop` 7 方法全解析 + 单向 `IsAvailable` 闩锁（`LootingBotsInterop.cs:34-163`）。
- `CheckLootingVigilance` 6 条低门槛条件 → `TryPreventBotFromLooting(10s)`，仅 ExtractLayer 激活时运行（`SAINLootingBotsIntegration.cs:128-191`）。
- `PeacefulLayer` 兴趣点探索为死代码；普通 bot 无 SAIN 和平层（和平态活动依赖 LB 层）。
- 对外契约面（`PlayerComponent.PlayVoiceLine`、`SAIN.Plugin.External` 反射面）与 upstream 逐行一致 → **fork 未破坏对外 API**。

### 3.6 SPT 3.11.4 源码面

- `server/` 近 stock（文档宣称的修复未在源码树中，属文档/代码不一致）；`bot.json` stock。
- `modules/` 仅 2 个 Stopwatch transpiler 改写（对 `BotOwner.UpdateManual` / `CoverPointMaster.method_0`，静态比对与官方等价）+ bot 激活时角色随机化（`CustomAiPatch` / `AIBrainSpawnWeightAdjustment`）。
- **不构成本次症状的成因**。

### 3.7 部署与环境事实

- MO2 实例：`C:\Users\Winde\AppData\Local\ModOrganizer\诺瓦克优化包`；gamePath=`E:\Game\EFT_Offline\SPT_3114_Novinsk_Edition`；mods 根=`E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2\mods\`。
- 部署 DLL 身份：SAIN / LootingBots 与 fork 仓库构建逐字节匹配（fork 标记字符串全命中）→ 部署=当前源码。
- 双份 friendlyPMC 安装：`已AI优化` 版在 profile 中**禁用**，仅"合并版本"启用（冗余无害）。
- 客户端 `Assembly-CSharp.dll` 被改写（2026-09-02，15,243,264 B；原版备份 15,406,648 B）——NRE 路径类型与原版逐行一致，**非本次成因**；改写目的未知（部署卫生事项）。
- 配置面：有效 F12 配置位于整合包"F12配置cfg文件"模组；SAIN 行为值的真实入口是 SAIN Editor（F6），F12 内只有 Editor 绑定开关（按 F12 找不到行为开关属正常）。

---

## 4. 对原始假设的裁决

| 假设 | 裁决 | 依据 |
|------|------|------|
| SPT 源码特制化未联调 | **不成立** | server 近 stock；modules 仅 2 个静态等价改写；NRE 栈路径与原版逐行一致 |
| Moew-SAIN fork 未联调 | **部分成立** | 协同代码存在且部署正确，但设计偏窄（单向闩锁、和平死代码、vigilance 仅 Extract 层）；是"协同失效"次因，非发呆主因 |
| Moew-LB fork 未联调 | **成立（普通 bot 维度）** | 移除和平层 + 层激活条件严格 + 版本滞后缺修复；注意该结构为上游 1.6.x 同源，非 Moew 独有 |
| 三方互不联调产生冲突 | **不成立** | 未发现互相破坏证据；层优先级无冲突；实质是各自独立缺陷叠加 |

---

## 5. 修复方向（按优先级，含风险）

### P0 — NRE 护栏（先做，解锁后续验证）

- **做法（双保险）**：
  1. Harmony postfix `BotFollower.Dispose`：boss 已死路径也清 `BossToFollow` 并复位（修复悬垂本身）；
  2. 护栏 `BotBoss.get_MoveSpeed`：`Mover == null` 时返回安全默认（0.7f），覆盖所有同类调用者。
- **载体**：新做一个小 mod（或并入现有护栏 mod）；**不得改 Assembly-CSharp**。
- **风险**：低（hot path 仅一次 null 比较）。仅做 ② 可止血但护卫仍卡 bossPatrol 模式；建议 ①+② 同时上。
- **顺带效果**：NRE 消失后 BigBrain 刷屏自动停止（P4 可降级为可选）。

### P1 — 普通 bot 和平行为恢复（LB fork）

- 短期：回填 `ForceBrainEnabled` 复位（低风险）。
- 回填 1.6.3 `InteractingPlayer` 修复：先确认 EFT 0.16 / SPT 3.11 容器类型具备该属性（KB 反编译缓存可查），可用则风险低。
- 中期：为 Peaceful 分支补行为，或提供替代和平层；恢复 `LootPatrol` 需评估与 LB 扫描的冲突。
- 注意：先执行验证协议 #3 确认"移除和平层"假设，再决定投入。

### P2 — SAIN fork 协同加固

- 闩锁改造：invoke 失败按方法粒度降级 + 日志升级 + 计数（低风险；本次日志证明闩锁未触发，优先级可降）。
- 删除 `PeacefulLayer` 死代码或补调用者（推荐删除）。
- `TryPreventBotFromLooting` 的语义确认：若意图为全局 vigilance，应移出 Extract 层（行为改动，需用户确认）。

### P3 — friendlyPMC

- 先做 A/B（禁用观察）再决定修不修；预期 NRE 不因禁用而消失（vanilla 缺口独立）。
- 若保留：修 `Dismiss` 提前 return 与 `AddBotFollower` catch 清理缺口（每处 <10 行，中风险）。
- 配置缓解：关闭 Goons 生成，观察护卫行为差异。

### P4 — BigBrain 节流（可选）

- NRE 修复后基本不需要；若仍要防其他异常刷屏，单独小 mod 做每秒一条 + 计数（保留 `return true` 语义）。

### P5 — 部署卫生（零代码）

- 文档化 Assembly-CSharp 改写内容/理由（2026-09-02），保留 `.spt-bak`。
- 修正"server 已含修复"但源码树中不存在的文档宣称（补代码或改文档，二选一）。
- 文档说明 SAIN 行为配置入口（F6 Editor）。

---

## 6. 验证协议

### 6.1 A/B 测试

| # | 目的 | 操作 | 观察 | 成功判据 |
|---|------|------|------|----------|
| 1 | NRE 独立性 | 禁用 friendlyPMC（含 Goons 生成），Ground Zero 杀 Kollontay | NRE 计数 | NRE 仍出现 → 证明 vanilla 缺口独立（预期）；Goons 相关 Warning 消失 |
| 2 | 护栏有效性 | 只装 P0 护栏 mod，重复场景 | NRE 计数 + 护卫行为 | NRE=0；护卫在 boss 死后恢复游荡/搜刮 |
| 3 | 普通 bot 假设 | 临时 build LB fork 恢复 Utility peace 层 | assault/PMC 和平期行为 | bot 恢复游荡 → R3 成立 |
| 4 | LB 回填 | 回填 ForceBrainEnabled 复位 + InteractingPlayer | 搜刮后二次行为 | 曾搜刮的 bot 不再停摆 |

### 6.2 插桩建议

- NRE 计数去重（签名 + bot id + 秒级聚合），替代裸日志。
- LB 层激活四条件逐项打点（BotState / 治疗 / IsBrainEnabled / 扫描调度）。
- SAIN `IsAvailable` 状态变更事件日志。
- `PatrolDataFollower` 模式切换日志（谁在何时切换/未切换）。
- `BotBoss.OnBossDead` 挂接日志（精确关联 boss 死亡与 NRE 起始帧）。
- 每 bot 每帧 AI 决策成功/失败计数（量化"发呆"）。
- 若运行时观测面可用（tarkov MCP raid events），拉取 Kollontay death 事件验证时序假设。

---

## 7. 未解点与残余不确定性

1. **Kollontay 死亡时刻与击杀者无日志证据**（vanilla 击杀不写 BepInEx 日志）；"立即出现"的精确时序靠推断，需 raid 事件或插桩确认。
2. **NRE 源 bot 的 pid 未识别**：44,790 条可能来自单个护卫全程，也可能多个护卫接力，现有日志无法区分。
3. **悬垂护卫为何未切出 bossPatrol 的完整调用链未追**（SAIN brain 接管 / BigBrain 层系统 / friendlyPMC 干预的可能性未逐一排除）——R1 修复后仍需追踪。
4. **NRE 对全局 AI 的间接性能影响未量化**（AICoreController 静默吞异常是否拖慢所有 bot 的 AI 帧）。
5. LB 900m 检测距离 + fork 扫描改写的性能开销未测量。
6. SAIN 实际行为配置（Editor 内部保存值）未读取，`CheckLootingVigilance` 门槛以代码默认为准。

---

## 8. 附录

### 8.1 关键文件索引

| 项 | 路径 |
|----|------|
| 运行日志 | `E:\Game\EFT_Offline\SPT_3114_Novinsk_Edition\BepInEx\LogOutput.log` |
| 部署 mods | `E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2\mods\` |
| MO2 实例 | `C:\Users\Winde\AppData\Local\ModOrganizer\诺瓦克优化包` |
| LB fork | `E:\云文件\GitHub\Moew-LootingBot-For-3114\SPT-LootingBots-1.6.1-spt-3.11` |
| SAIN fork | `E:\云文件\GitHub\Moew-SAIN-For-3114` |
| SPT 源码 | `E:\云文件\GitHub\SamMeow_SPT3114_source_code` |

### 8.2 证据产物（临时目录，可能被清理）

- `D:\Temp\opencode\nre-recon\` — NRE 路径反编译产物（BotBoss / GClass545 / PatrolDataFollower / BigBrain patch / friendlyPMC 相关）
- `D:\Temp\opencode\sain-fork-recon\` — SAIN fork 差异基线（recon.txt、full diff）
- `D:\Temp\opencode\ecosystem-recon2\` — BossNotifier / FFT / friendlyPMC 反编译产物

### 8.3 知识库依据（本仓库 SPT 3.11 资产）

- `knowledge/spt-kb/curated/api-notes-3.11/`（3.11 服务端 API 笔记）
- `knowledge/spt-kb/curated/operations/client-mod-compat-audit-playbook.md`（客户端 mod 兼容审计）
- `knowledge/spt-kb/curated/operations/3114-il-conflict-report-v3.md`（IL 冲突面）
- `external/decompile-cache/eft-0.16-spt3114/`（EFT 0.16 / SPT 3.11.4 反编译缓存，用于 vanilla 对照）
- `docs/wayfinder/findings/001-spt-conflict-taxonomy.md`（互操作与冲突分类）

---

*本报告由多路只读取证（日志全量统计、六处代码库/二进制反编译、部署身份比对）交叉验证后落成；所有修复动作尚未执行，待审阅后制定实施计划。*

---

## 修订记录

### 2026-09-19（修复设计阶段追加）：R3 修正

- 二次反编译 + 独立复核裁决确认：`Utility peace`（GClass129）是工具层（无任务时返回 holdPosition），**不是游荡层**；真正游荡层 = `PatrolAssault`（GClass130，`ShallUseNow()` 恒 true，priority 0/1/2），LB 与 SAIN 均未移除它 → 原文 R3"移除和平层 → 扫描间隙无层驱动 → 不游荡"的因果链**不成立**。
- **修正后的机制**（置信：机制 0.85 / 行为后果 0.6，待运行时坐实）：LB `LootingLayer` 因扫描窗口永续而**永久激活**（priority 4/5 压过 PatrolAssault）：
  1. `IsScheduledScan => ScanTimer < Time.time`（`LootFinder.cs:26-29`）——计时器到期后恒真；
  2. **扫描空手后无任何代码重置 ScanTimer**（重置仅发生在搜刮完成 `LootingLayer.cs:89` 与层被压制 `FindLootLogic.Stop`）→ 空手后每帧重入 FindLootLogic → **无限扫描循环，bot 站桩**；
  3. 加剧：cfg 检测距离 900m（默认 80）+ **fork 删除了上游 ScanScheduler 并发节流**（归档 1.6.1 `FindLootLogic.cs:25`）→ 兼解释卡顿。
- 归属：**fork 特制化引入的回归（删除调度器）**，非"未联调冲突"；上游同构缺陷被调度器掩盖。
- 影响：修复计划（`fix-plan.md`）新增 P0 项"空手扫描循环修复"；原"和平层"相关改动方案取消。
- R1/R2/R4/R5/R6/R7 及 NRE 相关结论不受影响。
