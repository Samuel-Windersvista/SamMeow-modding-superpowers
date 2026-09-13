$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$cliPath = Join-Path $repoRoot "tools/mo2-control-plane/broker/bin/mo2-cli.ps1"
$fixturePath = Join-Path $PSScriptRoot "fixtures/fake-kernel-response.json"

$primitiveCommands = @(
    "profile.list"
    "profile.get-current"
    "profile.set-current"
    "executables.list"
    "executables.get"
    "mods.list"
    "plugins.list"
    "organizer.refresh"
    "launch.start"
    "launch.status"
    "launch.wait"
    "launch.stop"
)

function Invoke-Cli {
    param(
        [string[]]$Arguments
    )

    $output = & pwsh -NoProfile -File $cliPath @Arguments 2>&1

    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Output = ($output | ForEach-Object { $_.ToString() }) -join "`n"
    }
}

$env:MO2_CONTROL_PLANE_FAKE_RESPONSE_PATH = $fixturePath

$capabilities = Invoke-Cli -Arguments @("system", "capabilities")
if ($capabilities.ExitCode -ne 0) {
    throw "system capabilities should succeed while advertising MO2 primitive commands"
}

$capabilitiesJson = $capabilities.Output | ConvertFrom-Json -ErrorAction Stop
foreach ($primitiveCommand in $primitiveCommands) {
    if ($capabilitiesJson.result.commands -notcontains $primitiveCommand) {
        throw "system capabilities should advertise primitive command: $primitiveCommand"
    }
}

$sessionOpen = Invoke-Cli -Arguments @("session", "open")
if ($sessionOpen.ExitCode -ne 0) {
    throw "session open should succeed before routing primitive commands"
}

$sessionOpenJson = $sessionOpen.Output | ConvertFrom-Json -ErrorAction Stop
$sessionId = $sessionOpenJson.result.session_id

foreach ($primitiveCommand in $primitiveCommands) {
    $parts = $primitiveCommand.Split('.', 2)
    $arguments = @($parts[0], $parts[1], "--session-id", $sessionId)
    if ($primitiveCommand -in @("launch.status", "launch.wait", "launch.stop")) {
        $arguments += @("--launch-id", "launch-test")
    }

    $result = Invoke-Cli -Arguments $arguments

    $resultJson = $result.Output | ConvertFrom-Json -ErrorAction Stop
    if ($primitiveCommand -like "launch.*") {
        if ($result.ExitCode -eq 0) {
            throw "Primitive launch route should fail closed for $primitiveCommand without a real transport"
        }

        if ($resultJson.ok) {
            throw "Primitive launch route should return ok=false for $primitiveCommand without a real transport"
        }

        if ($resultJson.error.code -ne "transport_error") {
            throw "Primitive launch route should surface transport_error for $primitiveCommand without a real transport"
        }

        if ($resultJson.error.message -notmatch [regex]::Escape("Launch command requires fake kernel or explicit transport payload: $primitiveCommand")) {
            throw "Primitive launch route should explain the missing real transport for $primitiveCommand"
        }

        continue
    }

    if ($result.ExitCode -ne 0) {
        throw "Primitive route should succeed for $primitiveCommand`: $($result.Output)"
    }

    if (-not $resultJson.ok) {
        throw "Primitive route should return ok=true for $primitiveCommand"
    }

    if ($resultJson.result.command -ne $primitiveCommand) {
        throw "Primitive route should preserve command name for $primitiveCommand"
    }

    if ($resultJson.result.stub -ne $true) {
        throw "Primitive route should return a stub marker for $primitiveCommand"
    }
}

Write-Host "MO2 primitive contract checks passed."
