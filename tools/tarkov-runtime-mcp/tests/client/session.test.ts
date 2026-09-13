// =============================================================================
// 会话获取：`/launcher/v2/profiles` 解析 profileId 并注入 PHPSESSID
//
// 只测外部行为：会话是否建立、错误码、是否静默空数据。
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
function snapshotConnection(session: Parameters<typeof fakeClient>[2] = { username: "Overseer" }) {
  const connection = new FakeConnection((options) => {
    if (options.path === "/singleplayer/settings/version") {
      return versionResponse(VERSION);
    }
    if (options.path === PROFILE_ROUTE) {
      return profileListResponse([pmcProfileFixture()]);
    }
    throw new Error(`fake 未预期的路由：${options.path}`);
  });
  return { connection, client: fakeClient(connection, undefined, session) };
}

describe("acquireSession", () => {
  it("username 匹配：返回 profileId 并注入到连接（后续请求带 PHPSESSID）", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION));

    const session = await acquireSession(connection, { username: "Overseer" });

    expect(session).toEqual({ username: "Overseer", profileId: "fake-profile-id" });
    expect(connection.getSessionId()).toBe("fake-profile-id");
  });

  it("未配置 username：抛 SESSION_NOT_CONFIGURED", async () => {
    const connection = new FakeConnection(() => versionResponse(VERSION));

    await expect(acquireSession(connection, {})).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.SESSION_NOT_CONFIGURED,
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

  it("未配置 username：返回结构化 SESSION_NOT_CONFIGURED，不静默返回空数据", async () => {
    const { client } = snapshotConnection({});
    const tool = createSnapshotTool(client);

    const result = await tool({ sections: ["profile"] });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.SESSION_NOT_CONFIGURED);
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
