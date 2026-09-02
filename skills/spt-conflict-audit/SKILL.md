---
name: spt-conflict-audit
description: "Use when auditing conflicts between SPT mods -- determining which mod wins, what conflicts exist, whether the configuration is safe. Triggers: 'why is this mod not working', 'which mods conflict', '冲突', '为什么这个mod没生效', 'is this combination safe', 'mod compatibility'."
---

# SPT Conflict Audit

Audit conflicts between SPT 4.1 mods using the 20-type conflict taxonomy. Full taxonomy reference: `docs/wayfinder/findings/001-spt-conflict-taxonomy.md`.

## Conflict severity classification

| Severity | Meaning | Action |
|----------|---------|--------|
| **B (Breaking)** | Crash at startup or core functionality failure | Must resolve before build |
| **S (Silent corruption)** | Data is wrong but no crash | Flag clearly, recommend testing |
| **O (Override)** | One mod wins, other loses functionality | Present trade-off to user |
| **C (Compatible)** | Mods can coexist | No action needed |

## Detection method classification

| Method | Meaning | Current capability |
|--------|---------|-------------------|
| **M (Metadata)** | Detectable from mod manifests/configs | Available now |
| **I (IL)** | Requires IL decompilation | Future capability |
| **R (Runtime)** | Only detectable at runtime | Cannot pre-detect |

## Audit workflow

### Step 1: Collect mod inventory

Gather all mods in the current selection:
- Server mods: read each mod's directory for IModMetadata properties, config files, route registrations
- Client mods: read each mod's BepInEx plugin metadata (BepInPlugin attribute, BepInDependency declarations)
- Cross-reference with Forge archive metadata for known dependencies/incompatibilities

**已验证的正确输入模型（2026-08-05 修正）：**

1. **冲突分析的正确输入 = modlist.txt 的启用 mod**，不是目录全部
   - MO2 `profiles/<profilename>/modlist.txt`：`+modname` 启用（参与运行）、`-modname` 禁用（不参与运行）
   - `_backup` 后缀和禁用状态的 mod **不参与游戏运行**，必须排除在冲突分析外
   - 错误案例：把 `friendlyPMC - 已AI优化`（禁用）和 `friendlyPMC 与 PITFireTeam 合并版本`（启用）同时纳入分析，误报双版本共存冲突

2. **MO2 mod 目录的嵌套结构必须完整映射**（不能截断前缀）
   - 3.11 混合 mod：`mods/<ModName>/user/mods/<actual-name>/` —— 扫描时必须用 **mod 根目录**（`mods/<ModName>/`），不能用嵌套子目录
   - 若扫描 `user/mods/<name>/` 子目录，`relative()` 会丢失 `user/mods/<name>/` 前缀，导致不同 mod 的同名文件（如 `db/base.json`）被**错误判为同路径冲突**
   - 真实位置：`user/mods/Hephaestus/db/base.json` vs `user/mods/acidphantasm-scorpion/db/base.json` —— **路径不同，不冲突**

3. **同路径文件必须比内容 hash（SHA-1）**，否则同模板误报
   - 同路径 + hash 全相同 → `duplicate_file`（无害重复，内容一致，谁覆盖谁都一样）
   - 同路径 + hash 不同 → `file_overwrite`（真冲突，未胜出内容被遮蔽）
   - 结构性噪音（`package.json`/`meta.ini`/`src/mod.ts`/开发脚手架）进忽略名单

### Step 2: Metadata-level conflict detection

**Server mod conflicts:**

| Check | Method | Severity |
|-------|--------|----------|
| Table injection same-key collision | Compare which tables/keys each mod modifies | O (last-writer-wins by TypePriority) |
| Route registration collision | Check if two mods register the same route path | B (first-registered-wins, second silently ignored) |
| Config key collision | Compare config file schemas for overlapping keys | O or S |
| DI service registration conflict | Check for competing service implementations | B (constructor failure or wrong service injected) |
| New ID collision | Check if two mods use the same MongoId for new items/traders/quests | S (silent overwrite) |

**Server mod load order prediction:**
- Mods load in order: `TypePriority` ascending, then `ModGuid` alphabetical as tiebreaker
- Later mods override earlier mods for table injections
- First-registered routes win
- Report the predicted load order and flag where override direction matters

**Client mod conflicts:**

| Check | Method | Severity |
|-------|--------|----------|
| BepInEx dependency conflict | Parse BepInDependency attributes for circular or missing deps | B (plugin load failure) |
| File overwrite collision | Compare file lists for same-path files in BepInEx/plugins/ | O (MO2 priority decides) |
| Config .cfg key collision | Compare BepInEx config schemas | O or S |
| Harmony patch target collision | (Requires IL analysis -- flag as unknown) | Unknown |

**Cross-layer conflicts:**

| Check | Method | Severity |
|-------|--------|----------|
| Server-client pairing mismatch | Check if server mod expects a client component that isn't present | S or B |
| Version coupling | Check if paired mods have compatible version requirements | B (load refusal) |
| Enum value mismatch | Check if shared enums have same numeric values across server/client | S (silent data corruption) |

### Step 3: Generate conflict report

Output a structured report:

```
## Conflict Report: <modpack name>

### Breaking (must resolve)
- [B] Mod A and Mod B both register route "/client/game/config" -- Mod A wins (registered first)

### Override (user decides priority)  
- [O] Mod C and Mod D both modify templateTable.Items["5447e1d04bdc2dff2f8b4567"]
  Predicted winner: Mod D (higher TypePriority)

### Silent corruption risk (flag for testing)
- [S] Mod E removes trader "ragman" -- existing saves with ragman quests may break

### Compatible
- [C] Mod F and Mod G -- no conflicts detected

### Unknown (requires IL analysis)
- [?] Mod H and Mod I -- both are BepInEx plugins, Harmony patch targets unknown
```

### Step 4: Present to user

Present the report with recommendations. For each breaking/override conflict:
- Explain what happens if both mods are kept
- Suggest resolution (drop one, adjust priority, find alternative)
- Let the user decide

## Known safe patterns

From the conflict taxonomy research, these patterns are verified safe:
- Multiple mods injecting into different tables (no shared keys)
- Multiple mods with different route prefixes
- Client mods with explicit BepInDependency chains (proper ordering)
- Server mods with well-separated TypePriority values

## Known dangerous patterns

- Multiple mods modifying the same item ID in templateTable
- Mods removing base game content that other mods reference
- Paired mods with mismatched versions
- Mods with circular BepInEx dependencies

## See also

- `docs/wayfinder/findings/001-spt-conflict-taxonomy.md` -- full 20-type taxonomy
- `curating-spt-modpack` -- the pipeline stage that invokes this skill
- `knowledge/spt-kb/curated/api-notes-4.1/di-container.md` -- DI system reference
- `knowledge/spt-kb/curated/api-notes-4.1/database-structure.md` -- table structure reference
