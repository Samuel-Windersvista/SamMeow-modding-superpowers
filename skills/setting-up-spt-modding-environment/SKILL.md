---
name: setting-up-spt-modding-environment
description: "Use on first install of this plugin, when MO2 or SPT is not yet detected, when starting a new modpack project, or when the user says 'set up', 'install', 'bootstrap', 'configure', '初始化', or '装环境' the SPT modding environment. Orchestrates: MO2 detection and control-plane install, MO2 visible launch, SPT install detection, path configuration storage, spt-kb availability check, mod template verification, and dev-log / release-changelog initialization."
---

# Setting Up the SPT Modding Environment

This is the first-run bootstrap. Use it when:

- The user installed `spt-modding-superpowers` and this is the first conversation
  in their project directory.
- A new modpack project is being set up from scratch.
- MO2 path or SPT path is not yet known to the agent.
- The user explicitly asks to "set up", "install", "bootstrap", "configure",
  "初始化", or "装环境" the SPT modding environment.
- A subsequent skill (e.g., `writing-spt-mod` or `curating-spt-modpack`) finds
  the environment incomplete and routes back here.

Target version: **SPT 4.1** (final locked version). SPT 3.11 materials are
reference-only. The mod management layer is **MO2** — retained from the BGS
lineage. There is **no xEdit** in this ecosystem, and **Forge is offline**: all
mod data comes from the local archive at `knowledge/spt-kb/archive/forge/`.

## Hard guardrails

- **Do NOT install MO2 yourself by default.** The default path is to detect MO2
  and, if absent, guide the human user to install it themselves. Agent-driven
  install requires explicit user consent ("yes, install MO2 for me").
- **Never write into the user's SPT installation directory** (the SPT folder
  next to the EFT copy). It is real game state. All mod/overlay changes are
  expressed as MO2 mod overlays; the MO2 VFS projects them at runtime. See
  `using-spt-modding-superpowers` rule 1.
- **Never write into the user's vanilla EFT install.** The SPT folder is the
  only writable runtime surface, and even that goes through MO2 overlays.
- **Treat the user's existing MO2 profile as canonical.** Do not silently mutate
  `profiles/<Profile>/modlist.txt`, INIs, or load order.
- **Pause and surface state before each install action.** The user should always
  know which MO2 root, which SPT root, and which file is about to be touched
  before it happens.
- **MO2 must run visibly.** Never start MO2 in any background / hidden mode.
  The launch helper `scripts/start-mo2.ps1` enforces this and surfaces "zombie"
  MO2 processes (running but no window) so they can be cleaned up before a
  fresh start.
- **Paths vary per user. Never hardcode MO2 or SPT paths.** Record detected
  paths in configuration (see Step 5) instead of assuming a standard location.

## What the control plane actually is

The "MO2 control plane" we deploy in this skill is a **Python MO2 plugin** plus
a **PowerShell broker** — the same integration retained from the BGS lineage:

- `tools/mo2-control-plane/live-bridge/mo2_agent_control.py` — the actual MO2
  plugin. When MO2 loads it, the plugin opens a named pipe and starts publishing
  bootstrap runtime files. This is what the agent harness talks to.
- `tools/mo2-control-plane/broker/` — PowerShell IPC client. Runs from the
  plugin checkout; no install step.

There is **no C++ DLL to build or deploy**. The Python plugin is the
integration. If a previous agent told you to "build Mo2AgentControl.dll first"
— that was wrong.

There is **no xEdit daemon** in this environment. SPT mods are DLL/JSON
packages, not plugin records; there is no record-level editor to drive. Mod
analysis routes through the `spt` MCP server when available (see
`using-spt-modding-superpowers`), and conflict reasoning routes through the
20-type conflict taxonomy at `docs/wayfinder/findings/001-spt-conflict-taxonomy.md`.

## Workflow

### Step 1 - Detect MO2 install

Search for `ModOrganizer.exe` in common locations:

- `$env:LOCALAPPDATA\ModOrganizer*\ModOrganizer.exe`
- `D:\ModOrganizer*\ModOrganizer.exe`, `E:\ModOrganizer*\ModOrganizer.exe`,
  `C:\ModOrganizer*\ModOrganizer.exe`
- The user's Documents folder.
- The user's Desktop / common steam-library siblings.

If exactly one MO2 install is found, propose it to the user for confirmation.
If multiple, list them with paths and ask which is the target. If none, go to
step 2.

Persist the confirmed install root in conversation context as `MO2_Root`. All
subsequent steps reference this variable.

### Step 2 - If MO2 absent: branch the user

Surface a `[BLOCKED]` notice and offer THREE paths, in order of preference:

**(a) Default - human install.** Provide the official MO2 release URL:
https://github.com/ModOrganizer2/modorganizer/releases/latest. Give the user
a brief setup outline (download installer, pick an install root outside Program
Files, launch once to initialize). Wait for the user to install and come back
with the path.

**(b) Agent-handled install (explicit user consent required).** Only if the
user says something like "yes, install MO2 for me, go ahead": download the
official MO2 installer to a temp location, run it (silent install where
supported), register the resulting path as `MO2_Root`. Surface every step
("downloading from ...", "installing to ...", "MO2 ready at ..."). Never
proceed without the explicit verbal consent.

**(c) No-MO2 mode.** If the user says "I do not need MO2 for this project"
(e.g., they are writing modpack docs / planning without a runtime), record
`MO2_Root = none`, continue through step 4 for SPT detection, then skip to
step 10 (dev-log / changelog init). Mark that any MO2-bound work will be
unavailable until they revisit this skill.

### Step 3 - Detect SPT install

SPT is installed into (or beside) a copy of EFT. Detect by looking for the
canonical 4.x layout. The primary marker is the runtime folder:

- `<SPT_Root>\SPT_Runtime\SPT.Server.exe` — server binary (primary marker)
- `<SPT_Root>\SPT_Runtime\SPT.Launcher.exe` — launcher
- `<SPT_Root>\SPT_Data\` — server data (configs, `Server/database/`)
- `<SPT_Root>\user\mods\` — server mod packages
- `<SPT_Root>\user\logs\` — server logs
- `<SPT_Root>\BepInEx\plugins\` + `BepInEx\patchers\` — client mods
- `<SPT_Root>\BepInEx\LogOutput.log` — client log

Search common candidates: the user's steam library (`<Steam>\steamapps\common\`
plus a sibling `SPT` folder), the user's own known folders, and any path the
user volunteers. Confirm with the user before recording.

Do **not** mistake a vanilla EFT install for an SPT install: EFT alone has no
`SPT_Runtime\SPT.Server.exe` and no `user\mods`. Require at least the server
binary marker plus one of (`SPT_Data\` or `BepInEx\`).

Persist the confirmed root in conversation context as `SPT_Root`.

### Step 4 - If SPT absent: branch the user

SPT 4.1 requires a legal EFT install plus the SPT installer (from the SPT
project, e.g. via their official site / GitHub releases). Surface a `[BLOCKED]`
notice and offer TWO paths:

**(a) Human install (default).** Point the user at the official SPT
installation guide in the KB: `knowledge/spt-kb/wiki/Installation_Guide.md`
plus `knowledge/spt-kb/wiki/Manual-Install-Instructions.md` (which documents
the `SPT_Runtime` layout). Wait for the user to complete the install and come
back with the path. Do not attempt to script the SPT installer without explicit
consent.

**(b) Agent-assisted verification only.** If the user says the installer
already ran, verify the layout per step 3's marker list and report which
markers are present or missing. Do not copy, patch, or move game files
yourself.

### Step 5 - Path configuration storage

Persist `MO2_Root` and `SPT_Root` so restarts and subsequent sessions do not
re-detect. The plugin convention:

- **OpenCode**: set in the harness env / config for the agent session:
  ```json
  { "env": { "SPT_MO2_ROOT": "<MO2_Root>", "SPT_ROOT": "<SPT_Root>" } }
  ```
- **Codex**: append to `~/.codex/config.toml`:
  ```toml
  [mcp_servers.spt.env]
  SPT_MO2_ROOT = "<MO2_Root>"
  SPT_ROOT = "<SPT_Root>"
  ```
- **Claude Code**: set the env vars in the shell that launches Claude Code, or
  edit `<plugin-root>/.mcp.json` to add an `env` block on the `spt` entry.

Also record the two paths in the modpack project docs (`<project_root>/docs/`)
so a future maintainer can see them. After the user sets the env vars, they
must restart their harness session so any MCP server picks them up.

Per-call override is also available where the `spt` MCP supports it
(`spt_start({ moRoot, sptRoot, ... })`). Useful for one-off testing, but the
env-var path is what makes restarts work without re-passing paths.

If a path changes later (user moved SPT or MO2), update the env config AND the
project docs, then re-run the smoke test in step 11.

### Step 6 - Verify the bundled knowledge base

The spt-kb is bundled in the plugin tree; **no download or pack install is
needed** (unlike the BGS KB, there is no remote pack distribution). Verify:

- `knowledge/spt-kb/index.json` exists and parses.
- `knowledge/spt-kb/curated/modding-guide/` has the 4.1 guide (4 chapters).
- `knowledge/spt-kb/curated/api-notes-4.1/` has the 4.1 API notes.
- `knowledge/spt-kb/archive/forge/hot-index.json` + `api/mods-catalog.json`
  exist (the offline Forge archive; see `archive/forge/README.md`).

Smoke check:

```text
read knowledge/spt-kb/index.json -> entries have version/domain/topic fields
pick one entry (e.g. curated/api-notes-4.1/mod-loading.md) and open it
```

Success criterion: `index.json` lists the expected curated docs for version
`4.1`, and at least one referenced file opens. If the index is missing or
stale, route to `maintaining-spt-modding-environment` for regeneration before
continuing.

### Step 7 - Install the MO2 control plane (Python + broker)

Once `MO2_Root` is known (path a or b from step 2), deploy the Python plugin:

```powershell
& "<plugin-root>/scripts/install-mo2-control-plane.ps1" -MO2Root "<MO2_Root>"
```

This deploys:

- `tools/mo2-control-plane/live-bridge/mo2_agent_control.py` -> `<MO2_Root>/plugins/`
- The `Mo2AgentControl/` support tree -> `<MO2_Root>/plugins/`
- `ModOrganizer.ini lock_gui=false` normalization.

After the script returns, list `<MO2_Root>/plugins/` and confirm
`mo2_agent_control.py` is present. The script will NOT install a `.dll` — that
is intentional (see "What the control plane actually is" above). If the script
errors, stop and surface it; do not continue.

### Step 8 - Start MO2 visibly

```powershell
& "<plugin-root>/scripts/start-mo2.ps1" -MO2Root "<MO2_Root>" -Profile "<Profile>"
```

The launcher script:

- Refuses to start if a visible MO2 is already running (use that one).
- If a zombie MO2 (running but no window) is detected, surfaces it and asks
  before killing — use `-KillStale` to skip the prompt.
- Starts MO2 with `-WindowStyle Normal` so the GUI is visible.
- Waits up to 30s for the main window to appear and reports its title.

Verify after launch: the user should see the MO2 GUI on their desktop. The
plugin's bootstrap runtime files should appear at
`<MO2_Root>/plugins/Mo2AgentControl/bootstrap/runtime/` within a few seconds:

- `status.json` -> `{ schemaVersion, state: "ok", mo2Pid }`
- `endpoint.json` -> `{ transport: "named-pipe", endpoint: "mo2-control-plane-<pid>" }`
- `capabilities.json` -> lists `launch.*`, `system.*` methods.

If those don't appear, the Python plugin didn't load. Check MO2's plugin
settings to confirm `mo2_agent_control` is enabled.

### Step 9 - Verify mod templates

The mod-development skills (`writing-spt-mod`) build from project templates.
Verify both exist:

- `templates/server-mod/` — server mod project template (targets
  `SPTarkov.Server.Core` 4.1.0).
- `templates/client-mod/` — client mod project template (BepInEx plugin).

If either is missing, surface `[GAP]` and route to
`maintaining-spt-modding-environment` (template restoration) before any
`writing-spt-mod` work. Template absence does not block modpack *curation*
(installing existing mods from the Forge archive), but it blocks new mod
development.

### Step 10 - Initialize dev-log and release-changelog

Ask the user for their **modpack project root**. This is usually one of:

- `<MO2_Root>/profiles/<ProfileName>/` (if the profile is the unit of work).
- A separate Git-tracked source directory the user maintains (more common for
  released modpacks).

Once `<project_root>` is known, route to:

- `writing-spt-modpack-devlog` skill - creates `<project_root>/docs/dev-log.md`
  with the project name and start date as the first entry.
- `writing-spt-modpack-changelog` skill - creates
  `<project_root>/docs/release-changelog.md` skeleton.

From here on, those two skills maintain the files at runtime; do not template
or pre-fill them in this skill.

### Step 11 - Verify with a semantic smoke test

Do NOT declare success on green script returns. Verify the whole chain:

1. **SPT binaries present.** `<SPT_Root>\SPT_Runtime\SPT.Server.exe` and
   `SPT.Launcher.exe` exist; `SPT_Data\`, `user\mods\`, and `BepInEx\` exist.
2. **KB available.** `knowledge/spt-kb/index.json` parses and the 4.1 curated
   docs are listed.
3. **Templates present** (or `[GAP]` recorded with route-to-maintain).
4. **MO2 control plane live.** `<MO2_Root>/plugins/mo2_agent_control.py`
   exists (skipped only in no-MO2 mode).
5. **MO2 visibly running.** Process has `MainWindowHandle != 0` and
   `<MO2_Root>/plugins/Mo2AgentControl/bootstrap/runtime/status.json` reports
   `state: "ok"` with a current `mo2Pid` (skipped only in no-MO2 mode).
6. **Docs initialized.** `<project_root>/docs/dev-log.md` and
   `<project_root>/docs/release-changelog.md` both exist (or the user
   explicitly declined — record the decline in conversation context).

If all pass: surface `[OK] SPT modding environment ready` with the recorded
`MO2_Root`, `SPT_Root`, and project root. Do not attempt a full SPT launch
during setup; that belongs to `testing-spt-modpack` (Level B verification).

## Acceptance (semantic, not surface)

The setup is complete only when ALL of the following hold. Do not declare
success otherwise:

- `MO2_Root` and `SPT_Root` are known and recorded (env config + project docs).
- `<SPT_Root>\SPT_Runtime\SPT.Server.exe` and `SPT.Launcher.exe` exist.
- `knowledge/spt-kb/index.json` parses and lists the 4.1 curated docs.
- `<MO2_Root>/plugins/mo2_agent_control.py` exists (skipped only in no-MO2 mode).
- MO2 is visibly running (process has `MainWindowHandle != 0`) and
  `<MO2_Root>/plugins/Mo2AgentControl/bootstrap/runtime/status.json` reports
  `state: "ok"` (skipped only in no-MO2 mode).
- `templates/server-mod/` and `templates/client-mod/` both exist, OR a `[GAP]`
  was surfaced and routed to `maintaining-spt-modding-environment`.
- `<project_root>/docs/dev-log.md` and
  `<project_root>/docs/release-changelog.md` both exist (or user explicitly
  declined — record the decline in conversation context).

In no-MO2 mode, only the SPT detection, KB check, template check, and
dev-log / changelog steps need to pass.

## Common mistakes

- Skipping the consent gate on step 2(b). It REQUIRES explicit user agreement;
  silent install or silent install-decision is forbidden.
- Silently choosing one MO2 install when multiple are detected. Always ask.
- Confusing a vanilla EFT install for an SPT install. Require the
  `SPT_Runtime\SPT.Server.exe` marker plus `SPT_Data\` or `BepInEx\`.
- **Trying to build `Mo2AgentControl.dll`** — that was a BGS-era skeleton and is
  not the integration. The Python plugin IS the integration.
- **Expecting an xEdit daemon to exist.** There is no xEdit in the SPT
  ecosystem. Mod analysis goes through the `spt` MCP / the conflict taxonomy,
  never through plugin-record tooling.
- **Trying to hit the live Forge API.** Forge is offline. All mod data comes
  from `knowledge/spt-kb/archive/forge/`.
- **Starting MO2 with `Start-Process -WindowStyle Hidden`** (or any other
  invisible/background mode). Always use the `start-mo2.ps1` helper, which
  forces a visible window.
- Writing into the user's SPT install or vanilla EFT install directly instead
  of via MO2 overlay. NEVER do this. See `using-spt-modding-superpowers`
  rule 1.
- Declaring success on a green script return without the semantic readback
  (the file existence checks above).
- Hardcoding an MO2 or SPT path into any skill, script, or recipe. Paths vary
  per user; always read from env config or detection.

## See also

For ongoing care after first-run, see `maintaining-spt-modding-environment`.

- `using-spt-modding-superpowers` - the per-session bootstrap; lists the full
  skill inventory and hard rules.
- `writing-spt-modpack-devlog`, `writing-spt-modpack-changelog` - runtime asset
  skills invoked from step 10.
- `writing-spt-mod` - new mod development from `templates/server-mod/` and
  `templates/client-mod/` (depends on step 9).
- `testing-spt-modpack` - Level B verification of a launched SPT install after
  setup.
- The installer scripts under `scripts/`: `install-mo2-control-plane.ps1`,
  `start-mo2.ps1`.
- The offline mod archive: `knowledge/spt-kb/archive/forge/README.md`.
