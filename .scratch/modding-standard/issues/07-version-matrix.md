# 07: version-matrix

**What to build:** 撰写 `version-matrix.md`：4.1.5↔5.0 差异对照表，汇总 02–06 各规则文件的 `Applies` 标签 + migration 文档（`api-mapping-311-to-41`、`5xx-source-verification`）+ `api-notes-5.0/`。表格化呈现：机制主题 / 4.1.5 行为 / 5.0 行为 / 对规则的影响（指向具体 Rule ID）。

**Blocked by:** 02, 03, 04, 05, 06

**Status:** done

- [x] 对照表覆盖全部规则中 `Applies` 非 both 的条目
- [x] 每行注明证据来源（源码路径或 KB 文档）
- [x] 与各规则文件的版本标签无矛盾（抽查核对）
- [x] index.json 登记

## Comments

**2026-09-14 完成（agent）**

- 交付：`version-matrix.md`（5 组对照表、29 行数据；覆盖全部 `Applies` 非 both 规则 + 带版本分支的 both 规则 + 关键机制差异）。
- `index.json` 登记（221 条，`ConvertFrom-Json` 通过）；README 4 处收尾（链接化 + 状态行更新）。
- 一致性抽查：非 both 项仅 STD-VER-002/003/004 与 STD-VERIFY-005（均 5.0）；版本分支规则（BUILD-002/003/004/006、CLI-001/006/007）与本表一致；17 个链接全部可解析。
- 说明：SPT 5.0 为预发布线（`5.0x-dev` 快照），表头已注明「使用前以源码复核」。