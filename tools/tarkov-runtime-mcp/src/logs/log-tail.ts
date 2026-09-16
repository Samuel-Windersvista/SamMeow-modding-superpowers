// =============================================================================
// 日志 tail 器：{path → offset} 字节游标增量读取
//
// 设计要点：
//   - **字节偏移**（非字符偏移）：UTF-8 多字节字符按 Buffer 读取后整体解码；
//     只消费到最后一个完整换行，未完成尾行留待下次（防截断重复）；
//   - 文件变短（轮转 / 截断）→ 偏移重置 0；
//   - 新文件自动纳入（每次 poll 重新列目录）；
//   - 目录 / 文件缺失、读取失败一律静默降级为空，**绝不抛出**（沿用 log-reader 惯例）。
// =============================================================================

import { open, readdir, stat, type FileHandle } from "node:fs/promises";
import { basename, join } from "node:path";

import type { LogEntry } from "./log-entry.js";
import { DEFAULT_LOG_MIN_LEVEL } from "./log-level.js";
import { parseErrorLogEntries, parseServerLogEntries } from "./log-parse.js";

/** 服务器日志默认子目录（SPT 5.0 `user/logs/` 下） */
export const DEFAULT_SERVER_LOG_SUBDIRS = ["spt", "kestrel", "requests"] as const;

/** 日志文件后缀（只有 `.log` 纳入） */
const LOG_FILE_SUFFIX = ".log";

/**
 * 通道不可用原因（机器可读，snake_case）：
 *   - `logs_root_missing`：日志根不存在 / 不是目录；
 *   - `no_log_dirs`：日志根存在但配置的子目录一个都不存在；
 *   - `path_unresolved`：路径未配置 / 无法推导（fatal 通道典型：未设 env 且无 SPT_DIR）；
 *   - `file_missing`：路径已解析但文件不存在 / 不是文件。
 */
export type LogWatchUnavailableReason =
  | "logs_root_missing"
  | "no_log_dirs"
  | "path_unresolved"
  | "file_missing";

/** 通道可用性（判别联合：available=true 时无 reason） */
export type LogWatchChannelStatus =
  | { available: true }
  | { available: false; reason: LogWatchUnavailableReason };


/**
 * 单文件字节游标：读取自上次偏移以来的**完整行**文本。
 * 半行不推进偏移（下次连同新数据一起返回），保证「不重不漏」。
 */
class LogFileCursor {
  private readonly offsets = new Map<string, number>();

  /** 读取新增完整行；无新数据 / 无完整行 / 读取失败 → `null` */
  async readNewLines(path: string): Promise<string | null> {
    let handle: FileHandle | null = null;
    try {
      const info = await stat(path);
      if (!info.isFile()) {
        return null;
      }
      const size = info.size;
      let offset = this.offsets.get(path) ?? 0;
      if (size < offset) {
        // 截断 / 轮转：重置偏移从头读
        offset = 0;
      }
      if (size === offset) {
        return null;
      }

      handle = await open(path, "r");
      const length = size - offset;
      const buffer = Buffer.alloc(length);
      const { bytesRead } = await handle.read(buffer, 0, length, offset);
      if (bytesRead <= 0) {
        return null;
      }

      const text = buffer.subarray(0, bytesRead).toString("utf8");
      const lastNewline = text.lastIndexOf("\n");
      if (lastNewline < 0) {
        // 无完整行：偏移不推进（半行留待下次）
        return null;
      }

      const complete = text.slice(0, lastNewline + 1);
      this.offsets.set(path, offset + Buffer.byteLength(complete, "utf8"));
      return complete;
    } catch {
      return null;
    } finally {
      if (handle !== null) {
        await handle.close().catch(() => undefined);
      }
    }
  }
}

export interface ServerLogTailerOptions {
  /** 日志根目录（其下为 spt / kestrel / requests 子目录） */
  logsRoot: string;
  /** 子目录名（默认 spt / kestrel / requests） */
  subdirs?: readonly string[];
  /** 采集最小级别（默认 warning，与桥侧采集阈值语义一致） */
  minLevel?: string;
  /** 观测时刻来源（测试注入确定性时钟） */
  now?: () => Date;
}

/** SPT server 日志（spt / kestrel / requests）增量 tail */
export class ServerLogTailer {
  private readonly logsRoot: string;
  private readonly subdirs: readonly string[];
  private readonly minLevel: string;
  private readonly now: () => Date;
  private readonly cursor = new LogFileCursor();

  constructor(options: ServerLogTailerOptions) {
    this.logsRoot = options.logsRoot;
    this.subdirs = options.subdirs ?? DEFAULT_SERVER_LOG_SUBDIRS;
    this.minLevel = options.minLevel ?? DEFAULT_LOG_MIN_LEVEL;
    this.now = options.now ?? (() => new Date());
  }

  /** 增量读取一轮；目录缺失 / 读取失败静默跳过 */
  async poll(): Promise<LogEntry[]> {
    const entries: LogEntry[] = [];
    const fallbackTs = this.now().toISOString();

    for (const subdir of this.subdirs) {
      for (const file of await this.listLogFiles(join(this.logsRoot, subdir))) {
        const content = await this.cursor.readNewLines(file);
        if (content === null) {
          continue;
        }
        entries.push(
          ...parseServerLogEntries(content, {
            source: `server:${basename(file)}`,
            minLevel: this.minLevel,
            fallbackTs,
          }),
        );
      }
    }

    return entries;
  }

  /** 列目录内的 `.log` 文件（按文件名排序，保证跨文件顺序确定）；失败 → 空数组 */
  private async listLogFiles(dir: string): Promise<string[]> {
    try {
      const names = await readdir(dir);
      return names
        .filter((name) => name.toLowerCase().endsWith(LOG_FILE_SUFFIX))
        .sort()
        .map((name) => join(dir, name));
    } catch {
      return [];
    }
  }

  /**
   * 通道可用性探测：只看路径 / 目录是否解析得到（不读内容、不动游标）。
   * 可用性与「是否有日志文件」无关——子目录存在即视为可用（无文件时 poll 返回空）。
   */
  async probe(): Promise<LogWatchChannelStatus> {
    try {
      const info = await stat(this.logsRoot);
      if (!info.isDirectory()) {
        return { available: false, reason: "logs_root_missing" };
      }
    } catch {
      return { available: false, reason: "logs_root_missing" };
    }

    for (const subdir of this.subdirs) {
      try {
        const info = await stat(join(this.logsRoot, subdir));
        if (info.isDirectory()) {
          return { available: true };
        }
      } catch {
        // 单个子目录缺失：继续看下一个
      }
    }
    return { available: false, reason: "no_log_dirs" };
  }
}

export interface FatalLogTailerOptions {
  /** `BepInEx/ErrorLog.log` 路径；缺省 / 不存在 → 静默返回空 */
  path?: string;
  /** 观测时刻来源（测试注入确定性时钟） */
  now?: () => Date;
}

/**
 * 进程级崩溃通道：以同款字节游标监视 `BepInEx/ErrorLog.log`，
 * 产出 `source=fatal` 组（整文件视为错误输出，不做级别过滤）。
 */
export class FatalLogTailer {
  private readonly path?: string;
  private readonly now: () => Date;
  private readonly cursor = new LogFileCursor();

  constructor(options: FatalLogTailerOptions = {}) {
    this.path = options.path;
    this.now = options.now ?? (() => new Date());
  }

  async poll(): Promise<LogEntry[]> {
    if (!this.path) {
      return [];
    }
    const content = await this.cursor.readNewLines(this.path);
    if (content === null) {
      return [];
    }
    return parseErrorLogEntries(content, { fallbackTs: this.now().toISOString() });
  }

  /** 通道可用性探测：未配置路径 → path_unresolved；路径不是存在的文件 → file_missing */
  async probe(): Promise<LogWatchChannelStatus> {
    if (!this.path) {
      return { available: false, reason: "path_unresolved" };
    }
    try {
      const info = await stat(this.path);
      if (!info.isFile()) {
        return { available: false, reason: "file_missing" };
      }
    } catch {
      return { available: false, reason: "file_missing" };
    }
    return { available: true };
  }
}
