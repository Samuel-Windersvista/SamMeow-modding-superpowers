# 03: Rewire the harness to SPT-only

**What to build:** The OpenCode harness loads this repo as an SPT toolkit: a fresh session injects the SPT bootstrap, only the MO2 and SPT MCP servers are declared, and no BGS branding or bootstrap remains anywhere in the plugin chain.

**Blocked by:** 01.

**Status:** done

- [ ] Plugin and package renamed to `spt-modding-superpowers` (manifest, entrypoint filename, exported plugin function, bootstrap marker constant, README/release-notes references)
- [ ] The OpenCode plugin entrypoint injects the SPT bootstrap skill; the BGS bootstrap marker constant and injection path are gone
- [ ] The MCP declaration surface lists MO2 and SPT only; xEdit and BGS KB are absent
- [ ] Claude Code manifest, Codex manifest, agent marketplace manifest, hook dispatcher, and the static MCP wiring file are removed
- [ ] The package manifest entry point is repointed to the OpenCode plugin entrypoint (it currently points into the materialized tree)
- [ ] The committed materialized `plugins/` distribution tree is removed from tracking and ignored; the portable build script is rewired to the new plugin name and SPT tool set
- [ ] A fresh OpenCode session in this repo injects the SPT bootstrap and shows no BGS context
