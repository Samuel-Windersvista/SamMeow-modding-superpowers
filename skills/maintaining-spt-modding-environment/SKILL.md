---
name: maintaining-spt-modding-environment
description: "Use after first-run for ongoing SPT modpack maintenance: update the spt-kb knowledge base (index.json, curated docs, version tags), prune spt-kb cache / stale Forge archive entries, health-check the environment (SPT + MO2 + templates), restore missing templates, or handle recurring SPT modding environment care. NOT for first-run setup (use setting-up-spt-modding-environment)."
---

# Maintaining the SPT Modding Environment

## When to use

- The environment is already set up and the user asks to maintain, refresh, update, or health-check it.
- The user says "maintain", "update KB", "prune cache", "health check", "维护", or "更新知识库".
- The user asks to regenerate or repair `knowledge/spt-kb/index.json`.
- The user asks to add, correct, or version-tag a curated doc in the spt-kb.
- The user asks to prune or clean the offline Forge archive (stale zips, superseded versions).
- The user asks to restore missing mod templates (`templates/server-mod/`, `templates/client-mod/`).
- The user asks whether to pin an SPT version or KB doc version.

## What this skill replaces

Use `setting-up-spt-modding-environment` for first-run: MO2 detection,
control-plane install, visible MO2 launch, SPT install detection, path
configuration, KB availability check, template verification, and first
semantic smoke.

This skill owns ongoing care after that first-run boundary: spt-kb updates,
Forge archive hygiene, template restoration, recurring environment health
checks, and version-pinning advice.

## What this skill does NOT cover (removed from the BGS lineage)

- **No Nexus credential management.** Forge is offline; there is no Nexus API
  key, no OAuth token, no `meta.ini` update-state refresh. Mod metadata comes
  exclusively from the local archive at `knowledge/spt-kb/archive/forge/`.
- **No xEdit.** There are no plugin records, no load-order files, no xEdit
  daemon to maintain.
- **No xSE / script-extender update cascade.** SPT client mods are BepInEx
  plugins pinned to the SPT runtime version, not to a game executable
  FileVersion. Version compatibility is governed by the SPT version (4.1
  locked), not a script-extender runtime tag.
- **No standalone translator CLI maintenance.** SPT translation routes through
  `using-spt-translator`, which reads mod text from the local archive; there is
  no PyPI CLI to install or upgrade.

## spt-kb structure and rules

The spt-kb at `knowledge/spt-kb/` is a **bundled, in-repo knowledge base** —
there is no remote pack distribution and no versioned cache root. Its layout:

| Path | Role | Rule |
|------|------|------|
| `index.json` | Machine-readable index (schema_version 1) | Keep in sync with actual files |
| `wiki/` | Official wiki vendor copy (read-only) | Never edit in place; write corrections to `curated/` |
| `curated/` | Refined layer: modding guide, API notes, recipes | Every doc carries version tags |
| `sources/` | Source registry (`repositories.md`, `third-party.md`) | Record provenance for every external asset |
| `archive/forge/` | Offline Forge snapshot (catalog, hot-index, mod zips, source clones) | Read-only snapshot; prune only with consent |

Version tags follow `VERSIONS.md`: `[3.11]` reference-only, `[4.0]`, `[4.1]`
(the locked target), `[通用]` cross-version. New curated content must be tagged;
do not write untagged SPT facts into `curated/`.

## Check + apply spt-kb updates

1. Start by reading `knowledge/spt-kb/index.json`. Verify the `generated` date
   is recent and the entry list matches the files that actually exist under
   `wiki/`, `curated/`, and `archive/`.
2. If entries reference files that no longer exist, or files exist that have no
   entry, regenerate the index (see below) after fixing the underlying issue.
3. For wiki updates: the `wiki/` tree is a vendor copy. If upstream docs
   change, refresh the copy from the registered source (see `sources/`
   registry), then re-run the index build. Do not hand-edit wiki files to "fix"
   them; correct facts belong in `curated/` with a note pointing at the wiki
   discrepancy.
4. For curated updates: add or edit files under `curated/`, ensure frontmatter
   version tags are correct, then rebuild the index.
5. Get user consent before any deletion (pruning) or before downloading large
   replacements.

## Rebuilding the index

The index build is a mechanical step:

```powershell
# Regenerate index.json from the current wiki/ + curated/ + archive/ trees.
# (Repo tooling; see tools/ for the exact script if one exists.)
# Fallback: hand-maintain index.json entries for any added/removed file,
# keeping the schema_version: 1 shape (path/title/version/domain/topic/source).
```

After any change, smoke-check:

```text
read knowledge/spt-kb/index.json
-> entries have version/domain/topic fields
-> a 4.1 curated doc (e.g. curated/api-notes-4.1/mod-loading.md) is listed and opens
```

## Cache / archive hygiene (Forge snapshot)

The Forge snapshot under `knowledge/spt-kb/archive/forge/` is a **point-in-time
offline archive**. Its tools (`snapshot.ps1`, `fetch-versions.ps1`,
`fetch-releases.ps1`, `build-index.ps1`) were for the live API, which is now
offline — do not re-run them expecting fresh data.

Hygiene policy:

- **Keep** the catalog (`api/mods-catalog.json`), hot-index
  (`hot-index.json`), per-mod detail/version JSONs, and the README.
- **Prunable with consent**: superseded release zips under
  `mods/<id>_release/` when multiple versions are present and only the
  best-version zip is referenced by the hot-index; stale `incoming`-style
  partial downloads left by interrupted fetches.
- **Do not prune** source clones under `mods/<id>_source/` — they are the only
  surviving copy of that code if Forge is gone.
- Before deleting any archive file, surface the exact path list to the user and
  get explicit approval. Preview with a dry-run listing first.

If the archive is missing entirely (fresh clone), restore it from the plugin
distribution or a backup rather than re-fetching from the dead API.

## Template restoration

`writing-spt-mod` depends on `templates/server-mod/` and
`templates/client-mod/`. If either is missing:

1. Check the plugin distribution / git history for the templates.
2. Restore them under `templates/` (server-mod targets `SPTarkov.Server.Core`
   4.1.0; client-mod is a BepInEx plugin project).
3. Verify with the template smoke used by `writing-spt-mod` (project builds
   against the 4.1 references).
4. If restoration is not possible, surface `[GAP]` and do not claim the
   development workflow is available.

## Health checks

Run the health check when maintenance touched any part of the environment:

```text
SPT:   <SPT_Root>\SPT_Runtime\SPT.Server.exe and SPT.Launcher.exe exist
       <SPT_Root>\SPT_Data\ , <SPT_Root>\user\mods\ , <SPT_Root>\BepInEx\ exist
MO2:   <MO2_Root>\ModOrganizer.exe exists
       <MO2_Root>\plugins\mo2_agent_control.py exists
Templates: templates/server-mod/ and templates/client-mod/ exist
KB:    knowledge/spt-kb/index.json parses and lists 4.1 curated docs
Config: SPT_MO2_ROOT / SPT_ROOT env config matches the recorded paths
```

Expected: all markers present, no missing template, no index drift. If the
health check surfaces a failure, route to the appropriate skill:
`setting-up-spt-modding-environment` for broken setup state,
`diagnosing-spt-problems` for runtime failures, `testing-spt-modpack` for
verification of the installed pack.

## Custom curated content + source registration

To add a custom knowledge record to the spt-kb:

1. Author it under `knowledge/spt-kb/curated/<section>/<id>.md` with frontmatter
   carrying `version`, `domain`, `topic`, `source: curated` (same shape as the
   existing curated docs).
2. Tag the version per `VERSIONS.md` (`[4.1]` etc.) and state the provenance
   (which source file under `sources/` or which archive entry it derives from).
3. Add an entry to `index.json` matching the schema.
4. If the content came from an external source not yet registered, add it to
   `knowledge/spt-kb/sources/repositories.md` (or `third-party.md`) first.

Never write custom content into `wiki/` (read-only vendor copy).

## Version-pinning advice

- **SPT version is pinned by policy: 4.1 is the final locked target.** SPT 3.11
  materials are reference-only learning material, not install targets. Record
  any deviation in the modpack dev-log.
- **Forge mod versions**: the archive's `best_version` / `best_spt` fields are
  the canonical compatibility record. When curating, pin mod versions that
  match SPT 4.1 (or documented 4.0-compatible versions while the 4.1 ecosystem
  migrates — see `archive/forge/README.md` 4.1 status note).
- **KB doc versions**: follow the version tags. A `[4.0]`-tagged doc is not
  authoritative for 4.1 behavior; prefer the `[4.1]` tagged docs and the
  4.0→4.1 migration docs (`wiki/SPT_41/Server_40_to_41.md`,
  `wiki/SPT_41/Client_40_to_41.md`).

## meta.ini `comments=` vs `notes=` field distinction (MO2 retained)

MO2's `meta.ini` has TWO note-shaped fields with different semantics — this
applies unchanged because MO2 is the mod management layer:

- **`comments=`** is the SHORT preview shown in the MO2 GUI mod list's
  "Comments" column. Use this for 1-2 sentence summaries, version status tags
  (`[UPDATE→vX]`), etc.
- **`notes=`** is the LONGER text shown only in the mod's Notes tab. Use this
  for detailed install notes, config-file placement records, conflict
  resolution memos, version history, etc.

For automation that synthesizes per-mod summaries from Forge metadata, write
to `comments=` by default.

## Anti-patterns + warnings

- Never write directly into the SPT install or vanilla EFT install. Any
  game-local change goes through an MO2 mod overlay or overwrite surface.
- Do not re-run the Forge snapshot scripts expecting live data; the API is
  offline. The archive is a snapshot to be preserved, not refreshed.
- Do not delete Forge source clones (`mods/<id>_source/`) — they are the only
  surviving source copy.
- Do not prune archive content without user consent and a dry-run listing.
- Do not hand-edit `wiki/` vendor files; corrections belong in `curated/`.
- Do not add untagged curated content; every doc needs a version tag.
- Do not attempt Nexus credential reads or xEdit maintenance workflows — they
  do not exist in this ecosystem.

## See also

- `setting-up-spt-modding-environment` — first-run MO2 / SPT / KB / template orchestrator.
- `using-spt-modding-superpowers` — session bootstrap, available tools, and hard SPT modding rules.
- `knowledge/spt-kb/README.md`, `knowledge/spt-kb/VERSIONS.md`,
  `knowledge/spt-kb/archive/forge/README.md` — KB structure, version policy, archive layout.
- `docs/internal/superpowers/specs/2026-06-25-spt-modding-superpowers-design.md` — design reference.
