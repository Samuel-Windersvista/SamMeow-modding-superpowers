# MO2 control-plane tests

Run each `*.test.ps1` from the repository root with PowerShell 7:

```powershell
pwsh -NoProfile -File tests/mo2-control-plane/<name>.test.ps1
```

The suite targets PowerShell 7. The broker CLI invokes `pwsh` for launch
targets, and the tests use `ConvertFrom-Json -AsHashtable`; Windows PowerShell
5.1 cannot run the CLI/launch tests.

## Offline

Pure source/layout checks: `layout`, `live-bridge-layout`, `live-plan`,
`live-bootstrap-contract`, `live-endpoint-contract`, `plugin-contract`,
`primitives-contract`, `session`, `protocol`, `mo2-cli-foundation`,
`launch-flow`, `deploy-live-bridge`, `live-deploy-contract`, `live-broker-contract`,
`live-ipc-contract`, `live-ipc-runtime`, `live-bootstrap-runtime`,
`live-launch-contract`, `live-system-handler`, `live-transport-runtime`,
`live-dialog-blocker-runtime`, `live-launch-flow`.

## Live-only (opt-in)

Three harnesses touch a real MO2 install. They stay off unless
`-AllowLiveSandbox` is passed, and they require `MO2_HARNESS_ROOT` set to the
live MO2 sandbox root (the directory containing `ModOrganizer.exe`):

| Test | Requires |
|---|---|
| `live-bootstrap-real.test.ps1` | `-AllowLiveSandbox`; `$env:MO2_HARNESS_ROOT`; optional `-DeployBridge` / `-RestartMo2` |
| `live-ipc-real.test.ps1` | `-AllowLiveSandbox`; `$env:MO2_HARNESS_ROOT`; optional `-DeployBridge` / `-RestartMo2` |
| `live-ping-real.test.ps1` | `-AllowLiveSandbox`; `$env:MO2_HARNESS_ROOT`; optional `-EnsureBridge` / `-RestartMo2` |

`MO2_ROOT` is the real install root and is intentionally not used by these
sandbox harnesses. Without `-AllowLiveSandbox` they fail with an opt-in message;
with it but without `MO2_HARNESS_ROOT` they fail with instructions for setting
it.
