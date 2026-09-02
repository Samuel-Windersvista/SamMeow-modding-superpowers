# MCP Specs

This directory holds MCP specs and contracts only.

## 实现状态（2026-08-19 盘点）

| Spec | 状态 |
|---|---|
| `spt-mcp-design.md` | **已实现**（`tools/spt-mcp/`，7 工具运营，46/46 测试绿） |
| `xedit-readonly.md` | **已实现**（xedit MCP 只读面，`tools/xedit-mcp/`） |
| `loot-metadata.md` | **BACKLOG — 仅有设计，未实现**（BGS 时代遗留规划，无当前需求方） |
| `nexus-metadata.md` | **BACKLOG — 仅有设计，未实现**（同上） |
| `translation-memory.md` | **BACKLOG — 仅有设计，未实现**（同上；SPT 侧翻译需求对应 RELEASE-NOTES 的 `using-spt-translator` deferred 项） |

> 三个 BACKLOG 设计不被任何活跃战线引用。若重启，先确认需求方存在再动工；否则建议未来清理时移入 `docs/archive/`。
