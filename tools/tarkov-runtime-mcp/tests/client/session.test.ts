// =============================================================================
// 会话获取：活跃探针优先 -> username 精确匹配 -> 单 profile 自动 -> 多 profile 歧义
//
// 只测外部行为：会话来源、错误码、是否静默空数据。
// =============================================================================

import { describe, expect, it } from "vitest";

import { acquireSession } from "../../src/client/session.js";
import { createSnapshotTool } from "../../src/tools/snapshot.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";
import { pmcProfileFixture, profileListResponse } from "../snapshot/fixtures.js";

const VERSION = "SPT 5.0.0 (BEM) ff0bf32";
const PROFILE_ROUTE = "/client/game/profile/list";

/** 仅响应版本与 profile 路由的 fake 连接 */
function snapshotConnection(
  session: Parameters<typeof fakeClient>[2] = { username: "Overseer" },
  fakeSession?: ConstructorParameters<typeof FakeConnection>[3],
) {
  const connection = new FakeConnection((options) => {
    if (options.path === "/singleplayer/settings/version") {
      return versionResponse(VERSION);
    }
    if (options.path === PROFILE_ROUTE) {
      return profileListResponse([pmcProfileFixture()]);
    }
    throw new Error(`fake 未预期的路由：${options.path}`);
  }, "127.0.0.1", 6969, fakeSession);
  return { connection, client: fakeClient(connection, undefined, session) };
}

describe("acquireSession — 选择规则", () => {
  it("活跃探针恰有 1 个活跃 profile：跟随玩家（source=active-probe），即使存在多个 profile", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION), "127.0.0.1", 6969, {
      profiles: [
        { username: "Overseer", profileId: "id-a" },
        { username: "Samuel", profileId: "id-b" },
      ],
      activeProfilesResponse: { activeProfiles: ["id-b"] },
    });

    const session = await acquireSession(connection, {});

    expect(session).toEqual({ username: "Samuel", profileId: "id-b", source: "active-probe" });
    expect(connection.getSessionId()).toBe("id-b");
  });

  it("探针返回多个活跃 profile：退回后续规则（单 profile 不适用时报歧义）", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION), "127.0.0.1", 6969, {
      profiles: [
        { username: "Overseer", profileId: "id-a" },
        { username: "Samuel", profileId: "id-b" },
      ],
      activeProfilesResponse: { activeProfiles: ["id-a", "id-b"] },
    });

    await expect(acquireSession(connection, {})).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.AMBIGUOUS_PROFILE,
    });
  });

  it("探针未安装（UNHANDLED 信封）：静默退回，不报错", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION));
    // 默认 fakeSession 的探针返回 UNHANDLED 信封；单 profile 应自动选择
    const session = await acquireSession(connection, {});

    expect(session.source).toBe("auto-single");
  });

  it("username 匹配：返回 profileId 并注入到连接（source=username）", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION));

    const session = await acquireSession(connection, { username: "Overseer" });

    expect(session).toEqual({
      username: "Overseer",
      profileId: "fake-profile-id",
      source: "username",
    });
    expect(connection.getSessionId()).toBe("fake-profile-id");
  });

  it("未配置 username + 单 profile：零配置自动选用（source=auto-single）", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION));

    const session = await acquireSession(connection, {});

    expect(session).toEqual({
      username: "Overseer",
      profileId: "fake-profile-id",
      source: "auto-single",
    });
    expect(connection.getSessionId()).toBe("fake-profile-id");
  });

  it("未配置 username + 多 profile：抛 AMBIGUOUS_PROFILE 并列出候选", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION), "127.0.0.1", 6969, {
      profiles: [
        { username: "Overseer", profileId: "id-a" },
        { username: "Samuel", profileId: "id-b" },
      ],
    });

    await expect(acquireSession(connection, {})).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.AMBIGUOUS_PROFILE,
      details: { candidates: ["Overseer", "Samuel"] },
    });
  });

  it("server 无任何 profile：抛 PROFILE_NOT_FOUND", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION), "127.0.0.1", 6969, {
      profiles: [],
    });

    await expect(acquireSession(connection, {})).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.PROFILE_NOT_FOUND,
    });
  });

  it("无匹配 profile：抛 PROFILE_NOT_FOUND", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION), "127.0.0.1", 6969, {
      username: "SomeoneElse",
    });

    await expect(acquireSession(connection, { username: "Overseer" })).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.PROFILE_NOT_FOUND,
    });
  });

  it("配置 password 且登录成功：建立会话", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION));

    const session = await acquireSession(connection, { username: "Overseer", password: "secret" });

    expect(session.profileId).toBe("fake-profile-id");
  });

  it("配置 password 且登录失败：抛 AUTH_FAILED", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION), "127.0.0.1", 6969, {
      loginResponse: false,
    });

    await expect(
      acquireSession(connection, { username: "Overseer", password: "wrong" }),
    ).rejects.toMatchObject({ code: RUNTIME_ERROR_CODES.AUTH_FAILED });
  });
});

describe("tarkov_snapshot 会话受限行为", () => {
  it("已配置 username：建立会话后正常返回数据", async () => {
    const { client } = snapshotConnection();
    const tool = createSnapshotTool(client);

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ sections: { profile: { profileCount: 1 } } });
  });

  it("未配置 username + 单 profile：自动选用，正常返回数据", async () => {
    const { client } = snapshotConnection({});
    const tool = createSnapshotTool(client);

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ sections: { profile: { profileCount: 1 } } });
  });

  it("未配置 username + 多 profile：返回结构化 AMBIGUOUS_PROFILE，不静默选目标", async () => {
    const { client } = snapshotConnection({}, {
      profiles: [
        { username: "Overseer", profileId: "id-a" },
        { username: "Samuel", profileId: "id-b" },
      ],
    });
    const tool = createSnapshotTool(client);

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.AMBIGUOUS_PROFILE);
    expect(result.data).toBeUndefined();
  });

  it("username 匹配失败：返回结构化 PROFILE_NOT_FOUND", async () => {
    const connection = new FakeConnection(
      (options) => {
        if (options.path === "/singleplayer/settings/version") {
          return versionResponse(VERSION);
        }
        throw new Error(`fake 未预期的路由：${options.path}`);
      },
      "127.0.0.1",
      6969,
      { username: "SomeoneElse" },
    );
    const tool = createSnapshotTool(fakeClient(connection, undefined, { username: "Overseer" }));

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.PROFILE_NOT_FOUND);
  });
});
