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
一个 SPT 大版本系列。tarkov-runtime-MCP 只适配 SPT 5.x 版本线（见 ADR-0002），3.11 与 4.x 材料仅作参考。
_Avoid_: 版本（过于宽泛）、分支

**Moving Target（移动靶）**:
SPT 5.0 当前处于 bleeding-edge 开发阶段（`5.0x-dev` 分支），API 与内容随时可变。版本线内的漂移靠 capability 自报与版本门禁吸收。
_Avoid_: 不稳定版本
