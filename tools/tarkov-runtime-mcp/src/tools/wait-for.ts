// =============================================================================
// tarkov_wait_for
//
// 对任意工具结果轮询求值谓词，直到满足或超时：
//   - 满足：返回求值结果与耗时（attempts / elapsedMs / value）
//   - 超时：返回结构化 WAIT_TIMEOUT（谓词 / 最后观察值 / 耗时）
//   - 首轮 bridge 缺席（BRIDGE_UNREACHABLE / CLIENT_BRIDGE_NOT_INSTALLED）：
//     立即返回该错误信封（T06），不把环境问题误报成 WAIT_TIMEOUT
//
// 谓词对目标工具的 data 求值（目标工具失败时对错误信封求值），因此不绑定
// 特定工具；raid.* 工具就位后天然适用。
// =============================================================================

import { z } from "zod";

import { evaluatePredicate, parsePredicate } from "../predicate/predicate.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../types.js";
import type { ToolHandler } from "./server-status.js";

/** 目标工具调用签名（复用 dispatcher.invoke） */
export type ToolCaller = (name: string, args: Record<string, unknown>) => Promise<Envelope>;

/** 默认超时上限：30s */
export const DEFAULT_WAIT_TIMEOUT_MS = 30_000;
/** 默认轮询间隔：500ms */
export const DEFAULT_WAIT_INTERVAL_MS = 500;
/** 允许的最大超时上限：5min（可经工厂选项覆盖） */
export const MAX_WAIT_TIMEOUT_MS = 300_000;

/**
 * 首轮观察命中这些错误码时立即返回该错误信封（T06）。
 *
 * 语义：bridge 缺席属于环境问题，不是「条件尚未满足」，继续轮询到超时会把它
 * 误报成 WAIT_TIMEOUT，掩盖真实排障路径。故首次观测即短路。
 *
 * `NOT_IN_RAID` 明确不在其中：「等进 raid / 等 bot 生成」是合法用法，必须继续轮询。
 */
export const WAIT_FAST_FAIL_ERROR_CODES: readonly string[] = [
  RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE,
  RUNTIME_ERROR_CODES.CLIENT_BRIDGE_NOT_INSTALLED,
];

const FAST_FAIL_CODES = new Set<string>(WAIT_FAST_FAIL_ERROR_CODES);

export const WaitForInput = z
  .object({
    /** 目标工具名（如 tarkov_server_status） */
    tool: z.string().min(1),
    /** 目标工具入参 */
    args: z.record(z.unknown()).optional(),
    /** 谓词表达式：`<字段路径> <运算符> <值>` */
    predicate: z.string().min(1),
    /** 超时上限（ms），缺省用默认值 */
    timeoutMs: z.number().int().positive().optional(),
    /** 轮询间隔（ms），缺省用默认值 */
    intervalMs: z.number().int().positive().optional(),
  })
  .strict();

export interface WaitForToolOptions {
  /** 默认超时上限（ms） */
  defaultTimeoutMs?: number;
  /** 默认轮询间隔（ms） */
  defaultIntervalMs?: number;
  /** 允许的最大超时上限（ms） */
  maxTimeoutMs?: number;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/** 目标工具成功时对 data 求值；失败时对错误信封求值（便于等待 code 变化） */
function observationPayload(envelope: Envelope): unknown {
  return envelope.ok ? envelope.data : envelope;
}

export function createWaitForTool(
  callTool: ToolCaller,
  options: WaitForToolOptions = {},
): ToolHandler {
  const defaultTimeoutMs = options.defaultTimeoutMs ?? DEFAULT_WAIT_TIMEOUT_MS;
  const defaultIntervalMs = options.defaultIntervalMs ?? DEFAULT_WAIT_INTERVAL_MS;
  const maxTimeoutMs = options.maxTimeoutMs ?? MAX_WAIT_TIMEOUT_MS;

  return async function runWaitFor(args: unknown): Promise<Envelope> {
    const parsed = WaitForInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        "tarkov_wait_for",
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        parsed.error.message,
      );
    }
    const input = parsed.data;

    const parsedPredicate = parsePredicate(input.predicate);
    if (!parsedPredicate.ok) {
      return errEnv(
        "tarkov_wait_for",
        `非法谓词：${parsedPredicate.reason}`,
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        { predicate: input.predicate, reason: parsedPredicate.reason },
      );
    }
    const predicate = parsedPredicate.predicate;

    const timeoutMs = input.timeoutMs ?? defaultTimeoutMs;
    if (timeoutMs > maxTimeoutMs) {
      return errEnv(
        "tarkov_wait_for",
        `超时上限超出允许范围（最大 ${maxTimeoutMs}ms）`,
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        { timeoutMs, maxTimeoutMs },
      );
    }
    const intervalMs = input.intervalMs ?? defaultIntervalMs;
    const targetArgs = input.args ?? {};

    const startedAt = Date.now();
    let attempts = 0;
    let lastValue: unknown = null;
    let lastPathFound = false;
    let lastObservation: Envelope | null = null;

    for (;;) {
      attempts += 1;
      const envelope = await callTool(input.tool, targetArgs);
      lastObservation = envelope;

      // T06：首轮 bridge 缺席类错误立即返回，不误报超时。
      // NOT_IN_RAID 不在快速失败集合内（等进 raid 是合法用法）。
      if (attempts === 1 && !envelope.ok && FAST_FAIL_CODES.has(envelope.code)) {
        return envelope;
      }

      const evaluation = evaluatePredicate(predicate, observationPayload(envelope));
      lastValue = evaluation.actual ?? null;
      lastPathFound = evaluation.pathFound;

      if (evaluation.satisfied) {
        return okEnv(
          "tarkov_wait_for",
          `谓词在 ${attempts} 次轮询后满足`,
          {
            tool: input.tool,
            predicate: input.predicate,
            attempts,
            elapsedMs: Date.now() - startedAt,
            value: evaluation.actual,
          },
        );
      }

      const elapsedMs = Date.now() - startedAt;
      if (elapsedMs >= timeoutMs) {
        return errEnv(
          "tarkov_wait_for",
          `谓词在 ${timeoutMs}ms 内未满足（已轮询 ${attempts} 次）`,
          RUNTIME_ERROR_CODES.WAIT_TIMEOUT,
          {
            tool: input.tool,
            predicate: input.predicate,
            attempts,
            elapsedMs,
            lastValue,
            pathFound: lastPathFound,
            lastObservation,
          },
        );
      }

      // 轮询间隔不超过剩余超时时间
      await sleep(Math.min(intervalMs, timeoutMs - elapsedMs));
    }
  };
}
