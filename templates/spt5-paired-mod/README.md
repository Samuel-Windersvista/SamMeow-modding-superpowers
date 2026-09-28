# SPT 5.0 Paired Mod 模板

同时含**客户端半**（IL2CPP / BepInEx 6 插件，装入 `BepInEx/plugins/`）与**服务端半**（SPT 5.0 服务端 mod，装入 `SPT_Runtime/user/mods/`）的 SPT 5.0 mod 项目模板。单仓库、单解决方案、单发布 zip。

适用于需要两端协同的 mod：客户端改「表现」（UI / 输入 / 渲染 / 本地计算），服务端改「规则与数据」，两端通过路由 / 共享常量约定交互。

- 目标运行时：SPT 5.0 开发线（EFT 1.1.5 / IL2CPP / BepInEx 6）
- 目标框架：客户端 `net6.0`，服务端 `net10.0`，Shared `netstandard2.0`
- 结构母版：4.1 `templates/paired-mod/`（本模板即其 5.0 平移版；差异见「5.0 差异摘要」）
- 部件来源：`templates/spt5-client-mod/`（客户端）+ `templates/spt5-server-mod/`（服务端）

## 目录结构

```
spt5-paired-mod/
├── SPT5PairedModTemplate.sln    # 单解决方案，含 Client / Server / Shared
├── Directory.Build.props        # 集中版本号（两端同版本）+ 可覆盖的 SPT5 路径属性 + 版本常量生成
├── Client/
│   ├── Client.csproj            # net6.0，引用 BepInEx/core + BepInEx/interop（路径来自 Directory.Build.props）
│   └── src/
│       ├── Plugin.cs            # BepInEx 6 插件入口（[BepInPlugin] + Load/Unload + 逐类补丁隔离）
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
│   │   ├── Routers/
│   │   │   └── ExampleRouter.cs     # StaticRouter 示例（[Injectable(TypePriority = OnLoadOrder.Routers)]）
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
| `{{SPT_INSTALL_PATH}}` | SPT 5.0 根目录（含 `EscapeFromTarkov.exe`、`BepInEx\` 与 `SPT_Runtime\`） | `D:\Games\SPT_5xx` |
| `{{TARGET_CLASS_NAME}}` | 补丁目标游戏类型（interop 真实类型名） | `EFT.Player` |
| `{{TARGET_METHOD_NAME}}` | 补丁目标方法名 | `SomeMethod` |

替换后命名空间自动变为 `{{ROOT_NAMESPACE}}.Client` / `.Server` / `.Shared`，程序集名变为 `{{MOD_CLASS_NAME}}.Client` / `.Server` / `.Shared`。

> 5.0 服务端命名空间仍为 `SPTarkov.*`（`SPTushonka.*` 是 NuGet 包名 / 上游目录名，二者不可混用）；客户端 SPT 库若用到则为 `SPTushonka.*`。

### 2. 重命名

- 项目文件夹：`spt5-paired-mod` → `MyPairedMod`
- 解决方案文件：`SPT5PairedModTemplate.sln` → `MyPairedMod.sln`（内部用相对路径引用 `Client\Client.csproj` 等，重命名 sln 不影响）
- `Client/Client.csproj`、`Server/Server.csproj`、`Shared/Shared.csproj` 文件名无需改（程序集名由 `AssemblyName` 决定）

### 3. 确定补丁目标

5.0 客户端游戏程序集是 `BepInEx/interop/Assembly-CSharp.dll`（Il2CppInterop 代理），必须先确认目标类型与方法：

1. 用 ilspycmd 反编译 interop 程序集：`ilspycmd -l c "<SPT5Path>\BepInEx\interop\Assembly-CSharp.dll"`
2. 找到目标类与方法，记录完整命名空间
3. 把 `{{TARGET_CLASS_NAME}}` 换成完整类型名（含命名空间），如 `EFT.Player`
4. 方法名用字符串写法（`"{{TARGET_METHOD_NAME}}"`），运行时解析，签名不匹配会在启动时抛异常

> 本地离线类清单可查 `knowledge/spt-kb/archive/eft-1.1.5/classes-1.1.5.txt`。

### 4. 构建

```powershell
dotnet build -c Release -p:SPT5Path="D:\Games\SPT_5xx"
```

- `SPT5Path` 指 SPT 5.0 根目录；`Directory.Build.props` 由它派生 `SPT5Runtime`（= 根目录\`SPT_Runtime`）、`BepInExCore`（= 根目录\`BepInEx\core`）与 `Interop`（= 根目录\`BepInEx\interop`）
- 兼容别名：`-p:SPTInstallPath=...`（等价于 `SPT5Path`）、`-p:SptRoot=...`（机器根，自动派生 `\SPT_5xx`）
- 也可分别覆盖：`-p:SPT5Runtime=...` / `-p:BepInExCore=...` / `-p:Interop=...`；或在 `Directory.Build.props` 里改默认值
- 引用运行时 DLL 时用了 `<Private>false</Private>`，输出目录不会带 SPT / BepInEx 的 DLL
- csproj 内置前置护栏：`CheckGameReferences`（客户端，interop 缺失即报错）与 `CheckSpt5RuntimeReferences`（服务端，`SPT5Runtime` 配错即报错）

> `BepInEx/interop/` 由 BepInEx 在游戏**首次启动**时生成；该目录不存在时客户端无法编译（护栏会给出明确报错）。

### 5. 打包（单 zip 双端）

```powershell
powershell -ExecutionPolicy Bypass -File scripts/pack.ps1 -Spt5Path "D:\Games\SPT_5xx"
```

产物 `<ModName>-<Version>.zip` 顶层：

```
BepInEx/plugins/<ModName>/          # <Mod>.Client.dll + <Mod>.Shared.dll
SPT_Runtime/user/mods/<ModName>/    # <Mod>.Server.dll + <Mod>.Shared.dll + config/
README.md
LICENSE
```

版本号默认从 `Directory.Build.props` 的 `<Version>` 解析（唯一来源），也可 `-Version 1.2.3` 覆盖。

### 6. 部署与验证

把 zip 直接拖入 SPT 5.0 根目录，或作为 MO2 overlay 安装。启动后：

- 服务端日志：`<ModName> 服务端已加载，物品模板数量: ...，exampleMultiplier=1，共享路由 /spt/<ModName>/example`
- 客户端日志（`BepInEx/LogOutput.log`）：`<ModName> v1.0.0 已加载（协议版本 1）`
- 示例路由可用 HTTP 请求 `/spt/<ModName>/example` 验证（路径大小写敏感）

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
5. 5.0 的 `ConfigLoader` **只扫 `SPT_Data/configs`，不扫 `user/mods/`**；mod 私有配置必须自管（本模板经 `IOnDIConstruct` 注册）。

> `IServiceCollection` 定义在 `Microsoft.Extensions.DependencyInjection.Abstractions`。SPT.Server 运行在 `Microsoft.AspNetCore.App` 共享框架上，csproj 用 `<FrameworkReference Include="Microsoft.AspNetCore.App" />` 引用同一框架取得该类型（共享框架程序集不会被复制进输出目录）。

### 客户端（`Client/src/Configuration.cs`）

经 `BasePlugin.Config`（`ConfigFile`）的 `Config.Bind` 声明，运行时落在 `BepInEx/config/<ModGuid>.cfg`；不要自建 JSON 配置读取——`STD-CFG-006`。

## 打包形态与加载机制（PKG-001 / PKG-003 / PKG-004）

- 发布归档顶层按**游戏根相对路径**组织：`BepInEx/plugins/<Name>/` 与 `SPT_Runtime/user/mods/<Name>/`——`STD-PKG-001`。
- paired mod 的**服务端全部 assembly 置于同一个** `user/mods/<Name>/` 目录顶层；不得拆成多个 `user/mods/` 目录——`STD-PKG-003`。
- 5.0 `ModLoader` 扫**目录顶层 `.dll`**，一个目录内可含多个 assembly，但**只能有一个 `IModMetadata` 实现**（`SingleOrDefault` 会抛 `Duplicate mod metadata found`）。本模板中即 `<Mod>.Server.dll`；`<Mod>.Shared.dll` 只是共享常量，不实现该接口——`STD-PKG-004`。

## 5.0 差异摘要（相对 4.1 paired-mod）

| 维度 | 4.1.5（Mono / BepInEx 5） | 5.0（IL2CPP / BepInEx 6，本模板） |
|---|---|---|
| 客户端目标框架 | `netstandard2.1` | **`net6.0`**（STD-BUILD-002） |
| 客户端引用 | `EscapeFromTarkov_Data\Managed\*.dll` | **`BepInEx\core\*.dll` + `BepInEx\interop\*.dll`**（STD-BUILD-003） |
| 客户端入口 | `BaseUnityPlugin` + `Awake()` | **`BepInEx.Unity.IL2CPP.BasePlugin` + `Load()`**（STD-CLI-001） |
| 客户端日志 | `Logger` | **`Log`（`ManualLogSource`）**（STD-CLI-006） |
| 客户端撤销 | `OnDestroy()` | **`Unload()`**（STD-CLI-007） |
| 客户端补丁应用 | `Harmony.PatchAll()` 一次性 | **逐类 `PatchAll(Type)` 隔离（fail-open）** |
| 服务端目标框架 | `net10.0` | `net10.0`（同） |
| 服务端命名空间 | `SPTarkov.*` | `SPTarkov.*`（同） |
| 服务端 `SptVersion` | `~4.1.0` | **`~5.0.0`**（必须含 `5.0.0`，STD-META-004 / STD-VER-002） |
| 服务端元数据 | `IModMetadata`（同） | `IModMetadata`（同；无 `package.json`） |
| 服务端路由 | 无示例 | 增加 `Routers/ExampleRouter.cs`（`StaticRouter`） |
| 路径属性 | `SPTInstallPath` / `SPTClientPath` / `SPTServerPath` | **`SPT5Path` / `SPT5Runtime` / `BepInExCore` / `Interop`**（`SPTInstallPath` / `SptRoot` 为兼容别名） |
| 部署（不变） | `BepInEx/plugins/` · `user/mods/` | 同上（client 建议子目录 `BepInEx/plugins/<Mod>/`） |

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
| STD-BUILD-002 | `Client/Client.csproj` | 客户端 5.0 用 `net6.0` |
| STD-BUILD-003 | `Client/Client.csproj` | `<HintPath>` + `<Private>false</Private>` 引用 `BepInEx/core` 与 `BepInEx/interop` |
| STD-BUILD-004 | `Server/Server.csproj` | 引用 `SPTarkov.Server.*`，版本不高于目标运行时 |
| STD-BUILD-005 | 三个 csproj | `AppendTargetFrameworkToOutputPath=false` |
| STD-BUILD-006 | `Directory.Build.props` | 安装路径属性可覆盖（`Condition` + `-p:`） |
| STD-META-001/002 | `Server/src/ModMetadata.cs` | `IModMetadata` 唯一实现，独立文件 |
| STD-META-003/004/005 | `Server/src/ModMetadata.cs` | 反向域名 GUID、tilde `SptVersion`（含 `5.0.0`）、三段式 `Version` |
| STD-META-006 | `Client/src/Plugin.cs` | `[BepInPlugin]` 三参数齐备 |
| STD-META-007 | `Directory.Build.props` | paired 两端共用同一版本号（集中定义 + 代码常量联动） |
| STD-SRV-001/002/003 | `Server/src/ModEntry.cs` | `[Injectable]`、`OnLoadOrder.X + n` 偏移、async + `CancellationToken` |
| STD-SRV-005/006/007 | `Server/src/Routers/ExampleRouter.cs` | `StaticRouter` + `RouteAction<T>` + `OnLoadOrder.Routers` |
| STD-SRV-008 | `Server/src/Services/ExampleService.cs`、`Routers/ExampleRouter.cs` | 用注入的 `ISptLogger<T>` 记录日志 |
| STD-CLI-001/002 | `Client/src/Plugin.cs` | `BasePlugin` + `Load`（5.0）；反向域名 GUID |
| STD-CLI-003/004 | `Client/src/Patches/ExamplePatch.cs` | `[HarmonyPatch]` 标注、`Patches/` 目录、interop 真实类型名 |
| STD-CLI-005 | `Client/src/Plugin.cs` | `[BepInDependency]` 声明依赖（模板内为注释示例） |
| STD-CLI-006/007 | `Client/src/Plugin.cs` | `BasePlugin.Log`；`Load` 应用补丁、`Unload` 撤销 |
| STD-CFG-001..005 | `Server/src/Config/*.cs`、`Server/config/*.jsonc` | 配置路径、命名、注册、禁 `[Injectable]`、默认副本 |
| STD-CFG-006 | `Client/src/Configuration.cs` | `Config.Bind` 声明客户端配置 |
| STD-LOG-001 | `Server/src/Services/ExampleService.cs` | 用注入的 `ISptLogger<T>` 记录日志 |
| STD-LOG-003 | `Client/src/Plugin.cs` | 用 BepInEx 日志源记录日志 |
| STD-LOG-005 | `Server/src/Config/ModConfigRegistration.cs`、`Server/src/ModEntry.cs` | 取消不是错误，正常传播 |
| STD-PKG-001 | `scripts/pack.ps1` | 归档顶层按游戏根相对路径组织 |
| STD-PKG-003 | `scripts/pack.ps1` | 两端同包、服务端同一 mod 目录 |
| STD-PKG-004 | `Server/src/ModMetadata.cs`、`scripts/pack.ps1` | 服务端目录唯一 `IModMetadata` |
| STD-PKG-005 | `Directory.Build.props` | 两端同版本（打包脚本从单一来源取版本） |
| STD-PKG-006 | `scripts/pack.ps1` | 归档随附 README / LICENSE |
| STD-VER-002 | `Server/src/ModMetadata.cs` | 沿用 4.1 服务端骨架，仅 `SptVersion` 改 `~5.0.0` |

## 5.0 坑（客户端高频雷区）

- **可选引用参数的 Nullable 桩**：托管侧调用带 `Il2CppSystem.Nullable<T>` 默认 `null` 参数的 API 会直接抛异常；需显式构造 `new Il2CppSystem.Nullable<T>()` 透传。「类型存在 + 能编译」不等于「能调用」。
- **`TomlTypeConverter` 缺省不含 `UnityEngine.Color`**：BepInEx 6 中 `ConfigEntry<Color>` 写盘即抛 `InvalidOperationException`；需在 `Bind` 前 `TomlTypeConverter.AddConverter`。
- **`AcceptableValueList` → ComboBox 崩溃**：`GUI.DoButtonGrid` 被 IL2CPP 剥离；改用 `CustomDrawer` 单按钮循环或纯文本输入。
- **virtual 属性 interop AV**：`TrackableTransform` 等虚属性实测返回坏指针 → `AccessViolation (0xc0000005)` 进程级崩溃且不可 `try/catch`；改用非虚等价物（如 `Component.transform`）。
- **失败路径禁止 `Destroy(gameObject)`**：组件挂在 GameWorld 对象上时会摧毁游戏世界；一律 `Destroy(this)`。
- **补丁体必须 `try/catch`（fail-open）**：异常穿透补丁进入游戏代码会闪退。本模板的逐类隔离只管「应用补丁」阶段，运行期异常仍需补丁体自行兜底。
- **`TryCast<T>()` 取代 `is` / 强转 / `GetType()`**：IL2CPP 下托管 LINQ 不可用，集合与类型判断走 Il2CppInterop 口径。
- **`StringTemplateId` 优先于 `TemplateId`**：物品/战利品查询以 `StringTemplateId` 为键（直接取 `TemplateId` 的隐式转换形态会导致查询全部 miss）；`ItemPrice.CurrencyId` 在 5.0 为 `Nullable<MongoID>`（用 `HasValue`/`Value`）。
- **`ClassInjector.RegisterTypeInIl2Cpp<T>()`**：注入的 MonoBehaviour 类型必须先注册，才能挂到 `GameObject` 上。
- **CS0012 引用链**：`EFT.Player`→`DissonanceVoip.dll`；`CameraManager.SSAA`→`Unity.Postprocessing.Runtime.dll`；EFT UI 基链→`Sirenix.Serialization`；TMP→`Unity.TextMeshPro.dll`。编译报 `CS0012` 时按提示补对应 interop 引用。
- **BepInEx 6 配置版本化迁移**：持久化配置不跟随默认值变更 → 用 `[Meta] CfgVer` + 逐键条件改写；`ConfigFile` 按需加载（不能用 `Keys` 枚举旧文件键）。

## 坑（服务端）

- 5.0 `IModMetadata` 是接口，不是 4.0 的抽象 record，**不要写 `override`**
- 版本必须是 semver 三段式（`1.0.0`），`1.0.0.0` 非法
- `SptVersion` 不含 `5.0.0` 会被 `ModValidator` 拒载；`ModGuid` 为空 / 重复 / 不合正则也会失败
- 自己的服务类用 `[Injectable]`；配置类**不要**加 `[Injectable]`，只能经 `IOnDIConstruct` + `AddSingleton` 注册
- 引用 `IServiceCollection` 需要 `Microsoft.AspNetCore.App` 框架引用（csproj 已配 `FrameworkReference`）
- DLL 必须放 mod 目录**顶层**（5.0 加载器不递归子目录）
- 需 Mod Web Pages（浏览器配置界面）时：服务端 SDK 换 `Microsoft.NET.Sdk.Web`，实现 `IModBlazorMetadata`，另引用 `SPTarkov.Server.Web.dll`

## 深入参考

- `templates/spt5-client-mod/`、`templates/spt5-server-mod/`（部件模板）
- `knowledge/spt-kb/curated/api-notes-5.0/`（mod-loading / di-container / http-routing / config-system）
- `knowledge/spt-kb/curated/operations/5xx-client-mod-dev-lessons.md`
- `knowledge/spt-kb/curated/modding-standard/`（规则集：01-structure / 02-metadata / 03-build / 04-server / 05-client / 06-config / 09-packaging）
- `knowledge/spt-kb/curated/modding-standard/evidence-index.md` 的 `EV-GAP-PAIRED`（paired 布局与打包证据）
- `docs/research/spt-5.0-mod-template-design.md` §G.3（paired 蓝图）
