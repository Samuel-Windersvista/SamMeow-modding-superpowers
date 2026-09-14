<#
.SYNOPSIS
    构建 paired mod 的 Client + Server + Shared，并打成单个发布 zip（双端同包）。

.DESCRIPTION
    产物为单个 zip，顶层按游戏根相对路径组织（STD-PKG-001）：
      BepInEx/plugins/<ModName>/          # 客户端半（Client.dll + Shared.dll）
      SPT_Runtime/user/mods/<ModName>/    # 服务端半（Server.dll + Shared.dll + config/）
      README.md / LICENSE                 # 随包（STD-PKG-006）
    服务端全部 assembly 合并进同一个 user/mods/<ModName>/ 目录（STD-PKG-003），
    该目录内只允许一个 IModMetadata 实现（STD-PKG-004）。

.EXAMPLE
    pwsh -File scripts/pack.ps1 -SptInstallPath "D:\SPT"

.EXAMPLE
    pwsh -File scripts/pack.ps1 -Version 1.2.3 -OutputDir .\dist
#>
[CmdletBinding()]
param(
    # 构建配置
    [string]$Configuration = 'Release',

    # STD-BUILD-006：SPT 根目录（含 EscapeFromTarkov.exe 与 SPT_Runtime\）；留空则用 Directory.Build.props 的默认值
    [string]$SptInstallPath = '',

    # STD-PKG-005：发布版本；留空则从 Directory.Build.props 的 <Version> 解析（唯一来源）
    [string]$Version = '',

    # 输出目录（默认 <仓库根>\dist）
    [string]$OutputDir = ''
)

$ErrorActionPreference = 'Stop'

# mod 文件夹名：与脚手架替换后的 {{MOD_CLASS_NAME}} 一致；两端的插件/模组目录同名。
$ModName = '{{MOD_CLASS_NAME}}'

$RepoRoot = Split-Path -Parent $PSScriptRoot

# STD-META-007 / STD-PKG-005：版本号集中定义在 Directory.Build.props；未显式传入时从中解析，避免两处版本漂移。
if ([string]::IsNullOrWhiteSpace($Version)) {
    $propsPath = Join-Path $RepoRoot 'Directory.Build.props'
    $props = Get-Content -LiteralPath $propsPath -Raw
    if ($props -match '<Version>([^<]+)</Version>') {
        $Version = $Matches[1].Trim()
    }
    else {
        throw "无法从 $propsPath 解析 <Version>，请用 -Version 显式传入。"
    }
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot 'dist'
}

# 1. 构建整个解决方案（Client + Server + Shared）。
$sln = Join-Path $RepoRoot 'PairedModTemplate.sln'
$buildArgs = @('build', $sln, '-c', $Configuration)
if (-not [string]::IsNullOrWhiteSpace($SptInstallPath)) {
    # STD-BUILD-006：安装路径属性可覆盖。
    $buildArgs += "-p:SPTInstallPath=$SptInstallPath"
}

Write-Host ("dotnet " + ($buildArgs -join ' '))
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build 失败（退出码 $LASTEXITCODE）。"
}

$clientOut = Join-Path $RepoRoot "Client\bin\$Configuration"
$serverOut = Join-Path $RepoRoot "Server\bin\$Configuration"

# 2. 组装发布树。
$stage = Join-Path $OutputDir 'stage'
if (Test-Path -LiteralPath $stage) {
    Remove-Item -LiteralPath $stage -Recurse -Force
}

$clientDest = Join-Path $stage "BepInEx\plugins\$ModName"
$serverDest = Join-Path $stage "SPT_Runtime\user\mods\$ModName"
New-Item -ItemType Directory -Path $clientDest, $serverDest -Force | Out-Null

# 客户端半：Client.dll + Shared.dll -> BepInEx/plugins/<Mod>/
Copy-Item -LiteralPath (Join-Path $clientOut "$ModName.Client.dll") -Destination $clientDest
Copy-Item -LiteralPath (Join-Path $clientOut "$ModName.Shared.dll") -Destination $clientDest

# 服务端半：全部 assembly 合并进同一个 user/mods/<Mod>/ 目录（STD-PKG-003）。
#   STD-PKG-004：本目录内只允许一个 IModMetadata 实现（即 Server.dll；Shared.dll 只是共享常量，不实现该接口）。
Copy-Item -LiteralPath (Join-Path $serverOut "$ModName.Server.dll") -Destination $serverDest
Copy-Item -LiteralPath (Join-Path $serverOut "$ModName.Shared.dll") -Destination $serverDest

# 服务端配置随包（STD-CFG-001 / STD-CFG-002 / STD-CFG-005）。
$serverConfig = Join-Path $RepoRoot 'Server\config'
if (Test-Path -LiteralPath $serverConfig) {
    Copy-Item -LiteralPath $serverConfig -Destination $serverDest -Recurse
}

# STD-PKG-006：README / LICENSE 随包。
foreach ($doc in @('README.md', 'LICENSE')) {
    $docPath = Join-Path $RepoRoot $doc
    if (Test-Path -LiteralPath $docPath) {
        Copy-Item -LiteralPath $docPath -Destination $stage
    }
}

# 3. 打 zip：顶层即 BepInEx/ 与 SPT_Runtime/（外加 README/LICENSE）。
if (-not (Test-Path -LiteralPath $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}
$zipPath = Join-Path $OutputDir "$ModName-$Version.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zipPath -Force

Write-Host "发布包已生成：$zipPath"
Write-Host "  BepInEx/plugins/$ModName/ 与 SPT_Runtime/user/mods/$ModName/（STD-PKG-001 / STD-PKG-003）"
