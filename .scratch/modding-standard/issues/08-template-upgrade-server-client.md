# 08: 模板升级：server + client

**What to build:** 按**校准后**的规则修订两个模板——`templates/server-mod`：补 `config/config.jsonc` + `defaultConfig.jsonc` 加载示例与 `IOnDIConstruct` 注册（当前缺失）、README/LICENSE 模板、规则引用注释（`STD-XXX-nnn`）；`templates/client-mod`：补 README/LICENSE 模板、规则引用注释、Harmony patch 组织对齐。S1 验证：两模板 `dotnet build` 均通过。

**Blocked by:** 02, 03, 04, 05, 11（规则先经试点校准，再固化进模板）

**Status:** done

- [x] server 模板含完整 config 加载示例且构建通过
- [x] client 模板构建通过
- [x] 模板注释引用对应 Rule ID（可被检索）
- [x] S1 构建验证结果记录在票内 Comments

## Comments

### S1 构建验证（2026-09-14）

模板本身含 `{{PLACEHOLDER}}`，无法原地编译；按 `skills/writing-spt-mod/SKILL.md` 的脚手架流程，复制模板到临时目录、替换占位符后以 Release 构建。占位符取值：`ROOT_NAMESPACE=SamMeow.TplCheck`、`MOD_CLASS_NAME=TplCheck`、`MOD_NAME=Template Check`、`MOD_GUID=com.sammeow.tplcheck`、`MOD_AUTHOR=SamMeow`、`MOD_VERSION=1.0.0`、`MOD_LICENSE=MIT`、`TARGET_CLASS_NAME=EFT.Player`、`TARGET_METHOD_NAME=SomeMethod`。

构建环境：dotnet SDK 10.0.300（另有 5.0.408）；SPT 4.1.5 安装于 `E:\Game\EFT_Offline\SPT_41x`（服务端 DLL 在 `SPT_Runtime\`；客户端 DLL 在 `BepInEx\core\` 与 `EscapeFromTarkov_Data\Managed\`）。

**server-mod**（`SPTInstallPath=E:\Game\EFT_Offline\SPT_41x\SPT_Runtime`）

```
dotnet build D:\Temp\opencode\s1-server -c Release
```

结果关键行：

```
ServerModTemplate -> D:\Temp\opencode\s1-server\bin\Release\TplCheck.dll
已成功生成。
    0 个警告
    0 个错误
```

**client-mod**（`SPTInstallPath=E:\Game\EFT_Offline\SPT_41x`）

```
dotnet build D:\Temp\opencode\s1-client -c Release
```

结果关键行：

```
ClientModTemplate -> D:\Temp\opencode\s1-client\bin\Release\TplCheck.dll
已成功生成。
    0 个警告
    0 个错误
```

两个输出目录仅含 `TplCheck.dll` / `TplCheck.pdb` / `TplCheck.deps.json`，未复制 SPT / BepInEx 运行时 DLL（`<Private>false</Private>` 与 `FrameworkReference` 生效）。

**环境差异说明（如实记录，非伪造通过）**：server 模板的 config 示例引用 `IServiceCollection`，该类型来自 `Microsoft.Extensions.DependencyInjection.Abstractions`。此 DLL 不在 `SPT_Runtime\`（SPT.Server 运行在 `Microsoft.AspNetCore.App` 共享框架上，见 `SPT.Server.runtimeconfig.json`），首次构建报 CS0234 / CS0246 / CS0012 失败。在 csproj 增加 `<FrameworkReference Include="Microsoft.AspNetCore.App" />` 后构建通过；该框架引用不会把共享框架程序集复制进输出目录。

### 交付物清单

- server 新增：`src/Config/ModConfig.cs`（POCO，禁 `[Injectable]`）、`src/Config/ModConfigRegistration.cs`（`IOnDIConstruct` + `AddSingleton`，含 `defaultConfig` 首启复制与 JSONC 注释容忍）、`config/config.jsonc`、`config/defaultConfig.jsonc`、`LICENSE`。
- server 更新：`ServerModTemplate.csproj`、`src/ModEntry.cs`（示范注入并消费 config）、`src/ModMetadata.cs`、`src/Services/ExampleService.cs`、`README.md`（新增配置章节与规则对照表）。
- client 新增：`LICENSE`。
- client 更新：`ClientModTemplate.csproj`、`src/Plugin.cs`、`src/Configuration.cs`、`src/Patches/ExamplePatch.cs`、`README.md`（规则对照表、5.0 分支说明）。
- 规则引用注释（`STD-XXX-nnn`，可检索）覆盖：STRUCT-001/003/005/006、BUILD-001..006、META-001..006、SRV-001/002/003/008、CLI-001..007、CFG-001..006、LOG-001/003/005、PKG-001/006。
- 既有校验 `tests/bootstrap/verify-templates.ps1` 通过（输出：SPT server and client templates carry the expected scaffold.）。

### 验收结论

四项验收标准均满足：server config 加载示例完整且构建通过；client 构建通过；模板注释引用 Rule ID 可检索；S1 结果已记录于本节。
