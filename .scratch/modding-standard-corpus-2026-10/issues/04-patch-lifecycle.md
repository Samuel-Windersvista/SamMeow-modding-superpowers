# 04: 补丁生命周期（客户端 + 服务端）

**What to build:** 让补丁规则与语料主导形态一致，并补上服务端补丁生命周期。客户端默认使用 `ModulePatch` / `PatchSet` 显式 `Enable()`，`PatchAll()` 只作为“所有目标已验证”的受控选项；同时加入逐 patch 目标缺失隔离、撤销前状态恢复、raid 缓存复位、精确目标解析规则，以及服务端 `IRuntimePatch` + DI 收集 + `IOnLoad` 启用的规则。

**Blocked by:** 02（证据基线刷新）

**Status:** ready-for-agent

- [ ] `STD-CLI-003` 保留 `[HarmonyPatch(typeof(...), "...")]` 要求，但把默认应用方式改为 `ModulePatch` / `PatchSet` 的显式 `Enable()`；`PatchAll()` 仅在所有目标已验证时允许。
- [ ] 新增 `STD-CLI-008`（目标解析失败记 Error 并跳过该 patch）、`STD-CLI-009`（撤销前恢复游戏状态）、`STD-CLI-010`（raid 结束复位缓存对象）、`STD-CLI-011`（精确目标解析），Level / Applies / Evidence 完整。
- [ ] 新增 `STD-SRV-009`：服务端补丁用 `IRuntimePatch` / `AbstractPatch` + DI `IEnumerable<IRuntimePatch>` + `IOnLoad` 统一 `Enable()`；服务端不以 `PatchAll()` 扫程序集。
- [ ] `STD-CLI-007` 的 `cli-007-harmony-lifecycle` 检查器补充 `ModulePatch` / `.Enable()` 识别，避免主导形态被误判为 N/A。
- [ ] 夹具覆盖：`ModulePatch.Enable` 通过、受控 `PatchAll` 通过、单个目标缺失只影响该 patch。
- [ ] `validate-mod-standard.ps1` 与 `run-fixtures.ps1` 全绿。

**执行说明：** 与 05、06 采用 worktree 并行；共享的规则注册表 / README / 证据索引冲突由 09 统一收口。本票只保证补丁相关规则自洽。依据 spec 的 D3、D4。
