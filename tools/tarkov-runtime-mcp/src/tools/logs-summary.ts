// =============================================================================
// logs_summary
//
// 统一日志聚合视图 = **桥组** + **服务器组**（MCP 侧 tail spt/kestrel/requests）
// + **fatal 组**（MCP 侧监视 BepInEx/ErrorLog.log，进程级崩溃栈）。
//   - 桥组经 BridgeConnection 拉取（归一化去重由桥侧完成）；
//   - 服务器 / fatal 组由 LogWatchService 惰性刷新后聚合（见 src/logs/）；
//   - 三路按 source 区分，合并后统一排序（count 降序 → lastTs 降序 → key 升序，
//     与桥侧同序）；`overflowDropped` 为桥 + MCP 侧合计；
//   - 入参 since 为**时间游标**（只回 lastTs 晚于它的组）：桥侧原样透传（桥自行
//     过滤），MCP 组按同语义本地过滤，保证合并视图语义统一。
//
// **桥故障降级（本工单核心）**：桥不可达 / 协议版本不匹配 / 旧版桥端点缺失时
// **不返回错误信封**，而是 ok 信封 + `bridge: {available:false, reason}`，
// 服务器组与 fatal 组照常返回——进程级崩溃会杀死桥进程，fatal 通道必须在该
// 场景下仍可用（本通道存在的根本理由）。logs_recent 保持严格门禁不变。
//
// 输出 data = { groups, overflowDropped, bridge, server, fatal }（确定性字段序）：
//   - `bridge`：桥通道可用性（可用时附桥自报；不可用时 `{available:false, reason}`，
//     reason ∈ unreachable / version_mismatch / endpoint_missing）；
//   - `server` / `fatal`：MCP 侧通道可用性（`{available:true}` 或 `{available:false, reason}`，
//     reason ∈ logs_root_missing / no_log_dirs / path_unresolved / file_missing）——
//     不可用时对应组为空，使「静默降级」在输出里可见（不再只能靠组数为 0 猜测）。
// =============================================================================

import { z } from "zod";

import {
  BridgeEndpointUnavailableError,
  EXPECTED_BRIDGE_PROTOCOL_VERSION,
  type BridgeConnection,
  type BridgeInfo,
} from "../bridge/connection.js";
import type { AggregatedLogGroup } from "../logs/log-aggregator.js";
import { compareLogGroups } from "../logs/log-aggregator.js";
import { EMPTY_LOG_WATCH, type LogWatchSource } from "../logs/log-watch.js";
import { filterGroupsSince, parseSinceToEpochMs } from "../logs/log-since.js";
import type { LogWatchChannelStatus } from "../logs/log-tail.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../types.js";
import { bridgeSelfReport } from "./raid-common.js";
import type { ToolHandler } from "./server-status.js";

export const LOGS_SUMMARY_TOOL_NAME = "logs_summary";

/** 桥不可用原因（结构化，便于调用方分支） */
export type BridgeUnavailableReason = "unreachable" | "version_mismatch" | "endpoint_missing";

/** 桥可用性（判别联合，`available` 为判别字段且置于首位） */
export type BridgeAvailability =
  | {
      available: true;
      pluginVersion: string;
      protocolVersion: number;
      samplingIntervalMs: number;
    }
  | { available: false; reason: BridgeUnavailableReason };

export const LogsSummaryInput = z
  .object({
    /**
     * 时间游标：只返回 lastTs 晚于它的组。
     * 接受端点自己输出的 ISO 8601（lastTs 原样回填）或整数 UTC Ticks；
     * 缺省 / 非正数 / 非法即不过滤（桥侧宽松解析）。
     */
    since: z.union([z.string().min(1), z.number().int()]).optional(),
  })
  .strict();

/** 聚合组输出归一（字段序稳定：key/level/source/count/firstTs/lastTs/sampleText） */
function toGroupOutput(group: AggregatedLogGroup): Record<string, unknown> {
  return {
    key: group.key,
    level: group.level,
    source: group.source,
    count: group.count,
    firstTs: group.firstTs,
    lastTs: group.lastTs,
    sampleText: group.sampleText,
  };
}

/** 通道可用性输出归一（字段序稳定：available[、reason]；available=true 时不带 reason） */
function toChannelStatusOutput(status: LogWatchChannelStatus): Record<string, unknown> {
  return status.available ? { available: true } : { available: false, reason: status.reason };
}

/** 桥组拉取结果：可用性 + 组 + 溢出计数（故障时组为空） */
interface BridgeGroupsResult {
  availability: BridgeAvailability;
  groups: AggregatedLogGroup[];
  overflowDropped: number;
}

/** 拉取桥组与桥可用性；任何桥侧故障都降级为 `{available:false, reason}`（不抛错） */
async function fetchBridgeGroups(
  connection: BridgeConnection,
  since: string | number | undefined,
): Promise<BridgeGroupsResult> {
  let info: BridgeInfo;
  try {
    info = await connection.getInfo();
  } catch {
    return { availability: { available: false, reason: "unreachable" }, groups: [], overflowDropped: 0 };
  }

  if (info.protocolVersion !== EXPECTED_BRIDGE_PROTOCOL_VERSION) {
    return {
      availability: { available: false, reason: "version_mismatch" },
      groups: [],
      overflowDropped: 0,
    };
  }

  try {
    const result = await connection.getLogsSummary(since);
    return {
      availability: { available: true, ...bridgeSelfReport(info) },
      groups: result.groups,
      overflowDropped: result.overflowDropped,
    };
  } catch (error) {
    const reason: BridgeUnavailableReason =
      error instanceof BridgeEndpointUnavailableError ? "endpoint_missing" : "unreachable";
    return { availability: { available: false, reason }, groups: [], overflowDropped: 0 };
  }
}

/** 降级说明（summary 行内）：端点缺失时附「更新桥 DLL」提示 */
function degradationNote(reason: BridgeUnavailableReason): string {
  const base = `桥不可用（${reason}）`;
  return reason === "endpoint_missing"
    ? `${base}：旧版桥无 /logs/summary 端点，请更新桥 DLL`
    : base;
}

export function createLogsSummaryTool(
  connection: BridgeConnection,
  logWatch: LogWatchSource = EMPTY_LOG_WATCH,
): ToolHandler {
  return async function runLogsSummary(args: unknown): Promise<Envelope> {
    const parsed = LogsSummaryInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        LOGS_SUMMARY_TOOL_NAME,
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        { details: parsed.error.message },
      );
    }
    const { since } = parsed.data;

    const bridgeResult = await fetchBridgeGroups(connection, since);
    const watched = await logWatch.snapshot();

    // MCP 组与桥侧同语义过滤（桥侧自行过滤其返回的组）。
    const sinceMs = parseSinceToEpochMs(since);
    const watchedGroups = filterGroupsSince(watched.groups, sinceMs);

    const groups = [...bridgeResult.groups, ...watchedGroups].sort(compareLogGroups);
    const overflowDropped = bridgeResult.overflowDropped + watched.overflowDropped;

    const fatalCount = watchedGroups.filter((group) => group.source === "fatal").length;
    const serverCount = watchedGroups.length - fatalCount;
    const bridgeCount = bridgeResult.groups.length;

    const overflowNote = overflowDropped > 0 ? `，溢出淘汰 ${overflowDropped} 组` : "";
    const summary =
      bridgeResult.availability.available === true
        ? `日志聚合已读取：${groups.length} 组（桥 ${bridgeCount} + 服务器 ${serverCount} + fatal ${fatalCount}${overflowNote}）`
        : `日志聚合已读取：${groups.length} 组（${degradationNote(bridgeResult.availability.reason)}；服务器 ${serverCount} + fatal ${fatalCount}${overflowNote}）`;

    return okEnv(LOGS_SUMMARY_TOOL_NAME, summary, {
      groups: groups.map(toGroupOutput),
      overflowDropped,
      bridge: bridgeResult.availability,
      server: toChannelStatusOutput(watched.server),
      fatal: toChannelStatusOutput(watched.fatal),
    });
  };
}
