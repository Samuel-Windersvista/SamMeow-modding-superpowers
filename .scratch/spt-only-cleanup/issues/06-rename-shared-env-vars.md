# 06: Rename shared environment variables and fix the MO2 test path

**What to build:** Shared tooling uses domain-correct environment variable names, and the MO2 acceptance path points at the SPT install instead of a BGS game.

**Blocked by:** 01.

**Status:** done

- [ ] `BGS_MO2_ROOT` is renamed to `MO2_ROOT` across shared tools and scripts
- [ ] `BGS_SPT_KB_ROOT` is renamed to `SPT_KB_ROOT` across SPT tools and scripts
- [ ] The MO2 MCP acceptance script's hardcoded BGS game path is replaced with the SPT install path
- [ ] No `BGS_`-prefixed environment variable remains in shared tooling
- [ ] The MO2 control-plane test suite passes
- [ ] The owner has been given the exact local environment changes to apply (manual step)
