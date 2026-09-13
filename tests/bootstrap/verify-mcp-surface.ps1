# MCP declaration invariant: the declared MCP servers are exactly mo2 and spt.
# The OpenCode plugin's config.mcp hook is the canonical surface; a static
# .mcp.json, if it still exists, must agree. xedit and bgs_kb must be absent.

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$pluginRelative = ".opencode/plugins/spt-modding-superpowers.js"
$pluginPath = Join-Path $repoRoot $pluginRelative

$requiredServers = @("mo2", "spt")
$forbiddenServers = @("xedit", "bgs_kb")

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
}

$staticMcpPath = Join-Path $repoRoot ".mcp.json"
if (Test-Path -LiteralPath $staticMcpPath) {
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

Complete-BootstrapCheck -SuccessMessage "MCP declaration surface is exactly mo2 + spt."
