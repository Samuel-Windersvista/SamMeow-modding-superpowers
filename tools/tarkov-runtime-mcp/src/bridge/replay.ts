// =============================================================================
// T07 回放：从录制 JSONL 构造 BridgeConnection
//
// 供回归断言使用：录制文件驱动 fake 连接，工具层输出应与录制逐字节一致
// （无时间戳噪声）。回放语义：
//   - 按 method（getRaidBots 另按 detail 入参）分桶，桶内按记录顺序消费；
//   - 桶耗尽后复用最后一条（getInfo 会被每个 raid 工具各调用一次，复用可
//     避免同一录制需要重复条目）；
//   - 桶内无匹配条目 -> 抛 BridgeUnreachableError（录制与调用不匹配）；
//   - 记录为失败条目 -> 重放时抛 BridgeUnreachableError（携带原始错误信息）。
// =============================================================================

import { readFileSync } from "node:fs";

import {
  BRIDGE_METHODS,
  BridgeUnreachableError,
  type BridgeConnection,
  type BridgeInfo,
  type BridgeMethod,
  type BridgeRaidBotsResult,
  type BridgeRaidEventsResult,
  type BridgeRaidPlayerResult,
  type BridgeRaidStatusResult,
} from "./connection.js";
import type { RecordingEntry } from "./recording.js";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function detailOf(args: unknown): boolean {
  return isRecord(args) && args.detail === true;
}

/** 解析单行录制；结构非法时抛 Error（带行号） */
export function parseRecordingLine(line: string, lineNumber: number): RecordingEntry {
  let parsed: unknown;
  try {
    parsed = JSON.parse(line);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    throw new Error(`录制文件第 ${lineNumber} 行不是合法 JSON：${message}`);
  }
  if (!isRecord(parsed)) {
    throw new Error(`录制文件第 ${lineNumber} 行不是对象`);
  }
  const { ts, method, args, ok, result } = parsed;
  if (typeof ts !== "string") {
    throw new Error(`录制文件第 ${lineNumber} 行缺少合法的 ts 字段`);
  }
  if (typeof method !== "string" || !BRIDGE_METHODS.includes(method as BridgeMethod)) {
    throw new Error(`录制文件第 ${lineNumber} 行 method 非法：${String(method)}`);
  }
  if (typeof ok !== "boolean") {
    throw new Error(`录制文件第 ${lineNumber} 行缺少合法的 ok 字段`);
  }
  if (!("result" in parsed)) {
    throw new Error(`录制文件第 ${lineNumber} 行缺少 result 字段`);
  }
  return {
    ts,
    method: method as BridgeMethod,
    args: args ?? null,
    ok,
    result,
  };
}

/** 读取 JSONL 录制文件为条目数组（跳过空行） */
export function loadRecording(path: string): RecordingEntry[] {
  const text = readFileSync(path, "utf8");
  const entries: RecordingEntry[] = [];
  const lines = text.split(/\r?\n/);
  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index].trim();
    if (!line) continue;
    entries.push(parseRecordingLine(line, index + 1));
  }
  return entries;
}

class ReplayBridgeConnection implements BridgeConnection {
  private readonly cursors = new Map<string, number>();

  constructor(private readonly entries: RecordingEntry[]) {}

  async getInfo(): Promise<BridgeInfo> {
    return this.next<BridgeInfo>("getInfo", null);
  }

  async getRaidStatus(): Promise<BridgeRaidStatusResult> {
    return this.next<BridgeRaidStatusResult>("getRaidStatus", null);
  }

  async getRaidPlayer(): Promise<BridgeRaidPlayerResult> {
    return this.next<BridgeRaidPlayerResult>("getRaidPlayer", null);
  }

  async getRaidBots(detail: boolean): Promise<BridgeRaidBotsResult> {
    return this.next<BridgeRaidBotsResult>("getRaidBots", { detail });
  }

  async getRaidEvents(since?: number, limit?: number): Promise<BridgeRaidEventsResult> {
    // 事件按 method 分桶顺序消费（since/limit 不入桶键：增量语义由录制顺序表达）
    return this.next<BridgeRaidEventsResult>("getRaidEvents", {
      since: since ?? null,
      limit: limit ?? null,
    });
  }

  private next<T>(method: BridgeMethod, args: unknown): T {
    const matching = this.entries.filter((entry) => {
      if (entry.method !== method) return false;
      if (method !== "getRaidBots") return true;
      return detailOf(entry.args) === detailOf(args);
    });
    if (matching.length === 0) {
      throw new BridgeUnreachableError(`回放记录缺少 ${method} 条目`);
    }
    const key = method === "getRaidBots" ? `${method}:${detailOf(args)}` : method;
    const cursor = this.cursors.get(key) ?? 0;
    const entry = matching[Math.min(cursor, matching.length - 1)];
    this.cursors.set(key, cursor + 1);
    if (!entry.ok) {
      throw new BridgeUnreachableError(
        `回放记录中 ${method} 为失败条目：${String(
          isRecord(entry.result) ? entry.result.error : entry.result,
        )}`,
      );
    }
    return entry.result as T;
  }
}

/** 以条目数组构造回放连接 */
export function createReplayConnection(entries: RecordingEntry[]): BridgeConnection {
  return new ReplayBridgeConnection(entries);
}

/** 从 JSONL 录制文件构造回放连接 */
export function loadReplayConnection(path: string): BridgeConnection {
  return createReplayConnection(loadRecording(path));
}
