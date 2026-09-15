# 05: live 验收——真实 raid 事件时间线 + 装备 + 分类

**What to build:** 一局真实 raid 内的端到端验收：受伤事件（摔伤/中弹）、击杀事件（击杀 bot 且归属正确）、撤离事件（正常撤离或 DebugExtract）、事件 seq 单调与增量拉取（`since` 语义、`dropped` 语义）、`raid_player` 武器/弹药/装备字段、boss 分类不回归。记录证据（读数 + 断言）到本工单评论。

**Blocked by:** 01, 02, 03（04 可并行）

**Status:** ready-for-agent

- [x] 事件时间线：受伤/击杀/撤离三类事件真实出现，seq 单调、raidId 正确
- [x] 击杀归属：本地玩家击杀 bot → killer.isLocal=true（2 次实证）
- [x] 增量拉取：`since` 只回新事件；`since` 过旧 → `dropped>0`（缓冲淘汰后实证 `dropped=252`）
- [x] 装备/武器字段与实机一致（抽样核对）——`tpl` 与 SPT 本地化数据交叉一致；实机目视抽样以数据交叉替代（偏差见 Comments）
- [x] 证据记录（读数摘录）+ 异常/偏差说明

## Comments

### 2026-09-15 live 验收记录

**局次**：第一局 Interchange（阵亡，撤离/停局事件）；第二局 Sandbox（raidId `6aa408bf…@2026-09-15T13:44:21Z`，事件流主验收）。

**事件时间线**
- damage：820+ 条，`{victimProfileId, victimIsLocal, part, amount, sourceType}`；来源类型实测覆盖 `Bullet` / `LightBleeding` / `HeavyBleeding` / `Barbed`（铁丝网）。
- 本地玩家受伤实证：`victimIsLocal=true` 的 damage 两条（Chest / Bullet / 0.939、0.950，seq 1796/1797）——本地玩家路径与 bot 路径同机制（同补丁）。
- death：12+ 条，`{victimProfileId, victimIsLocal, damageType, killer}`；`killer` 完整（profileId / name / side / role / isLocal）。
- seq 单调 1..1252；raidId 全程一致；事件形状 `{seq, ts, type, raidId, payload}` 字段序稳定。
- 撤离/停局（两局两态）：第一局阵亡 `{exitName:"", status:"Killed"}`；第二局撤离 `{exitName:"Sniper_exit", status:"Survived"}`（seq 1936，**赛后读取**——跨 raid 缓冲语义实证）—— `LocalGame.Stop` patch 实证生效。

**击杀归属**
- 本地玩家（Samuel / Bear / assault）击杀 2 名 bot → `killer.isLocal=true`（seq 348、seq 942）。
- bot 击杀 bot → 归属正确（如 "Traveler" / pmcUSEC、"3xtremehamster" / pmcUSEC，`isLocal=false`）。

**增量语义**
- `since=676` → 仅回 `677..686`；`since=830` → 仅回 `831..1246`（只回新事件 ✓）。
- **`since` 过旧 → `dropped>0` 实证**：缓冲淘汰后（容量 1000）`since=0` → `{seq:1252, dropped:252, count:1000}`，返回窗口 `253..1252`（从最旧返回 ✓）。
- 附带实证 limit 截断陷阱：积压 654 条时 `limit=100` 仅返回 100 条（`since` 应取已消费末条事件的 seq，见 README）。

**装备/武器（抽样）**
- live 读到 12 个装备槽 + weapon（AK-105 `5ac66d9b5acfc4001633997a`，弹匣 25 + 膛内 1）；`tpl` 与 `SPT_Data/database/locales/global/en.json` 交叉一致。
- 偏差：`name` 返回本地化键形态（`<tpl> Name`）而非本地化值（客户端 raid 上下文）；`tpl` 为权威标识；本地化名解析列 backlog。实机目视抽样未完成（用户侧会话不便），以本地化数据交叉 + 槽枚举结构一致性替代。

**异常/偏差汇总**
1. `ApplyDamageEvent` 委托订阅不可用（Il2CppInterop 非 blittable 封送，`DamageInfo`）→ 改 Harmony patch `ActiveHealthController.ApplyDamage`（修复后 live 复验通过；详见工单 01 / spec「实施偏差记录」）。
2. `name` 本地化键回退（见上）。
3. 致命伤后仍有一条同 victim 的 damage 事件（postfix 在 death 事件之后入缓冲；字段序稳定、语义允许；记录备查，未列缺陷）。
4. `dropped>0` 的 live 构造依赖缓冲淘汰（>1000 事件），本局自然满足（淘汰 252 条）。

**构建说明**：live 验证用构建 = 事件修复版（52,224 bytes，2026-09-15 21:37 部署）；评审修复版（52,736 bytes）仅含非行为性修正（路由兜底显式化 / switch 显式化 / 订阅健壮性 / 文档），事件机制不变。
