import { describe, expect, it } from "vitest";

import { createLogsRecentTool } from "../../src/tools/logs-recent.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import {
  defaultBridgeInfo,
  endpointMissing,
  fakeBridge,
  logsRecent,
  unreachable,
} from "../helpers/fake-bridge.js";

describe("logs_recent（BridgeConnection 驱动）", () => {
  it("返回 ok 信封，data 含 seq/dropped/entries（与桥字段一致）", async () => {
    const tool = createLogsRecentTool(fakeBridge({ logsRecent: logsRecent() }));

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.tool).toBe("logs_recent");
    expect(result.data).toEqual({
      seq: 3,
      dropped: 0,
      entries: [
        {
          seq: 1,
          ts: "2026-09-15T10:00:00.000Z",
          level: "warning",
          source: "Unity",
          text: "ComboBox: value is null",
        },
        {
          seq: 2,
          ts: "2026-09-15T10:00:01.000Z",
          level: "error",
          source: "Assembly-CSharp",
          text: "KeyNotFoundException: loot patch",
        },
        {
          seq: 3,
          ts: "2026-09-15T10:00:02.000Z",
          level: "fatal",
          source: "BepInEx",
          text: "AccessViolationException: TrackableTransform",
        },
      ],
    });
  });

  it("输出确定性：相同输入产生完全相同的 JSON 与稳定字段序", async () => {
    const tool = createLogsRecentTool(fakeBridge({ logsRecent: logsRecent() }));

    const first = await tool({});
    const second = await tool({});

    expect(JSON.stringify(first)).toBe(JSON.stringify(second));
    if (!first.ok) return;
    expect(Object.keys(first.data as object)).toEqual(["seq", "dropped", "entries"]);
    const entries = (first.data as { entries: object[] }).entries;
    expect(Object.keys(entries[0])).toEqual(["seq", "ts", "level", "source", "text"]);
  });

  it("空缓冲：entries 为空数组且 seq/dropped 保留", async () => {
    const tool = createLogsRecentTool(
      fakeBridge({ logsRecent: { seq: 9, dropped: 4, entries: [] } }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toEqual({ seq: 9, dropped: 4, entries: [] });
    expect(result.summary).toContain("0 条");
  });

  it("since/level/limit 透传到桥连接（level 归一化为小写）", async () => {
    const bridge = fakeBridge({ logsRecent: logsRecent() });
    const tool = createLogsRecentTool(bridge);

    await tool({ since: 42, level: "Warning", limit: 10 });

    expect(bridge.lastLogsRecentArgs).toEqual({ since: 42, level: "warning", limit: 10 });
  });

  it("level 大小写混合均可（WARNING / Fatal → 小写透传）", async () => {
    const bridge = fakeBridge({ logsRecent: logsRecent() });
    const tool = createLogsRecentTool(bridge);

    await tool({ level: "WARNING" });
    expect(bridge.lastLogsRecentArgs).toEqual({
      since: undefined,
      level: "warning",
      limit: undefined,
    });

    await tool({ level: "Fatal" });
    expect(bridge.lastLogsRecentArgs).toEqual({
      since: undefined,
      level: "fatal",
      limit: undefined,
    });
  });

  it("缺省参数：三个入参均为 undefined（桥侧取缺省语义 = 不过滤）", async () => {
    const bridge = fakeBridge({ logsRecent: logsRecent() });
    const tool = createLogsRecentTool(bridge);

    await tool({});

    expect(bridge.lastLogsRecentArgs).toEqual({
      since: undefined,
      level: undefined,
      limit: undefined,
    });
  });

  it("未知 level 取值（Information / banana / 空白）→ INVALID_INPUT，消息列出合法值", async () => {
    const bridge = fakeBridge({ logsRecent: logsRecent() });
    const tool = createLogsRecentTool(bridge);

    for (const level of ["Information", "banana", "   "]) {
      const result = await tool({ level });

      expect(result.ok).toBe(false);
      if (result.ok) return;
      expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
      expect(result.message).toContain("fatal / error / warning / message / info / debug");
      expect(result.details).toEqual({ level });
    }
    // 拒绝发生在调用桥之前（不产生静默不过滤的请求）
    expect(bridge.callCounts.logsRecent).toBe(0);
  });

  it("since 过旧：dropped>0 时摘要提示丢失条数", async () => {
    const tool = createLogsRecentTool(
      fakeBridge({ logsRecent: logsRecent({ seq: 12, dropped: 3, entries: [] }) }),
    );

    const result = await tool({ since: 1 });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.summary).toContain("3");
  });

  it("bridge 不可达（info 失败）：返回 BRIDGE_UNREACHABLE", async () => {
    const tool = createLogsRecentTool(fakeBridge({ info: unreachable("connect ECONNREFUSED") }));

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE);
    expect(result.tool).toBe("logs_recent");
  });

  it("bridge 不可达（端点失败）：返回 BRIDGE_UNREACHABLE", async () => {
    const tool = createLogsRecentTool(
      fakeBridge({ info: defaultBridgeInfo(), logsRecent: unreachable("socket hang up") }),
    );

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE);
  });

  it("协议版本不一致：返回 BRIDGE_VERSION_MISMATCH（含 expected/actual）", async () => {
    const tool = createLogsRecentTool(fakeBridge({ info: defaultBridgeInfo({ protocolVersion: 2 }) }));

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_VERSION_MISMATCH);
    expect(result.details).toEqual({ expected: 1, actual: 2 });
  });

  it("旧版桥端点 404：返回 LOGS_ENDPOINT_UNAVAILABLE 且提示更新桥 DLL（不误报版本门禁失败）", async () => {
    const tool = createLogsRecentTool(
      fakeBridge({ logsRecent: endpointMissing("/logs/recent") }),
    );

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.LOGS_ENDPOINT_UNAVAILABLE);
    expect(result.code).not.toBe(RUNTIME_ERROR_CODES.BRIDGE_VERSION_MISMATCH);
    expect(result.code).not.toBe(RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE);
    expect(result.message).toContain("更新桥 DLL");
    expect(result.details).toEqual({
      reason: "logs_endpoint_unavailable",
      endpoint: "/logs/recent",
    });
  });

  it("非法输入（since 负数 / limit 非正 / level 非字符串 / 未知字段）：返回 INVALID_INPUT", async () => {
    const tool = createLogsRecentTool(fakeBridge());

    const negativeSince = await tool({ since: -1 });
    const zeroLimit = await tool({ limit: 0 });
    const numericLevel = await tool({ level: 7 });
    const unknown = await tool({ unexpected: true });

    for (const result of [negativeSince, zeroLimit, numericLevel, unknown]) {
      expect(result.ok).toBe(false);
      if (result.ok) return;
      expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
    }
  });
});
