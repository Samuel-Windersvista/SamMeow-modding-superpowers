---
version: [3.11, 4.1]
domain: server
topic: migration
source: curated
---

# 服务端 mod 迁移知识：SPT 3.11 -> 4.1.2（实测验证 2026-08-08）

> 状态：已提炼（2026-08-08）| 来源：3114 整合包迁移实战（Wave 3/4：1038/954/1657/861/1278/1950）
> 核心认知：**SPT 4.x 服务端从 TS 改为 C#（SPTarkov）**——3.x 的 TS/JS 服务端 mod
> 不兼容，需重写为 C# 或找作者 4.x 版本。作者已更新的 mod 直接用；未更新的需手动升级。

---

## 1. SPTarkov 4.x 服务端 mod 结构（实测）

```
SPT_Runtime/user/mods/<ModName>/
└── <ModName>.dll          # 编译产物（SPTarkov.Server.Core 引用）

项目形态：
- csproj: SDK 风格, TargetFramework net10.0（4.1.x 时代）, PackageReference:
    SPTarkov.Server.Core 4.1.x（主）
    SPTarkov.Common / SPTarkov.DI（辅助）
- 入口: [Injectable(TypePriority = OnLoadOrder.Preload/PostSptModLoader...)] 类 + IOnLoad/接口
- 部署: MO2 overlay 的 SPT_Runtime/user/mods/<ModName>/ 目录
```

## 2. 判定源码基线（3.x TS vs 4.x C# vs 4.0 早期 C#）

| 特征 | 判定 |
|------|------|
| `src/mod.ts` + `types/*.d.ts` + `build.mjs` | 3.x TS 服务端（**不兼容，需重写 C#**） |
| csproj 含 `SPTarkov.Server.Core` + `[Injectable]` + `IOnLoad` | 4.x C# 服务端（可直接编译） |
| csproj 用 SPTarkov 4.0.x / net9.0 | 4.0 早期 C#（需升级到 4.1.2 / net10） |
| 客户端 csproj 引用 `spt-core/spt-custom/spt-debugging` | 4.x 客户端基线 |

## 3. SPTarkov 4.0 -> 4.1.2 升级映射（WeaponCustomizer 1950 实测）

| 4.0 | 4.1.2 |
|-----|-------|
| `AbstractModMetadata`（抽象类） | `IModMetadata`（接口，去 override 与 IsBundleMod） |
| `OnLoad()` / `OnUpdate()` | `OnLoadAsync(CancellationToken)` / `OnUpdateAsync(...)` |
| `OnLoadOrder.PreSptModLoader` | `OnLoadOrder.Preload` |
| `OnLoadOrder.PostSptModLoader` | `OnLoadOrder.PostLoad` |
| `ISptLogger` | `SPTarkov.Common.Models.Logging` 命名空间 |
| `LogTextColor.Cyan` | Spectre 颜色参数（ILogger 新签名） |
| `ServiceLocator.ServiceProvider.GetService<T>()` | 构造注入 + `[Injectable]` |
| `StaticRouter` 旧签名 | `RouteAction` 带 `CancellationToken`，`OnLoadOrder.Routers+1` |
| `FileUtil.ReadFileAsync/WriteFileAsync` | 补 `CancellationToken` 参数 |
| net9.0 + SPTarkov 4.0.x | net10.0 + SPTarkov 4.1.2 |

**通用原则**：服务端 mod 升级 = 升 csproj（net10 + 4.1.2 包）→ 按编译错误逐处改签名 → 依赖注入
（ServiceLocator 全局移除，改构造注入 + `[Injectable]`）。

## 4. 3.x TS 服务端 -> 4.1 C# 重写（未完成项，865/2162/2246 待处理）

3.x TS mod（`src/mod.ts` 导出 mod 类 + 用 3.x 的 DatabaseServer/Traders 等）在 4.1 **不可用**。
重写路径：
1. 建 C# 项目：net10.0 + SPTarkov.Server.Core 4.1.2
2. 逻辑翻译：TS → C#（数据库读写、assort 生成、quest 定义等）
3. `[Injectable]` 注册 + `IOnLoad`/`IPostDBLoadMod` 等接口实现
4. 4.1 服务端 API 参考：SPT-archive 里的 Server 仓库源码 / NuGet 包反编译

## 5. 客户端+服务端混合 mod 的部署模式（实测）

| mod | 客户端 | 服务端 | 部署 |
|-----|--------|--------|------|
| 1038 TraderModding | BepInEx/plugins/ | SPT_Runtime/user/mods/ | 拆两个 overlay（客户端文件/服务器文件） |
| 954 BorkelRNVG | BepInEx/plugins/ | SPT_Runtime/user/mods/ | 同上 |
| 861 MoreCheckmarks | BepInEx/plugins/ | SPT_Runtime/user/mods/ | 同上 |
| 1278 PackNStrap | BepInEx/plugins/ | SPT_Runtime/user/mods/ | 同上 |
| 1950 WeaponCustomizer | BepInEx/plugins/ | SPT_Runtime/user/mods/ | 同上 |

服务端 DLL 部署路径：`<overlay>/SPT_Runtime/user/mods/<ModName>/<ModName>.dll`（目录名通常=项目名）。

## 6. 版本注意

- **SPTarkov NuGet 版本必须 ≥ 服务端运行时版本**（4.1.2 服务端配 4.1.0/4.1.1/4.1.2 包均可，
  4.0.x 包编译的 DLL 在 4.1.2 运行时有风险——见 1923 LockableDoors Server 4.0.5 待核实项）
- 服务端 DLL 编译用 `dotnet build -c Release`（MSBuild.exe 缺 Microsoft.NET.Sdk 解析器，
  SDK 风格项目必须用 dotnet CLI）

### 6.1 ModValidator 版本校验机制（2026-08-08 反编译 SPT.Server.dll 权威确认）

SPT 4.1.2 服务端加载 mod 时 `SPTarkov.Server.Modding.ModValidator` 做 4 重校验：

| 校验 | 逻辑（反编译源码） | 对 4.0.x mod 的结果 |
|------|-------------------|--------------------|
| `ValidateCoreAssemblyReference` | 读 mod 引用的 SPTarkov.Server.Core 版本，**只拒绝"要求版本 > 当前 SPT"**（`version2 > version` 抛异常） | 4.0.3/4.0.4 < 4.1.2 → **通过** |
| `IsModCompatibleWithSpt` | `semVer.Satisfies(SPT_VERSION, mod.SptVersion)`——**SptVersion 元数据必须满足当前版本** | `~4.0.0` 不满足 `4.1.2` → **失败** |
| `AreModDependenciesFulfilled` | 依赖 ModGuid + 版本范围检查 | 缺依赖 → 失败 |
| `IsModCompatible` | Incompatibilities 列表检查 | 冲突 → 失败 |

**任一失败 → `modloader-no_mods_loaded` 错误 + 全部 mod 不加载**（不是跳过单个，是全部拒载）。

**关键结论（实测依据，非猜测）**：
1. **4.0.x 编译的 mod DLL 在 4.1.2 不能直接跑**——`IsModCompatibleWithSpt` 用 semver 校验
   `IModMetadata.SptVersion`，`~4.0.0`/`4.0.*` 不满足 4.1.2 → 拒载
2. **程序集版本检查不是障碍**（只查向上不兼容），**元数据 SptVersion 声明才是障碍**
3. **破解路径**：反编译 mod DLL（ilspycmd）→ 改 `ModMetadata.SptVersion` 为 `~4.1.0`/`4.1.*` →
   用 SPTarkov.Server.Core 4.1.2 NuGet 重编译 → 部署
4. 反编译工具：`ilspycmd`（dotnet tool，已装 v10.1.1）；record 类型 ModMetadata 有 ILSpy bug，
   需 cecil 读属性结构手写补全（见 `archive/forge/c-bucket/decompiled/` 实例）

### 6.2 SptVersion semver 语法速查（SPT 服务端用 SemanticVersioning 库）

- `~4.0.0` = >=4.0.0 <4.1.0（**不含 4.1**）
- `4.0.*` = >=4.0.0 <4.1.0
- `~4.1.0` = >=4.1.0 <4.2.0（含 4.1.2 ✓）
- `>=4.0.2 <4.0.9` = 精确区间
- 改 SptVersion 时用 `~4.1.0` 或 `4.1.*` 最稳

## 7. SPTarkov 4.0 → 4.1 服务端重构（2026-08-08 反编译对比实测）

### 7.1 重大移除（4.0.13 有 → 4.1.0/4.1.2 无）

| 4.0 类型 | 4.1 状态 | 替代方案 |
|----------|---------|---------|
| `Services.DatabaseService`（GetTables()） | **移除** | 注入表模型（见 7.2） |
| `Servers.ConfigServer`（GetConfig<T>()） | **移除** | 启动时 `ConfigLoader.Initialize()`（静态） |
| `Servers.DatabaseServer` | **移除** | 表模型注入 |
| `ModHelper` | 移 namespace | `Helpers.Server.ModHelper` |
| `TraderControllerClass`（客户端同款） | 移除 | — |

### 7.2 4.1 数据库模式：表模型直接注入（权威参考：Croupier 4.1.2 反编译）

4.1 每张数据库表是一个 **record 类型**（`SPTarkov.Server.Core.Models.Spt.Tables.*`）：
`TradersTable` / `LocaleTable` / `TemplateTable` / `GlobalTable` / `BotTable` / `HideoutTable` 等。
它们**实现字典接口**，可注入后直接当字典用：

```csharp
[Injectable(InjectionType.Transient, 2147483647)]
public class AddCustomTraderHelper(
    ISptLogger<AddCustomTraderHelper> logger,
    ICloner cloner,
    TradersTable tradersTable,   // ← 注入表模型
    LocaleTable localeTable)
{
    // 注册商人：
    ((Dictionary<MongoId, Trader>)(object)tradersTable).TryAdd(traderId, trader);
    // 查商人改 assort：
    ((Dictionary<MongoId, Trader>)(object)tradersTable).TryGetValue(traderId, out var trader);
    // locale 注册：
    localeTable.Global[lang].AddTransformer(dict => { dict[$"{id} FullName"] = name; return dict; });
}
```

### 7.3 IModMetadata 接口（4.1 服务端 mod 元数据）

反编译确认（`SPTarkov.Server.Core.Models.Spt.Mod.IModMetadata`）：
- **Version: `SemanticVersioning.Version`**（不是 System.Version）
- **SptVersion: `SemanticVersioning.Range`**（不是 string！）
- ModDependencies: `Dictionary<string, SemanticVersioning.Range>`
- 全部 **init-only** 属性（record 风格，用 `init` 不是 `set`）
- 需实现 HasPrepatcher/Contributors

```csharp
public class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.x.y";
    public SemanticVersioning.Version Version { get; init; } = new(1, 0, 0);
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    // ... 其余 init 属性
}
```

### 7.4 ImageRouter（4.1 图片注册）

4.1 的 `ImageRouter` 只有 `AddRoute(string key, string valueToAdd)`（key=URL 路径, value=文件路径），
没有 4.0 的 `AddImageToDb`。商人图片注册：
```csharp
imageRouter.AddRoute($"/files/trader/{traderId}", imageFilePath);
```

### 7.5 4.0→4.1 服务端重编译流水线（已验证：Artem 1023 成功）

1. 反编译 4.0 DLL：`ilspycmd -l c <dll>` 列类型 → `ilspycmd -t <Type> <dll>` 逐类反编译
   （record ModMetadata 有 ILSpy bug，用 cecil 读属性结构手写）
2. 建项目：net10.0 + `SPTarkov.Server.Core 4.1.2` + `SPTarkov.DI 4.1.2` + `SemanticVersioning 3.0.0`
   （WTT 系 mod 还需引用 WTT-ServerCommonLib，已在 2310 A 桶 overlay 里，4.1.0 编译版）
3. 按编译错误逐处映射（7.1-7.4 的规则）
4. 状态机/async 反编译太乱 → 按逻辑手写重写（参考反编译的调用序列）
5. `dotnet build -c Release` → 部署替换 → 源码留档 `archive/forge/c-bucket/recompiled/<id>-<Name>/`
6. 成果：Artem 1023 / Painter 1025 / TGC 1125 全部 4.1.2 重编译成功（0 错误）

### 7.6 反编译改写陷阱（TGC 1125 实战）

1. **async 状态机反编译不可直接编译**：`<OnLoad>d__N` 编译器生成类 + IL 注释——必须删掉，
   从 MoveNext 的调用序列手工重建 OnLoadAsync（用 regex 提取 `tGC.xxx()` 调用序列）
2. **`Enumerator<T>/Enumerator<K,V>` 是 4.0 内部结构**（4.1 移除）——反编译代码
   `Enumerator<X> e = dict.GetEnumerator(); while(e.MoveNext()){...}` 需改 foreach。
   **自动正则改写嵌套循环极易出错**（变量遮蔽、嵌套 while）——大 mod 建议直接手写
   核心方法（精简版），别机械改写全部
3. **`MongoId.op_Implicit(x)`** 反编译残留 → `new MongoId(x)`
4. **`[RequiredMember]`/`[CompilerFeatureRequired]` 特性**：net10 编译器禁止手动使用
   （CS9033/CS8335）——反编译的 Model 类必须删这些特性
5. **4.1 类型名变化**：`ItemTemplate`→`TemplateItem`、`Item.TemplateId`→`Item.Template`、
   `LocaleDetails` 在 `SPTarkov.Server.Core.Models.Spt.Mod`
6. **大 mod 精简策略**：核心功能（WTT CustomItemService 注册 + 商人 assort + locale）优先，
   锦上添花逻辑（filter 复制/臂章）可后补——先保证编译通过 + 核心功能可用

### 7.7 SptVersion 补丁法（免重编译快速升级，2026-08-08 批量验证 12 个 mod）

> **⚠️ 2026-08-09 重大修正：此方法无效，已废弃！**
> 实测 4.1.2 服务端加载 12 个补丁后的 4.0 mod 全部 ReflectionTypeLoadException：
> **根因**：4.0 mod 的 metadata 类继承 `AbstractModMetadata`（4.0 抽象类），
> 而 4.1.0/4.1.2 把该类移除、改为 `IModMetadata` 接口（铁证见 7.8 版本对比）。
> **IL 类引用断链**——`Could not load type '...AbstractModMetadata' from assembly 'SPTarkov.Server.Core, Version=4.1.2.0'`
> 光改 SptVersion 字符串（ldstr）救不了类引用。**唯一路径 = 反编译重编译**（7.5 流水线）。

**原记录（已废弃）**：~~ModValidator 的 `IsModCompatibleWithSpt` 只校验 `IModMetadata.SptVersion` 元数据（semver），不校验代码 API。若 mod 未使用 4.1 移除的服务，只需改 SptVersion 字符串即可在 4.1.2 加载——无需反编译重编译~~（判断前提错误：4.0 mod 必然继承 AbstractModMetadata）

### 7.8 AbstractModMetadata → IModMetadata（4.0→4.1 硬断链，2026-08-09 反编译对比实测）

| 版本 | metadata 类型 | 说明 |
|------|--------------|------|
| 4.0.13 | `AbstractModMetadata`（抽象类） | 4.0 mod 全部继承它 |
| 4.1.0 | `IModMetadata`（接口） | **类移除改接口** |
| 4.1.2 | `IModMetadata`（接口） | 同 4.1.0 |

**影响**：所有 4.0 编译的服务端 mod（继承 AbstractModMetadata）在 4.1.2 **必然加载失败**
（ReflectionTypeLoadException），**没有任何补丁捷径**（IL 类引用无法字符串替换）。必须反编译
→ 改 `: AbstractModMetadata` 为 `: IModMetadata`（属性 set→init）→ 重编译。

**ModLoader 元数据校验**（反编译 SPT.Server.dll LoadMod 确认）：
```csharp
if (string.IsNullOrEmpty(ModGuid) || string.IsNullOrEmpty(Name) ||
    string.IsNullOrEmpty(Author) || string.IsNullOrEmpty(License))
    throw new ModLoaderException("missing one of these properties: ModGuid, Name, Author, or License");
```
→ 手写 ModMetadata 时 4 个字段必须非空（License 尤其容易漏，默认 "" 会拒载）

**7.7 判断标准修正**：4.0 mod 升级 = 必走重编译（7.5），SptVersion 补丁仅作为
重编译前的临时验证手段（看能不能过 metadata 校验），不能作为最终方案。

### 7.9 WTT 系依赖区间（2026-08-09 实测，scorpion 拒载根因）

WTT 系 mod（Artem/Painter/Scorpion/ECOT/TGC/PackNStrap 等）的 `ModDependencies` 声明
`com.wtt.commonlib` 版本区间——**4.0 时代是 ~2.0.x，4.1 的 WTT-ServerCommonLib 是 3.0.3**：
- 区间 `~2.0.15`/`~2.0.20` 在 4.1.2 会**依赖校验失败**（ModValidator.AreModDependenciesFulfilled）→ 该 mod 拒载 → **全部 mod 拒载**
- **必须改为 `>=3.0.0 <4.0.0`**（或 `~3.0.0`）
- 依赖 GUID 是 `com.wtt.commonlib`（**不是** com.wtt.servercommonlib——易错点）
- 排查方法：重编译后查 ModMetadata.cs 的 ModDependencies；日志报 "需要 com.wtt.commonlib 版本~2.0.x，当前已安装 3.0.3" 即此问题

**其他依赖陷阱**（ECOT 实战）：ModDependencies 里的依赖若在整合包中不存在（如
`com.epicrangetime.aio`），同样会导致依赖校验失败——移除不存在的依赖声明。

### 7.10 Router 生命周期（2026-08-09 实机崩溃根因，PitFireTeam + Scorpion 验证）

**症状**：服务器启动崩溃：
```
Cannot consume scoped service 'DynamicRouter' from singleton 'HttpServer'
(Error while validating the service descriptor ...)
```

**根因**：4.1.2 的 `HttpServer` 是 **Singleton**，它消费 `IEnumerable<DynamicRouter>` /
`IEnumerable<StaticRouter>`（通过 HttpRouter）。若 mod 把 Router 注册为 **Scoped**，
DI 容器构建时验证失败 → **服务器直接崩溃**（不是拒载）。

**规则（4.0→4.1 迁移必查）**：
- **Router 类（: StaticRouter / : DynamicRouter）必须注册为 `Transient`**（或 Singleton，安全）
- **禁止 Scoped**——Scoped Router = 启动崩溃
- 4.0 的 `[Injectable(Scoped, ...)]` 对 Router 是 4.0 允许的，4.1.2 必须改 Transient
- 排查：grep 所有 `: StaticRouter`/`: DynamicRouter` 类的 `[Injectable(...)]` 生命周期
- 非 Router 类（IOnLoad/Service/Helper）用 Scoped/Singleton 都没问题

**实测案例**：
- PitFireTeam（2676）：StaticRouter Scoped → Transient 后启动成功（实机验证"服务器已开启"）
- Scorpion（1348）：CustomDynamicRouter Scoped → Transient 修复
- HarryHideout（1303）：Singleton DynamicRouter 可工作（Singleton 可注入 Singleton）
- RaidOverhaul（1192）：Transient 正确

## 8. TS/JS → C# 迁移（3.11 服务端 mod 到 4.1，2026-08-11 AKResonant 1850 实战验证）

> 适用于：3114 包中 `user/mods/<name>/src/mod.ts` 形态的 TS 服务端 mod。
> 核心认知：这些 TS mod 本质是**WTT 库 API 的 TS 壳**——数据/bundle 全复用，只换壳为 C# WTT-ServerCommonLib 服务调用。

### 8.1 判定：TS mod 是否可行 C# 迁移

| 特征 | 迁移难度 |
|------|---------|
| TS 用 WTTInstanceManager + CustomItemService/CustomAssortSchemeService 等 | 极低（C# WTT 有等价服务） |
| TS 用 SPT 3.x 数据库 API（DatabaseServer.GetTables() 等） | 中等（改为表模型注入，见 7.2） |
| TS 有复杂业务逻辑（自定义伤害计算、AI 行为等） | 高（需 C# 手写重写、API 逐项验证） |

### 8.2 WTT 服务映射（TS → C#）

| TS 服务（3.x WTT） | C# 服务（4.1 WTT-ServerCommonLib 3.0.3） | 读取约定 |
|-------------------|----------------------------------------|---------|
| `CustomItemService.createItemFromClone` | `WTTCustomItemServiceExtended.CreateCustomItems(Assembly, string?)` | `db/CustomItems/*.json` |
| `CustomAssortSchemeService` | `WTTCustomAssortSchemeService.CreateCustomAssortSchemes(Assembly, string?)` | `db/CustomAssortSchemes/*.json` |
| `CustomWeaponPresets` | `WTTCustomWeaponPresetService.CreateCustomWeaponPresets(Assembly, string?)` | `db/CustomWeaponPresets/*.json` |
| `CustomBotLoadoutService` | `WTTCustomBotLoadoutService.CreateCustomBotLoadouts` | `db/CustomBotLoadouts/*.json` |

所有服务通过 `[Injectable]` 构造注入，C# 主类只需 `OnLoadAsync` 里调 3-4 个 Create 方法即可完成原 TS 全部逻辑。

### 8.3 数据迁移陷阱（AKResonant 四轮修复实战）

**陷阱 1：TS 用人类可读名称，C# 严格 ObjectId 校验**
- 现象：`ObjectId must be a 24-character hex string, but got "SerbuShotgun"`
- 原因：TS 版的 `masterySections.Templates` 存武器名称（TS 运行时延后转换），
  C# WTT 服务 JSON 反序列化时立即校验 MongoId 格式
- 修复：名称→真实 24 位 hex ID（查游戏数据库 `items.json`）

**陷阱 2：TS embed preset，C# 需要独立文件**
- 现象：`Error adding weapon preset ItemPresets: NullReference`
- 原因：TS 版把 `weaponpresets` 嵌在物品配置 JSON 字段里，
  C# WTT 服务（`WTTCustomWeaponPresetService`）从 `db/CustomWeaponPresets/*.json` **独立目录**读取
- 修复：将 `weaponpresets` 数据提取到独立文件，格式为 `{presetId: Preset}`

**陷阱 3：残留空壳文件拖垮 LoadAllJsonFiles**
- 现象：同上 NullReference 报错
- 原因：原包目录残留 29B `WeaponPresets.json`（`{"ItemPresets": {}}`），
  WTT 服务读目录**所有** JSON 文件，空对象反序列化 `value.Items` = null
- 修复：删除所有残留空壳文件（部署后检查目录文件清单）

**陷阱 4：标准 SPT Preset 格式 ≠ TS 版 Preset 格式**
- 4.1 的 `Preset` 类（`SPTarkov.Server.Core.Models.Spt.Tables.Preset`）属性：
  `Id/Type/ChangeWeaponName/Name/Parent/Items/Encyclopedia`（无下划线，SPT JsonUtil 自动映射 `_id`→`Id`）
- 标准 Preset：**无 `_parent` 字段**；`_items[0]` 必须有 `_tpl`（武器模板 ID）
- 若 preset 引用的物品模板在 Handbook FlatItems 中不存在（如自定义物品注册异常），
  会导致游戏端 `FlatItems.Items does not contains preset key` 崩溃
- 修复：Preset 非必须（仅 Handbook 展示）——可直接删除 preset 文件保住核心功能

### 8.4 TS→C# 重写模板（AKResonant 模式）

```csharp
// 主类：注入 WTT 服务，OnLoadAsync 调 3-4 个 Create 方法
[Injectable(InjectionType.Singleton)]
public class ModName(
    ISptLogger<ModName> logger,
    WTTCustomItemServiceExtended customItemService,
    WTTCustomAssortSchemeService assortSchemeService,
    WTTCustomWeaponPresetService weaponPresetService) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken ct)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await customItemService.CreateCustomItems(assembly, null);
        await assortSchemeService.CreateCustomAssortSchemes(assembly, null);
        if (/* 有 preset 数据 */)
            await weaponPresetService.CreateCustomWeaponPresets(assembly, null);
    }
}
```

csproj：net10.0 + SPTarkov.Server.Core 4.1.2 + SPTarkov.DI 4.1.2 + SemanticVersioning 3.0.0
+ Reference WTT-ServerCommonLib.dll（从 2310 A 桶 overlay 提取，3.0.3 版本）。
ModDependencies 必须声明 `com.wtt.commonlib >=3.0.0 <4.0.0`。

### 8.5 TS→C# 迁移检查清单

部署到 overlay 后必查：
1. **`db/CustomItems/*.json`**：所有 ObjectId 字段 24 位 hex ✓；无人类可读名称残留
2. **`db/CustomWeaponPresets/`**：无残留空壳文件；Preset 格式 = `{presetId: {_id, _items, _encyclopedia, ...}}`；无 `_parent` 字段
3. **`db/CustomAssortSchemes/`**：trader assort 格式合法
4. **`bundles.json`**：manifest 条目全对应 `bundles/` 目录下的文件
5. **DLL**：`IModMetadata` 实现 + `IOnLoad` 实现 + `ModDependencies com.wtt.commonlib` 区间正确
6. **bundle**：标准流程（UnityPy 深读脚本引用 + 版本头）——Egress 所有 bundle 来自 3114 原包（Forge 无更新版），原包 Unity 2022.3.43f1 已验证兼容
