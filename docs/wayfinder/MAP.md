# Wayfinder Map: SPT Toolchain Systematization

> Label: `wayfinder:map`
> Created: 2026-08-02
> Tracker: local-markdown (GitHub Issues pending enablement)

## Destination

Build a dual-purpose SPT 4.1 toolchain on the bgs-modding-superpowers skeleton:

- **Mod development**: AI writes new mods (server C# / client BepInEx), human defines functionality specs
- **Modpack curation**: AI analyzes local mod archives for conflicts, human makes inclusion/exclusion decisions, AI executes mechanical build via MO2

Core new component: `spt` MCP server -- replaces xEdit's role for SPT mod analysis (BepInEx plugin metadata, server mod IModMetadata, file overwrite conflicts).

Input source: local Forge archives only (1822 mod metadata, 95 release zips, 124 source backups -- 4.x-compatible hot mods fully covered, see `knowledge/spt-kb/archive/forge/README.md`). No live Forge dependency -- Forge is shutting down.

MO2 retained as mod management layer. SPT native directory structure not used directly.

Target version: SPT 4.1 (final locked version, may never change).

## Notes

- Domain: SPT (Single Player Tarkov) mod development + modpack automation
- bgs upstream skills serve as architectural reference; SPT skills will gradually replace them
- SPT 4.1 server source fork: `E:\云文件\GitHub\SamMeow_SPT410_source_code`
- SPT archive (20 repos): 4 repos merged to `external/spt-archive/` (see #6); 16 repos remain at `E:\云文件\GitHub\SPT-archive`
- Feasibility report: `docs/可行性研究报告-SPT整合包自动化搭建.md` (v3.0, baseline SPT 3.11.4)
- Gradual replacement of bgs upstream capabilities with SPT equivalents
- IL decompilation pipeline reserved as future capability (Mono.Cecil / ICSharpCode.Decompiler)
- Mod dev: AI writes code, human defines what the mod does
- Modpack: AI analyzes + executes, human decides what goes in the pack

## Decisions so far

- [Survey SPT 4.1 mod conflict surface](tickets/001-survey-spt-mod-conflict-surface.md) -- 20 conflict types catalogued; server = dual-semantic (last-writer-wins vs first-registered-wins); client detection almost entirely IL-level; 4 hard-conflict categories crash at startup. [Findings](findings/001-spt-conflict-taxonomy.md)
- [Survey MO2 VFS integration for SPT](tickets/002-survey-mo2-vfs-integration.md) -- VFS feasible with custom game plugin; MO2 priority = file-overwrite only, cannot control load order; 3 integration options documented (A/B/C, C recommended). [Findings](findings/002-mo2-vfs-spt-integration.md)
- [Define modpack build pipeline stages](tickets/004-define-modpack-build-pipeline.md) -- 6-stage pipeline confirmed (intent -> match/dev -> conflict -> review -> build -> verify); build output = MO2 profile (option A primary); verification = launch + mod-loading log confirmation; usvfs propagation test is hard prerequisite.
- [Define SPT skill inventory and routing](tickets/003-define-spt-skill-inventory.md) -- 14 skills defined (9 mapped, 3 transformed, 2 new); dropped load-order/archive/papyrus; trigger table with bilingual triggers confirmed; spt MCP + MO2 game plugin implementations surfaced as dependencies.
- [Define mod development agent workflow](tickets/005-define-mod-dev-workflow.md) -- two independent pipelines (server/client); templates in-repo; source fork = reference-only, compile refs from installed SPT; feedback loop = B baseline (compile+deploy+launch+log verify) with optional C upgrade (auto functional check) per mod type.
- [Merge SPT-archive into main repo](tickets/006-merge-spt-archive.md) -- 4 repos merged to `external/spt-archive/` (server-mod-examples, modules, mod-examples, wiki; ~64 MB, no git history); 16 repos excluded (remain external); paths updated in INDEX.md and README.
- [Restructure spt-kb for agent consumption](tickets/007-restructure-spt-kb.md) -- YAML frontmatter added to all 71 .md files (47 wiki + 24 curated); index.json generated (filterable by version/domain/topic); INDEX.md updated with agent pointer.
- [Validate usvfs propagation](tickets/008-validate-usvfs-propagation.md) -- **CLOSED 2026-08-05**: SPT-specialized MO2 done (`SamMeow-Tarkov-specific-Mod-Organizer`); usvfs logs + VFS probe confirm 3-process chain (Server/Launcher/EFT) injected and file-level visibility works. Pipeline can proceed with VFS-based MO2 profile (option A/C); no fallback to option B.

- SPT-specialized MO2 -- **DONE 2026-08-05**: dedicated SPT MO2 variant at `E:\云文件\GitHub\SamMeow-Tarkov-specific-Mod-Organizer` (build: `E:\build\spt-mo2\prefix\install\bin`); multi-version game plugins (3.7 Aki / 3.8-3.11 / 4.0 / 4.1); bridge bat orchestration; webview2 blacklist; local saves; zh_CN l10n; release 0.1.0. usvfs propagation validated (#8 closed).

## In progress (2026-08-07)

> **进度快照过期提示（2026-08-22 标注）**：本节 #9 内嵌进度停在 2026-08-07。最新全局状态以 [handoff-20260822.md](handoff-20260822.md) 为准（迁移主线冻结于 08-11；本周期为 SPT 4.1.3 突变应急 + PerformanceTweaks413 v0.3.0 + fork 源码审查 + 议会基础设施修复）。迁移主线细节沿革：handoff-20260811.md、handoff-20260819.md。

- **[Migrate 3114 modpack to SPT 4.1](tickets/009-migrate-3114-to-spt41.md)** -- EPIC: port Life_in_Norvinsk_v0.3.2 (165 enabled mods) to SPT 4.1.2 at MO2 instance `Inescapable Tarkov`. Per-mod triage: Forge 4.1 version (A) / port from 3114 source (B) / escalate to Overseer (C). Stages M0-M6; first full-chain exercise of the toolchain. **Batch 0 compatibility pre-test CANCELLED (2026-08-07, Overseer directive): 4.0-constrained mods are definitively incompatible with 4.1.x (client bump breaks them; server mods also cannot be trusted) -- B bucket = full recompile-fix, no verify-as-is short-circuit.** **M1 DONE 2026-08-07** (8/8 bucket-A overlays filled). **M3: priority 1 DONE (9/69) + priority 2 15/20 DONE (Artem/Painter/TGC recompiled 4.1.2 via decompile pipeline; 12 more patched via SptVersion-patch method -- no recompile needed, modlist 87 consistent 3-way). Remaining p2: 1263 EpicRangeTime + 2132 CornerStore (7z BCJ2 extraction blocked, need 7-Zip). server-mod-311-to-41.md ch.7 complete (refactor + recompile pipeline + decompile traps + SptVersion patch method ch.7.7). KB 93 entries.** -- QuestTracker 1140, Croupier 1971, brightlasers 1358, HollywoodFX 2003, TarkovHDRework 1896, bossemedals 1539, ref-sptfriendly 1538, IncreaseClimbHeight 1575, HoodsEnergyDrinks 1688 -- downloaded 4.1 builds from Forge (7z/zip), deployed, modlist 71 enabled consistent 3-way. C-bucket split: 9 have 4.1.x (DONE), 18 at ~4.0 (priority 2: easy 4.0->4.1), 33 at 3.x (priority 3: hardest, pack-value eval needed), 1 404 (BetterBackpacks). M2 was 44/49 B-bucket (all deployed). Bundle knowledge: bundle-compat-311-to-41.md (26 checked, author patterns). Server knowledge: server-mod-311-to-41.md. Blender MCP + spt-mcp working. Known gaps: 2299 source-wrong, 865s/2162s/2246 server TS->C#, 1923 Server 4.0.5, HeliCrash UnityToolkit. Forge daily check running 08:20.

## In progress (2026-08-05)

- **spt MCP server** (`tools/spt-mcp/`) -- core component for modpack curation (#3/#4 dependency). Implemented + verified: 7 tools (list/read/scan/analyze/predict/forge_search/kb_query); **fixed critical bug** -- server mod detection now reads real 4.1 DLL IModMetadata via .NET helper (`tools/spt-mcp/helper/`, AsmResolver reads ctor IL) instead of non-existent package.json; 49 tests green; e2e verified against real SPT_410 mods (found real bundles.json conflict between Skills Extended & Secure Mapbook Mod). Remaining: client DLL BepInPlugin attribute reading (BepInPlugin GUID/BepInDependency), IL-level conflict detection (deferred to IL pipeline).
- **3114 modpack source archive** -- collected source for 91/94 real mods into Forge archive (`archive/forge/mods/<id>_source/`, 77 dirs, ~59 MB source-only); MANIFEST at `archive/3114-sources/MANIFEST.md`; 3 mods have no public source (Chinese community/self-made). Lessons learned documented at `knowledge/spt-kb/curated/operations/destructive-operation-guardrails.md`. **SUPERSEDED 2026-08-07**: source backup extended to ALL 4.x-compatible hot mods (84/84 covered, 124 dirs total: 65 git clone + 59 zip extract; source URLs backfilled into `hot-index.json` github field, 65/95 populated; method = Forge page "Source Code" block regex extraction, see `archive/forge/README.md`).
- **IL decompilation pipeline (basic, integrated)** -- **DONE 2026-08-05 (v3 refined + integrated into conflict-engine)**: `tools/spt-mcp/il-helper/` (Mono.Cecil 0.11.6) reads client DLL Harmony patch targets + IL behavior. **Integrated into conflict-engine as severity "I" (IL level)**: `detectIlConflicts` (high = same targetMethod truncated by 2+ plugins' Prefix-return-false; medium = same method writesField/writesResult by 2+; low = shared target). End-to-end on 3114 modpack (165 enabled mods): **149 analyzed, 274 patches extracted, 0 B / 0 O / 24 I (4 medium / 20 low, 0 high)**. Reports: `curated/operations/3114-il-conflict-report-v3.md` (source-level verdict), `v2.md` (bug records). Methodology rules: `curated/operations/conflict-analysis-input-model.md`.

## Recently completed (2026-08-06 / 08-07)

- **SPT 4.1.2 environment upgrade** -- **DONE 2026-08-06 (installed), VERIFIED 2026-08-07**: server at `E:\Game\EFT_Offline\SPT_410` runs 4.1.2-RELEASE+cf04a11 (`SPTarkov.Server.Core.dll` FileVersion=4.1.2, ProductVersion embeds commit `cf04a112` = fork tag `4.1.2`); client binary unchanged vs 4.1.1 (EFT 0.16.9.5.40743); 4.1.1->4.1.2 diff = 6 files, no API breaks. Environment state table in `knowledge/spt-kb/VERSIONS.md`.
- **LootingBots 3.11 -> 4.1 port** -- **DONE 2026-08-07 (verified 7/7 patches enabled)**: full type-mapping port (25+ obfuscated names resolved via Mono.Cecil against 4.1.1 Assembly-CSharp), deployed to `SPT_410\BepInEx\plugins\`. Knowledge: `knowledge/spt-kb/curated/migration/pilot-experience-lootingbots.md` (mapping table, traps: global-namespace ObjectPool shadowing UnityEngine.Pool, C# using-alias non-transitivity, Harmony field-injection renames, Handbook price restructure -> Item.Template.CreditsPrice).

## Not yet specified

- ~~IL decompilation pipeline architecture~~ -- **BASIC VERSION DONE 2026-08-05** (`tools/spt-mcp/il-helper/` Mono.Cecil). Remaining: deeper IL analysis (Transpiler IL pattern matching, reflection interop detection), integrating into conflict-engine as severity "I" tier.
- Modpack version management -- how to version and track modpack releases post-Forge
- Post-Forge mod update strategy -- how to handle new mod versions without live Forge API

## Out of scope

- BGS modding workflow improvements (upstream concern, separate project)
- Live Forge API integration (Forge shutting down)
- SPT 3.11.x support (3.11 materials are reference-only)
