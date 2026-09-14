# 09: 新增 paired 模板

**What to build:** 新建 `templates/paired-mod`：`Client/` + `Server/` + `Shared/`（+ 可选 `Fika/`）布局、单 `.sln` 统管、版本号联动（集中属性定义）、打包脚本约定（单 zip 同时含 `BepInEx/plugins/<Mod>/` 与 `SPT_Runtime/user/mods/<Mod>/`；服务端目录唯一 `IModMetadata`）。对齐 ticket 08 的模板惯例与规则引用注释。S1 验证：`dotnet build` 通过。

**Blocked by:** 08

**Status:** done

- [x] paired 模板结构完整（Client/Server/Shared + sln + 打包脚本）
- [x] 版本号联动机制就位（两端同版本）
- [x] 构建通过（S1 验证，记录在票内 Comments）
- [x] 规则引用注释与既有模板一致

## Comments

### S1 构建验证（2026-09-14）

模板本身含 `{{PLACEHOLDER}}`，无法原地编译；复制 `templates/paired-mod` 到 `D:\Temp\opencode\s1-paired`，替换占位符后以 Release 构建。占位符取值：`ROOT_NAMESPACE=SamMeow.TplCheck`、`MOD_CLASS_NAME=TplCheck`、`MOD_NAME=Template Check`、`MOD_GUID=com.sammeow.tplcheck`、`MOD_AUTHOR=SamMeow`、`MOD_VERSION=1.0.0`、`MOD_LICENSE=MIT`、`SPT_INSTALL_PATH=E:\Game\EFT_Offline\SPT_41x`、`TARGET_CLASS_NAME=EFT.Player`、`TARGET_METHOD_NAME=SomeMethod`。

构建环境：dotnet SDK 10.0.300；SPT 4.1.5 安装于 `E:\Game\EFT_Offline\SPT_41x`（客户端 DLL 在 `BepInEx\core\` 与 `EscapeFromTarkov_Data\Managed\`；服务端 DLL 在 `SPT_Runtime\`）。

命令：

```
dotnet build D:\Temp\opencode\s1-paired\PairedModTemplate.sln -c Release -p:SPTInstallPath="E:\Game\EFT_Offline\SPT_41x"
```

结果关键行：

```
Shared -> D:\Temp\opencode\s1-paired\Shared\bin\Release\TplCheck.Shared.dll
Client -> D:\Temp\opencode\s1-paired\Client\bin\Release\TplCheck.Client.dll
Server -> D:\Temp\opencode\s1-paired\Server\bin\Release\TplCheck.Server.dll

已成功生成。
    0 个警告
    0 个错误
```

另验证：不带 `-p:SPTInstallPath` 时（走 `Directory.Build.props` 默认值）同样 0 警告 0 错误，证明默认属性可用且可覆盖（STD-BUILD-006）。

版本联动验证：生成的编译期常量 `Server\obj\Release\ModVersion.g.cs` 内容为 `internal static class ModVersion { public const string Value = "1.0.0"; }`，来源为 `Directory.Build.props` 的 `<Version>`；三个 csproj 均无 `<Version>` 元素。

输出目录仅含 `TplCheck.Client.dll` / `TplCheck.Server.dll` / `TplCheck.Shared.dll`（及 pdb / deps.json），未复制 SPT / BepInEx 运行时 DLL（`<Private>false</Private>` 与 `FrameworkReference` 生效）。

打包脚本实跑：

```
powershell -NoProfile -ExecutionPolicy Bypass -File D:\Temp\opencode\s1-paired\scripts\pack.ps1 -SptInstallPath "E:\Game\EFT_Offline\SPT_41x"
```

产物 `dist\TplCheck-1.0.0.zip` 条目（顶层即游戏根相对路径）：

```
BepInEx\plugins\TplCheck\TplCheck.Client.dll
BepInEx\plugins\TplCheck\TplCheck.Shared.dll
SPT_Runtime\user\mods\TplCheck\TplCheck.Server.dll
SPT_Runtime\user\mods\TplCheck\TplCheck.Shared.dll
SPT_Runtime\user\mods\TplCheck\config\config.jsonc
SPT_Runtime\user\mods\TplCheck\config\defaultConfig.jsonc
README.md
LICENSE
```

服务端 mod 目录内唯一 `IModMetadata` 实现为 `TplCheck.Server.dll`（源码中仅 `Server/src/ModMetadata.cs` 实现该接口，`Shared.dll` 只是共享常量），满足 STD-PKG-004。

既有校验 `tests/bootstrap/verify-templates.ps1` 通过（server / client 模板不变）。`tests/bootstrap/verify-layout.ps1` 报 7 项缺失不变式（`plugins`、`hooks`、`.claude-plugin`、`.codex-plugin`、`.agents`、`.mcp.json`、`external/spt-archive`），均为本次改动前既存的仓库状态（本次仅新增 `templates/paired-mod/`），与 paired 模板无关。

### 交付物清单

新增 `templates/paired-mod/`（20 个文件）：

- `PairedModTemplate.sln`：单解决方案含 Client / Server / Shared 三个工程。
- `Directory.Build.props`：集中 `<Version>`（两端 + Shared 继承，csproj 不各自写版本，STD-META-007 / STD-PKG-005）；可覆盖的 `SPTInstallPath` / `SPTClientPath` / `SPTServerPath`（STD-BUILD-006）；MSBuild target 将 `$(Version)` 生成为编译期常量 `ModVersion.Value` 供两端代码引用。
- `Client/Client.csproj`（netstandard2.1）、`Client/src/Plugin.cs`、`Client/src/Configuration.cs`、`Client/src/Patches/ExamplePatch.cs`。
- `Server/Server.csproj`（net10.0）、`Server/src/ModMetadata.cs`、`Server/src/ModEntry.cs`、`Server/src/Config/ModConfig.cs`、`Server/src/Config/ModConfigRegistration.cs`、`Server/src/Services/ExampleService.cs`、`Server/config/config.jsonc`、`Server/config/defaultConfig.jsonc`。
- `Shared/Shared.csproj`（netstandard2.0）、`Shared/src/SharedConstants.cs`。
- `scripts/pack.ps1`：单 zip 双端（STD-PKG-001 / PKG-003 / PKG-004 / PKG-005 / PKG-006）。
- `README.md`、`LICENSE`、`.gitignore`。

规则引用注释（`STD-XXX-nnn`，可检索）覆盖：STRUCT-001/003/004/005/006、BUILD-001..006、META-001..007、SRV-001/002/003/008、CLI-001..007、CFG-001..006、LOG-001/003/005、PKG-001/003/004/005/006、DEP-002。

### 验收结论

四项验收标准均满足：结构完整（Client/Server/Shared + sln + pack.ps1）；版本号联动就位（单一 `<Version>` 来源 + 代码常量，三个 csproj 均无 `<Version>`）；S1 构建 0 警告 0 错误；规则引用注释与既有模板同风格（`// STD-XXX-nnn` + README 规则对照表）。
