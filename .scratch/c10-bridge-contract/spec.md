# C10 · Bridge 跨语言契约工件化 — 实施规格

> 来源：架构审查候选 C10 + grilling 确认（2026-09-17）。报告：`D:\Temp\architecture-review-20260916-1337.html` §2 C10 卡（组 B · 代码接缝 · P2）。
> 侦察：`exp-2` 桥接双端触点清点（2026-09-17）。
> **进展（Work Status）**: CLOSED — 2026-09-17（全流程完成：车道 A/B + 双轴评审 1 BLOCKER + 修复轮 F1-F6 + 编排者终验 bootstrap 12/12；未提交）

## 决策记录（grilling 确认）

| # | 决策 | 结论 |
|---|------|------|
| D1 | 锁定机制 | **单一源契约工件**：`shared/bridge-contract/contract.json`（协议版本 + 日志归一化规则表 + 聚合常量）；两端消费同一数据；日志规则引擎薄化为读表执行；行为由共享 golden 夹具锁定 |
| D2 | 位置与载体 | `shared/bridge-contract/`（仓库根 `shared/` 既有惯例）；C# 经 `EmbeddedResource` 编译期嵌入 DLL；TS 经运行时读取（路径在仓库布局与便携树布局下均成立） |
| D3 | 夹具内容 | 三件套：①日志归一化向量 ②日志聚合向量 ③payload 样例（含错误形状）——两端测试消费同一夹具文件 |
| D4 | 协议版本策略 | 语义保持（整数 / 握手门禁 / mismatch → `BRIDGE_VERSION_MISMATCH`，`details:{expected,actual}`）；bump = 仅 wire 不兼容变更时 +1；**新增 `docs/adr/0009-bridge-cross-language-contract.md`** 固化契约工件与双轴版本语义（组件版本 ≠ wire 协议版本） |
| D5 | 门禁接线 | 两端测试套件消费共享夹具（TS `npm test` + C# `dotnet test`）+ **bootstrap 第 12 项 `verify-bridge-contract`**（轻检查：工件与夹具可解析、消费点完好、便携接线在列） |
| D6 | 变更原则 | **零 wire 行为变更**：pre/post golden 对照证明行为不变；不做协议 v2、不做传输/端点改动、不做 payload 全量 codegen（记入记录项） |

## 事实基线（exp-2 侦察实测）

**双语言重复面（16 项）**：协议版本常量（`tools/tarkov-runtime-bridge/src/Plugin.cs:42` `ProtocolVersion = 1` ↔ `tools/tarkov-runtime-mcp/src/bridge/connection.ts:18` `EXPECTED_BRIDGE_PROTOCOL_VERSION = 1`）；7 端点 payload 字段与顺序（C# `src/BridgePayloads.cs` ↔ TS `src/bridge/connection.ts:44-325`）；路由表（`src/BridgeRouter.cs:44-50` ↔ `src/bridge/http-bridge-connection.ts` 各方法）；404/405/500 错误形状；日志归一化 5 规则（`src/LogSummaryNormalizer.cs:28-68` ↔ `src/logs/log-normalizer.ts:27-56`）；聚合语义（`src/LogSummaryStore.cs:77-244` 500 组上限/overflowDropped/组序 ↔ `src/logs/log-aggregator.ts:18-168`）；日志级别排名（`src/LogWatchLevel.cs` ↔ `src/logs/log-level.ts`）；`detail`/`since`/`limit` 参数语义；时间戳格式。

**关键坐标**：TS 版本常量使用点 `src/tools/raid-common.ts:14,38,72` + `src/tools/logs-summary.ts:30,106`；C# 使用点 `src/Plugin.cs:94` + `src/HttpBridgeServer.cs:243-247` + `src/BridgePayloads.cs:22`。TS 端 `bridge/` = connection / http-bridge-connection / recording / replay；C# 端源文件 20 个（Server/Router/Payloads/Stores/Normalizer/Collectors/Patches）。

**测试现状**：TS——`tests/bridge/http-bridge-connection.test.ts`（node:http stub）、`tests/bridge/recording-replay.test.ts`（JSONL）、`tests/logs/log-normalizer.test.ts`、`tests/logs/log-aggregator.test.ts`、`tests/helpers/fake-bridge.ts`、`tests/fixtures/live-raid/raid-sandbox-2026-09-14.jsonl`（基线 395 测试）。C#——`tools/tarkov-runtime-bridge/tests/TarkovRuntimeBridge.Tests/`（xunit；`BridgePayloadsTests.cs` 硬编码 golden JSON、`BridgeRouterTests.cs`、`LogSummaryNormalizerTests.cs` 等）。**无任何共享文件**。

**构建/环境**：dotnet SDK 10.0.300 可用；bridge csproj + `Directory.Build.props`；DLL → `BepInEx/plugins/TarkovRuntimeBridge.dll`（MO2 overlay）。C# 消费共享 JSON 的可行路径 = `EmbeddedResource`。

**上下文**：ADR-0007（in-raid bridge transport：127.0.0.1、默认端口 49777、主线程采样/HTTP 线程只读、MCP 按需拉取）；`ProtocolVersion` 自引入以来保持 1，无 bump 历史；协议版本与组件版本（C5 统一为 0.2.0）已分离。

## 目标设计

### 1) 契约工件 `shared/bridge-contract/contract.json`

```jsonc
{
  "protocolVersion": 1,
  "logNormalization": {
    "rules": [
      { "id": "guid",    "pattern": "…", "replacement": "<guid>" },
      { "id": "id24hex", "pattern": "…", "replacement": "<id>" },
      { "id": "hex",     "pattern": "…", "replacement": "<hex>" },
      { "id": "number",  "pattern": "…", "replacement": "<n>" }
    ],
    "collapseWhitespace": true,
    "trim": true
  },
  "logAggregation": { "maxGroups": 500 }
}
```

- 规则集从现状两端提取（语义等价；**限定 JS/.NET 共通 regex 子集**）；规则顺序即应用顺序；引擎 = 顺序应用替换 → 折叠空白 → trim。
- （修复轮 F1 修订）`collapseWhitespace` 布尔升级为 `whitespacePattern`（显式字符类 = JS `\s` 25 码点集合）；折叠与 trim 均用该集合；契约 pattern 内不再出现 `\s/\d/\w`（消除引擎语义依赖）。
- 若某条规则无法通约 → 该条降级为"双实现 + 向量锁定"并在 spec/ADR 记录（预期不发生）。
- `protocolVersion` 为唯一 wire 协议版本源；**不得**在两端源码另留字面量（消费点改为从契约派生）。

### 2) 两端消费

- **C#**：`TarkovRuntimeBridge.csproj` 加 `<EmbeddedResource Include="…\shared\bridge-contract\contract.json" LogicalName="TarkovRuntimeBridge.bridge-contract.json" />`；新增 `src/BridgeContract.cs`（启动/静态初始化时解析，暴露 `ProtocolVersion` / `NormalizationRules` / `MaxGroups`）；`Plugin.ProtocolVersion`、`LogSummaryNormalizer`（规则引擎化）、`LogSummaryStore`（maxGroups）改为读契约；加载失败 = 响亮报错。
- **TS**：新增 `src/bridge/contract.ts`（运行时读取 + 解析 + 校验 + 缓存；路径解析在仓库布局与便携树布局下均成立，失败响亮报错）；`connection.ts` 的 `EXPECTED_BRIDGE_PROTOCOL_VERSION` 改为契约派生（保持导出名，消费点不变）；`log-normalizer.ts` 规则引擎化读契约；`log-aggregator.ts` maxGroups 读契约。
- **便携树**：`scripts/build-portable-plugin.ps1` 复制 `shared/bridge-contract/` + `$requiredPortablePaths` 加 `shared/bridge-contract/contract.json`。

### 3) 共享 golden 夹具 `shared/bridge-contract/fixtures/`

- `log-normalization.json`：`{ "cases": [ { "name", "input", "expected" } ] }`（覆盖 GUID / 24hex / 0x / 数字 / 空白折叠 / 幂等 / 边界；含来自 live-raid 录制的真实样本）。
- `log-aggregation.json`：`{ "cases": [ { "name", "entries": [ { "ts", "level", "source", "text" } ], "expected": { "groups": [ … ] } } ] }`（覆盖组序 count↓/lastTs↓/key↑、500 上限、overflowDropped）。
- `fixtures/payloads/*.json`：每端点一份 canonical JSON 样例 + `errors.json`（404/405/500 形状）；C# 测试断言构造输出与样例一致（字段序）；TS 测试断言解析器接受样例。
- **消费**：两端测试读取**同一文件**（TS 经仓库相对路径；C# 经测试工程解析仓库相对路径或 CopyToOutputDirectory）。

### 4) pre/post 捕获（零行为变更证明）

- pre（重构前）：对固定语料（夹具输入 + live-raid 派生样本）捕获两端归一化/聚合输出 + C# payload 样例 → `.scratch/c10-bridge-contract/goldens/pre/`；记录两端测试基线数。
- post（重构后）：同语料再捕获 → `goldens/post/`；**逐字节对照**（差异逐条声明）；两端测试套件全绿。

### 5) ADR-0009（车道 B）

`docs/adr/0009-bridge-cross-language-contract.md`：契约工件决策、单一源范围、双轴版本语义（组件版本 vs wire 协议版本）、bump 策略、夹具/golden 机制、非目标（payload codegen 等）。格式对齐既有 ADR。

### 6) bootstrap 第 12 项（车道 B）

`tests/bootstrap/verify-bridge-contract.ps1`：① `contract.json` 与全部 fixture JSON 可解析；② 夹具 cases 非空；③ 消费点完好（csproj 含 EmbeddedResource include；TS 源引用契约模块；便携构建清单含 `shared/bridge-contract/contract.json`）。注册进 `verify-all.ps1`（插在 `verify-version-identity.ps1` 之后、`verify-doc-stats.ps1` 之前）。输出风格对齐既有脚本。

## 已声明行为 delta

1. wire 行为：**零变更**（pre/post golden + 双端套件证明）。
2. 新增工件：`shared/bridge-contract/`（contract + fixtures）+ `src/bridge/contract.ts`（TS）+ `src/BridgeContract.cs`（C#）+ ADR-0009 + bootstrap #12（11→12）。
3. 两端协议版本常量改为契约派生（值仍为 1）。
4. 便携树新增 `shared/bridge-contract/`。
5. 空白语义收敛（修复轮 F1，向 JS 对齐；TS 不变、C# 变化）：C# 折叠/trim 对 U+FEFF 边缘「不→是」、U+0085 边缘「是→不」；空白字符类升为契约数据（`whitespacePattern` = JS `\s` 25 码点集合，Node 全码点枚举实测 SET-EQUAL）。
6. ECMAScript 词类对齐（修复轮 F2）：`é`+24hex、`ß0x`、阿拉伯-印度数字边界等 C# 行为向 TS 对齐（旧分叉修复）；U+0130 残余记录于 ADR-0009（不修 pattern）。

## 车道任务

### 车道 A（fixer）：契约工件 + fixtures + pre/post 捕获 + 双端重构 + 测试改造
1. pre 捕获（两端现状输出 + 测试基线数）→ `goldens/pre/`。
2. `shared/bridge-contract/contract.json`（规则从现状提取、共通子集核对）+ `fixtures/` 三件套（期望值以 pre 捕获为准，双端交叉核对）。
3. C# 端：EmbeddedResource + `BridgeContract.cs` + 三处消费改造（Plugin/Normalizer/Store）。
4. TS 端：`contract.ts` + 三处消费改造（connection/normalizer/aggregator）。
5. 双端测试改造：消费共享夹具（新测试文件/改造既有）；C# 侧注意 fixture 路径解析。
6. post 捕获 + 对照 + 双端套件全绿；报告 pre/post 差异（期望：无）。

### 车道 B（fixer）：ADR + bootstrap #12 + 便携接线
1. `docs/adr/0009-bridge-cross-language-contract.md`。
2. `tests/bootstrap/verify-bridge-contract.ps1` + `verify-all.ps1` 注册（第 12 项）。
3. `scripts/build-portable-plugin.ps1`：复制 `shared/bridge-contract/` + `$requiredPortablePaths` 追加。
4. 验证：脚本语法/解析检查；#12 在车道 A 落地前可红（工件未生成）——只报告不越界。

### 车道 C（orchestrator）：终验 + review + 记录
- 全量矩阵 → oracle 双轴 code-review → 修复轮 → dev-log + spec CLOSED。

## 写作用域（并行互斥）

- **车道 A**：`shared/bridge-contract/**`、`tools/tarkov-runtime-bridge/**`（src/tests/csproj）、`tools/tarkov-runtime-mcp/**`（src/tests）、`.scratch/c10-bridge-contract/goldens/**`。
- **车道 B**：`docs/adr/0009-*.md`、`tests/bootstrap/verify-bridge-contract.ps1`（新）、`tests/bootstrap/verify-all.ps1`、`scripts/build-portable-plugin.ps1`。
- 交集为空。

## 验证矩阵

| # | 命令/动作 | 通过标准 | 车道 |
|---|-----------|----------|------|
| A1 | pre/post 捕获对照 | 两端逐字节一致（差异逐条声明） | A |
| A2 | `dotnet test tools/tarkov-runtime-bridge/tests/TarkovRuntimeBridge.Tests` | 全绿（≥ 基线数） | A |
| A3 | `npm --prefix tools/tarkov-runtime-mcp test` | 全绿（基线 395） | A |
| A4 | `dotnet build tools/tarkov-runtime-bridge -c Release` | exit 0（EmbeddedResource 生效） | A |
| A5 | 共享夹具双端消费 | 两端测试读同一夹具文件且全绿 | A |
| B1 | `tests/bootstrap/verify-bridge-contract.ps1` | exit 0（依赖 A 的工件；此前可红） | B→C |
| B2 | `verify-all.ps1` | **12/12** | C |
| C1 | 便携重建 + 树内契约可达 + 树内 tarkov 套件（如可行） | 全绿 | C |
| C2 | 回归：`verify-version-identity` / `validate-index` / `validate-mod-standard` | exit 0 | C |

## 修复轮补记（2026-09-17，oracle 双轴评审后；全部落地，终验通过）

| # | 项 | 处置 |
|---|----|------|
| F1 | **[BLOCKER]** `\s` ECMAScript 反向对齐（两端空白语义分叉 + C# 相对 pre-C10 变更；对抗向量分叉 5/18→8/18） | Option A：空白字符类升为契约数据 `whitespacePattern`（Node 全码点枚举 25 个，SET-EQUAL）；两端引擎编译契约 pattern；trim 同集合收敛；对抗向量（U+00A0/U+3000/U+202F/U+2009/U+2028/U+FEFF/U+0085 中段+边缘）入共享夹具；四处不实声明纠正 |
| F2 | ECMAScript 对齐的 C# 行为变更未声明 | 对齐向量入夹具（é/阿拉伯数字/ß0x）+ ADR-0009 记录 + delta 补记 |
| F3 | U+0130 残余 | ADR-0009 记录为已知残余（不修 pattern） |
| F4 | 校验语义不对称 | `BridgeContract.cs` 接受整值 `1.0`；`contract.ts` 拒绝空 `replacement`（双端对齐） |
| F5 | bootstrap #12 加固 | 补 `LogicalName` 匹配 + `Plugin.cs` 派生断言 |
| F6 | NIT ×3 | `BridgeContract.cs` 注释纠正；`LogSummaryStoreTests` 硬编码 500 → 契约值；ADR bump 流程补一句 |

验证（终验 fresh 复跑）：bootstrap **12/12**；C# **316**/0；TS **414**/0；post 捕获 5/5 + CS↔TS 2/2 MATCH；便携树重建 + 树内对拍（nbsp→space / nel preserved / bom trimmed）。

## 风险与逃生方案

- R1 正则跨引擎差异（JS/.NET）→ 共通子集 + 共享向量双向验证；不可通约则降级记录。
- R2 TS 运行时路径解析（dev vs 便携树）→ 布局镜像 + 响亮失败 + 矩阵 C1 覆盖。
- R3 C# 嵌入资源加载失败 → 启动响亮报错 + A2/A4 覆盖。
- R4 pre/post 偏差 → 字节级对照 + 逐条声明；预期零偏差。
- R5 双车道并行 → 写作用域互斥表；B 的 #12 绿灯依赖 A，由 C 终验。

## 红线

- 不 commit；**零 wire 行为变更**；协议版本值保持 1；不改端点/传输/ADR-0007 语义。
- 不动：`mods/**`、`templates/**`、`knowledge/spt-kb/archive/**`、`external/**`、`tests/mod-standard/fixtures/**`、`docs/global` 之外历史档案。
- 中文注释与回复；无 emoji。
