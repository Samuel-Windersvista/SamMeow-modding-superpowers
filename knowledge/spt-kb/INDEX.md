# SPT 知识库全局索引

按「我想做什么」检索。最后更新：2026-08-02
> Agent 机读索引：`index.json`（按 version/domain/topic 过滤）

## 写服务端 mod (Server Mod)

| 任务 | 首选资料 | 版本 |
|------|---------|------|
| 入门：mod 类型与结构 | `wiki/Mod_Types.md`、`wiki/modding/Modding_Resources.md` | [通用] |
| 官方示例代码 | `external/spt-archive/server-mod-examples/`（登记见 sources/repositories.md） | [4.x] |
| 4.0→4.1 服务端迁移 | `wiki/SPT_41/Server_40_to_41.md` | [4.1] |
| 4.1 Mod 网页（Mod Web Pages） | `wiki/SPT_41/modding/server/Mod_Web_Pages.md` | [4.1] |
| 服务端 API 笔记（源码提炼） | `curated/api-notes-4.1/`（含 architecture-map.md 架构图） | [4.1] |
| 4.1.5 服务端完整源码 | `E:\云文件\GitHub\SamMeow_SP-Tushonka_source_code`（fork，目录 `SPTushonka.*`，命名空间 `SPTarkov.*`） | [4.1] |
| 4.1.5 源码审查报告（bug/优化） | `curated/operations/415-source-review-report.md` | [4.1] |
| 3.11 服务端实现对照 | `E:\云文件\GitHub\SamMeow_SPT3114_source_code`（SPT-AKI 3.11.5-Live-In-Norvinsk-Edition，基线 3.11.x） | [3.11] |
| 3.11 服务端 API 笔记（源码提炼） | `curated/api-notes-3.11/`（DI/路由/mod 加载/config/数据库/存档） | [3.11] |

## 写客户端 mod (Client Mod / BepInEx)

| 任务 | 首选资料 | 版本 |
|------|---------|------|
| 客户端 mod 快速指南 | `wiki/modding/tutorials/Client_Modding_Quick_Guide.md` | [通用] |
| 官方客户端模块源码 | `external/spt-archive/modules/` | [4.1] |
| 4.0→4.1 客户端迁移 | `wiki/SPT_41/Client_40_to_41.md` | [4.1] |
| 4.1 类名映射（混淆对照） | `wiki/SPT_41/modding/client/Class_Name_Mappings.md` | [4.1] |
| 4.1 枚举扩展 | `wiki/SPT_41/modding/EnumExtensions.md` | [4.1] |
| dnSpy 调试教程 | `wiki/modding/tutorials/debug_dnSpy.md` | [通用] |
| 示例代码 | `external/spt-archive/mod-examples/` | [3.11] |

## 数据参考（写 mod 常查的表）

| 数据 | 位置 |
|------|------|
| 商人 ID 与信息 | `wiki/modding/references/trader-information.md` |
| 任务（quest）数值参考 | `wiki/modding/references/quest-values.md` |
| 地图/场景信息 | `wiki/modding/references/location-information.md`、`map-scenes.md` |
| Bot 类型 | `wiki/modding/references/bot-types.md` |
| 身体部位 | `wiki/modding/references/body-part-reference.md` |
| 技能 ID | `wiki/modding/references/skills-reference.md` |
| 物品 ID 查询 | `E:\云文件\GitHub\F-数据与工具/spt-item-finder\`（工具，外部保留）、`E:\云文件\GitHub\G-网站与维基/db-website\`（外部保留） |
| **live 参考数据（tarkov.dev 提炼，[live-ref]）** | `curated/game-data-ref/`：弹药 `ammo.md`、护甲/头盔 `armor.md`、任务体系 `tasks.md`、地图点位 `maps.md`、易物/制作/藏身处 `barters-crafts-hideout.md`、商人库存 `traders.md`（原始快照 `archive/tarkov-dev/`） |

## 安装与整合包

| 任务 | 首选资料 |
|------|---------|
| 手动安装（4.0） | `wiki/SPT_40/Manual-Installation-Instructions_40.md` |
| 手动安装（3.11） | `wiki/SPT_311/Manual-Installation-Instructions_311.md` |
| 装 mod / 卸 mod | `wiki/Installing_Mods.md`、`wiki/Uninstalling_Mods.md` |
| 推荐 mod 清单 | `wiki/Recommended_Mods_40.md`、`wiki/SPT_311/Recommended_Mods_311.md` |
| 排障 50/50 法 | `wiki/5050-method.md` |
| 已知问题（SPT/mod/EFT） | `wiki/Known_SPT_Issues_40.md` 等三篇 |
| 性能调优 | `wiki/Performance_Tuning.md` |

## 任务配方（curated，持续填充）

见 `curated/recipes/` — 加商人、改物品、自定义任务等逐步配方。

## 资料溯源与应急

- 仓库清单与锁定 commit：`sources/repositories.md`
- 第三方资料与抓取应急预案：`sources/third-party.md`

## Forge 模组站归档

| 要什么 | 位置 |
|--------|------|
| 热门 mod 总索引（元数据+源码+版本） | `archive/forge/hot-index.json` |
| 全站 1822 个 mod 目录 | `archive/forge/api/mods-catalog.json` |
| 某 mod 详情/全版本历史 | `archive/forge/api/hot-mods/<id>.json`、`<id>.versions.json` |
| 源码 clone | `archive/forge/mods/<id>_source/` |
| 抓取/续跑脚本 | `archive/forge/tools/` |

> 2026-09-02：`mods/<id>_release/` 成品 zip 已清理（94 目录约 398MB）——release 版对知识参照价值低且占空间，源码 clone 保留；hot-index 中 `best_link` 为 Forge 站外 URL 记录不受影响。
