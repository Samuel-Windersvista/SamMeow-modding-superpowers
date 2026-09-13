import { describe, expect, it } from "vitest";

import { performHandshake } from "../../src/client/handshake.js";
import { DEFAULT_ANCHORED_VERSION } from "../../src/config.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import { FakeConnection, versionResponse } from "../helpers/fake-connection.js";

const ANCHOR = DEFAULT_ANCHORED_VERSION;

describe("performHandshake: 成功路径（fake SptConnection 驱动）", () => {
  it("解析出 SPT 版本并通过门禁", async () => {
    const connection = new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32"));

    const result = await performHandshake(connection, ANCHOR);

    expect(result.version).toMatchObject({
      raw: "SPT 5.0.0 (BEM) ff0bf32",
      project: "SPT",
      core: "5.0.0",
      channel: "BEM",
      commit: "ff0bf32",
    });
    expect(result.gate.passed).toBe(true);
    expect(result.anchor.raw).toBe(ANCHOR);
    expect(result.baseUrl).toBe("https://127.0.0.1:6969");
    expect(connection.requests[0]).toMatchObject({
      method: "GET",
      path: "/singleplayer/settings/version",
    });
  });

  it("兼容 camelCase 的 version 字段", async () => {
    const connection = new FakeConnection(() => ({
      status: 200,
      body: { version: "SPT 5.0.0 (BEM) abc1234" },
      text: '{"version":"SPT 5.0.0 (BEM) abc1234"}',
    }));

    const result = await performHandshake(connection, ANCHOR);
    expect(result.version.core).toBe("5.0.0");
    expect(result.version.channel).toBe("BEM");
  });
});

describe("performHandshake: 版本不匹配路径", () => {
  it("版本线不同：抛 VERSION_MISMATCH 且含期望/实际", async () => {
    const connection = new FakeConnection(() => versionResponse("SPT 4.1.5 (BE) abc1234"));

    await expect(performHandshake(connection, ANCHOR)).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.VERSION_MISMATCH,
      details: { expected: ANCHOR, actual: "SPT 4.1.5 (BE) abc1234" },
    });
  });

  it("构建通道不同（正式版 vs 锚定 BEM）：抛 VERSION_MISMATCH", async () => {
    const connection = new FakeConnection(() => versionResponse("SPT 5.0.0"));

    await expect(performHandshake(connection, ANCHOR)).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.VERSION_MISMATCH,
      details: { expected: ANCHOR, actual: "SPT 5.0.0" },
    });
  });

  it("标签无法解析：抛 VERSION_MISMATCH", async () => {
    const connection = new FakeConnection(() => ({
      status: 200,
      body: { Version: "definitely-not-a-version" },
      text: '{"Version":"definitely-not-a-version"}',
    }));

    await expect(performHandshake(connection, ANCHOR)).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.VERSION_MISMATCH,
      details: { expected: ANCHOR, actual: "definitely-not-a-version" },
    });
  });
});

describe("performHandshake: 不可达路径", () => {
  it("连接抛错：抛 SERVER_UNREACHABLE", async () => {
    const connection = new FakeConnection(() => new Error("ECONNREFUSED"));

    await expect(performHandshake(connection, ANCHOR)).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.SERVER_UNREACHABLE,
    });
  });

  it("HTTP 非 2xx：抛 SERVER_UNREACHABLE", async () => {
    const connection = new FakeConnection(() => ({ status: 500, body: null, text: "" }));

    await expect(performHandshake(connection, ANCHOR)).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.SERVER_UNREACHABLE,
      details: { baseUrl: "https://127.0.0.1:6969", status: 500 },
    });
  });
});
