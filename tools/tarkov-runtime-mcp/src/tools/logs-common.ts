// =============================================================================
// logs.* 工具共享逻辑
//
// logs 工具（logs_recent / logs_summary）复用 raid_* 的 BridgeConnection 接缝与
// 门禁阶梯（不新增 MCP-桥接缝）：
//   ensureBridgeInfo -> 拉取端点 -> 错误映射
//
// 与 raid 工具的区别：
//   - 日志与 raid 状态无关（非 raid 时照常可用），故**不返回 NOT_IN_RAID**；
//   - 桥为旧版（无 /logs/* 端点）时端点返回 404，映射为结构化
//     LOGS_ENDPOINT_UNAVAILABLE（提示更新桥 DLL），**不误报** BRIDGE_VERSION_MISMATCH
//     ——协议门禁只比对 `/bridge/info` 的 protocolVersion，与端点是否存在无关。
// =============================================================================

import { BridgeEndpointUnavailableError } from "../bridge/connection.js";
import { RUNTIME_ERROR_CODES, errEnv, type ErrEnvelope } from "../types.js";
import { bridgeUnreachableEnvelope } from "./raid-common.js";

/** 已注册的 logs.* 工具名 */
export const LOGS_TOOL_NAMES = ["logs_recent", "logs_summary"] as const;

/** 桥端点缺失（旧版桥 DLL 无 /logs/* 端点）的结构化信封 */
export function logsEndpointUnavailableEnvelope(
  tool: string,
  error: BridgeEndpointUnavailableError,
): ErrEnvelope {
  return errEnv(tool, error.message, RUNTIME_ERROR_CODES.LOGS_ENDPOINT_UNAVAILABLE, {
    details: {
      reason: "logs_endpoint_unavailable",
      endpoint: error.endpoint,
    },
  });
}

/**
 * 拉取 logs 端点结果的包装：
 *   - 端点缺失（BridgeEndpointUnavailableError）-> LOGS_ENDPOINT_UNAVAILABLE；
 *   - 其余连接类失败                            -> BRIDGE_UNREACHABLE。
 */
export async function fetchLogsResult<T>(
  tool: string,
  fetch: () => Promise<T>,
): Promise<{ ok: true; result: T } | { ok: false; envelope: ErrEnvelope }> {
  try {
    return { ok: true, result: await fetch() };
  } catch (error) {
    if (error instanceof BridgeEndpointUnavailableError) {
      return { ok: false, envelope: logsEndpointUnavailableEnvelope(tool, error) };
    }
    return { ok: false, envelope: bridgeUnreachableEnvelope(tool, error) };
  }
}
