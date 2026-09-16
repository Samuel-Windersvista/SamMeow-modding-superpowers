# C5 · 身份命中逐条分类表（S1b）

> 数据源：`git grep -n -i -E "bgs-modding-superpowers|awesome-bgs-mod-master|BGS_MODDING_SUPERPOWERS"`（2026-09-17，全仓 tracked 文件）。
> 口径：规格 §6 + D5 —— 指针/操作性引用 → 已归一；事件主体、署名/谱系、历史计划主体 → 保留原文；测试夹具/注释域 → 超范围只报告不改。
> 分类取值：`已归一` / `保留-事件` / `保留-署名` / `保留-历史计划` / `超范围`。

## 逐条分类

### LICENSE 与 fork 署名

| path:line | 分类 | 理由 |
|---|---|---|
| `LICENSE:3` | 保留-署名 | D7：上游版权行保留，fork 行追加于 :4（`SamMeow-modding-superpowers contributors`）。 |
| `tools/tarkov-runtime-bridge/LICENSE:3` | 保留-署名 | D7：同上，桥子项目 LICENSE 保留上游行 + fork 行。 |
| `skills/using-spt-modding-superpowers/SKILL.md:155` | 保留-署名 | `Forked from: bgs-modding-superpowers (…)` 是谱系署名，规格 §6 明示保留。 |

### 事件记录（旧名是事件主体）

| path:line | 分类 | 理由 |
|---|---|---|
| `RELEASE-NOTES.md:23` | 保留-事件 | 记录"移除物化的 `plugins/bgs-modding-superpowers` 树"——那棵树当年确实叫此名。 |
| `RELEASE-NOTES.md:93` | 保留-事件 | 记录"幽灵物化契约退役"清理的 6 路径之一；旧名是被清理对象。 |
| `docs/dev-log.md:45` | 保留-事件 | BGS 残留清理条目；旧名是清理对象（编排者负责本文件，本轮不改）。 |
| `docs/dev-log.md:155` | 保留-事件 | 幽灵物化退役条目；旧名是清理对象（同上）。 |
| `docs/internal/specs/session-wiring-contract.md:38` | 保留-事件 | 契约表的"已退役路径"行：`plugins/bgs-modding-superpowers/` + 退役日期 2026-09-16，属历史事实行。 |
| `docs/全局路线与进度报告.md:105` | 保留-事件 | 历史进度行（删除线条目）：旧插件文件名为事件主体；现态已补于同行的 `spt-modding-superpowers.js` 说明。 |
| `.scratch/c4-session-wiring/spec.md:15` | 保留-事件 | 事件记录："残留（已清理）"清单，旧树名是清理对象。 |
| `.scratch/spt-only-cleanup/baseline.md:14` | 保留-事件 | 清理前基线测量表（13,301 文件），旧树名是测量对象。 |

### 历史计划 / 历史设计（旧名是计划主体）

| path:line | 分类 | 理由 |
|---|---|---|
| `docs/internal/superpowers/plans/2026-05-31-reshape-to-superpowers-plugin-shape.md:1,22,24,42,43,64,66,94,108,161,165,169,198,213` | 保留-历史计划 | 该计划标题即"Reshape `awesome-bgs-mod-master` → …"；旧名/旧路径是计划主题，归一其指针即篡改史实。 |
| `…2026-05-31….md:260,264,268,271` | 保留-历史计划 | 规格 S1a 明示"清理对象主体类 4 行保留"（P5 目标的描述对象）。 |
| `…2026-05-31….md:249,250,290,321` | 已归一 | S1a 本轮已改为当前仓库路径 `E:\云文件\GitHub\SamMeow-modding-superpowers`（命令示例指针类），故不再命中。 |
| `…2026-05-31….md:104,183,320` | （不命中） | `bump-version` 历史引用，不含三模式，规格 S1a 明示不动。 |
| `docs/internal/superpowers/plans/2026-06-01-cc-followup-mcp-and-skill-fixes.md:17,47,61` | 保留-历史计划 | 历史计划中的旧 skill 路径 `using-bgs-modding-superpowers` 是当时修改对象。 |
| `docs/internal/superpowers/plans/2026-06-02-agentic-cross-game-kb.md:88,89,90,108,138,145,151` | 保留-历史计划 | 历史计划中的旧 harness/旧 skill 名是当时修改对象（bgs-kb 战线）。 |
| `docs/internal/superpowers/specs/2026-06-02-agentic-cross-game-kb-design.md:183,285,559` | 保留-历史计划 | 历史设计：`owner` 字段值、`%LOCALAPPDATA%` 缓存目录名，均为当时设计事实。 |
| `docs/internal/superpowers/specs/2026-06-25-spt-modding-superpowers-design.md:6,14,63,65,393,399,410,655` | 保留-历史计划 | SPT 改造设计文档：旧名是"来源项目/改造对象"，属谱系叙述。 |
| `docs/internal/specs/2026-08-02-spt-knowledge-rescue-design.md:13` | 保留-历史计划 | 历史设计："基于 bgs-modding-superpowers 改造版流程"，谱系引用。 |
| `docs/wayfinder/MAP.md:12` | 保留-历史计划 | wayfinder 地图："on the bgs-modding-superpowers skeleton"，谱系叙述。 |
| `docs/wayfinder/tickets/003-define-spt-skill-inventory.md:28,53` | 保留-历史计划 | wayfinder 工单：以旧 skeleton 的 skill 清单为参照，历史依据。 |

### 超范围（tests 域 / 注释夹具，只报告不改）

| path:line | 分类 | 理由 |
|---|---|---|
| `tests/bootstrap/verify-bootstrap-injection.ps1:19,20` | 超范围 | tests 域；且为负向断言的针（意图保留，用于断言插件不含旧身份）。 |
| `tests/mo2-vfs-launcher/xedit-client.mo2-sandbox-real.test.ps1:13,97` | 超范围 | tests 域：真实沙箱测试的硬编码路径夹具，规格明示只报告不改。 |
| `tools/mcp-kit/src/schema.ts:76` | 超范围 | 源码注释中的历史文档路径（`D:\awesome-bgs-mod-master\AGENTS.md`），规格明示只报告不改。 |
| `tools/mo2-mcp-sidecar/tests/test_archive_safety.py:112,222` | 超范围 | tests 域：夹具路径。 |
| `tools/mo2-mcp/tests/configure-executable-path-encoding.test.ts:15` | 超范围 | tests 域：注释夹具路径。 |

## 计数汇总

| 分类 | 条数 |
|---|---|
| 已归一（本轮 S1a，已从命中集消失） | 4 |
| 保留-事件 | 8 |
| 保留-署名 | 3 |
| 保留-历史计划 | 43 |
| 保留小计 | 54 |
| 超范围 | 8 |
| **grep 命中总数** | **62** |

## 范围说明

- `git grep` 只覆盖 tracked 文件。未跟踪的 `.scratch/c5-release-identity/spec.md` 自身含 7 行模式命中（:13/:26/:28/:30/:105/:111/:167），系规格的数据源定义与侦察记录，属超范围（未跟踪规格文件，非交付面）。
- 操作性表面扫描（规格 §5）结果为空：`git grep -in -E "…" -- scripts/ package.json .opencode/ tools/mo2-control-plane/ ":(glob)tools/*/package.json"` exit 1（无命中）。
- 本轮归一动作（S1a/S4/N4p）不新增命中；S4 的 `<owner>`、N4p 的日期均不在三模式内。
