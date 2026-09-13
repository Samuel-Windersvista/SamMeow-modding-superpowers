# MCP entrypoint invariant.
#
# The OpenCode plugin declares the mo2 and spt MCP servers with a command that
# points at tools/<server>/dist/index.js. Those dist trees are build output and
# are deliberately NOT tracked, so a fresh clone must run `npm install` (which
# builds via each package's `prepare` script) before the servers can start.
#
# A missing entrypoint is a silent MCP outage — exactly the failure this check
# exists to catch.

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$entries = @(
    "tools/mo2-mcp/dist/index.js",
    "tools/spt-mcp/dist/index.js"
)

foreach ($entry in $entries) {
    $path = Join-Path $repoRoot $entry
    if (-not (Test-Path -LiteralPath $path)) {
        $serverDir = Split-Path -Parent (Split-Path -Parent $entry)
        Add-BootstrapFailure "missing MCP entrypoint: $entry (run 'npm install' in $serverDir)"
    }
}

# Both servers must build on `npm install`, otherwise a fresh clone silently
# loses its MCP surface until someone remembers to run the build by hand.
foreach ($manifest in @("tools/mo2-mcp/package.json", "tools/spt-mcp/package.json")) {
    $manifestPath = Join-Path $repoRoot $manifest
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        Add-BootstrapFailure "missing MCP package manifest: $manifest"
        continue
    }

    $package = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $prepare = $null
    if ($null -ne $package.scripts) {
        $prepare = $package.scripts.prepare
    }

    if ([string]::IsNullOrWhiteSpace([string]$prepare)) {
        Add-BootstrapFailure "$manifest has no 'prepare' script, so 'npm install' will not build dist"
    }
}

# Build output must not be tracked, or the repo re-accretes the bloat the
# SPT-only cleanup removed.
$trackedDist = @(git -C $repoRoot ls-files "tools/mo2-mcp/dist" "tools/spt-mcp/dist" 2>$null)
if ($trackedDist.Count -gt 0) {
    Add-BootstrapFailure "MCP dist trees must stay untracked: $($trackedDist.Count) tracked path(s), e.g. $($trackedDist[0])"
}

Complete-BootstrapCheck -SuccessMessage "MCP entrypoints present and buildable; dist stays untracked."
