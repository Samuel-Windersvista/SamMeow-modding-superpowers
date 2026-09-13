$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$liveSandboxHelperPath = Join-Path $repoRoot "tests/mo2-control-plane/live-sandbox.ps1"
$liveBootstrapRealTestPath = Join-Path $repoRoot "tests/mo2-control-plane/live-bootstrap-real.test.ps1"
$livePingRealTestPath = Join-Path $repoRoot "tests/mo2-control-plane/live-ping-real.test.ps1"
$liveIpcRealTestPath = Join-Path $repoRoot "tests/mo2-control-plane/live-ipc-real.test.ps1"

if (-not (Test-Path $liveBootstrapRealTestPath -PathType Leaf)) {
    throw "Missing real bootstrap harness: tests/mo2-control-plane/live-bootstrap-real.test.ps1"
}

if (-not (Test-Path $livePingRealTestPath -PathType Leaf)) {
    throw "Missing real ping harness: tests/mo2-control-plane/live-ping-real.test.ps1"
}

if (-not (Test-Path $liveSandboxHelperPath -PathType Leaf)) {
    throw "Missing live sandbox helper: tests/mo2-control-plane/live-sandbox.ps1"
}

if (-not (Test-Path $liveIpcRealTestPath -PathType Leaf)) {
    throw "Missing real IPC harness: tests/mo2-control-plane/live-ipc-real.test.ps1"
}

$liveSandboxHelper = Get-Content -Path $liveSandboxHelperPath -Raw
$liveBootstrapRealTest = Get-Content -Path $liveBootstrapRealTestPath -Raw
$livePingRealTest = Get-Content -Path $livePingRealTestPath -Raw
$liveIpcRealTest = Get-Content -Path $liveIpcRealTestPath -Raw

foreach ($script in @(
    @{ Path = 'tests/mo2-control-plane/live-bootstrap-real.test.ps1'; Content = $liveBootstrapRealTest },
    @{ Path = 'tests/mo2-control-plane/live-ping-real.test.ps1'; Content = $livePingRealTest },
    @{ Path = 'tests/mo2-control-plane/live-ipc-real.test.ps1'; Content = $liveIpcRealTest }
)) {
    foreach ($phrase in @(
    'live-sandbox.ps1',
    'Enter-SandboxHarnessLock',
    'Start-Process -FilePath $mo2ExecutablePath -PassThru'
    )) {
        if ($script.Content -notmatch [regex]::Escape($phrase)) {
            throw "$($script.Path) is missing phrase: $phrase"
        }
    }
}

foreach ($helperPhrase in @(
    'function Get-ProcessFromPath',
    'function Stop-SandboxMo2FromPath',
    'function New-SandboxHarnessMutexName',
    'function Enter-SandboxHarnessLock',
    'Global\Mo2ControlPlaneLiveIpc_'
)) {
    if ($liveSandboxHelper -notmatch [regex]::Escape($helperPhrase)) {
        throw "tests/mo2-control-plane/live-sandbox.ps1 is missing phrase: $helperPhrase"
    }
}

if ($liveBootstrapRealTest -notmatch [regex]::Escape('if ($BaselineLineCount -ge $allLines.Count) {')) {
    throw 'tests/mo2-control-plane/live-bootstrap-real.test.ps1 should guard against stale mo_interface.log slices'
}

if ($liveBootstrapRealTest -notmatch [regex]::Escape('return @()')) {
    throw 'tests/mo2-control-plane/live-bootstrap-real.test.ps1 should return no fresh log entries when no new lines were appended'
}

if ($liveBootstrapRealTest -match [regex]::Escape('Get-Process -Name "ModOrganizer"')) {
    throw 'tests/mo2-control-plane/live-bootstrap-real.test.ps1 should not stop every ModOrganizer process by name'
}

if ($livePingRealTest -match [regex]::Escape('Get-Process -Name "ModOrganizer"')) {
    throw 'tests/mo2-control-plane/live-ping-real.test.ps1 should not stop every ModOrganizer process by name'
}

Write-Host "MO2 live harness contract checks passed."
