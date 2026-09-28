# SPT 5.0 客户端 Mod 模板

最小可用的 SPT 5.0 客户端 mod 模板（IL2CPP / BepInEx 6 插件 + Harmony 补丁），
部署到游戏目录的 `BepInEx/plugins/<ModName>/`。

- 目标运行时：SPT 5.0 开发线（EFT 1.1.5 / IL2CPP / BepInEx 6）
- 目标框架：`net6.0`（见 `STD-BUILD-002` 的 5.0 分支）
- 4.1.5（Mono / BepInEx 5）形态见 `templates/client-mod/`，两者入口基类、引用路径、日志属性均不同，勿混用。

## 目录结构

```
spt5-client-mod/
├── ClientModTemplate.csproj     # net6.0；HintPath 引用 BepInEx/core + BepInEx/interop
├── src/
│   ├── Plugin.cs                # BepInEx 6 插件入口（BasePlugin + Load/Unload + [BepInPlugin]）
│   ├── Configuration.cs         # BepInEx 配置封装（ConfigEntry 模式）
│   └── Patches/
│       └── ExamplePatch.cs      # Harmony 补丁示例（Prefix / Postfix）
├── README.md
├── LICENSE
└── .gitignore
```

## 占位符

| 占位符 | 说明 | 示例值 |
|---|---|---|
| `{{ROOT_NAMESPACE}}` | 根命名空间 | `SamMeow.MyClientMod` |
| `{{MOD_CLASS_NAME}}` | 类名前缀（PascalCase，同时是程序集名） | `MyClientMod` |
| `{{MOD_NAME}}` | 显示名 | `My Client Mod` |
| `{{MOD_GUID}}` | 全局唯一 ID（`[BepInPlugin]` GUID，反向域名记法） | `com.sammeow.myclientmod` |
| `{{MOD_AUTHOR}}` | 作者名（写入 `LICENSE` 版权行） | `SamMeow` |
| `{{MOD_VERSION}}` | 版本（semver 三段式） | `1.0.0` |
| `{{SPT_INSTALL_PATH}}` | **SPT 5.0 实例根目录**（含 `EscapeFromTarkov.exe` 与 `BepInEx/`） | `D:\Games\SPT_5xx` |
| `{{TARGET_CLASS_NAME}}` | 补丁目标游戏类型（interop 真实类型名） | `EFT.Player` |
| `{{TARGET_METHOD_NAME}}` | 补丁目标方法名 | `SomeMethod` |

> `{{SPT_INSTALL_PATH}}` 只是默认值；命令行 `-p:SPT5Path=...` 或 `-p:SPTInstallPath=...` 可覆盖（`STD-BUILD-006`）。

## 使用步骤

### 1. 替换占位符

按上表把模板文件中的 `{{...}}` 全部替换为实际值（`.gitignore` 无占位符）。

### 2. 重命名

- 项目文件夹：`spt5-client-mod` → `MyClientMod`
- `ClientModTemplate.csproj` → `MyClientMod.csproj`

### 3. 用 ilspycmd 确认 interop 类型

5.0 的游戏程序集是 `BepInEx/interop/Assembly-CSharp.dll`（Il2CppInterop 代理），
`{{TARGET_CLASS_NAME}}` 必须是其中真实存在的类型：

```powershell
# 列出所有类（含命名空间）
ilspycmd -l c "<SPT5Path>\BepInEx\interop\Assembly-CSharp.dll"

# 反编译单个类型，确认方法名与签名
ilspycmd "EFT.Player" -o out
```

本地离线类清单可查 `knowledge/spt-kb/archive/eft-1.1.5/classes-1.1.5.txt`。
方法名用字符串写法（`"{{TARGET_METHOD_NAME}}"`），运行时解析，编译期不校验签名。

### 4. 构建

```powershell
dotnet build -c Release -p:SPT5Path="D:\Games\SPT_5xx"
```

也可用兼容别名：`-p:SPTInstallPath="D:\Games\SPT_5xx"`。

> `SPT5Path` 配错（或该实例从未启动过一次、`BepInEx/interop/` 尚未生成）时，
> `CheckGameReferences` 会在构建期直接报错，而不是抛出一堆 `CS0246`。

### 5. 部署

把构建产物复制到子目录（推荐，避免与其它插件 DLL 混杂）：

```
D:\Games\SPT_5xx\BepInEx\plugins\MyClientMod\MyClientMod.dll
```

### 6. 验证

启动游戏，BepInEx 日志（`BepInEx/LogOutput.log`）应出现加载行：

```
[Info   : My Client Mod] My Client Mod 1.0.0 已加载
```

配置文件生成在 `BepInEx/config/com.sammeow.myclientmod.cfg`。

## 配置（BepInEx ConfigFile）

客户端配置经 `BasePlugin.Config`（`ConfigFile`）的 `Config.Bind` 声明，运行时落在
`BepInEx/config/<ModGuid>.cfg`；不要自建 JSON 配置读取——`STD-CFG-006`。见 `src/Configuration.cs`。

## 规则对照（STD）

模板内关键位置已用注释标注对应 Rule ID（`STD-XXX-nnn`，可检索）：

| Rule ID | 位置 | 要求 |
|---|---|---|
| STD-STRUCT-001 | `.gitignore` | 排除 `bin/`、`obj/` 与 IDE/用户文件 |
| STD-STRUCT-003 | `src/` | 源码放 `src/` 或功能子目录 |
| STD-STRUCT-005 | `README.md` | 仓库根 README 说明用途、安装与配置 |
| STD-STRUCT-006 | `LICENSE` | 仓库根提供授权文件 |
| STD-BUILD-002 | csproj | 5.0 用 `net6.0`（4.1.5 用 `netstandard2.1`） |
| STD-BUILD-003 | csproj | `<HintPath>` + `<Private>false</Private>` 引用 `BepInEx/core` 与 `BepInEx/interop` 运行时程序集 |
| STD-BUILD-005 | csproj | `AppendTargetFrameworkToOutputPath=false` |
| STD-BUILD-006 | csproj | 安装路径属性可覆盖（`Condition` + `-p:SPT5Path` / `-p:SPTInstallPath`） |
| STD-META-005/006 | `src/Plugin.cs` | 三段式版本；`[BepInPlugin]` 三参数齐备 |
| STD-CLI-001/002 | `src/Plugin.cs` | `BasePlugin` + `Load()`（5.0）；反向域名 GUID |
| STD-CLI-003/004 | `src/Patches/ExamplePatch.cs` | `[HarmonyPatch]` 标注、`Patches/` 目录、interop 真实类型名 |
| STD-CLI-005 | `src/Plugin.cs` | `[BepInDependency]` 声明依赖（模板内为注释示例） |
| STD-CLI-006/007 | `src/Plugin.cs` | `BasePlugin.Log`；`Load` 应用补丁、`Unload` 撤销 |
| STD-CFG-006 | `src/Configuration.cs` | `Config.Bind` 声明客户端配置 |
| STD-LOG-003 | `src/Plugin.cs` | 用 BepInEx 日志源记录日志 |

## 5.0 关键点

- **目标框架 `net6.0`**：IL2CPP / BepInEx 6 插件在 .NET 6 运行时下加载（`STD-BUILD-002`）。
- **入口是 `BasePlugin` + `Load()`**：不是 `BaseUnityPlugin` + `Awake()`；撤销路径是 `Unload()`，不是 `OnDestroy()`（`STD-CLI-001`、`STD-CLI-007`）。
- **日志属性是 `Log`（`ManualLogSource`）**：4.1.5 为 `Logger`（`STD-CLI-006`）。本模板用 `internal static new ManualLogSource Log` 暴露静态日志源，供补丁类复用。
- **引用分两处**：`BepInEx/core`（`BepInEx.Core`、`BepInEx.Unity.IL2CPP`、`Il2CppInterop.Runtime`、`0Harmony`）与 `BepInEx/interop`（`Il2Cppmscorlib`、`Assembly-CSharp`、`UnityEngine.*`）；全部 `HintPath` + `Private=false`（`STD-BUILD-003`）。
- **首次启动生成 `BepInEx/interop/`**：该目录由 BepInEx 在游戏首次启动时生成；在此之前无法编译（`CheckGameReferences` 会给出明确报错）。
- **部署子目录约定**：DLL 建议放 `BepInEx/plugins/<ModName>/`，而非直接平铺在 `plugins/`。
- **补丁逐类隔离**：入口用 `TryApplyPatch(typeof(PatchClass), label)` 一类一应用，某类失败不影响其余类（照 `tools/tarkov-runtime-bridge` 模式）。

## 坑

- **可选引用参数的 Nullable 桩**：托管侧调用带 `Il2CppSystem.Nullable<T>` 默认 `null` 参数的 API 会直接抛异常（interop 桩对 `null` 做 `NotNull` 转换 + `unbox`）；需显式构造 `new Il2CppSystem.Nullable<T>()` 透传。「类型存在 + 能编译」不等于「能调用」。
- **`TomlTypeConverter` 缺省不含 `UnityEngine.Color`**：BepInEx 6 中 `ConfigEntry<Color>` 写盘即抛 `InvalidOperationException`；需在 `Bind` 前 `TomlTypeConverter.AddConverter`。
- **`AcceptableValueList` → ComboBox 崩溃**：`GUI.DoButtonGrid` 被 IL2CPP 剥离；改用 `CustomDrawer` 单按钮循环或纯文本输入。
- **virtual 属性 interop AV**：`TrackableTransform` 等虚属性实测返回坏指针 → `AccessViolation (0xc0000005)` 进程级崩溃且不可 `try/catch`；改用非虚等价物（如 `Component.transform`）。
- **失败路径禁止 `Destroy(gameObject)`**：组件挂在 GameWorld 对象上时会摧毁游戏世界；一律 `Destroy(this)`。
- **补丁体必须 `try/catch`（fail-open）**：异常穿透补丁进入游戏代码会闪退。本模板的逐类隔离只管「应用补丁」阶段，运行期异常仍需补丁体自行兜底。
- **`TryCast<T>()` 取代 `is` / 强转 / `GetType()`**：IL2CPP 下托管 LINQ 不可用，集合与类型判断走 Il2CppInterop 口径。
- **`StringTemplateId` 优先于 `TemplateId`**：物品/战利品查询以 `StringTemplateId` 为键（直接取 `TemplateId` 的隐式转换形态会导致查询全部 miss）；`ItemPrice.CurrencyId` 在 5.0 为 `Nullable<MongoID>`（用 `HasValue`/`Value`）。
- **`ClassInjector.RegisterTypeInIl2Cpp<T>()`**：注入的 MonoBehaviour 类型必须先注册，才能挂到 `GameObject` 上。
- **CS0012 引用链**：`EFT.Player`→`DissonanceVoip.dll`；`CameraManager.SSAA`→`Unity.Postprocessing.Runtime.dll`；EFT UI 基链→`Sirenix.Serialization`；TMP→`Unity.TextMeshPro.dll`。编译报 `CS0012` 时按提示补对应 interop 引用。
- **bundle 资产生命周期**：静态持有 + `HideFlags.DontUnloadUnusedAsset`，否则场景切换后 `Instantiate` 抛 NRE；`AssetBundle.LoadFromStream` 不可用（托管 Stream 不可封送），改用 `LoadFromMemory(byte[])` + `LoadAsset(name).TryCast<GameObject>()`。
- **BepInEx 6 配置版本化迁移**：持久化配置不跟随默认值变更 → 用 `[Meta] CfgVer` + 逐键条件改写；`ConfigFile` 按需加载（不能用 `Keys` 枚举旧文件键）。

完整参考：`docs/research/spt-5.0-mod-template-design.md`、
`knowledge/spt-kb/curated/operations/5xx-client-mod-dev-lessons.md`、
`mods/SPT5-AccurateCircularRadar/README.md`。
