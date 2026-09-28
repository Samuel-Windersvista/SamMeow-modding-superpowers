---
version: [5.0]
domain: server
topic: migration
title: "SPT5 整合 mod 实战经验：六组 3.11 TS 源 → 单一 C# 服务端 mod（Inescapable-Tarkovs-Softcore）"
keywords: ["SPT5", "整合", "consolidation", "locale-key", "实体变体", "客户端与服务端", "移植陷阱", "Spec审查", "worktree", "部署保护"]
summary: "Inescapable-Tarkovs-Softcore v0.1.0 实战沉淀：运行时真值（DLL>源码快照）、locale-key 实体名（按 id 匹配）、实体变体枚举（安全容器 15 员）、id 世代全扫、行为归属判定（客户端 IOF 案例）、读代码不读注释、部署期验证点（手册价缓存/DI 注入）、方向语义与错标；附工程模式（JSONC 容错配置/磁盘优先数据/部署保护）与工作流经验（Spec 轴独立审查三案例/worktree 车道/Windows 编码陷阱）"
---

# SPT5 整合 mod 实战经验：六组 3.11 TS 源 → 单一 C# 服务端 mod

> 来源：`Inescapable-Tarkovs-Softcore` v0.1.0 首个 ship 周期（2026-09-26，grill → spec → 链路实现/双轴审查 → 部署 → 验收反馈修复 R1）。
> 目标环境：SPT 5.0.0（build 47242 / EFT 1.1.5 / net10.0 / IL2CPP + BepInEx 6）。
> 项目侧完整回望：该 repo `docs/retro-2026-09-26.md`。

## 一、领域陷阱（每条均有实证）

### T1 运行时真值：编译/反射以 `SPT_Runtime\*.dll` 为准
源码快照与运行 DLL 存在修饰符/可空性差异（快照 `double?` vs 运行时 `required double`；运行时广泛使用 `required` 成员）。**规则：构建引用与反射核对一律以运行时程序集为真值，源码快照仅作语义参考。**

### T2 实体名是本地化键：一律按 id 匹配
SPT 5.0 生产 DB 中**全部任务**的 `name` 是本地化键（`<id> name`）——按 `Name.Contains(...)`/`== "Collector"` 匹配会**静默 0 命中**（786/786），且无告警。任何按名筛选都必须改为按 **id**（或经 `LocaleTable` 解析名，但以 id 为主）。同类思路适用于其他以 name 做键的实体（trader/bot 等）。

### T3 实体变体：按父类枚举 + 映射 + 未知告警
实例：SPT5 安全容器家族 **15 个成员**（Alpha/Beta/Epsilon/Gamma/Kappa + Gamma_tue/cultic_kappa/gamma_damaged/louise_pitton/beltbag/tournament + Boss/Developer/Tetta 异常档），玩家档案实际装备的是**变体**，只改基础 6 条 id → 表面“修改无效”。**规则：家族类修改一律“按父类枚举 + 显式映射表 + 显式跳过表 + 未知成员告警”（未来 5.x 新增自动提示），而不是枚举固定 id 清单。**

### T4 id 世代更替：旧清单必须对照当前 DB 全扫
5.x 期间：`MEDKIT`→`MED_KIT` 等重命名、4.x 时代叠加层新增的 id 在 5.0 消失（9 条背包 id）、新增大量物品（袖章 5→37、新食品/杂物/电子）。**规则：任何“照抄旧表”的移植必须先跑“失效 + 新增候选”全扫**（数据源：`SPT_Runtime\SPT_Data\database\templates\items.json`，顶层是 id→item 的字典，脚本按 `_parent` 过滤家族）。

### T5 行为归属判定：先问「客户端还是服务端」
实例：旧包「仓库自动排序自动合并堆叠」来自**客户端插件**（Seion IOF，挂 `GridSortPanel` + `TransferOrMerge`）；**原版 Sort 从不合并**（0.16 与 0.16.9.5 反编译双证：`Sort()` 仅重排，无 Merge；合并仅存在于拖拽路径，且要求同 `_tpl` + 同 `SpawnedInSession`(FiR) + `StackObjectsCount < StackMaxSize`）。纯服务端 mod 无法复刻客户端交互——移植前先判定归属，避免把「客户端插件的行为」记成「原版行为」。

### T6 读代码不读注释：「最终游玩态」= 实际执行文件 + 实际 DB
三个证伪案例：① SURV 是否含 `HideoutContainersChanger` 覆盖（注释/推断说没有，实际有 → 容器尺寸错 5 处）；② ScavCase 配置注释称“更快”，代码是 `时间 / 0.5` = **变慢**（忠实保留，方向语义须落档）；③ Crisis 任务 `+30` 在 5.0 已不可复现（Level 条件被 BSG 从数据移除）。**规则：默认值/行为以实际执行文件为准；注释、README、快照只作线索。**

### T7 部署期即验证期（服务端侧验证点清单）
只能上真服务器验证的项：手册价缓存时序（Preload 改写 `HandbookBase.Items` vs `HandbookPriceCache` 谁先）、DI 单例可注入性（`HideoutConfig`/`ScavCaseConfig`/`RagfairConfig` 等是否注册）、客户端呈现路径、配置阶段覆盖（SPT post-DB 是否抹除写入）。**做法：部署前列清单，首启读服务器日志 + 关键项进游戏核对。**

### T8 方向语义与源数据错标
`cellsH`=列/宽、`cellsV`=行/高；tuple 记法在项目内固定 `(cellsV, cellsH)`。源数据可能自带错标（实例：源把「臂章父类 `5447e1d0`」与「近战父类 `5b3f15d4`」写反——前者实为 Knife、后者实为 ArmBand；双开关同开时并集正确、单开关语义对调）。**规则：数据表 + 锚点断言 + 与源 SHA256/逐字节比对；发现错标先记录再纠正。**

## 二、工程模式（可复用）

- **E1 配置：JSONC 容错**——`//` 注释 + 尾随逗号容忍；未知键告警移除、缺键回落默认、非法值告警+默认；嵌套子节递归校验；字典型叶键（`overrides` 类）不递归进 Dictionary 反射面，改为“值为对象 + 内层值按值类型校验（整型拒绝小数）”；非法节只回落该节（不得全量重置）。
- **E2 数据文件：磁盘优先 + 嵌入兜底**——`data/**` 随包发货到 mod 目录（玩家可编辑、JSONC）；加载顺序 = 磁盘 → 嵌入（缺失/损坏告警回落，不崩）；**升级 copy-if-missing**（config 与 data 绝不覆盖玩家编辑，仅 DLL 始终覆盖）。
- **E3 部署保护**——MO2 离线部署（mod 目录 + meta.ini + modlist 行）安全可行；**profile 先备份**（可回滚）；**DLL 更新前必须关闭 游戏/服务器/启动器**（文件锁实况）；日志在 MO2 运行时落 `overwrite\SPT_Runtime\user\logs\`（VFS 重定向）。
- **E4 测试纪律**——夹具必须对齐**生产形态**（实例：夹具用人类名 vs 生产 locale-key → 测试全绿而行为死）；关键值用锚点断言（每档 1 个 id）；数据改动随带计数/唯一性测试；测试绿 ≠ 行为对，需独立源对照。

## 三、工作流经验（agent 编排）

- **W1 独立 Spec 轴审查不可省**：三个只有它能抓到的案例——覆盖层误判（T6①）、locale-key 静默失效（T2）、配方首条命中漂移（目标配方在 5.0 有多条时按 `endProduct` 首条命中会落错，需按 recipe id 钉住）。
- **W2 worktree 车道并行**：4 路 worktree + 1 条串行链可行；冲突面集中在共享文件（模块配置/模板/README），add/add 冲突按并集解决；共享逻辑改动必须做**语义和解**（递归校验 vs 字典型叶实例）。
- **W3 Windows 编码陷阱**：PowerShell `Get-Content -Raw` + `Set-Content -Encoding utf8` 管道读写 UTF-8 = 乱码 + 可能吞行（实例：吞掉一条断言）；批量编辑用整文件写入；含中文 `.ps1` 必须 UTF-8 with BOM；控制台读日志建议指定 UTF-8 并二次核对。
- **W4 网络韧性**：git 443 间歇失败 → fail-fast 探测（`-c http.lowSpeedLimit=1000 -c http.lowSpeedTime=20`）优于盲等；积压后一次性推送。

## 相关

- 前置迁移知识：`curated/migration/server-mod-311-to-41.md`（3.11→4.1 服务端重写模板）
- 5.0 API 面：`curated/api-notes-5.0/`（config / database / DI / 路由 / 存档 / mod 加载）
- 日志与启动模型：`curated/api-notes-5.0/mod-loading.md`、`save-profile.md`（含 `InvalidStackObjectsCountFix` 迁移）
