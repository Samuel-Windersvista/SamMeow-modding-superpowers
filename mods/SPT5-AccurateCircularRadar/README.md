# SPT5-AccurateCircularRadar（Tyrian-Radar for SPT 5.0）

把 **Accurate Circular Radar**（上游 mod 名 `Tyrian-Radar`）从 SPT 4.1.3（Mono / BepInEx 5 / net48）
移植到 **SPT 5.0 / EFT 1.1.5 build 47242**（IL2CPP / BepInEx 6 / net6.0）。

- **上游作者**：Leonana69
- **上游仓库**：<https://github.com/Leonana69/Tyrian-Radar-Standalone>
- **上游版本**：1.3.4（Forge id 1100 "Accurate Circular Radar"）
- **上游许可证**：CC BY 3.0（见 `LICENSE.md`）
- **本移植 DLL 名**：`Tyrian-Radar.dll`（与上游一致）
- **本移植作者**：**SamMeow**（移植）；原作者 Leonana69
- **本移植版本号**：`1.3.4-spt5.1`（`PluginMetadata.Version`；程序集版本 `1.3.4.1`）

> 本项目是**移植副本**，不是上游的官方分支。上游代码按 CC BY 3.0 署名使用；移植引入的
> API 适配与降级改动在下方「移植偏差与降级记录」中逐条列出。

---

## 1. 目录结构

```
mods/SPT5-AccurateCircularRadar/
  SPT5AccurateCircularRadar.csproj    # AssemblyName=Tyrian-Radar / RootNamespace=Radar
  README.md                           # 本文件
  LICENSE.md                          # 上游归属 + CC BY 3.0
  MODDING-STD-WAIVER.md               # 对 2 条机检规则的显式豁免说明
  src/                                # 全部源码（namespace Radar，逻辑等价）
    RadarPlugin.cs                    # BasePlugin 入口（原 BaseUnityPlugin）
    PluginMetadata.cs                 # GUID com.leonana69.radar / Name "Accurate Circular Radar - SPT5" / Version 1.3.4-spt5.1
    RadarConfig.cs                    # 全部 BepInEx 设置 + 语言键集迁移（见实战修复记录 12）
    Locales.cs                        # 配置项多语言 + ResolveLanguage 语言归一化
    AssetFileManager.cs               # 内嵌 bundle / PNG 加载（IL2CPP 化）
    HaloRadar.cs                      # 每帧驱动 + 脉冲动画（协程 -> Update）
    InRaidRadarManager.cs             # 进 raid 后生成 HUD + 按键开关
    RadarHudLayout.cs                 # HUD 层级 / 罗盘模式
    PolygonGraphic.cs                 # 雷区填充多边形（Graphic 子类，ClassInjector）
    RadarRegion.cs                    # 雷区四边形投影
    Target.cs                         # blip 基类（世界 -> 雷达投影）
    BlipPlayer.cs                     # 玩家/PMC/SCAV/BOSS/BTR blip
    BlipOther.cs                      # 物资/地雷/撤离点/转移点 blip
    LootTracker.cs                    # 物资筛选 + 四叉树索引
    MinefieldTracker.cs               # 地雷与雷区轮廓
    ExfilTracker.cs                   # 撤离点与转移点
    Quadtree.cs                       # 点四叉树（纯托管，原样）
    ItemPricing.cs                    # 跳蚤/商人估价（会话与网络路径，含降级）
    Patches/GameStartPatch.cs         # GameWorld.OnGameStarted 后置补丁
    Patches/LootItemPatches.cs        # DictionaryListHydra<int,LootItem> Add/Remove 补丁
    Patches/PlayerPatch.cs            # Player.OnMakingShot 后置补丁
  bundle/                             # 上游内嵌资源（原样复制，25 个文件）
    radarhud.bundle                   #   Unity 2019.4 构建的 AssetBundle（未重编）
    *.png                             #   24 张 blip / HUD 贴图
```

## 2. 构建

```powershell
dotnet build "mods\SPT5-AccurateCircularRadar\SPT5AccurateCircularRadar.csproj" -c Release
```

产物：`mods\SPT5-AccurateCircularRadar\bin\Release\Tyrian-Radar.dll`（约 232 KB）。

安装根可覆盖（两个名字互为别名）：

```powershell
dotnet build <csproj> -c Release -p:SPTInstallPath="D:\Games\SPT"
dotnet build <csproj> -c Release -p:SPT5Path="D:\Games\SPT"
```

默认 `E:\Game\EFT_Offline\SPT_5xx`。

### 内嵌资源名（务必保持）

`RootNamespace=Radar` + 资源位于 `bundle\`，因此内嵌资源名自动为 `Radar.bundle.<file>`，
与 `AssetFileManager.ResourcePrefix = "Radar.bundle."` 匹配。构建后 DLL 内的 25 个资源名已核实：

```
Radar.bundle.radarhud.bundle
Radar.bundle.normal_enemy.png / _up.png / _down.png      （boss_enemy / dead_enemy / loot / btr / mine / exfiltraction 同构）
Radar.bundle.RadarBackground.png / RadarBorder.png / RadarPulse.png
```

## 3. 引用（全部 HintPath 到本地 SPT 安装，`Private=false`）

| 来源 | 程序集 |
|---|---|
| `BepInEx\core` | BepInEx.Core、BepInEx.Unity.IL2CPP、0Harmony、Il2CppInterop.Runtime |
| `BepInEx\interop` | Il2Cppmscorlib、Il2CppSystem、Assembly-CSharp、Comfort、PlayerEnums、UnityEngine.CoreModule、UnityEngine.UI、UnityEngine.UIModule、UnityEngine.ImageConversionModule、UnityEngine.AssetBundleModule、UnityEngine.PhysicsModule、Newtonsoft.Json |
| `BepInEx\plugins\sptushonka` | SPTushonka.Reflection、SPTushonka.Common |
| `BepInEx\plugins\sptushonka\ConfigurationManager` | BepInEx.KeyboardShortcut |

- `BepInEx.KeyboardShortcut.dll` 是必需的：BepInEx 6 把 `BepInEx.Configuration.KeyboardShortcut`
  从 Core 拆出，其**静态构造**会向 `TomlTypeConverter` 注册该类型的转换器；不引用则
  `ConfigEntry<KeyboardShortcut>` 无法绑定。该类型被标记 `[Obsolete]`（建议改用
  `BepInEx.Unity.IL2CPP.Configuration.KeyboardShortcut`），本移植**刻意保留旧类型**：
  它与随包分发的 ConfigurationManager（F12 菜单按键绑定 UI）是同一份类型。故 csproj 中
  `NoWarn` 关闭 `CS0618`。
- `Newtonsoft.Json` 保留在引用清单中以与上游一致，但**当前代码不再使用**（见偏差记录 7）。

## 4. 已核实的 4.1 -> 5.0 API 映射

| 4.1 | 5.0（本移植采用） |
|---|---|
| `BepInEx.BaseUnityPlugin` + `Awake` | `BepInEx.Unity.IL2CPP.BasePlugin` + `Load` / `Unload` |
| `SPT.Reflection.Patching.ModulePatch` | `SPTushonka.Reflection.Patching.ModulePatch`（API 相同，仅换 using） |
| `SPT.Common.Http.RequestHandler` | `SPTushonka.Common.Http.RequestHandler`（`HttpClient.PostAsync` -> `PostJsonAsync`） |
| `Comfort.Common.Singleton<T>` | 同（`Singleton<GameWorld>.Instantiated/Instance` 实测可用） |
| `GameWorld.OnGameStarted` / `MainPlayer` / `AllPlayersEverExisted` / `ItemOwners` / `LootItems` / `MineManager` | 同 |
| `Player.OnMakingShot(IWeapon, Vector3)` / `ProfileId` / `Transform` / `HealthController.IsAlive` | 同 |
| `EFT.Interactive.DictionaryListHydra<int,LootItem>` | **全局命名空间** `DictionaryListHydra<int,LootItem>` |
| `ClientAppUtils.GetMainApp().GetClientBackEndSession()` | `Singleton<ClientApplication<IEftSession>>.Instance.GetClientBackEndSession()` |
| `IEftSession.Traders` / `GetSupplyData(id)` | 同（`GetSupplyData` 返回 Il2Cpp `Task<Result<SupplyData>>`，可 await） |
| `Trader.Id` / `_supplyData` / `LocalizedName` / `CurrencyCourses` / `GetUserItemPrice` | 同；`ItemPrice.CurrencyId` 由 `string` 变为 `Nullable<MongoID>` |
| `EFT.Interactive.LocationScene` / `MineDirectionalManager` | **全局命名空间**（`LootItem`/`BorderZone`/`ExfiltrationPoint`/`TransitPoint` 仍在 `EFT.Interactive`） |

## 5. 移植偏差与降级记录

> 逐条对应任务书「重点适配项」。标注 **[等价]** 的是行为等价改写，**[降级]** 的是行为有可观察差异。

1. **插件入口** [等价] — `BaseUnityPlugin`+`Awake` 改为 `BasePlugin`+`Load`/`Unload`。
   `BasePlugin` 不是 MonoBehaviour，删除了 `gameObject` / `DontDestroyOnLoad` / `Destroy` 与
   `Instance != this` 单例守卫（BepInEx 6 每插件只 Load 一次）。保留静态 `Instance` 与 `Log`
   （`Log = base.Log`，`internal static new ManualLogSource Log`，`new` 用于隐藏 `BasePlugin.Log`）。

2. **补丁** [等价] — 仅把 `using SPT.Reflection.Patching;` 换成 `using SPTushonka.Reflection.Patching;`。
   补丁类与补丁体未改动；`GameStartPatch` 仍在 `Load()` 中 `Enable()`，`LootItem*` 与
   `PlayerOnMakingShot` 仍在 `InRaidRadarManager.Awake()` 中 `Enable()`（上游时机）。

3. **IL2CPP 注入** [等价] — `HaloRadar`、`InRaidRadarManager`、`PolygonGraphic` 在 `Load()` 中
   先 `ClassInjector.RegisterTypeInIl2Cpp<T>()`，并各加 `public X(IntPtr pointer) : base(pointer)`。
   当前无需 `[HideFromIl2Cpp]`（没有把托管类型暴露给 il2cpp 的方法；含托管参数的方法都是
   C# 侧内部调用）。

4. **会话获取** [等价+降级] — 按 4.1 `ClientAppUtils` 源码本体改写为
   `Singleton<ClientApplication<IEftSession>>.Instance.GetClientBackEndSession()`，整条路径
   try/catch，失败返回 `null`，调用方降级。

5. **`DictionaryListHydra` 泛型补丁** [等价，风险已隔离] — 采用直译
   `typeof(DictionaryListHydra<int, LootItem>).GetMethod("Add"/"Remove", …)`。
   `ModulePatch.Enable()` 自带 try/catch 并在目标方法解析失败时记 error 且不应用补丁，
   因此即使 Harmony 无法解析该泛型闭包也不会影响插件其余部分（只是失去中途新增/移除物资的
   blip 同步，`LootTracker` 的定期重扫仍然生效）。**未**改用轮询兜底，因为轮询会退化成全量重扫。
   运行期请重点观察日志中 `LootItemAddPatch: …` / `LootItemRemovePatch: …` 的 error 行。

6. **资源加载 IL2CPP 化** [等价] —
   `AssetBundle.LoadFromStream(Stream)` 在 interop 中要求 `Il2CppSystem.IO.Stream`（托管 Stream
   无法封送），改为 `AssetBundle.LoadFromMemory(byte[])`（interop 参数为 `Il2CppStructArray<byte>`，
   对 `byte[]` 有隐式转换）；`LoadAsset<T>` 泛型重载在 interop 中不存在，改为
   `LoadAsset(name).TryCast<GameObject>()`。`ImageConversion.LoadImage(Texture2D, byte[], bool)`
   同样依赖 `byte[]` -> `Il2CppStructArray<byte>` 隐式转换。
   `Assembly.GetExecutingAssembly().GetManifestResourceStream`、`Texture2D`、`Sprite.Create` 保持托管。

7. **JSON 请求体** [等价] — 上游用 `JsonConvert.SerializeObject(托管 POCO)`。IL2CPP 下
   `Newtonsoft.Json` 是游戏内程序集的代理，把**托管** POCO 交给原生序列化器不可行，
   故 `FleaPriceCache` 改为手写 `{"templateId":"<id>"}`（templateId 为十六进制 id，无需转义）。
   `Newtonsoft.Json` 引用因此不再被使用（保留仅为与上游引用清单一致）。

8. **协程改造** [等价] —
   - `HaloRadar.PulseCoroutine` -> `TickPulse()`，在 `Update` 中按 `Time.deltaTime / _pulseInterval`
     推进，角度仍为 `(1 - t) * 360`，每 `_pulseInterval` 秒一圈。
   - `ItemPricing.InitCoroutine` -> 托管 `async Task InitAsync()`（`_ = InitAsync()`）。
     `await` Il2Cpp `Task<T>` 可行：Il2CppInterop 生成的 `TaskAwaiter<T>` 实现了托管
     `System.Runtime.CompilerServices.INotifyCompletion`。

9. **容器增删事件 -> 轮询** [降级] — **5.0 interop 的 `IItemOwner` 接口没有暴露
   `AddItemEvent` / `RemoveItemEvent`**（事件访问器只存在于其内部 `Il2CppProxy` 上且为 private，
   编译期无法访问），无法按上游方式订阅容器内容变化。改为在 `LootTracker.Tick()`（扫描间隔）
   中重估**已跟踪容器里落在 `OuterRange` 内**的那些：容器要出现在雷达上本来就必须在
   `OuterRange` 内，因此对**可见行为等价**；差异是
   - 超出外圈的容器即使内容变化也不刷新 blip（它本来也不显示，玩家接近后会在一个扫描间隔内补上）；
   - 新增容器（如中途落地的空投箱）在容器变为"值得显示"时最迟一个扫描间隔后出现。
   代价：每个扫描间隔对已跟踪容器各读一次 `transform.position`（不做全量估价，只有范围内的才估价）。

10. **商人/跳蚤估价** [降级] —
    - `RagfairGetPrices`：用显式 `System.Action<Result<Il2CppSystem.Collections.Generic.Dictionary<string,float>>>`
      变量经 `Callback<T>` 的隐式转换订阅（lambda 不能直接走用户自定义转换）。回调参数
      `Result<T>` 是 Il2Cpp 值类型，若运行期封送被拒，异常被 try/catch 吞掉，`_fleaPrices` 保持
      `null`，只保留 LootValue HTTP 价格。
    - 商人报价：`Traders` 枚举 + `GetUserItemPrice`；`ItemPrice.CurrencyId` 在 5.0 是
      `Nullable<MongoID>`（4.1 为 string），需 `HasValue`/`Value` 显式转换后再查 `CurrencyCourses`。
      `GetSupplyData` 失败时 `_supplyData` 为 null，`GetUserItemPrice` 返回空 -> 该商人报价按 0 计。
    - 所有会话/网络路径均 try/catch，失败退化为 `-1`（跳蚤）/ `0`（商人），**不崩溃、不阻塞主线程**。
    - 上游的 `TraderPrices`（按物品名缓存商人报价）与 `FleaPriceCache`（300 秒 TTL + 后台探测）
      策略保持。

11. **KeyboardShortcut** [等价] — 先核实 `BepInEx.KeyboardShortcut.dll` 的类型确为
    `BepInEx.Configuration.KeyboardShortcut` 且 `IsDown()` 可用，故保留原代码（按键配置仍是
    `ConfigEntry<KeyboardShortcut>`，未退化为 `KeyCode` 轮询）。

12. **Color 配置转换器** [新增，必要] — BepInEx 6 的 `TomlTypeConverter` 只内置基元与枚举，
    **没有 `UnityEngine.Color`**，上游的 `ConfigEntry<Color>` 在 5.0 下会在写配置时抛
    `InvalidOperationException`。故 `RadarPlugin.Load()` 在 `RadarConfig.Bind` **之前**调用
    `TomlTypeConverter.AddConverter(typeof(Color), …)`，格式为 `"r g b a"`（与 ConfigurationManager
    的 Color 显示格式一致，兼容 1/3/4 分量解析）。若已有转换器则本调用被忽略（`AddConverter` 返回 false）。

13. **对象名** [保持原样，待运行期验证] — `GameObject.Find("FPS Camera")`、
    `GameObject.Find("compas_glass_LOD0")` 未改动。

14. **Bundle** [保持原样] — 直接复用上游 Unity 2019.4 构建的 `radarhud.bundle` 与 PNG，
    未改动 Unity 工程（本机无 Unity Editor）。

15. **PluginMetadata** [按 Overseer 要求调整] — GUID `com.leonana69.radar` 保持不变（配置文件连续）；
    Name 改为 `Accurate Circular Radar - SPT5`；Version 改为 `1.3.4-spt5.1`（上游 1.3.4 + 移植修订）；
    新增 `Author` 常量（启动日志输出移植署名）；csproj `<Version>` 同步为 `1.3.4-spt5.1`。

### 实战修复记录（live 驱动，2026-09-15/16）

> 首次实机验证暴露的缺陷与修复，按发现顺序；全部已实机复验。

1. **bundle 资产生命周期** [修复，必要] — 上游把 `AssetBundle` 只放在局部变量里；IL2CPP 下无引用链的
   bundle 资产会在场景切换/资产清理时失效，`Instantiate(prefab)` 抛 `NullReferenceException`。
   修复：`_bundle` 静态持有 + 加载出的 prefab/贴图标 `HideFlags.DontUnloadUnusedAsset` +
   `LoadAsset` 失败时按资产名扫描兜底 + 战局内实例化加护栏与诊断日志。
2. **失败路径不得 `Destroy(gameObject)`** [修复，致命] — 本组件挂在 **GameWorld 的 GameObject** 上
   （`GameStartPatch` 的 `__instance.gameObject.AddComponent<T>()`）；上游失败路径的
   `Destroy(gameObject)` 会**摧毁游戏世界对象**，导致局内移动/武器/交互连锁崩坏。
   修复：全部失败路径改 `Destroy(this)`（只移除组件）。
3. **补丁体异常护栏** [修复，致命] — `LootItemRemovePatch` 前置钩子对不存在的 key 调 `GetByKey`
   抛 `KeyNotFoundException`（1.1.5 实现为 `Dictionary.get_Item`），异常穿透补丁进入游戏代码导致闪退。
   修复：`RemoveByKey` 先 `ContainsKey` 再取值；**全部 4 个补丁体加 try/catch**（补丁绝不向游戏代码抛异常）。
4. **`TrackableTransform` → `Component.transform`** [修复，致命] — `TrackableTransform` 是 virtual
   属性，interop 需运行时解析虚方法（`il2cpp_object_get_virtual_method`）；实测该解析返回坏指针，
   `il2cpp_runtime_invoke` 直接 **AccessViolation（0xc0000005，faulting module `coreclr.dll`）** —
   进程级崩溃且**不可 try/catch**。修复：改用非虚的 `Component.transform`。
5. **F12 ConfigurationManager ComboBox 崩溃** [规避] — `Language` 项用 `AcceptableValueList`，
   CM 以 ComboBox 渲染，而 ComboBox 依赖被 IL2CPP 剥离的 `GUI.DoButtonGrid` →
   `NotSupportedException` 每帧刷屏（单会话 6,477 + 6,421 条）+ GUIClip 失衡。修复：改纯文本输入
   （非法值回退 EN）；未安装 LootValue 时跳过其探测（消除 404 报错）。
6. **价格源修复（prices.json 直读）** [新增，必要] — 5.0 下 ragfair 回调因 `Result<T>` 非 blittable
   被拒、LootValue 为可选；新增本地价格源：直读
   `SPT_Runtime/SPT_Data/database/templates/prices.json`（4,719 条，与服务端下发数据同源）。
   同时 `TemplateId` 取值改为优先 `StringTemplateId`（与桥同款）——**这是价格表命中的关键**
   （旧的隐式转换形态导致全部查询 miss，`tracked=0`）。
7. **SPT PMC bot 颜色** [修复] — SPT 的 PMC bot 是 `side=Savage` + `role=pmcBEAR/pmcUSEC`，
   上游按 side 取色会让它们落入「boss」默认分支渲染成红色。修复：按 role 给回 PMC 颜色。
8. **商人价缓存修正** [修复] — 失败结果（0）此前不缓存，导致每件物品每次估价都遍历全部商人；
   改为按 `TemplateId` 缓存（含失败值，初始化完成后生效）。
9. **中文本地化（Overseer 要求）** — F12 界面中文化：分区名（基础/高级/颜色/界面设置）+
   键名/描述经 `Locales` 的 ZH 词表；默认 `Language=ZH`；补翻上游漏翻的「愿望单」条目。
10. **诊断日志** — `Loot scan: owners/tracked/maxPrice/skipped[…]` + `Loot sample: …` +
    `Local flea price table loaded: N entries.`（每次 Rebuild 输出，供实机定位）。
11. **价格源隔离 + 商人价熔断** [修复，必要] — 商人供货数据异步就绪后 `GetUserItemPrice` 抛异常，
    被 `GetBestPrice` 的外层 catch 吞掉 → 整个估价退化为 `-1`，已命中的本地价格表结果一并被丢弃
    （`tracked=0`；此时调阈值无效，因为根本没有价格参与比较）。
    修复：`GetBestPrice` 内三个价格源（跳蚤 / 商人 / 手册价）各自独立 try/catch，单一来源失败不毒化整价；
    `GetBestTraderPrice` 首次异常即熔断（`Trader pricing disabled after failure: …`），本会话不再走商人价，
    本地价格表继续供价。
    复验（2026-09-16 Sandbox 局）：`Loot scan: owners=1067, tracked=69, maxPrice=139000, threshold=30000`
    （修复前 `tracked=0`）。
12. **语言切换的键名迁移（方案 B：保留切换 + 重启生效 + 设置值自动迁移）** [修复，必要] —
    配置键名是「翻译后的字符串」，在插件加载时按 `Language` 值一次性确定：语言一变就生成全新键集，
    旧键成为孤儿，值分裂（实测 cfg 中 EN + ZH 两套键并存；F12 只显示当前绑定键集，**孤儿键不可见** →
    表现为「F12 菜单变英文」）。
    修复：
    - 标记文件 `<配置目录>/com.leonana69.radar.lang`（内容 = 上次绑定键集的语言，如 `ZH`），
      `RadarConfig.Bind()` 结束时写入；所有 I/O 失败静默降级为 Warning，绝不影响加载。
    - 启动时读标记：与本次有效语言（`Locales.ResolveLanguage`，非法值回退 EN）不同 → 用**独立
      `ConfigFile(configFilePath, saveOnInit: false)`**（不挂到插件、不写盘、不污染 F12 菜单）按旧语言
      键名逐个读回 33 个设置的值，赋给当前已绑定的条目，再 `config.Save()` 持久化，最后更新标记。
      标记缺失 / 损坏 → 跳过迁移（只补写标记，避免误搬）；`Language` 条目自身不迁移。
    - 因此**改动 `Language` 后需重启游戏生效**（配置项描述已注明）；F12 中改语言不会即时重建键名。
    - 一次性清理历史遗留的双键集：`D:\Temp\opencode\radar-cfg-cleanup.mjs`（默认写盘，`--dry-run` 预览）
      把 EN / ZH 两套键合并为 ZH 键集（值取最近使用的 EN 键集），并写好标记文件；须在**游戏完全关闭**后运行。
      注：插件自身的迁移**不会**删除旧键（实测 BepInEx 的 `ConfigFile.Save()` 保留孤儿键），
      故首次升级到本版本时若不同时跑清理脚本，历史 cfg 里「最近使用的一套（EN 键）值」会被忽略、
      回落到较旧的 ZH 键值——迁移只保证「本次生效语言切换」不丢值。

### 其它纯 IL2CPP 改写（编译期强制，非可选）

| 位置 | 4.1 | 5.0 写法 | 原因 |
|---|---|---|---|
| `Target` / `RadarHudLayout` | `(RectTransform)transform.Find(...)` | `.TryCast<RectTransform>()` | interop 的 `Find` 按声明类型（Transform）包装代理，`is`/强转恒失败 |
| `PolygonGraphic` | `mesh.SetVertices/SetTriangles/SetColors(托管 List<T>)` | `mesh.vertices/triangles/colors32 = T[]` | interop 只接受 `Il2CppSystem.Collections.Generic.List<T>`；属性版接受 `Il2CppStructArray<T>`（`T[]` 隐式转换） |
| `PolygonGraphic` | `protected override OnPopulateMesh(Mesh)` | `public override` | interop 把该成员生成为 `public`，重写不能收窄访问性 |
| `RadarRegion` | `new GameObject(name, typeof(RectTransform), …)` | `new GameObject(name)` + 逐个 `AddComponent<T>()` | interop 的 ctor 参数是 `Il2CppReferenceArray<Il2CppSystem.Type>` |
| `BlipPlayer` | `blipImage.GetOrAddComponent<Outline>()` | `GetComponent<Outline>() ?? AddComponent<Outline>()` | `GetOrAddComponent` 扩展在 5.0 interop 下不可靠 |
| `MinefieldTracker` | `zone.GetType().Name == "Minefield"` | `zone.TryCast<Minefield>() != null` | `GetAllObjects<BorderZone>()` 返回的代理按声明类型包装，`GetType()` 恒为 BorderZone；`TryCast` 走原生类型判定 |
| `MinefieldTracker` | `BorderZone._triggerZoneSettings` / `_extents` 反射取值 | 直接字段访问 | interop 已把这两个字段生成为 public；反射对 Il2Cpp 代理对象不可靠 |
| `LootTracker` / `HaloRadar` / `ExfilTracker` / `ItemPricing` | 托管 LINQ（`Reverse`/`First`/`Count`/`ToArray`/`OrderByDescending`） | 显式循环 | Il2Cpp 集合只实现 `Il2CppSystem.Collections.Generic` 接口，托管 LINQ 不适用 |

## 6. 运行期验证结果（2026-09-16 实机 + 复测）

原「待验证清单」已实测（MO2 覆盖层 `界面-AccurateCircularRadar-1.3.4-spt5.1`，Sandbox 局）：

| 项 | 结果 |
|---|---|
| 插件加载 / 内嵌 bundle | ✓ `Radar prefabs loaded.` + `Radar assets loaded.`（2019.4 bundle 在 Unity 2022.3.43f2 可用） |
| HUD 生成 | ✓ `Radar instantiated` + `Radar loaded`；HUD 可见（圆盘/边框/刻度环/扫描扇区/玩家标记） |
| 敌人 blip | ✓ scav 绿 / boss 红 / 尸体方块；PMC 修复后为 USEC 黄 / BEAR 橙 |
| 战利品 blip | ✓ `Loot scan: owners=1067, tracked=69, maxPrice=139000, threshold=30000`（价格源隔离修复后；修复前 `tracked=0`）；目视确认高价值物品显示 |
| 估价链路 | ✓ `Local flea price table loaded: 4719 entries.`；商人价首次失败即熔断（Warning，本地价格表继续供价）；ragfair 回调按预期不可用（5.0 已知拒绝形态，1 条 Warning）；LootValue 未安装时探测跳过（无 404） |
| F12 中文界面 | ✓ 分区/键名/描述中文；无报错刷屏 |
| F12 阈值即时性 | ✓ 阈值 30000 → 55634 / 54718 / … / 52887 连续改动，`tracked` 69 → 17，每次改动即触发 Rebuild（`Loot scan: … threshold=<新值>`） |
| 稳定性 | ✓ 无闪退（TrackableTransform 修复后战局全程无 CTD）；F12 无异常刷屏（唯一 ConfigurationManager 匹配为 Il2CppInterop Info 注册行） |
| 罗盘模式 / 雷区轮廓 / 容器轮询 | 未专项实测（默认关闭 / 低风险，按代码路径实现） |

复测关键日志（2026-09-16，Sandbox 局）：

```
Local flea price table loaded: 4719 entries.
Loot scan: owners=1067, tracked=69, maxPrice=139000, threshold=30000
Trader pricing disabled after failure: Object reference not set to an instance of an object.   (Warning，正常：本地价格表继续供价)
Ragfair price table unavailable: Delegate has parameter of type Comfort.Common.Result…        (5.0 已知拒绝形态)
```

## 7. 遗留风险

| 风险 | 影响 | 说明 |
|---|---|---|
| `DictionaryListHydra<int,LootItem>` 泛型闭包补丁 | 中途新增/移除的世界物资 blip 不同步 | `ModulePatch` 会记 error 并跳过；`LootTracker.Tick` 的定期重扫仍会纠正范围内物资。实机未专项验证该补丁的生效性 |
| 容器内容变化改为轮询 | 外圈容器变化不刷新；新容器最迟一个扫描间隔出现 | 对可见行为等价（外圈不绘制），见偏差 9 |
| ~~ragfair 回调封送~~ | 已被 prices.json 直读取代 | 回调不可用已实机证实；本地价格表 4,719 条命中（验证记录见 §6） |
| `GetSupplyData` 可用性 | 商人报价可能退化为 0 | 已被 prices.json 覆盖；失败不阻塞 |
| prices.json 直读耦合 SPT 数据布局 | SPT 改路径/格式时估价退化 | 路径 `SPT_Runtime/SPT_Data/database/templates/prices.json`；缺失时回退手册价（通常为 0） |
| Color 转换器格式 | 与未来其它插件的 Color 格式不一致 | 已按 ConfigurationManager 显示格式 `r g b a`；若已被其它插件注册则沿用对方的 |
| `TryCast<Minefield>()` | 雷区轮廓可能全部丢失 | 依赖 interop 生成 `EFT.Interactive.Minefield`（已确认存在）；失败只记 warning |
| `Graphic.OnPopulateMesh` 重写 | 雷区填充不渲染 | 已按 interop 的 `public` 签名重写；注入类型的虚方法回调未专项实测 |
| ~~上游 bundle 为 Unity 2019.4 构建~~ | 已验证兼容 | 2019.4 bundle 在 EFT 1.1.5（Unity 2022.3.43f2）实机加载成功（prefab + 贴图） |
