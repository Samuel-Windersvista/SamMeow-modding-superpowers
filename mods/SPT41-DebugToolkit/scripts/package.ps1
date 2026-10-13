# SamMeow.DebugToolkit 打包脚本（工单 01 雏形）
#
# 默认：dotnet build -c Release -> 打包为游戏根相对布局的 zip。
# 产出：dist\SamMeow.DebugToolkit-<version>.zip
#   BepInEx\plugins\SamMeow.DebugToolkit.dll
#   README.md / LICENSE / THIRD-PARTY-NOTICES.md
#
# 用法：
#   powershell -ExecutionPolicy Bypass -File scripts\package.ps1
#   powershell -ExecutionPolicy Bypass -File scripts\package.ps1 -SkipBuild
#
# 任一步失败非零退出。

[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$Configuration = "Release",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }

$modRoot = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $modRoot "SPT41DebugToolkit.csproj"
if (-not (Test-Path -LiteralPath $csproj)) {
    Write-Error "未找到 csproj：$csproj"
}

# ---- 版本（单源：csproj <Version>） -----------------------------------------

$csprojText = [System.IO.File]::ReadAllText($csproj, [System.Text.Encoding]::UTF8)
$m = [regex]::Match($csprojText, "<Version>\s*([^<]+?)\s*</Version>")
if (-not $m.Success) {
    Write-Error "csproj 中未找到 <Version>"
}
$version = $m.Groups[1].Value
Write-Host "版本：$version"

# ---- 构建 --------------------------------------------------------------------

if (-not $SkipBuild) {
    Write-Host "构建（$Configuration）..."
    & dotnet build $csproj -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet build 失败（exit $LASTEXITCODE）"
    }
}
else {
    Write-Host "跳过构建（-SkipBuild）。"
}

$dll = Join-Path $modRoot ("bin\{0}\SamMeow.DebugToolkit.dll" -f $Configuration)
if (-not (Test-Path -LiteralPath $dll)) {
    Write-Error "未找到构建产物：$dll"
}

# ---- 组装（游戏根相对布局） --------------------------------------------------

$distDir = if ($OutputDir -ne "") { $OutputDir } else { Join-Path $modRoot "dist" }
New-Item -ItemType Directory -Force -Path $distDir | Out-Null

$staging = Join-Path $env:TEMP ("SamMeow.DebugToolkit-pkg-" + [guid]::NewGuid().ToString("N"))
$pluginsDir = Join-Path $staging "BepInEx\plugins"
New-Item -ItemType Directory -Force -Path $pluginsDir | Out-Null

Copy-Item -LiteralPath $dll -Destination (Join-Path $pluginsDir "SamMeow.DebugToolkit.dll") -Force

foreach ($name in @("README.md", "LICENSE", "THIRD-PARTY-NOTICES.md")) {
    $src = Join-Path $modRoot $name
    if (-not (Test-Path -LiteralPath $src)) {
        Write-Error "缺少随包文件：$src"
    }
    Copy-Item -LiteralPath $src -Destination (Join-Path $staging $name) -Force
}

# ---- 压缩 --------------------------------------------------------------------

$zip = Join-Path $distDir ("SamMeow.DebugToolkit-{0}.zip" -f $version)
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }

Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip -CompressionLevel Optimal
Remove-Item -LiteralPath $staging -Recurse -Force

Write-Host "已打包：$zip"
exit 0
