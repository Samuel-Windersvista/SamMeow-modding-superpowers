// =============================================================================
// mcp-kit/envelope.ts — MCP 工具信封（canonical）
//
// 决策 D4：spt + tarkov 统一为 canonical；mo2 本轮保留其错误方言
// （另立可选小工单）。
//
// canonical 形状：
//   ok  : { ok: true,  tool, summary, data? }
//   err : { ok: false, tool, code, message, hint?, details? }
//
// 与两台既有方言的关系：
//   - spt 原 err 为 { summary, hint } —— 迁移后 summary → message（字段级 delta，
//     已在 C2 spec「已声明 wire delta」登记）。
//   - tarkov 原 err 为 { message, details } —— 是 canonical 的子集，字段名不变。
//   - hint / details 均为可选：未提供时不落键（JSON.stringify 下与显式 undefined
//     等价，避免在对象形状上引入无意义噪声）。
// =============================================================================

export interface OkEnvelope {
  ok: true;
  tool: string;
  summary: string;
  data?: unknown;
}

export interface ErrEnvelope {
  ok: false;
  tool: string;
  /** 机器可读错误码 */
  code: string;
  /** 人类可读错误说明 */
  message: string;
  /** 修复提示（可选，例如可用工具清单、缺失资源路径） */
  hint?: string;
  /** 结构化细节（可选，例如 VERSION_MISMATCH 的 { expected, actual }） */
  details?: unknown;
}

export type Envelope = OkEnvelope | ErrEnvelope;

/** 构造成功信封 */
export function okEnv(tool: string, summary: string, data?: unknown): OkEnvelope {
  return { ok: true, tool, summary, data };
}

/**
 * 构造错误信封。
 *
 * @param tool    工具名
 * @param message 人类可读错误说明（canonical 字段名为 message）
 * @param code    机器可读错误码
 * @param extra   可选补充：hint（修复提示）/ details（结构化细节）
 */
export function errEnv(
  tool: string,
  message: string,
  code: string,
  extra?: { hint?: string; details?: unknown },
): ErrEnvelope {
  const envelope: ErrEnvelope = { ok: false, tool, code, message };
  if (extra?.hint !== undefined) envelope.hint = extra.hint;
  if (extra?.details !== undefined) envelope.details = extra.details;
  return envelope;
}
