#Requires -Version 5.1
<#
.SYNOPSIS
    构建 InescapableTarkovsSoftcore 服务端 mod 并组装 MO2 overlay 产物树。

.DESCRIPTION
    产出 build/overlay/SPT_Runtime/user/mods/com.sammeow.inescapable-softcore/，
    内含 mod DLL 与受控配置模板（config/default-config.json）的逐字节拷贝 config.json。
    本期（v0.1.0）无静态件、无启动器背景，脚本不含任何 customBackground 逻辑。
    幂等：每次运行先清空重建 overlay 根目录。任何失败以非零退出码结束。

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

    Write-Host "[ITS] Overlay 就绪：$modDir" -ForegroundColor Green
    Write-Host "[ITS] 部署映射：overlay 根 -> 游戏根 SPT_41x；服务端 mod -> SPT_Runtime\user\mods\" -ForegroundColor Green
    exit 0
}
catch {
    [Console]::Error.WriteLine("[ITS] 构建失败：$($_.Exception.Message)")
    exit 1
}
