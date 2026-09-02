# SamMeow-modding-superpowers

[BB-84C/bgs-modding-superpowers](https://github.com/BB-84C/bgs-modding-superpowers) 的 **SPT 改造版**——面向《离线塔科夫 SPT》mod 开发与整合包自动化搭建的 agent 插件工作区。

> 定位：原项目是 Bethesda Game Studio 模组包策展工具（xEdit / MO2 / bgs_kb）；本仓库保留其上游能力，并以此为骨架改造为 SPT 生态服务。

**快速上手：** [`docs/使用指南.md`](docs/使用指南.md)

## 为什么存在

SPT（Single Player Tarkov）项目可能停止运作。本仓库承担两项使命：

1. **资料抢救**：SPT 官方 wiki、21 个官方仓库、Forge 模组站 1822 个 mod 的元数据、95 个热门 mod 成品与 18 个源码仓库，已全部归档（见下方知识库）。
2. **能力建设**：基于 bgs-modding-superpowers 的架构（MCP 服务器 + skills + 控制面），改造为 SPT 4.1 的 mod 开发与整合包搭建流水线。

## SPT 版本策略

| 阶段 | 版本 | 说明 |
|------|------|------|
| 基线（现状） | SPT 3.11.4 | 可行性研究报告的验证基线（150+ mod） |
| 目标（终态） | **SPT 4.1** | 最终锁定版本，可能永远停留于此 |

mod 开发一律按 4.1 目标编写（C# 服务端，`IModMetadata`/DI/Table 注入体系），3.11 资料仅作概念对照。

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
| SPT 关键仓库（已合并入库） | `external/spt-archive/`（server-mod-examples、modules、mod-examples、wiki） |
| SPT 官方 20 仓库全量 clone（外部保留） | `E:\云文件\GitHub\SPT-archive\` |
| SPT 4.1 服务端源码 fork | `E:\云文件\GitHub\SamMeow_SPT410_source_code` |

## 整合包搭建与 mod 开发

**架构决策已锁定**（wayfinder，2026-08-02，见 `docs/wayfinder/MAP.md`）：

- **6 阶段管线**：意图理解 -> mod 匹配/开发 -> 冲突分析 -> 人工审查 -> 构建 -> 验证
- **14 个 SPT skills**：9 个从 bgs 映射 + 3 个转型 + 2 个新增（见 `skills/using-spt-modding-superpowers/`）
- **冲突分类学**：20 类冲突，元数据级可检测大部分服务端冲突（见 `docs/wayfinder/findings/`）
- **Mod 模板**：`templates/server-mod/` + `templates/client-mod/`（开发中）
- **MO2 保留**作为 mod 管理层，SPT 特化版 MO2 为未来方向
- **Forge 离线模式**：全部 mod 数据来自本地归档，不依赖 live API

需求分析（历史）：`docs/可行性研究报告-SPT整合包自动化搭建.md`（v3.0，基线 SPT 3.11.4，部分结论已被 wayfinder 取代）

---

## 上游能力保留（bgs-modding-superpowers v0.2）

以下为原项目能力，保留未动，供未来改造 SPT 工作流时复用：

- **`xedit` MCP server** — 九种意图工具 + 原子 `xedit_call` 透传，7 阶段 harness 管线
- **`bgs_kb` MCP server** — 本地 SQLite + FTS5 知识库查询（`bgs_kb_status` / `bgs_kb_query` / `bgs_kb_get`）
- **MO2 control plane** — C++ MO2 插件 DLL + Python 加载器 + broker（`scripts/install-mo2-control-plane.ps1` 部署）
- **xEdit hook bridge** — Delphi DLL，解除 MO2 下 xEdit 无人值守启动限制（`tools/xedit-hook-bridge/dist/`）
- **Skills**：`using-bgs-modding-superpowers`、`setting-up-bgs-modding-environment`、`xedit-automation`、`xedit-conflict-audit`、`writing-modpack-devlog`、`writing-modpack-changelog`

### 安装（上游方式）

OpenCode：

```json
{
  "plugin": ["bgs-modding-superpowers@git+https://github.com/BB-84C/bgs-modding-superpowers.git"]
}
```

Claude Code / Codex 安装方式见 `.opencode/INSTALL.md` 与 `.mcp.json`（通过 `${CLAUDE_PLUGIN_ROOT}` 解析）。

> 注：SPT 版 skills 正在编写中。核心 skills 已完成（bootstrap、writing-spt-mod、curating-spt-modpack、spt-conflict-audit、building-spt-modpack），辅助 skills 进行中。上游 bgs skills 保留作参考。

### 首次运行（上游模式）

在模组包项目目录提问「Set up the BGS modding environment.」——检测 MO2、安装控制面、可选拉取 xEdit、初始化 dev-log 与 changelog。

### 需求（上游）

- Windows（MO2 控制面与 xEdit hook bridge 仅限 Windows）
- 目标游戏：Skyrim SE/AE、Fallout 4/76、Starfield（上游）；SPT 场景不需要 MO2
- Node 22+（MCP 服务器运行于 Node）

### 指向你自己的 MO2

`xedit` MCP 通过 `BGS_MO2_ROOT` 环境变量定位 MO2（`ModOrganizer.exe` 所在目录）：

- OpenCode（`~/.config/opencode/opencode.json`，harness MCP 块，在 `xedit` server 条目上设 env）：

  ```json
  {
    "mcp": {
      "xedit": { "env": { "BGS_MO2_ROOT": "D:\\Starfield MO2" } }
    }
  }
  ```

- Codex（`~/.codex/config.toml`，`codex plugin add` 之后）：

  ```toml
  [mcp_servers.xedit.env]
  BGS_MO2_ROOT = "D:\\Starfield MO2"
  ```

- Claude Code：编辑物化插件 `.mcp.json` 的 env 块，或在启动 Claude Code 的 shell 中设置环境变量。

设置后启动器默认使用 `<BGS_MO2_ROOT>/tools/xEdit/xEdit.exe`，从 `<BGS_MO2_ROOT>/profiles/<profile>/` 解析 `plugins.txt`。每次调用的覆盖参数（`xedit_start({ moRoot, launcherPath, ... })`）优先于环境变量。`setting-up-bgs-modding-environment` skill 会在首次运行时检测你的 MO2 安装并引导接线。

### 贡献与许可

- 贡献：见 [CONTRIBUTING.md](CONTRIBUTING.md)；内部设计文档在 `docs/internal/`
- 许可：MIT（见 [LICENSE](LICENSE)）
