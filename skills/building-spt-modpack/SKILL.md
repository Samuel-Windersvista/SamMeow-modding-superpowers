---
name: building-spt-modpack
description: "Use when assembling or building the SPT modpack -- MO2 profile generation, mod overlay assembly, build execution. Triggers: 'build the pack', 'assemble', 'generate profile', '构建整合包', '打包', 'create MO2 profile'."
---

# Building SPT Modpack

Execute the modpack build: take the user's confirmed mod list and assemble it into a working MO2 profile.

## Prerequisites

- Mod list confirmed by user (from `curating-spt-modpack` stage 4)
- Conflicts resolved or accepted
- MO2 installed and SPT 4.1 game instance configured
- SPT 4.1 installed at user's configured path

## Build process

### Step 1: Prepare MO2 instance

1. Verify MO2 is running with the SPT 4.1 instance selected
2. If no instance exists, create one pointing at the user's SPT 4.1 installation
3. Create a new profile named after the modpack (e.g., `Norvinsk-0.1.0`)

### Step 2: Install mods into MO2

For each mod in the confirmed list:

1. **From Forge archive** (most common):
   - Locate the mod's release zip in `knowledge/spt-kb/archive/forge/mods/<id>_release/`
   - Install into MO2 as a new mod overlay
   - Name the overlay: `<category>-<mod-name>-<version>`

2. **From source** (custom-developed mods):
   - Build the mod if not already built (`dotnet build -c Release`)
   - Create an MO2 mod overlay with the compiled DLL(s)
   - Server mod DLLs go in overlay path `SPT_Runtime/user/mods/<ModName>/`
   - Client mod DLLs go in overlay path `BepInEx/plugins/`

3. **Set mod priority** in MO2:
   - Foundation/library mods first (lowest priority number)
   - Core overhaul mods next
   - Content mods after that
   - Fine-tuning mods last (highest priority number)
   - MO2 priority determines file overwrite order (higher = wins)

### Step 3: Configure mods

Some mods need post-install configuration:
- Server mod configs: `<SPT>/SPT_Runtime/user/mods/<ModName>/config.json` or similar
- BepInEx configs: `<SPT>/BepInEx/config/<plugin-guid>.cfg`
- These files are generated on first run; pre-seed them if the user has specific preferences

### Step 4: Verify build

Route to `testing-spt-modpack` for post-build verification.

**Minimum verification (Level B):**
1. Launch SPT via MO2
2. Confirm SPT.Server.exe starts without errors
3. Confirm SPT.Launcher.exe connects to server
4. Confirm game reaches main menu
5. Parse server log for each mod's registration confirmation
6. Parse BepInEx console output for each plugin's load confirmation

**If verification fails:** route to `diagnosing-spt-problems` with the failure details.

### Step 5: Record build

After successful verification:
1. Log the build to the modpack's dev-log (route to `writing-spt-modpack-devlog`)
2. Record: mod list with versions, MO2 profile name, build date, verification results
3. If this is a release build, route to `writing-spt-modpack-changelog`

## Build output

The build output is an **MO2 profile** that can be:
- Shared as an MO2 profile export (for other MO2 users)
- Documented as a mod list with install instructions
- Backed up for rollback purposes

**Note:** Self-contained distribution (non-MO2) is reserved for special cases. The primary distribution format is MO2 profile.

## Anti-patterns

- **Do not** write mod files directly into the SPT installation directory -- always use MO2 overlays
- **Do not** skip conflict analysis before building -- even if the user says "just build it"
- **Do not** hardcode MO2 or SPT paths -- use configured paths from environment setup
- **Do not** declare a build successful without Level B verification

## See also

- `curating-spt-modpack` -- the curation pipeline that feeds this skill
- `testing-spt-modpack` -- post-build verification
- `diagnosing-spt-problems` -- failure triage when build verification fails
- `writing-spt-modpack-devlog` -- build logging
- `setting-up-spt-modding-environment` -- MO2/SPT path configuration
