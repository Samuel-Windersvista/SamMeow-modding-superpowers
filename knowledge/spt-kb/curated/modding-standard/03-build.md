---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 03 构建与目标框架（BUILD）

> **Domain slug:** `BUILD` · **规则 ID 前缀:** `STD-BUILD-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 02，2026-09-14）。

## 维度范围

- 目标框架：服务端 `net10.0` / 客户端按版本（4.1.5 `netstandard2.1`；5.0 `net6.0`）
- 程序集引用方式（`SPTarkov.Server.*` / BepInEx / Harmony）
- csproj 关键属性与构建产物布局

## 规则

### STD-BUILD-001 — 以 `net10.0` 构建服务端 mod

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`templates/server-mod/ServerModTemplate.csproj:5`、`modding-guide/01-environment-toolchain.md`（.NET 10 / `net10.0`）；语料：本次普查 114 个引用 `SPTarkov.Server` 的 csproj 中 95 个为 `net10.0`（83.3%）（EV-CORPUS-CSPROJ）。
- **Rule:** 服务端 mod 的 `<TargetFramework>` 必须是 `net10.0`，与 SPT 服务端程序集保持一致。

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <OutputType>Library</OutputType>
</PropertyGroup>
```

> 深入：[modding-guide/01-environment-toolchain.md](../modding-guide/01-environment-toolchain.md)

### STD-BUILD-002 — 客户端使用与目标版本运行时兼容的目标框架

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`templates/client-mod/ClientModTemplate.csproj:5-9`（Unity Mono 无法加载 `net10.0` 程序集；官方客户端模块以 `netstandard2.1` 为目标）、`modding-guide/03-client-mod-anatomy.md`；5.0 机制：`mods/SPT5-NoStaminaDrain/SPT5NoStaminaDrain.csproj:5-9`（注释明示 SPT 5.0 / EFT 1.1.5 为 IL2CPP + BepInEx 6，插件必须面向 `net6.0`）；语料：本次普查 264 个引用 BepInEx 的 csproj 中 227 个为 Mono 兼容框架（`netstandard2.1` 135、`net472` 79、`net471` 7 等），仅 1 个 `net10.0`（EV-CORPUS-CSPROJ）。
- **Rule:** 客户端 mod 的 `<TargetFramework>` 必须使用与目标 SPT 版本运行时兼容的框架，按版本分支：
  - 4.1.5（Mono / BepInEx 5）：`netstandard2.1`（`net472`/`net471` 亦可），禁止 `net10.0`、`net9.0` 等 .NET Core/5+ 目标；
  - 5.0（IL2CPP / BepInEx 6）：`net6.0`。

```xml
<!-- 4.1.5（Mono / BepInEx 5） -->
<PropertyGroup>
  <TargetFramework>netstandard2.1</TargetFramework>
  <ImplicitUsings>disable</ImplicitUsings>
</PropertyGroup>
```

```xml
<!-- 5.0（IL2CPP / BepInEx 6） -->
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <ImplicitUsings>disable</ImplicitUsings>
</PropertyGroup>
```

> 深入：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)

### STD-BUILD-003 — 用 `<HintPath>` 引用客户端运行时程序集并设 `<Private>false</Private>`

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`templates/client-mod/ClientModTemplate.csproj:29-51`（`HintPath` 指向 `$(SPTInstallPath)` 下 DLL 并设 `Private=false`）；5.0 机制：`mods/SPT5-NoStaminaDrain/SPT5NoStaminaDrain.csproj:40-47`（IL2CPP 客户端引用 `$(SPT5Path)\BepInEx\interop\Assembly-CSharp.dll`、`Il2Cppmscorlib.dll`，并设 `Private=false`）；语料：本次普查 264 个客户端 csproj 中 226 个使用 `<HintPath>`、111 个设 `<Private>false</Private>`（EV-CORPUS-CSPROJ）。
- **Rule:** 客户端引用 BepInEx、Harmony、`Assembly-CSharp`、UnityEngine 等运行时程序集时，必须通过 `<HintPath>` 指向实际运行时 DLL，并设 `<Private>false</Private>`，不得把这些 DLL 复制进构建输出。引用路径随版本而异：
  - 4.1（Mono / BepInEx 5）：`$(SPTInstallPath)\EscapeFromTarkov_Data\Managed\*.dll`（如 `Assembly-CSharp.dll`）；
  - 5.0（IL2CPP / BepInEx 6）：`$(SPT5Path)\BepInEx\interop\*.dll`（Il2CppInterop 代理程序集，如 `Assembly-CSharp.dll`、`Il2Cppmscorlib.dll`）与 `$(SPT5Path)\BepInEx\core\*.dll`（`BepInEx.Core`、`BepInEx.Unity.IL2CPP`、`0Harmony`）。

```xml
<!-- 4.1（Mono / BepInEx 5） -->
<ItemGroup>
  <Reference Include="BepInEx">
    <HintPath>$(SPTInstallPath)\BepInEx\core\BepInEx.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <Reference Include="Assembly-CSharp">
    <HintPath>$(SPTInstallPath)\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

```xml
<!-- 5.0（IL2CPP / BepInEx 6）：core 引用 BepInEx 6，游戏程序集引用 interop 代理 -->
<ItemGroup>
  <Reference Include="BepInEx.Core">
    <HintPath>$(SPT5Path)\BepInEx\core\BepInEx.Core.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <Reference Include="Assembly-CSharp">
    <HintPath>$(SPT5Path)\BepInEx\interop\Assembly-CSharp.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

> 深入：模板 [templates/client-mod/ClientModTemplate.csproj](../../../../templates/client-mod/ClientModTemplate.csproj)（仓库根相对路径）

### STD-BUILD-004 — 以匹配版本的 `SPTarkov.Server.*` 引用构建服务端 mod

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`modding-guide/01-environment-toolchain.md`（引用包 `SPTarkov.Server.Core` 版本必须与服务器匹配）、`api-notes-5.0/mod-loading.md`（mod 引用的 Core 版本高于运行时即抛异常，先于其它校验）、`templates/server-mod/ServerModTemplate.csproj:27-45`；语料：本次普查 114 个引用 `SPTarkov.Server` 的 csproj 中 83 个用 NuGet `PackageReference`、31 个用 `<HintPath>`/本地 `<Reference>`（EV-CORPUS-CSPROJ）；试点：`tools/tarkov-active-probe/TarkovActiveProbe.csproj:24-35` 以 `<HintPath>` 指向运行时 DLL，版本不高于运行时即合规。
- **Rule:** 服务端 mod 必须引用 `SPTarkov.Server.Core`（及 `SPTarkov.DI`、`SPTarkov.Common`），且引用的 `SPTarkov.Server.*` 版本不得高于目标运行时版本。推荐使用 NuGet `PackageReference` 并锁定与目标 SPT 一致的版本号；当 `<HintPath>` / 本地 `<Reference>` 指向的 DLL 版本与目标运行时匹配（不高于运行时）时同样合规。

```xml
<ItemGroup>
  <PackageReference Include="SPTarkov.Server.Core" Version="4.1.0" />
  <PackageReference Include="SPTarkov.DI" Version="4.1.0" />
  <PackageReference Include="SPTarkov.Common" Version="4.1.0" />
</ItemGroup>
```

> 深入：[api-notes-5.0/mod-loading.md](../api-notes-5.0/mod-loading.md)

### STD-BUILD-005 — 设置 `AppendTargetFrameworkToOutputPath=false` 扁平化构建输出

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`templates/server-mod/ServerModTemplate.csproj:22`、`templates/client-mod/ClientModTemplate.csproj:24`（注释说明「输出路径不带框架名，方便找 DLL」）；语料：本次普查 509 个 csproj 中 145 个设置该属性（服务端 67/114、客户端 43/264）（EV-CORPUS-CSPROJ）。
- **Rule:** 设置 `<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>`，让构建产物直接落在 `bin/<Configuration>/`，而不是带框架名的子目录，便于把 DLL 复制到 mod 目录顶层。

```xml
<PropertyGroup>
  <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
</PropertyGroup>
```

> 深入：模板 [templates/server-mod/ServerModTemplate.csproj](../../../../templates/server-mod/ServerModTemplate.csproj)、[templates/client-mod/ClientModTemplate.csproj](../../../../templates/client-mod/ClientModTemplate.csproj)（仓库根相对路径）

### STD-BUILD-006 — 用可覆盖的安装路径属性定位安装目录

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`templates/server-mod/ServerModTemplate.csproj:19`、`templates/client-mod/ClientModTemplate.csproj:22`（`Condition="'$(SPTInstallPath)' == ''"`，允许命令行覆盖）；5.0 机制：`mods/SPT5-NoStaminaDrain/SPT5NoStaminaDrain.csproj:19-20`（`SPT5Path`）、`tools/tarkov-active-probe/TarkovActiveProbe.csproj:17,19`（`SPT5Runtime`），均为版本化自定义属性名且可覆盖；语料：`skills/writing-spt-mod/SKILL.md` 将「硬编码路径」列为反模式（`EV-CORPUS-MATERIALS`）。
- **Rule:** 用一个安装根路径属性定位 SPT 安装目录，并允许 `dotnet build -p:<属性名>=...` 覆盖。属性名不强制固定，可用版本化自定义名（如 `SPTInstallPath`、`SPT5Path`、`SPT5Runtime`）；属性必须可覆盖——用 `Condition="'$(<属性名>)' == ''"` 提供默认值，默认值可为某台机器的路径，但不得写成无法覆盖的硬编码。

```xml
<PropertyGroup>
  <!-- 属性名可自定（如 SPTInstallPath / SPT5Path / SPT5Runtime），关键是用 Condition 提供可覆盖的默认值 -->
  <SPTInstallPath Condition="'$(SPTInstallPath)' == ''">D:\SPT</SPTInstallPath>
</PropertyGroup>
```

```text
dotnet build -c Release -p:SPTInstallPath="D:\SPT\Server"
```

> 深入：模板 [templates/server-mod/ServerModTemplate.csproj](../../../../templates/server-mod/ServerModTemplate.csproj)（仓库根相对路径）
