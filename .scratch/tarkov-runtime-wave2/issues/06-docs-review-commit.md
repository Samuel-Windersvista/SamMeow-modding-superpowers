# 06: 文档收尾 + 双轴评审 + 提交

**What to build:** 第二波收尾：桥 README / MCP README 增补事件与装备契约；dev-log 记录；双轴评审（Standards + Spec）；修复评审发现；提交（拆笔方式沿用 Phase 2 先例：桥 / MCP / 文档台账）。

**Blocked by:** 01, 02, 03, 04, 05

**Status:** ready-for-agent

- [x] README（桥 + MCP）增补：`/raid/events` 契约、事件语义（击杀=death 且 killer!=null）、装备字段、配置无新增项说明
- [x] dev-log 一条（第二波：交付/证据/偏差）——`docs/dev-log.md`「2026-09-15（晚）」条
- [x] 双轴评审完成，发现项修复或记录
- [ ] 提交（桥 / MCP / 文档台账三笔或按实际改动调整），工作区干净

## Comments

### 2026-09-15 收尾

- **README**：桥 README 增补 `/raid/events` 契约、事件来源与语义、装备字段、`since` 精确用法、补丁常开与「本波无新增配置项」；MCP README 增补 `raid_events` 工具、事件语义、since 语义、录制 method/args、「本波无新增环境变量」。
- **dev-log**：已加「2026-09-15（晚）— tarkov-runtime 第二波」条。
- **双轴评审**：@oracle ×2（Standards 9 项 / Spec 10 项）；修复分两 lane 落地——桥+机检+标准文档（S1/S2/S3/S4/S7/S8/S9/P-c4；单测 152/152、机检 PASS=13/WAIVED=1、四套模板 FAIL=0）与 MCP（P-c1/S5/P-a3/P-c3/P-c5b；237/237 + typecheck + build）。
- **待办**：live 验收收尾（工单 05）→ 最终构建部署 → 三笔提交。
