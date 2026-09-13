# SPT-only bootstrap verification entrypoint.
#
# Runs every sub-check in its own PowerShell process so one failing invariant
# never hides the others, prints a per-check PASS/FAIL summary, and exits
# non-zero if any check failed. Individual checks stay red until the cleanup
# ticket that satisfies them lands.

$ErrorActionPreference = "Continue"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$checks = @(
    "tests/bootstrap/verify-layout.ps1",
    "tests/bootstrap/verify-skills.ps1",
    "tests/bootstrap/verify-bootstrap-injection.ps1",
    "tests/bootstrap/verify-mcp-surface.ps1",
    "tests/bootstrap/verify-mcp-entrypoints.ps1",
    "tests/bootstrap/verify-git-hygiene.ps1",
    "tests/bootstrap/verify-templates.ps1"
)

Write-Host "============================================"
Write-Host "SPT-only bootstrap verification"
Write-Host "Repository: $repoRoot"
Write-Host "============================================"

$results = @()

foreach ($check in $checks) {
    $scriptPath = Join-Path $repoRoot $check
    $name = [System.IO.Path]::GetFileNameWithoutExtension($check)

    Write-Host ""
    Write-Host ("--- {0} ---" -f $check)

    $output = @()
    $exitCode = 1

    if (-not (Test-Path -LiteralPath $scriptPath)) {
        $output = @("sub-check script not found: $check")
    } else {
        try {
            $output = @(& powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath 2>&1)
            $exitCode = $LASTEXITCODE
        } catch {
            $output = @("failed to invoke sub-check: $($_.Exception.Message)")
            $exitCode = 1
        }
    }

    foreach ($line in $output) {
        Write-Host ("    {0}" -f $line)
    }

    $results += [pscustomobject]@{
        Name     = $name
        Passed   = ($exitCode -eq 0)
        ExitCode = $exitCode
    }
}

Write-Host ""
Write-Host "============================================"
Write-Host "Bootstrap verification summary"
Write-Host "============================================"

foreach ($result in $results) {
    $label = if ($result.Passed) { "PASS" } else { "FAIL" }
    Write-Host ("  [{0}] {1}" -f $label, $result.Name)
}

$failed = @($results | Where-Object { -not $_.Passed })

Write-Host ""
if ($failed.Count -eq 0) {
    Write-Host ("All {0} bootstrap checks passed." -f $results.Count)
    exit 0
}

$failedNames = ($failed | ForEach-Object { $_.Name }) -join ", "
Write-Host ("{0} of {1} bootstrap checks FAILED: {2}" -f $failed.Count, $results.Count, $failedNames)
exit 1
