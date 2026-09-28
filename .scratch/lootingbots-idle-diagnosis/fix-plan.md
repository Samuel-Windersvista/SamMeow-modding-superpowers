# SPT 3.11.4 Novinsk Edition — LB / SAIN 针对性修复计划

- 日期：2026-09-19
- 范围：仅 Moew-LootingBots fork 与 Moew-SAIN fork（按指示；NRE 护栏 / friendlyPMC / BigBrain 节流 / 部署卫生另行处理）
- 依据：`diagnosis-report.md`（含文末修订记录）+ 两份修复设计草案（`D:\Temp\opencode\lb-fix-design\`、`D:\Temp\opencode\sain-fix-design\`）+ oracle 复核裁决
- 状态：**待审批**；本计划未执行任何修改

---

## 0. 前提修订：R3 已修正（必读）

诊断报告原 R3（"LB 移除原版和平层 → 扫描间隙无层驱动 → 普通 bot 不游荡"）经二次反编译 + 独立裁决**否证**：

- `Utility peace`（GClass129）是工具层（无任务时返回 holdPosition），**不是游荡层**；真正游荡层 = `PatrolAssault`（GClass130，`ShallUseNow()` 恒 true，priority 0/1/2），LB 与 SAIN 均未移除它。
- **修正后的机制**（置信：机制 0.85 / 行为后果 0.6，待运行时坐实）：
  1. `IsScheduledScan => ScanTimer < Time.time`（`LootFinder.cs:26-29`）——计时器到期后**恒真**；
  2. **扫描空手后无任何代码重置 ScanTimer**（重置仅发生在搜刮完成 `LootingLayer.cs:89` 与层被压制 `FindLootLogic.Stop`）→ 空手后每帧重入 FindLootLogic → **无限扫描循环，bot 站桩**；
  3. LB 层（priority 4/5）因此永久激活，压制 PatrolAssault → 永不游荡；
  4. 加剧：cfg 检测距离 **900m**（默认 80）+ **fork 删除了上游 ScanScheduler 并发节流**（归档 1.6.1 `FindLootLogic.cs:25`）→ 每 bot 每帧 900m OverlapSphere + 3000 collider 池（兼解释卡顿）。
- 症状映射："很难进入搜刮" = 过滤链严、空手率高；"战斗后短暂正常" = 战斗期 LB Stop 重置 timer，和平后一轮游荡直到下次空手再站桩。
- 归属：**fork 特制化引入的回归（删除调度器）**，非"未联调"；上游同构缺陷被调度器掩盖。
- 结论：新增 **P0 项 LB-1**；原"和平层"相关改动（让 PeacefulLogic 可达 / 恢复 Utility peace）**取消**（反证 + 会加剧站桩）。

---

## 1. 修复项总览

| 编号 | 仓库 | 项 | 优先级 | 依赖 | 状态 |
|------|------|----|--------|------|------|
| LB-1 | LB | **空手扫描循环修复（ResetScanTimer）** | **P0（主症状）** | 无 | 方案已定 |
| LB-2 | LB | InteractContainer + InteractingPlayer（1.6.3 移植） | P1 | 无 | 方案已定（API 已验证） |
| LB-3 | LB | ForceBrainEnabled 复位（1.7 移植） | P1 | 与 SAIN-3 联动 | 方案已定 |
| LB-4 | LB | 尸体 rootItem 处理 | P2 | 无 | 方案已定 |
| LB-5 | LB | ItemAppraiser 价格缓存修复 | P2 | 与 SAIN-2 捆绑验证 | 方案已定 |
| LB-6 | LB | ScanScheduler 节流回移（可选增强） | P3 | LB-1 后按性能测量决定 | 评估项 |
| LB-M1 | LB cfg | 检测距离 900 → 80-150m | **立即缓解（零代码）** | 无 | 建议先行 |
| SAIN-1 | SAIN | 闩锁改造（per-method 降级 + 指数退避） | P1 | 无 | 方案已定 |
| SAIN-2 | SAIN | GetItemPrice 类型错配修复 | P1 | 无 | 方案已定（已核实） |
| SAIN-3 | SAIN | vigilance 门控决策 + 调参 | P2（**需用户决策**） | 若启用全局 tick：必须先 SAIN-1+SAIN-2；与 LB-3 同批 | 待决策 |
| SAIN-4 | SAIN | PeacefulLayer / 兴趣点死代码处理 | P3 | 无 | 建议：不接通 / 删除 |

---

## 2. 修复项详情

### LB-1（P0）空手扫描循环修复

- **问题**：空手扫描后 ScanTimer 不重置 → 无限扫描 → LB 层常驻 → 站桩、不游荡（见第 0 节）。
- **改法（二选一，推荐 A；语义要求一致）**：
  - A. `Logic/FindLootLogic.cs:24-45`：在"本轮搜索结束且未发现目标"的路径上补 `_lootFinder.ResetScanTimer()`；
  - B. `LootingLayer.IsCurrentActionEnding`（`LootingLayer.cs:75-93`）的 FindLootLogic 分支补 `ResetScanTimer`（与搜刮完成路径 `:89` 对齐）。
- **语义要求（实现时核对）**：
  - 仅在"空手结束"时重置；**不得**在首次进入或发现目标后重置（否则首次扫描被延迟 / 目标丢失无法前往搜刮）；
  - 重置后 `IsScheduledScan=false` → 层让位 → PatrolAssault 接棒游荡；下一周期（10s±20%）再扫描。
- **风险**：低（仅扫描调度）；扫描节奏从"每帧"回到"~10s/轮"，为期望行为。
- **验证**：LB Debug 日志 "Starting scan"/"No loot in range" 频率从每帧重入回落到 ~10s/次；非战斗普通 bot 恢复游荡；站桩时长显著下降。

### LB-2（P1）InteractContainer + InteractingPlayer（上游 1.6.3 移植）

- **改法**：`Utilities/LootUtils.cs:62-66` 新签名 `InteractContainer(LootableContainer container, IPlayer player, EInteractionType action)`，body 内先 `container?.InteractingPlayer = player;` 再 `Interact`；调用点 `Components/LootingBrain.cs:387`、`:400` 传入 bot 的 IPlayer。
- **API 验证（已完成）**：部署 0.16 程序集 `WorldInteractiveObject.InteractingPlayer`（IPlayer，get/set）存在；`BotOwner` 实现 IPlayer（具体传 `BotOwner` 本体或 `BotOwner.GetPlayer`，以实现时程序集验证为准）。
- **不采用**上游 1.7 的 `vmethod_0/1`（4.0 混淆依赖）。
- **风险**：低；与 LB-3/4/5 无依赖。

### LB-3（P1）ForceBrainEnabled 复位（上游 1.7 移植）

- **改法**：在覆盖所有 ForceScan 结束路径处复位 `ForceBrainEnabled=false`——候选点：`FindLootLogic.Stop`（与 ResetScanTimer 同点，推荐）/ `LootFinder.FindLootCoroutine` finally（`LootFinder.cs:248-252`）/ `FindLootLogic` 的 `!HasFreeSpace` 早退分支（`FindLootLogic.cs:26-32`）。
- **风险**：低；cfg 启用下 `IsBrainEnabled` 本已 true，本项为残留治理。
- **联动**：SAIN-3 若启用全局 vigilance，TryForceBotToScanLoot 频率上升 → 本项须先行完成（同批）。

### LB-4（P2）尸体 rootItem 处理

- **改法**：先判定 `canLootCorpse` 并明确 corpse 的 rootItem（`GetComponentInParent<Corpse>()?.ItemOwner?.RootItem`），再对**非空** rootItem 做 ignore 检查；`CacheActiveLootId(rootItem.Id)`（`:225`）保持 rootItem 非空（防御 `rootItem?.Id`）。
- **风险**：低-中（改判定顺序，须避免引入 NPE）。

### LB-5（P2）ItemAppraiser 价格缓存修复

- **改法**：仅缓存非武器物品（`cacheable = !(lootItem is Weapon && ValueFromMods)`），或按上游 1.7 直接删除缓存。
- **风险**：低；与 SAIN-2 捆绑验证（价格链变活后实际生效）。

### LB-M1（立即，零代码）配置缓解

- 文件：`E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2\mods\` 下"F12配置cfg文件"模组的 `BepInEx\config\me.skwizzy.lootingbots.cfg`（glob 定位；也可游戏内 F12 修改）。
- 改动：检测距离 900 → 80-150m。
- 效果：直接缩短单轮扫描帧数与站桩时长（循环修复前的即时缓解），并降低卡顿。
- 风险：低（回到默认量级）。

### LB-6（P3，可选）ScanScheduler 节流回移评估

- 上游归档 1.6.1 `FindLootLogic.cs:25 ScanScheduler.CanStartScan`；fork 删除。LB-1 修复后按性能测量决定是否回移。

### SAIN-1（P1）闩锁改造

- **目标语义**：`IsAvailable = 插件加载 且 External 类型解析成功`；方法级失败独立降级。
- **实现要点**：per-method slot（MethodInfo + Disabled/DisabledUntil/FailCount/Logged）；失败指数退避 30→300s（用 `Time.unscaledTime`）；首次失败 Warning（含方法名），每 20 次复述；Init 时任一方法缺失打一次 Error（IsFullyAvailable 诊断）；全局重置（ResetMethodHealth）为可选增强。
- **风险**：低；注意调用方语义（fallback 与"真 false"一致，已核对）；改动需在文档声明。

### SAIN-2（P1）GetItemPrice 类型错配修复

- **现状**：`LootingBotsInterop.GetItemPrice(LootItem)`（`:220/:232`）以 `LootItem` 反射调用 LB `External.GetItemPrice(Item)` → 必抛 ArgumentException → 触发闩锁全灭（当前因预设 `ExtractFromLoot=false` 休眠）。
- **改法**：传 `item.Item ?? item.ItemOwner?.RootItem`（3.11 API 已验证）；**null 时直接 return 0f，不 invoke**（避免 LB 侧 NPE 再触发降级）。不在 LB 侧重载。

### SAIN-3（P2，需决策）vigilance 门控

- **现状**：预设 `ExtractFromLoot=false` → `SAINLootingBotsIntegration.Update()` 整段休眠；`TryEnsureSafeLootingPosition` 无调用者。
- **选项 a（默认建议）**：保持休眠；若未来启用则调参（Suppression>0 → >0.25；枪声 50m → 30m；固定 10s → 分级 10s/3-5s）。
- **选项 b**：移至 `BotComponent.ManualUpdate` 全局 tick（对所有 SAIN bot 生效）；**前置：SAIN-1 + SAIN-2 已完成**；与 LB-3 同批。
- **决策点**：是否要让"搜刮中遇威胁中断 / 战后强制搜刮"在本包生效？

### SAIN-4（P3）PeacefulLayer / 兴趣点死代码

- **结论**：不建议扩展给普通 bot（45 > LB 4/5，会压制搜刮）；兴趣点探索为死代码（`RecordPoint` 无调用者；GetNextAction 兴趣点分支动作类型错误）。
- **建议**：整体删除（含 `SAINInterestPointClass` 与 BotComponent 属性），或保留并注释"仅 Partisan、未接通"。删除前确认无外部反射引用。

---

## 3. 批次与顺序建议

| 批次 | 内容 | 目的 |
|------|------|------|
| 批次 0 | LB-M1 配置调整 + 开启 LB Debug 日志，跑 1 场基线 | 即时缓解 + 坐实修正后的 R3（扫描频率/站桩时长） |
| 批次 1 | LB-1 | 单变量验证主症状修复（恢复游荡） |
| 批次 2 | LB-2 + LB-3 + LB-4 + LB-5（一次构建） | LB 移植组（低风险） |
| 批次 3 | SAIN-1 + SAIN-2（一次构建） | 协同面加固 |
| 批次 4 | SAIN-3（按决策）+ SAIN-4 | 决策项收尾 |

- 每批次独立构建/部署/回滚；批次 1 与 2 可合并（速度优先，牺牲单变量归因）。
- 实施可由 @fixer 按本计划 + 两份设计草案逐批执行；每批完成后交给用户做一场验证 raid。

---

## 4. 构建 / 部署 / 回滚

**LB（netstandard2.1）**
- 构建（工作目录 = 仓库根）：`dotnet build "LootingBots\LootingBots.csproj" -c Release`
- 产物：`LootingBots\bin\Release\netstandard2.1\skwizzy.LootingBots.dll`
- 部署：覆盖 `E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2\mods\` 下 `skwizzy.LootingBots.dll`（glob 定位；基线 SHA `F5356266…DE581B`）
- 备份：覆盖前 `Copy-Item <target> <target>.bak-<时间戳>`；回滚 = 复制回并删 .bak

**SAIN（net471）**
- 构建：`dotnet build "SAIN.csproj" -c Release` → `bin\Release\net471\SAIN.dll`
- 注意：csproj PostBuild 目标 1 路径失效，**不要依赖 PostBuild 投递**；手动复制
- 部署：`...\mods\[6]AI增强-SAIN-Moew-v4.4.0增强版\BepInEx\plugins\SAIN\SAIN.dll` + 同步仓库 `release\SAIN.dll`
- 备份：基线 SHA `597E9124…9E22`

**通用**：操作前关闭游戏与 MO2；每次部署后记录新哈希；改动只落在两处 fork 仓库与部署副本，不改游戏原始文件。

---

## 5. 验证协议

**插桩（临时，可做进各 fork 或独立观察 mod）**
1. 每 bot 打点：`IsScheduledScan / IsScanRunning / LootingLayer IsActive / 当前层名`（坐实 LB-1 机制与修复效果）。
2. LB Debug 日志：扫描开始/无果频率、站桩时长。
3. SAIN 层激活状态打点（排除其他层持有）。
4. NRE 护栏上线后（另行计划）观察普通 bot 是否连带变化（排除 NRE 间接影响）。

**每批次成功判据**
- 批次 1：非战斗期普通 Assault/PMC bot 恢复游荡；扫描日志频率从"每帧重入"回落至 ~10s/次；站桩时长显著下降。
- 批次 2：搜刮容器后不再停摆（LB-2）；ForceScan 后无残留（LB-3）；尸体可搜刮（LB-4）；武器估价一致（LB-5 + SAIN-2 捆绑）。
- 批次 3：协同不再整体失效（方法级失败仅告警不扩散）；GetItemPrice 不再抛错。

**回滚判据**：任一回归（bot 更静止 / 崩溃 / 性能恶化）→ 回滚该批次 DLL 并记录。

---

## 6. 需要用户决策的点

1. **SAIN-3**：vigilance 是否全局启用（选项 b）？默认建议：a（保持休眠 + 调参备用）。
2. **SAIN-4**：死代码删除 vs 保留注释。
3. **批次合并**：批次 1+2 是否合并为一次构建（速度 vs 归因）。
4. **LB-M1**：是否先行调整检测距离（建议是）。

---

## 7. 范围外（按指示搁置）

- NRE 护栏（诊断报告 P0）、friendlyPMC 缺口修复、BigBrain 日志节流、部署卫生（Assembly-CSharp 改写说明、文档/代码不一致）。

---

## 8. 附录

- 基线哈希：LB DLL `F5356266…DE581B`（101,888 B）；SAIN DLL `597E9124…9E22`（1,023,488 B）
- 设计草案产物：`D:\Temp\opencode\lb-fix-design\`、`D:\Temp\opencode\sain-fix-design\SAIN-R5-R7-修复设计草案.md`
- 相关文档：`diagnosis-report.md`（含 R3 修订记录）、KB `curated/operations/client-mod-compat-audit-playbook.md`

---

*本计划为待审批稿；审批后按批次执行，每批次单变量验证。*

---

## 执行日志

### 批次 0+1（2026-09-19 23:55）

- **LB-M1 已执行**：`me.skwizzy.lootingbots.cfg`（F12 配置模组）三个检测距离键 900 → 150；备份 `me.skwizzy.lootingbots.cfg.bak-20260919-2355`。
- **LB-1 已实施并部署**：`LootingBots/LootingLayer.cs` `IsCurrentActionEnding()` 的 FindLootLogic 分支新增"空手扫描结束后 `ResetScanTimer()`"（语义走查：首次激活不重置 / 发现目标不重置 / 空手重置；`IsBotLooting = LootTaskRunning || HasActiveLootable` 已复核）。
  - 构建：`dotnet build -c Release` 成功（0 警告 0 错误）。
  - 部署：`[6]AI自动搜索物品-LootingBots - 已AI优化\BepInEx\plugins\skwizzy.LootingBots.dll`
  - 哈希：旧 `F5356266…DE581B` → 新 `4761BA00…E9163`；备份 `skwizzy.LootingBots.dll.bak-20260919-2355`。
- **状态**：待 OVERSEER 验证 raid（观察：非战斗普通 bot 游荡恢复 / 搜刮恢复；boss 护卫 NRE 属搁置项）。
- **回滚**：复制对应 `.bak-20260919-2355` 覆盖回原位即可。

### 跟班 NRE 护栏（追加工作流，2026-09-20 01:07）

- **背景**：批次 0+1 验证发现"boss 击杀后跟班滑步"（follower/NRE 族，原诊断报告 R1）；IL 取证定位两处悬垂解引用：`BossToFollow.PatrollingData`（GClass548，41,591 条/场）与 boss `Mover`（GClass545，3,365 条/场）。
- **实施**：新建独立 mod `SamMeow.FollowerGuard` v0.1.0（A: BotBoss.get_MoveSpeed 护栏；B: PatrolDataFollower.ManualUpdate 护栏；C: BotFollower.Dispose 清理缺口修补；D: 4 访问器 null 短路；共 7 patch）。
  - 工程：`mods\SamMeow.FollowerGuard\`（netstandard2.1 / BepInEx 5 / HarmonyX）；构建 0 警告 0 错误；DLL SHA256 `ED26FA1A…19B9`（9,728 B）。
  - 部署：MO2 覆盖层 `mods\[6]AI修复-护卫NRE护栏-FollowerGuard\`（含 meta.ini）；modlist.txt 已注册（第 197 行；备份 `modlist.txt.bak-followerguard-20260920010709`）。
  - 静态核对：目标类型/方法签名经 Mono.Cecil 逐一确认；部署副本哈希与构建产物一致（已独立复核）。
- **预期**：NRE 44,956/场 → 0；跟班在 boss 死亡后恢复 simple 巡逻；滑步消除。
- **状态**：待验证 raid（观察：杀 boss 后跟班恢复巡逻；新日志 `[FollowerGuard] applied 7/7 patches` 与 NRE 计数）。
- **回滚**：删除覆盖层目录 + 移除 modlist.txt 该行（或还原 modlist 备份）。

### 价格链优化批次（2026-09-20 11:33）

- **背景**：价格链路分析（`loot-price-chain-analysis.md`）后经用户批准实施 A2+A1+A3+A7+B2。
- **改动**（LB fork）：
  - `Components/ItemAppraiser.cs`：A2 堆叠计价（`* StackObjectsCount`）；A1 缓存仅存"非武器单价"（武器不读写缓存）；A3 market 未命中回退 handbook + 0 价不写缓存 + Init 无条件构建 handbook；AmmoBox 按其弹药计价。
  - `Components/LootingInventoryController.cs`：A7 弹药豁免（IsUsableAmmo + 三武器槽弹膛匹配）。
  - `Components/LootFinder.cs`：B2 空扫描冷却（连续 3 次空扫 → 180s；与 LB-1 的 ResetScanTimer 协同：LockUntilNextScan 抑制）。
- **A5 核实**：`LootingInventoryController.cs:764` 确认为笔误（应为 `lootWeapon.Id`），影响低，未修（随后续批次）。
- **构建**：0 警告 0 错误；DLL SHA256 `7D05926B…FDF5`（102,912 B）。
- **部署**：`[6]AI自动搜索物品-LootingBots - 已AI优化\BepInEx\plugins\skwizzy.LootingBots.dll`；旧 `4761BA00…` → 新 `7D05926B…`；备份 `skwizzy.LootingBots.dll.bak-20260920-1133`。
- **状态**：待验证 raid（观察：弹药/消耗品可被拾取、武器估值正确、连续空扫后进入 3 分钟冷却）。
- **回滚**：复制 `.bak-20260920-1133` 覆盖回原位。

### 批次 2+3（LB 移植组 + SAIN 加固，2026-09-20 12:06）

- **批次 2（LB，fix-1）**：
  - LB-2 `LootUtils.InteractContainer` 增加 IPlayer 参数并设 `container.InteractingPlayer`（上游 1.6.3 语义；调用点 LootingBrain.cs:387/400 传 BotOwner）。
  - LB-3 `ForceBrainEnabled` 复位三路径（FindLootLogic.Stop / `!HasFreeSpace` 早退 / FindLootCoroutine finally——覆盖 Unity 协程 Stop 不执行 finally 的缺口）。
  - LB-4 尸体 rootItem 解析（`GetComponentInParent<Corpse>()?.ItemOwner?.RootItem`）+ ignore 判定加非空守卫 + `CacheActiveLootId(rootItem?.Id)` 防御。
  - A5 `LootingInventoryController.cs:764` 笔误修正（`lootWeapon.Id`）。
  - 构建 0 警告 0 错误；DLL SHA `7F2E78F3…`（102,912 B）；部署：旧 `7D05926B…` → 新；备份 `.bak-20260920-1206`。
- **批次 3（SAIN，fix-2）**：
  - SAIN-1 `LootingBotsInterop` 闩锁改造：per-method Slot/MethodSlot + TryInvoke 指数退避 30→300s（unscaledTime）+ 首次/每 20 次日志 + `IsFullyAvailable` 诊断 + `ResetMethodHealth`（未接 raid-start 钩子）；`IsAvailable` = 类型解析成功；公开 API 不变。
  - SAIN-2 `GetItemPrice` 改取 `item.Item ?? item.ItemOwner?.RootItem`，null 短路 return 0f。
  - 构建 0 错误（4 条既有警告）；DLL SHA `B0D1B3A3…`（1,024,000 B）；部署：旧 `597E9124…` → 新；备份 `.bak-20260920-1206`；仓库 `release\SAIN.dll` 已同步。
- **状态**：待验证 raid（观察：搜刮容器后不停摆 / 尸体可搜刮 / 武器估值与弹药拾取正常 / 无新增异常；SAIN 日志应出现方法级告警而协同不整体失效）。
- **回滚**：分别复制对应 `.bak-20260920-1206` 覆盖回原位（SAIN 另需回滚 `release\SAIN.dll` 备份）。

### SAIN-3b + P2-1 + friendlyPMC 护栏（2026-09-20 13:56）

- **SAIN-3b（vigilance 全局启用）**：`BotComponent.ManualUpdate` 加入 `LootingBotsIntegration?.Update()`；移除 ExtractLayer / PeacefulLayer 两处门控调用（保留 FullOnLoot 读取）。构建 0 错误；DLL SHA `B0D1B3A3…` → `23A0E126…`；备份 `.bak-20260920-1356`；`release\SAIN.dll` 已同步。
- **价格链 P2-1（A4 每格阈值 + cfg 同批）**：`LootingBrain.IsValuableEnough` 除格（武器豁免）；`LootingInventoryController` 的 AllowedToPickup/日志接入 itemSize；cfg `PMC Min 2000→1500`、`Scav Min 3000→2000`（备份 `.bak-20260920-1356`）。构建 0 错误；DLL SHA `7F2E78F3…` → `30D2EAFA…`；备份 `.bak-20260920-1356`。
- **friendlyPMC 护栏（P2+P4+P3）**：新建 `SamMeow.FriendlyPmcGuard` v0.1.0（P2 Dismiss 清理 + P4 死 bot 招募拦截；全反射；SHA `75F06E3A…`）部署为新覆盖层 `[6]AI修复-friendlyPMC护栏-FriendlyPmcGuard` 并注册 modlist（第 198 行）；`FollowerGuard` 升 v0.1.1 增加 P3（InitPlayer 清 followerAIBase；SHA `ED26FA1A…` → `94143221…`；备份 `.bak-20260920-1356`）。
- **状态**：待验证 raid（重点：SAIN-3b 中断/恢复行为；每格阈值拾取取舍；护栏日志 "dismissed dead follower cleanup" / "skip recruit dead bot"；全量 NRE 保持 0）。
- **回滚**：各 `.bak-20260920-1356` 覆盖回原位；FriendlyPmcGuard 可禁用/删除覆盖层并移除 modlist 行。

### 价格链 P2-2（B1 ScanScheduler + C1c cfg，2026-09-20 14:05）

- **B1**：新建 `Utilities/ScanScheduler.cs`（全局并发上限 + 取票/归还）；`LootingBots` 新增配置 `MaxConcurrentScans`（默认 3，范围 0-35）；`LootFinder.BeginSearch` 取票（ForceScan 豁免）+ 双归还点（协程 finally + `FindLootLogic.Stop`，幂等防双归还）；`FindLootLogic.Update` 取票失败 → `OverrideNextScanTime(1.5s)`（与 LB-1/B2 三层协同）；`LootingBrain.Start` 调 Init、`LootCache.ClearAllCaches` 调 Reset；codemap 更新。
- **C1c**：cfg 三距离 250 → 尸体 120 / 容器 100 / 散落 150（阈值保持 PMC 1500 / Scav 2000）。
- 构建 0 警告 0 错误；DLL SHA `30D2EAFA…` → `1570E24E…`（104,448 B）；备份同目录 `.bak-<时间戳>`（DLL 与 cfg 各一份）。
- **状态**：待验证 raid（扫描节奏 / 性能 / 回归；若偏保守可在 F12 调 `MaxConcurrentScans=5`）。
- **回滚**：对应 `.bak-<时间戳>` 覆盖回原位。

### 价格链 P2-3（C3 尸体优先项 + Unlootable 过滤，2026-09-20 17:37）

- **API 预验证**：`UnlootableComponent` / `IsUnlootableFrom` / `GetOwner()` 扩展方法等全部可用（0.16）。
- **改动**：`LootUtils.cs` 新增 `GetPriorityItems` + `AddItemsInSlots`（含 Unlootable 过滤 + 保留 locked 语义）；`LootingBrain.cs:331-339` 改用新方法；codemap 更新（含 LB-2 遗留签名行）。
- 构建 0 警告 0 错误；DLL SHA `1570E24E…` → `1A08863C…`（104,960 B）；**已部署**（备份 `.bak-20260920-1737`）。
- **状态**：待验证 raid（尸体拾取更精准、无效事务减少）。

### 分发补丁包（2026-09-20）

- **产物**：`E:\Game\EFT_Offline\【生活在诺文斯克】bot行为修复补丁_v1.0_2026-09-20.zip`（约 453 KB）
- **内容**（镜像 MO2 mods 结构 + profiles + README + SHA256SUMS）：
  - LootingBots DLL `1A08863C…` / SAIN DLL `23A0E126…` / FollowerGuard v0.1.1 覆盖层（DLL `94143221…`）/ FriendlyPmcGuard v0.1.0 覆盖层（DLL `75F06E3A…`）/ LB cfg `7E3132C1…` / modlist.txt `1CD6AC5B…`
- **安装**：关闭 MO2 → 合并覆盖 `mods\` → （如需要）modlist 添加 FriendlyPmcGuard 行 → 启动。
- **注意**：建议本机验证 P2-3 通过后再分发；若发现问题重新打包。
