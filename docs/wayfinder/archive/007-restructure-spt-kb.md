# Ticket: Restructure spt-kb for agent consumption

> Label: `wayfinder:task`
> Status: **closed** (2026-08-02)
> Blocks: (none)
> Blocked by: (none)

## Resolution

Added YAML frontmatter metadata layer to all 71 markdown files (47 wiki + 24 curated) without restructuring directories.

**Metadata schema:** version ([4.1]/[4.0]/[3.11]/[通用]), domain (server/client/both), topic, source. Recipes carry extra `recipe_task` field.

**Generated `knowledge/spt-kb/index.json`** -- 71 entries, machine-readable, filterable by version/domain/topic. Agent retrieval path: read index.json -> filter -> open file. No prior directory knowledge needed.

**Version distribution:** [4.1]=29, [通用]=33, [4.0]=6, [3.11]=3
**Domain distribution:** server=19, both=48, client=4

**INDEX.md updated** with agent index pointer line.

## Question

Reorganize `knowledge/spt-kb/` from its current "by resource type" layout into a structure optimized for agent retrieval and consumption.

**Current layout:**
```
knowledge/spt-kb/
├── wiki/                    # 64 wiki files (vendor copy)
├── curated/
│   ├── modding-guide/       # 4 chapters
│   ├── api-notes-4.1/       # 6 API notes
│   └── recipes/             # 11 task recipes
├── archive/forge/           # 1822 mod metadata + zips + source
└── sources/                 # repo registry + third-party
```

**Problems:**
- Agent has to know WHERE to look before it can find WHAT it needs
- INDEX.md is human-oriented ("I want to do X") but not machine-queryable
- No structured metadata for agent consumption (what game version, what mod type, what topic)
- Forge archive metadata is raw JSON dumps, not indexed for search

**Goals:**
- Agent can find relevant knowledge by task/topic without knowing the directory layout
- Structured metadata enables filtering by version, mod type, domain
- INDEX.md becomes machine-parseable (or replaced by a structured index)
- bgs_kb MCP server can potentially index this for FTS5 search
