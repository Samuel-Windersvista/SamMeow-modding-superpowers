# 01: Safety baseline and Matt skills config

**What to build:** A recoverable pre-cleanup state plus the Matt Pocock agent-skills configuration, so every later ticket is reversible and the engineering skills know where issues, labels, and domain docs live.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] `archive/bgs-final` tag exists on the pre-cleanup `main` HEAD
- [ ] `chore/spt-only-cleanup` branch exists and is checked out
- [ ] Pre-cleanup baseline recorded: tracked file count (~49,535), the failing bootstrap-verification output, and the top-level tracked-directory distribution
- [ ] `docs/agents/issue-tracker.md`, `docs/agents/triage-labels.md`, `docs/agents/domain.md` exist and are tracked
- [ ] `AGENTS.md` exists at the repo root with an `## Agent skills` block and is tracked (`.gitignore` no longer ignores it)
- [ ] First themed commit landed on the branch
