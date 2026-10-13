# 07: 进游戏验收与收尾

**What to build:** 用户进游戏按清单逐组验收（agent 提供清单并登记结果）；发现项处置或登记；移植记录收尾；发行归档 zip 至项目 `release/`。

**Blocked by:** 06（服务器启动核验）

**Status:** ready-for-agent

- [ ] 验收清单逐项核对：臂章重量与堆叠 / 背包尺寸与过滤 / 堆叠上限 / 藏身处建造免 FIR / 跳蚤入口与等级 / 商人收价与保险 / 战局时长倍率 / Collector 可接取（用户游玩期间渐进验证中）
- [x] 发现项逐条处置或登记（含复测）：F1 / F4 / F6 / F2 四项已部署并复验（F2 于 2026-10-13 重启复验 PASS）
- [x] 发行归档 `release/[5]软核大修-InescapableTarkovsSoftcore-0.1.0.zip`（含 DLL + config；结构 = overlay 树）
- [x] 移植记录完整（部署与验收结论已回填 port-notes §5）

## Comments

- 2026-10-13：发行归档完成：`release/[5]软核大修-InescapableTarkovsSoftcore-0.1.0.zip`（96,687 bytes；内含 `SPT_Runtime/user/mods/com.sammeow.inescapable-softcore/{InescapableTarkovsSoftcore.dll, config.json}`；DLL SHA256 头 `91B9C9E9…`）。
- 移植记录补记完成：port-notes §5 全面回填（部署与启动核验 / 四项修复登记 / F2 根因与复验证据 / T07 状态）。
- 验收清单（11 项）由用户在游玩期间渐进验证；异常项将回报并登记于 port-notes §5.3。
