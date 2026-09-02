param(
  [string]$PluginName = "Tosox.DynamicItemWeights",
  [string]$DllName = "Tosox.DynamicItemWeights.dll",
  [string]$ConfigJson = "emptyweights.json"
)

$repoRoot = $PSScriptRoot
$bin = Join-Path $repoRoot "bin\Release"
$dll = Join-Path $bin $DllName
if (-not (Test-Path $dll)) {
  throw "DLL not found. Build Release first."
}

$ver = (Get-Item $dll).VersionInfo.FileVersion
$packRoot = Join-Path $repoRoot "packed"

# Create layout
$dstPlugin = Join-Path $packRoot "BepInEx\plugins"
$dstConfig = Join-Path $packRoot "BepInEx\plugins\$PluginName"
New-Item $dstPlugin -ItemType Directory -Force | Out-Null
New-Item $dstConfig -ItemType Directory -Force | Out-Null

# Copy DLL
Copy-Item $dll $dstPlugin

# Include default base-weights file
$defaultJson = Join-Path $repoRoot $ConfigJson
Copy-Item $defaultJson $dstConfig

# Zip
$zipName = "$PluginName-v$ver.zip"
$zipPath = Join-Path $repoRoot $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $packRoot "*") -DestinationPath $zipPath

# Clean up
Remove-Item $packRoot -Recurse -Force -ErrorAction SilentlyContinue | Out-Null

Write-Host "Packaged -> $zipPath"
