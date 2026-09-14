---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 01 仓库与目录结构（STRUCT）

> **Domain slug:** `STRUCT` · **规则 ID 前缀:** `STD-STRUCT-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 02，2026-09-14）。

## 维度范围

- 单仓库分层惯例：`src/`、`Server/`、`Client/`、`Shared/` 等顶层目录的取舍
- 根目录仓库文件：README、LICENSE、`.gitignore`、`.editorconfig`
- 构建产物（`bin/`、`obj/`）禁止提交源码仓库
- 顶层目录命名与功能分区惯例

## 规则

### STD-STRUCT-001 — 在仓库根提供 `.gitignore` 排除构建产物与 IDE 文件

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`templates/server-mod/.gitignore`、`templates/client-mod/.gitignore`；语料：近期 297 个源码目录中 269 个含 `.gitignore`（EV-CORPUS-CSPROJ）。
- **Rule:** 每个 mod 仓库根目录必须包含 `.gitignore`，至少排除构建产物（`bin/`、`obj/`）与常见 IDE/用户文件。

```gitignore
# 构建产物
bin/
obj/

# IDE / 用户文件
*.user
.vs/
.idea/
```

> 深入：模板 [templates/server-mod/.gitignore](../../../../templates/server-mod/.gitignore)、[templates/client-mod/.gitignore](../../../../templates/client-mod/.gitignore)（仓库根相对路径）

### STD-STRUCT-002 — 禁止提交 `bin/`、`obj/` 构建产物

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`templates/server-mod/.gitignore`、`templates/client-mod/.gitignore`（模板显式忽略构建产物）；语料：近期 297 个目录中 `bin/` 与 `obj/` 各出现 19 次（约 6.4% 仓库违规提交）（`EV-CORPUS-STRUCT`、`EV-CORPUS-TOP5`）。
- **Rule:** 源码仓库不得包含 `bin/`、`obj/` 及其内容；构建产物只应存在于本地或 CI 输出目录，由发布流程单独打包。

```powershell
# 已误提交时从索引移除（保留本地文件）
git rm -r --cached bin obj
```

> 深入：[evidence-index.md](evidence-index.md) `EV-CORPUS-TOP5`

### STD-STRUCT-003 — 用 `src/` 或功能子目录组织源码，避免平铺仓库根

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`templates/server-mod/src/`、`templates/client-mod/src/`；语料：近期目录顶层 `src/` 出现 34 次，而「全部源码平铺根目录」是 `EV-CORPUS-TOP5` 列出的主要不规范点之一（`EV-CORPUS-STRUCT`、`EV-CORPUS-TOP5`）。
- **Rule:** 单端 mod 应把 `.cs` 源文件放入 `src/` 或按功能命名的子目录（如 `Patches/`、`Helpers/`），工程文件可留在仓库根，避免多个源文件与工程文件混摊在根目录。

```text
my-server-mod/
├── MyServerMod.csproj
├── src/
│   ├── ModMetadata.cs
│   ├── ModEntry.cs
│   └── Services/
└── .gitignore
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)

### STD-STRUCT-004 — paired mod 使用单仓库并按 `Client/`、`Server/` 分层

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`external/spt-archive/wiki/Mod_Types.md:40-41`（仅说明可同时含服务端与客户端组件，未规定布局）；语料：58 个 hybrid mod 的 12 例布局抽样均为单仓库，顶层 `Client/` 出现 33 次、`Server/` 28 次（`EV-GAP-PAIRED`、`EV-CORPUS-STRUCT`）。
- **Rule:** 同时含服务端与客户端组件的 mod 应放在同一个仓库，用顶层 `Client/`、`Server/` 目录区分两端，两端共享的纯数据/常量放 `Shared/`，Fika 兼容层放 `Fika/`。

```text
my-paired-mod/
├── MyPairedMod.sln
├── Client/
│   └── MyPairedMod.Client.csproj   # netstandard2.1
├── Server/
│   └── MyPairedMod.Server.csproj   # net10.0
├── Shared/                         # 可选：两端共享的纯数据/常量
├── Fika/                           # 可选：Fika 兼容层
└── README.md
```

> 深入：[evidence-index.md](evidence-index.md) `EV-GAP-PAIRED`

### STD-STRUCT-005 — 在仓库根提供 `README.md` 说明安装与使用

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`templates/server-mod/README.md`、`templates/client-mod/README.md`；语料：近期 297 个目录中 217 个含 `README*`（约 73%）（`EV-CORPUS-STRUCT`）。
- **Rule:** 仓库根应包含 `README.md`，至少说明 mod 用途、安装位置（`user/mods/` 或 `BepInEx/plugins/`）与主要配置项。

```text
# My Mod

SPT 4.1 服务端 mod。安装：把 MyMod.dll 放入 user/mods/MyMod/。
配置：config/config.jsonc（首次加载自动从 defaultConfig.jsonc 复制）。
```

> 深入：[evidence-index.md](evidence-index.md) `EV-CORPUS-STRUCT`

### STD-STRUCT-006 — 在仓库根提供 `LICENSE` 声明授权

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 语料：近期 297 个目录中 227 个含 `LICENSE*`（约 76%）（`EV-CORPUS-STRUCT`）；机制：模板未附带 LICENSE 文件。
- **Rule:** 仓库根应包含 `LICENSE`（或 `LICENSE.md`），明确授权条款，便于玩家与整合包维护者判断可否再分发。

```text
MIT License

Copyright (c) 2026 <author>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files ...
```

> 深入：[evidence-index.md](evidence-index.md) `EV-CORPUS-STRUCT`

### STD-STRUCT-007 — 在仓库根提供 `.editorconfig` 统一代码风格

- **Level:** MAY
- **Applies:** both
- **Evidence:** 语料：`EV-CORPUS-ANOMALY` 的规范样本 `Mission-Control_2653_source` 含 `.editorconfig`；无专项计数（`EV-CORPUS-CSPROJ` 未统计该文件）。
- **Rule:** 可选地在仓库根提供 `.editorconfig`，让不同 IDE 与贡献者对齐缩进、换行与文件编码。

```ini
root = true

[*]
charset = utf-8
indent_style = space
indent_size = 4
end_of_line = lf
```

> 深入：[evidence-index.md](evidence-index.md) `EV-CORPUS-ANOMALY`
