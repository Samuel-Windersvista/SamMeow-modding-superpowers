# 07: version-matrix

**What to build:** 撰写 `version-matrix.md`：4.1.5↔5.0 差异对照表，汇总 02–06 各规则文件的 `Applies` 标签 + migration 文档（`api-mapping-311-to-41`、`5xx-source-verification`）+ `api-notes-5.0/`。表格化呈现：机制主题 / 4.1.5 行为 / 5.0 行为 / 对规则的影响（指向具体 Rule ID）。

**Blocked by:** 02, 03, 04, 05, 06

**Status:** ready-for-agent

- [ ] 对照表覆盖全部规则中 `Applies` 非 both 的条目
- [ ] 每行注明证据来源（源码路径或 KB 文档）
- [ ] 与各规则文件的版本标签无矛盾（抽查核对）
- [ ] index.json 登记
