# 1594 — Progressive Bot System 重编译记录（SPT 4.0 → 4.1.2）

## 基本信息

- Mod: Acid's Progressive Bot System（Acid 的 AI 装备管理系统）
- Mod GUID: `com.acidphantasm.progressivebotsystem`
- 原版本: 2.0.8（SPT 4.0 编译，SptVersion 已补丁为 `~4.1.0`）
- 源 DLL: `c-bucket-extracted\1594\SPT\user\mods\acidphantasm-progressivebotsystem\acidphantasm-progressivebotsystem.dll`（1,877,504 字节）
- 重编译目标: SPTarkov.Server.Core / SPTarkov.DI 4.1.2，net10.0
- 产物: `acidphantasm-progressivebotsystem.dll`（1,982,464 字节）
- 构建: 0 警告 0 错误（`dotnet build -c Release`）
- 日期: 2026-08-09

## 编译环境

- .NET SDK 10.0.300
- NuGet: SPTarkov.Server.Core 4.1.2、SPTarkov.DI 4.1.2、SemanticVersioning 3.0.0
- HintPath 引用（来自 `E:\Game\EFT_Offline\SPT_410\SPT_Runtime\`，4.1.2 服务端）:
  0Harmony.dll、MudBlazor.dll（9.7.0）、SPTarkov.Common.dll、SPTarkov.Reflection.dll、SPTarkov.Server.Web.dll
- 反编译: ilspycmd 10.1.1，C# 8 语言版本全量（172 文件）+ C# 14 单类型覆盖 11 个主构造器类

## 改动清单（4.0 → 4.1.2 API 迁移）

1. **ModMetadata.cs**（重写）:
   - `: AbstractModMetadata, IModWebMetadata` → `: IModMetadata, IModBlazorMetadata`
   - `set` → `init`（IModMetadata 全部属性 init）
   - 新增 `HasPrepatcher = false`（4.1.2 必填）
   - `License` 变为非空 string（保留 `CC BY-NC-ND 4.0`）
   - 移除 record 样板代码（EqualityContract/Equals/GetHashCode 等）与 `IsBundleMod`
   - Incompatibilities 保留 `["li.barlog.andern"]`，SptVersion `~4.1.0`，Version 2.0.8

2. **IOnLoad 接口**（9 个类）: `OnLoad()` → `OnLoadAsync(CancellationToken)`
   - PatchManager、ProgressiveBotSystem、ModConfig、DataLoader、BotActivityHelper、BotConfigHelper、BotEquipmentHelper、BotQuestHelper、VanillaItemHelper

3. **DatabaseService → 表模型直接注入**（4.1.2 移除 DatabaseService）:
   - `_databaseService.GetBots()` → `BotTable` 注入（`.Types`）
   - `_databaseService.GetItems()` → `TemplateTable` 注入（`.Items`，键 MongoId）
   - `_databaseService.GetCustomization()` → `TemplateTable.Customization`
   - `_databaseService.GetGlobals()` → `GlobalTable` 注入（`.Configuration` / `.ItemPresets`）
   - 涉及: BotConfigHelper、VanillaItemHelper、CustomBotWeaponGenerator、GenerateBotLevel（ServiceLocator）、SetBotAppearance_Patch（ServiceLocator）

4. **ConfigServer → 配置模型直接注入**（4.1.2 移除 ConfigServer，`GetConfig<T>()` 改 DI 注入）:
   - CustomBotWeaponGenerator: `BotConfig` + `PmcConfig` + `RepairConfig`
   - CustomBotEquipmentModGenerator: `BotConfig`
   - CustomBotLootGenerator / CustomBotInventoryGenerator / BotConfigHelper: `BotConfig` + `PmcConfig`

5. **ServiceLocator**（4.1.2 移除 `SPTarkov.Server.Core.DI.ServiceLocator`）:
   - 新建 `_progressiveBotSystem.ServiceLocator` 静态类（IServiceProvider 容器）
   - PatchManager 构造器注入 `IServiceProvider`，`OnLoadAsync` 中赋值
   - Harmony patch 类（GenerateBotLevel / SetBotAppearance_Patch / GenerateInventory_Patch / SetRandomisedGameVersionAndCategory_Patch / AddDogTagToBot_Patch / StaticRouterHooks / ApbsInventoryMagGen）继续使用 `ServiceLocator.ServiceProvider.GetRequiredService<T>()`

6. **命名空间重组**（4.1.2 子命名空间化）: 通过 GlobalUsings.cs 全局引入
   - `Helpers.Items` / `Helpers.Bot` / `Helpers.Profile` / `Helpers.InRaid` / `Helpers.Server`
   - `Services.Locales` / `Services.Bot` / `Services.Profile` / `Services.Commerce` / `Services.Items` / `Services.Server`
   - `Generators.Bot` / `Generators.Loot` / `Common.Models.Logging`

7. **ISptLogger / 日志 API**:
   - `ISptLogger<T>` 移入 `SPTarkov.Common.Models.Logging`
   - `LogTextColor/LogBackgroundColor` 枚举已移除（4.1.2 用 Spectre.Console.Color）→ ApbsLogger 的 `LogWithColor` 调用改为默认颜色
   - `LogLevel` 来自 `Microsoft.Extensions.Logging`

8. **MongoId API**:
   - `MongoId.op_Implicit(x)` 显式调用（~100 处）→ `(MongoId)(x)` 或 `new MongoId(x)`
   - `((MongoId)(ref x))._002Ector(...)` / `..ctor()` 反编译产物 → `x = new MongoId(...)`
   - `MongoId?` 值传递简化（`val3.ParentId = equipment`）

9. **编译器生成代码修复**（ilspycmd 反编译产物）:
   - 删除 `[RequiredMember]` / `[CompilerFeatureRequired]`（225 行，record 降级产物）
   - 删除 `base._002Ector();`（12 文件）
   - InlineArray 块（`_003C_003Ey__InlineArray*` + `PrivateImplementationDetails`）→ 普通 `FrozenSet.Create(new[] {...})` / `Task.WhenAll(a, b)`
   - `<>z__ReadOnlyArray` / `<>z__ReadOnlySingleElementList` → 数组 / `List<T>`
   - CSharp14 覆盖 11 个主构造器类（primary constructor 正常反编译，消除 `_003Cxxx_003EP` 无声明字段问题）

10. **路由 API**（4.1.2 StaticRouter）:
    - `RouteAction<T>` 泛型移除 → 非泛型 `RouteAction(url, Func<string, IRequestData, MongoId, string?, CancellationToken, ValueTask<object>>, bodyType)`
    - StaticRouterHooks 重写 5 条路由注册

11. **MudBlazor 9.7.0**:
    - `IDialogService.Show<T>()` → `ShowAsync<T>()`（Presets.cs）
    - `LightBlue.Lighten2` 等 → `Colors.LightBlue.Lighten2`（Home.cs）
    - `DialogOptions.set_MaxWidth` 反编译产物 → 对象初始化器
    - `Color` / `Path` / `Appearance` 命名冲突 → using 别名

12. **其他**:
    - `((AbstractModMetadata)new ModMetadata()).Version` → `new ModMetadata().Version`（ProgressiveBotSystem / Home）
    - `BaseClasses` / `ItemTpl` 缺失 using 补充（`Models.Enums`）
    - `GlobalTable.Configuration.Exp.Level.ExperienceTable`（GenerateBotLevel）
    - switch 表达式 `(MongoId)value.Parent switch` → `value.Parent.ToString() switch`

## 部署

- 目标: `E:\Game\EFT_Offline\Inescapable Tarkov\mods\[6]Acid的AI装备管理系统-progressivebotsystem - 已AI优化\SPT_Runtime\user\mods\acidphantasm-progressivebotsystem\acidphantasm-progressivebotsystem.dll`
- 已备份原 4.0 DLL 为 `acidphantasm-progressivebotsystem.dll.spt40-backup`
- 未触碰 mod 的 Data / Presets / wwwroot / config.json / blacklists.json

## 文件

- `acidphantasm-progressivebotsystem.dll` — 4.1.2 重编译产物（1,982,464 字节）
- `acidphantasm-progressivebotsystem.dll.spt40-original` — 4.0 原版（1,877,504 字节，SptVersion 已补丁 ~4.1.0）

## 备注 / 潜在风险

- ServiceLocator 为 mod 内实现（4.1.2 服务端无此类型），依赖 PatchManager 在 OnLoadAsync 时注入 IServiceProvider；Harmony patch 执行时机晚于服务端 DI 构建，正常成立。
- Harmony 补丁目标方法（如 `BotGenerator.SetBotAppearance`、`BotLevelGenerator.GenerateBotLevel`）若在 4.1.2 中签名变化，patch 会静默失效（AccessTools.Method 运行时解析），需实机验证。
- Web UI 依赖 MudBlazor 9.7.0（服务端自带版本）。
- 编译 0 错误；未进行服务端启动实测（需用户实机验证）。
