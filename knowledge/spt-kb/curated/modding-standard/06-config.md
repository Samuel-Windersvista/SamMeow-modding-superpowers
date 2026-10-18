---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 06 配置系统（CFG）

> **Domain slug:** `CFG` · **规则 ID 前缀:** `STD-CFG-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 04，2026-09-14）。

## 维度范围

- 服务端标准路径 `config/config.jsonc` + `defaultConfig.jsonc` 首启复制模式
- `IOnDIConstruct` + `AddSingleton` 加载
- 配置类禁止 `[Injectable]`
- 客户端 BepInEx `Config.Bind`
- 边界：本维度仅覆盖「玩家可改的运行时配置」；随包交付的只读内容数据（如商人 assort、静态数据文件）不属于配置，相关规则判 N-A

## 规则

### STD-CFG-001 — 服务端配置置于 mod 自身目录内并用 ModHelper 解析绝对路径

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`Libraries/SPTarkov.Server.Core/Helpers/Server/ModHelper.cs:10-19`、`knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md:65-76`（EV-GAP-CFG）；语料：`GetAbsolutePathToModFolder` 91 处（EV-GAP-CFG）
- **Rule:** 服务端 mod 的配置文件必须随 mod 一同部署在 `user/mods/<ModName>/` 内，运行时用 `ModHelper.GetAbsolutePathToModFolder(Assembly)` 取得 mod 根目录后拼接相对路径读取；禁止硬编码绝对路径或依赖进程工作目录。本规则仅适用于玩家可改的运行时配置；随包交付的只读内容数据（如商人 assort、静态数据文件）不属于配置，判 N-A。

```csharp
using SPTarkov.Server.Core.Helpers.Server;

[Injectable]
public class MyConfigManager(ModHelper modHelper)
{
    public string GetConfigPath()
    {
        // 返回 user/mods/<ModName>/（DLL 所在目录），再拼接相对路径
        string modFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        return Path.Combine(modFolder, "config", "config.jsonc");
    }
}
```

> 深入：[api-notes-4.1/config-system.md](../api-notes-4.1/config-system.md)

### STD-CFG-002 — 采用 config/config.jsonc 子目录与 .jsonc 扩展名约定

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md:65-76`（EV-GAP-CFG）；语料：`config/` 20 次 vs 根目录 15 次（EV-GAP-CFG）、`*.jsonc` 85 个 vs `config.json` 47 个（EV-CORPUS-CFG）
- **Rule:** mod 私有配置应放在 mod 根目录的 `config/` 子目录下并命名 `config.jsonc`，优先使用允许注释的 `.jsonc` 扩展名；`.json` 可接受但非首选。本规则同样只覆盖玩家可改的运行时配置；只读内容数据判 N-A。

```text
user/mods/<ModName>/
├── config/
│   ├── config.jsonc         # 玩家可改的运行时配置
│   └── defaultConfig.jsonc  # 首次安装/配置损坏时的默认副本
└── <ModName>.dll
```

> 深入：[api-notes-4.1/config-system.md](../api-notes-4.1/config-system.md)

### STD-CFG-003 — 用 IOnDIConstruct 与 AddSingleton 注册配置并构造注入消费

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`Libraries/SPTarkov.Server.Core/DI/IOnDIConstruct.cs`（EV-GAP-CFG、EV-MECH-COORD）、`knowledge/spt-kb/curated/api-notes-4.1/di-container.md`；语料：`IOnDIConstruct` 14 处（EV-GAP-CFG）
- **Rule:** mod 私有配置必须在 `IOnDIConstruct.OnDIConstructAsync` 中读盘后经 `serviceCollection.AddSingleton(config)` 注册为 DI 单例；消费方（服务、路由、入口）通过构造函数参数注入该配置类型。

```csharp
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Server.Core.DI;

public class MyModConfigRegistration : IOnDIConstruct
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,                      // 容忍尾随逗号
        ReadCommentHandling = JsonCommentHandling.Skip,  // 支持 // 与 /* */ 注释
    };

    public static async Task OnDIConstructAsync(IServiceCollection services, CancellationToken ct)
    {
        string modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string configPath = Path.Combine(modFolder, "config", "config.jsonc");
        string defaultPath = Path.Combine(modFolder, "config", "defaultConfig.jsonc");

        // 首启（或配置被删）时用默认副本初始化，绝不覆盖玩家已有改动
        if (!File.Exists(configPath) && File.Exists(defaultPath))
        {
            File.Copy(defaultPath, configPath);
        }

        await using var stream = File.OpenRead(configPath);
        MyModConfig config = await JsonSerializer.DeserializeAsync<MyModConfig>(stream, JsonOptions, ct)
            ?? new MyModConfig();

        services.AddSingleton(config);
    }
}
```

> 5.0 差异：API 形态一致；服务端命名空间仍为 `SPTarkov.*`（如 `SPTarkov.DI`），`SPTushonka.DI` 是 NuGet 包名。
> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)

### STD-CFG-004 — 禁止给配置类标注 [Injectable]

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md:75` 明确警告、`knowledge/spt-kb/curated/api-notes-4.1/di-container.md`；语料：无对应计数（机制推断，无语料先例）（EV-NOCORPUS）
- **Rule:** 配置类（POCO / record）不得标注 `[Injectable]`；否则容器会用默认值新建实例，磁盘上的 JSON 永远不会被读取。配置只能经 `IOnDIConstruct` + `AddSingleton` 注册。

```csharp
// 错误：容器用默认值构造实例，config.jsonc 被忽略
// [Injectable]
public record MyModConfig { public bool Enabled { get; set; } = true; }

// 正确：纯 POCO，由 IOnDIConstruct 读盘后 AddSingleton
public record MyModConfig { public bool Enabled { get; set; } = true; }
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)

### STD-CFG-005 — 随附 defaultConfig.jsonc 并在首启复制为 config.jsonc

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`EV-GAP-CFG` 推荐模式（`config/defaultConfig.jsonc` 首启复制）；语料：`config/defaultConfig.jsonc` 1 例（`Ammo-Stats-In-Names_2989_source`，EV-GAP-CFG）
- **Rule:** 除玩家可改的 `config/config.jsonc` 外，应同时提交 `config/defaultConfig.jsonc`，在目标文件不存在时复制为 `config.jsonc`；不得直接覆盖已存在的玩家配置。

```csharp
if (!File.Exists(configPath) && File.Exists(defaultPath))
{
    File.Copy(defaultPath, configPath);
}
```

> 深入：[api-notes-4.1/config-system.md](../api-notes-4.1/config-system.md)

### STD-CFG-006 — 客户端配置使用 BepInEx Config.Bind

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`templates/client-mod/src/Configuration.cs`、`knowledge/spt-kb/curated/modding-guide/03-client-mod-anatomy.md`；语料：源码中 `.cfg` 0 个（EV-CORPUS-CFG，BepInEx 运行时配置不入库）
- **Rule:** 客户端 mod 的配置必须通过 `BaseUnityPlugin.Config`（`ConfigFile`）的 `Config.Bind` 声明，运行时落在 `BepInEx/config/<ModGuid>.cfg`；不要自建 JSON 配置读取。

```csharp
public class MyConfiguration
{
    public ConfigEntry<bool> Enabled { get; }

    public MyConfiguration(ConfigFile configFile)
    {
        Enabled = configFile.Bind(
            "General",   // 分区名
            "Enabled",   // 键名
            true,        // 默认值
            "是否启用本 mod");
    }
}
```

> 深入：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)

### STD-CFG-007 — 在隐藏的 `Internal` 段保存配置 schema 版本

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：BepInEx `ConfigFile.Bind` 可绑定对玩家不可见的分区/项，服务端配置随 `config.jsonc` 落盘（`knowledge/spt-kb/curated/api-notes-4.1/config-system.md`、`templates/client-mod/src/Configuration.cs`）（EV-GAP-CFG）；语料：11 个仓库含 `ConfigVersion` / `configVersion`（EV-CORPUS-CFG），样例 `Better-Thermal-&-Night-Vision_2531_source/BetterThermalNightVision.cs:33`（`Internal` 段 `ConfigVersion`）
- **Rule:** 存在破坏性配置结构变更的 mod，应把 schema 版本号持久化在对玩家隐藏的 `Internal` 段（客户端 BepInEx 用隐藏项；服务端在配置对象上保留同名字段），作为版本不匹配检测的依据；不要把 schema 版本混进玩家可见的普通配置项。

```csharp
// 客户端：schema 版本放在对玩家隐藏的 Internal 段
ConfigEntry<int> configVersion = configFile.Bind(
    "Internal", "ConfigVersion", 1,
    "配置 schema 版本；自动维护，玩家不可见、勿改");
// 装了 BepInEx ConfigurationManager 时可用 ConfigurationManagerAttributes { Browsable = false } 隐藏该项
```

> 深入：[api-notes-4.1/config-system.md](../api-notes-4.1/config-system.md)

### STD-CFG-008 — 配置 schema 版本不匹配时显式迁移或重置，不得静默按新 schema 重解释

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`Config.Bind` 与服务端 `IOnDIConstruct` 读盘时对**已存在**的键保留旧值，结构变更后的旧值会在新 schema 下被静默重新解释（`Libraries/SPTarkov.Server.Core/Helpers/Server/ModHelper.cs:10-19`、`knowledge/spt-kb/curated/api-notes-4.1/config-system.md`）（EV-GAP-CFG）；语料：50 个仓库 / 433 行配置迁移相关代码、11 个仓库含 `ConfigVersion`（EV-CORPUS-CFG），样例 `Better-Thermal-&-Night-Vision_2531_source/BetterThermalNightVision.cs:105-115`（版本不匹配即重置）、`CloudSix-Volumetric-Clouds_2567_source/Source/CloudConfig.cs:203-214`（`Clear`/`Reload` 重置）、`Career-Log_2713_source/src/Persistence/RaidRecordMigration.cs:8-13`（`CurrentSchemaVersion` + `<=0` 回填）
- **Rule:** 读配置时若持久化的 schema 版本与当前版本不匹配，必须显式处理：迁移（按版本顺序重放转换、保留用户可识别值）或整体重置（删除/清空旧文件并从默认副本重建），并记录日志；不得在不做任何处理的情况下直接按新 schema 重新解释旧配置。「整体重置」是合法策略，但必须记录可预期的日志，让用户知道配置已被重置。

```csharp
int written = configFile.Bind("Internal", "ConfigVersion", CurrentSchemaVersion).Value;
if (written != CurrentSchemaVersion)
{
    logger.LogWarning($"配置 schema {written} -> {CurrentSchemaVersion}：迁移或重置");
    if (written <= 0)
    {
        // 无版本记录的旧配置：按当前 schema 回填，保留可识别字段
    }
    else
    {
        // 逐版本重放迁移；无法迁移则重置为默认并记录原因
    }
    configVersionEntry.Value = CurrentSchemaVersion;
}
```

> 深入：[api-notes-4.1/config-system.md](../api-notes-4.1/config-system.md)

### STD-CFG-009 — 让配置 schema 版本独立于发布版本并整数递增

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：schema 版本只需单调可比，与插件发布版本（csproj `<Version>` / `[BepInPlugin]` 版本）无耦合（`knowledge/spt-kb/curated/api-notes-4.1/config-system.md`）；语料：11 个仓库含 `ConfigVersion`（EV-CORPUS-CFG），样例 `Better-Thermal-&-Night-Vision_2531_source/BetterThermalNightVision.cs:33`（整数 schema 版本）与 `CloudSix-Volumetric-Clouds_2567_source/Source/CloudConfig.cs:203-214`（误用发布版本字符串）对照
- **Rule:** schema 版本应独立于发布版本，优先使用从 `1` 起、仅在配置结构发生破坏性变化时才递增的整数；不要把 `ConfigVersion` 绑定到插件版本字符串，否则每次发布都会触发不必要的迁移或重置。

```csharp
private const int CurrentSchemaVersion = 3;   // 与插件版本号解耦；结构破坏性变更时才 +1
```

> 深入：[api-notes-4.1/config-system.md](../api-notes-4.1/config-system.md)

### STD-CFG-010 — 服务端持久化 JSON 记录 `SchemaVersion` 与 `CurrentSchemaVersion` 并对 `<=0` 回填

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：服务端 mod 自行读写持久化文件，加载时需自带版本字段以识别旧格式（`knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md`）（EV-GAP-CFG）；语料：`Career-Log_2713_source/src/Persistence/RaidRecordMigration.cs:8-13`（`CurrentSchemaVersion` 常量 + `<=0` 回填）、50 个仓库 / 433 行迁移相关代码（EV-CORPUS-CFG）
- **Rule:** 服务端 mod 持久化到磁盘的自有 JSON 记录应包含 `SchemaVersion` 字段，并用代码内 `CurrentSchemaVersion` 常量表示当前版本；加载旧记录时若 `SchemaVersion <= 0`（缺失或未初始化）先回填到最早的已知版本，再走显式迁移路径（见 `STD-CFG-008`）。

```csharp
public sealed class RaidRecord
{
    public int SchemaVersion { get; set; }   // 缺失或旧文件为 0
    // ... 业务字段
}

public static class RaidRecordMigration
{
    public const int CurrentSchemaVersion = 3;

    public static RaidRecord Load(RaidRecord record)
    {
        if (record.SchemaVersion <= 0) { record.SchemaVersion = 1; }   // 回填最早的已知版本
        while (record.SchemaVersion < CurrentSchemaVersion)
        {
            MigrateOnce(record);                 // 逐版本转换，保留用户值
            record.SchemaVersion++;
        }
        return record;
    }

    private static void MigrateOnce(RaidRecord record) { /* 单步迁移 */ }
}
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)
