# Tools

This directory holds implementation code or automation invoked by commands or agents.

It now also hosts the MO2 control plane scaffold, split into a broker CLI and plugin kernel, plus the generic VFS launcher and xEdit outer client layer.

## Sub-packages

| Path | Purpose | Status |
|---|---|---|
| `mo2-control-plane/` | MO2-side broker CLI + agent-control plugin + Python live bridge. The broker owns `system.*` discovery and `launch.start/status/wait/stop` over a local named-pipe transport. | Foundation in place |
| `mo2-vfs-launcher/` | Generic, tool-agnostic launcher behind the MO2 `OpenCodeVfsLauncher` entrypoint, plus `xedit-client.ps1` — the canonical outer client (`process launch / status / wait / stop` + `automation call`). | Foundation in place |
| `spt-mcp/` | File-based MCP server for SPT 4.1 mod analysis: mod inventory, conflict analysis, Forge archive search, and SPT knowledge-base query. | Active |
| `mo2-mcp/` | Unified MO2 MCP server. | Active |
