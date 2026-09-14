# SPT 4.1 服务端 Mod 模板

最小可用的 SPT 4.1 服务端 mod 项目模板。用于脚手架新的服务端 mod（.NET 类库，放入 `SPT_Runtime/user/mods/<ModName>/`）。

## 目录结构

```
server-mod/
├── ServerModTemplate.csproj     # 项目文件（HintPath 引用 SPT 4.1 DLL）
├── src/
│   ├── ModMetadata.cs           # IModMetadata 实现（mod 身份与版本声明）
│   ├── ModEntry.cs              # 入口类（[Injectable] + IOnLoad，示范注入 config）
│   ├── Config/
│   │   ├── ModConfig.cs                 # 配置 POCO（禁止 [Injectable]）
│   │   └── ModConfigRegistration.cs     # IOnDIConstruct + AddSingleton 加载
│   └── Services/
│       └── ExampleService.cs    # 表注入示例（GlobalTable / TemplateTable）
├── config/
│   ├── config.jsonc             # 玩家可改的运行时配置（首启从 defaultConfig 复制）
│   └── defaultConfig.jsonc      # 默认副本
├── README.md
├── LICENSE
└── .gitignore
```

## 使用步骤

### 1. 替换占位符

所有占位符采用 `{{PLACEHOLDER}}` 格式，全局搜索替换即可：

| 占位符 | 说明 | 示例值 |
|---|---|---|
| `{{ROOT_NAMESPACE}}` | 根命名空间 | `SamMeow.MyMod` |
| `{{MOD_CLASS_NAME}}` | 类名前缀（PascalCase，同时是程序集名） | `MyMod` |
| `{{MOD_NAME}}` | 人类可读的 mod 名 | `My Mod` |
| `{{MOD_GUID}}` | 全局唯一 ID（反向域名） | `com.sammeow.mymod` |
| `{{MOD_AUTHOR}}` | 作者名 | `SamMeow` |
| `{{MOD_VERSION}}` | mod 版本（semver 三段式） | `1.0.0` |
| `{{MOD_LICENSE}}` | 许可证名 | `MIT` |
| `{{SPT_INSTALL_PATH}}` | SPT 4.1 服务器根目录（含 `SPT.Server.exe`） | `D:\SPT\Server` |

### 2. 重命名

- 项目文件夹：`server-mod` → `MyMod`
- `ServerModTemplate.csproj` → `MyMod.csproj`
- `src/ModEntry.cs` 内的类名 `{{MOD_CLASS_NAME}}Entry` 已随替换自动更名，无需手动改

### 3. 构建

```powershell
dotnet build -c Release -p:SPTInstallPath="D:\SPT\Server"
```

- `SPTInstallPath` 也可以在 csproj 里直接改死（替换 `{{SPT_INSTALL_PATH}}` 后无需再传参）
- 引用 SPT DLL 时用了 `<Private>false</Private>`，输出目录里不会带 SPT 的 DLL

### 4. 部署

把构建产物复制到服务器 mod 目录：

```
D:\SPT\Server\user\mods\MyMod\MyMod.dll
```

> 注意：目录名（`MyMod`）就是服务器显示/识别用的 mod 文件夹名，与 `ModGuid` 不必一致，但建议可读。

把 `config/` 目录一并复制过去：

```
D:\SPT\Server\user\mods\MyMod\
├── MyMod.dll
└── config\
    ├── config.jsonc
    └── defaultConfig.jsonc
```

### 5. 验证

启动服务器，日志中应看到：

```
[SUCCESS] My Mod 已加载，物品模板数量: ...，exampleMultiplier=1
```

若启动失败（metadata 未映射配置、注入类型无法解析），服务器会在启动阶段直接报错——见知识库 `api-notes-4.1/mod-loading.md` 失败模式。

## 配置（config/config.jsonc）

玩家可改的服务端配置采用 **`config/config.jsonc` + `config/defaultConfig.jsonc`** 约定：

1. `ModConfig.cs` 是纯 POCO，**禁止**加 `[Injectable]`（否则容器用默认值新建实例，磁盘 JSON 永远不会被读）——`STD-CFG-004`。
2. `ModConfigRegistration.cs` 实现 `IOnDIConstruct`：在 DI 容器构建前读盘，`serviceCollection.AddSingleton(config)` 注册为单例——`STD-CFG-003`。
3. 首启（或 `config.jsonc` 被删）时从 `defaultConfig.jsonc` 复制；**绝不覆盖**玩家已有改动——`STD-CFG-005`。
4. 反序列化容忍 `.jsonc` 的注释与尾随逗号——`STD-CFG-002`。
5. 消费方（服务 / 路由 / 入口）构造函数直接注入 `MyModConfig`——`ModEntry.cs` 已示范。
6. 配置随 mod 部署在 `user/mods/<ModName>/config/` 内，用相对 mod 根目录的路径读取（`STD-CFG-001`）；静态注册方法用程序集位置取 mod 根，运行期消费方可用 `ModHelper.GetAbsolutePathToModFolder(Assembly)`。

> `IServiceCollection` 定义在 `Microsoft.Extensions.DependencyInjection.Abstractions`。SPT.Server 运行在 `Microsoft.AspNetCore.App` 共享框架上，csproj 用 `<FrameworkReference Include="Microsoft.AspNetCore.App" />` 引用同一框架取得该类型（共享框架程序集不会被复制进输出目录）。

## 规则对照（STD）

模板内关键位置已用注释标注对应 Rule ID（`STD-XXX-nnn`，可检索）：

| Rule ID | 位置 | 要求 |
|---|---|---|
| STD-STRUCT-001 | `.gitignore` | 排除 `bin/`、`obj/` 与 IDE/用户文件 |
| STD-STRUCT-003 | `src/` | 源码放 `src/` 或功能子目录，不平铺根目录 |
| STD-STRUCT-005 | `README.md` | 仓库根 README 说明用途、安装与配置 |
| STD-STRUCT-006 | `LICENSE` | 仓库根提供授权文件 |
| STD-BUILD-001 | csproj | 目标框架 `net10.0` |
| STD-BUILD-004 | csproj | 引用 `SPTarkov.Server.*`，版本不高于目标运行时 |
| STD-BUILD-005 | csproj | `AppendTargetFrameworkToOutputPath=false` |
| STD-BUILD-006 | csproj | 安装路径属性可覆盖（`Condition` + `-p:`） |
| STD-META-001/002 | `src/ModMetadata.cs` | `IModMetadata` 唯一实现，独立文件 |
| STD-META-003/004/005 | `src/ModMetadata.cs` | 反向域名 GUID、tilde `SptVersion`、三段式 `Version` |
| STD-SRV-001/002/003 | `src/ModEntry.cs`、`src/Services/ExampleService.cs` | `[Injectable]`、`OnLoadOrder.X + n` 偏移、async + `CancellationToken` |
| STD-SRV-008 | `src/Services/ExampleService.cs` | 用注入的 `ISptLogger<T>` 记录日志 |
| STD-CFG-001..005 | `src/Config/*.cs`、`config/*.jsonc` | 配置路径、命名、注册、禁 `[Injectable]`、默认副本 |
| STD-LOG-001 | `src/Services/ExampleService.cs` | 用注入的 `ISptLogger<T>` 记录日志 |
| STD-LOG-005 | `src/Config/ModConfigRegistration.cs`、`src/ModEntry.cs` | 取消不是错误，正常传播 |

## 关键 API 速查（4.1）

| 要做什么 | 写法 |
|---|---|
| 声明加载阶段 | `[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]` |
| 生命周期 | `IOnLoad.OnLoadAsync(CancellationToken)` / `IOnUpdate` |
| 注入数据库表 | 构造函数直接声明 `GlobalTable` / `TemplateTable` / `TradersTable` 等（位于 `SPTarkov.Server.Core.Models.Spt.Tables`） |
| 注入 SPT 内置配置 | 直接声明具体类型（如 `InsuranceConfig`），配置类**不要**加 `[Injectable]` |
| 注入 mod 私有配置 | 在 `IOnDIConstruct.OnDIConstructAsync` 读盘后 `AddSingleton`（`src/Config/ModConfigRegistration.cs`），消费方构造函数注入 `MyModConfig` |
| 自定义路由 | `AbstractRouter`，优先级基于 `OnLoadOrder.Routers` 偏移 |
| 日志 | `ISptLogger<T>`（`SPTarkov.Common.Models.Logging`）：`logger.Success(...)` / `logger.Info(...)` |

完整参考：`knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md` 与 `api-notes-4.1/di-container.md`。

## 坑

- 4.1 中 `IModMetadata` 是接口，不是 4.0 的抽象 record，**不要写 `override`**
- 版本必须是 semver 三段式（`1.0.0`），`1.0.0.0` 非法
- 自己的服务类用 `[Injectable]`；配置类**不要**加 `[Injectable]`，只能经 `IOnDIConstruct` + `AddSingleton` 注册
- 引用 `IServiceCollection` 需要 `Microsoft.AspNetCore.App` 框架引用（csproj 已配 `FrameworkReference`）
- 需 Mod Web Pages（浏览器配置界面）时：SDK 换 `Microsoft.NET.Sdk.Web`，实现 `IModBlazorMetadata`，另引用 `SPTarkov.Server.Web.dll`
- 发布归档按 `SPT_Runtime/user/mods/<Name>/` 组织，并把 `README.md`、`LICENSE` 一并随包（`STD-PKG-001`、`STD-PKG-006`）
