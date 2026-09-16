# Layout invariant: the repository carries the SPT-only tree and none of the
# dead BGS/harness shape (materialized BGS plugin tree, harness manifests,
# BGS-era MCP manifest, BGS knowledge base).
#
# Recalibrated 2026-09-16 (C4 session-wiring contract):
# - The OpenCode-only session-wiring contract materializes NOTHING at the repo
#   root. The retired harness paths -- plugins/ hooks/ .claude-plugin/
#   .codex-plugin/ .agents/ .mcp.json -- are asserted ABSENT, not merely
#   untracked. If any of them reappears on disk, this check fails: that is the
#   intended signal (a regression to the old harness shape), not a false alarm.
# - external/spt-archive was dropped from the tree entirely -- it is a
#   gitignored, locally cloned vendored corpus backing the knowledge base (see
#   .gitignore and the clonedeps skill); its on-disk presence is a
#   machine-local workflow matter.

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

# Retired harness-runtime paths: the OpenCode-only contract never writes these
# at the repo root, so their reappearance is a contract violation.
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
