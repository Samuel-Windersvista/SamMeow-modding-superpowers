# 11: 试点合规报告 ×3 + 反向校准

**What to build:** 对三个自研 mod（WarsawTrader / tarkov-active-probe / NoStaminaDrain）各出一份逐规则合规报告（PASS / FAIL / N-A + 修复建议），存 `.scratch/modding-standard/pilot/`。**本票允许修改规则文件**：试点发现规则不合理（无法遵守 / 与机制矛盾 / 证据不足）时修订规则并在 `evidence-index.md` 记录变更——校准闭环是交付的一部分。

**Blocked by:** 02, 03, 04, 05, 06

**Status:** done

- [x] 三份合规报告完成，覆盖全部已发布规则（或注明 N-A 理由）
- [x] 发现的规则问题已修订（或记录为已知豁免）
- [x] 规则修订在 evidence-index 留痕（改了什么、为什么）
- [x] 报告作为规范的首批应用样例，可供后续引用

## Comments

**2026-09-14 完成（agent）**

- 三份报告（`.scratch/modding-standard/pilot/`）：`warsaw-trader.md`（PASS 31 / FAIL 4 / N-A 49）、`tarkov-active-probe.md`（23 / 6 / 55）、`spt5-no-stamina-drain.md`（19 / 7 / 58）；84 条逐规则全覆盖。
- 校准修订 8 组（R1–R8）已应用并留痕于 `evidence-index.md` §6「校准记录（EV-CALIBRATION）」：
  - R1 5.0 客户端形态（BUILD-002/003、CLI-001/006/007：`net6.0` / `BepInEx/interop/` / `BasePlugin`+`Load()` / `ManualLogSource` / `Dispose()`）
  - R2 monorepo 作用域（STRUCT-001/002/005/006、PKG-006）；R3 `.gitignore` 清单明确化；R4 CFG 边界（配置 vs 随包只读数据）；R5 SRV/LOG 边界情形；R6 PKG/VERIFY 适用性；R7 BUILD-004 分级收敛；R8 BUILD-006 属性名可自定义。
- 未采纳：逐规则 `Domain` 字段（记录于 EV-CALIBRATION，未来可选项）。
- FAIL 归因：集中于仓库卫生文件（README / LICENSE / `.gitignore`）与边界情形；作用域/口径类误判经 R2–R6 澄清，真实缺口保留为 mod 侧修复建议。
- 终验：84 规则 / 0 重复 / 五要素齐全 / 链接与锚点全解析。
