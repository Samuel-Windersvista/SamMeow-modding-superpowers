# Standard compliance invariant, in three segments:
#
#   1. registry integrity -- scripts/validate-mod-standard.ps1 asserts that
#      rules.json (the machine-readable registry) and the 13 prose chapters
#      agree on ID set, title, Level and Applies;
#   2. template machine-check -- the mod templates satisfy the machine-checkable
#      subset of the Modding Standard (scripts/check-mod-standard.ps1).
#      Scaffold placeholders {{LIKE_THIS}} demote value-shape checks to SKIP,
#      which is acceptable for unscaffolded templates;
#   3. checker fixture regression -- tests/mod-standard/run-fixtures.ps1 pins the
#      checker's exit codes, FAIL ID sets and per-rule statuses.
#
# Any unwaived FAIL fails this gate.

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$checker = Join-Path $repoRoot "scripts/check-mod-standard.ps1"
$validator = Join-Path $repoRoot "scripts/validate-mod-standard.ps1"
$fixtureRunner = Join-Path $repoRoot "tests/mod-standard/run-fixtures.ps1"

# ---- 1/3 registry <-> prose --------------------------------------------------

Write-Host "  [1/3] registry validator (rules.json <-> prose) ..."

if (-not (Test-Path -LiteralPath $validator)) {
    Add-BootstrapFailure "registry validator not found: scripts/validate-mod-standard.ps1"
} else {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $validator
    if ($LASTEXITCODE -ne 0) {
        Add-BootstrapFailure "Modding Standard registry validation failed (scripts/validate-mod-standard.ps1, exit $LASTEXITCODE)"
    }
}

# ---- 2/3 template machine-check ---------------------------------------------

Write-Host "  [2/3] template machine-check ..."

$targets = @(
    @{ Path = "templates/server-mod";       Kind = "server" },
    @{ Path = "templates/client-mod";       Kind = "client" },
    @{ Path = "templates/paired-mod/Server"; Kind = "server" },
    @{ Path = "templates/paired-mod/Client"; Kind = "client" }
)

if (-not (Test-Path -LiteralPath $checker)) {
    Add-BootstrapFailure "Modding Standard checker not found: scripts/check-mod-standard.ps1"
} else {
    foreach ($t in $targets) {
        $modPath = Join-Path $repoRoot $t.Path
        Write-Host ("  checking {0} ({1}) ..." -f $t.Path, $t.Kind)
        & powershell -NoProfile -ExecutionPolicy Bypass -File $checker -ModPath $modPath -Kind $t.Kind
        if ($LASTEXITCODE -ne 0) {
            Add-BootstrapFailure "Modding Standard check failed: $($t.Path) (exit $LASTEXITCODE)"
        }
    }
}

# ---- 3/3 checker fixture regression -----------------------------------------

Write-Host "  [3/3] checker fixture regression ..."

if (-not (Test-Path -LiteralPath $fixtureRunner)) {
    Add-BootstrapFailure "fixture runner not found: tests/mod-standard/run-fixtures.ps1"
} else {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $fixtureRunner
    if ($LASTEXITCODE -ne 0) {
        Add-BootstrapFailure "Modding Standard checker fixture regression failed (tests/mod-standard/run-fixtures.ps1, exit $LASTEXITCODE)"
    }
}

Complete-BootstrapCheck -SuccessMessage "registry, templates and fixtures all satisfy the machine-checkable Modding Standard subset."
