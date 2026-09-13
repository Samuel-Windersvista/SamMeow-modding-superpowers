# SPT Mod 编写规范——语料调查报告

调查对象：`knowledge/spt-kb/archive/forge/mods/` 下 `MANIFEST-sp-mod-2026-09.md` 列出的 297 个近期源码目录。所有计数均通过 PowerShell 脚本读取清单并映射到实际目录完成，判定命令在对应小节给出。

---

## A. 语料构成调查

### 1. 类型分布

判定方法：对每个源码目录递归查找 `.csproj`，读取文本后检查是否出现 `SPTarkov.Server`（服务端引用）与 `BepInEx`（客户端引用）；根目录存在 `package.json` 视为 JS/TS 服务端 mod；两者兼有记为 hybrid；两者皆无记为 other。

命令概要：

```powershell
$recent = Get-Content MANIFEST-sp-mod-2026-09.md | ... # 解析第一列目录名
foreach ($r in $recent) {
    $csprojs = Get-ChildItem -LiteralPath $r -File -Filter '*.csproj' -Recurse
    $hasPkg   = Test-Path -LiteralPath (Join-Path $r 'package.json')
    # 读取 csproj 文本匹配 SPTarkov\.Server / BepInEx
}
```

| 类型 | 计数 | 说明 |
|------|------|------|
| client-only | 148 | 仅引用 BepInEx 的客户端插件 |
| server-only (C#) | 43 | 引用 SPTarkov.Server.* 且不引用 BepInEx |
| server-only (JS/TS) | 11 | 仅有 `package.json`，无 C# 工程 |
| hybrid | 58 | 同时引用 SPTarkov.Server.* 与 BepInEx（常见 paired mod） |
| other | 37 | 无代码工程，多为资源/Reshade/数据库覆盖包 |
| 未能映射 | 1 | 清单中的 `[SAIN]-Twitch-Players_1895_source` 与目录名存在括号编码差异 |

### 2. 语言分布

| 语言 | 计数 | 判定 |
|------|------|------|
| C# | 278 | 存在 `.csproj` |
| TypeScript/JavaScript | 10 | 仅有 `package.json` 且无 csproj |
| 两者兼有 | 1 | 同时存在 csproj 与根目录 package.json |
| 其他 | 8 | 既无 csproj 也无 package.json |

命令概要：与 A1 相同，额外统计 `package.json` 与 `.csproj` 的存在性。

### 3. 结构惯例

近期目录顶层子目录出现频率（按出现次数降序）：

| 目录 | 出现次数 | 备注 |
|------|----------|------|
| Patches | 61 | 客户端 Harmony Patch 集中地 |
| Properties | 35 | C# 工程默认生成 |
| src | 34 | 通用源码目录 |
| Client | 33 | paired mod 的客户端工程 |
| Server | 28 | paired mod 的服务端工程 |
| .github | 27 | GitHub Actions / 工作流 |
| config | 27 | 配置文件目录 |
| Resources | 20 | 资源文件 |
| obj / bin | 19 / 19 | 构建产物，部分仓库误提交 |
| docs | 17 | 文档目录 |
| db | 14 | 数据库 JSON 覆盖 |
| Helpers / Utils | 14 / 12 | 辅助类目录 |
| tests | 13 | 测试目录 |
| Assets | 13 | 美术/音频资源 |
| Scripts | 12 | 脚本/工具 |

`csproj` 位置分布（近期 509 个 csproj）：

| 位置 | 计数 |
|------|------|
| 源码根目录 | 151 |
| Client/ | 39 |
| src/ | 37 |
| Server/ | 27 |
| tests/ | 20 |
| 其他（project/tools/Plugin/Prepatch/Fika 等） | 235 |

工程文件与仓库文件惯例：

| 文件/目录 | 出现次数（近期 297） | 比例 |
|-----------|----------------------|------|
| README* | 217 | ~73 % |
| LICENSE* | 227 | ~76 % |
| 构建脚本（*.sh / *.ps1 / *.bat / Build* / Makefile） | 35 | ~12 % |
| CI（.github / .gitlab-ci.yml） | 34 | ~11 % |

命令概要：

```powershell
Get-ChildItem -LiteralPath $modRoot -Directory | Group-Object Name | Sort-Object Count -Descending
Get-ChildItem -LiteralPath $modRoot -File | Where-Object { $_.Name -like 'README*' }
```

### 4. 元数据惯例

采样 19 个含 `IModMetadata` / `AbstractModMetadata` 的 C# 服务端 mod（路径见下表），加上 183 个 `[BepInPlugin("...",...)]` 客户端插件入口，观察结果如下：

- **服务端元数据文件位置极不统一**：`ModMetadata.cs`、`Metadata.cs`、`src/ModMetadata.cs`、`Server/Metadata.cs`、`csharp/*/Mod/*Metadata.cs` 等均有出现。
- **GUID 命名**：服务端样例中 `com.author.name` 反向域名占多数，但也能见到 `Ombarella`、`me.sol.sain`、`7Bpencil.WeaponCamoAndStickers` 等非 `com.*` 写法；客户端 `[BepInPlugin]` 中约 155/183 使用 `com.*`。
- **SptVersion 写法**：样例全部为 tilde 范围，例如 `~4.1.0`、`~4.1.2`、`~4.1.3`、`~4.1`，以及插值 `$"~{SAINVersionInfo.SptVersion}"`；未见到 `^4.1` 或精确版本。
- **ModDependencies**：C# 服务端元数据中几乎全部为 `null`、`[]` 或 `new()`；客户端依赖则通过 `[BepInDependency]` 声明，共 170 处，常见 soft/hard dependency。

采样文件路径：

- `Advanced-Flea-Simulator_2526_source/FleaSimulatorMetaData.cs`
- `Hideout-Overhaul_2982_source/src/ModMetadata.cs`
- `Tushonka-Territories_2942_source/Server/Metadata.cs`
- `VAI-Non-Realistic-Tapkov-Project_875_source/Server/VAI-NRTP/ModMetadata.cs`
- `Mission-Control_2653_source/csharp/MissionControl/Mod/MissionControlMetadata.cs`
- `Stat-Rewards_2655_source/src/StatRewards/Mod/StatRewardsMetadata.cs`
- `Kalashnikov-Enhanced-Modding_3010_source/KalashnikovEnhancedModdingServer/ModMetadata.cs`
- `Late-to-the-Party_814_source/Server/ModMetadata.cs`
- `SPT-Casino_2994_source/src/Casino.Server/ModMetadata.cs`
- `Realistic-Insurance_2949_source/RealisticInsuranceMetadata.cs`
- `Cobra's-SPT-Explorer_3009_source/CobrasSPTExplorer.Server/Metadata/ModMetadata.cs`
- `FieldKit-Dev-Toolkit,..._2856_source/Server/FieldKitServerMod.cs`
- `The-Blacklist-flea-market-enhancements_755_source/TheBlacklist/TheBlacklistModMetadata.cs`
- `SAIN-Solarint's-AI-Modifications-Full-AI-Combat-System-Replacement_791_source/SAINServerMod/SAINServermodMetadata.cs`
- `WTT-CommonLib_2310_source/WTT-ServerCommonLib/WTTServerCommonLib.cs`

命令概要：

```powershell
Select-String -LiteralPath $csFiles -Pattern 'IModMetadata|AbstractModMetadata'
Select-String -LiteralPath $csFiles -Pattern '\[BepInPlugin\("([^"]+)"'
```

### 5. 机制用法频率

在近期 297 个目录的 8066 个 `.cs` 文件中统计出现次数：

| 机制/类型 | 出现次数 | 备注 |
|-----------|----------|------|
| `[Injectable]` | 1018 | 服务端 DI 注册 |
| `IOnLoad` | 279 | 服务端生命周期 |
| `ISptLogger` | 614 | 服务端日志 |
| `StaticRouter` | 90 | 服务端路由 |
| `DynamicRouter` | 12 | 服务端动态路由 |
| `IOnUpdate` | 6 | 服务端更新回调 |
| `BaseUnityPlugin` | 275 | 客户端 BepInEx 入口 |
| `[BepInPlugin]` | 251 | 客户端插件声明 |
| `[BepInDependency]` | 170 | 客户端依赖声明 |
| `Logger.` | 4968 | 客户端 BepInEx `BaseUnityPlugin.Logger` 调用 |
| `ModMetadata` | 271 | 元数据类型引用 |
| `AbstractModMetadata` | 24 | 4.0 遗留抽象类 |

命令概要：

```powershell
$files = $recentPaths | ForEach-Object { Get-ChildItem -LiteralPath $_ -File -Filter '*.cs' -Recurse }
(Select-String -LiteralPath $files -Pattern '^\s*\[Injectable').Count
(Select-String -LiteralPath $files -Pattern '\bIOnLoad\b').Count
(Select-String -LiteralPath $files -Pattern '\bISptLogger\b').Count
...
```

### 6. 配置惯例

| 配置文件类型 | 出现次数 | 说明 |
|--------------|----------|------|
| `config.json`（任意位置） | 47 | 多为 mod 配置或资源元数据 |
| `*.jsonc`（任意位置） | 85 | 服务端默认配置、预设等常见 |
| `.cfg`（源码根目录） | 0 | BepInEx 运行时配置不在源码中提交 |

常见位置/命名样例：

- `Server/config/config.json`：`ABPS-Acid's-Bot-Placement-System_2097_source`
- `config/config.jsonc`：`Ammo-Stats_167_source`
- `config/defaultConfig.jsonc`：`Ammo-Stats-In-Names_2989_source`
- `Presets/default.jsonc`：`Advanced-Flea-Simulator_2526_source`
- `src/Config/settings.jsonc`：`Classic-Movement_1860-2_source`
- `data/config.json`：`bluehead's-AIO-Trader_374_source`

命令概要：

```powershell
Get-ChildItem -LiteralPath $modRoot -Filter 'config.json' -Recurse
Get-ChildItem -LiteralPath $modRoot -Filter '*.cfg' -Recurse
```

### 7. 异常样本

**结构较规范（各 3 例）**：

1. `Mission-Control_2653_source`：含 `README.md`、`LICENSE`、`.editorconfig`、`.gitignore`、`RELEASE_NOTES_v1.1.0.md`、`CLAUDE.md`，源码置于 `csharp/`，配置置于 `config/`，入口/服务/路由分层清晰。
2. `Hideout-Overhaul_2982_source`：`src/` 源码 + `db/` 数据，`ModMetadata.cs` 带完整 XML 注释说明每个字段，含 `slnx` 与 `LICENSE`、`README.md`。
3. `Tushonka-Territories_2942_source`：paired mod 典型布局，顶层 `Client/`、`Server/`、`Fika/` 分离，含 `sln` 与 `LICENSE`。

**结构较混乱（各 3 例）**：

1. `Ai-Limit_1945_source`：全部文件堆在根目录，使用 `packages.config` 与旧版 NuGet，未提供 README/LICENSE，且提交 `bin/` 与 `obj/`。
2. `AlwaysLevelEndurance_697_source`：目录下只有 `bin/` 与 `obj/`，无可见源码，疑似仅提交构建产物。
3. `Sicc-Case-Fix_2687_source`：全部源码与工程文件平铺在根目录，无子目录组织，依赖单一 `SiccCaseFix.cs`。

---

## B. 既有规范素材清单

### `skills/writing-spt-mod/SKILL.md`

现有指引要点：

- 明确区分 server/client/paired 三条管线。
- server 流程要求先查 KB、再按模板 scaffold、最后 build + 部署到 `user/mods/`。
- client 流程要求 BepInEx + Harmony，部署到 `BepInEx/plugins/`。
- 列出关键反模式：不要用源码 fork 编译、不要硬编码路径、不要跳过 KB、不要无端建 paired。

当前缺失/不足：

- 未规定目录结构（`Server/`/`Client/` 还是 `src/`）。
- 未规定 `ModMetadata.cs` 文件名与位置。
- 未说明 `README`/`LICENSE` 等仓库文件是否必须。
- 未给出 GUID 命名强制规则（仅模板用占位符）。
- 未给出 `config.json` 与 BepInEx `.cfg` 的选择标准。
- 未涉及 `.editorconfig`、`.gitignore`、CI 等工程 hygiene。

### `knowledge/spt-kb/curated/modding-guide/`

4 章：

1. `01-environment-toolchain.md` — 环境与工具链。
2. `02-server-mod-anatomy.md` — 服务端 mod 解剖（IModMetadata、OnLoadOrder、DI、路由、Patch、日志、Bundle、Web 页面、枚举扩展）。
3. `03-client-mod-anatomy.md` — 客户端 mod 解剖（BepInEx、Harmony、4.1 反混淆、枚举扩展流程）。
4. `04-example-walkthroughs.md` — 示例走读。

### `knowledge/spt-kb/curated/api-notes-4.1/`

9 个文件：`architecture-map.md`、`config-system.md`、`database-structure.md`、`di-container.md`、`http-routing.md`、`mod-loading.md`、`README.md`、`save-profile.md`、`server-mod-metadata-dll.md`。

### `knowledge/spt-kb/curated/api-notes-5.0/` 与 `api-notes-3.11/`

- 5.0：10 个文件，比 4.1 多 `modding-api.md`。
- 3.11：8 个文件，结构与 4.1 类似但面向 3.11 API。

### `knowledge/spt-kb/curated/operations/`

与工程实践/移植相关的文档：

- `3114-client-mod-build-gotchas.md` — SPT 3.11.4 客户端 mod 构建坑。
- `3114-eft016-perf-hotspots.md` — EFT 0.16 客户端性能热点。
- `3114-il-conflict-report*.md`（3 份）— IL/Harmony patch 冲突深度报告。
- `413-fork-transition.md` — 4.1.3 与官方归并 fork 迁移。
- `415-source-review-report.md` — 4.1.5 服务端源码审查。
- `5xx-source-verification.md` — 5.x 源码核实。
- `client-mod-compat-audit-playbook.md` — 客户端 mod Harmony 冲突审计 playbook。
- `conflict-analysis-input-model.md` — 冲突分析输入模型。
- `destructive-operation-guardrails.md` — 破坏性操作护栏。
- `pilot-experience-performancetweaks.md` — PerformanceTweaks 制作经验。

### `knowledge/spt-kb/curated/migration/`

- `api-mapping-311-to-41.md` / `server-mod-311-to-41.md` / `client-mod-311-to-41.md` — 3.11 → 4.1 API/服务端/客户端迁移。
- `bundle-311-to-41.md` / `bundle-compat-311-to-41.md` — Bundle 迁移与兼容性。
- 多份 `pilot-experience-*.md` — 具体 mod（WarsawTrader、SkillsExtended、ETT 等）迁移实战经验。
- `client-obfuscation-mapping-skills-extended.md` / `pilot-experience-kmytarkovapi.md` / `pilot-experience-lootingbots.md` — 客户端混淆名映射实战。

### `templates/server-mod/` 与 `templates/client-mod/`

- 服务端模板存在：`ServerModTemplate.csproj`、`src/ModMetadata.cs`、`src/ModEntry.cs`、`src/Services/ExampleService.cs`、`.gitignore`。
  - 已体现：`.NET 10` / `SPTarkov.Server.Core` + `SPTarkov.DI` + `SPTarkov.Common` 引用、`IModMetadata` 实现、`[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]`、`ISptLogger<T>`、`SPTInstallPath` 属性、`AppendTargetFrameworkToOutputPath=false`。
- 客户端模板存在：`ClientModTemplate.csproj`、`src/Plugin.cs`、`src/Configuration.cs`、`src/Patches/ExamplePatch.cs`、`.gitignore`。
  - 已体现：`netstandard2.1`、BepInEx + Harmony + Assembly-CSharp + UnityEngine 引用、`[BepInPlugin]`、`Config.Bind`、Harmony `PatchAll`。

### `knowledge/spt-kb/wiki-tushonka/modding/`

关键条目：

- `SPT_41_Modding.md` — 4.1 modding 总览。
- `SPT_41_Modding/Client_40_to_41.md`、`Server_40_to_41.md`、`Server_413_Changes.md`、`414_Changes.md` — 版本迁移。
- `SPT_41_Modding/client/Class_Name_Mappings.md` — 4.0 → 4.1 客户端反混淆映射。
- `SPT_41_Modding/server/Mod_Web_Pages.md` — 服务端 Web 页面。
- `SPT_41_Modding/EnumExtensions.md` — 枚举扩展。
- `tutorials/Client_Modding_Quick_Guide.md`、`debug_dnSpy.md`、`WTT_Vol1.md` — 入门与调试。
- `references/` — 身体部位、bot 类型、地图场景、任务值、技能、商人等参考数据。

### `docs/agents/` 与 `AGENTS.md`

- `docs/agents/domain.md` — 域文档使用约定（CONTEXT.md、ADR、术语表）。
- `docs/agents/issue-tracker.md` — `.scratch/<feature-slug>/` 目录结构、spec/issues 文件、triage label 约定。
- `docs/agents/triage-labels.md` — 五种 triage role。
- `AGENTS.md` — 引用上述文件，强调单上下文 repo 的 `CONTEXT.md` + `docs/adr/` 结构；未直接涉及 mod 工程规范。

---

## C. 结论

### 已有素材支撑、可立即写规则的维度

1. **服务端元数据结构**：`02-server-mod-anatomy.md`、`server-mod-metadata-dll.md`、模板 `ModMetadata.cs` 已完整说明 `IModMetadata` 各字段；可写规则要求必须实现 `IModMetadata`，且 `SptVersion` 用 tilde 范围。
2. **加载顺序与 DI**：`02-server-mod-anatomy.md`、`di-container.md`、模板 `ModEntry.cs` 已明确 `[Injectable(TypePriority = OnLoadOrder.X + n)]`、`IOnLoadAsync` 带 `CancellationToken`；可写规则禁止裸数字 `TypePriority`。
3. **客户端基本形态**：`03-client-mod-anatomy.md`、模板 `Plugin.cs` 已明确 `BaseUnityPlugin` + `[BepInPlugin]` + Harmony；可写规则要求 GUID 用反向域名并声明版本。
4. **配置系统选择**：`config-system.md`、模板 `Configuration.cs` 已区分服务端 POCO JSON（`IOnDIConstruct` 加载）与客户端 BepInEx `Config.Bind`；可写规则禁止把 config 类标 `[Injectable]`。
5. **目录 hygiene 基础**：模板自带 `.gitignore`；KB 强调不要提交 `bin/obj`。可写规则要求 `.gitignore` 必须排除 `bin/`、`obj/`、`.idea/` 等。
6. **构建目标框架**：模板明确 server `net10.0`、client `netstandard2.1`；可写规则禁止 client 用 `net10.0`。

### 缺少证据、需进一步调查/实验的维度

1. **ModDependencies 的标准用法**：语料中 C# 服务端 `ModDependencies` 几乎为空，未见真实依赖声明样例；需要官方示例或源码确认推荐写法（`new()` + `Add("guid", range)`）。
2. **config.json 的“标准”位置**：语料中配置文件散落 `config/`、`Server/config/`、`data/`、`src/Config/`、`Presets/` 等，KB 未强制统一路径；需确认加载 API（`modHelper.GetAbsolutePathToModFolder`）的推荐相对路径。
3. **paired mod 的仓库组织**：部分用顶层 `Server/` + `Client/`，部分用 `src/Server/` + `src/Client/`，部分是两个独立仓库；需给出推荐结构。
4. **Web 页面 / Blazor 元数据的工程模板**：`02-server-mod-anatomy.md` 有说明，但模板未包含 `IModBlazorMetadata` 示例，语料中实现方式多样。
5. **枚举扩展（Prepatcher）的目录与命名**：`EnumExtensions.md` 描述流程，但缺少 `user/patchers/<ModGuid>/` 下 JSON 文件命名与内容格式细节。
6. **客户端依赖版本范围**：`[BepInDependency]` 只支持字符串版本，语料中 soft/hard dependency 混用；需要规则说明何时用 soft、何时用 hard。

### 语料中观察到的最大不规范点 Top 5

1. **构建产物 `bin/` / `obj/` 大量提交到源码仓库**：近期 297 个目录中 `bin/` 与 `obj/` 各出现 19 次，部分目录（如 `Ai-Limit_1945_source`、`AlwaysLevelEndurance_697_source`）几乎只包含构建产物。估计约 6–7 % 的近期仓库存在此问题。
2. **缺乏 README / LICENSE**：约 27 % 无 README，约 24 % 无 LICENSE；对玩家与整合包维护者不友好。
3. **`ModMetadata.cs` 文件名与位置不统一**：同一接口实现散布为 `ModMetadata.cs`、`Metadata.cs`、`*Metadata.cs` 或内嵌于其他类，路径可能在根、`src/`、`Server/`、`csharp/*/Mod/` 等；增加工具自动扫描成本。
4. **GUID 命名不统一**：虽然 `com.author.name` 是主流，但仍存在 `Ombarella`、`me.sol.sain`、`7Bpencil.xxx` 等非反向域名写法，可能导致冲突。
5. **全部源码平铺根目录**：少数 mod（如 `Sicc-Case-Fix_2687_source`、`Ai-Limit_1945_source`）没有 `src/`、`Server/`、`Client/` 等分层，所有 `.cs` 与工程文件混在一起，可维护性差。

---

报告生成时间：2026-09-14
报告路径：`E:\云文件\GitHub\SamMeow-modding-superpowers\.scratch\modding-standard\corpus-survey.md`
