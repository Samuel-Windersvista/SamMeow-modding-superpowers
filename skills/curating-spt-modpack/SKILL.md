---
name: curating-spt-modpack
description: "Use when planning or building the whole SPT modpack incrementally -- batch strategy, rollback point, naming convention, style declaration, mod selection. Triggers: 'plan the pack', 'batch strategy', 'rollback point', 'naming convention', '规划整合包', '整合包怎么搭', '风格', '方向', '批次策略', '回滚点'."
---

# Curating SPT Modpack

Whole-pack incremental build strategy for SPT 4.1 modpacks. This is the cross-stage skill that per-mod skills feed into.

## Modpack pipeline overview

The 6-stage pipeline (from wayfinder ticket #4):

```
[1] 意图理解        用户输入(一句话/mod清单) -> AI 解析为功能需求列表
       |
[2] mod 匹配        功能需求 -> 检索归档 mod（有就直接用）
       |                          |
       |                    [2b] mod 开发（没有就现写，走 writing-spt-mod）
       |                          |
[3] 冲突分析        选定的 mod 集合 -> 元数据级冲突检测报告（走 spt-conflict-audit）
       |
[4] 人工审查        冲突报告 -> 用户决定取舍
       |
[5] 构建           用户确认后的清单 -> MO2 profile（走 building-spt-modpack）
       |
[6] 验证           MO2 启动 SPT -> mod 加载确认（走 testing-spt-modpack）
```

This skill covers stages [1]-[4]. Stage [5] routes to `building-spt-modpack`. Stage [6] routes to `testing-spt-modpack`.

## Stage 1: Intent understanding

User input can be:
- **Explicit mod list**: "I want these 30 mods" -> skip to stage 2 with the list
- **One-sentence direction**: "I want a hardcore survival pack" -> decompose into feature requirements, then match to mods in stage 2
- **Mixed**: some known mods + some open slots -> fill known slots, discover for open slots

Decompose direction into feature requirements. Example:
- "硬核生存" -> harder AI, realistic ballistics, limited trader stock, health system overhaul, economy rebalance
- Each requirement maps to one or more candidate mods from the Forge archive

## Stage 2: Mod matching

Search the local Forge archive (`knowledge/spt-kb/archive/forge/`) for candidate mods:

1. Read `knowledge/spt-kb/archive/forge/hot-index.json` for the 95 hot mods
2. Read `knowledge/spt-kb/archive/forge/api/mods-catalog.json` for the full 1822 mod catalog
3. Filter by: SPT version compatibility (4.1), mod type (server/client), category
4. For each candidate, route to `evaluating-spt-mods` for quality/fit assessment

If no suitable mod exists for a requirement, route to `writing-spt-mod` for custom development (stage 2b).

## Stage 3: Conflict analysis

Once a mod set is selected, run conflict analysis. Route to `spt-conflict-audit`.

The conflict taxonomy (from `docs/wayfinder/findings/001-spt-conflict-taxonomy.md`) defines 20 conflict types. At the curation stage, focus on **metadata-detectable** conflicts:

- **Server mods**: table injection key collisions, route conflicts, config key overlaps, DI service registration conflicts
- **Client mods**: BepInEx plugin dependency conflicts, file overwrite collisions
- **Cross-layer**: server-client pairing mismatches, version coupling

IL-level conflicts (Harmony patch targets) require the future IL decompilation pipeline -- flag as "unknown" for now.

## Stage 4: Human review

Present the conflict report to the user:

1. **Breaking conflicts** (crash at startup): must resolve -- choose one mod or find alternative
2. **Override conflicts** (one mod wins): present the trade-off, let user decide priority
3. **Silent corruption risks**: flag clearly, recommend testing
4. **Compatible**: no action needed

User decides which mods to keep, drop, or replace. AI records the decisions.

## Batch strategy

For large modpacks (50+ mods), work in batches:

| Batch | Content | Risk |
|-------|---------|------|
| 1 | Foundation mods (libraries, frameworks, dependencies) | Low -- these rarely conflict |
| 2 | Core overhaul mods (AI, health, ballistics) | Medium -- check conflicts carefully |
| 3 | Content mods (traders, items, quests) | Medium -- table injection collisions possible |
| 4 | Visual/UI mods | Low-Medium -- client-side, fewer conflicts |
| 5 | Fine-tuning mods (loot tweaks, economy) | High -- these often override the same data |

Each batch gets its own conflict analysis + human review cycle. Do not dump all mods into one analysis pass.

## Rollback points

Define rollback boundaries between batches. If batch N fails verification:
- Roll back to end of batch N-1
- Investigate the failure
- Re-attempt with a smaller or different batch N

## Style declaration

Before starting, declare the pack's style direction. This guides mod selection trade-offs:
- What experience should the player have?
- What SPT features are emphasized? (survival, combat, economy, exploration)
- What is explicitly NOT wanted?

Record the style declaration in the modpack project's dev-log.

## Naming convention

Modpack naming: `<Name>-<Version>` (e.g., `Norvinsk-0.1.0`).
MO2 profile naming: same as modpack name.
Mod overlay naming in MO2: `<category>-<mod-name>-<version>` for clarity.

## See also

- `evaluating-spt-mods` -- per-mod quality/fit evaluation (feeds stage 2)
- `interpreting-spt-mod-instructions` -- per-mod install instructions (feeds stage 2)
- `spt-conflict-audit` -- conflict analysis engine (feeds stage 3)
- `writing-spt-mod` -- custom mod development (feeds stage 2b)
- `building-spt-modpack` -- build execution (stage 5)
- `testing-spt-modpack` -- post-build verification (stage 6)
- `docs/wayfinder/findings/001-spt-conflict-taxonomy.md` -- 20-type conflict taxonomy
