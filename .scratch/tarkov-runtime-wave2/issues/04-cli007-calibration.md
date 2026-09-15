# 04: CLI-007 标准与检查器校准（移除桥豁免）

**What to build:** Modding Standard 的 STD-CLI-007 与机检同步 5.0 实际形态：`05-client.md` 文本改为「5.0：`Unload()`（`BasePlugin`）/ `Dispose()`（组件）」；`scripts/check-mod-standard.ps1` 的 CLI-007 判定接受 `Unload()` 为 5.0 撤销路径；`version-matrix.md` 同步（若涉及）；移除 `tools/tarkov-runtime-bridge/MODDING-STD-WAIVER.md` 中的 CLI-007 行并复跑机检确认仍 FAIL=0。

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] `05-client.md` STD-CLI-007 文本与证据锚点更新（5.0 `Unload()`）
- [x] 检查器 CLI-007 接受 `Unload()`（收紧后保留 `override bool Unload(` 与 `UnpatchSelf`）
- [x] 桥豁免文件移除 CLI-007 行；复跑 `check-mod-standard.ps1 -TargetSptVersion 5.0.0` → FAIL=0（实测 **WAIVED 3→1**，CLI-003 与 CLI-007 两行同删，优于工单预期 3→2）
- [x] 版本矩阵同步；模板（client-mod / paired-mod）注释同步（超出本工单文本范围，属 handoff 明示的第二波交付项）

## Comments

### 2026-09-15 校准落地 + 评审加固

**落地**：`05-client.md` STD-CLI-007 文本改「5.0：`Unload()`（`BasePlugin`）/ `Dispose()`（组件）」+ 锚点更新；`check-mod-standard.ps1` CLI-007 接受 `Unload()`；`version-matrix.md` 同步；桥豁免移除 CLI-003/007 两行。

**实测**：桥 `-TargetSptVersion 5.0.0` → **PASS=13 / FAIL=0 / WAIVED=1**（仅剩 CLI-006）；四套模板机检 FAIL=0（client 13 / paired-Client 13 / paired-Server 21 / server 20）。

**评审加固（Standards 轴）**：
- `$hasUnpatch` 收紧：仅接受 `UnpatchSelf` 或 `override bool Unload(`；移除裸 `Unload(` / `.Dispose()` / `UnpatchAll`（假通过风险）。
- 「无 Harmony → CLI-007 N/A 即 PASS」前置条件保留并加固（`new Harmony(` / `HarmonyLib.Harmony` / `PatchAll(` 三形态），脚本注释写明依据。
