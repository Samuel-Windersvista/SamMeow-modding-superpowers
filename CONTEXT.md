# SamMeow Modding Superpowers

SPT（Single Player Tarkov）mod 开发与 modpack 策展的 agent 工具包：知识库、mod 模板、冲突分析、MO2 集成构建管线，以及（设计中）运行时游戏状态读取 MCP。

## Language

### 组件

**spt-MCP**:
现有离线分析 MCP。只读文件系统与知识库：mod 清单、冲突分析、Forge 归档搜索、MO2 状态。不要求 SPT/游戏进程运行。
_Avoid_: SPT 工具、mod 分析器

**tarkov-runtime-MCP**:
设计中的运行时状态读取 MCP。SPT server / 游戏进程存活时可用，通过 Bridge 读取实时游戏状态。独立于 spt-MCP（见 ADR-0001）。
_Avoid_: 运行时 MCP、live MCP、tarkov-MCP

**Bridge**:
安装进游戏侧、为 tarkov-runtime-MCP 暴露状态的组件。按 ADR-0003 采用混合策略：局外状态零桥（MCP 直连 `/client/*` 路由），局内状态用 Client Bridge（BepInEx 插件，Phase 2，MO2 overlay 交付）。首版无任何游戏侧组件。
_Avoid_: 插件、适配器（adapter 指别的东西）、shim

### 状态域

**Out-of-Raid State（局外状态）**:
SPT server 掌握的持久化状态：存档 profile、库存、商人、任务、藏身处。经 Server Bridge 读取。首版唯一实现的状态域。
_Avoid_: 离线状态、菜单状态

**In-Raid State（局内状态）**:
raid 进行中仅存在于客户端进程的状态：玩家位置、血量、AI。需 Client Bridge（BepInEx + Harmony + IPC）。架构预留 `raid.*` 工具命名空间，首版不实现。
_Avoid_: 实时状态（局外状态也是实时的，此词有歧义）

**Snapshot（快照）**:
某一时刻游戏状态的结构化、确定性、可 diff 的读取结果。modpack 测试自动化的基本断言单位。
_Avoid_: dump、export

### 版本

**Version Line（版本线）**:
一个 SPT 大版本系列。当前两条活跃线：4.1.x（稳定开发基线，Modding Standard 的 Dev-Baseline）与 5.x（新主线，已发布）；3.11 为历史对照。tarkov-runtime-MCP 锚定 5.x 线（见 ADR-0002；5.0 发布后该范围决策不变）。
_Avoid_: 版本（过于宽泛）、分支

**Moving Target（移动靶）**:
SPT 5.0 已正式发布（2026-09-14 确认；此前 `5.0x-dev`/BEM 阶段 API 漂移剧烈），5.x 线内仍可能存在 API 与内容漂移。版本线内的漂移靠 capability 自报与版本门禁吸收。
_Avoid_: 不稳定版本

### 规范

**Modding Standard（mod 编写规范）**:
从语料与官方机制归纳的、分级的 mod 编写规则集。定位是"工程化默认值"：模板按规范生成、技能引用规则 ID；文档是载体而非本体。
_Avoid_: 风格指南、best practices 清单

**Rule ID**:
规范的稳定引用标识，格式 `STD-<DOMAIN>-<nnn>`（如 `STD-STRUCT-001`）。规则分 MUST / SHOULD / MAY 三级。
_Avoid_: 编号、条目号

**Evidence Tier（证据分级）**:
规则证据的强度层级：机制证据（源码/接口）> 语料证据（统计）> 经验证据。MUST 需机制+语料双源；无语料先例的规则须显式标注。
_Avoid_: 来源、引用

**Waiver（豁免）**:
对 MUST 规则的有记录偏离：在 mod README 或项目 dev-log 写明理由与替代方案。
_Avoid_: 例外、特批
