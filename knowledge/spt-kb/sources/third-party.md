# 第三方资料登记与应急预案

最后更新：2026-08-02

## 应急预案：GitHub 仓库消失后的 wiki 抓取通道

wiki.sp-tarkov.com 是 Wiki.js 实例，若 `sp-tarkov/wiki` 仓库被删/转私有，可用以下备选通道：

1. **Wiki.js GraphQL API**：`https://wiki.sp-tarkov.com/graphql`
   - Wiki.js 默认暴露 GraphQL，`pages { list { path title } }` 可枚举页面，`pages { single(id) { content } }` 可取 Markdown 源（站点 editor 为 markdown）
   - 匿名权限取决于站点配置（当前匿名可读页面，source.read 为 false，渲染内容可读）
2. **HTML 渲染页抓取**：直接 GET 页面 URL，正文在 `<template slot="contents">` 内，可转 Markdown
3. **archive.org 快照**：`https://web.archive.org/web/*/wiki.sp-tarkov.com` 作为最后兜底

## 其他官方触点

| 资源 | URL | 说明 |
|------|-----|------|
| SPT 官网 | https://www.sp-tarkov.com/ | 内容源在 `G-网站与维基/sp-tarkov-website/` |
| SPT Forge（模组站） | https://forge.sp-tarkov.com/ | 源码在 `F-数据与工具/forge/`（可复现站内 mod 元数据结构） |
| Discord | http://discord.sp-tarkov.com/ | 社区讨论存档获取难度高，暂记渠道 |

## Forge 模组站选择性抓取计划

> 状态：**已执行（2026-08-02）**，产物见 `../archive/forge/`
> 决策：成品与源代码都要。已落地：全站目录 1822 条、95 个热门 mod 详情与全版本历史、95 个成品 zip（约 398MB）、18 个源码仓库 clone。抓取脚本在 `../archive/forge/tools/`，可重跑续抓。

```
archive/forge/
├── pages/       （未单独抓取——详情全文已在 api/hot-mods/<id>.json 的 description 字段）
└── mods/        mod 本体
    ├── <modid>-<version>_release/    成品 zip（best version 下载）
    └── <modid>-<version>_source/     源码 clone（18 个）
```

## 第三方教程 / 社区资料

> 抓取的正文放 `../archive/`，此处只登记出处。

| 资料 | 来源 | 状态 |
|------|------|------|
| WTT_Vol1 教程 | 已收录于 wiki（`wiki/modding/tutorials/WTT_Vol1.md`） | 已有 |

## 结构化游戏数据源：tarkov.dev（live 参考）

> 登记于 2026-09-02。GraphQL 端点 `api.tarkov.dev/graphql` 因上游 Issue #474 故障，实际使用 REST dump 通道 `json.tarkov.dev`。

| 项 | 内容 |
|----|------|
| 官网 | https://tarkov.dev/（API 文档 https://tarkov.dev/api/） |
| dump 通道 | https://json.tarkov.dev（端点清单 `/endpoints`，游戏模式 regular/pve/pvp-season） |
| 源码 | https://github.com/the-hideout/tarkov-api（GPLv3，package.json 声明 ISC 不一致）；数据管理 https://github.com/the-hideout/tarkov-data-manager |
| 数据来源链 | tarkov-changes.com 数据挖掘（live 客户端）+ EFT fandom wiki + 社区仓库 + 自营跳蚤扫描器 |
| 数据面 | 5312 物品（26 种属性类型）/ 212 弹药 / 517 任务（20 种目标类型）/ 789 易物 / 17 地图全点位 / 9 商人 / 藏身处生产链；17 语言本地化（含 zh） |
| 快照落点 | `../archive/tarkov-dev/snapshot-2026-09-02/`（全量 dump 入 git，抓取脚本 `fetch-dump.ps1`） |
| 对应 live 版本 | EFT 1.1.0.1.46911（2026-08-24 记录）；**SPT 4.1.2 锁定 0.16.9.5.40743，数据仅作 [live-ref] 参考** |
| 已知缺口 | Issue #481：商人枪械报价仅 18/171；dump 的 name 为 `<itemId> Name` 占位 key，需 `_zh`/`_en` 字典 join |
| 排除了 | 跳蚤价格历史（`/prices`）、服务器状态（`/status`）等 live 特有数据；pve/pvp-season 模式未抓 |

## EFT 世界观 / 背景知识来源（lore 分区）

> 对应 `../curated/lore/` 分区。lore 文档为提炼性质（非全文搬运），provenance 逐文档标注。登记于 2026-08-16。

| 来源 | URL | 用途 | 抓取通道 |
|------|-----|------|---------|
| 逃离塔科夫中文 Wiki | https://www.eftarkov.com/ | 主线剧情章节、门票结局、破冰船任务线、商人任务页、删档历史（中文一手） | webfetch 页面直抓（已验证可用） |
| EFT 官方 Fandom Wiki | https://escapefromtarkov.fandom.com/wiki/Escape_from_Tarkov_Wiki | 派系（USEC/BEAR/Scav/Cultists）、TerraGroup、地图 lore、商人背景、术语 | 根 URL 403；走 MediaWiki API（`/api.php?action=query&prop=revisions&rvprop=content&rvslots=main&format=json&titles=<Page>`）或 websearch 摘录通道 |
| Wikipedia | https://en.wikipedia.org/wiki/Escape_from_Tarkov | 客观时间设定（2015-2026）、地图清单、Russia-2028 宇宙、开发史 | webfetch 直抓 |
| IGN Wiki（Story Chapters） | https://www.ign.com/wikis/escape-from-tarkov/Story_Chapters | 1.0 九章主线触发条件核对 | webfetch 直抓 |
| NamuWiki（Contract Wars/Storyline） | https://en.namu.wiki/w/Contract%20Wars/%EC%8A%A4%ED%86%A0%EB%A6%AC%20%EB%9D%BC%EC%9D%B8 | 前传剧情（Norvinsk SEZ 由来、冲突起因） | webfetch（参考级，非主源） |
