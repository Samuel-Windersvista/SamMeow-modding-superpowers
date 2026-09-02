# SPT 资料抢救与知识库建设工程 — 设计文档

日期：2026-08-02
状态：已批准（初步版本，后续迭代）
背景：离线塔科夫 SPT 项目可能停止运作，需要抢在资料消失前完成收集与备份，并为未来基于 SPT 4.1 的 mod 开发建立知识库。

## 目标

1. 完整抢救 SPT 官方资料（wiki、GitHub 组织仓库、模组示例）
2. 在 SamMeow-modding-superpowers 仓库内建立结构化知识库 `knowledge/spt-kb/`
3. 知识库服务于两个未来场景：编写 SPT 4.1 的 mod；搭建 SPT 4.1 整合包（基于 bgs-modding-superpowers 改造版流程）

## 关键侦察结论

- wiki.sp-tarkov.com 是 Wiki.js，内容源直接托管在 `github.com/sp-tarkov/wiki`（Markdown 格式），git clone 即得全站原始 Markdown，无需爬虫
- sp-tarkov GitHub 组织共 21 个公开仓库
- 本地已有 SPT 4.1 服务端源码 fork：`E:\云文件\GitHub\SamMeow_SPT410_source_code`（fork 自 sp-tarkov/server-csharp）

## 架构：双层结构

### 第一层：原始归档层（不进 git）

`E:\云文件\GitHub\SPT-archive\` — 21 个仓库的全量 clone（含全部分支与历史）。体量大，只做本地留存 + 可选的 GitHub 异地冗余。

### 第二层：知识库层（随 superpowers 仓库版本控制）

```
knowledge/spt-kb/
├── README.md               导航入口 + 使用说明
├── INDEX.md                全局主题索引（面向 agent 检索）
├── VERSIONS.md             SPT 版本地图：3.11 LTS / 4.0 / 4.1 现状与差异
├── wiki/                   wiki 全站 Markdown vendor 副本
│   └── UPSTREAM.md         锁定上游 commit hash + 抓取日期
├── curated/                提炼层（核心价值）
│   ├── modding-guide/      按任务重组的 mod 开发指南
│   ├── api-notes-4.1/      从本地 4.1 源码提炼的 API/架构笔记
│   └── recipes/            常见任务配方（加商人、改物品、自定义任务等）
├── sources/
│   ├── repositories.md     21 仓库登记册：分级、用途、本地路径、锁定 commit
│   └── third-party.md      第三方教程/社区资料登记 + 应急预案
└── archive/                选择性抓取的 Forge 页面、第三方教程（Markdown 化）
```

## 仓库抢救分级

| 级别 | 仓库 | 理由 |
|------|------|------|
| Tier 1 立即 | wiki, server-mod-examples, mod-examples, modules | 写 mod 直接依赖的文档与范例 |
| Tier 2 优先 | server（旧 TS 版）, forge, assembly-tool, patcher, launcher | 3.11→4.1 过渡期对照资料 |
| Tier 3 兜底 | installer, build, bento, PatcherPizza, spt-item-finder, EftPatchHelper, sp-tarkov-website, bot-generator, db-website, loot-dump-processor, launcher-patchgen | 完整性存档 |

server-csharp 已有本地 fork，不重复 clone，在登记册记录路径与远程 URL。

## 提炼层规范

- wiki 内容按「任务为中心」重组进 curated/，而非照搬 wiki 页面结构
- 所有文档标注版本适用性标签：`[3.11]` `[4.0]` `[4.1]`
- api-notes-4.1 从本地源码提炼 wiki 没有的硬情报：DI 容器、mod 加载流程、config 系统、数据库结构

## 验证

- 每次 clone 后记录锁定 commit 进登记册
- wiki vendor 后抽查页面数量与线上导航对照
- 完成后跑知识库内部死链检查

## 应急预案

若 GitHub 仓库被删/转私有：Wiki.js 提供 GraphQL API（`/graphql`）可抓取渲染内容，作为备选抓取通道。写入 `sources/third-party.md` 备案。

## 执行顺序

1. 建 SPT-archive，批量 clone Tier 1 → 2 → 3
2. vendor wiki 进 spt-kb/wiki，写 UPSTREAM.md
3. 写 repositories.md 登记册 + VERSIONS.md 版本地图
4. 写 README/INDEX 骨架
5. curated 提炼层骨架（内容迭代填充）
6. Forge/第三方教程选择性抓取（最后做）
