import { describe, expect, it } from "vitest";

import { SptClient } from "../../src/client/client.js";
import { DEFAULT_ANCHORED_VERSION } from "../../src/config.js";
import { createInstancesTool } from "../../src/tools/instances.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";

const ANCHOR = DEFAULT_ANCHORED_VERSION;

describe("tarkov_instances", () => {
  it("单候选端口可达：返回单实例信息", async () => {
    const client = fakeClient(new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32")));
    const tool = createInstancesTool(client);

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      count: 1,
      model: "single-instance",
      instances: [
        {
          host: "127.0.0.1",
          port: 6969,
          baseUrl: "https://127.0.0.1:6969",
          versionLabel: "SPT 5.0.0 (BEM) ff0bf32",
          reachable: true,
        },
      ],
    });
    // 内部连接对象不得泄漏到工具输出
    expect(JSON.stringify(result.data)).not.toContain("connection");
  });

  it("无端口可达：返回空实例列表（不抛错）", async () => {
    const client = fakeClient(new FakeConnection(() => new Error("ECONNREFUSED")));
    const tool = createInstancesTool(client);

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ count: 0, instances: [] });
  });

  it("多候选端口：跳过不可达端口，返回命中的端口", async () => {
    const client = new SptClient({
      host: "127.0.0.1",
      candidatePorts: [6969, 6970],
      anchorVersion: ANCHOR,
      connect: (_host, port) =>
        port === 6970
          ? new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32"), "127.0.0.1", 6970)
          : new FakeConnection(() => new Error("ECONNREFUSED"), "127.0.0.1", 6969),
    });
    const tool = createInstancesTool(client);

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ count: 1, instances: [{ port: 6970 }] });
  });
});
