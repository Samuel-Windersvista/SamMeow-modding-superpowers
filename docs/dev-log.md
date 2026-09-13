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

## 2026-09-13 — post-restart smoke check

Restarted OpenCode and verified the SPT-only harness end to end. The smoke check
found two breakages the cleanup had introduced; both are fixed.

### Found and fixed

- **Runtime dependencies were deleted along with their git index entries.**
  `git rm --cached` on `tools/spt-mcp/node_modules` (plus the merge that removed
  it from the index) deleted the directory from disk, and `tools/mo2-mcp/dist`
  was absent. Both MCP servers could no longer start:
  `ERR_MODULE_NOT_FOUND: Cannot find package '@modelcontextprotocol/sdk'`.
  Fixed by `npm install` in `tools/spt-mcp` (138 packages; its `prepare` script
  re-ran `tsc`) and `npm install` + `npm run build` in `tools/mo2-mcp`.
  `node_modules` stays untracked — it is regenerable.
- **The owner's global `opencode.json` pointed `spt-mcp` at the deleted
  materialized tree** (`plugins/bgs-modding-superpowers/tools/spt-mcp/dist/index.js`).
  Repointed to `tools/spt-mcp/dist/index.js`.

### Verified

- `mo2-mcp` starts and logs `mo2-mcp ready (session ..., binding=unbound)`.
- `spt-mcp` starts with no module-resolution errors.
- The session skill set is the 14 SPT skills, with no BGS skills.
- The injected bootstrap carries the SPT marker (`EXTREMELY_IMPORTANT_SPT_MODDING_SUPERPOWERS`).
- `verify-all.ps1` 6/6 PASS; `git status` clean apart from pre-existing owner work.

### Follow-up

- Restart OpenCode once more so the corrected `spt-mcp` path is picked up.
- Note: the repo plugin registers `mo2` and `spt` MCP servers itself, so the global
  `opencode.json` `spt-mcp` entry was a functionally duplicate (and worse-configured)
  registration — it passed none of `SPT_KB_ROOT` / `SPT_MCP_HELPER` / `SPT_IL_HELPER`.
  It has been removed, so both MCP servers are now scoped to this repo only.

## 2026-09-13 — MCP build-artifact policy unified

The two bundled MCP servers disagreed on whether to ship `dist/`:
`tools/spt-mcp/dist` was tracked (the package had no `.gitignore`), while
`tools/mo2-mcp/dist` was ignored and its package had no `prepare` script. A fresh
clone therefore had a dead `mo2` MCP — the plugin points at
`tools/mo2-mcp/dist/index.js`, which nothing produced.

Unified to "dist is build output, never tracked":

- `tools/spt-mcp/.gitignore` added; `tools/spt-mcp/dist` untracked (26 files).
- `tools/mo2-mcp/package.json` gains `"prepare": "npm run build"`, matching
  `tools/spt-mcp`, so a single `npm install` builds each server.
- New `tests/bootstrap/verify-mcp-entrypoints.ps1` asserts both entrypoints exist,
  both packages carry a `prepare` script, and neither dist tree is tracked.
  Wired into `verify-all.ps1` (now 7 checks).

A fresh clone needs one command per server directory (`npm install`) before the
MCP surface is live, and the suite now fails loudly if that step is missed.
