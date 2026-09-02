---
version: [4.1]
domain: server
topic: index
source: curated
---
# SPT 4.1 服务端 API 笔记（源码提炼）

> 状态：首轮提炼完成（源自 4.1 迁移文档、EnumExtensions、Mod_Web_Pages 与官方示例；标注「待核对」处需对照本地 4.1 源码逐一验证）
> 适用版本：[4.1] | 提炼自：`E:\云文件\GitHub\SamMeow_SPT410_source_code`

这些是 wiki 上没有、只有读源码才能得到的硬情报。每篇笔记标注源码文件坐标（路径 + 类名），源码更新后按坐标复查。

## 笔记规划

| 笔记 | 要回答的问题 | 源码入口线索 |
|------|------------|-------------|
| di-container.md | 服务怎么注册与解析？mod 能拿到哪些服务？ | `SPTarkov.Server/` 下的 DI 启动配置 |
| mod-loading.md | mod 加载顺序、mod.json 解析、依赖与版本约束检查 | 加载器相关类 |
| config-system.md | SPT 自身 config 的加载与覆盖机制，mod 如何读 config | config 相关类 |
| database-structure.md | 内存数据库（物品/商人/任务/地图）的表结构与访问接口 | database 相关类 |
| http-routing.md | 自定义路由怎么挂？客户端请求如何到达 mod | server 路由层 |
| save-profile.md | 存档结构、读写时机、mod 持久化数据放哪 | profile/save 相关类 |

## 写作规范

- 每个结论附源码坐标：`文件路径:类名`（行号会变，不记行号）
- 区分「公开 API（官方意图给 mod 用）」与「内部实现（能用但可能随版本变）」
- 与 `wiki/SPT_41/Server_40_to_41.md` 交叉引用，不重复抄写
