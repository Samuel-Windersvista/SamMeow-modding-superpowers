# 2614 - RZCustomProfiles 4.1.2 重编译记录

## 概述
- 来源：c-bucket 2614（SPT 4.0 服务端 mod "RZCustomProfiles"，新增开局角色）
- 产物：`RZCustomProfiles.dll`（net10.0，55808 字节）
- 部署：`E:\Game\EFT_Offline\Inescapable Tarkov\mods\[5]新增5个开局角色-CustomProfiles\SPT_Runtime\user\mods\RZCustomProfiles\RZCustomProfiles.dll`
- 原 4.0 DLL 保留为同目录 `RZCustomProfiles.dll.bak`

## 4.0 -> 4.1.2 API 迁移
| 项目 | 4.0 | 4.1.2 |
|---|---|---|
| 元数据 | `record ModMetadata : AbstractModMetadata` | `class ModMetadata : IModMetadata`（set->init，补 `HasPrepatcher`） |
| 加载接口 | `IOnLoad.OnLoad()` | `IOnLoad.OnLoadAsync(CancellationToken)` |
| 数据库访问 | `DatabaseService`（已移除） | 注入表模型 `TemplateTable` / `TradersTable` / `LocaleTable` / `GlobalTable` |
| 命名空间迁移 | `Helpers.ModHelper` / `Helpers.InventoryHelper` / `Helpers.PrestigeHelper` / `Services.LocaleService` / `Services.CreateProfileService` | `Helpers.Server.*` / `Helpers.Profile.*` / `Services.Locales.*` / `Services.Profile.*` |
| 模型迁移 | `Models.Spt.Templates.TemplateItem*` | `Models.Eft.Common.Tables.TemplateItem*` |

## Injectable 属性（从 4.0 DLL 元数据 blob 精确解码）
4.0 构造器 `(InjectionType, Type?, int)` -> 4.1.2 `(InjectionType, int TypePriority)`：
- GlobalPatcher：Scoped，TypePriority=1100000
- ProfilesPatcher：Scoped，TypePriority=1100000
- ExaminedPatcher：Scoped，TypePriority=1100001
- HarmonyHook：Scoped，TypePriority=1100001
- ProfilesUtilities / AssortUtilities：Scoped（默认优先级）
- ConfigLoader：Singleton

## 依赖
- SPTarkov.Server.Core 4.1.2、SPTarkov.DI 4.1.2、SemanticVersioning 3.0.0
- HarmonyX 2.16.1（原 DLL 引用 0Harmony 2.15；服务器 SPT_Runtime 自带 0Harmony 2.16.1）
- FastCloner（经 SPTarkov.Server.Core 传递引用 3.5.5；调用形式 `FastCloner.FastCloner.DeepClone<T>()`）
- 元数据值按原 DLL 构造函数 IL 提取：ModGuid `com.rz.customprofiles`、Name `RZCustomProfiles`、Author `RemzDNB`、Version `1.1.0`、SptVersion `~4.1.0`（原 DLL 已补丁）、License `MIT`

## 行为保持要点
- `ResetAllTradersInProfile`（protected，4.1.2 仍在）前缀补丁 `ApplyPrestige` + 后缀补丁 `ApplyTradersLoyalty` / `ApplyAchievements`
- `PrestigeHelper.ProcessPendingPrestige(old, new, pending)` 三参签名未变
- `PrestigeHelper.AddPrestigeRewardsToProfile`（4.1.2 私有三参）前缀补丁 `SkipPrestigeRewards`（无参 prefix 兼容）
- 配置/配置文件读取逻辑原样保留（masterConfig.json / profiles/*.json）

## 验证
- `dotnet build -c Release`：0 错误 / 31 nullable 警告（原 4.0 无注解，属预期）
- 运行时反射验证（net10 + 4.1.2 程序集加载）：IModMetadata 实现与属性值、4 个 IOnLoad 的 `OnLoadAsync(CancellationToken)`、全部构造函数注入类型、Harmony 目标方法签名 —— 全部通过
- 部署后 SHA256 与构建产物一致
