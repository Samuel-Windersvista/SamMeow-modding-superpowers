// =============================================================================
// quests section 归一
//
// 数据来源：`POST /client/quest/list`（SPT server 现有 `/client/*` 路由，零桥
// 直连；见 ADR-0003）。5.0 该路由改用 `StreamedRouteAction`（KB
// `api-notes-5.0/http-routing.md`），请求方法/响应形状无记载 —— 按 profile
// 同款假设 POST + 空 body，响应为任务数组（或 `{ quests: [...] }`）。
// 假设，待 ticket 06 live smoke 验证。
//
// 归一为计数型摘要：可用 / 进行（含可交付）/ 完成 / 失败，不做全量枚举。
// 任务状态归类复用 util.countQuests（与 profile section 同一套映射）。
// =============================================================================

import type { QuestsSection, SectionMeta } from "./schema.js";
import { asRecordArray, countQuests, extractArray } from "./util.js";

/** quests section 的数据来源路由 */
export const QUESTS_ROUTE = "/client/quest/list";

/** quests section 元信息（来源路由 + 新鲜度） */
export function questsMeta(): SectionMeta {
  return { source: "route", route: QUESTS_ROUTE, freshness: "live" };
}

/** 把 `/client/quest/list` 响应归一为 quests section 摘要 */
export function normalizeQuestsSection(body: unknown): QuestsSection {
  const list = asRecordArray(extractArray(body, "quests"));
  return {
    meta: questsMeta(),
    questCount: list.length,
    counts: countQuests(list),
  };
}
