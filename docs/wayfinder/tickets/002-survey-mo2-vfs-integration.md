# Ticket: Survey MO2 VFS integration approach for SPT

> Label: `wayfinder:research`
> Status: **closed** (2026-08-02)
> Blocks: #4
> Blocked by: (none)

## Resolution

Research complete. Findings: [002-mo2-vfs-spt-integration.md](../findings/002-mo2-vfs-spt-integration.md)

**Key outcomes:**
- MO2 VFS can overlay SPT mod directories, but requires a custom Python game plugin (`mobase.IPluginGame`) pointing dataDirectory at SPT root
- MO2 priority only arbitrates file overwrites -- cannot control SPT server load order (TypePriority + ModGuid) or BepInEx plugin order (BepInDependency topology)
- Control plane BGS-specificity concentrated in `plugins.*` and `profile.initialize`; `mods.*`, `organizer.*`, `launch.*` directly reusable
- Biggest technical risk: usvfs process propagation across 3-process chain (Server → Launcher → game exe) -- needs empirical validation
- Three integration options documented: A (full VFS), B (no-VFS build pipeline), C (hybrid -- recommended)
- Note for #6: preserve `build-4.0-assets/BepInEx/` structure as stock baseline evidence

## Question

How does MO2's virtual file system (USVFS) map to SPT's mod loading model? Design the integration approach.

**Key differences from Bethesda games:**
- Bethesda: `.esp/.esm/.esl` plugin files loaded by game engine, MO2 manages Data/ overlay
- SPT: BepInEx client plugins (DLLs in `BepInEx/plugins/`), server mods (DLLs in `user/mods/`), config files, and asset bundles -- no plugin file format, no load order file

**Questions to answer:**
- Can MO2's VFS overlay work for SPT's directory structure? (SPT expects mods in specific folders)
- How do MO2 profiles map to SPT modpack configurations?
- How does MO2's mod priority/ordering translate to BepInEx plugin load order and server mod priority?
- What does the MO2 control plane need to expose for SPT? (current control plane is BGS-specific)
- Can MO2's conflict detection (file overwrite) work for SPT mods?

**Sources:**
- MO2 documentation and source (USVFS)
- Existing MO2 control plane code in this repo (`scripts/install-mo2-control-plane.ps1`, C++ plugin)
- SPT installation structure (from wiki: `knowledge/spt-kb/wiki/Installing_Mods.md`)
- SPT 4.1 server source: how it discovers and loads mods

## Output

An integration design document: how MO2 + SPT work together, what the control plane needs to expose, and how mod priority/ordering maps across the two systems.
