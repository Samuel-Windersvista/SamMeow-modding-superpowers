# 2676 PitFireTeam — 服务端重编译 4.1.2

> forgeId: 2676 | slug: pitfireteam-squad-assistance | 来源: Forge 下载
> 原 DLL: `SPT\user\mods\pitFireTeam-ServerMod\pitFireTeam.Server.dll`（4.0.8 编译，310KB）
> 目标: SPTarkov 4.1.2（net10.0）

## 状态

**已重编译 4.1.2 + 运行时验证通过**（2026-08-09）

## 4.0 → 4.1.2 迁移要点

| 变更点 | 4.0 旧写法 | 4.1.2 新写法 |
|---|---|---|
| ModMetadata | `record PitFireTeamServerMetadata : AbstractModMetadata` | `record ... : IModMetadata`（11 属性全 init-only，新增必填 `HasPrepatcher`，移除 `IsBundleMod`） |
| 加载钩子 | `IOnLoad.OnLoad()` | `IOnLoad.OnLoadAsync(CancellationToken)`（接口移至 `SPTarkov.Server.Core.DI`） |
| 数据库访问 | `DatabaseService.GetTraders()/GetLocales()` | 表模型注入：`TradersTable`（Dictionary<MongoId,Trader>）、`LocaleTable.Global`（`LazyLoad<GlobalLocaleDictionary>`） |
| 全局配置 | `databaseService.GetGlobals()` | `GlobalTable` 注入（`globalTable.Configuration`） |
| 配置读取 | `ConfigServer.GetConfig<T>()` | 具体 Config 类直接注入（`LostOnDeathConfig`、`PmcConfig`） |
| Injectable | `[Injectable(InjectionType.X)]`（4.0 无 TypePriority 语义） | Router: `Transient` + `typePriority: 400000`；Callbacks: `Transient`；Service/Plugin: `Singleton` |
| 日志接口 | `SPTarkov.Server.Core.Models.Utils.ISptLogger` | `SPTarkov.Common.Models.Logging.ISptLogger` |
| Helper 命名空间 | `Core.Helpers` | `Core.Helpers.Items` / `Core.Helpers.Profile` / `Core.Helpers.Server` |
| 通知 | `NotificationSendHelper.SendMessage/SendMessageToPlayer` | `SendMessageAsync` / `SendMessageToPlayerAsync` |
| 组邀请通知 | `WsGroupMatchInviteAccept/Decline`（已移除） | `WsNotificationEvent` + `ExtensionData` 字典承载扩展字段 |
| Trader 构造 | `new Trader()` + setter | record required init 属性，对象初始化器一次性设置（Base/Assort/Dialogue/QuestAssort） |
| MongoId | 指针噪音 `(*(MongoId*)(&id))` | `id.ToString()` |
| 反编译噪音 | `((object)x/*cast*/).ToString()`、InlineArray、`MongoId.op_Implicit` | 清理为直接调用/隐式转换 |
| CounterKeyValue.Value | `double?` | `long?`（赋值处加 `(long?)` 转换） |
| ProfileController 替换 | `TypeOverride=typeof(ProfileController)` + virtual override | **不可行**（4.1.2 方法非 virtual + TypeOverride 移除）；功能已由 `FriendlyTeammateSocialRouter` 链式路由 `/client/profile/view` 覆盖，FriendlyProfileController 已删除 |

## 注册语义（4.1.2 DI 实测反射值）

- `PitFireTeamServerPlugin`: `[Injectable(Singleton, typePriority: 400001)]`（与原 4.0 的 TypePriority=400001 一致）
- 5 个 Router: `[Injectable(Transient, typePriority: 400000)]`（核心 StaticRouter 同款）
- 5 个 Callbacks: `[Injectable(Transient)]`
- 5 个 Service: `[Injectable(Singleton)]`

> 注意：Router 不能注册为 Scoped——`HttpServer`(Singleton) 消费 `IEnumerable<StaticRouter>`，Scoped 会触发
> "Cannot consume scoped service 'StaticRouter' from singleton 'HttpServer'" 启动崩溃。

## 运行时验证（SPT_410 测试服务器）

启动日志确认：
- 模组加载：`PitFireTeam 版本0.9.0 (GUID: xyz.pit.fireteam | targets SPT: ~4.1.0) 已加载`
- 插件钩子：`PitFireTeam loaded`
- 商人注册：`Registered courier trader '67d3a28a3d6f4f7dbd09ed13'`
- 配置应用：`pitFireTeam PMC armband enforcement: enabled`
- 服务器完整启动：`服务器已开启，游戏愉快`

## 产物

- `pitFireTeam.Server.dll`（315392 字节 = 308KB）— 4.1.2 重编译产物
- `src/` — 完整可编译源码（csproj + 6 个源目录）

## 部署

目标：`E:\Game\EFT_Offline\Inescapable Tarkov\mods\[6]友方AI系统-friendlypmc与PITFireTeam合并版本\SPT_Runtime\user\mods\pitFireTeam-ServerMod\pitFireTeam.Server.dll`
（旧 4.0 DLL 备份为 `pitFireTeam.Server.dll.bak`）

> 客户端部分（`BepInEx\plugins\pitFireTeam`）未动，仅重编译服务端 DLL。
