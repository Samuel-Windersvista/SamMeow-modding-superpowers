---
name: writing-spt-mod
description: "Use when the user wants to write a new SPT 4.1 mod from scratch -- server mod (C#/IModMetadata/DI) or client mod (BepInEx/Harmony). Triggers: 'write a mod', 'create a mod', 'add a trader', 'add an item', '写个mod', '加个商人', '加个物品', 'add a quest', 'add custom weapon'."
---

# Writing SPT Mods

Write a new SPT 4.1 mod from scratch. Two independent pipelines: server mod and client mod. Choose based on what the mod does.

## Pipeline selection

| Mod does what? | Pipeline | Template |
|---|---|---|
| Add/edit traders, items, quests, database values, routes, bot config | **Server** | `templates/server-mod/` |
| Modify game behavior, UI, patches, client-side features | **Client** | `templates/client-mod/` |
| Both (paired mod) | **Both** -- two projects, shared spec | Both templates |

## Server mod workflow

### Step 1: Parse intent

Understand what the user wants in SPT terms:
- What game system does it touch? (traders, items, quests, loot, bots, etc.)
- What database tables does it need? (see `knowledge/spt-kb/curated/api-notes-4.1/database-structure.md`)
- Does it need custom routes? (see `api-notes-4.1/http-routing.md`)
- Does it need config? (see `api-notes-4.1/config-system.md`)

### Step 2: Research the API surface

Search the knowledge base for relevant references:
1. Read `knowledge/spt-kb/index.json`, filter by `version: ["4.1"]`, `domain: "server"`, and relevant `topic`
2. Check `knowledge/spt-kb/curated/recipes/` for a matching recipe (add-trader, add-item, add-quest, etc.)
3. If the recipe exists, follow it. If not, read the relevant API notes.
4. For API details not in the KB, read the 4.1 source at the user's configured `SPT410SourcePath` (see project config or ask user).

### Step 3: Scaffold from template

1. Copy `templates/server-mod/` to the user's mod project directory
2. Replace all `{{PLACEHOLDER}}` values:
   - `{{MOD_NAME}}` -- mod name (PascalCase, no spaces)
   - `{{MOD_GUID}}` -- unique GUID for the mod
   - `{{MOD_VERSION}}` -- initial version (default "1.0.0")
   - `{{MOD_DESCRIPTION}}` -- one-line description
   - `{{AUTHOR}}` -- author name
   - `{{SPT_INSTALL_PATH}}` -- path to user's SPT 4.1 installation (for DLL references)
3. Rename files: `ModMetadata.cs` -> match mod name, etc.

### Step 4: Write mod code

Follow the patterns from the knowledge base:
- DI registration: implement `IInjectable` with appropriate `TypePriority`
- Table injection: inject database tables via constructor, modify in `OnLoad()`
- Config: use `IConfigManager` or direct JSON config
- Routes: extend `AbstractRouter` for custom endpoints

Key references:
- `knowledge/spt-kb/curated/api-notes-4.1/di-container.md` -- DI system
- `knowledge/spt-kb/curated/api-notes-4.1/mod-loading.md` -- mod lifecycle
- `knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md` -- full anatomy

### Step 5: Build and verify

```
dotnet build -c Release
```

Compilation references come from the user's installed SPT (`$(SPTInstallPath)` property in csproj). Do NOT reference the source fork for compilation -- it is read-only reference.

**Verification baseline (Level B):**
1. Deploy DLL to `<SPT>/SPT_Runtime/user/mods/<ModName>/` (via MO2 or direct copy for dev)
2. Launch SPT.Server.exe
3. Check server console/log for mod registration confirmation
4. Check for DI errors, config type mismatches, or startup crashes

**Optional functional verification (Level C):** per mod type, where feasible:
- Added a trader? Query the trader list endpoint.
- Added an item? Check the item appears in the database.
- Modified loot weights? Compare before/after values.

## Client mod workflow

### Step 1: Parse intent

- What game behavior does it modify?
- What classes/methods does it need to patch? (see `knowledge/spt-kb/wiki/SPT_41/modding/client/Class_Name_Mappings.md`)
- Does it need BepInEx config entries?
- Does it depend on other client mods? (BepInDependency)

### Step 2: Research the patch targets

1. Read `knowledge/spt-kb/wiki/SPT_41/Client_40_to_41.md` for 4.1 changes
2. Read `knowledge/spt-kb/wiki/SPT_41/modding/client/Class_Name_Mappings.md` for deobfuscated class names
3. Check `external/spt-archive/modules/` for official client module source as reference
4. For deeper analysis, the user may need dnSpy/ILSpy to inspect `Assembly-CSharp.dll`

### Step 3: Scaffold from template

1. Copy `templates/client-mod/` to the user's mod project directory
2. Replace all `{{PLACEHOLDER}}` values (same format as server mod)
3. Rename files to match mod name

### Step 4: Write mod code

Follow BepInEx + Harmony patterns:
- Plugin entry: `BaseUnityPlugin` subclass with `[BepInPlugin]` attribute
- Patches: Harmony `PatchAll()` or manual `Harmony.Patch()`
- Config: `ConfigEntry<T>` via `Config.Bind()`

### Step 5: Build and verify

Same as server mod, but deploy to `<SPT>/BepInEx/plugins/`.

**Verification:** Check BepInEx console output for plugin load confirmation. Check for Harmony patch errors.

## Paired mod (server + client)

Some mods need both a server component and a client component:
1. Create two projects from the two templates
2. Server mod handles data/logic; client mod handles presentation/interaction
3. Communication via custom routes (see `knowledge/spt-kb/curated/recipes/10-mod-communication.md`)
4. Client can check server mod presence via reflection (see conflict taxonomy patterns from `docs/wayfinder/findings/001-spt-conflict-taxonomy.md`)

## Anti-patterns

- **Do not** reference the SPT source fork for compilation -- use installed SPT DLLs
- **Do not** hardcode paths -- use `$(SPTInstallPath)` property
- **Do not** skip the knowledge base lookup -- recipes exist for common tasks
- **Do not** guess API names -- verify against 4.1 source or API notes
- **Do not** create both server and client projects unless the mod truly needs both
