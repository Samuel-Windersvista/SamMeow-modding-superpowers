# 11: Repair the MO2 control-plane test suite

**What to build:** `tests/mo2-control-plane/` verifies the current control-plane scaffold again — the offline-runnable subset passes, and the live-only tests fail loudly and explain why when their prerequisites are absent.

**Blocked by:** None (can start immediately).

**Status:** done

- [ ] The scaffold-rotten tests (`layout`, `plugin-contract`, `live-plan`, `live-endpoint-contract`, `live-bridge-layout`) assert the CURRENT scaffold, not files deleted by commit `19436f9e` (`tools/mo2-control-plane/plugin/README.md`, `plugin/CMakeLists.txt`, `tools/mo2-control-plane/live-integration.md`)
- [ ] The runtime tests stop invoking Python through a multi-line `python -c "<script>"` string (PowerShell 5.1 strips the embedded double quotes and corrupts the script); they write the script to a temp file and invoke `python <file> <arg>` instead
- [ ] The `-real` live tests stay opt-in behind `-AllowLiveSandbox` and fail with a clear, actionable message when `MO2_ROOT` / `MO2_HARNESS_ROOT` are unset
- [ ] Every offline-runnable test in `tests/mo2-control-plane/` passes
- [ ] A short note records which tests remain live-only and what they require
