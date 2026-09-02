# Ticket: Define mod development agent workflow

> Label: `wayfinder:grilling`
> Status: **closed** (2026-08-02)
> Blocks: (none)
> Blocked by: #3

## Resolution

**Two independent pipelines** -- server mod dev and client mod dev are separate workflows sharing KB retrieval and scaffold generation, but with distinct toolchains, conflict surfaces, and verification approaches.

**Key decisions:**

1. **Scaffold**: Templates stored in this repo (`templates/server-mod/`, `templates/client-mod/`). Agent copies template and fills in mod-specific logic. Deterministic project structure, no per-request regeneration.

2. **Source fork role**: `SamMeow_SPT410_source_code` is **reference-only** (agent reads it to understand APIs). Compilation references come from the user's installed SPT binaries. SPT install path detection is `setting-up-spt-modding-environment`'s job.

3. **Feedback loop**: Two-tier verification:
   - **Baseline (B)**: compile + MO2 deploy + SPT launch + log-parse mod loading confirmation. Blocks all 4 startup-crash conflict categories from #1.
   - **Optional upgrade (C)**: automated functional verification per mod type where feasible (e.g., query trader list after adding a trader). Decision made per-mod, not globally.

**Workflow sketch (server mod):**
```
User: "I want a mod that does X"
  -> Agent parses intent, identifies SPT API surface needed
  -> Agent searches KB (api-notes-4.1, recipes) + reads 4.1 source for API details
  -> Agent copies templates/server-mod/, writes mod code
  -> dotnet build (references from installed SPT)
  -> Deploy via MO2, launch SPT, parse logs
  -> [Optional] automated functional check
  -> Hand to user for gameplay verification
```

## Question

What does the agent workflow look like for writing a new SPT 4.1 mod from scratch?

**Scenario:** User says "I want a mod that does X." Agent needs to:
1. Understand what X means in SPT terms
2. Find relevant API surface (from knowledge base + 4.1 source code)
3. Scaffold the mod project
4. Write the code
5. Test/verify

**Questions:**
- Where does the mod project scaffold live? (template in this repo? generated on the fly?)
- How does the agent navigate the 4.1 source code to find relevant APIs?
- What's the feedback loop? (How does the user verify the mod works?)
- Server mod vs client mod -- same workflow or different?
- How does the agent use `knowledge/spt-kb/curated/api-notes-4.1/` and `recipes/`?
- What's the role of the SPT 4.1 source fork (`SamMeow_SPT410_source_code`) -- reference only, or does the agent build against it?

**Available resources:**
- `knowledge/spt-kb/curated/modding-guide/` -- 4.1 mod dev guide
- `knowledge/spt-kb/curated/api-notes-4.1/` -- API notes from source reading
- `knowledge/spt-kb/curated/recipes/` -- task recipes
- `E-Mod开发示例/server-mod-examples/` -- official examples (4.x)
- `SamMeow_SPT410_source_code` -- full 4.1 server source
