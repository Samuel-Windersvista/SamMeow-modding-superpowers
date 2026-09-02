# SPT Forge API snapshot script
# Purpose: snapshot forge.sp-tarkov.com mod metadata locally (read-only public API, no auth)
# Usage: powershell -ExecutionPolicy Bypass -File snapshot.ps1
param(
    [int]$CatalogPerPage = 50
)

$ErrorActionPreference = 'Stop'
$UA = 'SPT-knowledge-rescue/1.0 (archival backup; contact: local)'
$Headers = @{ 'User-Agent' = $UA }
$Base = 'https://forge.sp-tarkov.com/api/v0'

$Root = Split-Path -Parent $PSScriptRoot
$ApiDir = Join-Path $Root 'api'
$HotDir = Join-Path $ApiDir 'hot-mods'
New-Item -ItemType Directory -Force -Path $ApiDir, $HotDir | Out-Null

function Get-Api([string]$Uri) {
    Start-Sleep -Milliseconds 300
    $attempt = 0
    while ($true) {
        try {
            return Invoke-RestMethod -Uri $Uri -Headers $Headers
        } catch {
            $code = $_.Exception.Response.StatusCode.value__
            $attempt++
            if ($code -in @(403, 429) -and $attempt -le 6) {
                $wait = [Math]::Min(30, 5 * $attempt)   # 5s, 10s, 15s, 20s, 25s, 30s
                Write-Output "[THROTTLE] $Uri (code $code), retry $attempt after ${wait}s"
                Start-Sleep -Seconds $wait
            } else { throw }
        }
    }
}

# ---------- 1. Reference data ----------
Write-Output '[*] fetching /spt/versions and /mod-categories ...'
(Get-Api "$Base/spt/versions") | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 (Join-Path $ApiDir 'spt-versions.json')
(Get-Api "$Base/mod-categories") | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 (Join-Path $ApiDir 'mod-categories.json')

# ---------- 2. Full catalog ----------
Write-Output '[*] fetching full mod catalog (paginated) ...'
$page = 1
$allMods = @()
while ($true) {
    $resp = Get-Api "$Base/mods?per_page=$CatalogPerPage&page=$page"
    $allMods += $resp.data
    $total = $resp.meta.total
    $pages = [Math]::Ceiling($total / $CatalogPerPage)
    if ($page % 5 -eq 0) { Write-Output "    page $page / $pages (collected $($allMods.Count))" }
    if ($page -ge $pages) { break }
    $page++
}
$allMods | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 (Join-Path $ApiDir 'mods-catalog.json')
Write-Output "[OK] catalog $($allMods.Count) entries -> api/mods-catalog.json"

# ---------- 3. Hot mod details + versions + source links ----------
# Source: union of mod IDs named in wiki Recommended_Mods_40 and Recommended_Mods_311
$HotIds = @(791,1109,812,1925,1945,2200,2667,2097,1594,954,2003,2521,456,2502,701,1298,910,2240,934,1621,1652,824,1425,1469,1431,2389,940,1140,1089,1342,2422,1341,2299,1283,2025,1888,865,2430,2405,1657,861,1260,933,1760,1358,1978,1117,1858,2394,2213,2470,2046,1965,1387,1437,2386,1477,1591,2523,1539,2329,1884,1274,1575,2494,1538,994,1454,2395,1867,1537,2360,236,2315,2095,2248,827,1311,2173,1159,789,1173,562,1015,592,2153,2142,1676,2148,1698,1349,551,1875,2136,2150,2250,2177,1860,707,2162,147) | Sort-Object -Unique

$GitHubRegex = 'https?://(?:www\.)?github\.com/[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+'
$index = [System.Collections.Generic.List[object]]::new()

foreach ($id in $HotIds) {
    $detailFile = Join-Path $HotDir "$id.json"
    if (-not (Test-Path $detailFile)) {
        $detail = $null
        $isAddon = $false
        try { $detail = (Get-Api "$Base/mod/$id").data } catch { }
        if (-not $detail) {
            try { $detail = (Get-Api "$Base/addon/$id").data; $isAddon = $true } catch { }
        }
        if (-not $detail) { Write-Output "[WARN] $id fetch failed (mod/addon 404)"; continue }
        $detail | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 $detailFile
    } else {
        $detail = (Get-Content $detailFile -Raw | ConvertFrom-Json)
        $isAddon = $detail.PSObject.Properties.Name -contains 'is_addon'   # not stored; detect via guid presence only
    }

    $verFile = Join-Path $HotDir "$id.versions.json"
    if (-not (Test-Path $verFile)) {
        $versions = @()
        $verPath = if ($isAddon) { "addon/$id/versions" } else { "mod/$id/versions" }
        try {
            $vpage = 1
            while ($true) {
                $vresp = Get-Api "$Base/$verPath?per_page=50&page=$vpage"
                $versions += $vresp.data
                $vtot = $vresp.meta.total
                if ($vpage -ge [Math]::Ceiling($vtot / 50)) { break }
                $vpage++
            }
        } catch { Write-Output "[WARN] $id versions fetch failed: $($_.Exception.Message)" }
        if ($versions.Count -gt 0) { $versions | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 $verFile }
    } else {
        $versions = (Get-Content $verFile -Raw | ConvertFrom-Json)
    }

    $ghLinks = @()
    if ($detail.description) {
        $ghLinks = [regex]::Matches([string]$detail.description, $GitHubRegex) |
            ForEach-Object { $_.Value.TrimEnd('/') } |
            Where-Object { $_ -match 'github\.com/[^/]+/[^/]+$' } |
            Sort-Object -Unique
    }
    $index.Add([pscustomobject]@{
        id = $id; is_addon = $isAddon; name = $detail.name; slug = $detail.slug;
        guid = $detail.guid; owner = $detail.owner.username;
        downloads = $detail.downloads; category = $detail.category.name;
        fika = $detail.fika_compatibility;
        github = ($ghLinks -join ' | ');
        version_count = if ($versions) { $versions.Count } else { 0 }
    })
    Write-Output "[OK] $id $($detail.name) versions=$($index[-1].version_count) gh=$($ghLinks.Count)"
}

$index | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 (Join-Path $Root 'hot-index.json')
Write-Output "[DONE] -> archive/forge/hot-index.json"
