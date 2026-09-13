# SPT 5.0 Mod API 能力评估报告

> 日期：2026-09-13 | 对象：`E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code`（分支 `5.0x-dev`，HEAD `ff0bf3281`）
> 方法：3 路并行只读源码分析（加载链 / 能力面 / 4.1→5.0 diff），逐条 `文件:行号` 取证
> 关联：`docs/spt-5.0x-dev-现状报告.md`（2026-09-05 版本，判断「无配套、不可用」——**本报告更新其配套结论**）
> 结论用途：判断本项目能否具备「移植、编写 SPT5 mod」的能力

---

## 0. 一句话结论

**SPT 5.0 的服务端 mod API 完整且开放，框架层几乎原样继承 4.1。** 本项目已具备编写 SPT5 **服务端 mod** 的 API 基础（`IModMetadata` + DI + Router + 自定义物品/任务 + Profile 持久化 + Web UI）；4.1 mod 移植到 5.0 属于**低成本**（改版本区间 + 重编译）。真正的能力缺口在**客户端侧（EFT 1.1.5 类名映射）与官方文档**，而非 API 缺失。

---

## 1. 问题回答：SPT 5.0 有 mod 接口吗？

**有，且是完整的一等公民机制。** 三条证据链：

1. **加载器存在**：`SPTushonka.Server/Modding/ModLoader.cs`（447 行）+ `ModValidator.cs`（369 行）在启动链中被调用（`SPTushonka.Server/Program.cs:189-197`）。
2. **元数据契约存在**：`IModMetadata` 要求每个 mod 提供唯一实现（`Libraries/SPTushonka.Server.Core/Models/Spt/Mod/IModMetadata.cs:30-102`）。
3. **官方示例存在**：`Testing/TestMod/`、`Testing/TestMod2/` 两个可编译示例项目（含 prepatch 示例）。

服务端 mod 形态：`user/mods/<modDir>/<*.dll>`，DLL 内实现 `IModMetadata` 的类即被识别（**不用 package.json / mod.json**）。

---

## 2. Mod API 全貌

### 2.1 元数据接口（`IModMetadata.cs:30-102`）

```csharp
public interface IModMetadata
{
    string ModGuid { get; init; }                       // 反域名风格，正则 ^[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*$
    string Name { get; init; }
    string Author { get; init; }
    List<string>? Contributors { get; init; }
    Version Version { get; init; }                      // SemanticVersioning.Version
    Range SptVersion { get; init; }                     // SemanticVersioning.Range，如 new Range("~5.0.0")
    bool HasPrepatcher { get; init; }
    List<string>? Incompatibilities { get; init; }
    Dictionary<string, Range>? ModDependencies { get; init; }
    string? Url { get; init; }
    string License { get; init; }
}
```
所有属性必须实现；不需要的显式赋 `null`。每个 mod 只能有一个实现，重复抛 `ModLoaderException`（`ModLoader.cs:317-320`）。

### 2.2 加载与校验

| 环节 | 规则 | 证据 |
|------|------|------|
| 扫描目录 | `./user/mods/`，一级子目录 = 一个 mod | `ModLoader.cs:20,122` |
| 识别文件 | 目录顶层 `.dll`；无 DLL 抛 `ModLoaderException` | `ModLoader.cs:259-270` |
| 元数据 | 反射查找 `IModMetadata` 实现并 `Activator.CreateInstance` | `ModLoader.cs:307-342` |
| 排序 | 先按 `ModGuid` 字母序，实际顺序由 `TypePriority` 决定（GUID 为 tiebreaker） | `ModLoader.cs:143`、`IModMetadata.cs:23-25` |
| 版本校验 | `SptVersion` 为 semver range，`Satisfies(running, mod.SptVersion)` | `ModValidator.cs:156-175` |
| 程序集引用校验 | mod 引用的 Core 版本 **高于** 运行时 → 抛异常 | `ModValidator.cs:186-211` |
| 依赖/不兼容 | `ModDependencies`（仅校验不排序）、`Incompatibilities` | `ModValidator.cs:243-324` |
| 拒绝客户端 mod | 目录名 `src`/`db`/`user`、名 `bepinex`、含 `plugins/`、非 Blazor mod 含 `.js`/`.ts` | `ModValidator.cs:331-365` |

**异常隔离**：单个 mod 加载失败会被 try/catch 并继续下一个（`ModLoader.cs:133-140`）；但**校验阶段任一失败则所有 mod 都不加载**（`ModValidator.cs:91-95`）。

### 2.3 生命周期钩子

```csharp
// DI/IOnLoad.cs:5 —— 启动一次性
public interface IOnLoad { Task OnLoadAsync(CancellationToken cancellationToken); }

// DI/IOnUpdate.cs:3 —— 周期（每 5s 轮询）
public interface IOnUpdate { Task<bool> OnUpdateAsync(long secondsSinceLastRun, CancellationToken cancellationToken); }

// DI/IOnDIConstruct.cs:24 —— 容器构建前手动注册（静态方法）
public interface IOnDIConstruct
{
    static abstract Task OnDIConstructAsync(IServiceCollection serviceCollection, CancellationToken cancellationToken);
}
```

阶段常量 `OnLoadOrder`（`DI/OnLoadOrder.cs:3`）：`Watermark=0` → `Preload=100000` → `GameCallbacks=200000` → `TraderRegistration=300000` → `Routers=400000` → `HandbookCallbacks=500000` → `SaveCallbacks=600000` → `TraderCallbacks=700000` → `PresetCallbacks=800000` → `RagfairCallbacks=900000` → `PostLoad=1000000`。

### 2.4 DI 与扩展点

```csharp
// SPTushonka.DI/Annotations/Injectable.cs:7
[Injectable(InjectionType.Singleton, typePriority = 400000)]
public class MyService { }   // InjectionType: HostedService | Singleton | Transient | Scoped
```

| 能力 | 类型/服务 | 证据 |
|------|-----------|------|
| 静态路由（精确匹配） | 继承 `StaticRouter` + `RouteAction<T>` | `DI/Router.cs:62,200` |
| 动态路由（包含匹配） | 继承 `DynamicRouter` | `DI/Router.cs:93` |
| ItemEvent 路由 | 继承 `ItemEventRouter` + `ItemRouteAction<T>` | `DI/Routing/ItemEventRouter.cs:8,58` |
| Profile 加载钩子 | 继承 `SaveLoadRouter` | `DI/Router.cs:148` |
| 自定义物品 | `CustomItemService.CreateItem` / `CreateItemFromClone` / `AddCustomWeaponToPMCs` | `Services/Modding/Custom/CustomItemService.cs:47,139,404` |
| 自定义任务 | `CustomQuestService.CreateQuest(NewQuestDetails)` | `Services/Modding/Custom/CustomQuestService.cs:24` |
| mod 自有持久化 | `ProfileDataService.Get/SaveProfileDataAsync<T>`（落盘 `user/profileData/{profileId}/{modKey}.json`） | `Services/Modding/ProfileDataService.cs:76,102` |
| 读写完整存档 | `SaveServer.GetProfile` / `GetProfiles` / `LoadProfileAsync` / `SaveProfileAsync` | `Servers/SaveServer.cs:93,122,188,253` |
| 读/改配置 | 注入具体 `*Config` 记录（如 `PmcConfig`、`HttpConfig`）；共 26 种 | `Loaders/ConfigLoader.cs:16`、`Models/Enums/ConfigTypes.cs:80` |
| 改数据库表 | 注入 `TemplateTable`/`LocaleTable`/`BotTable` 等 | `SPTushonka.Server/Helpers/DatabaseTables.cs:6` |
| Mod Web Pages | 元数据同时实现 `IModBlazorMetadata`（`WWWRootUrl`/`HomePage`/`HomePageDescription`） | `Server.Web/IModBlazorMetadata.cs:21`、`SPTWeb.cs:24,94` |
| 日志 | 注入 `ISptLogger<T>` | `Common/Models/Logging/ISptLogger.cs:5` |
| 枚举 prepatch | `HasPrepatcher=true` + `user/patchers/{ModGuid}/*.json`（AsmResolver 改 enum 常量后内存重托管） | `Modding/EnumPatcher.cs`、`Modding/PrepatchLoadContext.cs` |
| 其它扩展面 | `IHttpListener`、`IWebSocketConnectionHandler`、`ISptWebSocketMessageHandler`、`IDialogueChatBot`、`IChatCommand`、`ISptCommand`、`IChatMessageHandler`、`IProfileMigration`、`IWeatherPreset`、`IInventoryMagGen`、`IRepeatableQuestGenerator`、`ISerializer`、`ICloner`、`IJsonConverterRegistrator` | 见各 `DI/`、`Helpers/`、`Generators/`、`Servers/` 目录 |

**注意**：5.0 **不存在** `ConfigServer` / `DatabaseServer` 类（已全库确认）。配置经 `ConfigLoader` 加载后按类型注册为单例；数据库经 `DatabaseTables.AddToServices` 注册各表——这是与 3.11（TS）时代认知最大的差别，也是与 4.1 C# 一致的形态。

---

## 3. 4.1 → 5.0 的 mod API 差异

| 项目 | 4.1.x | 5.0.0 | 影响 |
|------|-------|-------|------|
| `IModMetadata` 属性 | 11 个属性 | **完全相同** | mod 元数据类无需改动 |
| `ModLoader` / `ModValidator` | — | **逻辑一致** | 加载规则不变 |
| `Injectable` 特性 | `[Injectable(InjectionType, typePriority)]` | **完全相同** | DI 写法不变 |
| `Router` / `RouteAction` 基类 | — | **完全相同** | 路由写法不变 |
| `ProfileChange` 输出模型 | 旧字段集 | 新增 `ChangedHideoutStashes`/`CompletableItems`/`ReadQuestData`/`NewQuestNotes`/`VariableValues`/`SeasonalRewards`/`BattlePassProgress`/`BattlePassUniversalDocumentBalance`；`TraderData` 增 `DialogueAvailable` | 直接构造/序列化 `ProfileChange` 的 mod 需补字段 |
| 删除的请求模型 | `GetClientDialogueRequestData`、`GetAchievementListRequest` | **已删除**；`/client/dialogue` 改用 `EmptyRequestData`，`/client/quest/list` 改 `StreamedRouteAction` | 引用者需改代码 |
| 版本号 | `SptVersion=4.1.5` | `SptVersion=5.0.0` | 老 mod 的 `SptVersion` 区间不含 5.0.0 会被拒载 |
| 客户端版本 | `compatibleTarkovVersion=0.16.9.40743` | `1.1.5.0.47242` | 客户端 mod / 启动器门禁变化 |
| 新增可依赖能力 | — | 赛季/通行证/商店/结局/任务链/教程；新表 `SeasonTable`、`ShopTable` | 新扩展点，旧 mod 不强制使用 |

**框架层无破坏性变更**——`git diff origin/4.1x-dev...5.0x-dev` 对 `Models/Spt/Mod`、`Modding`、`DI` 目录无输出。

---

## 4. 生态配套现状（2026-09-13，较 09-05 有重大进展）

| 仓库 | 4.1 线 | 5.0 线 | 变化 |
|------|--------|--------|------|
| `SP-Tushonka/server-csharp` | `4.1x-dev` / `main` | **`5.0x-dev`** | — |
| `SP-Tushonka/modules`（客户端 BepInEx） | `4.0.x-dev` / `master` | **`5.0x-dev`** | **新增**（09-05 时无） |
| `SP-Tushonka/launcher` | `4.1.x-dev` / `mod-manager` | **`5.0.x-dev`** | **新增**（09-05 时无） |

即：**服务端 + 客户端模块 + 启动器三条 5.0 分支均已出现**，生态开始成形（成熟度待验证）。另有 `5.0.0-BEM-20260909/0910` 预发布标签。

---

## 5. 本项目能力评估：能否移植/编写 SPT5 mod

### 5.1 服务端 mod —— 已具备，成本低

| 场景 | 评估 | 说明 |
|------|------|------|
| **新写 SPT5 服务端 mod** | **可** | API 面完整；4.1 的 `modding-guide/`、`recipes/`、`api-notes-4.1/` 知识**大部分可迁移**（DI/Router/生命周期写法一致） |
| **4.1 mod 移植到 5.0** | **低成本** | ① `SptVersion` 区间改为含 `5.0.0`；② 对 `SPTarkov.Server.Core 5.0.0` 重编译；③ 若引用了已删请求模型则小改 |
| **依赖新系统（通行证/赛季/商店/结局）** | 需学习 | 属功能扩展，非兼容修复；需新增 `api-notes-5.0` 沉淀 |

### 5.2 客户端 mod（BepInEx）—— 能力缺口所在

- SPT 服务端对 client mod 的校验规则未变；**真正的门槛是 EFT `1.1.5.0.47242` 的 `Assembly-CSharp` 类名/命名空间**——4.1 的 `Class_Name_Mappings` 不能直接套用。
- 本项目已有 3.11→4.1 客户端迁移流水线经验（`curated/migration/client-mod-311-to-41.md`、混淆名映射系列），方法可复用，但**需要一次 5.0 类名映射重建**。

### 5.3 能力缺口清单

| 缺口 | 严重度 | 建议动作 |
|------|--------|----------|
| 无 5.0 API 笔记（对照 `api-notes-4.1/`） | 中 | 待 5.0 稳定后建 `api-notes-5.0/` |
| 无 EFT 1.1.5 客户端类名映射 | **高**（客户端 mod） | 反编译 1.1.5 `Assembly-CSharp` 重建映射 |
| 官方无 modding 文档（仓库内无 `MODDING.md`/`docs/`） | 中 | 依赖源码 XML 注释 + `Testing/TestMod` 示例 |
| 5.0 生态成熟度未知（无正式版、客户端分支新出） | **高** | 观察；勿用于生产整合包 |
| mod 模板（`templates/server-mod` 面向 4.1） | 中 | 新增 5.0 模板前先确认 API 冻结 |

---

## 6. 建议

1. **能力建设（可立即做，不依赖 5.0 稳定）**：把本报告的 API 面沉淀为 `curated/api-notes-5.0/`（或先写「4.1/5.0 通用 mod API」合并笔记），因为**框架层 4.1≈5.0**，写作成本低、复用价值高。
2. **客户端映射（高价值、需人力）**：待取得 EFT 1.1.5 客户端后，复用 3.11→4.1 流水线做 5.0 类名映射。
3. **模板与配方**：现有 `templates/server-mod`（4.1）先保留；5.0 模板待 API 冻结后再建。
4. **生产策略**：整合包仍以 4.1.5 为稳定线；5.0 仅作能力预研（与 `VERSIONS.md` 现行策略一致，策略变更需 Overseer 决策）。

---

## 7. 证据索引

- 元数据：`Libraries/SPTushonka.Server.Core/Models/Spt/Mod/IModMetadata.cs:30-102`
- 加载器：`SPTushonka.Server/Modding/ModLoader.cs:20,122,133-143,259-270,307-342`
- 校验器：`SPTushonka.Server/Modding/ModValidator.cs:91-95,156-211,243-324,331-365`
- 生命周期：`Libraries/SPTushonka.Server.Core/DI/IOnLoad.cs:5`、`DI/IOnUpdate.cs:3`、`DI/IOnDIConstruct.cs:24`、`DI/OnLoadOrder.cs:3`
- DI：`Libraries/SPTushonka.DI/Annotations/Injectable.cs:7`、`DependencyInjectionHandler.cs:74`
- 路由：`Libraries/SPTushonka.Server.Core/DI/Router.cs:22,62,93,148,179-239`、`DI/Routing/ItemEventRouter.cs:8,44,58`
- 自定义内容：`Services/Modding/Custom/CustomItemService.cs:47,139,404`、`Services/Modding/Custom/CustomQuestService.cs:24`
- 持久化：`Services/Modding/ProfileDataService.cs:76,102`、`Servers/SaveServer.cs:93,122,188,253`
- 配置/数据库：`Loaders/ConfigLoader.cs:16`、`SPTushonka.Server/Helpers/DatabaseTables.cs:6`
- Web：`Libraries/SPTushonka.Server.Web/IModBlazorMetadata.cs:21`、`SPTWeb.cs:24,94`
- Prepatch：`SPTushonka.Server/Modding/EnumPatcher.cs`、`PrepatchAssemblyWriter.cs`、`PrepatchLoadContext.cs`
- 启动链：`SPTushonka.Server/Program.cs:189-197,258`、`Extensions/ProgramExtensions.cs:16`
- 示例：`Testing/TestMod/`、`Testing/TestMod2/`

---

## 附：与既有文档的关系

- 本报告**更新** `docs/spt-5.0x-dev-现状报告.md` 第 3 节的「配套缺失」结论：`modules`/`launcher` 的 5.0 分支**已出现**（2026-09-13 核实）。
- 本报告聚焦 **mod API 能力**，不重复该报告的 git 血统/完成度分析。
- 5.0 源码克隆记录见 `knowledge/spt-kb/curated/operations/5xx-source-verification.md`（[5.0]）。
