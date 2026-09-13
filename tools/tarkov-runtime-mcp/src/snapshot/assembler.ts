// =============================================================================
// 快照组装器
//
// 按 `SUPPORTED_SECTIONS` 的规范顺序组装所选 section：输出字段顺序与入参顺序
// 无关，保证相同输入逐字节一致。每个 section 发起自身所需的路由请求，一次
// 调用原子返回全部所选数据。
// =============================================================================

import type { SptConnection } from "../transport/connection.js";
import { HIDEOUT_ROUTE, normalizeHideoutSection } from "./hideout.js";
import { INVENTORY_ROUTE, normalizeInventorySection } from "./inventory.js";
import { PROFILE_ROUTE, normalizeProfileSection } from "./profile.js";
import { QUESTS_ROUTE, normalizeQuestsSection } from "./quests.js";
import {
  SNAPSHOT_SCHEMA_VERSION,
  SUPPORTED_SECTIONS,
  type Snapshot,
  type SnapshotSectionName,
} from "./schema.js";
import { TRADERS_ROUTE, normalizeTradersSection } from "./traders.js";
import { unwrapEnvelope } from "./util.js";

/** 判断 section 名是否已实现 */
export function isSupportedSection(name: string): name is SnapshotSectionName {
  return (SUPPORTED_SECTIONS as readonly string[]).includes(name);
}

/**
 * 组装快照。
 *
 * @param connection S1 接缝：传输层抽象（测试注入 fake）
 * @param sections 已校验的 section 名集合
 */
export async function assembleSnapshot(
  connection: SptConnection,
  sections: readonly SnapshotSectionName[],
): Promise<Snapshot> {
  const selected = new Set<SnapshotSectionName>(sections);
  const snapshot: Snapshot = { schemaVersion: SNAPSHOT_SCHEMA_VERSION, sections: {} };

  for (const name of SUPPORTED_SECTIONS) {
    if (!selected.has(name)) continue;
    switch (name) {
      case "profile": {
        const response = await connection.request({ method: "POST", path: PROFILE_ROUTE, body: {} });
        snapshot.sections.profile = normalizeProfileSection(unwrapEnvelope(response.body));
        break;
      }
      case "traders": {
        const response = await connection.request({ method: "POST", path: TRADERS_ROUTE, body: {} });
        snapshot.sections.traders = normalizeTradersSection(unwrapEnvelope(response.body));
        break;
      }
      case "quests": {
        const response = await connection.request({ method: "POST", path: QUESTS_ROUTE, body: {} });
        snapshot.sections.quests = normalizeQuestsSection(unwrapEnvelope(response.body));
        break;
      }
      case "hideout": {
        const response = await connection.request({ method: "POST", path: HIDEOUT_ROUTE, body: {} });
        snapshot.sections.hideout = normalizeHideoutSection(unwrapEnvelope(response.body));
        break;
      }
      case "inventory": {
        const response = await connection.request({ method: "POST", path: INVENTORY_ROUTE, body: {} });
        snapshot.sections.inventory = normalizeInventorySection(unwrapEnvelope(response.body));
        break;
      }
    }
  }

  return snapshot;
}
