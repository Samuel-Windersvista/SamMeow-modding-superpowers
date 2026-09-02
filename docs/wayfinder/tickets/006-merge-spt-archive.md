# Ticket: Merge SPT-archive into main repository

> Label: `wayfinder:task`
> Status: **closed** (2026-08-02)
> Blocks: (none)
> Blocked by: (none)

## Resolution

Merged 4 toolchain-relevant repos into `external/spt-archive/` (plain copy, no git history, ~64 MB total):

- `server-mod-examples/` (main @ 7c78aa7e)
- `modules/` (master @ 425bf000)
- `mod-examples/` (master @ d0381f27)
- `wiki/` (main @ ba82cdff)

Excluded 16 repos (630 MB working tree + ~2 GB git history) -- remain in external `E:\云文件\GitHub\SPT-archive\`. Full manifest: `external/spt-archive/MANIFEST.md`.

Updated path references in `knowledge/spt-kb/INDEX.md` and `README.md`.

## Question

How to merge the 20 SPT-archive repos into SamMeow-modding-superpowers?

**Current state:**
- `E:\云文件\GitHub\SPT-archive\` contains 20 SPT official repos (server, modules, launcher, installer, forge, wiki, mod-examples, etc.)
- Each is a separate git repo with its own history
- Total size and relevance varies -- some are critical (server, modules), others may be less needed (sp-tarkov-website, db-website)

**Decisions needed:**
- Merge strategy: git subtree? submodule? plain copy with history stripped?
- Which repos to include vs exclude?
- Where in the monorepo do they live? (`external/spt-archive/<repo>/`? `knowledge/spt-kb/sources/<repo>/`?)
- Do we need full git history or just the locked commit snapshot?
- Size concern: 20 repos with history could be large

**User intent:** Merged, not kept as separate repos.
