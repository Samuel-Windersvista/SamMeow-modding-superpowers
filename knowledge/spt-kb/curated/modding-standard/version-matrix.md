---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 版本差异对照表（4.1.5 ↔ 5.0）

> 定位：汇总 4.1.5 与 5.0 的机制差异，支撑规则集的 `Applies` 标签与版本分支判定（见 [README.md](README.md)）。
> 规则集为**单份规范 + 规则级版本标签**，不维护两份并行规范（ADR-0005）。
> 证据来源以 KB 文档路径（相对本目录）与 `5.0x-dev` 源码路径（`SPTushonka.*`）为准。
> 状态：ticket 07 交付（2026-09-14）。SPT 5.0 已由社区 fork 正式发布（2026-09-14 确认，ADR-0006）；本矩阵基于预发布快照 HEAD `ff0bf3281` 核实，5.0 正式版的差异需按 release tag 复核。

## 0. `Applies` 非 both 的规则清单

规则集中 `Applies` 非 `both` 的条目全部为 `5.0`（无 `4.1.5`-only 规则）：

| Rule ID | Level | Applies | 规则文件 | 本表对应机制主题（章节） |
|---------|-------|---------|----------|--------------------------|
| STD-VER-002 | SHOULD | 5.0 | [11-version-differences.md](11-version-differences.md) | 服务端 mod 骨架、SPT 版本号（§2） |
| STD-VER-003 | MAY | 5.0 | [11-version-differences.md](11-version-differences.md) | 新增内容系统（§4） |
| STD-VER-004 | SHOULD | 5.0 | [11-version-differences.md](11-version-differences.md) | 客户端补丁目标类型（§1）、目标客户端 EFT 版本（§2） |
| STD-VERIFY-005 | SHOULD | 5.0 | [10-verification.md](10-verification.md) | 结构化状态断言（§4） |

## 1. 客户端运行时与构建（BUILD / CLI）

| 机制主题 | 4.1.5 行为 | 5.0 行为 | 对规则的影响（Rule ID） | 证据来源 |
|----------|-----------|----------|------------------------|----------|
| 客户端运行时 | Mono / BepInEx 5 | IL2CPP / BepInEx 6 | STD-BUILD-002、STD-BUILD-003、STD-CLI-001、STD-CLI-006、STD-CLI-007 | [../api-notes-5.0/mod-loading.md](../api-notes-5.0/mod-loading.md)；[03-build.md](03-build.md)、[05-client.md](05-client.md) |
| 客户端目标框架 | `netstandard2.1`（`net472`/`net471` 亦可），禁 `net10.0`/`net9.0` | `net6.0` | STD-BUILD-002 | [03-build.md](03-build.md)；`mods/SPT5-NoStaminaDrain/SPT5NoStaminaDrain.csproj:5-9` |
| 客户端程序集引用路径 | `$(SPTInstallPath)\EscapeFromTarkov_Data\Managed\*.dll` | `$(SPT5Path)\BepInEx\interop\*.dll`（Il2CppInterop 代理）与 `$(SPT5Path)\BepInEx\core\*.dll` | STD-BUILD-003 | [03-build.md](03-build.md)；`mods/SPT5-NoStaminaDrain/SPT5NoStaminaDrain.csproj:40-47` |
| 客户端插件入口 | `BaseUnityPlugin` + `Awake()` | `BepInEx.Unity.IL2CPP.BasePlugin` + `Load()` | STD-CLI-001 | [05-client.md](05-client.md)；`mods/SPT5-NoStaminaDrain/src/Plugin.cs:9-19` |
| 客户端日志源 | `BaseUnityPlugin.Logger` | `BasePlugin.Log`（`ManualLogSource`） | STD-CLI-006、STD-LOG-003 | [05-client.md](05-client.md)、[07-logging.md](07-logging.md) |
| 补丁撤销时机 | `Awake` 应用、`OnDestroy` 撤销 | `Load()` 应用、`Unload()`（`BasePlugin`）或 `Dispose()`（组件）撤销（`BasePlugin` 无 `IDisposable.Dispose()`） | STD-CLI-007 | [05-client.md](05-client.md)；`external/references/bepinex-mcp/plugins/BepInExMCP.IL2CPP/Plugin.cs:121-126`、`tools/tarkov-runtime-bridge/src/Plugin.cs`（`Unload()`，当前 99-130） |
| 客户端补丁目标类型 | 4.1 反混淆后的真实类型名 | EFT 1.1.5 类名需重新核对 | STD-CLI-004、STD-VER-004 | [../migration/client-mod-311-to-41.md](../migration/client-mod-311-to-41.md)；`docs/eft-1.1.5-类名映射重建报告.md` |
| 安装路径属性名 | `SPTInstallPath`（`Condition` 可覆盖） | 可版本化自定义：`SPT5Path`、`SPT5Runtime`（仍须可覆盖） | STD-BUILD-006 | [03-build.md](03-build.md)；`mods/SPT5-NoStaminaDrain/SPT5NoStaminaDrain.csproj:19-20`、`tools/tarkov-active-probe/TarkovActiveProbe.csproj:17,19` |

## 2. 服务端框架与版本声明（BUILD / META / VER）

| 机制主题 | 4.1.5 行为 | 5.0 行为 | 对规则的影响（Rule ID） | 证据来源 |
|----------|-----------|----------|------------------------|----------|
| 服务端目标框架 | `net10.0` | `net10.0`（未变） | STD-BUILD-001 | [../operations/5xx-source-verification.md](../operations/5xx-source-verification.md)（`TargetFramework` 相同） |
| SPT 版本号 | `SptVersion=4.1.5`；mod 用 `~4.1.x` 区间 | `SptVersion=5.0.0`；mod 须改 `~5.0.x`，否则 `Satisfies` 不满足被拒载 | STD-META-004、STD-VER-002 | [../api-notes-5.0/mod-loading.md](../api-notes-5.0/mod-loading.md)；[../operations/5xx-source-verification.md](../operations/5xx-source-verification.md) |
| 目标客户端 EFT 版本 | `compatibleTarkovVersion=0.16.9.40743` | `1.1.5.0.47242`（跨大版本线） | STD-VER-004 | [../operations/5xx-source-verification.md](../operations/5xx-source-verification.md) |
| 服务端 mod 骨架 | `IModMetadata`（11 属性）、`[Injectable]`、`IOnLoad`、路由、配置注入 | 完全相同（`git diff` 对 `Models/Spt/Mod`、`Modding`、`DI` 无输出） | STD-VER-002（沿用 4.1 骨架） | [../api-notes-5.0/architecture-map.md](../api-notes-5.0/architecture-map.md)、[../api-notes-5.0/mod-loading.md](../api-notes-5.0/mod-loading.md) |
| 服务端程序集引用校验 | 引用 `SPTarkov.Server.Core` 版本高于运行时 → 抛异常 | 逻辑一致（先于其它校验、不可隔离） | STD-BUILD-004 | [../api-notes-5.0/mod-loading.md](../api-notes-5.0/mod-loading.md) |
| 命名空间前缀 | `SPTarkov.*`（如 `SPTarkov.Server.Core`、`SPTarkov.Common.Models.Logging`） | `SPTushonka.*`（如 `SPTushonka.Server.Core`、`SPTushonka.Common.Models.Logging`） | STD-BUILD-004、STD-CFG-003、STD-LOG-001 | [06-config.md](06-config.md)、[07-logging.md](07-logging.md)；[../api-notes-5.0/di-container.md](../api-notes-5.0/di-container.md) |

## 3. 服务端 DI / 生命周期 / 路由 / 配置 / 加载（SRV / CFG / DEP）

| 机制主题 | 4.1.5 行为 | 5.0 行为 | 对规则的影响（Rule ID） | 证据来源 |
|----------|-----------|----------|------------------------|----------|
| DI 注解与容器 | `[Injectable(InjectionType, TypePriority)]`，默认 `Transient` / `int.MaxValue` | 完全相同 | STD-SRV-001、STD-SRV-002 | [../api-notes-5.0/di-container.md](../api-notes-5.0/di-container.md) |
| 生命周期接口 | `IOnLoad` / `IOnUpdate` / `IOnDIConstruct` | 相同；`IOnLoad` 由两执行器按 `TypePriority` 分段（`GameCallbacks` 为界） | STD-SRV-003、STD-SRV-004 | [../api-notes-5.0/di-container.md](../api-notes-5.0/di-container.md)、[../api-notes-5.0/architecture-map.md](../api-notes-5.0/architecture-map.md) |
| 路由基类与 action | `StaticRouter` / `DynamicRouter` / `RouteAction<T>` | 基类相同；新增 `StreamedRouteAction<T>`；删除 `GetClientDialogueRequestData`、`GetAchievementListRequest` | STD-SRV-005、STD-SRV-006、STD-SRV-007 | [../api-notes-5.0/http-routing.md](../api-notes-5.0/http-routing.md) |
| 配置系统 | 无 `ConfigServer`；静态 `ConfigLoader` 读 `SPT_Data/configs`，按 CLR 类型注册 DI 单例；mod 配置走 `IOnDIConstruct` + `AddSingleton` | 形态一致；命名空间前缀改 `SPTushonka.*` | STD-CFG-001…STD-CFG-006 | [../api-notes-5.0/config-system.md](../api-notes-5.0/config-system.md) |
| mod 加载目录与元数据 | `./user/mods/` 一级子目录 = 一个 mod，顶层 `.dll`，不用 `package.json` | 逻辑一致；`IModMetadata` 11 属性完全相同 | STD-META-001、STD-PKG-004 | [../api-notes-5.0/mod-loading.md](../api-notes-5.0/mod-loading.md) |
| 加载顺序 | `TypePriority` 升序，同优先级按 `ModGuid` tiebreaker；无 `loadBefore`/`loadAfter` | 相同 | STD-SRV-002、STD-DEP-001 | [../api-notes-5.0/mod-loading.md](../api-notes-5.0/mod-loading.md) |
| 依赖声明语义 | `ModDependencies` 硬依赖，仅校验不参与排序 | 相同 | STD-DEP-001、STD-DEP-002、STD-DEP-003 | [../api-notes-5.0/mod-loading.md](../api-notes-5.0/mod-loading.md) |

## 4. 5.0 新增内容系统与验证手段（VER / VERIFY）

| 机制主题 | 4.1.5 行为 | 5.0 行为 | 对规则的影响（Rule ID） | 证据来源 |
|----------|-----------|----------|------------------------|----------|
| 新增内容系统 | 无 | 赛季/通行证/tarcoin 商店/结局/剧情任务链/教程；新表 `SeasonTable`、`ShopTable` | STD-VER-003（可选扩展点，旧 mod 不强制使用） | [../operations/5xx-source-verification.md](../operations/5xx-source-verification.md)、[../api-notes-5.0/modding-api.md](../api-notes-5.0/modding-api.md) |
| `ProfileChange` / 请求模型 | 旧字段集 | 新增 `SeasonalRewards`、`BattlePassProgress` 等字段；删除部分请求模型 | 无直接规则（构造/序列化 `ProfileChange` 的 mod 需补字段） | `docs/spt-5.0-mod-api-能力评估报告.md` |
| 结构化状态断言 | 以日志断言为主 | `tarkov-runtime` MCP：`tarkov_server_status` / `tarkov_wait_for` / `tarkov_snapshot` | STD-VERIFY-005 | `tools/tarkov-runtime-mcp/src/index.ts`、`.scratch/tarkov-runtime-mcp/spec.md` |
| 架构代际（3.11 → 4.1 → 5.0） | C# + `SPTarkov.DI`（4.1 起） | 同 4.1；3.11 的 TS/tsyringe/`ConfigServer`/`DatabaseServer` 机制在 4.1 与 5.0 均不存在 | 规则集基线（META / SRV / CFG 均以 C# 形态为前提） | [../migration/api-mapping-311-to-41.md](../migration/api-mapping-311-to-41.md) |

## 5. 证据形态不对称（记录在案）

- **4.1.5**：mod 语料（`archive/forge/mods/`）+ 文档（modding-guide / api-notes-4.1 / wiki-tushonka）。
- **5.0**：源码（`5.0x-dev`）+ 源码实读笔记（api-notes-5.0）；**暂无 mod 语料**，涉及 5.0 的规则多为机制推断（登记 `EV-NOCORPUS`）。
- 详见 [evidence-index.md](evidence-index.md) 与 [README.md](README.md) 的「版本标签与证据形态」。

---

> 一致性核对（抽查）：本表行与各规则文件的 `Applies` / 版本分支文本一致——`STD-BUILD-002/003/006`、`STD-CLI-001/006/007` 的 5.0 分支见 [03-build.md](03-build.md)、[05-client.md](05-client.md)；`STD-CFG-003`、`STD-LOG-001` 的 `SPTushonka.*` 命名空间注记见 [06-config.md](06-config.md)、[07-logging.md](07-logging.md)；`STD-VER-002/003/004` 与 `STD-VERIFY-005` 的 `Applies: 5.0` 见 [11-version-differences.md](11-version-differences.md)、[10-verification.md](10-verification.md)。
