# C4 · 会话接线契约（Session Wiring）— 实施规格

> 来源：架构审查候选 C4 + grilling 闭合决策（2026-09-16）。
> **进展（Work Status）**: CLOSED — 2026-09-16（三车道 + 两项补充修复：便携 tarkov 一致性、便携 node_modules 解码缺陷；bootstrap 9/9；未提交）

## 决策（Q1–Q5，全部按推荐）
- Q1-A：OpenCode-only 契约——插件只做三件事（skills 路径 / config.mcp / bootstrap 注入），不物化任何文件
- Q2-A：tarkov-runtime-MCP 进插件 config.mcp（3 台：mo2 / spt / tarkov）
- Q3-A：残留备份后删除（已完成：`D:\Temp\opencode\bgs-leftover-backup-20260916.zip`，27.4MB）
- Q4-A：verify-layout 恢复 absent 断言；.gitignore 删幽灵规则
- Q5-A：契约文档 `docs/internal/specs/session-wiring-contract.md`（车道 B）

## 事实基线（2026-09-16 核验）
- 插件（135 行）零物化；OMO 源码无物化；残留 mtime 冻结 09-14 12:03，多次重启未再生
- 残留（**已清理**）：`plugins/bgs-modding-superpowers`（94.8MB）、`.mcp.json`（798B，5 服务器全指向旧树）、`hooks/` `.claude-plugin/` `.codex-plugin/` `.agents/`（空）
- tarkov MCP 唯一注册处 = 陈旧 `.mcp.json`（OpenCode 不消费）→ 会话内未挂载
- `wait_for` MAX_WAIT_TIMEOUT_MS = 300_000 → tarkov 挂载 timeout = **360000**

## 车道 A 任务（fixer）

1. `.opencode/plugins/spt-modding-superpowers.js`：
   - 新增 `const TARKOV_MCP_ENTRY = path.join(PLUGIN_ROOT, 'tools', 'tarkov-runtime-mcp', 'dist', 'index.js');`
   - `config.mcp.tarkov ??= { type: 'local', command: ['node', TARKOV_MCP_ENTRY], enabled: true, environment: {}, timeout: 360000 };`
     注释说明：wait_for 上限 300s + 余量；env 透传（用户覆盖语义同 mo2/spt）。
   - 头注释：MCP 声明 2 台 → 3 台（mo2 / spt / tarkov）。
2. `tests/bootstrap/verify-mcp-surface.ps1`：断言 3 台 MCP（mo2 / spt / tarkov）+ 各自 entry 文件存在（按既有断言风格扩展，先读文件）。
3. `tests/bootstrap/verify-layout.ps1`：
   - `runtimePaths` 的 "not tracked" 断言改为 **absent 断言**（`plugins`、`hooks`、`.claude-plugin`、`.codex-plugin`、`.agents`、`.mcp.json` 六路径不应存在）；
   - 删除 "materialized at the repo root by the OpenCode plugin on every session start" 幽灵注释，改为真实契约说明（OpenCode-only；这些路径已退役；再现即检查失败=正确信号）。
4. `.gitignore`：删除 `/plugins/` 规则（旧构建输出目录；现行输出为 `/dist/`，保留）与第 27-33 行 harness 幽灵块（注释 + 5 条规则）。
5. `scripts/build-portable-plugin.ps1`：如仍存在 "does not mutate the live repo-root plugins/ workaround tree" 之类已过时表述，同步修正（`plugins/` 已退役）。
5b. **[车道 A 交付后补充] 便携包 tarkov 一致性**：`scripts/build-portable-plugin.ps1` 的 preflight / 复制清单加入 `tarkov-runtime-mcp`（与 mo2/spt 同待遇——避免便携树声明 3 台却缺 1 台 entry）；`tests/bootstrap/verify-mcp-entrypoints.ps1` 补 tarkov entry（与 3 台声明面一致）。验证：打包脚本实跑（前置齐备时）确认便携树含 tarkov；bootstrap 9/9。
6. 验证：`powershell -NoProfile -ExecutionPolicy Bypass -File tests/bootstrap/verify-all.ps1` **9/9 全绿**（前置：车道 C 清理已完成）。
7. **不 commit**；不碰车道 B 文件（`docs/internal/specs/`、`docs/README.md`、`RELEASE-NOTES.md`、`docs/dev-log.md`、`.scratch/`）。

## 车道 B 任务（orchestrator）
- `docs/internal/specs/session-wiring-contract.md`（新，单一权威契约文档）
- `docs/README.md` 索引登记；`RELEASE-NOTES.md` C4 小节；`docs/dev-log.md` 补记

## 车道 C 任务（orchestrator，已完成）
- 备份 zip → 删除 6 路径 → 验证 absent（全部通过）

## 验证标准
- bootstrap 9/9（含更新后的 verify-mcp-surface / verify-layout）
- 残留清零 + `git status` 无噪音
- 会话重启后 tarkov 工具面可见（重启后复核，同 C1 模式）
