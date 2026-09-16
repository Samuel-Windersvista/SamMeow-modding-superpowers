// =============================================================================
// raid_events
//
// 经 BridgeConnection 增量拉取 raid 事件时间线（damage / death / extraction）：
//   - 入参 since = 已消费的最后一条事件的 seq（缺省从最旧开始）、limit（截断条数）；
//     输出 seq 字段是最新序号，仅用于判断是否有新事件；limit 截断时用最后一条
//     已返回事件的 seq 续拉；
//   - 输出 data = { inRaid, seq, dropped, events }（确定性字段序）；
//   - `dropped > 0` 表示 since 过旧、已被环形缓冲淘汰。
// 语义：
//   - `/raid/events` 在非 raid 时仍返回缓冲（桥侧路由始终返回完整结构，
//     `inRaid` 仅为状态字段），故赛后时间线（含撤离事件）可读；
//   - 因此本工具**不返回 NOT_IN_RAID**：非 raid 时为 ok 信封且 inRaid:false；
//   - 桥不可达：err 信封 BRIDGE_UNREACHABLE；
//   - 协议版本不一致：err 信封 BRIDGE_VERSION_MISMATCH。
// =============================================================================

import { z } from "zod";

import type { BridgeConnection, RaidEvent } from "../bridge/connection.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../types.js";
import { ensureBridgeInfo, fetchRaidResult } from "./raid-common.js";
import type { ToolHandler } from "./server-status.js";

export const RAID_EVENTS_TOOL_NAME = "raid_events";

export const RaidEventsInput = z
  .object({
    /** 已消费的最后一条事件的 seq（独占）：只返回 seq > since 的事件；缺省从最旧开始 */
    since: z.number().int().nonnegative().optional(),
    /** 单次最多返回的事件条数；缺省由桥侧决定 */
    limit: z.number().int().positive().optional(),
  })
  .strict();

/** 事件输出归一（字段序稳定：seq/ts/type/raidId/payload） */
function toEventOutput(event: RaidEvent): Record<string, unknown> {
  switch (event.type) {
    case "damage":
      return {
        seq: event.seq,
        ts: event.ts,
        type: event.type,
        raidId: event.raidId,
        payload: {
          victimProfileId: event.payload.victimProfileId,
          victimIsLocal: event.payload.victimIsLocal,
          part: event.payload.part,
          amount: event.payload.amount,
          sourceType: event.payload.sourceType,
        },
      };
    case "death":
      return {
        seq: event.seq,
        ts: event.ts,
        type: event.type,
        raidId: event.raidId,
        payload: {
          victimProfileId: event.payload.victimProfileId,
          victimIsLocal: event.payload.victimIsLocal,
          damageType: event.payload.damageType,
          killer:
            event.payload.killer === null
              ? null
              : {
                  profileId: event.payload.killer.profileId,
                  name: event.payload.killer.name,
                  side: event.payload.killer.side,
                  role: event.payload.killer.role,
                  isLocal: event.payload.killer.isLocal,
                },
        },
      };
    case "extraction":
      return {
        seq: event.seq,
        ts: event.ts,
        type: event.type,
        raidId: event.raidId,
        payload: {
          exitName: event.payload.exitName,
          status: event.payload.status,
        },
      };
  }
}

export function createRaidEventsTool(connection: BridgeConnection): ToolHandler {
  return async function runRaidEvents(args: unknown): Promise<Envelope> {
    const parsed = RaidEventsInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        RAID_EVENTS_TOOL_NAME,
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        { details: parsed.error.message },
      );
    }
    const { since, limit } = parsed.data;

    // 门禁阶梯：ensureBridgeInfo -> 拉取端点；非 raid 不视为错误（缓冲仍有效）。
    const info = await ensureBridgeInfo(RAID_EVENTS_TOOL_NAME, connection);
    if (!info.ok) {
      return info.envelope;
    }

    const fetched = await fetchRaidResult(RAID_EVENTS_TOOL_NAME, () =>
      connection.getRaidEvents(since, limit),
    );
    if (!fetched.ok) {
      return fetched.envelope;
    }
    const result = fetched.result;

    const state = result.inRaid ? "在 raid" : "不在 raid（读取缓冲）";

    return okEnv(
      RAID_EVENTS_TOOL_NAME,
      `raid 事件已读取（${state}）：${result.events.length} 条（最新 seq ${result.seq}${result.dropped > 0 ? `，since 过旧丢失 ${result.dropped} 条` : ""}）`,
      {
        inRaid: result.inRaid,
        seq: result.seq,
        dropped: result.dropped,
        events: result.events.map(toEventOutput),
      },
    );
  };
}
