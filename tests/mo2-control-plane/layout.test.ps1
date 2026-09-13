$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$requiredPaths = @(
    "tools/mo2-control-plane/README.md",
    "tools/mo2-control-plane/broker/README.md"
)

foreach ($path in $requiredPaths) {
    if (-not (Test-Path (Join-Path $repoRoot $path))) {
        throw "Missing control-plane scaffold path: $path"
    }
}

$rootReadme = Get-Content -Path (Join-Path $repoRoot "tools/mo2-control-plane/README.md") -Raw
foreach ($phrase in @(
    "control plane",
    "broker CLI"
)) {
    if ($rootReadme -notmatch [regex]::Escape($phrase)) {
        throw "tools/mo2-control-plane/README.md is missing phrase: $phrase"
    }
}

$brokerReadme = Get-Content -Path (Join-Path $repoRoot "tools/mo2-control-plane/broker/README.md") -Raw
foreach ($phrase in @(
    "broker CLI",
    "capability discovery",
    "session/artifact"
)) {
    if ($brokerReadme -notmatch [regex]::Escape($phrase)) {
        throw "tools/mo2-control-plane/broker/README.md is missing phrase: $phrase"
    }
}

Write-Host "MO2 control-plane layout checks passed."
