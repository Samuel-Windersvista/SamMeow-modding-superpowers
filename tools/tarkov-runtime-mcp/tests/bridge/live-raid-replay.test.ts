// T07 回归：真实 raid 录制回放（fixture）——工具层输出与录制逐字段一致
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

import { loadReplayConnection } from "../../src/bridge/replay.js";
import { createRuntime } from "../../src/index.js";

const fixturePath = join(
  dirname(fileURLToPath(import.meta.url)),
  "..",
  "fixtures",
  "live-raid",
  "raid-sandbox-2026-09-14.jsonl",
);

const testConfig = {
  host: "127.0.0.1",
  candidatePorts: [6969],
  anchorVersion: "5.0.0-BEM-20260914",
  bridgeHost: "127.0.0.1",
  bridgePort: 49777,
};

function runtime() {
  return createRuntime({
    bridge: loadReplayConnection(fixturePath),
    config: testConfig,
  });
}

describe("live raid 录制回放（T07 fixture：Sandbox 真实录制）", () => {
  it("raid_status 与录制逐字段一致", async () => {
    const { invoke } = runtime();
    const result = await invoke("raid_status", {});
    expect(result.ok).toBe(true);
    const data = (result as { data: Record<string, unknown> }).data;
    expect(data.map).toBe("Sandbox");
    expect(data.status).toBe("Started");
    expect(data.remainingSeconds).toBe(1700.794);
    expect(data.raidId).toBe("000000000000000000000000@2026-09-14T15:47:07.5431365Z");
    expect(data.bridge).toEqual({
      pluginVersion: "0.1.0",
      protocolVersion: 1,
      samplingIntervalMs: 250,
    });
  });

  it("raid_player 两次读取按录制顺序消费（字段确定性）", async () => {
    const { invoke } = runtime();
    const first = await invoke("raid_player", {});
    const second = await invoke("raid_player", {});
    const d1 = (first as { data: Record<string, unknown> }).data;
    const d2 = (second as { data: Record<string, unknown> }).data;
    expect(d1.position).toEqual({ x: 66.44463, y: 14.34571, z: 167.8687 });
    expect(d1.pose).toBe("Stand");
    expect((d1.health as Record<string, unknown>).total).toBe(433.93283);
    expect(d1.sampleAgeMs).toBe(109);
    // 第二笔为录制中的第二条 getRaidPlayer（顺序消费）
    expect(d2.sampleAgeMs).toBe(235);
  });

  it("raid_bots 摘要与明细均可回放（PMC/Scav 分类修正后的录制）", async () => {
    const { invoke } = runtime();
    const summary = await invoke("raid_bots", {});
    const detail = await invoke("raid_bots", { detail: true });
    const s = (summary as { data: Record<string, unknown> }).data;
    const d = (detail as { data: Record<string, unknown> }).data;
    expect(s.total).toBe(12);
    expect(s.byCategory).toEqual({ pmc: 2, scav: 10, boss: 0, other: 0 });
    expect(d.truncated).toBe(false);
    const bots = d.bots as unknown[];
    expect(Array.isArray(bots)).toBe(true);
    // 明细未截断 ⇒ 明细条数等于总数
    expect(bots.length).toBe(d.total);
  });

  it("同一录制两次独立回放输出逐字节一致（确定性）", async () => {
    const run = async () => {
      const { invoke } = runtime();
      return JSON.stringify([
        await invoke("raid_status", {}),
        await invoke("raid_player", {}),
        await invoke("raid_bots", {}),
        await invoke("raid_bots", { detail: true }),
      ]);
    };
    expect(await run()).toBe(await run());
  });
});
