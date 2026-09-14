// =============================================================================
// raid_bots
//
// 经 BridgeConnection 拉取 raid bot 状态（T05）：
//   - 默认摘要：总数 + 存活 + PMC/Scav/Boss/其他 分类计数 + 生成器计数；
//   - detail=true：追加每个 bot 的明细（位置/角色/阵营/存活）与截断标记。
// 行为：
//   - 调用前 ensure `/bridge/info`（协议版本门禁）；
//   - 在 raid：ok 信封，data 确定性字段序；
//   - 不在 raid：err 信封 NOT_IN_RAID；
//   - 桥不可达：err 信封 BRIDGE_UNREACHABLE；
//   - 协议版本不一致：err 信封 BRIDGE_VERSION_MISMATCH。
// =============================================================================

import { z } from "zod";

import type { BridgeConnection } from "../bridge/connection.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../types.js";
import { fetchRaidSample } from "./raid-common.js";
import type { ToolHandler } from "./server-status.js";

export const RAID_BOTS_TOOL_NAME = "raid_bots";

export const RaidBotsInput = z
  .object({
    detail: z.boolean().optional(),
  })
  .strict();

export function createRaidBotsTool(connection: BridgeConnection): ToolHandler {
  return async function runRaidBots(args: unknown): Promise<Envelope> {
    const parsed = RaidBotsInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        RAID_BOTS_TOOL_NAME,
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        parsed.error.message,
      );
    }
    const detail = parsed.data.detail === true;

    const sample = await fetchRaidSample(
      RAID_BOTS_TOOL_NAME,
      connection,
      () => connection.getRaidBots(detail),
      { message: "当前不在 raid 中，无 bot 状态" },
    );
    if (!sample.ok) {
      return sample.envelope;
    }
    const result = sample.result;

    const summary = {
      total: result.total,
      alive: result.alive,
      byCategory: {
        pmc: result.byCategory.pmc,
        scav: result.byCategory.scav,
        boss: result.byCategory.boss,
        other: result.byCategory.other,
      },
      spawner: {
        aliveAndLoading: result.spawner.aliveAndLoading,
        delayed: result.spawner.delayed,
        allWithDelayed: result.spawner.allWithDelayed,
      },
      sampleAgeMs: result.sampleAgeMs,
    };

    if (result.detail) {
      return okEnv(
        RAID_BOTS_TOOL_NAME,
        `raid bot 明细已读取：共 ${result.total}（存活 ${result.alive}，PMC ${result.byCategory.pmc} / Scav ${result.byCategory.scav} / Boss ${result.byCategory.boss} / 其他 ${result.byCategory.other}）${result.truncated ? "，明细已截断" : ""}，新鲜度 ${result.sampleAgeMs}ms`,
        {
          ...summary,
          bots: result.bots.map((bot) => ({
            x: bot.x,
            y: bot.y,
            z: bot.z,
            role: bot.role,
            side: bot.side,
            alive: bot.alive,
          })),
          truncated: result.truncated,
        },
      );
    }

    return okEnv(
      RAID_BOTS_TOOL_NAME,
      `raid bot 摘要已读取：共 ${result.total}（存活 ${result.alive}，PMC ${result.byCategory.pmc} / Scav ${result.byCategory.scav} / Boss ${result.byCategory.boss} / 其他 ${result.byCategory.other}），新鲜度 ${result.sampleAgeMs}ms`,
      summary,
    );
  };
}
