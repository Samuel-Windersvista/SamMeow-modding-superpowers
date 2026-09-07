---
version: [3.11]
domain: server
topic: index
source: curated
---
# SPT 3.11 服务端 API 笔记（源码提炼）

> 状态：**已核实（源码实读 2026-09-07）** | 版本：[3.11]
> 源码位置：`E:\云文件\GitHub\SamMeow_SPT3114_source_code`（仓库 `SPT-AKI 3.11.5-Live-In-Norvinsk-Edition`，基线 SPT-AKI 3.11.x；`server/project/package.json` 名称 `spt-server` 版本 `3.11.3`，`assets/configs/core.json` `sptVersion: "3.11.4"`）
> 路径别名：`@spt/*` → `server/project/src/*`（tsconfig paths）

这些是 wiki 里没有的、只有对源码才能得到的硬情报。每篇笔记标注源文件位置，便于源码更新后复核。
注意：本仓库是 3.11.x 分支（TypeScript 版服务端），**不是 4.x C# 版**。4.x 的笔记见 `api-notes-4.1/`。

## 笔记规划

| 笔记 | 要回答的问题 | 源码位置 |
|------|------------|-------------|
| architecture-map.md | 系统总体架构、启动链、请求链、目录职责 | `Program.ts`、`entry/run.ts`、`utils/App.ts` |
| di-container.md | 服务怎么注册、mod 能用到哪些 tag | `di/Container.ts`、`di/Router.ts` |
| mod-loading.md | mod 加载顺序、package.json 字段、版本约束、容错 | `loaders/PreSptModLoader.ts`、`loaders/PostSptModLoader.ts`、`loaders/PostDBModLoader.ts`、`loaders/ModLoadOrder.ts`、`loaders/ModTypeCheck.ts` |
| config-system.md | SPT 内建 config 文件与键名规则 | `servers/ConfigServer.ts`、`models/enums/ConfigTypes.ts` |
| database-structure.md | 内存数据库、JSON 文件路径 → 对象树机制 | `servers/DatabaseServer.ts`、`utils/DatabaseImporter.ts`、`utils/ImporterUtil.ts`、`models/spt/server/IDatabaseTables.ts` |
| http-routing.md | 自定义路由怎么挂、客户端的请求怎么到 mod | `routers/HttpRouter.ts`、`routers/ImageRouter.ts`、`routers/ItemEventRouter.ts`、`di/Router.ts`、`di/Serializer.ts` |
| save-profile.md | 存档结构、写盘时机、mod 持久化数据方案 | `servers/SaveServer.ts`、`callbacks/SaveCallbacks.ts`、`models/eft/profile/ISptProfile.ts` |

## 写作规范

- 每条结论标源码位置：`文件路径:行号`（行号随源码演进可能漂移，以文件名为准）。
- 区分「公开 API」（官方文档/模板，mod 用）与「内部实现」（不可依赖，版本间会变）。
- 与 `wiki/SPT_311/`、`curated/migration/` 交叉引用，不重复抄写。

## 关键勘误（与常见旧认知的差异，以源码为准）

1. **路由基类是 `Router`**（`di/Router.ts`），继承链为 `StaticRouter` / `DynamicRouter` / `ItemEventRouterDefinition` / `SaveLoadRouter`。**不存在 `ApiRouter` / `BaseRouter`**。
2. **生命周期接口是 `OnLoad` / `OnUpdate`**（`di/OnLoad.ts`、`di/OnUpdate.ts`），无 `I` 前缀。
3. **无 `PreDBModLoader`**：postDBLoad 由 `PostDBModLoader implements OnLoad` 充当，挂在 OnLoad 队列的第 3 位。
4. **无 `services/ModService.ts` / `ModSaveService` / `ModConfigService`**；mod 周期调度由 `App`（`utils/App.ts`）驱动，持久化方案见 save-profile.md。
5. **钩子执行顺序：preSptLoad → postDBLoad → postSptLoad**（postSptLoad 在 postDBLoad 之后，由 OnLoad 注册顺序决定，见 mod-loading.md）。
6. **package.json 常规字段是 `license`（美式）**，`licence`（英式）只在接口声明里出现、加载校验不读；`isMain` / `postLoad` / `tro` / `loadOrder` 字段**不存在**。
