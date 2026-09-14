# Shared assertion helpers for the SPT-only bootstrap verification suite.
#
# The original verify-*.ps1 scripts threw on the first missing path, so a single
# run reported only one problem. These helpers accumulate failures instead. Each
# sub-check dot-sources this file and finishes with Complete-BootstrapCheck,
# which prints every unmet invariant and sets the process exit code.

$script:BootstrapFailures = New-Object System.Collections.Generic.List[string]

function Add-BootstrapFailure {
    param([string]$Message)

    $script:BootstrapFailures.Add($Message)
}

function Assert-PathExists {
    param(
        [string]$Path,
        [string]$Label = $Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        Add-BootstrapFailure "missing required path: $Label"
    }
}

function Assert-PathAbsent {
    param(
        [string]$Path,
        [string]$Label = $Path
    )

    if (Test-Path -LiteralPath $Path) {
        Add-BootstrapFailure "path should be absent but exists: $Label"
    }
}

function Assert-ContentContains {
    param(
        [string]$Content,
        [string]$Needle,
        [string]$Label
    )

    if ($Content -notmatch [regex]::Escape($Needle)) {
        Add-BootstrapFailure "$Label is missing required text: $Needle"
    }
}

function Assert-PathNotTracked {
    param(
        [string]$RepoRoot,
        [string]$Path,
        [string]$Label = $Path
    )

    $tracked = @(git -C $RepoRoot ls-files -- $Path)
    if ($tracked.Count -gt 0) {
        Add-BootstrapFailure "$Label is tracked by git but must stay harness-runtime-local ($($tracked.Count) tracked file(s), first: $($tracked[0]))"
    }
}

function Assert-ContentNotContains {    param(
        [string]$Content,
        [string]$Needle,
        [string]$Label
    )

    if ($Content -match [regex]::Escape($Needle)) {
        Add-BootstrapFailure "$Label contains forbidden text: $Needle"
    }
}

function Complete-BootstrapCheck {
    param([string]$SuccessMessage)

    if ($script:BootstrapFailures.Count -gt 0) {
        Write-Host ("  {0} unmet invariant(s):" -f $script:BootstrapFailures.Count)
        foreach ($failure in $script:BootstrapFailures) {
            Write-Host ("    FAIL: {0}" -f $failure)
        }
        exit 1
    }

    Write-Host ("  {0}" -f $SuccessMessage)
    exit 0
}
