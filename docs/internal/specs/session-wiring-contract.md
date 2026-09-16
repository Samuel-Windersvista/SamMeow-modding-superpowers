# 会话接线契约（Session Wiring Contract）

> 状态：**现行** · 2026-09-16 建立（架构审查 C4 交付）
> 关联：ADR-0006（纯 OpenCode 姿态）、ADR-0008（运行时布局解析）、`.scratch/c4-session-wiring/spec.md`

## 契约（唯一权威）

OpenCode 插件 `.opencode/plugins/spt-modding-superpowers.js` 在会话启动时只做三件事：

1. **skills 路径注册**：把 `<plugin>/skills` 追加进 `config.skills.paths`。
2. **MCP 注册**：经 `config.mcp` 声明三台本地 stdio 服务器（见下表）。
3. **bootstrap 注入**：把 `using-spt-modding-superpowers` 技能体注入首条用户消息（幂等标记防重复注入）。

**它不物化任何文件。** 仓库根不存在任何"由插件生成"的运行时文件；此前 `.gitignore` 与
`tests/bootstrap/verify-layout.ps1` 中「`hooks/`、`.mcp.json`、`.claude-plugin/`、
`.codex-plugin/`、`.agents/` 每会话物化」的声明是历史遗留的**幽灵契约**，已于 2026-09-16
随 C4 退役（残留备份：`D:\Temp\opencode\bgs-leftover-backup-20260916.zip`）。

## config.mcp 清单（3 台）

| 名称 | 入口 | 环境 | timeout (ms) | 说明 |
|------|------|------|--------------|------|
| `mo2` | `tools/mo2-mcp/dist/index.js` | 透传 `{}` | 240000 | MO2 控制面（惰性绑定，`mo2_session` 选择实例） |
| `spt` | `tools/spt-mcp/dist/index.js` | 透传 `{}` | 60000 | 文件系统分析 + KB（路径经共享解析器，见 Runtime Layout） |
| `tarkov` | `tools/tarkov-runtime-mcp/dist/index.js` | 透传 `{}` | 360000 | 运行时状态（`wait_for` 上限 300s + 余量） |

- 三台均为构建产物依赖：挂载前需 `npm --prefix tools/<server> run build`（dist 不跟踪）。**共享内核先构建**：`npm --prefix tools/mcp-kit install && npm --prefix tools/mcp-kit run build`——三台以相对路径 `../../mcp-kit/dist/index.js` 导入 kit，kit 的 `dist/` 缺失时三台同时启动失败（`tests/bootstrap/verify-mcp-entrypoints.ps1` 对此做断言）。便携包由 `scripts/build-portable-plugin.ps1` 物化：三台 + kit 的 `dist/` 与运行时 `node_modules/` 均随包（自包含）；`archive/` 永不进包。
- 环境一律透传（`environment: {}`）：用户经父进程环境变量覆盖；**显式设置但无效即报错，
  不静默回退**（语义见 `shared/runtime-layout.mjs` / ADR-0008）。
- 用户可在 `opencode.json` 覆盖插件声明（插件用 `??=`，用户配置优先）。

## 已退役形态（不物化、不跟踪、不应存在）

| 路径 | 退役 | 备注 |
|------|------|------|
| `hooks/`、`.claude-plugin/`、`.codex-plugin/`、`.agents/` | 2026-09-14 移除 / 2026-09-16 清理 | 多宿主适配（Claude Code / Codex）残留，空目录 |
| `.mcp.json` | 2026-09-16 | BGS 时代 MCP 注册（5 服务器全指向旧树）；tarkov 挂载已改由插件 `config.mcp` 承担 |
| `plugins/bgs-modding-superpowers/` | 2026-09-16 | BGS 时代打包树（94.8MB，已备份） |

`tests/bootstrap/verify-layout.ps1` 对这 6 条路径做 **absent 断言**：它们再现即检查失败——
这是正确的信号（说明存在未退役的物化者），应查明来源并清理。

## 与相邻契约的关系

- **路径解析契约**（Runtime Layout）：`shared/runtime-layout.mjs` 唯一解析点——KB 根 /
  forge 归档 / helper 产物的路径与校验（ADR-0008）。
- **姿态**：纯 OpenCode、纯 SPT（ADR-0006）。多宿主适配（Claude Code / Codex）为已退役
  能力，不复活——除非有新的显式决策。
