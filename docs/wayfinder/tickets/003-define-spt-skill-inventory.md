# Ticket: Define SPT skill inventory and routing triggers

> Label: `wayfinder:grilling`
> Status: **closed** (2026-08-02)
> Blocks: #5
> Blocked by: #1, #4

## Resolution

14 SPT skills defined across 3 categories:

**Mapped from bgs (9):** setting-up, maintaining, evaluating, interpreting, curating, diagnosing, testing, devlog, changelog
**Transformed from bgs (3):** spt-mcp-automation (from xedit-automation), spt-conflict-audit (from xedit-conflict-audit), using-spt-translator (retained -- localization needs exist in-game, BepInEx menus, server locale.json)
**New, no bgs precedent (2):** writing-spt-mod (mod dev workflow), building-spt-modpack (build stage, may merge into curating after implementation if redundant)

**Dropped:** writing-bgs-load-order (no plugins.txt in SPT), using-bgs-archive (BA2/BSA is BGS format), using-bgs-papyrus (Papyrus is BGS language)

**Trigger table:** Full routing table with Chinese + English triggers confirmed. Key corrections from bgs literal translations: "declare 风格" -> "风格/方向/我要一个XX的包", "策展整合包" -> "规划整合包/整合包怎么搭".

**Dependencies surfaced:**
- spt MCP server implementation (needed by spt-mcp-automation skill)
- SPT MO2 game plugin implementation (needed by building-spt-modpack)

## Question

What SPT-specific skills does this plugin need, and what triggers each one?

The bgs-modding-superpowers skeleton has these skills as reference:

| BGS Skill | SPT Equivalent? |
|-----------|----------------|
| `setting-up-bgs-modding-environment` | SPT environment setup? |
| `maintaining-modding-environments` | SPT environment maintenance? |
| `evaluating-bgs-mods` | Evaluating SPT mods from local archive? |
| `interpreting-mod-author-instructions` | SPT mod install from local archive? |
| `curating-bgs-modpack` | SPT modpack curation strategy? |
| `diagnosing-bgs-problems` | SPT crash/issue diagnosis? |
| `testing-bgs-modpack` | SPT modpack verification? |
| `xedit-automation` | `spt` MCP automation hub? |
| `xedit-conflict-audit` | SPT conflict audit? |
| `writing-bgs-load-order` | SPT mod priority/load config? |
| `using-bgs-translator` | SPT mod translation? |
| `using-bgs-archive` | SPT archive handling? |
| `using-bgs-papyrus` | SPT Papyrus? (probably not) |
| `writing-modpack-devlog` | SPT devlog? |
| `writing-modpack-changelog` | SPT changelog? |

**Decisions needed:**
- Which bgs skills have direct SPT equivalents (rename + adapt)?
- Which bgs skills are BGS-only and should be dropped?
- Which new skills are SPT-specific with no bgs precedent?
- What are the trigger phrases for each SPT skill?
- How does the skill routing table look (like the one in `using-bgs-modding-superpowers`)?
