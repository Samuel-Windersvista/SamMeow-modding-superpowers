# SPT 离线塔科夫整合包自动化构建工具包 — 可行性研究与设计报告

> 版本：v1.0
> 日期：2026-06-25
> 基线：SPT 4.0+（C# server / .NET 9）、MO2 必须、以 MOD 源码仓库为主
> 来源项目：`bgs-modding-superpowers` 改造

---

## 一、摘要

### 核心结论

**将 `bgs-modding-superpowers` 改造为 SPT 4.0+ 离线塔科夫整合包自动化构建工具包，在工程上是可行的，但不是“换皮”，而是“借骨换肉”。核心目标是实现源码级（强）MOD 合并：将多个同类 MOD 的 C# 源码合并为单个或少数几个合成 MOD，从而直接消除大量兼容性问题和运行时冲突。**

- 可复用：插件 harness、Skill 框架、MCP 服务器框架、知识库引擎、审计/日志模式、MO2-MCP 控制平面。
- 必须重建：BGS 领域知识与技能、xEdit MCP、冲突审计模型、load-order 语义、archive/translator/papyrus 工具链。
- 关键新增：SPT IL/源码分析管线、源码级强合并引擎、合成 MOD 编译与更新管线、功能域配置模型、SPT 知识库。

### 关键约束（由用户锁定）

| 约束 | 取值 | 对架构的影响 |
|------|------|-------------|
| SPT 版本 | 4.0+（C# server） | 服务端 MOD 与客户端 MOD 统一使用 .NET IL 分析，无需再处理 TypeScript/package.json |
| MO2 角色 | **必需的管理器** | 最终产物必须是 MO2 mod overlay，运行时仍由 MO2/USVFS 投影；工具包不输出自包含目录 |
| MOD 来源 | 以 GitHub 源码仓库为主，Forge release archive 为辅 | 工具包必须具备源码拉取、源码分析、源码合并、源码编译能力 |
| 首期范围 | 不写具体 MOD 列表，先做可行性探索与设计报告 | 本阶段聚焦架构设计与风险识别，不进入实现 |

### 主要风险

1. **强合并的法律风险**：将多个 MOD 的源码合并为新的 DLL 必然涉及重新分发修改后的代码，必须逐 MOD 审查许可证（MIT/Apache/GPL/自定义）。
2. **强合并的维护负担**：原 MOD 更新后，合成 MOD 必须重新合并、重新编译、重新验证。
3. **SPT API 快速演进**：4.0 刚完成 C# 重写，API 和 mod 元数据格式可能在大版本间变化。
4. **Harmony / DI 运行时语义冲突无法完全静态检测**：IL 分析能发现“谁在 patch 谁”，但无法证明 patch 的语义是否互斥。
5. **MO2 与源码编译工作流的整合复杂度**：MO2 overlay 传统上用于分发预编译文件，源码级合并需要引入构建步骤。

---

## 二、背景与现状

### 2.1 SPT 4.0+ 架构

```
+-------------------+     HTTPS/WS     +-------------------+
|   游戏客户端       | <---------------> |   SPT 服务器        |
| (EscapeFromTarkov |                  | (SPT.Server.exe)   |
|  + BepInEx 注入)   |                  | C# .NET 9.0       |
+-------------------+                  +-------------------+
         |                                      |
    客户端 MOD:                          服务端 MOD:
    BepInEx/plugins/*.dll               SPT/user/mods/<name>/
    (C# BepInEx/Harmony)                (C# DLL + config/ + ...)
```

关键事实：

- SPT 4.0+ 服务端已从 Node.js/TypeScript 彻底迁移为 C#/.NET 9（仓库 `sp-tarkov/server-csharp`）。
- 服务端 MOD 现在也是 C# DLL，通过 `ModDllLoader` + 自定义 DI 框架加载。
- MOD 元数据嵌入在 C# `AbstractModMetadata` 子类中，而非 `package.json`。
- 客户端 MOD 保持 C# BepInEx/Harmony 不变。
- 官方 MOD 下载源为 Forge（`forge.sp-tarkov.com/mods`），MOD 压缩包必须包含 `SPT/`、`BepInEx/` 或两者兼有。

### 2.2 当前 bgs-modding-superpowers 能力

`bgs-modding-superpowers` 是一个多 harness AI 插件，核心由以下层次组成：

1. **插件入口**：OpenCode JS 插件、Claude Code hooks、Codex 插件，负责注入 bootstrap skill 和注册 MCP。
2. **Skill 系统**：16 个 Markdown skill，通过 `name` + `description` frontmatter 被 agent 自动触发。
3. **MCP 服务器**：
   - `xedit-mcp`：xEdit daemon 的非阻塞 harness
   - `bgs-kb-mcp`：SQLite+FTS5 知识库
   - `mo2-mcp`：MO2 控制平面
4. **工具链**：PowerShell 脚本、Python 工具（translator/papyrus/mo2-assets-engine/mo2-mcp-sidecar）、Rust 工具（bgs-archive）、Delphi DLL（xedit-hook-bridge）。
5. **知识库**：`knowledge/bgs-kb/` 下的 6 个 pack、147 条核心记录。

### 2.3 改造的必要性

BGS 与 SPT 的 modding 生态在底层模型上完全不同：

| 维度 | BGS | SPT 4.0+ |
|------|-----|----------|
| 插件格式 | `.esp/.esm/.esl` | C# DLL |
| 冲突模型 | record override / ITM / UDR / winning override | Harmony patch 竞争 / DI 单例竞争 / JSON 路径覆盖 |
| 加载顺序 | `plugins.txt` + ESL/ESM/master 语义 | C# 程序集加载顺序 + Harmony 优先级 + `OnLoadOrder` + mod 依赖 |
| 资产格式 | BA2/BSA + loose files | Unity asset bundles + loose files + JSON |
| 脚本语言 | Papyrus | C# / Harmony |
| 本地化 | SST/XML 字典 | 游戏内 JSON locale |
| 核心编辑器 | xEdit | 不适用；需 IL 反编译器和源码分析器 |

因此，BGS 专用组件不能简单迁移，必须替换为 SPT 专用组件。

---

## 三、目标与范围

### 3.1 总体目标

构建一个 AI 驱动的 SPT 4.0+ 整合包构建工具包，能够：

1. 从 GitHub 源码仓库（必要时从 Forge release archive）获取 MOD。
2. 对服务端和客户端 MOD 进行静态分析（IL 反编译 + 源码分析）。
3. 检测 MOD 间冲突（DI 竞争、Harmony patch 竞争、JSON 路径覆盖、依赖缺失等）。
4. 按功能域配置模型整合多个 MOD。
5. 通过源码级强合并将多个 MOD 合并为单个或少数合成 MOD，输出可在 MO2 中管理的 mod overlay。
6. 生成 dev-log / release-changelog 等项目资产。

### 3.2 范围边界

**在本设计阶段内**：

- 不实现具体 MOD 分析器。
- 不实现 IL 反编译管线的原型代码。
- 不实现 MO2 overlay 的写入逻辑。
- 输出：架构设计、组件边界、风险矩阵、分阶段路线图。

**在后续实现阶段**：

- Phase 1：SPT 分析型工具包（IL/源码分析 + 冲突检测）。
- Phase 2：源码合并与构建管线。
- Phase 3：功能域配置模型与自动化。

---

## 四、架构设计

### 4.1 高层架构

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        AI Agent (OpenCode / Claude / Codex)                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐              │
│  │ using-spt-...   │  │ evaluating-spt- │  │ spt-conflict-   │              │
│  │   bootstrap     │  │   mods          │  │   audit         │              │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘              │
│           │                    │                    │                       │
│  ┌────────▼────────────────────▼────────────────────▼─────────────────┐     │
│  │                     spt-modding-superpowers.js                     │     │
│  │              (OpenCode plugin entry / skill registration)          │     │
│  └────────┬───────────────────────────────────────────────────────────┘     │
└───────────┼─────────────────────────────────────────────────────────────────┘
            │
            │  MCP
            ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                              MCP Servers                                   │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────┐  │
│  │ spt-analysis-mcp     │  │ mo2-mcp              │  │ spt-kb-mcp       │  │
│  │ (IL/source analysis) │  │ (MO2 control plane)  │  │ (SPT knowledge)  │  │
│  └────────┬─────────────┘  └────────┬─────────────┘  └────────┬─────────┘  │
└───────────┼─────────────────────────┼─────────────────────────┼────────────┘
            │                         │                         │
            ▼                         ▼                         ▼
┌──────────────────────┐  ┌──────────────────────┐  ┌──────────────────────┐
│ SPT-IL-Toolkit       │  │ mo2-mcp-sidecar      │  │ spt-kb packs         │
│ (C# / Mono.Cecil)    │  │ (Python JSON-RPC)    │  │ (SQLite+FTS5)        │
├──────────────────────┤  ├──────────────────────┤  ├──────────────────────┤
│ - DLL loader         │  │ - profile read       │  │ - modding guides     │
│ - Harmony patch scan │  │ - mod install        │  │ - API drift          │
│ - DI scan            │  │ - overlay write      │  │ - conflict patterns  │
│ - metadata scan      │  │ - FOMOD/SPT install  │  │ - version matrix     │
└────────┬─────────────┘  └────────┬─────────────┘  └──────────────────────┘
         │                         │
         ▼                         ▼
┌──────────────────────┐  ┌──────────────────────┐
│ SPT Source Merger    │  │ MO2 VFS / USVFS      │
│ (C# / .NET 9 SDK)    │  │ (runtime projection) │
├──────────────────────┤  └──────────────────────┘
│ - namespace flatten  │
│ - DI reconciliation  │
│ - Harmony priority   │
│ - JSON config merge  │
└──────────────────────┘
```

### 4.2 组件详细设计

#### 4.2.1 `spt-analysis-mcp`

**定位**：替换 `xedit-mcp`，成为 SPT 领域的核心分析 harness。

**职责**：

- 扫描并加载 MOD 源码仓库或预编译 DLL。
- 对服务端 MOD DLL 提取 `ModMetadata`、DI 注册、`AbstractPatch`、路由注册、DB 修改路径。
- 对客户端 MOD DLL 提取 `BepInPlugin`、`BepInDependency`、`HarmonyPatch`、`HarmonyPriority`。
- 输出结构化 JSON 摘要，供冲突检测使用。

**工具表面（设计期草案）**：

| Tool | 用途 |
|------|------|
| `spt_analysis_status` | 分析器就绪状态 |
| `spt_analysis_load_mod` | 加载一个 MOD 的源码或 DLL |
| `spt_analysis_scan_metadata` | 提取 MOD 元数据 |
| `spt_analysis_scan_patches` | 提取 Harmony/AbstractPatch |
| `spt_analysis_scan_di` | 提取 DI 注册 |
| `spt_analysis_scan_db_paths` | 提取 DB JSON 修改路径 |
| `spt_analysis_export_summary` | 输出 MOD 结构化摘要 |
| `spt_analysis_compare_mods` | 对比多个 MOD 摘要，输出冲突矩阵 |

**实现语言**：C#（利用 Mono.Cecil、ICSharpCode.Decompiler）。

**复用点**：复用 `xedit-mcp` 的 pipeline 模式（validate → state-check → rules → forward → envelope → response → audit），但底层 forward 对象从 xEdit daemon 改为 `SPT-IL-Toolkit` 的本地进程/库调用。

#### 4.2.2 `SPT-IL-Toolkit`

**定位**：核心 IL 分析引擎，覆盖服务端和客户端 MOD。

**输入**：

- C# DLL 文件路径（来自 GitHub 构建产物或本地编译）
- 可选的源码目录（用于补充符号名、注释、配置 JSON）

**输出**：

```json
{
  "modGuid": "com.example.my-mod",
  "name": "MyMod",
  "version": "1.2.3",
  "sptVersion": "~4.0.0",
  "side": "server|client|hybrid",
  "metadata": { "dependencies": [], "incompatibilities": [], "isBundleMod": false },
  "diRegistrations": [
    { "serviceType": "IBotGenerator", "implementationType": "MyBotGenerator", "priority": "PreSptModLoader" }
  ],
  "harmonyPatches": [
    { "targetType": "Player", "targetMethod": "UpdateSwayFactors", "patchType": "Postfix", "priority": "Normal", "mod": "MyMod" }
  ],
  "dbPaths": [
    { "file": "user/mods/MyMod/db/traders.json", "operation": "merge" }
  ],
  "configKeys": [ "EnableFeatureX", "DamageMultiplier" ],
  "referencedAssemblies": [ "0Harmony.dll", "SPTarkov.Server.Core.dll" ]
}
```

**技术栈**：

- `Mono.Cecil`：读取 .NET 程序集元数据
- `ICSharpCode.Decompiler`：将 IL 反编译为可读 C#，用于辅助 LLM 理解复杂 patch
- 自定义分析器：识别 `AbstractModMetadata`、`AbstractPatch`、`IOnLoad`、`IInjectable` 等 SPT 特定模式

#### 4.2.3 `SPT Source Merger`

**定位**：源码级 MOD 合并引擎。

**输入**：

- 多个 MOD 的源码仓库
- 冲突矩阵 + 人工确认的 merge-manifest
- 功能域配置（experience.json）

**输出**：

- 一个或多个新的 C# 项目（合成 MOD）
- 合并后的 JSON 配置文件
- 编译后的 DLL，可直接放入 MO2 mod overlay

**合并策略（分层，默认执行 L6 源码融合）**：

| 层级 | 策略 | 示例 | 默认执行 |
|------|------|------|----------|
| L1 文件合并 | 简单覆盖/追加 | 不冲突的 JSON、贴图、bundle | 是 |
| L2 JSON 合并 | namespace 展平 + 冲突字段前缀 | 多个 MOD 的 `config.json` | 是 |
| L3 类型合并 | 重命名空间、接口适配 | 两个 MOD 都实现 `IBotGenerator` | 是 |
| L4 DI 合并 | 选主控制器、装饰器链、条件注册 | Realism vs APBS 的 bot 生成 | 是，冲突时人工确认 |
| L5 Harmony 合并 | 优先级调整、transpiler 合并 | 多个 Postfix 修改同一方法返回值 | 是，冲突时人工确认 |
| L6 源码融合 | 将多个 MOD 源码合并为一个程序集 | 高频冲突 MOD 组合 | 是（核心目标） |

**强合并技术方案**：

1. **源码归一化**
   - 将每个 MOD 的 C# 项目统一转换为标准 MSBuild 项目结构。
   - 将原 MOD 的命名空间从 `ModA` 映射到 `Synthetic.<feature>.ModA`，避免全局类型名冲突。
   - 对冲突的类型名、类名、方法名进行重命名或合并。

2. **元数据合并**
   - 为每个功能域生成一个新的 `ModMetadata` 记录。
   - 合并 `ModDependencies` 和 `Incompatibilities`：原 MOD 的依赖变成合成 MOD 的依赖；不兼容性取并集。
   - 合成 MOD 的 `ModGuid` 必须是新的全局唯一值（如 `com.synthetic.<feature>.v1`）。

3. **DI 注册合并**
   - 对同名服务（如 `IBotGenerator`），默认采用“最后一个注册生效”策略，但允许 merge-manifest 指定主实现或装饰器链。
   - 将原 MOD 的 `[Injectable]` 属性保留到合成项目中，必要时调整 `TypePriority`。

4. **Harmony Patch 合并**
   - 在合成项目中保留所有原 patch 类，必要时调整 `[HarmonyPriority]` / `[HarmonyBefore]` / `[HarmonyAfter]`。
   - 对同一目标方法的多个 patch，按 merge-manifest 调整优先级；无法自动合并时停止并请求人工裁决。

5. **JSON / 配置合并**
   - 按 namespace 展平多个 MOD 的 `config.json`：冲突字段加前缀（如 `ModA_SettingX`、`ModB_SettingX`）。
   - DB 修改路径的 JSON 按功能域合并为统一的合并后文件。

6. **编译输出**
   - 使用 .NET 9 SDK 编译合成项目。
   - 输出 `Synthetic.<Feature>.dll` + `config/` + 其他资产到 MO2 mod overlay。

**重要约束**：

- 强合并是默认路径。当自动合并无法安全完成时（如两个 MOD 都直接替换同一核心方法且语义互斥），工具包必须停止并请求人工裁决，不得静默降级为弱合并。
- 每次构建必须生成完整的 merge-manifest 和 diff 报告，记录哪些 MOD 被合并、哪些被排除、哪些需要人工决策。
- 合并输出必须保留原 MOD 的许可证声明、作者信息和源码引用。
- 合成 MOD 必须使用新的 `ModGuid`，且不能与任何原 MOD 冲突。
- 构建管线必须支持“增量重合并”：当任意原 MOD 更新时，只重新分析/合并受影响的功能域。

#### 4.2.4 `mo2-mcp` 适配

**定位**：保留现有 `mo2-mcp`，但扩展其支持 SPT 的安装结构。

**新增职责**：

- 识别 SPT MOD 压缩包结构（`SPT/`、`BepInEx/`、两者兼有）。
- 将合并后的合成 MOD 安装为 MO2 mod overlay。
- 管理 load order / priority（MO2 的 modlist.txt）。
- 在 MO2 中启动 SPT Launcher / Server。

**注意**：MO2 overlay 规则仍然是铁律——所有写入必须通过 `mo2-mcp`，不可直接写入 SPT 游戏目录。

#### 4.2.5 `spt-kb-mcp`

**定位**：替换 `bgs-kb-mcp` 的内容，保留其 SQLite+FTS5 引擎。

**内容领域**：

- SPT 安装与配置
- SPT 4.0+ MOD 开发规范
- 服务端/客户端 MOD 结构
- DI / Harmony / load order 机制
- 常见冲突模式与解决策略
- 版本兼容性矩阵

### 4.3 数据流

```
用户输入：
  ├─ experience.json（功能域配置，可选）
  ├─ mods-lock.json（MOD 列表 + 版本 + GitHub 仓库）
  └─ build-profile.json（构建参数）

        ↓

[Source Acquisition]
  ├─ git clone MOD 源码仓库
  ├─ 必要时从 Forge 下载 release archive
  └─ 缓存到本地工作区

        ↓

[Analysis Pipeline]
  ├─ SPT-IL-Toolkit 分析源码/DLL
  ├─ 输出每个 MOD 的结构化摘要
  └─ LLM 辅助分析复杂源码语义

        ↓

[Conflict Detection]
  ├─ 交叉对比摘要
  ├─ 生成冲突矩阵
  └─ 按功能域分类

        ↓

[Human Decision Gate]
  ├─ 展示冲突与合并建议
  ├─ 用户确认 merge-manifest
  └─ 关键设计决策必须人工确认

        ↓

[Source Merger]
  ├─ 按 merge-manifest 合并源码
  ├─ 编译合成 MOD
  └─ 合并 JSON 配置

        ↓

[MO2 Overlay Output]
  ├─ 生成 MO2 mod 目录结构
  ├─ 写入 mods/<synthetic-mod>/
  └─ 更新 modlist.txt / profile

        ↓

[Verification]
  ├─ 静态：结构检查、依赖检查
  ├─ 动态：启动 SPT server，检查日志错误
  └─ 生成 dev-log / release-changelog
```

---

## 五、与现有 bgs-modding-superpowers 的复用映射

### 5.1 可直接复用

| 现有组件 | 复用方式 |
|----------|----------|
| 插件 harness（OpenCode/Claude/Codex） | 复制 `.opencode/plugins/bgs-modding-superpowers.js` 为 `spt-modding-superpowers.js`，修改 skill 路径和 MCP 注册 |
| Skill 框架 | 保留 SKILL.md frontmatter 规范，内容重写 |
| MCP 服务器框架 | 保留 `xedit-mcp` 的 pipeline、state machine、audit 模式 |
| `bgs-kb-mcp` 引擎 | 保留 SQLite+FTS5 构建/查询代码，内容替换为 SPT 知识 |
| `mo2-mcp` | 保留控制平面和 overlay 规则，扩展 SPT MOD 安装识别 |
| `writing-modpack-devlog` / `writing-modpack-changelog` | 直接复用，路径和模板微调 |

### 5.2 需要重写

| 现有组件 | 重写为 |
|----------|--------|
| `using-bgs-modding-superpowers` | `using-spt-modding-superpowers` |
| `setting-up-bgs-modding-environment` | `setting-up-spt-modding-environment` |
| `maintaining-modding-environments` | `maintaining-spt-modding-environments` |
| `xedit-automation` | `spt-analysis-automation` |
| `xedit-conflict-audit` | `spt-conflict-audit` |
| `writing-bgs-load-order` | `writing-spt-load-order`（MO2 priority + SPT load order） |
| `evaluating-bgs-mods` | `evaluating-spt-mods` |
| `interpreting-mod-author-instructions` | `interpreting-spt-mod-instructions` |
| `curating-bgs-modpack` | `curating-spt-modpack` |
| `diagnosing-bgs-problems` | `diagnosing-spt-problems` |
| `testing-bgs-modpack` | `testing-spt-modpack` |
| `using-bgs-archive` | 弃用或替换为 Unity bundle 处理工具（远期） |
| `using-bgs-papyrus` | 弃用 |
| `using-bgs-translator` | 远期可替换为 SPT locale JSON 处理工具 |

### 5.3 必须新建

| 新组件 | 说明 |
|--------|------|
| `spt-analysis-mcp` | SPT 专用分析 harness |
| `SPT-IL-Toolkit` | C# IL 分析引擎 |
| `SPT Source Merger` | 源码合并与编译引擎 |
| `spt-kb-mcp` 内容 | SPT 知识库 records |

---

## 六、冲突模型

### 6.1 服务端冲突类型

| 冲突类型 | 检测方法 | 自动处理可能性 |
|----------|----------|----------------|
| `ModGuid` 重复 | 元数据扫描 | 高（报错） |
| `SptVersion` 不兼容 | 版本范围解析 | 高（锁定版本） |
| DI 服务重复注册 | 扫描 `[Injectable]` + `IOnLoad` | 高（通过强合并重构为单一/装饰器注册） |
| `OnLoadOrder` 优先级竞争 | 扫描 `TypePriority` | 中（可调整） |
| DB JSON 路径覆盖 | 静态分析文件操作 / 源码扫描 | 高（namespace 展平） |
| HTTP 路由重复 | 扫描 `IHttpListener` / route 注册 | 中（需人工确认） |
| 程序集引用缺失 | 依赖图分析 | 高（报错） |

### 6.2 客户端冲突类型

| 冲突类型 | 检测方法 | 自动处理可能性 |
|----------|----------|----------------|
| Harmony patch 目标方法冲突 | IL 扫描 `[HarmonyPatch]` | 高（报告） |
| Harmony 优先级竞争 | 扫描 `[HarmonyPriority]` | 中（可调整） |
| `BepInDependency` 缺失 | 扫描 `[BepInDependency]` | 高（报错） |
| 配置键冲突 | 扫描 `Config.Bind` | 高（namespace 展平） |
| 程序集引用冲突 / 版本不匹配 | 依赖图分析 | 中（绑定重定向） |

### 6.3 不可静态检测的冲突

- **运行时语义冲突**：两个 Postfix 都修改 `__result`，但语义上互斥。
- **DB 加载顺序依赖**：MOD A 修改 DB 后，MOD B 在 `PostDbLoad` 中读取。
- **异步生命周期竞争**：服务端 MOD 的 `OnLoad` / `OnUpdate` / `PostDBLoad` 回调顺序。
- **Unity bundle 语义冲突**：多个 MOD 替换同一 asset bundle 中的资源。

这些冲突需要动态测试或人工判断。

---

## 七、功能域配置模型

### 7.1 双层抽象

保留您原报告中的设计：

```json
// experience.json（用户可见）
{
  "version": "LIN-2026.06.25",
  "features": {
    "economy": { "type": "barter", "difficulty": "hard" },
    "combat": { "ballistics": "realistic", "armor": "reworked" },
    "ai": { "behavior": "realistic", "loot": "enabled" },
    "progression": { "hideout": "enabled", "skills": "extended" }
  }
}

// feature-map.json（构建工具可见）
{
  "economy.barter": {
    "primary": "barter_economy",
    "secondary": ["Softcore"],
    "parameters": { "Softcore.economyOptions": "hard" }
  },
  "combat.ballistics.realistic": {
    "primary": "Realism",
    "requires": ["Realism-Client"]
  },
  "ai.behavior.realistic": {
    "primary": "SAIN",
    "requires": ["BigBrain", "Waypoints"]
  }
}
```

### 7.2 与 MO2 的整合

功能域配置不直接对应单个 MOD，而是对应一组 MOD 及其参数。构建工具输出一个或多个合成 MOD 到 MO2 overlay，每个合成 MOD 代表一个功能域或一组正交功能域。

```
MO2 mods/
  ├─ LIN-Core/           # 基础设施合成 MOD
  ├─ LIN-Economy/        # 经济功能域合成 MOD
  ├─ LIN-Combat/         # 战斗功能域合成 MOD
  ├─ LIN-AI/             # AI 功能域合成 MOD
  └─ LIN-Assets/         # 纯资产 MOD（不合并，仅按优先级堆叠）
```

---

## 八、风险矩阵

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| SPT API / MOD 元数据格式变化 | 高 | 高 | 版本锁定；分析器使用抽象层；KB 跟踪版本差异 |
| 强合并违反 MOD 许可证 | 高 | 高 | 构建前强制许可证扫描；仅合并允许修改再分发的 MOD；保留作者和许可证声明；GPL 传染链检测 |
| 强合并后维护负担爆炸 | 高 | 高 | 仅对高频冲突 MOD 组合启用；建立增量重合并管线；自动化更新检测与差异报告 |
| IL 分析遇到混淆 DLL | 极低 | 中 | SPT 生态无混淆传统；如遇混淆降级为签名级分析 |
| LLM 幻觉导致错误分析 | 中 | 高 | LLM 输出仅作为建议；关键路径人工确认；分析摘要可被验证 |
| MO2 与源码编译工作流整合失败 | 中 | 高 | 先实现最小端到端原型；MO2 overlay 规则作为不变约束 |
| MOD 作者删库/改私有 | 中 | 高 | 维护 forks 镜像；支持 Forge archive 作为 fallback |
| 运行时语义冲突无法检测 | 高 | 中 | 设计动态冒烟测试；明确告知用户自动化的上限 |
| 法律风险（反灰条款、BSG EULA） | 低 | 高 | 不绕过正版验证；不重新分发 EFT 本体；遵守 SPT 许可 |

---

## 九、强合并后的更新策略

强合并一旦实施，原 MOD 的任何更新都必须触发重新合并流程。必须设计一条可持续的更新管线，否则整合包会迅速腐烂。

### 9.1 增量重合并

```
检测原 MOD 更新
    ↓
拉取更新后的源码
    ↓
仅重新分析受影响的功能域
    ↓
生成更新差异报告（added / changed / removed / conflicting）
    ↓
自动重合并（若无新冲突）
    ↓
人工裁决（若出现新冲突）
    ↓
重新编译合成 MOD
    ↓
写入 MO2 overlay 并更新 modlist.txt
```

### 9.2 更新差异报告格式

每次重合并必须输出：

```json
{
  "feature": "Economy",
  "syntheticMod": "LIN-Economy",
  "upstreamChanges": [
    { "mod": "Softcore", "from": "1.2.3", "to": "1.3.0", "changeType": "minor" },
    { "mod": "barter_economy", "from": "2.0.1", "to": "2.0.2", "changeType": "patch" }
  ],
  "conflictsIntroduced": [
    { "type": "DI", "service": "ITraderService", "mods": ["Softcore", "barter_economy"] }
  ],
  "actions": ["auto-merged", "needs-human-decision", "blocked"],
  "newBuild": "LIN-Economy-2026.06.30.dll"
}
```

### 9.3 版本锁定与延迟升级

- `mods-lock.json` 必须记录每个原 MOD 的精确版本和 commit hash。
- 默认不自动接受 major 版本更新；minor/patch 更新可配置为自动。
- 当 SPT 本体大版本更新时，整个功能域映射可能需要人工审查。

### 9.4 许可证再审查

- 每次原 MOD 更新后，必须重新检查其许可证是否变更。
- 若某 MOD 从允许修改的许可证变为不允许，则该 MOD 必须退出合成 MOD，改为独立 MO2 mod 安装。

---

## 十、分阶段实施路线图

### Phase 0：基础数据建设（1-2 周）

- 建立 `spt-kb/` 知识库骨架
- 定义 MOD 清单模板 `mods-lock.json`
- 收集 5-10 个代表性 MOD 的 GitHub 仓库作为验证集
- 完成本设计文档的细化版本

### Phase 1：SPT 分析型工具包（6-8 周）

- 实现 `SPT-IL-Toolkit` 原型
  - 客户端 DLL 分析（Harmony patch、BepInDependency）
  - 服务端 DLL 分析（ModMetadata、DI 注册、AbstractPatch）
- 实现 `spt-analysis-mcp` 最小工具集
- 实现冲突检测引擎（覆盖 L1-L6 所有层级）
- 重写核心 skill：`using-spt-modding-superpowers`、`spt-analysis-automation`、`spt-conflict-audit`
- 验证：对 5-10 个 MOD 生成冲突矩阵

### Phase 2：源码合并与构建管线（6-8 周）

- 实现 `SPT Source Merger`
  - JSON namespace 展平
  - C# 项目合并与编译
  - 合成 MOD 输出
- 扩展 `mo2-mcp` 支持 SPT 安装结构
- 实现 MO2 overlay 写入
- 实现静态验证（依赖、结构、许可证检查）
- 验证：端到端合并 3-5 个正交 MOD

### Phase 3：功能域配置与自动化（4-6 周）

- 定义初始功能域（经济、战斗、AI、进度、商人等）
- 实现 `experience.json` → `mods-lock.json` + `merge-manifest.json` 转换
- 实现 MOD 版本锁定与更新检测
- 扩展 `evaluating-spt-mods`、`curating-spt-modpack`
- 验证：从功能域配置到 MO2 overlay 的完整流程

### Phase 4：测试与诊断能力（4-6 周）

- 实现 `testing-spt-modpack`
  - 静态验证
  - SPT server 启动冒烟测试
  - 日志错误检测
- 实现 `diagnosing-spt-problems`
- 完善 dev-log / release-changelog 工作流

### Phase 5：生态完善（远期）

- 扩展 MOD 覆盖范围
- 建立社区共享的 feature-map
- 可选：Unity bundle 处理工具
- 可选：SPT locale JSON 处理工具

---

## 十一、可行性结论

### 直接回答

**“将 bgs-modding-superpowers 改造为 SPT 4.0+ 整合包自动化构建工具包是否现实？”**

**现实，但有明确的边界，且强合并是核心假设。**

- 现有项目的**基础设施**（harness、Skill、MCP、KB、MO2 控制平面）可以复用，约减少 30-40% 的工程量。
- **领域层必须重建**：xEdit 替换为 IL/源码分析器，BGS 冲突模型替换为 SPT 冲突模型，知识库全部重写。
- **源码级强合并是核心目标**。工具包默认尝试将多个 MOD 合并为合成 MOD；无法安全合并时必须停止并请求人工裁决，不能静默降级。
- **MO2 保留为必需管理器**简化了运行时架构，但要求所有构建输出必须符合 MO2 overlay 规范。
- **自动化上限**：约 50-65% 的端到端集成工作可自动化，剩余 35-50% 涉及许可证审查、运行时语义验证、强合并失败时的人工裁决。强合并的可行性高度依赖 MOD 的源码质量和许可证兼容性。

### 推荐路径

采用**分阶段混合路线**：

1. 先完成 Phase 0-1，建立 SPT 分析型工具包。
2. 用真实 MOD 验证 IL 分析对 4.0+ 服务端和客户端 MOD 的覆盖率。
3. 再进入 Phase 2，实现最小可用的源码合并 + MO2 overlay 输出。
4. 最后扩展到功能域配置和自动化测试。

### 下一步行动

1. 用户审阅本设计报告。
2. 确认是否进入 Phase 0（基础数据建设）。
3. 若进入 Phase 0，首先建立 `knowledge/spt-kb/` 骨架和 MOD 清单模板。

---

## 十二、附录

### A. 参考资源

| 资源 | 链接 |
|------|------|
| SPT 服务器源码（C#） | https://github.com/sp-tarkov/server-csharp |
| SPT 服务端 MOD 示例 | https://github.com/sp-tarkov/server-mod-examples |
| SPT 客户端 MOD 示例 | https://github.com/Jehree/SPTClientModExamples |
| SPT MOD 下载站 | https://forge.sp-tarkov.com/mods |
| SPT Wiki | https://wiki.sp-tarkov.com |
| Mono.Cecil | https://github.com/jbevain/cecil |
| ICSharpCode.Decompiler | https://github.com/icsharpcode/ILSpy |
| Harmony | https://github.com/pardeike/Harmony |

### B. 术语表

| 术语 | 说明 |
|------|------|
| SPT | Single Player Tarkov，离线塔科夫 |
| MO2 | Mod Organizer 2，MOD 管理器 |
| USVFS | MO2 的虚拟文件系统 |
| BepInEx | Unity 插件加载框架 |
| Harmony | 运行时方法补丁框架 |
| IL | Intermediate Language，.NET 中间语言 |
| DI | Dependency Injection，依赖注入 |
| overlay | MO2 的 mod 目录，运行时被投影到游戏目录 |

---

> **Vault-Tec 免责声明**
> 本报告基于 2026-06-25 的公开技术信息。SPT 4.0+ 仍在活跃开发中，API 和 MOD 规范可能变化。
> Vault-Tec 不对因 MOD 合并导致的许可证纠纷、存档损坏或 overseer 职业倦怠承担责任。
> 毕竟 — Preparing for the Future!

--- END OF DESIGN REPORT ---
