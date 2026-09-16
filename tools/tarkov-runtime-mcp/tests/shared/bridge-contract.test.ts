// =============================================================================
// C10 契约工件与共享夹具消费（TS 端）
//
// 断言三件事：
//   1. 契约运行时读取 + 校验 + 缓存 + 响亮失败（src/bridge/contract.ts）；
//   2. 消费点确实由契约派生（EXPECTED_BRIDGE_PROTOCOL_VERSION / 归一化规则 / MAX_LOG_GROUPS）；
//   3. 两端共享夹具（shared/bridge-contract/fixtures/**）逐条通过——
//      C# 端消费同一批文件（见 tools/tarkov-runtime-bridge/tests/TarkovRuntimeBridge.Tests/BridgeContractTests.cs）。
//
// 夹具路径经 import.meta.url 解析：tests/shared/ → 仓库根 ../../../..。
// =============================================================================

import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

import {
  BRIDGE_CONTRACT_URL,
  loadBridgeContract,
  parseBridgeContract,
} from "../../src/bridge/contract.js";
import { EXPECTED_BRIDGE_PROTOCOL_VERSION } from "../../src/bridge/connection.js";
import {
  parseBridgeInfoPayload,
  parseLogsRecentPayload,
  parseLogsSummaryPayload,
  parseRaidBotsPayload,
  parseRaidEventsPayload,
  parseRaidPlayerPayload,
  parseRaidStatusPayload,
} from "../../src/bridge/http-bridge-connection.js";
import { LogSummaryAggregator, MAX_LOG_GROUPS } from "../../src/logs/log-aggregator.js";
import { normalizeLogText } from "../../src/logs/log-normalizer.js";

const FIXTURES_URL = new URL("../../../../shared/bridge-contract/fixtures/", import.meta.url);

function readFixture(relativePath: string): string {
  return readFileSync(fileURLToPath(new URL(relativePath, FIXTURES_URL)), "utf8");
}

function readFixtureJson(relativePath: string): unknown {
  return JSON.parse(readFixture(relativePath));
}

/** 纯字母唯一键（规避数字归一化导致同键塌缩） */
function alphaKey(index: number): string {
  const chars: string[] = [];
  let value = index;
  do {
    chars.push(String.fromCharCode("a".charCodeAt(0) + (value % 26)));
    value = Math.floor(value / 26);
  } while (value > 0);
  return "key-" + chars.reverse().join("");
}

// -----------------------------------------------------------------------------
// 契约
// -----------------------------------------------------------------------------

describe("桥接契约（共享工件）", () => {
  it("运行时读取并校验契约工件", () => {
    const contract = loadBridgeContract();

    expect(contract.protocolVersion).toBeGreaterThanOrEqual(1);
    expect(contract.logNormalization.whitespacePattern.length).toBeGreaterThan(0);
    expect(contract.logNormalization.trim).toBe(true);
    expect(contract.logAggregation.maxGroups).toBeGreaterThanOrEqual(1);
    expect(contract.logNormalization.rules.map((rule) => rule.id)).toEqual([
      "guid",
      "id24hex",
      "hex",
      "number",
    ]);
    expect(contract.logNormalization.rules.map((rule) => rule.replacement)).toEqual([
      "<guid>",
      "<id>",
      "<hex>",
      "<n>",
    ]);
  });

  it("契约文件本身与解析结果一致（同一份工件）", () => {
    const raw = JSON.parse(readFileSync(fileURLToPath(BRIDGE_CONTRACT_URL), "utf8"));

    expect(parseBridgeContract(raw)).toEqual(loadBridgeContract());
  });

  it("消费点由契约派生（协议版本 / 聚合上限）", () => {
    const contract = loadBridgeContract();

    expect(EXPECTED_BRIDGE_PROTOCOL_VERSION).toBe(contract.protocolVersion);
    expect(MAX_LOG_GROUPS).toBe(contract.logAggregation.maxGroups);
  });

  it("读取结果被缓存（进程内单例）", () => {
    expect(loadBridgeContract()).toBe(loadBridgeContract());
  });

  it("非法契约响亮报错（不静默降级）", () => {
    expect(() => parseBridgeContract(null)).toThrow(/根节点/);
    expect(() => parseBridgeContract({})).toThrow(/protocolVersion/);
    expect(() =>
      parseBridgeContract({
        protocolVersion: 1,
        logNormalization: { rules: [], whitespacePattern: "[ ]+", trim: true },
        logAggregation: { maxGroups: 500 },
      }),
    ).toThrow(/rules/);
    expect(() =>
      parseBridgeContract({
        protocolVersion: 1,
        logNormalization: {
          rules: [{ id: "bad", pattern: "(", replacement: "<x>" }],
          whitespacePattern: "[ ]+",
          trim: true,
        },
        logAggregation: { maxGroups: 500 },
      }),
    ).toThrow(/pattern/);
    expect(() =>
      parseBridgeContract({
        protocolVersion: 1,
        logNormalization: {
          rules: [{ id: "a", pattern: "a", replacement: "b" }],
          whitespacePattern: "(",
          trim: true,
        },
        logAggregation: { maxGroups: 500 },
      }),
    ).toThrow(/whitespacePattern/);
    expect(() =>
      parseBridgeContract({
        protocolVersion: 1,
        logNormalization: {
          rules: [{ id: "a", pattern: "a", replacement: "" }],
          whitespacePattern: "[ ]+",
          trim: true,
        },
        logAggregation: { maxGroups: 500 },
      }),
    ).toThrow(/replacement/);
    expect(() =>
      parseBridgeContract({
        protocolVersion: 1,
        logNormalization: {
          rules: [{ id: "a", pattern: "a", replacement: "b" }],
          whitespacePattern: "[ ]+",
          trim: true,
        },
        logAggregation: { maxGroups: 0 },
      }),
    ).toThrow(/maxGroups/);
  });

  it("接受 JSON 的 1.0 形态整数（与 C# 端对齐）", () => {
    const contract = parseBridgeContract({
      protocolVersion: 1.0,
      logNormalization: {
        rules: [{ id: "a", pattern: "a", replacement: "b" }],
        whitespacePattern: "[ ]+",
        trim: true,
      },
      logAggregation: { maxGroups: 500.0 },
    });

    expect(contract.protocolVersion).toBe(1);
    expect(contract.logAggregation.maxGroups).toBe(500);
  });

  it("拒绝非整值数字（1.5）", () => {
    expect(() =>
      parseBridgeContract({
        protocolVersion: 1.5,
        logNormalization: {
          rules: [{ id: "a", pattern: "a", replacement: "b" }],
          whitespacePattern: "[ ]+",
          trim: true,
        },
        logAggregation: { maxGroups: 500 },
      }),
    ).toThrow(/protocolVersion/);
  });
});

// -----------------------------------------------------------------------------
// 共享夹具：归一化
// -----------------------------------------------------------------------------

describe("共享夹具：日志归一化向量", () => {
  it("全部向量通过（与 C# 端消费同一文件）", () => {
    const fixture = readFixtureJson("log-normalization.json") as {
      cases: { name: string; input: string; expected: string }[];
    };

    expect(fixture.cases.length).toBeGreaterThan(0);

    const failures = fixture.cases
      .filter((testCase) => normalizeLogText(testCase.input) !== testCase.expected)
      .map(
        (testCase) =>
          `${testCase.name}: 期望 ${JSON.stringify(testCase.expected)}，实际 ${JSON.stringify(
            normalizeLogText(testCase.input),
          )}`,
      );

    expect(failures).toEqual([]);
  });
});

// -----------------------------------------------------------------------------
// 共享夹具：聚合
// -----------------------------------------------------------------------------

describe("共享夹具：日志聚合向量", () => {
  it("全部向量通过（含组序 / 淘汰 / overflowDropped）", () => {
    const fixture = readFixtureJson("log-aggregation.json") as {
      cases: {
        name: string;
        maxGroups?: number;
        since?: string;
        entries: { ts: string; level: string; source: string; text: string }[];
        expected: { groups: unknown[]; overflowDropped: number };
      }[];
    };

    expect(fixture.cases.length).toBeGreaterThan(0);

    for (const testCase of fixture.cases) {
      const aggregator = new LogSummaryAggregator(testCase.maxGroups ?? MAX_LOG_GROUPS);
      for (const entry of testCase.entries) {
        aggregator.observe(entry);
      }

      const sinceMs = testCase.since === undefined ? null : Date.parse(testCase.since);
      const snapshot = aggregator.snapshot(sinceMs);

      expect({ name: testCase.name, groups: snapshot.groups, overflowDropped: snapshot.overflowDropped }).toEqual({
        name: testCase.name,
        groups: testCase.expected.groups,
        overflowDropped: testCase.expected.overflowDropped,
      });
    }
  });

  it("组数上限取自契约（第 maxGroups+1 个新键触发淘汰）", () => {
    const aggregator = new LogSummaryAggregator();

    for (let index = 0; index < MAX_LOG_GROUPS + 3; index += 1) {
      aggregator.observe({ ts: "2026-09-14T05:58:00.0000000Z", level: "warning", source: "s", text: alphaKey(index) });
    }

    const snapshot = aggregator.snapshot(null);

    expect(snapshot.groups).toHaveLength(MAX_LOG_GROUPS);
    expect(snapshot.overflowDropped).toBe(3);
  });
});

// -----------------------------------------------------------------------------
// 共享夹具：payload 样例（TS 断言解析器接受样例）
// -----------------------------------------------------------------------------

describe("共享夹具：payload 样例", () => {
  it("解析器接受 /bridge/info 样例且协议版本为契约值", () => {
    const info = parseBridgeInfoPayload(readFixtureJson("payloads/bridge-info.json"));

    expect(info.protocolVersion).toBe(loadBridgeContract().protocolVersion);
    expect(info.capabilities.endpoints).toContain("/logs/summary");
    expect(info.capabilities.sections).toContain("logs");
  });

  it("解析器接受 /raid/status 样例", () => {
    expect(parseRaidStatusPayload(readFixtureJson("payloads/raid-status.json"))).toEqual({
      inRaid: true,
      map: "factory4_day",
      status: "Started",
      remainingSeconds: 120.5,
      raidId: "profile-1@2026-09-14T00:00:00.0000000Z",
      sampleAgeMs: 750,
    });
  });

  it("解析器接受 /raid/player 样例（无武器 / 无装备）", () => {
    const player = parseRaidPlayerPayload(readFixtureJson("payloads/raid-player.json"));

    expect(player).toMatchObject({ inRaid: true, weapon: null, equipment: [], pose: "Stand", sampleAgeMs: 750 });
  });

  it("解析器接受 /raid/player 样例（含武器与装备）", () => {
    const player = parseRaidPlayerPayload(readFixtureJson("payloads/raid-player-equipped.json"));

    expect(player).toMatchObject({
      inRaid: true,
      weapon: { tpl: "5447a9cd4bdc2dbd208b4567", name: "M4A1", ammoInMag: 30, ammoInChamber: 1 },
    });
    if (player.inRaid) {
      expect(player.equipment).toHaveLength(2);
      expect(player.equipment[0]).toEqual({
        slot: "Headwear",
        tpl: "5aa7e276e5b5b000171d0647",
        name: "Altyn",
      });
    }
  });

  it("解析器接受 /raid/bots 摘要与明细样例", () => {
    const summary = parseRaidBotsPayload(readFixtureJson("payloads/raid-bots.json"), false);
    expect(summary).toMatchObject({ inRaid: true, detail: false, total: 4, alive: 4 });

    const detail = parseRaidBotsPayload(readFixtureJson("payloads/raid-bots-detail.json"), true);
    expect(detail).toMatchObject({ inRaid: true, detail: true, truncated: true, total: 4 });
    if (detail.inRaid && detail.detail) {
      expect(detail.bots).toHaveLength(2);
      expect(detail.bots[1]).toEqual({ x: -4.5, y: 0, z: 0, role: "bossKilla", side: "Savage", alive: false });
    }
  });

  it("解析器接受 /raid/events 样例（damage / death / extraction）", () => {
    const events = parseRaidEventsPayload(readFixtureJson("payloads/raid-events.json"));

    expect(events.seq).toBe(3);
    expect(events.dropped).toBe(0);
    expect(events.events.map((event) => event.type)).toEqual(["damage", "death", "extraction"]);

    const death = events.events[1];
    if (death.type === "death") {
      expect(death.payload.killer).toEqual({
        profileId: "profile-1",
        name: "SamMeow",
        side: "Usec",
        role: "pmcUSEC",
        isLocal: true,
      });
    }
  });

  it("解析器接受 /logs/recent 样例", () => {
    const recent = parseLogsRecentPayload(readFixtureJson("payloads/logs-recent.json"));

    expect(recent.seq).toBe(2);
    expect(recent.entries).toHaveLength(2);
    expect(recent.entries[0]).toEqual({
      seq: 1,
      ts: "2026-09-14T00:00:00.0000000Z",
      level: "warning",
      source: "Unity",
      text: "line1\nline2",
    });
  });

  it("解析器接受 /logs/summary 样例", () => {
    const summary = parseLogsSummaryPayload(readFixtureJson("payloads/logs-summary.json"));

    expect(summary.overflowDropped).toBe(0);
    expect(summary.groups).toHaveLength(1);
    expect(summary.groups[0]).toEqual({
      key: "Failed to load asset <n>",
      level: "error",
      source: "Assembly-CSharp",
      count: 2,
      firstTs: "2026-09-14T00:00:00.0000000Z",
      lastTs: "2026-09-14T00:00:00.0000010Z",
      sampleText: "Failed to load asset 12",
    });
  });

  it("错误形状样例与桥侧错误体一致", () => {
    const errors = readFixtureJson("payloads/errors.json") as Record<string, string>;

    expect(errors.not_found).toBe('{"error":"not_found"}');
    expect(errors.method_not_allowed).toBe('{"error":"method_not_allowed"}');
    expect(errors.internal_error).toBe('{"error":"internal_error"}');
    expect(errors.not_in_raid).toBe('{"inRaid":false}');
  });
});
