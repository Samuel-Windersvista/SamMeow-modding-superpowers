// =============================================================================
// inventory section 归一
//
// 数据来源：`POST /client/game/profile/list`（与 profile section 同路由）。响应信封
// `{err, errmsg, data}` 由 util.unwrapEnvelope 统一解开。
//
// 5.0 live 实测（2026-09-13）data 为扁平 profile 数组，库存位于 PMC 条目的
// `Inventory.items`（扁平物品数组，父子关系由 parentId 表达）；scav 条目 `savage`
// 为 null，据此选取 PMC。
//
// 归一为计数型摘要：物品条目总数 + 不同模板数，不做全量枚举。
// =============================================================================

import type { InventorySection, SectionMeta } from "./schema.js";
import { asRecordArray, extractArray, isRecord, readString } from "./util.js";

/** inventory section 的数据来源路由 */
export const INVENTORY_ROUTE = "/client/game/profile/list";

/** inventory section 元信息（来源路由 + 新鲜度） */
export function inventoryMeta(): SectionMeta {
  return { source: "route", route: INVENTORY_ROUTE, freshness: "live" };
}

/** 选取 PMC 条目（`savage` 非空）；无匹配时回落首个条目 */
function selectPmcProfile(body: unknown): Record<string, unknown> | null {
  const profiles = asRecordArray(extractArray(body, "profiles"));
  const pmc = profiles.find((entry) => readString(entry.savage) !== "");
  return pmc ?? profiles[0] ?? null;
}

/** 读取 PMC 库存对象（兼容 PascalCase / camelCase） */
function readInventory(pmc: Record<string, unknown>): Record<string, unknown> | null {
  if (isRecord(pmc.Inventory)) return pmc.Inventory;
  if (isRecord(pmc.inventory)) return pmc.inventory;
  return null;
}

/** 提取 PMC 物品数组；无法识别时返回空数组 */
function extractItems(body: unknown): Record<string, unknown>[] {
  const pmc = selectPmcProfile(body);
  if (!pmc) return [];
  const inventory = readInventory(pmc);
  if (!inventory || !Array.isArray(inventory.items)) return [];
  return asRecordArray(inventory.items);
}

/** 把 `/client/game/profile/list` 响应归一为 inventory section 摘要 */
export function normalizeInventorySection(body: unknown): InventorySection {
  const items = extractItems(body);
  const templates = new Set<string>();
  for (const item of items) {
    const tpl = readString(item._tpl);
    if (tpl) templates.add(tpl);
  }
  return {
    meta: inventoryMeta(),
    itemCount: items.length,
    distinctTemplates: templates.size,
  };
}
