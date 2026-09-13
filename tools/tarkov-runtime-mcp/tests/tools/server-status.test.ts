import { describe, expect, it } from "vitest";

import { DEFAULT_ANCHORED_VERSION } from "../../src/config.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import { createServerStatusTool } from "../../src/tools/server-status.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";

const ANCHOR = DEFAULT_ANCHORED_VERSION;

describe("tarkov_server_status", () => {
  it("握手成功：返回版本、门禁结果与连接信息", async () => {
    const client = fakeClient(new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32")));
    const tool = createServerStatusTool(client);

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      connection: { host: "127.0.0.1", port: 6969, baseUrl: "https://127.0.0.1:6969" },
      version: { core: "5.0.0", channel: "BEM", commit: "ff0bf32" },
      anchor: ANCHOR,
      gate: { passed: true, anchor: ANCHOR },
      capabilities: { bridge: "not_installed" },
    });
  });

  it("版本不匹配：返回结构化 VERSION_MISMATCH（含期望/实际）", async () => {
    const client = fakeClient(new FakeConnection(() => versionResponse("SPT 4.1.5 (BE) abc1234")));
    const tool = createServerStatusTool(client);

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.VERSION_MISMATCH);
    expect(result.details).toEqual({ expected: ANCHOR, actual: "SPT 4.1.5 (BE) abc1234" });
  });

  it("不可达：返回结构化 SERVER_UNREACHABLE", async () => {
    const client = fakeClient(new FakeConnection(() => new Error("connect ECONNREFUSED")));
    const tool = createServerStatusTool(client);

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.SERVER_UNREACHABLE);
  });

  it("非法输入：返回 INVALID_INPUT", async () => {
    const client = fakeClient(new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32")));
    const tool = createServerStatusTool(client);

    const result = await tool({ unexpected: true });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
  });
});
