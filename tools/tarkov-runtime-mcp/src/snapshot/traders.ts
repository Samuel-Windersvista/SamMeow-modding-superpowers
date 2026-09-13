// =============================================================================
// traders section 归一
//
// 数据来源：`POST /client/trading/api/traderSettings`（SPT server 现有 `/client/*`
// 路由，零桥直连；见 ADR-0003）。该路由为静态路由（`TraderStaticRouter`），
// 请求方法/响应形状无 5.0 KB 记载 —— 按 profile 同款假设 POST + 空 body，
// 响应为商人设置数组（或 `{ traders: [...] }`）。假设，待 ticket 06 live smoke 验证。
//
// 归一为计数型摘要：商人总数 + 逐商人（好感 / 忠诚等级 / assort 条目数），
// 逐商人条目按 id 升序，保证快照确定性；不做全量 assort 枚举。
// =============================================================================

import type { SectionMeta, TraderSummary, TradersSection } from "./schema.js";
import { asRecordArray, extractArray, readNumber, readString } from "./util.js";

/** traders section 的数据来源路由 */
export const TRADERS_ROUTE = "/client/trading/api/traderSettings";

/** traders section 元信息（来源路由 + 新鲜度） */
export function tradersMeta(): SectionMeta {
  return { source: "route", route: TRADERS_ROUTE, freshness: "live" };
}

/** 商人 id：兼容 `_id` / `id` / `traderId` 三种字段名 */
function readTraderId(entry: Record<string, unknown>): string {
  return readString(entry._id) || readString(entry.id) || readString(entry.traderId);
}

/** 忠诚等级：优先显式 `loyaltyLevel`，否则退化为 `loyaltyLevels` 档位数组长度 */
function readLoyaltyLevel(entry: Record<string, unknown>): number {
  if (typeof entry.loyaltyLevel === "number") {
    return readNumber(entry.loyaltyLevel);
  }
  return Array.isArray(entry.loyaltyLevels) ? entry.loyaltyLevels.length : 0;
}

/** assort 条目数：兼容显式计数与 assort/items 数组两种形状 */
function readAssortItems(entry: Record<string, unknown>): number {
  const candidate = entry.assortItems ?? entry.assort ?? entry.items;
  if (typeof candidate === "number") return readNumber(candidate);
  if (Array.isArray(candidate)) return candidate.length;
  return 0;
}

/** 逐商人摘要按 id 升序（确定性；id 相同保持原相对顺序） */
function compareTraders(a: TraderSummary, b: TraderSummary): number {
  if (a.id < b.id) return -1;
  if (a.id > b.id) return 1;
  return 0;
}

/** 把 `/client/trading/api/traderSettings` 响应归一为 traders section 摘要 */
export function normalizeTradersSection(body: unknown): TradersSection {
  const entries = asRecordArray(extractArray(body, "traders"));
  const traders: TraderSummary[] = entries.map((entry) => ({
    id: readTraderId(entry),
    standing: readNumber(entry.standing),
    loyaltyLevel: readLoyaltyLevel(entry),
    assortItems: readAssortItems(entry),
  }));
  traders.sort(compareTraders);

  let totalAssortItems = 0;
  for (const trader of traders) {
    totalAssortItems += trader.assortItems;
  }

  return {
    meta: tradersMeta(),
    traderCount: traders.length,
    totalAssortItems,
    traders,
  };
}
