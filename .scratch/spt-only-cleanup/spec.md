# Spec: SPT-only cleanup + Matt skills integration

> **进展（Work Status）**: CLOSED — 2026-09-13（11 票完成）

Status: ready-for-agent
Type: task

## Problem Statement

This repository (`SamMeow-modding-superpowers`) is a personal, offline SPT (Single
Player Tarkov) modding toolkit forked from BB-84C's BGS (Bethesda Game Studios)
modding plugin. It has grown bloated and disorganized:

- It carries two complete, parallel skill trees (12 BGS skills + 12 SPT skills)
  plus near-duplicate generic/SPT devlog and changelog skills, even though the
  owner only does SPT work.
- It tracks ~49,535 files, of which ~770 are .NET build artifacts that churn on
  every build, ~21,370 are a vendored reference archive with no runtime consumer,
  ~13,301 are a materialized distribution copy of the plugin, and ~8,353 are
  Forge mod archives and source clones under the SPT knowledge base.
- The BGS half is wired into the harness itself (plugin entrypoint, MCP
  declarations, session hooks, build scripts, the materialized plugin tree), so
  it cannot simply be ignored.
- The repository has not adopted the Matt Pocock engineering-skills conventions:
  there is no `docs/agents/`, no `CONTEXT.md`, no `docs/adr/`, and no issue
  tracker.
- The only verification suite (`tests/bootstrap/`) is rotten: it asserts a
  pre-reshape repo shape and fails immediately.

The owner wants a lean, SPT-only production engine that can carry future modpack
production, with working verification and the Matt skills environment in place.

## Solution

Strip the BGS side entirely from the harness, skill tree, knowledge base, tools,
and scripts; collapse the repository to SPT-only; remove bloat from git tracking;
adopt the Matt Pocock agent-skills configuration; and rebuild the verification
suite around SPT-only invariants.

Everything removed is preserved by an `archive/bgs-final` git tag taken before
deletion (forward-delete, no history rewrite). The work lands on a
`chore/spt-only-cleanup` branch as themed commits.

## User Stories

1. As the repo owner, I want the plugin/package renamed to
   `spt-modding-superpowers`, so the name matches what the repository actually
   does.
2. As the repo owner, I want a fresh OpenCode session in this repo to inject the
   SPT bootstrap, so the agent starts from SPT rules rather than BGS rules.
3. As the repo owner, I want the BGS bootstrap marker gone, so no BGS context
   leaks into SPT sessions.
4. As the repo owner, I want only the MO2 and SPT MCP servers declared, so xEdit
   and BGS knowledge tools do not appear.
5. As the repo owner, I want all 12 BGS skills removed, so the visible skill set
   is SPT-only.
6. As the repo owner, I want the BGS knowledge base removed, so the knowledge
   base is SPT-only.
7. As the repo owner, I want BGS-only tools removed (xEdit MCP, xEdit hook
   bridge, BGS KB MCP, BGS archive, BGS Papyrus, BGS translator), so the tools
   tree is SPT-only.
8. As the repo owner, I want BGS-only scripts removed, so the scripts tree
   reflects the SPT toolchain.
9. As the repo owner, I want the shared tools' environment variables renamed away
   from the `BGS_` prefix, so naming is domain-correct.
10. As the repo owner, I want the committed materialized plugin tree removed from
    tracking, so the repo carries source only and distribution is built on
    demand.
11. As the repo owner, I want the Claude Code and Codex entrypoints and the
    session hooks removed, so only the OpenCode harness remains.
12. As the repo owner, I want no .NET build artifacts tracked, so `git status`
    stays clean across builds.
13. As the repo owner, I want the vendored SPT reference archive deleted locally
    and removed from tracking, so ~21,000 files leave the index.
14. As the repo owner, I want the Forge mod archive untracked but kept locally,
    so it stays usable without bloating git.
15. As the repo owner, I want the duplicate generic devlog and changelog skills
    removed in favor of the SPT versions, so there is one canonical pair.
16. As the repo owner, I want the stale docs archive and the early skill drafts
    deleted, so BGS-era plans do not confuse future work.
17. As the repo owner, I want the narrative fiction folder moved out to my
    knowledge base, so the engineering repo holds engineering docs only.
18. As the repo owner, I want the Matt agent-skills config files present, so the
    engineering skills know where issues, labels, and domain docs live.
19. As the repo owner, I want `AGENTS.md` tracked with an agent-skills block, so
    agents read the repo configuration.
20. As the repo owner, I want a local-markdown issue tracker, so I can plan and
    track work offline.
21. As the repo owner, I want the bootstrap verification entrypoint rewritten to
    assert SPT-only invariants, so I have a passing, meaningful acceptance suite.
22. As the repo owner, I want the MO2 control-plane tests to keep passing with
    SPT paths, so shared infrastructure stays verified.
23. As the repo owner, I want BGS-only tests removed, so the test tree is
    SPT-only.
24. As the repo owner, I want a pre-cleanup tag, so I can recover anything I
    deleted by mistake.
25. As the repo owner, I want the cleanup landed as themed commits on a dedicated
    branch, so the change is reviewable and reversible.
26. As the repo owner, I want a documented fresh-session smoke check, so I can
    confirm the harness rewire actually took effect.
27. As the repo owner, I want the tracked file count reduced to roughly a few
    thousand, so clones and backups are fast.
28. As the repo owner, I want the README and release notes updated to SPT-only,
    so the front door is not lying.
29. As the repo owner, I want a `.gitignore` that covers build artifacts and the
    vendored archives, so bloat cannot creep back.
30. As the repo owner, I want the `upstream` remote kept read-only, so I can
    consult the BGS original without merging it back.
31. As the repo owner, I want the in-progress SPT mods kept, so they stay with
    the toolkit.
32. As the repo owner, I want the curated SPT knowledge (index, curated guides,
    wiki snapshot) still tracked, so the curated knowledge survives.
33. As the repo owner, I want the live wayfinder decision map retained and its
    completed tickets archived, so the current planning context survives.

## Implementation Decisions

1. **Deletion safety.** Take an `archive/bgs-final` tag on `main` HEAD before any
   deletion. Forward-delete only; no history rewrite, no force-push. The fork
   relationship with upstream is preserved.
2. **Branching.** All work lands on `chore/spt-only-cleanup` as themed commits,
   merged to `main` at the end.
3. **Plugin identity.** Rename the plugin and package to
   `spt-modding-superpowers`: package manifest, OpenCode plugin entrypoint and its
   filename, the exported plugin function name, the bootstrap marker constant,
   README, and release notes.
4. **Bootstrap chain.** The OpenCode plugin entrypoint injects the SPT bootstrap
   skill. The BGS bootstrap marker constant and its injection path are removed.
5. **Harness scope.** OpenCode only. The Claude Code plugin manifest, the Codex
   plugin manifest, the agent marketplace manifest, the hook dispatcher, and the
   static MCP wiring file are removed. MCP declaration lives entirely in the
   OpenCode plugin's config hook, declaring MO2 and SPT only.
6. **Materialized tree.** The committed `plugins/` distribution tree is removed
   from tracking and ignored. The portable-build script is rewired to the new
   plugin name and SPT tool set, and is run on demand rather than committed.
7. **Shared-tool rename.** `BGS_MO2_ROOT` becomes `MO2_ROOT`; `BGS_SPT_KB_ROOT`
   becomes `SPT_KB_ROOT`, across shared tools and scripts. The owner's local
   environment config is updated to match.
8. **Skill tree.** The 12 BGS skills and the BGS bootstrap are removed. The
   generic devlog and changelog skills are removed in favor of the SPT-flavored
   pair. The SPT maintenance skill's naming is normalized for consistency.
9. **Knowledge.** The BGS knowledge base is removed. The SPT knowledge index,
   curated guides, wiki snapshot, and sources stay tracked. The Forge archive
   subtree becomes untracked but remains on disk.
10. **Git hygiene.** Untrack all `obj/` and `bin/` artifacts; delete the vendored
    SPT reference archive from disk; delete stray explorer artifacts; restructure
    `.gitignore` so build artifacts and vendored archives cannot re-enter.
11. **Docs.** Delete the docs archive and the early skill drafts. Move the
    fiction folder to the owner's knowledge base. Retain the wayfinder decision
    map, archiving completed tickets. Keep live SPT specs.
12. **Matt config.** Write the three `docs/agents/` config files; create a
    tracked `AGENTS.md` carrying the agent-skills block; adopt single-context
    domain layout, a local-markdown tracker, and the five default triage labels.
13. **Verification.** Rewrite the bootstrap verification entrypoint and its
    sub-checks as SPT-only invariant assertions: layout, skill set, bootstrap
    injection, MCP declaration surface, git hygiene, templates. Remove the
    sub-checks that assert the dead shape. Fix the MO2 acceptance script's
    hardcoded path. Remove the xEdit client tests.
14. **Scope discipline.** Infrastructure only. No new SPT features.

## Testing Decisions

- **What makes a good test here.** Assert external invariants, not implementation
  details. The external behavior of this effort is the repository's shape as an
  SPT-only OpenCode plugin: what a fresh session loads, which skills exist, which
  MCP servers are declared, and what git tracks.
- **Primary seam.** The existing bootstrap verification entrypoint. It is
  rewritten in place; no new seam is introduced. This keeps the seam count at
  one.
- **Sub-checks kept and rewritten:** layout, skills, templates.
- **Sub-checks added:** bootstrap injection (the plugin entrypoint injects the
  SPT marker and not the BGS marker); MCP declaration surface; git hygiene (no
  tracked build artifacts or vendored archives).
- **Sub-checks removed:** hooks (harness is OpenCode-only); foundation and specs
  (they assert the dead BGS-era shape).
- **Shared infrastructure seam.** The MO2 control-plane test suite and the MO2
  MCP acceptance script must keep passing after the environment-variable rename
  and path fix.
- **Prior art.** The existing PowerShell `verify-*.ps1` pattern — scripts that
  throw on missing paths or phrases — is the established style and is preserved.
- **Manual smoke (not automated).** Restart OpenCode and confirm the SPT
  bootstrap is injected, the SPT skill set is visible, BGS skills are gone, and
  the MO2 and SPT MCP tools respond.
- **Async verification.** Run the rewritten verification entrypoint directly in
  the verification phase against the final state; do not rely on worker
  self-reports.

## Out of Scope

- Any new SPT feature, mod, or modpack work.
- Git history rewriting or force-pushing.
- Deleting the `upstream` remote.
- Renaming the GitHub repository.
- Reworking the content of SPT skill bodies (removal and renaming only).
- Building an actual modpack (the first production target is undecided).
- `CONTEXT.md` and ADR content beyond the layout decision; those are created
  lazily later.

## Further Notes

- The tracked-file count should drop from ~49,535 to roughly a few thousand.
  Record the before/after in the dev-log.
- Two risks to watch: the package manifest entry must be repointed after the
  materialized tree is deleted; and the MO2 shared tooling is labeled shared but
  is entangled with the BGS environment variable and a BGS test path, so SPT
  usage must be retested.
- The owner's local environment may already set the BGS-prefixed environment
  variables; those must be renamed to match.
- Keep the in-progress mods directory and the SPT mod templates.
- The vendored SPT reference archive's original copy lives outside the repo, so
  deleting the local copy is safe.
