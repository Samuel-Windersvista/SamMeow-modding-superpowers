# C6 · 状态权威（State Authority）— 实施规格

> **进展（Work Status）**: CLOSED — 2026-09-16（两车道交付；bootstrap 9/9 全绿含新 doc-stats 机检；车道 A 会话异常终止于汇报阶段，产物经编排者复核与全量验证；未提交）

> 来源：架构审查候选 C6 + grilling 闭合决策（2026-09-16）。
> 车道 A（fixer）：数字治理 + 机检脚本 + VERSIONS 环境段。
> 车道 B（orchestrator 直接编辑）：docs/README.md 索引 + HISTORICAL 横幅 + .scratch 状态行 + dev-log 补记。

## 决策（Q1–Q5，全部按推荐）
- Q1-A：新建 `docs/README.md` 总索引 + 权威分工表（车道 B）
- Q2-A：权威映射（dev-log=状态时间线 / .scratch=在办工作 / wayfinder=已决决策 / RELEASE-NOTES=发布摘要 / docs/README=总索引）；全局路线报告加 HISTORICAL 横幅（车道 B）
- Q3-B+A：数字最小化 + 锚点机检（车道 A）
- Q4-A：历史面原位标记（车道 B）
- Q5-A：VERSIONS 环境段更新为实测值（车道 A）

## 车道 A 任务（fixer，文件范围：README.md / RELEASE-NOTES.md / docs/使用指南.md / knowledge/spt-kb/VERSIONS.md / scripts/verify-doc-stats.ps1 / tests/bootstrap/verify-all.ps1）

### A1. 新增 `scripts/verify-doc-stats.ps1`（数字锚点机检）
- 至少 3 个锚点，模式统一：正则解析文档声称值 → 文件系统实测 → 断言相等；不一致 → 非零退出 + 醒目输出（文档、锚点、声称值 vs 实测值）。
  - a. skills 数量：README 技能表声称 == `skills/` 子目录数
  - b. index.json 条目数：文档中出现处（若有）== `knowledge/spt-kb/index.json` 的 entries 数
  - c. tools 子工程数：文档中出现处（若有）== `tools/` 子目录数（排除 README.md）
- 锚点在文档中不存在（最小化后）→ 跳过并打印说明，不误报。
- 接入 `tests/bootstrap/verify-all.ps1`（新增一项检查，保持既有 8 项全绿）。

### A2. 数字最小化修正
- `README.md`：技能表删除 `using-spt-translator` 幽灵行（该技能不存在）；「16 个 skill」→ 与实际一致（15）；KB 表易漂移计数改为定性表述或指向事实源（保留少量必须数字，供 A1 机检）。
- `RELEASE-NOTES.md`：「index.json 106 条」修正或改定性；「7 个工具」→ 8（或改定性）；第 68 行「插件注册处注入 KB-root 环境变量」更新为运行时布局解析（C1）表述；第 55 行 `using-spt-translator` 行标注「未实现（架构审查 C6 记录）」或删除。另在「未发布」段新增简短小节「运行时布局契约（C1）」：共享解析器 + 响亮失败（kb_unavailable / spt_health）+ 便携包携带知识层。
- `docs/使用指南.md`：快速漂移扫描（106 / 71 条 / 16 skills / 7 工具 等模式），修正。
- `knowledge/spt-kb/VERSIONS.md` 环境段（56-66 行）：实测本机 SPT_41x / SPT_5xx 版本（读 `SPT_Runtime` 的 `SPTarkov.Server.Core.dll` FileVersion 或启动日志），改写为「环境状态（2026-09-16 核实）」；路径更正（不再写 SPT_410）；不确定的项如实标注。

### A3. 验证
- `powershell -NoProfile -ExecutionPolicy Bypass -File tests/bootstrap/verify-all.ps1` 全绿（含新检查）。
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-doc-stats.ps1` 单独运行输出正常（声称值==实测值）。

### 红线
- 不 commit；不碰车道 B 文件（`docs/README.md`、`docs/全局路线与进度报告.md`、`docs/wayfinder/**`、`.scratch/**`、`docs/dev-log.md`）。
- 只做修正与最小化，不改文档结构/章节顺序；历史段落保留（除明确过时表述）。

## 车道 B 任务（orchestrator）
- `docs/README.md`（新）：docs 总索引（逐条状态标签）+ 权威分工表。
- `docs/全局路线与进度报告.md` 顶部 HISTORICAL 横幅（注明日期与去向）。
- `docs/wayfinder/MAP.md` 顶部 HISTORICAL 横幅。
- `.scratch/<slug>/` 各 slug 顶部状态行（ACTIVE/CLOSED + 日期）。
- `docs/dev-log.md` 补记：架构审查交付 + C1 完成 + C6 条目。
