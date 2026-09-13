// 快照测试 fixture：模拟 SPT `/client/game/profile/list` 的响应体。
//
// 形状依据 KB `api-notes-5.0/save-profile.md`（SptProfile 顶层：info/characters）与
// 研究文档 `docs/research/spt-runtime-state-export.md` 的路由表。字段名采用 SPT
// JsonUtil 的 camelCase 序列化约定。

import type { SptResponse } from "../../src/transport/connection.js";

/** 构造 `/client/game/profile/list` 的 200 响应 */
export function profileListResponse(profiles: unknown[]): SptResponse {
  return { status: 200, body: profiles, text: JSON.stringify(profiles) };
}

/**
 * 一个完整的 PMC profile 条目。
 *
 * 技能：3 条，其中 2 条 progress > 0。
 * 任务：6 条，覆盖字符串状态、数字枚举状态与未知状态（未知状态不计数）。
 */
export function pmcProfileFixture(): Record<string, unknown> {
  return {
    info: {
      id: "64f0a1b2c3d4e5f6a7b8c9d0",
      scavId: "64f0a1b2c3d4e5f6a7b8c9d1",
      aid: 424242,
      username: "Overseer",
      wipe: false,
      edition: "Edge of Darkness",
    },
    characters: {
      pmc: {
        info: { level: 42, experience: 1234567 },
        skills: {
          common: [
            { id: "Strength", progress: 100 },
            { id: "Endurance", progress: 0 },
            { id: "Attention", progress: 50.5 },
          ],
        },
        quests: [
          { qid: "quest-available", status: "AvailableForStart" },
          { qid: "quest-started", status: "Started" },
          { qid: "quest-finishable", status: 3 },
          { qid: "quest-success", status: "Success" },
          { qid: "quest-fail", status: 5 },
          { qid: "quest-unknown", status: "NotARealStatus" },
        ],
      },
      scav: { info: { level: 5, experience: 100 } },
    },
  };
}
