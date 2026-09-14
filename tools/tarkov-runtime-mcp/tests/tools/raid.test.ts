import { describe, expect, it } from "vitest";

import { TOOL_DEFINITIONS, createDispatcher, createRuntime } from "../../src/index.js";
import { RAID_TOOL_NAMES } from "../../src/tools/raid.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";
import {
  defaultBridgeInfo,
  fakeBridge,
  inRaidBotsSummary,
  inRaidPlayer,
  inRaidStatus,
  unreachable,
} from "../helpers/fake-bridge.js";

function dispatcher(
  bridge = fakeBridge({
    status: inRaidStatus(),
    player: inRaidPlayer(),
    bots: inRaidBotsSummary(),
  }),
) {
  return createDispatcher(
    fakeClient(new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32"))),
    bridge,
  );
}

describe("raid.* 命名空间（Phase 2 全部真实化）", () => {
  it("工具定义包含全部 raid.* 工具", () => {
    const names = TOOL_DEFINITIONS.map((tool) => tool.name);
    for (const name of RAID_TOOL_NAMES) {
      expect(names).toContain(name);
    }
  });

  it("raid.* 工具描述不再含「占位」", () => {
    for (const name of RAID_TOOL_NAMES) {
      const tool = TOOL_DEFINITIONS.find((definition) => definition.name === name);
      expect(tool?.description).not.toContain("占位");
    }
  });

  it("raid_status 经 bridge 返回元数据 + 桥自报", async () => {
    const result = await dispatcher()("raid_status", {});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      raidId: "raid-abc",
      bridge: { pluginVersion: "0.1.0", protocolVersion: 1 },
    });
  });

  it("raid_player 经 bridge 返回真实玩家状态", async () => {
    const result = await dispatcher()("raid_player", {});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ pose: "stand", sampleAgeMs: 100 });
  });

  it("raid_bots 经 bridge 返回摘要", async () => {
    const result = await dispatcher()("raid_bots", {});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ total: 10, alive: 8 });
  });

  it("raid_bots detail=true 经 dispatcher 透传明细", async () => {
    const bridge = fakeBridge({
      bots: {
        inRaid: true,
        detail: true,
        total: 1,
        alive: 1,
        byCategory: { pmc: 1, scav: 0, boss: 0, other: 0 },
        spawner: { aliveAndLoading: 1, delayed: 0, allWithDelayed: 1 },
        sampleAgeMs: 5,
        bots: [{ x: 1, y: 2, z: 3, role: "pmc", side: "Bear", alive: true }],
        truncated: false,
      },
    });

    const result = await dispatcher(bridge)("raid_bots", { detail: true });

    expect(result.ok).toBe(true);
    expect(bridge.lastBotsDetail).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ truncated: false });
  });

  it("bridge 不可达：dispatcher 返回 BRIDGE_UNREACHABLE", async () => {
    const result = await dispatcher(fakeBridge({ info: unreachable("connect ECONNREFUSED") }))(
      "raid_player",
      {},
    );

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE);
  });

  it("协议版本不一致：dispatcher 返回 BRIDGE_VERSION_MISMATCH", async () => {
    const result = await dispatcher(fakeBridge({ info: defaultBridgeInfo({ protocolVersion: 3 }) }))(
      "raid_status",
      {},
    );

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_VERSION_MISMATCH);
  });

  it("createRuntime 注入 bridge：raid_player 经注入连接读取", async () => {
    const { invoke } = createRuntime({
      config: {
        host: "127.0.0.1",
        candidatePorts: [6969],
        anchorVersion: "5.0.0-BEM-20260914",
      },
      connect: () => new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32")),
      bridge: fakeBridge({ player: inRaidPlayer({ position: { x: 7, y: 8, z: 9 } }) }),
    });

    const result = await invoke("raid_player", {});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ position: { x: 7, y: 8, z: 9 } });
  });

  it("未注册的 raid.* 前缀调用仍返回 CLIENT_BRIDGE_NOT_INSTALLED", async () => {
    const result = await dispatcher()("raid_teleport", {});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.CLIENT_BRIDGE_NOT_INSTALLED);
  });

  it("非 raid 的未知工具返回 INVALID_INPUT", async () => {
    const result = await dispatcher()("totally_unknown", {});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
  });
});
