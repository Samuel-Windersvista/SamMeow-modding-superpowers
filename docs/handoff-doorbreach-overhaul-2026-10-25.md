# Handoff: DoorBreachOverhaul — 06 代码 + Level B 完成、07 代码完成；Level C 与部署待办

> 交接日期：2026-10-25 ｜ 交接方：实施会话（06 → 07） ｜ 接收方：新会话 agent
> 上一份交接：`docs/handoff-doorbreach-overhaul-2026-10-24.md`（其“下一 frontier = 06”已被本文件取代）
> 项目规则：交接文档放仓库 `docs/`（禁止临时目录）；本文件即按此规则落盘。未做任何 git commit。

## 一、当前进度一句话

工单 **01–05 ✅**；**06 代码 + S0 + Level B ✅，Level C 逐开关实测挂起**；**07 代码 + 构建 + 覆盖层部署 ✅，未实机**；**08 服务端代码/覆盖层部署 ✅，游戏内 C4 物品与 bundle 验证仍待**；**04 的 KeyCard 菜单单项仍待补测**。下一 frontier：**补 06/07/08 的 Level C → 09 收口**。

## 二、当前交付与环境

| 项 | 值 |
|---|---|
| 客户端工程 | `mods/SamMeow-DoorBreachOverhaul` |
| 服务端工程 | `mods/SamMeow-DoorBreachOverhaulServer`（08 版本，未改动） |
| MO2 实例 | `E:\Game\EFT_Offline\Norvinsk_Forever`（profile Default） |
| 游戏 / 服务端 | `E:\Game\EFT_Offline\SPT_41x`（SPT 4.1.6 / EFT 0.16.9.40743），服务端根 `SPT_41x\SPT_Runtime` |
| 覆盖层 | `[8]门的大修-破门大修-DoorBreachOverhaul`（客户端 + 服务端两棵树） |
| 当前覆盖层 DLL | **07 构建（已部署）** `4FF4DB6FCB5FF923E624F35DA9F1B7A1DCA59C77739CBAB333A1DA85AAADE61C` / **175,616 B** |
| 07 部署备份 | `.artifacts\doorbreach-ticket09-backup\2026-10-25-07-deployed\`（16 文件 / 2,407,674 B，DLL hash 一致） |
| 备份 | `.artifacts\doorbreach-ticket06-backup\DoorBreachOverhaul.pre-ticket06.dll`（05，`A33D6E21…`）、`…\DoorBreachOverhaul.ticket06-initial.dll`（06 初始，`CE2D0115…`）；**06 诊断 DLL `1EF07AB9…` 未单独留存**（部署时备份命令失败；07 构建含同一诊断日志） |
| 当前 cfg | `Norvinsk_Forever\overwrite\BepInEx\config\com.sammeow.doorbreach.cfg`；**不要重置用户配置** |
| 构建/测试 | `dotnet build -c Release` 0/0；S0 **100/100**；标准检查 PASS=12 / FAIL=0 / WAIVED=2 |

当前 cfg 关键值（用户已确认，勿覆盖）：`Pleb=False / SemiPleb=False / 金属门破门弹=True / 容器=True / 后备箱=True / 耐久 200–400 / 霰弹 12 / 弹药 4 / 黑名单空 / Destructible Doors=true`；06 新增 `Allow Safety Exclusions=False`、`Allow High Risk Exclusions=False`。

## 三、06 已完成事实（代码 + Level B）

- 规则层：`src/Rules/ExclusionPolicy.cs`（纯函数），`DoorDestructionRegistry` 与 `AttackPolicy.EvaluateDestruction` 共用；`DoorClassKind` 增 `NonOperatable / Double / Trigger / Extraction`；`ExclusionCategory`（None/Fika/Safety/HighRisk/Physical）。
- 配置：`6. 排除与开关` 新增 `Allow Safety Exclusions` / `Allow High Risk Exclusions`（默认均 false，战局启动快照）；`ModConfig` / `RuleConfig` / 插件快照日志已接。
- 注册表：Fika 硬排除 → 物理不可行（无弹道碰撞体/材质无档案/无几何/异常尺寸；回写 `DoorClassKind.Excluded`）→ `ExclusionPolicy`；双门候选排除物理/Fika/高危组，只把 `Normal` 改写 `Double` 并重跑判定。
- 调试面板：非受理门显示 `safety: ...` / `high risk: ...` / `physical: ...`；顶栏只统计仍被排除项。
- 诊断日志（为 Level C 加）：每局 `[Destruction] N doors: X destructible, safety excluded S, high risk excluded H, physical P`；每扇门首次判定 `[Policy] <doorId> (<DoorClass>/<Exclusion>): accepted=... (reason)`。
- Level B：17 个 DoorBreach 补丁全部 Enabled；`Failed to create patch=0`；DoorBreach 异常=0；快照 `安全类排除=False, 高危组排除=False`。
- 06 工单状态 `ready-for-human`；Level C 三轮清单见工单 Comments。

## 四、07 已完成事实（代码 + 构建）

- 菜单门控：`ExplosiveBreachComponent.IsPlantExplosiveDisabled` 统一判定（持有 C4 + 门状态 + 破门角度）；异常按禁用、`LogOnce` 单次告警；`DoorMenuMerge` 调用。
- 放置：锚定回退链 Lock → Handle → Center；`StartExplosiveBreach` 返回 bool，放置失败不扣物品/不发包/不启动倒计时；`ItemFactory`/`CreateItem`/`SetupItem`/GameWorld 缺失均降级。
- 引爆：`FractureState.Detonate()`（整扇门板脱离，`DestructionResult.Detonated=true`）→ `DoorDamageIntake.ApplyDetonation` → 既有视觉/玻璃管线 → `DoorTakeover`（爆炸冲量上限 `mass*8`，子弹仍 `mass*0.5`）。**不再 `Destroy(door.gameObject)`**。
- 回退：`TryDetonateDoor` 仅在 `DestructionEnabled && ShowHoles && entry.Eligible && Frame != null` 时走破坏；否则 `OpenDoorIfNotAlreadyOpen`（安全类/高危组开关关闭、物理、Fika、破坏关闭、Show Holes 关闭）。
- 爆炸伤害/特效：Effects 缺失单次告警；半径/伤害 <=0 跳过；按 Player 去重；LOS 用 `GetComponentInParent<Player>`。
- 日志：`[C4] 放置：<doorId> anchor=...`、`[C4] 引爆：<doorId> -> destruction|fallback-open`。
- 07 工单状态 `ready-for-human`；与 08 的 10 步联合端到端清单在工单 Comments。

## 五、待办（按序）

1. **部署 07（已完成 2026-10-25）**：
   - 覆盖层已更新为 `4FF4DB6F…` / 175,616 B；README 19,060 B；`meta.ini version=0.1.0`；服务端 DLL/bundle/db 未变。
   - 部署后整目录备份：`.artifacts\doorbreach-ticket09-backup\2026-10-25-07-deployed\`（16 文件 / 2,407,674 B）。
   - **[偏差]** 覆盖前整目录备份失败，06 诊断 DLL `1EF07AB9…` 未单独留存；07 构建已包含 06 诊断日志，Level C 仍可用 07 构建执行。05/06 初始备份仍在。
   - MO2 运行中完成复制；若 MO2 覆盖 `meta.ini`，重启后复核 `version=0.1.0`。下一步请 Overseer 经 MO2 启动，agent 读日志断言 17 补丁、0 failed、快照两开关、无 DoorBreach 异常。
2. **06 Level C（三轮，不重启游戏）**：
   - 第 1 局：两开关关；打普通门 + 安全类/高危组各一；预期 `[Policy] ... accepted=True`（普通）/`accepted=False (safety: …|high risk: …)`，`[Destruction] safety excluded S>0`。
   - 第 2 局：`Allow Safety Exclusions=True`；预期 `safety excluded 0`、安全类 `accepted=True`、高危组仍 `high risk`。
   - 第 3 局：`Allow High Risk Exclusions=True`；预期 `high risk excluded 0`、高危门 `accepted=True`、安全类仍 `safety`。
   - 重点：任务脚本/撤离交互不被误伤；双门两扇同时翻转；物理项永远 `physical`。
   - **已得部分结果（Shoreline，默认全关）**：注册表 `373 doors / 321 destructible / safety 0 / high risk 0 / physical 52`；普通门 `door_Shoreline_West_00045` accepted=True；`door_Shoreline_Sanatorium_indoor_00046` 29 次命中、上铰链 17% / 下铰链 54%、`leaf Attached`（未脱落属预期，铰链未归零）。**Shoreline 无安全类/高危类门，需换 Interchange/Labs/Reserve/Streets 继续。**
   - **Factory 边界（默认全关）**：注册表 `77 doors / 47 destructible / safety excluded 1 / high risk 0 / physical 29`；`Factory_Rework_Basement_00031` 面板 `(KeycardDoor) Locked`，但 `physical: material MetalThick stays vanilla` → 归 physical，不是 safety；即使开 `Allow Safety Exclusions` 也不会可破坏。**结论：物理不可行优先于门类；要测安全类开关需找 WoodThin/WoodThick/MetalThin 的 KeycardDoor。**
   - **Interchange KeycardDoor 安全类 A/B PASS**：`Shopping, Mall_DesignStuff_00051`（`KeycardDoor` / `Metal 1`）。A 默认全关：不出孔，面板 `safety: keycard door`，本图 `242/83/1/0/158`。B（`Allow Safety Exclusions=true` 重进）：面板 `destructible: MetalThin`、`safety excluded 0`、`hits 26`、`cells 544/544`；日志 `[Policy] ... (Keycard/Safety): accepted=True (accepted (locked))`。**本图 safety 只有 1，滑门/开关门不在此图；滑门去 Labs，开关门去 Reserve/Streets/Labs，以面板 `(type)` 为准。**
3. **07 + 08 联合 E2E**：按工单 07 Comments 的 10 步（服务端日志 → 工作台产出 C4 → 菜单门控 → 放置/倒计时 → 普通门 destruction → 排除门 fallback-open → 物理门 fallback → 范围伤害 → 无服务端降级 → 反复进出无残留）。
4. **04 KeyCard 菜单单项**：上锁/解锁 KeyCard 的 DO 三动作与 C4 去重实测（独立待办）。
5. **09 部署收口**：覆盖层打包/meta.ini、替换旧 DO 覆盖层并保留回滚、BB 未就绪两项收口、`[15]` 配置对照迁移记录、Level C 全清单、README/许可/偏差表、回滚点记录。
   - 已完成非部署部分：staging `.artifacts\doorbreach-ticket09-staging\`（meta.ini `version=0.1.0`、最新 README/LICENSE/NOTICES、07 DLL、服务端 08 树、`DEPLOY.md`）；分发包 `.artifacts\DoorBreachOverhaul-0.1.0.zip`（2,279,701 B / SHA256 `047EDC3E…`）；配置迁移 `docs/doorbreach-config-migration-2026-10-25.md`；profile 备份 `Default_pre-doorbreach-0.1.0-20261025`；BB 未就绪项为 `pending-mods-v0.1.0.md` 第 27/28 项（Level C 通过后再标记）。

## 六、环境坑 / 已知基础设施问题

- **@oracle 模型已弃用**：matt-bridge 预设的 oracle 使用 `opencode-go/gemini-4-argon-contributor`；本会话复用 ora-1 与全新会话两次均 `ModelDeprecated`。未改用户配置。下次审查前需二选一：① 把 oracle 换到可用模型（如 `deepseek/deepseek-v4-pro` 或 `kimi-code-plan-cn/k3`）并重启 OpenCode；② 继续主会话双轴自审并记录。**不要**反复重试旧 oracle 会话。
- 旧交接记录的后台 subagent `unknown certificate verification error` 仍可能复现；优先主会话或前台。
- BepInEx 磁盘日志级别为 Info（`BepInEx\config\BepInEx.cfg`），Debug 判定行不落盘；06 已加 Info 诊断行，足够断言。不要为此改全局日志级别。
- 游戏运行中覆盖层 DLL 被锁；部署前确认 `EscapeFromTarkov` / `SPT.Server` / `SPT.Launcher` 退出（MO2 可留）。
- MO2 MCP 会话可能漂移：先 `mo2_session({mo2Root:"E:\\Game\\EFT_Offline\\Norvinsk_Forever"})`；`mo2_install` 整数 priority 不生效，装后用 `mo2_send_mod_to`。
- `Fontaine-SPTRealismCommon-Lib` 的 `ArgumentOutOfRangeException` 是包内既有噪声；不要把新异常混进去。
- 本轮所有改动未提交；**不要擅自 commit**。
- 当前游戏可能仍在运行；部署前先查进程。

## 七、新会话首条消息怎么开始

1. 读本文件 → 读 06/07 工单 Comments（含 Level C 清单与 07 的 10 步 E2E）。
2. 查 `EscapeFromTarkov` / `SPT.Launcher` / `SPT.Server` 进程；若在运行，先请 Overseer 退出。
3. 确认部署 07（已完成：覆盖层 = `4FF4DB6F…` / 175,616 B；备份在 `.artifacts\doorbreach-ticket09-backup\2026-10-25-07-deployed\`）→ 请 Overseer 经 MO2 启动 → 读日志断言。
4. 按 06 三轮 + 07/08 十步做 Level C；agent 读 `BepInEx\LogOutput.log` 的 `[Destruction]` / `[Policy]` / `[C4]` 行断言并记录偏差。
5. 通过后做 09 收口（BB 第 27/28 项、dev-log、spec 风险面）；若审查需要 @oracle，先解决第六节的模型弃用问题。

## 八、Suggested skills

- `implement` / `implement-spec` —— 09 部署收口
- `tdd` —— 若 09 仍要扩 S0
- `code-review` / `@oracle`（需先修模型）—— 收口审查
- `diagnosing-spt-problems`、`spt-conflict-audit`、`spt-mcp-automation` —— 任务/撤离门误伤、MO2/日志断言
- `handoff` —— 下次会话边界
