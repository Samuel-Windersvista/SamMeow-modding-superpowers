# 05: Remove the BGS knowledge base, tools, and scripts

**What to build:** The knowledge, tools, and scripts trees become SPT-only.

**Blocked by:** 03.

**Status:** ready-for-agent

- [ ] `knowledge/bgs-kb/` is removed
- [ ] BGS-only tools are removed: xEdit MCP, xEdit hook bridge, BGS KB MCP, BGS archive, BGS Papyrus, BGS translator
- [ ] BGS-only scripts are removed (xEdit fetch, hook-bridge install, script-extender update, Creation Club split, KB release/author/rebuild, Nexus BGS update-state)
- [ ] `.version-bump.json` and the portable build script no longer reference BGS-only paths
- [ ] SPT tools (SPT MCP, DB dump, class map, locale test, migration pilots, MO2 family) remain intact
- [ ] The tools and layout checks pass
