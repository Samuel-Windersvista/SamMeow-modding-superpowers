# SPT 知识库 (spt-kb)

为 SPT 4.1 mod 开发与整合包搭建服务的抢救性知识库。
建立日期：2026-08-02 | 背景：SPT 项目可能停止运作，本库为资料抢救 + 提炼的产物。

## 目录导航

| 目录 | 内容 | 何时查阅 |
|------|------|---------|
| [`INDEX.md`](INDEX.md) | 全局主题索引 | 找任何资料的入口 |
| [`VERSIONS.md`](VERSIONS.md) | SPT 版本地图（3.11 / 4.0 / 4.1） | 确认某资料适用哪个版本 |
| [`wiki/`](wiki/) | 官方 wiki 全站 Markdown vendor 副本 | 查官方文档原文 |
| [`curated/`](curated/) | 提炼层：重组指南、API 笔记、任务配方、live 参考数据 | 实际写 mod 时 |
| [`sources/`](sources/) | 资料来源登记册（仓库清单、第三方资料、应急预案） | 追溯出处、更新资料 |
| [`archive/`](archive/) | Forge 快照、tarkov.dev 数据快照、第三方教程 | 查社区资料 |

## 本地相关资产（本仓库之外）

| 资产 | 路径 |
|------|------|
| SPT 官方仓库归档（20 个全量 clone） | `E:\云文件\GitHub\SPT-archive\` |
| SPT 4.1 服务端源码（本地 fork） | `E:\云文件\GitHub\SamMeow_SPT410_source_code` |

## 使用原则

1. `wiki/` 是只读原文，修改请写到 `curated/`
2. 所有 curated 文档必须标注版本标签（见 VERSIONS.md）
3. 引用外部资产时使用 `sources/repositories.md` 登记的路径，不凭记忆写路径
