# 12: 机械自检（S3）+ index 收尾

**What to build:** 对规则集做机械检查（S3 接缝）：① Rule ID 唯一性（全文件扫描无重复）；② MUST 规则的证据完整性（对照 `evidence-index.md` 核验双源）；③ `Applies` 版本标签齐全（4.1.5 / 5.0 / both）；④ README 索引与实际文件一致；⑤ index.json 全量登记核对。检查方式：核对清单或小脚本（结果记录在票内 Comments）。

**Blocked by:** 07, 08, 09, 10, 11

**Status:** done

- [x] ID 唯一性检查通过（或冲突已修复）
- [x] MUST 双源证据核验通过（缺失项已补或降级）
- [x] 版本标签齐全
- [x] index.json 与文件树一致，README 索引无悬空
- [x] 检查结果与任何修复记录在票内 Comments

## Comments

**2026-09-14 完成（agent，S3 机械自检）**

检查脚本（PowerShell，对 `knowledge/spt-kb/curated/modding-standard/` 全量扫描）结果：

| # | 检查项 | 结果 |
|---|--------|------|
| ① | Rule ID 唯一性 | 84 条 / 0 重复（STRUCT 7 / META 7 / BUILD 6 / SRV 8 / CLI 7 / CFG 6 / LOG 5 / DEP 5 / PKG 7 / VERIFY 9 / VER 3 / BND 6 / PERF 8） |
| ② | MUST 双源证据（机制 + 语料） | 28 条 MUST 全部双源；0 缺失 |
| ② | 无语料先例登记 | 22 条标注规则全部在 `EV-NOCORPUS` 可检索；0 未登记 |
| ③ | Applies 版本标签 | 84 条全部为 `4.1.5` / `5.0` / `both`；0 非法值 |
| ④ | README 索引 vs 文件树 | 16 文件；README 15 个链接 0 悬空 |
| ⑤ | index.json 全量登记 | 16/16 登记、0 缺失、0 多余 |
| ⑥ | 附加：链接 / 锚点 / 五要素 / 代码围栏 | 0 悬空链接；2 个锚点「未解析」为 `EV-CORPUS-*` / `EV-GAP-*` 通配符写法误报；五要素 13/13 文件齐全；围栏全配对 |

**本次检查发现并修复**：`EV-NOCORPUS` 规则级登记表原用缩写记法（如 `STD-SRV-002 / -006 / -007`、`STD-VERIFY-002 – -009`），完整 Rule ID 机械检索不可命中（11 条误报未登记）→ 已展开为完整 ID 列表，复检 0 未登记。