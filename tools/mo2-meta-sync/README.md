# mo2-meta-sync

Sync a mod repo's description fields into an MO2 overlay's `meta.ini`, and pull
them back out to bootstrap the repo-side source of truth.

Node ESM, **zero external dependencies**. Run with any Node >= 18 (`node sync.mjs ...`).

## Why this exists

MO2 writes an overlay's descriptive fields (`comments` / `notes` / `version` /
`installationFile`) once at install. The deploy flow then protects `meta.ini` from
being overwritten (correct: it holds MO2 bookkeeping and injected secrets), so those
fields drift forever after the mod repo updates.

Fix: keep the source of truth in the mod repo as a `fragment.json`, and sync it into
`meta.ini` at deploy time with this tool.

## CLI contract

```
node tools/mo2-meta-sync/sync.mjs --fragment <fragment.json> --meta <path/to/meta.ini> [--dry-run]
node tools/mo2-meta-sync/sync.mjs --pull --meta <path/to/meta.ini> --out <fragment.json>
node tools/mo2-meta-sync/sync.mjs --self-test
```

- **push** (default, first form): write the fragment's keys into `[General]`.
- **`--dry-run`**: print the planned diff and write nothing.
- **pull**: read the four keys out of an existing `meta.ini` into a `fragment.json`.
- **`--self-test`**: run the built-in round-trip test (see below).

Exit code is `0` on success or no-op, non-zero with an ASCII-only message on error
(missing file, bad JSON, unparsable `meta.ini`, disallowed key).

## `fragment.json` shape

All keys optional; **only keys that are present are written**.

```json
{
  "comments": "一行中文摘要",
  "notes": "- 第一条\n- 第二条",
  "version": "0.1.0",
  "installationFile": "llmc.zip"
}
```

Only these four keys are accepted. Any other key is rejected (error) so that
MO2 bookkeeping keys such as `validated`, `color`, or `tracked` can never be
clobbered by a stray fragment. A UTF-8 BOM on the fragment is tolerated.

## Guarantees

- **Minimal, byte-safe edit.** Only the four allowed keys inside `[General]` are
  touched. Every other key and every other section survives **byte-for-byte**
  (including `color=@Variant(\0\0\0\x43\0\xff\xff...)`, `validated`, `nexus*`,
  `tracked`, and `[installedFiles]` entries like `1\modid=0`).
- **Missing key = append.** If a fragment key is absent from `[General]`, it is
  appended to the end of `[General]` (still before the next section header).
- **QSettings-style values.** Multi-line values, values containing a quote, and
  values containing a backslash are written wrapped in `"` with `\n`, `\"`, `\\`
  escaped. Single-line simple values are written bare, matching the file's own
  style for an already-present key. A raw newline is never emitted inside a value.
- **Encoding/line-ending fidelity.** Detects CRLF vs LF (and CR) and reuses it;
  preserves a UTF-8 BOM if the original had one, writes UTF-8 **without** BOM if it
  did not.
- **Atomic write.** Temp file in the same directory + rename. A partial write never
  lands. A no-op (values already in sync) writes nothing.
- **ASCII-safe console.** All stdout/stderr is ASCII-escaped (`\uXXXX`), so a
  Windows GBK console cannot corrupt output. Chinese is never printed raw.

## Usage

```sh
# Dry-run: show what would change, write nothing
node tools/mo2-meta-sync/sync.mjs --fragment mod/meta.fragment.json --meta "$MODS/MyMod/meta.ini" --dry-run

# Apply
node tools/mo2-meta-sync/sync.mjs --fragment mod/meta.fragment.json --meta "$MODS/MyMod/meta.ini"

# Bootstrap a fragment from an existing overlay
node tools/mo2-meta-sync/sync.mjs --pull --meta "$MODS/MyMod/meta.ini" --out mod/meta.fragment.json

# Self-check
node tools/mo2-meta-sync/sync.mjs --self-test   # prints SELF-TEST PASS
```

## Self-test

`--self-test` creates a temp dir, writes a synthetic `meta.ini` (CRLF, no BOM,
containing a `color=@Variant(\0\0\0\x43...)` line and an `[installedFiles]`
section), pushes a fragment whose `notes` value is multi-line with an embedded
quote and a backslash, then asserts:

- (a) the described keys are updated, and a missing key is appended;
- (b) every other byte is preserved (exact expected-bytes comparison), CRLF kept,
  no BOM introduced;
- (c) escaping round-trips (value is parsed back and compared);
- (d) `--pull` returns the same values.

Prints `SELF-TEST PASS` (exit 0) or `SELF-TEST FAIL: <reason>` (exit 1).

## Scope

Write scope is `tools/mo2-meta-sync/**`. This tool never writes into the game
installation or an MO2 overlay's `meta.ini` unless you explicitly point `--meta`
at it (and never on `--dry-run`).
