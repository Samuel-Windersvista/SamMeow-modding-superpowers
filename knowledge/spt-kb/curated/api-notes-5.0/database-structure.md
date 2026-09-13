---
version: [5.0]
domain: server
topic: database
source: curated
---
# 数据库结构笔记 [5.0]

> **[UNSTABLE-PREVIEW]** SPT 5.0 处于开发初期（`5.0x-dev` 分支，尚无正式版）。本笔记为 2026-09-13 源码快照（HEAD `ff0bf3281`）提炼，上游 API 可能变更；使用前请以源码复核，勿据此做长期承诺。

> 状态：**源码实读（2026-09-13）** | 版本：[5.0]
> 源码：`SPTushonka.Server/Helpers/DatabaseTables.cs`、`SPTushonka.Server/Helpers/DatabaseImporter.cs`、`Libraries/SPTushonka.Server.Core/Models/Spt/Tables/`、`Libraries/SPTushonka.Server.Core/Utils/ImporterUtil.cs`

## 关键事实：无 DatabaseServer

- 5.0 **不存在** `DatabaseServer` 类。数据库是一个不可变记录 `DatabaseTables`（12 张强类型表），由 `DatabaseImporter` 从磁盘导入后，经 `DatabaseTables.AddToServices` 把**每张表**注册为 DI 单例。
- 与 3.11（`DatabaseServer.getTables()` 可变对象树 + `IDatabaseTables`）不同，5.0 是 C# 强类型表，与 4.1 形态一致。
- 源码仓库不含 `SPT_Data/`；数据库 JSON 与配置一样是运行时资产（`DatabaseImporter.cs:18`）。

## `DatabaseTables` 全部表清单（`SPTushonka.Server/Helpers/DatabaseTables.cs`）

`public sealed record DatabaseTables`（`:6`），12 个 `required` 属性（`:8-30`）：

| 属性 | JSON 概念 | 表类型（`Models/Spt/Tables/`） | 位置 |
|------|-----------|-------------------------------|------|
| `Bots` | bots | `BotTable` | `DatabaseTables.cs:8` |
| `Hideout` | hideout | `HideoutTable` | `:10` |
| `Locales` | locales | `LocaleTable` | `:12` |
| `Locations` | locations | `LocationTable` | `:14` |
| `Match` | match | `MatchTable` | `:16` |
| `Templates` | templates | `TemplateTable` | `:18` |
| `Traders` | traders | `TradersTable` | `:20` |
| `Globals` | globals | `GlobalTable` | `:22` |
| `Season` | season | `SeasonTable` | `:24` |
| `Shop` | shop | `ShopTable` | `:26` |
| `Server` | server | `ServerTable` | `:28` |
| `Settings` | settings | `SettingsTable` | `:30` |

### 注册进 DI（`AddToServices`，`DatabaseTables.cs:32-46`）

```csharp
public void AddToServices(IServiceCollection services)
{
    services.AddSingleton(Bots);
    services.AddSingleton(Hideout);
    // ... 每张表各 AddSingleton
    services.AddSingleton(Settings);
}
```

- 由 `ProgramHelpers.CreateNewHostBuilder` 调用：`databaseTables.AddToServices(builder.Services)`（`ProgramHelpers.cs:59`）。
- **`DatabaseTables` 记录本身不注册**，只注册 12 张表实例；因此 mod 应注入具体表类型（如 `TemplateTable`），而非 `DatabaseTables`。
- 5.0 相对早期版本新增 `SeasonTable`、`ShopTable`（`DatabaseTables.cs:24,26`），对应赛季 / 商店系统。

## 表模型组织（`Libraries/SPTushonka.Server.Core/Models/Spt/Tables/`）

- 12 个顶层表记录文件：`BotTable.cs`、`GlobalTable.cs`、`HideoutTable.cs`、`LocaleTable.cs`、`LocationTable.cs`、`MatchTable.cs`、`SeasonTable.cs`、`ServerTable.cs`、`SettingsTable.cs`、`ShopTable.cs`、`TemplateTable.cs`、`TradersTable.cs`。
- `Globals/` 子目录 4 个拆分记录：`ExpGlobals.cs`、`HealthGlobals.cs`、`RagfairGlobals.cs`、`TutorialGlobals.cs`（被 `GlobalTable` 的组合类型引用，`GlobalTable.cs:7,282,432,444,590`）。
- 每个表是 C# record，属性用 `[JsonPropertyName("...")]` 映射 JSON，且大量 `required`（编译期强制导入时填充）。示例：
  - `TemplateTable`（`TemplateTable.cs:9-97`）：`Character`、`CustomisationStorage`、`Items`（`Dictionary<MongoId, TemplateItem>`）、`MainQuestNotes`、`Prestige`、`Quests`、`QuestChains`、`VariableGroups`、`QuestVariables`、`SubtitleTracks`、`Tapes`、`Endings`、`RepeatableQuests`、`Handbook`、`Customization`、`Dialogue`、`Profiles`、`Prices`、`DefaultEquipmentPresets`、`Achievements`、`CustomAchievements`、`LocationServices`、`TutorialLoadout?`。
  - `GlobalTable`（`GlobalTable.cs:11-30`）：`Configuration`（JSON `config`）、`LocationInfection`、`BotPresets`、`BotWeaponScatterings`、`ItemPresets`、`InventoryTarcoinMigrationProdAllowedAids`；其组合类型 `GlobalConfig`（`:261-593`）含 `Exp`/`Health`/`RagFair`/`Tutorial` 等子记录。

## 导入流程：JSON → 强类型表

### `DatabaseImporter`（`SPTushonka.Server/Helpers/DatabaseImporter.cs`）

| 项 | 值 | 位置 |
|---|-----|------|
| 数据根 | `SptDataPath = "./SPT_Data/"` | `DatabaseImporter.cs:18` |
| 依赖 | `ISptLogger<DatabaseImporter>`、`ServerLocalisationService`、`ImporterUtil`、`JsonUtil` | `:11-16` |

1. **`LoadHashesAsync(token)`**（`:22-55`）：
   - 读 `SPT_Data/checks.dat`，内容为 **base64 编码的 JSON**（`:24-38`）。
   - 解码为 `List<FileHash>`（内部类，字段 `Path`/`Hash`，`:135-139`），写入 `_databaseHashes` 字典（`:44-47`）。
   - `VerifyFilesExist()`（`:60-80`）：逐个检查清单文件是否在磁盘；缺失则记错误（最多报 10 个）并抛 `ValidationErrorException`（`:69-79`）。**该步只为「文件存在性」，真正的哈希比对在导入时做。**
   - 文件缺失/解码失败统一包成 `ValidationErrorException`（`:49-52`）。
2. **`LoadDatabaseAsync(shouldVerify, token)`**（`:91-118`）：
   ```csharp
   var dataToImport = await importerUtil.LoadRecursiveAsync<DatabaseTables>(
       $"{SptDataPath}database/",
       shouldVerifyDatabase ? VerifyDatabaseAsync : null,
       cancellationToken: cancellationToken);
   ```
   返回 `DatabaseTables?`；异常 `OperationCanceledException` 记 warning 后重抛（`:112-117`）。
3. **`VerifyDatabaseAsync(fileName, computedHash, token)`**（`:123-133`）：把计算哈希与 `_databaseHashes` 清单比对，不一致抛 `ValidationErrorException`（`:127-130`）。哈希算法由 `ImporterUtil` 侧的 `XxHash3` 计算（`ImporterUtil.cs:245`）。
4. 调用点：`Program.StartServerAfterModLoading`（`Program.cs:214-224`）；非 DEBUG 才 `LoadHashesAsync`（`Program.cs:216-220`）。

### `ImporterUtil`：JSON 文件路径 == 对象属性路径（`Libraries/SPTushonka.Server.Core/Utils/ImporterUtil.cs`）

`[Injectable(InjectionType.Singleton)]`（`:15`）。核心方法 `LoadRecursiveAsync<T>(filePath, onFileHashed, onObjectDeserialized, token)`（`:21-31`）。

- **忽略清单**（`:18-19`）：
  - 目录：`./SPT_Data/database/locales/server`、`./SPT_Data/database/locales/web`（`:18,83-86`）。
  - 文件：`bearsuits.json`、`usecsuits.json`、`archivedquests.json`（`:19,67-73`）。
- **递归遍历**（`:44-97`）：对 `filePath` 下每个文件与子目录派发任务，`Task.WhenAll` 等待（`:94`）。
- **文件 → 属性**（`ProcessFileAsync`，`:99-148`）：
  - `GetSetMethod(去扩展名小写文件名, loadedType, out propertyType, out isDictionary)`（`:114-119`）——按**文件名**找同名属性（大小写不敏感，见 `GetSetMethod`，`:293-299`）。
  - 反序列化后经反射 `setMethod.Invoke`（`:130-133`）；若目标属性是 `Dictionary<,>`，用 `Add(strippedFileName, value)`（`:132,283-288`）。
  - 反序列化/找属性失败 → 记 `Critical` 并抛（`:143-147`）。
- **目录 → 属性/字典项**（`ProcessDirectoryAsync`，`:150-223`）：
  - 目录名去下划线（`:164`）。
  - **MongoId 命名的目录**（如 `traders/<mongoId>/`）：取其**父目录名**定位属性（`:166-172`），递归加载后写入父级字典 `dictionary[new MongoId(directoryName)] = loadedData`（`:184-191`）。
  - 普通目录：`GetSetMethod(目录名, ...)` 后递归并 `setMethod.Invoke`（`:195-208`）。
- **`LazyLoad<T>` 支持**（`DeserializeFileAsync`，`:225-252`）：属性类型是 `LazyLoad<>` 时，用表达式树构造延迟加载委托（`:232-235,254-276`）；否则直接反序列化。
- **边读边哈希**：传入 `onFileHashed` 时用 `HashingReadStream(fileStream, new XxHash3())` 在反序列化同一遍算出哈希（`:244-249`），供 `VerifyDatabaseAsync` 校验。

一句话：`SPT_Data/database/<子目录>/<文件>.json` 的路径逐段映射到 `DatabaseTables` 的属性 / 字典键，`locales/server`、`locales/web` 与 3 个 suit 文件被显式排除。

## mod 如何注入并修改表

- **注入具体表类型**（表实例已注册为单例）：例如内建 `ProfileValidatorHelper(TemplateTable templateTable, TradersTable traderTable, CoreConfig coreConfig, ...)`（`Helpers/Profile/ProfileValidatorHelper.cs:16-22`）——用 `templateTable.Items`、`templateTable.Customization`、`traderTable.GetTrader(id)`（`:51,175,387`）。
- **修改**：表是 record，但集合属性（`Dictionary`/`List`）是可变引用；mod 在 `IOnLoad`（尤其数据库导入后阶段）改集合内容即生效。注意 `DatabaseTables` 的属性是 `init`，但表**内部**集合可增删改。
- 内建修改示例：`CustomItemService`、`DatabaseIntegrityService.TakeItemSnapshot()`（`Callbacks/SaveCallbacks.cs:25`）等。
- 无「URL import / 重载」机制：数据库为内存对象，导入一次；5.0 的启动顺序保证 `DatabaseImporter` 在 Web 主机与 `IOnLoad` 之前完成（见 architecture-map）。

## 与 4.1 / 3.11 的关系

- 与 4.1：形态一致（`DatabaseTables` + `AddToServices` + 强类型表）。
- 与 3.11：3.11 用 `DatabaseServer.getTables()` 返回可变对象树、`ImporterUtil.placeObject` 原地挂载；5.0 改为强类型 record + 反射属性映射，且无 `DatabaseServer`。

## 已核实位置

- 表清单/注册：`SPTushonka.Server/Helpers/DatabaseTables.cs:6-46`
- 导入器：`SPTushonka.Server/Helpers/DatabaseImporter.cs:11-55,60-80,91-118,123-133`
- 导入工具：`Libraries/SPTushonka.Server.Core/Utils/ImporterUtil.cs:15-31,44-97,99-148,150-223,225-276,278-328`
- 表模型：`Libraries/SPTushonka.Server.Core/Models/Spt/Tables/TemplateTable.cs:9-97`、`GlobalTable.cs:11-30,261-593`
- DI 注册调用：`SPTushonka.Server/Helpers/ProgramHelpers.cs:59`
- 内建注入示例：`Libraries/SPTushonka.Server.Core/Helpers/Profile/ProfileValidatorHelper.cs:16-22,51,175,387`
