# Release Notes

## v0.2.0-spt (unreleased)

SPT-only release of the SamMeow SPT modding toolkit -- an OpenCode plugin for SPT 4.1 mod development and modpack automation.

### SPT-only cleanup

- Removed the entire parallel BGS half: skills, knowledge base, MCP servers, tools, scripts, and the non-OpenCode harness entrypoints. The repository is now SPT-only and OpenCode-only.
- Renamed the plugin and package to `spt-modding-superpowers`. The OpenCode plugin entrypoint now injects the SPT bootstrap only.
- MCP declaration surface is exactly `mo2` + `spt`.
- Renamed shared environment variables away from the `BGS_` prefix.
- Removed the committed materialized plugin tree; distribution is built on demand via `scripts/build-portable-plugin.ps1`.
- Untracked build artifacts and vendored archives; hardened `.gitignore` so they cannot re-enter.
- Rewrote the bootstrap verification suite around SPT-only invariants.
- Adopted the Matt agent-skills configuration: `docs/agents/` config files, a tracked `AGENTS.md`, a local-markdown issue tracker, and the five default triage labels.

### Architecture (wayfinder, 2026-08-02)

All architectural decisions locked via wayfinder map (`docs/wayfinder/MAP.md`). 7 tickets closed, 1 deferred.

### Added

- **SPT knowledge base** (`knowledge/spt-kb/`): wiki vendor copy (47 files), 4.1 mod dev guide (4 chapters), API notes (7), task recipes (12), Forge archive (1822 mod metadata, 95 release zips, 18 source clones). Machine-readable index at `index.json` (106 entries as of 2026-08-19, filterable by version/domain/topic).
- **SPT core skills** (5): `using-spt-modding-superpowers` (bootstrap + routing), `writing-spt-mod` (mod dev from templates), `curating-spt-modpack` (6-stage curation pipeline), `spt-conflict-audit` (20-type conflict taxonomy), `building-spt-modpack` (MO2 profile build).
- **SPT 4.1 MO2 game plugin** (`game_spt41.py`): stopgap compatibility plugin for generic MO2, handles SPT_Runtime/ path layout.
- **Conflict taxonomy** (`docs/wayfinder/findings/001-spt-conflict-taxonomy.md`): 20 conflict types across server/client/cross-layer, rated by severity (B/S/O/C) and detection method (M/I/R).
- **MO2 VFS integration analysis** (`docs/wayfinder/findings/002-mo2-vfs-spt-integration.md`): 3 integration options (A/B/C), control plane differences, process propagation risk assessment.

### Changed

- **README**: updated to reflect the SPT-only toolkit, wayfinder decisions, and current progress.
- **KB INDEX.md**: updated paths to internal `external/spt-archive/`, added agent index pointer.

### Deferred

- usvfs process propagation validation (#8) -- ~~awaiting SPT-specialized MO2~~ **已解决（2026-08-05，ticket #8 CLOSED，见 wayfinder/MAP.md）**.
- ~~`spt-mcp-automation` skill~~ -- **已完成（2026-08-04，见下方 Completed）**；此行系时序记录残留，保留以反映演进.
- `using-spt-translator` skill -- awaiting SPT translation approach.
- IL decompilation pipeline -- ~~architecture reserved~~ **基础版已完成（3114 IL 冲突报告管线，tools/spt-mcp/il-helper）**.

### Completed (2026-08-04)

- 8 SPT support skills: `setting-up-spt-modding-environment`, `maintaining-spt-modding-environment`, `evaluating-spt-mods`, `interpreting-spt-mod-instructions`, `diagnosing-spt-problems`, `testing-spt-modpack`, `writing-spt-modpack-devlog`, `writing-spt-modpack-changelog`. All targeting SPT 4.1, with the local Forge archive as data source, MO2 integration, and conflict taxonomy wiring.
- Mod templates: `templates/server-mod/` (net10.0, IModMetadata + DI + table injection pattern) and `templates/client-mod/` (netstandard2.1, BepInEx + Harmony + config pattern). All API signatures verified against SPT 4.1 source code. Both templates compile clean (0 errors, 0 warnings) against stub assemblies. `{{PLACEHOLDER}}` format for agent find-and-replace scaffolding.
- `spt-mcp-automation` skill: hub skill for spt MCP server operations, routing doctrine, anti-patterns, sub-agent recipes.
- `spt` MCP server design spec (`docs/internal/mcp-specs/spt-mcp-design.md`): 10 tools across 5 categories (mod inventory, conflict analysis, Forge archive, knowledge base, MO2 bridge). No daemon -- pure file-system analysis. .NET helper CLI for DLL attribute reading (deferred).
- `spt` MCP server implementation (`tools/spt-mcp/`): 7 tools operational (list-mods, read-mod-metadata, scan-mod-files, analyze-conflicts, predict-load-order, forge-search, kb-query). Conflict engine with GUID duplicate/version mismatch/file overlap/config collision detection + load order prediction. 46/46 unit tests passing. TypeScript strict mode, zero type errors. Smoke tested via stdio MCP protocol.
- Usage guide (`docs/使用指南.md`): quick-start for Overseer -- environment setup, mod writing, modpack building, knowledge base lookup, mod search, troubleshooting, progress tracking, system boundaries, file navigation.
- Plugin registration: `spt` MCP server registered in the OpenCode plugin `config.mcp` hook (source at `tools/spt-mcp/`). MCP handshake verified (spt-mcp v0.1.0).
- Template real-world validation: server-mod template compiled against real SPT 4.1 installation (0 errors, 0 warnings). Fixed missing `SemanticVersioning.dll` reference in csproj (bug found by real compilation, not caught by stub-based verification). Client template DLL references verified present.
- MCP real-world validation: conflict analysis detected real version mismatch (AlgorithmicLevelProgression declares SPT 3.11, target is 4.1 -> Breaking). forge-search and kb-query verified against live data (17 SAIN mods, 12 recipes). Fixed KB root path resolution when running from the plugin tree (KB-root env var injected in plugin registration). Known limitation: server mods without package.json (e.g., BarlogM-Unda) are skipped -- requires future .NET helper CLI for DLL attribute reading.
- Feasibility study: 3.11 -> 4.1 mod migration (`docs/feasibility-311-to-41-migration.md`). Analyzed real mod sources from Forge archive. Key finding: 3.11 -> 4.1 is a complete rewrite (TypeScript -> C#, tsyringe -> SPT DI, different API surface), not a migration. Success probability: 4.0->4.1 = 85-95%, 3.11 simple mods = 70-85%, 3.11 complex mods = 20-40%. Biggest gap: no 3.11->4.1 API mapping document.
- Exploration: bundle upgrade path + Blender MCP integration (`docs/exploration-bundle-and-3d-pipeline.md`). Analyzed Life_in_Norvinsk modpack (177 mods, 2622 bundles, 17.9 GB). Key finding: SPT 3.11 and 4.1 share the same Unity version (2022.3.43f1) -- 75-85% of bundles can be directly copied, only 3-8% need rebuilding. Bundle migration is NOT the bottleneck; DLL recompilation is. Blender MCP feasible for static item modeling (B+), not for weapon rigging/animation (D). Detailed findings in `docs/wayfinder/findings/003-bundle-upgrade-analysis.md` and `004-blender-mcp-analysis.md`.
- Bundle difference audit (`docs/bundle-difference-audit.md`): direct file-level verification (not doc inference). Ground truth: both SPT 3.11 and 4.1 games use Unity 2022.3.43f1, but 85.4% of the 2622 modpack bundles were built with Unity 2019.4 (forward-compat in 3.11). 0/2622 bundles reference obfuscated class names -- MonoBehaviour bindings (PreviewPivot, EFT.Visual.LoddedSkin, HotObject) are stable across versions. Real differences: shader patch-level drift (needs per-mod in-game verification, symptom = purple models) and IsBundleMod metadata removal. Migration requires per-mod runtime verification, not blind copy.
- Auto-migration feasibility report (`docs/feasibility-auto-migration-pipeline.md`): full assessment of automated 3.11.4 -> 4.1 mod migration pipeline (including bundles). Based on real scans of Life_in_Norvinsk (91 TS server mods, 132 DLLs, 2622 bundles). Council-reviewed (B+ grade). Core numbers: server TS->C# 67% auto (assuming locale blocker solvable), client DLL 50-70% (with source) / 20-30% (no source), bundle 80-90%, overall zero-human ~35-45%. 5 blockers identified; locale write blocker downgraded from "hard" to "likely solvable, needs 10-min experiment". 3 P0 gates: 4.1 dbdump, 3.11 dbdump, GClass numbering alignment. Supporting research in `docs/wayfinder/findings/005-auto-ts-to-csharp-conversion.md` and `006-auto-client-bundle-migration.md`.
- **Locale blocker SOLVED (v1.2, empirically verified)**: discovered SPT 4.1's official locale modification mechanism -- `LazyLoad<T>.AddTransformer()` (used by PostDbLoadService.RenamePreraidLocales). Built and deployed LocaleTest mod to 4.1 server: transformer registration + readback verification SUCCEEDED ("LocaleTest transformer WORKS on SPT 4.1"). ETT-class mods (locale text rewriting) are now automatable. Server-side automation rate upgraded from 67% to ~75%.
- **4.1 dbdump mod built and verified** (`tools/dbdump-mod/`): behavior-verification foundation. Dumps 6 key tables (templateItems 18.9MB, templateQuests 5.6MB, traders 4.3MB, globalsConfig 301KB, templateHandbook 515KB, locales 53MB incl. lazy-loaded transformer-applied state) to JSON for database-state diffing. Deployed and verified against real SPT 4.1 server.
- **3.11 dbdump mod built and verified** (`tools/dbdump-mod-311/`): TypeScript/JS behavior baseline tool. Dumps the same 6 tables from the 3.11 server (items 17.3MB, quests 5.0MB, traders 2.4MB, globalsConfig 286KB, handbook 493KB, locales 47.9MB). Enables 3-way diff (3.11 clean vs 3.11+mod vs 4.1+mod) for migration behavior equivalence.
- **P0 gates complete (v1.3)**: 4.1 dbdump PASSED, 3.11 dbdump PASSED, locale blocker SOLVED (empirically), GClass numbering PARTIALLY verified (mapping covers 84.1% of 3.11's 3920 GClass names; 624 missing scattered across range; 4.0 assembly unavailable for binary comparison; pragmatic path = mapping coverage + compile-error-driven iteration). Report updated to v1.3.
- **ETT end-to-end pilot PASSED**: first full migration pipeline run (TS->C# locale mod). All 6 stages verified: data carry-over (config/QuestInfo/GunsmithLocaleEN), TS->C# conversion using AddTransformer locale pattern, compile gate (0 errors, compiler caught ListOrT/Path/Trader.Id/Item.Tpl API mismatches), load gate (mod registered, no errors), behavior gate (dbdump diff confirmed all 5 locale text modifications applied: Leads to 9775x, Collector 4403x, Lightkeeper 1734x, Durability 425x, Requires key 1156x). Key learnings recorded in `knowledge/spt-kb/curated/migration/pilot-experience-ett.md` (JsonUtil case-sensitivity, nested requiredKeys shape, compile-error-driven iteration). Migration pilot project at `tools/migration-pilots/ett/`.
- **SecureMapbookMod bundle pilot PASSED**: second full pipeline run (TS->C# + item bundle). Validated: bundle file (4.94MB, Unity 2022.3.43f1) zero-change direct load, bundles.json zero-change compatibility, IsBundleMod field removal, CustomItemService.CreateItemFromClone migration. Fixed a real 3.11 bug: original slot ID generation used (char)(98+i) producing non-hex chars from 6th slot onward (MongoId crash in 4.1); migrated version uses valid hex. Behavior verified via dbdump (mapbook item 6621a2e3a8d8b1a9f0e3b4c5 created with slot filters). Learnings in `knowledge/spt-kb/curated/migration/pilot-experience-secure-mapbook.md`. Pilot project at `tools/migration-pilots/secure-mapbook/`.
- **SkillsExtended client migration: obfuscation + 90% API adaptation COMPLETE, deep logic rewrite remains (~8 points)**: resolved all 8 obfuscated class names via AsmResolver member-signature matching; 68 class-name + 35 EBuffId + 5 Item replacements; rewrote GetBarterPricePatch (Assortment/ItemPrice), MovementContextSetSpeedLimitPatch (SetPhysicalCondition), mapped Notification/Prone/Stats/Camera. Compile errors reduced from 40+ to ~8 independent deep-rewrite points (Item subclass->component system: HealthEffectsComponent/KeycardComponent/Silencer; LockPicking interaction types; SPT Utils API). Full mapping in `knowledge/spt-kb/curated/migration/client-obfuscation-mapping-skills-extended.md`, continuation point in `tools/migration-pilots/skills-extended/PROGRESS.md`.
- KB migration knowledge added (3 docs): `knowledge/spt-kb/curated/migration/api-mapping-311-to-41.md` (server API 1:1 mapping + 542-file usage stats + 5 blockers, locale blocker now marked SOLVED with AddTransformer pattern), `client-mod-311-to-41.md` (client migration guide), `bundle-311-to-41.md` (bundle migration guide). index.json now 74 entries.
