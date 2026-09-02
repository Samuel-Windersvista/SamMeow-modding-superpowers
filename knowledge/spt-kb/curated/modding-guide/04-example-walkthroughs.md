---
version: [4.1]
domain: both
topic: examples
source: curated
---
# 官方示例逐例解读（4.0 → 4.1 迁移对照）[4.1]

> 来源：`E-Mod开发示例/server-mod-examples/`（官方 25 个示例项目，**当前为 4.0 语法**）
> 使用方式：每个示例 = 一个独立场景。照抄前按「4.1 需要改什么」列改写。
> 通用 4.1 改写规则：`AbstractModMetadata`→`IModMetadata`（去 override、删 IsBundleMod）；`OnLoad()`→`OnLoadAsync(CancellationToken)`；`PostDBModLoader`→`PostLoad`（或按语义选）；`DatabaseService/ConfigServer`→注入具体表/配置；路由 action 加 token；namespace 迁移（见 02 文档第 12 节）。

| # | 示例 | 演示内容 | 4.1 需要改什么 |
|---|------|---------|---------------|
| 1 | Logging | 日志注入 `ISptLogger<T>` | `ISptLogger` 命名空间 → `SPTarkov.Common.Models.Logging`；级别枚举换 Microsoft 版 |
| 2 | EditDatabase | 编辑 globals/bot/hideout/locations | `DatabaseService.GetXxx()` → 注入具体 Table（`GlobalTable`/`BotTable`/`HideoutTable`/`LocationTable`） |
| 3 | EditSptConfig | 改 SPT 配置 | `ConfigServer.GetConfig<T>()` → 直接注入 `T` |
| 5 | ReadCustomJsonConfig | 读自己 mod 的 config.json | 4.1 推荐 `IOnDIConstruct` 注册单例（配 Mod Web Pages 可做编辑 UI） |
| 6 | OverrideMethod | 用继承/重写覆盖服务行为（无 Harmony） | 依赖注入方式不变；若覆盖的是路由行为，参考 Routers 优先级 |
| 6.1 | OverrideMethodHarmony | Harmony patch 覆盖方法 | `ServiceLocator` 移除 → 构造函数注入 + static 字段；标 `[Injectable]` + `IEnumerable<IRuntimePatch>` 统一启用 |
| 7 | UseMultipleClasses | 多类协作 + [Injectable] 互相注入 | 不变 |
| 8 | OnLoad | IOnLoad 生命周期 | `OnLoadAsync(CancellationToken)` |
| 9 | OnUpdate | IOnUpdate 周期任务 | `OnUpdateAsync(long, CancellationToken)` |
| 10 | CustomRoute | 自定义 HTTP 路由 | action 加 `cancellationToken` 参数；`TypePriority = OnLoadOrder.Routers + 1` |
| 11 | RegisterClassesInDI | 注册自己的类到 DI | 不变（仍是 [Injectable]） |
| 12 | Bundle | 打包 bundle（bundles.json） | `IsBundleMod` 删除，靠 bundles.json 自动检测 |
| 13 | AddTraderWithAssortJson | 加商人（assort 来自 JSON） | 元数据接口；`TradersTable`/`TemplateTable`；`_ragfairConfig.Traders` 注入 `RagfairConfig` |
| 13.1 | AddTraderWithDynamicAssorts | 加商人（代码动态生成 assort，FluentTraderAssortCreator） | 同 13 |
| 14 | AfterDBLoadHook | 数据库加载后钩子 | 阶段概念变化：数据库加载提前，`PostDBModLoader`→`PostLoad`（或按需 `Preload`） |
| 15 | HttpListenerExample | 独立 HttpListener 端口 | 推荐改用 Router（见 10）或 MVC Controller（Web 页面场景） |
| 18 | CustomItemService | 自定义物品（CustomItemService） | `Services.Mod.CustomItemService` → `Services.Modding.Custom.CustomItemService` |
| 18.1 | CustomItemServiceLootBox | 自定义物品：战利品箱 | 同 18 |
| 20 | CustomChatBot | 自定义聊天机器人 | 命名空间迁移（Services 表） |
| 21 | CustomCommandoCommand | Commando 命令 | 命名空间迁移 |
| 22 | CustomSptCommand | SPT 控制台命令 | 命名空间迁移 |
| 23 | CustomAbstractChatBot | 抽象聊天机器人基类 + 多命令 | 命名空间迁移 |
| 24 | Websocket | WebSocket 连接/消息处理 | 命名空间迁移；如需关闭通知用 `Services.Server.NotificationSendHelper` |
| 25 | AddCustomLocales | 添加本地化文本 | `Services.Locales.*` |

## 学习路径建议

1. 从 1 → 2 → 8 → 10 → 11 顺序过一遍：日志 → 数据访问 → 生命周期 → 路由 → DI
2. 然后按目标挑：加商人（13/13.1）、自定义物品（18）、命令（21/22）、聊天（20/23）
3. 每个示例改写 4.1 后，对照 `E:\云文件\GitHub\SamMeow_SPT410_source_code` 源码验证类型与命名空间
4. 迁移文档里出现的 Prepatching 页是死链（404），枚举扩展看 EnumExtensions.md（02 文档第 11 节）
