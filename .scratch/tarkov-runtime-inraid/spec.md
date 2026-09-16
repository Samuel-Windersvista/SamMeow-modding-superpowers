# tarkov-runtime-MCP Phase 2 — 局内状态（In-Raid State）Spec

> **进展（Work Status）**: 主体已交付并 live 验证（T01 首刀 2026-09-14 + 二波 2026-09-15）；残余收尾项见 dev-log

Status: ready-for-agent

## Problem Statement

modpack 测试与 mod 验证目前只有局外状态读取（Phase 1）：SPT server 启动后，agent 可以结构化读取 profile / 商人 / 任务 / 藏身处等持久化状态。但 **raid 进行中**的玩家与 bot 实时状态（位置、血量、姿态、存活、生成情况）只存在于游戏客户端进程内存中——server 不掌握、日志在 raid 中不更新、外部进程无从读取。后果：

- `testing-spt-modpack` 的 Level C（raid 冒烟）只能人工肉眼，无法自动断言；
- bot 类 mod（SAIN / LootingBots / QuestingBots 等）的开发验证缺少运行时观测面，只能靠看画面；
- `tarkov-runtime-mcp` 的 `raid.*` 命名空间自 Phase 1 起是占位，调用返回 `CLIENT_BRIDGE_NOT_INSTALLED`。

## Solution

为 `tarkov-runtime-mcp` 补上局内状态域（Phase 2），采用已定的混合桥策略（ADR-0003）：

- 新建 **BepInEx 6 客户端桥**（`tarkov-runtime-bridge`）：游戏进程内采样局内状态，经 **127.0.0.1 HTTP 服务**暴露；纯只读；经 **MO2 overlay** 交付（不写游戏安装目录）。
- `tarkov-runtime-mcp` 新增 **Bridge 客户端 + `raid.*` 工具**：`raid_status` / `raid_player` / `raid_bots`，并扩展 `tarkov_wait_for` 支持 raid 谓词；按需拉取（pull）。
- 传输决策见 **ADR-0007**（插件内嵌 HTTP + 拉取；MCP 侧留可替换接缝）。
- 首刀为垂直切片：一次打穿 usvfs 客户端投送、IL2CPP 读数、HttpListener、MCP 联通四个高风险假设。

## User Stories

1. As a modpack 维护者, I want agent 在 raid 进行中读取玩家状态（位置/朝向/姿态/血量/存活）, so that raid 冒烟测试可以自动断言而不是肉眼
2. As a bot mod 开发者, I want 读取 bot 摘要（总数 + PMC/Scav/Boss 分类计数）, so that 我能快速断言 bot 类 mod 的生成效果
3. As a bot mod 开发者, I want 按需拉取 bot 明细（每个 bot 的位置/类型/存活）, so that 我能定位异常生成与分布问题
4. As a test 编写者, I want raid 元数据（地图名/raid 状态/剩余时间/raidId）, so that 断言能锚定到具体一场 raid
5. As a test 编写者, I want 每条响应携带数据新鲜度（`sample_age_ms`）, so that 我能判断断言依据的时效性
6. As a test 编写者, I want `tarkov_wait_for` 支持 raid 谓词（如 `raid.bots.alive_count > 5`）, so that 测试能与 raid 进程同步而不靠 sleep
7. As a test 编写者, I want wait_for 超时返回结构化失败（谓词/最后观测值/耗时）, so that 失败原因可直接进测试报告
8. As a test 编写者, I want bridge 未安装时 `raid.*` 返回明确错误码, so that 我能区分「没装桥」与「数据异常」
9. As a modpack 维护者, I want 游戏未运行 / 不在 raid 时的结构化错误码, so that 排障路径清晰
10. As a modpack 维护者, I want 桥经 MO2 overlay 交付并可回滚, so that 不违反「不写游戏目录」硬规则
11. As a modpack 维护者, I want 桥加载可在 BepInEx 日志中断言, so that Level B 验证覆盖桥自身
12. As a 维护者, I want 桥自报版本/协议版本/能力清单, so that 版本不匹配可诊断而不是玄学失败
13. As a 维护者, I want 采样间隔可配置（默认 1s）, so that 性能与新鲜度可以权衡
14. As a 维护者, I want 端口可配置且冲突可诊断, so that 与其他本机服务共存
15. As a 维护者, I want 桥仅绑定 127.0.0.1 且代码路径纯只读, so that 安全边界清晰（无外部暴露、无写游戏状态）
16. As a 测试者, I want MCP 侧可录制真实 raid 的响应流为 JSONL, so that 回归测试可以回放
17. As a 测试者, I want 回放由 fixture 驱动 fake 连接, so that 回归不依赖真实游戏在跑
18. As a 未来开发者, I want 传输在 MCP 侧是可替换接缝, so that 换 WebSocket/命名管道不需要重写工具层
19. As a 未来开发者, I want 事件流（击杀/受伤/撤离）预留接入点, so that 第二波功能有明确位置
20. As a 维护者, I want 桥源码遵循 Modding Standard（或记录豁免）, so that 质量门槛与仓库一致
21. As a modpack 维护者, I want 单实例模型下 MCP 用默认端口 + 可覆盖配置直连, so that 无需寻址机制
22. As a 维护者, I want 桥的状态模型与 MCP 输出 schema 解耦（采样模型 → 快照 schema）, so that 游戏内数据结构变化不直接击穿工具面
23. As a test 编写者, I want `raid_status` 同时报告桥自身状态（安装/连接/版本/采样健康）, so that 一次调用可诊断「桥活着吗」

## Implementation Decisions

1. **组件**
   - 新组件 `tarkov-runtime-bridge`（C# / BepInEx 6 IL2CPP 插件；以 Modding Standard 客户端 mod 形态组织）。
   - `tarkov-runtime-mcp` 增量：bridge 客户端模块 + `raid.*` 工具实现（替换 Phase 1 占位）+ `tarkov_wait_for` 谓词扩展 + 录制/回放。
2. **传输与拓扑（ADR-0007）**
   - 插件内嵌 HTTP 服务，绑定 `127.0.0.1`，默认端口 `49777`（可配）；MCP 主动拉取（pull）。
   - MCP 侧以 `BridgeConnection` 抽象封装传输；工具层只依赖该抽象。
   - 端点契约：`/bridge/info`（版本/协议版本/能力/采样状态）、`/raid/status`、`/raid/player`、`/raid/bots`（`detail` 参数）。
3. **采样模型**
   - 主线程采样循环：默认 1s（可配 0.25–5s），状态写入线程安全缓冲；HTTP 线程只读缓冲，不直接访问游戏对象。
   - 响应携带 `sample_age_ms`（数据新鲜度）。
   - 空闲零计算：无人拉取时仅采样循环的固定小开销。
4. **状态模型（第一波字段）**
   - raid 元数据：地图名、raid 状态（装载/进行/结束）、剩余时间、raidId。
   - player：位置、朝向、姿态（站/蹲/趴）、血量（总 + 各肢体）、存活。
   - bots：默认摘要（总数 + PMC/Scav/Boss 分类计数）；`detail` 时返回明细（每个 bot 的位置/类型/存活）。
   - 输出 schema 确定性（字段排序稳定、可 diff）。
5. **工具面（纯只读）**
   - `raid_status`：raid 元数据 + 桥自报（安装/连接/版本/采样健康）。
   - `raid_player`：玩家状态。
   - `raid_bots`：bot 摘要 / 明细。
   - `tarkov_wait_for`：谓词空间扩展至 raid 状态（不新增 `raid_wait_for` 工具）。
   - bridge 缺席时保留 `CLIENT_BRIDGE_NOT_INSTALLED` 语义。
6. **错误模型**
   - 新增错误码（机器可读 + 人类可读双字段）：`CLIENT_BRIDGE_NOT_INSTALLED` / `BRIDGE_UNREACHABLE` / `NOT_IN_RAID` / `BRIDGE_VERSION_MISMATCH` / `UNSUPPORTED_SECTION`。
7. **版本门禁与能力自报**
   - 桥自报：插件版本、协议版本、能力清单（已实现端点/sections）、采样配置。
   - MCP 握手校验协议版本；版本线锚定 SPT 5.0（EFT 1.1.5）。
8. **交付与安装（MO2 overlay）**
   - mod 根 = 游戏根；桥以 `BepInEx/plugins/<assembly>.dll` 形式投影；MO2 命名 `<category>-<mod-name>-<version>`（如 `工具-tarkov-runtime-client-bridge-0.1.0`），meta.ini 含 comments/notes。
   - 不写游戏安装目录（硬规则 1）。
9. **录制与回放**
   - MCP 侧 JSONL：时间戳 + 原始响应；默认关（env/配置开启）；live 验收时开；录制样本进 MCP 测试 fixtures 供回放。
10. **参考源与核验**
    - 参考：`SP-Tushonka/modules@5.0x-dev`（本地 clone，tip `b5513e6`）、TechHappy Web Minimap（导出与 bot 枚举模式）、bepinex-mcp（HttpListener 模式）、`knowledge/spt-kb/archive/eft-1.1.5/`（类名清单；成员签名以本地 interop 核验为准）。
    - 首刀即核验：usvfs 客户端投送（Launcher→游戏链）、IL2CPP 成员签名（position/health/pose/timer/bots）。
11. **首刀（tracer bullet）**
    - 垂直切片：插件骨架 + 玩家位置单字段 + HTTP 单端点 + MCP `raid_player` 最小版 + MO2 overlay 交付 + live 验收。
12. **工单形态**
    - 8 票：T00 侦查核验 / T01 首刀 / T02 采样与错误模型加固 / T03 玩家+raid 全字段 / T04 bot 域 / T05 wait_for 扩展 / T06 录制回放 / T07 文档与规范收尾（见 to-tickets 产物）。

## Testing Decisions

- 好测试的标准：只测外部行为（工具输出 schema、错误码、谓词语义、回放确定性），不测内部实现；fixture 驱动、无时间戳噪声。
- 接缝布局：
  - **复用 S1/S3**（Phase 1 已有）：传输抽象 + MCP 工具边界为主力测试面。
  - **唯一新接缝**：MCP 侧 `BridgeConnection` 抽象——工具层测试以 fake bridge 驱动；录制 JSONL 以 fake connection 回放。
  - 桥侧（C#）：纯逻辑（路由、序列化、缓冲）普通单测；游戏耦合部分（Harmony/GameWorld 读取）以 live 验收为准，不做跨进程假接口。
- 受测模块：`raid_status` / `raid_player` / `raid_bots` / `tarkov_wait_for`（扩展谓词）/ `BridgeConnection` / 录制回放器。
- Prior art：`tools/tarkov-runtime-mcp/tests/tools/*`（per-tool 测试）、`tests/helpers/fake-connection.ts`（fake 连接模式）、`tests/snapshot/*`（fixture 驱动）。
- Live 验收：Level B（MO2 启动 → BepInEx 日志加载行 → 握手成功）；Level C（进 raid → 三工具真实数据人工核对 → 至少一段录制进回归）。
- 验收门槛（已确认）：MCP 测试全绿 + live 验收 + 一段真实 raid 录制 fixture + 文档/Modding Standard 合规 + 只读/仅 127.0.0.1/默认关录制。

## Out of Scope

- 写操作/控制（传送、生成、改状态）
- 装备/武器状态、bot 行为态深入（SAIN 等 mod 内部状态）→ 第二波
- 事件流（击杀/受伤/撤离/枪声）→ 第二波（本 spec 仅预留扩展点）
- 战利品域（地面/容器）、局内库存
- SPT 4.x / 3.11 适配（ADR-0002）
- 非 raid 客户端状态（藏身处/菜单实时）
- 多实例发现与寻址（单实例模型）
- WebSocket/命名管道/sidecar 传输（ADR-0007 保留替换路径）

## Further Notes

- 决策记录：ADR-0007（传输选型）；关联 ADR-0001（独立 MCP）、ADR-0002（5.x only）、ADR-0003（混合桥）、ADR-0006（姿态）。
- 研究：生态先例调研（Web Minimap / bepinex-mcp / SPT-RPC / TarkovMonitor 边界）、MO2 客户端投送链路侦察（`game_spt5.py` 映射规则）、EFT 1.1.5 类名映射重建报告。
- 遗留假设：usvfs 对 Launcher→游戏子进程链的客户端投送（T01 实测）；IL2CPP 成员签名（T00 核验）。
- 术语：CONTEXT.md（In-Raid State / Client Bridge / Snapshot / Bridge）。
- 参考源登记：`SP-Tushonka/modules@5.0x-dev`（本地 `SamMeow_SP-Tushonka_modules_source_code`，tip `b5513e6`）。
- 环境基线：SPT 5.0 `BEM-20260914`（2026-09-14 起；客户端 modules 与 0910 同 commit，EFT `1.1.5.0.47242` 未变；server 侧 0910→0914 改动不触及本 spec 依赖的接口）。
