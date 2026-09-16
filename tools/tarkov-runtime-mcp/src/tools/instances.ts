// =============================================================================
// tarkov_instances
//
// 探测候选端口并返回发现的单实例信息（不执行版本门禁）。
// =============================================================================

import { z } from "zod";

import type { SptClient } from "../client/client.js";
import { toErrorEnvelope } from "../errors.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../types.js";
import type { ToolHandler } from "./server-status.js";

export const InstancesInput = z.object({}).strict();

export function createInstancesTool(client: SptClient): ToolHandler {
  return async function runInstances(args: unknown): Promise<Envelope> {
    const parsed = InstancesInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        "tarkov_instances",
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        { details: parsed.error.message },
      );
    }
    try {
      const data = await client.instances();
      return okEnv("tarkov_instances", `发现 ${data.count} 个 SPT server 实例`, data);
    } catch (error) {
      return toErrorEnvelope("tarkov_instances", error);
    }
  };
}
