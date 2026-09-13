// =============================================================================
// hideout section 归一
//
// 数据来源：`POST /client/hideout/areas`（SPT server 现有 `/client/*` 路由，
// 零桥直连；见 ADR-0003）。该路由返回玩家藏身处区域（`HideoutArea`：
// `type` + `level` + active/constructing 等）；数据库侧的区域配置在
// `/client/hideout/settings`，此处不用。请求方法/响应形状无 5.0 KB 记载 ——
// 按 profile 同款假设 POST + 空 body，响应为区域数组（或 `{ areas: [...] }`）。
// 假设，待 ticket 06 live smoke 验证。
//
// 归一为计数型摘要：区域总数 / 已建造数 / 等级总和 + 逐区域等级（按 type 升序）。
// =============================================================================

import type { HideoutAreaSummary, HideoutSection, SectionMeta } from "./schema.js";
import { asRecordArray, extractArray, readNumber } from "./util.js";

/** hideout section 的数据来源路由 */
export const HIDEOUT_ROUTE = "/client/hideout/areas";

/** hideout section 元信息（来源路由 + 新鲜度） */
export function hideoutMeta(): SectionMeta {
  return { source: "route", route: HIDEOUT_ROUTE, freshness: "live" };
}

/** 逐区域按 type 升序，同 type 按 level 升序（确定性） */
function compareAreas(a: HideoutAreaSummary, b: HideoutAreaSummary): number {
  if (a.type !== b.type) return a.type - b.type;
  return a.level - b.level;
}

/** 把 `/client/hideout/areas` 响应归一为 hideout section 摘要 */
export function normalizeHideoutSection(body: unknown): HideoutSection {
  const entries = asRecordArray(extractArray(body, "areas"));
  const areas: HideoutAreaSummary[] = entries.map((entry) => ({
    type: readNumber(entry.type),
    level: readNumber(entry.level),
  }));
  areas.sort(compareAreas);

  let builtAreas = 0;
  let totalLevel = 0;
  for (const area of areas) {
    if (area.level > 0) builtAreas += 1;
    totalLevel += area.level;
  }

  return {
    meta: hideoutMeta(),
    areaCount: areas.length,
    builtAreas,
    totalLevel,
    areas,
  };
}
