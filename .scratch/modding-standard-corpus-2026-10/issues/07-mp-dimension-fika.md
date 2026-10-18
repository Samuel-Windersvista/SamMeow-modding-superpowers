# 07: `MP` 维度（Fika / 多人兼容）+ ADR-0013

**What to build:** 新增 `MP` 维度，把 Fika 多人兼容从“散落经验”升级为可引用的规则集。核心是：Fika interop 隔离在独立程序集、主程序集不出现 Fika 类型；用 SoftDependency + Chainloader GUID + 版本门禁检测；包注册/退订与 manager 生命周期绑定；host/client 角色分叉与中继；兼容性决策写入 README/ADR。

**Blocked by:** 01（validator 预重构）、02（证据基线刷新）

**Status:** ready-for-agent

- [ ] 写 ADR-0013，定义 `MP` domain、范围、与 `CLI` / `SRV` 的边界；记录 5.0 / IL2CPP 无语料的限制。
- [ ] 新增第 14 个维度章节；`MP-001`（MUST，独立 interop 程序集 + 反射桥 + `NoInlining`）、`MP-002`（SHOULD，SoftDependency + GUID + 版本范围 + local-only 降级）、`MP-003`（SHOULD，manager 创建后注册包、销毁时退订、幂等/重试）、`MP-004`（SHOULD，host/client 分叉与 relay）、`MP-005`（MAY，README/ADR 记录兼容性决策）。
- [ ] 规则注册表新增 `MP` domain 与 5 条规则；README 维度索引、规则计数、扩展流程同步。
- [ ] `version-matrix.md` 注明 MP 证据为 4.1 Mono/BepInEx 5；5.0 先标 4.1 / 待验证。
- [ ] KB 索引登记新章节；`validate-index.mjs` exit 0。
- [ ] 夹具覆盖 MP-001 的“主程序集出现 Fika 类型”反例（若可稳定检查）；不可稳定检查的规则保持 `check: null`。
- [ ] `validate-mod-standard.ps1` 与 `run-fixtures.ps1` 全绿。

**执行说明：** 与 08 采用 worktree 并行；共享规则注册表 / README / 证据索引冲突由 09 统一收口。依据 spec 的 D7。
