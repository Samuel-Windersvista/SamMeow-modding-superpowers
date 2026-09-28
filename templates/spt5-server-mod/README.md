# SPT 5.0 服务端 Mod 模板

最小可用的 SPT 5.0 服务端 mod 项目模板。用于脚手架新的服务端 mod（`.NET 10` 类库，放入 `SPT_Runtime/user/mods/<ModName>/`）。

- 目标版本：**SPT 5.0**（IL2CPP / BepInEx 6 客户端线；服务端仍为 `net10.0`）
- 结构母版：4.1 `templates/server-mod/`（服务端骨架 4.1 → 5.0 机制一致，见规则 `STD-VER-002`）
- 真实参照：`tools/tarkov-active-probe/`（运行中的 5.0 服务端 mod）、官方测试件 `Testing/TestMod` / `TestMod2`

## 目录结构

```
spt5-server-mod/
├── ServerModTemplate.csproj     # 项目文件（HintPath 引用 SPT 5.0 运行时 DLL）
├── src/
│   ├── ModMetadata.cs           # IModMetadata 实现（mod 身份与版本声明）
│   ├── ModEntry.cs              # 入口类（[Injectable] + IOnLoad，示范注入 config）
│   ├── Config/
│   │   ├── ModConfig.cs                 # 配置 POCO（禁止 [Injectable]）
│   │   └── ModConfigRegistration.cs     # IOnDIConstruct + AddSingleton 加载
│   ├── Services/
│   │   └── ExampleService.cs    # 表注入示例（GlobalTable / TemplateTable）
│   └── Routers/
│       └── ExampleRouter.cs     # StaticRouter 示例（[Injectable(TypePriority = OnLoadOrder.Routers)]）
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
| `{{SPT5_RUNTIME_PATH}}` | SPT 5.0 运行时目录（`SPT_Runtime`，含 `SPTarkov.Server.Core.dll`） | `E:\Game\EFT_Offline\SPT_5xx\SPT_Runtime` |

> `{{SPT5_RUNTIME_PATH}}` 也可不替换——构建时用 `-p:SPT5Runtime="..."` 覆盖（属性带 `Condition`，命令行优先）。

### 2. 重命名

- 项目文件夹：`spt5-server-mod` → `MyMod`
- `ServerModTemplate.csproj` → `MyMod.csproj`
- 其余类名（`{{MOD_CLASS_NAME}}Entry` / `{{MOD_CLASS_NAME}}Metadata` / `{{MOD_CLASS_NAME}}Router` 等）随占位符替换自动更名，无需手动改

### 3. 构建

```powershell
dotnet build -c Release -p:SPT5Runtime="D:\SPT\SPT_5xx\SPT_Runtime"
```

- 引用 SPT DLL 时用了 `<Private>false</Private>`，输出目录里不会带 SPT 的 DLL
- csproj 内置 `CheckSpt5RuntimeReferences` 护栏：`SPT5Runtime` 配错时构建期即报错，不会产出静默缺引用的 DLL

### 4. 部署

把构建产物复制到服务器 mod 目录。**5.0 加载器只扫每个 mod 目录的顶层 `.dll`**，DLL 必须放顶层：

```
D:\SPT\SPT_5xx\SPT_Runtime\user\mods\MyMod\
├── MyMod.dll
└── config\
    ├── config.jsonc
    └── defaultConfig.jsonc
```

> 目录名（`MyMod`）就是服务器该 mod 的文件夹名，与 `ModGuid` 不必一致，但建议可读。

### 5. 验证

启动服务器，日志中应看到：

```
[SUCCESS] My Mod 已加载，物品模板数量: ...，exampleMultiplier=1
```

示例路由 `MyModRouter` 注册后可用 HTTP 请求 `/spt/<MyMod>/example` 验证（路径大小写敏感；`/spt/...` 为 mod 自定义路由的独立前缀约定）。

若启动失败（metadata 未映射配置、注入类型无法解析、`SptVersion` 不含 `5.0.0`），服务器会在启动阶段直接报错——见 `knowledge/spt-kb/curated/api-notes-5.0/mod-loading.md` 失败模式。

## 5.0 关键点

1. **目标框架 `net10.0`**：与服务端程序集一致（`STD-BUILD-001`）。客户端才是 `net6.0`，别混。
2. **namespace 用 `SPTarkov.*`**：程序集名/命名空间是 `SPTarkov.*`；`SPTushonka.*` 是 NuGet 包名与上游源码目录名，二者不可混用（包 `SPTushonka.DI` → 程序集/命名空间 `SPTarkov.DI`）。
3. **无 `package.json`**：mod 身份/版本/依赖全部由 DLL 内的 `IModMetadata` 承担；加载器只扫 `user/mods/<Mod>/` 顶层的 `.dll`，找不到 DLL 即抛 `ModLoaderException`。
4. **`SptVersion` 必须含 `5.0.0`**：本模板用 `new("~5.0.0")`（= `>=5.0.0 <5.1.0`）。老 mod 的 `~4.1.0` 区间在 5.0 上 `Satisfies` 失败会被拒载（`STD-META-004` / `STD-VER-002`）。
5. **DI / 生命周期**（与 4.1 相同）：

   | 要做什么 | 写法 |
   |---|---|
   | 声明可注入类 | `[Injectable]` / `[Injectable(InjectionType.Singleton)]` |
   | 声明加载阶段 | `[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]`（永远基于 `OnLoadOrder.X` 写偏移） |
   | 生命周期 | `IOnLoad.OnLoadAsync(CancellationToken)` / `IOnUpdate.OnUpdateAsync(...)` |
   | 容器构建前注册 | `IOnDIConstruct.OnDIConstructAsync(IServiceCollection, CancellationToken)`（静态） |
   | 日志 | `ISptLogger<T>`（`SPTarkov.Common.Models.Logging`）：`logger.Success(...)` / `logger.Info(...)` |

   `OnLoadOrder` 阶段常量（升序）：`Watermark=0` → `Preload=100000` → `GameCallbacks=200000` → `TraderRegistration=300000` → `Routers=400000` → ... → `PostLoad=1000000`。

6. **路由注册形态**：继承 `StaticRouter`（精确匹配）或 `DynamicRouter`（包含匹配），标 `[Injectable(TypePriority = OnLoadOrder.Routers)]`，由 DI 自动收集。构造签名：
   `StaticRouter(JsonUtil jsonUtil, List<RouteAction> routes)`，每条路由用 `new RouteAction<EmptyRequestData>(绝对路径, (url, info, sessionId, output, ct) => ...)`。路由路径是**完整绝对路径**；模块自定义路由建议独立前缀。详见 `src/Routers/ExampleRouter.cs` 与 `api-notes-5.0/http-routing.md`。

## 配置（config/config.jsonc）

玩家可改的服务端配置采用 **`config/config.jsonc` + `config/defaultConfig.jsonc`** 约定：

1. `ModConfig.cs` 是纯 POCO，**禁止**加 `[Injectable]`（否则容器用默认值新建实例，磁盘 JSON 永远不会被读）——`STD-CFG-004`。
2. `ModConfigRegistration.cs` 实现 `IOnDIConstruct`：在 DI 容器构建前读盘，`serviceCollection.AddSingleton(config)` 注册为单例——`STD-CFG-003`。
3. 首启（或 `config.jsonc` 被删）时从 `defaultConfig.jsonc` 复制；**绝不覆盖**玩家已有改动——`STD-CFG-005`。
4. 反序列化容忍 `.jsonc` 的注释与尾随逗号——`STD-CFG-002`。
5. 消费方（服务 / 路由 / 入口）构造函数直接注入 `MyModConfig`——`ModEntry.cs` 已示范。
6. 注意：5.0 的 `ConfigLoader` **只扫 `SPT_Data/configs`，不扫 `user/mods/`**；mod 私有配置必须自管（本模板经 `IOnDIConstruct` 注册）。

> `IServiceCollection` 定义在 `Microsoft.Extensions.DependencyInjection.Abstractions`。SPT 5.0 服务端运行在 `Microsoft.AspNetCore.App` 共享框架上，csproj 用 `<FrameworkReference Include="Microsoft.AspNetCore.App" />` 引用同一框架取得该类型（共享框架程序集不会被复制进输出目录）。

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
| STD-META-003/004/005 | `src/ModMetadata.cs` | 反向域名 GUID、tilde `SptVersion`（含 `5.0.0`）、三段式 `Version` |
| STD-SRV-001/002/003 | `src/ModEntry.cs`、`src/Services/ExampleService.cs` | `[Injectable]`、`OnLoadOrder.X + n` 偏移、async + `CancellationToken` |
| STD-SRV-005/006/007 | `src/Routers/ExampleRouter.cs` | `StaticRouter` + `RouteAction<T>` + `OnLoadOrder.Routers` |
| STD-SRV-008 | `src/Services/ExampleService.cs`、`src/Routers/ExampleRouter.cs` | 用注入的 `ISptLogger<T>` 记录日志 |
| STD-CFG-001..005 | `src/Config/*.cs`、`config/*.jsonc` | 配置路径、命名、注册、禁 `[Injectable]`、默认副本 |
| STD-DEP-002 | `src/ModMetadata.cs` | 无硬依赖时 `ModDependencies` 保持 null |
| STD-LOG-001 | `src/Services/ExampleService.cs` | 用注入的 `ISptLogger<T>` 记录日志 |
| STD-LOG-005 | `src/Config/ModConfigRegistration.cs`、`src/ModEntry.cs` | 取消不是错误，正常传播 |
| STD-VER-002 | `src/ModMetadata.cs`、整体 | 沿用 4.1 服务端骨架，仅 `SptVersion` 改 `~5.0.0` |

## 坑

- 5.0 服务端 `IModMetadata` 是接口，不是 4.0 的抽象 record，**不要写 `override`**
- 版本必须是 semver 三段式（`1.0.0`），`1.0.0.0` 非法
- `SptVersion` 不含 `5.0.0` 会被 `ModValidator` 拒载；`ModGuid` 为空 / 重复 / 不合正则也会失败
- 自己的服务类用 `[Injectable]`；配置类**不要**加 `[Injectable]`，只能经 `IOnDIConstruct` + `AddSingleton` 注册
- `ModValidator` 会拒绝 `plugins/` 目录或疑似客户端（`.js`/`.ts`）的 mod；纯服务端 mod 目录不要放这些
- 引用 `IServiceCollection` 需要 `Microsoft.AspNetCore.App` 框架引用（csproj 已配 `FrameworkReference`）
- DLL 必须放 mod 目录**顶层**（加载器不递归子目录）
- 需 Mod Web Pages（浏览器配置界面）时：SDK 换 `Microsoft.NET.Sdk.Web`，实现 `IModBlazorMetadata`，另引用 `SPTarkov.Server.Web.dll`
- 发布归档按 `SPT_Runtime/user/mods/<Name>/` 组织，并把 `README.md`、`LICENSE` 一并随包（`STD-PKG-001`、`STD-PKG-006`）

## 参考

- 规则集：`knowledge/spt-kb/curated/modding-standard/`
- 5.0 API 笔记：`knowledge/spt-kb/curated/api-notes-5.0/`（mod-loading / di-container / http-routing / config-system）
- 模板设计蓝图：`docs/research/spt-5.0-mod-template-design.md`（§G.2）
- 真实样板：`tools/tarkov-active-probe/`、`mods/SPT5-NoStaminaDrain/`
