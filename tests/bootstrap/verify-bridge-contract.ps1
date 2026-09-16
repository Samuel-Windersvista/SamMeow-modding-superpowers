# 桥接跨语言契约工件（C10）：工件/夹具可解析 + 夹具 cases 非空 + 消费点完好。
#
# 第 1 部分：shared/bridge-contract/contract.json 与 fixtures/** 下全部 JSON
#           可用 ConvertFrom-Json 解析（缺失或非法 JSON 即失败）。
# 第 2 部分：夹具 cases 非空——log-normalization / log-aggregation 各至少一条，
#           payloads 目录至少一份 JSON。
# 第 3 部分：消费点完好——C# csproj 含 contract.json 的 EmbeddedResource include、
#           TS src/bridge/contract.ts 存在、便携构建必需清单含 contract.json。
#
# 输出风格与同目录既有 bootstrap 脚本一致（PASS/FAIL 行 + 汇总）；exit 0/1。
# 注意：本脚本含中文注释，必须以 UTF-8 BOM 保存——无 BOM 的中文 PS1 在
# PowerShell 5.1 下会吞换行（C5 教训）。

$ErrorActionPreference = "Stop"

# .NET 本地化异常消息（含中文）在 PS 5.1 默认 ANSI 代码页下会乱码；与
# verify-version-identity.ps1 / verify-all.ps1 保持一致，固定为 UTF-8。
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$contractRel = "shared/bridge-contract/contract.json"
$fixturesRel = "shared/bridge-contract/fixtures"

# 读取并以 UTF-8 解析单个 JSON 文件；失败时累积失败并返回 $null。
function Read-JsonArtifact {
    param(
        [Parameter(Mandatory)][string]$RelativePath
    )

    $full = Join-Path $repoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $full)) {
        Add-BootstrapFailure "missing bridge contract artifact: $RelativePath"
        return $null
    }

    try {
        return (Get-Content -LiteralPath $full -Raw -Encoding UTF8 | ConvertFrom-Json)
    } catch {
        Add-BootstrapFailure "bridge contract artifact is not valid JSON: $RelativePath ($($_.Exception.Message))"
        return $null
    }
}

# ---- 1/3 工件与夹具可解析 -----------------------------------------------------

Write-Host "  [1/3] bridge contract artifacts parse ..."

$contract = Read-JsonArtifact -RelativePath $contractRel

$fixturesDir = Join-Path $repoRoot $fixturesRel
$fixtureObjects = @{}
if (-not (Test-Path -LiteralPath $fixturesDir)) {
    Add-BootstrapFailure "missing bridge contract fixture directory: $fixturesRel"
} else {
    $fixtureFiles = @(Get-ChildItem -LiteralPath $fixturesDir -Recurse -File -Filter *.json)
    if ($fixtureFiles.Count -eq 0) {
        Add-BootstrapFailure "bridge contract fixture directory has no JSON files: $fixturesRel"
    }
    foreach ($file in $fixtureFiles) {
        $relative = $file.FullName.Substring($repoRoot.Length).TrimStart([char]'\', [char]'/').Replace('\', '/')
        $parsed = Read-JsonArtifact -RelativePath $relative
        if ($null -ne $parsed) {
            $fixtureObjects[$relative] = $parsed
        }
    }
}

if ($null -ne $contract) {
    Write-Host ("  [OK]   {0}: parsed" -f $contractRel)
}
if ($fixtureObjects.Count -gt 0) {
    Write-Host ("  [OK]   {0}: {1} JSON file(s) parsed" -f $fixturesRel, $fixtureObjects.Count)
}

# ---- 2/3 夹具 cases 非空 ------------------------------------------------------

Write-Host "  [2/3] bridge contract fixtures carry cases ..."

# 返回 cases 数量；夹具缺失或结构不符时累积失败并返回 -1。
function Get-FixtureCaseCount {
    param(
        [Parameter(Mandatory)][string]$RelativePath
    )

    if (-not $fixtureObjects.ContainsKey($RelativePath)) {
        Add-BootstrapFailure "missing bridge contract fixture: $RelativePath"
        return -1
    }

    $cases = $fixtureObjects[$RelativePath].cases
    if ($null -eq $cases) {
        Add-BootstrapFailure "fixture has no 'cases' array: $RelativePath"
        return -1
    }

    return @($cases).Count
}

foreach ($fixture in @(
    "$fixturesRel/log-normalization.json",
    "$fixturesRel/log-aggregation.json"
)) {
    $caseCount = Get-FixtureCaseCount -RelativePath $fixture
    if ($caseCount -gt 0) {
        Write-Host ("  [OK]   {0}: {1} case(s)" -f $fixture, $caseCount)
    } elseif ($caseCount -eq 0) {
        Add-BootstrapFailure "fixture has an empty 'cases' array: $fixture"
    }
}

$payloadFixtures = @($fixtureObjects.Keys | Where-Object { $_ -like "$fixturesRel/payloads/*.json" })
if ($payloadFixtures.Count -gt 0) {
    Write-Host ("  [OK]   {0}: {1} payload fixture(s)" -f "$fixturesRel/payloads", $payloadFixtures.Count)
} else {
    Add-BootstrapFailure "no payload fixture JSON found under $fixturesRel/payloads/"
}

# ---- 3/3 消费点完好 ----------------------------------------------------------

Write-Host "  [3/3] bridge contract consumers intact ..."

# C#：csproj 必须把 contract.json 编译期嵌入 DLL（EmbeddedResource）。
$csprojRel = "tools/tarkov-runtime-bridge/TarkovRuntimeBridge.csproj"
$csprojFull = Join-Path $repoRoot $csprojRel
if (-not (Test-Path -LiteralPath $csprojFull)) {
    Add-BootstrapFailure "missing C# bridge project: $csprojRel"
} else {
    $csprojText = [IO.File]::ReadAllText($csprojFull, [Text.Encoding]::UTF8)
    if ($csprojText -notmatch '<EmbeddedResource[^>]*Include\s*=\s*"[^"]*bridge-contract[\\/]+contract\.json"') {
        Add-BootstrapFailure ($csprojRel + ' is missing the EmbeddedResource include for shared/bridge-contract/contract.json')
    } elseif ($csprojText -notmatch 'LogicalName\s*=\s*"TarkovRuntimeBridge\.bridge-contract\.json"') {
        Add-BootstrapFailure ($csprojRel + ' EmbeddedResource is missing LogicalName="TarkovRuntimeBridge.bridge-contract.json" (must match BridgeContract.ResourceName)')
    } else {
        Write-Host ("  [OK]   {0}: EmbeddedResource include + LogicalName present" -f $csprojRel)
    }
}

# C#：协议版本消费点必须从契约派生（不得回退源码字面量）。
$pluginRel = "tools/tarkov-runtime-bridge/src/Plugin.cs"
$pluginFull = Join-Path $repoRoot $pluginRel
if (-not (Test-Path -LiteralPath $pluginFull)) {
    Add-BootstrapFailure "missing C# bridge plugin: $pluginRel"
} else {
    $pluginText = [IO.File]::ReadAllText($pluginFull, [Text.Encoding]::UTF8)
    if ($pluginText -notmatch 'BridgeContract\.ProtocolVersion') {
        Add-BootstrapFailure ($pluginRel + ' does not derive ProtocolVersion from BridgeContract')
    } else {
        Write-Host ("  [OK]   {0}: ProtocolVersion derived from BridgeContract" -f $pluginRel)
    }
}

# TS：契约模块必须存在（运行时读取 + 校验 + 缓存）。
$tsContractRel = "tools/tarkov-runtime-mcp/src/bridge/contract.ts"
Assert-PathExists -Path (Join-Path $repoRoot $tsContractRel) -Label $tsContractRel

# 便携构建：必需清单必须包含契约工件（否则便携树静默丢契约）。
$portableScriptRel = "scripts/build-portable-plugin.ps1"
$portableScriptFull = Join-Path $repoRoot $portableScriptRel
if (-not (Test-Path -LiteralPath $portableScriptFull)) {
    Add-BootstrapFailure "missing portable build script: $portableScriptRel"
} else {
    $portableText = [IO.File]::ReadAllText($portableScriptFull, [Text.Encoding]::UTF8)
    if ($portableText -notmatch '"shared/bridge-contract/contract\.json"') {
        Add-BootstrapFailure ($portableScriptRel + ' $requiredPortablePaths is missing shared/bridge-contract/contract.json')
    } else {
        Write-Host ("  [OK]   {0}: required portable path present" -f $portableScriptRel)
    }
}

Complete-BootstrapCheck -SuccessMessage "bridge contract artifacts parse, fixtures carry cases, consumers intact."
