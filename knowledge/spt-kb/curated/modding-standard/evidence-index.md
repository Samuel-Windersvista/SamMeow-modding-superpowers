---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 证据索引（Evidence Index）

> 用途：Modding Standard 全部规则的统一证据来源。规则文件以 `EV-*` 锚点引用本文件条目。
> 固化自：`.scratch/modding-standard/corpus-survey.md`（2026-09-14）、`.scratch/modding-standard/gap-investigation.md`（2026-09-14）。
> 维护约定：语料或源码更新后，按文末「复核触发」复核受影响条目。

## 0. 证据分级约定

- **机制证据（源码/接口）**：直接来自 SPT 4.1/5.0 源码、官方示例与模板的接口定义和校验逻辑，优先级最高。
- **语料证据（统计）**：来自 297 个近期源码目录 / 392 个 `_source` 目录 / 8066 个 `.cs` 文件的统计，优先级次之。
- **经验证据**：从既有 KB 文档与实测归纳的惯例，优先级最低。
- **MUST 需机制 + 语料双源**：任何 MUST 级规则必须同时具备机制证据与语料证据（语料样本 ≥10 或全量检查）；仅有语料归纳的结论不得升级为 MUST。
- **规则引用格式**：规则条目以 `（EV-XXX）` 形式引用本文件锚点，例如 `（EV-GAP-DEP）`。

## 1. 语料证据（EV-CORPUS-*）

### EV-CORPUS-SOURCE — 调查范围

- 近期源码目录：**297 个**，来自 `knowledge/spt-kb/archive/forge/mods/MANIFEST-sp-mod-2026-09.md` 所列目录。
- 全量源码目录：**392 个** `_source` 目录（同一 `archive/forge/mods/` 下）。
- 机制调查源码：`E:\云文件\GitHub\SamMeow_SPT410_source_code`、`E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code`。
- 官方示例：`external/spt-archive/server-mod-examples`、`mod-examples`、`modules`、`wiki`。
- 项目模板：`templates/server-mod/`、`templates/client-mod/`。
- 报告生成时间：2026-09-14。
- 报告路径：`E:\云文件\GitHub\SamMeow-modding-superpowers\.scratch\modding-standard\corpus-survey.md`、`E:\云文件\GitHub\SamMeow-modding-superpowers\.scratch\modding-standard\gap-investigation.md`。
- 统计方法：所有计数均通过 PowerShell 脚本读取清单并映射到实际目录完成，判定命令在各小节给出。

### EV-CORPUS-TYPE — 类型分布

判定方法：对每个源码目录递归查找 `.csproj`，读取文本后检查是否出现 `SPTarkov.Server`（服务端引用）与 `BepInEx`（客户端引用）；根目录存在 `package.json` 视为 JS/TS 服务端 mod；两者兼有记为 hybrid；两者皆无记为 other。

| 类型 | 计数 | 说明 |
|------|------|------|
| client-only | 148 | 仅引用 BepInEx 的客户端插件 |
| server-only (C#) | 43 | 引用 SPTarkov.Server.* 且不引用 BepInEx |
| server-only (JS/TS) | 11 | 仅有 `package.json`，无 C# 工程 |
| hybrid | 58 | 同时引用 SPTarkov.Server.* 与 BepInEx（常见 paired mod） |
| other | 37 | 无代码工程，多为资源/Reshade/数据库覆盖包 |

> 复核注（2026-09-14）：原报告记「未能映射 1」例（`[SAIN]-Twitch-Players_1895_source`，称括号编码差异）；复核确认该目录实际存在（`Test-Path -LiteralPath` 为真），MANIFEST 实表 297 行、`total=297 ok=297 missing=0`，本表五类合计 297。原 1 例差异系源报告映射口径（PowerShell 通配符对 `[ ]` 的处理）所致；本表已按实际口径修正。

命令概要：

```powershell
$recent = Get-Content MANIFEST-sp-mod-2026-09.md | ...
foreach ($r in $recent) {
    $csprojs = Get-ChildItem -LiteralPath $r -File -Filter '*.csproj' -Recurse
    $hasPkg   = Test-Path -LiteralPath (Join-Path $r 'package.json')
}
```

### EV-CORPUS-LANG — 语言分布

| 语言 | 计数 | 判定 |
|------|------|------|
| C# | 278 | 存在 `.csproj` |
| TypeScript/JavaScript | 10 | 仅有 `package.json` 且无 csproj |
| 两者兼有 | 1 | 同时存在 csproj 与根目录 package.json |
| 其他 | 8 | 既无 csproj 也无 package.json |

命令概要：与类型分布（EV-CORPUS-TYPE）相同，额外统计 `package.json` 与 `.csproj` 的存在性。

### EV-CORPUS-STRUCT — 结构惯例

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

### EV-CORPUS-CSPROJ — 工程文件普查（csproj / .gitignore / ModMetadata.cs）

> 补充普查（2026-09-14，ticket 02 撰写期执行，只读）。对象：MANIFEST 近期 297 个源码目录，共 509 个 csproj。供 STRUCT / META / BUILD 维度规则引用。

| 指标 | 结果 |
|------|------|
| `.gitignore` 存在 | 269 / 297 |
| 文件名恰为 `ModMetadata.cs` | 63 / 297（其余散落 `Metadata.cs`、`*Metadata.cs` 等） |
| 服务端 csproj（引用 `SPTarkov.Server`） | 114 |
| └ TargetFramework | `net10.0` 95 / `net9.0` 17 / `net9.0-windows` 1 |
| └ 引用方式 | `<PackageReference>` 83 / 本地 DLL 31 |
| └ `AppendTargetFrameworkToOutputPath=false` | 67；`<Private>false</Private>` 26；`<HintPath>` 34 |
| 客户端 csproj（引用 BepInEx、不含服务端） | 264 |
| └ TargetFramework | `netstandard2.1` 135 / `net472` 79 / `net471` 7 / `netstandard21` 3 / `net48` 2 / `net46` 1 / `net10.0` 1 |
| └ `<HintPath>` | 226；`AppendTargetFrameworkToOutputPath=false` 43；`<Private>false</Private>` 111 |
| `<Version>` 格式 | 三段式 214 / 四段式 5 / MSBuild 属性插值 72（`$(AssemblyVersion)` 49、`$(ModVersion)` 14、`$(Version)` 9） |

命令（完整脚本，可复现）：

```powershell
$root = 'knowledge\spt-kb\archive\forge\mods'
$manifest = Join-Path $root 'MANIFEST-sp-mod-2026-09.md'
$dirs = Get-Content -LiteralPath $manifest | Where-Object { $_ -match '^\| ' } |
  ForEach-Object { ($_ -split '\|')[1].Trim() } | Where-Object { $_ -like '*_source' } | Select-Object -Unique

$serverTf=@{}; $clientTf=@{}
$srv=0; $cli=0; $sAppend=0; $cAppend=0; $sPriv=0; $cPriv=0; $sHint=0; $cHint=0
$gitignore=0; $modMetaFile=0; $csprojTotal=0
$ver3=0; $ver4=0; $verOther=0; $appendTotal=0
$srvPkgRef=0; $srvLocalRef=0; $cliHintRef=0
function Add-Tf($h,$k){ if($k){ if($h.ContainsKey($k)){$h[$k]++}else{$h[$k]=1} } }

foreach ($d in $dirs) {
  $p = Join-Path $root $d
  if (Test-Path -LiteralPath (Join-Path $p '.gitignore')) { $gitignore++ }
  if (Get-ChildItem -LiteralPath $p -File -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq 'ModMetadata.cs' }) { $modMetaFile++ }
  foreach ($c in (Get-ChildItem -LiteralPath $p -File -Filter '*.csproj' -Recurse -ErrorAction SilentlyContinue)) {
    $csprojTotal++
    $t = Get-Content -LiteralPath $c.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $t) { continue }
    $tf=$null; if ($t -match '<TargetFramework>([^<]+)</TargetFramework>') { $tf=$Matches[1].Trim() }
    $isS = ($t -match 'Include="SPTarkov\.Server' -or $t -match 'SPTarkov\.Server\.Core\.dll')
    $isC = ($t -match 'Include="BepInEx' -or $t -match 'BepInEx\.dll')
    $sPkg = ($t -match '<PackageReference[^>]*Include="SPTarkov\.Server')
    if ($isS) { $srv++; Add-Tf $serverTf $tf; if($sPkg){$srvPkgRef++}else{$srvLocalRef++} }
    if ($isC -and -not $isS) { $cli++; Add-Tf $clientTf $tf; if($t -match '<HintPath>'){$cliHintRef++} }
    if ($isS -and -not $isC) { if($t -match 'AppendTargetFrameworkToOutputPath>false'){$sAppend++}; if($t -match '<Private>false</Private>'){$sPriv++}; if($t -match '<HintPath>'){$sHint++} }
    if ($isC -and -not $isS) { if($t -match 'AppendTargetFrameworkToOutputPath>false'){$cAppend++}; if($t -match '<Private>false</Private>'){$cPriv++} }
    if ($t -match 'AppendTargetFrameworkToOutputPath>false') { $appendTotal++ }
    if ($t -match '<Version>([^<]+)</Version>') {
      $v=$Matches[1].Trim()
      if ($v -match '^\d+\.\d+\.\d+$'){$ver3++} elseif ($v -match '^\d+\.\d+\.\d+\.\d+$'){$ver4++} else {$verOther++}
    }
  }
}
[pscustomobject]@{ manifestDirs=$dirs.Count; csprojTotal=$csprojTotal; gitignore=$gitignore; modMetadataFile=$modMetaFile
  serverCsproj=$srv; serverTargetFramework=$serverTf; serverPkgRef=$srvPkgRef; serverLocalRef=$srvLocalRef
  serverAppendFalse=$sAppend; serverPrivateFalse=$sPriv; serverHintPath=$sHint
  clientCsproj=$cli; clientTargetFramework=$clientTf; clientHintPath=$cliHintRef; clientAppendFalse=$cAppend; clientPrivateFalse=$cPriv
  appendTargetFrameworkFalseTotal=$appendTotal; version3Seg=$ver3; version4Seg=$ver4; versionOther=$verOther } | ConvertTo-Json -Depth 6
```

结果（2026-09-14 实跑）：

```json
{
  "manifestDirs": 297, "csprojTotal": 509, "gitignore": 269, "modMetadataFile": 63,
  "serverCsproj": 114,
  "serverTargetFramework": { "net9.0": 17, "net9.0-windows": 1, "net10.0": 95 },
  "serverPkgRef": 83, "serverLocalRef": 31,
  "serverAppendFalse": 67, "serverPrivateFalse": 26, "serverHintPath": 34,
  "clientCsproj": 264,
  "clientTargetFramework": { "net471": 7, "net472": 79, "net46": 1, "net10.0": 1, "netstandard21": 3, "netstandard2.1": 135, "net48": 2 },
  "clientHintPath": 226, "clientAppendFalse": 43, "clientPrivateFalse": 111,
  "appendTargetFrameworkFalseTotal": 145,
  "version3Seg": 214, "version4Seg": 5, "versionOther": 72
}
```

注：`versionOther` 72 例为 MSBuild 属性插值（`$(AssemblyVersion)` 49、`$(ModVersion)` 14、`$(Version)` 9），非违规。`net9.0` 17 例可能含向 5.x 迁移的过渡期样本。

### EV-CORPUS-META — 元数据惯例

采样 19 例含 `IModMetadata` / `AbstractModMetadata` 的 C# 服务端 mod（下表列出 15 例坐标），加上 183 个 `[BepInPlugin("...",...)]` 客户端插件入口，观察结果如下：

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
- `FieldKit-Dev-Toolkit,-Weapon-Control,-Loot-Entity-Browser,-Spawning-&-ESP_2856_source/Server/FieldKitServerMod.cs`
- `The-Blacklist-flea-market-enhancements_755_source/TheBlacklist/TheBlacklistModMetadata.cs`
- `SAIN-Solarint's-AI-Modifications-Full-AI-Combat-System-Replacement_791_source/SAINServerMod/SAINServermodMetadata.cs`
- `WTT-CommonLib_2310_source/WTT-ServerCommonLib/WTTServerCommonLib.cs`

命令概要：

```powershell
Select-String -LiteralPath $csFiles -Pattern 'IModMetadata|AbstractModMetadata'
Select-String -LiteralPath $csFiles -Pattern '\[BepInPlugin\("([^"]+)"'
```

### EV-CORPUS-MECH — 机制用法频率

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
```

### EV-CORPUS-CFG — 配置惯例

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

### EV-CORPUS-ANOMALY — 异常样本

**结构较规范（各 3 例）**：

1. `Mission-Control_2653_source`：含 `README.md`、`LICENSE`、`.editorconfig`、`.gitignore`、`RELEASE_NOTES_v1.1.0.md`、`CLAUDE.md`，源码置于 `csharp/`，配置置于 `config/`，入口/服务/路由分层清晰。
2. `Hideout-Overhaul_2982_source`：`src/` 源码 + `db/` 数据，`ModMetadata.cs` 带完整 XML 注释说明每个字段，含 `slnx` 与 `LICENSE`、`README.md`。
3. `Tushonka-Territories_2942_source`：paired mod 典型布局，顶层 `Client/`、`Server/`、`Fika/` 分离，含 `sln` 与 `LICENSE`。

**结构较混乱（各 3 例）**：

1. `Ai-Limit_1945_source`：全部文件堆在根目录，使用 `packages.config` 与旧版 NuGet，未提供 README/LICENSE，且提交 `bin/` 与 `obj/`。
2. `AlwaysLevelEndurance_697_source`：目录下只有 `bin/` 与 `obj/`，无可见源码，疑似仅提交构建产物。
3. `Sicc-Case-Fix_2687_source`：全部源码与工程文件平铺在根目录，无子目录组织，依赖单一 `SiccCaseFix.cs`。

### EV-CORPUS-TOP5 — 语料最大不规范点 Top 5

1. **构建产物 `bin/` / `obj/` 大量提交到源码仓库**：近期 297 个目录中 `bin/` 与 `obj/` 各出现 19 次，部分目录（如 `Ai-Limit_1945_source`、`AlwaysLevelEndurance_697_source`）几乎只包含构建产物。估计约 6–7 % 的近期仓库存在此问题。
2. **缺乏 README / LICENSE**：约 27 % 无 README，约 24 % 无 LICENSE；对玩家与整合包维护者不友好。
3. **`ModMetadata.cs` 文件名与位置不统一**：同一接口实现散布为 `ModMetadata.cs`、`Metadata.cs`、`*Metadata.cs` 或内嵌于其他类，路径可能在根、`src/`、`Server/`、`csharp/*/Mod/` 等；增加工具自动扫描成本。
4. **GUID 命名不统一**：虽然 `com.author.name` 是主流，但仍存在 `Ombarella`、`me.sol.sain`、`7Bpencil.xxx` 等非反向域名写法，可能导致冲突。
5. **全部源码平铺根目录**：少数 mod（如 `Sicc-Case-Fix_2687_source`、`Ai-Limit_1945_source`）没有 `src/`、`Server/`、`Client/` 等分层，所有 `.cs` 与工程文件混在一起，可维护性差。

### EV-CORPUS-MATERIALS — 既有规范素材

`skills/writing-spt-mod/SKILL.md`：

- 现有指引要点：明确区分 server/client/paired 三条管线；server 流程要求先查 KB、再按模板 scaffold、最后 build + 部署到 `user/mods/`；client 流程要求 BepInEx + Harmony，部署到 `BepInEx/plugins/`；列出关键反模式：不要用源码 fork 编译、不要硬编码路径、不要跳过 KB、不要无端建 paired。
- 当前缺失/不足：未规定目录结构（`Server/`/`Client/` 还是 `src/`）；未规定 `ModMetadata.cs` 文件名与位置；未说明 `README`/`LICENSE` 等仓库文件是否必须；未给出 GUID 命名强制规则（仅模板用占位符）；未给出 `config.json` 与 BepInEx `.cfg` 的选择标准；未涉及 `.editorconfig`、`.gitignore`、CI 等工程 hygiene。

`knowledge/spt-kb/curated/modding-guide/`（4 章）：

1. `01-environment-toolchain.md` — 环境与工具链。
2. `02-server-mod-anatomy.md` — 服务端 mod 解剖（IModMetadata、OnLoadOrder、DI、路由、Patch、日志、Bundle、Web 页面、枚举扩展）。
3. `03-client-mod-anatomy.md` — 客户端 mod 解剖（BepInEx、Harmony、4.1 反混淆、枚举扩展流程）。
4. `04-example-walkthroughs.md` — 示例走读。

`knowledge/spt-kb/curated/api-notes-4.1/`（9 个文件）：

- `architecture-map.md`、`config-system.md`、`database-structure.md`、`di-container.md`、`http-routing.md`、`mod-loading.md`、`README.md`、`save-profile.md`、`server-mod-metadata-dll.md`。

`knowledge/spt-kb/curated/api-notes-5.0/` 与 `api-notes-3.11/`：

- 5.0：9 个文件（比 4.1 多 `modding-api.md`、少 `server-mod-metadata-dll.md`）。
- 3.11：8 个文件，结构与 4.1 类似但面向 3.11 API。

`knowledge/spt-kb/curated/operations/`（工程实践/移植相关）：

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

`knowledge/spt-kb/curated/migration/`：

- `api-mapping-311-to-41.md` / `server-mod-311-to-41.md` / `client-mod-311-to-41.md` — 3.11 → 4.1 API/服务端/客户端迁移。
- `bundle-311-to-41.md` / `bundle-compat-311-to-41.md` — Bundle 迁移与兼容性。
- 多份 `pilot-experience-*.md` — 具体 mod（WarsawTrader、SkillsExtended、ETT 等）迁移实战经验。
- `client-obfuscation-mapping-skills-extended.md` / `pilot-experience-kmytarkovapi.md` / `pilot-experience-lootingbots.md` — 客户端混淆名映射实战。

`templates/server-mod/` 与 `templates/client-mod/`：

- 服务端模板存在：`ServerModTemplate.csproj`、`src/ModMetadata.cs`、`src/ModEntry.cs`、`src/Services/ExampleService.cs`、`.gitignore`。
  - 已体现：`.NET 10` / `SPTarkov.Server.Core` + `SPTarkov.DI` + `SPTarkov.Common` 引用、`IModMetadata` 实现、`[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]`、`ISptLogger<T>`、`SPTInstallPath` 属性、`AppendTargetFrameworkToOutputPath=false`。
- 客户端模板存在：`ClientModTemplate.csproj`、`src/Plugin.cs`、`src/Configuration.cs`、`src/Patches/ExamplePatch.cs`、`.gitignore`。
  - 已体现：`netstandard2.1`、BepInEx + Harmony + Assembly-CSharp + UnityEngine 引用、`[BepInPlugin]`、`Config.Bind`、Harmony `PatchAll`。

`knowledge/spt-kb/wiki-tushonka/modding/` 关键条目：

- `SPT_41_Modding.md` — 4.1 modding 总览。
- `SPT_41_Modding/Client_40_to_41.md`、`Server_40_to_41.md`、`Server_413_Changes.md`、`414_Changes.md` — 版本迁移。
- `SPT_41_Modding/client/Class_Name_Mappings.md` — 4.0 → 4.1 客户端反混淆映射。
- `SPT_41_Modding/server/Mod_Web_Pages.md` — 服务端 Web 页面。
- `SPT_41_Modding/EnumExtensions.md` — 枚举扩展。
- `tutorials/Client_Modding_Quick_Guide.md`、`debug_dnSpy.md`、`WTT_Vol1.md` — 入门与调试。
- `references/` — 身体部位、bot 类型、地图场景、任务值、技能、商人等参考数据。

`docs/agents/` 与 `AGENTS.md`：

- `docs/agents/domain.md` — 域文档使用约定（CONTEXT.md、ADR、术语表）。
- `docs/agents/issue-tracker.md` — `.scratch/<feature-slug>/` 目录结构、spec/issues 文件、triage label 约定。
- `docs/agents/triage-labels.md` — 五种 triage role。
- `AGENTS.md` — 引用上述文件，强调单上下文 repo 的 `CONTEXT.md` + `docs/adr/` 结构；未直接涉及 mod 工程规范。

### EV-CORPUS-OPENGAPS — 开放缺口与可写规则清单

corpus-survey.md C 节结论（方向性提示，不含统计数字）：

**已有素材支撑、可立即写规则的维度（6 条）**：

1. 服务端元数据结构（`modding-guide/02-server-mod-anatomy.md`、`api-notes-4.1/server-mod-metadata-dll.md`、模板 `ModMetadata.cs`）→ 要求实现 `IModMetadata`、`SptVersion` 用 tilde 范围。
2. 加载顺序与 DI（`modding-guide/02`、`api-notes-4.1/di-container.md`、模板 `ModEntry.cs`）→ 禁止裸数字 `TypePriority`。
3. 客户端基本形态（`modding-guide/03-client-mod-anatomy.md`、模板 `Plugin.cs`）→ GUID 用反向域名并声明版本。
4. 配置系统选择（`api-notes-4.1/config-system.md`、模板 `Configuration.cs`）→ 禁止把 config 类标 `[Injectable]`。
5. 目录 hygiene 基础（模板 `.gitignore`、KB）→ `.gitignore` 必须排除 `bin/`、`obj/`、`.idea/` 等。
6. 构建目标框架（模板）→ 禁止 client 用 `net10.0`。

**缺少证据、需进一步调查/实验的维度（6 条；1–3 已解决）**：

1. ModDependencies 的标准用法 → 已解决，见 `EV-GAP-DEP`（机制推断，无语料先例）。
2. config.json 的"标准"位置 → 已解决，见 `EV-GAP-CFG`。
3. paired mod 的仓库组织 → 已解决，见 `EV-GAP-PAIRED`。
4. **Web 页面 / Blazor 元数据的工程模板**（开放）：`02-server-mod-anatomy.md` 有说明，但模板未包含 `IModBlazorMetadata` 示例，语料中实现方式多样。
5. **枚举扩展（Prepatcher）的目录与命名**（开放）：`EnumExtensions.md` 描述流程，但缺少 `user/patchers/<ModGuid>/` 下 JSON 文件命名与内容格式细节。
6. **客户端依赖版本范围**（开放）：`[BepInDependency]` 只支持字符串版本，语料中 soft/hard dependency 混用；需要规则说明何时用 soft、何时用 hard。

## 2. 缺口调查证据（EV-GAP-*）

### EV-GAP-DEP — ModDependencies 标准用法

**机制端结论**：`ModDependencies` 是**硬依赖**——声明的依赖 mod 必须存在且版本满足范围，否则服务器拒绝加载全部 mod。

- `Libraries/SPTarkov.Server.Core/Models/Spt/Mod/IModMetadata.cs:91` 定义：

  ```csharp
  /// Key is the dependency's Mod GUID, value is the required version range
  Dictionary<string, Range>? ModDependencies { get; init; }
  ```

- `SPTarkov.Server/Modding/ModValidator.cs:243-287` 校验逻辑：
  - `ModDependencies == null` 直接通过；
  - key 为 `pkg.ModGuid`（自身）只抛 Warning，不阻止加载；
  - 依赖找不到 → `errorsFound = true`；
  - 依赖版本不满足 `semVer.Satisfies(value.Version, requiredVersion)` → `errorsFound = true`；
  - 任一错误都会导致 `ModValidator` 返回空列表（`modloader-no_mods_loaded`）。
- 本地化文本 `Libraries/SPTarkov.Server.Assets/SPT_Data/database/locales/server/en.json:227/241`：
  - `modloader-missing_dependency`: "Mod: {{mod}} requires: {{modDependency}} to be installed."
  - `modloader-outdated_dependency`: "Mod: {{mod}} requires: {{modDependency}} version: {{requiredVersion}}. Current installed version is: {{currentVersion}}"

**实践端结论**：**语料中没有任何真实非空依赖声明**。所有服务端 `ModDependencies` 都是 `null`、`new()`、`new Dictionary<...>()`、`[]`。

- 近期 297 个目录中，157 个 `.cs` 文件出现 `ModDependencies`，全部为**空初始化**；
- 全量 392 个 `_source` 目录中，28 个文件以 `= new()` 形式初始化，**0 个**包含真实键值；
- 官方 `server-mod-examples`（`1Logging/Logging.cs:60`、`13AddTraderWithAssortJson/AddTraderWithAssortJson.cs:25` 等）全部置为 `null`；
- 4.1/5.0 源码中的 `Testing/TestMod/TestMod.cs:21`、`Testing/TestMod2/TestMod2.cs:22` 同样为 `null`。

**推荐写法**：

```csharp
public Dictionary<string, Range>? ModDependencies { get; init; } = new()
{
    ["com.example.required-mod"] = new Range("~1.0.0"),
    ["com.example.another-dep"] = new Range(">=2.0.0 <3.0.0"),
};
```

- **key**：依赖 mod 的 `ModGuid`（不是程序集名、不是 nuget 包名）。
- **value**：`SemanticVersioning.Range`，支持 `~`、`^`、`>=` 等 npm/semver 语法；4.1/5.0 源码均使用 `SemanticVersioning` 库校验。
- **无依赖时**：保持 `null` 或 `new()` 空字典；不要声明可选/软依赖。
- **与客户端 `[BepInDependency]` 的对应关系**：
  - 服务端 `ModDependencies` 等价于客户端的**硬依赖**（默认 `BepInDependency` 无 `SoftDependency` 标志）；
  - 客户端软依赖示例：`[BepInDependency("com.tyfon.uifixes", BepInDependency.DependencyFlags.SoftDependency)]`（`ReceiveAllChats.Client/Plugin.cs:12`）；
  - 服务端目前**没有软依赖机制**，需要按条件加载的逻辑应在 `IOnLoad` 中自行判断依赖存在性。

### EV-GAP-CFG — config 文件标准路径

**机制端结论**：服务端 mod 的配置文件放在**自己的 mod 目录内**，通过 `ModHelper.GetAbsolutePathToModFolder(Assembly)` 取得根路径后拼接相对路径读取；推荐通过 `IOnDIConstruct` 注册为 DI 单例，而不是标 `[Injectable]`。

- `Libraries/SPTarkov.Server.Core/Helpers/Server/ModHelper.cs:10-19`：

  ```csharp
  public string GetAbsolutePathToModFolder(Assembly modAssembly) => Path.GetDirectoryName(modAssembly.Location);
  public string GetAbsolutePathToModFolder() => GetAbsolutePathToModFolder(Assembly.GetCallingAssembly());
  ```

  即返回 `user/mods/<ModName>/`（DLL 所在目录）。
- `knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md:65-76` 给出推荐模式：

  ```csharp
  public class MyModConfigRegistration : IOnDIConstruct
  {
      public static async Task OnDIConstructAsync(IServiceCollection serviceCollection)
      {
          MyModConfig config = await LoadConfigFromDiskAsync();
          serviceCollection.AddSingleton(config);
      }
  }
  ```

  并明确警告：config 类上**不要**加 `[Injectable]`。
- `knowledge/spt-kb/curated/api-notes-4.1/config-system.md` 再次确认：4.1 已移除 `ConfigServer.GetConfig<T>()`，mod 私有配置走 `IOnDIConstruct` + `AddSingleton`。
- `templates/server-mod/` **未包含**任何配置加载示例，`templates/client-mod/src/Configuration.cs` 仅展示 BepInEx `ConfigFile.Bind`。

**实践端结论**：配置位置分散，但 `config/` 子目录 + `config.jsonc` 是最常见模式；根目录 `config.json` 次之。

配置文件位置分布（近期 297 个目录中，过滤出 101 个真正的 mod config 文件）：

| 目录 | 出现次数 | 示例 |
|------|----------|------|
| `config/` | 20 | `Mission-Control_2653_source/config/config.jsonc` |
| `(root)` | 15 | `Cobra's-Durability-Tweaks_3019_source/config.json` |
| `Server/` | 4 | `No-Gear_2987_source/Server/config.json` |
| `Shared/Config/` | 3 | `Late-to-the-Party_814_source/Shared/Config/config.json` |
| `TheBlacklist/Config/` | 2 | `The-Blacklist-flea-market-enhancements_755_source/TheBlacklist/Config/advancedConfig.jsonc` |
| `Server/Resources/` | 2 | `RUAF-Come-Home!_2427_source/Server/Resources/config.jsonc` |
| 其他（各 1） | — | `data/config.json`、`src/config.jsonc`、`Resources/config.config.default.json` 等 |

加载 API 分布（近期 297 个目录中）：

- 91 个文件使用 `GetAbsolutePathToModFolder(...)`；
- 14 个文件使用 `IOnDIConstruct` 注册配置；
- 102 处出现 `JsonSerializer.Deserialize<...Config>` 或 `modHelper.GetJsonDataFromModFile<T>` 读取配置。

典型路径拼接：

- `Path.Combine(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "config.json")`（`Ironman_2965_source/Server/Config/ConfigManager.cs:33`）
- `Path.Combine(modFolder, "config", "config.jsonc")`（`Caliber-Split-Storage-Solutions_2990_source/Loaders/ConfigRegistration.cs:31-32`）
- `modHelper.GetJsonDataFromModFile<FairEquipmentRestorationConfig>("Config", "config.jsonc")`（`Fair-Equipment-Restoration_2575_source/FairEquipmentRestoration/Config/FairEquipmentRestorationConfigRegistration.cs:25`）

**推荐标准**：

```
user/mods/<ModGuid>/
├── config/
│   ├── config.jsonc      # 玩家可改的运行时配置
│   └── defaultConfig.jsonc # 首次安装/配置损坏时的默认副本
└── <ModName>.dll
```

```csharp
public class MyModConfigRegistration : IOnDIConstruct
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static async Task OnDIConstructAsync(IServiceCollection services, CancellationToken ct)
    {
        string modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string configPath = Path.Combine(modFolder, "config", "config.jsonc");
        string defaultPath = Path.Combine(modFolder, "config", "defaultConfig.jsonc");

        if (!File.Exists(configPath) && File.Exists(defaultPath))
            File.Copy(defaultPath, configPath);

        await using var stream = File.OpenRead(configPath);
        MyModConfig config = await JsonSerializer.DeserializeAsync<MyModConfig>(stream, JsonOptions, ct)
            ?? new MyModConfig();

        services.AddSingleton(config);
    }
}
```

- **路径**：`config/config.jsonc`（相对 mod 根目录）。
- **扩展名**：优先 `.jsonc`，允许带注释；`.json` 也可接受。
- **加载方式**：`IOnDIConstruct` + `AddSingleton`；不要给配置类加 `[Injectable]`。
- **默认配置**：同时提交 `defaultConfig.jsonc`，安装脚本或首次加载时复制为 `config.jsonc`，避免直接覆盖玩家改动。
- **与 MO2 的交互**：配置放在 mod 目录内，天然成为 MO2 mod 内容的一部分；运行时写回应通过 VFS 映射到 overwrite，符合「不要直接写入真实 SPT 目录」的硬规则。
- **客户端配置**：继续使用 BepInEx `ConfigFile.Bind`，最终写入 `BepInEx/config/<GUID>.cfg`；由 `templates/client-mod/src/Configuration.cs` 示范。

### EV-GAP-PAIRED — paired mod 仓库组织

**实践端结论**：所有 sampled hybrid mod 均为**单仓库**；目录命名高度不统一，但功能分区（Client/Server/Shared/Fika）是共识；发布形态多为**单个 zip 同时包含 `BepInEx/plugins/<Mod>` 与 `SPT_Runtime/user/mods/<Mod>`**。

- 近期 297 个目录中，hybrid（同时引用 `SPTarkov.Server` 与 `BepInEx`）共 **58** 个。
- 目录布局抽样（12 个）：
  - `Tushonka-Territories_2942_source`：`Client/` + `Server/` + `Fika/`，顶层 `TushonkaTerritories.sln`。
  - `VAI-Non-Realistic-Tapkov-Project_875_source`：`Client/` + `Server/VAI-NRTP/`。
  - `Mission-Control_2653_source`：`csharp/MissionControl/`（server）+ `csharp/MissionControl.Client/`。
  - `ReceiveAllChats_2785_source`：`ReceiveAllChats.Client/` + `ReceiveAllChats.Server/` + `ReceiveAllChats.Shared/`。
  - `SPT-Casino_2994_source`：`src/<Game>.Client/` + `src/<Game>.Server/` + `src/<Game>.Game/` + `tests/` + `tools/`，顶层 `.slnx`。
  - `WTT-Content-Backport_2512_source`：`WTT-ContentBackport/`（server）+ `WTT-ContentBackportClient/` + `WTT-ContentBackportPatcher/`。
  - `WTT-CommonLib_2310_source`：`WTT-ClientCommonLib/` + `WTT-ClientCommonLibFika/` + `WTT-ServerCommonLib/`，顶层 `WTT-CommonLib.sln`。
  - `SAIN-Solarint's-AI-Combat-System-Replacement_791_source`：`SAIN/`（client）+ `SAINServerMod/` + `SAIN.Preset.Shared/` + `SAIN.ServerInterop/`。
- **单仓库**：抽样 hybrid mod 的 `MANIFEST` URL 全部为单一 GitHub repo，未发现 client/server 分仓库的案例。
- **版本联动**：
  - `Tushonka-Territories_2942_source/Client/Client.csproj:10` 与 `Server/Server.csproj:11` 均为 `<Version>1.3.4</Version>`；
  - `SPT-Casino_2994_source/scripts/casino/pack.ps1:38` 使用单一 `$version = '1.2.61'` 同时打包客户端插件与所有服务端 assembly。
- **发布打包**：
  - `SPT-Casino_2994_source/releases/casino/SPT_CasinoV1.2.61.zip` 顶层同时包含 `BepInEx/`（67 个条目）和 `SPT_Runtime/`（22 个条目），即一个 zip 覆盖两端；
  - `SPT-Casino_2994_source/scripts/casino/pack.ps1` 详细说明：
    - 客户端插件编译到 `BepInEx/plugins/Casino/`；
    - 服务端所有 assembly（含多个 `.Server`/`.Game`）合并到 `SPT_Runtime/user/mods/Casino/`；
    - 原因：`ModLoader.LoadMods` 按**目录**加载，一个目录内可含多个 assembly，但**只能有一个 `IModMetadata`**（`SingleOrDefault` 会抛 "Duplicate mod metadata found"）。
- `external/spt-archive/wiki/Mod_Types.md:40-41` 仅说明 "Some mods include both a server and a client component"，未规定目录结构。

**推荐标准**：

```
my-paired-mod/
├── MyPairedMod.sln
├── Client/
│   ├── MyPairedMod.Client.csproj   # netstandard2.1, BepInEx + Harmony
│   └── src/Plugin.cs
├── Server/
│   ├── MyPairedMod.Server.csproj   # net10.0, SPTarkov.Server.*
│   └── src/ModMetadata.cs
├── Shared/                         # 可选：两端共享的纯数据/常量
│   └── MyPairedMod.Shared.csproj   # netstandard2.0 或 netstandard2.1
├── Fika/                           # 可选：Fika 兼容层
└── README.md
```

```xml
<!-- MyPairedMod.sln 应包含 Client、Server、Shared（及可选 Fika）项目 -->
```

- **仓库**：单仓库；除非客户端与服务端由不同团队维护且生命周期完全独立，否则不建议拆仓库。
- **目录命名**：优先顶层 `Client/`、`Server/`、`Shared/`、`Fika/`；若项目较多（如 SPT-Casino），可改为 `src/<Name>.Client/`、`src/<Name>.Server/`，但仍建议按功能后缀命名。
- **版本号**：客户端 csproj、服务端 csproj、共享项目应使用同一版本号（可通过 MSBuild 属性集中定义），避免玩家看到两端版本不一致。
- **解决方案**：一个 `.sln`/`.slnx` 包含两半，方便一次性构建与 CI。
- **发布打包**：
  - 单个 zip，根目录为 `BepInEx/plugins/<ModName>/` 与 `SPT_Runtime/user/mods/<ModName>/`；
  - 服务端目录内可含多个 assembly（DLL），但**只能有一个 `IModMetadata` 实现**；
  - 若原先是多个独立 mod 合并而来，需像 SPT-Casino 一样重命名冲突的运行时文件（如 `config.json`、`escrow.json`），并在安装脚本中处理旧目录迁移。
- **与 SPT 加载机制的交互**：服务端 `ModLoader` 按 `user/mods/` 下的**目录**加载，一个目录即一个 mod；因此 paired mod 的服务端半必须共享同一个 mod 目录，不能拆成 `user/mods/MyMod-Server/` 和 `user/mods/MyMod-Client/`（后者是客户端目录）。

### EV-GAP-NOEV — 未找到证据的声明

- 未在 392 个源码目录或官方示例中找到任何**非空** `ModDependencies` 的真实样例；推荐写法是根据接口类型与校验器逻辑推断得出。
- wiki/curated 文档中**没有**针对 paired mod 仓库目录结构的明确规范；推荐布局是根据语料中 58 个 hybrid mod 的共同做法归纳。

## 3. 机制证据坐标（EV-MECH-COORD）

| 主题 | 坐标（文件:行） | 版本 | 说明 |
|------|-----------------|------|------|
| ModDependencies 接口定义 | `Libraries/SPTarkov.Server.Core/Models/Spt/Mod/IModMetadata.cs:91` | 4.1/5.0 | key=依赖 ModGuid，value=版本范围 `Dictionary<string, Range>?` |
| ModDependencies 校验 | `SPTarkov.Server/Modding/ModValidator.cs:243-287` | 4.1/5.0 | 缺失/版本不满足即整批拒载；自依赖仅 Warning |
| 依赖错误本地化 | `Libraries/SPTarkov.Server.Assets/SPT_Data/database/locales/server/en.json:227/241` | 4.1/5.0 | `modloader-missing_dependency` / `modloader-outdated_dependency` 文本 |
| mod 目录路径 API | `Libraries/SPTarkov.Server.Core/Helpers/Server/ModHelper.cs:10-19` | 4.1/5.0 | `GetAbsolutePathToModFolder(Assembly)` 返回 `user/mods/<ModName>/` |
| 配置注册接口定义 | `Libraries/SPTarkov.Server.Core/DI/IOnDIConstruct.cs` | 4.1/5.0 | `IOnDIConstruct` 接口（`OnDIConstructAsync` 静态抽象）；5.0 对应 `Libraries/SPTushonka.Server.Core/DI/IOnDIConstruct.cs` |
| 配置注册推荐模式 | `knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md:65-76` | 4.1 | `IOnDIConstruct` + `AddSingleton`；警告 config 类不要加 `[Injectable]` |
| 4.1 配置系统说明 | `knowledge/spt-kb/curated/api-notes-4.1/config-system.md` | 4.1 | 4.1 已移除 `ConfigServer.GetConfig<T>()` |
| paired mod 官方描述 | `external/spt-archive/wiki/Mod_Types.md:40-41` | wiki | 仅说明可同时含 server 与 client 组件，未规定目录结构 |
| paired 打包脚本 | `SPT-Casino_2994_source/scripts/casino/pack.ps1:38` | 语料 | 单一 `$version = '1.2.61'` 同时打包两端 |
| paired 发布 zip | `SPT-Casino_2994_source/releases/casino/SPT_CasinoV1.2.61.zip` | 语料 | 顶层含 `BepInEx/`（67 条目）与 `SPT_Runtime/`（22 条目） |
| 版本联动（客户端） | `Tushonka-Territories_2942_source/Client/Client.csproj:10` | 语料 | `<Version>1.3.4</Version>` |
| 版本联动（服务端） | `Tushonka-Territories_2942_source/Server/Server.csproj:11` | 语料 | `<Version>1.3.4</Version>` |
| 配置路径拼接 | `Ironman_2965_source/Server/Config/ConfigManager.cs:33` | 语料 | `Path.Combine(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "config.json")` |
| 配置路径拼接 | `Caliber-Split-Storage-Solutions_2990_source/Loaders/ConfigRegistration.cs:31-32` | 语料 | `Path.Combine(modFolder, "config", "config.jsonc")` |
| 配置路径拼接 | `Fair-Equipment-Restoration_2575_source/FairEquipmentRestoration/Config/FairEquipmentRestorationConfigRegistration.cs:25` | 语料 | `modHelper.GetJsonDataFromModFile<FairEquipmentRestorationConfig>("Config", "config.jsonc")` |
| 客户端软依赖示例 | `ReceiveAllChats_2785_source/ReceiveAllChats.Client/Plugin.cs:12` | 语料 | `[BepInDependency("com.tyfon.uifixes", BepInDependency.DependencyFlags.SoftDependency)]` |
| 官方示例依赖置空 | `external/spt-archive/server-mod-examples/1Logging/Logging.cs:60` | 4.1/5.0 | `ModDependencies` 置 `null` |
| 官方示例依赖置空 | `external/spt-archive/server-mod-examples/13AddTraderWithAssortJson/AddTraderWithAssortJson.cs:25` | 4.1/5.0 | `ModDependencies` 置 `null` |
| 源码测试 mod 依赖置空 | `Testing/TestMod/TestMod.cs:21` | 4.1/5.0 | `ModDependencies` 置 `null` |
| 源码测试 mod 依赖置空 | `Testing/TestMod2/TestMod2.cs:22` | 4.1/5.0 | `ModDependencies` 置 `null` |

## 4. 无语料先例清单（EV-NOCORPUS）

1. **ModDependencies 真实声明** — 适用标注：**机制推断，无语料先例**。392 个源码目录与官方示例中均无非空 `ModDependencies`；推荐写法（`new()` + `Add("guid", range)`）由 `IModMetadata.cs:91` 接口类型与 `ModValidator.cs:243-287` 校验逻辑推断。
2. **paired mod 仓库结构的文档级规范** — 适用标注：**机制推断，无语料先例**。wiki/curated 文档无明确规范；推荐布局由语料中 58 个 hybrid mod 的共同做法归纳。

### 规则级登记（ticket 02–06 撰写期，2026-09-14）

以下规则在 Evidence 栏标注「机制推断，无语料先例」（级别均为 SHOULD；`STD-VERIFY-009` 为 MAY）：

| 文件 | Rule IDs | 无语料原因（机制来源摘要） |
|------|----------|--------------------------|
| `04-server.md` | STD-SRV-002 / -006 / -007 | `TypePriority` 写法、Router action 签名、Router→Callbacks 分层：语料未统计（机制：modding-guide/02、api-notes-4.1/5.0 http-routing） |
| `05-client.md` | STD-CLI-004 / -007 | Harmony 目标类型名、Awake/OnDestroy 生命周期：无语料计数（机制：modding-guide/03、模板 Plugin.cs） |
| `06-config.md` | STD-CFG-004 | config 类禁 `[Injectable]`：无直接语料计数（机制：modding-guide/02 警告） |
| `07-logging.md` | STD-LOG-004 / -005 | 异常记录降级、取消传播：语料未统计（机制：ISptLogger 签名、modding-guide/02） |
| `08-dependencies.md` | STD-DEP-003 | 可选依赖 `IOnLoad` 自判：无自判模式语料（机制：ModValidator 仅硬依赖） |
| `09-packaging.md` | STD-PKG-002 | MO2 overlay / meta.ini 约定：无约定语料（机制：tools/mo2-mcp 接口） |
| `10-verification.md` | STD-VERIFY-002 – -009 | 工具链/技能文档单源（机制：skills/testing-spt-modpack、tools/spt-mcp、tools/tarkov-runtime-mcp） |
| `11-version-differences.md` | STD-VER-004 | 5.0 无 mod 语料（机制：5xx-source-verification「待专项评估」） |
| `13-perf-security.md` | STD-PERF-007 / -008 | 路径遍历、fail-closed：单点审查发现（机制：415-source-review-report） |

注：`STD-DEP-001` 由上方第 1 条覆盖。

## 5. 复核触发（EV-REVIEW）

- **语料集更新**（新增 MANIFEST 或新增 `_source` 目录）→ 复核 `EV-CORPUS-*`（类型/语言/结构/元数据/机制/配置/异常/TOP5）。
- **4.1/5.0 源码更新** → 复核 `EV-GAP-*` 与 `EV-MECH-COORD`（接口定义、校验逻辑、路径 API、本地化文本）。
- **KB 新增文档**（modding-guide / api-notes / operations / migration / templates / wiki-tushonka / docs/agents）→ 复核 `EV-CORPUS-MATERIALS`。
