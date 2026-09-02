---
version: [4.1]
domain: client
topic: migration
source: curated
---

# 客户端混淆名映射（KmyTarkovApi 实战解析）

> 状态：已验证（2026-08-07，Mono.Cecil 从 4.1.1 Assembly-CSharp.dll 实读解析 + 全解决方案 6 项目编译 0 错误）
> 用途：API 库型客户端 mod（3.11 -> 4.1）的混淆名映射，与 LootingBots 案例互补（本案例覆盖"库"类 mod）
> 方法：成员签名匹配（同 LootingBots）+ 编译循环修复

---

## 1. 核心映射表（KmyTarkovApi 15 个混淆名）

| 3.11 混淆名 | 4.1 真名 | 判定依据 |
|------------|---------|---------|
| `ISession` | `EFT.IEftSession` | 接口继承链含 Traders/BackEndConfig |
| `LauncherItemClass` | `EFT.InventoryLogic.Launcher` | FirearmController.UnderbarrelWeapon 返回类型 |
| `MainMenuControllerClass` | `EFT.MainMenuShowOperation` | 静态 Execute → Task<MainMenuShowOperation> + Unsubscribe() |
| `LocaleManagerClass` | `EFT.LocalizationManager` | 静态属性 Instance + Culture（原 String_0） |
| `PoolManagerClass` | `EFT.ObjectsFactory` | LoadBundlesAndCreatePools + PoolsCategory/AssemblyType 嵌套类型 |
| `LoadingProgressStruct` | `EFT.InitLevelProgress` | LoadBundlesAndCreatePools 的 IProgress 泛型参数 |
| `JobPriorityClass` | `Diz.Jobs.EJobPriority` | 枚举 Low/General/Immediate（**3.11 静态类 → 4.1 枚举**，反射属性改直接取值） |
| `ResourceKeyManagerAbstractClass` | **无（4.1 已删除）** | voice 数据改由 EFT.CustomizationSolver._voices 实例字典管理（键 MongoID）→ 降级 null |
| `BackendConfigSettingsClass` | `EFT.GlobalConfiguration` | BackEndConfig.Config 字段类型 |
| `AbstractQuestControllerClass` | `EFT.Quests.QuestController` | Player.QuestController 属性 + Quests 属性 |
| `QuestClass` | `EFT.Quests.Quest` | QuestBook : BindableList<Quest> : IEnumerable<Quest> |
| `ProfileHealthClass` | `EFT.Profile/HealthInfo` | Profile 嵌套类型 |
| `NetworkHealthControllerAbstractClass` | `EFT.HealthSystem.NetworkHealthController` | 命名空间化 |
| `StashGridClass` | `EFT.InventoryLogic.Grid` | Grid.Items + CompoundItem.Grids 返回 Grid[] |
| `DamageInfoStruct` | `EFT.Ballistics.DamageInfo` | 加 using EFT.Ballistics |

## 2. 关键事实（本案例新增认知）

1. **库类 mod 的移植陷阱**：KmyTarkovApi 是多项目解决方案（Api/Configuration/Reflection/Utils/Build），编译错误会跨项目传染。改主项目时同源项目（KmyTarkovConfiguration 引用了 LocaleManagerClass）也要同步改。
2. **3.11 静态类 → 4.1 枚举**：JobPriorityClass（静态类带常量）在 4.1 变成 `Diz.Jobs.EJobPriority` 枚举。反射代码中原来按类名反射取静态属性的逻辑要改成直接引用枚举值。
3. **类型被删除的处置**：ResourceKeyManagerAbstractClass 在 4.1 无对应物（功能重构）。处置规范：标注 `// PORT-NOTE:` + 降级返回 null + 调用方判空。**不要**为了编译通过而强行映射到错误类型。
4. **4.1 方法重载增多**：PoolManagerClassHelper 的 GetMethod 因 4.1 存在 2 个重载，改用按参数类型过滤（RefTool.GetEftMethod 按 PoolsCategory 参数）。
5. **全局 Timer 遮蔽**：Assembly-CSharp.dll 有全局命名空间 `Timer` 类遮蔽 `System.Threading.Timer` → 全限定 `System.Threading.Timer`（与 LootingBots 的 ObjectPool 遮蔽同族问题）。

## 3. 移植工作流（库类 mod 完整走通）

1. 复制源码到工作区（`D:\Temp\opencode\m2-port\`）
2. 建 `Refs-410\`：从 `SPT_410\EscapeFromTarkov_Data\Managed` + `BepInEx\core` 复制引用 dll；第三方（Crc32.NET、HtmlAgilityPack）从 NuGet 下载
3. 改 csproj：HintPath → `..\..\Refs-410\`、删 SignAssembly（pfx 缺失）、保留 net472
4. 编译 → 收集混淆名错误 → Mono.Cecil 成员签名匹配 → 批量替换 → 循环至 0 错误
5. 验证：独立重建（删 bin/obj + /t:Rebuild）确认 0 错误

**结果**：全解决方案 6 项目编译 0 错误，KmyTarkovApi.dll 产出（1.5.0.0）。

## 4. 可复用结论

1. **库类 mod 映射成功率**：14/15 混淆名解析成功，1 个（ResourceKeyManagerAbstractClass）确认 4.1 删除并降级——与 LootingBots 案例同为高成功率。
2. **M2 移植队列的参考**：B 桶 55 个 mod 中依赖 KmyTarkovApi 的（如 GamePanelHUD 456）可在其 DLL 就绪后编译。
3. **跨项目改名注意**：多项目解决方案中，公共类型改名要全局替换（含引用它的兄弟项目），不只是报错文件。

---

# 附：GamePanelHUD / VCQL 移植补充（2026-08-07 同日完成）

## GamePanelHUD（456，10 项目）新增映射

| 3.11 混淆名 | 4.1 真名 | 说明 |
|------------|---------|------|
| `ThrowWeapItemClass` | `EFT.InventoryLogic.ThrowWeap` | ThrowType 属性匹配 |
| `LauncherItemClass` | `EFT.InventoryLogic.Launcher` | Chambers/WeaponTemplate 匹配 |
| `MagazineItemClass` | `EFT.InventoryLogic.Magazine` | Cecil 成员匹配 |
| `SearchableItemItemClass` | `EFT.InventoryLogic.SearchableItem` | Cecil 成员匹配 |
| `GamePanelHUDCorePlugin.HUDCoreClass` | `GamePanelHUDCore.Models.HUDCoreModel.Instance` | 旧静态单例→新单例模型 |
| `GamePanelHUDCorePlugin.HUDClass<,>` | 已移除 | 4.1 删除通用 HUD 机制，数据改存插件静态字段 |

**新陷阱**：4.1 的 `EFT` 命名空间新增了 `IUpdate` 接口，与 `KmyTarkovUtils.IUpdate` 冲突 → 用 `using IUpdate = KmyTarkovUtils.IUpdate;` 显式别名。IL 操纵补丁中的 `typeof(DamageInfoStruct)` 也要同步改。

## VCQL（649，单项目）新陷阱

| 3.x | 4.1 | 说明 |
|-----|-----|------|
| `ConsoleScreen`（bsg.console.core.dll） | `EFT.UI.ConsoleScreen`（Assembly-CSharp.dll） | 删 bsg 引用，using EFT.UI 覆盖 |
| 全局 `Utils`（4.1 新增混淆工具类） | — | 与 mod 自己的 Utils 类冲突 → `Core.Utils.` 限定 |
| `TriggerWithId.Awake()` 非虚 | `virtual` | 子类加 `override`（CS0114） |

## 部署注意（关键！）

MSBuild CopyLocal 会把**全部引用 dll** 复制到 bin\Release。部署到 BepInEx\plugins 时**只放 mod 本体 DLL**，不要把 Assembly-CSharp.dll / Comfort.dll / spt-*.dll 一起丢进去（游戏本体已有，重复会冲突）。

## M2 工作区规范（已沉淀）

- 工作区：`D:\Temp\opencode\m2-port\`（Refs-410 共享引用 + 各 mod 独立工作副本 `{Mod}-src\`）
- 只读源在 `knowledge\spt-kb\archive\forge\mods\<id>_source\`，工作副本自建
- csproj 改造模板：HintPath→`..\..\Refs-410\`、删 SignAssembly、保留 net472
- 第三方 dll 从 NuGet 拉取（nupkg 改 .zip 扩展名后 Expand-Archive）
- 委派 fixer 时给：工作区路径 + 已确认映射表 + Mono.Cecil 方法 + 编译命令
