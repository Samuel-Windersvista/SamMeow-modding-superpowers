# 09: 集成验收（规则集 / 索引 / bootstrap）

**What to build:** 把 03–08 的 worktree 分支合并成一份自洽的规则集：解决规则注册表、README、证据索引的合并冲突；同步规则数、维度数、导航与 version-matrix；跑通全部校验与夹具；输出一份可审计的验收报告。

**Blocked by:** 03、04、05、06、07、08

**Status:** ready-for-agent

- [ ] 合并 03–08 的 worktree 分支；解决规则注册表 / README / 证据索引冲突；无重复 ID、无重编号。
- [ ] 维度数为 15（原 13 + MP + LOC）；规则计数与 README / 注册表 / 校验器一致。
- [ ] `validate-mod-standard.ps1` exit 0：ID 集合一致、title/Level/Applies/domain 逐字一致、MUST 双源标记完整。
- [ ] `run-fixtures.ps1` exit 0：新增可检规则与 CLI-003 修正的夹具全绿。
- [ ] `sync-index` / `validate-index` exit 0：MP/LOC 章节进入 `index.json`（schema 2）。
- [ ] bootstrap 验证仍全绿；无 P3 项（存档迁移 / 诊断钩子 / TS 运行时）被误带入。
- [ ] 验收报告记录：15 维度、规则总数、证据锚点、扫描基线 631 / 34,050、已知 5.0 证据缺口。

**执行说明：** 这是唯一允许修改跨票共享文件并统一收口的票；03–08 不得在各自票内强行解决跨票冲突。依据 spec 的 D11、D12 与 Testing Decisions。
