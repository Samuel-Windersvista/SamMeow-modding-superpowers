# SPT 官方 GitHub 仓库登记册

抓取日期：2026-08-02 | 归档位置：`E:\云文件\GitHub\SPT-archive\`
上游组织：https://github.com/sp-tarkov （共 21 个公开仓库，20 个已本地归档，server-csharp 另有本地 fork）
最近更新：2026-10-14（补记 SP-Tushonka fork 克隆与 4.1.6 tag `731d7a2a`，见「本地已有（未重复 clone）」表）

## Tier 1 — 写 mod 直接依赖

| 仓库 | 锁定 commit | 日期 | 用途 |
|------|------------|------|------|
| wiki | ba82cdff | 2026-08-02 | 官方 wiki Markdown 源，已 vendor 进 `../wiki/` |
| server-mod-examples | 7c78aa7e | 2026-03-04 | 服务端 mod 官方示例集 |
| mod-examples | d0381f27 | 2025-04-08 | mod 示例（3.11.3 时代） |
| modules | 425bf000 | 2026-08-02 | 客户端 BepInEx 模块源码（SPT 官方 client mod） |

## Tier 2 — 过渡期对照资料

| 仓库 | 锁定 commit | 日期 | 用途 |
|------|------------|------|------|
| server | 96e5b73f | 2025-09-07 | 旧 TypeScript 服务端（SPT 3.11 LTS 对应实现） |
| forge | f1cdff3e | 2026-08-01 | SPT Forge 模组站源码（含 mod 元数据结构） |
| assembly-tool | 13d9183d | 2026-07-30 | 程序集处理工具 |
| patcher | 2a5d47a3 | 2024-12-29 | 客户端补丁器 |
| launcher | 8b63aeb0 | 2026-07-30 | SPT 启动器 |

## Tier 3 — 完整性存档

| 仓库 | 锁定 commit | 日期 | 用途 |
|------|------------|------|------|
| installer | 68425ebd | 2026-08-01 | 官方安装器 |
| build | d0548785 | 2026-07-30 | 构建脚本/CI |
| bento | 4685fac5 | 2026-07-04 | 发行版打包 |
| PatcherPizza | 690ab0f8 | 2026-07-31 | 补丁分发配置 |
| spt-item-finder | d1c2cc58 | 2026-07-23 | 物品 ID 查询工具 |
| EftPatchHelper | 2dd090bf | 2025-11-15 | EFT 版本降级补丁辅助 |
| sp-tarkov-website | 32c03c1e | 2025-11-06 | 官网内容源 |
| bot-generator | fd087444 | 2025-10-22 | bot 数据生成 |
| db-website | b8d16378 | 2025-10-20 | 数据库查询站（上游已归档） |
| loot-dump-processor | 87931dc4 | 2025-08-25 | 战利品数据处理 |
| launcher-patchgen | 1b015020 | 2025-03-30 | 启动器补丁生成 |

## 本地已有（未重复 clone）

| 仓库 | 本地路径 | 说明 |
|------|---------|------|
| server-csharp (SP-Tushonka fork，4.1.x 现行) | `E:\云文件\GitHub\SamMeow_SP-Tushonka_source_code` | SPT 4.1 C# 服务端现行源码（origin = SamMeow 自有 fork，upstream = SP-Tushonka/server-csharp）。tags 含 `4.1.0`~`4.1.6`；**tag `4.1.6` = `731d7a2a`（2026-09-18；较 4.1.5 `7d7add55` +21 commits / 44 files）**；`upstream/4.1x-dev` = `62896c02`（2026-10-01，较 tag 再领先 1 个 locale commit）；最后 fetch 2026-10-05。**部署验证**：`E:\Game\EFT_Offline\SPT_41x\SPT_Runtime\SPTarkov.Server.Core.dll` ProductVersion = `4.1.6-RELEASE+731d7a2.20260918` —— 与 tag `4.1.6` 同 commit。读码入口：工作树在 `refactor/buy-restriction-module` 分支，读 4.1.6 源码用 `git show 4.1.6:<路径>` 或 worktree 检出 |
| server-csharp (早期 fork) | `E:\云文件\GitHub\SamMeow_SPT410_source_code` | SPT 4.1 C# 服务端早期克隆。本地 `main` 停在 4.1.2（`cf04a112`，tag `4.1.2`）；`tushonka/4.1x-dev` = `9b5d735a`（2026-09-05）；`upstream/4.1.x-dev` = `e97f4426`（2026-08-08）；**无 4.1.6 tag——查 4.1.6 请用上一行 SP-Tushonka 克隆**。历史部署验证（2026-08 期登记）：SPT_410 环境 DLL 与 `cf04a112` 一致；该目录现已不在 `EFT_Offline` 原位，未复核 |
| server (3.11 branch, 本地) | `E:\云文件\GitHub\SamMeow_SPT3114_source_code` | SPT-AKI 3.11.5-Live-In-Norvinsk-Edition（基线 SPT-AKI 3.11.x，TypeScript 版）。与官方 sp-tarkov/server（Tier 2 登记，96e5b73f/2025-09-07）同代；`server/project/package.json` `spt-server` v3.11.3，`assets/configs/core.json` sptVersion=3.11.4。**api-notes-3.11/ 笔记的实读来源** |
| server-csharp (5.0x-dev, 本地) | `E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code` | SPT 5.0.0 开发线，SP-Tushonka fork。分支 `5.0x-dev`（克隆时 HEAD `ff0bf3281`/2026-09-12），`Build.props` SptVersion=5.0.0，`core.json` compatibleTarkovVersion=1.1.5.0.47242；含 `5.0.0-BEM-20260909/0910` 预发布标签。**克隆需走代理 `http://127.0.0.1:7890`（直连 GitHub git 被重置）；LFS 已 pull（18 文件约 257MB）**。核实记录见 `curated/operations/5xx-source-verification.md` |
| wiki (SP-Tushonka fork, 本地) | `E:\云文件\GitHub\SP-Tushonka-wiki` | SP-Tushonka fork 的 wiki 源（现行 wiki.sp-tushonka.com 的渲染源）；锁定 commit `392e5005`（2026-09-11）；已 vendor 进 `../wiki-tushonka/`。克隆需走代理 `http://127.0.0.1:7890` |

## 完整性校验方法

```powershell
# 任一仓库验证 HEAD 是否与登记册一致
git -C "E:\云文件\GitHub\SPT-archive\<仓库名>" log -1 --format='%H'
```

注意：完整 commit hash 记录于 git 历史中；上表为短 hash。全部分支已在 clone 时获取（remote-tracking）。
