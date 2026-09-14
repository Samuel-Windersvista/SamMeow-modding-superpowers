---
name: writing-spt-mod
description: "Use when the user wants to write a new SPT 4.1 mod from scratch -- server mod (C#/IModMetadata/DI) or client mod (BepInEx/Harmony). Triggers: 'write a mod', 'create a mod', 'add a trader', 'add an item', '写个mod', '加个商人', '加个物品', 'add a quest', 'add custom weapon'."
---

# Writing SPT Mods

Write a new SPT 4.1 mod from scratch. Three pipelines: server mod, client mod, and paired (server + client). Choose based on what the mod does.

所有产出对照 SPT mod 编写规范：`knowledge/spt-kb/curated/modding-standard/README.md`。实现时按规则 ID 引用（`STD-<DOMAIN>-<nnn>`），不要模糊说"写得规范点"；偏离 MUST 规则须走本文末尾的[豁免流程](#豁免流程waiver)。

## Pipeline selection

| Mod does what? | Pipeline | Template |
|---|---|---|
| Add/edit traders, items, quests, database values, routes, bot config | **Server** | `templates/server-mod/` |
| Modify game behavior, UI, patches, client-side features | **Client** | `templates/client-mod/` |
| Both (paired mod) | **Both** -- single repo, shared spec | `templates/paired-mod/` |

## Server mod workflow

Template: `templates/server-mod/`。

### Step 1: Parse intent

Understand what the user wants in SPT terms:
- What game system does it touch? (traders, items, quests, loot, bots, etc.)
- What database tables does it need? (see `knowledge/spt-kb/curated/api-notes-4.1/database-structure.md`)
- Does it need custom routes? (see `api-notes-4.1/http-routing.md`)
- Does it need config? (see `api-notes-4.1/config-system.md`)

### Step 2: Research the API surface

Search the knowledge base for relevant references:
1. Read `knowledge/spt-kb/index.json`, filter by `version: ["4.1"]`, `domain: "server"`, and relevant `topic`
2. Check `knowledge/spt-kb/curated/recipes/` for a matching recipe (add-trader, add-item, add-quest, etc.)
3. If the recipe exists, follow it. If not, read the relevant API notes.
4. For API details not in the KB, read the 4.1 source at the user's configured `SPT410SourcePath` (see project config or ask user).

### Step 3: Scaffold from template

1. Copy `templates/server-mod/` to the user's mod project directory
2. Replace all `{{PLACEHOLDER}}` values (see `templates/server-mod/README.md`):
   - `{{ROOT_NAMESPACE}}` -- root namespace
   - `{{MOD_CLASS_NAME}}` -- mod class/assembly name (PascalCase, no spaces)
   - `{{MOD_NAME}}` -- human-readable mod name
   - `{{MOD_GUID}}` -- unique reverse-domain GUID for the mod (`STD-META-003`)
   - `{{MOD_VERSION}}` -- initial version, three-part semver (default "1.0.0", `STD-META-005`)
   - `{{MOD_DESCRIPTION}}` -- one-line description
   - `{{MOD_AUTHOR}}` -- author name
   - `{{MOD_LICENSE}}` -- license name
   - `{{SPT_INSTALL_PATH}}` -- path to user's SPT 4.1 installation (for DLL references)
3. Rename files: `ServerModTemplate.csproj` -> match mod name; `src/ModMetadata.cs` keeps its name (`STD-META-002`).
4. Keep the template's repository layout: root `.gitignore` (`STD-STRUCT-001`), source under `src/` rather than flat at repo root (`STD-STRUCT-003`), root `README.md` (`STD-STRUCT-005`) and `LICENSE` (`STD-STRUCT-006`). Exactly one `IModMetadata` implementation per mod directory (`STD-META-001`).

### Step 4: Write mod code

Follow the patterns from the knowledge base, anchored to the standard:
- Metadata: reverse-domain `ModGuid` (`STD-META-003`), tilde-range `SptVersion` (`STD-META-004`), three-part `Version` (`STD-META-005`), metadata in `ModMetadata.cs` (`STD-META-002`).
- Build: target `net10.0` (`STD-BUILD-001`), reference matching `SPTarkov.Server.*` (`STD-BUILD-004`), flat output via `AppendTargetFrameworkToOutputPath=false` (`STD-BUILD-005`), overridable install-path property (`STD-BUILD-006`).
- DI registration: annotate server classes with `[Injectable]` (`STD-SRV-001`); express `TypePriority` as `OnLoadOrder.X + n`, never bare numbers (`STD-SRV-002`).
- Lifecycle: async signatures that propagate `CancellationToken` (`STD-SRV-003`); periodic work implements `IOnUpdate` (`STD-SRV-004`).
- Table injection: inject database tables via constructor, modify in `OnLoad()`.
- Config: place under the mod's own directory with `config/config.jsonc` + `config/defaultConfig.jsonc` (`STD-CFG-001`/`STD-CFG-002`/`STD-CFG-005`); the config POCO must NOT carry `[Injectable]` (`STD-CFG-004`); load it in `IOnDIConstruct` with `AddSingleton` (`STD-CFG-003`).
- Routes: register via `StaticRouter` / `DynamicRouter` (`STD-SRV-005`); router actions carry `CancellationToken` (`STD-SRV-006`); routers only declare routes, business logic lives in injectable Callbacks classes (`STD-SRV-007`).
- Logging: inject `ISptLogger<T>` (`STD-SRV-008` / `STD-LOG-001`); never swallow exceptions silently -- log and degrade (`STD-LOG-004`); let `OperationCanceledException` propagate (`STD-LOG-005`).
- Dependencies: declare server hard dependencies via `ModDependencies` (`STD-DEP-001`); leave it empty when there is no hard dependency (`STD-DEP-002`).
- Bundles/assets: register server bundles under `bundles/` with `bundles.json` (`STD-BND-001`/`STD-BND-002`).

Key references:
- `knowledge/spt-kb/curated/api-notes-4.1/di-container.md` -- DI system
- `knowledge/spt-kb/curated/api-notes-4.1/mod-loading.md` -- mod lifecycle
- `knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md` -- full anatomy

### Step 5: Build and verify

```
dotnet build -c Release
```

Release build is the first verification step (`STD-VERIFY-001`). Compilation references come from the user's installed SPT (`$(SPTInstallPath)` property in csproj, `STD-BUILD-006`). Do NOT reference the source fork for compilation -- it is read-only reference.

**Verification baseline (Level B):** (`STD-VERIFY-002`)
1. Deploy DLL to `<SPT>/SPT_Runtime/user/mods/<ModName>/` (via MO2 or direct copy for dev)
2. Launch SPT.Server.exe
3. Check server console/log for mod registration confirmation -- assert the load log (`STD-VERIFY-003`)
4. Check for DI errors, config type mismatches, or startup crashes

**Optional functional verification (Level C):** (`STD-VERIFY-009`) per mod type, where feasible:
- Added a trader? Query the trader list endpoint.
- Added an item? Check the item appears in the database.
- Modified loot weights? Compare before/after values.

## Client mod workflow

Template: `templates/client-mod/`。

### Step 1: Parse intent

- What game behavior does it modify?
- What classes/methods does it need to patch? (see `knowledge/spt-kb/wiki/SPT_41/modding/client/Class_Name_Mappings.md`)
- Does it need BepInEx config entries?
- Does it depend on other client mods? (BepInDependency)

### Step 2: Research the patch targets

1. Read `knowledge/spt-kb/wiki/SPT_41/Client_40_to_41.md` for 4.1 changes
2. Read `knowledge/spt-kb/wiki/SPT_41/modding/client/Class_Name_Mappings.md` for deobfuscated class names
3. Check `external/spt-archive/modules/` for official client module source as reference
4. For deeper analysis, the user may need dnSpy/ILSpy to inspect `Assembly-CSharp.dll`

### Step 3: Scaffold from template

1. Copy `templates/client-mod/` to the user's mod project directory
2. Replace all `{{PLACEHOLDER}}` values (see `templates/client-mod/README.md`; same format as server mod)
3. Rename files: `ClientModTemplate.csproj` -> match mod name
4. Keep the repository layout: root `.gitignore` (`STD-STRUCT-001`), source under `src/` (`STD-STRUCT-003`), root `README.md` (`STD-STRUCT-005`) and `LICENSE` (`STD-STRUCT-006`).
5. Build settings: client targets `netstandard2.1` for 4.1.5 (`STD-BUILD-002`), references runtime assemblies via `<HintPath>` + `<Private>false</Private>` (`STD-BUILD-003`), flat output (`STD-BUILD-005`), overridable install-path property (`STD-BUILD-006`).

### Step 4: Write mod code

Follow BepInEx + Harmony patterns, anchored to the standard:
- Plugin entry: `BaseUnityPlugin` subclass with `[BepInPlugin]` (`STD-CLI-001`); plugin GUID uses reverse-domain notation (`STD-CLI-002` / `STD-META-006`); three-part version (`STD-META-005`).
- Patches: annotate with `[HarmonyPatch]` (`STD-CLI-003`) and use the target version's real type names (`STD-CLI-004`); apply patches in the entry method and undo them in the lifecycle callback (`STD-CLI-007`).
- Patch strategy: prefer Prefix/Postfix with an independent toggle and fail-open behavior (`STD-PERF-004`).
- Config: declare via `Config.Bind` (`STD-CFG-006`); do not hand-roll JSON config reading.
- Logging: use the BepInEx log source (`Logger`) (`STD-CLI-006` / `STD-LOG-003`).
- Dependencies: declare required client deps with `[BepInDependency]` (`STD-CLI-005` / `STD-DEP-004`); optional deps use the `SoftDependency` flag and null-check/degrade (`STD-DEP-005`).

### Step 5: Build and verify

Same as server mod, but deploy to `<SPT>/BepInEx/plugins/`.

**Verification:** Check BepInEx console output for plugin load confirmation -- assert the client plugin load log (`STD-VERIFY-004`). Check for Harmony patch errors.

## Paired mod (server + client)

Template: `templates/paired-mod/` (single repo, single solution, single release zip).

Some mods need both a server component and a client component:
1. Copy `templates/paired-mod/` and keep the single-repo `Client/` + `Server/` layering, with pure shared data in `Shared/` (`STD-STRUCT-004`)
2. Server mod handles data/logic; client mod handles presentation/interaction -- server owns "rules and data", client owns "presentation" (`STD-STRUCT-004`)
3. Both halves share one version number, defined once in `Directory.Build.props` (`STD-META-007` / `STD-PKG-005`)
4. Communication via custom routes (`STD-SRV-005`/`STD-SRV-006`/`STD-SRV-007`); see `knowledge/spt-kb/curated/recipes/10-mod-communication.md`
5. Client can check server mod presence via reflection (see conflict taxonomy patterns from `docs/wayfinder/findings/001-spt-conflict-taxonomy.md`)
6. Package as one zip covering both halves: game-root-relative archive layout (`STD-PKG-001`), both halves in the same archive and the server half in a single `user/mods/<Name>/` directory (`STD-PKG-003`), exactly one `IModMetadata` implementation in that directory (`STD-PKG-004`), README/LICENSE shipped with the archive (`STD-PKG-006`)

## 豁免流程（Waiver）

偏离 MUST 规则必须留痕，流程见 `knowledge/spt-kb/curated/modding-standard/README.md` 的「豁免流程（Waiver）」：

1. **适用范围**：仅 MUST 规则。SHOULD 偏离只需一句话说明理由（无需豁免标记）；MAY 不适用。
2. **记录位置**：mod 仓库 README 或项目 dev-log（整合包项目记 dev-log）。
3. **记录内容**：① 理由 -- 为什么无法遵守；② 替代方案 -- 如何达成同等效果或安全。
4. **标记格式**：`Waiver: STD-XXX-nnn`（每行一条；`XXX` 为 domain slug、`nnn` 为规则编号）。

mod README / dev-log 中的记录示例：

```markdown
## Standard Waivers
- Waiver: STD-XXX-nnn — 理由：<...>；替代方案：<...>
```

## Standard reference

- 规则集索引：`knowledge/spt-kb/curated/modding-standard/README.md`（五要素格式、Rule ID 规则、豁免流程）
- 13 维度文件：`knowledge/spt-kb/curated/modding-standard/01-structure.md` 至 `13-perf-security.md`
- 版本对照：`knowledge/spt-kb/curated/modding-standard/version-matrix.md`（4.1.5 ↔ 5.0，含 `STD-VER-*` 与 `STD-BUILD-*` 差异）

## Anti-patterns

- **Do not** reference the SPT source fork for compilation -- use installed SPT DLLs
- **Do not** hardcode paths -- use `$(SPTInstallPath)` property
- **Do not** skip the knowledge base lookup -- recipes exist for common tasks
- **Do not** guess API names -- verify against 4.1 source or API notes
- **Do not** create both server and client projects unless the mod truly needs both
