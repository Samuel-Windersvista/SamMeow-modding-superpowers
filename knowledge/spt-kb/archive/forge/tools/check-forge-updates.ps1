# check-forge-updates.ps1
# Daily Forge update check: compare locally known latest version vs Forge API latest.
# On update: refresh KB versions file + write report.
# Usage: powershell -ExecutionPolicy Bypass -File check-forge-updates.ps1 [-Ids 1015,1038] [-MaxIds 124]
param(
    [string]$Ids = '',
    [int]$MaxIds = 0,
    [switch]$RefreshVersions
)

$ErrorActionPreference = 'Stop'
$UA = 'SPT-knowledge-rescue/1.0 (archival backup; contact: local)'
$Headers = @{ 'User-Agent' = $UA }
$Base = 'https://forge.sp-tarkov.com/api/v0'

$Root = 'E:\云文件\GitHub\SamMeow-modding-superpowers\knowledge\spt-kb\archive\forge'
$ModsIndex = Join-Path $Root 'MODS-INDEX.md'
$HotDir = Join-Path $Root 'api\hot-mods'
$StateFile = Join-Path $Root 'updates-state.json'
$OutDir = 'D:\Temp\opencode\forge-update-check'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$Date = Get-Date -Format 'yyyyMMdd'
$ReportFile = Join-Path $OutDir "updates-report-$Date.json"

function Get-Api([string]$Uri) {
    Start-Sleep -Seconds 3
    $attempt = 0
    while ($true) {
        try { return Invoke-RestMethod -Uri $Uri -Headers $Headers }
        catch {
            $code = $_.Exception.Response.StatusCode.value__
            $attempt++
            if ($code -in @(403, 429) -and $attempt -le 6) {
                $wait = [Math]::Min(45, 8 * $attempt)
                Write-Output "[THROTTLE] $Uri (code $code) retry $attempt after ${wait}s"
                Start-Sleep -Seconds $wait
            } else { throw }
        }
    }
}

# ---------- 1. Read id list ----------
$allIds = @()
if ($Ids) {
    $allIds = $Ids -split ',' | ForEach-Object { [int]$_ } | Sort-Object -Unique
} else {
    foreach ($line in (Get-Content $ModsIndex)) {
        if ($line -match '^\|\s*(\d+)\s*\|') { $allIds += [int]$Matches[1] }
    }
    $allIds = $allIds | Sort-Object -Unique
}
if ($MaxIds -gt 0) { $allIds = $allIds | Select-Object -First $MaxIds }
Write-Output "[*] checking $($allIds.Count) mods: $($allIds -join ',')"

# ---------- 2. Load state ----------
$state = @{}
if (Test-Path $StateFile) {
    $loaded = Get-Content $StateFile -Raw | ConvertFrom-Json
    if ($loaded) {
        foreach ($item in @($loaded)) { $state[[string]$item.id] = $item }
    }
}

# ---------- 3. Check each mod ----------
$report = @()
$newCount = 0
$failCount = 0

foreach ($id in $allIds) {
    $verFile = Join-Path $HotDir "$id.versions.json"
    try {
        $resp = Get-Api "$Base/mod/$id/versions?per_page=50&page=1"
        $versions = @($resp.data)
        if ($versions.Count -eq 0) {
            Write-Output "[SKIP] $id no versions"
            continue
        }
        # Latest version: API returns newest-first; sort by published_at when parseable, else keep order
        $latest = $null
        $parsed = @($versions | Where-Object { $_.published_at -and ($_.published_at -as [datetime]) })
        if ($parsed.Count -gt 0) {
            $latest = $parsed | Sort-Object { [datetime]$_.published_at } -Descending | Select-Object -First 1
        } else {
            $latest = $versions | Select-Object -First 1
        }
        $latestVer = [string]$latest.version
        $latestSpt = [string]$latest.spt_version_constraint
        $latestLink = [string]$latest.link

        $prev = $state[$id]
        $prevVer = ''
        if ($prev) { $prevVer = [string]$prev.latestVersion }

        if ($prevVer -and $prevVer -ne $latestVer) {
            Write-Output "[UPDATE] $id : $prevVer -> $latestVer (spt: $latestSpt)"
            $newCount++
            $report += [pscustomobject]@{
                id = $id; prevVersion = $prevVer
                newVersion = $latestVer; sptConstraint = $latestSpt
                link = $latestLink; date = (Get-Date -Format 'yyyy-MM-dd HH:mm')
            }
            if ($RefreshVersions) {
                $versions | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 $verFile
            }
        } else {
            Write-Output "[OK] $id $latestVer (unchanged)"
        }

        $state[$id] = [pscustomobject]@{
            id = $id; latestVersion = $latestVer; sptConstraint = $latestSpt
            checkedAt = (Get-Date -Format 'yyyy-MM-dd HH:mm')
        }
    } catch {
        Write-Output "[FAIL] $id : $($_.Exception.Message)"
        $failCount++
    }
}

# ---------- 4. Write state + report ----------
# Store as array of {id, latestVersion, ...} to avoid numeric-key JSON issues
$stateArr = @($state.Values)
$stateArr | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 $StateFile
if ($report.Count -gt 0) {
    $report | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 $ReportFile
} else {
    '[]' | Set-Content -Encoding UTF8 $ReportFile
}

Write-Output ''
Write-Output '=== SUMMARY ==='
Write-Output "checked: $($allIds.Count) | updates: $newCount | failed: $failCount"
Write-Output "report: $ReportFile"
if ($newCount -gt 0) {
    Write-Output 'UPDATES FOUND - mods to review:'
    $report | ForEach-Object { Write-Output "  [$($_.id)] $($_.prevVersion) -> $($_.newVersion) (spt $($_.sptConstraint)) $($_.link)" }
}
