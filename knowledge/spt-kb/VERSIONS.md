# SPT 版本地图

最后更新：2026-10-14 | 状态标注：`[已确认]` = 有资料佐证，`[待核实]` = 需要进一步确认

> **2026-08-21 重大变更**：官方 sp-tarkov 组织已于 2026-08-11 归档全部仓库（4.1.2 为官方最后版本）；
> 社区 fork **SP-Tushonka/server-csharp** 成为事实延续，2026-08-20 发布 **4.1.3**（47 commits/2059 文件，
> 但 ~2025 个是 SPTarkov→SPTushonka 目录重命名噪声；程序集名/命名空间/IModMetadata/客户端版本要求全部不变，
> server/client mod 均二进制兼容）。详见 `curated/operations/413-fork-transition.md`。
>
> **2026-09-01 复查**：fork 的 `main` = 4.1.3 发布快照（发布后未动）；**活跃开发在 `4.1x-dev` 分支**
> （4.1.3 后已 +12 commits 至 2026-08-27：启动时序/空投下限/钥匙卡/非法 sessionId 等修复，
> mod 接口零改动、二进制兼容保持）。跟踪最新代码以 `4.1x-dev` 为准；
> 注意 fork 内 `v3.2.0`~`v4.2.0` 标签是 2025 旧历史线残留，与 4.1.x 无关。
>
> **2026-09-13 SPT 5.x 发现**：SP-Tushonka fork 新增 **`5.0x-dev`** 分支，`SptVersion=5.0.0`，
> 目标客户端 **EFT `1.1.5.0.47242`**（4.1.x 为 `0.16.9.40743`），已有 `5.0.0-BEM-20260909/0910` 预发布标签。
> 新增 BattlePass/赛季/tarcoin 商店/多结局/剧情任务链/扩展包等系统与破冰船等新地图。
> 已克隆至 `E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code`；详见 `curated/operations/5xx-source-verification.md`。
> **2026-09-14 策略复核完成（ADR-0006）**：「最终目标 SPT 4.1、可能永久停留」表述作废——4.1.5 为稳定开发基线，5.0 为已发布新主线，双轨适配。详见 `docs/adr/0006-posture-active-toolchain.md`。
> **2026-10-14 补记**：4.1 线发布 **4.1.6**（2026-09-18，tag `4.1.6` = `731d7a2a`）——4.1 稳定开发基线随补丁更新为 **4.1.6**（双轨策略不变）；本机 SPT_41x 已运行 4.1.6-RELEASE 构建，5.0 线滚动至 `8b1e800.20261010`。
>
> **2026-09-13 能力沉淀**：新增 `curated/api-notes-5.0/`（9 篇，**全部带 [UNSTABLE-PREVIEW] 标记**，因 5.0 仍在开发初期）。
> 客户端侧：已重建 EFT 1.1.5 类名清单（IL2CPP 但名称未混淆，16,435 类），制品见 `archive/eft-1.1.5/`，报告见 `docs/eft-1.1.5-类名映射重建报告.md`。

## 版本总览

| 版本 | 服务端实现 | 状态 | 资料位置 |
|------|-----------|------|---------|
| 3.11 LTS | TypeScript（`server` 仓库） | 长期支持版 [已确认] | `wiki/SPT_311/` |
| 4.0 | C# 重写（`server-csharp`） | 已发布，对应 EFT 1.0 时代 [已确认] | `wiki/SPT_40/`、`wiki/FAQs_40.md` |
| 4.1 | C#（`server-csharp`） | 已发布 4.1.0~4.1.6（2026-09-18 最新 4.1.6）[已确认] | `wiki/SPT_41/`、本地 fork |
| 5.0 | C#（`server-csharp`，SP-Tushonka fork） | **已发布**（2026-09-14 确认；原 `5.0x-dev` 开发线，`5.0.0-BEM-*` 为早期预发布标签；正式 tag 明细待复核）[已确认] | `curated/operations/5xx-source-verification.md`、本地 fork `SamMeow_SP-Tushonka_5xx_source_code` |

## 4.1 发布线（2026-08-07 初核；2026-10-14 补 4.1.4/4.1.5/4.1.6）

| 版本 | 日期 | 服务端 commit | 下载哈希 | 要点 |
|------|------|--------------|---------|------|
| 4.1.0 | ~2026-07 | — | — | 4.1 系列首个稳定版 |
| 4.1.1 | ~2026-07 末 | `e18bd1ea` | e18bd1e | 修复轮 |
| 4.1.2 | 2026-08-06 | `cf04a112`（tag `4.1.2`） | cf04a11 | 启动器路径校验、SIC 字段校验增强、禁用 Factory 新增 PMC 波次、中文语言包更新、跳蚤货币倍率=1、修复 `customPmcWaves` 空数组清除已有波次 |
| 4.1.3 | 2026-08-20 | `ddce41c6`（tag `4.1.3`，SP-Tushonka fork） | — | 官方归档后社区接管首发；XxHash3 文件哈希、DatabaseIntegrityService、ConfigEditor `OnAppliedToRuntimeAsync` 回调等；mod 二进制兼容 |
| 4.1.4 | 2026-09-04 | `072d5340`（tag `4.1.4`） | — | bundle 头校验（#21）、SIC 默认凭据改密提示与迁移、战局外 hp 回复按部位拆分、空投最小玩家数=1、钥匙卡次数随机化、MongoId TryParse / AddItemAssorts 加固等；locale 扩充（芬兰语） |
| 4.1.5 | 2026-09-05 | `7d7add55`（tag `4.1.5`） | — | 小补丁（3 commits）：bundle 签名放宽 + locale 更新 |
| 4.1.6 | 2026-09-18 | `731d7a2a`（tag `4.1.6`） | — | 补丁维护版（21 commits / 44 files）：装甲板过滤与密封容器奖励池修复、商人 assort 黑名单反转修复、保险删除交易字典加固（#112）、热路径分配削减与空值防护、库存循环引用修复、通知按 runtime type 序列化、locale 扩充（罗马尼亚语/泰语） |
| 4.1.x-dev | 滚动 | `62896c02`（2026-10-01 快照） | — | fork `4.1x-dev` 分支；tag `4.1.6` 后仅 +1 locale commit，接口零破坏；4.1.3 期明细见 `curated/operations/413-fork-transition.md` |

- 4.1.2 下载：`https://spt-releases.modd.in/SPT-4.1.2-40743-cf04a11.7z`（需 EFT 0.16.9.5.40743 + .NET Runtime 10.0.9）
- 4.1.2 与 4.1.1 使用相同 EFT 版本号（40743），客户端二进制未变；服务端 mod 兼容 4.0.x+，4.0.x 时代 mod 不兼容
- Forge 上 4.1.2 兼容 mod 数：108（2026-08-07 快照）；4.1.x 兼容 600（2026-10-11 快照，其中标 4.1.6 者 547——见 `docs/forge-4.1x-mod-survey-2026-10.md`）
- 本机 4.1 线运行版本 = **4.1.6-RELEASE+731d7a2**（2026-09-18 构建；见「本项目环境状态」）

## 本项目版本策略

- **稳定开发基线：SPT 4.1.6**（4.1 线最新补丁，2026-09-18；原 4.1.5 为发布时点基线，随补丁更新——Modding Standard Dev-Baseline；原「最终目标 4.1、可能永久停留」表述作废，见 ADR-0006 及其 2026-10-14 注记）；**新主线：SPT 5.0（已发布）**，双轨适配中
- 3.11 资料价值：大量社区 mod 与教程基于 3.11，是 modding 概念的主要学习材料
- 4.0→4.1 迁移文档是最关键的桥梁资料：
  - `wiki/SPT_41/Server_40_to_41.md` — 服务端迁移
  - `wiki/SPT_41/Client_40_to_41.md` — 客户端迁移
  - `wiki/SPT_41/modding/` — 4.1 专属 modding 变更（EnumExtensions、类名映射、Mod Web Pages）

## 本项目环境状态（2026-10-14 复核实）

| 组件 | 版本 | 验证方式 |
|------|------|---------|
| 服务器 4.1 线（`E:\Game\EFT_Offline\SPT_41x\SPT_Runtime`） | **4.1.6**-RELEASE+731d7a2 | `SPTarkov.Server.Core.dll` FileVersion=4.1.6，ProductVersion `4.1.6-RELEASE+731d7a2.20260918`（2026-09-18 构建；与 fork tag `4.1.6` 同 commit） |
| 客户端二进制 4.1 线 | EFT `0.16.9.40743` | `EscapeFromTarkov.exe` FileVersion 0.16.9.40743 |
| 服务器 5.0 线（`E:\Game\EFT_Offline\SPT_5xx\SPT_Runtime`） | **5.0.0**-BLEEDINGEDGEMODS+8b1e800 | `SPTarkov.Server.Core.dll` FileVersion=5.0.0，ProductVersion `5.0.0-BLEEDINGEDGEMODS+8b1e800.20261010`（2026-10-10 构建；BE 通道滚动产物，非正式 release tag） |
| 客户端二进制 5.0 线 | EFT `1.1.5.47242` | `EscapeFromTarkov.exe` FileVersion 1.1.5.47242；IL2CPP |
| server mods（`SPT_Runtime\user\mods`） | 4.1 线：无；5.0 线：5 个（ITBS ×4 + TrueRealtime map server） | 目录清单 |
| 客户端插件 | 4.1 线：`BepInEx\plugins\spt`（仅官方模块）；5.0 线：`BepInEx\plugins\sptushonka`（官方）+ `TrueRealTimeMap.Spike`（第三方） | 目录清单 |

**结论**：本机双轨环境就位——4.1 线为 4.1.6 RELEASE（2026-09-18 构建），5.0 线为 5.0.0 BE 通道（2026-10-10 滚动构建）。4.1 线 server mods 为空（整合包 mod 经 MO2 覆盖层投送）；5.0 线已挂 ITBS / TrueRealtime 系列开发中 mod。

## 版本标签约定

知识库所有 curated 文档使用以下标签标注适用版本：

- `[3.11]` — 仅适用 3.11 LTS
- `[4.0]` — 仅适用 4.0
- `[4.1]` — 适用 4.1（本项目主目标；现行补丁 4.1.6）
- `[5.0]` — 适用 SPT 5.0（2026-09-13 登记，**2026-09-14 确认已正式发布**；目标客户端 EFT `1.1.5.0.47242`）：双轨适配中，预发布快照的 API 结论按正式 tag 复核前维持原标注
- `[通用]` — 跨版本概念
- `[live-ref]` — live EFT 参考数据（2026-09-02 新增）：来自 tarkov.dev 等 live 数据源，非 SPT 事实；仅作对照/概念参考，数值权威性以 SPT 本地数据库为准。文档须标注快照日期与对应 live 版本（见 `archive/tarkov-dev/README.md`）

## 待核实清单

- [x] 4.1 的准确发布时间线与对应 EFT 版本号 — **已核实**（4.1.2 = 2026-08-06，EFT 0.16.9.5.40743；见上方发布线表）
- [x] server-csharp 仓库的分支模型 — **已核实**（本地 fork 远程：`upstream/main` + `upstream/develop` + `upstream/4.1.x-dev`；4.1 开发在 `4.1.x-dev` 分支，`main` 为稳定线，tag `4.1.2` 打在 `main` 上）
- [x] 4.1 线最新补丁与本地源码映射 — **已核实**（4.1.6 = tag `731d7a2a`，2026-09-18；本机 DLL ProductVersion 与 tag 同 commit）
- [ ] 3.11 → 4.1 的 mod 迁移是否必须经 4.0 概念过渡
