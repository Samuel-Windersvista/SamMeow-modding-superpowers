# 02: 桥侧归一化聚合 + /logs/summary

**What to build:** 已捕获日志按归一化键聚合（保守剥离易变 token：24 位 hex、GUID、纯数字、文件偏移），每组 `{key, level, source, count, firstTs, lastTs, sampleText}`；组数上限默认 500，溢出按最久未更新淘汰并计 `overflowDropped`；`GET /logs/summary?since=<ts>` 返回 `{groups, overflowDropped}`，`since` 按时间过滤新增/更新组。刷屏型错误（如数千条同错误）聚合为 1 组 + count，不再淹没信号。

**Blocked by:** 01

**Status:** ready-for-human

- [x] 归一化：剥离 24-hex/GUID/数字/偏移；保守策略——不同错误不误合并（xunit）
- [x] 聚合组字段与计数正确（count / firstTs / lastTs / sampleText）（xunit）
- [x] 组上限 500 溢出淘汰 + `overflowDropped` 计数（xunit）
- [x] `since` 时间过滤：仅返回新增/更新组（xunit）
- [x] 端点契约 `{groups, overflowDropped}` 字段序稳定、非 raid 可用、路由与 capabilities 更新
- [x] 读路径加锁安全（日志线程写 vs HTTP 线程读）
- [x] 既有 xunit 全绿

## Comments

### 2026-09-16 深夜 实现 + 验收（orchestrator）

- @fixer 实现（TDD）：`LogSummaryNormalizer` + `LogSummaryStore`（500 组上限、最久未更新淘汰、`overflowDropped` 累计、组序 count 降序→lastTs 降序→key 升序）+ `LogSummaryQuery`（since 宽松解析）+ `GET /logs/summary`。
- 关键：count 覆盖全部采集条目、不受环形缓冲 1000 淘汰影响（5000 条专项测试）；写路径与缓冲并列、独立加锁不嵌套。
- 评审修复轮并入：24hex 右边界放宽与 TS 侧同构（真实样本 `…3c2bs` 合并回归）、store 级死属性清理（评审记录见工单 06）。
- 全波套件：桥 **295/295**（评审修复后 orchestrator 独立复跑）；live 验收见工单 05。
