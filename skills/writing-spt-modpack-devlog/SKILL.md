---
name: writing-spt-modpack-devlog
description: "Use when starting or appending to an SPT modpack project dev-log. Creates <project>/docs/dev-log.md if absent; appends a dated entry on subsequent calls. Triggers - 'log this', 'add to dev log', 'record what I did', 'note this change', '记录一下', 'devlog'."
---

## Overview

The modpack dev-log is the durable record of what the curator did to the SPT modpack, in chronological order, with enough context for a future maintainer to understand why a change happened. This skill creates `<project>/docs/dev-log.md` at runtime, then appends entries as the modpack evolves.

## When to Use

| Use this skill when... | Result |
| --- | --- |
| The user says "log this", "add to dev log", "record what I did", "note this change", or "记录一下" | Append a dated entry to the project dev-log. |
| A mod was added, removed, replaced, upgraded, downgraded, patched, or moved in the MO2 profile | Record what changed and why. |
| A conflict-audit session finished | Summarize the finding and link evidence from `spt-conflict-audit` output. |
| A server failure, mod-not-loading, BepInEx console error, or raid-start crash was investigated | Preserve the investigation result, even if the fix is not final. |
| An SPT version or mod version decision was made (4.1 pin, 4.0-version pin, rejection of a 3.11 mod) | Record the decision, context, and evidence. |
| A modpack release was cut | Add the curator-facing note and cross-link to `writing-spt-modpack-changelog`. |
| The user made a manual decision that future agents must not re-litigate | Record the decision, context, and evidence. |

## When NOT to Use

| Do not use this skill when... | Use instead |
| --- | --- |
| The user is preparing public release notes for players | `writing-spt-modpack-changelog` |
| The user asks for a one-off explanation without wanting a project record | Answer directly. |
| The project root is unknown and the user refuses to identify it | Stop after explaining what is missing. |
| The entry would expose private notes, credentials, or personal information | Ask for a sanitized version first. |
| The request is to inspect or resolve mod conflicts | `spt-conflict-audit` first, then log the result. |

## Rules

<EXTREMELY-IMPORTANT>
This skill creates or appends documentation inside the user's SPT modpack project. It does not install mods, change the SPT installation, modify MO2 profiles, or write into game installation folders. If the user asks for those actions, route to the appropriate modding skill before writing the dev-log entry.
</EXTREMELY-IMPORTANT>

1. Detect the modpack project root before writing.
   - If the user provides `<modpack_project_root>`, use it.
   - If the current working directory looks like a modpack directory because it already has `Data/`, `profiles/`, or `docs/`, default to `pwd` and tell the user what default you used.
   - Otherwise ask once: "Which modpack project root should I use? I can default to the current directory if this is the project root."
2. Locate `<project>/docs/dev-log.md`.
   - If `<project>/docs/` does not exist, create it.
   - If `dev-log.md` is missing, create it before appending.
3. On first creation, use this header shape:
   - `# <Project Name> Dev Log`
   - `Started: <local ISO-8601 timestamp with offset>`
   - A short note that entries are newest-first.
   - `## Entries`
4. Use local time with UTC offset for every entry timestamp, and keep that choice consistent throughout the file.
   - Example: `2026-08-04T14:30:00+08:00`.
   - Do not mix local dates, UTC dates, and vague dates such as "today".
5. Each entry must include these parts:
   - ISO-8601 timestamp with offset.
   - Short title.
   - Body paragraph or paragraphs explaining what changed and why.
   - Optional `Mods touched` subsection when specific mods, versions, or tools were involved.
   - Optional `Refs` subsection linking evidence, logs, issue threads, or conflict-audit captures.
6. Append entries newest-first under the `## Entries` section.
   - Insert the new entry immediately below `## Entries`.
   - Do not append new entries at the bottom unless the file already uses oldest-first and the user explicitly wants to keep it that way.
7. Do not duplicate the most recent entry.
   - Read the newest entry timestamp and title.
   - If the new title and body would be identical or effectively identical within 10 minutes of the newest entry, surface: "This looks like a duplicate of `<existing title>` from `<timestamp>`. Append anyway? (y/N)"
   - Default to no if the user does not confirm.
8. Preserve evidence when the user references it.
   - If the user points at a captured server log excerpt, BepInEx console output, conflict report, screenshot, text log, or other local evidence file, copy it into `<project>/docs/dev-log-artifacts/<entry-slug>/`.
   - Link the copied artifact from the entry body or `Refs` subsection.
   - Prefer copying over linking to a fragile temporary path.
9. Make the entry useful to a future curator.
   - Include the decision, cause, tradeoff, or unresolved question.
   - Avoid entries that only say "fixed stuff" or "updated mods".
10. Keep private working noise out of the dev-log.
    - Do not paste entire terminal transcripts unless they are the evidence.
    - Summarize the result, then link artifacts.
11. If the user gave rough notes, preserve meaning rather than polishing away operational details.
    - Keep mod names, version pins (e.g. `best_spt` decisions), config-file changes, symptom, and reproduction facts.
    - Clean up grammar only enough to make the entry readable.
12. After writing, report the file path, entry title, timestamp, and any artifacts copied.

## Quick Reference

| Field | Canonical shape | Example |
| --- | --- | --- |
| File | `<project>/docs/dev-log.md` | `docs/dev-log.md` |
| Entry heading | `### <timestamp> - <short title>` | `### 2026-08-04T14:30:00+08:00 - Pinned loot overhaul to 4.0-compatible version` |
| Body | One or more paragraphs explaining what changed and why | `Pinned the loot overhaul to its 4.0-compatible release because the newest version targets a different SPT band and failed server validation.` |
| Mods touched | Optional bullet list | `- Example Loot Overhaul (pinned to v1.4.0)` |
| Refs | Optional bullet list of copied artifacts or external references | `- dev-log-artifacts/loot-pin/server-log-validation-error.txt` |

Canonical entry shape:

```markdown
### 2026-08-04T14:30:00+08:00 - Pinned loot overhaul to 4.0-compatible version

Pinned the loot overhaul to its 4.0-compatible release because the newest
version targets a different SPT band and failed server validation at startup.
The pin keeps the intended loot behavior without blocking the server boot.

#### Mods touched

- Example Loot Overhaul (pinned to v1.4.0)

#### Refs

- dev-log-artifacts/loot-pin/server-log-validation-error.txt
```

## Examples

### Bad

```markdown
Fixed the loot thing.
```

This entry has no date, no mod names, no version context, no evidence, and no explanation of what was fixed.

### Good

```markdown
### 2026-08-04T14:30:00+08:00 - Pinned loot overhaul to 4.0-compatible version

Kept the loot overhaul's table edits but pinned it to v1.4.0 (its best_spt is
4.0.*) after the newest version failed modValidator at server start. This
preserves the intended loot behavior without blocking the server boot on the
4.1 target.

#### Mods touched

- Example Loot Overhaul (v1.4.0, best_spt 4.0.*)

#### Refs

- dev-log-artifacts/loot-pin/server-log-validation-error.txt
```

This entry tells a future curator what changed, why it changed, which mod and version were involved, and where the evidence lives.

## Common Mistakes

- Writing a loose note without a timestamp.
- Logging only the action and omitting the reason.
- Using the dev-log as public release notes instead of curator history.
- Duplicating the same entry because the user repeated "log this" after a tool run.
- Linking to temporary evidence paths that will disappear.
- Copying huge raw logs into the main entry instead of summarizing and linking artifacts.
- Recording "updated mods" without naming the mods and versions.
- Turning rough curator notes into generic prose that loses version-pin or config context.
- Asking multiple root-location questions instead of one question with a default.
- Creating the file somewhere outside the actual modpack project root.

## Rationalizations

| Excuse | Reality |
| --- | --- |
| "This is just a tiny note; it does not need structure." | Tiny notes become the only record future agents can trust. Date, title, context, and refs are the minimum useful shape. |
| "The release changelog will cover it." | The changelog is for players. The dev-log is for curators and future agents. They answer different questions. |
| "The evidence is in my terminal scrollback." | Scrollback is not durable. Copy evidence into `docs/dev-log-artifacts/<entry-slug>/` and link it. |
| "I can append at the bottom; it is easier." | Newest-first keeps the current state visible. Insert below `## Entries` unless the user explicitly chose oldest-first. |
| "The user knows what they meant by 'the loot thing'." | Future maintainers will not. Name the mod, version pin, config file, symptom, or decision. |
| "I should ask several questions to make sure the entry is perfect." | Ask once for the project root if needed. Otherwise write the best entry from available context and flag uncertainties inline. |
| "The artifact path is already linked in the conversation." | Conversation links are not project records. Copy or preserve the artifact in the project docs tree. |
| "I should make this sound polished." | Accuracy beats polish. Keep the operational facts and make the wording readable. |

## See also

- `writing-spt-modpack-changelog` - use for player-facing release notes and version sections.
- `setting-up-spt-modding-environment` - use when the SPT modpack project structure itself is being created or located.
