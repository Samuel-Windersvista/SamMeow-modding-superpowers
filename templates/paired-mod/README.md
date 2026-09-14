# SPT 4.1 Paired Mod 模板

同时含**客户端半**（BepInEx 插件，装入 `BepInEx/plugins/`）与**服务端半**（SPT 服务端 mod，装入 `SPT_Runtime/user/mods/`）的 SPT 4.1 mod 项目模板。单仓库、单解决方案、单发布 zip。

适用于需要两端协同的 mod：客户端改「表现」（UI / 输入 / 渲染 / 本地计算），服务端改「规则与数据」，两端通过路由 / 共享常量约定交互。

## 目录结构

```
paired-mod/
├── PairedModTemplate.sln        # 单解决方案，含 Client / Server / Shared
├── Directory.Build.props        # 集中版本号（两端同版本）+ 可覆盖的 SPT 路径属性 + 版本常量生成
├── Client/
│   ├── Client.csproj            # netstandard2.1，引用 BepInEx / 0Harmony / Assembly-CSharp / UnityEngine
│   └── src/
│       ├── Plugin.cs            # BepInEx 插件入口（[BepInPlugin] + Awake/OnDestroy）
│       ├── Configuration.cs     # BepInEx 配置封装（Config.Bind）
│       └── Patches/
│           └── ExamplePatch.cs  # Harmony 补丁示例（Prefix / Postfix）
├── Server/
│   ├── Server.csproj            # net10.0，引用 SPTarkov.Server.Core / DI / Common / SemanticVersioning
│   ├── src/
│   │   ├── ModMetadata.cs       # IModMetadata 实现（唯一，PKG-004）
│   │   ├── ModEntry.cs          # 入口类（[Injectable] + IOnLoad）
│   │   ├── Config/
│   │   │   ├── ModConfig.cs                 # 配置 POCO（禁止 [Injectable]）
│   │   │   └── ModConfigRegistration.cs     # IOnDIConstruct + AddSingleton 加载
│   │   └── Services/
│   │       └── ExampleService.cs    # 表注入示例（GlobalTable / TemplateTable）
│   └── config/
│       ├── config.jsonc         # 玩家可改的运行时配置（首启从 defaultConfig 复制）
│       └── defaultConfig.jsonc  # 默认副本
├── Shared/
│   ├── Shared.csproj            # netstandard2.0，纯数据 / 常量，两端共同引用
│   └── src/
│       └── SharedConstants.cs   # 共享常量示例（路由路径 / 协议版本）
├── scripts/
│   └── pack.ps1                 # 构建 + 打包：单 zip 同时含两端
├── README.md
├── LICENSE
└── .gitignore
```

> `Fika/` 兼容层为可选扩展（EV-GAP-PAIRED），本模板未包含；如需要，新增顶层 `Fika/` 工程并加入 `.sln`。

## 使用步骤

### 1. 替换占位符

所有占位符采用 `{{PLACEHOLDER}}` 格式，全局搜索替换即可：

| 占位符 | 说明 | 示例值 |
|---|---|---|
| `{{ROOT_NAMESPACE}}` | 根命名空间（三端共用前缀） | `SamMeow.MyPairedMod` |
| `{{MOD_CLASS_NAME}}` | 类名前缀（PascalCase，同时是程序集名前缀与 mod 目录名） | `MyPairedMod` |
| `{{MOD_NAME}}` | 人类可读的 mod 名 | `My Paired Mod` |
| `{{MOD_GUID}}` | 全局唯一 ID（反向域名；两端共用同一个） | `com.sammeow.mypairedmod` |
| `{{MOD_AUTHOR}}` | 作者名 | `SamMeow` |
| `{{MOD_VERSION}}` | 版本（semver 三段式；只出现在 `Directory.Build.props`） | `1.0.0` |
| `{{MOD_LICENSE}}` | 许可证名 | `MIT` |
| `{{SPT_INSTALL_PATH}}` | SPT 根目录（含 `EscapeFromTarkov.exe` 与 `SPT_Runtime\`） | `D:\SPT` |
| `{{TARGET_CLASS_NAME}}` | 补丁目标游戏类型（4.1 反混淆真名） | `EFT.Player` |
| `{{TARGET_METHOD_NAME}}` | 补丁目标方法名 | `SomeMethod` |

替换后命名空间自动变为 `{{ROOT_NAMESPACE}}.Client` / `.Server` / `.Shared`，程序集名变为 `{{MOD_CLASS_NAME}}.Client` / `.Server` / `.Shared`。

### 2. 重命名

- 项目文件夹：`paired-mod` → `MyPairedMod`
- 解决方案文件：`PairedModTemplate.sln` → `MyPairedMod.sln`（内部用相对路径引用 `Client\Client.csproj` 等，重命名 sln 不影响）
- `Client/Client.csproj`、`Server/Server.csproj`、`Shared/Shared.csproj` 文件名无需改（程序集名由 `AssemblyName` 决定）

### 3. 确定补丁目标

4.1 客户端已反混淆（类型有真名真命名空间），但必须先确认目标方法与签名：

1. 用 dnSpy / ILSpy 打开 `EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`
2. 找到目标类与方法，记录完整命名空间
3. 把 `{{TARGET_CLASS_NAME}}` 换成完整类型名（含命名空间），如 `EFT.Player`
4. 方法名用字符串写法（`"{{TARGET_METHOD_NAME}}"`），运行时解析，签名不匹配会在启动时抛异常

### 4. 构建

```powershell
dotnet build -c Release -p:SPTInstallPath="D:\SPT"
```

- `SPTInstallPath` 指 SPT 根目录；`Directory.Build.props` 由它派生 `SPTClientPath`（= 根目录）与 `SPTServerPath`（= 根目录\`SPT_Runtime`）
- 也可分别覆盖：`-p:SPTClientPath=...` / `-p:SPTServerPath=...`；也可在 `Directory.Build.props` 里改死默认值
- 引用运行时 DLL 时用了 `<Private>false</Private>`，输出目录不会带 SPT / BepInEx 的 DLL

### 5. 打包（单 zip 双端）

```powershell
pwsh -File scripts/pack.ps1 -SptInstallPath "D:\SPT"
```

产物 `<ModName>-<Version>.zip` 顶层：

```
BepInEx/plugins/<ModName>/          # Client.dll + Shared.dll
SPT_Runtime/user/mods/<ModName>/    # Server.dll + Shared.dll + config/
README.md
LICENSE
```

版本号默认从 `Directory.Build.props` 的 `<Version>` 解析（唯一来源），也可 `-Version 1.2.3` 覆盖。

### 6. 部署与验证

把 zip 直接拖入 SPT 根目录，或作为 MO2 overlay 安装。启动后：

- 服务端日志：`<ModName> 服务端已加载，物品模板数量: ...，共享路由 /spt/<ModName>/ping`
- 客户端日志（`BepInEx/LogOutput.log`）：`<ModName> v1.0.0 已加载（协议版本 1）`

客户端配置生成在 `BepInEx/config/<ModGuid>.cfg`；服务端配置生成在 `SPT_Runtime/user/mods/<ModName>/config/config.jsonc`。

## 版本号联动（META-007 / PKG-005）

版本号**只定义一次**，在 `Directory.Build.props`：

```xml
<PropertyGroup>
  <Version>{{MOD_VERSION}}</Version>
</PropertyGroup>
```

- 三个工程（Client / Server / Shared）继承该 `<Version>`，各自的 csproj **不写** `<Version>`
- `Directory.Build.props` 里的 MSBuild target 把 `$(Version)` 生成为编译期常量 `ModVersion.Value`，供客户端 `[BepInPlugin]` 与服务端 `IModMetadata.Version` 引用
- 因此代码里也没有第二处版本字符串；改版本只改 `Directory.Build.props` 一行

## 配置

### 服务端（`Server/config/`）

采用 **`config/config.jsonc` + `config/defaultConfig.jsonc`** 约定：

1. `ModConfig.cs` 是纯 POCO，**禁止**加 `[Injectable]`（否则容器用默认值新建实例，磁盘 JSON 永远不会被读）——`STD-CFG-004`。
2. `ModConfigRegistration.cs` 实现 `IOnDIConstruct`：在 DI 容器构建前读盘，`serviceCollection.AddSingleton(config)` 注册为单例——`STD-CFG-003`。
3. 首启（或 `config.jsonc` 被删）时从 `defaultConfig.jsonc` 复制；**绝不覆盖**玩家已有改动——`STD-CFG-005`。
4. 反序列化容忍 `.jsonc` 的注释与尾随逗号——`STD-CFG-002`。
5. 配置随 mod 部署在 `user/mods/<ModName>/config/` 内，用相对 mod 根目录的路径读取——`STD-CFG-001`。

> `IServiceCollection` 定义在 `Microsoft.Extensions.DependencyInjection.Abstractions`。SPT.Server 运行在 `Microsoft.AspNetCore.App` 共享框架上，csproj 用 `<FrameworkReference Include="Microsoft.AspNetCore.App" />` 引用同一框架取得该类型（共享框架程序集不会被复制进输出目录）。

### 客户端（`Client/src/Configuration.cs`）

经 `BaseUnityPlugin.Config`（`ConfigFile`）的 `Config.Bind` 声明，运行时落在 `BepInEx/config/<ModGuid>.cfg`；不要自建 JSON 配置读取——`STD-CFG-006`。

## 打包形态与加载机制（PKG-001 / PKG-003 / PKG-004）

- 发布归档顶层按**游戏根相对路径**组织：`BepInEx/plugins/<Name>/` 与 `SPT_Runtime/user/mods/<Name>/`——`STD-PKG-001`。
- paired mod 的**服务端全部 assembly 置于同一个** `user/mods/<Name>/` 目录；不得拆成多个 `user/mods/` 目录——`STD-PKG-003`。
- `ModLoader.LoadMods` 按**目录**加载，一个目录内可含多个 assembly，但**只能有一个 `IModMetadata` 实现**（`SingleOrDefault` 会抛 `Duplicate mod metadata found`）。本模板中即 `Server.dll`；`Shared.dll` 只是共享常量，不实现该接口——`STD-PKG-004`。

## 规则对照（STD）

模板内关键位置已用注释标注对应 Rule ID（`STD-XXX-nnn`，可检索）：

| Rule ID | 位置 | 要求 |
|---|---|---|
| STD-STRUCT-001 | `.gitignore` | 排除 `bin/`、`obj/` 与 IDE/用户文件 |
| STD-STRUCT-003 | `Client/src/`、`Server/src/` | 源码放 `src/` 或功能子目录，不平铺根目录 |
| STD-STRUCT-004 | `Client/`、`Server/`、`Shared/` | paired mod 单仓库，按 `Client/`、`Server/` 分层，共享纯数据放 `Shared/` |
| STD-STRUCT-005 | `README.md` | 仓库根 README 说明用途、安装与配置 |
| STD-STRUCT-006 | `LICENSE` | 仓库根提供授权文件 |
| STD-BUILD-001 | `Server/Server.csproj` | 服务端目标框架 `net10.0` |
| STD-BUILD-002 | `Client/Client.csproj` | 客户端 4.1.5 用 `netstandard2.1`（5.0 用 `net6.0`，本模板未实现） |
| STD-BUILD-003 | `Client/Client.csproj` | `<HintPath>` + `<Private>false</Private>` 引用运行时程序集 |
| STD-BUILD-004 | `Server/Server.csproj` | 引用 `SPTarkov.Server.*`，版本不高于目标运行时 |
| STD-BUILD-005 | 三个 csproj | `AppendTargetFrameworkToOutputPath=false` |
| STD-BUILD-006 | `Directory.Build.props` | 安装路径属性可覆盖（`Condition` + `-p:`） |
| STD-META-001/002 | `Server/src/ModMetadata.cs` | `IModMetadata` 唯一实现，独立文件 |
| STD-META-003/004/005 | `Server/src/ModMetadata.cs` | 反向域名 GUID、tilde `SptVersion`、三段式 `Version` |
| STD-META-006 | `Client/src/Plugin.cs` | `[BepInPlugin]` 三参数齐备 |
| STD-META-007 | `Directory.Build.props` | paired 两端共用同一版本号（集中定义 + 代码常量联动） |
| STD-SRV-001/002/003 | `Server/src/ModEntry.cs` | `[Injectable]`、`OnLoadOrder.X + n` 偏移、async + `CancellationToken` |
| STD-SRV-008 | `Server/src/Services/ExampleService.cs` | 用注入的 `ISptLogger<T>` 记录日志 |
| STD-CLI-001/002 | `Client/src/Plugin.cs` | `BaseUnityPlugin` + `Awake`（4.1.5）；反向域名 GUID |
| STD-CLI-003/004 | `Client/src/Patches/ExamplePatch.cs` | `[HarmonyPatch]` 标注、`Patches/` 目录、真实类型名 |
| STD-CLI-005 | `Client/src/Plugin.cs` | `[BepInDependency]` 声明依赖（模板内为注释示例） |
| STD-CLI-006/007 | `Client/src/Plugin.cs` | BepInEx 日志源；`Awake` 应用补丁、`OnDestroy` 撤销 |
| STD-CFG-001..005 | `Server/src/Config/*.cs`、`Server/config/*.jsonc` | 配置路径、命名、注册、禁 `[Injectable]`、默认副本 |
| STD-CFG-006 | `Client/src/Configuration.cs` | `Config.Bind` 声明客户端配置 |
| STD-LOG-001 | `Server/src/Services/ExampleService.cs` | 用注入的 `ISptLogger<T>` 记录日志 |
| STD-LOG-003 | `Client/src/Plugin.cs` | 用 `BaseUnityPlugin.Logger` 记录日志 |
| STD-LOG-005 | `Server/src/Config/ModConfigRegistration.cs`、`Server/src/ModEntry.cs` | 取消不是错误，正常传播 |
| STD-PKG-001 | `scripts/pack.ps1` | 归档顶层按游戏根相对路径组织 |
| STD-PKG-003 | `scripts/pack.ps1` | 两端同包、服务端同一 mod 目录 |
| STD-PKG-004 | `Server/src/ModMetadata.cs`、`scripts/pack.ps1` | 服务端目录唯一 `IModMetadata` |
| STD-PKG-005 | `Directory.Build.props` | 两端同版本（打包脚本从单一来源取版本） |
| STD-PKG-006 | `scripts/pack.ps1` | 归档随附 README / LICENSE |

## 关键点（4.1）

- **客户端目标框架是 `netstandard2.1`，服务端是 `net10.0`**：客户端在 Unity 的 Mono 运行时下加载，net10.0 程序集无法被 Mono 加载。
- **5.0 形态（IL2CPP / BepInEx 6）不在本模板内实现**：需把客户端目标框架换成 `net6.0`、入口基类换成 `BepInEx.Unity.IL2CPP.BasePlugin`、入口方法换成 `Load()`（撤销路径 `Dispose()`）、日志属性换成 `Log`，并把引用路径改到 `BepInEx/interop/` 与 `BepInEx/core/`——参见 `STD-BUILD-002`、`STD-BUILD-003`、`STD-CLI-001`、`STD-CLI-006`、`STD-CLI-007`。
- **客户端改「表现」，服务端改「规则与数据」**；跨端同步的枚举数值、路由路径等必须一致——放 `Shared/` 避免漂移。
- 不要为 enum 扩展写自研 prepatcher DLL：4.1 由服务端 mod 经 `ClientEnumDefinitions` 注册，客户端内建 prepatcher 拉取。
- `BepInEx/plugins/spt/` 与 `BepInEx/patchers/spt-prepatch.dll` 是 SPT 官方文件，卸载 mod 时不要删。

## 坑

- 4.1 中 `IModMetadata` 是接口，不是 4.0 的抽象 record，**不要写 `override`**
- 版本必须是 semver 三段式（`1.0.0`），`1.0.0.0` 非法
- 自己的服务类用 `[Injectable]`；配置类**不要**加 `[Injectable]`，只能经 `IOnDIConstruct` + `AddSingleton` 注册
- 服务端 mod 目录内多个 assembly 时，务必只有元数据 assembly 实现 `IModMetadata`（`Shared.dll` 等不得实现）
- 引用 `IServiceCollection` 需要 `Microsoft.AspNetCore.App` 框架引用（csproj 已配 `FrameworkReference`）
- 补丁目标方法签名与 `{{TARGET_METHOD_NAME}}` 不匹配 → 启动即炸（先 dnSpy 确认）
- 4.0 编译的客户端 mod 在 4.1 上无法加载，必须用 4.1 程序集重新编译
- 需 Mod Web Pages（浏览器配置界面）时：服务端 SDK 换 `Microsoft.NET.Sdk.Web`，实现 `IModBlazorMetadata`，另引用 `SPTarkov.Server.Web.dll`

## 深入参考

- `knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md`
- `knowledge/spt-kb/curated/modding-guide/03-client-mod-anatomy.md`
- `knowledge/spt-kb/curated/modding-standard/`（规则集：01-structure / 02-metadata / 03-build / 04-server / 05-client / 06-config / 09-packaging）
- `knowledge/spt-kb/curated/modding-standard/evidence-index.md` 的 `EV-GAP-PAIRED`（paired 布局与打包证据）
