# Download best-version release zips for hot mods (resumable).
param(
    [int]$MaxCount = 30
)
$ErrorActionPreference = 'Continue'
$UA = 'SPT-knowledge-rescue/1.0 (archival backup; contact: local)'
$Root = Split-Path -Parent $PSScriptRoot
$ModsDir = Join-Path $Root 'mods'
$index = Get-Content (Join-Path $Root 'hot-index.json') -Raw | ConvertFrom-Json

$pending = @($index | Where-Object { $_.best_link -and $_.best_version })
Write-Output "total with release links: $($pending.Count), downloading up to $MaxCount"
$done = 0
foreach ($m in $pending) {
    if ($done -ge $MaxCount) { break }
    $relDir = Join-Path $ModsDir ("$($m.id)_release")
    New-Item -ItemType Directory -Force -Path $relDir | Out-Null
    $safeVer = $m.best_version -replace '[^0-9A-Za-z._-]', '_'
    $outFile = Join-Path $relDir "$safeVer.zip"
    if (Test-Path $outFile) { Write-Output "SKIP $($m.id) $safeVer (exists)"; continue }
    try {
        Start-Sleep -Milliseconds 800
        Invoke-WebRequest -Uri $m.best_link -Headers @{ 'User-Agent' = $UA } -OutFile $outFile
        $size = [Math]::Round((Get-Item $outFile).Length / 1MB, 2)
        Write-Output "[OK] $($m.id) $($m.name) ver=$($m.best_version) spt=$($m.best_spt) size=${size}MB"
        $done++
    } catch {
        Write-Output "[FAIL] $($m.id) $($m.name): $($_.Exception.Message)"
    }
}
