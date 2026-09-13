import { describe, expect, it } from "vitest";

import { DEFAULT_ANCHORED_VERSION } from "../../src/config.js";
import { createRuntime } from "../../src/index.js";
import { createSnapshotTool, SNAPSHOT_TOOL_NAME } from "../../src/tools/snapshot.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";
import {
  hideoutAreasFixture,
  hideoutAreasResponse,
  inventoryProfileFixture,
  pmcProfileFixture,
  profileListResponse,
  questListResponse,
  questsFixture,
  traderSettingsResponse,
  tradersFixture,
} from "../snapshot/fixtures.js";

const VERSION = "SPT 5.0.0 (BEM) ff0bf32";
const PROFILE_ROUTE = "/client/game/profile/list";

/** 构造一个同时响应版本端点与 profile 路由的 fake client */
function clientWith(profiles: unknown[]) {
  return fakeClient(
    new FakeConnection((options) => {
      if (options.path === "/singleplayer/settings/version") {
        return versionResponse(VERSION);
      }
      if (options.path === PROFILE_ROUTE) {
        return profileListResponse(profiles);
      }
      throw new Error(`fake 未预期的路由：${options.path}`);
    }),
  );
}

/** 构造响应全部已实现 section 路由的 fake client（默认 sections 场景） */
function fullClient() {
  return fakeClient(
    new FakeConnection((options) => {
      switch (options.path) {
        case "/singleplayer/settings/version":
          return versionResponse(VERSION);
        case PROFILE_ROUTE:
          return profileListResponse([inventoryProfileFixture()]);
        case "/client/trading/api/traderSettings":
          return traderSettingsResponse(tradersFixture());
        case "/client/quest/list":
          return questListResponse(questsFixture());
        case "/client/hideout/areas":
          return hideoutAreasResponse(hideoutAreasFixture());
        default:
          throw new Error(`fake 未预期的路由：${options.path}`);
      }
    }),
  );
}

describe("tarkov_snapshot", () => {
  it("sections=['profile'] 返回等级/技能/任务进度计数摘要", async () => {
    const tool = createSnapshotTool(clientWith([pmcProfileFixture()]));

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.tool).toBe(SNAPSHOT_TOOL_NAME);
    expect(result.data).toMatchObject({
      schemaVersion: 1,
      sections: {
        profile: {
          meta: { source: "route", route: PROFILE_ROUTE, freshness: "live" },
          profileCount: 1,
          pmc: {
            level: 42,
            experience: 1234567,
            skills: { total: 3, leveled: 2 },
            quests: { available: 1, inProgress: 2, completed: 1, failed: 1 },
          },
        },
      },
    });
  });

  it("省略 sections 时默认读取全部已实现 section", async () => {
    const tool = createSnapshotTool(fullClient());

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: {
        profile: { profileCount: 1 },
        traders: { traderCount: 3 },
        quests: { questCount: 5 },
        hideout: { areaCount: 3 },
        inventory: { itemCount: 3 },
      },
    });
  });

  it("空 profile 列表：返回零计数摘要（pmc=null）", async () => {
    const tool = createSnapshotTool(clientWith([]));

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      sections: {
        profile: {
          meta: { source: "route", route: PROFILE_ROUTE },
          profileCount: 0,
          pmc: null,
        },
      },
    });
  });

  it("profile 条目缺少 characters.pmc：pmc=null 但 profileCount 保留", async () => {
    const tool = createSnapshotTool(clientWith([{ info: { id: "x" }, characters: {} }]));

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ sections: { profile: { profileCount: 1, pmc: null } } });
  });

  it("相同输入两次调用产出逐字节一致", async () => {
    const tool = createSnapshotTool(clientWith([pmcProfileFixture()]));

    const first = await tool({ sections: ["profile"] });
    const second = await tool({ sections: ["profile"] });

    expect(first.ok && second.ok).toBe(true);
    expect(JSON.stringify(first)).toBe(JSON.stringify(second));
  });

  it("未知 section：返回结构化 UNSUPPORTED_SECTION，且不发起路由请求", async () => {
    const connection = new FakeConnection((options) => {
      if (options.path === "/singleplayer/settings/version") {
        return versionResponse(VERSION);
      }
      throw new Error("不应在 section 校验失败时发起 profile 路由请求");
    });
    const tool = createSnapshotTool(fakeClient(connection));

    const result = await tool({ sections: ["raid"] });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.UNSUPPORTED_SECTION);
    expect(result.details).toEqual({
      unsupported: ["raid"],
      supported: ["profile", "traders", "quests", "hideout", "inventory"],
    });
  });

  it("版本不匹配：透传 VERSION_MISMATCH（快照不建立在错误版本上）", async () => {
    const tool = createSnapshotTool(
      fakeClient(new FakeConnection(() => versionResponse("SPT 4.1.5 (BE) abc1234"))),
    );

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.VERSION_MISMATCH);
  });

  it("非法输入：返回 INVALID_INPUT", async () => {
    const tool = createSnapshotTool(clientWith([pmcProfileFixture()]));

    const result = await tool({ sections: "profile" });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
  });

  it("经 dispatcher 注册可用：tarkov_snapshot 被路由到快照工具", async () => {
    const connection = new FakeConnection((options) => {
      if (options.path === "/singleplayer/settings/version") {
        return versionResponse(VERSION);
      }
      return profileListResponse([pmcProfileFixture()]);
    });
    const { invoke } = createRuntime({
      config: {
        host: "127.0.0.1",
        candidatePorts: [6969],
        anchorVersion: DEFAULT_ANCHORED_VERSION,
        username: "Overseer",
      },
      connect: () => connection,
    });

    const result = await invoke(SNAPSHOT_TOOL_NAME, { sections: ["profile"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ sections: { profile: { profileCount: 1 } } });
  });
});
