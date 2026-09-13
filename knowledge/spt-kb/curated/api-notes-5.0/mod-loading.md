---
version: [5.0]
domain: server
topic: mod-loading
source: curated
---
# Mod 加载流程笔记 [5.0]

> **[UNSTABLE-PREVIEW]** SPT 5.0 处于开发初期（`5.0x-dev` 分支，尚无正式版）。本笔记为 2026-09-13 源码快照（HEAD `ff0bf3281`）提炼，上游 API 可能变更；使用前请以源码复核，勿据此做长期承诺。

> 状态：**源码实读（2026-09-13）** | 版本：[5.0]
> 源码：`SPTushonka.Server/Modding/ModLoader.cs`（447 行）、`ModValidator.cs`（369 行）、`EnumPatcher.cs`、`PrepatchLoadContext.cs`、`PrepatchAssemblyWriter.cs`、`Libraries/SPTushonka.Server.Core/Models/Spt/Mod/`

## mod 形态与加载目录

- **扫描目录**：`ModPath = "./user/mods/"`（`ModLoader.cs:20`），`Directory.GetDirectories(ModPath)` 遍历一级子目录，每个子目录 = 一个 mod（`ModLoader.cs:122`）。
- **DLL 识别**：`new DirectoryInfo(path).GetFiles()` **只搜顶层**（`ModLoader.cs:259`）；扩展名 `.dll`（大小写不敏感，`ModLoader.cs:261`）逐个载入。
- **程序集加载上下文**：`loadContext = AssemblyLoadContext.GetLoadContext(typeof(ModLoader).Assembly) ?? AssemblyLoadContext.Default`，再用 `loadContext.LoadFromAssemblyPath(Path.GetFullPath(file.FullName))`（`ModLoader.cs:255-264`）。即：普通启动载入默认上下文；内存 prepatch 启动时载入 `SPT.PrepatchHost` 上下文（见下「prepatch」）。
- **无 DLL**：抛 `ModLoaderException($"No Assemblies found in path: ...")`（`ModLoader.cs:267-270`）。
- **目录不存在**：自动创建 `user/mods/`（`ModLoader.cs:98-101`）。
- **mod 数据模型** `SptMod`：`Directory`、`ModMetadata`、`Assemblies`（`Models/Spt/Mod/SptMod.cs:6-15`）。
- 官方示例：`Testing/TestMod/`（元数据 + `IOnDIConstruct` + `IOnLoad` + MVC controller）、`Testing/TestMod2/`（prepatch 示例）。
- **不用 `package.json` / `mod.json`**：元数据来自 DLL 内实现 `IModMetadata` 的类型。

## 元数据契约 `IModMetadata`（`IModMetadata.cs:30-102`）

```csharp
public interface IModMetadata
{
    string ModGuid { get; init; }                       // 反域名风格，正则 ^[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*$
    string Name { get; init; }
    string Author { get; init; }
    List<string>? Contributors { get; init; }
    Version Version { get; init; }                      // SemanticVersioning.Version
    Range SptVersion { get; init; }                     // SemanticVersioning.Range，如 new Range("~5.0.0")
    bool HasPrepatcher { get; init; }
    List<string>? Incompatibilities { get; init; }      // 不兼容的 ModGuid 列表
    Dictionary<string, Range>? ModDependencies { get; init; }  // key = 依赖的 ModGuid
    string? Url { get; init; }
    string License { get; init; }
}
```

- 每个 mod 必须**恰好一个**实现；重复抛 `ModLoaderException`（`ModLoader.cs:317-320`）。
- 元数据用反射找类型并 `Activator.CreateInstance`（`ModLoader.cs:307-342`）。
- `ModGuid`/`Name`/`Author`/`License` 任一为空 → `ModLoaderException`（`ModLoader.cs:279-289`）。
- 类型加载失败（`ReflectionTypeLoadException`）→ `OutdatedModException`，附「引用了本版本服务器不存在的类型」摘要（`ModLoader.cs:344-366`）。

## 加载与校验规则

| 环节 | 规则 | 证据 |
|------|------|------|
| 扫描目录 | `./user/mods/`，一级子目录 = 一个 mod | `ModLoader.cs:20,122` |
| 识别文件 | 目录**顶层** `.dll`；无 DLL 抛 `ModLoaderException` | `ModLoader.cs:259-270` |
| 元数据 | 反射查找 `IModMetadata` 实现并 `Activator.CreateInstance` | `ModLoader.cs:307-342` |
| mod 列表排序 | `_loadedMods` 按 `ModGuid` 字母序（`OrdinalIgnoreCase`） | `ModLoader.cs:143` |
| GUID 校验 | `ModGuidRegex()` = `^[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*$` | `ModValidator.cs:56-70,367-368` |
| 版本校验 | `SptVersion` 为 semver range，`semVer.Satisfies(SPT_VERSION, mod.SptVersion)` | `ModValidator.cs:154-178` |
| 程序集引用校验 | mod 引用的 `SPTarkov.Server.Core` 版本**高于**运行时 → 抛异常（**先于其它校验、不可隔离**） | `ModValidator.cs:30-33,186-211` |
| 依赖校验 | `ModDependencies` key=ModGuid，逐项 `semVer.Satisfies(依赖版本, 要求区间)`；仅校验，**不参与排序** | `ModValidator.cs:243-287` |
| 不兼容校验 | `Incompatibilities` 中任一 ModGuid 已加载 → 失败 | `ModValidator.cs:289-324` |
| 重复 ModGuid | 分组发现重复 → 从列表移除该 GUID 的所有 mod，记 `_skippedMods` | `ModValidator.cs:116-137` |
| 拒绝客户端 mod | `Name` 为 `bepinex`/`user`/`src`/`db`、含 `plugins/` 目录、非 `IModBlazorMetadata` 且含 `.js`/`.ts` | `ModValidator.cs:331-365` |

- 客户端 mod 判定细节：`modName` 取 `mod.ModMetadata.Name`（不是目录名）；`plugins/` 判 `{mod.Directory}/plugins`；`.js`/`.ts` 递归统计（`ModValidator.cs:333-341`）。若元数据同时实现 `IModBlazorMetadata`，则跳过 `.js`/`.ts` 检查（`:355-362`）。
- 校验失败汇总：`errorsFound == true` → 记 `modloader-no_mods_loaded` 并 **return []**，即所有 mod 都不加载（`ModValidator.cs:91-95`）。
- 依赖自引用 / 不兼容自引用只 warning、继续（`ModValidator.cs:250-254,296-300`）。

## 加载顺序 / `TypePriority` 语义

- **mod 列表顺序**：`ModLoader.LoadMods` 结束后按 `ModGuid` 升序排序（`ModLoader.cs:143`）。
- **类/可注入组件的顺序**：`IModMetadata` 的 XML 文档明确写「classes are ordered first by `Injectable.TypePriority`, then by `ModGuid` as a tiebreaker；实践中 `TypePriority` 几乎总是决定性因素」（`IModMetadata.cs:22-26`）。
- **DI 注册顺序**：`DependencyInjectionHandler.InjectAll` 用 `OrderBy(tRef => tRef.InjectableAttribute.TypePriority)` 排序后注册（`DependencyInjectionHandler.cs:74`）。
- **`TypePriority` 默认值**：`int.MaxValue`（`Injectable.cs:7`）。越低越先注册/执行。
- `OnLoadOrder` 阶段常量（`DI/OnLoadOrder.cs:3-15`）：`Watermark=0` → `Preload=100000` → `GameCallbacks=200000` → `TraderRegistration=300000` → `Routers=400000` → `HandbookCallbacks=500000` → `SaveCallbacks=600000` → `TraderCallbacks=700000` → `PresetCallbacks=800000` → `RagfairCallbacks=900000` → `PostLoad=1000000`。
- 注意：5.0 **没有** `loadAfter`/`loadBefore`/`order.json`/`loadorder.json` 之类的 mod 间显式排序机制（3.11 的机制在此不存在）。

## 异常隔离

- `LoadMod` 逐个 try/catch：`OutdatedModException` 记 error、`Exception` 记 critical，均**继续下一个** mod（`ModLoader.cs:129-141`）。
- **校验阶段任一失败则所有 mod 都不加载**（`ModValidator.cs:91-95`）。
- `ValidateCoreAssemblyReference` 在 `ValidateMods` 最先执行且**不包 try/catch**（`ModValidator.cs:30-33,186-211`）；mod 引用过高版本 Core 会直接向上抛，终止加载。
- 内部实现注意：`_skippedMods` 存的是 **ModGuid**（`ModValidator.cs:127`），而 `ShouldSkipMod` 用 **`{Author}-{Name}`** 查找（`ModValidator.cs:238-241`）；两者键格式不一致，重复 mod 的跳过判定可能不生效（以源码为准）。

## prepatch（枚举补丁）

### 元数据与目录

- `HasPrepatcher = true` 时，`LoadMod` 调 `LoadEnumPrepatch(ModGuid, Path.Combine(PatcherPath, ModGuid))`（`ModLoader.cs:291-294`）；`PatcherPath = "./user/patchers/"`（`ModLoader.cs:21`）。
- `LoadEnumPrepatch`（`ModLoader.cs:368-416`）：
  1. 目录必须存在，否则 `ModLoaderException`（`:370-375`）。
  2. 顶层恰好 **1 个** `.json`（多/少均抛异常，`:377-395`）。
  3. 反序列化为 `List<EnumEntryDefinition>`（`:398-408`）；`null`/空 → `ModLoaderException`（`:410-413`）。

### 两遍启动流程

`ModLoader.RunModLoader`（`ModLoader.cs:28-59`）：
1. 判断当前是否已在 prepatch 宿主进程：`AssemblyLoadContext.GetLoadContext(typeof(ModLoader).Assembly)?.Name == PrepatchLoadContext.ContextName`（`:31-32`）。
2. 非宿主进程时：`LoadPrepatchesForPrepatchPass()` 扫 `user/patchers/*` 目录全部加载（`:39,61-89`）。
3. 若存在 enum prepatch：`ApplyPrepatchesInMemory()`（`:46`）→ `BootPatchedServerInMemory(patchedCore, args)`（`:49`）→ 返回 `ModLoaderRunResult(false, [])`，本进程**不再启动服务器**（`:50`）。
4. 宿主进程（已 patched）跳过上述，走 `LoadMods` + `ValidateMods`，返回 `ModLoaderRunResult(true, loadedMods)`（`:55-58`）。

- `ApplyPrepatchesInMemory`（`:150-182`）：`RunEnumPrepatches` → `_serverCoreModule.Write()`（`PrepatchAssemblyWriter.Write`，`PrepatchAssemblyWriter.cs:6-13`，用 `ManagedPEImageBuilder(MetadataBuilderFlags.PreserveAll)`）→ 写 `./SPTarkov.Server.Core.Patched.dll` 与 `.pdb`（`ModLoader.cs:22,167-173`）。
- `RunEnumPrepatches`（`:188-219`）：按 `ModGuid` 再 `DefinitionPath` 升序执行（`:196-199`），逐个 `EnumPatcher.Patch`；异常记 critical 并标记该 patch 失败（`:201-216`）；全部成功才返回 true（`:218`）。
- `BootPatchedServerInMemory`（`:235-246`）：新建 `PrepatchLoadContext(hostAssemblyPath, patchedCore.Assembly, patchedCore.Symbols)`，`LoadFromAssemblyPath(hostAssemblyPath)`，反射调用 `SPTarkov.Server.Program.Main(args)`（`:241-245`）。
- `TryLoadServerCoreBytes`（`:418-440`）：从执行程序集同目录读 `SPTarkov.Server.Core.dll` 与 `.pdb` 为 `ModuleDefinition.FromBytes`。

### prepatch JSON 文件格式（`EnumEntryDefinition`）

`Models/Spt/Mod/EnumEntryDefinition.cs:8-39`：

| JSON 字段 | C# 属性 | 类型 | 说明 |
|-----------|---------|------|------|
| `enumType` | `EnumType` | `string`（required） | 枚举全名，如 `EFT.EBuffId`；嵌套类型用 `+`，如 `EFT.InventoryLogic.Weapon+EFireMode`（`:11-15`） |
| `constantName` | `ConstantName` | `string`（required） | 新常量名；已存在则报错（`:17-22`） |
| `constantValue` | `ConstantValue` | `long`（required） | 新常量值；按枚举底层类型转换（`:24-30`） |
| `jsonEnumName` | `JsonEnumName` | `string?` | **CLIENT ONLY**，写入 `JsonEnumName` 特性（`:32-38`） |

示例（`Testing/TestMod2/TestPrepatch.json`）：

```json
[
  {
    "enumType": "SPTarkov.Server.Core.Models.Enums.SkillTypes",
    "constantName": "TestSkillEntry",
    "constantValue": 100
  }
]
```

### 打补丁语义（`EnumPatcher`）

`Modding/EnumPatcher.cs:10-80`：
- 按 `entry.EnumType` 在模块里按 `FullName` 精确找类型；找不到或非 enum → `ModLoaderException`（`:20-30`）。
- 常量名已存在 → `ModLoaderException`（`:32-35`）；常量值已存在 → `ModLoaderException`（`:37-40`）。
- 新建 `FieldDefinition`（`Public|Static|Literal|HasDefault`），`ConvertConstant` 按底层类型 `I1/U1/I2/U2/I4/U4/I8/U8` 转换并做溢出检查（`:42-79`）；不支持的类型抛异常（`:70`）。

### prepatch 宿主上下文（`PrepatchLoadContext`）

`Modding/PrepatchLoadContext.cs:9-69`：
- `ContextName = "SPT.PrepatchHost"`（`:12`）。
- 加载名为 `SPTarkov.Server.Core` 的程序集时，从内存字节流加载 patched Core（可选 pdb）（`:29-41`）。
- 框架程序集与默认上下文共享：前缀 `Microsoft.`、`System.`、`MudBlazor`、`ZLogger`、`0Harmony`、`MonoMod.`、`Mono.Cecil`（`:16-25,43-59`）。
- 其余程序集经 `AssemblyDependencyResolver` 解析（`:27,61-62`）。

## 最小示例：元数据 + 生命周期 + 自定义 DI

来自 `Testing/TestMod/TestMod.cs`：

```csharp
public sealed class TestModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.example.mymod";
    public string Name { get; init; } = "my-mod";
    public string Author { get; init; } = "Me";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("1.0.0");
    public Range SptVersion { get; init; } = new("~5.0.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}

// 容器构建前手动注册自有服务（可选）
[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class MyPreload(ISptLogger<MyMod> logger, MyService service) : IOnDIConstruct, IOnLoad
{
    public static Task OnDIConstructAsync(IServiceCollection services, CancellationToken token)
    {
        services.AddSingleton(new MyService());
        return Task.CompletedTask;
    }

    public Task OnLoadAsync(CancellationToken token)
    {
        logger.Info(service.Hello);
        return Task.CompletedTask;
    }
}
```

（`TestMod.cs:11-47`；`IOnDIConstruct` 静态方法由 `ServiceCollectionExtensions.AddModDIConstructorsAsync` 反射调用，`SPTushonka.Server/Extensions/ServiceCollectionExtensions.cs:11-53`。）

路由注册见 http-routing.md（继承 `StaticRouter`/`DynamicRouter`/`ItemEventRouter` 并标 `[Injectable(TypePriority = OnLoadOrder.Routers)]`）。生命周期两阶段执行见 architecture-map.md。

## 与 4.1 的关系

- `ModLoader` / `ModValidator` 逻辑一致；`IModMetadata` 11 个属性**完全相同** → 加载规则不变、mod 元数据类无需改动。
- 版本号变化：`SptVersion` 由 `4.1.5` 变为 `5.0.0`；老 mod 的 `SptVersion` 区间不含 `5.0.0` 会被拒载。
- 注意官方示例仍写 `SptVersion = new("~4.1.0")`（`Testing/TestMod/TestMod.cs:18`、`TestMod2/TestMod2.cs:19`）——在 5.0 上不会满足 `Satisfies`，需自行改为 `~5.0.0` 才能加载。

## 已核实位置

- 元数据：`Libraries/SPTushonka.Server.Core/Models/Spt/Mod/IModMetadata.cs:22-26,30-102`
- 加载器：`SPTushonka.Server/Modding/ModLoader.cs:20-22,28-59,61-89,91-144,150-182,188-219,235-246,253-297,307-366,368-416,418-440`
- 校验器：`SPTushonka.Server/Modding/ModValidator.cs:22-110,116-137,154-211,238-324,331-368`
- prepatch：`SPTushonka.Server/Modding/EnumPatcher.cs:10-80`、`PrepatchLoadContext.cs:9-69`、`PrepatchAssemblyWriter.cs:6-13`
- 定义模型：`Libraries/SPTushonka.Server.Core/Models/Spt/Mod/EnumEntryDefinition.cs:8-39`、`SptMod.cs:6-15`
- 示例：`Testing/TestMod/TestMod.cs:11-47`、`Testing/TestMod2/TestMod2.cs:12-65`、`Testing/TestMod2/TestPrepatch.json`
- `IOnDIConstruct` 调用：`SPTushonka.Server/Extensions/ServiceCollectionExtensions.cs:11-53`
- 阶段常量：`Libraries/SPTushonka.Server.Core/DI/OnLoadOrder.cs:3-15`
- 启动链调用：`SPTushonka.Server/Program.cs:187-197`
