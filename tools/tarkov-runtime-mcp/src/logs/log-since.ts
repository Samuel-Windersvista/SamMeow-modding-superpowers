// =============================================================================
// `since` 游标解析与本地过滤
//
// `/logs/summary` 的 `since` 为**时间游标**（只回 `lastTs` 晚于它的组），桥侧
// 接受两种形态：
//   1. 端点自己输出的 ISO 8601（`lastTs` 原样回填即可往返）；
//   2. 整数 UTC Ticks（C# `DateTime.Ticks`，100ns 单位）。
// 缺省 / 非法 / 非正数 → 不过滤（与桥侧 `LogSummaryQuery.ParseSince` 宽松解析一致）。
//
// 用途：桥故障降级时（或服务器 / fatal 组）需在 MCP 侧做等价过滤，故这里给出
// 与桥侧同语义的解析与比较，保证合并视图的 `since` 语义统一。
// =============================================================================

/** C# `DateTime.Ticks` 每毫秒的刻度数（100ns 单位） */
const TICKS_PER_MS = 10_000;

/** 0001-01-01T00:00:00Z 与 1970-01-01T00:00:00Z 之间的毫秒数 */
const TICKS_EPOCH_OFFSET_MS = 62_135_596_800_000;

/** 纯整数字符串（桥侧 long.TryParse 的等价形态） */
const INTEGER_STRING_PATTERN = /^[+-]?\d+$/;

/**
 * `since` → epoch 毫秒；缺省 / 非法 / 非正数 → `null`（不过滤）。
 * 数字与纯整数字符串按 UTC Ticks 解释；其余字符串按 ISO 8601 / 可解析日期解释。
 */
export function parseSinceToEpochMs(since: string | number | undefined): number | null {
  if (since === undefined) {
    return null;
  }
  if (typeof since === "number") {
    return ticksToEpochMs(since);
  }
  const trimmed = since.trim();
  if (trimmed === "") {
    return null;
  }
  if (INTEGER_STRING_PATTERN.test(trimmed)) {
    return ticksToEpochMs(Number.parseInt(trimmed, 10));
  }
  const parsed = Date.parse(trimmed);
  return Number.isNaN(parsed) ? null : parsed;
}

function ticksToEpochMs(ticks: number): number | null {
  if (!Number.isFinite(ticks) || ticks <= 0) {
    return null;
  }
  return ticks / TICKS_PER_MS - TICKS_EPOCH_OFFSET_MS;
}

/**
 * ISO 8601 → epoch 毫秒；非法 → `null`。
 * 兼容 C# 往返格式（7 位小数；`Date.parse` 截断到毫秒）。
 */
export function isoToEpochMs(iso: string): number | null {
  const parsed = Date.parse(iso);
  return Number.isNaN(parsed) ? null : parsed;
}

/**
 * 只保留 `lastTs` **严格晚于** `sinceMs` 的组（`since` 独占，与桥侧一致）。
 * `sinceMs` 为 `null` → 返回入参的浅副本（不过滤）；`lastTs` 非法的组视为最旧、被排除。
 */
export function filterGroupsSince<T extends { lastTs: string }>(
  groups: readonly T[],
  sinceMs: number | null,
): T[] {
  if (sinceMs === null) {
    return [...groups];
  }
  return groups.filter((group) => {
    const lastMs = isoToEpochMs(group.lastTs);
    return lastMs !== null && lastMs > sinceMs;
  });
}
