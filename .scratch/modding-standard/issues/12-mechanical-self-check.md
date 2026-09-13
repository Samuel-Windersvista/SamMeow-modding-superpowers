# 12: 机械自检（S3）+ index 收尾

**What to build:** 对规则集做机械检查（S3 接缝）：① Rule ID 唯一性（全文件扫描无重复）；② MUST 规则的证据完整性（对照 `evidence-index.md` 核验双源）；③ `Applies` 版本标签齐全（4.1.5 / 5.0 / both）；④ README 索引与实际文件一致；⑤ index.json 全量登记核对。检查方式：核对清单或小脚本（结果记录在票内 Comments）。

**Blocked by:** 07, 08, 09, 10, 11

**Status:** ready-for-agent

- [ ] ID 唯一性检查通过（或冲突已修复）
- [ ] MUST 双源证据核验通过（缺失项已补或降级）
- [ ] 版本标签齐全
- [ ] index.json 与文件树一致，README 索引无悬空
- [ ] 检查结果与任何修复记录在票内 Comments
