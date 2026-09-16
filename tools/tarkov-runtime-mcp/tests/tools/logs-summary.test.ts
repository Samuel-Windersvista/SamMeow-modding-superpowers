// =============================================================================
// logs_summary：桥组 + 服务器组 + fatal 组统一视图
//
// 工单 04 起：桥不可达 / 协议不匹配 / 旧桥端点缺失**不再**返回错误信封，
// 而是 ok 信封 + `bridge.available=false` + reason，服务器组与 fatal 组照常返回
// （进程级崩溃会杀死桥进程，fatal 通道必须在该场景下仍可用）。
// =============================================================================

import { describe, expect, it } from "vitest";

import { createLogsSummaryTool } from "../../src/tools/logs-summary.js";
import type { LogWatchSnapshot, LogWatchSource } from "../../src/logs/log-watch.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import {
  defaultBridgeInfo,
  endpointMissing,
  fakeBridge,
  logsSummary,
  unreachable,
} from "../helpers/fake-bridge.js";

function fakeLogWatch(snapshot: Partial<LogWatchSnapshot> = {}): LogWatchSource {
  return {
    snapshot: async () => ({
      groups: [],
      overflowDropped: 0,
      server: { available: true },
      fatal: { available: true },
      ...snapshot,
    }),
  };
}

const SERVER_GROUP = {
  key: "Fixed item: <id>s undefined StackObjectsCount value, now set to <n>",
  level: "warning",
  source: "server:spt20260914.log",
  count: 6477,
  firstTs: "2026-09-14T05:58:00.159Z",
  lastTs: "2026-09-14T05:58:02.200Z",
  sampleText:
    "Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1",
};

const FATAL_GROUP = {
  key: "AccessViolationException: Attempted to read or write protected memory. <n>",
  level: "error",
  source: "fatal",
  count: 2,
  firstTs: "2026-09-16T00:00:00.000Z",
  lastTs: "2026-09-16T00:00:00.000Z",
  sampleText: "AccessViolationException: Attempted to read or write protected memory. 1",
};

describe("logs_summary（桥组 + 服务器组 + fatal 组）", () => {
  it("桥可用：桥组原样输出 + bridge.available=true + 桥自报", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({ logsSummary: logsSummary() }),
      fakeLogWatch(),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.tool).toBe("logs_summary");
    expect(result.data).toEqual({
      groups: [
        {
          key: "KeyNotFoundException: loot patch <n>",
          level: "error",
          source: "Assembly-CSharp",
          count: 6477,
          firstTs: "2026-09-15T10:00:00.000Z",
          lastTs: "2026-09-15T10:00:02.000Z",
          sampleText: "KeyNotFoundException: loot patch 42",
        },
        {
          key: "ComboBox: value is null",
          level: "warning",
          source: "Unity",
          count: 1,
          firstTs: "2026-09-15T09:59:00.000Z",
          lastTs: "2026-09-15T09:59:00.000Z",
          sampleText: "ComboBox: value is null",
        },
      ],
      overflowDropped: 0,
      bridge: {
        available: true,
        pluginVersion: "0.1.0",
        protocolVersion: 1,
        samplingIntervalMs: 1000,
      },
      server: { available: true },
      fatal: { available: true },
    });
  });

  it("输出确定性：字段序稳定（groups/overflowDropped/bridge/server/fatal，组内字段序固定）", async () => {
    const tool = createLogsSummaryTool(fakeBridge({ logsSummary: logsSummary() }));

    const first = await tool({});
    const second = await tool({});

    expect(JSON.stringify(first)).toBe(JSON.stringify(second));
    if (!first.ok) return;
    expect(Object.keys(first.data as object)).toEqual([
      "groups",
      "overflowDropped",
      "bridge",
      "server",
      "fatal",
    ]);
    const groups = (first.data as { groups: object[] }).groups;
    expect(Object.keys(groups[0])).toEqual([
      "key",
      "level",
      "source",
      "count",
      "firstTs",
      "lastTs",
      "sampleText",
    ]);
    expect(Object.keys((first.data as { bridge: object }).bridge)).toEqual([
      "available",
      "pluginVersion",
      "protocolVersion",
      "samplingIntervalMs",
    ]);
  });

  it("合并视图：桥组 + 服务器组 + fatal 组，source 区分且排序同桥侧", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({
        logsSummary: {
          groups: [
            {
              key: "bridge-boom",
              level: "error",
              source: "Unity",
              count: 3,
              firstTs: "2026-09-15T09:59:00.000Z",
              lastTs: "2026-09-15T10:00:00.000Z",
              sampleText: "bridge-boom",
            },
          ],
          overflowDropped: 1,
        },
      }),
      fakeLogWatch({
        groups: [
          { ...SERVER_GROUP, key: "server-old", count: 1, lastTs: "2026-09-15T08:00:00.000Z" },
          { ...SERVER_GROUP, key: "server-boom", count: 3, lastTs: "2026-09-15T09:00:00.000Z" },
          FATAL_GROUP,
        ],
        overflowDropped: 2,
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as {
      groups: Array<{ key: string; source: string }>;
      overflowDropped: number;
    };
    // count 降序 → lastTs 降序 → key 升序（桥组与 MCP 组统一排序）
    expect(data.groups.map((group) => group.key)).toEqual([
      "bridge-boom",
      "server-boom",
      FATAL_GROUP.key,
      "server-old",
    ]);
    expect(data.groups.map((group) => group.source)).toContain("fatal");
    expect(data.overflowDropped).toBe(3);
  });

  it("count 并列时按 lastTs 降序（fatal 崩溃栈晚于服务器组）", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({ logsSummary: { groups: [], overflowDropped: 0 } }),
      fakeLogWatch({
        groups: [SERVER_GROUP, { ...FATAL_GROUP, count: SERVER_GROUP.count }],
        overflowDropped: 0,
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect((result.data as { groups: Array<{ key: string }> }).groups.map((group) => group.key)).toEqual(
      [FATAL_GROUP.key, SERVER_GROUP.key],
    );
  });

  it("since 透传桥端点，并同语义本地过滤 MCP 组（since 独占）", async () => {
    const bridge = fakeBridge({ logsSummary: logsSummary() });
    const tool = createLogsSummaryTool(
      bridge,
      fakeLogWatch({ groups: [SERVER_GROUP, FATAL_GROUP], overflowDropped: 0 }),
    );

    const result = await tool({ since: "2026-09-14T05:58:02.200Z" });

    expect(bridge.lastLogsSummaryArgs).toEqual({ since: "2026-09-14T05:58:02.200Z" });
    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const keys = (result.data as { groups: Array<{ key: string }> }).groups.map((group) => group.key);
    // 服务器组 lastTs == since（独占）→ 排除；fatal 组晚于 since → 保留
    expect(keys).toContain(FATAL_GROUP.key);
    expect(keys).not.toContain(SERVER_GROUP.key);
  });

  it("since 为整数 UTC Ticks：同样本地过滤 MCP 组", async () => {
    const ticks = (Date.parse("2026-09-14T05:58:02.200Z") + 62_135_596_800_000) * 10_000;
    const tool = createLogsSummaryTool(
      fakeBridge({ logsSummary: { groups: [], overflowDropped: 0 } }),
      fakeLogWatch({ groups: [SERVER_GROUP, FATAL_GROUP], overflowDropped: 0 }),
    );

    const result = await tool({ since: ticks });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect((result.data as { groups: Array<{ key: string }> }).groups.map((group) => group.key)).toEqual(
      [FATAL_GROUP.key],
    );
  });

  it("桥不可达：ok 信封 + bridge.available=false（reason=unreachable），服务器/fatal 组照常", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({ info: unreachable("connect ECONNREFUSED") }),
      fakeLogWatch({ groups: [SERVER_GROUP, FATAL_GROUP], overflowDropped: 2 }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as {
      groups: Array<{ source: string }>;
      overflowDropped: number;
      bridge: { available: boolean; reason?: string };
    };
    expect(data.bridge).toEqual({ available: false, reason: "unreachable" });
    expect(data.groups.map((group) => group.source)).toEqual(["server:spt20260914.log", "fatal"]);
    expect(data.overflowDropped).toBe(2);
    expect(result.summary).toContain("桥不可用");
  });

  it("桥端点失败（非 404）：降级为 reason=unreachable（不抛错）", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({ info: defaultBridgeInfo(), logsSummary: unreachable("socket hang up") }),
      fakeLogWatch({ groups: [SERVER_GROUP], overflowDropped: 0 }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect((result.data as { bridge: unknown }).bridge).toEqual({
      available: false,
      reason: "unreachable",
    });
    expect((result.data as { groups: unknown[] }).groups).toHaveLength(1);
  });

  it("协议版本不一致：降级为 reason=version_mismatch（不误报，且不调用桥端点）", async () => {
    const bridge = fakeBridge({ info: defaultBridgeInfo({ protocolVersion: 2 }) });
    const tool = createLogsSummaryTool(
      bridge,
      fakeLogWatch({ groups: [SERVER_GROUP, FATAL_GROUP], overflowDropped: 0 }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect((result.data as { bridge: unknown }).bridge).toEqual({
      available: false,
      reason: "version_mismatch",
    });
    expect(bridge.callCounts.logsSummary).toBe(0);
    expect((result.data as { groups: unknown[] }).groups).toHaveLength(2);
  });

  it("旧版桥端点缺失（404）：降级为 reason=endpoint_missing 且提示更新桥 DLL", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({ logsSummary: endpointMissing("/logs/summary") }),
      fakeLogWatch({ groups: [SERVER_GROUP, FATAL_GROUP], overflowDropped: 0 }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect((result.data as { bridge: unknown }).bridge).toEqual({
      available: false,
      reason: "endpoint_missing",
    });
    expect(result.summary).toContain("更新桥 DLL");
    expect((result.data as { groups: unknown[] }).groups).toHaveLength(2);
  });

  it("桥不可达且无 MCP 组：ok 空视图（不返回错误码）", async () => {
    const tool = createLogsSummaryTool(fakeBridge({ info: unreachable("ECONNREFUSED") }));

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toEqual({
      groups: [],
      overflowDropped: 0,
      bridge: { available: false, reason: "unreachable" },
      server: { available: false, reason: "path_unresolved" },
      fatal: { available: false, reason: "path_unresolved" },
    });
    expect("code" in result).toBe(false);
  });

  it("server 通道不可用：server.available=false + reason，服务器组为空（fatal 组照常）", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({ logsSummary: { groups: [], overflowDropped: 0 } }),
      fakeLogWatch({
        groups: [FATAL_GROUP],
        server: { available: false, reason: "logs_root_missing" },
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as {
      groups: Array<{ source: string }>;
      server: { available: boolean; reason?: string };
      fatal: { available: boolean; reason?: string };
    };
    expect(data.server).toEqual({ available: false, reason: "logs_root_missing" });
    expect(data.fatal).toEqual({ available: true });
    expect(data.groups.map((group) => group.source)).toEqual(["fatal"]);
  });

  it("fatal 通道不可用：fatal.available=false + reason，fatal 组为空（服务器组照常）", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({ logsSummary: { groups: [], overflowDropped: 0 } }),
      fakeLogWatch({
        groups: [SERVER_GROUP],
        fatal: { available: false, reason: "file_missing" },
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as {
      groups: Array<{ source: string }>;
      server: { available: boolean; reason?: string };
      fatal: { available: boolean; reason?: string };
    };
    expect(data.fatal).toEqual({ available: false, reason: "file_missing" });
    expect(data.server).toEqual({ available: true });
    expect(data.groups.map((group) => group.source)).toEqual(["server:spt20260914.log"]);
  });

  it("三通道可独立降级：桥不可达 + server 不可用 + fatal 可用（fatal 组仍返回）", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({ info: unreachable("ECONNREFUSED") }),
      fakeLogWatch({
        groups: [FATAL_GROUP],
        server: { available: false, reason: "no_log_dirs" },
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toEqual({
      groups: [FATAL_GROUP],
      overflowDropped: 0,
      bridge: { available: false, reason: "unreachable" },
      server: { available: false, reason: "no_log_dirs" },
      fatal: { available: true },
    });
  });

  it("可用性字段无多余键：available=true 时不带 reason", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({ logsSummary: { groups: [], overflowDropped: 0 } }),
      fakeLogWatch(),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as { server: object; fatal: object };
    expect(Object.keys(data.server)).toEqual(["available"]);
    expect(Object.keys(data.fatal)).toEqual(["available"]);
  });

  it("摘要区分三通道计数与溢出淘汰", async () => {
    const tool = createLogsSummaryTool(
      fakeBridge({ logsSummary: { groups: [], overflowDropped: 1 } }),
      fakeLogWatch({ groups: [SERVER_GROUP, FATAL_GROUP], overflowDropped: 2 }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.summary).toContain("服务器");
    expect(result.summary).toContain("fatal");
    expect(result.summary).toContain("3");
  });

  it("非法输入（since 非法类型 / 空字符串 / 未知字段）：返回 INVALID_INPUT", async () => {
    const tool = createLogsSummaryTool(fakeBridge());

    const booleanSince = await tool({ since: true });
    const emptySince = await tool({ since: "" });
    const objectSince = await tool({ since: { ts: 1 } });
    const unknown = await tool({ unexpected: true });

    for (const result of [booleanSince, emptySince, objectSince, unknown]) {
      expect(result.ok).toBe(false);
      if (result.ok) return;
      expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
    }
  });
});
