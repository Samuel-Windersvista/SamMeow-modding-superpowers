// =============================================================================
// inventory section 归一
//
// 数据来源：`POST /client/game/profile/list`（与 profile section 同路由，见
// KB `api-notes-5.0/save-profile.md`）。库存数据内嵌于 profile 的
// `characters.pmc.inventory.items`（扁平物品数组，父子关系由 parentId 表达）。
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

/** 兼容 SPT JsonUtil 的 camelCase 序列化：`inventory` 优先，`Inventory` 兜底 */
function readInventory(pmc: Record<string, unknown>): Record<string, unknown> | null {
  if (isRecord(pmc.inventory)) return pmc.inventory;
  if (isRecord(pmc.Inventory)) return pmc.Inventory;
  return null;
}

/** 提取首个 profile 的 PMC 物品数组；无法识别时返回空数组 */
function extractItems(body: unknown): Record<string, unknown>[] {
  const profiles = asRecordArray(extractArray(body, "profiles"));
  const first = profiles[0];
  if (!first) return [];
  const characters = first.characters;
  if (!isRecord(characters) || !isRecord(characters.pmc)) return [];
  const inventory = readInventory(characters.pmc);
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
