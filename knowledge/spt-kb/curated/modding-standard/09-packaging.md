---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 09 打包与发布（PKG）

> **Domain slug:** `PKG` · **规则 ID 前缀:** `STD-PKG-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 05，2026-09-14）；`STD-PKG-002` 于 2026-10-16 meta.ini 描述字段冻结事故后重写，并新增 `STD-PKG-008`；新增 `STD-PKG-009`（mod 自身 `CHANGELOG.md` 版本记录，2026-10-17）。

## 维度范围

- MO2 overlay 布局（`SPT_Runtime/user/mods/<Name>/`、`BepInEx/plugins/<Name>/`）
- meta.ini `comments`/`notes` 描述字段与仓库真源约定
- paired 单 zip 双端
- 服务端 mod 目录唯一 `IModMetadata`
- README / LICENSE 必备
- mod 仓库内 `CHANGELOG.md` 版本记录与版本号一致性

## 规则

### STD-PKG-001 — 以游戏根相对路径组织发布归档

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`knowledge/spt-kb/wiki/Installing_Mods.md`（SPT 4.0 起 mod 归档必须以 `SPT`/`BepInEx` 顶层结构交付，整体拖入游戏根目录）；`knowledge/spt-kb/curated/api-notes-4.1/mod-loading.md`（`ModPath = "./user/mods/"`）；语料：58 个 hybrid 的 12 例布局/打包抽样中，发布形态多为单归档且含两端树；SPT-Casino 发布 zip 顶层含 `BepInEx/`（67 条目）与 `SPT_Runtime/`（22 条目）（EV-GAP-PAIRED）
- **Rule:** 发布归档的顶层目录必须按游戏根相对路径组织：客户端产物置于 `BepInEx/plugins/<Name>/`，服务端产物置于 `SPT_Runtime/user/mods/<Name>/`，使归档可直接拖入 SPT 根目录，或原样作为 MO2 overlay 安装。本规则面向发布归档 / MO2 overlay 场景；纯源码仓库（不产出发布归档）判 N-A。

```
<release>.zip
├── BepInEx/
│   └── plugins/
│       └── <Name>/            # 客户端插件 DLL 及其资源
└── SPT_Runtime/
    └── user/
        └── mods/
            └── <Name>/        # 服务端 mod DLL、config/ 等
```

> 深入：[wiki/Installing_Mods.md](../../wiki/Installing_Mods.md)、[wiki-tushonka/SPT_4x/Mod_Types.md](../../wiki-tushonka/SPT_4x/Mod_Types.md)

### STD-PKG-002 — 为每个 mod 建立独立 MO2 overlay 并维护 meta.ini 描述字段

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`tools/mo2-mcp/src/tools/mo2-install.ts`（`comments` 为必填 `.min(1)`，与可选 `notes` 一并写入 meta.ini `[General]`）；`tools/mo2-mcp/src/tools/mo2-set-mod-notes.ts`（`notes` 可后置写入）；`skills/building-spt-modpack/SKILL.md`（overlay 命名 `<category>-<mod-name>-<version>`，每个 overlay 自描述）；语料：自研 `mods/SamMeow-LLMTraderChat` 的 overlay `meta.ini` 描述字段在 6+ 次能力跃迁后仍停留在首次安装文本（EV-CORPUS-MO2META）
- **Rule:** 每个 mod 以独立 MO2 overlay 安装，overlay 名遵循 `<category>-<mod-name>-<version>`，`version` 与发布产物一致。meta.ini 的可见描述字段必须描述**当前**能力，不得停留在首次安装时的状态：
  - `comments` —— 一行中文功能摘要（MO2 mod 列表可见的短描述）。
  - `notes` —— 多行 `- ` 项目符号，逐项描述**当前能力 / 前置条件 / 隐私 / 回滚方式**；安装记录（来源、安装日期、作者说明）可以保留，但不再是唯一或主要内容。
  - 能力或版本变化后必须重写描述字段（同步机制见 `STD-PKG-008`）。
  本规则面向产出 MO2 overlay 的场景；纯源码仓库（不产出 overlay）判 N-A，**若产出 overlay 则 N-A 不成立**。

```
[General]
comments="工具-示例探针-0.1.0：批次 3 新增，服务端探针"
notes="- 当前能力：注册 1 个服务端探针路由，回显探测结果\n- 前置条件：需先装 WTT-CommonLib\n- 隐私：不联网，无外部数据外发\n- 回滚：删除本 overlay 即可，无持久化写入"
```

> 深入：[skills/building-spt-modpack/SKILL.md](../../../../skills/building-spt-modpack/SKILL.md)、[tools/mo2-mcp/src/tools/mo2-install.ts](../../../../tools/mo2-mcp/src/tools/mo2-install.ts)
> 交叉引用：`STD-PKG-008`（meta.ini 描述字段的仓库真源与部署同步）。

### STD-PKG-003 — 将 paired mod 的两端置于同一归档与同一服务端目录

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`ModLoader.LoadMods` 遍历 `user/mods/` 下每个目录、一个目录即一个 mod（EV-GAP-PAIRED、`knowledge/spt-kb/curated/api-notes-4.1/mod-loading.md`）；`knowledge/spt-kb/wiki/Installing_Mods.md`（归档同时含 `SPT`/`BepInEx` 两树时整体安装）；语料：58 个 hybrid 的 12 例布局抽样均为单仓库、发布形态多为单归档（EV-GAP-PAIRED），SPT-Casino 发布 zip 顶层含两端树（EV-GAP-PAIRED）
- **Rule:** paired mod 的客户端半置于 `BepInEx/plugins/<Name>/`，服务端全部 assembly 置于**同一个** `SPT_Runtime/user/mods/<Name>/` 目录，两半在同一归档中交付；不得把服务端半拆成多个 `user/mods/` 目录。

```
MyPairedMod-1.3.4.zip
├── BepInEx/plugins/MyPairedMod/
│   └── MyPairedMod.Client.dll
└── SPT_Runtime/user/mods/MyPairedMod/
    ├── MyPairedMod.Server.dll
    └── MyPairedMod.Game.dll
```

> 深入：[api-notes-4.1/mod-loading.md](../../curated/api-notes-4.1/mod-loading.md)、[wiki/Installing_Mods.md](../../wiki/Installing_Mods.md)

### STD-PKG-004 — 服务端 mod 目录内仅保留一个 `IModMetadata` 实现

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`ModLoader` 按目录加载，一个目录只能有一个 `IModMetadata` 实现（`SingleOrDefault` 抛 "Duplicate mod metadata found"，EV-GAP-PAIRED）；`knowledge/spt-kb/curated/api-notes-4.1/mod-loading.md`（每个 mod 必须恰好一个 `IModMetadata` 实现）；语料：19 例服务端元数据采样均为单实现（EV-CORPUS-META），SPT-Casino 打包说明（EV-GAP-PAIRED）
- **Rule:** 一个 `SPT_Runtime/user/mods/<Name>/` 目录内只允许存在一个 `IModMetadata` 实现；多 assembly 合并打包时，须确保除元数据 assembly 外的其余 assembly 不实现该接口。

```
SPT_Runtime/user/mods/MyMod/
├── MyMod.Metadata.dll     # 唯一实现 IModMetadata
└── MyMod.Runtime.dll      # 不得再实现 IModMetadata
```

> 深入：[api-notes-4.1/mod-loading.md](../../curated/api-notes-4.1/mod-loading.md)
> 交叉引用：`STD-META-001`（`IModMetadata` 唯一实现）。

### STD-PKG-005 — paired mod 两端使用同一版本号

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：无（发布版本联动为惯例，非加载机制要求）；语料：Tushonka-Territories `Client/Client.csproj` 与 `Server/Server.csproj` 均为 `1.3.4`、SPT-Casino 打包脚本用单一 `$version` 同时打包两端（EV-GAP-PAIRED）
- **Rule:** paired mod 的客户端工程、服务端工程（及共享工程）使用同一版本号（建议通过 MSBuild 属性集中定义），使玩家在两端看到一致版本。

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <Version>1.3.4</Version>
  </PropertyGroup>
</Project>
```

> 深入：[api-notes-4.1/mod-loading.md](../../curated/api-notes-4.1/mod-loading.md)
> 交叉引用：`STD-META-007`（paired 版本号联动）。

### STD-PKG-006 — 随仓库提供 README 与 LICENSE

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：无（仓库卫生惯例）；语料：近期 297 个仓库中 README 217 个、LICENSE 227 个（EV-CORPUS-STRUCT），缺失率约 27 % / 24 %（EV-CORPUS-TOP5）
- **Rule:** README / LICENSE 的提供要求见 `STD-STRUCT-005`、`STD-STRUCT-006`；本规则只补充打包侧要求——发布归档须随附 README 与 LICENSE，便于玩家与整合包维护者开箱使用。

```
<release>.zip
├── README.md      # 安装 / 配置 / 兼容性
├── LICENSE        # 授权
├── BepInEx/plugins/<Name>/       # 客户端产物（如有）
└── SPT_Runtime/user/mods/<Name>/ # 服务端产物（如有）
```

> 深入：[evidence-index.md](../../curated/modding-standard/evidence-index.md)（EV-CORPUS-STRUCT、EV-CORPUS-TOP5）
> 交叉引用：`STD-STRUCT-005`、`STD-STRUCT-006`（README / LICENSE 要求）。

### STD-PKG-007 — 组装多来源 mod 时重命名冲突的运行时文件并迁移旧目录

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`ModLoader` 按目录加载、一个目录即一个 mod（EV-GAP-PAIRED）；语料：SPT-Casino 由多个独立来源合并打包时重命名冲突的 `config.json`/`escrow.json`，并在安装脚本中处理旧目录迁移（EV-GAP-PAIRED）
- **Rule:** 当发布包由多个独立来源组装（或由旧版多目录合并）时，须重命名相互冲突的运行时文件（如 `config.json`、`escrow.json`），并在安装脚本中处理旧目录的迁移/清理，避免同名文件互相覆盖。

```
# 合并打包示例
SPT_Runtime/user/mods/MyMod/
├── config.json          # 主 mod 配置
├── casino.escrow.json   # 原冲突文件改名后并入
└── MyMod.Server.dll
```

> 深入：[operations/destructive-operation-guardrails.md](../../curated/operations/destructive-operation-guardrails.md)

### STD-PKG-008 — meta.ini 描述字段以 mod 仓库为真源并随部署同步

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：仓库内真源随代码版本控制、可被审查；`tools/mo2-mcp/src/tools/mo2-set-mod-notes.ts`（描述字段可后置写入 overlay meta.ini）；语料：自研 `mods/SamMeow-LLMTraderChat` 的真源置于仓库内 `deploy/meta.json`，但 overlay `meta.ini` 以 `robocopy（保留 meta.ini）` 部署，导致描述字段冻结于 M1「M1 测试构建 / 仅解锁 Therapist 回显」，而 mod 已历经 19 商人、LLM 回复、动态态度、任务知识等 6+ 次能力跃迁（EV-CORPUS-MO2META）
- **Rule:** meta.ini 的 `comments` / `notes` / 版本字段的**真源必须在 mod 仓库内**（如 `<mod>/deploy/meta.json`），随代码版本控制、可被审查，不得只存在于 MO2 实例的 meta.ini 中。
  - **部署同步**：部署或重部署时，用 `tools/mo2-meta-sync/sync.mjs` 把仓库真源同步进 overlay `meta.ini`——**只改描述字段，保留全部 MO2 记账字段**（如 `[General]` 的 `installedFiles`、`[settings]` 等由 MO2 维护的键）。
  - **触发条件**：任何影响用户可见能力或版本号的变更（功能增删、前置条件变化、隐私行为变化、回滚方式变化、版本号变动）都必须更新仓库真源并重新同步。
  - **反模式**：不得以会跳过 / 保护 meta.ini 的复制方式（如 `robocopy` 保留 meta.ini）作为唯一部署手段——那样描述字段会与能力脱节。
  本规则面向产出 MO2 overlay 的场景；纯源码仓库（不产出 overlay）判 N-A，**若产出 overlay 则 N-A 不成立**。

```
<mod>/
├── deploy/
│   └── meta.json          # 真源：comments / notes / version
└── ...                    # 其余源码（随 git 版本控制）

# 部署时同步（只改描述字段，保留 MO2 记账字段）
node tools/mo2-meta-sync/sync.mjs --repo <mod> --overlay <MO2 overlay 路径>
```

> 深入：[tools/mo2-mcp/src/tools/mo2-set-mod-notes.ts](../../../../tools/mo2-mcp/src/tools/mo2-set-mod-notes.ts)、[evidence-index.md](../../curated/modding-standard/evidence-index.md)（EV-CORPUS-MO2META）
> 交叉引用：`STD-PKG-002`（overlay 建立与描述字段要求）。

### STD-PKG-009 — 为每个产出可用版本的 mod 维护仓库内 `CHANGELOG.md`

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：版本声明真源 `Directory.Build.props` `<Version>`（单端工程则为 csproj `<Version>`，`STD-META-005`）与 `deploy/meta.json` 的 `version`（`STD-PKG-008`）——本规则以这些声明点为一致性校验锚点（SPT 加载链不读取 CHANGELOG，故机制侧是版本真源而非加载机制）；语料：`archive/forge/mods/` 下 61 个 `_source` 目录随仓库提供 `CHANGELOG.md`，多为 `## [<version>] - <YYYY-MM-DD>` 分节 + 分组条目（如 `Kappa-Tracker_2981_source`、`CombatLog_3072_source`、`AutoIFF_2784_source`）；自研 `mods/SamMeow-LLMTraderChat`、`mods/PerformanceTweaks`、`mods/PerformanceTweaks413` 亦维护 `CHANGELOG.md`（EV-CORPUS-CHANGELOG）
- **Rule:** 每个产出**首个可用版本**（首次可部署 / 可发布的产物）的 mod，必须在 mod 仓库内建立 `<mod>/CHANGELOG.md`，随代码版本控制，并把该版本作为首节写入。此后**每次版本迭代、或任何影响用户可见能力的变更**都必须追加一节（不是可选）。约束：
  - **格式**：每版本一节，标题 `## <version> - <YYYY-MM-DD>`（最新在上）；节内按分组列出变更（建议：新增 / 变更 / 修复 / 移除 / 已知限制）；条目须可追溯到实际变更内容。
  - **一致性（可校验）**：`CHANGELOG.md` 最新版本号必须与 mod 的 `Directory.Build.props` `<Version>`（单端工程则为 csproj `<Version>`）、以及（若已接入 meta 同步，`STD-PKG-008`）`deploy/meta.json` 的 `version` 三者一致。
  - **反模式**：① 只在整合包级 release notes 记录，而 mod 自身无记录；② 版本号三处漂移；③ 用 changelog 代替 README 的功能说明。
  - **边界**：本规则管 mod 自身的 changelog；它与整合包级 release-changelog（`<project>/docs/release-changelog.md`，见 `writing-spt-modpack-changelog`）是**两个层级**，互不替代。
  本规则面向产出可用版本的 mod 仓库；从未产出可用版本的纯实验 / 源码仓库可判 N-A，**一旦产出可用版本即不再适用 N-A**（N-A 口径同 `STD-PKG-002`）。

```
<mod>/
├── CHANGELOG.md           # 版本真源之一：最新节版本号须与 <Version> / meta.json 一致
├── Directory.Build.props  # <Version>
├── deploy/
│   └── meta.json          # version（若已接入 meta 同步，STD-PKG-008）
└── ...

# CHANGELOG.md（最新在上）
## 1.4.0 - 2026-10-17
### 新增
- 新增 XXX 功能
### 修复
- 修复 YYY 崩溃
## 1.3.4 - 2026-09-30
### 新增
- 首个可用版本
```

> 深入：[skills/writing-spt-modpack-changelog/SKILL.md](../../../../skills/writing-spt-modpack-changelog/SKILL.md)、[evidence-index.md](../../curated/modding-standard/evidence-index.md)（EV-CORPUS-CHANGELOG）
> 交叉引用：`STD-META-005`（`Version` 三段式 semver 声明）、`STD-PKG-002`（overlay 建立与描述字段，N-A 口径）、`STD-PKG-008`（meta.json 版本真源与部署同步）。
