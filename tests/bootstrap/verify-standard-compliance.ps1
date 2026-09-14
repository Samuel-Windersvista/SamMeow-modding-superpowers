# Standard compliance invariant: the mod templates satisfy the machine-checkable
# subset of the Modding Standard (phase-2 checker, scripts/check-mod-standard.ps1).
# Scaffold placeholders {{LIKE_THIS}} demote value-shape checks to SKIP, which is
# acceptable for unscaffolded templates; any unwaived FAIL fails this gate.

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$checker = Join-Path $repoRoot "scripts/check-mod-standard.ps1"

$targets = @(
    @{ Path = "templates/server-mod";       Kind = "server" },
    @{ Path = "templates/client-mod";       Kind = "client" },
    @{ Path = "templates/paired-mod/Server"; Kind = "server" },
    @{ Path = "templates/paired-mod/Client"; Kind = "client" }
)

foreach ($t in $targets) {
    $modPath = Join-Path $repoRoot $t.Path
    Write-Host ("  checking {0} ({1}) ..." -f $t.Path, $t.Kind)
    & powershell -NoProfile -ExecutionPolicy Bypass -File $checker -ModPath $modPath -Kind $t.Kind
    if ($LASTEXITCODE -ne 0) {
        Add-BootstrapFailure "Modding Standard check failed: $($t.Path)"
    }
}

Complete-BootstrapCheck -SuccessMessage "all templates satisfy the machine-checkable Modding Standard subset."
