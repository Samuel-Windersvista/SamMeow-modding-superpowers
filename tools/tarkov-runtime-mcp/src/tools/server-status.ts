// =============================================================================
// tarkov_server_status
//
// 返回连接信息、server 自报版本、锚定 tag 与门禁结果、已加载 server mod 清单
// （来源标注 server-log，见 ADR-0003），以及 MCP 自身能力自报。
// 未连接/版本不匹配时返回结构化错误信封。
//
// mod 清单读取失败（日志缺失/不可读）只降级 mods 字段，不拖垮整体结果。
// =============================================================================

import { z } from "zod";

import type { SptClient } from "../client/client.js";
import { toErrorEnvelope } from "../errors.js";
import {
  readServerModList,
  resolveServerLogDir,
  type ReadServerModListOptions,
  type ServerModListResult,
} from "../logs/log-reader.js";
import {
  RUNTIME_ERROR_CODES,
  errEnv,
  okEnv,
  type Envelope,
  type ServerStatusData,
} from "../types.js";
import { RAID_TOOL_NAMES } from "./raid.js";

export const ServerStatusInput = z.object({}).strict();

export type ToolHandler = (args: unknown) => Promise<Envelope>;

/** MCP 当前已实现的工具（不含 Phase 2 占位） */
const IMPLEMENTED_TOOLS = [
  "tarkov_server_status",
  "tarkov_instances",
  "tarkov_snapshot",
  "tarkov_wait_for",
] as const;

/** MCP 当前已实现的状态 sections */
const IMPLEMENTED_SECTIONS = ["server_status", "instances", "mods", "profile"] as const;

/** 能力自报：吸收 5.0 版本线内部漂移，明确首版边界 */
export interface ServerStatusCapabilities {
  /** 已实现的工具名 */
  tools: string[];
  /** 已注册但未实现的 Phase 2 占位工具 */
  placeholderTools: string[];
  /** 已实现的状态 sections */
  sections: string[];
  /** 局内状态桥（Phase 2 BepInEx Client Bridge）状态 */
  bridge: "not_installed";
}

/** server_status 输出数据：在 ticket 01 骨架上加 mods 与增强能力自报 */
export interface ServerStatusToolData extends Omit<ServerStatusData, "capabilities"> {
  capabilities: ServerStatusCapabilities;
  mods: ServerModListResult;
}

export type ReadModListFn = (options: ReadServerModListOptions) => Promise<ServerModListResult>;

export interface ServerStatusToolDeps {
  /** server 日志目录；缺省按 resolveServerLogDir() 从环境变量/cwd 推导 */
  logDir?: string;
  /** 覆盖日志读取器（测试注入） */
  readModList?: ReadModListFn;
}

function degradeMods(logDir: string, error: unknown): ServerModListResult {
  const message = error instanceof Error ? error.message : String(error);
  return {
    source: "server-log",
    available: false,
    reason: "log_unreadable",
    message: `日志读取失败：${message}`,
    logDir,
    logFile: null,
  };
}

export function createServerStatusTool(
  client: SptClient,
  deps: ServerStatusToolDeps = {},
): ToolHandler {
  const readModList = deps.readModList ?? readServerModList;

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
      const logDir = deps.logDir ?? resolveServerLogDir();

      // 日志读取本身不应拖垮 server_status：任何异常都降级为结构化不可用
      let mods: ServerModListResult;
      try {
        mods = await readModList({ logDir });
      } catch (error) {
        mods = degradeMods(logDir, error);
      }

      const data: ServerStatusToolData = {
        ...status,
        capabilities: {
          tools: [...IMPLEMENTED_TOOLS],
          placeholderTools: [...RAID_TOOL_NAMES],
          sections: [...IMPLEMENTED_SECTIONS],
          bridge: "not_installed",
        },
        mods,
      };

      const channel = status.version.channel ? ` (${status.version.channel})` : "";
      const modsSummary = mods.available
        ? `server mod ${mods.count} 个（来源 server-log）`
        : `server mod 清单不可用（server-log: ${mods.reason}）`;
      return okEnv(
        "tarkov_server_status",
        `SPT ${status.version.core}${channel} 版本门禁通过；${modsSummary}`,
        data,
      );
    } catch (error) {
      return toErrorEnvelope("tarkov_server_status", error);
    }
  };
}
