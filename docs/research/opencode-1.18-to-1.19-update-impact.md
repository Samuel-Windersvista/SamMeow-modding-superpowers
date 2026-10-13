# OpenCode 1.18.x → 1.19.0 更新内容与兼容性影响报告

## 元信息

- 研究日期：2026-10-11
- 版本范围：`opencode-ai` 1.18.0 (2026-07-14) 至 1.19.0 (2026-10-11)
- 目标环境：Windows，npm 全局安装 `opencode-ai@1.19.0`
- 环境组件：
  - 自定义插件 `spt-modding-superpowers`（项目级 `.opencode/plugins/spt-modding-superpowers.js`，使用 `experimental.chat.messages.transform` hook 注入 bootstrap）
  - 第三方预设插件 `oh-my-opencode-slim`（全局加载，使用 `experimental.chat.messages.transform` 与 `experimental.chat.system.transform` hook）
  - 技能目录：`~/.config/opencode/skills/`、`~/.agents/skills/`、项目 `skills/` 等
- 来源类型：
  - npm registry 版本元数据（`npm view opencode-ai versions/time`）
  - GitHub 上游仓库 release tags 与 commit 对比（`anomalyco/opencode`，原 `sst/opencode`）
  - 官方文档 `https://opencode.ai/docs/*`
  - 官方 JSON Schema `https://opencode.ai/config.json`
  - 本地环境文件：`~/.config/opencode/opencode.json`、`oh-my-opencode-slim.json`、项目 `.opencode/plugins/spt-modding-superpowers.js`

## 版本时间线（一句话版）

| 版本 | 发布时间 (UTC) | 一句话要点 |
|------|----------------|------------|
| 1.18.0 | 2026-07-14 20:27 | 1.18 系列基线。 |
| 1.18.1 | 2026-07-14 21:35 | 修复 Windows 抽屉关闭按钮、Meta reasoning 默认 xhigh。 |
| 1.18.2 | 2026-07-15 16:13 | 限制 subagent 嵌套深度（默认 1）、TUI 子代理菜单可用上箭头退出。 |
| 1.18.3 | 2026-07-16 15:31 | Azure Cognitive Services 端点恢复、stats 页面调整。 |
| 1.18.4 | 2026-07-20 15:25 | 大量 UI/桌面/统计页面打磨。 |
| 1.18.5 | 2026-07-24 22:15 | 修复 provider reasoning 选项语义、opentui 升降级。 |
| 1.18.6 | 2026-07-27 02:45 | MCP client SDK 升级到 v2，并恢复 legacy SDK 兼容。 |
| 1.18.7 | 2026-07-27 07:30 | MCP 回调端口、SSE 错误重连修复。 |
| 1.18.8 | 2026-07-28 06:05 | 修复 dnd-kit scroll、OpenAI header timeout。 |
| 1.18.9 | 2026-07-28 18:12 | 修复 Muse 系列模型系统提示路由。 |
| 1.18.10 | 2026-07-30 14:39 | 修复 custom agents selector、desktop wslServers ready state。 |
| 1.18.11 | 2026-08-01 11:43 | 修复 legacy config 读取、stats 比较图表。 |
| 1.18.12 | 2026-08-04 00:57 | 修复消息按创建时间排序（多处）。 |
| 1.18.13 | 2026-08-04 12:10 | 修复 config permission 自动接受、prompt input agent toggle 响应式。 |
| 1.18.14 | 2026-08-05 20:56 | 修复 Mistral prompt cache key、provider 选择逻辑。 |
| 1.18.15 | 2026-08-07 06:51 | 修复缺失 message parents、空 prompt text parts。 |
| 1.18.16 | 2026-08-10 06:05 | 修复 response message phases 回归、增大 OpenAI header timeout。 |
| 1.18.17 | 2026-08-12 20:23 | 修复 Kimi 提示按 provider 选择、屏蔽账户文案。 |
| 1.18.18 | 2026-08-13 01:13 | 修复 skills 文档中的全局配置路径。 |
| 1.18.19 | 2026-08-20 06:21 | 修复子代理工具错误 surface、resumable subagent errors、run 模式权限回答。 |
| 1.18.20 | 2026-08-21 08:09 | 修复 provider cache key 选择、Muse reasoning 默认与系统提示。 |
| 1.18.21 | 2026-08-21 14:51 | UI/UX 继续打磨。 |
| 1.18.22 | 2026-08-24 14:37 | 文档更新（ACR Zed custom agent config）。 |
| 1.18.23 | 2026-08-25 06:33 | 文档：prompt 数据处理说明。 |
| 1.18.24 | 2026-08-28 04:09 | 修复 config snapshot 比较为 JSON。 |
| 1.18.25 | 2026-08-28 05:57 | 测试：移除 flaky subagent test。 |
| 1.18.26 | 2026-09-01 21:51 | 修复原生配置路径权限断言。 |
| 1.18.27 | 2026-09-02 21:39 | 修复 Anthropic thinking blockBinding 可配置退出。 |
| 1.18.28 | 2026-09-04 15:40 | OpenAI usage normalization 与 tier threshold 配置。 |
| 1.18.29 | 2026-09-04 23:46 | （无显著公开变更摘要） |
| 1.18.30 | 2026-09-09 03:33 | 从 v2 移植 Astra 系统提示。 |
| 1.18.31 | 2026-09-14 17:47 | （无显著公开变更摘要） |
| 1.18.32 | 2026-09-21 22:50 | （无显著公开变更摘要） |
| 1.18.33 | 2026-09-28 04:23 | （无显著公开变更摘要） |
| 1.18.34 | 2026-09-30 22:38 | 修复 TUI `/status` 对话框中插件名提取使用 `path.sep`。 |
| 1.18.35 | 2026-10-06 20:21 | stats 增加 canonical redirects 与 agent-readable 数据格式。 |
| 1.19.0 | 2026-10-11 01:38 | **1.19 首个稳定版**；当前 npm latest。 |

> 截至 2026-10-11，`opencode-ai` 与 `@opencode-ai/plugin` 在 npm 上的最新稳定版均为 1.19.0，无 1.19.1/1.20 发布记录。

## 变更详情（按主题分组）

### 1. 插件 API / hooks

**状态：签名未变，但触发上下文有微调风险。**

- `experimental.chat.messages.transform` 与 `experimental.chat.system.transform` 两个实验性 hook 在 `v1.18.0` 与 `v1.19.0` 的 `@opencode-ai/plugin` 类型定义中**签名完全一致**（输入 `input: {}`，输出 `output.messages: { info: Message, parts: Part[] }[]` / `output.system: string[]`）。
  - 来源：上游 `packages/plugin/src/index.ts` 的 tag 对比（`v1.18.0` vs `v1.19.0`）。
- 触发位置同样保留在 `packages/opencode/src/session/prompt.ts`（主对话循环）和 `packages/opencode/src/session/compaction.ts`（session 压缩）。
- **但是**，本区间内有多条提交涉及消息排序：
  - `fix(tui): order messages by creation time (#40994)`
  - `fix(opencode): order legacy message loop by time (#40990)`
  - `fix(app): use chronological message boundaries (#40991)` / `(#41006)`
  - `fix(app): order stored messages by creation time (#41001)`
  - `fix(web): order shared messages by time (#40995)`
  - `fix(app): tolerate missing message parents (#37918)`
  - `fix(app): omit empty prompt text parts (#37577)`
  - 这些修复改变了 `output.messages` 在传给 hook 之前的内部顺序/过滤逻辑，理论上会影响依赖“第一个 user message”定位的插件。
- `config` hook 仍然可用；spt-modding-superpowers 通过它写入 `config.skills.paths` 与 `config.mcp.*`。

### 2. 配置 schema

- **新增 / 保留**：
  - `skills.paths` 继续存在，类型为 `string[]`，描述为 "Additional paths to skill folders"。spt-modding-superpowers 通过 `config` hook 写入该字段的方式仍然有效。
  - `subagent_depth` 默认仍为 `1`，本区间内通过 `fix(core): limit subagent nesting depth (#37124)` 明确了限制逻辑。
  - `agent.permission` 是推荐方式；`agent.tools` 已标记 deprecated。
- **变化**：
  - `feat(opencode): load supported v2 config in v1 (#45421)`（2026-08-27）：v1 开始加载部分 v2 配置，可能引入新的 key 名或结构。对于使用非官方字段的第三方预设，需要确认这些字段是否被 v2 兼容层覆盖。
  - `fix(opencode): ignore unknown config fields (#41312)`（2026-08-08）：配置解析器改为忽略未知字段而不是报错。这对 oh-my-opencode-slim 的自定义字段 `orchestratorPrompt` 是双刃剑：不会导致启动失败，但如果该字段不再被插件读取，则功能静默失效。
- **非官方字段说明**：
  - `orchestratorPrompt` **不是** OpenCode 官方 `AgentConfig` schema 中的字段。它是 `oh-my-opencode-slim` 自定义 schema 的字段，由该插件在初始化时解析并注入到 orchestrator 的 system prompt 中。只要 oh-my-opencode-slim 的 `config()` hook 仍能读取到该字段，功能就保持；与 OpenCode 的 `ignore unknown config fields` 无直接冲突。

### 3. 技能（skills）机制

- 官方技能搜索路径未变：`.opencode/skills/<name>/SKILL.md`、`~/.config/opencode/skills/<name>/SKILL.md`、`.claude/skills/`、`.agents/skills/` 等。
- `config.skills.paths` 仍支持附加自定义技能目录。
- 本区间内仅见文档修复 `fix(skills): Update global config path in documentation (#42337)`，无 skills 解析格式或加载行为变更。
- 风险点：如果 `experimental.chat.messages.transform` 的调用顺序因 message ordering 修复而变化，依赖该 hook 做 skill 可用性过滤的 `oh-my-opencode-slim` 内部 hook（`filter-available-skills`）可能受到间接影响。

### 4. MCP 支持变化

- **重大变更**：`feat(mcp): upgrade client SDK to v2 (#39247)`、`chore(mcp): upgrade client to 2.0.0 (#39369)`（2026-07-28）。
- **兼容性修复**：同日 `fix(mcp): restore legacy SDK compatibility (#39373)`。
- 对 spt-modding-superpowers 的影响：
  - 该插件通过 `config` hook 注册三个本地 stdio MCP servers（`mo2`、`spt`、`tarkov`）。只要 OpenCode 1.19.0 对 legacy MCP 的兼容层继续有效，这些 server 应能正常启动。
  - `@modelcontextprotocol/sdk` 从 1.x 升级到 2.x 可能改变 transport 行为；但 legacy compatibility 修复已回退兼容旧协议调用。
- 其他相关修复：
  - `fix(opencode): stop MCP SSE error reconnect loops (#39697)`
  - `fix(mcp): recover expired SDK sessions (#39265)`
  - `fix(mcp): honor callback port in debug (#39259)`
  - `fix(core): correct MCP environment field in built-in skill (#39175)`

### 5. 子代理（subagents）与 orchestration

- `fix(core): limit subagent nesting depth (#37124)`：默认 `subagent_depth = 1`，防止子代理再调子代理。
- `fix(opencode): surface subagent tool errors (#43821)` / `surface resumable subagent errors (#43657)` / `answer subagent permissions in run (#43675)`：改进了子代理错误与权限处理。
- 对 oh-my-opencode-slim 的 `orchestrator` / `council` 等多代理编排功能可能有行为微调，尤其是错误冒泡与权限回答逻辑。

### 6. 消息 / prompt / provider 相关

- 多次修复消息按创建时间排序，影响 UI 与内部循环。
- `fix(opencode): ignore unknown config fields` 让非官方字段不再报错。
- `feat(opencode): load supported v2 config in v1` 让 v1 能读取部分 v2 配置。
- `fix(app): preserve agent picker for existing users (#39300)`：保留老用户的 agent picker。
- provider 相关：Kimi / Muse / Mistral / Anthropic thinking 等模型特化修复，主要影响模型输出质量与缓存 key，不影响插件 API。

### 7. 其他（TUI / desktop / stats / CI）

- 大量 UI、desktop app、stats 页面、CI/CD 工作流的提交。对 CLI-only 或插件机制本身影响较小，但对 TUI 体验有影响。

## 高风险项

| 风险 | 影响对象 | 严重度 | 说明 |
|------|----------|--------|------|
| `experimental.chat.messages.transform` 消息顺序变化 | spt-modding-superpowers、oh-my-opencode-slim | 中 | 1.18.x 期间多次修复消息按创建时间排序，理论上可能改变 hook 看到的 `output.messages` 顺序。如果插件依赖“第一个 user message”或特定 part 索引注入内容，可能出现注入位置偏移或重复注入（虽有幂等标记）。 |
| 多个插件共享同一 hook 的触发时序 | spt-modding-superpowers、oh-my-opencode-slim | 中 | oh-my-opencode-slim 内部有 `task-session-manager`、`phase-reminder`、`filter-available-skills` 三个 hook 也注册 `experimental.chat.messages.transform`。OpenCode 按加载顺序串行触发；如果某一步抛异常或修改了 `output.messages` 结构，后续 hook 会受影响。 |
| `@opencode-ai/plugin` 版本漂移 | spt-modding-superpowers、oh-my-opencode-slim | 中高 | spt-modding-superpowers 的 `.opencode/package.json` 锁定 `@opencode-ai/plugin@1.17.9`；oh-my-opencode-slim-patched 依赖 `@opencode-ai/plugin ^1.3.17`（已非常旧）。OpenCode 1.19.0 自带 plugin 1.19.0。虽然 hook 签名未变，但类型、内部 context 对象、Message/Part 结构可能有微调，旧类型定义不再反映运行时真实结构。 |
| MCP client SDK v2 升级 | spt-modding-superpowers 注册的 mo2/spt/tarkov MCP | 中 | 1.18.6 升级 MCP client SDK 到 v2，并同日恢复 legacy compatibility。若 spt/tarkov 等本地 MCP server 仍使用旧 SDK 1.x 行为，需确认 legacy 兼容层是否覆盖全部使用场景。 |
| `orchestratorPrompt` 是非官方字段 | oh-my-opencode-slim | 低中 | 该字段不在 OpenCode 官方 schema 中。虽然 `ignore unknown config fields` 保证不会报错，但如果 oh-my-opencode-slim 的 `config()` hook 读取逻辑受 v2 配置兼容层影响，可能导致 orchestrator prompt 注入失败。 |
| v2 配置兼容层 | 所有自定义插件/预设 | 中 | `load supported v2 config in v1` 让部分 v2 key 可在 v1 使用。若用户的 `opencode.json` 或插件生成的配置与 v2 key 冲突，可能被覆盖或忽略。 |

## 待核事项

1. **实际运行时验证**（已核验 2026-10-11，结果见下文附录）：在 OpenCode 1.19.0 下启动一次对话，确认：
   - `spt-modding-superpowers` 的 bootstrap 是否仍被注入到第一条用户消息；
   - `oh-my-opencode-slim` 的 orchestrator prompt 是否仍出现在 system prompt；
   - `mo2`/`spt`/`tarkov` 三个 MCP server 是否能正常初始化。
2. **插件依赖升级**（已于 2026-10-11 完成）：`spt-modding-superpowers/.opencode/package.json` 中 `@opencode-ai/plugin` 已从 `1.17.9` 升级到 `1.19.0`（npm install + npm ls 实装验证无误）。
3. **oh-my-opencode-slim 版本**：当前本地 patched 版本为 `1.1.1`，依赖 `@opencode-ai/plugin ^1.3.17` / `@opencode-ai/sdk ^1.3.17`。建议检查上游 `alvinunreal/oh-my-opencode-slim` 是否有针对 OpenCode 1.18+/1.19+ 的更新分支。
4. **v2 配置键冲突**：检查 `opencode.json` 中是否有与 v2 兼容层同名的新 key（如 `references`、`mode` 等），避免被意外覆盖。
5. **message ordering 回归测试**：如果 spt-modding-superpowers 的 bootstrap 注入出现重复或错位，需在 `experimental.chat.messages.transform` 中增加更严格的幂等检查（目前仅检查 `EXTREMELY_IMPORTANT_SPT_MODDING_SUPERPOWERS` 文本标记）。

## 核验补充（主控核验，2026-10-11）

> 主控在收尾阶段直接抽查一手来源，补充如下（不改变上文结论）：

### v1.19.0 本体 release notes（逐字核验）

GitHub release `v1.19.0`（published 2026-10-11T01:39:22Z）本体改动很小，与「本区间以打磨为主」的结论一致。要点：

- **OpenCode 2 升级路径**：新增 `opencode upgrade --major`，用于升级到 OpenCode 2。
- **TUI 更新行为变化**：改为使用 `/update` 安装更新或升级到 OpenCode 2；**更新不再自动安装**。
- 其他：允许 OpenAI `ultrafast` service tier；PTY 连接 origin / ticket 校验。

> [注意] OpenCode 2 已可从 1.19.0 经 `opencode upgrade --major` 到达。本报告范围不含 2.x 的破坏性差异；升级到 2.x 前须另行评估（插件 API、skills 机制、配置 schema）。

### 版本序列与 npm 实时核验

- GitHub releases 列表（前 6 条，2026-10-11）：最新为 `v1.19.0`，其次 `v1.18.35`（2026-10-06）、`v1.18.34`（2026-09-30）、`v1.18.33`（2026-09-28）等；无 1.19.1 / 1.20。
- npm 实测（2026-10-11）：`opencode-ai@1.19.0`、`@opencode-ai/plugin@1.19.0` 均为 latest，与本机安装版本一致。

## 附：本机环境指纹与运行期冒烟（exp-1 + 主控，2026-10-11）

> 结论先行：在 opencode 1.19.0 下，本环境的插件注入与三个本地 MCP 均通过运行期冒烟；下方为指纹明细与遗留关注点。

### 环境指纹

- 配置面：`opencode.json`（v1：`autoupdate:"notify"`、`plugin[]`、`agent{}`、`mcp{}`、`lsp`、`instructions[]`）与 `cli.json`（v2 CLI 独立配置：`plugins[]`、`session`、`diffs`、`animations`）并存。
- 插件加载源（实测）：
  - `spt-modding-superpowers`：项目级 `.opencode/plugins/spt-modding-superpowers.js`（149 行；hooks：`config` @74、`experimental.chat.messages.transform` @135；经 `config` hook 追加 `skills.paths`，注册 `mo2`/`spt`/`tarkov` 三个 stdio MCP）。
  - `oh-my-opencode-slim`：`opencode.json` 指向 `E:/云文件/GitHub/oh-my-opencode-slim`（v2.2.18，双 v1/v2 支持）；另存遗留副本 `oh-my-opencode-slim-patched/`（v1.1.1）；`cli.json` 裸名引用属 v2 CLI 侧，解析落点待确认。
  - `opencode-quota`（@slkiser/opencode-quota@4.10.2，读写非官方 key `experimental.quotaToast`）；`@mohak34/opencode-notifier@0.2.5`（依赖存在、未挂载，疑似休眠）。
- 版本四联漂移：运行体 `1.19.0` / config 实装 `@opencode-ai/plugin@1.18.27` / 本仓库锁定 `1.17.9` / slim-patched `^1.3.17`。
- 技能面：本仓库 `skills/` 15 个；`~/.config/opencode/skills/` 9 个；frontmatter 仅 `name`+`description`。
- OC2 前兆：本机并存 `@opencode/cli@2.0.13`（bin `opencode2`）；插件包含 `@opencode-ai/plugin/dist/v2` 子路径。

### 运行期冒烟（本会话，opencode 1.19.0）

| 项目 | 结果 | 证据 |
|------|------|------|
| bootstrap 注入 | [OK] | 本会话首条用户消息内可见 `EXTREMELY_IMPORTANT_SPT_MODDING_SUPERPOWERS` 注入块 |
| 技能路径注册 | [OK] | 会话技能清单包含本仓库 `skills/` 全部技能（如 `building-spt-modpack`） |
| orchestrator 注入 | [OK] | 系统提示含 orchestrator 段与 orchestratorPrompt 说明 |
| MCP `spt` | [OK] | `spt_health`：repo 模式，KB 与 helper 全部可用 |
| MCP `mo2` | [OK]（已绑定） | 2026-10-11 经 `mo2_session` 绑定至 `E:\Game\EFT_Offline\Norvinsk_Forever`（SPT4 / Default / mods 72 全启用 / sidecar ready）；此前自动绑定失败主因：环境变量 `MO2_ROOT` 指向安装壳目录（无 `ModOrganizer.ini`） |
| MCP `tarkov` | [OK] | `tarkov_instances` 正常响应（0 实例为预期） |
| 其他观察 | - | 本会话工具面含 `chrome-devtools` MCP；`blender-mcp` 未接入（配置指向的 `C:\Users\Winde\.local\bin\uvx.exe` 不存在，属既有配置问题） |

> 至此「待核事项 #1」的运行期三项（bootstrap / orchestrator / MCP 初始化）全部核验通过；「待核事项 #2」（插件依赖升级）已于 2026-10-11 执行完成（`.opencode` 实装 `@opencode-ai/plugin@1.19.0`）。

## 来源清单

1. npm registry 版本元数据
   - `npm view opencode-ai versions --json`
   - `npm view opencode-ai time --json`
2. GitHub 上游仓库 tags
   - `https://api.github.com/repos/anomalyco/opencode/tags?per_page=100`
3. 上游仓库 `v1.18.0` / `v1.19.0` 关键文件
   - `https://raw.githubusercontent.com/anomalyco/opencode/v1.18.0/packages/plugin/src/index.ts`
   - `https://raw.githubusercontent.com/anomalyco/opencode/v1.19.0/packages/plugin/src/index.ts`
   - `https://raw.githubusercontent.com/anomalyco/opencode/v1.18.0/packages/opencode/package.json`
   - `https://raw.githubusercontent.com/anomalyco/opencode/v1.19.0/packages/opencode/package.json`
4. 官方文档
   - `https://opencode.ai/docs`
   - `https://opencode.ai/docs/config`
   - `https://opencode.ai/docs/plugins`
   - `https://opencode.ai/docs/agents`
   - `https://opencode.ai/docs/skills`
5. 官方配置 schema
   - `https://opencode.ai/config.json`
6. GitHub 代码搜索（`anomalyco/opencode`）
   - `experimental.chat.messages.transform` 出现位置：`packages/plugin/src/index.ts`、`packages/opencode/src/session/prompt.ts`、`packages/opencode/src/session/compaction.ts`
7. 本地环境文件
   - `C:\Users\Winde\.config\opencode\opencode.json`
   - `C:\Users\Winde\.config\opencode\oh-my-opencode-slim.json`
   - `C:\Users\Winde\.config\opencode\oh-my-opencode-slim-patched\package.json`
   - `C:\Users\Winde\.config\opencode\oh-my-opencode-slim-patched\src\tools\preset-manager.ts`
   - `E:\云文件\GitHub\SamMeow-modding-superpowers\.opencode\package.json`
   - `E:\云文件\GitHub\SamMeow-modding-superpowers\.opencode\plugins\spt-modding-superpowers.js`
8. 第三方预设仓库
   - `https://github.com/alvinunreal/oh-my-opencode-slim`（`orchestratorPrompt` 字段与 `experimental.chat.system.transform` 注入逻辑）
9. 主控核验（2026-10-11）
   - `https://api.github.com/repos/anomalyco/opencode/releases/tags/v1.19.0`
   - `https://api.github.com/repos/anomalyco/opencode/releases?per_page=6`
   - `npm view opencode-ai version` / `npm view @opencode-ai/plugin version`
