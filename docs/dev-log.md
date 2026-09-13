# Dev Log

## 2026-09-13 — tarkov-runtime-MCP 首版建成并通过真实 server 冒烟

- 完成 `.scratch/tarkov-runtime-mcp/` 全部 7 张工单（01 穿甲弹 / 02 快照+profile / 03 sections 补全 / 04 日志解析 / 05 wait_for / 06 注册+冒烟 / 07 会话获取+信封修复）。
- 工具面：`tarkov_server_status` / `tarkov_instances` / `tarkov_snapshot`（profile/traders/quests/hideout/inventory）/ `tarkov_wait_for`；`raid.*` 占位待 Phase 2 BepInEx Client Bridge。
- 测试：142/142 绿；live 冒烟两轮（第一轮暴露会话缺失与信封解包两缺陷 → ticket 07 修复；第二轮全部通过，真实数据人工核对一致）。
- 关键经验：5.0 响应统一 `{err,errmsg,data}` 信封；`PHPSESSID` = profileId，经免会话的 `/launcher/v2/profiles` 获取；`/launcher/v2/mods` 是 mod 清单的最优路由源。
- 待办（follow-up）：hideout 等级改读 profile `Hideout.Areas`；traders standing/assort 改读 `TradersInfo`；MCP 进程需 export `TARKOV_RUNTIME_MCP_USERNAME`（可选 PASSWORD）才能用 session 受限工具。
