import { describe, expect, it } from "vitest";

import { createRaidBotsTool } from "../../src/tools/raid-bots.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import {
  defaultBridgeInfo,
  fakeBridge,
  inRaidBotsDetail,
  inRaidBotsSummary,
  unreachable,
} from "../helpers/fake-bridge.js";

describe("raid_bots（BridgeConnection 驱动）", () => {
  it("摘要模式：返回计数与生成器信息（detail 缺省为 false）", async () => {
    const bridge = fakeBridge({
      bots: inRaidBotsSummary({
        total: 12,
        alive: 9,
        byCategory: { pmc: 5, scav: 6, boss: 1, other: 0 },
        spawner: { aliveAndLoading: 11, delayed: 1, allWithDelayed: 12 },
        sampleAgeMs: 250,
      }),
    });
    const tool = createRaidBotsTool(bridge);

    const result = await tool({});

    expect(result.ok).toBe(true);
    expect(bridge.lastBotsDetail).toBe(false);
    if (!result.ok) return;
    expect(result.tool).toBe("raid_bots");
    expect(result.data).toEqual({
      total: 12,
      alive: 9,
      byCategory: { pmc: 5, scav: 6, boss: 1, other: 0 },
      spawner: { aliveAndLoading: 11, delayed: 1, allWithDelayed: 12 },
      sampleAgeMs: 250,
    });
  });

  it("明细模式：detail=true 透传并返回 bots 与 truncated", async () => {
    const bridge = fakeBridge({ bots: inRaidBotsDetail() });
    const tool = createRaidBotsTool(bridge);

    const result = await tool({ detail: true });

    expect(result.ok).toBe(true);
    expect(bridge.lastBotsDetail).toBe(true);
    if (!result.ok) return;
    expect(result.data).toEqual({
      total: 2,
      alive: 1,
      byCategory: { pmc: 1, scav: 1, boss: 0, other: 0 },
      spawner: { aliveAndLoading: 2, delayed: 0, allWithDelayed: 2 },
      sampleAgeMs: 100,
      bots: [
        { x: 10, y: 0, z: 20, role: "pmc", side: "Bear", alive: true },
        { x: 30, y: 0, z: 40, role: "scav", side: "Savage", alive: false },
      ],
      truncated: false,
    });
  });

  it("边界：0 bot 摘要仍为 ok", async () => {
    const tool = createRaidBotsTool(
      fakeBridge({
        bots: inRaidBotsSummary({
          total: 0,
          alive: 0,
          byCategory: { pmc: 0, scav: 0, boss: 0, other: 0 },
          spawner: { aliveAndLoading: 0, delayed: 0, allWithDelayed: 0 },
        }),
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect((result.data as { total: number }).total).toBe(0);
  });

  it("边界：大量 bot 明细携带 truncated 标记", async () => {
    const bots = Array.from({ length: 200 }, (_, index) => ({
      x: index,
      y: 0,
      z: index,
      role: "scav",
      side: "Savage",
      alive: index % 2 === 0,
    }));
    const tool = createRaidBotsTool(
      fakeBridge({ bots: inRaidBotsDetail({ total: 500, bots, truncated: true }) }),
    );

    const result = await tool({ detail: true });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as { bots: unknown[]; truncated: boolean };
    expect(data.bots).toHaveLength(200);
    expect(data.truncated).toBe(true);
  });

  it("不在 raid：返回 NOT_IN_RAID 错误信封", async () => {
    const tool = createRaidBotsTool(fakeBridge({ bots: { inRaid: false } }));

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.NOT_IN_RAID);
  });

  it("bridge 不可达：返回 BRIDGE_UNREACHABLE", async () => {
    const tool = createRaidBotsTool(fakeBridge({ info: unreachable("ECONNREFUSED") }));

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE);
    expect(result.message).toContain("启动失败");
    expect(result.message).toContain("BepInEx");
  });

  it("协议版本不一致：返回 BRIDGE_VERSION_MISMATCH", async () => {
    const tool = createRaidBotsTool(fakeBridge({ info: defaultBridgeInfo({ protocolVersion: 0 }) }));

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_VERSION_MISMATCH);
    expect(result.details).toEqual({ expected: 1, actual: 0 });
  });

  it("输出确定性：摘要与明细分别逐字节稳定且字段序固定", async () => {
    const summaryTool = createRaidBotsTool(fakeBridge({ bots: inRaidBotsSummary() }));
    const first = await summaryTool({});
    const second = await summaryTool({});
    expect(JSON.stringify(first)).toBe(JSON.stringify(second));
    if (!first.ok) return;
    expect(Object.keys(first.data as object)).toEqual([
      "total",
      "alive",
      "byCategory",
      "spawner",
      "sampleAgeMs",
    ]);

    const detailTool = createRaidBotsTool(fakeBridge({ bots: inRaidBotsDetail() }));
    const detail = await detailTool({ detail: true });
    if (!detail.ok) return;
    expect(Object.keys(detail.data as object)).toEqual([
      "total",
      "alive",
      "byCategory",
      "spawner",
      "sampleAgeMs",
      "bots",
      "truncated",
    ]);
    const bots = (detail.data as { bots: object[] }).bots;
    expect(Object.keys(bots[0])).toEqual(["x", "y", "z", "role", "side", "alive"]);
  });

  it("非法输入：detail 非布尔返回 INVALID_INPUT", async () => {
    const tool = createRaidBotsTool(fakeBridge());

    const result = await tool({ detail: "yes" });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
  });
});
