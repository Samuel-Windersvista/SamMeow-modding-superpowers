// =============================================================================
// raid_player
//
// 经 BridgeConnection 拉取玩家局内全字段（T04）：位置/朝向/姿态/血量（总+肢体）。
// 行为：
//   - 调用前 ensure `/bridge/info`（协议版本门禁）；
//   - 在 raid：ok 信封，data = { position, rotation, pose, health, sampleAgeMs }
//     （确定性字段序）；
//   - 不在 raid：err 信封 NOT_IN_RAID；
//   - 桥不可达（拒绝/超时/非 2xx/响应非法）：err 信封 BRIDGE_UNREACHABLE；
//   - 协议版本不一致：err 信封 BRIDGE_VERSION_MISMATCH。
// =============================================================================

import { z } from "zod";

import type { BridgeConnection } from "../bridge/connection.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../types.js";
import { fetchRaidSample } from "./raid-common.js";
import type { ToolHandler } from "./server-status.js";

export const RAID_PLAYER_TOOL_NAME = "raid_player";

export const RaidPlayerInput = z.object({}).strict();

export function createRaidPlayerTool(connection: BridgeConnection): ToolHandler {
  return async function runRaidPlayer(args: unknown): Promise<Envelope> {
    const parsed = RaidPlayerInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        RAID_PLAYER_TOOL_NAME,
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        parsed.error.message,
      );
    }

    const sample = await fetchRaidSample(
      RAID_PLAYER_TOOL_NAME,
      connection,
      () => connection.getRaidPlayer(),
      { message: "当前不在 raid 中，无玩家局内状态" },
    );
    if (!sample.ok) {
      return sample.envelope;
    }
    const result = sample.result;

    return okEnv(
      RAID_PLAYER_TOOL_NAME,
      `raid 玩家状态已读取（姿态 ${result.pose}，血量 ${result.health.total}${result.health.alive ? "" : "（已阵亡）"}，新鲜度 ${result.sampleAgeMs}ms）`,
      {
        position: {
          x: result.position.x,
          y: result.position.y,
          z: result.position.z,
        },
        rotation: {
          x: result.rotation.x,
          y: result.rotation.y,
        },
        pose: result.pose,
        health: {
          alive: result.health.alive,
          total: result.health.total,
          parts: {
            Head: result.health.parts.Head,
            Chest: result.health.parts.Chest,
            Stomach: result.health.parts.Stomach,
            LeftArm: result.health.parts.LeftArm,
            RightArm: result.health.parts.RightArm,
            LeftLeg: result.health.parts.LeftLeg,
            RightLeg: result.health.parts.RightLeg,
          },
        },
        sampleAgeMs: result.sampleAgeMs,
      },
    );
  };
}
