import { describe, expect, it } from "vitest";

import { createRaidStatusTool } from "../../src/tools/raid-status.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import { defaultBridgeInfo, fakeBridge, inRaidStatus, unreachable } from "../helpers/fake-bridge.js";

describe("raid_status（BridgeConnection 驱动）", () => {
  it("在 raid：返回 raid 元数据 + 桥自报", async () => {
    const tool = createRaidStatusTool(
      fakeBridge({
        info: defaultBridgeInfo({ pluginVersion: "0.2.1", sampling: { intervalMs: 500 } }),
        status: inRaidStatus({
          map: "Customs",
          status: "running",
          remainingSeconds: 900,
          raidId: "raid-42",
          sampleAgeMs: 77,
        }),
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.tool).toBe("raid_status");
    expect(result.data).toEqual({
      map: "Customs",
      status: "running",
      remainingSeconds: 900,
      raidId: "raid-42",
      sampleAgeMs: 77,
      bridge: {
        pluginVersion: "0.2.1",
        protocolVersion: 1,
        samplingIntervalMs: 500,
      },
    });
  });

  it("不在 raid：返回 NOT_IN_RAID 错误信封，并附带桥自报（桥可达且协议通过）", async () => {
    const tool = createRaidStatusTool(
      fakeBridge({
        info: defaultBridgeInfo({ pluginVersion: "0.2.1", sampling: { intervalMs: 500 } }),
        status: { inRaid: false },
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.NOT_IN_RAID);
    expect(result.details).toEqual({
      bridge: {
        pluginVersion: "0.2.1",
        protocolVersion: 1,
        samplingIntervalMs: 500,
      },
    });
  });

  it("bridge 不可达：返回 BRIDGE_UNREACHABLE（含排查提示，无 bridge 字段）", async () => {
    const tool = createRaidStatusTool(fakeBridge({ info: unreachable("ECONNREFUSED") }));

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE);
    expect(result.message).toContain("未运行");
    expect(result.message).toContain("BepInEx");
    expect(result.details).not.toHaveProperty("bridge");
  });

  it("协议版本不一致：返回 BRIDGE_VERSION_MISMATCH", async () => {
    const tool = createRaidStatusTool(
      fakeBridge({ info: defaultBridgeInfo({ protocolVersion: 9 }) }),
    );

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_VERSION_MISMATCH);
    expect(result.details).toEqual({ expected: 1, actual: 9 });
  });

  it("输出确定性：相同输入逐字节稳定且字段序固定", async () => {
    const tool = createRaidStatusTool(fakeBridge({ status: inRaidStatus() }));

    const first = await tool({});
    const second = await tool({});

    expect(JSON.stringify(first)).toBe(JSON.stringify(second));
    if (!first.ok) return;
    expect(Object.keys(first.data as object)).toEqual([
      "map",
      "status",
      "remainingSeconds",
      "raidId",
      "sampleAgeMs",
      "bridge",
    ]);
    expect(Object.keys((first.data as { bridge: object }).bridge)).toEqual([
      "pluginVersion",
      "protocolVersion",
      "samplingIntervalMs",
    ]);
  });

  it("非法输入：返回 INVALID_INPUT", async () => {
    const tool = createRaidStatusTool(fakeBridge());

    const result = await tool({ unexpected: true });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
  });
});
