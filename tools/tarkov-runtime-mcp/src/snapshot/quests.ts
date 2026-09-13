// =============================================================================
// quests section 归一
//
// 数据来源：`POST /client/quest/list`（SPT server 现有 `/client/*` 路由，零桥
// 直连；见 ADR-0003）。5.0 该路由改用 `StreamedRouteAction`（KB
// `api-notes-5.0/http-routing.md`）。响应信封 `{err, errmsg, data}` 由
// util.unwrapEnvelope 统一解开。
//
// 5.0 live 实测（2026-09-13）：data 为任务定义数组；`status` 恒为 0，玩家真实
// 状态在 SPT 附加字段 `sptStatus`（1=AvailableForStart，2=Started）。归一优先取
// `sptStatus`，缺失时回落到 `status`。
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

/** 优先取 `sptStatus`（live 真实状态），缺失时回落 `status` */
function effectiveStatus(quest: Record<string, unknown>): unknown {
  return "sptStatus" in quest ? quest.sptStatus : quest.status;
}

/** 把 `/client/quest/list` 响应归一为 quests section 摘要 */
export function normalizeQuestsSection(body: unknown): QuestsSection {
  const list = asRecordArray(extractArray(body, "quests"));
  const normalized = list.map((quest) => ({ status: effectiveStatus(quest) }));
  return {
    meta: questsMeta(),
    questCount: list.length,
    counts: countQuests(normalized),
  };
}

