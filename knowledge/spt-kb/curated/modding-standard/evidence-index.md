---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 证据索引（Evidence Index）

> 用途：Modding Standard 全部规则的统一证据来源。规则文件以 `EV-*` 锚点引用本文件条目。
> 固化自：`.scratch/modding-standard/corpus-survey.md`（2026-09-14）、`.scratch/modding-standard/gap-investigation.md`（2026-09-14）。
> 语料基线刷新（2026-10-18）：`EV-CORPUS-*` 计数已按 **631 个 `_source` 目录 / 34,050 个文件**重算；种子产物见 `.artifacts/forge-refresh-2026-10-18/`（corpus-classification / evidence-refresh-data / metadata-scan2 / metadata-scan）。2026-09 的 297 / 392 计数保留为**历史基线**并在各节标注。
> 维护约定：语料或源码更新后，按文末「复核触发」复核受影响条目。

## 0. 证据分级约定

- **机制证据（源码/接口）**：直接来自 SPT 4.1/5.0 源码、官方示例与模板的接口定义和校验逻辑，优先级最高。
- **语料证据（统计）**：来自 **631 个 `_source` 目录 / 34,050 个文件 / 13,795 个 `.cs` 文件**（2026-10-18 全量刷新）的统计，优先级次之。历史基线：2026-09 的 297 个近期目录 / 392 个 `_source` 目录 / 8066 个 `.cs` 文件。
- **经验证据**：从既有 KB 文档与实测归纳的惯例，优先级最低。
- **MUST 需机制 + 语料双源**：任何 MUST 级规则必须同时具备机制证据与语料证据（语料样本 ≥10 或全量检查）；仅有语料归纳的结论不得升级为 MUST。
- **规则引用格式**：规则条目以 `（EV-XXX）` 形式引用本文件锚点，例如 `（EV-GAP-DEP）`。

## 1. 语料证据（EV-CORPUS-*）

### EV-CORPUS-SOURCE — 调查范围

- 全量源码目录：**631 个** `_source` 目录（`knowledge/spt-kb/archive/forge/mods/` 下），共 **34,050 个文件**；统计日期 **2026-10-18**。
- 历史基线（2026-09-14）：近期目录 **297 个**（`MANIFEST-sp-mod-2026-09.md` 所列）、全量 `_source` 目录 **392 个**、**8066 个** `.cs` 文件。2026-10 起本节及各 `EV-CORPUS-*` 计数以 631 全量为准，旧值仅作方法溯源。
- 机制调查源码：`E:\云文件\GitHub\SamMeow_SPT410_source_code`、`E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code`。
- 官方示例：`external/spt-archive/server-mod-examples`、`mod-examples`、`modules`、`wiki`。
- 项目模板：`templates/server-mod/`、`templates/client-mod/`。
- 报告生成时间：2026-10-18（本轮刷新）；2026-09-14（前序基线）。
- 证据产物（2026-10-18 刷新）：
  - `.artifacts/forge-refresh-2026-10-18/corpus-classification.json` — 类型 / 语言 / 顶层目录 / csproj 位置 / 根文件 / 元数据存在性 / 配置 / 异常 的仓库级统计。
  - `.artifacts/forge-refresh-2026-10-18/evidence-refresh-data.json` — csproj / 机制 / 配置 / Top5 / changelog 的聚合计数。
  - `.artifacts/forge-refresh-2026-10-18/metadata-scan2.json` — ModGuid / SptVersion / ModDependencies / monorepo / package.json 的精确统计与样例。
  - `.artifacts/forge-refresh-2026-10-18/metadata-scan.json` — 依赖 / 迁移 / 诊断 / Fika / 补丁 / 阻塞调用的行级样例。
- 历史报告路径：`E:\云文件\GitHub\SamMeow-modding-superpowers\.scratch\modding-standard\corpus-survey.md`、`E:\云文件\GitHub\SamMeow-modding-superpowers\.scratch\modding-standard\gap-investigation.md`（2026-09-14）。
- 统计方法：所有计数均通过脚本读取语料目录完成（Node `.mjs` 于 `.artifacts/forge-refresh-2026-10-18/`，PowerShell 用于历史小节），判定命令在各小节给出。

### EV-CORPUS-TYPE — 类型分布

判定方法：对每个源码目录递归查找 `.csproj`，读取文本后检查是否出现 `SPTarkov.Server`（服务端引用）与 `BepInEx`（客户端引用）；根目录存在 `package.json` 视为 JS/TS 服务端 mod；两者兼有记为 hybrid；两者皆无记为 other。

| 类型 | 计数（2026-10-18，631 全量） | 说明 |
|------|------|------|
| client-only | 253 | 仅引用 BepInEx 的客户端插件 |
| server-only (C#) | 139 | 引用 SPTarkov.Server.* 且不引用 BepInEx |
| server-only (JS/TS) | 17 | 仅有 `package.json`，无 C# 工程 |
| hybrid | 132 | 同时引用 SPTarkov.Server.* 与 BepInEx（常见 paired mod） |
| other | 90 | 无代码工程，多为资源/Reshade/数据库覆盖包 |
| **合计** | **631** | |

> 历史基线（2026-09-14，297 个近期目录）：client-only 148 / server-only (C#) 43 / server-only (JS/TS) 11 / hybrid 58 / other 37，五类合计 297。原报告记「未能映射 1」例（`[SAIN]-Twitch-Players_1895_source`）系 PowerShell 通配符对 `[ ]` 的处理所致，复核确认该目录实际存在；旧口径已并入上表历史值。

命令概要（2026-10-18 全量口径，扫描全部 `_source` 目录）：

```javascript
const repos = fs.readdirSync(ARCHIVE, { withFileTypes: true })
  .filter((d) => d.isDirectory() && /_source(?:-|$)/.test(d.name))
  .map((d) => d.name).sort();
// 每个 repo：递归收集 .csproj；/SPTarkov\.Server|SPTushonka\.Server/ 判服务端，/BepInEx/ 判客户端；
// 根目录 package.json 判 JS/TS；两者兼有 = hybrid，皆无 = other。
```

### EV-CORPUS-LANG — 语言分布

| 语言 | 计数（2026-10-18，631 全量） | 判定 |
|------|------|------|
| C# | 547 | 存在 `.csproj` |
| TypeScript/JavaScript | 16 | 仅有 `package.json` 且无 csproj |
| 两者兼有 | 4 | 同时存在 csproj 与根目录 package.json |
| 其他 | 64 | 既无 csproj 也无 package.json |
| **合计** | **631** | |

> 历史基线（2026-09-14，297 个近期目录）：C# 278 / TS-JS 10 / 两者兼有 1 / 其他 8。

命令概要：与类型分布（EV-CORPUS-TYPE）相同，额外统计 `package.json` 与 `.csproj` 的存在性。

### EV-CORPUS-STRUCT — 结构惯例

631 个源码目录顶层子目录出现频率（按出现次数降序，**已排除 `.git`**；全量含 `.git` 578）：

| 目录 | 出现次数 | 备注 |
|------|----------|------|
| obj | 324 | 构建产物（含编译中间输出），非源码 |
| bin | 314 | 构建产物（编译输出），非源码 |
| Patches | 112 | 客户端 Harmony Patch 集中地 |
| src | 82 | 通用源码目录 |
| Properties | 56 | C# 工程默认生成 |
| .github | 53 | GitHub Actions / 工作流 |
| Client | 50 | paired mod 的客户端工程 |
| Server | 48 | paired mod 的服务端工程 |
| docs | 39 | 文档目录 |
| config | 36 | 配置文件目录 |
| db | 34 | 数据库 JSON 覆盖 |
| scripts | 29 | 脚本/工具 |
| tools | 29 | 工具目录 |
| tests | 28 | 测试目录 |
| Resources | 27 | 资源文件 |
| Helpers | 24 | 辅助类目录 |
| assets | 20 | 美术/音频资源 |
| bundles | 19 | bundle 资源 |
| .idea | 18 | JetBrains IDE 配置 |

> 口径说明：上表为**目录存在性**统计，`obj` / `bin` 计入本地构建输出目录（`corpus-classification.mjs` 在遍历文件时跳过 `bin`/`obj`，但顶层目录名仍被计入）。**真正的「构建产物提交」违规数**按 git 跟踪判定，见 `EV-CORPUS-TOP5` 第 1 条（仅 7 / 9 个目录被跟踪）。

`csproj` 位置分布（631 全量，共 977 个 csproj）：

| 位置 | 计数 |
|------|------|
| 源码根目录 | 307 |
| src/ | 74 |
| Client/ | 56 |
| Server/ | 46 |
| tests/ | 32 |
| 其他（`*/子项目/`、`properties/`、`tools/`、`Plugin/`、`Fika/` 等） | 462 |

工程文件与仓库文件惯例（631 个仓库根目录）：

| 文件/目录 | 出现次数（631） | 比例 |
|-----------|----------------------|------|
| README*（大小写不敏感 glob） | 420 | ~66.6 % |
| LICENSE*（大小写不敏感 glob） | 455 | ~72.1 % |
| `.gitignore` | 503 | ~79.7 % |
| 构建脚本（`build*` / `Makefile` / `*.ps1` / `*.sh` / `*.bat`） | 74 | ~11.7 % |
| CI（`.github/workflows` / `.gitlab-ci.yml`） | 49 | ~7.8 % |

> 注：TOP5 中的「缺失 README / LICENSE」按精确文件名 `README.md`/`readme.md` 与 `LICENSE`/`LICENSE.md`/`LICENSE.txt` 判定，数值略低于本表的 glob 计数（418 / 455）。

命令概要：

```javascript
// 顶层子目录（含构建/IDE 目录，仅 .git 单独说明）
const top = fs.readdirSync(root, { withFileTypes: true }).filter((d) => d.isDirectory()).map((d) => d.name);
// 根文件
const rootFiles = fs.readdirSync(root, { withFileTypes: true }).filter((d) => d.isFile()).map((d) => d.name);
```

### EV-CORPUS-CSPROJ — 工程文件普查（csproj / .gitignore / ModMetadata.cs）

> 普查（2026-10-18 刷新）。对象：全部 **631 个** `_source` 源码目录，共 **977 个** csproj。供 STRUCT / META / BUILD 维度规则引用。
> 历史基线（2026-09-14）：297 个近期源码目录 / 509 个 csproj（旧值在下方命令块标注）。

| 指标 | 结果（2026-10-18） |
|------|------|
| `.gitignore` 存在 | 503 / 631 |
| 文件名恰为 `ModMetadata.cs` | 118 / 631（其余散落 `Metadata.cs`、`*Metadata.cs`、嵌套路径等） |
| 服务端 csproj（引用 `SPTarkov.Server`） | 322 |
| └ TargetFramework | `net10.0` 259 / `net9.0` 57 / `net9.0-windows` 1 / 无 5 |
| └ 引用方式 | `<PackageReference>` 149 / 本地 DLL 173 |
| └ `AppendTargetFrameworkToOutputPath=false` | 204；`<Private>false</Private>` 48；`<HintPath>` 77 |
| 客户端 csproj（引用 BepInEx、不含服务端） | 470 |
| └ TargetFramework | `netstandard2.1` 266 / `net472` 119 / `net471` 16 / `net48` 4 / `netstandard21` 3 / `netstandard2.0` 1 / `net481` 1 / `net8.0` 1 / `net46` 1 / 无 58 |
| └ `<HintPath>` | 395；`AppendTargetFrameworkToOutputPath=false` 80；`<Private>false</Private>` 156 |
| `<Version>` 格式 | 三段式 412 / 四段式 5 / 其他 140（MSBuild 属性插值等） |

命令（完整脚本，2026-09 方法，扫描 MANIFEST 近期 297 个目录，可复现历史基线；下方附 2026-10 全量产物）：

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

结果（2026-10-18 全量刷新，631 个 `_source` 目录；来源 `.artifacts/forge-refresh-2026-10-18/evidence-refresh-data.json`）：

```json
{
  "repos": 631, "csprojTotal": 977, "gitignore": 503, "modMetadataFilename": 118,
  "serverCsproj": 322,
  "serverTargetFramework": { "net10.0": 259, "net9.0": 57, "net9.0-windows": 1, "(none)": 5 },
  "serverPkgRef": 149, "serverLocalRef": 173,
  "serverAppendFalse": 204, "serverPrivateFalse": 48, "serverHint": 77,
  "clientCsproj": 470,
  "clientTargetFramework": { "netstandard2.1": 266, "net472": 119, "net471": 16, "net48": 4,
    "netstandard21": 3, "netstandard2.0": 1, "net481": 1, "net8.0": 1, "net46": 1, "(none)": 58 },
  "clientHint": 395, "clientAppendFalse": 80, "clientPrivateFalse": 156,
  "appendTargetFrameworkFalseTotal": 297,
  "version3": 412, "version4": 5, "versionOther": 140
}
```

> 2026-10 全量扫描脚本：`.artifacts/forge-refresh-2026-10-18/evidence-refresh-data.mjs`（遍历全部 `_source` 目录，跳过 `node_modules`/`.git`/`bin`/`obj` 等）。
> 历史基线（2026-09-14，297 个近期目录 / 509 csproj）：`gitignore` 269、`modMetadataFile` 63、`serverCsproj` 114（`net10.0` 95 / `net9.0` 17 / `net9.0-windows` 1；`serverPkgRef` 83 / `serverLocalRef` 31）、`clientCsproj` 264（`netstandard2.1` 135 / `net472` 79 / `net471` 7 / `netstandard21` 3 / `net48` 2 / `net46` 1 / `net10.0` 1）、`version3Seg` 214 / `version4Seg` 5 / `versionOther` 72。
> 注：2026-09 的 `versionOther` 72 例为 MSBuild 属性插值（`$(AssemblyVersion)` 49、`$(ModVersion)` 14、`$(Version)` 9），非违规；2026-10 的 140 例同属该口径。`net9.0`（57 例）可能含向 5.x 迁移的过渡期样本。

### EV-CORPUS-META — 元数据惯例

**存在性（2026-10-18，631 个 `_source` 目录，来源 `corpus-classification.json`）**：296 个仓库出现 `ModGuid`；294 个出现 `SptVersion`；292 个出现 `IModMetadata` / `AbstractModMetadata`。

**ModGuid（来源 `metadata-scan2.json`）**：共 **322 条**赋值声明，**277 个唯一值**，其中 **266 个反向域名**（占唯一值 **96 %**，`com.author.name` 形态）；反向域名之外的 11 个唯一值包括 `Ombarella`、`me.sol.sain`、`local.heborian`、`UNIQUE PACKAGE NAME HERE`、`Light.LightsAutomaticIdentiier` 等。按仓库计，**14 / 631** 个仓库至少含一个非反向域名 GUID（`evidence-refresh-data.json`），仍属少数。

**SptVersion（来源 `metadata-scan2.json`）**：共 **326 条**声明，其中 **297 条为 tilde 范围**（**91 %**，如 `~4.1.0` 137、`~4.0.0` 38、`~4.1.3` 30、`~4.1.6` 25、`~4.1` 10，含插值 `$"~{SAINVersionInfo.SptVersion}"` 形态）；**15 条精确版本**（如 `4.0.13` 4、`3.11.3` 4、`4.1.6` 3）；**13 条范围**（如 `>=4.1.3 <4.2.0` 3、`^4.0.13` 2）；**1 条其他**（`/narconet/sptversion`）。
> 修正（2026-10-18）：2026-09 的「样例全部为 tilde 范围，未见到 `^4.1` 或精确版本」仅基于 15 例采样，**与全量语料不符**——精确实例与范围/`^` 写法确有存在，只是占比小（15 + 13 / 326）。tilde 仍是生态主导写法。

**ModDependencies（来源 `metadata-scan2.json`）**：共 **53 个仓库 / 82 对**非空依赖。最常见依赖：`com.wtt.commonlib` 49、`com.wtt.contentbackport` 11、`com.morebotsapi.tacticaltoaster` 4、`com.epicrangetime.aio` 3、`me.sol.sain` 3。典型写法：`["com.wtt.commonlib"] = new Range("~3.0.6")` / `{ "com.wtt.commonlib", new Range("~3.0.0") }`。
> 修正（2026-10-18）：2026-09 的「C# 服务端元数据中几乎全部为 `null` / `[]` / `new()`，无语料先例」基于 297 近期目录，**与 631 全量不符**——真实非空依赖声明有 53 仓库 / 82 对。客户端依赖仍主要通过 `[BepInDependency]` 声明（`[BepInDependency]` 共 291 处，见 `EV-CORPUS-MECH`）。

**服务端元数据文件位置极不统一**：`ModMetadata.cs`、`Metadata.cs`、`src/ModMetadata.cs`、`Server/Metadata.cs`、`csharp/*/Mod/*Metadata.cs` 等均有出现（文件名恰为 `ModMetadata.cs` 者仅 118 / 631，见 `EV-CORPUS-CSPROJ`）。

2026-09 采样坐标（历史；15 例）：

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

```javascript
// 声明级正则（metadata-scan2.mjs）
text.matchAll(/ModGuid\s*(?:\{[^}]*\})?\s*=\s*"([^"]+)"/g);
text.matchAll(/SptVersion\s*(?:\{[^}]*\})?\s*=\s*(?:new\s*\(\s*)?"([^"]+)"/g);
// 反向域名判定：/^[A-Za-z][\w-]*(\.[\w-]+){2,}$/
```

### EV-CORPUS-MECH — 机制用法频率

在全部 **631 个** `_source` 目录的 **13,795 个 `.cs` 文件**中统计**原始正则命中次数**（2026-10-18；来源 `evidence-refresh-data.json`）：

| 机制/类型 | 命中次数 | 备注 |
|-----------|----------|------|
| `[Injectable]` | 1720 | 服务端 DI 注册 |
| `IOnLoad` | 565 | 服务端生命周期 |
| `ISptLogger` | 1004 | 服务端日志 |
| `StaticRouter` | 143 | 服务端路由 |
| `DynamicRouter` | 17 | 服务端动态路由 |
| `IOnUpdate` | 15 | 服务端更新回调 |
| `BaseUnityPlugin` | 505 | 客户端 BepInEx 入口 |
| `[BepInPlugin]` | 481 | 客户端插件声明 |
| `[BepInDependency]` | 291 | 客户端依赖声明 |
| `Logger.` | 3758 | 客户端 BepInEx `BaseUnityPlugin.Logger` 调用 |
| `ModMetadata` | 542 | 元数据类型引用 |
| `AbstractModMetadata` | 76 | 4.0 遗留抽象类 |

补充计数（文件/仓库级，2026-10-18；供补丁/异步/并发规则引用）：

| 机制/类型 | 计数 | 备注 |
|-----------|------|------|
| `ModulePatch` / `PatchSet` | 277 仓库 | 客户端补丁主导形态 |
| `PatchAll()` | 91 文件 | 受控/全程序集应用 |
| `Thread.Sleep` | 27 文件 / 20 仓库 | 客户端热路径风险 |
| `async void` | 129 文件 | 异常围栏缺口 |
| `SemaphoreSlim` | 60 文件 | 并发限流 |

> 口径：上表为**未去重的正则命中次数**（同一 token 多次出现即多次计数），非文件数或仓库数；跨 631 目录汇总。
> 历史基线（2026-09-14，297 个近期目录 / 8066 个 `.cs`）：`[Injectable]` 1018、`IOnLoad` 279、`ISptLogger` 614、`StaticRouter` 90、`DynamicRouter` 12、`IOnUpdate` 6、`BaseUnityPlugin` 275、`[BepInPlugin]` 251、`[BepInDependency]` 170、`Logger.` 4968、`ModMetadata` 271、`AbstractModMetadata` 24（旧表口径不同，不可直接对比）。

命令概要：

```javascript
// evidence-refresh-data.mjs：对每个仓库的 .cs 文件累计正则命中
mech.injectable += (t.match(/\[Injectable/g) || []).length;
mech.onLoad     += (t.match(/\bIOnLoad\b/g) || []).length;
mech.sptLogger  += (t.match(/\bISptLogger\b/g) || []).length;
```

### EV-CORPUS-CFG — 配置惯例

631 个 `_source` 目录内的**文件级**计数（2026-10-18；来源 `evidence-refresh-data.json`）：

| 配置文件类型 | 文件数 | 说明 |
|--------------|----------|------|
| `config.json`（任意位置，文件名精确匹配） | 133 | 多为 mod 配置或资源元数据 |
| `*.jsonc`（任意位置） | 113 | 服务端默认配置、预设等常见 |
| `.cfg`（源码根目录） | 1 | BepInEx 运行时配置一般不在源码中提交 |
| 含 `ConfigVersion` / `configVersion` 的仓库 | 11 | 配置 schema 版本管理（见 `STD-CFG-007..010` 候选） |

> 历史基线（2026-09-14，297 近期目录）：`config.json` 47、`*.jsonc` 85、根 `.cfg` 0。

常见位置/命名样例：

- `Server/config/config.json`：`ABPS-Acid's-Bot-Placement-System_2097_source`
- `config/config.jsonc`：`Ammo-Stats_167_source`
- `config/defaultConfig.jsonc`：`Ammo-Stats-In-Names_2989_source`
- `Presets/default.jsonc`：`Advanced-Flea-Simulator_2526_source`
- `src/Config/settings.jsonc`：`Classic-Movement_1860-2_source`
- `data/config.json`：`bluehead's-AIO-Trader_374_source`

命令概要：

```javascript
if (base === "config.json") cfg.configJson++;
if (base.endsWith(".jsonc")) cfg.jsonc++;
if (base.endsWith(".cfg") && path.dirname(f) === root) cfg.cfgRoot++;
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

**当前异常计数（2026-10-18，631 个 `_source` 目录；来源 `corpus-classification.json` / `evidence-refresh-data.json`）**：

| 异常 | 仓库数 | 口径 |
|------|--------|------|
| 空 `catch { }` | 96 | 正则命中至少一处空捕获块 |
| git 跟踪的 `bin/` / `obj/` | 7 / 9（任一 = 11，~1.7 %） | `git ls-files bin obj` 判定；注意「目录存在」为 324 / 314，但绝大多数未被跟踪 |
| 硬编码绝对路径 | 19 | 命中盘符路径 / `Path.Combine(@"C:...")` |
| 无根 README | 213（418 个存在） | 精确文件名 `README.md`/`readme.md` |
| 无根 LICENSE | 176（455 个存在） | `LICENSE`/`LICENSE.md`/`LICENSE.txt` |
| 扁平根目录 | 261 | 根目录直接含 `.cs` 且无 `src/`/`Server/`/`Client` 分层 |

> 口径说明：**261 / 631 的「扁平根目录」不等于 261 个违规**。大量仓库是单文件小型客户端 mod，源码直接放根目录是常见且合理的形态；只有当仓库为多组件（client + server + shared 等）或规模较大、难以导航时，才应按 `STD-STRUCT-001` 判为结构问题。`bin` / `obj` 同理：目录存在 ≠ 提交，务必以 git 跟踪为准。

### EV-CORPUS-TOP5 — 语料最大不规范点 Top 5

（2026-10-18，631 个 `_source` 目录）

1. **构建产物 `bin/` / `obj/` 提交到源码仓库**：324 个 `obj` 目录、314 个 `bin` 目录**存在于本地**，但按 git 跟踪判定只有 **7 / 9** 个被跟踪（任一 = 11 个仓库，**~1.7 %**）。旧 6–7 % 的估计基于目录存在性，**不准确**；git 跟踪口径才是可信指标。
2. **缺乏 README / LICENSE**：**213 / 631**（~33.8 %）无根 `README`，**176 / 631**（~27.9 %）无根 `LICENSE`；对玩家与整合包维护者不友好。
3. **`ModMetadata.cs` 文件名与位置不统一**：仅 **118 / 631** 使用精确文件名 `ModMetadata.cs`；其余散布为 `Metadata.cs`、`*Metadata.cs` 或嵌套在 `src/`、`Server/`、`csharp/*/Mod/` 等路径，增加工具自动扫描成本。
4. **GUID 命名**：**14 / 631** 个仓库至少含一个非反向域名 `ModGuid`（如 `Ombarella`、`me.sol.sain`、`Light.LightsAutomaticIdentiier`），仍属少数；反向域名 `com.author.name` 是绝对主流（266 / 277 个唯一值，96 %）。
5. **扁平根目录**：**261 / 631** 个仓库在根目录直接放 `.cs` 且无 `src/`/`Server/`/`Client` 分层，但**不可一概判为违规**——其中大量是单文件小型客户端 mod，根目录直放是合理惯例；仅当仓库为多组件或规模较大、难以导航时才按结构规则处理。

### EV-CORPUS-MO2META — MO2 overlay meta.ini 描述字段冻结事故

> 单例语料（2026-10-16 观测）。与上方统计型语料不同，本条为实测事件证据：一次真实部署流程中描述字段与能力脱节的完整记录，为 `STD-PKG-002`、`STD-PKG-008` 提供语料侧依据。

- **对象**：自研 mod `mods/SamMeow-LLMTraderChat`（paired：客户端插件 + 服务端 mod）。
- **事件**：MO2 overlay 的 `meta.ini` `comments`/`notes` 冻结于首次安装文本（M1：「M1 测试构建 / 仅解锁 Therapist 回显」），而 mod 此后历经 6+ 次能力跃迁（19 个商人、LLM 回复、动态态度、任务知识）。
- **直接原因**：每次重部署都用 `robocopy（保留 meta.ini）`——保护 MO2 记账字段（正确）的同时，从未刷新描述字段。
- **结构性原因**：`meta.ini` 只存在于 MO2 实例，不在 mod 仓库内 → 脱离版本控制、打包与审查。
- **规则原因**：原 `STD-PKG-002` 将 `notes` 定义为「安装记录（来源、版本、安装日期、作者说明）」，在该字面口径下 M1 文本是合规的，因此没有任何机制要求更新。
- **整改方向**：描述字段真源移入仓库（`<mod>/deploy/meta.json`），部署时用 `tools/mo2-meta-sync/sync.mjs` 同步进 overlay `meta.ini`（只改描述字段、保留记账字段）；触发条件为任何影响用户可见能力或版本号的变更。
- **证据坐标**：`mods/SamMeow-LLMTraderChat/docs/acceptance.md:57`（robocopy 保 meta.ini 部署记录）、`.scratch/llm-trader-chat/issues/13-all-traders-onboarding.md:61`（overlay 部署收口记录）。
- **适用规则**：`STD-PKG-002`、`STD-PKG-008`（均为 SHOULD）。

### EV-CORPUS-CHANGELOG — mod 仓库内 CHANGELOG.md 惯例

> 语料普查（2026-10-18 刷新）。对象：`knowledge/spt-kb/archive/forge/mods/` 下全部 `_source` 目录，以及自研 `mods/` 目录。为 `STD-PKG-009` 提供语料侧依据。

- **计数**：**82 个** `_source` 目录随仓库提供 `CHANGELOG.md`，共 **82 个** `CHANGELOG.md` 文件（2026-10-18；来源 `evidence-refresh-data.json`）。历史基线（2026-09）：61 个目录 / 109 个文件（旧值含嵌套多份，文件名大小写不一）。
- **格式观测**：主流为 `## [<version>] - <YYYY-MM-DD>` 分节、**最新在上**，节内按 `### Added` / `### Changed` / `### Fixed` 等分组——如 `Kappa-Tracker_2981_source/CHANGELOG.md`（`## [1.3.0] - 2026-09-11` + `### Added` / `### Changed`）、`CombatLog_3072_source/CHANGELOG.md`、`AutoIFF_2784_source/CHANGELOG.md` 结构相同。部分目录的版本节点不带方括号或日期（如 `## [1.0.0]`）。
- **自研对照**：`mods/SamMeow-LLMTraderChat/CHANGELOG.md`、`mods/PerformanceTweaks/CHANGELOG.md`、`mods/PerformanceTweaks413/CHANGELOG.md`。
- **技能侧佐证**：`skills/writing-spt-mod/SKILL.md`、`skills/writing-spt-modpack-changelog/SKILL.md`、`skills/porting-spt-mod-to-spt5/SKILL.md` 均已把「每 mod 自带 `CHANGELOG.md`」定为强制流程并引用 `STD-PKG-009`。
- **覆盖面说明**：82 个目录提供 changelog，占 631 全量语料约 **13 %**（历史口径为近期 297 目录的 ~20 %），说明该惯例尚未普及；但样本量 82 仍 **≥10**，满足 MUST 语料门槛（README「分级与证据标准」）。
- **适用规则**：`STD-PKG-009`（MUST）。

命令概要：

```javascript
const cl = walk(repo).filter((f) => path.basename(f).toLowerCase() === "changelog.md");
if (cl.length > 0) { changelog.repos++; changelog.files += cl.length; }
```

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

- `docs/agents/domain.md` — 域文档使用约定（GLOSSARY.md、ADR、术语表）。
- `docs/agents/issue-tracker.md` — `.scratch/<feature-slug>/` 目录结构、spec/issues 文件、triage label 约定。
- `docs/agents/triage-labels.md` — 五种 triage role。
- `AGENTS.md` — 引用上述文件，强调单上下文 repo 的 `GLOSSARY.md` + `docs/adr/` 结构；未直接涉及 mod 工程规范。

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

1. ModDependencies 的标准用法 → 已解决，见 `EV-GAP-DEP`（机制 + 53 仓库 / 82 对语料）。
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

**实践端结论**：631 全量语料中**存在真实非空依赖声明**——共 **53 个仓库 / 82 对**（54 个声明文件；来源 `metadata-scan2.json`）。

- 依赖分布（`metadata-scan2.json` byGuid）：`com.wtt.commonlib` 49、`com.wtt.contentbackport` 11、`com.morebotsapi.tacticaltoaster` 4、`com.epicrangetime.aio` 3、`me.sol.sain` 3，其余为 1–2 次的零散依赖。
- **两种等价写法**均出现：
  - 索引器写法：`["com.wtt.commonlib"] = new Range("~3.0.6")`（`.22lr-Ammunition_2516_source/ModMetadata.cs`）；
  - 集合初始化器写法：`{ "com.wtt.commonlib", new Range("~3.0.0") }`（`Content-Backport-Prestiges_2540_source/ContentBackportPrestigesServer/ContentBackportPrestigesModMetadata.cs`、`ECOT-Eukyre's-Consortium-of-Things_2195_source/EukyreECOT/EukyreMod.cs`）。
- 范围写法以 tilde 为主（`~3.0.6` 17 次、`~3.0.0` 12 次），另有 `^`、`>=` 与精确版本（如 `2.0.24`）。
- **共享库依赖是主导场景**：`com.wtt.commonlib` 一类共享库占绝大多数对次，其次为内容回填库（`com.wtt.contentbackport`）。
- 客户端依赖仍主要通过 `[BepInDependency]` 声明（`[BepInDependency]` 共 291 处，见 `EV-CORPUS-MECH`）。
- 官方 `server-mod-examples`（`1Logging/Logging.cs:60`、`13AddTraderWithAssortJson/AddTraderWithAssortJson.cs:25` 等）与源码测试 mod（`Testing/TestMod/TestMod.cs:21`、`Testing/TestMod2/TestMod2.cs:22`）仍为 `null`——其存在不推翻生态中 53 个仓库的真实声明。

> 修正（2026-10-18）：2026-09 的「语料中没有任何真实非空依赖声明」基于 297 近期目录，**与 631 全量不符**——真实非空依赖声明有 53 仓库 / 82 对。

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

- （已解决，2026-10-18）原「未在 392 个源码目录或官方示例中找到任何**非空** `ModDependencies` 真实样例」的声明**不再成立**：631 全量语料中有 53 个仓库 / 82 对，见 `EV-GAP-DEP`。
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

1. **paired mod 仓库结构的文档级规范** — 适用标注：**机制推断，无语料先例**。wiki/curated 文档无明确规范；推荐布局由语料中 58 个 hybrid mod 的共同做法归纳。

### 规则级登记（ticket 02–06 撰写期，2026-09-14）

以下规则在 Evidence 栏标注「机制推断，无语料先例」（级别均为 SHOULD；`STD-VERIFY-009` 为 MAY）：

| 文件 | Rule IDs | 无语料原因（机制来源摘要） |
|------|----------|--------------------------|
| `04-server.md` | STD-SRV-002、STD-SRV-006、STD-SRV-007 | `TypePriority` 写法、Router action 签名、Router→Callbacks 分层：语料未统计（机制：modding-guide/02、api-notes-4.1/5.0 http-routing） |
| `05-client.md` | STD-CLI-004、STD-CLI-007 | Harmony 目标类型名、Awake/OnDestroy 生命周期：无语料计数（机制：modding-guide/03、模板 Plugin.cs） |
| `06-config.md` | STD-CFG-004 | config 类禁 `[Injectable]`：无直接语料计数（机制：modding-guide/02 警告） |
| `07-logging.md` | STD-LOG-004、STD-LOG-005 | 异常记录降级、取消传播：语料未统计（机制：ISptLogger 签名、modding-guide/02） |
| `08-dependencies.md` | STD-DEP-003 | 可选依赖 `IOnLoad` 自判：无自判模式语料（机制：ModValidator 仅硬依赖） |
| `10-verification.md` | STD-VERIFY-002、STD-VERIFY-003、STD-VERIFY-004、STD-VERIFY-005、STD-VERIFY-006、STD-VERIFY-007、STD-VERIFY-008、STD-VERIFY-009 | 工具链/技能文档单源（机制：skills/testing-spt-modpack、tools/spt-mcp、tools/tarkov-runtime-mcp） |
| `11-version-differences.md` | STD-VER-004 | 5.0 无 mod 语料（机制：5xx-source-verification「待专项评估」） |
| `13-perf-security.md` | STD-PERF-007、STD-PERF-008 | 路径遍历、fail-closed：单点审查发现（机制：415-source-review-report） |

## 5. 复核触发（EV-REVIEW）

- **语料集更新**（新增 MANIFEST 或新增 `_source` 目录）→ 复核 `EV-CORPUS-*`（类型/语言/结构/元数据/机制/配置/异常/TOP5）。
- **4.1/5.0 源码更新** → 复核 `EV-GAP-*` 与 `EV-MECH-COORD`（接口定义、校验逻辑、路径 API、本地化文本）。
- **KB 新增文档**（modding-guide / api-notes / operations / migration / templates / wiki-tushonka / docs/agents）→ 复核 `EV-CORPUS-MATERIALS`。
- **mod 仓库 CHANGELOG 惯例变化**（发布模板 / 技能流程调整）→ 复核 `EV-CORPUS-CHANGELOG`。

> 本轮刷新记录（2026-10-18）：语料从 392 扩至 **631 个 `_source` 目录 / 34,050 个文件**，`EV-CORPUS-*` 全部重算；证据产物见 `.artifacts/forge-refresh-2026-10-18/`（corpus-classification / evidence-refresh-data / metadata-scan2 / metadata-scan）。**审阅者需在本次刷新后重新核对各 MUST 规则的语料计数**（尤其 `STD-PKG-009`、`STD-DEP-001` 与新增候选规则所引用的数字），确认仍满足 ≥10 样本门槛。

## 6. 校准记录（EV-CALIBRATION）

ticket 11 试点校准（2026-09-14）：以 3 个自研 mod 的逐规则合规报告为输入，对规则集做以下修订。报告：`.scratch/modding-standard/pilot/warsaw-trader.md`（PASS 31 / FAIL 4 / N-A 49）、`tarkov-active-probe.md`（23 / 6 / 55）、`spt5-no-stamina-drain.md`（19 / 7 / 58）。

**语料基线刷新（2026-10-18）**：语料扩至 **631 个 `_source` 目录 / 34,050 个文件**；`EV-CORPUS-*` 全部按新全量重算，证据产物位于 `.artifacts/forge-refresh-2026-10-18/`（corpus-classification.json / evidence-refresh-data.json / metadata-scan2.json / metadata-scan.json）。本次仅刷新证据计数，**未改动任何规则 ID、Level、Applies 或规则文本**；审阅者应按 `EV-REVIEW` 提示重新核对受影响 MUST 规则的语料计数。

| 修订 | 涉及规则 | 类型 | 改了什么 | 为什么 |
|------|----------|------|----------|--------|
| R1 | STD-BUILD-002/003、STD-CLI-001/006/007 | 与机制矛盾 | 增加 5.0（IL2CPP / BepInEx 6）分支：`net6.0`、`BepInEx/interop/` 引用、`BasePlugin` + `Load()`、`ManualLogSource`、`Dispose()` 撤销 | 原规则文本仅描述 4.1 Mono 形态但标 `both`；试点 5.0 mod 实证不同（`mods/SPT5-NoStaminaDrain/`） |
| R2 | STD-STRUCT-001/002/005/006、STD-PKG-006 | 表述不清 | monorepo / 嵌套 mod 判定口径；bin/obj 以 git 跟踪判定；README / LICENSE 允许仓库根提供；PKG-006 去重指向 STRUCT | 试点为 monorepo 嵌套目录，作用域歧义导致误判 FAIL |
| R3 | STD-STRUCT-001 | 表述不清 | 排除清单明确化（`bin/`、`obj/`、`.vs/`、`.idea/`、`*.user`） | 两个试点因「等」模糊在 `.vs/` / `*.user` 上失分 |
| R4 | STD-CFG-001/002 | 表述不清 | 界定「玩家可改运行时配置」与「随包只读内容数据」边界 | WarsawTrader 的 `data/` 内容数据被误套配置规则 |
| R5 | STD-SRV-003/005/006、STD-LOG-001/005 | 表述不清 | 纯同步 token 判定；`+1` 偏移归 SRV-002；`_` 参数判定；无日志 mod 不强制产生日志 | 试点边界情形判定无据 |
| R6 | STD-PKG-001/002、STD-VERIFY-002..009/006 | 表述不清 | 适用性口径（发布归档 / 批次验证 vs 纯源码仓库）；spt-mcp 工具限制注记 | 试点场景与规则默认场景不匹配 |
| R7 | STD-BUILD-004 | 分级不当 | MUST 收敛为「引用版本不得高于运行时」；PackageReference 降为推荐 | 「优先」与 MUST 混写 |
| R8 | STD-BUILD-006 | 表述不清 | 属性名可版本化自定义（`SPTInstallPath` / `SPT5Path` / `SPT5Runtime` 等），须可覆盖 | 试点用 `SPT5Runtime` 被误判 |

未采纳建议：逐规则 `Domain` 字段（spt5-no-stamina-drain 报告提出）——文件级 domain + `Applies` + 规则文本已可判定，避免 84 条全量格式变更；记录为未来可选项。
