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

/** 通用数组 200 响应构造器 */
function arrayResponse(body: unknown[]): SptResponse {
  return { status: 200, body, text: JSON.stringify(body) };
}

// -----------------------------------------------------------------------------
// traders（`/client/trading/api/traderSettings`）
// -----------------------------------------------------------------------------

/** 构造 traderSettings 的 200 响应（裸数组） */
export function traderSettingsResponse(traders: unknown[]): SptResponse {
  return arrayResponse(traders);
}

/**
 * 商人设置 fixture：3 个商人，覆盖显式计数、assort 数组、loyaltyLevels 档位数组
 * 三种字段形状。
 *   - 显式 assortItems 计数
 *   - assort 数组长度
 *   - loyaltyLevels 数组长度（等级兜底）
 */
export function tradersFixture(): Record<string, unknown>[] {
  return [
    { _id: "trader-prapor", standing: 1.25, loyaltyLevel: 4, assortItems: 10 },
    { _id: "trader-therapist", standing: 0.5, loyaltyLevel: 2, assort: [{}, {}, {}] },
    { _id: "trader-fence", standing: 0, loyaltyLevels: [{}, {}, {}] },
  ];
}

// -----------------------------------------------------------------------------
// quests（`/client/quest/list`）
// -----------------------------------------------------------------------------

/** 构造 quest/list 的 200 响应（裸数组） */
export function questListResponse(quests: unknown[]): SptResponse {
  return arrayResponse(quests);
}

/** 任务 fixture：覆盖字符串状态与数字枚举状态，四个计数桶各 1~2 条 */
export function questsFixture(): Record<string, unknown>[] {
  return [
    { _id: "quest-available", status: "AvailableForStart" },
    { _id: "quest-started", status: "Started" },
    { _id: "quest-finishable", status: 3 },
    { _id: "quest-success", status: "Success" },
    { _id: "quest-fail", status: "Fail" },
  ];
}

// -----------------------------------------------------------------------------
// hideout（`/client/hideout/areas`）
// -----------------------------------------------------------------------------

/** 构造 hideout/areas 的 200 响应（裸数组） */
export function hideoutAreasResponse(areas: unknown[]): SptResponse {
  return arrayResponse(areas);
}

/** 藏身处区域 fixture：type 乱序，含一个未建造（level=0）区域 */
export function hideoutAreasFixture(): Record<string, unknown>[] {
  return [
    { type: 2, level: 1, active: true },
    { type: 0, level: 3, active: true },
    { type: 1, level: 0, active: false },
  ];
}

// -----------------------------------------------------------------------------
// inventory（复用 `/client/game/profile/list`）
// -----------------------------------------------------------------------------

/** 带库存的 PMC profile fixture：3 件物品、2 个不同模板 */
export function inventoryProfileFixture(): Record<string, unknown> {
  return {
    info: { id: "64f0a1b2c3d4e5f6a7b8c9d0", username: "Overseer" },
    characters: {
      pmc: {
        info: { level: 10, experience: 1000 },
        inventory: {
          items: [
            { _id: "item-1", _tpl: "tpl-A" },
            { _id: "item-2", _tpl: "tpl-A" },
            { _id: "item-3", _tpl: "tpl-B" },
          ],
        },
      },
    },
  };
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
