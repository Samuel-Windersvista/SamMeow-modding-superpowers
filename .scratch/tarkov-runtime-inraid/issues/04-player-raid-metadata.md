# 04: 玩家 + raid 元数据全字段（raid_status / raid_player 完成）

**What to build:** 玩家域与 raid 元数据全量落地：位置/朝向/姿态（站/蹲/趴）/血量（总 + 各肢体）/存活；地图名/raid 状态（装载/进行/结束）/剩余时间/raidId。`raid_status`（raid 元数据 + 桥自报）与 `raid_player`（全字段）完成；输出 schema 确定性（字段排序稳定、可 diff），数据来源与新鲜度标注。

**Blocked by:** 03

**Status:** ready-for-agent

- [x] raid_status / raid_player 输出与 spec 字段清单一致；同输入输出逐字节稳定（确定性测试）
- [x] 血量（总+肢体）与姿态读数经 live 核对（受伤/姿态变化可见）
- [x] MCP 侧 fake-bridge 测试覆盖全部字段、缺省值与 `NOT_IN_RAID` 路径
- [x] live 验收记录（含受伤前后对比）

## Comments

### 2026-09-14 双 lane 实施 + live 核验（T04 完成，raidId 三轮修正）

- **桥侧（fix-3）**：`/raid/player` 全字段（position/rotation/pose/health{alive,total,parts}）+ `/raid/status`（map/status/remainingSeconds/raidId）
- **MCP 侧（fix-4）**：`raid_status`（元数据 + bridge 自报）/ `raid_player`（全字段）；确定性字段序
- **live 实证（三局 raid）**：
  - 姿态 Stand↔Duck（下蹲实时同步）；朝向随转身变化
  - **受伤对比**：Chest 85→78.93、total 440→433.93（实时反映）
  - `remainingSeconds` 递减（1733.463→1728.54）；map=Sandbox；status=Started
- **raidId 三轮 live 驱动修正**：① 空 profileId（`GameWorld.CurrentProfileId` 客户端为空）→ 改用 `MainPlayer.ProfileId`；② `StartDateTime` 非墙钟且同局翻转（`0001-11-23…`↔`no-start`）→ 判定不可靠；③ 改为**桥自持会话起点墙钟**——最终形态 `<profileId>@<UTC ISO>`，两次读取（间隔 5s）**完全一致**
- 测试：确定性 + NOT_IN_RAID + 全字段（fake）；live 见上
