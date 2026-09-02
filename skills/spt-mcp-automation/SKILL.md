---
name: spt-mcp-automation
description: "Use when performing any SPT mod analysis via the spt MCP server -- mod inventory, conflict analysis, Forge archive search, knowledge base query, MO2 status. Triggers on any task involving SPT mod DLL analysis, IModMetadata, BepInEx plugin metadata, server mod table injection, or mod conflict detection."
---

# SPT MCP Automation

Hub skill for all `spt` MCP server operations. The spt MCP provides file-based SPT mod analysis -- no daemon, no lifecycle management, all tools return synchronously.

## Available MCP tools (7)

### Mod inventory (3)

| Tool | Use |
|---|---|
| `spt_list_mods` | Scan a directory for mods. `{ path, type: "server"\|"client" }` -> mod list with name/version/guid/dependencies. Server mods = dirs with top-level DLL (4.1, metadata read via .NET helper from IModMetadata) or package.json (legacy); client = recursive .dll scan. |
| `spt_read_mod_metadata` | Read one mod's full metadata. `{ modPath, type }` -> server: IModMetadata fields (name/guid/author/version/sptVersion) read from DLL via helper, or package.json; client: file-derived metadata. |
| `spt_scan_mod_files` | List all files in a mod. `{ modPath }` -> file list with relative paths and sizes. |

### Conflict analysis (2)

| Tool | Use |
|---|---|
| `spt_analyze_conflicts` | Analyze a set of mods for conflicts. `{ modPaths[], sptPath }` -> conflict report graded by severity (B=guid dup/version mismatch, O=file overwrite/config collision). |
| `spt_predict_load_order` | Predict server mod load order. `{ modPaths[] }` -> ordered list by TypePriority -> ModGuid. |

### Forge archive (1)

| Tool | Use |
|---|---|
| `spt_forge_search` | Search archived Forge mods. `{ query?, category?, sptVersion?, modType? }` -> matching mod list (downloads desc). |

### Knowledge base (1)

| Tool | Use |
|---|---|
| `spt_kb_query` | Query the spt-kb. `{ topic?, domain?, version?, keyword? }` -> matching KB entries (title match). |

## Server mod metadata source (4.1)

SPT 4.1 server mods are DLLs in `user/mods/<mod>/`; metadata (ModGuid/Name/Author/Version/SptVersion) is read from the DLL's `IModMetadata` implementation via the `.NET helper` (`tools/spt-mcp/helper`, AsmResolver reads the parameterless ctor IL constants). The `package.json` path is legacy/fallback only.

- Helper auto-located via `SPT_MCP_HELPER` env (set by the OpenCode plugin). Rebuild after helper changes: `dotnet build tools/spt-mcp/helper -c Release`.
- If `spt_list_mods` returns 0 server mods or `read_mod_metadata` errors with "helper not found", the helper is missing -- rebuild it or set `SPT_MCP_HELPER`.

## Routing doctrine

- **Mod conflict questions** -> `spt_analyze_conflicts` (via `spt-conflict-audit` skill)
- **"What mods are installed?"** -> `spt_list_mods`
- **"Tell me about this mod"** -> `spt_read_mod_metadata` or `spt_forge_search`
- **"Find a mod that does X"** -> `spt_forge_search`
- **"How do I do X in SPT modding?"** -> `spt_kb_query`

## Anti-patterns

- **Do not** parse mod DLLs with your own Python/JS -- use the MCP tools (or the helper CLI)
- **Do not** guess mod metadata -- use `spt_read_mod_metadata`
- **Do not** hand-analyze conflicts by reading source code -- use `spt_analyze_conflicts`
- **Do not** loop hundreds of mods through your own context for conflict analysis -- delegate to a read-only subagent that uses the MCP tools
- **Do not** attempt IL-level analysis (Harmony patch targets) with these tools -- that requires the future IL decompilation pipeline

## Sub-agent recipe

For large-scale conflict surveys (many mods):

1. Spawn a read-only subagent
2. Give it the mod directory paths and `sptPath`
3. It calls `spt_analyze_conflicts` with the full mod list
4. It returns a distilled conflict report
5. You present the report to the user

This keeps the bulk data out of your context while producing the analysis.

## See also

- `spt-conflict-audit` -- conflict analysis workflow using these tools
- `docs/wayfinder/findings/001-spt-conflict-taxonomy.md` -- 20-type conflict taxonomy
- `docs/internal/mcp-specs/spt-mcp-design.md` -- MCP server design spec
