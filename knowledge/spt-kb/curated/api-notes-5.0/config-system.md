---
version: [5.0]
domain: server
topic: config
source: curated
---
# 配置系统笔记 [5.0]

> **[UNSTABLE-PREVIEW]** SPT 5.0 处于开发初期（`5.0x-dev` 分支，尚无正式版）。本笔记为 2026-09-13 源码快照（HEAD `ff0bf3281`）提炼，上游 API 可能变更；使用前请以源码复核，勿据此做长期承诺。

> 状态：**源码实读（2026-09-13）** | 版本：[5.0]
> 源码：`Libraries/SPTushonka.Server.Core/Loaders/ConfigLoader.cs`、`Models/Enums/ConfigTypes.cs`、`Models/Spt/Config/BaseConfig.cs`、`SPTushonka.Server/Helpers/ProgramHelpers.cs`

## 关键事实：无 ConfigServer

- 5.0 **不存在** `ConfigServer` 类。配置由**静态类** `ConfigLoader` 在宿主构建前一次性读入，随后按 **CLR 类型**注册为 DI 单例。
- 这是与 3.11（TypeScript，`ConfigServer` + `spt-*` 字符串键）最大的差别，也是与 4.1 C# 一致的形态。
- 配置对象是 C# record，继承抽象 `BaseConfig`，带 `kind` 属性（`BaseConfig.cs:5-9`）。例：`CoreConfig.Kind = "spt-core"`（`Models/Spt/Config/CoreConfig.cs:7-10`）。

## `ConfigLoader` 加载机制（`Loaders/ConfigLoader.cs`）

| 项 | 值 | 位置 |
|---|-----|------|
| 配置目录 | `SPT_Data/configs`（相对工作目录；源码仓库不含该目录） | `ConfigLoader.cs:13` |
| 接受扩展名 | `.json`、`.jsonc` | `ConfigLoader.cs:14,61-64` |
| 入口 | `static Task<IReadOnlyDictionary<Type, BaseConfig>> Initialize(ILogger?, CancellationToken)` | `ConfigLoader.cs:16-19` |
| 文件枚举 | `Directory.GetFiles(_configPath, "*")`（不递归） | `ConfigLoader.cs:57` |
| JSON 选项 | `ReadCommentHandling = Skip`（支持 JSONC）、`WriteIndented = false`、`DefaultIgnoreCondition = WhenWritingNull`、`Encoder = UnsafeRelaxedJsonEscaping` | `ConfigLoader.cs:26-36` |
| DEBUG 专有 | `#if DEBUG` 时 `UnmappedMemberHandling = Disallow`（未知字段报错） | `ConfigLoader.cs:32-34` |
| 转换器 | `ListOrTConverterFactory`、`DictionaryOrListConverter`、`ProcessBaseTradeRequestDataConverter`、`StringToMongoIdConverter`、`EftEnumConverterFactory`、`EftListEnumConverterFactory`、`EnumerableConverterFactory`、`StringOrIntConverterFactory`、`FloatOrIrregularFloatArrayFactory` | `ConfigLoader.cs:38-54` |

流程（`ConfigLoader.cs:56-101`）：
1. 遍历文件，跳过非 `.json`/`.jsonc`（`:59-64`）。
2. `GetConfigTypeByFilename(file)` 求配置类型（`:66`）。
3. 找不到类型 → `LogError("... has no associated ConfigTypes entry. Skipping.")` 并跳过（`:68-72`）。
4. 反序列化为该类型（`:74-83`）；`JsonException` → 记错误并 **throw**（服务器拒绝启动，`:85-90`）；结果为 `null` → 记错误并 **throw**（`:92-96`）。
5. `configs[configType] = (BaseConfig)deserializedContent`（`:98`）；返回 `IReadOnlyDictionary<Type, BaseConfig>`（`:101`）。

### 文件名 → 配置类型的匹配规则（易错点）

`GetConfigTypeByFilename`（`ConfigLoader.cs:104-115`）**不是**「`"spt-" + 文件名`」精确匹配，而是：

```csharp
Func<ConfigTypes, bool> filterMethod = entry => entry.GetValue().Contains(Path.GetFileNameWithoutExtension(filename));
```

即：把文件名（去扩展名）对每个 `ConfigTypes` 的 `GetValue()`（`spt-*` 串）做**子串包含**判断，取第一个命中的枚举值，再经 `GetConfigType()` 得到 CLR 类型（`ConfigTypes.cs:43-77`）。因此 `core.json` 命中 `spt-core` → `CoreConfig`。文件名若不含任何 `spt-*` 串，该配置被跳过（不是错误）。

## 全部配置类型清单（源码 28 种）

`ConfigTypes` 枚举共 **28 个成员**（`Models/Enums/ConfigTypes.cs:80-110`）。注意：能力评估报告曾记「26 种」，以本次源码实读为准（`BTR_DELIVERY`、`LOST_ON_DEATH`、`GIFTS` 等均在列）。

| `ConfigTypes` | `GetValue()`（`kind`） | CLR 类型 |
|---------------|------------------------|----------|
| `AIRDROP` | `spt-airdrop` | `AirdropConfig` |
| `BACKUP` | `spt-backup` | `BackupConfig` |
| `BOT` | `spt-bot` | `BotConfig` |
| `BTR_DELIVERY` | `spt-btrdelivery` | `BtrDeliveryConfig` |
| `PMC` | `spt-pmc` | `PmcConfig` |
| `CORE` | `spt-core` | `CoreConfig` |
| `HEALTH` | `spt-health` | `HealthConfig` |
| `HIDEOUT` | `spt-hideout` | `HideoutConfig` |
| `HTTP` | `spt-http` | `HttpConfig` |
| `IN_RAID` | `spt-inraid` | `InRaidConfig` |
| `INSURANCE` | `spt-insurance` | `InsuranceConfig` |
| `INVENTORY` | `spt-inventory` | `InventoryConfig` |
| `ITEM` | `spt-item` | `ItemConfig` |
| `LOCALE` | `spt-locale` | `LocaleConfig` |
| `LOCATION` | `spt-location` | `LocationConfig` |
| `LOOT` | `spt-loot` | `LootConfig` |
| `MATCH` | `spt-match` | `MatchConfig` |
| `PLAYERSCAV` | `spt-playerscav` | `PlayerScavConfig` |
| `PMC_CHAT_RESPONSE` | `spt-pmcchatresponse` | `PmcChatResponseConfig` |
| `QUEST` | `spt-quest` | `QuestConfig` |
| `RAGFAIR` | `spt-ragfair` | `RagfairConfig` |
| `REPAIR` | `spt-repair` | `RepairConfig` |
| `SCAVCASE` | `spt-scavcase` | `ScavCaseConfig` |
| `TRADER` | `spt-trader` | `TraderConfig` |
| `WEATHER` | `spt-weather` | `WeatherConfig` |
| `SEASONAL_EVENT` | `spt-seasonalevents` | `SeasonalEventConfig` |
| `LOST_ON_DEATH` | `spt-lostondeath` | `LostOnDeathConfig` |
| `GIFTS` | `spt-gifts` | `GiftsConfig` |

`GetValue()` 与 `GetConfigType()` 均为 switch 表达式，未知值抛 `ArgumentOutOfRangeException`（`ConfigTypes.cs:39-40,75-76`）。

## 配置对象如何注册进 DI

- `ProgramHelpers.CreateNewHostBuilder` 在宿主构建时把**配置字典逐项注册**（`ProgramHelpers.cs:44-47`）：

```csharp
foreach (var configEntry in configuration)
{
    builder.Services.AddSingleton(configEntry.Key, configEntry.Value);
}
```

  - key 是**具体配置类型**（`Type`），value 是已反序列化的实例 → 解析时按 `CoreConfig`、`HttpConfig` 等具体类型注入即可。
  - `DatabaseTables` 也在此方法内注册（`ProgramHelpers.cs:49-59`）。
- 早期 provider 同样带配置：`CreateEarlySptProvider` → `CreateNewHostBuilder(..., localeTable: CreateEarlyLocaleTable())`（`ProgramHelpers.cs:115-141`），供 `ModLoader`/`ModValidator` 等在 Web 主机前使用。
- **mod 自定义配置**：通过 `IOnDIConstruct` 在容器构建前手动注册（`IOnDIConstruct.cs:24-36`、`ServiceCollectionExtensions.cs:11-53`）；该接口文档明确说明它用于「注册 mod 专属配置或无法经 `[Injectable]` 注册的服务」（`IOnDIConstruct.cs:9-23`）。`ConfigLoader` 本身**只扫 `SPT_Data/configs`，不扫 `user/mods/`**（`ConfigLoader.cs:13,57`）。

## mod 读取 / 修改配置

- **读取**：构造函数注入具体配置类型。内建示例：
  - `SaveCallbacks(..., CoreConfig coreConfig)` → `coreConfig.ProfileSaveIntervalInSeconds`（`Callbacks/SaveCallbacks.cs:12-18,35`）。
  - `SptHttpListener(..., HttpConfig? httpConfig = null)` → `httpConfig?.LogRequests`（`Servers/Http/SptHttpListener.cs:21-29,40`）。
  - `Program.cs` 直接 `app.Services.GetRequiredService<HttpConfig>()`（`Program.cs:260`）。
- **修改**：配置是 DI 单例，注入得到的对象是**共享引用**；在 `IOnLoad`（尤其 Pre-SPT-load 阶段，见 architecture-map）里改字段即在运行时生效。例：`ProgramExtensions.RunPreSptLoadCallbacks` 的注释明确「让 mod 在容器启动前改 SPT config，例如让 Fika 能改 `HttpConfig`」（`Extensions/ProgramExtensions.cs:18-19`）。
- **覆盖语义**：**没有**「默认 config → mod config 层叠/覆盖」机制；`ConfigLoader` 只读 `SPT_Data/configs`，mod 目录下的 `config/*.json` 不被自动加载（`ConfigLoader.cs:13,57`）。mod 若要独立配置，需自管 JSON（`IOnDIConstruct` 注册 + 自读），或直接改内建单例对象。
- **时机**：Pre-SPT-load（`TypePriority < GameCallbacks`）改配置可影响容器构建后的行为；`IOnDIConstruct` 可注册新服务/配置。

## `CoreConfig` 关键字段（示例）

`Models/Spt/Config/CoreConfig.cs:7-56`：`Kind="spt-core"`、`ProjectName`、`CompatibleTarkovVersion`、`ServerName`、`ProfileSaveIntervalInSeconds`（JSON `profileSaveIntervalSeconds`，`:21-22`）、`SptFriendNickname`、`AllowProfileWipe`、`BsgLogging`、`Release`、`Fixes`（含 `RemoveModItemsFromProfile` 等，`:163-197`）、`Survey`、`Features`（含 `CompressProfile`，`:199-218`）、`ServerStartTime`、`CustomWatermarkLocaleKeys`。

## 与 4.1 / 3.11 的关系

- 与 4.1：形态一致（C# record + 静态 Loader + 按类型单例）。
- 与 3.11：3.11 用 `ConfigServer.getConfig(ConfigTypes.X)` 与 `spt-*` 字符串键、`assets/configs/`（开发版）或 `SPT_Data/Server/configs/`（编译版）；5.0 无 `ConfigServer`，改为 `SPT_Data/configs` + 类型注入。

## 已核实位置

- 加载器：`Libraries/SPTushonka.Server.Core/Loaders/ConfigLoader.cs:13-14,16-101,104-115`
- 枚举：`Libraries/SPTushonka.Server.Core/Models/Enums/ConfigTypes.cs:7-77,80-110`
- 基类：`Libraries/SPTushonka.Server.Core/Models/Spt/Config/BaseConfig.cs:5-9`
- DI 注册：`SPTushonka.Server/Helpers/ProgramHelpers.cs:44-47,115-141`
- 自定义注册：`Libraries/SPTushonka.Server.Core/DI/IOnDIConstruct.cs:24-36`、`SPTushonka.Server/Extensions/ServiceCollectionExtensions.cs:11-53`
- 内建用法：`Callbacks/SaveCallbacks.cs:12-18,35`、`Servers/Http/SptHttpListener.cs:21-29,40`、`Program.cs:260`
