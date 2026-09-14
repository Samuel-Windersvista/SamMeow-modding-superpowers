# 项目姿态调整：从抢救模式到现役工具链

状态：已接受（2026-09-14） | 决策者：Overseer | 范围：全仓库姿态与版本策略

## 背景

本仓库的创立前提是「SPT 项目可能停止运作」，因而承担资料抢救使命（官方 wiki、Forge 模组站、源码仓库的本地归档）。后续事态：2026-08-11 官方 sp-tarkov 组织归档全部仓库（4.1.2 为官方最后版本），社区 fork SP-Tushonka 成为事实延续；2026-09-14，**SPT 5.0 已由社区 fork 正式发布**。

抢救前提不复存在。归档资产仍有长期价值（参考、离线兜底、语料），但不再是"末日保险"。

## 决策

1. **抢救使命关闭。** 归档层（wiki vendor copy、Forge 归档、源码登记册）转为长期参考资产 + 离线兜底，常态维护，不再以项目停更为前提。
2. **版本策略重述。** 4.1.5 = 稳定开发基线（Modding Standard Dev-Baseline 不变，理由改为生态成熟/语料充分，而非版本终局性）；5.0 = 已发布新主线，双轨适配继续（`version-matrix.md` 管辖迁移差异）；3.11.x = 历史对照。原「最终目标 SPT 4.1，可能永久停留」表述作废。
3. **本地关联资产清单扩展为六项**：SPT-archive（20 仓 clone）、4.1.5 源码 fork、3.11.4 源码、5.x 源码、特化 MO2 源码（`SamMeow-Tarkov-specific-Mod-Organizer`）、特化 MO2 构建产物（`E:\build\spt-mo2\prefix\install\bin`）。
4. **ADR-0002 维持不变。** tarkov-runtime-mcp 只适配 5.x 的范围决策在 5.0 发布后反而成立；若未来需要 4.1.5 运行时支持，按 ADR-0002 预留的低成本回 ported 路径另行立项。
5. **Forge/sp-mod.com 策略不变，动机改写。** 本地归档仍是权威数据源，但理由从「唯一幸存副本」改为「离线可复现 + 抓取速率/ToS 合规」；live API（sp-mod.com API v0）作为补充通道存在。

## 后果

- 全仓库清扫「停止运作 / 抢救 / final locked / 预发布」等陈旧表述（README、VERSIONS.md、skills、docs、KB 框架文案）；历史文档（可行性研究报告、rescue 设计文档、wayfinder 记录）保留原样，以日期标注为历史。
- KB 中基于预发布快照的 5.0 技术结论（`api-notes-5.0/` 的 UNSTABLE 标记、`5xx-source-verification.md` 的状态结论、version-matrix 快照基线）**需按正式 release tag 复核**——列入后续工单，本 ADR 不代为改写技术结论。
- 开发基线是否/何时从 4.1.5 迁移到 5.x 属于 Modding Standard 的修订决策，不在本 ADR 范围。
