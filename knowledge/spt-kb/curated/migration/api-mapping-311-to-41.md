---
version: [3.11, 4.1]
domain: server
topic: migration
source: curated
---

# 3.11 -> 4.1 API 映射表

> 状态：已提炼（2026-08-05）| 用途：自动迁移管线的最关键知识前置
> 方法：对照 3.11 TypeScript 源码（SPT-archive/server）与 4.1 C# 源码/API 笔记实读核实
> 覆盖：服务端 mod 常用 API 模式的 1:1 映射

---

## 1. 入口与生命周期

| 3.11 (TypeScript) | 4.1 (C#) | 映射难度 |
|-------------------|----------|----------|
| `module.exports = { mod: new XxxMod() }` | `IModMetadata` record + `[Injectable]` class | 高（新增元数据类） |
| `class XxxMod implements IPreSptLoadMod` + `preSptLoad(container)` | `IOnDIConstruct` 或 `[Injectable]` 构造 | 中 |
| `class XxxMod implements IPostDBLoadMod` + `postDBLoad(container)` | `IOnLoad` + `OnLoadAsync(CancellationToken)` | 中 |
| `class XxxMod implements IPostSptLoadMod` + `postSptLoad(container)` | `IOnLoad`（PostLoad 阶段）或 `IHostedService` | 中 |
| 无（3.11 无元数据类） | `IModMetadata`：ModGuid/Name/Author/Version/SptVersion/Incompatibilities/ModDependencies | 高（必须新建） |
| package.json 的 `sptVersion` 字段 | `SptVersion` Range 属性 `new("~4.1.0")` | 低 |
| package.json 的 `incompatibilities` | `Incompatibilities` 列表 | 低 |
| package.json 的 `loadBefore`/`loadAfter` | `TypePriority`（OnLoadOrder.X + 偏移） | 中 |

## 2. DI 容器

| 3.11 (tsyringe) | 4.1 (MSDI + SPT 封装) | 映射难度 |
|------------------|------------------------|----------|
| `container.resolve(T)` | 构造函数参数注入 | 中 |
| `container.register(T, ...)` | `[Injectable]` 特性 + `IOnDIConstruct` | 中 |
| `@injectable()` 装饰器 | `[Injectable(InjectionType.X)]` | 低 |
| `@inject(T)` 构造参数 | 构造参数直接声明（无注解） | 低 |
| `DependencyContainer` 类型 | 无直接等价物（用构造注入替代） | 高（架构变化） |

## 3. 表/数据库访问

| 3.11 | 4.1 | 映射难度 |
|------|-----|----------|
| `DatabaseServer.getTables()` | 构造函数注入 `GlobalTable` `TemplateTable` 等 | 高（架构变化） |
| `database.templates.quests` | `TemplateTable.Quests` | 低 |
| `database.templates.items` | `TemplateTable.Items` | 低 |
| `database.locales.global` | `LocaleTable.Global`（注意：直接改不保存，需走 LocaleService） | 中 |
| `database.traders` | `TradersTable` | 低 |
| `database.globals.config` | `GlobalTable.Configuration` | 低 |
| `database.bots.types` | `BotTable.Types` | 低 |
| `database.hideout` | `HideoutTable` | 低 |
| `database.locations` | `LocationTable` | 低 |
| `database.settings` | `SettingsTable` | 低 |
| `database.server` | `ServerTable` | 低 |
| `database.match` | `MatchTable` | 低 |

## 4. 常用服务

| 3.11 服务 | 4.1 等价 | 映射难度 |
|-----------|---------|----------|
| `ILogger` (`InstanceManager.logger`) | `ISptLogger<T>` | 低 |
| `logger.log(msg, LogTextColor.GREEN)` | `logger.Success(msg)` / `logger.Info(msg)` | 低 |
| `ConfigServer.getConfig(name)` | 构造注入 `XxxConfig`（强类型） | 中 |
| `SaveServer` (profile 读写) | `SaveProfileService` / 构造注入 | 中 |
| `ItemHelper` | 构造注入 `ItemHelper` | 低 |
| `HashUtil` | 构造注入 `HashUtil` | 低 |
| `JsonUtil` | `JsonUtil` / `System.Text.Json` | 低 |
| `FileSystem.vfs.read()` | `System.IO.File.ReadAllText` 或 `IFileSystem` | 中 |
| `StaticRouterModService.registerStaticRouter()` | `AbstractRouter` 子类 + `[Injectable]` | 中 |
| `DynamicRouterModService.registerDynamicRouter()` | `AbstractRouter` 子类（动态路由） | 中 |
| `CustomItemService.createItemFromClone()` | `CustomItemService.CreateItemFromClone()` | 低（改名） |
| `ImageRouter` | `BundleLoader` / `SPTarkov.Server.Web` | 中 |
| `LocaleService` | `LocaleService` | 低 |
| `ProfileHelper` | `ProfileHelper` | 低 |
| `RagfairPriceService` | `RagfairPriceService` | 低 |
| `PreSptModLoader` | 不需要（DI 自动发现） | 高（删除） |
| `ImporterUtil` | `IFileSystem` / 直接 IO | 中 |

## 5. 配置

| 3.11 | 4.1 | 映射难度 |
|------|-----|----------|
| `config/config.json` 相对路径读 | `IConfigManager` 或 JSON 反序列化 | 中 |
| `ConfigServer.getConfig("traders")` | 构造注入 `TraderConfig` | 低 |
| mod 自带 config JSON | 模组 `config/` 目录 + `IConfigManager` | 中 |

## 6. 路由

| 3.11 | 4.1 | 映射难度 |
|------|-----|----------|
| `StaticRouterModService.registerStaticRouter(name, routes, action)` | `AbstractRouter` 子类 `RegisterRouteAction()` | 中 |
| `DynamicRouterModService.registerDynamicRouter(name, match, action)` | `AbstractRouter` 子类（动态） | 中 |
| `routeAction(url, info, sessionID)` | `RouteAction(string url, ...)` | 低 |

## 7. 日志级别映射

| 3.11 LogTextColor | 4.1 ISptLogger 方法 |
|--------------------|---------------------|
| `LogTextColor.GREEN` | `logger.Success()` |
| `LogTextColor.YELLOW` | `logger.Warning()` |
| `LogTextColor.RED` | `logger.Error()` |
| `LogTextColor.WHITE`/`CYAN` | `logger.Info()` |
| `LogTextColor.BLACK`/`MAGENTA` | `logger.Debug()` |

## 8. 生命周期时序（TypePriority 对照）

| 3.11 阶段 | 4.1 OnLoadOrder 常量 | 语义 |
|-----------|----------------------|------|
| preSptLoad | `OnLoadOrder.PreLoad` | 最早，注册阶段 |
| postDBLoad | `OnLoadOrder.PostLoad`（原 PostDBModLoader） | 数据库加载后 |
| postSptLoad | `OnLoadOrder.PostLoad + N` | 全部加载后 |
| 无 | `OnLoadOrder.Routers` | 路由注册基准 |

> 4.1 中 `TypePriority` 决定加载顺序，后加载者覆盖先加载者（表注入），先注册者胜（路由）。

---

## 映射难度汇总

| 难度 | 占比 | 处理方式 |
|------|------|----------|
| 低（改名/机械） | ~50% | 可自动化的查找替换 + 编译验证 |
| 中（模式适配） | ~35% | LLM 理解业务逻辑后适配，模板引导 |
| 高（架构变化） | ~15% | 需要人工判断，无法纯机械转换 |

---

## 3.11 特有 API 使用面实测（542 个 TS 文件统计）

| 3.11 API | 涉及文件 | 映射 |
|----------|---------|------|
| `database`（表访问） | 298 | 表注入（全自动） |
| `postDBLoad` | 169 | `OnLoadOrder.PostLoad + 1` |
| `traders` | 154 | `TradersTable` |
| `preSptLoad` | 152 | `OnLoadOrder.Preload` |
| `locales` | 150 | **LocaleTable（高危，见下）** |
| `assort` | 146 | 数据搬运（全自动） |
| `globals` | 144 | `GlobalTable` |
| `container.resolve` | 126 | 构造注入 |
| `configServer` | 118 | 类型注入 |
| `jsonUtil` | 84 | 4.1 JsonUtil（命名空间变化） |
| `imageRouter` | 59 | `ImageRouter` |
| `CustomItemService` | 59 | 4.1 `CustomItemService` |
| `StaticRouterModService` | 52 | `AbstractRouter` |
| `addTrader` | 36 | 配方 01 |
| `ragfairPriceService` | 32 | 4.1 RagfairPriceService |
| `DynamicRouterModService` | 24 | `DynamicRouter` 子类 |
| `vfs` | 6 | `ModHelper` / System.IO |

## 3.11 -> 4.1 自动化阻断器（不可 1:1 映射）

### 阻断器 1（**已解决** -- 官方 AddTransformer 机制）：LocaleTable.Global 直写

- 3.11 习惯：`database.locales.global[lang][key] = text` 直接改内存表
- 4.1 警告：`LocaleTable.Global` 懒加载，"DO NOT USE THIS PROPERTY DIRECTLY, USE LOCALESERVICE INSTEAD... CHANGES WILL NOT BE SAVED"
- **官方解法（源码实证 2026-08-05）**：`LazyLoad<T>.AddTransformer(Func<T?, T?>)` -- 每次请求 locale 时应用修改。SPT 4.1 自己的 `PostDbLoadService.RenamePreraidLocales()` 就是这么做的：
  ```csharp
  localeTable.Global["en"].AddTransformer(data =>
  {
      data["Offline raid test mode"] = "SPT";
      data["Offline raid description"] = " ";
      return data;
  });
  ```
- **迁移模板**：3.11 的 `database.locales.global[lang][key] = value` 映射为：
  ```csharp
  localeTable.Global[lang].AddTransformer(data => { data[key] = value; return data; });
  ```
- **影响修正**：locale 型 mod（ETT 等 150/542 文件）**可自动化迁移**，不再是硬阻断。服务端整体自动化率上修。

### 阻断器 2：运行时枚举扩展

- 3.11：`Traders[traderId] = traderId` 动态扩展枚举
- 4.1：枚举扩展走 prepatcher（声明式），服务端无此机制
- 处理：重写为字符串/MongoId 常量

### 阻断器 3：assort JSON 数据形状

- 3.11 数据按 3.11 库形状组织，4.1 `AddCustomTraderHelper` 期望形状可能微差
- 数据文件搬运全自动，形状校验需对照 4.1 实况

### 阻断器 4：类扩展 override 服务端核心类

- 3.11：`extends BotGenerator` 猴子补丁式 override（JS 常态）
- 4.1：核心类是 `[Injectable]` 单例，子类化 + virtual override 语义完全重做
- 影响：复杂 mod（SAIN 类 745 文件、progressivebotsystem 44 文件）落此档，等于重新设计

### 阻断器 5：TS 类型系统 -> C# 类型系统

- 大部分自动（Record->Dictionary、可选字段->nullable、forEach->LINQ）
- union 类型、`any` 需半自动（LLM 设计类型）
- EFT 模型字段 3.11/4.1 同名，类型翻译难度低

## 已知无法 1:1 映射的模式

1. **tsyringe 容器动态注册**（运行时 register）-- 4.1 需要静态 `[Injectable]`，需重构
2. **`PreSptModLoader`** -- 4.1 无此概念，DI 自动发现替代
3. **`FileSystem.vfs`** -- 4.1 用 System.IO，路径解析不同
4. **`DatabaseServer.getTables()`** -- 4.1 直接注入各表，需重写访问点
5. **mod 间运行时互操作**（container.resolve 其他 mod 的服务）-- 4.1 需 `IEnumerable<T>` 或显式接口
