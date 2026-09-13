# 08: 模板升级：server + client

**What to build:** 按**校准后**的规则修订两个模板——`templates/server-mod`：补 `config/config.jsonc` + `defaultConfig.jsonc` 加载示例与 `IOnDIConstruct` 注册（当前缺失）、README/LICENSE 模板、规则引用注释（`STD-XXX-nnn`）；`templates/client-mod`：补 README/LICENSE 模板、规则引用注释、Harmony patch 组织对齐。S1 验证：两模板 `dotnet build` 均通过。

**Blocked by:** 02, 03, 04, 05, 11（规则先经试点校准，再固化进模板）

**Status:** ready-for-agent

- [ ] server 模板含完整 config 加载示例且构建通过
- [ ] client 模板构建通过
- [ ] 模板注释引用对应 Rule ID（可被检索）
- [ ] S1 构建验证结果记录在票内 Comments
