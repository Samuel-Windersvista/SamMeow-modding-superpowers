// =============================================================================
// 结构化运行时错误
//
// 传输层/握手层抛出 SptRuntimeError，工具层统一转为 ErrEnvelope。
// =============================================================================

import { RUNTIME_ERROR_CODES, errEnv, type ErrEnvelope, type RuntimeErrorCode } from "./types.js";

export class SptRuntimeError extends Error {
  readonly code: RuntimeErrorCode;
  readonly details?: unknown;

  constructor(code: RuntimeErrorCode, message: string, details?: unknown) {
    super(message);
    this.name = "SptRuntimeError";
    this.code = code;
    this.details = details;
  }
}

export function isSptRuntimeError(error: unknown): error is SptRuntimeError {
  return error instanceof SptRuntimeError;
}

/** 把任意异常转为工具层错误信封（保留结构化错误码与 details） */
export function toErrorEnvelope(tool: string, error: unknown): ErrEnvelope {
  if (isSptRuntimeError(error)) {
    return errEnv(tool, error.message, error.code, error.details);
  }
  const message = error instanceof Error ? error.message : String(error);
  return errEnv(tool, `内部错误：${message}`, RUNTIME_ERROR_CODES.INTERNAL_ERROR);
}
