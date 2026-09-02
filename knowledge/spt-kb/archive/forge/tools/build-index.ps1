# Rebuild hot-index.json from local JSON files (offline, no network).
# Run after snapshot.ps1 / fetch-versions.ps1 complete.
# Preserves manually-curated github fields (from Forge page Source Code blocks) when description parsing yields nothing.
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$HotDir = Join-Path $Root 'api\hot-mods'
$GitHubRegex = 'https?://(?:www\.)?github\.com/[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+'

# Load existing github values to preserve (id -> github)
$oldGithub = @{}
$oldIdxPath = Join-Path $Root 'hot-index.json'
if (Test-Path $oldIdxPath) {
    foreach ($old in (Get-Content $oldIdxPath -Raw | ConvertFrom-Json)) {
        $oldGithub[[int]$old.id] = $old.github
    }
}

$index = [System.Collections.Generic.List[object]]::new()
foreach ($detailFile in Get-ChildItem $HotDir -Filter '*.json' | Where-Object { $_.Name -notmatch '\.versions\.json$' }) {
    $detail = Get-Content $detailFile.FullName -Raw | ConvertFrom-Json
    $id = $detail.id
    $verFile = Join-Path $HotDir "$id.versions.json"
    $versions = if (Test-Path $verFile) { (Get-Content $verFile -Raw | ConvertFrom-Json) } else { @() }

    $ghLinks = @()
    if ($detail.description) {
        $ghLinks = [regex]::Matches([string]$detail.description, $GitHubRegex) |
            ForEach-Object { $_.Value.TrimEnd('/') } |
            Where-Object { $_ -match 'github\.com/[^/]+/[^/]+$' } |
            Sort-Object -Unique
    }
    # Fall back to curated value if description parse produced nothing
    if ($ghLinks.Count -eq 0 -and $oldGithub.ContainsKey([int]$id)) {
        $ghLinks = @($oldGithub[[int]$id]) | Where-Object { $_ }
    }

    # best version selection: prefer 4.1 constraint, then 4.0, else latest by published_at
    $best = $null
    if ($versions) {
        $sorted = @($versions | Sort-Object published_at -Descending)
        $best = $sorted | Where-Object { $_.spt_version_constraint -match '^4\.1' } | Select-Object -First 1
        if (-not $best) { $best = $sorted | Where-Object { $_.spt_version_constraint -match '^4\.0' } | Select-Object -First 1 }
        if (-not $best) { $best = $sorted[0] }
    }

    $index.Add([pscustomobject]@{
        id = $id; name = $detail.name; slug = $detail.slug; guid = $detail.guid;
        owner = $detail.owner.username; downloads = $detail.downloads;
        category = $detail.category.name; fika = $detail.fika_compatibility;
        github = ($ghLinks -join ' | ');
        version_count = $versions.Count;
        best_version = if ($best) { $best.version } else { $null };
        best_spt = if ($best) { $best.spt_version_constraint } else { $null };
        best_link = if ($best) { $best.link } else { $null };
        best_size = if ($best) { $best.content_length } else { $null }
    })
}
$index | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 (Join-Path $Root 'hot-index.json')
Write-Output "[DONE] hot-index.json with $($index.Count) entries"
$index | Where-Object { $_.github } | ForEach-Object { Write-Output "GH $($_.id) $($_.name): $($_.github)" } | Select-Object -First 40
