---
version: [4.1]
domain: both
topic: environment
source: curated
---
# 环境与工具链 [4.1]

> 适用版本：[4.1] | 主要来源：`wiki/modding/Modding_Resources.md`、`wiki/SPT_41/modding/EnumExtensions.md`、`wiki/SPT_41/modding/server/Mod_Web_Pages.md`、`E-Mod开发示例/server-mod-examples/`

## 开发环境

| 组件 | 要求 | 说明 |
|------|------|------|
| .NET SDK | .NET 10（`net10.0`） | Mod Web Pages 项目示例使用 `net10.0`；服务端 mod 通常同样以 net10.0 为目标 |
| IDE | Rider / VS2022+ / VS Code | 任意 |
| 引用包 | `SPTarkov.Server.Core` 4.1.0（必需） | NuGet 包，版本必须与服务器匹配 |
| 引用包 | `SPTarkov.Server.Web` 4.1.0（Web 页面用） | 仅实现 `IModBlazorMetadata` 时 |
| 调试 | dnSpy | 客户端调试：`wiki/modding/tutorials/debug_dnSpy.md` |
| 客户端基础 | BepInEx + Harmony | 文档：docs.bepinex.dev / harmony.pardeike.net |

> 服务端会对齐 mod 构建所基于的 `SPTarkov.Server.Core` 版本，不匹配则拒绝加载（见 `wiki/SPT_41/Server_40_to_41.md` 开头警告）。

## 参考资源

### 官方
- 服务端 mod 示例集：`E-Mod开发示例/server-mod-examples/`（25 个项目，覆盖绝大多数常见场景）
- SPT 技术文档（DeepWiki 自动生成）：https://deepwiki.com/sp-tarkov/server-csharp/1-overview — 注意：此站为第三方镜像站，若 SPT 停止运作可能失效，必要时抓取存档

### 数据查询（写 mod 查 ID 用）
- SPT 物品数据库：https://db.sp-tarkov.com/（对应源码仓库 `G-网站与维基/db-website/`）
- Tarkov-Dev API：https://api.tarkov.dev/
- Tarkynator：https://tarkynator.com/

### IDE 插件
- Rider SPT ID 高亮插件：https://github.com/madmanbeavisx/spt-id-highlighter

### 物品创建教程
- `wiki/modding/tutorials/WTT_Vol1.md`（静态物品创建，Vol.1）
- 自定义武器 SDK 教程（Google Doc，外部链接，如失效需存档）

## 版本坐标备忘

- 4.1 服务端源码：`E:\云文件\GitHub\SamMeow_SPT410_source_code`（本地 fork，读源码是最终权威）
- 4.1 客户端模块源码：`A-核心服务端/modules/`
- 官方示例（4.0 时代，迁移见 `modding-guide/04-example-walkthroughs.md`）：`E-Mod开发示例/server-mod-examples/`

## 坑

- 官方示例仓库当前为 4.0 语法（`AbstractModMetadata`、`DatabaseService`、`PostDBModLoader`），照抄前先对照 4.1 迁移文档改写（见 04-example-walkthroughs.md）
- `db.sp-tarkov.com` 上游仓库已归档（db-website），若主站失效数据查询只能靠本地 SPT 安装的 database 文件夹
