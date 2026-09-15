# tarkov-runtime-MCP 第二波 — 事件流 + 装备状态 + 小项 Spec

Status: ready-for-agent

## Problem Statement

Phase 2 已交付局内状态**快照**（玩家 / raid 元数据 / bot），但**事件维度**缺失：击杀、死亡、受伤、撤离这类"发生了什么"只能靠快照差分猜测（会漏、会迟），bot mod 验证与 raid 冒烟断言需要可靠的事件时间线。同时缺**装备/武器状态**（当前武器与弹药）。另有三个收尾小项：boss 判定表、`getInfo` 缓存策略、CLI-007 标准校准。

## Solution

桥侧新增**事件捕获 + 环形缓冲 + 增量拉取**（沿用 ADR-0007 拉取模型，不引入推送通道）：

- **捕获**：现成 C# 事件为主——`IHealthController.ApplyDamageEvent(EBodyPart, float, DamageInfo)`（受伤）、`HealthChangedEvent`（同签名）、`DiedEvent(EDamageType)`（死亡）；`GameWorld.OnPersonAdd(IPlayer)` 用于给新玩家挂订阅（内部机制，不单独出事件）。撤离检测走 spike（`AbstractGame.Stop` 链）。
- **缓冲**：环形缓冲（容量 1000，seq 单调递增），事件带 `raidId`；`GET /raid/events?since=<seq>&limit=<n>` 返回增量 + `dropped` 计数。
- **MCP 侧**：新增 `raid_events` 工具（since/limit），`raid_player` 扩展装备/武器字段。
- **小项**：boss 判定表扩展（`sectantpriest` 等）；`getInfo` 改为每次调用拉取；CLI-007 标准与检查器同步 5.0 `Unload()` 形态并移除桥豁免。

## User Stories

1. As a bot mod 开发者, I want 读取击杀事件（谁杀谁、本地玩家是否参与）, so that 我能断言 bot 战斗行为
2. As a test 编写者, I want 读取受伤事件（部位/伤害量/来源类型）, so that 我能验证伤害类 mod
3. As a test 编写者, I want 读取死亡与撤离事件, so that 我能断言 raid 生命周期
4. As a test 编写者, I want 事件带单调 seq 与增量拉取, so that 我不用轮询全量、不丢事件
5. As a test 编写者, I want 事件带 raidId, so that 跨 raid 的事件不混淆
6. As a 维护者, I want `dropped` 计数, so that 我能知道 since 过旧导致的事件丢失
7. As a test 编写者, I want `raid_player` 带当前武器/弹药, so that 我能验证武器类 mod
8. As a test 编写者, I want 装备槽摘要（头盔/护甲/背包 tpl）, so that 我能验证装备类 mod
9. As a bot mod 开发者, I want boss 族判定覆盖 `sectantPriest` 等, so that 分类计数准确
10. As a 维护者, I want `getInfo` 每次调用拉取, so that 桥升级后协议校验即时生效
11. As a 维护者, I want CLI-007 标准文本与检查器同步 5.0 `Unload()` 形态, so that 豁免不再是必需
12. As a 未来开发者, I want 事件 schema 确定性（字段序稳定、可 diff）, so that 可做金样本断言

## Implementation Decisions

1. **事件捕获（桥侧）**：订阅 `IHealthController.ApplyDamageEvent` / `DiedEvent`（现成 C# 事件，无需 Harmony）；新玩家经 `GameWorld.OnPersonAdd` 挂订阅。撤离检测 spike（候选：Harmony patch `AbstractGame.Stop`；备选：`GameStatus` 转移）。**击杀归属**：由受伤事件维护 `victimProfileId → lastDamager` 映射（含时间窗），死亡时成对输出；不可得时为 `null`（不猜）。
2. **事件模型**（字段序稳定）：`{seq, ts(UTC ISO), type, raidId, payload}`；`type ∈ {damage, death, extraction}`：
   - `damage`: `{victimProfileId, victimIsLocal, part, amount, sourceType}`
   - `death`: `{victimProfileId, victimIsLocal, damageType, killer: {profileId,name,side,role,isLocal}|null}`（**击杀 = death 且 killer != null**，不重复出事件）
   - `extraction`: `{exitName, status}`（本地玩家；spike 后定字段）
3. **缓冲与拉取**：环形缓冲 1000；seq 桥进程内单调；`GET /raid/events?since=<seq>&limit=<n>` → `{inRaid, seq, dropped, events:[...]}`；since 过旧 → 从最旧返回且 `dropped>0`；缓冲跨 raid 保留（事件带 raidId），环形自然淘汰。**`/raid/events` 在非 raid 时仍返回缓冲**（`inRaid` 仅为状态字段）——赛后时间线读取（含撤离事件）依赖此语义。
4. **MCP 工具**：`raid_events`（`since?: number`、`limit?: number`）；`wait_for` 可对其求值（如 `seq > N`）。**`raid_events` 不返回 `NOT_IN_RAID`**：非 raid 时照常返回 `{inRaid:false, seq, dropped, events}`；仅桥不可达/协议不符为错误。`extraction` 载荷解析保持宽容（字段缺失归一，不因 spike 字段差异抛错）。
5. **装备/武器**（`/raid/player` 扩展）：`weapon: {tpl, name, ammoInMag, ammoInChamber}|null`（源：`Player.HandsController` → `IFirearmHandsController.Item` + 弹药计数，成员 spike）；`equipment: [{slot, tpl, name}]`（源：`Profile.Inventory.Equipment` 槽枚举，spike）。
6. **boss 判定表**：role（忽略大小写）含 `boss` **或** 命中显式表（`sectantpriest`）→ boss；表为桥侧单点常量 + 单测。
7. **getInfo**：去掉缓存，每次工具调用拉取（本地 HTTP，成本可忽略；桥升级后即时校验）。
8. **CLI-007 校准**：`05-client.md` STD-CLI-007 文本改为「5.0：`Unload()`（BasePlugin）/ `Dispose()`（组件）」；`check-mod-standard.ps1` 接受 `Unload()` 为 5.0 撤销路径；移除 `tools/tarkov-runtime-bridge/MODDING-STD-WAIVER.md` 的 CLI-007 行并复跑机检。

## Testing Decisions

- 好测试：只测外部行为（事件 schema / seq 与 dropped 语义 / 装备字段 / 分类表），fixture 驱动、确定性字段序。
- 接缝：复用 `BridgeConnection`（新增 `getRaidEvents`）+ 桥侧纯逻辑类（事件缓冲、归属映射、分类表、JSON 可独立单测）；**不新增接缝**。
- Prior art：桥侧 xunit（85）与 MCP vitest（214）的既有模式；录制/回放 fixture 可扩事件样本。
- Live 验收：一局 raid 内制造受伤/击杀（对 bot 开火或摔伤），撤出（正常撤离或 DebugExtract），断言事件时间线（seq 单调、类型与归属正确）。

## Out of Scope

- 推送 / WebSocket（ADR-0007 维持拉取）；枪声/投掷/拾取等高频事件；事件跨进程持久化；事件回放 UI。
- bot 行为态深入（SAIN 等 mod 内部状态）仍属远期。

## Further Notes

- 关联：Phase 2 spec（`.scratch/tarkov-runtime-inraid/spec.md`）、ADR-0007、T01 签名报告（`IHealthController` 事件成员已核验：`ApplyDamageEvent`/`HealthChangedEvent` = `Action<EBodyPart, float, DamageInfo>`；`DiedEvent` = `Action<EDamageType>`）。
- 已知 spike 项：`AbstractGame.Stop` 签名与撤离字段、武器弹药成员路径、装备槽枚举路径、`DamageInfo` 的伤害来源链。
- CLI-007 校准涉及 `knowledge/spt-kb/curated/modding-standard/05-client.md` 与 `scripts/check-mod-standard.ps1`（版本矩阵同步为加分项）。

## 实施偏差记录（2026-09-15，live 驱动）

- **受伤捕获机制变更**：原方案「订阅现成 C# 事件 `IHealthController.ApplyDamageEvent`」在 IL2CPP 下不可行——`DamageInfo` 为非 blittable struct，Il2CppInterop 的 `DelegateSupport.ConvertDelegate` 拒绝封送（live 日志实锤），且异常连带跳过同块的 `DiedEvent` 订阅。**实际实现**：Harmony patch `ActiveHealthController.ApplyDamage(EBodyPart, float, DamageInfo)`（prefix 记录归属 / postfix 输出事件；生态 6+ mod 先例，方法 non-virtual 无子类覆盖），`DiedEvent` 订阅独立隔离。事件 schema 与字段序不变。
- **撤离检测落地**：spike 结论为 `EFT.LocalGame.Stop(string, ExitStatus, string, float)`（spec 候选 `AbstractGame.Stop` 不存在）；postfix 补丁 live 实证生效（阵亡落 `{exitName:"", status:"Killed"}`）。
- **`name` 字段偏差**：武器/装备的 `name` 在客户端 raid 上下文返回本地化键形态（`<tpl> Name`）而非本地化值；`tpl` 为权威标识；本地化名解析列 backlog。
- **`since` 用法精确化**：`since` 应取**已消费的最后一条事件的 `seq`**（响应 `seq` 为最新序号，仅用于判断是否有新事件；`limit` 截断时用本次返回最后一条事件的 `seq` 续拉）——live 实测 limit 截断真实发生（积压 654 条时 `limit=100` 仅返回 100 条）。
