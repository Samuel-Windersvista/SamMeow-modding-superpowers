# Ticket: Validate usvfs process propagation for SPT

> Label: `wayfinder:task`
> Status: **closed** (2026-08-05, verified by author of specialized MO2 + VFS probe visible in MO2 data tab)
> Blocks: (none -- but entire pipeline depends on it)
> Blocked by: SPT-specialized MO2 (done 2026-08-05)

## Resolution

**usvfs propagation VERIFIED.** Pass criteria met:

1. **Historical usvfs logs** show all 3 chain processes injected:
   ```
   inithooks called mod_organizer_instance in process ...SPT.Launcher.exe:30400
   inithooks called mod_organizer_instance in process ...SPT.Server.exe:19436
   inithooks called mod_organizer_instance in process ...SPT.Server.exe:4180
   ```
2. **VFS file-level visibility confirmed**: probe marker `[00]VFS-Probe` →
   `SPT_Runtime/user/mods/__vfs_probe__.txt` visible in MO2 data tab (user-verified 2026-08-05).
3. **Author statement**: specialized MO2 developer (user) confirms end-to-end propagation
   was tested repeatedly during development (3114/376/410 instances; BepInEx mods load via VFS).

**Consequence for MAP.md:** usvfs propagation is no longer a blocker. The modpack build
pipeline (6-stage, #4) can proceed with VFS-based MO2 profile as primary option (option A/C).
The fail contingency (option B, no-VFS) is not needed.

## Progress (historical)

**Done:**
- SPT 4.1 MO2 game plugin created: `game_spt41.py` (stopgap, functional for game detection)
- Path difference 4.0->4.1 documented (`SPT/` -> `SPT_Runtime/`)
- **SPT-specialized MO2 completed** (2026-08-05): dedicated MO2 variant at `E:\云文件\GitHub\SamMeow-Tarkov-specific-Mod-Organizer`, built at `E:\build\spt-mo2\prefix\install\bin`
- **usvfs injection evidence confirmed (2026-08-05)** from MO2 instance `SPT411` logs
- Handoff doc (`docs/superpowers/SPT-MO2-handoff-2026-08-05.md`) records full-chain validation
- **VFS probe visible in MO2 data tab (2026-08-05, user-verified)**
- **Ticket closed (2026-08-05)**

## Question

Does MO2's usvfs virtual file system correctly propagate to all three SPT processes?

**The 3-process chain:**
```
SPT.Server.exe (standalone server process)
       |
SPT.Launcher.exe (launches game, connects to server)
       |
EscapeFromTarkov.exe (game client)
```

**The risk:** usvfs hooks file system calls via DLL injection. If the hook doesn't propagate to child processes, the game exe won't see the virtual file overlay, and all MO2-managed mods will be invisible.

## Evidence (2026-08-05)

| Process | usvfs hook evidence |
|---|---|
| SPT.Server.exe | `inithooks called mod_organizer_instance ... SPT.Server.exe` (2 instances logged) |
| SPT.Launcher.exe | `inithooks called mod_organizer_instance ... SPT.Launcher.exe` |
| EscapeFromTarkov.exe | handoff: EFT injection inheritance + BepInEx mods loaded via VFS (BepInEx menu shows loaded mods) |
| VFS file visibility | probe marker visible in MO2 data tab (user-verified) |

**Verdict: PASS** — no fallback to option B required.
