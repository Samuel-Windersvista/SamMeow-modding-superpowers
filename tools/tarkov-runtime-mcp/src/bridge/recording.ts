// =============================================================================
// T07 录制：RecordingBridgeConnection
//
// 装饰任意 BridgeConnection，把每次调用写一行 JSONL（追加写、逐行 flush）：
//   {"ts":"<ISO>","method":"getInfo|getRaidStatus|getRaidPlayer|getRaidBots|getRaidEvents",
//    "args":<入参|null>,"ok":<bool>,"result":<响应或错误信息>}
//
// 默认关：仅当配置提供路径（env TARKOV_RUNTIME_MCP_BRIDGE_RECORD）时由
// createRuntime 包装默认/注入连接。录制是旁路能力：写盘失败只告警到 stderr，
// 不中断工具调用。
// =============================================================================

import { appendFileSync, mkdirSync } from "node:fs";
import { dirname } from "node:path";

import type {
  BridgeConnection,
  BridgeInfo,
  BridgeMethod,
  BridgeRaidBotsResult,
  BridgeRaidEventsResult,
  BridgeRaidPlayerResult,
  BridgeRaidStatusResult,
} from "./connection.js";

/** 单行录制条目（JSONL 行结构） */
export interface RecordingEntry {
  /** ISO 时间戳 */
  ts: string;
  method: BridgeMethod;
  /** 入参（getRaidBots 为 { detail }；getRaidEvents 为 { since, limit }；其余为 null） */
  args: unknown;
  /** 调用是否成功 */
  ok: boolean;
  /** 成功时为响应体；失败时为 { error } */
  result: unknown;
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

export interface RecordingBridgeOptions {
  /** 时间戳来源（测试注入确定性时钟） */
  now?: () => Date;
}

export class RecordingBridgeConnection implements BridgeConnection {
  private readonly now: () => Date;
  private ensuredDir = false;

  constructor(
    private readonly inner: BridgeConnection,
    private readonly path: string,
    options: RecordingBridgeOptions = {},
  ) {
    this.now = options.now ?? (() => new Date());
  }

  async getInfo(): Promise<BridgeInfo> {
    return this.record("getInfo", null, () => this.inner.getInfo());
  }

  async getRaidStatus(): Promise<BridgeRaidStatusResult> {
    return this.record("getRaidStatus", null, () => this.inner.getRaidStatus());
  }

  async getRaidPlayer(): Promise<BridgeRaidPlayerResult> {
    return this.record("getRaidPlayer", null, () => this.inner.getRaidPlayer());
  }

  async getRaidBots(detail: boolean): Promise<BridgeRaidBotsResult> {
    return this.record("getRaidBots", { detail }, () => this.inner.getRaidBots(detail));
  }

  async getRaidEvents(since?: number, limit?: number): Promise<BridgeRaidEventsResult> {
    return this.record("getRaidEvents", { since: since ?? null, limit: limit ?? null }, () =>
      this.inner.getRaidEvents(since, limit),
    );
  }

  /** 调用内层并把结果（或错误）记录为一行 JSONL；错误原样抛出 */
  private async record<T>(
    method: BridgeMethod,
    args: unknown,
    call: () => Promise<T>,
  ): Promise<T> {
    let result: T;
    try {
      result = await call();
    } catch (error) {
      this.append({
        ts: this.now().toISOString(),
        method,
        args,
        ok: false,
        result: { error: errorMessage(error) },
      });
      throw error;
    }
    this.append({ ts: this.now().toISOString(), method, args, ok: true, result });
    return result;
  }

  /** 追加一行并 flush；写盘失败降级为 stderr 告警，不中断调用 */
  private append(entry: RecordingEntry): void {
    try {
      if (!this.ensuredDir) {
        mkdirSync(dirname(this.path), { recursive: true });
        this.ensuredDir = true;
      }
      appendFileSync(this.path, `${JSON.stringify(entry)}\n`, "utf8");
    } catch (error) {
      process.stderr.write(
        `[tarkov-runtime-mcp] 录制写入失败（${this.path}）：${errorMessage(error)}\n`,
      );
    }
  }
}
