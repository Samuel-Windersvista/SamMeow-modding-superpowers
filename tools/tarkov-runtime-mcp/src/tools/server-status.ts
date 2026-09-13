// =============================================================================
// tarkov_server_status
//
// 返回连接信息、server 自报版本、锚定 tag 与门禁结果，以及 MCP 自身能力自报。
// 未连接/版本不匹配时返回结构化错误信封。
// =============================================================================

import { z } from "zod";

import type { SptClient } from "../client/client.js";
import { toErrorEnvelope } from "../errors.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../types.js";

export const ServerStatusInput = z.object({}).strict();

export type ToolHandler = (args: unknown) => Promise<Envelope>;

export function createServerStatusTool(client: SptClient): ToolHandler {
  return async function runServerStatus(args: unknown): Promise<Envelope> {
    const parsed = ServerStatusInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        "tarkov_server_status",
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        parsed.error.message,
      );
    }
    try {
      const status = await client.serverStatus();
      const channel = status.version.channel ? ` (${status.version.channel})` : "";
      return okEnv(
        "tarkov_server_status",
        `SPT ${status.version.core}${channel} 版本门禁通过`,
        status,
      );
    } catch (error) {
      return toErrorEnvelope("tarkov_server_status", error);
    }
  };
}
