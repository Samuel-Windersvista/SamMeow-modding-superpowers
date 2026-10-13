# 06: 服务器启动核验

**What to build:** 用户启动 MO2 + SPT 服务器后，解析服务器日志确认：mod 加载成功、各组变更 summary 齐全、数据缺口告警计数（预期：背包 9 条 / barter 1 条）、Crisis/Drip-Out/Collector 实际行为登记、无未处理异常。

**Blocked by:** 05（MO2 部署）；需用户配合启动服务器（agent 不代启动 MO2/游戏）

**Status:** ready-for-agent

- [x] 日志确认本 mod 加载（入口行/加载顺序）
- [x] 各组 summary 行齐全（组名 + 变更计数）
- [x] 数据缺口告警计数与预期一致；差异项登记原因
- [x] Crisis / Drip-Out / Collector 实际行为登记于移植记录（以开放项登记，待游玩复核，见 port-notes §6/§5.3）
- [x] 服务器无未处理异常（或异常已定位到非本 mod）

## Comments

- 2026-10-13：核验通过（启动日志实证）。加载行 `[ITS] Inescapable Tarkov's Softcore v0.1.0 loaded`；合计 7 模块 / 变更 10609 / 警告 32 / 错误 0 / 跳过 0；数据缺口 = 背包 9 + trueItems 23（已回填 port-notes §5.1）。
- 唯一 Error 为良性 `Unable to add bundle: barrel_vpo215_600mm_366tkm.bundle`（与 BetterRearSights 先到先得注册冲突）；F1/F4 修复后 Critical/报错清零。
