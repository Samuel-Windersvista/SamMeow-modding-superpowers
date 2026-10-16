# Modding Standard 豁免记录

> 依据 `knowledge/spt-kb/curated/modding-standard/README.md` 的 Waiver 条款整理。
> 复跑：`powershell -File scripts/check-mod-standard.ps1 -ModPath mods/SamMeow-LLMTraderChat -TargetSptVersion 4.1.6`
> 结果：`PASS=27  FAIL=3  SKIP=0  WAIVED=0`（3 条 FAIL 均为下列**检查器缺陷误报**）。
> 说明：本文件是**记录式豁免**，未接入检查器 `-WaiverFile` 机制；处置方向为修复仓库检查器（逐条见下）。
>
> **severity 注记**：`STD-BUILD-006` 为 **SHOULD** —— modding-standard 规定 SHOULD 不强制、**无需豁免标记，仅需说明理由**；`STD-BUILD-001` / `STD-META-003` 为 **MUST** —— 二者均为 MUST 类的**误报**（已在本文件说明）。
>
> **门禁注记**：因记录式豁免未接入豁免机制，`check-mod-standard` 当前**仍会 `exit 1`**。若未来自动化以 exit code 为发布门禁，需二选一：① 补**原生 waiver 镜像**（在 mod 根 `MODDING-STD-WAIVER.md` 写 `Waiver: STD-XXX-NNN: <reason>` 行，检查器即把对应 FAIL 降级为 `WAIVED`）；② 推进**检查器修复**（去向见各条「处置」）。

## STD-BUILD-001 — 服务端 TargetFramework == net10.0（MUST 类误报）

- **误报根因**：检查器把**所有** csproj 文本拼接为 `proj` 后取**首个** `<TargetFramework>` 匹配。paired mod（Client `netstandard2.1` + Server `net10.0`）拼入后，首个命中是 Client 的 `netstandard2.1`，服务端规则据此误判。
- **证据**：`src/Client/SamMeow.LLMTraderChat.Client.csproj` = `netstandard2.1`（Mono 可加载，STD-BUILD-002 亦据此 PASS）；`src/Server/SamMeow.LLMTraderChat.Server.csproj` = `net10.0`（SPT 4.1 要求）。服务端实机构建/加载正常（M1/M2 实机日志）。
- **处置**：**豁免**。仓库检查器修复方向：`proj-value-*` 家族按**每个 csproj 文件**分别取值（而非拼接取首个），或按 `kind` 选择对应 csproj（依 `IModMetadata`/`BaseUnityPlugin` 归属判定）。去向：`scripts/check-mod-standard.ps1` + `knowledge/spt-kb/curated/modding-standard/rules.json`。

## STD-BUILD-006 — 可覆盖的安装路径 `<SPTInstallPath Condition=`（SHOULD；无需豁免标记，仅说明理由）

- **误报根因**：检查器只认字面量 `<SPTInstallPath Condition=`；本仓库惯例为 `SptRoot` + `SptClientDir`/`SptRuntimeDir` 族。
- **证据**：`Directory.Build.props`（mod 级与 `mods/` 级）用 `<SptRoot Condition="'$(SptRoot)' == ''">`、`$(SptClientDir)`、`$(SptRuntimeDir)`，且均可命令行覆盖（`-p:SptRoot=...`）。
- **处置**：**豁免**。仓库检查器修复方向：把该规则 pattern 扩展为 `(SPTInstallPath|SptRoot|SptClientDir|SptRuntimeDir)\s*...Condition=`，或在 `rules.json` 的 `check.params.patterns` 加入仓库实际约定名族。去向：`rules.json`（BUILD-006）。

## STD-META-003 — 反向域名式唯一 ModGuid（MUST 类误报）

- **误报根因**：`meta-003-guid` 处理器以 `ModGuid\s*\{[^}]*\}\s*=\s*"([^"]+)"` 只认**字面量默认值**；本 mod 为单源常量，`ModGuid` 取自 `SharedConstants.ModGuid`，正则匹配不到。
- **证据**：`src/Server/ModMetadata.cs`：`public string ModGuid { get; init; } = SharedConstants.ModGuid;`；`src/Shared/SharedConstants.cs`：`public const string ModGuid = "sammeow.llmtraderchat";`（客户端 `[BepInPlugin]` 与服务端共用同一常量）。运行时值正确：测试 `ScaffoldSmokeTests.Metadata_ExposesExpectedIdentityFields` 断言 `"sammeow.llmtraderchat"`。
- **处置**：**豁免**。仓库检查器修复方向：识别 `= <Type>.<Const>` 间接赋值并回查常量定义，或在 `ModMetadata.cs` 之外的 `SharedConstants.cs` 常量声明处匹配字面量。去向：`scripts/check-mod-standard.ps1`（`meta-003-guid` handler）。

## 其它

- **STD-CFG-006**（客户端使用 BepInEx `Config.Bind`）：工单 07 已转 **PASS**，无需豁免。
