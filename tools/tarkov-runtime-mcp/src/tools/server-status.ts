// =============================================================================
// tarkov_server_status
//
// 返回连接信息、server 自报版本、锚定 tag 与门禁结果、已加载 server mod 清单
// （路由 `/launcher/v2/mods` 优先，source: "route"；失败回落日志，source:
// "server-log"，见 ADR-0003 与工单 07），以及 MCP 自身能力自报。
// 未连接/版本不匹配时返回结构化错误信封。
//
// mod 清单读取失败（路由与日志均不可用）只降级 mods 字段，不拖垮整体结果。
// =============================================================================

import { z } from "zod";

import type { SptClient } from "../client/client.js";
import type { ServerModsRouteResult } from "../client/mods-route.js";
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

/** server mod 清单：路由来源优先，日志来源兜底 */
export type ServerModsResult = ServerModsRouteResult | ServerModListResult;

/** MCP 当前已实现的工具（Phase 2 起 raid.* 全部真实化，无占位） */
const IMPLEMENTED_TOOLS = [
  "tarkov_server_status",
  "tarkov_instances",
  "tarkov_snapshot",
  "tarkov_wait_for",
  ...RAID_TOOL_NAMES,
] as const;

/** MCP 当前已实现的状态 sections（含快照全部 section；与 snapshot/schema.ts 保持一致） */
const IMPLEMENTED_SECTIONS = [
  "server_status",
  "instances",
  "mods",
  "profile",
  "traders",
  "quests",
  "hideout",
  "inventory",
] as const;

/** 能力自报：吸收 5.0 版本线内部漂移，明确首版边界 */
export interface ServerStatusCapabilities {
  /** 已实现的工具名 */
  tools: string[];
  /** 已实现的状态 sections */
  sections: string[];
  /** 局内状态桥（BepInEx Client Bridge）支持状态：Phase 2 已支持 */
  bridge: "supported";
}

/** server_status 输出数据：在 ticket 01 骨架上加 mods 与增强能力自报 */
export interface ServerStatusToolData extends Omit<ServerStatusData, "capabilities"> {
  capabilities: ServerStatusCapabilities;
  mods: ServerModsResult;
}

export type ReadModListFn = (options: ReadServerModListOptions) => Promise<ServerModListResult>;
export type ReadModsRouteFn = (client: SptClient) => Promise<ServerModsRouteResult>;

export interface ServerStatusToolDeps {
  /** server 日志目录；缺省按 resolveServerLogDir() 从环境变量/cwd 推导 */
  logDir?: string;
  /** 覆盖日志读取器（测试注入） */
  readModList?: ReadModListFn;
  /** 覆盖路由读取器（测试注入） */
  readModsRoute?: ReadModsRouteFn;
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

function modsSummary(mods: ServerModsResult): string {
  if (mods.available) {
    return `server mod ${mods.count} 个（来源 ${mods.source}）`;
  }
  return `server mod 清单不可用（server-log: ${mods.reason}）`;
}

export function createServerStatusTool(
  client: SptClient,
  deps: ServerStatusToolDeps = {},
): ToolHandler {
  const readModList = deps.readModList ?? readServerModList;
  const readModsRoute = deps.readModsRoute ?? ((target: SptClient) => target.serverModsRoute());

  /** 路由优先（`/launcher/v2/mods`），失败回落日志；两者均失败降级为结构化不可用 */
  async function resolveMods(logDir: string): Promise<ServerModsResult> {
    try {
      return await readModsRoute(client);
    } catch {
      // 路由不可用（网络/非 2xx）：静默回落日志来源
    }
    try {
      return await readModList({ logDir });
    } catch (error) {
      return degradeMods(logDir, error);
    }
  }

  return async function runServerStatus(args: unknown): Promise<Envelope> {
    const parsed = ServerStatusInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        "tarkov_server_status",
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        { details: parsed.error.message },
      );
    }
    try {
      const status = await client.serverStatus();
      const logDir = deps.logDir ?? resolveServerLogDir();
      const mods = await resolveMods(logDir);

      const data: ServerStatusToolData = {
        ...status,
        capabilities: {
          tools: [...IMPLEMENTED_TOOLS],
          sections: [...IMPLEMENTED_SECTIONS],
          bridge: "supported",
        },
        mods,
      };

      const channel = status.version.channel ? ` (${status.version.channel})` : "";
      return okEnv(
        "tarkov_server_status",
        `SPT ${status.version.core}${channel} 版本门禁通过；${modsSummary(mods)}`,
        data,
      );
    } catch (error) {
      return toErrorEnvelope("tarkov_server_status", error);
    }
  };
}
