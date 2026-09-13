# 07: 会话获取 + 信封解包修复 + mods 路由源（live smoke 暴露�?

**What to build:** ticket 06 真实 server 冒烟暴露的两个缺陷与一个改进，修复后重跑冒烟使快照返回真实数据�?

1. **会话获取**：session 受限路由（profile/list、traderSettings、quest/list 等）要求 `PHPSESSID` cookie 等于目标 profile �?profileId（源码证据：`HttpServer.cs` 直接�?cookie 值构�?`MongoId`；`LauncherV2Controller.GetSessionId` 表明 sessionId �?profile id）。握手时自动获取：POST `/launcher/v2/profiles`（免会话）按配置�?username 匹配 profile，取�?`profileId` 作为后续全部请求�?`PHPSESSID`。若配置�?password，先 POST `/launcher/v2/login` 验证。username 未配置或匹配不到 profile 时，快照�?session 受限工具返回结构�?`SESSION_NOT_CONFIGURED` / `PROFILE_NOT_FOUND`（新增错误码），不得静默返回空数据。配置经环境变量（如 `TARKOV_RUNTIME_MCP_USERNAME` / `TARKOV_RUNTIME_MCP_PASSWORD`）�?
2. **信封解包修复**�?.0 真实响应统一�?`{err, errmsg, data: [...]}` 信封（live 实测，四个路由均如此）。现有正常器未解此信封导�?hideout 等有数据路由返回 0。修�?util 层解包；用冒烟中捕获的真实响应（裁剪版）补充 fixture�?
3. **mods 来源升级**：`/launcher/v2/mods` 免会话返回已加载 server mod 元数据（live 实测返回 `{}`，因�?server �?mod——是真相）。`server_status.mods` 改为路由优先（`source: "route"`），日志兜底（`source: "server-log"`）�?

**Blocked by:** None（基于已合回�?main；实质上�?ticket 06 冒烟的修复闭环）

**Status:** ready-for-human

- [ ] 配置 username 后握手自动获取会话，session 受限 sections 返回真实数据
- [ ] 未配�?username �?session 受限工具返回结构化错误，而非静默空数�?
- [ ] `{err,errmsg,data}` 信封�?util 层统一解包；hideout �?section 正常返回真实计数
- [ ] 真实响应裁剪版进�?fixture，正常器测试以其驱动
- [ ] `server_status.mods` 路由优先、日志兜底，schema 标注实际来源
- [ ] 全量测试绿（含既�?115�?
- [ ] 修复后在运行中的 5.0 server 上重跑冒烟：�?sections 快照返回非零真实数据（与游戏内人工核对）
