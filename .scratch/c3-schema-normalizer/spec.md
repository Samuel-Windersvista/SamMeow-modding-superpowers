# C3 · mo2-mcp schema 归一化抽离 — 实施规格

> 来源：架构审查候选 C3 + 确认（2026-09-16）。**纯搬移重构，零行为变化。**
> **进展（Work Status）**: CLOSED — 2026-09-16（纯搬移交付：schema-normalizer.ts 323 行；index.ts 456→167；510 绿 + bootstrap 9/9；未提交）

## 事实基线
- 抽离面：`tools/mo2-mcp/src/index.ts:180-456` —— `normalizeMcpInputSchema`（223-301）+
  `HoistedDiscriminants`（303-306）+ `extractDiscriminants`（308-408）+
  `_flattenUnionBranches`（410-456）+ 函数级文档注释块（约 180-222，含
  "DO NOT REINTRODUCE anyOf/oneOf/allOf" 禁令说明）
- 消费者：`index.ts:112`（ListTools 处理器）+ `tests/normalize-mcp-input-schema.test.ts:34`
  （import 自 `../src/index.js`）
- 专项测试已存在（481 行，BUG-13/27 回归守卫）→ 无需新增测试，只需更新 import

## 车道 A 任务（fixer）
1. 新建 `tools/mo2-mcp/src/schema-normalizer.ts`：
   - 纯搬移上述函数 + 类型 + 文档注释；**逻辑一字不改**。
   - 模块头注释：说明 BUG-13/27 背景与为何独立成模块（局部性：入口只留接线；
     最高 bug 密度逻辑有独立测试面）。
   - 导出：`normalizeMcpInputSchema`（其余保持模块私有）。
2. `tools/mo2-mcp/src/index.ts`：
   - 删除搬移段（180-456）；保留 `schemaFor`（zod→JSON 转换胶水）与注册逻辑不动。
   - 新增 `import { normalizeMcpInputSchema } from "./schema-normalizer.js";`
   - 预期：456 行 → 约 180 行。
3. `tests/normalize-mcp-input-schema.test.ts`：import 改指 `../src/schema-normalizer.js`；
   测试内容不动。index 不保留 re-export。
4. 验证：
   - `cd tools/mo2-mcp; npm run typecheck; npm test`（全量套件全绿）
   - `npm run build`
   - `powershell -NoProfile -ExecutionPolicy Bypass -File tests/bootstrap/verify-all.ps1` **9/9**
5. 红线：零行为变化（diff 应只含搬移与 import 行）；**不 commit**；不碰车道 B 文件（`.scratch/`、`docs/`）。

## 车道 B 任务（orchestrator）
- 本规格 + dev-log 补记（fixer 交付后）。
