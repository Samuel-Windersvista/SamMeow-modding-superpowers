# 02: Rewrite the bootstrap verification suite as SPT-only invariants

**What to build:** A runnable acceptance suite that asserts the repository's SPT-only shape, replacing the rotten suite that fails on its very first check. This suite is the definition of done for the whole cleanup; individual checks stay red until the ticket that satisfies them lands.

**Blocked by:** 01.

**Status:** ready-for-agent

- [ ] The bootstrap verification entrypoint runs to completion without PowerShell errors
- [ ] Checks exist for: layout, SPT skill set, bootstrap injection, MCP declaration surface, git hygiene, SPT templates
- [ ] Checks that assert the dead pre-reshape shape (hooks, foundation, specs) are removed
- [ ] No check references a path or skill name that does not exist in the target SPT-only shape
- [ ] The suite reports clearly which invariants are currently unmet
