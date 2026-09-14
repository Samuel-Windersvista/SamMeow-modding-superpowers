---
version: [5.0]
domain: server
topic: index
source: curated
---
# SPT 5.0 服务端 API 笔记（源码提炼）

> **[UNSTABLE-PREVIEW]** 注记基于 2026-09 的预发布快照（`5.0x-dev`）。SPT 5.0 已正式发布（2026-09-14，ADR-0006）；本篇 API 结论需按正式 release tag 复核，复核完成前维持 UNSTABLE 标记。

> 状态：**源码快照提炼（2026-09-13）** | 版本：[5.0]
> 源码位置：`E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code`（分支 `5.0x-dev`，HEAD `ff0bf3281`）
> 内容来源：`docs/spt-5.0-mod-api-能力评估报告.md`（2026-09-13，逐条 `文件:行号` 取证）
> 关联：`docs/spt-5.0x-dev-现状报告.md`（2026-09-05）——本系列更新其「配套缺失」结论；5.0 源码克隆记录见 `curated/operations/5xx-source-verification.md`（[5.0]）

这些是 wiki 里没有、只有读源码才能得到的硬情报。每条结论标 `文件:行号`，便于源码更新后复核。

## 一句话结论

**SPT 5.0 的服务端 mod API 完整且开放，框架层几乎原样继承 4.1。** 服务端 mod 具备 API 基础（`IModMetadata` + DI + Router + 自定义物品/任务 + Profile 持久化 + Web UI）；4.1 mod 移植到 5.0 属低成本。真正的能力缺口在**客户端侧（EFT 1.1.5 类名映射）与官方文档**，而非 API 缺失。

## 笔记规划

| 笔记 | 要回答的问题 | 源码入口线索 |
|------|------------|-------------|
| architecture-map.md | 启动链、请求链、目录职责、控制器/服务规模 | `SPTushonka.Server/Program.cs`、`Extensions/ProgramExtensions.cs` |
| mod-loading.md | `user/mods` 扫描、`IModMetadata`、`ModValidator` 校验规则、加载顺序、异常隔离、prepatch（EnumPatcher） | `SPTushonka.Server/Modding/ModLoader.cs`、`ModValidator.cs`、`EnumPatcher.cs` |
| di-container.md | `[Injectable]`、`InjectionType`、`TypePriority`、`IOnLoad`/`IOnUpdate`/`IOnDIConstruct`、`OnLoadOrder` 常量 | `Libraries/SPTushonka.DI/Annotations/Injectable.cs`、`Libraries/SPTushonka.Server.Core/DI/` |
| http-routing.md | `Router`/`StaticRouter`/`DynamicRouter`/`ItemEventRouter`/`SaveLoadRouter`、`RouteAction`、注册方式 | `Libraries/SPTushonka.Server.Core/DI/Router.cs`、`DI/Routing/ItemEventRouter.cs` |
| modding-api.md | `CustomItemService`、`CustomQuestService`、`ProfileDataService`、`SaveServer`、`IModBlazorMetadata`、`ISptLogger<T>`、其它扩展接口清单 | `Services/Modding/`、`Servers/SaveServer.cs`、`Libraries/SPTushonka.Server.Web/` |
| config-system.md | `ConfigLoader`、28 种 `*Config`、无 `ConfigServer` 的事实 | `Loaders/ConfigLoader.cs`、`Models/Enums/ConfigTypes.cs` |
| database-structure.md | `DatabaseTables`、强类型 Table 模型、各表注入 | `SPTushonka.Server/Helpers/DatabaseTables.cs` |
| save-profile.md | `SaveServer`、`ProfileDataService`、profile 数据 | `Servers/SaveServer.cs`、`Services/Modding/ProfileDataService.cs` |

## 与 api-notes-4.1 / api-notes-3.11 的关系

| 对照 | 结论 |
|------|------|
| vs `api-notes-4.1/`（C# 版） | **框架层无破坏性变更**：`git diff origin/4.1x-dev...5.0x-dev` 对 `Models/Spt/Mod`、`Modding`、`DI` 目录无输出。4.1 笔记的 DI/Router/生命周期写法**大部分可迁移**。 |
| vs `api-notes-3.11/`（TypeScript 版） | 3.11 的 `ConfigServer` / `DatabaseServer` / `package.json` / tsyringe 机制在 5.0 **不适用**；5.0 是 C# 形态，配置经 `ConfigLoader`、数据库经 `DatabaseTables`。 |

## 关键勘误（与 3.11 时代认知的差异，以源码为准）

1. **5.0 不存在 `ConfigServer` / `DatabaseServer` 类**（全库确认）。配置经 `ConfigLoader` 加载后按类型注册为单例；数据库经 `DatabaseTables.AddToServices` 注册各表。
2. **服务端 mod 无 `package.json` / `mod.json`**：形态为 `user/mods/<modDir>/<*.dll>`，DLL 内实现 `IModMetadata` 即被识别。
3. **加载顺序**：mod 列表按 `ModGuid` 字母序（`ModLoader.cs:143`）；可注入类/生命周期组件按 `TypePriority` 升序，`ModGuid` 作 tiebreaker（`IModMetadata.cs:22-26`、`DependencyInjectionHandler.cs:74`）。

## 写作规范

- 每条结论标源码位置：`文件路径:行号`（行号随源码演进可能漂移，以文件名为准）。
- 区分「公开 API」（mod 用，官方意图）与「内部实现」（能用但版本间会变）。
- 与 `api-notes-4.1/`、`api-notes-3.11/`、`curated/migration/` 交叉引用，不重复抄写。

## 稳定性警告

- SPT 5.0 已正式发布（2026-09-14）；笔记撰写时（2026-09-13）`5.0x-dev` 为开发分支，`5.0.0-BEM-20260909/0910` 为早期预发布标签。
- 5.0 生态成熟度未知（服务端 + 客户端 `modules` + `launcher` 三条 5.0 分支均已在 2026-09-13 出现，成熟度待验证）。
- **勿据此做长期承诺；生产整合包仍以 4.1.5 为稳定线，5.0 仅作能力预研**（策略变更需 Overseer 决策）。

## 本系列尚未覆盖

- **客户端侧（BepInEx / EFT 1.1.5 类名映射）不在本系列范围**：本系列只覆盖 SPT 5.0 **服务端** API；客户端 `Assembly-CSharp` 类名/命名空间映射需另立笔记（参见 `curated/migration/` 的 3.11→4.1 迁移流水线）。

## 已核实位置

- 元数据：`Libraries/SPTushonka.Server.Core/Models/Spt/Mod/IModMetadata.cs:30-102`
- 加载器：`SPTushonka.Server/Modding/ModLoader.cs:20,122,133-143,259-270,307-342`
- 校验器：`SPTushonka.Server/Modding/ModValidator.cs:91-95,156-211,243-324,331-365`
- 生命周期：`Libraries/SPTushonka.Server.Core/DI/IOnLoad.cs:5`、`DI/IOnUpdate.cs:3`、`DI/IOnDIConstruct.cs:24`、`DI/OnLoadOrder.cs:3`
- DI：`Libraries/SPTushonka.DI/Annotations/Injectable.cs:7`、`DependencyInjectionHandler.cs:74`
- 路由：`Libraries/SPTushonka.Server.Core/DI/Router.cs:22,62,93,148,179-239`、`DI/Routing/ItemEventRouter.cs:8,44,58`
- 自定义内容：`Services/Modding/Custom/CustomItemService.cs:47,139,404`、`Services/Modding/Custom/CustomQuestService.cs:24`
- 持久化：`Services/Modding/ProfileDataService.cs:76,102`、`Servers/SaveServer.cs:93,122,188,253`
- 配置/数据库：`Loaders/ConfigLoader.cs:16`、`SPTushonka.Server/Helpers/DatabaseTables.cs:6`
- Web：`Libraries/SPTushonka.Server.Web/IModBlazorMetadata.cs:21`、`SPTWeb.cs:24,94`
- Prepatch：`SPTushonka.Server/Modding/EnumPatcher.cs`、`PrepatchAssemblyWriter.cs`、`PrepatchLoadContext.cs`
- 启动链：`SPTushonka.Server/Program.cs:189-197,258`、`Extensions/ProgramExtensions.cs:16`
- 示例：`Testing/TestMod/`、`Testing/TestMod2/`
