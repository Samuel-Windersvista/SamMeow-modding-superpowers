# 1192-RaidOverhaul 重编译留档（4.0 -> 4.1.2）

> 日期：2026-08-09 | 来源：c-bucket 1192 RaidOverhaul 3.0.3（SPT 4.0.13 时代，forge 版）
> 产物：`RO-Server.dll`（530,944 字节，net10.0，AssemblyName=RO-Server，Version 3.0.3）

## 迁移内容

| 项 | 4.0（原） | 4.1.2（新） |
|---|---|---|
| 元数据 | `: AbstractModMetadata`（record，含 IsBundleMod） | `: IModMetadata`（接口）；set→init；删 IsBundleMod，增 `HasPrepatcher=false` |
| 生命周期 | `IOnLoad.OnLoad()` | `IOnLoad.OnLoadAsync(CancellationToken)` |
| 数据库 | `DatabaseService` 聚合入口（GetTables/GetItems/GetLocations/GetGlobals/GetBots/GetTrader） | 直接注入表模型：`TemplateTable`/`TradersTable`/`LocationTable`/`GlobalTable`/`BotTable`/`LocaleTable` |
| 配置 | `ConfigServer.GetConfig<T>()` | 直接构造注入 `TraderConfig`/`RagfairConfig`/`WeatherConfig`/`LocationConfig`/`LostOnDeathConfig` |
| 日志 | `ISptLogger<T>`（SPTarkov.Server.Core.Models.Utils）+ `LogTextColor` | `SPTarkov.Common.Models.Logging.ISptLogger<T>` + Spectre `Color`（mod 自定义 LogTextColor 枚举做映射）+ M.E.Logging `LogLevel` |
| 路由 | `RouteAction(Func<string, T, MongoId, string, ValueTask<string>>)` | 动作签名增加 `CancellationToken` 参数 |
| 服务命名空间 | `Helpers`/`Services` 扁平 | `Helpers.Items`/`Helpers.Profile`/`Helpers.Server`/`Helpers.Traders`/`Services.Locales`/`Services.Items`/`Services.Server`/`Services.Commerce` |
| 坐标类型 | `XYZ`（可变） | `Eft.Common.Vector3`（readonly struct，init 属性）——WeightChanges 改为重建 Vector3 |
| 配置体重/耐力 | 原 `Vector3.X *= mult` | 重建 `new Vector3(X*mult, Y*mult, Z)` |
| 商户表 | `Traders[traderId]` | `TradersTable.GetTrader(id)` / `TryAdd`；`Trader` record 改对象初始化器 |

## MoreBots 依赖移除（Overseer 决策）

- 原 ROMain 构造注入 `MoreBotsAPI`/`FactionService`/`MoreBotsCustomBotTypeService`（程序集 `MoreBotsServer.dll`，forge mod 2426）。
- 本机与《战地乱改》(Inescapable Tarkov) 整合包均无 MoreBotsAPI；4.1.2 KB 确认构造参数解析失败 = 容器构建失败 = 启动即炸，故 **stub 不可行**。
- 处理：删除 3 个注入与 `EnableCustomBoss` 分支中的 MoreBots 调用（bot 数据加载/阵营/自定义 bot 类型），Legion 自定义 boss 功能禁用并打日志提示；其余功能（事件配置路由、天气/季节、需求办公室商人、自定义物品、任务、藏身所配方、locale）全部保留。
- `ModDependencies`：删除 `com.morebotsapi.tacticaltoaster`；`com.wtt.commonlib` 由 `>=2.0.15` 改为 **`>=3.0.0 <4.0.0`**（整合包 WTT-ServerCommonLib 3.0.3 实装版本）。

## 外部引用

- `WTT-ServerCommonLib.dll` v3.0.3（已 4.1 兼容：IModMetadata + 2 参 Injectable）——编译时 `Reference + HintPath`（Refs/WTT-ServerCommonLib.dll），运行时由前置库 mod 提供。

## 与 SamMeow 3114 特化版对比

`E:\云文件\GitHub\SamMeow-Raid-Overhaul-For-3114` 为 2.7.2 特化 fork：改动集中在**客户端 BepInEx 插件**（EventController 边界修复、DoorController 性能、KeyPatch、事件通知/翻译、撤离箱）+ db 数据（Executioner/Judge 武器口径）。服务端逻辑（PkController=Peacekeeper 自定义物品、LegionController、RaidController、WeatherController、ReqsController）在 4.0 C# 移植版中均已覆盖。本次重编译未合并 3114 的 db 数据差异（数据文件非本任务范围），如需可另做数据合并。

## 部署

- 目标：`E:\Game\EFT_Offline\Inescapable Tarkov\mods\《生活在诺文斯克》战局大修-RaidOverhaul沉浸感增强设置\SPT_Runtime\user\mods\RaidOverhaul\RO-Server.dll`（原 4.0 DLL 已备份为 `RO-Server.dll.bak-40`）。
- 编译：`dotnet build -c Release` → 0 错误 0 警告。
