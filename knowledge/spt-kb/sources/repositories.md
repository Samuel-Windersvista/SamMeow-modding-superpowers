# SPT 官方 GitHub 仓库登记册

抓取日期：2026-08-02 | 归档位置：`E:\云文件\GitHub\SPT-archive\`
上游组织：https://github.com/sp-tarkov （共 21 个公开仓库，20 个已本地归档，server-csharp 另有本地 fork）

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
| server-csharp (fork) | `E:\云文件\GitHub\SamMeow_SPT410_source_code` | SPT 4.1 C# 服务端，fork 自 sp-tarkov/server-csharp。本地 `main` 已同步至 4.1.2（`cf04a112`，tag `4.1.2`）；`upstream/4.1.x-dev` = `04a58332`（4.1 开发线）。**部署验证**：`E:\Game\EFT_Offline\SPT_410\SPT_Runtime\SPTarkov.Server.Core.dll` FileVersion=4.1.2，ProductVersion 含 `cf04a112` —— 与 fork tag 一致，服务器运行的就是 4.1.2 源码构建 |

## 完整性校验方法

```powershell
# 任一仓库验证 HEAD 是否与登记册一致
git -C "E:\云文件\GitHub\SPT-archive\<仓库名>" log -1 --format='%H'
```

注意：完整 commit hash 记录于 git 历史中；上表为短 hash。全部分支已在 clone 时获取（remote-tracking）。
