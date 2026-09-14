# 05: bot 域（raid_bots：摘要 + 明细）

**What to build:** bot 域完整落地。默认摘要（总数 + PMC/Scav/Boss 分类计数）；`detail` 参数返回明细（每个 bot 的位置/类型/存活）；明细响应体积有上限保护。`raid_bots` 工具完成。

**Blocked by:** 03

**Status:** ready-for-agent

- [x] 摘要与明细两模式输出正确且确定性
- [x] live 核对：raid 内 bot 计数与画面/日志抽样一致
- [x] fake-bridge 测试覆盖边界（0 bot / 大量 bot / 字段缺省）
- [x] 明细响应体积有上限保护（上限或分页），文档说明

## Comments

### 2026-09-14 双 lane 实施 + live 核验（T05 完成，分类修正）

- **桥侧（fix-3）**：`/raid/bots`（摘要 + `?detail=1`；明细上限 200 + `truncated`）；分类 **role 优先**（live 修正：`pmcUSEC`/`pmcBEAR` 的 `side` 为 `Savage`，原 side 优先会把 PMC 误计为 scav）
- **MCP 侧（fix-4）**：`raid_bots`（`detail` 入参，摘要/明细两模式）
- **live 实证**：击杀 1 个 scav 后计数 **13→12** 精确反映（spawner 13→12 / allWithDelayed 16→15）；分类修正后 PMC 5 / Scav 11（另一局 2/10）；明细含位置/role/side/alive
- 测试：摘要/明细 + 边界（0 bot/截断）+ 确定性（fake）
