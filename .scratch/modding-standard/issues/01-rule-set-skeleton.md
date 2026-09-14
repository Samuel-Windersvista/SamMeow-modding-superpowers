# 01: 规则集骨架 + evidence-index

**What to build:** 建立 `knowledge/spt-kb/curated/modding-standard/` 的物理结构：`README.md`（使用说明：Rule ID 规则 `STD-<DOMAIN>-<nnn>`、MUST/SHOULD/MAY 分级与证据标准、豁免流程 `Waiver: STD-XXX-nnn`、13 维度索引）；13 个维度文件骨架（`01-structure.md` … `13-perf-security.md`，含标题与维度说明）；`evidence-index.md`（固化 `.scratch/modding-standard/corpus-survey.md` 与 `gap-investigation.md` 的关键统计、命令与路径，作为全部规则的统一证据来源）；index.json 登记。

**Blocked by:** None (can start immediately)

**Status:** done

- [x] 目录与文件结构就位（README + 13 维度骨架 + evidence-index）
- [x] README 完整说明 ID 规则 / 分级标准 / 豁免流程 / 维度索引
- [x] evidence-index 收录两份调查的关键数据（计数、统计命令、文件路径），可被规则文件直接引用
- [x] index.json 登记新文件（含 version/domain/topic/source 字段）

## Comments

**2026-09-14 完成记录（agent）**

- 交付：`knowledge/spt-kb/curated/modding-standard/` 共 15 文件（README + 13 维度骨架 + evidence-index）；`index.json` 新增 15 条（205 → 220，字段齐全，`ConvertFrom-Json` 通过）。
- 验收：4 项 checkbox 全通过；证据坐标路径扫描 30/30 存在；README 链接 14/14 无悬空。
- code-review 双轴处置：
  - **Spec 轴**：验收面达标；保真度抽查 10 锚点一致。处置 2 项：移除 README「硬约束通道」（对齐 spec:47：MUST = 机制 + 语料双源；无语料先例按单源判 SHOULD）；二期检查器锚点写法修正为 `##`/`###` 级。
  - **Standards 轴**：处置 5 项：① 类型表「未能映射 1」复核为源报告误判（`[SAIN]-Twitch-Players_1895_source` 实际存在，MANIFEST `total=297 ok=297 missing=0`）→ 删行 + 复核注；② api-notes 5.0 计数 10→9（实际 9 文件，多 `modding-api.md`、少 `server-mod-metadata-dll.md`）；③ FieldKit 截断路径补全；④ Fair-Equipment 省略路径补全（`FairEquipmentRestoration/Config/...`）；⑤ README 版本矩阵标注「待交付」；另 evidence-index MUST 双源补「样本 ≥10 或全量检查」门槛。
  - 保留判断项：index.json title 无版本后缀（系列内自洽，评审判可接受）；骨架注释跨文件重复（骨架期特性，规则填充时替换）；`Waiver: STD-XXX-nnn` 占位符（spec:50 原文约定，不改）。
