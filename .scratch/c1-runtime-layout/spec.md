# C1 · 运行时布局契约（Runtime Layout）— 实施规格

> **进展（Work Status）**: CLOSED — 2026-09-16（实现 + 双轴审查 + 会话内验收三项全过；未提交）

> 来源：架构审查候选 C1（知识层静默死亡）+ grilling 三轮闭合决策（2026-09-16）。
> 状态：已确认，进入实施。执行者：@fixer（tdd）。**不 commit**（用户偏好）。
> 决策记录：`docs/adr/0008-runtime-layout-resolution.md`；术语：`CONTEXT.md`。

## 1. 背景（活体实证）

- `.opencode/plugins/spt-modding-superpowers.js:90` 把 `SPT_KB_ROOT` 解析到仓库**上两级**的不存在路径；spt-mcp 的 `resolveKbRoot()` 无条件信任 env，`loadJson()` 吞错返回 null → KB 工具呈现「0 条匹配」且无报错（本会话实测：`spt_kb_query(keyword=SPT)` → 0 条；`spt_forge_search(radar)` → 0 个；而 `spt_list_mods` 正常）。
- helper 定位（`mod-reader.ts:27-45`、`il-reader.ts:35-52`）存在同类陷阱：env 显式设置即优先、不校验存在性。

## 2. 已定决策（Q1–Q11，全部确认）

| # | 决策 |
|---|---|
| Q1-B | 建**共享运行时布局解析器**（单一源码），非双点补丁 |
| Q2-A | 启动 stderr 健康警告 + 工具级结构化错误；不退出进程；纯文件系统工具不受影响 |
| Q3-B | env **显式设置但无效 → 报错（不回退）**；未设 → 默认派生 + 存在性校验 |
| Q4-A | 便携包携带知识层（index+curated+wiki，约 5MB）；archive 永不进包 → forge 工具显式降级 |
| Q5-A | spt-mcp vitest 覆盖 resolver 矩阵 + 工具降级行为 |
| Q6-A | helper exe 纳入同规则（存在性校验 + 明确报错；不自动构建） |
| Q7-A | `shared/runtime-layout.mjs`（无构建纯 ESM）+ `.d.mts`；插件与 spt-mcp 双端相对导入；便携包复制 `shared/` |
| Q8-A | helper 产物为打包前置条件（存在则随包；缺失则打包警告 + 打包版降级） |
| Q9-A | 新增 `spt_health` 只读工具（布局报告） |
| Q10-A | `build-portable-plugin.ps1` 改动并入本交付 |
| Q11-A | 插件**不再设置三个 env**；解析权归共享解析器；插件仅做启动健康警告 |

## 3. 接口冻结（shared/runtime-layout.mjs）

```js
// 无依赖纯 ESM。不抛错，返回报告。
export function resolveRuntimeLayout(pluginRoot, env = process.env) -> RuntimeLayout
export function formatLayoutWarnings(layout) -> string   // 多行文本，供 stderr
```

```ts
// shared/runtime-layout.d.mts
export type ResourceSource = 'env' | 'default';
export interface ResourceStatus { path: string; source: ResourceSource; ok: boolean; reason?: string; }
export interface RuntimeLayout {
  mode: 'repo' | 'portable';        // 仅标签：pluginRoot/.git 存在 => 'repo'，否则 'portable'
  pluginRoot: string;
  kb: { root: ResourceStatus; index: ResourceStatus; archive: ResourceStatus };
  helpers: { metadata: ResourceStatus; il: ResourceStatus };
  warnings: string[];               // = 各 !ok 资源的 reason
}
export function resolveRuntimeLayout(pluginRoot: string, env?: NodeJS.ProcessEnv): RuntimeLayout;
export function formatLayoutWarnings(layout: RuntimeLayout): string;
```

### 解析规则（唯一计算点）

| 资源 | env 键 | 未设 → 默认 | 校验 |
|---|---|---|---|
| KB 根 | `SPT_KB_ROOT` | `<pluginRoot>/knowledge/spt-kb` | 目录存在 |
| KB 索引 | —（继承根 source） | `<kbRoot>/index.json` | 文件存在 |
| forge 归档 | — | `<kbRoot>/archive/forge` | 目录 + `api/mods-catalog.json` + `hot-index.json` **均存在**（任一缺失 → ok:false，reason 点名缺失项；防「目录在、数据缺」的静默 0 匹配） |
| metadata helper | `SPT_MCP_HELPER` | `<pluginRoot>/tools/spt-mcp/helper/bin/Release/spt-metadata-reader.exe` | 文件存在 |
| IL helper | `SPT_IL_HELPER` | `<pluginRoot>/tools/spt-mcp/il-helper/bin/Release/spt-il-reader.exe` | 文件存在 |

- env 显式设置但校验失败 → `ok:false` + `reason`（中文完整句，含路径与后果提示）；**不回退**。
- env 未设 → 默认路径 + 校验；失败同样 `ok:false` + `reason`。
- index/archive 的 `source` 继承 KB 根的 source。
- reason 示例：`env SPT_KB_ROOT 指向的路径不存在：<path>（显式设置无效即报错，不回退）` / `forge 归档数据缺失：<path>/api/mods-catalog.json（归档目录在但 API 快照不在，请按 scripts/spt-kb 流程刷新）` / `helper 产物未构建：<path>（构建：dotnet build tools/spt-mcp/helper -c Release）`。

## 4. 文件级变更清单

### 4.1 新增
- `shared/runtime-layout.mjs` —— 上述实现。
- `shared/runtime-layout.d.mts` —— 类型声明。
- `tools/spt-mcp/src/runtime-layout.ts` —— spt-mcp 侧薄包装：
  - `PLUGIN_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..', '..')`（src 与 dist 同深度，两处均解析到包根）
  - `getLayout(env = process.env): RuntimeLayout`（模块级缓存）
  - `resetLayoutForTest(): void`（清缓存，供测试）
  - 导入说明符：`../../../shared/runtime-layout.mjs`（从 src 与 dist 均解析到 `<包根>/shared/…`）
- `tools/spt-mcp/src/tools/health.ts` —— `HealthInput = z.object({}).strict()`；`runHealth(): Envelope` → `okEnv('spt_health', <摘要>, layout)`。
- `tools/spt-mcp/tests/unit/runtime-layout.test.ts`、`tools/spt-mcp/tests/unit/kb-query.test.ts` —— 见 §5。

### 4.2 修改
- `tools/spt-mcp/src/forge-reader.ts`：
  - 删除 `resolveKbRoot()`；`loadCatalog/loadHotIndex/bestSptMap` 的默认参数改为 `getLayout().kb.root.path`（保留显式传参能力）。
  - `loadJson`、`KB_INDEX_REL`、`FORGE_*` 常量保留。
- `tools/spt-mcp/src/tools/kb-query.ts`：先 `getLayout()`；`!layout.kb.index.ok` → `errEnv('spt_kb_query', '知识库不可用', KB_UNAVAILABLE, <reason + path>)`；否则按原逻辑查询（`loadJson(layout.kb.root.path, KB_INDEX_REL)`）。
- `tools/spt-mcp/src/tools/forge-search.ts`：先 `getLayout()`；`!layout.kb.archive.ok` → `errEnv('spt_forge_search', 'Forge 归档不可用', KB_UNAVAILABLE, <reason + path>)`；否则原逻辑。
- `tools/spt-mcp/src/types.ts`：`SPT_ERROR_CODES` 增加 `KB_UNAVAILABLE: "kb_unavailable"`。
- `tools/spt-mcp/src/index.ts`：
  - `main()` 启动时：`const layout = getLayout(); if (layout.warnings.length) process.stderr.write(formatLayoutWarnings(layout) + '\n');`（在 `server.connect` 之前）。
  - 注册 `spt_health`（TOOL_DEFINITIONS + HANDLERS，8 个工具）。
  - 更新文件头注释（7 → 8 个工具）；`spt_kb_query` 描述删除「71 条目」旧数字（避免继续漂移）。
- `tools/spt-mcp/src/mod-reader.ts`：`helperPath()` 改为读 `getLayout().helpers.metadata`（`ok ? path : null`）；缺失时的 error 文案带上 `reason`。
- `tools/spt-mcp/src/il-reader.ts`：同规则改读 `helpers.il`；且 helper 不可用时**不得静默返回空**——`spt_analyze_conflicts` 的结果必须可见该降级（冲突报告或 envelope data 携带 warnings 数组，含 reason），否则 IL 段缺失对调用方不可区分。
- `.opencode/plugins/spt-modding-superpowers.js`：
  - 删除 `SPT_KB_ROOT`/`SPT_MCP_HELPER`/`SPT_IL_HELPER` 三个 const 及对应注释（约 80–92 行）；
  - `config.mcp.spt.environment` 改为 `{}`（父进程 env 透传，用户自定义仍生效）；
  - 工厂函数内**动态导入**（防炸插件加载）：`const mod = await import('../../shared/runtime-layout.mjs').catch(() => null);` 成功则 `resolveRuntimeLayout(PLUGIN_ROOT)` 并 `console.error(formatLayoutWarnings(layout))`（仅当有 warnings）；失败则 `console.error('[spt-modding-superpowers] runtime-layout 模块缺失，跳过健康检查')`。
- `scripts/build-portable-plugin.ps1`（按脚本既有模式扩展）：
  - source list 增加：`shared/`（两个文件）；`knowledge/spt-kb/` 的**非 archive** 子集（index.json、INDEX.md、VERSIONS.md、curated/、wiki/、wiki-tushonka/、sources/——若个别不存在则跳过）；`tools/spt-mcp/helper/bin/Release/` 与 `tools/spt-mcp/il-helper/bin/Release/`（存在则随包，缺失则醒目警告）。
  - 复制后断言：`<out>/shared/runtime-layout.mjs`、`<out>/knowledge/spt-kb/index.json` 存在，否则 `Write-Error`。

## 5. 测试矩阵（vitest，`tools/spt-mcp/tests/unit/`）

`runtime-layout.test.ts`（对 `shared/runtime-layout.mjs` 直接测，env 用参数注入）：
1. env 未设 + 默认路径齐全（临时目录构造 `knowledge/spt-kb/index.json` + `archive/forge`）→ 各资源 ok:true，warnings 空。
2. env 未设 + 默认缺失 → ok:false + reason 含路径。
3. `SPT_KB_ROOT` 设为不存在路径 → ok:false，且**未回退**（path === env 值）。
4. `SPT_KB_ROOT` 设为有效临时 KB → source:'env'、ok:true。
5. index 缺失（根存在）→ kb.index.ok:false、kb.root.ok:true。
6. archive 缺失 → kb.archive.ok:false。
6b. archive 目录存在但 `api/mods-catalog.json` / `hot-index.json` 缺失 → kb.archive.ok:false + reason 点名文件。
7. helper env 无效 → helpers.metadata.ok:false；helper 默认缺失 → ok:false + reason 含构建提示。
8. warnings 与 !ok 资源一一对应。

`forge-reader.test.ts` 的「真实 KB」断言（1822 mods / 95 hot-index / 真实搜索冒烟）依赖本机 archive 快照（gitignored、按 `scripts/spt-kb` 流程填充）。改为：fixture 文件缺失时**条件跳过**（vitest `skipIf`，附醒目说明），存在时断言强度不变——使 `npm test` 在无快照机器上全绿、有快照机器上全量断言。

`kb-query.test.ts`（经 `resetLayoutForTest()` + `process.env` 操纵）：
1. `SPT_KB_ROOT` 指向不存在路径 → `runKbQuery({})` 返回 `errEnv` 且 `code === 'kb_unavailable'`。
2. `SPT_KB_ROOT` 指向含 index.json 的临时 KB → 返回 ok（matchCount ≥ 0，filters 回显）。

（`forge-search` 的降级可加一条同类测试，若成本低则加。）

## 6. 验证步骤（执行者必须全跑）

1. `cd tools/spt-mcp && npm run typecheck && npm test` —— 全绿。
2. `cd tools/spt-mcp && npm run build` —— 构建通过。
3. Node 级活体检查（dist 直测，绕过会话缓存）：
   - env 未设：`runKbQuery({keyword:'SPT'})` → `ok:true` 且 `matchCount > 0`（仓库 KB 221 条）。
   - `SPT_KB_ROOT=E:\不存在的路径`：`runKbQuery({})` → `ok:false`、`code:'kb_unavailable'`。
   - 同法验证 `spt_health` 返回完整布局报告。
4. `pwsh tests/bootstrap/verify-all.ps1` —— 仍全绿（若与本次插件改动冲突，如实报告，不得削弱断言）。
5. `pwsh scripts/build-portable-plugin.ps1` —— 若前置（tools/*/dist）齐备则实跑并确认输出含 `shared/` 与 `knowledge/spt-kb/index.json`；若前置缺失则静态核对改动并报告原因。
6. 结束时 `git status --short` 列出变更文件清单。

## 7. 红线

- **不 commit**（用户偏好：无明确要求不提交）。
- 不改 `knowledge/` 内容（只读）；不动 `archive/`。
- 不新增运行时依赖（shared 模块零依赖）。
- 遵循既有代码风格：中文注释、`okEnv/errEnv` 信封、zod `.strict()`、工具注册表模式。
- TS 导入 `.mjs` 的已知风险：若 `tsc` 报 rootDir 相关错误（TS6059），优先用 tsconfig 调整解决（如 `rootDir` 保持 + 声明文件豁免；不得改动 outDir 结构导致 dist 布局漂移），并在最终报告说明处理方式。
