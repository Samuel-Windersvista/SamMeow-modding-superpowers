# Ticket: Survey SPT 4.1 mod conflict surface

> Label: `wayfinder:research`
> Status: **closed** (2026-08-02)
> Blocks: #3, #4
> Blocked by: (none)

## Resolution

Research complete. Findings: [001-spt-conflict-taxonomy.md](../findings/001-spt-conflict-taxonomy.md)

**Key outcomes:**
- 20 conflict types identified across server/client/cross-layer, each rated by severity (B/S/O/C) and detection method (M/I/R)
- Server-side arbitration is dual-semantic: tables/configs = last-writer-wins (TypePriority → ModGuid), routes = first-registered-wins
- Client-side conflict detection is almost entirely IL-level or runtime-only -- metadata surface is very thin
- 4 hard-conflict categories crash at startup (DI constructor failure, config type mismatch, enum duplicate, wwwroot URL collision)
- Profile-level conflicts (removing trader/quest/item mods) can permanently break saves
- Cross-layer best practices documented from real mods (Questing Bots source-verified patterns)

## Question

What can conflict between SPT 4.1 mods? Produce a conflict taxonomy covering:

**Server mods (C# / IModMetadata / DI):**
- Table injection collisions (two mods injecting into the same database table)
- Route/handler overrides
- Config key collisions
- Dependency injection service registration conflicts
- Load order / priority issues

**Client mods (BepInEx):**
- BepInEx plugin load order and dependencies
- Harmony patch target collisions (two mods patching the same method)
- Harmony patch priority conflicts
- File overwrite conflicts (same file in BepInEx/plugins or SPT/user/mods)
- Config file (BepInEx .cfg) key collisions

**Cross-layer:**
- Server mod expects a client mod that isn't present (or vice versa)
- Version mismatches between paired server/client components

**Sources to survey:**
- `knowledge/spt-kb/wiki/` -- modding docs
- `knowledge/spt-kb/curated/api-notes-4.1/` -- DI/loading/config/database notes
- `knowledge/spt-kb/curated/recipes/` -- task recipes showing mod interaction patterns
- `knowledge/spt-kb/archive/forge/` -- real mod metadata for concrete examples
- `E-Mod开发示例/server-mod-examples/` -- official mod examples
- `A-核心服务端/modules/` -- official client module source

## Output

A conflict taxonomy document classifying SPT mod conflicts by type, detection method (metadata-level vs IL-level), and severity. This taxonomy drives the `spt` MCP server's conflict detection capabilities.
