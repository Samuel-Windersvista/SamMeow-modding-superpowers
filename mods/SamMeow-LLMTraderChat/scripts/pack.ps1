#Requires -Version 5.1
<#
.SYNOPSIS
    构建 LLM Trader Chat（Client + Server + Shared）并打成单个可部署 zip（双端同包）。

.DESCRIPTION
    产物为单个 zip，顶层按游戏根相对路径组织：
      BepInEx/plugins/SamMeow.LLMTraderChat/          # 客户端半（Client.dll + Shared.dll）
      SPT_Runtime/user/mods/SamMeow.LLMTraderChat/    # 服务端半（Server.dll + Shared.dll + config/ + knowledge/）
      README.md / LICENSE / GLOSSARY.md               # 随包
    服务端全部 assembly 合并进同一个 user/mods/SamMeow.LLMTraderChat/ 目录，
    该目录内只允许一个 IModMetadata 实现（Server.dll；Shared.dll 不实现该接口）。
    版本号默认从 Directory.Build.props 的 <Version> 解析（唯一来源），也可 -Version 覆盖。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/pack.ps1

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/pack.ps1 -SptRoot "D:\SPT" -Version 0.1.0
#>
[CmdletBinding()]
param(
    # 构建配置
    [string]$Configuration = 'Release',

    # SPT 根目录（含 EscapeFromTarkov.exe 与 SPT_Runtime\）；留空则用 Directory.Build.props 的默认值
    [string]$SptRoot = '',

    # 发布版本；留空则从 Directory.Build.props 的 <Version> 解析（唯一来源）
    [string]$Version = '',

    # 输出目录（默认 <项目根>\release）
    [string]$OutputDir = ''
)

$ErrorActionPreference = 'Stop'

# mod 文件夹名：两端的插件/模组目录同名
$ModName = 'SamMeow.LLMTraderChat'

$RepoRoot = Split-Path -Parent $PSScriptRoot

# 记录是否显式传入 -Version：决定是否向构建透传 -p:Version，使 DLL 内嵌版本与归档命名一致。
$versionExplicit = -not [string]::IsNullOrWhiteSpace($Version)

# 版本号集中定义在 Directory.Build.props；未显式传入时从中解析，避免两处版本漂移。
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
    $OutputDir = Join-Path $RepoRoot 'release'
}

# 1. 构建整个解决方案（Client + Server + Shared + Tests）。
$sln = Join-Path $RepoRoot 'SamMeow.LLMTraderChat.sln'
$buildArgs = @('build', $sln, '-c', $Configuration)
if (-not [string]::IsNullOrWhiteSpace($SptRoot)) {
    # 安装路径属性可覆盖。
    $buildArgs += "-p:SptRoot=$SptRoot"
}
if ($versionExplicit) {
    # 显式 -Version 时透传给构建，使 DLL 内嵌版本与归档命名一致
    # （默认版本仍由 Directory.Build.props 的 <Version> 提供）。
    $buildArgs += "-p:Version=$Version"
}

Write-Host ("dotnet " + ($buildArgs -join ' '))
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build 失败（退出码 $LASTEXITCODE）。"
}

$clientOut = Join-Path $RepoRoot "src\Client\bin\$Configuration"
$serverOut = Join-Path $RepoRoot "src\Server\bin\$Configuration"

# 2. 组装发布树（暂存于 build\stage，被 .gitignore 忽略）。
$stage = Join-Path $RepoRoot 'build\stage'
if (Test-Path -LiteralPath $stage) {
    Remove-Item -LiteralPath $stage -Recurse -Force
}

$clientDest = Join-Path $stage "BepInEx\plugins\$ModName"
$serverDest = Join-Path $stage "SPT_Runtime\user\mods\$ModName"
New-Item -ItemType Directory -Path $clientDest, $serverDest -Force | Out-Null

# 拷贝前显式预检构建产物，缺失即响亮失败（避免打出自带空目录的坏包）。
$clientDll = Join-Path $clientOut "$ModName.Client.dll"
$clientSharedDll = Join-Path $clientOut "$ModName.Shared.dll"
$serverDll = Join-Path $serverOut "$ModName.Server.dll"
$serverSharedDll = Join-Path $serverOut "$ModName.Shared.dll"
foreach ($artifact in @($clientDll, $clientSharedDll, $serverDll, $serverSharedDll)) {
    if (-not (Test-Path -LiteralPath $artifact)) {
        throw "未找到构建产物：$artifact（构建是否成功？配置 $Configuration）。"
    }
}

# 客户端半：Client.dll + Shared.dll -> BepInEx/plugins/<Mod>/
Copy-Item -LiteralPath $clientDll -Destination $clientDest
Copy-Item -LiteralPath $clientSharedDll -Destination $clientDest

# 服务端半：全部 assembly 合并进同一个 user/mods/<Mod>/ 目录（Shared.dll 不实现 IModMetadata）。
Copy-Item -LiteralPath $serverDll -Destination $serverDest
Copy-Item -LiteralPath $serverSharedDll -Destination $serverDest

# 服务端配置与知识文档随包（config 含 config.jsonc + defaultConfig.jsonc；knowledge 为 per-trader 文档目录）。
foreach ($dir in @('config', 'knowledge')) {
    $src = Join-Path $RepoRoot $dir
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination $serverDest -Recurse
    }
}

# README / LICENSE / GLOSSARY 随包。
foreach ($doc in @('README.md', 'LICENSE', 'GLOSSARY.md')) {
    $docPath = Join-Path $RepoRoot $doc
    if (Test-Path -LiteralPath $docPath) {
        Copy-Item -LiteralPath $docPath -Destination $stage
    }
}

# 3. 打 zip：顶层即 BepInEx/ 与 SPT_Runtime/（外加 README/LICENSE/GLOSSARY）。
if (-not (Test-Path -LiteralPath $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}
$zipPath = Join-Path $OutputDir "$ModName-$Version.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zipPath -Force

Write-Host "发布包已生成：$zipPath"
Write-Host "  BepInEx/plugins/$ModName/ 与 SPT_Runtime/user/mods/$ModName/"
