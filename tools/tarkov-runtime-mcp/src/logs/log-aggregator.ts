// =============================================================================
// MCP 侧日志聚合器（桥侧 `LogSummaryStore` 的 TS 移植）
//
// 与桥侧同构：归一化键聚合、count 覆盖**全部**观测（不随任何环形缓冲淘汰）、
// level 取组内最高严重度、source 取首个来源、sampleText 取首个原文、
// firstTs/lastTs 取极值；组数上限（默认 500）后按「最久未更新」淘汰并累计
// `overflowDropped`；组序固定为 count 降序 → lastTs 降序 → key 序升序。
//
// 与桥侧的差异：时间用 epoch 毫秒比较（桥侧用 UTC Ticks），输出 `ts` 保留
// 观测到的原始 ISO 字符串（不重新格式化，避免精度与格式漂移）。
// =============================================================================

import type { LogEntry } from "./log-entry.js";
import { logLevelRank } from "./log-level.js";
import { isoToEpochMs } from "./log-since.js";
import { normalizeLogText } from "./log-normalizer.js";

/** 聚合组数上限（与桥侧 `LogSummaryStore.DefaultMaxGroups` 一致） */
export const MAX_LOG_GROUPS = 500;

/** 聚合组（字段序稳定，与桥侧 /logs/summary 的 groups[] 同构） */
export interface AggregatedLogGroup {
  /** 归一化文本（见 log-normalizer） */
  key: string;
  /** 组内最高严重度级别名 */
  level: string;
  /** 首个来源 */
  source: string;
  /** 全部观测条数 */
  count: number;
  /** 组内最早时刻（UTC ISO 8601） */
  firstTs: string;
  /** 组内最新时刻（UTC ISO 8601） */
  lastTs: string;
  /** 首个原始文本样本 */
  sampleText: string;
}

/** 聚合读取结果 */
export interface AggregatedLogSnapshot {
  groups: AggregatedLogGroup[];
  /** 累计被溢出淘汰的组数（单调递增，不随 since 重置） */
  overflowDropped: number;
}

/** 组内可变状态（仅本模块读写） */
interface GroupState {
  level: string;
  source: string;
  count: number;
  firstMs: number;
  lastMs: number;
  firstTs: string;
  lastTs: string;
  sampleText: string;
}

/** 组序：count 降序 → lastTs 降序 → key 序升序（与桥侧一致） */
export function compareLogGroups(left: AggregatedLogGroup, right: AggregatedLogGroup): number {
  if (left.count !== right.count) {
    return left.count > right.count ? -1 : 1;
  }
  const leftMs = isoToEpochMs(left.lastTs) ?? 0;
  const rightMs = isoToEpochMs(right.lastTs) ?? 0;
  if (leftMs !== rightMs) {
    return leftMs > rightMs ? -1 : 1;
  }
  if (left.key === right.key) {
    return 0;
  }
  return left.key < right.key ? -1 : 1;
}

export class LogSummaryAggregator {
  private readonly groups = new Map<string, GroupState>();
  private readonly maxGroups: number;
  private overflowDropped = 0;

  constructor(maxGroups: number = MAX_LOG_GROUPS) {
    this.maxGroups = maxGroups < 1 ? 1 : Math.trunc(maxGroups);
  }

  /** 观测一条日志（纯内存、零 I/O） */
  observe(entry: LogEntry): void {
    const key = normalizeLogText(entry.text);
    const level = entry.level;
    const observedMs = isoToEpochMs(entry.ts) ?? 0;

    const existing = this.groups.get(key);
    if (existing !== undefined) {
      existing.count += 1;
      if (observedMs < existing.firstMs) {
        existing.firstMs = observedMs;
        existing.firstTs = entry.ts;
      }
      if (observedMs > existing.lastMs) {
        existing.lastMs = observedMs;
        existing.lastTs = entry.ts;
      }
      // 升级为观测到的最高严重度：组内出现 error 时不得仍标 warning。
      if (logLevelRank(level) < logLevelRank(existing.level)) {
        existing.level = level;
      }
      return;
    }

    if (this.groups.size >= this.maxGroups) {
      this.evictLeastRecentlyUpdated();
    }

    this.groups.set(key, {
      level,
      source: entry.source,
      count: 1,
      firstMs: observedMs,
      lastMs: observedMs,
      firstTs: entry.ts,
      lastTs: entry.ts,
      sampleText: entry.text,
    });
  }

  /**
   * 组快照（不可变副本），`sinceMs` 为 `null` 或 ≤0 时不过滤；
   * 过滤语义为 `lastTs > sinceMs`（since 独占）。
   */
  snapshot(sinceMs: number | null = null): AggregatedLogSnapshot {
    const groups: AggregatedLogGroup[] = [];
    for (const [key, state] of this.groups) {
      if (sinceMs !== null && sinceMs > 0 && state.lastMs <= sinceMs) {
        continue;
      }
      groups.push({
        key,
        level: state.level,
        source: state.source,
        count: state.count,
        firstTs: state.firstTs,
        lastTs: state.lastTs,
        sampleText: state.sampleText,
      });
    }
    groups.sort(compareLogGroups);
    return { groups, overflowDropped: this.overflowDropped };
  }

  /** 溢出淘汰：最久未更新（lastTs 最小）优先；并列取 key 序最小者（确定性） */
  private evictLeastRecentlyUpdated(): void {
    let victimKey: string | null = null;
    let victimLastMs = Number.MAX_SAFE_INTEGER;

    for (const [key, state] of this.groups) {
      if (
        victimKey === null ||
        state.lastMs < victimLastMs ||
        (state.lastMs === victimLastMs && key < victimKey)
      ) {
        victimKey = key;
        victimLastMs = state.lastMs;
      }
    }

    if (victimKey !== null) {
      this.groups.delete(victimKey);
      this.overflowDropped += 1;
    }
  }
}
