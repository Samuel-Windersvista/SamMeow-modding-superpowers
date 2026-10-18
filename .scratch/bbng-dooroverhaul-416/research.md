# BB-NG × Sage's Door Overhaul 合并移植研究（SPT 4.1.6）

- 日期：2026-10-17
- 目标：把 Backdoor Bandit（下称 BB）移植到 SPT 4.1.6，并在其基础上并入 Sage's Door Overhaul（下称 DO）的门破坏功能与门操作功能，供《永远的诺文斯克》4.1.6 包使用
- 研究方式：3 路并行侦察（源码机制 / 官方迁移知识 / 外部生态）+ 发行包解包实证 + 本机环境核查
- 状态：研究完成，待决策后进入 spec

---

## 0. 结论速览

1. **"移植"的量比预期小得多**：BB-NG 作者已在 master 完成 4.1 API 迁移（commit `451c6cb "4.1"`，2026-10-13），只是没发 release。本地克隆 `external/references/backdoorbandit-ng` 的 HEAD 即该提交，全仓无混淆残留（`GClass*`/`DamageInfoStruct`/`GetActionsClass` 零命中）。
2. **剩余工作**：重建构建工程（作者把主 csproj/build.ps1 从仓库排除了）→ 用本机 4.1.6 程序集编译 → ilspycmd 触点核验 → 修已知缺陷（配置链路）→ 服务端组件策略决策。
3. **真正的难点在"合并"**：BB 与 DO 有三处硬交界——交互菜单同一 patch 点（`InteractionContextHelper.GetAvailableActions` 的 Door 重载 postfix）、命中链双观察者（`BallisticCollider.ApplyHit` vs `collider.OnHitAction`）、门状态与锁语义冲突（BB：锁门默认可破仅靠 KeyId 黑名单；DO：锁门豁免破坏——正是要移除的）。合并 = 统一为一套管线，属于新开发项目而非单纯移植。
4. **依赖与工具链在本机已全部就绪**：WTT-ClientCommonLib 3.0.6（BB 硬依赖）已在包内；.NET SDK 10.0.300、ilspycmd、7-Zip、SPT 4.1.6 引用程序集（EFT 0.16.9.40743）齐备。
5. **建议分阶段**：阶段 1 = 共存版（BB-NG 4.1 + DO 去豁免小改，双 mod 并行进包验证）；阶段 2 = 单 mod 合并（本命目标）。可随时由上游 2.0.5+ 发布调整策略。

---

## 1. 需求背景（Overseer 口述）

- 不喜欢 DO 的"锁门豁免破坏"（不攻击上锁门是设计硬规则，无开关）；主张破门方法由 mod 提供、是否用钥匙由玩家决定，参照 Backdoor Bandit。
- 方案：以 BB 为基座移植到 4.1.6，在其基础上并入 DO 的门破坏（弹孔/铰链/门闩/门板脱落）与门操作（Peek / Open Further / Open Quietly / 推门 / 踢门持枪等）。
- 3.11 时代的既有基线（《生活在诺文斯克》）：BB 1.11.2（dvize 系 3.10-port 更新版，hub file 2575，meta 注释"整合包默认所有武器可破门，已汉化"）+ DoorBreacher 服务端组件；4.1.6 包中 BB 客户端/服务器文件均在未就绪清单。

---

## 2. Backdoor Bandit 谱系与现状

| 世代 | 作者/仓库 | 最新版本 | SPT 目标 | 状态 | 许可 |
|---|---|---|---|---|---|
| 原版 | dvize / `github.com/dvize/BackdoorBandit` | 1.9.0（2024-07-08） | ~3.9.0 | 停更（最后 push 2024-07-11） | MIT |
| 3.10/3.11 更新版 | v-engelowski / `github.com/v-engelowski/backdoorbandit`（旧名 backdoorbandit-3.10） | 1.11.2（2025-05-13） | ~3.11.0 | 停更 | MIT |
| **BB-NG** | kobethuy / `github.com/thuynguyentrungdang/backdoorbandit` | 2.0.4（2026-01-08，Forge mod 2443 标 ~4.0.0/4.0.13） | ~4.0.0 | **活跃**：master 已有 4.1 迁移提交（未发版） | MIT（LICENSE.txt 为占位符式版权行） |

**author 的 4.1 提交（`451c6cb`，2026-10-13）关键内容**（GitHub 提交页：13 文件 +107/-45）：
- 引用路径整批改指作者本机 `D:\ref41\`；主工程与 Fika 子工程都在编译目标内。
- 反混淆改名已做：`DamageInfoStruct`→`DamageInfo`、`GetActionsClass.smethod_14/13`→`InteractionContextHelper.GetAvailableActions`、`ActionsReturnClass`→`AvailableInteractionState`、`ActionsTypesClass`→`InteractionAction`、`ItemFactoryClass`→`ItemFactory`、`GStruct153`→`OperationResult`（`ItemManipulator.Discard`）、`NotificationManagerClass`→`NotificationManager`。
- 版本位：主插件 2.0.4、Fika 插件 1.0.4。
- 无任何 4.1 release、Forge 无 4.1 版本；社区无第三方适配 fork。

**GUID 事实**：源码 `Plugin.cs:12` 与实包 DLL 均为 `com.dvize.BackdoorBandit`（Forge 页面元数据曾报 `com.rairaitheraichu.backdoorbandit`，与实包不符，按元数据错误处理）。

---

## 3. BB-NG 机制解剖（基于 master 源码）

### 3.1 结构与关键文件

| 路径 | 作用 |
|---|---|
| `Plugin.cs` | 入口；从服务器拉配置（`/backdoorbandit/load`）；启用 5 个 patch；展开静态配置字段 |
| `DoorBreachComponent.cs` | Raid 开始时给全图 Door / LootableContainer / Trunk 挂自建 `Hitpoints` 血量组件；维护武器白名单 |
| `ExplosiveBreachComponent.cs` | C4 流程：放置、倒计时蜂鸣、爆炸特效/伤害、强制开门、`Destroy(door.gameObject)` |
| `DamageUtility.cs` | 命中合法性判定链（模式/材质/武器/弹药/命中点锚定/黑名单） |
| `Models/{ModConfig,HitPoints,C4Instance}.cs` | 服务端配置 POCO；血量组件；已放置 C4 运行时句柄 |
| `Patches/` | 见下表 |
| `Fika/FikaBridge.cs` | 主插件侧无 Fika 依赖的事件桥 |
| `BackdoorBanditFika/` | Fika COOP 伴生插件（独立 csproj，发行为 FikaAddon） |
| `server files/` | **旧世代遗留快照**（TS DoorBreacher + BepInEx 载荷），与实际发布不一致（见 §4） |
| `VersionChecker/` | dvize 系 TarkovVersion 检查器（**死代码**：`CheckEftVersion` 全仓无调用） |
| `additional_files/` | 4 张武器白名单 JSON + 2 个蜂鸣音效（发布包资源副本） |

### 3.2 Harmony patch 总表（主工程 5 + Fika 1）

| # | 目标 | 类型 | 位置 |
|---|---|---|---|
| 1 | `BallisticCollider.ApplyHit(DamageInfo)` | Postfix | `Patches/ApplyHitPatch.cs:30` |
| 2 | `InteractionContextHelper.GetAvailableActions`（Door 重载，加 "Plant Explosive"） | Postfix | `Patches/ActionMenuDoorPatch.cs:13` |
| 3 | 同上（KeyCard 重载变体，带去重守卫） | Postfix | `Patches/ActionMenuKeyCardPatch.cs:14` |
| 4 | `GameWorld.OnGameStarted`（挂两个 Component；跳过 batchmode） | Postfix | `Patches/OnGameStartedPatch.cs:10` |
| 5 | `PerfectCullingBakeGroup.Toggle`（Prefix 整方法替换，修门销毁后 NRE） | Prefix | `Patches/PerfectCullingNullRefPatch.cs:12` |
| 6 | `RemoveItemFromPlayerInventory`（仅 Fika 子插件） | Prefix | `BackdoorBanditFika/Patches/...:16` |

### 3.3 判定链（射击/近战/爆炸 破门与容器）

1. **铺场**：`DoorBreachComponent.Awake` 遍历 `Door/LootableContainer/Trunk`（Interactive 层、可操作、状态合法），挂 `Hitpoints` 并赋随机血量 `Random.Range(MinHitPoints, MaxHitPoints)`。
2. **命中**：`ApplyHit` postfix 仅受理**本机玩家**射击；排除 Lamp/Window 与 Explosion 伤害类型。
3. **合法性**（`DamageUtility.cs`）：PlebMode 全放行；SemiPlebMode 非金属门全放行；正常模式按近战/枪弹/破片/自定义破门弹/白名单弹药 + 武器白名单判定；金属门需 `BreachingRoundOpensMetalDoors`；**命中点锚定**：门需距 `DoorHandle` <0.25m（无 Handle 则任意命中），后备箱走 `CarLock_Hand/Lock`，容器走 `Lock` 子物体。
4. **黑名单**：`BlacklistedRooms`（按 `door.KeyId`）拒绝——**没有"是否有钥匙"检查，锁门默认可破**。
5. **结算**：`hitpoints -= damage`；无单发阈值、无概率、确定性累积。
6. **开门**：HP≤0 → `DoorState=Shut` → 临时 `interactWithoutAnimation=true` → `player.CurrentManagedState.ExecuteDoorInteraction(door, InteractionResult(Breach))`（原生踹门动画）→ 恢复；容器：先 `Open()` 解锁再 `ExecuteDoorInteraction(Open)`。
7. **C4 路线**：交互菜单 "Plant Explosive"（门状态门槛 + 背包有 C4）→ 放置 → 倒计时 → `Effects.EmitGrenade("big_explosion")` + 可选范围伤害 → **`Destroy(door.gameObject)`（门直接消失，无残骸）**；`PerfectCullingNullRefPatch` 是该行为的配套防护。

### 3.4 配置模型

- 载体：**服务端下发**（客户端 `Awake` 内 `RequestHandler.GetJsonAsync("/backdoorbandit/load")`）；失败返回 null → 后续读取会 NRE（async void 内无二次防护）。
- 字段：4 张武器白名单、`BlacklistedRooms`（KeyId）、`WhitelistedRounds`、`PlebMode`、`SemiPlebMode`、`BreachingRoundOpensMetalDoors`、`OpenLootableContainers`、`OpenCarDoors`、`MinHitPoints/MaxHitPoints`、`ExplosiveTimerInSec`、`ModExplosionStats`。
- 实包默认值（`config.json`，解包所得）：shotgun 11 个模板 id、grenadeLauncher 6 个、melee 5 个、other 空；blacklistedRooms 12 个 KeyId；whitelistedRounds 3 个；全部开关默认 false；血量 100–200；C4 计时 10s；爆炸默认不伤人（半径 5 / 伤害 80）。
- 无"按门类型/材质/地图"的粒度；定制基本都要扩展这套模型。

### 3.5 破坏视觉现状

**没有弹孔/门板破损/碎片**。射击路线 = 原生踹门动画；C4 路线 = 通用大爆炸特效 + 门体 Destroy。DO 的破坏系统（弹孔网格、断片、铰链脱落、门闩）正是需要"补进来"的部分。

---

## 4. 2.0.4 实包构成（解包实证，2026-10-17）

```
SPT\user\mods\BackdoorBanditServer\
  BackdoorBanditServer.dll        (18432 B, sha256 F3B07863...FAA08)  ← C# 服务端 mod
  bundles\C4Explosive.bundle      (2147196 B)
  bundles.json / config\config.json / db\CustomHideoutRecipes\C4.json / db\CustomItems\C4Explosive.json
BepInEx\plugins\BackdoorBanditClient\
  DoorBreach.dll                  (41984 B, sha256 AC47C445...B6C90)  ← 客户端插件（发布名）
  audio\Beep.mp3 / audio\FinalBeepTone.mp3
```

要点：
- **实际发布的服务端组件已是 C#**（`BackdoorBanditServer.dll`，字符串含 `AbstractModMetadata`、`/backdoorbandit/load`、`OnLoad`）；仓库里的 TS `DoorBreacher` 是旧世代遗留，两份不是一回事。
- **服务端源码不在仓库**（主 csproj、build.ps1、服务端工程均被 .gitignore 或未公开）。4.1.6 服务端有两种选择：按 `Server_40_to_41` 指南重建小体量 C# 服务端（其职责仅：注册 C4 物品模板 + bundle + 配方 + 提供 `/backdoorbandit/load` 配置端点），或客户端改为本地配置后裁掉 C4/破门弹功能。
- 客户端硬编码依赖的自定义物品 id：C4 `6636606320e842b50084e51a`（现役功能）；破门弹 `660249a0712c1005a4a3ab41`（作者已于 2.0.0 移除该物品，客户端残留引用为死分支，无实际影响）。

---

## 5. 4.0 → 4.1 迁移差量（官方知识 × 作者已完成项）

### 5.1 官方要点（KB：`wiki/SPT_41/modding/Client_40_to_41.md`、`Class_Name_Mappings.md`、`Server_40_to_41.md`）

- 客户端核心 = 反混淆改名：4.1 类型有真名 + 真命名空间；**所有 4.0 mod 必须重编译**；改名表约 9240 条；替换后补 `using`。
- 与本项目相关映射（部分）：`DamageInfoStruct→EFT.Ballistics.DamageInfo`；`GetActionsClass.smethod_8/9→InteractionContextHelper.GetAvailableActions/GetAvailableInteractionState`；`ActionsReturnClass→EFT.UI.AvailableInteractionState`；`ActionsTypesClass→EFT.UI.InteractionAction`；`ExplosiveHitArmorColliderStruct→EFT.ExplosionDamageInfo`；门家族嵌套类型一批（`EFT.Interactive.*`）。
- 4.1.4+：一批序列化字段名回改（按字符串反射的 mod 需手工替换）——门/弹道无相关条目。
- 服务端 4.0→4.1：`AbstractModMetadata`→`IModMetadata`；`OnLoad/OnUpdate`→`OnLoadAsync/OnUpdateAsync(+CancellationToken)`；表/配置注入化（`DatabaseServer` 等不存在）；命名空间搬家；`ISptLogger`/LogLevel 变化；bundle 检测改 `bundles.json`；Patching 走 DI 等。
- 版本门禁基准：SPT 4.1.x = EFT `0.16.9.40743`（与本机实测一致）。

### 5.2 作者已完成（master）与剩余核验

- 已完成：上述客户端改名全量（我方 grep 复核：`GetActionsClass`/`DamageInfoStruct`/`GClass`/`vmethod_` = 0 命中）。
- 剩余核验（建议 ilspycmd 触点表，按 KB `pilot-experience-spt41-debugtoolkit` 方法）：`BallisticCollider.ApplyHit(DamageInfo)`、`InteractionContextHelper.GetAvailableActions(GamePlayerOwner, Door)`、`Door.IsBreachAngle`、`Door.KickOpen`、`ExecuteDoorInteraction`、`PerfectCullingBakeGroup.Toggle`、WTT 依赖 API 在 4.1.6 的签名。
- 已知缺陷/债（移植时一并处理）：
  1) 配置 HTTP 依赖 + null NRE 隐患 → 建议改本地 BepInEx 配置（或本地缓存 + 强校验）；
  2) `Properties/AssemblyInfo.cs` 仍是 3.11 时代值（`TarkovVersion(35392)`、AssemblyVersion 1.11.2）——因 `CheckEftVersion` 是死代码，无运行时影响，但发布前应刷新；
  3) 仓库内 audio 路径（`additional_files/`）与运行时路径（`BackdoorBanditClient/audio/`）不一致——打包脚本需按实包布局；
  4) Fika 子工程按目标 Fika 版本重编（单机可整体不装）。

---

## 6. 与 DO 的合并分析（技术交界）

| 交界 | BB-NG | DO | 合并策略 |
|---|---|---|---|
| 交互菜单 | `InteractionContextHelper.GetAvailableActions(GamePlayerOwner, Door)` postfix ×2（加 "Plant Explosive"） | **同一方法 postfix**（`Patches/AddDoorActionsPatch.cs`，追加 Peek/Open Further/Open Quietly） | 合并为单 postfix 链；动作去重守卫已有先例（BB 的 `IsActionAdded`）；顺序需显式控制 |
| 命中链 | `BallisticCollider.ApplyHit` postfix（消耗伤害→HP 池） | `collider.OnHitAction` 订阅（破坏模拟；`DoorDamageIntake.cs:158` 锁门豁免=要移除） | 统一观察者：一击同时喂 BB HP 与 DO 破坏模拟；锁门豁免删除后锁门获得完整破坏 |
| 门状态/锁 | 锁门默认可破（KeyId 黑名单制）；开门惯用法 `DoorState=Shut + Breach 交互 + interactWithoutAnimation` | 尊重 `EDoorState.Locked`；自建破坏状态机（FractureState / Takeover / LatchEngaged） | 定义唯一状态权威：建议 BB 的 HP 归零改为驱动 DO 的完整破坏（门板脱落）而非直接 Destroy；C4 同理对准毁伤管线 |
| 破坏视觉 | 无（仅大爆炸特效） | 全套（弹孔/断片/铰链/门闩/门扇脱落，含实验开关） | 保留 DO 视觉系统为主 |
| 门体销毁防护 | `PerfectCullingNullRefPatch`（门 Destroy 后 NRE） | 无此问题（保留门对象） | 若合并后仍可能 Destroy 门（C4 路线），保留该补丁 |
| 配置 | 服务端下发 + 4 张白名单 JSON + 开关 | 本地 F12（BepInEx config，分 5 节） | 统一本地配置（利于"玩家自己决定"的定制粒度）；服务端仅保留物品体系（若保留 C4） |
| 容器/后备箱 | 有（`OpenLootableContainers`/`OpenCarDoors`） | 无（只管 Door） | 保留 BB 侧能力 |
| Fika | 有伴生插件+事件桥 | 检测到 Fika 时整体自关（破坏未同步） | 自用单机可双方都裁 Fika；若未来联机再评估 |

**3.11 冲突前史参考**：BB 曾与 Raid Overhaul 在 `GetActionsClass.smethod_14`（门交互另一目标）有中危 IL 碰撞；与 DoorRandomizer 等 23 个插件同在 `GameWorld.OnGameStarted` 订阅组（事件型，设计允许）。4.1 中这些混淆方法名已改真名，冲突点需重新按实装定位。

---

## 7. 本机环境与依赖事实

- SPT 4.1.6 @ `E:\Game\EFT_Offline\SPT_41x`（EFT `0.16.9.40743`，`SPT_Runtime` 布局）；4.1.6 为社区续命线（SP-Tushonka/build，2026-09-18），4.1 线最新。
- 构建链：.NET SDK 10.0.300 / 5.0.408；`ilspycmd` 已装；7-Zip 26.02；引用程序集（Assembly-CSharp / BepInEx / 0Harmony / spt-reflection 等）齐全。
- 包内现状（《永远的诺文斯克》）：DO v1.0.0 已装（含实验铰链开启、[15] 叠层存 F12 配置）；DoorRandomizer 已装；**WTT-ClientCommonLib 3.0.6 已装（BB 硬依赖满足）**；BB 客户端/服务端在未就绪清单候补。
- 上游 watch：BB-NG 发版动态（master 即 4.1，随时可能发 2.0.5+；若上游发版，优先以其为基座，减少自维护面）。

---

## 8. 风险清单

1. **上游发版时机**：若作者近期发 4.1 release，自建移植可降级为"以其 release 为基座做合并"；策略需可切换。
2. **双状态机合并复杂度**：BB HP 池与 DO 破坏模拟是两套独立"门耐久"，合并语义（何时算"破开"、何时"打烂"）需在 spec 敲定；这是本项目主要不确定性。
3. **服务端重建**：C4/破门弹物品体系需 4.1 服务端组件（源码不在仓库，体量小但需重建 + bundle 兼容验证——`bundle-compat-311-to-41.md` 判断内置组件安全）。
4. **未验证区**：4.1.6 运行时实测（编译通过≠运行正常，需按验证链走）；BB×DO 共存无任何公开先例。
5. **交互菜单挤占**：双 mod 同点 postfix + 包内其他门 mod（DoorRandomizer 等）叠加后的菜单行为需实测。
6. **合规**：MIT（BB）× BSD-3（DO）合并可；保留双方 LICENSE 与署名（BB-NG 的 LICENSE 版权行为占位符，建议补注 dvize/v-engelowski/kobethuy）。
7. **维护面**：合并版跟随两上游演进的成本高于单上游；建议 fork 仓库自持、上游更新时 cherry-pick 关键修复。

---

## 9. 建议路径与工作量预估

| 阶段 | 内容 | 预估 |
|---|---|---|
| 0 立即可做 | 重建 `BackdoorBandit.csproj`（以 Fika 子工程 csproj + DO csproj 为模板，引用指本机 4.1.6）；编译 master；ilspycmd 触点核验表；修配置链路 | 半天级 |
| 1 共存版 | BB-NG(4.1) + DO（仅去锁门豁免的小改：`DoorDamageIntake.cs:158/218` 两处）双 mod 进包；实机验证（枪破锁门、DO 破坏、菜单共存、C4） | 1–2 天含测试 |
| 2 合并版（本命） | 单 mod 融合：统一命中管线、交互菜单、状态权威、配置体系；保留 DO 视觉破坏 + BB 破门语义 | 多天，走 spec→tickets→逐单实现 |
| 备选 | 等上游 4.1 release 后以其为基座做合并 | 取决于上游 |

---

## 10. 待决策清单（进入 spec/grilling 时敲定）

1. **形态**：直接做合并版，还是共存版先行再合并？
2. **C4/破门弹**：保留（需 4.1 服务端重建）还是首期裁掉（纯客户端枪/近战破门）？
3. **配置载体**：统一本地 BepInEx 配置（推荐）还是维持服务端下发？
4. **Fika**：首期是否需要？单机自用可裁。
5. **发布形态**：仅自用进包，还是做成可发布 fork（影响署名/文档/仓库管理）？
6. **DO 功能取舍**：全量并入（含实验铰链/门扇脱落 + Peek/静开/推门/踢门持枪）还是分模块渐进？
7. **物品体系**：若保留 C4，是否同时保留 3.11 时代的 DoorBreacher 类物品（或被 BB 自带体系取代）？

---

## 附录 A：证据索引

- BB-NG 仓库（master@451c6cb）：`https://github.com/thuynguyentrungdang/backdoorbandit`；提交：`/commit/451c6cb36bbd98f516ca8c1971e9f91fab274b77`
- BB-NG releases：2.0.4 资产 `BackdoorBandit-4.0_2.0.4.7z`（2219974 B）；FikaAddon-1.0.4
- Forge：BB-NG `sp-mod.com/mod/2443`；WTT-CommonLib `sp-mod.com/mod/2310`（3.0.6，~4.1.3）；DO `sp-mod.com/mod/3144`
- 原版/更新版仓库：`github.com/dvize/BackdoorBandit`（PR #5 未合并）、`github.com/v-engelowski/backdoorbandit`
- DO 仓库：`github.com/SageSPT/Door-Overhaul`（v1.0.0，BSD-3；锁门豁免 `Destruction/DoorDamageIntake.cs:158/218`）
- 本地物证：`external/references/backdoorbandit-ng/`（源码克隆）；`D:\Temp\opencode\bbng-2.0.4\`（发行包解包）
- 二进物证：客户端 `DoorBreach.dll` sha256 `AC47C445BE34D4CC389A8B3AAAF537A19055E264795E0D91FBB9B7B7C27B6C90`；服务端 `BackdoorBanditServer.dll` sha256 `F3B078634D1D05FC17FD64123844B179E828B2BFD78379805880FE3F736FAA08`
- KB 依据：`knowledge/spt-kb/wiki/SPT_41/modding/Client_40_to_41.md`、`.../client/Class_Name_Mappings.md`、`.../Server_40_to_41.md`、`curated/migration/client-mod-311-to-41.md`、`curated/operations/3114-il-conflict-report-v2.md`、`curated/migration/pilot-experience-spt41-debugtoolkit.md`
- 包内事实：`E:\Game\EFT_Offline\Norvinsk_Forever\docs\pending-mods-v0.1.0.md`（BB 未就绪）、`...\mods\[8]门的大修-DoorOverhaul\`、`...\mods\[1]前置框架-WTT公共库-WTT-ClientCommonLib\meta.ini`（3.0.6）

## 附录 B：BB-NG 关键源码行号

- `Plugin.cs:12`（`[BepInPlugin("com.dvize.BackdoorBandit", "dvize.BackdoorBandit", "2.0.4")]`）；`:70-84`（HTTP 配置拉取）
- `Patches/ApplyHitPatch.cs:30-49`（ApplyHit postfix）；`:51-59`（本机玩家/命中类型过滤）；`:61-82`（Trunk/LootableContainer/Door 分支）；`:89-91,107-109`（容器/后备箱开关）；`:129-130`（黑名单）；`:153-166`（伤害结算）；`:168-241`（开门/开容器）
- `DamageUtility.cs:14-84`（模式与武器/弹药分派）；`:86-94`（黑名单）；`:126-145`（破片/自定义破门弹 id）；`:152-211`（命中点锚定：DoorHandle/车锁/容器锁）
- `DoorBreachComponent.cs:48-82`（铺场与血量）；`:137-161`（合法性过滤）；`:201-217`（白名单合并）
- `ExplosiveBreachComponent.cs:128-196`（放 C4）；`:261-330`（倒计时）；`:332-404`（爆炸/开门/Destroy）
- `PerfectCullingNullRefPatch.cs:12-63`（整方法替换）
- 未完成项：`server files/`（TS DoorBreacher）为旧快照；`Properties/AssemblyInfo.cs:23`（`TarkovVersion(35392)`旧值）
