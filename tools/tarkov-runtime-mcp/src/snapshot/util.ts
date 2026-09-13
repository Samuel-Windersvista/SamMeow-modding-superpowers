// =============================================================================
// 快照归一共享工具
//
// 各 section 归一器共用的小工具与任务状态归类逻辑，保证对缺失/非法字段的
// 容错一致（缺失即回落 0 / 空），从而维持快照确定性。
// =============================================================================

import type { QuestCounts } from "./schema.js";

export function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/** 有限数字取值；缺失/非法回落到 0（确定性） */
export function readNumber(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

/** 字符串取值；缺失/非字符串回落到空串（确定性） */
export function readString(value: unknown): string {
  return typeof value === "string" ? value : "";
}

/** 记录数组过滤；非数组返回空数组 */
export function asRecordArray(value: unknown): Record<string, unknown>[] {
  return Array.isArray(value) ? value.filter(isRecord) : [];
}

/**
 * 从响应体中提取数组：兼容裸数组与 `{ <key>: [...] }` 包裹两种形状。
 * 无法识别时返回空数组。
 */
export function extractArray(body: unknown, ...keys: string[]): unknown[] {
  if (Array.isArray(body)) return body;
  if (isRecord(body)) {
    for (const key of keys) {
      const value = body[key];
      if (Array.isArray(value)) return value;
    }
  }
  return [];
}

/** EFT QuestStatus 名称 -> 计数桶（小写匹配） */
const QUEST_STATUS_BY_NAME: Record<string, keyof QuestCounts> = {
  availableforstart: "available",
  started: "inProgress",
  availableforfinish: "inProgress",
  success: "completed",
  fail: "failed",
  failrestartable: "failed",
  markedasfailed: "failed",
  expired: "failed",
};

/** EFT QuestStatus 枚举值 -> 计数桶（状态被序列化为数字时的兜底） */
const QUEST_STATUS_BY_NUMBER: Record<number, keyof QuestCounts> = {
  1: "available",
  2: "inProgress",
  3: "inProgress",
  4: "completed",
  5: "failed",
  6: "failed",
  7: "failed",
  8: "failed",
};

/** 任务状态归类；未知状态返回 null（不计入任何桶） */
export function classifyQuestStatus(status: unknown): keyof QuestCounts | null {
  if (typeof status === "string") {
    return QUEST_STATUS_BY_NAME[status.toLowerCase()] ?? null;
  }
  if (typeof status === "number") {
    return QUEST_STATUS_BY_NUMBER[status] ?? null;
  }
  return null;
}

/** 任务进度计数：可用 / 进行（含可交付）/ 完成 / 失败 */
export function countQuests(quests: unknown): QuestCounts {
  const list = asRecordArray(quests);
  const counts: QuestCounts = { available: 0, inProgress: 0, completed: 0, failed: 0 };
  for (const quest of list) {
    const bucket = classifyQuestStatus(quest.status);
    if (bucket) {
      counts[bucket] += 1;
    }
  }
  return counts;
}
