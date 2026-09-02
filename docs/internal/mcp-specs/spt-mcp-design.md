# SPT MCP Server -- 设计规格

> 日期：2026-08-04
> 状态：已批准（自主决策，Overseer 授权）
> 依据：wayfinder #1 冲突分类学、#2 MO2 集成分析、#4 管线定义

## 修订记录（2026-08-05，实战验证后）

**核心假设修正：server mod 元数据不在 package.json，而在 DLL 的 IModMetadata。**

原设计假设 server mod = `<modPath>/package.json`。实战验证（spt-mcp helper + WarsawTrader/SkillsExtended/SptDbDump 三个真实 mod）确认：
- SPT 4.1 的 server mod 是 `user/mods/<mod>/` 下顶层 DLL，**无 package.json**（ModLoader.cs 直接扫 .dll）
- 元数据（ModGuid/Name/Version/SptVersion）从 DLL 的 IModMetadata 实现读取（构造器 IL ldstr 常量，AsmResolver 静态提取）
- `package.json` 路径保留为 legacy 回退

实现落地：`tools/spt-mcp/helper/`（.NET CLI `spt-metadata-reader`，AsmResolver 读构造器 IL）+ `src/mod-reader.ts` 的 `listServerMods` 识别顶层 DLL 目录。详见 `knowledge/spt-kb/curated/api-notes-4.1/server-mod-metadata-dll.md`。

## 定位

`spt` MCP server 是 SPT 4.1 mod 分析的核心工具，替代 xEdit 在 bgs 工作流中的角色。它提供 mod 清单读取、冲突分析、Forge 归档查询、知识库检索、MO2 桥接能力。

## 核心架构决策

### 无 daemon

xedit MCP 需要一个运行中的 xEdit 进程（60-240s 启动，named pipe 通信，生命周期管理）。spt MCP **不需要** -- SPT mod 分析是纯文件系统操作，所有工具同步返回。

**理由：**
- 服务端 mod 元数据在 `package.json` 中（JSON 文件，直接读）
- 客户端 mod 元数据在 DLL 属性中（通过 .NET helper CLI 读取，同步调用）
- 冲突分析是纯计算（输入 = mod 元数据集合，输出 = 冲突报告）
- Forge 归档是本地 JSON 文件
- 知识库是本地 index.json + markdown 文件

### .NET helper CLI（客户端 DLL 元数据读取）

客户端 BepInEx 插件的元数据（BepInPlugin GUID、BepInDependency、版本号）嵌在 .NET 程序集属性中。Node.js 无法直接读取。

**方案：** 一个轻量 C# CLI 工具（`tools/spt-mcp/dotnet-metadata-reader/`），使用 `System.Reflection.Metadata` 读取程序集属性，输出 JSON。MCP server 通过 `child_process.execFile` 同步调用。

**为什么不现在做 IL 分析：** IL 级冲突检测（Harmony patch 目标、Transpiler 模式）需要完整的 IL 反编译管线（Mono.Cecil），那是后期能力。当前只需要读取程序集属性级别的元数据。

## 工具清单（10 个）

### Mod 清单（3）

| 工具 | 功能 | 输入 | 输出 |
|------|------|------|------|
| `spt_list_mods` | 扫描目录列出所有 mod | `{ path, type: "server"\|"client" }` | `[{ name, version, guid, dependencies[], path }]` |
| `spt_read_mod_metadata` | 读单个 mod 的完整元数据 | `{ modPath, type }` | package.json 内容 或 BepInEx 属性 |
| `spt_scan_mod_files` | 列出 mod 的文件清单 | `{ modPath }` | `[{ relativePath, sizeBytes }]` |

**实现要点：**
- 服务端 mod：读 `<modPath>/package.json`，提取 name/version/guid/dependencies
- 客户端 mod：调用 .NET helper CLI 读 DLL 的 BepInPlugin/BepInDependency 属性
- 文件扫描：递归列出所有文件，返回相对路径和大小

### 冲突分析（2）

| 工具 | 功能 | 输入 | 输出 |
|------|------|------|------|
| `spt_analyze_conflicts` | 分析一组 mod 的冲突 | `{ modPaths: string[], sptPath }` | 冲突报告（按 B/S/O/C 分级） |
| `spt_predict_load_order` | 预测服务端 mod 加载顺序 | `{ modPaths: string[] }` | 排序后的 mod 列表 |

**冲突检测项（M 级，元数据可检测）：**

| 检测项 | 方法 | 严重度 |
|--------|------|--------|
| ModGuid 重复 | 比较所有 mod 的 GUID | B |
| 依赖声明缺失/循环 | 解析 dependencies | B |
| SPT 版本失配 | 比较 mod 的 sptVersion 与目标版本 | B |
| 路由 URL 撞车 | 读 mod 源码中的路由注册 | B（先注册者胜） |
| 表 key 修改重叠 | 读 mod 源码中的表注入 key | O（后写者胜，按预测顺序） |
| 文件覆盖 | 比较文件清单中的同路径文件 | O（MO2 优先级仲裁） |
| 配置文件同 key | 比较 config 文件 schema | O/S |
| 枚举常量名/值撞车 | 读 patcher JSON | B |
| Harmony 目标重叠 | （标记 unknown，需 IL） | ? |

**加载顺序预测：**
- 按 TypePriority 升序排列
- 相同 TypePriority 按 ModGuid 字母序
- 后加载的 mod 覆盖先加载的（表注入）
- 先注册的路由胜出（路由）

### Forge 归档（2）

| 工具 | 功能 | 输入 | 输出 |
|------|------|------|------|
| `spt_forge_search` | 搜索归档 mod | `{ query?, category?, sptVersion?, modType? }` | 匹配的 mod 列表 |
| `spt_forge_get_mod` | 获取 mod 详情 | `{ modId }` | 完整元数据 + 版本历史 + 文件列表 |

**实现要点：**
- 读 `knowledge/spt-kb/archive/forge/api/mods-catalog.json`（1822 mod 目录）
- 读 `knowledge/spt-kb/archive/forge/hot-index.json`（95 热门 mod 索引）
- 读 `knowledge/spt-kb/archive/forge/api/hot-mods/<id>.json`（单个 mod 详情）
- 搜索 = 对 catalog 做内存过滤（标题、描述、分类匹配）

### 知识库（2）

| 工具 | 功能 | 输入 | 输出 |
|------|------|------|------|
| `spt_kb_query` | 查询 spt-kb | `{ topic?, domain?, version?, keyword? }` | 匹配的 KB 条目列表 |
| `spt_kb_get` | 获取 KB 条目全文 | `{ path }` | 文件内容（markdown） |

**实现要点：**
- 读 `knowledge/spt-kb/index.json`
- 按 version/domain/topic 过滤
- keyword 对 title 做子串匹配

### MO2 桥接（1）

| 工具 | 功能 | 输入 | 输出 |
|------|------|------|------|
| `spt_mo2_status` | MO2 控制面状态 | `{}` | `{ connected, profile?, modCount? }` |

**实现要点：**
- 复用现有 MO2 控制面（`mo2_agent_control.py`）
- BGS 特异性集中在 `plugins.*`（esp/esm），SPT 下置空
- `mods.*`、`organizer.*`、`launch.*` 通用，直接复用
- 初始版本只暴露 status，mod 操作后续按需添加

## 目录结构

```
tools/spt-mcp/
├── package.json
├── tsconfig.json
├── src/
│   ├── index.ts                 -- MCP server 入口
│   ├── types.ts                 -- 类型定义
│   ├── tools/
│   │   ├── list-mods.ts         -- spt_list_mods
│   │   ├── read-mod-metadata.ts -- spt_read_mod_metadata
│   │   ├── scan-mod-files.ts    -- spt_scan_mod_files
│   │   ├── analyze-conflicts.ts -- spt_analyze_conflicts
│   │   ├── predict-load-order.ts-- spt_predict_load_order
│   │   ├── forge-search.ts      -- spt_forge_search
│   │   ├── forge-get-mod.ts     -- spt_forge_get_mod
│   │   ├── kb-query.ts          -- spt_kb_query
│   │   ├── kb-get.ts            -- spt_kb_get
│   │   └── mo2-status.ts        -- spt_mo2_status
│   ├── conflict-engine.ts       -- 冲突分析引擎（纯函数）
│   ├── mod-reader.ts            -- mod 元数据读取（package.json + DLL helper）
│   └── forge-reader.ts          -- Forge 归档读取
├── dotnet-metadata-reader/      -- .NET helper CLI
│   ├── MetadataReader.csproj
│   └── Program.cs
└── tests/
    └── unit/
        ├── conflict-engine.test.ts
        ├── mod-reader.test.ts
        └── forge-reader.test.ts
```

## 技术选型

- **TypeScript + Node.js** -- 与 xedit MCP 一致
- **@modelcontextprotocol/sdk** -- MCP 协议实现
- **zod** -- 输入验证
- **无外部 daemon** -- 纯文件系统 + 纯计算
- **.NET helper CLI** -- net10.0，System.Reflection.Metadata，单文件发布

## 与 xedit MCP 的差异

| 组件 | xedit MCP | spt MCP | 原因 |
|------|-----------|---------|------|
| daemon | 需要（xEdit 进程） | 不需要 | SPT 分析是文件操作 |
| 生命周期管理 | 需要 | 不需要 | 无进程 |
| named pipe | 需要 | 不需要 | 无进程间通信 |
| 7 阶段 pipeline | 需要 | 简化（validate + execute） | 无状态机 |
| audit 日志 | 需要 | 后续添加 | 初期不需要 |
| 非阻塞状态机 | 需要 | 不需要 | 所有工具同步返回 |

## 后续扩展（不在本次范围）

- IL 反编译集成（Harmony patch 目标检测）
- MO2 mod 安装/卸载/启停操作
- spt_kb FTS5 全文搜索（当前是精确过滤）
- audit 日志
- 冲突分析缓存
