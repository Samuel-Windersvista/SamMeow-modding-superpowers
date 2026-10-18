# 06: 结构 / 元数据 / TS 遗产补充

**What to build:** 补上三类现有规范未覆盖但语料明确的布局与迁移知识：monorepo 多 mod 布局、TS 3.11 服务端 mod 的遗产识别、以及 TS → C# 的元数据迁移映射。目标是让 agent 不再把“一个仓库多个 `IModMetadata`”误判为违规，也不会误以为 4.1+ 仍以 `package.json` 作为服务端 mod 主形态。

**Blocked by:** 02（证据基线刷新）

**Status:** done

- [x] 新增 `STD-STRUCT-008`（SHOULD）：一个仓库可含多个可部署 mod 目录；每个部署目录恰好一个 `IModMetadata`；README 标注目录与部署边界。
- [x] 新增 `STD-META-008`（SHOULD）：`package.json` + `sptVersion` 的 TS/JS 服务端 mod 是 3.11 遗产；4.1/5.0 新服务端 mod 使用 C# `IModMetadata`。
- [x] 新增 `STD-VER-005`（MAY）：移植映射 `sptVersion` → `SptVersion`、`loadBefore/loadAfter` → `TypePriority`、`incompatibilities` → `Incompatibilities`。
- [x] 结构 / 元数据 / 版本差异章节的 Evidence 引用 11 个 monorepo 仓库、41 个 package.json mod、ABPS 3.11 TS 与 4.1 C# 双版本对照。
- [x] `validate-mod-standard.ps1` 与 `run-fixtures.ps1` 全绿。

**执行说明：** 与 04、05 采用 worktree 并行；共享注册表 / README / 证据索引冲突由 09 收口。依据 spec 的 D9。

## Comments

- 2026-10-18：实现并合并。新增 STRUCT-008 / META-008 / VER-005；覆盖 monorepo 多 mod、TS 3.11 遗产识别、TS→C# 迁移映射。最终集成 103 条 / MUST 31/31。
