# SPT Mod 编写规范——证据缺口调查报告

调查范围：

- 近期 297 个源码目录（`knowledge/spt-kb/archive/forge/mods/MANIFEST-sp-mod-2026-09.md` 所列）
- 全量 392 个 `_source` 目录（同一 `archive/forge/mods/` 下）
- SPT 4.1/5.0 源码：`E:\云文件\GitHub\SamMeow_SPT410_source_code`、`E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code`
- 官方示例：`external/spt-archive/server-mod-examples`、`mod-examples`、`modules`、`wiki`
- 项目模板：`templates/server-mod/`、`templates/client-mod/`

---

## 缺口 1：ModDependencies 的标准用法

### 机制端结论

`ModDependencies` 是**硬依赖**：声明的依赖 mod 必须存在且版本满足范围，否则服务器拒绝加载全部 mod。

证据：

- `Libraries/SPTarkov.Server.Core/Models/Spt/Mod/IModMetadata.cs:91` 定义：
  ```csharp
  /// Key is the dependency's Mod GUID, value is the required version range
  Dictionary<string, Range>? ModDependencies { get; init; }
  ```
- `SPTarkov.Server/Modding/ModValidator.cs:243-287` 校验逻辑：
  - `ModDependencies == null` 直接通过；
  - key 为 `pkg.ModGuid`（自身）只抛 Warning，不阻止加载；
  - 依赖找不到 → `errorsFound = true`；
  - 依赖版本不满足 `semVer.Satisfies(value.Version, requiredVersion)` → `errorsFound = true`；
  - 任一错误都会导致 `ModValidator` 返回空列表（`modloader-no_mods_loaded`）。
- 本地化文本 `Libraries/SPTarkov.Server.Assets/SPT_Data/database/locales/server/en.json:227/241`：
  - `modloader-missing_dependency`: "Mod: {{mod}} requires: {{modDependency}} to be installed."
  - `modloader-outdated_dependency`: "Mod: {{mod}} requires: {{modDependency}} version: {{requiredVersion}}. Current installed version is: {{currentVersion}}"

### 实践端结论

**语料中没有任何真实非空依赖声明**。所有服务端 `ModDependencies` 都是 `null`、`new()`、`new Dictionary<...>()`、`[]`。

证据：

- 近期 297 个目录中，157 个 `.cs` 文件出现 `ModDependencies`，全部为**空初始化**；
- 全量 392 个 `_source` 目录中，28 个文件以 `= new()` 形式初始化，**0 个**包含真实键值；
- 官方 `server-mod-examples`（`1Logging/Logging.cs:60`、`13AddTraderWithAssortJson/AddTraderWithAssortJson.cs:25` 等）全部置为 `null`；
- 4.1/5.0 源码中的 `Testing/TestMod/TestMod.cs:21`、`Testing/TestMod2/TestMod2.cs:22` 同样为 `null`。

### 推荐写法

```csharp
public Dictionary<string, Range>? ModDependencies { get; init; } = new()
{
    ["com.example.required-mod"] = new Range("~1.0.0"),
    ["com.example.another-dep"] = new Range(">=2.0.0 <3.0.0"),
};
```

- **key**：依赖 mod 的 `ModGuid`（不是程序集名、不是 nuget 包名）。
- **value**：`SemanticVersioning.Range`，支持 `~`、`^`、`>=` 等 npm/semver 语法；4.1/5.0 源码均使用 `SemanticVersioning` 库校验。
- **无依赖时**：保持 `null` 或 `new()` 空字典；不要声明可选/软依赖。
- **与客户端 `[BepInDependency]` 的对应关系**：
  - 服务端 `ModDependencies` 等价于客户端的**硬依赖**（默认 `BepInDependency` 无 `SoftDependency` 标志）；
  - 客户端软依赖示例：`[BepInDependency("com.tyfon.uifixes", BepInDependency.DependencyFlags.SoftDependency)]`（`ReceiveAllChats.Client/Plugin.cs:12`）；
  - 服务端目前**没有软依赖机制**，需要按条件加载的逻辑应在 `IOnLoad` 中自行判断依赖存在性。

---

## 缺口 2：config 文件的标准路径

### 机制端结论

服务端 mod 的配置文件放在**自己的 mod 目录内**，通过 `ModHelper.GetAbsolutePathToModFolder(Assembly)` 取得根路径后拼接相对路径读取；推荐通过 `IOnDIConstruct` 注册为 DI 单例，而不是标 `[Injectable]`。

证据：

- `Libraries/SPTarkov.Server.Core/Helpers/Server/ModHelper.cs:10-19`：
  ```csharp
  public string GetAbsolutePathToModFolder(Assembly modAssembly) => Path.GetDirectoryName(modAssembly.Location);
  public string GetAbsolutePathToModFolder() => GetAbsolutePathToModFolder(Assembly.GetCallingAssembly());
  ```
  即返回 `user/mods/<ModName>/`（DLL 所在目录）。
- `knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md:65-76` 给出推荐模式：
  ```csharp
  public class MyModConfigRegistration : IOnDIConstruct
  {
      public static async Task OnDIConstructAsync(IServiceCollection serviceCollection)
      {
          MyModConfig config = await LoadConfigFromDiskAsync();
          serviceCollection.AddSingleton(config);
      }
  }
  ```
  并明确警告：config 类上**不要**加 `[Injectable]`。
- `knowledge/spt-kb/curated/api-notes-4.1/config-system.md` 再次确认：4.1 已移除 `ConfigServer.GetConfig<T>()`，mod 私有配置走 `IOnDIConstruct` + `AddSingleton`。
- `templates/server-mod/` **未包含**任何配置加载示例，`templates/client-mod/src/Configuration.cs` 仅展示 BepInEx `ConfigFile.Bind`。

### 实践端结论

配置位置分散，但 `config/` 子目录 + `config.jsonc` 是最常见模式；根目录 `config.json` 次之。

证据（近期 297 个目录中，过滤出 101 个真正的 mod config 文件）：

| 目录 | 出现次数 | 示例 |
|------|----------|------|
| `config/` | 20 | `Mission-Control_2653_source/config/config.jsonc` |
| `(root)` | 15 | `Cobra's-Durability-Tweaks_3019_source/config.json` |
| `Server/` | 4 | `No-Gear_2987_source/Server/config.json` |
| `Shared/Config/` | 3 | `Late-to-the-Party_814_source/Shared/Config/config.json` |
| `TheBlacklist/Config/` | 2 | `The-Blacklist-flea-market-enhancements_755_source/TheBlacklist/Config/advancedConfig.jsonc` |
| `Server/Resources/` | 2 | `RUAF-Come-Home!_2427_source/Server/Resources/config.jsonc` |
| 其他（各 1） | — | `data/config.json`、`src/config.jsonc`、`Resources/config.config.default.json` 等 |

加载 API 分布（近期 297 个目录中）：

- 91 个文件使用 `GetAbsolutePathToModFolder(...)`；
- 14 个文件使用 `IOnDIConstruct` 注册配置；
- 102 处出现 `JsonSerializer.Deserialize<...Config>` 或 `modHelper.GetJsonDataFromModFile<T>` 读取配置；
- 典型路径拼接：
  - `Path.Combine(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "config.json")`（`Ironman_2965_source/Server/Config/ConfigManager.cs:33`）
  - `Path.Combine(modFolder, "config", "config.jsonc")`（`Caliber-Split-Storage-Solutions_2990_source/Loaders/ConfigRegistration.cs:31-32`）
  - `modHelper.GetJsonDataFromModFile<FairEquipmentRestorationConfig>("Config", "config.jsonc")`（`Fair-Equipment-Restoration_2575_source/.../FairEquipmentRestorationConfigRegistration.cs:25`）

### 推荐标准

```
user/mods/<ModGuid>/
├── config/
│   ├── config.jsonc      # 玩家可改的运行时配置
│   └── defaultConfig.jsonc # 首次安装/配置损坏时的默认副本
└── <ModName>.dll
```

```csharp
public class MyModConfigRegistration : IOnDIConstruct
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static async Task OnDIConstructAsync(IServiceCollection services, CancellationToken ct)
    {
        string modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string configPath = Path.Combine(modFolder, "config", "config.jsonc");
        string defaultPath = Path.Combine(modFolder, "config", "defaultConfig.jsonc");

        if (!File.Exists(configPath) && File.Exists(defaultPath))
            File.Copy(defaultPath, configPath);

        await using var stream = File.OpenRead(configPath);
        MyModConfig config = await JsonSerializer.DeserializeAsync<MyModConfig>(stream, JsonOptions, ct)
            ?? new MyModConfig();

        services.AddSingleton(config);
    }
}
```

- **路径**：`config/config.jsonc`（相对 mod 根目录）。
- **扩展名**：优先 `.jsonc`，允许带注释；`.json` 也可接受。
- **加载方式**：`IOnDIConstruct` + `AddSingleton`；不要给配置类加 `[Injectable]`。
- **默认配置**：同时提交 `defaultConfig.jsonc`，安装脚本或首次加载时复制为 `config.jsonc`，避免直接覆盖玩家改动。
- **与 MO2 的交互**：配置放在 mod 目录内，天然成为 MO2 mod 内容的一部分；运行时写回应通过 VFS 映射到 overwrite，符合「不要直接写入真实 SPT 目录」的硬规则。
- **客户端配置**：继续使用 BepInEx `ConfigFile.Bind`，最终写入 `BepInEx/config/<GUID>.cfg`；由 `templates/client-mod/src/Configuration.cs` 示范。

---

## 缺口 3：paired mod 的仓库组织

### 实践端结论

所有 sampled hybrid mod 均为**单仓库**；目录命名高度不统一，但功能分区（Client/Server/Shared/Fika）是共识；发布形态多为**单个 zip 同时包含 `BepInEx/plugins/<Mod>` 与 `SPT_Runtime/user/mods/<Mod>`**。

证据：

- 近期 297 个目录中，hybrid（同时引用 `SPTarkov.Server` 与 `BepInEx`）共 **58** 个。
- 目录布局抽样（12 个）：
  - `Tushonka-Territories_2942_source`：`Client/` + `Server/` + `Fika/`，顶层 `TushonkaTerritories.sln`。
  - `VAI-Non-Realistic-Tapkov-Project_875_source`：`Client/` + `Server/VAI-NRTP/`。
  - `Mission-Control_2653_source`：`csharp/MissionControl/`（server）+ `csharp/MissionControl.Client/`。
  - `ReceiveAllChats_2785_source`：`ReceiveAllChats.Client/` + `ReceiveAllChats.Server/` + `ReceiveAllChats.Shared/`。
  - `SPT-Casino_2994_source`：`src/<Game>.Client/` + `src/<Game>.Server/` + `src/<Game>.Game/` + `tests/` + `tools/`，顶层 `.slnx`。
  - `WTT-Content-Backport_2512_source`：`WTT-ContentBackport/`（server）+ `WTT-ContentBackportClient/` + `WTT-ContentBackportPatcher/`。
  - `WTT-CommonLib_2310_source`：`WTT-ClientCommonLib/` + `WTT-ClientCommonLibFika/` + `WTT-ServerCommonLib/`，顶层 `WTT-CommonLib.sln`。
  - `SAIN-Solarint's-AI-Combat-System-Replacement_791_source`：`SAIN/`（client）+ `SAINServerMod/` + `SAIN.Preset.Shared/` + `SAIN.ServerInterop/`。
- **单仓库**：抽样 hybrid mod 的 `MANIFEST` URL 全部为单一 GitHub repo，未发现 client/server 分仓库的案例。
- **版本联动**：
  - `Tushonka-Territories_2942_source/Client/Client.csproj:10` 与 `Server/Server.csproj:11` 均为 `<Version>1.3.4</Version>`；
  - `SPT-Casino_2994_source/scripts/casino/pack.ps1:38` 使用单一 `$version = '1.2.61'` 同时打包客户端插件与所有服务端 assembly。
- **发布打包**：
  - `SPT-Casino_2994_source/releases/casino/SPT_CasinoV1.2.61.zip` 顶层同时包含 `BepInEx/`（67 个条目）和 `SPT_Runtime/`（22 个条目），即一个 zip 覆盖两端；
  - `SPT-Casino_2994_source/scripts/casino/pack.ps1` 详细说明：
    - 客户端插件编译到 `BepInEx/plugins/Casino/`；
    - 服务端所有 assembly（含多个 `.Server`/`.Game`）合并到 `SPT_Runtime/user/mods/Casino/`；
    - 原因：`ModLoader.LoadMods` 按**目录**加载，一个目录内可含多个 assembly，但**只能有一个 `IModMetadata`**（`SingleOrDefault` 会抛 "Duplicate mod metadata found"）。
- `external/spt-archive/wiki/Mod_Types.md:40-41` 仅说明 "Some mods include both a server and a client component"，未规定目录结构。

### 推荐标准

```
my-paired-mod/
├── MyPairedMod.sln
├── Client/
│   ├── MyPairedMod.Client.csproj   # netstandard2.1, BepInEx + Harmony
│   └── src/Plugin.cs
├── Server/
│   ├── MyPairedMod.Server.csproj   # net10.0, SPTarkov.Server.*
│   └── src/ModMetadata.cs
├── Shared/                         # 可选：两端共享的纯数据/常量
│   └── MyPairedMod.Shared.csproj   # netstandard2.0 或 netstandard2.1
├── Fika/                           # 可选：Fika 兼容层
└── README.md
```

```xml
<!-- MyPairedMod.sln 应包含 Client、Server、Shared（及可选 Fika）项目 -->
```

- **仓库**：单仓库；除非客户端与服务端由不同团队维护且生命周期完全独立，否则不建议拆仓库。
- **目录命名**：优先顶层 `Client/`、`Server/`、`Shared/`、`Fika/`；若项目较多（如 SPT-Casino），可改为 `src/<Name>.Client/`、`src/<Name>.Server/`，但仍建议按功能后缀命名。
- **版本号**：客户端 csproj、服务端 csproj、共享项目应使用同一版本号（可通过 MSBuild 属性集中定义），避免玩家看到两端版本不一致。
- **解决方案**：一个 `.sln`/`.slnx` 包含两半，方便一次性构建与 CI。
- **发布打包**：
  - 单个 zip，根目录为 `BepInEx/plugins/<ModName>/` 与 `SPT_Runtime/user/mods/<ModName>/`；
  - 服务端目录内可含多个 assembly（DLL），但**只能有一个 `IModMetadata` 实现**；
  - 若原先是多个独立 mod 合并而来，需像 SPT-Casino 一样重命名冲突的运行时文件（如 `config.json`、`escrow.json`），并在安装脚本中处理旧目录迁移。
- **与 SPT 加载机制的交互**：服务端 `ModLoader` 按 `user/mods/` 下的**目录**加载，一个目录即一个 mod；因此 paired mod 的服务端半必须共享同一个 mod 目录，不能拆成 `user/mods/MyMod-Server/` 和 `user/mods/MyMod-Client/`（后者是客户端目录）。

---

## 未找到证据的声明

- 未在 392 个源码目录或官方示例中找到任何**非空** `ModDependencies` 的真实样例；推荐写法是根据接口类型与校验器逻辑推断得出。
- wiki/curated 文档中**没有**针对 paired mod 仓库目录结构的明确规范；推荐布局是根据语料中 58 个 hybrid mod 的共同做法归纳。

---

报告生成时间：2026-09-14
报告路径：`E:\云文件\GitHub\SamMeow-modding-superpowers\.scratch\modding-standard\gap-investigation.md`
