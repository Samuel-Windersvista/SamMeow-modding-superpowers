---
name: writing-spt-modpack-changelog
description: "Use when cutting an SPT modpack release. Creates <project>/docs/release-changelog.md if absent; appends a new version section with grouped changes. Triggers - 'cut a release', 'release notes', 'changelog', '发版', 'v1.2.3 changes', 'what changed since last version'."
---

## Overview

The SPT modpack release changelog is the user-visible record of what changed between releases, grouped by semantic category: Added, Changed, Fixed, Removed, and Compatibility. This skill creates `<project>/docs/release-changelog.md` at runtime, then inserts a new version section whenever the curator cuts a release.

## When to Use

| Use this skill when... | Result |
| --- | --- |
| The user says "cut a release", "release notes", "changelog", "发版", or "what changed since last version" | Create or update the release changelog. |
| A target version is being prepared for a Discord post, a GitHub release, a Forge page, or another player-facing surface | Produce grouped, readable release notes. |
| The user wants changes since the last tag, version, or release section | Summarize those changes into semantic buckets. |
| The user has finished an SPT modpack milestone | Insert a new version section newest-first. |
| The user asks whether the release is major, minor, or patch | Suggest a version bump based on modpack impact. |

## When NOT to Use

| Do not use this skill when... | Use instead |
| --- | --- |
| The user made a random working change and wants to remember it later | `writing-spt-modpack-devlog` |
| The user wants a single-mod curator note | `writing-spt-modpack-devlog` |
| The user asks for mod conflict inspection or resolution | `spt-conflict-audit` first. |
| There is no release boundary, version, tag, or player-facing summary intent | Keep working; do not invent a release. |
| The user needs to set up the SPT modpack project structure first | `setting-up-spt-modding-environment` |

## Rules

<EXTREMELY-IMPORTANT>
This skill writes release notes for humans. It does not install mods, alter the SPT installation, change MO2 profiles, publish to Forge, or open a release page. Draft the changelog locally; any public posting or release publication requires explicit user confirmation outside this skill.
</EXTREMELY-IMPORTANT>

1. Detect the modpack project root before writing.
   - If the user provides `<modpack_project_root>`, use it.
   - If the current working directory looks like a modpack directory because it already has `Data/`, `profiles/`, or `docs/`, default to `pwd` and tell the user what default you used.
   - Otherwise ask once: "Which modpack project root should I use? I can default to the current directory if this is the project root."
2. Locate `<project>/docs/release-changelog.md`.
   - If `<project>/docs/` does not exist, create it.
   - If `release-changelog.md` is missing, create it before inserting the version.
3. On first creation, use this header shape:
   - `# <Project Name> Release Changelog`
   - One-line intro: `Player-facing release notes for <Project Name>. Newest releases are listed first.`
   - `## Releases`
4. Determine the target version.
   - If the user provides a version, use it exactly after checking that it is not already present.
   - If the user does not provide a version, read the newest release heading and ask for the new version once.
   - Suggest a semver bump: major for SPT-version or profile-breaking changes, minor for added mods or compatibility tweaks, patch for fixes only.
5. Use the current local date for the release date unless the user provides a specific release date.
   - Version headings use `## <version> (<release-date>)`.
   - Use `YYYY-MM-DD` for release dates.
6. Each version section must use semantic buckets:
   - `### Added`
   - `### Changed`
   - `### Fixed`
   - `### Removed`
   - `### Compatibility`
7. Omit empty buckets.
   - Do not leave placeholder headings such as `### Removed` with "None" under them unless the user explicitly wants a full template.
8. Insert versions newest-first under `## Releases`.
   - Insert the new version immediately below `## Releases`.
   - If the file already uses oldest-first and the user wants to preserve it, follow the existing convention and note that choice.
9. Optionally consume the dev-log.
   - If `<project>/docs/dev-log.md` exists, offer to summarize entries since the last release into the appropriate buckets.
   - The user must confirm or edit the proposed summary before you write it into the changelog.
   - Do not blindly convert every dev-log entry into player-facing copy; curator noise stays in the dev-log.
10. Write for players, not for internal tooling.
    - Keep entries concrete and readable.
    - Avoid private debugging details, raw conflict jargon (TypePriority, DI collisions, IL patches), and internal agent process notes.
    - Mention mod names and versions when they help users understand compatibility or upgrade risk.
11. Be explicit about SPT version compatibility.
    - State the target SPT version (4.1 locked) in the release, and flag any mods that are pinned to older compatibility bands (e.g. "pinned to a 4.0-compatible release") in the Compatibility bucket.
12. Do not duplicate an existing version.
    - If the target version heading already exists, ask whether to amend that section or choose a new version.
    - Do not insert a second `## <version>` heading.
13. After writing, report the file path, version, release date, buckets included, and whether dev-log entries were consumed.

## Quick Reference

| Field | Canonical shape | Example |
| --- | --- | --- |
| File | `<project>/docs/release-changelog.md` | `docs/release-changelog.md` |
| Version heading | `## <version> (<release-date>)` | `## v1.4.0 (2026-08-04)` |
| Bucket heading | `### Added`, `### Changed`, `### Fixed`, `### Removed`, `### Compatibility` | `### Compatibility` |
| Entry style | Short player-facing bullet | `- Added a patch for Example Loot and Example Bot AI.` |
| Ordering | Newest release first under `## Releases` | `v1.4.0` above `v1.3.2` |

Canonical version shape:

```markdown
## v1.4.0 (2026-08-04)

### Added

- Added Example Loot Overhaul and its modpack config.

### Changed

- Rebalanced the early-game trader stock for a slower economy start.

### Fixed

- Fixed the raid-start crash caused by Example Client Mod's UI patch.

### Compatibility

- Targets SPT 4.1. Example Loot Overhaul is pinned to its 4.0-compatible
  release until a 4.1 build ships.
```

## Examples

### Bad

```markdown
Changed a bunch of stuff:
- new loot mod
- fixed bugs
- removed old patch
```

This has no version, no release date, no grouping, and no signal about compatibility risk.

### Good

```markdown
## v1.4.0 (2026-08-04)

### Added

- Added Example Loot Overhaul with a curated modpack config.

### Changed

- Updated trader stock balance in the early-game shops.

### Fixed

- Fixed the raid-start crash caused by Example Client Mod's UI patch.

### Removed

- Removed the obsolete Example Bot Hotfix; its changes are now included upstream.

### Compatibility

- Targets SPT 4.1. Example Loot Overhaul is pinned to its 4.0-compatible
  release; Example Client Mod requires a fresh profile for its UI settings.
```

This section gives players the version, date, grouped changes, and upgrade risk in one place.

## Common Mistakes

- Using the changelog for rough working notes that belong in the dev-log.
- Forgetting the version or release date.
- Dumping ungrouped bullets under one heading.
- Leaving empty bucket headings in the final changelog.
- Copying raw dev-log text without translating it for players.
- Hiding compatibility or save/profile-risk information because it sounds negative.
- Creating duplicate version headings.
- Asking repeated version questions instead of suggesting a semver bump.
- Publishing release notes directly instead of drafting them for user review.
- Overstating changes with marketing language instead of concrete release facts.

## Rationalizations

| Excuse | Reality |
| --- | --- |
| "The dev-log already says what changed." | The dev-log is curator history. The changelog is player-facing release communication. They need different detail and tone. |
| "A single bullet list is faster." | Grouped buckets let users scan for added content, fixes, removals, and compatibility risk. |
| "Empty headings show that we considered every category." | Empty headings are noise in release notes. Omit empty buckets unless the user asked for a full template. |
| "I do not know the version, so I will invent one." | Ask once and suggest a bump. Version numbers are release authority, not filler text. |
| "The conflict details are important, so I will paste them all." | Players need the effect and upgrade risk. Keep raw conflict evidence in the dev-log or artifacts. |
| "This is just a patch release, so no changelog is needed." | Patch releases still need a record of what was fixed and whether users should update. |
| "Compatibility notes make the release look risky." | Hidden risk is worse. Clear compatibility notes reduce support burden. |
| "The user said release notes, so I can post them." | This skill drafts local changelog text. Public posting is a separate confirmed action. |

## See also

- `writing-spt-modpack-devlog` - use for curator-facing chronological work records.
- `setting-up-spt-modding-environment` - use when the SPT modpack project root or docs structure must be established first.
