import { describe, expect, it } from "vitest";

import { createRaidEventsTool } from "../../src/tools/raid-events.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import {
  defaultBridgeInfo,
  fakeBridge,
  inRaidEvents,
  unreachable,
} from "../helpers/fake-bridge.js";

describe("raid_events（BridgeConnection 驱动）", () => {
  it("在 raid：返回 ok 信封，data 含 inRaid/seq/dropped/events（确定性字段序）", async () => {
    const tool = createRaidEventsTool(fakeBridge({ events: inRaidEvents() }));

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.tool).toBe("raid_events");
    expect(result.data).toEqual({
      inRaid: true,
      seq: 3,
      dropped: 0,
      events: [
        {
          seq: 1,
          ts: "2026-09-15T10:00:00.000Z",
          type: "damage",
          raidId: "raid-abc",
          payload: {
            victimProfileId: "pmc-local",
            victimIsLocal: true,
            part: "LeftLeg",
            amount: 12.5,
            sourceType: "Bullet",
          },
        },
        {
          seq: 2,
          ts: "2026-09-15T10:00:05.000Z",
          type: "death",
          raidId: "raid-abc",
          payload: {
            victimProfileId: "bot-1",
            victimIsLocal: false,
            damageType: "Bullet",
            killer: {
              profileId: "pmc-local",
              name: "LocalPMC",
              side: "Bear",
              role: "pmc",
              isLocal: true,
            },
          },
        },
        {
          seq: 3,
          ts: "2026-09-15T10:05:00.000Z",
          type: "extraction",
          raidId: "raid-abc",
          payload: { exitName: "Crossroads", status: "Success" },
        },
      ],
    });
  });

  it("输出确定性：相同输入产生完全相同的 JSON 与稳定字段序", async () => {
    const tool = createRaidEventsTool(fakeBridge({ events: inRaidEvents() }));

    const first = await tool({});
    const second = await tool({});

    expect(JSON.stringify(first)).toBe(JSON.stringify(second));
    if (!first.ok) return;
    expect(Object.keys(first.data as object)).toEqual(["inRaid", "seq", "dropped", "events"]);
    const events = (first.data as { events: object[] }).events;
    expect(Object.keys(events[0])).toEqual(["seq", "ts", "type", "raidId", "payload"]);
    expect(Object.keys((events[0] as { payload: object }).payload)).toEqual([
      "victimProfileId",
      "victimIsLocal",
      "part",
      "amount",
      "sourceType",
    ]);
    expect(Object.keys((events[1] as { payload: { killer: object } }).payload.killer)).toEqual([
      "profileId",
      "name",
      "side",
      "role",
      "isLocal",
    ]);
    expect(Object.keys((events[2] as { payload: object }).payload)).toEqual([
      "exitName",
      "status",
    ]);
  });

  it("killer 为 null：原样输出 null（不猜归属）", async () => {
    const tool = createRaidEventsTool(
      fakeBridge({
        events: inRaidEvents({
          seq: 1,
          dropped: 0,
          events: [
            {
              seq: 1,
              ts: "2026-09-15T10:00:05.000Z",
              type: "death",
              raidId: "raid-abc",
              payload: {
                victimProfileId: "bot-1",
                victimIsLocal: false,
                damageType: "Fall",
                killer: null,
              },
            },
          ],
        }),
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const events = (result.data as { events: Array<{ payload: { killer: unknown } }> }).events;
    expect(events[0].payload.killer).toBeNull();
  });

  it("since/limit 透传到桥连接", async () => {
    const bridge = fakeBridge({ events: inRaidEvents() });
    const tool = createRaidEventsTool(bridge);

    await tool({ since: 42, limit: 10 });

    expect(bridge.lastEventsArgs).toEqual({ since: 42, limit: 10 });
  });

  it("空事件：events 为空数组且 seq/dropped 保留", async () => {
    const tool = createRaidEventsTool(
      fakeBridge({ events: { inRaid: true, seq: 7, dropped: 2, events: [] } }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toEqual({ inRaid: true, seq: 7, dropped: 2, events: [] });
  });

  it("不在 raid：返回 ok 信封，inRaid:false 且缓冲事件保留（赛后时间线可读）", async () => {
    const tool = createRaidEventsTool(
      fakeBridge({
        events: {
          inRaid: false,
          seq: 9,
          dropped: 0,
          events: [
            {
              seq: 9,
              ts: "2026-09-15T10:05:00.000Z",
              type: "extraction",
              raidId: "raid-abc",
              payload: { exitName: "Crossroads", status: "Success" },
            },
          ],
        },
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.tool).toBe("raid_events");
    expect(result.data).toEqual({
      inRaid: false,
      seq: 9,
      dropped: 0,
      events: [
        {
          seq: 9,
          ts: "2026-09-15T10:05:00.000Z",
          type: "extraction",
          raidId: "raid-abc",
          payload: { exitName: "Crossroads", status: "Success" },
        },
      ],
    });
  });

  it("bridge 不可达（info 失败）：返回 BRIDGE_UNREACHABLE", async () => {
    const tool = createRaidEventsTool(fakeBridge({ info: unreachable("connect ECONNREFUSED") }));

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE);
    expect(result.tool).toBe("raid_events");
  });

  it("bridge 不可达（端点失败）：返回 BRIDGE_UNREACHABLE", async () => {
    const tool = createRaidEventsTool(
      fakeBridge({ info: defaultBridgeInfo(), events: unreachable("socket hang up") }),
    );

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE);
  });

  it("协议版本不一致：返回 BRIDGE_VERSION_MISMATCH（含 expected/actual）", async () => {
    const tool = createRaidEventsTool(
      fakeBridge({ info: defaultBridgeInfo({ protocolVersion: 2 }) }),
    );

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_VERSION_MISMATCH);
    expect(result.details).toEqual({ expected: 1, actual: 2 });
  });

  it("非法输入（since 负数 / limit 非正 / 未知字段）：返回 INVALID_INPUT", async () => {
    const tool = createRaidEventsTool(fakeBridge());

    const negativeSince = await tool({ since: -1 });
    const zeroLimit = await tool({ limit: 0 });
    const unknown = await tool({ unexpected: true });

    for (const result of [negativeSince, zeroLimit, unknown]) {
      expect(result.ok).toBe(false);
      if (result.ok) return;
      expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
    }
  });
});
