# Development Log

## 2026-09-13 — SPT-only cleanup

**Branch:** `chore/spt-only-cleanup` (9 commits) · **Recovery tag:** `archive/bgs-final`

Stripped the inherited BGS (Bethesda) half and reduced the repository to an
SPT-only OpenCode toolkit carrying the Matt Pocock agent-skills configuration.

### Result

- **Tracked files:** 49,535 → 792 (-98.4%)
- **Removed skills:** 14 BGS + 2 generic devlog/changelog; `skills/` now holds exactly the 14 SPT skills
- **Removed knowledge/tools:** `knowledge/bgs-kb/`; xEdit MCP, xEdit hook bridge, BGS KB MCP, BGS archive, BGS Papyrus, BGS translator
- **Removed scripts:** xEdit fetch, hook-bridge install, script-extender update, Creation Club split, KB release/author/rebuild, Nexus BGS update-state
- **Untracked/deleted:** `external/spt-archive/` (21,370 files), materialized `plugins/` tree (13,301), `knowledge/spt-kb/archive/` (8,353 untracked, kept on disk), .NET `obj`/`bin` artifacts, `tools/spt-mcp/node_modules` (4,350), `.opencode/artifacts/`
- **Renamed:** plugin/package `bgs-modding-superpowers` → `spt-modding-superpowers`; env vars `BGS_MO2_ROOT`/`BGS_SPT_KB_ROOT` → `MO2_ROOT`/`SPT_KB_ROOT`
- **Harness:** OpenCode-only (Claude Code / Codex manifests, hooks, `.mcp.json` removed); bootstrap injects `using-spt-modding-superpowers`; MCP surface is `mo2` + `spt`
- **Docs:** `docs/agents/{issue-tracker,triage-labels,domain}.md`, `AGENTS.md` (tracked), single-context `CONTEXT.md`/`docs/adr/` layout, local-markdown tracker under `.scratch/`
- **Moved out:** `docs/fiction/` → `NorvinskStalker_knowledgeBase\fiction\SamMeow-modding-fiction\`
- **Deleted docs:** `docs/archive/`, `docs/internal/future-skills/`; completed wayfinder tickets archived
- **Verification:** `tests/bootstrap/verify-all.ps1` rebuilt as SPT-only invariant checks (layout, skills, bootstrap injection, MCP surface, git hygiene, templates) — **6/6 PASS**

### Open items

- Owner must rename local environment variables (`BGS_MO2_ROOT` → `MO2_ROOT`, `BGS_MO2_*` → `MO2_*`, `BGS_SPT_KB_ROOT` → `SPT_KB_ROOT`) and provide `MO2_ROOT` / `MO2_HARNESS_ROOT` for the MO2 acceptance scripts.
- Fresh-session smoke check pending: restart OpenCode, confirm the SPT bootstrap is injected, the SPT skill set is visible, and the `mo2` / `spt` MCP servers respond.
- Pre-existing MO2 control-plane test failures are not regressions (missing `plugin/README.md`, missing `live-integration.md`, no `pwsh`, Python inline-script issues).
