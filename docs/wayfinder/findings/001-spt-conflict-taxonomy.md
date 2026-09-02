# SPT 4.1 Mod 冲突表面分类学

> 状态:侦察完成(2026-08-02)| 适用版本:[4.1]| 目的:wayfinder 票 #1 产出物
> 素材:api-notes-4.1/(6 篇)、curated/recipes/(11 篇)、modding-guide/、wiki/(SPT_41 迁移文档、EnumExtensions、Mod_Types、Installing_Mods)、E-Mod开发示例/server-mod-examples/、A-核心服务端/modules/、archive/forge/(hot-index.json、api/、mods/)

---

## 1. 服务器模组冲突表面(Server Mod)

### 1.1 表注入(Table Injection)

**机制**:4.1 移除 `DatabaseServer/DatabaseService`,10 张表(`GlobalTable/BotTable/HideoutTable/LocaleTable/LocationTable/MatchTable/TemplateTable/TradersTable/ServerTable/SettingsTable`)全部是 DI 单例,构造函数直接注入。数据源 = 服务器启动时从 `SPT_Data/Server/database/` 加载的内存对象。

**冲突形态**:

| 冲突 | 具体表现 | 例子 |
|------|---------|------|
| 同 key 覆写 | `Dictionary` 直接赋值/`AddOrUpdate`,后加载者胜 | 两 mod 都改 `templateTable.Items[itemId]`;`botTable.Types["assault"].FirstNames` |
| 新增 key 撞车 | 两 mod 用同一 MongoId 新增物品/商人/任务,后者静默覆盖前者 | `CustomItemService.CreateItemFromClone` 的 `NewId` 撞车(24 位 hex 撞车概率低,但手抄 ID 可能) |
| 权重调整叠加 | 相对权重,数值互相干扰而非覆盖 | `LootConfig.StaticItemWeightAdjustment`、bot 装备 `AddOrUpdate(..., 999999)` 垄断 |
| 列表清空/追加 | 一个 mod 清空、另一个追加,顺序决定结果 | `assaultBot.FirstNames.Clear()` vs `.Add()`(示例 2) |
| Locale 表直改 | `LocaleTable.Global` 懒加载,直接改**不保存**;必须走 `LocaleService` | 两 mod 同时改同一 locale key,后者胜;走服务则可能按注入顺序叠加 |

**关键事实**:表是**共享单例对象**,mod 之间不是隔离的——所有修改是对同一内存对象的 in-place 变更。`TypePriority` 决定谁最后写,后写者胜(配方 04:「多个 mod 改同一物品时,TypePriority 决定覆盖顺序——后加载者胜」)。

**失败模式**:`TryGetValue`/`FirstOrDefault` 判空失败 → null 引用异常(示例 2 注释明示);物品 ID 抄错 = 静默无效(配方 02/04)。

### 1.2 路由/Handler(Route / Handler)

**机制**:两类端点两套体系——游戏流量走 **Router**(`AbstractRouter` 子类),网页/工具流量走 **MVC Controller**。路由通过 `IEnumerable<AbstractRouter>` DI 多实例注入,按 `TypePriority` 排序(`OnLoadOrder.Routers` 为基准;内置路由全部坐在 `Routers` 上)。

**冲突规则(迁移文档+笔记实读确认)**:

- **两 mod 覆盖同一路由 = 先注册者胜,无警告**(配方 09 坑、http-routing.md、迁移文档「whoever ended up first」)。排序 = `TypePriority` 升序,同值按 ModGuid 字母序。
- 覆盖 SPT 路由:`Routers - 1`(你优先)vs `Routers + 1`(SPT 优先)。
- **body 类型不匹配 → 运行时异常**(命名路由 + 两个类型),而非静默 null(4.1 强类型 `RouteAction<TRequest> : IRequestData`)。
- Item Event 路由:4.1 改构造器传 `ItemRouteAction<T>` 列表,无 switch、无 `BaseInteractionRequestDataConverter`。同样先注册者胜。
- Web 页面:`wwwroot/` 静态资源**两 mod 撞 URL = 启动硬失败**(配方 11)。

### 1.3 配置(Config)

**机制**:`ConfigLoader`(static)扫 `SPT_Data/configs/*.json|.jsonc` → `IReadOnlyDictionary<Type, BaseConfig>`(**类型为 key**)→ DI 单例,按具体类型注入。`ConfigServer.GetConfig<T>()` 已移除。

**冲突形态**:

| 冲突 | 表现 |
|------|------|
| 类型未映射配置 | 注入解析失败 → **启动即炸**(不是运行时) |
| 同类型注入 | 所有 mod 拿同一单例实例,互相 in-place 改;后加载者(高 TypePriority)覆盖 |
| 字典 key 撞车 | `RagfairConfig.Traders.TryAdd`(try 语义,先到先得);`LootConfig` 的各 Dictionary 直接 `[key]=`(后到覆盖) |
| mod 自建配置撞类型 | 两 mod 各用 `IOnDIConstruct` 注册同名同命名空间的 config 类 → 容器重复注册,后者覆盖前者 |
| 误加 `[Injectable]` | config 类被容器用默认值重建,**JSON 永不加载**(配方 11 坑) |

**注意**:SPT 自身配置**每类型一文件**,mod 无法在文件层撞 SPT 配置;冲突全发生在共享单例的内存变更层(同 1.1)。DEBUG 构建未知字段报错(`JsonUnmappedMemberHandling.Disallow`)。

### 1.4 DI 服务注册(DI Service Registration)

**机制**:底层 MSDI;`[Injectable(InjectionType, typePriority)]` 反射扫描各程序集;注册具体类 + **非 System 接口 + 基类**(递归)+ 泛型。默认 `Transient`、`typePriority = int.MaxValue`(极晚加载)。`DependencyInjectionHandler` 一次性使用,注册后再调抛异常。

**冲突形态**:

- **构造参数无法解析 → 容器构建失败,启动即炸**(非运行时 null)——这是 4.1 最硬的冲突失败模式。
- **循环依赖**(A 注入 B, B 注入 A)→ 容器解析失败,启动即炸(配方 10 坑)。
- 接口/基类被多实现注册:MSDI 下 `IEnumerable<T>` 拿到全部实现(`IEnumerable<IRuntimePatch>`、`IEnumerable<IProfileMigration>` 就是这么用的);单实例注入 `T` 时**最后一个注册者胜**(MSDI 语义:后 Add 覆盖先 Add,同类型单例)。
- 两 mod 声明同 fully-qualified 类名 + 同接口 → 静默重复注册,行为不可预期。
- `[Injectable]` 用 `HostedService` 但没实现 `IHostedService` → 抛 `ArgumentException`。

**跨 mod 共享(官方通道)**:A mod 的 `[Injectable]` 类可被 B mod 构造注入(配方 10)——这是 4.1 唯一官方 mod 间通信机制。共享类必须 `InjectionType.Singleton`,否则每处注入新实例(数据不同步,最常见错误)。

### 1.5 加载顺序(Load Order)

**机制**(mod-loading.md 源码实读):

```
RunModLoader()
├─ prepatch 阶段(user/patchers/ 读 enum 定义 → 内存补丁 → 新进程跑 Patched Core)
└─ LoadMods(): 遍历 user/mods/<ModGuid>/ 每目录一个 DLL
    └─ modValidator.ValidateMods()  # 版本兼容/依赖/冲突校验
```

- **顺序**:先 `TypePriority` 升序,同值按 **ModGuid 字母序**(tiebreaker,IModMetadata 注释确认)。
- **阶段常量**(低→高,间隔 100000):`Watermark → Preload → GameCallbacks → TraderRegistration → Routers → HandbookCallbacks → SaveCallbacks → TraderCallbacks → PresetCallbacks → RagfairCallbacks → PostLoad`。
- 数据库/配置文件在一切之前已加载;往库加数据(物品/商人/任务)必须在 `Preload`(避免 profile 因物品不存在加载失败)。
- **元数据校验**(ModValidator):版本 = SemanticVersioning 三段式(`"1.0.0.0"` 四段式非法);`SptVersion` 用 Range(如 `~4.1.0`);`ModDependencies: Dictionary<string, Range>`(按 **mod 名**→版本范围);`Incompatibilities: List<string>`(按 **mod 名**,示例 13 用 `["ReadJsonConfigExample"]`)。
- `IProfileMigration`(存档迁移管线,`IEnumerable` 注入)异常 → profile 无法加载,抛 `InvalidOperationException`。
- 服务器检查 mod 构建基于的 `SPTarkov.Server.Core` 版本,**不匹配拒绝加载**(迁移文档开头警告)。

**注意**:`Incompatibilities`/`ModDependencies` 匹配的是 **Mod Name 字符串**(不是 GUID)——脆弱:名称改了即失配,且大小写敏感、无版本匹配(4.0 语义,4.1 待源码确认是否升级)。

---

## 2. 客户端模组冲突表面(Client Mod)

### 2.1 BepInEx 插件结构

**机制**:BepInEx 5.4.23.2 x64(modules README);插件 = `BepInEx/plugins/<mod>/<dll>`,`[BepInPlugin(GUID, Name, Version)]` + `BaseUnityPlugin`,`Awake()` 里逐个 `new Patch().Enable()`。官方模块输出到 `BepInEx/plugins/spt/`(spt-common/reflection/core/custom/singleplayer),由 prepatcher **启动时校验存在性**,缺一个即报错退出(SptPrePatcher.ValidateSptPlugins)。

**冲突形态**:

- **BepInPlugin GUID 重复** → BepInEx 加载冲突(第二个插件报错/被拒)。
- **BepInDependency 缺失** → 依赖插件未加载,加载失败。
- `spt-prepatch.dll`(`BepInEx/patchers/`)是 SPT 官方文件,卸载 mod 时误删 = 游戏起不来(wiki/指南坑)。
- 4.1 客户端反混淆:4.0 的 `GClass680` 等混淆名全改真名;任何引用游戏类型的客户端 mod **必须用 4.1 程序集重编译**,否则直接无法加载(Client_40_to_41.md 警告)——这是版本层面的硬冲突。

### 2.2 Harmony 补丁

**机制**:SPT 用 `ModulePatch`(SPT.Reflection.Patching)+ 每补丁独立 `Harmony` 实例(HarmonyId = 类名);`GetTargetMethod()` 反射解析目标(4.0 靠字段特征找混淆类型,如 ScavProfileLoadPatch 用 `GetField("timeAndWeather")` 特征匹配);Prefix/Postfix/Transpiler/Finalizer 按特性收集。

**冲突形态**:

- **两插件 patch 同一方法**:Harmony 2 按补丁应用顺序链式执行。多个 Prefix 依次跑,任一返回 false 则跳过原方法;**多个 Transpiler 按序叠加**(后一个看到前一个的输出 IL,可能已不匹配预期模式 → 找 `searchCode` 失败的 Transpiler 静默返回原指令,如 ScavProfileLoadPatch 的 `if (searchIndex == -1) return instructions;`)。
- **目标解析失败** → `PatchException`(命名目标方法)。
- **SPT 官方插件内一个 patch 抛异常 → 后续 patch 全部不加载**(SPTCustomPlugin 的 try/catch:报错弹窗 + `Application.Quit()`)。第三方插件行为类似(BepInEx 捕获)。
- 4.1 服务器侧 patch 由 DI 管理(`IEnumerable<IRuntimePatch>` 在 Preload 启用),`Enable()/Disable()` 有**所有权检查**(`IsYourPatch`,非创建者程序集调用静默 no-op)。
- `ServiceLocator` 移除:依赖走构造器 + static 字段(4.1);两 patch 若都依赖解析同一被移除类型 → 启动失败。

**反模式预警**(来自 modules 源码):客户端 patch 直接引用 `Assembly-CSharp` 内部类型名,一旦 EFT/SPT 改名即失效——这是 IL 层最易碎的冲突面。

### 2.3 文件覆盖(File Overwrite)

**机制**:mod 以 `SPT/`、`BepInEx/` 目录结构解压进游戏根目录;Installing_Mods wiki 明示:同名文件夹/文件拖入会**合并并覆盖重复文件,不删除非重复文件**。

**冲突形态**:

| 冲突 | 表现 |
|------|------|
| 同名 DLL 覆盖 | 两个 mod 各带同名程序集(尤其内置依赖如 `Harmony.dll`/`Newtonsoft.Json.dll`)→ 后者覆盖前者,版本可能不兼容 |
| 同名插件 DLL | 两 mod 输出同名插件文件 → 覆盖,一个彻底消失 |
| `bundles.json`/bundle 文件撞名 | 服务器按 `bundles.json` 自动检测 bundle;bundle key 撞车 → 覆盖/加载错乱 |
| config 文件同名 | `user/mods/<guid>/config.json` 各 mod 目录隔离(靠 GUID 目录),但**非标准目录结构**的 mod 可能共用路径 |

### 2.4 配置文件(BepInEx .cfg)

- BepInEx 配置 = `BepInEx/config/<PluginGUID>.cfg`,**按 GUID 一插件一文件** → 只要 GUID 唯一,文件层天然隔离。
- 文件内按 `[Section]` + `Key = Value`,`ConfigEntry<T>` 绑定。
- 冲突仅在 **GUID 重复**(两插件共享 .cfg 文件)或**手改语法错误**(Notepad++ 校验,非 mod 冲突)。
- F12 菜单 = ConfigurationManager(可选组件,README 标注),与 .cfg 文件层并存;`QuestingBotsPluginConfig` 等社区 mod 也用 BepInEx.Configuration。

---

## 3. 跨层冲突(Cross-Layer)

### 3.1 服务端-客户端配对

已知双组件模组(forge hot-index + 源码核实):**SAIN**(SAIN 客户端 + SAINServerMod)、**Questing Bots**(Client + Server 双工程)、**Looting Bots**(客户端 + LootingBotsServerMod)、**UIFixes**(客户端 + .Server 工程)、**Custom Raid Times**、**QCAdjustments**、**AllQuestsCheckmarks** 等。forge `mods/SAIN-Solarint-s-AI-Modifications-Full-AI-Combat-System-Replacement_791_source/` 与 `Questing-Bots_1109_source/` 目录直接展示双工程结构。

**配对机制与冲突**:

- **枚举扩展(4.1 新流程)**:服务端 mod 经 `ClientEnumDefinitions` 注册 → 客户端内建 prepatcher 启动时请求 `/singleplayer/customEnumEntries` → 写入 `Assembly-CSharp`。**服务端必须先启动并注册完毕**,否则客户端插件 `Enum.TryParse` 失败,功能缺失甚至报错。
- **配对校验**:Questing Bots 服务端 `ClientLibraryExistsTest` 按相对路径检查 `BepInEx/plugins/<ModName>/<ModName>-Client.dll` 是否存在(ModIntegrityTests)——缺客户端 = 服务端主动报错。
- **运行时互操作检测**:客户端 `Type.GetType("LootingBots.External, skwizzy.LootingBots")` / `Type.GetType("SAIN.Interop.SAINExternal, SAIN")`(BepInEx.Bootstrap)反射探测第三方 mod,按需开互操作层。
- **硬编码互斥**(Questing Bots 更新日志实证):检测到 Performance Improvements 0.2.1-0.2.3 / Please Just Fight → 游戏+服务端持续报错;检测到 Reality / ABPS / Unda / Phobos / ORBIT → **自动禁用自身生成系统**。这类互斥是**运行时主动互操作**,不在元数据层声明。

### 3.2 版本耦合(Version Coupling)

| 层面 | 机制 | 失配后果 |
|------|------|---------|
| 服务端 ↔ SPTarkov.Server.Core | 服务器校验 mod 构建版本,不匹配**拒绝加载**(迁移文档) | 服务端启动即报错/拒绝 |
| 客户端 ↔ Assembly-CSharp | 4.1 反混淆,4.0 编译的 mod **无法加载** | 插件加载失败 |
| 服务端 ↔ 客户端 mod 版本 | Questing Bots:服务端 SPT 版本范围 → 客户端读取该范围设定自身要求(更新日志 0.11.0);配对 DLL 版本需一致 | 功能行为不一致 |
| 服务端 ↔ 依赖 mod 版本 | Questing Bots 要求 BigBrain >= 1.4.0、Waypoints >= 1.8.2、SAIN 特定版本,低于则**运行时弹错** | 报错/降级行为 |
| 枚举值跨端 | 服务端/客户端各自注册,名字可不同但**数值必须一致**(EnumExtensions.md);数值不匹配 = 一侧序列化正确另一侧解释成别的值 | **静默数据损坏** |
| forge 元数据 | hot-index.json `best_spt` 字段(如 `~4.0.0`/`>=4.0 <4.1.0`)、`fika` 布尔、`fika_compatibility`(versions.json) | 选错版本 = 拒绝加载或运行时崩 |

**profile 耦合**(wiki Mod_Types/Installing_Mods):加商人/任务/物品类 mod 可加入现有 profile;**移除可能使 profile 无法加载**(profile 引用已不存在的 mod 物品/商人 ID)。SPT 的 ProfileFixer 尽力修复但不保证——这是**数据层跨会话冲突**。

---

## 4. 冲突严重度分类

图例:B=Breaking(崩溃/核心功能失败) S=Silent corruption(数据错但无崩溃) O=Override(一方赢一方功能丢失) C=Compatible(知晓共存)

| # | 冲突类型 | 层级 | 严重度 | 说明 |
|---|---------|------|--------|------|
| 1 | 构造参数解析失败 / 循环依赖 / 类型未映射配置 | 服务端 DI | **B** | 容器构建失败,启动即炸 |
| 2 | 两 mod 同路由(先注册者胜) | 服务端路由 | **O** | 后者功能静默失效,无警告 |
| 3 | 路由 body 类型不匹配 | 服务端路由 | **B**(该请求) | 异常命名路由+双类型,请求失败 |
| 4 | wwwroot URL 撞车 | 服务端 Web | **B** | 启动硬失败(配方 11) |
| 5 | 同表同 key 修改(后加载者胜) | 服务端表 | **O** | 后者覆盖;可预期(靠 TypePriority 排序) |
| 6 | 新增 MongoId 撞车 | 服务端表 | **O/S** | 后者覆盖(物品/商人/任务);任务 ID 重复时 CreateQuest 静默返回 Errors(配方 06) |
| 7 | Locale 直改 `Global`(不保存) | 服务端表 | **S** | 改动不落盘;必须 LocaleService |
| 8 | 枚举常量名/值重复 | 服务端/客户端 prepatch | **B** | EnumPatcher 抛 InvalidOperationException;服务端启动失败/客户端加载中断 |
| 9 | 枚举数值跨端失配 | 跨层 | **S** | 一侧序列化正常,另一侧解释为不同值 |
| 10 | IProfileMigration 异常 | 服务端存档 | **B** | 存档无法加载,InvalidOperationException |
| 11 | 双组件 mod 缺一端 | 跨层 | **B/S** | 服务端主动报错(QB 的完整性测试)或客户端枚举缺失功能异常 |
| 12 | 版本失配(SPT Core / Assembly-CSharp / 依赖 mod) | 跨层 | **B** | 拒绝加载或运行时弹错 |
| 13 | profile 引用已移除 mod 的物品/商人 | 跨层数据 | **B**(profile 级) | 存档不可加载;ProfileFixer 不保证 |
| 14 | BepInPlugin GUID 重复 | 客户端 | **B/O** | BepInEx 加载冲突 |
| 15 | 两 patch 同一目标方法 | 客户端 Harmony | **C/O** | Harmony 链式共存;Prefix false/Transpiler 叠加可能导致后者失效(→O) |
| 16 | 插件内一个 patch 失败 | 客户端 | **B** | 官方插件后续 patch 全不加载 + 弹窗退出 |
| 17 | 文件覆盖(同名 DLL/插件) | 客户端 | **O** | 最后安装者胜(覆盖式安装语义) |
| 18 | DI 共享单例(配方 10) | 服务端 | **C** | 官方支持;忘写 Singleton = S(数据不同步) |
| 19 | ModDependencies/Incompatibilities 声明 | 服务端 | **C** | 校验在加载器执行;按 mod 名匹配(脆弱) |
| 20 | 运行时互操作/主动互斥(QB/SAIN/LB) | 跨层 | **C** | 反射探测 + 自动禁用,最成熟的共存模式 |

---

## 5. 检测方法分类

图例:M=Metadata-detectable(读清单/配置即可,无需反编译) I=IL-detectable(需 Mono.Cecil/反编译) R=Runtime-only(只能运行时发现)

| 冲突 | 检测法 | 依据 |
|------|--------|------|
| ModGuid / BepInPlugin GUID 重复 | **M** | 读 mod 元数据即可 |
| ModDependencies / Incompatibilities | **M** | 元数据声明,加载器校验 |
| SptVersion / 构建版本失配 | **M** | 元数据 + 服务器启动校验 |
| 路由 URL 撞车 | **M** | 读路由构造器 `RouteAction("/path",...)` 源码即可(无需反编译,源码/反编译产物可见) |
| 表 key 修改重叠 | **M/I** | 读 `OnLoad` 源码可见 key;但精确判定"同一 MongoId"需对照数据库 |
| 枚举常量名/值撞车 | **M** | patcher JSON + `ClientEnumDefinitions` 注册源码 |
| 文件覆盖(同名文件/DLL) | **M** | 归档文件路径比对 |
| bundle key 撞车 | **M** | bundles.json 比对 |
| 配置文件同名/同 GUID | **M** | .cfg / 目录结构比对 |
| Harmony 目标方法重叠 | **I** | `GetTargetMethod()` 的反射特征需在反编译(如 dnSpy)中解析后才知目标;混淆期(4.0)更是只有 IL 层可判 |
| Transpiler 模式冲突 | **I** | 需反编译对比 IL 搜索模式 |
| 类名/命名空间冲突(反混淆改名) | **I** | 引用类型需 IL 层解析 |
| 构造参数解析失败 / 循环依赖 | **R** | 容器构建时才暴露(启动) |
| 路由 body 类型不匹配 | **R** | 请求到达时抛异常 |
| 服务端-客户端配对缺失 | **R/M** | QB 有显式完整性测试(算 M+);多数 mod 无此测试 → R |
| 枚举拉取失败(服务端未启动) | **R** | 启动时序问题 |
| profile 卸载后不可加载 | **R** | 数据层,运行时/加载时暴露 |
| 数值/权重静默叠加干扰 | **R** | 只能游戏内观察 |
| 运行时互操作探测(QB/SAIN) | **R** | Type.GetType 运行时反射 |

---

## 6. 核心结论(给策展/诊断工作流)

1. **服务端 mod 冲突几乎都是「共享可变单例上的后写者胜」**——加载顺序(TypePriority → ModGuid 字母序)就是冲突仲裁器;策展工具应能解析 TypePriority 并预测覆盖方向。
2. **启动即炸的三类硬冲突**:DI 构造失败、类型未映射配置、枚举名/值重复、wwwroot URL 撞车。
3. **路由冲突是「先注册者胜」**,与表冲突方向相反(表=后写胜,路由=先注册胜)——两套仲裁语义不能混用。
4. **客户端最脆弱的是 Harmony 目标方法 + 反混淆命名**:4.1 改名后所有 patch 目标都要重解析,这是 4.0→4.1 迁移期最高发冲突。
5. **跨层 mod 的最佳实践是「运行时主动互操作」**(QB 的 Type.GetType 探测 + 主动禁用),优于元数据 Incompatibilities(按名称字符串匹配,脆弱)。
6. **元数据可检测面有限**:大量冲突(行为叠加、枚举数值失配、profile 损坏)只能运行时发现 → 策展流水线需要「运行时验证批次」环节(见 testing-bgs-modpack)。

---

## 附录:关键源码/文档坐标

- 加载器:`SPTarkov.Server/Modding/ModLoader.cs`、`ModValidator.cs`(mod-loading.md)
- DI:`Libraries/SPTarkov.DI/DependencyInjectionHandler.cs`、`Annotations/Injectable.cs`、`Server.Core/DI/IOnDIConstruct.cs`、`OnLoadOrder.cs`
- 表:`Libraries/SPTarkov.Server.Core/Models/Spt/Tables/*`(TemplateTable.cs 含属性捷径)
- 配置:`Libraries/SPTarkov.Server.Core/Loaders/ConfigLoader.cs`
- 路由:`Libraries/SPTarkov.Server.Core/Routers/`(HttpRouter.cs、各 ItemEventRouter)
- 存档:`Servers/SaveServer.cs`、`Routers/SaveLoad/ProfileSaveLoadRouter.cs`、`Services/Profile/ProfileMigrationService.cs`
- 客户端补丁:`A-核心服务端/modules/project/SPT.Reflection/Patching/ModulePatch.cs`、`SPT.Custom/SPTCustomPlugin.cs`、`SPT.PrePatch/SptPrePatcher.cs`、`EnumPatcher.cs`
- 跨层实例:`archive/forge/mods/SAIN-Solarint-s-AI-Modifications-Full-AI-Combat-System-Replacement_791_source/`(SAIN 双工程)、`Questing-Bots_1109_source/`(Questing Bots 双工程 + Interop 目录)
- 官方文档:`wiki/SPT_41/Server_40_to_41.md`、`Client_40_to_41.md`、`modding/EnumExtensions.md`、`wiki/Mod_Types.md`、`Installing_Mods.md`
