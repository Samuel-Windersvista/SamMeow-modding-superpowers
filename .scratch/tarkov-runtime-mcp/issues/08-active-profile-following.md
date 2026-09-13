# 08: 活跃 profile 跟随（A: 自动选择规则 + B: 可选活跃探针 mod）

**What to build:** 会话获取从"必须配置 username"升级为跟随玩家实际使用的 profile：

- **A（必做，纯 MCP 侧）**：`/launcher/v2/profiles` 返回恰好 1 个 profile 时零配置自动选用；多 profile 且未配置 username 时返回结构化 `AMBIGUOUS_PROFILE`（列出候选 username）；已配置 username 维持精确匹配。探测到活跃探针路由（B）时优先使用活跃 profile。
- **B（可选增强，游戏侧微 mod）**：SPT 5.0 C# server mod（IModMetadata + StaticRouter），只注册一个只读路由 `/spt/runtime/active-profiles`，返回 `ProfileActivityService.GetActiveProfileIdsWithinMinutes(N)` 的 profileId 列表。MCP 握手时探测该路由：可用则跟随活跃 profile，不可用退回 A 规则（渐进增强，不存在不报错）。部署方式与 MO2 overlay 验证（假设 A-1）在执行时确认。

**Blocked by:** None（基于 main；A 与 B 可分别验证）

**Status:** ready-for-agent

- [ ] 单 profile 零配置自动选用，session 受限工具直接可用
- [ ] 多 profile 未配置 username 返回 `AMBIGUOUS_PROFILE` 且列出候选
- [ ] 已配置 username 时精确匹配行为不回退
- [ ] 探针路由可用时优先跟随活跃 profile；不可用时静默退回 A 规则
- [ ] 探针 mod 在真实 5.0 server 加载并成功返回活跃 profile（live 验证）
- [ ] 全量测试绿（含既有 142）
