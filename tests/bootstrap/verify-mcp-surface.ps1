# MCP declaration invariant: the declared MCP servers are exactly mo2, spt and
# tarkov. The OpenCode plugin's config.mcp hook is the canonical surface; a
# static .mcp.json, if one is ever re-introduced and tracked, must agree.
# xedit and bgs_kb must be absent.
#
# Each declared server must also have a built stdio entrypoint on disk -- a
# declared-but-missing entry is a silent MCP outage (same failure mode as
# verify-mcp-entrypoints.ps1, which covers the mo2/spt dist trees).

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$pluginRelative = ".opencode/plugins/spt-modding-superpowers.js"
$pluginPath = Join-Path $repoRoot $pluginRelative

$requiredServers = @("mo2", "spt", "tarkov")
$forbiddenServers = @("xedit", "bgs_kb")

$serverEntries = @{
    "mo2"    = "tools/mo2-mcp/dist/index.js"
    "spt"    = "tools/spt-mcp/dist/index.js"
    "tarkov" = "tools/tarkov-runtime-mcp/dist/index.js"
}

function Assert-McpServerSet {
    param(
        [string[]]$Declared,
        [string]$Label
    )

    $unique = @($Declared | Sort-Object -Unique)

    foreach ($server in $requiredServers) {
        if ($unique -notcontains $server) {
            Add-BootstrapFailure "$Label does not declare required MCP server '$server'"
        }
    }

    foreach ($server in $forbiddenServers) {
        if ($unique -contains $server) {
            Add-BootstrapFailure "$Label still declares forbidden MCP server '$server'"
        }
    }

    foreach ($server in $unique) {
        if ($requiredServers -notcontains $server -and $forbiddenServers -notcontains $server) {
            Add-BootstrapFailure "$Label declares unexpected MCP server '$server' (expected only: $($requiredServers -join ', '))"
        }
    }
}

if (-not (Test-Path -LiteralPath $pluginPath)) {
    Add-BootstrapFailure "missing OpenCode plugin entrypoint: $pluginRelative"
} else {
    $plugin = Get-Content -LiteralPath $pluginPath -Raw

    # Match only assignment forms (config.mcp.NAME = ... / ??= ...) so that prose
    # comments mentioning config.mcp.<server> are not counted as declarations.
    $declared = @(
        [regex]::Matches($plugin, "config\.mcp\.([A-Za-z_][A-Za-z0-9_]*)\s*(?:\?\?=|=)") |
            ForEach-Object { $_.Groups[1].Value }
    )

    Assert-McpServerSet -Declared $declared -Label "OpenCode plugin config.mcp hook"

    foreach ($server in $requiredServers) {
        $entry = $serverEntries[$server]
        $entryPath = Join-Path $repoRoot $entry
        if (-not (Test-Path -LiteralPath $entryPath)) {
            $serverDir = Split-Path -Parent (Split-Path -Parent $entry)
            Add-BootstrapFailure "MCP server '$server' is declared but its entrypoint is missing: $entry (run 'npm install' in $serverDir)"
        }
    }
}

# .mcp.json is retired harness state: the OpenCode-only session-wiring contract
# materializes nothing at the repo root, and verify-layout.ps1 asserts this path
# absent. If one is ever re-introduced AND tracked by git, it must agree with
# the plugin's config.mcp hook (the canonical declaration surface).
$staticMcpPath = Join-Path $repoRoot ".mcp.json"
$staticTracked = @(git -C $repoRoot ls-files -- ".mcp.json")
if ((Test-Path -LiteralPath $staticMcpPath) -and $staticTracked.Count -gt 0) {
    try {
        $static = Get-Content -LiteralPath $staticMcpPath -Raw | ConvertFrom-Json
        $staticServers = @()
        if ($null -ne $static.mcpServers) {
            $staticServers = @($static.mcpServers.PSObject.Properties.Name)
        }
        Assert-McpServerSet -Declared $staticServers -Label ".mcp.json"
    } catch {
        Add-BootstrapFailure ".mcp.json is present but could not be parsed as JSON: $($_.Exception.Message)"
    }
}

Complete-BootstrapCheck -SuccessMessage "MCP declaration surface is exactly mo2 + spt + tarkov, with all entrypoints present."
