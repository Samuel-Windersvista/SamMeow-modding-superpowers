#Requires -Version 5.1
<#
.SYNOPSIS
    构建 InescapableTarkovsSoftcore 服务端 mod 并组装 MO2 overlay 产物树。

.DESCRIPTION
    产出 build/overlay/SPT_Runtime/user/mods/com.sammeow.inescapable-softcore/，
    产物 = mod DLL + config.json（config/default-config.json 的逐字节拷贝）
    + config/ 外置数据表全树（12 个模板，源 = data/）。
    无静态件、无启动器背景，脚本不含任何 customBackground 逻辑。
    幂等：每次运行先清空重建 overlay 根目录；拷贝后断言 12 个数据表全部就位。
    任何失败以非零退出码结束。

.PARAMETER Configuration
    构建配置。默认 Release。

.PARAMETER SptRuntimeDir
    可选的 SPT 运行时引用程序集目录覆盖，透传给 MSBuild 的 -p:SptRuntimeDir=...

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/build.ps1
    powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Configuration Debug -SptRuntimeDir "D:\SPT_Runtime"
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$SptRuntimeDir
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot    = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src\InescapableTarkovsSoftcore\InescapableTarkovsSoftcore.csproj'
$assembly    = 'InescapableTarkovsSoftcore'
$modDirName  = 'com.sammeow.inescapable-softcore'
$overlayRoot = Join-Path $repoRoot 'build\overlay'
$modDir      = Join-Path $overlayRoot "SPT_Runtime\user\mods\$modDirName"
$dllPath     = Join-Path $repoRoot "src\InescapableTarkovsSoftcore\bin\$Configuration\net10.0\$assembly.dll"
$configTemplate = Join-Path $repoRoot 'config\default-config.json'
$configDest  = Join-Path $modDir 'config.json'

try {
    Write-Host "[ITS] Building $assembly ($Configuration)..." -ForegroundColor Cyan
    $buildArgs = @($projectPath, '-c', $Configuration)
    if ($SptRuntimeDir) {
        $buildArgs += "-p:SptRuntimeDir=$SptRuntimeDir"
    }

    & dotnet build @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build 失败，退出码 $LASTEXITCODE"
    }

    if (-not (Test-Path -LiteralPath $dllPath)) {
        throw "未找到构建产物：$dllPath"
    }

    if (-not (Test-Path -LiteralPath $configTemplate)) {
        throw "未找到配置模板：$configTemplate"
    }

    Write-Host "[ITS] 组装 overlay 至 $modDir ..." -ForegroundColor Cyan
    # 幂等：每次运行先清空重建 overlay 根，避免旧产物残留
    if (Test-Path -LiteralPath $overlayRoot) {
        Remove-Item -LiteralPath $overlayRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $modDir -Force | Out-Null

    Copy-Item -LiteralPath $dllPath -Destination $modDir -Force

    # config.json 为受控模板 config/default-config.json 的逐字节拷贝（JSONC，运行时容忍注释）
    Copy-Item -LiteralPath $configTemplate -Destination $configDest -Force

    # 外置数据表模板：data/ 源 → overlay config/ 子树（幂等；与内嵌资源同源，含中文 JSONC 文件头）
    $dataRoot      = Join-Path $repoRoot 'data'
    $configDir     = Join-Path $modDir 'config'
    $trueItemsDest = Join-Path $configDir 'trueitems'
    $softcoreDest  = Join-Path $configDir 'softcore'
    New-Item -ItemType Directory -Path $trueItemsDest -Force | Out-Null
    New-Item -ItemType Directory -Path $softcoreDest -Force | Out-Null

    Copy-Item -Path (Join-Path $dataRoot 'trueitems\*.json') -Destination $trueItemsDest -Force
    Copy-Item -LiteralPath (Join-Path $dataRoot 'antigravArmbands\armbands.json') -Destination (Join-Path $configDir 'armbands.json') -Force
    Copy-Item -LiteralPath (Join-Path $dataRoot 'backpacks\backpacks.json') -Destination (Join-Path $configDir 'backpacks.json') -Force
    Copy-Item -Path (Join-Path $dataRoot 'softcore\*.json') -Destination $softcoreDest -Force

    # 断言 12 个外置数据表全部就位（缺失则非零退出）
    $expectedTables = @(
        'config\trueitems\barter.json',
        'config\trueitems\clothing.json',
        'config\trueitems\keycards.json',
        'config\trueitems\medicals.json',
        'config\trueitems\partsnmods.json',
        'config\trueitems\provisions.json',
        'config\armbands.json',
        'config\backpacks.json',
        'config\softcore\scavcase.json',
        'config\softcore\fleamarket.json',
        'config\softcore\crafting-rebalance.json',
        'config\softcore\crafting-recipes.json'
    )
    $missingTables = @($expectedTables | Where-Object { -not (Test-Path -LiteralPath (Join-Path $modDir $_)) })
    if ($missingTables.Count -gt 0) {
        throw ('overlay 缺少外置数据表：' + [Environment]::NewLine + '  - ' + ($missingTables -join ([Environment]::NewLine + '  - ')))
    }

    Write-Host "[ITS] Overlay 就绪：$modDir" -ForegroundColor Green
    Write-Host "[ITS] 产物清单（$(($expectedTables.Count) + 2) 项：DLL + config.json + 12 数据表）：" -ForegroundColor Green
    Get-ChildItem -LiteralPath $modDir -Recurse -File | ForEach-Object {
        Write-Host ("  " + $_.FullName.Substring($modDir.Length + 1))
    }
    Write-Host "[ITS] 部署映射：overlay 根 -> 游戏根 SPT_41x；服务端 mod -> SPT_Runtime\user\mods\" -ForegroundColor Green
    exit 0
}
catch {
    [Console]::Error.WriteLine("[ITS] 构建失败：$($_.Exception.Message)")
    exit 1
}
