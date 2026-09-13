# 03: 维度 ④-⑤ 规则：SRV / CLI 机制

**What to build:** 撰写机制维度规则——`04-server.md`（DI：`[Injectable(TypePriority = OnLoadOrder.X + n)]` 禁止裸数字、`IOnLoad`/`IOnUpdate` 生命周期、`StaticRouter`/`DynamicRouter` 路由注册、Callbacks、`ISptLogger<T>` 日志注入）、`05-client.md`（`BaseUnityPlugin` + `[BepInPlugin]` 反向域名 GUID、Harmony patch 组织与目标选择、`[BepInDependency]` 声明、客户端日志）。格式要求同 ticket 02。

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] 两个维度文件完成，规则五要素齐全
- [ ] MUST 规则双源证据；引用 api-notes-4.1/5.0 的 di-container、mod-loading、http-routing 作为机制证据
- [ ] 内联样例：Injectable 类骨架、Router 注册片段、Plugin 入口骨架、Harmony patch 样板
- [ ] index.json 登记本票文件
