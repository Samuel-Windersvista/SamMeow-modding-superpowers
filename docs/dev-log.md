# Dev Log

## 2026-09-13 — tarkov-runtime-MCP 首版建成并通过真实 server 冒烟

- 完成 `.scratch/tarkov-runtime-mcp/` 全部 7 张工单（01 穿甲弹 / 02 快照+profile / 03 sections 补全 / 04 日志解析 / 05 wait_for / 06 注册+冒烟 / 07 会话获取+信封修复）。
- 工具面：`tarkov_server_status` / `tarkov_instances` / `tarkov_snapshot`（profile/traders/quests/hideout/inventory）/ `tarkov_wait_for`；`raid.*` 占位待 Phase 2 BepInEx Client Bridge。
- 测试：142/142 绿；live 冒烟两轮（第一轮暴露会话缺失与信封解包两缺陷 → ticket 07 修复；第二轮全部通过，真实数据人工核对一致）。
- 关键经验：5.0 响应统一 `{err,errmsg,data}` 信封；`PHPSESSID` = profileId，经免会话的 `/launcher/v2/profiles` 获取；`/launcher/v2/mods` 是 mod 清单的最优路由源。
- 待办（follow-up）：hideout 等级改读 profile `Hideout.Areas`；traders standing/assort 改读 `TradersInfo`；MCP 进程需 export `TARKOV_RUNTIME_MCP_USERNAME`（可选 PASSWORD）才能用 session 受限工具。

## 2026-09-13（晚）— ticket 08：活跃 profile 跟随 + MO2 VFS 全链路验证

- 会话获取升级为规则链：探针跟随 -> username 精确 -> 单 profile 零配置自动 -> 多 profile 结构化歧义（AMBIGUOUS_PROFILE）。
- 新增可选 server mod `tools/tarkov-active-probe`（ADR-0004）：只读路由 `/spt/runtime/active-profiles` 暴露 ProfileActivityService 活跃窗口。
- 里程碑：**首次经 MO2 VFS 启动 SPT 5.0 server 并成功加载自定义 server mod**（假设 A-1 验证通过）；MCP 零配置（无 username）直连并返回真实数据。

## 2026-09-13（深夜）— MO2 安装能力完善：命名约定 + comments/notes 备注

- `mo2_install` / `mo2_create_mod` 新增 `comments`（必填：MO2 列表短摘要）+ `notes`（可选：安装记录）参数，写入 meta.ini（QSettings 兼容转义）；工具描述固化 `<category>-<mod-name>-<version>` 命名约定。
- `mo2_set_mod_notes` 修复转义缺陷：引号 / 反斜杠 / 换行按 QSettings 格式编码，多行备注不再破坏 meta.ini。
- 探针覆盖层改名 `TarkovActiveProbe` → `工具-MCP活跃探针-0.1.0`，并补 comments + notes（本实例修复）。
- 测试：mo2-mcp 510 passed / 0 failed（新增 `qsQuote` 单测 + `mo2_install` / `mo2_create_mod` 注释写入测试）。
- `tarkov_snapshot` 输出新增 `session`（source / username / profileId）可观测性——测试自动化可断言"这次读的是哪个 profile、经哪条路径"。
- **live 验证通过（玩家实际游玩中）**：探针路由返回活跃 profileId；零配置快照 `session.source=active-probe`，正确跟随 Samuel——"玩家实时用哪个 profile，MCP 就读哪个"全链路证实。tarkov-runtime-mcp 150/150 绿。
- 注意：本机 mo2 MCP 连接在 ticket 08 的 run_tool 调用后断开（实例修复经脚本完成）；`snapshot.session` 字段在下次会话重启 MCP 后生效。
