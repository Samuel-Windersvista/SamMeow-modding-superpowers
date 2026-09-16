# MCP entrypoint invariant.
#
# The OpenCode plugin declares the mo2, spt and tarkov MCP servers with a
# command that points at tools/<server>/dist/index.js. Those dist trees are
# build output and are deliberately NOT tracked, so a fresh clone must run
# `npm install` (which builds via each package's `prepare` script) before the
# servers can start. The shared kernel tools/mcp-kit is NOT an MCP server, but
# the three servers import ../../mcp-kit/dist/index.js relatively, so a missing
# kit build is the same outage.
#
# A missing entrypoint is a silent MCP outage — exactly the failure this check
# exists to catch.

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$entries = @(
    "tools/mo2-mcp/dist/index.js",
    "tools/spt-mcp/dist/index.js",
    "tools/tarkov-runtime-mcp/dist/index.js",
    "tools/mcp-kit/dist/index.js"
)

foreach ($entry in $entries) {
    $path = Join-Path $repoRoot $entry
    if (-not (Test-Path -LiteralPath $path)) {
        $packageDir = Split-Path -Parent (Split-Path -Parent $entry)
        if ($entry -like "tools/mcp-kit/*") {
            # Shared library, not an MCP server: distinguish the failure so the
            # operator knows one missing build takes all three servers down.
            Add-BootstrapFailure "missing shared kernel build: $entry (run 'npm install' in $packageDir); mo2/spt/tarkov import it via ../../mcp-kit/dist/index.js"
        } else {
            Add-BootstrapFailure "missing MCP entrypoint: $entry (run 'npm install' in $packageDir)"
        }
    }
}

# All servers must build on `npm install`, otherwise a fresh clone silently
# loses its MCP surface until someone remembers to run the build by hand. The
# kit is included: its `prepare` script is what produces the shared dist.
foreach ($manifest in @(
    "tools/mo2-mcp/package.json",
    "tools/spt-mcp/package.json",
    "tools/tarkov-runtime-mcp/package.json",
    "tools/mcp-kit/package.json"
)) {
    $manifestPath = Join-Path $repoRoot $manifest
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        Add-BootstrapFailure "missing package manifest: $manifest"
        continue
    }

    # Read as UTF-8 explicitly: package.json files may carry CJK text (e.g.
    # tools/mcp-kit's description), and Windows PowerShell 5.1 would otherwise
    # decode the raw bytes with the console ANSI code page and fail the parse.
    $package = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
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
$trackedDist = @(git -C $repoRoot ls-files "tools/mo2-mcp/dist" "tools/spt-mcp/dist" "tools/tarkov-runtime-mcp/dist" "tools/mcp-kit/dist" 2>$null)
if ($trackedDist.Count -gt 0) {
    Add-BootstrapFailure "dist trees must stay untracked: $($trackedDist.Count) tracked path(s), e.g. $($trackedDist[0])"
}

Complete-BootstrapCheck -SuccessMessage "MCP entrypoints + shared kernel present and buildable; dist stays untracked."
