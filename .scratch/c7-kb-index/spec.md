# C7 · KB 索引接口归一 + 管线闭环 — 实施规格

> 来源：架构审查候选 C7 + grilling 确认（2026-09-16）。报告：`D:\Temp\architecture-review-20260916-1337.html` §2 C7 卡。
> **进展（Work Status）**: CLOSED — 2026-09-16（全流程完成：车道 A 交付 + 终验 + oracle 双轴 review + 修复轮 11 项；未提交）

## 决策记录（grilling 确认）

| # | 决策 | 结论 | 依据 |
|---|------|------|------|
| D1 | schema 版本 | **v2 + 数据迁移**：单形态（required = path/title/version[]/domain/topic/source；optional = keywords/summary）；2 条 string→array、3 条空 source 按路径前缀填充；schema_version 升 2 | 契约现在被 validator 强制，版本号显式标记断代 |
| D2 | 契约落点 | **spt-mcp `src/kb/` 契约模块**（类型/校验/查询，vitest 覆盖）+ `scripts/spt-kb/validate-index.mjs` 薄包装调 dist | 单一契约实现；便携树两处均已随包 |
| D3 | 闭环范围 | **upsert 生成器 + drift 报告**：扫描 wiki/wiki-tushonka/curated；新文件生成条目；**已有条目绝不改写**（title/keywords/summary 为手工资产）；`--write` 才落盘 | 核心证据：源文件无 title 字段（wiki 与 tushonka 同页标题不同），全量重建会丢数据 |
| D4 | 运行时容错 | **结构校验响亮失败 + version 防御归一**（string→[string]） | 现役 bug 直接修复；一次数据瑕疵不封死查询 |
| D5 | 验证接线 | **bootstrap 第 10 项**（verify-kb-index）+ spt-mcp vitest + kb_query pre/post golden | 接口即测试面 |

## 事实基线（侦察实测）

- `knowledge/spt-kb/index.json`：`schema_version: 1`，`generated: 2026-09-14`，**221 条** + 3 指针段（`tarkov_dev` / `forge_catalog` / `external_repos`，各含 path/description）。
- 条目形态：A=189（path/title/version/domain/topic/source）；B=32（+keywords/summary）；B 中 2 条 version 为 **string**。
- 数据瑕疵：version string=2（`curated/migration/bundle-compat-311-to-41.md`、`curated/migration/server-mod-311-to-41.md`）；source 空=3（上述 2 条 + `archive/forge/c-bucket/MANIFEST.md`）。
- version 值域：`通用`=98 / `4.1`=71 / `5.0`=29 / `3.11`=27 / `4.0`=11 / `live-ref`=8。domain：both=157 / server=47 / client=17。source：wiki=111 / curated=107 / 空=3（wiki-tushonka 条目 source 记为 `wiki`）。
- **现役 bug（活体实测）**：`spt_kb_query({version:"4.1"})` → `internal_error: entry.version.map is not a function`——version 过滤参数 100% 不可用（`tools/spt-mcp/src/tools/kb-query.ts:58`）。
- 管线：**索引生成器不存在**（`skills/maintaining-spt-modding-environment/SKILL.md:85-88` 原文「see tools/ for the exact script if one exists」+ 手工登记 fallback）；`scripts/spt-kb/finalize-manifest.mjs` 只产 Forge 源码归档 MANIFEST，与 index.json 无交互。
- 源文件 frontmatter（wiki 与 curated 均带）：`version: [通用]`（flow 数组）/ `domain` / `topic` / `source`；**无 title**（title 为手工资产）。
- 消费者：`spt-mcp` kb_query（唯一代码消费者）· 技能（读取指引）· `scripts/verify-doc-stats.ps1`（221 计数锚）· `scripts/build-portable-plugin.ps1`（拷贝 index.json）。

## 目标设计

### tools/spt-mcp/src/kb/ 契约模块（新）

```
tools/spt-mcp/src/kb/
  index.ts        # 公共导出面：contract + query
  contract.ts     # 类型 + KB_SCHEMA_VERSION=2 + validateIndex（严格）+ parseIndexForQuery（结构校验+version 归一）+ collectStats
  query.ts        # queryIndex(entries, filters) —— 自 kb-query.ts 抽出的过滤逻辑
```

```ts
// contract.ts
export const KB_SCHEMA_VERSION = 2;
export interface KbIndexEntry {
  path: string; title: string; version: string[];
  domain: "server" | "client" | "both"; topic: string; source: string;
  keywords?: string[]; summary?: string;
}
export function validateIndex(raw: unknown):
  | { ok: true; stats: KbIndexStats }
  | { ok: false; errors: string[] };
// 严格规则：schema_version===2；entries 为数组；path 非空且唯一；title/topic/source 非空；
// version 为 string[]（strict，不归一）；domain ∈ {server,client,both}；keywords?: string[]；summary?: string。
export function parseIndexForQuery(raw: unknown):
  | { ok: true; entries: KbIndexEntry[] }
  | { ok: false; reason: string };
// 结构校验（entries 数组、条目含 path 等）→ 失败即响亮 reason；单条 version 为 string 时归一为 [string]。
export function collectStats(entries: KbIndexEntry[]): KbIndexStats;
// total / bySource / byDomain / byTopic / byVersion / withKeywords / withSummary

// query.ts
export function queryIndex(entries: KbIndexEntry[], filters: {
  topic?: string; domain?: string; version?: string; keyword?: string;
}): KbIndexEntry[];
// 语义与现状逐字保持：topic 精确 ci；domain 精确或 both；version 数组包含或含"通用"；keyword 对 title 子串 ci；按 path 排序。
```

- `src/tools/kb-query.ts`：改薄——布局检查 → loadJson → `parseIndexForQuery`（失败 → 响亮 `kb_unavailable`/结构化错误）→ `queryIndex` → okEnv。工具信封形状不变。
- `src/types.ts`：`KbEntry` 改为从 `./kb/index.js` re-export（facade；保持既有 import 面）。

### scripts/spt-kb/ 薄包装与生成器（新）

- `validate-index.mjs`：读 index.json（默认仓库内，可 `--index <path>`）→ `import { validateIndex } from "../../tools/spt-mcp/dist/kb/index.js"` → 打印 stats（计数/分布）+ errors → exit 0/1。要求 spt-mcp dist 已构建（失败提示 `npm --prefix tools/spt-mcp run build`）。
- `sync-index.mjs`：**upsert 生成器 + drift 报告**。
  - 扫描 `knowledge/spt-kb/wiki/`、`wiki-tushonka/`、`curated/` 下 `*.md`。
  - 已有条目：只校验（validateIndex 单条规则），**绝不改写**。
  - 新文件：生成候选条目（title = frontmatter.title → 首个 H1 → 文件名；version/domain/topic/source 来自 frontmatter，source 缺省按路径前缀推导；keywords/summary 若 frontmatter 有则带上）。
  - drift 报告：`新增（文件缺条目）` / `孤儿（条目缺文件）` / `非法条目` / 统计。
  - `--write`：仅应用新增条目 + 更新 `generated`；不删除孤儿（孤儿仅报告）。
  - 默认 dry-run（无 `--write` 不改盘）。
- frontmatter 解析：手写 YAML 子集（`key: value`、`key: [a, b]`、引号字符串），零依赖。

### 数据迁移（一次性）

- `.scratch/c7-kb-index/tools/migrate-index-v2.mjs`（留在 .scratch 作凭据）：
  1. version string → `[string]`（2 条）；
  2. 空 source 按路径前缀填充（`curated/`→`curated`；`wiki*/`→`wiki`；`archive/`→`archive`）；
  3. `schema_version` 1 → 2；
  4. 保持既有 JSON 格式约定（缩进/换行/UTF-8 无 BOM）；`generated` 不变。
- 迁移后 `validate-index.mjs` 首跑全绿。

### 接线与文档

- `tests/bootstrap/verify-kb-index.ps1`（新）：跑 `node scripts/spt-kb/validate-index.mjs` 断言 exit 0；dist 缺失时给出构建提示。加入 `verify-all.ps1`（9 → **10 项**）。
- `skills/maintaining-spt-modding-environment/SKILL.md`「Rebuilding the index」段：替换「if one exists」为真实命令（`sync-index.mjs` 报告/`--write`；`validate-index.mjs`）；schema_version 1 → 2 表述。
- `scripts/spt-kb/README.md`：脚本清单补两个新脚本。
- 若 `sync-index --write` 实际新增条目（计数变化）：同步更新 README.md 计数声明与其它 221 引用（`git grep 221`），保持 verify-doc-stats 锚点绿。

## 已声明行为 delta（全部需 golden/审查确认）

1. **kb_query version 过滤：崩溃 → 修复**（pre: `internal_error`；post: 正常过滤）。唯一行为修复。
2. kb_query 其余过滤/信封/排序不变。**输出条目经契约模块按 canonical 键序重建**（`path/title/version/domain/topic/source/keywords?/summary?`）——form-B 条目（32 条）原键序被归一，未列入契约的额外字段不会出现在输出中（当前数据无此类字段）；值语义不变（JSON 键序无语义）。golden 实测：`topic` 调用逐字节相同；`{}`/`domain`/`keyword` 为「已声明数据迁移（值级 5 处）+ 键序归一」合并 diff（行级 102/95、94/87、16/12）；`version` 调用从错误信封变为结果（169 命中 = 71 + 98 通用，语义正确）。
3. index.json 数据：2 条 version 归一、3 条 source 填充、schema_version→2；条目集合不变（除非 sync `--write` 新增，逐项记录）；**文件键序未动**（diff +13/−6；实测 sync 零新增，`--write` no-op）。
4. bootstrap 9 → 10 项（新增 verify-kb-index）。

## 车道任务

### 车道 A（fixer）：golden 捕获 + 契约模块 + 迁移 + 脚本 + 接线
0. **改前 golden**：`.scratch/c7-kb-index/tools/capture-kb-query.mjs`（spawn spt-mcp dist → initialize → 依次调用 `spt_kb_query`：`{}`、`{topic:"config"}`、`{domain:"server"}`、`{keyword:"mod"}`、`{version:"4.1"}`；逐调用存 JSON）→ `.scratch/c7-kb-index/goldens/pre/`。参考 `.scratch/c2-mcp-kit/tools/capture-tools-list.mjs` 的握手模式。
1. `src/kb/` 契约模块 + kb-query 改薄 + types facade + vitest（`tests/unit/kb-contract.test.ts` 新；`kb-query.test.ts` 补 string-version 归一用例）。
2. 迁移脚本 + 执行（index.json 数据修正）。
3. `scripts/spt-kb/validate-index.mjs` + `sync-index.mjs`；跑 sync dry-run 出 drift 报告；评估并（如合理）`--write` 应用新增条目（逐项记录；计数变化则同步 README 等引用）。
4. `tests/bootstrap/verify-kb-index.ps1` + verify-all 接线（10 项）；技能/README 文档更新。
5. 验证 + 改后 golden 对比 + 报告。

### 车道 B（orchestrator）：终验 + review + 记录
- 全量验证矩阵 → oracle 双轴 code-review → 修复轮 → dev-log + spec CLOSED。

## 验证矩阵

| # | 命令/动作 | 通过标准 |
|---|-----------|----------|
| 1 | `npm --prefix tools/spt-mcp run typecheck` + `test` + `build` | 全绿（73+ 新增） |
| 2 | `node scripts/spt-kb/validate-index.mjs` | exit 0；stats 与实测一致 |
| 3 | `node scripts/spt-kb/sync-index.mjs`（dry） | drift 报告合理；`--write` 后重跑零新增 |
| 4 | kb_query golden pre/post 对比 | 仅已声明 delta（version 修复 + `--write` 新增记录） |
| 5 | `powershell -NoProfile -ExecutionPolicy Bypass -File tests/bootstrap/verify-all.ps1` | **10/10** |
| 6 | 便携重建 + 便携树跑 validate-index | exit 0（薄包装相对 dist 导入在便携树成立） |
| 7 | OpenCode 重启后复核 | `spt_kb_query({version:...})` 正常返回（**重启后复核**） |

## 风险与逃生方案

- R1 dist 未构建时 validate-index 失败 → 包装给出明确构建提示；bootstrap 检查同理（与 verify-mcp-entrypoints 惯例一致）。
- R2 sync `--write` 引入意外条目 → 默认 dry-run；应用前报告逐项审查；孤儿不删。
- R3 迁移破坏 index.json 格式 → 迁移脚本先备份（`.scratch/c7-kb-index/goldens/index-pre-migration.json`）再写。
- R4 便携树 scripts→dist 相对导入断裂 → 终验第 6 项显式覆盖。

## 红线

- 不 commit；不碰 `tools/mcp-kit`（冻结）与三台 MCP 既有工具行为（kb_query 除外，即已声明 delta 1）。
- 已有条目的 title/keywords/summary 绝不改写（手工资产）。
- 中文注释；无 emoji；需重启生效项标注「重启后复核」。
