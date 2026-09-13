// =============================================================================
// 快照组装器
//
// 按 `SUPPORTED_SECTIONS` 的规范顺序组装所选 section：输出字段顺序与入参顺序
// 无关，保证相同输入逐字节一致。每个 section 只发起一次路由请求，一次调用
// 原子返回全部所选数据。
// =============================================================================

import type { SptConnection } from "../transport/connection.js";
import { PROFILE_ROUTE, normalizeProfileSection } from "./profile.js";
import {
  SNAPSHOT_SCHEMA_VERSION,
  SUPPORTED_SECTIONS,
  type Snapshot,
  type SnapshotSectionName,
} from "./schema.js";

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
    if (name === "profile") {
      const response = await connection.request({
        method: "POST",
        path: PROFILE_ROUTE,
        body: {},
      });
      snapshot.sections.profile = normalizeProfileSection(response.body);
    }
  }

  return snapshot;
}
