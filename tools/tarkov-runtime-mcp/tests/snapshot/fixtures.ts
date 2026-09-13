// 快照测试 fixture：模拟 SPT 5.0 `/client/*` 路由的响应。
//
// 形状依据 2026-09-13 live smoke 实测（本机运行中的 SPT 5.0 server）：
//   - 统一信封 `{err, errmsg, data}`；
//   - `/client/game/profile/list` 的 data 为「扁平 profile」数组：PMC 条目带非空
//     `savage`，字段为 PascalCase（`Info` / `Skills.Common` / `Quests` / `Inventory` /
//     `Hideout` / `TradersInfo`）；
//   - scav 条目 `savage` 为 null。
//
// 与 KB `api-notes-5.0/save-profile.md` 描述的「存档文件」结构（`info`/`characters.pmc`）
// 不同：路由返回的是客户端面扁平化后的 profile，此处以 live 实测为准。

import type { SptResponse } from "../../src/transport/connection.js";

/** 构造 SPT `/client/*` 路由的成功响应信封 `{err:0, errmsg:null, data}` */
export function envelopeResponse(data: unknown): SptResponse {
  const body = { err: 0, errmsg: null, data };
  return { status: 200, body, text: JSON.stringify(body) };
}

/** 构造 `/client/game/profile/list` 的 200 响应 */
export function profileListResponse(profiles: unknown[]): SptResponse {
  return envelopeResponse(profiles);
}

// -----------------------------------------------------------------------------
// traders（`/client/trading/api/traderSettings`）
// -----------------------------------------------------------------------------

/** 构造 traderSettings 的 200 响应 */
export function traderSettingsResponse(traders: unknown[]): SptResponse {
  return envelopeResponse(traders);
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

/** 构造 quest/list 的 200 响应 */
export function questListResponse(quests: unknown[]): SptResponse {
  return envelopeResponse(quests);
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

/** 构造 hideout/areas 的 200 响应 */
export function hideoutAreasResponse(areas: unknown[]): SptResponse {
  return envelopeResponse(areas);
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

/** 带库存的 PMC profile fixture（live 扁平形状）：3 件物品、2 个不同模板 */
export function inventoryProfileFixture(): Record<string, unknown> {
  return {
    _id: "6aa408bf3c427c14241039f5",
    savage: "6aa408bf3c427c14241039f6",
    Info: { Nickname: "Overseer", Side: "Bear", Level: 10, Experience: 1000 },
    Inventory: {
      items: [
        { _id: "item-1", _tpl: "tpl-A" },
        { _id: "item-2", _tpl: "tpl-A" },
        { _id: "item-3", _tpl: "tpl-B" },
      ],
    },
  };
}

/**
 * 一个完整的 PMC profile 条目（live 扁平形状）。
 *
 * 技能：3 条，其中 2 条 progress > 0。
 * 任务：6 条，覆盖字符串状态、数字枚举状态与未知状态（未知状态不计数）。
 */
export function pmcProfileFixture(): Record<string, unknown> {
  return {
    _id: "6aa408bf3c427c14241039f5",
    savage: "6aa408bf3c427c14241039f6",
    Info: {
      Nickname: "Overseer",
      Side: "Bear",
      Level: 42,
      Experience: 1234567,
    },
    Skills: {
      Common: [
        { Id: "Strength", Progress: 100 },
        { Id: "Endurance", Progress: 0 },
        { Id: "Attention", Progress: 50.5 },
      ],
    },
    Quests: [
      { qid: "quest-available", status: "AvailableForStart" },
      { qid: "quest-started", status: "Started" },
      { qid: "quest-finishable", status: 3 },
      { qid: "quest-success", status: "Success" },
      { qid: "quest-fail", status: 5 },
      { qid: "quest-unknown", status: "NotARealStatus" },
    ],
  };
}
