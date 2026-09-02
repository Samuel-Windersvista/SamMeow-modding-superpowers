---
version: [4.1]
domain: both
topic: migration
source: curated
---
# SPT 4.1.3 与官方归档/fork 迁移（2026-08-21 实证）

> 状态：已验证（官方 releases 页 + fork tag 列表 + 本地 git 实拉 4.1.3 tag 逐 commit/diff 分析）
> 关键性：改变了所有"官方仓库"引用的去向；但 mod 兼容性结论是好消息

## 时间线（实证）

| 日期 | 事件 |
|---|---|
| 2026-08-06 | sp-tarkov 发布 4.1.2（官方最后版本） |
| 2026-08-11 | **sp-tarkov 组织归档全部仓库**（build/server-csharp 等全部只读） |
| 2026-08-12 / 08-16 | SP-Tushonka fork 出 4.1.3 的 BE/BEM 预览构建 tag |
| 2026-08-20 | **SP-Tushonka/server-csharp 发布 4.1.3**（tag） |

## 4.1.3 的实质（47 commits / 2059 文件变更的拆解）

- **~2025 个文件是重命名噪声**：`SPTarkov.*` 目录/项目文件夹改为 `SPTushonka.*`（如 Libraries/SPTarkov.Server.Core → Libraries/SPTushonka.Server.Core）
- **真实功能变更**（commits 精选）：
  - 性能：MD5 文件哈希换 XxHash3、启动分配削减、locale 常驻加载、spawnpoint Where 优化、ID 查找分配修复
  - 修复：Fence 分配、钱堆支付按客户端请求、MongoId 运算符、JSON 扩展惰性创建
  - 新增：DatabaseIntegrityService、配置文件删除校验、**ConfigEditor 新增 `OnAppliedToRuntimeAsync` 回调**（SIC 运行时改配置后的通知钩子，acidphantasm PR #4）
  - 工程质量：备份服务文化无关化 + 测试、ModLoader 异常改进

## 兼容性裁决（全部本地 git 实证）

| 面 | 结论 | 证据 |
|---|---|---|
| C# 命名空间 | **不变**（仍 SPTarkov.*） | IModMetadata.cs 两版 namespace 逐字节相同 |
| 程序集名 | **不变**（csproj 显式 AssemblyName=SPTarkov.Server.Core；宿主 exe 仍 SPT.Server） | 4.1.3 csproj |
| IModMetadata 接口 | **不变**（100% 相似度纯改名） | diff -M |
| ModValidator | **不变**（100% 相似度纯改名） | diff -M |
| 客户端版本要求 | **不变**（compatibleTarkovVersion = 0.16.9.40743） | 4.1.3 core.json |

**结论：4.1.2 编译的 server mod 与 client mod 在 4.1.3 上理论二进制兼容，无需重编译。**
"变化较大"的全部冲击在**源码仓库层面**（克隆地址、目录结构、分支名 4.1x-dev），不在运行时层面。

## 行动项（对本仓库）

- 源码引用：本地 fork `E:\云文件\GitHub\SamMeow_SPT410_source_code` 已加 `tushonka` remote 并拉取 4.1.3 tag
- KB 中所有 "sp-tarkov" 链接指向已归档仓库（只读快照仍可读，但不再更新）；新工作以 SP-Tushonka fork 为准
- Forge/外部生态对 4.1.3 的态度待观察（fork 的发布渠道/版本号是否被 Forge 接受未确认）
- 待核实：4.1.3 是否有官方客户端 patch 包更新（fork 无 Releases 页面，客户端 patch 分发渠道不明）

## 客户端侧补充（2026-08-22 实证）

- **客户端源码家 = `modules` 仓库**（SPT.* BepInEx 插件源码，即 spt-core/spt-singleplayer 等 7 个 DLL 的娘家）。
  SP-Tushonka 组织有活跃 fork（modules 更新于 2026-08-20，master=4.1.3 tag）
- 用户本地 fork `E:\云文件\GitHub\SamMeow_SP-Tushonka_modules_source_code` 已加 tushonka remote 并同步到 4.1.3（9 commits：ModulePatch 支持 Reverse Patch、bundle 下载移到 launcher、插件校验、项目挪根目录等）
- **4.1 modules 无性能补丁目录**：3.11 时代的 `Patches/Performance`（RemoveStopwatch 系列）在 4.1 重构中被移除（"Validate and remove patches"），原因未明——提性能 PR 前先弄清
- **4.1 有更强的融合路径**：SPT.PrePatch 项目 + 独立 `patcher` 仓库 = 预补丁（启动前直接改客户端 IL），可做 Harmony 运行时做不到的事
- SP-Tushonka 组织全景（14 仓）：server-csharp、**modules**（客户端）、launcher、build、forge、server-mod-examples、lfs、bento、installer、assembly-tool、server（旧 TS，已归档）、spt-item-finder、patcher、wiki——社区已完整接管全栈

## 分支拓扑与 4.1x-dev 复查（2026-09-01 实证）

> 复查动因：此前扫描以 `main` 分支为对象，Overseer 指出活跃开发在 `4.1x-dev`。
> 方法：本地 fork 直拉 tushonka remote，逐 commit/diff 实证（fetch 时间 2026-09-01）。

### 分支/Tag 拓扑（全部本地 git 实证）

| 引用 | 指向 | 日期 | 含义 |
|---|---|---|---|
| `main` | `ddce41c6` | 2026-08-20 | **= tag `4.1.3`**，发布版快照，发布后未动 |
| `4.1x-dev` | `bb102040` | 2026-08-27 | **活跃开发分支**，4.1.3 之后 12 个 commit（朝下一发布版滚动） |
| tag `v3.2.0-rc2`~`v4.2.0` | 2025-06 老提交 | — | **无关历史线**（fork 带入的旧标签），与 4.1.x 无关，勿误判为新版本 |

**结论：`main` 即 4.1.3 发布版源码；`4.1x-dev` 是 4.1.x 滚动开发线。跟踪"最新 413 系代码"应以 `4.1x-dev` 为准。**

### 4.1.3(main) → 4.1x-dev 差异（12 commits / 28 文件 / +182 -99）

| Commit | 内容 | 类别 |
|---|---|---|
| `bb102040` | ProfileActivityService 跳过非法 sessionId（#18） | 健壮性 |
| `eb4744a6` | Crowdin 语言包更新，新增芬兰语 | 数据 |
| `2c040f8ca` | LauncherV2 补版本黑名单检查（建号/版本列表两处） | 修复 |
| `28cbb6efd` | PostDbLoadService 强制空投 MinimumPlayersCountToSpawnAirdrop>1 → 1（#17） | **行为变更** |
| `c8f47f2ba` | FenceService 钥匙卡使用次数随机化 + 钥匙卡纳入耐久折价（#16） | **行为变更** |
| `c6ad0b8d7` | **OnLoad 回调从 ExecuteAsync 移到 StartAsync**：启动期阻塞执行，异常在启动阶段即暴露 | **启动时序变更** |
| `d3d1e50c8` | give 命令修复：int.TryParse 防崩、越界校验、IsRealItem 防给分类节点（#13） | 修复 |
| `f413c4221` | Improve AddItemAssorts：Fence 组货用 parent 查找缓存替代 LINQ 全表扫 | 性能 |
| `06ea49939` | README 构建选项勘误（BLEEDING_EDGE → BLEEDINGEDGE） | 文档 |
| `c9334cea7` | csproj 构建属性修复（SptUsesDebugLogger 控制日志配置选择） | 构建 |
| `500dea014` | 新增 `TryParseMongoId` 扩展；`IsValidMongoId(MongoId)` 语义改为 `!IsEmpty`；AdoptOrphanedItems 用 HashSet 消除 O(n²) | API新增+性能 |
| `4d0d83df7` | StringOrInt.ToString 的 `||`→`&&`，修任务生产解锁 null traderId（#15） | 修复 |
| （附） | 删除死代码 `ConnectResponse` record；Status.razor 空引用防护 | 清理 |

### 兼容性裁决（4.1.3 → 4.1x-dev，diff 实证）

| 面 | 结论 | 证据 |
|---|---|---|
| IModMetadata / ModValidator / ModLoader | **零改动** | `git diff --stat` 对这些路径无输出 |
| core.json / compatibleTarkovVersion | **不变**（0.16.9.40743） | diff 无 core.json |
| 程序集名/命名空间 | **不变** | 仅 README/csproj 构建属性改动 |
| server mod 二进制兼容 | **保持**（无破坏面） | 上述三项 |
| client mod | **不受影响**（服务端仓变更，客户端 EFT 版本未动） | — |

### 对 mod 作者的三点注意（行为层，非编译层）

1. **空投人数下限被服务端强制为 1**：PostDbLoadService 在 DB 加载后把所有地图 `MinimumPlayersCountToSpawnAirdrop > 1` 一律改 1。mod 若想设更高值，OnLoad 优先级必须排在 PostDbLoadService 之后，否则被覆盖。
2. **OnLoadAsync 时序变化**：回调改在 `StartAsync` 执行（启动完成前阻塞跑完），异常直接在启动阶段抛出暴露，不再进后台循环的 catch。排查启动问题时日志位置变了。
3. **`IsValidMongoId(MongoId)` 语义收窄**：从"24 位 hex 字符检查"改为"非空检查"；字符串校验请改用新增的 `TryParseMongoId(string, out MongoId)`。

### 与 fork 源码审查报告（docs/SPT413-fork-源码审查报告.md）的对账

上游这 12 个 commit **均未触及**审查报告的 P0 六项（EventOutputHolder foreach 删字典、共享名单无锁、通知队列、抽奖捷径概率、重复 mod 查重失灵、死条件）。唯一沾边：`f413c4221` Fence 组货性能改进与报告 §9（克隆/复印浪费）同区域但不等价。**P0 批次修复仍需在用户自己的 fork 上进行。**
