---
version: [4.1]
domain: server
topic: index
source: curated
---
# SPT 4.1 服务端 API 笔记（源码提炼）

> 状态：首轮提炼完成 + 2026-09-06 对照 4.1.5 fork 源码实读核对（`E:\云文件\GitHub\SamMeow_SP-Tushonka_source_code`，commit `7d7add55`）
> 适用版本：[4.1] | 源码：`E:\云文件\GitHub\SamMeow_SP-Tushonka_source_code`（fork，目录 `SPTushonka.*`，命名空间仍 `SPTarkov.*`）

这些是 wiki 上没有、只有读源码才能得到的硬情报。每篇笔记标注源码文件坐标（路径 + 类名），源码更新后按坐标复查。

## 笔记规划

| 笔记 | 要回答的问题 | 源码入口线索 |
|------|------------|-------------|
| architecture-map.md | 系统整体架构、启动链、请求链、模块职责地图 | `SPTushonka.Server/Program.cs`、`DependencyInjectionHandler.cs` |
| di-container.md | 服务怎么注册与解析？mod 能拿到哪些服务？ | `SPTushonka.DI/DependencyInjectionHandler.cs` |
| mod-loading.md | mod 加载顺序、mod.json 解析、依赖与版本约束检查 | `SPTushonka.Server/Modding/ModLoader.cs` |
| config-system.md | SPT 自身 config 的加载与覆盖机制，mod 如何读 config | `Core/Loaders/ConfigLoader.cs` |
| database-structure.md | 内存数据库（物品/商人/任务/地图）的表结构与访问接口 | `Core/Utils/ImporterUtil.cs`、`DatabaseTables` |
| http-routing.md | 自定义路由怎么挂？客户端请求如何到达 mod | `Core/DI/Router.cs`、`Routers/HttpRouter.cs` |
| save-profile.md | 存档结构、读写时机、mod 持久化数据放哪 | `Core/Servers/SaveServer.cs`、`Core/Migration/` |

## 写作规范

- 每个结论附源码坐标：`文件路径:类名`（行号会变，不记行号）
- 区分「公开 API（官方意图给 mod 用）」与「内部实现（能用但可能随版本变）」
- 与 `wiki/SPT_41/Server_40_to_41.md` 交叉引用，不重复抄写
