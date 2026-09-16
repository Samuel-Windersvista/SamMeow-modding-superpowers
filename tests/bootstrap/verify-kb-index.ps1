# KB index contract invariant (C7).
#
# knowledge/spt-kb/index.json is the single source spt_kb_query reads. Its shape
# (schema_version 2, entry fields) is a contract implemented in
# tools/spt-mcp/src/kb and enforced by scripts/spt-kb/validate-index.mjs against
# the built dist. A drifted index degrades agent retrieval silently -- and, in
# the pre-v2 shape, broke the version filter outright (entry.version.map is not
# a function) -- so the contract is pinned here.
#
# The contract lives in the spt-mcp dist build. A missing dist is reported with
# an explicit build hint (same convention as verify-mcp-entrypoints.ps1).

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$contractEntry = Join-Path $repoRoot "tools/spt-mcp/dist/kb/index.js"
$validateScript = Join-Path $repoRoot "scripts/spt-kb/validate-index.mjs"

if (-not (Test-Path -LiteralPath $contractEntry)) {
    Add-BootstrapFailure "missing spt-mcp KB contract build: tools/spt-mcp/dist/kb/index.js (run 'npm --prefix tools/spt-mcp run build')"
} elseif (-not (Test-Path -LiteralPath $validateScript)) {
    Add-BootstrapFailure "missing KB index validator: scripts/spt-kb/validate-index.mjs"
} elseif (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Add-BootstrapFailure "node is not on PATH; scripts/spt-kb/validate-index.mjs cannot run"
} else {
    $output = @(& node $validateScript 2>&1)
    $exitCode = $LASTEXITCODE

    foreach ($line in $output) {
        Write-Host ("    {0}" -f $line)
    }

    if ($exitCode -ne 0) {
        Add-BootstrapFailure "KB index contract check failed (node scripts/spt-kb/validate-index.mjs exit $exitCode)"
    }
}

Complete-BootstrapCheck -SuccessMessage "KB index satisfies the v2 contract (schema_version 2, entry shape)."
