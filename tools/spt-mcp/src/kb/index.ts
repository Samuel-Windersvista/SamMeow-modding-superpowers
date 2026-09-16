// =============================================================================
// kb/index.ts — KB 契约模块公共导出面（C7）
//
// 消费者：
//   - src/tools/kb-query.ts（运行时查询）
//   - src/types.ts（KbEntry facade re-export）
//   - scripts/spt-kb/validate-index.mjs（经 dist 相对导入）
//   - scripts/spt-kb/sync-index.mjs（经 dist 相对导入）
// =============================================================================

export {
  KB_SCHEMA_VERSION,
  collectStats,
  parseIndexForQuery,
  validateIndex,
} from "./contract.js";
export type {
  KbDomain,
  KbIndexEntry,
  KbIndexStats,
} from "./contract.js";

export { queryIndex } from "./query.js";
export type { KbQueryFilters } from "./query.js";
