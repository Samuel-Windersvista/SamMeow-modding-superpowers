# SkillsExtended 迁移试点 — STATUS

> 本文件是 C9「pilots 退出机制」（决策 D4）的状态标记，随试点保留原位；
> 归档动作待实机验证完成后执行（见下「归档去向」）。

## 状态（完成度）

**服务端完成；客户端编译通过但实机未验证。**（三合一试点：服务端 TS + 客户端 DLL + bundle）

| 项 | 事实 |
|---|---|
| 类型 | 服务端 mod（net10.0）+ 客户端插件（net472）+ bundle |
| 版本 | v1.6.0（`SkillsExtended.csproj` / `SkillsExtendedClient.csproj`） |
| 服务端源码 | `src/ModEntry.cs`（171 行）+ `src/ModMetadata.cs` + `src/SkillsRoutes.cs`（2 条静态路由） |
| 客户端源码 | `client-src/`（上游 C# 源码，25+ Harmony patch）+ `SkillsExtendedClient.csproj` 编译壳 |
| 资产 | `bundles.json`、`bundles/`、`config/`、`data/` |
| 试点结论 | `knowledge/spt-kb/curated/migration/pilot-experience-skills-extended.md`（状态：已验证，2026-08-05） |

## Owner

Samuel-Windersvista（Overseer）

## 验证状态

**客户端实机未验证**（`PROGRESS.md:43` 未勾选）。

| 项 | 结果 |
|---|---|
| 服务端编译 | 0 错误；1 既有警告（CS9113：`ModEntry.cs:55` 参数 `tradersTable` 未读取）——2026-09-17 C9 复验：修复 client-src glob 排除后可复现 |
| 服务端加载 | mod 加载成功，0 错误 |
| 服务端行为（dbdump） | Lockpick 物品（`6622c28a...`）创建成功 + 加入特殊槽位 filters；1 个配方；locale 导入成功 |
| 客户端编译 | 0 错误 0 警告（93 类型入 DLL，72 KB） |
| **客户端实机** | **未验证** |

客户端实机待确认的高优先级点：`DoorActionPatch` 目标方法名绑定
（`InteractionContextHelper.GetAvailableActions` 有 20+ 重载，`AccessTools.Method` 按参数类型精确匹配
已过编译，运行时需确认绑定成功）；lockpick / 医疗 / 消音器功能行为。

## 关闭条件

1. 客户端实机验证：SPT 4.1 启动，BepInEx 控制台确认 SkillsExtended 加载 + Harmony patch 绑定成功。
2. 进 raid 验证 lockpick / 医疗 / 消音器 / 门交互功能无回归。

## 归档去向

`examples/`（待上述验证完成后执行；本单不移动文件）。

---

> 相关：`tools/migration-pilots/skills-extended/PROGRESS.md`、
> `knowledge/spt-kb/curated/migration/pilot-experience-skills-extended.md`、
> `knowledge/spt-kb/curated/migration/client-obfuscation-mapping-skills-extended.md`。
