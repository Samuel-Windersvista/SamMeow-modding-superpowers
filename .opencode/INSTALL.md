# Installing `spt-modding-superpowers` in OpenCode

This plugin is OpenCode-only. Add it to the `plugin` array in your
`opencode.json` (global or project-level). For a local checkout, point at the
absolute path of the clone (forward slashes work on Windows):

```json
{
  "plugin": ["file:E:/云文件/GitHub/SamMeow-modding-superpowers"]
}
```

Or install straight from git once the package is published:

```json
{
  "plugin": ["spt-modding-superpowers@git+https://github.com/Samuel-Windersvista/SamMeow-modding-superpowers.git"]
}
```

Restart OpenCode. OpenCode reads the package's `main` entry
(`.opencode/plugins/spt-modding-superpowers.js`), which:

- appends the repo-root `skills/` directory to `config.skills.paths`, so all 15
  SPT skills (including the per-session bootstrap) are discovered;
- registers three local stdio MCP servers via the `config.mcp` hook:
  - `mo2` (`tools/mo2-mcp/dist/index.js`) — MO2 control plane;
  - `spt` (`tools/spt-mcp/dist/index.js`) — file-based SPT mod analysis;
  - `tarkov` (`tools/tarkov-runtime-mcp/dist/index.js`) — SPT 5.x runtime
    state: server handshake/version gate, in-raid bridge, log aggregation;
- injects the `using-spt-modding-superpowers` bootstrap into the first user
  message of every session.

The `spt` server and the plugin resolve the knowledge base and the optional
.NET helper CLIs through the single resolver in `shared/runtime-layout.mjs`
(`SPT_KB_ROOT` / `SPT_MCP_HELPER` / `SPT_IL_HELPER` override the defaults; a
portable tree derives them from its package root). An explicitly-set but invalid
path is reported as an error and never silently falls back -- KB lookups then
return `kb_unavailable`. The helpers default to `tools/spt-mcp/helper/` and
`tools/spt-mcp/il-helper/`.

## Prerequisites

- Windows (the MO2 control plane is Windows-only).
- Node 22+ for the three MCP servers.

The MCP `dist/` bundles are build output (not tracked). After cloning, run
`npm install` in each package — `prepare` builds `dist/` automatically. Build
the shared kernel first; the three servers import it from `tools/mcp-kit/dist`:

```powershell
npm --prefix tools/mcp-kit install; npm --prefix tools/mcp-kit run build
npm --prefix tools/mo2-mcp install; npm --prefix tools/mo2-mcp run build
npm --prefix tools/spt-mcp install; npm --prefix tools/spt-mcp run build
npm --prefix tools/tarkov-runtime-mcp install; npm --prefix tools/tarkov-runtime-mcp run build
```

Optional .NET helper CLIs (server DLL metadata + client IL analysis):

```powershell
dotnet build tools/spt-mcp/helper -c Release
dotnet build tools/spt-mcp/il-helper -c Release
```

## Verify

Start a new OpenCode session and ask:

> Tell me about your SPT modding superpowers.

The agent should reference the `using-spt-modding-superpowers` skill and offer to run `setting-up-spt-modding-environment` if MO2 / SPT haven't been detected yet.

## Version pinning

```json
{
  "plugin": ["spt-modding-superpowers@git+https://github.com/Samuel-Windersvista/SamMeow-modding-superpowers.git#v0.2.0"]
}
```

## Windows fallback (npm-managed local install)

If the direct git install path is slow or unreliable on Windows, install into your global OpenCode tree first:

```powershell
npm install spt-modding-superpowers@git+https://github.com/Samuel-Windersvista/SamMeow-modding-superpowers.git --prefix "$HOME\.config\opencode"
```

Then point `opencode.json` at the local path:

```json
{
  "plugin": ["~/.config/opencode/node_modules/spt-modding-superpowers"]
}
```

## Local development checkout

If you cloned this repo and want to run the plugin from your local checkout, point `opencode.json` at the absolute path of the clone:

```json
{
  "plugin": ["file:/path/to/your/SamMeow-modding-superpowers/checkout"]
}
```

The plugin resolves its skills and MCP entries relative to the repo root, so a
plain clone plus the per-package `npm install` step (see Prerequisites) is
enough. After editing the TypeScript sources under `tools/<package>/src/`,
rebuild the affected package — if you edited `tools/mcp-kit/`, rebuild it first
and then the affected servers. To materialize a hand-distributable copy:

```powershell
pwsh scripts/build-portable-plugin.ps1 -Force
```

That writes `dist/portable-plugin/spt-modding-superpowers/` plus a
`marketplace.json` (the script's defaults for `-OutputDir` and `-PluginName`).
Pass `-OutputDir <dir>` to change the destination.
