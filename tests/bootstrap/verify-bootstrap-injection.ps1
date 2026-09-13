# Bootstrap injection invariant: the OpenCode plugin entrypoint exists under its
# SPT name, injects the SPT bootstrap skill, and carries no BGS marker or
# BGS_-prefixed environment variable.

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$pluginRelative = ".opencode/plugins/spt-modding-superpowers.js"
$pluginPath = Join-Path $repoRoot $pluginRelative

if (-not (Test-Path -LiteralPath $pluginPath)) {
    Add-BootstrapFailure "missing OpenCode plugin entrypoint: $pluginRelative"
} else {
    $plugin = Get-Content -LiteralPath $pluginPath -Raw

    Assert-ContentContains -Content $plugin -Needle "using-spt-modding-superpowers" -Label $pluginRelative
    Assert-ContentNotContains -Content $plugin -Needle "BGS_MODDING_SUPERPOWERS" -Label $pluginRelative
    Assert-ContentNotContains -Content $plugin -Needle "bgs-modding-superpowers" -Label $pluginRelative
    # The plugin must not inject any BGS_-prefixed environment variable: the
    # tools now read the unprefixed names (MO2_ROOT, SPT_KB_ROOT, ...).
    Assert-ContentNotContains -Content $plugin -Needle "BGS_" -Label $pluginRelative
}

Complete-BootstrapCheck -SuccessMessage "bootstrap injection targets the SPT marker only."
