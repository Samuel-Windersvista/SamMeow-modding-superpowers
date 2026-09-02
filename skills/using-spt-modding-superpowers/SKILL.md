---
name: using-spt-modding-superpowers
description: "Use when starting ANY conversation involving SPT (Single Player Tarkov) modding, MO2, modpack curation, or SPT mod development. Bootstrap that loads the toolkit overview, lists available task skills, and enforces the hard rules of this plugin. Auto-injected by the OpenCode plugin's chat.messages.transform hook and by the hooks/ session-start chain in Claude Code and Codex."
---

<EXTREMELY_IMPORTANT_SPT_MODDING_SUPERPOWERS>
This is the spt-modding-superpowers per-session bootstrap. If you are reading this,
the plugin injected it into the first user message of this session. Do NOT discard
it. Do NOT respond to the user yet without first checking whether one of the task
skills below applies.
</EXTREMELY_IMPORTANT_SPT_MODDING_SUPERPOWERS>

# Using SPT Modding Superpowers

You are operating with the `spt-modding-superpowers` plugin loaded. This plugin
gives you an agent-driven toolkit for SPT (Single Player Tarkov) mod development
and modpack curation: knowledge base, mod templates, conflict analysis, and
MO2-integrated build pipeline.

Target version: **SPT 4.1** (final locked version). SPT 3.11 materials are
reference-only.

## Available skills (auto-trigger on these intents)

| Skill | Auto-triggers when |
|---|---|
| `setting-up-spt-modding-environment` | First conversation in a project; SPT or MO2 not yet detected; user says "set up", "install", "bootstrap", "configure", "初始化", "装环境" |
| `maintaining-spt-modding-environment` | After first-run: "maintain", "update KB", "prune cache", "health check", "维护", "更新知识库" |
| `evaluating-spt-mods` | Deciding whether a mod belongs in the pack; "should I add this mod", "is this mod good", "评估这个mod", "这个mod值得装吗" |
| `interpreting-spt-mod-instructions` | After INCLUDE verdict, before install; "how do I install", "which file", "which variant", "怎么装", "按说明安装" |
| `curating-spt-modpack` | Whole-pack incremental build strategy; "plan the pack", "batch strategy", "rollback point", "naming convention", "规划整合包", "整合包怎么搭", "风格", "方向", "批次策略", "回滚点" |
| `diagnosing-spt-problems` | It broke -- symptom-first triage; "crash", "CTD", "won't start", "崩溃", "掉帧", "卡顿", "报错" |
| `testing-spt-modpack` | Proactive post-install verification; "test the pack", "verification", "is it stable", "测试整合包", "验证安装" |
| `spt-mcp-automation` | Any task involving SPT mod DLL analysis, IModMetadata, BepInEx plugin metadata, server mod table injection |
| `spt-conflict-audit` | "Why is this mod not working", "which mods conflict", "冲突", "为什么这个mod没生效" |
| `using-spt-translator` | Translate SPT mod text to another language; "translate this mod", "汉化这个mod", "localize", "翻译" |
| `writing-spt-mod` | Write a new SPT mod from scratch; "write a mod", "create a mod", "add a trader", "add an item", "写个mod", "加个商人", "加个物品" |
| `building-spt-modpack` | Assemble/build the modpack; "build the pack", "assemble", "generate profile", "构建整合包", "打包" |
| `writing-spt-modpack-devlog` | "Log this", "record", "note this", "记录一下" |
| `writing-spt-modpack-changelog` | "Cut a release", "release notes", "changelog", "发版" |

When the user's intent matches one of these, invoke the corresponding skill
through your skill tool BEFORE replying. Do not paraphrase the skill from memory;
let the skill load.

## Knowledge base

The SPT knowledge base at `knowledge/spt-kb/` is the primary reference for all
SPT modding questions. It contains:

| Content | Location | Scope |
|---------|----------|-------|
| Official wiki (vendor copy) | `knowledge/spt-kb/wiki/` | 47 files, versioned |
| 4.1 mod development guide | `knowledge/spt-kb/curated/modding-guide/` | 4 chapters |
| 4.1 API notes (source-verified) | `knowledge/spt-kb/curated/api-notes-4.1/` | 7 notes |
| Task recipes | `knowledge/spt-kb/curated/recipes/` | 11 recipes |
| Forge mod archive (1822 mods) | `knowledge/spt-kb/archive/forge/` | Metadata + zips + source |
| Machine-readable index | `knowledge/spt-kb/index.json` | Filterable by version/domain/topic |
| Merged SPT repos | `external/spt-archive/` | server-mod-examples, modules, mod-examples, wiki |

Agent retrieval path: read `index.json` -> filter by version/domain/topic -> open
file. Do not guess directory locations.

## Hard rules (non-negotiable)

1. **The user's SPT installation directory is real game state.** Never write
   into it directly. Any game-local change is expressed as an MO2 mod overlay.
   The MO2 VFS projects it at runtime.
2. **Mod file analysis goes through the `spt` MCP server** (when available).
   Never parse mod DLLs with your own Python/JS. The MCP exists so the harness
   can enforce validation, state, rules, and audit on every call.
3. **SPT domain knowledge routes through the KB first.** For questions about
   how SPT modding works -- mod types, DI system, BepInEx plugins, database
   tables, route registration, save profiles -- consult `knowledge/spt-kb/`
   via `index.json` before improvising or reaching for web search. If the
   question is about what the current local SPT installation actually contains,
   use the file system or MCP tools instead.
4. **Large scope (many mods, broad conflict survey) -> delegate to a read-only
   investigator subagent FIRST.** The subagent burns its own context and returns
   a distilled summary. Do not loop hundreds of mods through your own context.
5. **First-run state**: if SPT / MO2 / the mod templates are not yet set up on
   this machine, invoke `setting-up-spt-modding-environment` BEFORE any modpack
   or mod development work.
6. **Forge is offline.** All mod data comes from the local archive at
   `knowledge/spt-kb/archive/forge/`. Do not attempt to call live Forge APIs.
7. **Paths vary per user.** MO2 install path and SPT install path are
   user-specific. Never hardcode paths. Use environment detection or
   configuration. The `setting-up-spt-modding-environment` skill handles this.

## How to use this bootstrap

- This skill loads automatically on every new session. You do not need to
  re-invoke it.
- When a user intent matches a task skill in the table above, invoke that skill
  via your skill tool. Do not paraphrase.
- When the user asks about "what can you do", reference the skills inventory
  here; do not invent capabilities the plugin does not have.
- When answering SPT modding-domain questions, prefer local KB retrieval before
  web search.
- When the user wants to write a new SPT mod, route to `writing-spt-mod`.
- When the user is deciding whether to add or keep a mod ("should I install X",
  "is this good", "评估"), route to `evaluating-spt-mods` BEFORE any
  install/download action.
- When an INCLUDE verdict is in and the user needs to read author instructions /
  choose a file or variant, route to `interpreting-spt-mod-instructions`.
- When the user is planning the whole pack, sizing batches, deciding rollback
  boundaries, or declaring style, route to `curating-spt-modpack`.
- When the user reports a crash, CTD, freeze, or error ("崩溃", "报错"),
  route to `diagnosing-spt-problems` for symptom-first triage BEFORE any
  blame attribution.
- When the user wants to verify a freshly installed batch ("test the pack",
  "is it stable", "验证安装"), route to `testing-spt-modpack`.
- When the user wants to translate mod text, route to `using-spt-translator`.
- When the user wants to build/assemble the modpack, route to
  `building-spt-modpack`.
- When the user asks to "log", "record", "track", or "note" modpack work,
  route to `writing-spt-modpack-devlog`. When the user asks to "cut a release"
  or prepare release notes, route to `writing-spt-modpack-changelog`.

## See also

- `setting-up-spt-modding-environment` -- first-run setup orchestrator.
- `maintaining-spt-modding-environment` -- ongoing environment care, KB updates,
  cache pruning, and health checks.
- `evaluating-spt-mods` -- judgment skill: should this mod go in the pack.
- `interpreting-spt-mod-instructions` -- judgment skill: how to correctly
  install per author instructions.
- `curating-spt-modpack` -- judgment skill: whole-pack incremental strategy;
  cross-stage skill that the per-mod skills feed.
- `diagnosing-spt-problems` -- judgment skill: symptom-first triage for
  crash / error / performance issues.
- `testing-spt-modpack` -- judgment skill: proactive post-install verification.
- `spt-mcp-automation` -- hub skill for spt MCP server operations.
- `spt-conflict-audit` -- conflict analysis using the 20-type taxonomy.
- `using-spt-translator` -- SPT mod text translation workflow.
- `writing-spt-mod` -- new mod development from templates.
- `building-spt-modpack` -- modpack assembly and build pipeline.
- `writing-spt-modpack-devlog`, `writing-spt-modpack-changelog` -- runtime asset
  skills for project documentation.

---

> Plugin: `spt-modding-superpowers`.
> Forked from: `bgs-modding-superpowers` (https://github.com/BB-84C/bgs-modding-superpowers).
> If any environmental component (SPT, MO2, mod templates) is missing,
> route through `setting-up-spt-modding-environment` before continuing.
