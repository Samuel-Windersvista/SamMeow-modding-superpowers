# Contributing to `spt-modding-superpowers`

Thanks for the interest. This doc is for contributors opening pull requests against this repo. End users should read [README.md](README.md) and [`.opencode/INSTALL.md`](.opencode/INSTALL.md) instead.

## Clone + bootstrap

```powershell
git clone https://github.com/<owner>/SamMeow-modding-superpowers.git
cd SamMeow-modding-superpowers
# Build the MCP servers
npm --prefix tools/mo2-mcp install; npm --prefix tools/mo2-mcp run build
npm --prefix tools/spt-mcp install; npm --prefix tools/spt-mcp run build
```

The repo carries a dedicated MO2 sandbox under `.artifacts/mo2/` (gitignored — bring your own for now). The development plan and roadmap live in `docs/internal/`.

## Branch conventions

- Work on feature branches: `feat/<topic>`, `fix/<topic>`, `chore/<topic>`, `reshape/<topic>`.
- Target `main` via pull request. Do not push directly to `main` (especially do not force-push).
- Prefer multiple small commits per logical step over one large monolithic commit.
- For ambiguous or high-risk implementation work, prefer best-of-N candidate generation over single-shotting.

## Test commands

- `tools/mo2-mcp/` — `npm test` (vitest unit tests). The live acceptance suite is gated behind `MO2_MCP_ACCEPTANCE=1` and requires a running MO2.
- `tools/spt-mcp/` — `npm test` (vitest unit tests).
- `tests/` (top-level) — PowerShell suites for the shared MO2 infrastructure plus the SPT-only bootstrap invariants. Run the bootstrap suite with `powershell -NoProfile -File tests/bootstrap/verify-all.ps1`.

## Where things live

| Path | Purpose |
|---|---|
| `skills/` | Shippable agent skills (Superpowers convention). Each dir has a `SKILL.md` with YAML frontmatter. |
| `tools/mo2-mcp/` | TypeScript MCP server for the MO2 control plane. Pre-built `dist/` is tracked; `prepare` rebuilds on install. |
| `tools/spt-mcp/` | TypeScript MCP server for file-based SPT mod analysis. Pre-built `dist/` is tracked. |
| `tools/mo2-vfs-launcher/` | PowerShell launcher surface for MO2. Runtime dependency. |
| `tools/mo2-control-plane/` | C++ MO2 plugin DLL source, Python loader, broker. |
| `tools/mo2-mcp-sidecar/` | Python JSON-RPC sidecar used by the MO2 MCP. |
| `tools/mo2-assets-engine/` | Offline archive/loose-file engine. |
| `.opencode/plugins/` | OpenCode plugin entrypoint and MCP `config.mcp` wiring. |
| `scripts/` | Version bumping, portable-build, and MO2 installer scripts. |
| `docs/internal/` | Roadmap, plans, design specs, MCP specs. |
| `tests/`, `.artifacts/` | Dev-only verification scaffolding (gitignored as appropriate). |

## Pull request expectations

- Reference the issue or design doc the PR implements. Plans live under `docs/internal/superpowers/plans/`.
- Run the relevant test suite locally and report what passed.
- If the change touches shippable surfaces (skills, MCP, plugin wiring, scripts), confirm it still installs cleanly into a fresh OpenCode profile against the dev MO2 sandbox.
- Don't commit `node_modules/`, `.artifacts/` content, or other gitignored material.

## Version bumping

Versions are tracked in `package.json` and bumped by `scripts/bump-version.sh` driven by `.version-bump.json`:

```powershell
bash scripts/bump-version.sh 0.2.0
```

(Requires `jq` available on PATH.)

## Code of conduct

Be civil. Don't ship code that you wouldn't be comfortable explaining to another maintainer in person.

## License

By contributing, you agree your contributions are licensed under the MIT license. See [LICENSE](LICENSE).
