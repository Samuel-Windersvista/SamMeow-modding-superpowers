# Layout invariant: the repository carries the SPT-only tree and none of the
# dead BGS/harness shape (materialized BGS plugin tree, empty harness dirs,
# BGS-era MCP manifest, BGS knowledge base).
# Recalibrated 2026-09-14: external/spt-archive was dropped from the absent
# list -- it is a gitignored, locally cloned vendored corpus backing the
# knowledge base (see .gitignore and the clonedeps skill). Its on-disk
# presence is a machine-local workflow matter, not a layout regression.

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$requiredPaths = @(
    "skills",
    "tools",
    "knowledge/spt-kb",
    "templates/server-mod",
    "templates/client-mod",
    "docs/agents",
    "tests",
    "AGENTS.md"
)

foreach ($relative in $requiredPaths) {
    Assert-PathExists -Path (Join-Path $repoRoot $relative) -Label $relative
}

$absentPaths = @(
    "plugins",
    "hooks",
    ".claude-plugin",
    ".codex-plugin",
    ".agents",
    ".mcp.json",
    "knowledge/bgs-kb"
)

foreach ($relative in $absentPaths) {
    Assert-PathAbsent -Path (Join-Path $repoRoot $relative) -Label $relative
}

$agentsPath = Join-Path $repoRoot "AGENTS.md"
if (Test-Path -LiteralPath $agentsPath) {
    $agents = Get-Content -LiteralPath $agentsPath -Raw
    Assert-ContentContains -Content $agents -Needle "## Agent skills" -Label "AGENTS.md"
}

Complete-BootstrapCheck -SuccessMessage "layout invariants hold (required paths present, dead shape absent)."
