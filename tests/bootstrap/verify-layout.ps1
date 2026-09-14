# Layout invariant: the repository carries the SPT-only tree and none of the
# dead BGS/harness shape is COMMITTED (materialized BGS plugin tree, harness
# manifests, BGS-era MCP manifest, BGS knowledge base).
# Recalibrated 2026-09-14:
# - external/spt-archive was dropped entirely -- it is a gitignored, locally
#   cloned vendored corpus backing the knowledge base (see .gitignore and the
#   clonedeps skill); its on-disk presence is a machine-local workflow matter.
# - plugins/ hooks/ .claude-plugin/ .codex-plugin/ .agents/ .mcp.json are
#   materialized at the repo root by the OpenCode plugin on every session
#   start. The invariant for them is "never tracked by git" (checked via
#   git ls-files), not "absent from disk" -- deletion does not survive a
#   running harness, which is expected, not a regression.

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

# Harness-runtime paths: regenerated at the repo root by the OpenCode plugin
# on every session start. The invariant is "never tracked by git".
$runtimePaths = @(
    "plugins",
    "hooks",
    ".claude-plugin",
    ".codex-plugin",
    ".agents",
    ".mcp.json"
)

foreach ($relative in $runtimePaths) {
    Assert-PathNotTracked -RepoRoot $repoRoot -Path $relative -Label $relative
}

$absentPaths = @(
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
