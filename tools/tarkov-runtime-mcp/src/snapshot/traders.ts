// =============================================================================
// traders section 归一
//
// 数据来源：`POST /client/trading/api/traderSettings`（SPT server 现有
// `/client/*` 路由，零桥直连；见 ADR-0003）。响应信封 `{err, errmsg, data}` 由
// util.unwrapEnvelope 统一解开。
//
// [已知语义缺口] 5.0 live 实测（2026-09-13）：该路由返回商人「静态设置」
// （`_id` / `name` / `loyaltyLevels` 档位配置等），不含玩家好感（standing）与
// assort 条目；玩家 standing 位于 `/client/game/profile/list` 的 PMC 条目
// `TradersInfo`。因此 `standing` / `assortItems` 恒为 0，`loyaltyLevel` 退化为
// `loyaltyLevels.length`（档位总数，非玩家当前等级）。改用 profile 来源属后续
// 工单（见 ticket 07 报告）。
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
