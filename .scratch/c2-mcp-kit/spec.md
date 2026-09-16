# C2 · MCP 共享内核（tools/mcp-kit）— 实施规格

> 来源：架构审查候选 C2 + grilling 确认（2026-09-16）。报告：`D:\Temp\architecture-review-20260916-1337.html` §2 C2 卡。
> **进展（Work Status）**: CLOSED — 2026-09-16（全流程完成：wave1/2 交付 + 终验 + oracle 双轴 review + 修复轮；未提交）

## 决策记录（grilling 确认）

| # | 决策 | 结论 | 依据 |
|---|------|------|------|
| D1 | kit 接线 | **相对 dist 导入**：三台 `import ... from "../../mcp-kit/dist/index.js"`；kit 构建出 dist + .d.ts；零 npm 链接 | 便携包沿用现有 `Copy-McpPackage` 机制（kit 为第四包）；`shared/runtime-layout.mjs` 相对导入先例（src/dist/便携同深度解析） |
| D2 | SDK 收敛 | mo2 `^0.6.0` → `^1.0.0`；kit 声明 `^1.0.0`（实装解析 1.30.0） | lib-1 研究：0.6↔1.0 所用 API 面（Server / StdioServerTransport / setRequestHandler / 请求 Schema）逐字节相同，升级零源码改动 |
| D3 | schema 管道 | canonical = `zodToJsonSchema(jsonSchema7)` → 去 `$schema` → `normalizeMcpInputSchema` | 单管道一处；mo2 从 openApi3 迁移；normalize 对无 union schema 为恒等 pass；golden 对比验证 |
| D4 | envelope | **spt + tarkov 统一**为 canonical；mo2 本轮保留其错误方言（另立可选小工单） | canonical err = `{message, hint?, details?}`（superset）；spt 错误字段 `summary`→`message`（机械改测试）；mo2 无第三份可去重 |

## 事实基线（侦察核实）

- spt / tarkov 入口双胞胎：`schemaFor`（jsonSchema7+去$schema）/ `jsonResult` / `invokedAsMain` 逐字相同。
  - `tools/spt-mcp/src/index.ts:48-52,116-121,174-189`；`tools/tarkov-runtime-mcp/src/index.ts:90-94,256-261,292-307`
- mo2 异类：无 `invokedAsMain`；SDK 0.6.1；schema 走 openApi3 + `normalizeMcpInputSchema`（C3 已抽为 `tools/mo2-mcp/src/schema-normalizer.ts`，323 行；测试 `tools/mo2-mcp/tests/normalize-mcp-input-schema.test.ts` 481 行）；dispatch 内联 `{ok:false,error:{code}}`（`tools/mo2-mcp/src/dispatch.ts:62,140`）。
- envelope 方言：spt err=`{summary,hint}`（`tools/spt-mcp/src/types.ts:154-183`）；tarkov err=`{message,details}`（`tools/tarkov-runtime-mcp/src/types.ts:56-80`）；mo2 无 envelope 类型。
- 打包：仓库无 npm workspaces；`scripts/build-portable-plugin.ps1:312-314` 硬编码三包；`Copy-McpRuntimeDependencies`（:232-272）按 `npm ls --omit=dev --parseable` 前缀过滤 vendor 物理依赖（kit 的依赖物理位于 kit/node_modules，可被现有机制正确 vendor）。
- 校验：`tests/bootstrap/verify-mcp-entrypoints.ps1` 硬编码三入口 + prepare 断言 + dist 未跟踪检查。
- 每包自带 `.gitignore`（`dist/` 规则）。

## 目标设计

### tools/mcp-kit 包结构

```
tools/mcp-kit/
  package.json          # private, type:module, deps: sdk ^1.0.0 + zod + zod-to-json-schema
                        # scripts: build=tsc -p tsconfig.json / prepare=npm run build / pretest=npm run build / test=vitest run / typecheck=tsc --noEmit
  tsconfig.json         # 模板=tools/spt-mcp/tsconfig.json（ES2022/NodeNext/outDir dist/rootDir src）+ declaration:true
  .gitignore            # dist/
  vitest.config.ts      # spawn 测试超时配置（bootstrap 冒烟）
  package-lock.json     # npm install 生成（随其他三包惯例）
  src/
    index.ts            # 公共导出面
    schema.ts           # schemaFor（canonical 管道）+ normalizeMcpInputSchema（自 mo2 逐字搬移）
    envelope.ts         # OkEnvelope / ErrEnvelope / Envelope + okEnv / errEnv
    result.ts           # ToolResult + jsonText + jsonResult
    bootstrap.ts        # runStdioServer + runMain
  tests/
    schema.test.ts      # mo2 normalize 测试搬移（内容不动，仅改 import）+ schemaFor 新用例
    envelope.test.ts    # canonical 形状断言
    bootstrap.test.ts   # stdio 冒烟：fixture 服务 initialize/tools-list/tools-call 往返
    fixtures/echo-server.mjs
```

### 公共 API（fixer 按此实现）

```ts
// schema.ts
export function schemaFor(schema: ZodTypeAny): Record<string, unknown>;
// = zodToJsonSchema(schema, { target: "jsonSchema7" }) → delete $schema → normalizeMcpInputSchema
export function normalizeMcpInputSchema(raw: Record<string, unknown>): Record<string, unknown>;
// 自 tools/mo2-mcp/src/schema-normalizer.ts 逐字搬移（含类型与文档注释）

// envelope.ts
export interface OkEnvelope { ok: true; tool: string; summary: string; data?: unknown; }
export interface ErrEnvelope { ok: false; tool: string; code: string; message: string; hint?: string; details?: unknown; }
export type Envelope = OkEnvelope | ErrEnvelope;
export function okEnv(tool: string, summary: string, data?: unknown): OkEnvelope;
export function errEnv(tool: string, message: string, code: string, extra?: { hint?: string; details?: unknown }): ErrEnvelope;

// result.ts
export interface ToolResult { content: Array<{ type: "text"; text: string }>; isError?: boolean; }
export function jsonText(value: unknown): { type: "text"; text: string };
export function jsonResult(body: unknown, isError?: boolean): ToolResult;

// bootstrap.ts
export interface McpToolDefinition { name: string; description: string; inputSchema: Record<string, unknown>; }
export interface StdioServerOptions {
  name: string;
  version: string;
  listTools: () => McpToolDefinition[];
  callTool: (name: string, args: Record<string, unknown>) => ToolResult | Promise<ToolResult>;
  onBeforeConnect?: () => void | Promise<void>;   // mo2: lifecycle.markReady 时序
  onConnected?: () => void | Promise<void>;       // mo2: eager bind + ready log 时序
}
export async function runStdioServer(options: StdioServerOptions): Promise<void>;
// = new Server({name,version},{capabilities:{tools:{}}}) + ListTools/CallTool 处理器
//   + 默认 SIGINT/SIGTERM（stderr 提示 + exit 0）+ await connect + onConnected
export function runMain(metaUrl: string, main: () => Promise<void>, label?: string): void;
// = invokedAsMain 判定（import.meta.url === pathToFileURL(argv[1]).href）+ 失败 catch（`${label} 启动失败：` + exit 1）
```

注意：`runMain` 的 metaUrl 必须由调用方传入（kit 内计算会比对错误 URL）。mo2 由无守卫改为有守卫（更安全，生产路径无行为变化）。

### 迁移映射

**spt-mcp**
- `src/index.ts`：删本地 `schemaFor`/`jsonResult`/`invokedAsMain`/`main` 主体；`TOOL_DEFINITIONS` 改用 kit `schemaFor`；`main()` = layout 警告 + `runStdioServer({...})`；尾部 = `runMain(import.meta.url, main, SERVER_NAME)`。
- `src/types.ts`：删本地 envelope 类型与构造器，改为从 kit **re-export**（facade：工具文件 import 不变）；保留 `SPT_ERROR_CODES`。
- 工具文件：`errEnv` 调用点第 4 参改 options 对象（`errEnv(t, m, c, hint)` → `errEnv(t, m, c, { hint })`）。
- 测试：错误信封断言 `summary` → `message`（注意 OK 信封的 `summary` 保留，仅 err 路径）。

**tarkov-runtime-mcp**
- `src/index.ts`：同 spt 结构（`createRuntime`/`createDispatcher`/`TOOL_DEFINITIONS` 导出名不变——6 个测试文件依赖）。
- `src/types.ts`：envelope 改 kit re-export；保留 `RUNTIME_ERROR_CODES`。
- `src/errors.ts`（toErrorEnvelope）与工具文件：`errEnv` 第 4 参改 `{ details }`。
- 测试：断言预期零变化（字段名 message/details 本就是 canonical）；仅 import 路径若受影响。

**mo2-mcp**
- `package.json`：sdk `^0.6.0` → `^1.0.0`；`npm install` 刷新 lockfile。
- `src/index.ts`：ListTools 用 kit `schemaFor`（替换 openApi3+normalize 内联）；`main()` = 构建 ctx + `runStdioServer({ onBeforeConnect: markReady, onConnected: eager bind + ready log })` + `runMain` 尾部。
- `src/dispatch.ts`：本地 `jsonText` 改 kit import（同实现）；`DispatchToolCallResult` 可换 kit `ToolResult`。
- 删除 `src/schema-normalizer.ts`（已搬移）。
- 测试：`tests/normalize-mcp-input-schema.test.ts` 搬移至 kit；`index-schema-enforcement` 保留（集成面）；**499 全绿**（normalize 11 项已搬 kit）。

## 已声明 wire delta（全部需 golden/审查确认）

1. spt 错误信封字段 `summary` → `message`（预期唯一字段级 delta）。
2. spt/tarkov ListTools：新增 normalize 步骤——若其 schema 无顶层 union 则为恒等（golden 预期零 diff）；若有 wrap 则逐项审查。
3. mo2 ListTools：target openApi3 → jsonSchema7（nullable/`$schema` 等编码差异）+ 去 `$schema`——逐项审查，确认对 OpenCode 为改进或等价。
4. mo2 新增 `runMain` 守卫与 SIGINT/SIGTERM 处理器（行为对齐，低风险）。
5. mo2 启动失败文案统一为 `mo2-mcp 启动失败：...`（原 `mo2-mcp fatal: ...`）。
6. mo2 无参数调用的审计 `argsHash`：`sha256("null")` → `sha256("{}")`（kit 统一 `arguments ?? {}`，与 spt/tarkov 一致；仅审计日志字段，无测试依赖）。

## 车道任务

### 车道 A（wave1，fixer）：golden 捕获 + kit 抽取 + spt 迁移
0. **改前 golden**：写 `.scratch/c2-mcp-kit/tools/capture-tools-list.mjs`（stdio 握手：initialize → initialized → tools/list，参考 `tools/mo2-mcp/tests/smoke.test.ts` 的握手模式；接受入口路径参数，可复用于便携冒烟）；对三台 dist 入口捕获 → `.scratch/c2-mcp-kit/goldens/pre/<server>.tools-list.json`。
1. 建 `tools/mcp-kit`（结构/API 见上）；搬移 normalize + 测试；写 schema/envelope/result/bootstrap 测试；`npm install` + build + test。
2. spt 迁移（见映射）。
3. 验证：kit test/build；spt `npm run typecheck` + `npm test` + build；golden 改后对比（spt）。
4. 报告：文件清单、测试输出、golden diff 摘要、偏差说明。**冻结 kit API**。

### 车道 B（wave2，fixer）：tarkov 迁移
依赖：车道 A 完成（kit API 冻结）。按映射执行 + 全量验证 + golden 对比（tarkov）。

### 车道 C（wave2，fixer）：mo2 迁移
依赖：车道 A 完成。按映射执行 + 全量验证（含 499 套件 + dist smoke）+ golden 对比（mo2，逐项审查 delta）。

### 车道 D（wave2，fixer）：接线收尾（与 B/C 并行）
- `scripts/build-portable-plugin.ps1`：`Copy-McpPackage -PackageName "mcp-kit"`（第四包）；`$RequiredArtifacts` 加 `tools/mcp-kit/dist/index.js`；头部注释同步。
- `tests/bootstrap/verify-mcp-entrypoints.ps1`：$entries 加 kit dist；manifest 循环加 kit package.json；git 未跟踪检查加 kit dist。
- 安装顺序文档：kit 先于三台（kit/dist 为服务器构建前置）——更新 verify 失败文案与开发安装说明（检索 README / docs 中 `npm install` 段落）。
- 若 `verify-layout.ps1` 含工具清单则同步。

### 车道 E（orchestrator）：验证 + 审查 + 记录
- 全量验证矩阵（下节）→ oracle 双轴 code-review（Standards / Spec）→ 修复轮 → dev-log 补记。

## 验证矩阵

| # | 命令/动作 | 通过标准 |
|---|-----------|----------|
| 1 | `npm --prefix tools/mcp-kit test`（含 build） | 全绿 |
| 2 | `npm --prefix tools/spt-mcp run typecheck` + `test` + `build` | 全绿（72+） |
| 3 | `npm --prefix tools/tarkov-runtime-mcp run typecheck` + `test` + `build` | 全绿 |
| 4 | `npm --prefix tools/mo2-mcp test` + `build` | 全绿（499；normalize 11 项已搬 kit） |
| 5 | golden 改后捕获 + diff（三台） | spt/tarkov 零 diff（或仅审查通过的 normalize wrap）；mo2 仅已声明 delta |
| 6 | `powershell -NoProfile -ExecutionPolicy Bypass -File tests/bootstrap/verify-all.ps1` | 9/9 |
| 7 | `scripts/build-portable-plugin.ps1` + 便携树三台冒烟（复用 capture 脚本指向便携 dist） | 三台 tools/list 往返成功；kit 正确 vendor |
| 8 | OpenCode 重启后复核 | 三台 MCP 加载；mo2 工具 schema 被接受（**重启后复核**） |

## 风险与逃生方案

- R1 tsc 相对 dist 导入解析失败（低概率）→ 逃生：改 `file:` 依赖 + 便携显式 vendor 步骤（决策 D1 备选）。
- R2 normalize 对 spt/tarkov 产生意外 wrap → golden 逐项审查；若为回退则将该 server 的管道降级为「jsonSchema7 + 去 $schema」（记录偏差）。
- R3 mo2 SDK 升级回归 → 499 套件 + smoke 兜底；若出现未知破坏，回退 0.6.1 并另立工单（kit 不用 SDK 新特性）。
- R4 便携 vendor 受 CJK 解码影响 → 沿用既有 UTF-8 强制模式（`build-portable-plugin.ps1:243-259`）。
- R5 fresh clone 顺序（kit 先）→ 文档 + verify 文案。

## 红线

- 不 commit；不碰 P0 未提交变更的语义（additive）。
- 零**未声明**行为变化；每处 delta 必须在上表登记并审查。
- 中文注释；无 emoji；需重启生效项标注「重启后复核」。
- 车道边界：A 不碰 wave2 文件；B/C/D 互不越界（tools/tarkov-runtime-mcp / tools/mo2-mcp / scripts+tests+docs）。
