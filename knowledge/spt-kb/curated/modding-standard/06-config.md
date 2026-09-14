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

## 规则

### STD-CFG-001 — 服务端配置置于 mod 自身目录内并用 ModHelper 解析绝对路径

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`Libraries/SPTarkov.Server.Core/Helpers/Server/ModHelper.cs:10-19`、`knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md:65-76`（EV-GAP-CFG）；语料：`GetAbsolutePathToModFolder` 91 处（EV-GAP-CFG）
- **Rule:** 服务端 mod 的配置文件必须随 mod 一同部署在 `user/mods/<ModName>/` 内，运行时用 `ModHelper.GetAbsolutePathToModFolder(Assembly)` 取得 mod 根目录后拼接相对路径读取；禁止硬编码绝对路径或依赖进程工作目录。

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
- **Rule:** mod 私有配置应放在 mod 根目录的 `config/` 子目录下并命名 `config.jsonc`，优先使用允许注释的 `.jsonc` 扩展名；`.json` 可接受但非首选。

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

> 5.0 差异：命名空间前缀为 `SPTushonka.*`（如 `SPTushonka.Server.Core.DI`），API 形态一致。
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
