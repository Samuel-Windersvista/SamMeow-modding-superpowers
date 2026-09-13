// =============================================================================
// tarkov_snapshot：traders / quests / hideout / inventory section
//
// 只测外部行为：工具输出 schema、空数据降级、多选组合与确定性。
// 路由响应以手写 fixture 驱动（S1 接缝 fake connection）。
// =============================================================================

import { describe, expect, it } from "vitest";

import type { SptResponse } from "../../src/transport/connection.js";
import { createSnapshotTool } from "../../src/tools/snapshot.js";
import { FakeConnection, ACTIVE_PROFILES_PATH, LAUNCHER_PROFILES_PATH, fakeClient, versionResponse } from "../helpers/fake-connection.js";
import {
  hideoutAreasFixture,
  hideoutAreasResponse,
  inventoryProfileFixture,
  profileListResponse,
  questListResponse,
  questsFixture,
  traderSettingsResponse,
  tradersFixture,
} from "../snapshot/fixtures.js";

const VERSION = "SPT 5.0.0 (BEM) ff0bf32";
const TRADERS_ROUTE = "/client/trading/api/traderSettings";
const QUESTS_ROUTE = "/client/quest/list";
const HIDEOUT_ROUTE = "/client/hideout/areas";
const PROFILE_ROUTE = "/client/game/profile/list";

/** 默认 fake 会话（走 username=Overseer 规则）；快照输出会带上它 */
const SESSION = { source: "username", username: "Overseer", profileId: "fake-profile-id" };

/** 用路由 -> 响应映射构造 fake client；未映射路由视为测试失败 */
function clientFor(routes: Record<string, SptResponse>) {
  return fakeClient(
    new FakeConnection((options) => {
      if (options.path === "/singleplayer/settings/version") {
        return versionResponse(VERSION);
      }
      const response = routes[options.path];
      if (!response) {
        throw new Error(`fake 未预期的路由：${options.path}`);
      }
      return response;
    }),
  );
}

describe("tarkov_snapshot.traders", () => {
  it("正常数据：返回商人总数、assort 总数与按 id 升序的逐商人摘要", async () => {
    const tool = createSnapshotTool(
      clientFor({ [TRADERS_ROUTE]: traderSettingsResponse(tradersFixture()) }),
    );

    const result = await tool({ sections: ["traders"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toEqual({
      schemaVersion: 1,
      session: SESSION,
      sections: {
        traders: {
          meta: { source: "route", route: TRADERS_ROUTE, freshness: "live" },
          traderCount: 3,
          totalAssortItems: 13,
          traders: [
            { id: "trader-fence", standing: 0, loyaltyLevel: 3, assortItems: 0 },
            { id: "trader-prapor", standing: 1.25, loyaltyLevel: 4, assortItems: 10 },
            { id: "trader-therapist", standing: 0.5, loyaltyLevel: 2, assortItems: 3 },
          ],
        },
      },
    });
  });

  it("空数据：无商人时返回零计数摘要", async () => {
    const tool = createSnapshotTool(clientFor({ [TRADERS_ROUTE]: traderSettingsResponse([]) }));

    const result = await tool({ sections: ["traders"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: { traders: { traderCount: 0, totalAssortItems: 0, traders: [] } },
    });
  });

  it("包裹形状：兼容 { traders: [...] } 响应", async () => {
    const body = { traders: tradersFixture() };
    const tool = createSnapshotTool(
      clientFor({ [TRADERS_ROUTE]: { status: 200, body, text: JSON.stringify(body) } }),
    );

    const result = await tool({ sections: ["traders"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ sections: { traders: { traderCount: 3 } } });
  });
});

describe("tarkov_snapshot.quests", () => {
  it("正常数据：返回任务总数与四类状态计数", async () => {
    const tool = createSnapshotTool(clientFor({ [QUESTS_ROUTE]: questListResponse(questsFixture()) }));

    const result = await tool({ sections: ["quests"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toEqual({
      schemaVersion: 1,
      session: SESSION,
      sections: {
        quests: {
          meta: { source: "route", route: QUESTS_ROUTE, freshness: "live" },
          questCount: 5,
          counts: { available: 1, inProgress: 2, completed: 1, failed: 1 },
        },
      },
    });
  });

  it("空数据：新档无任务时返回零计数摘要", async () => {
    const tool = createSnapshotTool(clientFor({ [QUESTS_ROUTE]: questListResponse([]) }));

    const result = await tool({ sections: ["quests"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: {
        quests: {
          questCount: 0,
          counts: { available: 0, inProgress: 0, completed: 0, failed: 0 },
        },
      },
    });
  });
});

describe("tarkov_snapshot.hideout", () => {
  it("正常数据：返回区域数、已建造数、等级总和与按 type 升序的区域等级", async () => {
    const tool = createSnapshotTool(
      clientFor({ [HIDEOUT_ROUTE]: hideoutAreasResponse(hideoutAreasFixture()) }),
    );

    const result = await tool({ sections: ["hideout"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toEqual({
      schemaVersion: 1,
      session: SESSION,
      sections: {
        hideout: {
          meta: { source: "route", route: HIDEOUT_ROUTE, freshness: "live" },
          areaCount: 3,
          builtAreas: 2,
          totalLevel: 4,
          areas: [
            { type: 0, level: 3 },
            { type: 1, level: 0 },
            { type: 2, level: 1 },
          ],
        },
      },
    });
  });

  it("空数据：新档无藏身处升级时返回零计数摘要", async () => {
    const tool = createSnapshotTool(clientFor({ [HIDEOUT_ROUTE]: hideoutAreasResponse([]) }));

    const result = await tool({ sections: ["hideout"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: { hideout: { areaCount: 0, builtAreas: 0, totalLevel: 0, areas: [] } },
    });
  });
});

describe("tarkov_snapshot.inventory", () => {
  it("正常数据：返回物品条目总数与不同模板数", async () => {
    const tool = createSnapshotTool(
      clientFor({ [PROFILE_ROUTE]: profileListResponse([inventoryProfileFixture()]) }),
    );

    const result = await tool({ sections: ["inventory"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toEqual({
      schemaVersion: 1,
      session: SESSION,
      sections: {
        inventory: {
          meta: { source: "route", route: PROFILE_ROUTE, freshness: "live" },
          itemCount: 3,
          distinctTemplates: 2,
        },
      },
    });
  });

  it("空数据：无 profile 时返回零计数摘要", async () => {
    const tool = createSnapshotTool(clientFor({ [PROFILE_ROUTE]: profileListResponse([]) }));

    const result = await tool({ sections: ["inventory"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: { inventory: { itemCount: 0, distinctTemplates: 0 } },
    });
  });

  it("profile 无库存字段：返回零计数摘要", async () => {
    const profile = { info: { id: "x" }, characters: { pmc: { info: { level: 1 } } } };
    const tool = createSnapshotTool(clientFor({ [PROFILE_ROUTE]: profileListResponse([profile]) }));

    const result = await tool({ sections: ["inventory"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: { inventory: { itemCount: 0, distinctTemplates: 0 } },
    });
  });
});

describe("tarkov_snapshot 多选与确定性", () => {
  it("多选组合：单次调用原子返回全部所选 section，字段顺序与入参顺序无关", async () => {
    const tool = createSnapshotTool(
      clientFor({
        [TRADERS_ROUTE]: traderSettingsResponse(tradersFixture()),
        [PROFILE_ROUTE]: profileListResponse([inventoryProfileFixture()]),
      }),
    );

    const result = await tool({ sections: ["inventory", "traders"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const sections = (result.data as { sections: Record<string, unknown> }).sections;
    // 输出顺序遵循 SUPPORTED_SECTIONS，而非入参顺序
    expect(Object.keys(sections)).toEqual(["traders", "inventory"]);
    expect(sections.traders).toMatchObject({ traderCount: 3 });
    expect(sections.inventory).toMatchObject({ itemCount: 3 });
  });

  it("只请求所选 section：不发起未选 section 的路由请求", async () => {
    const connection = new FakeConnection((options) => {
      if (options.path === "/singleplayer/settings/version") {
        return versionResponse(VERSION);
      }
      if (options.path === TRADERS_ROUTE) {
        return traderSettingsResponse(tradersFixture());
      }
      throw new Error(`不应请求未选 section 的路由：${options.path}`);
    });
    const tool = createSnapshotTool(fakeClient(connection));

    const result = await tool({ sections: ["traders"] });

    expect(result.ok).toBe(true);
    // 握手会两次访问版本端点（探测 + 门禁），会话获取访问 /launcher/v2/profiles
    // 与活跃探针 /spt/runtime/active-profiles；此处只关注业务路由
    const businessPaths = connection.requests
      .map((request) => request.path)
      .filter(
        (path) =>
          path !== "/singleplayer/settings/version" &&
          path !== LAUNCHER_PROFILES_PATH &&
          path !== ACTIVE_PROFILES_PATH,
      );
    expect(businessPaths).toEqual([TRADERS_ROUTE]);
  });

  it("相同输入两次调用产出逐字节一致（含全部新 section）", async () => {
    const routes = {
      [PROFILE_ROUTE]: profileListResponse([inventoryProfileFixture()]),
      [TRADERS_ROUTE]: traderSettingsResponse(tradersFixture()),
      [QUESTS_ROUTE]: questListResponse(questsFixture()),
      [HIDEOUT_ROUTE]: hideoutAreasResponse(hideoutAreasFixture()),
    };
    const tool = createSnapshotTool(clientFor(routes));

    const first = await tool({ sections: ["inventory", "quests", "hideout", "traders"] });
    const second = await tool({ sections: ["inventory", "quests", "hideout", "traders"] });

    expect(first.ok && second.ok).toBe(true);
    expect(JSON.stringify(first)).toBe(JSON.stringify(second));
  });
});
