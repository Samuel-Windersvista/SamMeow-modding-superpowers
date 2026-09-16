// =============================================================================
// 日志级别归一化与严重度比较（桥侧 `LogWatchLevel` 的 TS 移植）
//
// 两侧级别字面量不同：
//   - 服务端日志（SPT / kestrel / requests）：`Debug|Information|Warning|Error`（+ `Fatal`）
//   - 桥侧 / BepInEx：`fatal|error|warning|message|info|debug`
// 统一归一为桥侧小写字面量（即 JSON `level` 字段的稳定取值）。
//
// 严重度排序由本模块显式给出：绝不比较字符串，也不依赖任何枚举数值
// （桥侧 BepInEx `LogLevel` 是 `[Flags]`，数值不反映严重度）。
// =============================================================================

/** 默认采集 / 聚合最小级别（与桥侧 `LogWatchMinLevel` 默认一致） */
export const DEFAULT_LOG_MIN_LEVEL = "warning";

/**
 * 合法级别名（小写，严重度降序）——桥侧 `LogWatchLevel` 的稳定字面量，
 * 也是 `/logs/recent?level=` 唯一识别的取值集合（大小写不敏感）。
 * 服务端日志字面量（`Information` / `Debug` / …）经 `normalizeLogLevel` 映射到本集合。
 */
export const LOG_LEVEL_NAMES = ["fatal", "error", "warning", "message", "info", "debug"] as const;

/** 未知级别的排名（永不满足任何有效阈值） */
const UNKNOWN_RANK = Number.MAX_SAFE_INTEGER;

/** 级别名（小写）→ 排名：0 = 最严重（fatal）… 5 = 最轻（debug） */
const LEVEL_RANKS: Readonly<Record<string, number>> = {
  fatal: 0,
  error: 1,
  warning: 2,
  message: 3,
  info: 4,
  debug: 5,
};

/**
 * 级别名归一化：去空白 + 大小写不敏感；未知 / 空 → `""`（视为「无有效级别」）。
 * 兼容服务端日志字面量（`Information` → `info`、`Critical` → `fatal`）。
 */
export function normalizeLogLevel(value: string): string {
  const trimmed = value.trim().toLowerCase();
  switch (trimmed) {
    case "fatal":
    case "critical":
      return "fatal";
    case "error":
      return "error";
    case "warning":
    case "warn":
      return "warning";
    case "message":
      return "message";
    case "information":
    case "info":
      return "info";
    case "debug":
      return "debug";
    default:
      return "";
  }
}

/**
 * 查询侧级别名**严格**解析（工具入参 / `/logs/recent?level=` 的合法值域）：
 * 只接受 {@link LOG_LEVEL_NAMES} 中的取值（去空白 + 大小写不敏感），归一为小写；
 * 未知取值 → `null`（由调用方拒绝，**绝不**静默当作「不过滤」）。
 *
 * 与 {@link normalizeLogLevel} 的分工：后者是**宽松**归一化，用于解析日志行与采集阈值
 * （额外接受服务端字面量 `Information` / `Debug` / `Critical`）；本函数是**严格**值域校验，
 * 因为桥侧 `/logs/recent?level=` 只识别 BepInEx 六个级别名——传 `Information` 在桥侧会
 * 落到「未知 → 不过滤」，故必须在 MCP 侧拦下。
 */
export function parseLogLevelName(value: string): string | null {
  const trimmed = value.trim().toLowerCase();
  return (LOG_LEVEL_NAMES as readonly string[]).includes(trimmed) ? trimmed : null;
}

/** 严重度排名：0 = 最严重（fatal）… 5 = 最轻（debug）；未知 / 空 → 最大排名 */
export function logLevelRank(level: string): number {
  const normalized = normalizeLogLevel(level);
  return normalized === "" ? UNKNOWN_RANK : LEVEL_RANKS[normalized];
}

/**
 * `level` 是否达到（不轻于）`minLevel` 阈值。
 * 阈值为空 / 未知 → 无过滤（一律纳入）；条目级别未知 → 不纳入（无法证明达到阈值）。
 */
export function isAtLeastLevel(level: string, minLevel: string): boolean {
  const normalizedMin = normalizeLogLevel(minLevel);
  if (normalizedMin === "") {
    return true;
  }
  return logLevelRank(level) <= LEVEL_RANKS[normalizedMin];
}
