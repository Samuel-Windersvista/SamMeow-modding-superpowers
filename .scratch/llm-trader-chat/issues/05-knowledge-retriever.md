# 05: 检索层（关键词实现 + 适配口）

**What to build:** 知识检索层：抽象接口（即"检索后端适配口"，后续嵌入实现不改接口）＋ 关键词/子串实现。行为：读取 per-trader 文档目录（md/txt），按标题/段落分块，返回 top-k 片段并带来源标识（文件名/标题）；中文按字符级/双字匹配兜底；空库与超大目录有明确行为（不崩溃、有上限）。单测覆盖。

**Blocked by:** 01: 项目脚手架

**Status:** ready-for-agent

- [x] 单测（fixture 文档目录）：命中、排序、top-k 上限、空库、中文匹配
- [x] 返回片段带来源标识，可供 prompt 引用标注
- [x] 无外部服务依赖；超大目录有明确上限行为
- [x] 接口形状为后续嵌入实现预留（同一抽象可替换）

## Comments

- 2026-10-14：工单收口。交付：`src/Server/Knowledge/*`（IKnowledgeRetriever 适配口 / KnowledgeSnippet / KnowledgeRetrieverLimits / KeywordKnowledgeRetriever）。测试：10 条（含修订轮新增 MaxFiles 小上限用例）。oracle 双轴评审：修订后收口；已知边界记录在案——CJK 双字跨词误报与单字符/数字漏报（现状可接受，M3 试点文档集评估是否升级嵌入后端）；片段长度总量控制在 06 prompt 组装处处理。未提交。
