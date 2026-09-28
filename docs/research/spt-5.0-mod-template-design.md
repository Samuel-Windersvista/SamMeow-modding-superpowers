# SPT 5.0 mod 模板设计研究

> 研究任务：为「纯血 SPT 5.0 mod 模板」（client / server / paired）建立构建依据与蓝图，供未来新项目使用
> 目标版本：SPT 5.0 开发线（EFT 1.1.5 / IL2CPP / BepInEx 6；client `net6.0`，server `net10.0`）
> 报告存放：`docs/research/`（`docs/README.md` 标记为「参考」）
> 调研日期：2026-09-27 · 方法：仓库内部素材侦察 + 外部一手资料（官方源码 / 官方文档）+ 本地运行时实测裁定
> 证据标记：`[实测]` 本机运行时/构建制品 · `[源码]` 官方仓库源码原文 · `[文档]` 官方文档 · `[推断]` 合理推导 · `[未知]` 未证实

## 一句话结论

SPT 5.0 模板的构建条件已经齐备，且不需要从零设计：**client 模板** = BepInEx 6 IL2CPP 插件骨架（`net6.0` + `BepInEx/core` + `BepInEx/interop` 引用 + `BasePlugin`/`Load`/`Unload` + HarmonyX），骨架直接取 `tools/tarkov-runtime-bridge`（生产级、含目录级 props 形态），坑位知识库取 `mods/SPT5-AccurateCircularRadar`；**server 模板** = `IModMetadata` + `[Injectable]` 组件（`net10.0` + 命名空间 `SPTarkov.*`），最小样板取 `tools/tarkov-active-probe`，结构母版取 `templates/server-mod`（4.1）；**paired 模板** = 平移 4.1 `templates/paired-mod`（Client/Server/Shared + 版本联动 + `pack.ps1`），Client 侧替换为 5.0 形态。唯一体系性风险是 5.0 以 BEM 滚动快照通道交付（日期戳 + build 标识，无传统稳定 tag），线内漂移为常态；对策是「实况驱动 + 可覆盖路径属性」而非锁定版本号。同时发现 3 处 KB 记载需要修订（服务端命名空间被误记为 `SPTushonka.*`，实为包名与命名空间的混淆）。

## A. 背景与范围

- 需求：`templates/` 现有三套模板（`client-mod` / `server-mod` / `paired-mod`）全部为 **SPT 4.1.5（Mono / BepInEx 5）** 形态；各 README 仅含 5.0 切换清单，无 5.0 实体模板。`skills/writing-spt-mod/SKILL.md` 目前也只服务 4.1 流程。
- 已有基础：本仓库已落地 4 个真实 SPT 5.0 工程（见 §D.2）、1 份 5.0 客户端实战经验库（`knowledge/spt-kb/curated/operations/5xx-client-mod-dev-lessons.md`）、8 篇 5.0 服务端 API 笔记（`knowledge/spt-kb/curated/api-notes-5.0/`）与规则层的 5.0 分支（`knowledge/spt-kb/curated/modding-standard/`）。
- 模板存在性（用户前提证实）：官方（SP-Tushonka 组织）无 SPT 5.0 专用模板/脚手架仓库；GitHub 公开检索仅见个别 mod 源码样本（无模板性质仓库）；Forge live 调查见 §J。BepInEx 通用 IL2CPP 模板存在（见 §E），但非 SPT 专用。
- 本报告范围：**研究 + 蓝图**，不落盘模板实体。模板落地、技能与规则联动修订为后续任务（见 §G.4）。

## B. 平台事实

| 事实 | 值 | 证据 |
|---|---|---|
| SPT 5.0 开发线 | `SP-Tushonka` 组织（`server-csharp` 5.0x-dev、`modules` 5.0x-dev 等 15 个公开仓库）；旧 `sp-tarkov/server-csharp` 已归档 | `[源码]` github.com/SP-Tushonka；github.com/sp-tarkov/server-csharp |
| 发布状态 | **已发布**（2026-09-14 起，`SPT-BLEEDINGEDGEMODS` 包通道）；无传统 `v5.0.0` tag，版本以日期戳 + build 标识（如 `5.0.0-47242-ec15a40-20260914`） | `[实测]` `SPT_5xx\SPT-BLEEDINGEDGEMODS-5.0.0-47242-ec15a40-20260914.7z` · `[源码]` SP-Tushonka/server-csharp tags · `[词表]` `CONTEXT.md`（Version Line / Moving Target） |
| 目标 EFT 版本 | 上游 `core.json`：`1.1.5.1.47510`；本机 SPT_5xx 实况：`1.1.5.0-47242`（快照差异，见 §H） | `[源码]` 上游 `core.json`（5.0x-dev）· `[实测]` `LogOutput.log:12` |
| 运行时形态 | IL2CPP + BepInEx 6；本机实测 `BepInEx 6.0.0-be.788`、`Unity 2022.3.43f2`、`.NET 6.0.7` | `[实测]` `SPT_5xx\BepInEx\LogOutput.log:1,5-7` |
| 客户端插件目标框架 | `net6.0` | `[文档]` BepInEx.Templates README · `[实测]` runtime-bridge / SPT5 mods csproj |
| 服务端目标框架 | `net10.0`（`Build.props` 中 `SptVersion` 默认 `5.0.0`） | `[源码]` SP-Tushonka/server-csharp `Build.props` · `[实测]` `TarkovActiveProbe.csproj:4` |
| 补丁与互操作库 | HarmonyX `2.10.2`（`0Harmony.dll`）；Il2CppInterop `1.5.3` | `[源码]` BepInEx repo / BE changelog |
| 服务端装配 | 自研 DI 容器；`IModMetadata` 取代 `package.json` | `[源码]` `ModLoader.cs`、`IModMetadata.cs`（5.0x-dev） |

补充实测（`E:\Game\EFT_Offline\SPT_5xx\`）：

- `SPT_Runtime\`：出厂 6 个 `SPTarkov.*.dll`（Server.Core / DI / Common / Reflection / Server.Web / Server.Assets）。
- `BepInEx\core\`：37 个 DLL（含 `BepInEx.Unity.IL2CPP.dll`、`Il2CppInterop.*`、`Cpp2IL.Core.dll` 等）。
- `BepInEx\interop\`：172 个 Il2CppInterop 代理程序集（`Assembly-CSharp.dll`、`Il2Cppmscorlib.dll`、`UnityEngine.*` 等）。
- `BepInEx\plugins\sptushonka\`：客户端 SPT 库 6 个 `SPTushonka.*.dll`（Common / Core / Custom / Debugging / Reflection / SinglePlayer）。
- `LogOutput.log`：`sptushonka-prepatch 5.0.0.0` patcher 加载；9 插件运行中（含雷达移植件与 Configuration Manager 19.0.0）。

## C. 命名空间 / 程序集裁定（矛盾收口）+ KB 勘误

调研中发现的唯一实质性记载冲突（内部 KB vs 实况），已由三方独立证据收口：

| 侧 | 包名 / 目录名（NuGet、源码树） | 程序集名（AssemblyName） | 代码命名空间（using） |
|---|---|---|---|
| **服务端** | `SPTushonka.*` | `SPTarkov.*` | `SPTarkov.*` |
| **客户端** | `SPTushonka.*` | `SPTushonka.*` | `SPTushonka.*` |

证据链：

1. `[实测]` `SPT_5xx\SPT_Runtime\` 出厂程序集名为 `SPTarkov.Server.Core.dll` / `SPTarkov.DI.dll` / `SPTarkov.Common.dll` / `SPTarkov.Reflection.dll` / `SPTarkov.Server.Web.dll`。
2. `[实测]` 运行中的服务端 mod `tools/tarkov-active-probe` 引用 `SPTarkov.Server.Core/DI/Common`（取自 `SPT_5xx\SPT_Runtime`），源码 `using SPTarkov.*`；`SptVersion = new("~5.0.0")`。
3. `[源码]` 上游 `SPTushonka.DI.csproj`（5.0x-dev）：`<PackageId>SPTushonka.DI</PackageId>` + `<AssemblyName>SPTarkov.DI</AssemblyName>` + `<RootNamespace>SPTarkov.DI</RootNamespace>`。
4. `[源码]` 客户端 `modules` 侧：`SPTushonka.SinglePlayer.csproj` 的 `AssemblyName`/`RootNamespace` 均为 `SPTushonka.*`；出厂 DLL 同名（同 §B 实测）。

**KB 勘误清单（建议独立小修，不在本报告范围内执行）**：

| 文件:行 | 现行表述（问题） | 应改为 |
|---|---|---|
| `curated/modding-standard/version-matrix.md:48` | 「命名空间前缀：4.1 `SPTarkov.*` → 5.0 `SPTushonka.*`（如 `SPTushonka.Server.Core`）」 | 区分「包名/目录名前缀」（`SPTushonka.*`）与「命名空间」（服务端仍 `SPTarkov.*`；客户端 `SPTushonka.*`） |
| `curated/modding-standard/06-config.md:106` | 「5.0 命名空间前缀为 `SPTushonka.*`（如 `SPTushonka.Server.Core.DI`）」 | 服务端 DI 命名空间为 `SPTarkov.DI`；`SPTushonka.DI` 是包名 |
| `curated/modding-standard/07-logging.md:40` | 「命名空间前缀为 `SPTushonka.*`（如 `SPTushonka.Common.Models.Logging`）」 | 服务端日志命名空间为 `SPTarkov.Common.Models.Logging` |

> 对模板的直接含义：**client 模板** using `SPTushonka.*`（如 `SPTushonka.Reflection.Patching`）；**server 模板** using `SPTarkov.*`（如 `SPTarkov.Server.Core.Models.Spt.Mod`、`SPTarkov.DI.Annotations`），只有引用包名/项目名才用 `SPTushonka.*`。

## D. 内部素材盘点（已有经验）

### D.1 现有 4.1 模板（结构母版）

| 模板 | 形态 | 关键资产 |
|---|---|---|
| `templates/client-mod/` | `netstandard2.1` / BepInEx 5 | `ClientModTemplate.csproj`（`{{PLACEHOLDER}}` + `SPTInstallPath` 可覆盖）；`src/Plugin.cs`（`BaseUnityPlugin`/`Awake`）；`Configuration.cs`；`Patches/ExamplePatch.cs`；README 含 5.0 切换清单 |
| `templates/server-mod/` | `net10.0` / `SPTarkov.*` | `ServerModTemplate.csproj`（`SPTarkov.Server.Core/DI/Common` + `SemanticVersioning` HintPath + `FrameworkReference Microsoft.AspNetCore.App`）；`ModMetadata.cs`（`IModMetadata` 11 属性）；`ModEntry.cs`（`[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]` + `IOnLoad`）；`Config/`、`Services/` |
| `templates/paired-mod/` | Client + Server + Shared | `Directory.Build.props`（集中 `<Version>` + `SPTInstallPath`/`SPTClientPath`/`SPTServerPath` 三别名 + 版本常量生成）；`scripts/pack.ps1`（stage 到 `BepInEx/plugins/<Mod>/` 与 `SPT_Runtime/user/mods/<Mod>/` 后打 zip） |

注意：三模板内均有已提交的 `obj/` 生成残留（新模板禁止重现）；`tests/bootstrap/verify-templates.ps1` 目前只校验 server/client 两目录（不含 paired），清单固定为 4.1 形态——新增 5.0 模板时需同步扩验证。

### D.2 四个真实 SPT 5.0 工程（活的参照物）

| 工程 | 角色 | 目标 |
|---|---|---|
| `mods/SPT5-NoStaminaDrain/` | 最小 client 样板（单插件 + 2 补丁；README 记录 v1 失败教训） | SPT 5.0 / EFT 1.1.5.47242 / `net6.0` |
| `mods/SPT5-AccurateCircularRadar/` | 完整移植样板 + 全坑位文档（README 317 行：API 映射表 12 条 / IL2CPP 改写表 9 条 / 偏差 15 条 / 修复 12 条 / 风险 9 条） | 同上（含 `SPTushonka.Reflection` 依赖） |
| `tools/tarkov-runtime-bridge/` | 生产级 client（`Directory.Build.props` 72 行即模板级骨架；单测工程；逐类补丁隔离 `TryApplyPatch`；`Unload()` 撤销） | `net6.0` |
| `tools/tarkov-active-probe/` | 真实 server（`IModMetadata` + `[Injectable] StaticRouter`；**无独立入口类**——入口即元数据 + 注入组件） | `net10.0` / SPT 5.0 运行中 |

### D.3 知识资产（可直接引用）

- `skills/porting-spt-mod-to-spt5/SKILL.md`：Iron Law（done 定义）、六阶段流程、`ilspycmd` API 触点核验（`knowledge/spt-kb/archive/eft-1.1.5/classes-1.1.5.txt` 16,435 类型）、11 条 IL2CPP 适配模式、坑位清单。
- `mods/SPT5-AccurateCircularRadar/README.md`：4.1→5.0 API 映射表、纯 IL2CPP 改写表、移植偏差/修复/风险全记录（精编见本报告 §F）。
- `knowledge/spt-kb/curated/operations/5xx-client-mod-dev-lessons.md`：Nullable 桩、`ModulePatch`/`Harmony.UnpatchID`、`CustomDrawer`、配置版本化迁移、纹理 marshal 铁律、服务端协同取数模式、CS0012 引用链。
- `knowledge/spt-kb/curated/api-notes-5.0/`（8 篇源码提炼）+ `wiki-tushonka/SPT_50`：服务端机制与安装说明。
- `.scratch/modding-standard/pilot/spt5-no-stamina-drain.md`：唯一 5.0 合规 pilot（PASS 19 / FAIL 7 / N-A 58）+ **8 条反向规则修订建议**（net6.0 分支、interop 引用、`BasePlugin`/`Load`、`Log`、撤销路径、`domain` 字段等）。
- `docs/spt-5.0-mod-api-能力评估报告.md`：服务端 API 完整继承 4.1；§6.3 曾建议「5.0 模板待 API 冻结后再建」（本报告以「实况驱动」策略回应，见 §H）。
- `scripts/check-mod-standard.ps1` 已支持 `-TargetSptVersion 5.0.0`；golden 记录：runtime-bridge PASS=13/FAIL=0/WAIVED=1，radar client golden 在案。

## E. 外部一手资料补充

- **BepInEx 官方模板**（`[文档]` BepInEx.Templates README）：
  - 稳定集：`dotnet new install BepInEx.Templates --nuget-source https://nuget.bepinex.dev/v3/index.json`，IL2CPP 短名 `bep6plugin_il2cpp`；
  - Bleeding Edge 集：`dotnet new install BepInEx.Templates::2.0.0-be.4 --nuget-source https://nuget.bepinex.dev/v3/index.json`，Unity IL2CPP 短名 **`bep6plugin_unity_il2cpp`**（SPT5 场景对应此模板）；
  - 官方模板 csproj 走 NuGet（`BepInEx.Unity.IL2CPP 6.0.0-be.*` + `BepInEx.PluginInfoProps`）；**SPT 生态惯例相反**：走 `<HintPath>` 引用本地安装（STD-BUILD-003）。模板设计取 SPT 惯例，NuGet 方案记为可选替代。
- **真实外部样本**（`[源码]`）：`github.com/serya1c/SPT5BEMGODMODE`（client，`net6.0`，自述目标 EFT 1.1.5.0-47242 / BepInEx 6.0.0-be.785）。亮点：`CheckGameReferences` 前置校验 target——`SptRoot` 配错时构建期即报错，值得吸收进模板。
- **服务端装载**（`[源码]` `ModLoader.cs`，5.0x-dev）：扫描 `./user/mods/` 子目录**顶层 .dll**，反射查找 `IModMetadata` 实现；**无 `package.json`**；校验器按 `SptVersion` 拒载不含 `5.0.0` 的老 mod。
- **官方示例**：`SP-Tushonka/server-mod-examples` 仍在用 4.1.3 NuGet 包（`SPTushonka.Common/DI/Server.Core`）；NuGet 上 5.0 预发布包已存在（最新 `5.0.0-pre.202609141239`，见 §J）。
- **官方测试 mod（源码副本）**：`SamMeow_SP-Tushonka_5xx_source_code\Testing\TestMod\TestMod.cs`（74 行）与 `TestMod2`（65 行）——上游自带的装载/生命周期测试件，可作最小 server mod 的第二参照。
- **专用模板现状**：官方无 SPT 5.0 专用模板/脚手架仓库（已核实 org 仓库清单）；完整社区调查（Forge live）见 §J。

## F. 关键差异清单（4.1 → 5.0，模板必须吸收）

### F.1 机制差异（规则级）

| 维度 | 4.1.5（Mono / BepInEx 5） | 5.0（IL2CPP / BepInEx 6） |
|---|---|---|
| client 目标框架 | `netstandard2.1` | **`net6.0`**（STD-BUILD-002） |
| client 引用 | `EscapeFromTarkov_Data\Managed\*.dll` | **`BepInEx\core\*.dll` + `BepInEx\interop\*.dll`**（STD-BUILD-003） |
| client 入口 | `BaseUnityPlugin` + `Awake()` | **`BepInEx.Unity.IL2CPP.BasePlugin` + `Load()`**（STD-CLI-001） |
| client 日志 | `Logger` | **`Log`（`ManualLogSource`）**（STD-CLI-006） |
| client 撤销 | `OnDestroy()` | **`Unload()`**（`BasePlugin` 无 `IDisposable.Dispose()`；STD-CLI-007） |
| client 补丁 | `SPT.Reflection.Patching` | `SPTushonka.Reflection.Patching`（API 同形，换 using） |
| server 元数据 | `IModMetadata`（同） | `IModMetadata`（同；`SptVersion` 必须含 `5.0.0`，如 `~5.0.0`） |
| server 命名空间 | `SPTarkov.*` | `SPTarkov.*`（见 §C） |
| 部署（不变） | `BepInEx/plugins/` · `user/mods/` | 同上（client 建议子目录 `BepInEx/plugins/<Mod>/`） |

### F.2 实战坑（模板 README 应内嵌的高频雷区）

- **可选引用参数的 Nullable 桩**：托管侧调用带 `Il2CppSystem.Nullable<T>` 默认 null 参数的 API 会直接抛异常（interop 桩对 null 做 `NotNull` 转换 + `unbox`）；需显式构造 `new Il2CppSystem.Nullable<T>()` 透传。「类型存在 + 能编译 ≠ 能调用」。
- **BepInEx 6 `TomlTypeConverter` 缺省不含 `UnityEngine.Color`**：`ConfigEntry<Color>` 写盘即抛 `InvalidOperationException`；需在 `Bind` 前 `TomlTypeConverter.AddConverter`。
- **`AcceptableValueList` → ComboBox 崩溃**：`GUI.DoButtonGrid` 被 IL2CPP 剥离；改用 `CustomDrawer` 单按钮循环或纯文本输入。
- **virtual 属性 interop AV**：`TrackableTransform` 等虚属性实测返回坏指针 → `AccessViolation (0xc0000005)` 进程级崩溃且**不可 try/catch**；改用非虚等价物（如 `Component.transform`）。
- **失败路径禁止 `Destroy(gameObject)`**：组件挂在 GameWorld 对象上时会摧毁游戏世界；一律 `Destroy(this)`。
- **补丁体必须 try/catch（fail-open）**：异常穿透补丁进入游戏代码会闪退；`ModulePatch` 自带每补丁 try/catch 纪律。
- **`StringTemplateId` 优先于 `TemplateId`**（查询键）；`ItemPrice.CurrencyId` 变为 `Nullable<MongoID>`（需 `HasValue`/`Value`）。
- **bundle 资产生命周期**：静态持有 + `HideFlags.DontUnloadUnusedAsset`，否则场景切换后 `Instantiate` 抛 NRE；`AssetBundle.LoadFromStream` 不可用（托管 Stream 不可封送）→ `LoadFromMemory(byte[])` + `LoadAsset(name).TryCast<GameObject>()`。
- **IL2CPP 集合与互操作**：托管 LINQ 不可用；`TryCast<T>()` 取代 `is`/强转/`GetType()`；`ClassInjector.RegisterTypeInIl2Cpp<T>()` 先注册再用；托管数组 marshal **按数组全长**（热路径必须按 rect 精确尺寸复用 scratch 数组）。
- **CS0012 引用链**：`EFT.Player`→`DissonanceVoip.dll`；`CameraManager.SSAA`→`Unity.Postprocessing.Runtime.dll`；EFT UI 基链→`Sirenix.Serialization`（+`.Config`/`.Utilities`/`.OdinInspector.Attributes`）；TMP→`Unity.TextMeshPro.dll`。
- **BepInEx 6 配置体系**：持久化配置不跟随默认值变更 → 做版本化迁移（`[Meta] CfgVer` + 逐键条件改写）；`ConfigFile` 按需加载（不能用 `Keys` 枚举旧文件键）；实时化参数在读取点每次读。

## G. 模板蓝图（build-ready）

### G.1 client 模板（建议落位：`templates/spt5-client-mod/`）

目录（沿用 4.1 模板形制）：

```
spt5-client-mod/
  ClientModTemplate.csproj    ← net6.0 形态（见下）
  src/Plugin.cs               ← BasePlugin + Load/Unload
  src/Configuration.cs        ← Config.Bind（STD-CFG-006）
  src/Patches/ExamplePatch.cs
  README.md  LICENSE  .gitignore
```

csproj 骨架（合成自 `tools/tarkov-runtime-bridge` / `SPT5BEMGODMODE` / STD-BUILD-003 原文）：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <RootNamespace>{{ROOT_NAMESPACE}}</RootNamespace>
    <AssemblyName>{{MOD_CLASS_NAME}}</AssemblyName>
    <Version>{{MOD_VERSION}}</Version>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <!-- STD-BUILD-006：可覆盖路径属性（SptRoot 为机器根） -->
    <SptRoot Condition="'$(SptRoot)' == ''">E:\Game\EFT_Offline</SptRoot>
    <SPT5Path Condition="'$(SPT5Path)' == '' AND '$(SPTInstallPath)' != ''">$(SPTInstallPath)</SPT5Path>
    <SPT5Path Condition="'$(SPT5Path)' == ''">$(SptRoot)\SPT_5xx</SPT5Path>
    <BepInExCore>$(SPT5Path)\BepInEx\core</BepInExCore>
    <Interop>$(SPT5Path)\BepInEx\interop</Interop>
  </PropertyGroup>

  <ItemGroup>
    <!-- core（BepInEx 6；STD-BUILD-003：HintPath + Private=false） -->
    <Reference Include="BepInEx.Core"><HintPath>$(BepInExCore)\BepInEx.Core.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="BepInEx.Unity.IL2CPP"><HintPath>$(BepInExCore)\BepInEx.Unity.IL2CPP.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Il2CppInterop.Runtime"><HintPath>$(BepInExCore)\Il2CppInterop.Runtime.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="0Harmony"><HintPath>$(BepInExCore)\0Harmony.dll</HintPath><Private>false</Private></Reference>
    <!-- interop（Il2CppInterop 代理；按需扩展 UnityEngine.* 模块） -->
    <Reference Include="Il2Cppmscorlib"><HintPath>$(Interop)\Il2Cppmscorlib.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Assembly-CSharp"><HintPath>$(Interop)\Assembly-CSharp.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="UnityEngine.CoreModule"><HintPath>$(Interop)\UnityEngine.CoreModule.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>

  <!-- 站位护栏（取自 SPT5BEMGODMODE）：路径配错时构建期报错 -->
  <Target Name="CheckGameReferences" BeforeTargets="ResolveReferences">
    <Error Condition="!Exists('$(Interop)\Assembly-CSharp.dll')" Text="Set SptRoot/SPT5Path to an SPT 5 installation with generated IL2CPP interop assemblies." />
  </Target>
</Project>
```

> 可选引用（按需，取自雷达工程实证）：`Comfort`、`PlayerEnums`、`SPTushonka.Reflection`/`SPTushonka.Common`（`$(SPT5Path)\BepInEx\plugins\sptushonka\`）、`UnityEngine.{UI,UIModule,ImageConversionModule,AssetBundleModule,PhysicsModule}`、`Newtonsoft.Json`。

`src/Plugin.cs` 骨架（合成自 runtime-bridge 的 Load/Unload/逐类隔离 + BepInEx 官方模板的日志属性）：

```csharp
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace {{ROOT_NAMESPACE}};

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    internal const string PluginGuid = "{{MOD_GUID}}";
    internal const string PluginName = "{{MOD_NAME}}";
    internal const string PluginVersion = "{{MOD_VERSION}}";

    internal static new ManualLogSource Log;
    private Harmony harmony;

    public override void Load()
    {
        Log = base.Log;
        var config = new ModConfig(Config);            // STD-CFG-006：可配置、可开关
        TryApplyHarmonyPatches();                      // 逐补丁类 try/catch，fail-open
        Log.LogInfo($"{PluginName} {PluginVersion} loaded");
    }

    public override bool Unload()                      // STD-CLI-007：5.0 撤销路径
    {
        if (harmony != null) { try { harmony.UnpatchSelf(); } catch { } harmony = null; }
        return true;
    }

    private void TryApplyHarmonyPatches()
    {
        try { harmony = new Harmony(PluginGuid); }
        catch (System.Exception e) { harmony = null; Log.LogError(e); return; }
        TryApplyPatch(typeof(ExamplePatch), "ExamplePatch");
    }

    private void TryApplyPatch(System.Type patchType, string label)
    {
        try { harmony.PatchAll(patchType); }
        catch (System.Exception e) { Log.LogError($"Patch {label} failed: {e}"); }
    }
}
```

README 需含：占位符表、`dotnet build -c Release`、部署到 `BepInEx/plugins/<ModName>/`、验证（`BepInEx/LogOutput.log` 出现加载行）、5.0 注意事项（首次启动生成 interop；`Unload()` 撤销；`Color` 转换器；Nullable 桩——见 §F.2）。

### G.2 server 模板（建议落位：`templates/spt5-server-mod/`）

平移 4.1 `templates/server-mod/` 的目录形制（`ServerModTemplate.csproj`、`src/ModMetadata.cs`、`src/ModEntry.cs`、`src/Config/`、`src/Services/`、`config/*.jsonc`），替换以下关键点：

- **csproj**：`net10.0`；`OutputType=Library`；引用改 `$(SPT5Runtime)\SPTarkov.Server.Core.dll` / `SPTarkov.DI.dll` / `SPTarkov.Common.dll` / `SemanticVersioning.dll`（`SPT5Runtime = $(SptRoot)\SPT_5xx\SPT_Runtime`，可覆盖；全部 `Private=false`）；`FrameworkReference Microsoft.AspNetCore.App` 视配置方式保留（4.1 模板有；active-probe 未用）。
- **ModMetadata**：`SptVersion = new("~5.0.0")`；其余 11 属性不变（`[源码]` `IModMetadata.cs`）。
- **入口形态**：`[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)] : IOnLoad`（服务承接）；路由形态：`[Injectable(TypePriority = OnLoadOrder.Routers)] : StaticRouter`（模板可附一个示例路由）。
- **README 补三点**：无 `package.json`（元数据在 DLL 内）；`SptVersion` 必须含 `5.0.0`；部署到 `user/mods/<Mod>/` 顶层 DLL（可含 `config/`）。

### G.3 paired 模板（建议落位：`templates/spt5-paired-mod/`）

- 平移 4.1 结构：`PairedModTemplate.sln` + `Directory.Build.props`（版本联动 + `SptRoot`/`SPT5Path`/`SPT5Runtime` 别名）+ `Client/` + `Server/` + `Shared/` + `scripts/pack.ps1`。
- 替换点：Client 用 §G.1 的 `net6.0` 形态；Server 用 §G.2；`Shared/` 保持 `netstandard2.0` 纯常量（不引 SPT/BepInEx）。
- `pack.ps1` 的 stage 目标不变：`BepInEx/plugins/<Mod>/`（Client + Shared）与 `SPT_Runtime/user/mods/<Mod>/`（Server + Shared + config），`Compress-Archive` 单 zip。

### G.4 落地与联动（后续任务建议）

1. 新建上述三目录（保留 4.1 三模板不动）；同步扩 `tests/bootstrap/verify-templates.ps1` 覆盖新目录。
2. `skills/writing-spt-mod/SKILL.md` 增加 5.0 pipeline 分支（映射到新目录）。
3. KB 勘误修订（§C 三处）+ `version-matrix.md` 补 5.0 模板落位说明。
4. 首版模板以 `mods/SPT5-NoStaminaDrain` 规格做烟雾构建（`dotnet build -c Release` 0 error）与一次 MO2 覆盖层部署验证；规则合规跑 `scripts/check-mod-standard.ps1 -TargetSptVersion 5.0.0`。

## H. 风险与未决项

| 风险 | 说明 | 缓解 |
|---|---|---|
| 上游 BEM 滚动快照（无传统稳定 tag） | 5.x 线内漂移为常态；`core.json` 指向 47510，本机快照为 47242 | 模板「实况驱动」：路径属性可覆盖、不锁 build 号；升级后重跑构建验证 |
| NuGet 5.0 包为 pre 版本（`5.0.0-pre.*`，见 §J） | 预发布号滚动，稳定性依赖官方节奏；`server-mod-examples` 仍用 4.1.3 稳定包 | 模板默认 HintPath（本地运行锁定）；NuGet 形态作为可选项并注明 pre |
| Forge 上无 SPT 5.x 兼容 mod（2026-09-27 实测，§J） | 社区侧无现成样本可借；模板为空白填补 | 样本以内置四工程 + 官方源码（`modules` / `Testing/TestMod`）为准 |
| HarmonyX/IL2CPP 泛型限制无官方逐项清单 | 实践可行（`List<T>`/`DamageInfo` 已实测打过），但无白名单 | 模板 README 记录「补丁失败即禁用该补丁（fail-open）」纪律 |
| 能力评估报告 §6.3「API 冻结后再建」保留意见 | 与本次「现在建」结论存在张力 | 以可覆盖路径 + 轻量版本参数 + README 标注「BEM 形态」化解 |
| bundle 模板 | 明确暂缓（modding-standard Out of Scope） | 不在本批范围 |

## I. 来源清单

**外部（网址）**

- `github.com/SP-Tushonka`（`server-csharp` / `modules` / `server-mod-examples`；含 5.0x-dev 分支源码）
- `github.com/sp-tarkov/server-csharp`（archived）
- `raw.githubusercontent.com/SP-Tushonka/server-csharp/5.0x-dev/…`：`SPTushonka.DI.csproj`、`Models/Spt/Mod/IModMetadata.cs`、`Modding/ModLoader.cs`、`Build.props`、`SPT_Data/configs/core.json`
- `raw.githubusercontent.com/SP-Tushonka/modules/5.0x-dev/Directory.Build.props` / `SPTushonka.SinglePlayer/…`
- `github.com/BepInEx/BepInEx.Templates`（README：`bep6plugin_unity_il2cpp`）
- `github.com/BepInEx/BepInEx`（HarmonyX 2.10.2 / Il2CppInterop 1.5.3 / dotnet-runtime 6.0.7）
- `docs.bepinex.dev`（IL2CPP 安装指南 / 插件开发教程）
- `builds.bepinex.dev/projects/bepinex_be`
- `github.com/serya1c/SPT5BEMGODMODE`
- `sp-mod.com/api/v0`（Forge live：`GET /mods`，`filter[spt_version]` / `per_page` / `sort`；实测 2026-09-27）
- `nuget.org`（经 `nuget.azure.cn` 镜像实测）：`SPTushonka.{Server.Core,DI,Common}` 版本列表（flatcontainer API）

**本地（路径）**

- `templates/{client,server,paired}-mod/`
- `mods/{SPT5-NoStaminaDrain,SPT5-AccurateCircularRadar}/`、`tools/{tarkov-runtime-bridge,tarkov-active-probe}/`
- `skills/{writing-spt-mod,porting-spt-mod-to-spt5}/SKILL.md`
- `knowledge/spt-kb/curated/{api-notes-5.0,operations,modding-standard}/…`
- `.scratch/modding-standard/pilot/spt5-no-stamina-drain.md`
- `docs/{spt-5.0-mod-api-能力评估报告,spt-5.0x-dev-现状报告,eft-1.1.5-il2cpp-逆向可行性报告}.md`
- 实测：`E:\Game\EFT_Offline\SPT_5xx\{SPT_Runtime,BepInEx\core,BepInEx\interop,BepInEx\plugins\sptushonka,BepInEx\LogOutput.log}`
- 源码副本：`E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code\`（server-csharp 5.0x-dev 完整克隆；含 `Testing/TestMod{2}`、`SPTushonka.Server/Modding/ModLoader.cs`）

## J. 社区生态快查（Forge live + NuGet）【2026-09-27 实测】

**Forge live（`sp-mod.com/api/v0`，只读免认证）**

| 查询（`GET /api/v0/mods`） | 结果 |
|---|---|
| 全量 | `total = 1921` |
| `filter[spt_version]=^5.0.0` | **`total = 0`** |
| `filter[spt_version]=5.0.0` | **`total = 0`** |
| `filter[spt_version]=^3.11.0`（机制对照） | `total = 613` |
| `filter[spt_version]=^4.1.0`（机制对照） | `total = 520` |

- 结论：过滤器机制经对照验证有效；**截至 2026-09-27，Forge 上尚无声明 SPT 5.x 兼容的 mod**——社区侧无现成 SPT5 样本/模板可参照，本仓库四个工程 + 官方 org 源码是当前唯一可用素材；「无纯血 SPT5 模板」前提完全成立。
- API 要点（供未来自动化复用）：SPT 版本过滤参数为 **`filter[spt_version]`**（SemVer 约束值）；分页 `page` / `per_page`（≤50）；排序 `sort=-updated_at`（允许字段含 `name`/`downloads`/`updated_at` 等）；源码链接经 `include=source_code_links` 或响应内 `source_code_links[]`（`{url,label}`）；`fields=` 白名单含非法字段名会返回 400（宁可不传）；无需认证。
- 参考查询：`https://sp-mod.com/api/v0/mods?filter[spt_version]=%5E5.0.0&per_page=50`。

**NuGet 包状态（经 `nuget.azure.cn` 镜像实测）**

| 包 | 稳定线 | 5.0 预发布线 |
|---|---|---|
| `SPTushonka.Server.Core` | 至 `4.1.6` | 4 个快照（`20260909…`–`202609141239`），最新 `5.0.0-pre.202609141239` |
| `SPTushonka.DI` | 至 `4.1.6` | 同上 |
| `SPTushonka.Common` | 至 `4.1.6` | 同上 |

- 含义：**server 模板的引用策略无须等待**——默认 HintPath（本地 `SPT_Runtime`，运行锁定，随本机升级自动跟随）；NuGet `SPTushonka.* 5.0.0-pre.*` 作为可选替代（需注明 pre 版本滚动风险）。客户端侧无对应 NuGet（BepInEx 6 走 BE feed）。
- 上游克隆证据补充：本地源码副本 `E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code\` 内含官方测试 mod `Testing/TestMod/TestMod.cs`（74 行）与 `TestMod2`（65 行），可作最小 server mod 的第二参照。

## 与既有资料的关系

- **更新上游结论**：§B/§E 承接 `docs/spt-5.0-mod-api-能力评估报告.md`；其 §6.3「5.0 模板待 API 冻结后再建」在本报告中被「实况驱动」构建路径取代（理由与缓解见 §H）。
- **实战经验索引**：§F 为 `mods/SPT5-AccurateCircularRadar/README.md` 与 `knowledge/spt-kb/curated/operations/5xx-client-mod-dev-lessons.md` 的精编；完整细节以原文件为准。
- **规则联动**：§C 勘误与 §G.4 联动项指向 `knowledge/spt-kb/curated/modding-standard/` 与 `skills/writing-spt-mod/`。
- **历史文档**：`docs/wayfinder/`（HISTORICAL）中 005 票「mod 开发工作流」为 4.1 时代产物；5.0 模板落地时按本报告口径复核。
- **既有研究**：`docs/research/spt-runtime-state-export.md`（同目录）。
- **术语核对**：发布状态口径已与 `CONTEXT.md`（Version Line / Moving Target）交叉核对并统一（2026-09-27）。
