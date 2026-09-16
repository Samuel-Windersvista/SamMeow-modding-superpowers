# docs/ —— 文档索引与权威分工

> 找不到东西时从这里出发。状态标签：**入口** / **现行** / **参考** / **计划** / **HISTORICAL**。
> 时效性判断：**HISTORICAL 只作历史记录，不反映现状**；现状以 `dev-log.md` 与 `.scratch/` 为准。

## 权威分工（谁说了算）

| 系统 | 管什么 | 位置 |
|------|--------|------|
| 状态时间线 | 当前运行状态、已完成工作的权威记录 | `docs/dev-log.md` |
| 在办工作 | 活跃 feature 的 spec / 票据（含进展状态行） | `.scratch/<slug>/` |
| 已决决策 | 架构决策（ADR 现行；wayfinder 票据为历史） | `docs/adr/`（现行）+ `docs/wayfinder/`（HISTORICAL） |
| 发布摘要 | 对外发布说明 | `RELEASE-NOTES.md`（仓库根） |
| 文档地图 | 本索引（定位 + 新鲜度标签） | `docs/README.md`（本页） |

> 历史面（wayfinder、旧路线报告）保留原位、不再更新；状态一律以 dev-log / .scratch 为准。

## 入口

| 文档 | 用途 | 状态 |
|------|------|------|
| `使用指南.md` | Overseer 快速上手（工具链全貌、常用流程） | 入口 |
| `dev-log.md` | 状态时间线（按时间升序，最新在末尾） | 现行 · 权威 |
| `adr/` | 架构决策记录（ADR-0001 … 0008） | 现行 |
| `agents/` | agent 技能配置（issue tracker / triage labels / domain） | 现行 |

## 设计文档

| 文档 | 用途 | 状态 |
|------|------|------|
| `internal/` | 内部设计（`mcp-specs/`、`specs/`、`standards/`、`hook-specs/`；含 BGS 时代 `superpowers/` 遗留） | 现行（部分历史） |
| `internal/specs/session-wiring-contract.md` | 会话接线契约（插件行为面 / config.mcp 清单 / 不物化声明） | 现行 · 权威 |
| `research/` | 调研资料（`spt-runtime-state-export.md`） | 参考 |

## 报告（审计 / 可行性 / 性能）

| 文档 | 内容 | 状态 |
|------|------|------|
| `全局路线与进度报告.md` | 2026-08-19 全局战线快照 | HISTORICAL |
| `可行性研究报告-SPT整合包自动化搭建.md` | v3.0（3.11.4 基线，2026-05-28） | HISTORICAL |
| `feasibility-311-to-41-migration.md` | 3.11→4.1 迁移可行性（2026-08-05） | 参考 |
| `feasibility-auto-migration-pipeline.md` | 全自动移植能力（v1.2，已审阅，2026-08-05） | 参考 |
| `feasibility-tarkov-dev-kb-ingestion.md` | tarkov.dev 数据源纳入计划（2026-09-02） | 计划（待审） |
| `bundle-difference-audit.md` | bundle 差异查证（2026-08-05，实测） | 参考 |
| `exploration-bundle-and-3d-pipeline.md` | bundle 升级 + 3D 管线探索（2026-08-05） | 参考 |
| `eft-0.16-性能分析与优化mod可行性报告.md` | EFT 0.16（3.11.4）性能分析（v3） | 参考 |
| `eft-0.16.9.5-spt412-性能复查报告.md` | 4.1.2 性能复查 + PerformanceTweaks 可移植性 | 参考 |
| `PerformanceTweaks412-实施计划.md` | 4.1.2 性能 mod 实施计划（2026-08-19） | 计划 |
| `eft-1.1.5-il2cpp-逆向可行性报告.md` | SPT 5.0 IL2CPP 逆向可行性（2026-09-13） | 参考 |
| `eft-1.1.5-ghidra-反编译报告.md` | Ghidra 原生逆向（2026-09-13） | 参考 |
| `eft-1.1.5-类名映射重建报告.md` | EFT 1.1.5 类名映射重建（2026-09-13） | 参考 |
| `spt-5.0x-dev-现状报告.md` | SP-Tushonka 5.0x-dev 现状（2026-09-05） | 参考（被下一条更新） |
| `spt-5.0-mod-api-能力评估报告.md` | 5.0 mod API 能力评估（2026-09-13） | 参考 |
| `SPT412-413-41xdev-差异报告.md` | 4.1.2 / 4.1.3 / 4.1x-dev 差异（2026-09-01） | 参考 |
| `SPT413-fork-源码审查报告.md` | 4.1.3 fork 源码体检（人话版） | 参考 |
| `spt-4.1.5-服务端代码审计报告.md` | 4.1.5 源码审计（2026-09-06） | 参考 |
| `server-optimization-audit-report.md` | 3114 / 415 服务端优化综合审计（2026-09-07） | 参考 |

## 决策记录（wayfinder，历史）

| 位置 | 内容 | 状态 |
|------|------|------|
| `wayfinder/MAP.md` | 2026-08-02 架构决策地图（8 票关闭） | HISTORICAL |
| `wayfinder/tickets/` | 决策票据 001–009 | HISTORICAL |
| `wayfinder/findings/` | 调研发现 001–006 | HISTORICAL |
| `wayfinder/handoff-*.md` | 迁移主线冻结快照（08-11 / 08-19 / 08-22） | HISTORICAL |
| `wayfinder/archive/` | 已归档票据（006 / 007） | HISTORICAL |
