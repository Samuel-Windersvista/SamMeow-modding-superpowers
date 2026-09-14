---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 09 打包与发布（PKG）

> **Domain slug:** `PKG` · **规则 ID 前缀:** `STD-PKG-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 05，2026-09-14）。

## 维度范围

- MO2 overlay 布局（`SPT_Runtime/user/mods/<Name>/`、`BepInEx/plugins/<Name>/`）
- meta.ini `comments`/`notes` 约定
- paired 单 zip 双端
- 服务端 mod 目录唯一 `IModMetadata`
- README / LICENSE 必备

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

### STD-PKG-002 — 为每个 mod 建立独立 MO2 overlay 并填写 meta.ini `comments`/`notes`

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`tools/mo2-mcp/src/tools/mo2-install.ts`（`comments` 为必填 `.min(1)`，与可选 `notes` 一并写入 meta.ini `[General]`）；`tools/mo2-mcp/src/tools/mo2-set-mod-notes.ts`（`notes` 可后置写入）；`skills/building-spt-modpack/SKILL.md`（overlay 命名 `<category>-<mod-name>-<version>`，每个 overlay 自描述）；语料：机制推断，无语料先例（EV-NOCORPUS）
- **Rule:** 每个 mod 以独立 MO2 overlay 安装，overlay 名遵循 `<category>-<mod-name>-<version>`，并在 meta.ini 写入非空 `comments`（MO2 mod 列表短摘要）与 `notes`（较长的安装记录：来源、版本、安装日期、作者说明）。本规则面向发布归档 / MO2 overlay 场景；纯源码仓库（不产出发布归档）判 N-A。

```
[General]
comments="工具-示例探针-0.1.0：批次 3 新增，服务端探针"
notes="来源：Forge <id>_release/xxx.zip；安装日期 2026-09-14；作者说明：需先装 WTT-CommonLib"
```

> 深入：[skills/building-spt-modpack/SKILL.md](../../../../skills/building-spt-modpack/SKILL.md)、[tools/mo2-mcp/src/tools/mo2-install.ts](../../../../tools/mo2-mcp/src/tools/mo2-install.ts)

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
