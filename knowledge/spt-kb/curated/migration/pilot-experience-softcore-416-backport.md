---
version: [4.1, 5.0]
domain: server
topic: migration
title: "SPT 5.0 → 4.1.6 反向移植实战：Inescapable-Tarkovs-Softcore（骨架同构判定 / 可空性适配清单 / 部署保护）"
keywords: [反向移植, backport, 5.0→4.1.6, Inescapable-Tarkovs-Softcore, 可空性, nullable, 空值守卫, 计数语义, 骨架同构, SptVersion, ModValidator, 运行时真值, 仓库行数, OOB, 隔离区, MO2, 部署保护, EmbeddedResource, 进游戏核对]
summary: "Inescapable-Tarkovs-Softcore 从 SPT 5.0（net10.0 C# 服务端）反向移植到 4.1.6 的实战沉淀：两端服务端骨架同构（Models/Spt/Mod、Modding、DI 层 git diff 零输出）→ 走受控适配而非重写，适配点合计 15 处（可空性 9 / 类型取整 1 / 工程与元数据 3 / 清单外与部署期 2），核心规则 = 以运行时程序集为真值逐项对照、可空一律 null 跳过且不计数、类型收窄保留原取整语义；工程侧：7 工单显式依赖链、215 项测试（含守卫专项断言）、src 零告警 + 测试 NoWarn 纪律、build.ps1 幂等 overlay、部署保护三保险（profile 备份 / MO2 关闭态 / 游戏全关）、隔离区操作、服务器日志标定预期值（7 模块 / 变更 10609 / 错误 0）；部署期修复四例（死目录 / 残货 / 开局仓库 50→56 行 OOB / 长路径另条）；与 3.11→4.1 跨架构重写的本质区别；开放项与两线并行维护约定。"
---

# SPT 5.0 → 4.1.6 反向移植实战：Inescapable-Tarkovs-Softcore

> 来源：2026-10-12/13 会话（spec → 7 工单 → 实现 → MO2 部署 → 服务器日志核验 → v0.1.0 发版）。
> 目标环境：SPT 4.1.6 服务端（net10.0 运行时引用）；源：SPT 5.0 线 `SamMeow-Inescapable-Tarkovs-Softcore`（52 文件 / 5260 LOC）。
> 项目侧完整记录：`mods/InescapableTarkovsSoftcore416/docs/port-notes.md`；决策：`mods/InescapableTarkovsSoftcore416/docs/adr/0001-port-from-5.0-codebase.md`。

## 一、结论先行：骨架同构 → 受控适配，而非重写

- 两端服务端骨架同构：`Models/Spt/Mod`、`Modding`、`DI` 层 `git diff` 零输出（见 `curated/modding-standard/version-matrix.md` 与 ADR-0001）→ 不重写骨架。
- 适配点合计 **15 处**（口径见 `port-notes.md` §1.1–§1.4）：可空性 9、类型取整 1、工程与元数据 3、清单外与部署期 2。
- 明确「不该改」清单（防过度适配）：六表构造器注入、五个 Config 注入、`ISptLogger<T>`、`ModHelper.GetAbsolutePathToModFolder`、`ListOrT<T>`、`StringToMongoIdConverter`、`MongoId` 隐式转换、全部枚举/静态常量、`RaidTimeAdjustmentService` 组合语义。
- 差异判定以**运行时程序集（DLL）为真值**逐项对照；源码快照仅作语义参考。

## 二、5.0 → 4.1.6 适配清单

| # | 差异 | 具体表现 | 应对 |
|---|------|---------|------|
| 1 | `StackMaxSize` `int?` | 弹药堆叠 | 空值守卫；null 与 0 均跳过 |
| 2 | `Unlootable` `bool?` | 可掠夺 | `!= true` 跳过，仅 true 处理 |
| 3 | `ProductionTime` `double?` | 制造时间 4 落点（Crafting 全局 / 单项 / Bitcoin / ScavCase） | `is { } time` 守卫后缩放 |
| 4 | `ConstructionTime` `double?` | 建设加速 | 守卫后缩放 |
| 5 | `MinSalesSum` `long?` | Skier 欧元换算 | `?? 0` 兜底后换算 |
| 6 | `IsSpawnedInSession` `bool?` | 藏身处免 FIR（含两分支） | `== true` 才置 false |
| 7 | `MinUserLevel` `int`（5.0 为 `double`） | 跳蚤等级 | `(int)Math.Round(..., MidpointRounding.AwayFromZero)` 保留取整语义 |
| 8 | `SptVersion` 区间门禁 | 不改则 `ModValidator` 拒载 | `new("~4.1.6")` |
| 9 | 构建引用路径 | 需指向目标实例 | 项目级 `Directory.Build.props` 自包含 `SptRoot` / `SptRuntimeDir`（遮蔽 `mods\` 根同名文件，可命令行覆盖） |
| 10 | 警告纪律 | 可空站点产生 CS8602 级警告 | 未启用 `WarningsAsErrors`；src 范围告警审查清零；测试工程 `NoWarn` CS8602/8604/8629（夹具已知噪声），产品代码不抑制 |
| 11 | `EscapeTimeLimit` `double?`（清单外） | RaidDuration 时限（5.0 为不可空） | null/≤0 跳过且不计数（实现期对照运行时程序集补发现） |
| 12 | 部署期：开局仓库行数 | 4.1.6 开局模板起始物品越界（新档 [OOB]） | `StashChanger.StashSizes` 标准仓 50→56 行后重新部署 |

> 注 1：表按可读性合并同类项（`ProductionTime` 的 4 个落点并为一行）；与 `port-notes.md` §1 的 15 处口径对应（可空性 9 / 取整 1 / 工程与元数据 3 / 清单外与部署期 2）。
> 注 2：4.1.6 现库**零 null `ProductionTime`**，时间守卫属防御性——价值在数据形态变化或异常数据下不误报、不崩溃。

**工程约定（非差异项，本次新项目基线）**：

- 计数语义：5 个时间站点统一「null 跳过且不计数」，`log.Changed()` 仅成功写入后调用（`port-notes.md` §2）。
- 查找表 `EmbeddedResource` 嵌入，运行时与测试读同一资源（不一致即测试红）；单例 `IModMetadata`、无 `package.json`。
- `scripts/build.ps1` 幂等重建 `build/overlay/`；`config.json` 为模板逐字节拷贝（UTF-8 with BOM）。

## 三、可复用规则（未来 5.x → 4.x 反向移植）

1. 先做**骨架同构判定**（层面对照 diff）：同构 → 走可枚举适配清单；跨架构（如 3.11 TS → 4.1 C#）→ 才走重写。
2. 差异扫描以**运行时程序集为真值**，逐项比对可空性与成员类型——不只读源码快照。
3. 可空一律「**null 跳过且不计数**」；类型收窄（`double→int/long`）必须保留原取整/兜底语义。
4. 清单之外仍有**补发现**（本例 `EscapeTimeLimit`、仓库行数）——实现期与部署期都要对照程序集/实机复查。

## 四、工程与流程经验

- **工单链（7 张，显式 Blocked by）**：01 编译基线（0 错误 + src 零告警）→ 02 测试移植 → 03 构建/overlay → 04 文档 → 05 部署 → 06 服务器日志核验 → 07 进游戏验收 + 发版。
- **测试纪律**：`dotnet test` 215 项全绿，含「可空守卫专项断言」（null 时零变更、不崩溃）。
- **部署保护三保险**：部署前 profile 备份（本例 `Default_pre-softcore416`）+ MO2 关闭态离线部署（mod 目录 + meta.ini + modlist 行）+ DLL 更新前确认游戏/服务器/启动器全关（文件锁实况）。
- **隔离区操作**：破坏性处置一律「移入 `_quarantine\<项>-<时间戳>`」而非删除（死目录、残货条目）。
- **验证分工**：agent 负责构建/单测/overlay 组装/部署/服务器日志解析；Overseer 负责启动 MO2 + 服务器并进游戏（agent 不代启动）。
- **服务器日志标定预期值**：首启核验「加载行 + 每模块 summary + 数据缺口计数」。本例：`[ITS] Inescapable Tarkov's Softcore v0.1.0 loaded`；7 模块 / 变更 10609 / 警告 32 / 错误 0；数据缺口（背包 9 条 + trueItems 23 条）为已知静默跳过形态。
- **覆盖层**：`mods\[5]软核大修-InescapableTarkovsSoftcore`（DLL SHA256 头部 `91B9C9E9…`）。
- **发版**：`release/` 产 zip = overlay 树（DLL + config），登记体积与 DLL SHA256 头。
- **Windows 编码纪律**：含中文 `.ps1` 用 UTF-8 with BOM；避免 `Get-Content -Raw` 管道改写；日志读取指定 UTF-8 并二次核对。

## 五、部署期修复登记（四例）

| # | 问题 | 修复 | 复验 |
|---|------|------|------|
| F1 | FikaSync 死目录致启动 Critical | 整体隔离至 `_quarantine\` | 重启后 Critical 清零 |
| F4 | Painter 商人 assort 残货报错 | 移除残货条目（原件隔离） | 重启后报错清零 |
| F6 | 4.1.6 开局模板起始物品越界（新档 [OOB]） | 标准仓行数 50→56 并重新部署 | 新档 [OOB]=0 |
| F2 | WTT Content Backport 3 个 bundle 静默缺失 | overlay 更名缩短物理路径 | 见 `curated/operations/usvfs-long-path-silent-missing.md`（已另条，复验 0 条） |

F6 根因注记：4.1.6 开局模板（`profiles.json`，含 Unheard）起始物品铺至 y=51、物品高 2 → 标准仓需 ≥53 行；渐进仓库将新档仓库统一转 `STASH_STANDARD_STASH_10X30` 时原 50 行越界（[OOB] 实测复现）。

## 六、与正向移植（3.11 → 4.1）的关键区别

- 正向 3.11→4.1：跨语言/跨架构硬断链（TS→C#、接口抽换、注入体系换代、Router 生命周期），必须反编译重编译（见 `curated/migration/server-mod-311-to-41.md`）。
- 本次 5.0→4.1.6：同一 C# 骨架内的适配，15 处可枚举清单；ADR-0001 据此否决「从源 TS 为 4.1.6 重写」。
- 但**数据形态差异不随骨架同构而消失**：Collector 前置 136 条、缺失 id（背包 9 条）等仍需按目标线数据单独裁决与登记。

## 七、开放项

- Crisis「+30」可复现性：4.1.6 `AvailableForStart` 已含 `Level=48` 条件，以运行日志定论。
- Collector：A4S 重组为单条 Level 20、A4F 追加 Gamma 交付——接取/完成/存档兼容待进游戏复核。
- 旧存档超上限堆叠：`InvalidStackObjectsCountFix` 迁移路径行为。
- 各 Config（barter / 报价数 / 保险 / Fence）游戏表现。
- 计数预期口径偏差：spec 预期「barter 1 条」与实测「trueItems 23 条」差异待归因。
- 进游戏核对清单 11 项：用户游玩期间渐进验证中（异常项登记于 `port-notes.md` §5.3）。
- 两线并行维护、互不同步：5.0 线后续修复需按需回搬。

## 相关

- 正向线同产品开发经验（3.11 TS → SPT5 C#）：`curated/migration/pilot-experience-inescapable-softcore.md`
- 3.11→4.1 服务端重写模板：`curated/migration/server-mod-311-to-41.md`、`curated/migration/api-mapping-311-to-41.md`
- 双轨差异矩阵：`curated/modding-standard/version-matrix.md`
- F2 长路径问题：`curated/operations/usvfs-long-path-silent-missing.md`
- 项目记录：`mods/InescapableTarkovsSoftcore416/docs/port-notes.md`、`mods/InescapableTarkovsSoftcore416/docs/adr/0001-port-from-5.0-codebase.md`
