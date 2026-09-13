[CmdletBinding()]
param(
  # Owner must supply MO2_ROOT: the live MO2 install root (the directory
  # containing ModOrganizer.exe). No BGS-era game path is baked in here.
  [string]$Mo2Root = $env:MO2_ROOT,
  # Owner must supply MO2_PROFILE (falls back to the MO2 default profile name).
  [string]$Profile = $(if ($env:MO2_PROFILE) { $env:MO2_PROFILE } else { "Default" }),
  [ValidateSet("all", "live", "closed")] [string]$Mode = "all"
)

if ([string]::IsNullOrWhiteSpace($Mo2Root)) {
  throw "MO2_ROOT is not set. Set it to the live MO2 install root (the directory containing ModOrganizer.exe) before running this acceptance script."
}

# The live suite also drives a second MO2 sandbox root (the dev harness).
# Owner must supply MO2_HARNESS_ROOT; no BGS-era sandbox path is baked in.
$HarnessRoot = $env:MO2_HARNESS_ROOT
if ([string]::IsNullOrWhiteSpace($HarnessRoot)) {
  throw "MO2_HARNESS_ROOT is not set. Set it to the live MO2 sandbox root used by the harness acceptance suite."
}

function Ensure-Mo2Alive {
  param([string]$Root)
  $existing = Get-Process -Name "ModOrganizer*" -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -like "$Root\*" }
  if (-not $existing) {
    Write-Host "[acceptance] Starting MO2 at $Root..."
    # Multiple MO2 launchers (different roots) can coexist; FO4 single-instance
    # only constrains the game runtime, not MO2 GUI processes.
    Start-Process -FilePath "$Root\ModOrganizer.exe" -WorkingDirectory $Root
    Start-Sleep -Seconds 25
  } else {
    Write-Host "[acceptance] MO2 at $Root already alive (PID $($existing.Id -join ','))"
  }
}

function Stop-AllMo2 {
  $procs = Get-Process -Name "ModOrganizer*" -ErrorAction SilentlyContinue
  if ($procs) {
    Write-Host "[acceptance] Stopping MO2 processes (PID $($procs.Id -join ','))..."
    $procs | Stop-Process -Force
    Start-Sleep -Seconds 5
  } else {
    Write-Host "[acceptance] No MO2 processes to stop"
  }
}

$env:MO2_MCP_ACCEPTANCE = "1"
$env:MO2_ROOT = $Mo2Root
$env:MO2_PROFILE = $Profile
$env:MO2_ACCEPTANCE_PROJECT_ROOT = (Resolve-Path "$PSScriptRoot\..").Path

Push-Location "$PSScriptRoot\..\tools\mo2-mcp"
try {
  npm run build
  if ($LASTEXITCODE -ne 0) { throw "build failed" }

  $liveExit = 0
  $closedExit = 0

  if ($Mode -in @("all", "live")) {
    Write-Host ""
    Write-Host "=== Phase: LIVE suite ==="
    # Live suite mixes realEnv (MO2_ROOT) tests and harnessEnv (MO2_HARNESS_ROOT)
    # tests in the same vitest run -- both MO2 launchers must be alive simultaneously.
    Ensure-Mo2Alive -Root $Mo2Root
    Ensure-Mo2Alive -Root $HarnessRoot
    npx vitest run tests/acceptance-live.test.ts
    $liveExit = $LASTEXITCODE
  }

  if ($Mode -in @("all", "closed")) {
    Write-Host ""
    Write-Host "=== Phase: CLOSED suite ==="
    Stop-AllMo2
    npx vitest run tests/acceptance-closed.test.ts
    $closedExit = $LASTEXITCODE
  }

  Write-Host ""
  Write-Host "=== Summary ==="
  if ($Mode -in @("all", "live")) { Write-Host "  Live   suite exit: $liveExit" }
  if ($Mode -in @("all", "closed")) { Write-Host "  Closed suite exit: $closedExit" }

  if ($liveExit -ne 0 -or $closedExit -ne 0) { exit 1 }
}
finally {
  Pop-Location
  Remove-Item env:MO2_MCP_ACCEPTANCE -ErrorAction SilentlyContinue
  Remove-Item env:MO2_ROOT -ErrorAction SilentlyContinue
  Remove-Item env:MO2_PROFILE -ErrorAction SilentlyContinue
  Remove-Item env:MO2_ACCEPTANCE_PROJECT_ROOT -ErrorAction SilentlyContinue
}
