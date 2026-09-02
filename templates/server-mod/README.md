# SPT 4.1 服务端 Mod 模板

最小可用的 SPT 4.1 服务端 mod 项目模板。用于脚手架新的服务端 mod（.NET 类库，放入 `SPT_Runtime/user/mods/<ModName>/`）。

## 目录结构

```
server-mod/
├── ServerModTemplate.csproj     # 项目文件（HintPath 引用 SPT 4.1 DLL）
├── src/
│   ├── ModMetadata.cs           # IModMetadata 实现（mod 身份与版本声明）
│   ├── ModEntry.cs              # 入口类（[Injectable] + IOnLoad）
│   └── Services/
│       └── ExampleService.cs    # 表注入示例（GlobalTable / TemplateTable）
├── README.md
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

### 5. 验证

启动服务器，日志中应看到：

```
[SUCCESS] My Mod 已加载，物品模板数量: ...
```

若启动失败（metadata 未映射配置、注入类型无法解析），服务器会在启动阶段直接报错——见知识库 `api-notes-4.1/mod-loading.md` 失败模式。

## 关键 API 速查（4.1）

| 要做什么 | 写法 |
|---|---|
| 声明加载阶段 | `[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]` |
| 生命周期 | `IOnLoad.OnLoadAsync(CancellationToken)` / `IOnUpdate` |
| 注入数据库表 | 构造函数直接声明 `GlobalTable` / `TemplateTable` / `TradersTable` 等（位于 `SPTarkov.Server.Core.Models.Spt.Tables`） |
| 注入配置 | 直接声明具体类型（如 `InsuranceConfig`），配置类**不要**加 `[Injectable]` |
| 自定义路由 | `AbstractRouter`，优先级基于 `OnLoadOrder.Routers` 偏移 |
| 日志 | `ISptLogger<T>`（`SPTarkov.Common.Models.Logging`）：`logger.Success(...)` / `logger.Info(...)` |

完整参考：`knowledge/spt-kb/curated/modding-guide/02-server-mod-anatomy.md` 与 `api-notes-4.1/di-container.md`。

## 坑

- 4.1 中 `IModMetadata` 是接口，不是 4.0 的抽象 record，**不要写 `override`**
- 版本必须是 semver 三段式（`1.0.0`），`1.0.0.0` 非法
- 自己的服务类用 `[Injectable]`；只有容器无法自建的对象（如配置文件实例）才用 `IOnDIConstruct`
- 需 Mod Web Pages（浏览器配置界面）时：SDK 换 `Microsoft.NET.Sdk.Web`，实现 `IModBlazorMetadata`，另引用 `SPTarkov.Server.Web.dll`
