# Pre-cleanup baseline

Recorded before any deletion. Recovery tag: `archive/bgs-final`. Working branch: `chore/spt-only-cleanup`.

## Tracked file count

49,535

## Top-level tracked distribution

| Path | Tracked files |
| ---- | ------------- |
| `external/spt-archive` | 21,370 |
| `plugins/bgs-modding-superpowers` | 13,301 (tools: 13,110) |
| `knowledge` | 8,828 |
| `tools` | 5,622 |
| `docs` | 176 |
| `mods` | 103 |
| `tests` | 47 |
| `skills` | 31 |
| `scripts` | 14 |
| `.opencode` | 13 |
| `templates` | 12 |
| `hooks` | 3 |
| root manifests | ~6 |

## Other bloat

- 770 tracked files under `obj/` or `bin/` (build artifacts)
- 8,353 tracked files under `knowledge/spt-kb/archive/`
- 6 stray `~allenexplorer~.ala` artifacts
- `external/decompile-cache/` (~17,000 files, already ignored)

## Bootstrap verification at baseline

`tests/bootstrap/verify-all.ps1` FAILS immediately:

```
FAILED: Missing required bootstrap files: docs/roadmap.md, docs/standards/repo-hygiene.md
```

The suite asserts a pre-reshape repo shape (dead skill names, deleted paths, a
hooks directory of markdown specs) and is rewritten by ticket 02.
