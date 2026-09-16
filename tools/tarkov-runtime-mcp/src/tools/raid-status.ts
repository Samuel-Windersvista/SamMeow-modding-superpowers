// =============================================================================
// raid_status
//
// 经 BridgeConnection 拉取 raid 元数据（地图/状态/剩余时间/raidId），并附带
// 桥自报（pluginVersion/protocolVersion/samplingIntervalMs），一次调用即可诊断
// 「桥活着吗、进 raid 了吗」。
// 行为：
//   - 调用前 ensure `/bridge/info`（协议版本门禁）；
//   - 在 raid：ok 信封，data = { map, status, remainingSeconds, raidId,
//     sampleAgeMs, bridge:{...} }（确定性字段序）；
//   - 不在 raid：err 信封 NOT_IN_RAID，details 附带桥自报（桥可达且协议通过时）；
//   - 桥不可达：err 信封 BRIDGE_UNREACHABLE；
//   - 协议版本不一致：err 信封 BRIDGE_VERSION_MISMATCH。
// =============================================================================

import { z } from "zod";

import type { BridgeConnection } from "../bridge/connection.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../types.js";
import { bridgeSelfReport, fetchRaidSample } from "./raid-common.js";
import type { ToolHandler } from "./server-status.js";

export const RAID_STATUS_TOOL_NAME = "raid_status";

export const RaidStatusInput = z.object({}).strict();

export function createRaidStatusTool(connection: BridgeConnection): ToolHandler {
  return async function runRaidStatus(args: unknown): Promise<Envelope> {
    const parsed = RaidStatusInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        RAID_STATUS_TOOL_NAME,
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        { details: parsed.error.message },
      );
    }

    const sample = await fetchRaidSample(
      RAID_STATUS_TOOL_NAME,
      connection,
      () => connection.getRaidStatus(),
      {
        message: "当前不在 raid 中，无 raid 元数据",
        details: (info) => ({ bridge: bridgeSelfReport(info) }),
      },
    );
    if (!sample.ok) {
      return sample.envelope;
    }
    const { result, info } = sample;

    return okEnv(
      RAID_STATUS_TOOL_NAME,
      `raid ${result.raidId}（${result.map}）状态 ${result.status}，剩余 ${result.remainingSeconds}s；bridge ${info.pluginVersion} 协议 ${info.protocolVersion}`,
      {
        map: result.map,
        status: result.status,
        remainingSeconds: result.remainingSeconds,
        raidId: result.raidId,
        sampleAgeMs: result.sampleAgeMs,
        bridge: bridgeSelfReport(info),
      },
    );
  };
}
