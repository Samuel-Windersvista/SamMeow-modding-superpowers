# SamMeow-modding-superpowers

面向《离线塔科夫 SPT》(Single Player Tarkov) 的 mod 开发与整合包自动化搭建工具包，以 OpenCode 插件形式分发。

> 定位：纯 SPT、纯 OpenCode 的 agent 工具链——用一组 skills 加本地 MCP 服务器，把「评估 mod -> 读懂作者说明 -> 写 mod -> 策展整合包 -> 冲突审计 -> 构建 -> 验证」串成一条可复用的流水线。

**快速上手：** [`docs/使用指南.md`](docs/使用指南.md)

## 为什么存在

SPT 项目可能停止运作。本仓库承担两项使命：

1. **资料抢救**：SPT 官方 wiki、官方仓库、Forge 模组站 mod 元数据与成品，已全部归档（见下方知识库）。
2. **能力建设**：以 MCP 服务器 + skills + MO2 控制面构成的 SPT 4.1 mod 开发与整合包搭建流水线。

## SPT 版本策略

| 阶段 | 版本 | 说明 |
|------|------|------|
| 基线（现状） | SPT 3.11.4 | 可行性研究报告的验证基线（150+ mod） |
| 目标（终态） | **SPT 5.x** | 未来的发展方向 |

mod 开发一律按 4.1 目标编写（C# 服务端，`IModMetadata`/DI/Table 注入体系），3.11 资料仅作概念对照。

## 技能集（`skills/`）

14 个 skill，覆盖整合包全生命周期：

| Skill | 用途 |
|-------|------|
| `using-spt-modding-superpowers` | 会话 bootstrap + 路由总表（每次会话自动注入） |
| `setting-up-spt-modding-environment` | 首次运行：MO2 检测、控制面安装、SPT 路径配置、模板与 dev-log 初始化 |
| `maintaining-spt-modding-environment` | 后续维护：知识库更新、缓存清理、环境体检 |
| `evaluating-spt-mods` | 安装前判断一个 mod 是否值得进包（质量/契合度/风险/包价值） |
| `interpreting-spt-mod-instructions` | 按作者说明选择文件与安装方式 |
| `curating-spt-modpack` | 整包增量策展：批次策略、回滚点、命名、风格声明 |
| `building-spt-modpack` | 生成 MO2 profile 并执行整合包构建 |
| `spt-conflict-audit` | 用 20 类冲突分类学审计 mod 冲突与胜出方 |
| `spt-mcp-automation` | `spt` MCP 的操作中枢与路由 |
| `testing-spt-modpack` | 安装后主动验证（Level B/C 标准） |
| `diagnosing-spt-problems` | 症状优先的崩溃 / 掉帧 / 加载失败诊断 |
| `writing-spt-mod` | 从模板写新 mod（服务端 C# 或客户端 BepInEx/Harmony） |
| `writing-spt-modpack-devlog` | 维护项目 dev-log |
| `writing-spt-modpack-changelog` | 维护发布 changelog |

## MCP 表面

OpenCode 插件通过 `config.mcp` 钩子声明两个本地 stdio MCP 服务器：

| Server | 入口 | 能力 |
|--------|------|------|
| `mo2` | `tools/mo2-mcp/dist/index.js` | MO2 控制面：会话绑定、profile/mod/plugin 读写、FOMOD 安装、资产冲突、备份/回滚、审计日志（约 40 个 `mo2_*` 工具） |
| `spt` | `tools/spt-mcp/dist/index.js` | 纯文件系统 SPT mod 分析（无守护进程）：mod 清点、元数据读取、文件扫描、冲突分析、加载顺序预测、Forge 归档检索、知识库查询 |

MO2 控制面由 C++ MO2 插件 DLL + Python 加载器/broker + sidecar 组成，用 `scripts/install-mo2-control-plane.ps1` 部署。

## 知识库：knowledge/spt-kb

本仓库核心资产。SPT 资料抢救的全部产物，供 agent 与人类检索：

| 目录 | 内容 | 规模 |
|------|------|------|
| [`knowledge/spt-kb/wiki/`](knowledge/spt-kb/wiki/) | 官方 wiki 全站 Markdown vendor 副本 | 47 文件（锁定上游 commit） |
| [`knowledge/spt-kb/curated/modding-guide/`](knowledge/spt-kb/curated/modding-guide/) | 4.1 mod 开发指南（服务端/客户端解剖、25 示例迁移对照） | 4 章 |
| [`knowledge/spt-kb/curated/api-notes-4.1/`](knowledge/spt-kb/curated/api-notes-4.1/) | 4.1 源码 API 笔记（DI/加载/配置/数据库/路由/存档，多数已源码实读核销） | 7 篇 |
| [`knowledge/spt-kb/curated/recipes/`](knowledge/spt-kb/curated/recipes/) | 任务配方：加商人/自定义物品/自定义任务/路由/mod 通信等 | 12 份 |
| [`knowledge/spt-kb/archive/forge/`](knowledge/spt-kb/archive/forge/) | Forge 模组站归档：全站目录 + 热门详情 + 成品 zip + 源码 clone + 抓取脚本 | 1822 mod / 398MB |
| [`knowledge/spt-kb/sources/`](knowledge/spt-kb/sources/) | 仓库登记册（commit 锁定）、第三方资料、应急预案 | 2 文件 |

入口：`knowledge/spt-kb/INDEX.md`（按「我想做什么」检索）、`VERSIONS.md`（版本地图）。

### 本地关联资产（仓库之外）

| 资产 | 路径 |
|------|------|
| SPT 官方 20 仓库全量 clone（外部保留） | `E:\云文件\GitHub\SPT-archive\` |
| SPT 4.1 服务端源码 fork | `E:\云文件\GitHub\SamMeow_SPT410_source_code` |

## 整合包搭建与 mod 开发

**架构决策已锁定**（wayfinder，2026-08-02，见 `docs/wayfinder/MAP.md`）：

- **6 阶段管线**：意图理解 -> mod 匹配/开发 -> 冲突分析 -> 人工审查 -> 构建 -> 验证
- **14 个 SPT skills**：覆盖策展、构建、评估、安装解读、冲突审计、测试、诊断、mod 编写、dev-log/changelog（见 `skills/using-spt-modding-superpowers/`）
- **冲突分类学**：20 类冲突，元数据级可检测大部分服务端冲突（见 `docs/wayfinder/findings/`）
- **Mod 模板**：`templates/server-mod/` + `templates/client-mod/`
- **MO2 保留**作为 mod 管理层，SPT 特化版 MO2 为未来方向
- **Forge 离线模式**：全部 mod 数据来自本地归档，不依赖 live API

需求分析（历史）：`docs/可行性研究报告-SPT整合包自动化搭建.md`（v3.0，基线 SPT 3.11.4，部分结论已被 wayfinder 取代）

## 安装与使用

见 [`.opencode/INSTALL.md`](.opencode/INSTALL.md)。要点：

- 纯 OpenCode 插件，无其他 harness 依赖。
- Windows（MO2 控制面仅限 Windows）。
- Node 22+（两个 MCP 服务器运行于 Node）。

### 贡献与许可

- 贡献：见 [CONTRIBUTING.md](CONTRIBUTING.md)；内部设计文档在 `docs/internal/`
- 许可：MIT（见 [LICENSE](LICENSE)）
