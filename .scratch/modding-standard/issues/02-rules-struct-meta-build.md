# 02: 维度 ①-③ 规则：STRUCT / META / BUILD

**What to build:** 撰写三个维度的规则文件——`01-structure.md`（仓库与目录结构：单仓库分层、`src/`/`Server/`/`Client/` 惯例、`bin/`/`obj/` 必须忽略、README/LICENSE 必备）、`02-metadata.md`（`IModMetadata` 字段与文件位置约定、GUID 反向域名、`SptVersion` tilde 区间、版本号联动）、`03-build.md`（server `net10.0` / client `netstandard2.1`、程序集引用方式、构建产物布局）。每条规则采用统一格式：`STD-XXX-nnn` 标题、MUST/SHOULD/MAY、`Applies` 版本标签、证据（机制引用 + 语料计数，引用 evidence-index）、内联关键样例、深入文档链接。

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] 三个维度文件完成，全部规则含 ID/分级/Applies/证据/样例五要素
- [ ] MUST 规则满足双源证据（机制+语料）；无语料先例的规则显式标注"机制推断，无语料先例"
- [ ] 关键代码样例内联（csproj 片段、ModMetadata 骨架）
- [ ] index.json 登记本票文件
