// =============================================================================
// 信封解包：`{err, errmsg, data}` 统一解包与 live 裁剪 fixture 驱动
//
// fixture 为 2026-09-13 从本机运行中的 SPT 5.0 server 抓取后裁剪（裁掉无关大字段，
// 保留结构代表性），见 tests/fixtures/live/。
// =============================================================================

import { describe, expect, it } from "vitest";

import { unwrapEnvelope } from "../../src/snapshot/util.js";
import { createSnapshotTool } from "../../src/tools/snapshot.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";
import hideoutEnvelope from "../fixtures/live/hideout-areas.envelope.json";
import profileEnvelope from "../fixtures/live/profile-list.envelope.json";
import questEnvelope from "../fixtures/live/quest-list.envelope.json";
import traderEnvelope from "../fixtures/live/trader-settings.envelope.json";

const VERSION = "SPT 5.0.0 (BEM) ff0bf32";
const PROFILE_ROUTE = "/client/game/profile/list";
const TRADERS_ROUTE = "/client/trading/api/traderSettings";
const QUESTS_ROUTE = "/client/quest/list";
const HIDEOUT_ROUTE = "/client/hideout/areas";

/** 用路由 -> 原始响应体映射构造 fake client（响应体已是信封） */
function liveClient(routes: Record<string, unknown>) {
  return fakeClient(
    new FakeConnection((options) => {
      if (options.path === "/singleplayer/settings/version") {
        return versionResponse(VERSION);
      }
      const body = routes[options.path];
      if (body === undefined) {
        throw new Error(`fake 未预期的路由：${options.path}`);
      }
      return { status: 200, body, text: JSON.stringify(body) };
    }),
  );
}

describe("unwrapEnvelope", () => {
  it("err=0：返回 data", () => {
    expect(unwrapEnvelope({ err: 0, errmsg: null, data: [1, 2] })).toEqual([1, 2]);
  });

  it("非信封（裸数组 / 业务对象）：原样返回", () => {
    expect(unwrapEnvelope([1, 2])).toEqual([1, 2]);
    expect(unwrapEnvelope({ traders: [] })).toEqual({ traders: [] });
  });

  it("err 非 0：抛结构化 ROUTE_ERROR（携带 errmsg），不静默返回空", () => {
    expect(() => unwrapEnvelope({ err: 1, errmsg: "boom", data: null })).toThrowError(/boom/);
    try {
      unwrapEnvelope({ err: 1, errmsg: "boom", data: null });
    } catch (error) {
      expect((error as { code?: string }).code).toBe(RUNTIME_ERROR_CODES.ROUTE_ERROR);
    }
  });
});

describe("tarkov_snapshot 信封解包（live 裁剪 fixture）", () => {
  it("profile：解包后返回真实 profile 计数与 PMC 摘要", async () => {
    const tool = createSnapshotTool(liveClient({ [PROFILE_ROUTE]: profileEnvelope }));

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: {
        profile: {
          profileCount: 2,
          pmc: {
            level: 1,
            experience: 0,
            skills: { total: 3, leveled: 0 },
            quests: { available: 0, inProgress: 3, completed: 0, failed: 0 },
          },
        },
      },
    });
  });

  it("inventory：从 PMC 条目 `Inventory.items` 计数", async () => {
    const tool = createSnapshotTool(liveClient({ [PROFILE_ROUTE]: profileEnvelope }));

    const result = await tool({ sections: ["inventory"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: { inventory: { itemCount: 3, distinctTemplates: 3 } },
    });
  });

  it("traders：解包后返回真实商人数与忠诚档位", async () => {
    const tool = createSnapshotTool(liveClient({ [TRADERS_ROUTE]: traderEnvelope }));

    const result = await tool({ sections: ["traders"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: { traders: { traderCount: 3, totalAssortItems: 0 } },
    });
    const traders = (result.data as { sections: { traders: { traders: unknown[] } } }).sections
      .traders.traders as Array<{ loyaltyLevel: number }>;
    expect(traders.map((t) => t.loyaltyLevel)).toEqual([4, 4, 2]);
  });

  it("quests：优先 `sptStatus`（status 恒 0 时不静默为 0 计数）", async () => {
    const tool = createSnapshotTool(liveClient({ [QUESTS_ROUTE]: questEnvelope }));

    const result = await tool({ sections: ["quests"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: {
        quests: { questCount: 5, counts: { available: 3, inProgress: 2, completed: 0, failed: 0 } },
      },
    });
  });

  it("hideout：解包后返回真实区域数", async () => {
    const tool = createSnapshotTool(liveClient({ [HIDEOUT_ROUTE]: hideoutEnvelope }));

    const result = await tool({ sections: ["hideout"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ sections: { hideout: { areaCount: 3 } } });
  });

  it("路由业务错误信封（err!=0）：返回结构化 ROUTE_ERROR", async () => {
    const tool = createSnapshotTool(
      liveClient({ [PROFILE_ROUTE]: { err: 42, errmsg: "profile failed", data: null } }),
    );

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.ROUTE_ERROR);
  });
});
