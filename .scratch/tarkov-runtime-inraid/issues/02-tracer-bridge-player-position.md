# 02: 首刀（穿甲弹）——桥骨架 + 玩家位置 + HTTP 端点 + MCP raid_player + MO2 交付 + live 验收

**What to build:** 第一个端到端垂直切片。BepInEx 6（IL2CPP）客户端插件骨架（Modding Standard client 形态，ModGuid 反域名），经 MO2 overlay（mod 根=游戏根，`BepInEx/plugins/`）装入 SPT 5.0 客户端；插件主线程采样循环读取玩家位置写入线程安全缓冲，经仅绑 127.0.0.1 的 HTTP 单端点暴露最小 schema（位置 + `sample_age_ms`，端口可配）；MCP 侧新增 `BridgeConnection` 抽象（本 Phase 唯一新接缝）与 `raid_player` 最小实现（拉取式）；bridge 未安装时保持 `CLIENT_BRIDGE_NOT_INSTALLED` 语义。live 验收：MO2 启动 → BepInEx 日志加载行 → 进 raid → MCP 读到真实位置（移动前后对比）。

**Blocked by:** 01

**Status:** ready-for-agent

- [x] 插件经 MO2 overlay 在 SPT 5.0 客户端加载成功（LogOutput.log 出现 Loading 行 + Chainloader startup complete）；usvfs 客户端投送结论记录（A-1 扩展实测）
- [x] HTTP 端点仅绑 127.0.0.1，返回最小 schema；端口可配；冲突时不崩溃
- [x] MCP `raid_player` 经 `BridgeConnection` 端到端读到真实数据；fake bridge 测试覆盖未安装/不可达路径
- [x] live 验收记录：进 raid 后位置读数与移动变化一致
- [x] 桥源码遵循 Modding Standard（client 形态）或记录豁免
- [x] dev-log 记录首刀三假设实测结论（usvfs 投送 / IL2CPP 读数 / HttpListener 可用性）

## Comments

### 2026-09-14 双 lane 实施 + live 验收（T02 完成）

- **fix-1（桥插件 C#）**：`tools/tarkov-runtime-bridge/`（net6.0 / BepInEx 6 IL2CPP）构建 0 error；GUID `com.sammeow.tarkov-runtime-bridge`；`Unload()` 撤销（BasePlugin 无 Dispose，已核）；HTTP 契约精确实现（404/405/NaN 退化）
- **fix-2（MCP 侧 TS）**：`BridgeConnection` 接缝 + `raid_player` 真实化 + `NOT_IN_RAID` 错误码 + 能力自报修正；168/168 测试绿（orchestrator 复核）+ typecheck + build；干跑三路径（in-raid / not-in-raid / 不可达）全绿
- **MO2 交付**：覆盖层 `工具-tarkov-runtime-client-bridge-0.1.0`（实例 `Inescapable Tarkov` / Default）→ `BepInEx/plugins/TarkovRuntimeBridge.dll` + meta.ini
- **live 验收（2026-09-14 晚，真实游戏）**：
  - 菜单态：端点 `{"inRaid":false}` 200；BepInEx 日志 `Loading [Tarkov Runtime Bridge 0.1.0]` + `listening on http://127.0.0.1:49777/raid/player`；MCP → `NOT_IN_RAID` ✓
  - raid 态：`{"inRaid":true,"position":{"x":64.30417,"y":14.749987,"z":157.47061},"sampleAgeMs":844}`；MCP ok 信封 ✓
  - 移动比对：x 64.30→99.06 / z 157.47→172.60（Δ≈+34.8/+15.1）；2s 后采样 age 500ms（采样循环活跃）✓
- **三假设实测结论**：① usvfs 客户端投送 **成立**（MO2 overlay → BepInEx/plugins 加载成功，A-1 扩展验证）；② IL2CPP 读数 **成立**（`MainPlayer.Position` 真实坐标，含移动）；③ HttpListener **可用**（游戏进程内绑定 127.0.0.1 无 URL ACL 障碍；另经受限令牌预测试 BIND_OK）
- 遗留：`BRIDGE_UNREACHABLE` 拆分与采样配置正式化 → T03；Modding Standard 全量机检 / README 收尾 → T08
