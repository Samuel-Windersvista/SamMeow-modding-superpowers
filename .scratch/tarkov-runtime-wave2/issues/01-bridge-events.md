# 01: 桥侧事件流——捕获 + 环形缓冲 + `/raid/events`（含归属映射）

**What to build:** 桥侧事件子系统：订阅现成 C# 事件（`IHealthController.ApplyDamageEvent` / `DiedEvent`；新玩家经 `GameWorld.OnPersonAdd` 挂订阅）捕获受伤与死亡；维护 `victim → lastDamager` 映射产出击杀归属；环形缓冲（1000，seq 单调）经 `GET /raid/events?since=<seq>&limit=<n>` 增量拉取（含 `dropped`）。撤离事件 spike 后落地（候选：`AbstractGame.Stop` patch）。纯逻辑（缓冲/seq/归属/JSON）单测覆盖。

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] `/raid/events?since=&limit=` 契约实现：`{inRaid, seq, dropped, events:[{seq,ts,type,raidId,payload}]}`，字段序稳定
- [x] damage 事件：部位/伤害量/来源类型；death 事件含 `killer`（不可得为 null）
- [x] 归属映射：本地玩家与 bot 的击杀均可归属（时间窗内 lastDamager）；映射有单测
- [x] 撤离事件落地：`LocalGame.Stop` postfix patch（spike：`AbstractGame.Stop` 不存在）
- [x] 纯逻辑单测覆盖：环形缓冲淘汰、seq 单调、since 过旧 → dropped、JSON 确定性
- [x] 构建 0 error；`dotnet test` 全绿（152/152，含既有 85）

## Comments

### 2026-09-15 实施 + live 验收

**落地**：事件子系统（`RaidEventBuffer` 环形缓冲 / `KillAttribution` 归属映射 / `RaidEventCollector` / `RaidEvent`）+ `/raid/events`（`since`/`limit`/`dropped`）+ `LocalGameStopPatch`。

**偏差（live 驱动，详见 spec「实施偏差记录」）**：受伤捕获改 Harmony patch `ActiveHealthController.ApplyDamage`（prefix 归属 / postfix 事件）——原 `ApplyDamageEvent` 委托订阅被 Il2CppInterop 非 blittable 封送限制否决（`DamageInfo`），且异常连带跳过 `DiedEvent` 订阅；修复后 `DiedEvent` 独立隔离订阅。

**live 证据（2026-09-15，Sandbox 局）**：
- 事件流 686+ 条：damage（部位/伤害量/来源类型：Bullet / LightBleeding / Barbed / HeavyBleeding）、death 多条；seq 单调 1..N、raidId 全一致、`dropped=0`。
- 击杀归属：本地玩家（Samuel / Bear / assault）击杀 bot → `killer.isLocal=true`（seq 348）；bot 击杀 bot 归属正确（如 "Traveler" / pmcUSEC）。
- 增量语义：`since=676` → 仅回 `677..686`（10 条新事件）；`dropped>0` 需缓冲淘汰（容量 1000），单测覆盖。
- 撤离事件：第一局阵亡实证 `{type:"extraction", exitName:"", status:"Killed"}`。

**验收**：见工单 05。
