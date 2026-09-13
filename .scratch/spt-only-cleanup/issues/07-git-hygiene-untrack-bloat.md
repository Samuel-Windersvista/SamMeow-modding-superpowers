# 07: Git hygiene — untrack bloat

**What to build:** The git index carries source only; regenerable artifacts and vendored archives stop bloating the repository, and `git status` stays clean across builds.

**Blocked by:** 01.

**Status:** done

- [ ] All tracked `obj/` and `bin/` build artifacts are untracked and ignored
- [ ] `external/spt-archive/` is deleted from disk and removed from tracking (its original lives outside the repo)
- [ ] `knowledge/spt-kb/archive/` is untracked but kept on disk; the SPT index, curated guides, wiki snapshot, and sources stay tracked
- [ ] Stray explorer artifacts (`.ala`) are removed
- [ ] `.gitignore` is restructured so build artifacts and vendored archives cannot re-enter
- [ ] The hygiene check passes and `git status` is clean
- [ ] Tracked file count has dropped from ~49,535 to roughly a few thousand, and the before/after is recorded
