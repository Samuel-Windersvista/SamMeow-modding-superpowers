// =============================================================================
// LogWatchService：MCP 侧日志观测面（服务器 tail + fatal 通道）
//
// 组成：
//   ServerLogTailer（spt / kestrel / requests）→ LogSummaryAggregator（server 聚合器）
//   FatalLogTailer（BepInEx/ErrorLog.log）    → LogSummaryAggregator（fatal 聚合器）
//
// **惰性刷新**（对齐 ADR-0007 拉取模型，不引入后台定时器）：`snapshot()` 时若距上次
// 刷新 ≥ `intervalMs` 才读增量，否则返回缓存。并发调用共享同一次刷新（避免重复计入）。
//
// 两个聚合器刻意分开：崩溃栈与服务器日志即使归一化键相同也应保留各自 source
// （`fatal` vs `server:<文件名>`），故不做跨通道合并；`overflowDropped` 为两者合计。
//
// 所有文件系统失败都在 tailer 内静默降级，本层绝不抛出。
// =============================================================================

import type { AggregatedLogGroup } from "./log-aggregator.js";
import { LogSummaryAggregator, compareLogGroups } from "./log-aggregator.js";
import {
  FatalLogTailer,
  ServerLogTailer,
  type LogWatchChannelStatus,
} from "./log-tail.js";

/** 日志观测视图（服务器组 + fatal 组 + 各自通道可用性；桥组由工具层另行合并） */
export interface LogWatchSnapshot {
  groups: AggregatedLogGroup[];
  /** MCP 侧累计溢出淘汰组数（server + fatal 合计） */
  overflowDropped: number;
  /** 服务器日志通道可用性（路径 / 目录层探测） */
  server: LogWatchChannelStatus;
  /** fatal 通道可用性（路径 / 文件层探测） */
  fatal: LogWatchChannelStatus;
}

/** 工具层依赖的最小接缝（测试注入 fake） */
export interface LogWatchSource {
  snapshot(): Promise<LogWatchSnapshot>;
}

/**
 * 空实现（未接入日志观测时的缺省；等价于「无服务器 / fatal 组」）。
 * 两个通道都报 `path_unresolved`（未配置观测面），使「没有数据」与「通道不可用」
 * 在输出里可区分，不留静默降级。
 */
export const EMPTY_LOG_WATCH: LogWatchSource = {
  snapshot: async () => ({
    groups: [],
    overflowDropped: 0,
    server: { available: false, reason: "path_unresolved" },
    fatal: { available: false, reason: "path_unresolved" },
  }),
};

export interface LogWatchServiceOptions {
  /** 日志根目录（其下 spt / kestrel / requests 子目录） */
  logsRoot: string;
  /** `BepInEx/ErrorLog.log` 路径；缺省 → fatal 通道静默为空 */
  errorLogPath?: string;
  /** 惰性刷新间隔（ms，配置层钳制 5000–10000） */
  intervalMs: number;
  /** 服务器日志采集最小级别（默认 warning） */
  minLevel?: string;
  /** 聚合组上限（默认 500） */
  maxGroups?: number;
  /** 时钟注入（测试用） */
  now?: () => Date;
  /** tailer 注入（测试用；缺省按路径构造） */
  serverTailer?: ServerLogTailer;
  fatalTailer?: FatalLogTailer;
}

export class LogWatchService implements LogWatchSource {
  private readonly intervalMs: number;
  private readonly now: () => Date;
  private readonly serverTailer: ServerLogTailer;
  private readonly fatalTailer: FatalLogTailer;
  private readonly serverAggregator: LogSummaryAggregator;
  private readonly fatalAggregator: LogSummaryAggregator;

  private cached: LogWatchSnapshot | null = null;
  private lastRefreshMs = Number.NEGATIVE_INFINITY;
  private pending: Promise<LogWatchSnapshot> | null = null;

  constructor(options: LogWatchServiceOptions) {
    this.intervalMs = options.intervalMs;
    this.now = options.now ?? (() => new Date());
    this.serverTailer =
      options.serverTailer ??
      new ServerLogTailer({
        logsRoot: options.logsRoot,
        minLevel: options.minLevel,
        now: this.now,
      });
    this.fatalTailer =
      options.fatalTailer ?? new FatalLogTailer({ path: options.errorLogPath, now: this.now });
    this.serverAggregator = new LogSummaryAggregator(options.maxGroups);
    this.fatalAggregator = new LogSummaryAggregator(options.maxGroups);
  }

  /** 惰性刷新后的聚合视图（间隔内返回同一缓存对象） */
  async snapshot(): Promise<LogWatchSnapshot> {
    if (this.cached !== null && this.now().getTime() - this.lastRefreshMs < this.intervalMs) {
      return this.cached;
    }
    if (this.pending !== null) {
      return this.pending;
    }
    this.pending = this.refresh().finally(() => {
      this.pending = null;
    });
    return this.pending;
  }

  private async refresh(): Promise<LogWatchSnapshot> {
    const serverEntries = await this.serverTailer.poll();
    for (const entry of serverEntries) {
      this.serverAggregator.observe(entry);
    }
    const serverStatus = await this.serverTailer.probe();

    const fatalEntries = await this.fatalTailer.poll();
    for (const entry of fatalEntries) {
      this.fatalAggregator.observe(entry);
    }
    const fatalStatus = await this.fatalTailer.probe();

    this.lastRefreshMs = this.now().getTime();
    const serverSnapshot = this.serverAggregator.snapshot(null);
    const fatalSnapshot = this.fatalAggregator.snapshot(null);

    // 通道不可用时**不暴露其组**：输出里「available=false + 空组」保持一致，
    // 避免把陈旧组误读成「该通道正在产出数据」；溢出计数为历史累计，不受影响。
    this.cached = {
      groups: [
        ...(serverStatus.available ? serverSnapshot.groups : []),
        ...(fatalStatus.available ? fatalSnapshot.groups : []),
      ].sort(compareLogGroups),
      overflowDropped: serverSnapshot.overflowDropped + fatalSnapshot.overflowDropped,
      server: serverStatus,
      fatal: fatalStatus,
    };
    return this.cached;
  }
}
