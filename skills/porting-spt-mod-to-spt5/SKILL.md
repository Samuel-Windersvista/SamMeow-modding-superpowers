---
name: porting-spt-mod-to-spt5
description: Use when porting an existing SPT 4.x mod (source available) to SPT 5.0 (EFT 1.1.5 / IL2CPP / BepInEx 6 / net6.0). Triggers - "port to SPT5", "port this mod to 5.0", "移植到5", "移植到SPT5", "update this mod for SPT 5.0", "make this mod work on SPT 5". Covers: source acquisition + archive check, API surface verification via ilspycmd, IL2CPP adaptation patterns, build, MO2 overlay deployment, BepInEx log verification. NOT for writing a new mod (use writing-spt-mod), installing a prebuilt mod (use interpreting-spt-mod-instructions), or translating mod text (use using-spt-translator).
---

# Porting an SPT Mod to SPT 5.0（执行技能）

一个判断 + 一套流程：**"我有一个能跑的 SPT 4.x mod（有源码），怎么让它跑在 SPT 5.0 上？"**

适用范围：**客户端 mod**（BepInEx / Harmony）为主 —— 4.1→5.0 的跨度是运行时换代（Mono→IL2CPP、BepInEx 5→6、net48→net6.0），几乎所有补丁点都要重新核对。服务端 mod 的 5.0 API 见 `knowledge/spt-kb/curated/api-notes-5.0/`。

## The Iron Law

```text
+--------------------------------------------------------------------------------------------------+
| 移植完成的定义（全部满足才算 done）：                                                              |
| 1. 上游源码完整（本地归档无缺）且 provenance 已记录（repo + commit + license）                      |
| 2. 每一个 EFT/SPT API 触点都已对照 1.1.5 interop 核实（或记录降级方案）                             |
| 3. dotnet build -c Release 0 error，内嵌资源名与代码常量匹配                                        |
| 4. 以 MO2 覆盖层部署，modlist 中唯一且启用（mo2_modlist 可读回）                                    |
| 5. 经 MO2 VFS 启动后 BepInEx 日志出现插件加载 + 资源加载行（Level B）                               |
+--------------------------------------------------------------------------------------------------+
```

## 0. 版本跨度（为什么不能只改引用）

| 项 | SPT 4.1 | SPT 5.0 / EFT 1.1.5 |
|---|---|---|
| 运行时 | Mono | **IL2CPP** |
| BepInEx | 5.x | **6.0.0-be** |
| 插件基类 | `BaseUnityPlugin`（`Awake`） | `BepInEx.Unity.IL2CPP.BasePlugin`（`Load`/`Unload`） |
| 目标框架 | netstandard2.1 / net48 | **net6.0** |
| 游戏程序集 | `EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll` | `BepInEx\interop\Assembly-CSharp.dll`（Il2CppInterop 代理） |
| 补丁框架 | Harmony 2 / `SPT.Reflection.Patching` | HarmonyX（随 BepInEx 6）/ `SPTushonka.Reflection.Patching` |
| SPT 客户端库 | `SPT.Common` / `SPT.Reflection` | `SPTushonka.Common` / `SPTushonka.Reflection`（`BepInEx\plugins\sptushonka\`） |

## 1. 工作流（六阶段）

### Phase 1 — 源码获取与归档核查

1. 先查本地归档：`knowledge/spt-kb/archive/forge/mods/<Name>_<id>_source/`。**检查完整性**：要有 `.cs` 源文件与 `.git`（曾出现「只剩 obj 残留」的残缺克隆）。
2. 残缺/缺失 → 补克隆（GitHub 直连被重置，必须走代理）：
   ```powershell
   git -c http.proxy=http://127.0.0.1:7890 clone --depth 1 <repo> <dir>
   ```
   批量采集走 `scripts/spt-kb/`（fetch → clone → finalize MANIFEST）。
3. 顺手查上游是否有 5.0 分支/新 tag（`git ls-remote --heads --tags <repo>`）——有则优先评估直接用。
4. 记录 provenance：仓库 URL + `git rev-parse HEAD` + 许可证（Forge 页面 Details 区）。

**完成判据**：源码完整可读，provenance 三件套（repo / commit / license）已落纸。

### Phase 2 — API 触点核实（ilspycmd）

1. 工具：`ilspycmd`（`-l c <dll>` 列类型；`-t "<Type>" <dll>` 反编译单类型）。存在性快查先 grep `knowledge/spt-kb/archive/eft-1.1.5/classes-1.1.5.txt`（16,435 类型清单）。
2. 数据源优先级：`BepInEx\interop\Assembly-CSharp.dll`（真实未混淆类名）→ `BepInEx\interop\UnityEngine.*.dll` → `BepInEx\plugins\sptushonka\*.dll`。
3. 产出**触点表**：mod 里每个 EFT/SPT/Unity API 调用 → 5.0 对应物（或降级方案）。这张表就是 Phase 4 的施工图。逐条核到方法签名/字段可见性级别，不要停在"类名存在"。
4. 参考：`docs/eft-1.1.5-类名映射重建报告.md`（4.1→1.1.5 类名对照 + 工具链）。

**完成判据**：每个触点都有「同名存在 / 改名 / 换命名空间 / 降级」四态之一的结论。

### Phase 3 — 工程搭建

- 位置 `mods/SPT5-<ModName>/`；`<AssemblyName>` 保持上游 DLL 名；`<RootNamespace>` 保持上游命名空间（内嵌资源名 = RootNamespace + 相对路径，例：`Radar.bundle.radarhud.bundle`）。
- csproj 骨架：net6.0、`AppendTargetFrameworkToOutputPath=false`、`Nullable disable`、`AllowUnsafeBlocks=true`（如需）、`SPTInstallPath` 可覆盖（STD-BUILD-006）。
- 引用清单（全部 HintPath → SPT5 安装、`Private=false`）：
  - `BepInEx\core`：BepInEx.Core、BepInEx.Unity.IL2CPP、0Harmony、Il2CppInterop.Runtime
  - `BepInEx\interop`：Il2Cppmscorlib、Il2CppSystem、Assembly-CSharp、Comfort、PlayerEnums、UnityEngine.CoreModule、UnityEngine.UI、UnityEngine.UIModule、UnityEngine.ImageConversionModule、UnityEngine.AssetBundleModule、UnityEngine.PhysicsModule、Newtonsoft.Json（按需增删）
  - `BepInEx\plugins\sptushonka`：SPTushonka.Reflection、SPTushonka.Common（按需）
- 资源（bundle/PNG）原样复制进 `bundle/`；**不要重建 Unity 工程**——除非运行时证实 bundle 不兼容且本机有对应版本 Unity Editor。
- 机检：`scripts/check-mod-standard.ps1 -ModPath <dir> -TargetSptVersion 5.0.0 -Kind client`。

**完成判据**：`dotnet build` 能跑通（哪怕先带错误清单），资源内嵌名已设计好。

### Phase 4 — 代码适配

按触点表逐文件改，保持行为等价；逻辑重写（如协程→Update、事件→轮询）单独记录到 README 的偏差表。样板参照 `tools/tarkov-runtime-bridge/`（本仓已跑通的 SPT5 客户端插件）。

**完成判据**：`dotnet build -c Release` 0 error；无 `SPT.` 旧命名空间残留（`SPTushonka.` 除外）；所有 MonoBehaviour/Graphic 子类已注册。

### Phase 5 — 构建与部署（MO2 覆盖层）

1. 产物核验：DLL 存在 + 内嵌资源名与代码常量匹配（二进制 grep 资源前缀）。
2. 覆盖层命名 `<category>-<mod-name>-<version>`（实例内既有类别如 `前置-` / `工具-` / `界面-`）；布局 = 游戏根相对（`BepInEx\plugins\<DLL>`）。
3. meta.ini：`comments`（必填短摘要）+ `notes`（安装记录）；用 `mo2_edit_meta` 原子合并。
4. modlist：`+<overlay>` 一行。**MO2 GUI 运行时其目录监视器会自动把新 mod 以 `-`（禁用）写入 modlist** —— 刷新（F5）后确认条目为 `+` 且唯一；重启 MO2 最稳。读回验证：`mo2_modlist`。
5. MCP 限制（离线场景）：`mo2_install` 需 sidecar、`mo2_create_mod` 需 live broker、`mo2_toggle_mod` 只操作已在 modlist 的条目 → 降级路径 = 文件系统铺放 + `mo2_edit_meta` + 手工 modlist 行。
6. **游戏必须关闭**（运行中的游戏锁定覆盖层 DLL）。

**完成判据**：`mo2_modlist` 读回 = 目标 mod 唯一且 `enabled=true`。

### Phase 6 — 运行时验证

1. 经 MO2 启动（VFS 必须生效；实例入口如 `运行离线塔科夫` → `sptvfsbridge.bat`）。
2. 检查 `<SPT>\BepInEx\LogOutput.log`：`Loading [<name> <version>]`、插件自身 load 行、资源加载行。到主菜单即可验证加载（无需进战局）。
3. Il2CppInterop 的 `unsupported parameter System.Object` 警告 = 托管侧事件处理器不可被原生调用，C# 侧订阅路径不受影响（cosmetic，可忽略）。
4. 进战局做功能自检（该 mod 的验收点逐条过）。

**完成判据**：日志出现插件加载 + 资源加载行；功能自检清单交给用户（或实测通过）。

## 2. API 映射表（4.1 → 5.0，已核实）

| 4.1 | 5.0 | 备注 |
|---|---|---|
| `BepInEx.BaseUnityPlugin` + `Awake` | `BepInEx.Unity.IL2CPP.BasePlugin` + `Load`/`Unload` | BasePlugin 非 MonoBehaviour：无 gameObject/DontDestroyOnLoad；日志用 `base.Log` |
| `SPT.Reflection.Patching.ModulePatch` | `SPTushonka.Reflection.Patching.ModulePatch` | API 完全相同（`GetTargetMethod`/`Enable`/`[PatchPrefix]`/`[PatchPostfix]`），只改 using |
| `SPT.Common.Http.RequestHandler` | `SPTushonka.Common.Http.RequestHandler` | 方法签名需核实（如 `PostAsync`→`PostJsonAsync`） |
| `SPT.Reflection.Utils.ClientAppUtils.GetMainApp().GetClientBackEndSession()` | `Singleton<ClientApplication<IEftSession>>.Instance.GetClientBackEndSession()` | `ClientApplication<T>` 在 `EFT` 命名空间；`TarkovApplication : CommonClientApplication<IEftSession>` |
| `EFT.Interactive.DictionaryListHydra<TKey,TValue>` | **全局命名空间** `DictionaryListHydra<TKey,TValue>` | interop 怪癖：无命名空间前缀 |
| `IEftSession.Traders` / `GetSupplyData(id)` | 同（`IEftSession : ITradingSession`，成员在基接口） | `Traders: IEnumerable<Trader>`；`GetSupplyData: Task<Result<SupplyData>>` |
| `EFT.Trading.Trader._supplyData` / `GetUserItemPrice` / `LocalizedName` / `CurrencyCourses` | 全部同名存在 | interop 中 `_supplyData` 为 public field |
| `KeyboardShortcut`（BepInEx 5 核心） | shim：`plugins\sptushonka\ConfigurationManager\BepInEx.KeyboardShortcut.dll` | `BepInEx.Configuration.KeyboardShortcut`（[Obsolete] 但可用；其静态构造注册 TOML 转换器） |
| `ConfigEntry<Color>` | 需自定义 `TomlTypeConverter`（BepInEx 6 无内置 Color） | 在 `Bind` 之前注册 |
| `GameWorld.OnGameStarted` / `Player.OnMakingShot(IWeapon, Vector3)` / `Player.ProfileId` / `GameWorld.MainPlayer` / `AllPlayersEverExisted` / `GameWorld.LootItems` | 全部同名存在 | 已核实（1.1.5 interop） |

## 3. IL2CPP 适配模式（逐条）

1. **自定义 MonoBehaviour / Graphic 子类**必须注册 + 带 `IntPtr` 构造：
   ```csharp
   ClassInjector.RegisterTypeInIl2Cpp<MyBehaviour>();   // Load() 中、任何使用之前
   public class MyBehaviour : MonoBehaviour
   {
       public MyBehaviour(IntPtr pointer) : base(pointer) { }
   }
   ```
2. **代理类型判定**：`is` / 强转 / `GetType().Name` 对 interop 代理恒失败 → 用 `TryCast<T>()`。
3. **Il2Cpp 集合无托管 LINQ** → 显式循环（`Reverse` / `First` / `Count` / `ToArray` / `OrderBy` 都别用）。
4. **资源加载**：`AssetBundle.LoadFromMemory(byte[])`（不要 `LoadFromStream`——interop 要 `Il2CppSystem.IO.Stream`）；`LoadAsset(name).TryCast<GameObject>()`（无泛型重载）；`ImageConversion.LoadImage(Texture2D, Il2CppStructArray<byte>, bool)`（`byte[]` 隐式转换）。
5. **协程**：`StartCoroutine(托管 IEnumerator)` 不可靠 → 改 Update 驱动计时。
6. **`Mesh` 集合参数**只收 `Il2CppSystem.Collections.Generic.List<T>` → 用属性赋值（`mesh.vertices = T[]`）。
7. `new GameObject(name, typeof(T))` → `new GameObject(name)` + `AddComponent<T>()`。
8. `OnPopulateMesh` 重写必须 `public override`（interop 生成为 public）。
9. **非 blittable struct 的委托**（如 `Action<EBodyPart, float, DamageInfo>`）无法经 `DelegateSupport.ConvertDelegate` 封送 → 用 Harmony 补丁替代事件订阅。
10. 含托管类型参数的方法标 `[HideFromIl2Cpp]`（避免 il2cpp 尝试封送）。
11. 泛型闭包补丁（`DictionaryListHydra<int, LootItem>`）可直接 `typeof(...).GetMethod(...)`；`ModulePatch.Enable()` 自带 try/catch，单补丁失败不拖垮插件其余部分。

## 4. 坑位清单（实战教训）

- **[致命] 宿主对象审计（`Destroy(gameObject)` 事故）**：把组件挂到游戏对象之前先确认宿主 —— 若组件挂在 `GameWorld` 等核心对象上（典型：`__instance.gameObject.AddComponent<T>()`），**所有失败路径只能 `Destroy(this)`**；`Destroy(gameObject)` 会摧毁游戏世界，局内移动 / 武器 / 交互连锁崩坏（2026-09-15 实战事故：上游 4.1 代码在雷达加载失败路径上继承了该缺陷）。移植时逐一审计上游的 `Destroy(gameObject)` 站点并核对宿主。
- **[致命] F12 ConfigurationManager 的 ComboBox 崩溃**：`AcceptableValueList` / 枚举型配置项会让 CM 用 ComboBox 渲染下拉列表，而 ComboBox 依赖被 IL2CPP 剥离的 `UnityEngine.GUI.DoButtonGrid` → `System.NotSupportedException: Method unstripping failed` 每帧刷屏 + `GUI Error: pushing more GUIClips` 失衡（实战：单会话 6,477 + 6,421 条）。规避：列表型配置改**纯文本输入**（代码侧校验 + 回退默认值），不要用 AcceptableValueList。
- **[致命] 补丁体绝不允许向游戏代码抛异常**：Harmony 补丁体（prefix/postfix）是游戏方法调用链的一部分 —— 任何异常都会穿透进游戏代码导致闪退（实战：`GetByKey` 对缺失 key 抛 `KeyNotFoundException` 直接 CTD）。所有补丁体 try/catch + 失败降级；读取外部数据前先做存在性检查（如 `ContainsKey`）。
- **[致命] 避开 virtual 属性的 interop 调用（AV 不可捕获）**：IL2CPP 下 `il2cpp_object_get_virtual_method` 对部分 virtual 属性返回坏指针 → `il2cpp_runtime_invoke` 直接 AccessViolation（0xc0000005，进程级崩溃，**.NET Core 不可 try/catch**；只有 `BepInEx/ErrorLog.log` 留痕）。实战：`InteractableObject.TrackableTransform` 崩溃 → 换非虚的 `Component.transform`。移植时对 hot path 上的 virtual 属性/方法保持怀疑，优先选非虚等价物。
- **`StringTemplateId` 优先于 `TemplateId`（MongoID）**：1.1.5 的 `Item` 同时提供两者；`TemplateId` 的隐式 string 转换形态不可靠（实战根因：价格表查询全部 miss → 战利品 tracked=0）。取 id 一律 `StringTemplateId`，空则回退 `TemplateId.ToString()`。
- **SPT PMC bot 是 `side=Savage` + `role=pmcBEAR/pmcUSEC`**：任何按 `side` 分类的逻辑都要先看 `role`（否则 PMC 会被误归为 scav/boss 颜色）。
- **价格源可直读服务端数据文件**：5.0 下 ragfair 回调受封送限制、可选服务端 mod 可能缺失；`SPT_Runtime/SPT_Data/database/templates/prices.json`（templateId→价格，约 4.7k 条）可直读兜底（实战：命中后战利品过滤恢复，maxPrice 139k）。
- **bundle Unity 版本**：读 bundle header（`UnityFS` 后版本串）。实测 **2019.4 构建的 bundle 在 EFT 1.1.5（Unity 2022.3.43f2）可加载**（4.1x 同 Unity 2022.3 家族亦然）——仍以运行时验证为准。不兼容时的后备：用同版本 Unity Editor 重建（本机无 Editor）或代码构建 UI。
- **MO2 监视器竞态**：见 Phase 5 第 4 条。
- **usvfs 配置重定向**：游戏**新建**的配置文件落到 `<MO2实例>\overwrite\BepInEx\config\`；已存在的文件写回原路径。
- **部署时机**：游戏运行中 DLL 被锁定，覆盖层写入前先退出游戏。
- **会话获取路径可能漂移**：`Singleton<ClientApplication<IEftSession>>` 运行期失败时 try/catch 降级（价格/数据类功能退化为空值，不崩溃、不阻塞主线程）。
- **不要引用上游 `dependencies\*.dll`**（4.1 时代产物）——编译引用一律来自 SPT5 安装。
- **行为等价优先**：无法直译的机制（事件、协程）改等价实现并在 README 记录偏差。

## 5. 参考

- 类名映射：`docs/eft-1.1.5-类名映射重建报告.md`；类型清单：`knowledge/spt-kb/archive/eft-1.1.5/classes-1.1.5.txt`
- 客户端规范：`knowledge/spt-kb/curated/modding-standard/05-client.md` + `version-matrix.md`
- IL2CPP 陷阱实录：`docs/dev-log.md`（2026-09-14/15 条目）
- 样板实现：`tools/tarkov-runtime-bridge/`（BasePlugin / ClassInjector / Harmony / Singleton 全部模式）
- **完整工作示例（4.1→5.0 全流程）**：`mods/SPT5-AccurateCircularRadar/`（工程）+ `knowledge/spt-kb/archive/ported-src/RadarStandalone_1100_spt5_port/`（归档）+ `knowledge/spt-kb/archive/forge/mods/RadarStandalone_1100_source/`（上游 4.1.3）
- 归档约定：`knowledge/spt-kb/archive/ported-src/MANIFEST.md`
- 工具：`ilspycmd`（`C:\Users\Winde\.dotnet\tools\ilspycmd.exe`）
