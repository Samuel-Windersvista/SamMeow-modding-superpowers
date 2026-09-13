# Tests

This directory holds the SPT-only verification suites for this repository.

## Bootstrap verification

`tests/bootstrap/verify-all.ps1` is the acceptance suite for the repository's
SPT-only shape. It runs every sub-check in its own PowerShell process, so one
failing invariant never hides the others, and prints a per-check PASS/FAIL
summary. It exits non-zero while any invariant is unmet.

Run it from the repository root:

```powershell
powershell -NoProfile -File tests/bootstrap/verify-all.ps1
```

Sub-checks (one per invariant group):

| Script | Invariant group |
|---|---|
| `verify-layout.ps1` | Required SPT paths exist; the dead BGS/harness shape is absent |
| `verify-skills.ps1` | The SPT skill set is present with valid frontmatter; BGS and generic skills are gone |
| `verify-bootstrap-injection.ps1` | The OpenCode plugin entrypoint injects the SPT bootstrap, not the BGS one |
| `verify-mcp-surface.ps1` | Declared MCP servers are exactly `mo2` and `spt` |
| `verify-git-hygiene.ps1` | No build artifacts or vendored archives are tracked |
| `verify-templates.ps1` | The SPT server and client mod templates carry their scaffolds |

`_assert.ps1` is the shared assertion helper. Unlike the original suite, its
assertions accumulate failures instead of throwing on the first one;
`Complete-BootstrapCheck` prints every unmet invariant before exiting non-zero.

Individual checks stay red until the cleanup ticket that satisfies them lands.
That is expected while the SPT-only cleanup is in progress.

## Other suites

- `tests/mo2-control-plane/` and `tests/mo2-vfs-launcher/` cover the shared MO2
  infrastructure and are not part of the bootstrap suite.
