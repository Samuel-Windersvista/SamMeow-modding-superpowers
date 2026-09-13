---
version: [5.0]
domain: server
topic: modding-api
source: curated
---
# Modding API 扩展点笔记 [5.0]

> **[UNSTABLE-PREVIEW]** SPT 5.0 处于开发初期（`5.0x-dev` 分支，尚无正式版）。本笔记为 2026-09-13 源码快照（HEAD `ff0bf3281`）提炼，上游 API 可能变更；使用前请以源码复核，勿据此做长期承诺。

> 状态：**源码快照提炼（2026-09-13）** | 版本：[5.0]
> 源码：`Services/Modding/Custom/CustomItemService.cs:47,139,404`、`Services/Modding/Custom/CustomQuestService.cs:24`、`Services/Modding/ProfileDataService.cs:76,102`、`Servers/SaveServer.cs:93,122,188,253`、`Libraries/SPTushonka.Server.Web/IModBlazorMetadata.cs:21`、`SPTWeb.cs:24,94`、`Common/Models/Logging/ISptLogger.cs:5`

## 扩展点一览

| 能力 | 类型 / 服务 | 证据 |
|------|------------|------|
| 自定义物品 | `CustomItemService.CreateItem` / `CreateItemFromClone` / `AddCustomWeaponToPMCs` | `CustomItemService.cs:47,139,404` |
| 自定义任务 | `CustomQuestService.CreateQuest(NewQuestDetails)` | `CustomQuestService.cs:24` |
| mod 自有持久化 | `ProfileDataService.Get/SaveProfileDataAsync<T>`（落盘 `user/profileData/{profileId}/{modKey}.json`） | `ProfileDataService.cs:76,102` |
| 读写完整存档 | `SaveServer.GetProfile` / `GetProfiles` / `LoadProfileAsync` / `SaveProfileAsync` | `SaveServer.cs:93,122,188,253` |
| 读 / 改配置 | 注入具体 `*Config` 记录（如 `PmcConfig`、`HttpConfig`）；共 28 种 | `Loaders/ConfigLoader.cs:16`、`Models/Enums/ConfigTypes.cs:80` |
| 改数据库表 | 注入 `TemplateTable` / `LocaleTable` / `BotTable` 等 | `SPTushonka.Server/Helpers/DatabaseTables.cs:6` |
| Mod Web Pages | 元数据同时实现 `IModBlazorMetadata`（`WWWRootUrl` / `HomePage` / `HomePageDescription`） | `Libraries/SPTushonka.Server.Web/IModBlazorMetadata.cs:21`、`SPTWeb.cs:24,94` |
| 日志 | 注入 `ISptLogger<T>` | `Common/Models/Logging/ISptLogger.cs:5` |
| 枚举 prepatch | `HasPrepatcher=true` + `user/patchers/{ModGuid}/*.json` | `Modding/EnumPatcher.cs`、`Modding/PrepatchLoadContext.cs` |

## 路由与生命周期扩展点

- 静态路由：继承 `StaticRouter` + `RouteAction<T>`。
- 动态路由：继承 `DynamicRouter`。
- ItemEvent 路由：继承 `ItemEventRouter` + `ItemRouteAction<T>`。
- Profile 加载钩子：继承 `SaveLoadRouter`。
- 生命周期：`IOnLoad` / `IOnUpdate` / `IOnDIConstruct`。
- 以上详见 http-routing.md 与 di-container.md。

## 其它扩展接口清单

`IHttpListener`、`IWebSocketConnectionHandler`、`ISptWebSocketMessageHandler`、`IDialogueChatBot`、`IChatCommand`、`ISptCommand`、`IChatMessageHandler`、`IProfileMigration`、`IWeatherPreset`、`IInventoryMagGen`、`IRepeatableQuestGenerator`、`ISerializer`、`ICloner`、`IJsonConverterRegistrator`（见各 `DI/`、`Helpers/`、`Generators/`、`Servers/` 目录）。

## 5.0 新增可依赖能力

- 赛季 / 通行证 / 商店 / 结局 / 任务链 / 教程；新表 `SeasonTable`、`ShopTable`（新扩展点，旧 mod 不强制使用）。

## 与 4.1 的关系

- 报告未在扩展点 API 上发现破坏性变更；`ProfileChange` 字段扩展见 save-profile.md。

## 已核实位置

- `Services/Modding/Custom/CustomItemService.cs:47,139,404`
- `Services/Modding/Custom/CustomQuestService.cs:24`
- `Services/Modding/ProfileDataService.cs:76,102`
- `Servers/SaveServer.cs:93,122,188,253`
- `Libraries/SPTushonka.Server.Web/IModBlazorMetadata.cs:21`、`SPTWeb.cs:24,94`
- `Common/Models/Logging/ISptLogger.cs:5`

## 相关笔记（完整清单指向）

- 全部 28 种 `*Config` 的枚举名 / `spt-*` 值 / CLR 类型清单见 `config-system.md`。
- 全部 12 张数据库表（`DatabaseTables`）与导入流程见 `database-structure.md`。
- 各 API 的完整签名、参数模型与调用时机以源码为准；本笔记只做扩展点索引。
