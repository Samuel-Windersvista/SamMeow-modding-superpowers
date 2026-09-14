# 08: 文档与规范收尾 + Phase 2 验收门槛复核

**What to build:** 收尾交付。桥 README（安装/配置/端口/安全边界/故障排查）；Modding Standard 合规检查（或豁免记录）；MCP 工具描述与文档更新（`raid.*` 从占位到实现、能力自报 bridge 状态更新）；dev-log 记录；按 spec 验收门槛逐条复核（MCP 测试全绿 + live 验收 + 录制 fixture + 只读/仅 127.0.0.1/默认关录制）。

**Blocked by:** 02, 03, 04, 05, 06, 07

**Status:** ready-for-agent

- [x] 桥 README 完整（安装/配置/安全/排查）
- [x] Modding Standard 检查通过或豁免记录在案
- [x] MCP 工具描述/能力自报与实现一致（`raid.*` 不再标「占位」）
- [x] spec 验收门槛逐条勾选完成
- [x] dev-log 记录收尾

## Comments

### 2026-09-14 收尾（T08 完成）

- **桥 README**：安装（MO2 overlay）/配置/端点契约/安全边界/**故障排查**（含 usvfs 配置重定向、URL ACL、no-start、配置不生效等 live 踩坑）/构建
- **Modding Standard 机检**（`-TargetSptVersion 5.0.0`）：**PASS=11 FAIL=0 WAIVED=3**；豁免记录 `tools/tarkov-runtime-bridge/MODDING-STD-WAIVER.md`（CLI-003/006/007：只读桥无 Harmony、5.0 日志形态与检查器 regex 差异、Unload 撤销路径）；修复项：LICENSE 补齐、`SPTInstallPath` 标准名（兼容 `GameDir` 别名）
- **MCP 工具描述/能力自报**：`raid.*` 全部真实化、`capabilities.bridge="supported"`、`placeholderTools` 清空
- **spec 验收门槛逐条复核**：MCP 测试全绿（214/214）✓；live 验收（三局 raid：菜单/raid/移动/击杀/受伤/下蹲/raidId 稳定性）✓；一段真实 raid 录制 fixture 并用于回归 ✓；文档与 Modding Standard 合规（机检 0 FAIL + 豁免）✓；只读 / 仅 127.0.0.1 / 默认关录制 ✓
- dev-log 已记（`docs/dev-log.md` T03–T07 条目）
