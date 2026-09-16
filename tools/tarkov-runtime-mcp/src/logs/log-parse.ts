// =============================================================================
// 日志行解析（服务器三目录 + fatal 通道）
//
// 服务器日志行格式（2026-09-16 采样，spt / kestrel / requests 三目录统一）：
//   [2026-09-14 13:58:00.159][Warning][SPTarkov.Server.X] message
//   - 级别名：Debug / Information / Warning / Error（+ Fatal）→ 归一为桥侧小写字面量；
//   - 时间戳无时区标记，按**本机本地时间**解释后转 UTC ISO（与桥侧 `ts` 同域可比）；
//   - 无前缀行（缩进异常栈）为**续行**，归入上一条目 text（\n 拼接）。
//
// fatal 通道（BepInEx `ErrorLog.log`）行格式：
//   [Error  :     Unity] message
//   - 条目边界为 `[级别  :` 前缀行；其余行为续行；
//   - 无时间戳 → 全部条目取观测时刻（fallbackTs）；
//   - 整文件视为错误输出，**不做级别过滤**（minLevel 传空串）。
//
// 未知级别名的条目：默认级别为空串 → 服务器通道按「未达阈值」排除
// （无法证明其达到 Warning+）；fatal 通道回退 `error`（文件本身即错误日志）。
// =============================================================================

import type { LogEntry } from "./log-entry.js";
import { DEFAULT_LOG_MIN_LEVEL, isAtLeastLevel, normalizeLogLevel } from "./log-level.js";

/** fatal 通道的来源标识（与桥侧区分：进程级崩溃栈） */
export const FATAL_LOG_SOURCE = "fatal";

/** fatal 通道未知级别名时的回退级别 */
export const FATAL_DEFAULT_LEVEL = "error";

/** `[ts][Level][Source] text` */
const SERVER_LINE_PATTERN = /^\[([^\]]+)\]\[([^\]]+)\]\[([^\]]+)\]\s?(.*)$/;

/** `[Level  : Source] text`（BepInEx 惯例：级别后带填充空格与冒号） */
const ERRORLOG_LINE_PATTERN = /^\[([A-Za-z]+)\s*:\s*([^\]]*)\]\s?(.*)$/;

/** `YYYY-MM-DD[ T]HH:mm:ss[.fraction]`（无时区标记） */
const LOCAL_TIMESTAMP_PATTERN =
  /^(\d{4})-(\d{2})-(\d{2})[ T](\d{2}):(\d{2}):(\d{2})(?:\.(\d{1,7}))?$/;

/** 单行解析结果（`ts` 为 null 表示该行无时间戳，由装配层回退观测时刻） */
export interface ParsedLogLine {
  ts: string | null;
  /** 归一化级别名；无法识别为 `""` */
  level: string;
  text: string;
}

/**
 * 行内本地时间 → UTC ISO 8601；非法 → `null`。
 * 小数位超过 3 位时截断到毫秒（C# 往返格式 7 位）。
 */
export function localTimestampToIsoUtc(raw: string): string | null {
  const match = LOCAL_TIMESTAMP_PATTERN.exec(raw.trim());
  if (!match) {
    return null;
  }
  const year = Number.parseInt(match[1], 10);
  const month = Number.parseInt(match[2], 10);
  const day = Number.parseInt(match[3], 10);
  const hour = Number.parseInt(match[4], 10);
  const minute = Number.parseInt(match[5], 10);
  const second = Number.parseInt(match[6], 10);
  const millis = match[7] ? Number.parseInt(match[7].slice(0, 3).padEnd(3, "0"), 10) : 0;

  // 显式范围校验：`new Date` 会静默滚动非法日期（如 09-32 → 10-02）。
  if (
    month < 1 ||
    month > 12 ||
    day < 1 ||
    day > 31 ||
    hour > 23 ||
    minute > 59 ||
    second > 59
  ) {
    return null;
  }

  const date = new Date(year, month - 1, day, hour, minute, second, millis);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  return date.toISOString();
}

/** 解析服务器日志行；非条目行（续行 / 空行）→ `null` */
export function parseServerLogLine(line: string): ParsedLogLine | null {
  const match = SERVER_LINE_PATTERN.exec(line);
  if (!match) {
    return null;
  }
  return {
    ts: localTimestampToIsoUtc(match[1]),
    level: normalizeLogLevel(match[2]),
    text: match[4],
  };
}

/** 解析 BepInEx ErrorLog 行；非条目行（栈续行）→ `null` */
export function parseErrorLogLine(line: string): ParsedLogLine | null {
  const match = ERRORLOG_LINE_PATTERN.exec(line);
  if (!match) {
    return null;
  }
  return {
    ts: null,
    level: normalizeLogLevel(match[1]),
    text: match[3],
  };
}

export interface BuildLogEntriesOptions {
  /** 来源标识（服务器日志 = `server:<文件名>`） */
  source: string;
  /** 最小级别（默认 warning）；空串 = 不过滤 */
  minLevel?: string;
  /** 无时间戳条目的回退时刻（UTC ISO）；缺省为当前时刻 */
  fallbackTs?: string;
  /** 级别无法识别时的回退级别（默认空串 = 不纳入） */
  defaultLevel?: string;
}

/**
 * 把日志文本装配为条目数组：
 *   - 续行（不匹配条目前缀）归入上一条目 text；首部续行直接跳过（不臆造条目）；
 *   - 条目间空行不并入（避免样本文本充斥空行）；
 *   - 按 `minLevel` 过滤（未知级别条目：回退级别为空串时按未达阈值排除）。
 */
export function buildLogEntries(
  content: string,
  parseLine: (line: string) => ParsedLogLine | null,
  options: BuildLogEntriesOptions,
): LogEntry[] {
  const minLevel = options.minLevel ?? DEFAULT_LOG_MIN_LEVEL;
  const fallbackTs = options.fallbackTs ?? new Date().toISOString();
  const defaultLevel = options.defaultLevel ?? "";

  const raw: ParsedLogLine[] = [];
  let current: ParsedLogLine | null = null;

  for (const line of stripByteOrderMark(content).split(/\r?\n/)) {
    const parsed = parseLine(line);
    if (parsed !== null) {
      current = parsed;
      raw.push(parsed);
      continue;
    }
    if (current === null || line.trim() === "") {
      continue;
    }
    current.text = `${current.text}\n${line}`;
  }

  const entries: LogEntry[] = [];
  for (const item of raw) {
    const level = item.level === "" ? defaultLevel : item.level;
    if (!isAtLeastLevel(level, minLevel)) {
      continue;
    }
    entries.push({
      ts: item.ts ?? fallbackTs,
      level,
      source: options.source,
      text: item.text,
    });
  }
  return entries;
}

export interface ServerLogParseOptions {
  /** 来源标识（`server:<文件名>`） */
  source: string;
  /** 最小级别（默认 warning） */
  minLevel?: string;
  /** 时间戳不可解析时的回退时刻（UTC ISO） */
  fallbackTs?: string;
}

/** 服务器日志文本 → 条目（默认仅 Warning 及以上） */
export function parseServerLogEntries(
  content: string,
  options: ServerLogParseOptions,
): LogEntry[] {
  return buildLogEntries(content, parseServerLogLine, {
    source: options.source,
    minLevel: options.minLevel,
    fallbackTs: options.fallbackTs,
  });
}

export interface ErrorLogParseOptions {
  /** 观测时刻（UTC ISO）：fatal 条目无行内时间戳，统一取它 */
  fallbackTs?: string;
  /** 未知级别名的回退级别（默认 error） */
  defaultLevel?: string;
}

/** fatal 通道（ErrorLog.log）文本 → 条目（不过滤级别） */
export function parseErrorLogEntries(
  content: string,
  options: ErrorLogParseOptions = {},
): LogEntry[] {
  return buildLogEntries(content, parseErrorLogLine, {
    source: FATAL_LOG_SOURCE,
    minLevel: "",
    fallbackTs: options.fallbackTs,
    defaultLevel: options.defaultLevel ?? FATAL_DEFAULT_LEVEL,
  });
}

/** 剥离 UTF-8 BOM（Windows 编辑器写入的日志首行常见） */
function stripByteOrderMark(content: string): string {
  return content.startsWith("\uFEFF") ? content.slice(1) : content;
}
