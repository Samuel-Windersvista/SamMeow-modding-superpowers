# 03: 维度 ④-⑤ 规则：SRV / CLI 机制

**What to build:** 撰写机制维度规则——`04-server.md`（DI：`[Injectable(TypePriority = OnLoadOrder.X + n)]` 禁止裸数字、`IOnLoad`/`IOnUpdate` 生命周期、`StaticRouter`/`DynamicRouter` 路由注册、Callbacks、`ISptLogger<T>` 日志注入）、`05-client.md`（`BaseUnityPlugin` + `[BepInPlugin]` 反向域名 GUID、Harmony patch 组织与目标选择、`[BepInDependency]` 声明、客户端日志）。格式要求同 ticket 02。

**Blocked by:** 01

**Status:** done

- [x] 两个维度文件完成，规则五要素齐全
- [x] MUST 规则双源证据；引用 api-notes-4.1/5.0 的 di-container、mod-loading、http-routing 作为机制证据
- [x] 内联样例：Injectable 类骨架、Router 注册片段、Plugin 入口骨架、Harmony patch 样板
- [x] index.json 登记本票文件

## Comments

**2026-09-14 完成（agent）**

- 交付：`04-server.md`（STD-SRV-001..008）、`05-client.md`（STD-CLI-001..007），共 15 条规则。
- 双轴评审后修复：SRV-003 MUST→SHOULD（取消传播主题归 STD-LOG-005）；SRV-001 补配置类例外；CLI-002 标题与 Rule 与 META-006 对齐；CLI-003 的 `Patches/` 目录约定降为建议；新增交叉引用 5 处。
- 5 条无语料先例规则已标注「机制推断，无语料先例」并登记 `EV-NOCORPUS`。
- 机械核验：五要素齐全、ID 唯一、链接与锚点全部可解析。
