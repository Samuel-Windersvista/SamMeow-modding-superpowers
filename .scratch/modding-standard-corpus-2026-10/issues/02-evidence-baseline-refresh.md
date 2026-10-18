# 02: 证据基线刷新（631 仓库 / 2026-10-18）

**What to build:** 把 Modding Standard 的语料证据基线从 2026-09 的 392 个源码目录刷新到 2026-10-18 的 631 个 `_source` 目录 / 34,050 个文件。更新 `EV-CORPUS-SOURCE`、类型/语言/结构/csproj/元数据/配置/异常/Top5 计数，并登记 `SptVersion` 与 `ModGuid` 的真实分布。不改任何规则文本或级别。

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `EV-CORPUS-SOURCE` 写明 631 个源码目录 / 34,050 个文件 / 2026-10-18。
- [x] `EV-CORPUS-TYPE`、`LANG`、`STRUCT`、`CSPROJ`、`META`、`CFG`、`ANOMALY`、`TOP5` 按原方法重算，保留原表结构。
- [x] 记录 `SptVersion` 326 条声明中 297 条 tilde（91%）、15 条精确、13 条范围。
- [x] 记录 `ModGuid` 322 条声明 / 277 个唯一值 / 266 个反向域名（96%）。
- [x] `EV-REVIEW` 与校准段记录本次刷新日期与证据产物位置。
- [x] `validate-mod-standard.ps1` 仍 exit 0；不修改规则 ID、Level、Applies。

**执行说明：** 种子数据见 `.artifacts/forge-refresh-2026-10-18/` 的 corpus-pattern-scan / metadata-scan / metadata-scan2。依据 spec 的 D1、D2 中与计数有关的部分。

## Comments

- 2026-10-18：已实现并验收。evidence-index 的 EV-CORPUS-SOURCE/TYPE/LANG/STRUCT/CSPROJ/META/MECH/CFG/ANOMALY/TOP5/CHANGELOG 全部刷新到 631 基线；validator exit 0。`EV-GAP-DEP` / `EV-NOCORPUS #1` 仍保留旧结论，按计划由 ticket 03 修正（跨票暂态不一致，非本票失败）。证据索引 diff 中还包含本会话前既有的 `09-packaging.md` 相关 hunk，提交时需注意归属。未 git commit。

