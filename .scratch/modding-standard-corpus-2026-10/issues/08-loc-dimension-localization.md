# 08: `LOC` 维度（本地化）+ ADR-0014

**What to build:** 新增 `LOC` 维度，把 locale 文件布局、语言码、回退链、客户端语言切换钩子、服务器 locale 合并与键命名空间升级为规则。目标是让本地化 mod 不再用 ISO 码（zh/ja）导致 locale 不加载，也不再用“缺失即显示裸键”的回退。

**Blocked by:** 01（validator 预重构）、02（证据基线刷新）

**Status:** ready-for-agent

- [ ] 写 ADR-0014，定义 `LOC` domain、范围、与 `CLI` / `SRV` / `STRUCT` 的边界；记录 5.0 无语料的限制。
- [ ] 新增第 15 个维度章节；`LOC-001`（SHOULD，`locales/<SPT 语言码>.json`，语言码用 `Locales.Global` 键而非 ISO）、`LOC-002`（SHOULD，`en.json` 主文件 + 当前语言 → en → 键名回退）、`LOC-003`（SHOULD，客户端语言切换钩子/监听器）、`LOC-004`（SHOULD，mod 键前缀 + 服务器 `<mongoId> <suffix>` 约定）、`LOC-005`（SHOULD，服务器 `AddTransformer` 合并而非重写整表）。
- [ ] 规则注册表新增 `LOC` domain 与 5 条规则；README 维度索引、规则计数、扩展流程同步。
- [ ] `version-matrix.md` 注明 LOC 证据为 4.1 Mono/BepInEx 5；5.0 先标 4.1 / 待验证。
- [ ] KB 索引登记新章节；`validate-index.mjs` exit 0。
- [ ] 夹具覆盖 locale 文件命名 / 语言码 / en 回退的可稳定检查子集；不可稳定检查的规则保持 `check: null`。
- [ ] `validate-mod-standard.ps1` 与 `run-fixtures.ps1` 全绿。

**执行说明：** 与 07 采用 worktree 并行；共享规则注册表 / README / 证据索引冲突由 09 统一收口。依据 spec 的 D8。
