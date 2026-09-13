# Git hygiene invariant: the index carries no .NET build artifacts and no
# vendored archives (the SPT reference archive or the Forge mod archives).

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$patterns = [ordered]@{
    "tracked .NET build artifact (obj)" = "*/obj/*"
    "tracked .NET build artifact (bin)" = "*/bin/*"
    "tracked vendored SPT archive"      = "external/spt-archive/*"
    "tracked Forge mod archive"         = "knowledge/spt-kb/archive/*"
}

$tracked = @(& git -C $repoRoot ls-files)

if ($LASTEXITCODE -ne 0) {
    Add-BootstrapFailure "git ls-files failed in $repoRoot (exit $LASTEXITCODE)"
} else {
    foreach ($entry in $patterns.GetEnumerator()) {
        $hits = @($tracked | Where-Object { $_ -like $entry.Value })
        if ($hits.Count -gt 0) {
            $sample = ($hits | Select-Object -First 3) -join "; "
            Add-BootstrapFailure ("{0}: {1} tracked path(s) match '{2}' (e.g. {3})" -f $entry.Key, $hits.Count, $entry.Value, $sample)
        }
    }
}

Complete-BootstrapCheck -SuccessMessage "git index carries no build artifacts or vendored archives."
