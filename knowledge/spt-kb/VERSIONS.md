# SPT 版本地图

最后更新：2026-09-01 | 状态标注：`[已确认]` = 有资料佐证，`[待核实]` = 需要进一步确认

> **2026-08-21 重大变更**：官方 sp-tarkov 组织已于 2026-08-11 归档全部仓库（4.1.2 为官方最后版本）；
> 社区 fork **SP-Tushonka/server-csharp** 成为事实延续，2026-08-20 发布 **4.1.3**（47 commits/2059 文件，
> 但 ~2025 个是 SPTarkov→SPTushonka 目录重命名噪声；程序集名/命名空间/IModMetadata/客户端版本要求全部不变，
> server/client mod 均二进制兼容）。详见 `curated/operations/413-fork-transition.md`。
>
> **2026-09-01 复查**：fork 的 `main` = 4.1.3 发布快照（发布后未动）；**活跃开发在 `4.1x-dev` 分支**
> （4.1.3 后已 +12 commits 至 2026-08-27：启动时序/空投下限/钥匙卡/非法 sessionId 等修复，
> mod 接口零改动、二进制兼容保持）。跟踪最新代码以 `4.1x-dev` 为准；
> 注意 fork 内 `v3.2.0`~`v4.2.0` 标签是 2025 旧历史线残留，与 4.1.x 无关。

## 版本总览

| 版本 | 服务端实现 | 状态 | 资料位置 |
|------|-----------|------|---------|
| 3.11 LTS | TypeScript（`server` 仓库） | 长期支持版 [已确认] | `wiki/SPT_311/` |
| 4.0 | C# 重写（`server-csharp`） | 已发布，对应 EFT 1.0 时代 [已确认] | `wiki/SPT_40/`、`wiki/FAQs_40.md` |
| 4.1 | C#（`server-csharp`） | 已发布 4.1.0/4.1.1/4.1.2（2026-08-06 最新）[已确认] | `wiki/SPT_41/`、本地 fork |

## 4.1 发布线（2026-08-07 核实）

| 版本 | 日期 | 服务端 commit | 下载哈希 | 要点 |
|------|------|--------------|---------|------|
| 4.1.0 | ~2026-07 | — | — | 4.1 系列首个稳定版 |
| 4.1.1 | ~2026-07 末 | `e18bd1ea` | e18bd1e | 修复轮 |
| 4.1.2 | 2026-08-06 | `cf04a112`（tag `4.1.2`） | cf04a11 | 启动器路径校验、SIC 字段校验增强、禁用 Factory 新增 PMC 波次、中文语言包更新、跳蚤货币倍率=1、修复 `customPmcWaves` 空数组清除已有波次 |
| 4.1.3 | 2026-08-20 | `ddce41c6`（tag `4.1.3`，SP-Tushonka fork） | — | 官方归档后社区接管首发；XxHash3 文件哈希、DatabaseIntegrityService、ConfigEditor `OnAppliedToRuntimeAsync` 回调等；mod 二进制兼容 |
| 4.1.x-dev | 滚动 | `bb102040`（2026-08-27 快照） | — | fork `4.1x-dev` 分支；4.1.3 后 12 commits，接口零破坏；明细见 `curated/operations/413-fork-transition.md` |

- 4.1.2 下载：`https://spt-releases.modd.in/SPT-4.1.2-40743-cf04a11.7z`（需 EFT 0.16.9.5.40743 + .NET Runtime 10.0.9）
- 4.1.2 与 4.1.1 使用相同 EFT 版本号（40743），客户端二进制未变；服务端 mod 兼容 4.0.x+，4.0.x 时代 mod 不兼容
- Forge 上 4.1.2 兼容 mod 数：108（2026-08-07 快照）

## 本项目版本策略

- **最终目标：SPT 4.1**，且可能永远停留在 4.1（不再跟随后续版本）
- 3.11 资料价值：大量社区 mod 与教程基于 3.11，是 modding 概念的主要学习材料
- 4.0→4.1 迁移文档是最关键的桥梁资料：
  - `wiki/SPT_41/Server_40_to_41.md` — 服务端迁移
  - `wiki/SPT_41/Client_40_to_41.md` — 客户端迁移
  - `wiki/SPT_41/modding/` — 4.1 专属 modding 变更（EnumExtensions、类名映射、Mod Web Pages）

## 本项目环境状态（2026-08-07 核实）

| 组件 | 版本 | 验证方式 |
|------|------|---------|
| 服务器（`E:\Game\EFT_Offline\SPT_410\SPT_Runtime`） | **4.1.2**-RELEASE+cf04a11 | `SPTarkov.Server.Core.dll` FileVersion=4.1.2，ProductVersion 含 commit `cf04a112`（与 fork 4.1.2 tag 一致）；启动日志 `Server version: SPT 4.1.2 - cf04a1` |
| 客户端二进制 | EFT 0.16.9.5.40743（4.1.1/4.1.2 相同） | 4.1.2 与 4.1.1 客户端未变（VERSIONS 发布线） |
| 客户端 mods（BepInEx） | BigBrain 1.4+、Waypoints、SAIN、LootingBots 1.7.0（移植）、spt 模块 | 4.1.2 无 API 破坏（6 文件 diff 确认），插件无需重编译；启动日志 `Enabled N patches` |
| server mods（`user\mods`） | 无 | 目录为空 |
| 安装包 | `SPT-4.1.2-40743-cf04a11.7z`（147MB）保留在根目录 | 8/6 解压安装完成，包可删可留 |

**结论**：4.1.2 环境升级于 2026-08-06 解压安装时完成，2026-08-07 复核确认全链路 4.1.2。无需额外操作。

## 版本标签约定

知识库所有 curated 文档使用以下标签标注适用版本：

- `[3.11]` — 仅适用 3.11 LTS
- `[4.0]` — 仅适用 4.0
- `[4.1]` — 适用 4.1（本项目主目标）
- `[通用]` — 跨版本概念
- `[live-ref]` — live EFT 参考数据（2026-09-02 新增）：来自 tarkov.dev 等 live 数据源，非 SPT 事实；仅作对照/概念参考，数值权威性以 SPT 本地数据库为准。文档须标注快照日期与对应 live 版本（见 `archive/tarkov-dev/README.md`）

## 待核实清单

- [x] 4.1 的准确发布时间线与对应 EFT 版本号 — **已核实**（4.1.2 = 2026-08-06，EFT 0.16.9.5.40743；见上方发布线表）
- [x] server-csharp 仓库的分支模型 — **已核实**（本地 fork 远程：`upstream/main` + `upstream/develop` + `upstream/4.1.x-dev`；4.1 开发在 `4.1.x-dev` 分支，`main` 为稳定线，tag `4.1.2` 打在 `main` 上）
- [ ] 3.11 → 4.1 的 mod 迁移是否必须经 4.0 概念过渡
