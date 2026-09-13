# tarkov-runtime-MCP — Spec

Status: ready-for-agent

## Problem Statement

本项目已具备离线分析面（spt-MCP：mod 清单、冲突分析、KB 查询、MO2 状态），但 modpack 测试自动化（`testing-spt-modpack`）缺少运行时手段：SPT server 启动后，agent 无法以结构化、可断言的方式读取游戏状态（server 版本、已加载 mod、profile/商人/任务/藏身处状态），只能靠人工看日志和启动器界面。目标版本线为 SPT 5.x（bleeding-edge），且 5.x 的 server mod 扩展接口未完工、预期大改，当前唯一稳定的 mod 接口在 BepInEx 客户端侧。

## Solution

新建独立的 **tarkov-runtime-MCP**（不并入 spt-MCP，ADR-0001），只适配 SPT 5.x（ADR-0002），采用混合桥策略（ADR-0003）：

- 首版**零桥**：不编写任何游戏侧组件，MCP 直接编排 SPT server 现有 `/client/*` 路由，读取局外状态。
- MCP 连接握手时经 `/singleplayer/settings/version` 零配置发现单实例并执行版本门禁；传输层处理 zlib 压缩、`PHPSESSID` cookie、5.0 的 shuffle 加密。
- 工具面为测试自动化造型：语义化快照 + `wait_for` 谓词原语。
- `raid.*` 命名空间预留，首版全部返回 `CLIENT_BRIDGE_NOT_INSTALLED`；Phase 2 由 BepInEx Client Bridge（MO2 overlay 交付）实现。

## User Stories

1. As a modpack 维护者, I want agent 能探测本机正在运行的 SPT server 实例, so that 测试无需手工配置地址端口
2. As a modpack 维护者, I want MCP 在连接时校验 SPT 版本与锚定 tag 一致, so that 断言结果建立在可复现的目标版本上
3. As a modpack 维护者, I want 版本不匹配时 MCP 明确拒绝并给出可读提示, so that 我不会在错误版本上得到看似通过的假断言
4. As a test 编写者, I want 读取 server 版本、启动时间、已加载 server mod 清单, so that 我能断言"每个 mod 都被 server 加载"（Level B 验证）
5. As a test 编写者, I want mod 清单数据标注来源（路由/日志兜底）, so that 我能判断断言的可信度
6. As a test 编写者, I want 一次调用拿到 profile 摘要快照（等级/技能/任务进度计数）, so that 断言是原子的而非多次往返拼合
7. As a test 编写者, I want 商人快照（好感/等级/assort 计数）, so that 我能验证商人类 mod 生效
8. As a test 编写者, I want 任务快照（可用/进行/完成计数）, so that 我能验证任务类 mod 生效
9. As a test 编写者, I want 藏身处快照（区域等级）, so that 我能验证藏身处相关 mod 生效
10. As a test 编写者, I want 快照 sections 可选, so that 我只拉取断言需要的数据
11. As a test 编写者, I want `wait_for` 谓词 + 超时原语（如 "server_status.mods contains X"）, so that 测试能与 server 启动/加载过程同步而不靠 sleep
12. As a test 编写者, I want `wait_for` 超时返回结构化失败（谓词、已观察到的最后值、耗时）, so that 失败原因可直接进测试报告
13. As a modpack 维护者, I want 局外状态读取只需 SPT server 运行、不需启动游戏客户端, so that 大部分验证保持轻量
14. As a 未来开发者, I want 调用 `raid.*` 工具得到明确的 `CLIENT_BRIDGE_NOT_INSTALLED`, so that 首版边界清晰、Phase 2 接入点已固定
15. As a 维护者, I want 5.0 shuffle 加密在 MCP 传输层透明处理, so that 工具层与测试不感知协议细节
16. As a 维护者, I want 快照输出确定性、可 diff（字段排序稳定、无时间戳噪声或可开关）, so that 快照可直接用于金样本断言
17. As a 维护者, I want 新 MCP 复用仓库现有 MCP 的工具注册与测试模式, so that 维护心智成本不随 MCP 数量增长

## Implementation Decisions

- **组件与位置**：本仓库 `tools/` 下新增一个组件 `tarkov-runtime-mcp`（TypeScript + vitest，复用 spt-mcp/mo2-mcp 的工具注册、类型与测试惯例）。Phase 2 才创建 `tarkov-runtime-bridge`（C# BepInEx 插件），本 spec 不含其实现。
- **内部模块划分**（模块边界即接缝，见 Testing Decisions）：
  - 传输层：HTTP 客户端，负责 zlib 压缩/解压、`PHPSESSID` cookie 会话、5.0 shuffle 加解密（实现依据 `knowledge/spt-kb/curated/api-notes-5.0/` 与 5.0 源码 `RequestEncryptionUtil`），统一错误映射。
  - 连接/握手：候选端点探测（默认 6969，可配置覆盖），`/singleplayer/settings/version` 解析 SPT 版本号，版本门禁（锚定具体 BEM tag，暂定 `5.0.0-BEM-20260910`；开工时若已有更新 BEM tag 则重新评估并在 PR 说明）。不匹配即拒绝，报文含期望/实际版本。单实例模型：无多实例寻址机制。
  - 路由客户端：`/client/*` 路由的薄调用层（profile、traders、quests、hideout 等），不做业务归一。
  - 快照组装器：把路由响应归一为确定性快照 schema（字段排序稳定、计数型摘要而非全量枚举、标注数据来源与数据新鲜度）。
  - 日志解析器：解析 SPT server 日志提取已加载 server mod 清单（`server_status.mods` 的兜底来源，schema 中标注 `source: "server-log"`）。
  - 工具层：`tarkov_instances`、`tarkov_server_status`、`tarkov_snapshot`、`tarkov_wait_for`，以及 `raid.*` 占位。
- **版本门禁在 MCP 而非桥**（ADR-0003 后果）：首版无游戏侧组件，门禁于握手时执行。
- **能力自报**：`tarkov_server_status` 返回 MCP 自身能力清单（实现的 sections、bridge 连接状态），吸收 5.0 版本线内部漂移。
- **错误模型**：结构化错误码（`VERSION_MISMATCH`、`SERVER_UNREACHABLE`、`UNSUPPORTED_SECTION`、`CLIENT_BRIDGE_NOT_INSTALLED`、`WAIT_TIMEOUT`），机器可读 + 人类可读双字段。
- **写操作**：首版纯只读。
- **集成**：作为新 MCP server 注册进 OpenCode 配置（与 spt/mo2 MCP 并列），命名 `tarkov`。

## Testing Decisions

- 好测试的标准：只测外部行为（工具输出 schema、错误码、门禁拒绝），不测内部实现；快照断言用确定性 fixture。
- 接缝布局（已与 Overseer 确认，全程只开一个新接缝）：
  - **S1（唯一新接缝）**：传输层抽象（`SptConnection` 形态）。多数测试以 fake connection + 录制/手写 fixture 驱动工具层；少量传输层单测覆盖 shuffle 加解密往返、zlib/cookie 处理、版本门禁拒绝路径。
  - **S2（复用现有模式）**：文件系统——日志解析器以 fixture 日志文件测试。
  - **S3（最高接缝）**：MCP 工具边界——主力测试面，per-tool 测试。
- 受测模块：工具层（全部工具）、传输层（协议细节）、快照组装器（归一正确性、确定性）、日志解析器、版本门禁。
- Prior art：`tools/spt-mcp/tests/unit/*`（fixture 驱动读接口测试）、`tools/mo2-mcp/tests/tools/*`（per-tool 测试模式）。
- 端到端冒烟（真实 5.0 server 一次握手 + 一次快照）留作实现末期人工/半自动验证，不进 CI。

## Out of Scope

- SPT 3.11 / 4.x 适配（ADR-0002）
- 局内（in-raid）状态与 BepInEx Client Bridge 实现（Phase 2；本 spec 仅含 `raid.*` 占位行为）
- SPT server mod / Server Bridge（待 5.x mod 接口稳定后再评估）
- 多实例自动发现与寻址（单实例模型）
- 写操作、历史/时序查询、全量物品流
- MO2 overlay 打包管线（Phase 2 随 Client Bridge 一并验证 A-1 假设）

## Further Notes

- 决策记录：`docs/adr/0001-separate-tarkov-runtime-mcp.md`、`0002-spt5-only-scope.md`、`0003-hybrid-zero-bridge-strategy.md`；术语：`CONTEXT.md`。
- 可行性研究：`docs/research/spt-runtime-state-export.md`；跨版本事实调查（3.11/4.1/5.0 机制差异、版本自报端点、shuffle 加密位置）见本会话背景调查报告，关键结论已并入 ADR。
- 遗留假设：A-1（MO2 VFS 投影 server mod 可靠性）推迟至 Phase 2 验证；A-3（BEM tag 锚定点）开工时复核。
