# Fetch versions for hot mods that don't have .versions.json yet.
# Slower pacing (3s) because the versions endpoint throttles hard.
# Resumable: skips ids that already have a versions file.
param(
    [int]$MaxIds = 25
)

$ErrorActionPreference = 'Stop'
$UA = 'SPT-knowledge-rescue/1.0 (archival backup; contact: local)'
$Headers = @{ 'User-Agent' = $UA }
$Base = 'https://forge.sp-tarkov.com/api/v0'
$HotDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'api\hot-mods'

function Get-Api([string]$Uri) {
    Start-Sleep -Seconds 3
    $attempt = 0
    while ($true) {
        try { return Invoke-RestMethod -Uri $Uri -Headers $Headers }
        catch {
            $code = $_.Exception.Response.StatusCode.value__
            $attempt++
            if ($code -in @(403, 429) -and $attempt -le 8) {
                $wait = [Math]::Min(60, 10 * $attempt)
                Write-Output "[THROTTLE] $Uri (code $code) retry $attempt after ${wait}s"
                Start-Sleep -Seconds $wait
            } else { throw }
        }
    }
}

$ids = Get-ChildItem $HotDir -Filter '*.json' |
    Where-Object { $_.Name -notmatch '\.versions\.json$' } |
    ForEach-Object { [int]($_.BaseName) } | Sort-Object -Unique |
    Where-Object { -not (Test-Path (Join-Path $HotDir "$_.versions.json")) } |
    Select-Object -First $MaxIds

Write-Output "pending ids: $($ids -join ',')"
foreach ($id in $ids) {
    try {
        $vpage = 1
        $versions = @()
        while ($true) {
            $vresp = Get-Api "$Base/mod/$id/versions?per_page=50&page=$vpage"
            $versions += $vresp.data
            $vtot = $vresp.meta.total
            if ($vpage -ge [Math]::Ceiling($vtot / 50)) { break }
            $vpage++
        }
        $versions | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 (Join-Path $HotDir "$id.versions.json")
        Write-Output "[OK] $id versions=$($versions.Count)"
    } catch {
        Write-Output "[FAIL] $id : $($_.Exception.Message)"
    }
}
