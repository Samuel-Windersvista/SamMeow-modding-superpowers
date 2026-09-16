import { describe, expect, it } from "vitest";

import { TOOL_DEFINITIONS, createDispatcher, createRuntime } from "../../src/index.js";
import { LOGS_TOOL_NAMES } from "../../src/tools/logs-common.js";
import { EMPTY_LOG_WATCH, type LogWatchSource } from "../../src/logs/log-watch.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";
import { fakeBridge, logsRecent, logsSummary } from "../helpers/fake-bridge.js";

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

function dispatcher(
  bridge = fakeBridge({ logsRecent: logsRecent(), logsSummary: logsSummary() }),
  logWatch: LogWatchSource = EMPTY_LOG_WATCH,
) {
  return createDispatcher(
    fakeClient(new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32"))),
    bridge,
    logWatch,
  );
}

function definition(name: string) {
  return TOOL_DEFINITIONS.find((tool) => tool.name === name);
}

describe("logs.* 工具注册（logs_recent / logs_summary）", () => {
  it("工具定义包含两个 logs 工具且带 inputSchema", () => {
    const names = TOOL_DEFINITIONS.map((tool) => tool.name);
    for (const name of LOGS_TOOL_NAMES) {
      expect(names).toContain(name);
      expect(definition(name)?.inputSchema).toBeTruthy();
    }
  });

  it("logs_recent 描述写明游标语义（since 独占 / dropped / limit 缺省与上限）与错误码", () => {
    const description = definition("logs_recent")?.description ?? "";

    expect(description).toContain("独占");
    expect(description).toContain("dropped");
    expect(description).toContain("上限 1000");
    expect(description).toContain("LOGS_ENDPOINT_UNAVAILABLE");
    expect(description).toContain("更新桥 DLL");
  });

  it("logs_recent 描述写明 level 合法值与 INVALID_INPUT 语义", () => {
    const description = definition("logs_recent")?.description ?? "";

    for (const level of ["fatal", "error", "warning", "message", "info", "debug"]) {
      expect(description).toContain(level);
    }
    expect(description).toContain("INVALID_INPUT");
    expect(description).toContain("不静默不过滤");
  });

  it("logs_summary 描述写明合并视图 / bridge.available / Warning+ 语义与游标语义", () => {
    const description = definition("logs_summary")?.description ?? "";

    expect(description).toContain("时间游标");
    expect(description).toContain("ISO 8601");
    expect(description).toContain("overflowDropped");
    // 合并视图三通道
    expect(description).toContain("服务器组");
    expect(description).toContain("fatal");
    expect(description).toContain("Warning 及以上");
    // 桥故障降级语义
    expect(description).toContain("bridge.available=false");
    expect(description).toContain("unreachable");
    expect(description).toContain("version_mismatch");
    expect(description).toContain("endpoint_missing");
    expect(description).toContain("更新桥 DLL");
  });

  it("logs_summary 描述写明 server/fatal 通道可用性与 reason 命名", () => {
    const description = definition("logs_summary")?.description ?? "";

    expect(description).toContain("server");
    expect(description).toContain("fatal");
    expect(description).toContain("available=false");
    for (const reason of [
      "logs_root_missing",
      "no_log_dirs",
      "path_unresolved",
      "file_missing",
    ]) {
      expect(description).toContain(reason);
    }
  });

  it("dispatcher 路由 logs_recent 到桥连接", async () => {
    const bridge = fakeBridge({ logsRecent: logsRecent({ seq: 8, dropped: 1 }) });
    const result = await dispatcher(bridge)("logs_recent", { since: 3 });

    expect(result.ok).toBe(true);
    expect(bridge.lastLogsRecentArgs).toEqual({ since: 3, level: undefined, limit: undefined });
    if (!result.ok) return;
    expect(result.data).toMatchObject({ seq: 8, dropped: 1 });
  });

  it("dispatcher 路由 logs_summary 到桥连接", async () => {
    const bridge = fakeBridge({ logsSummary: logsSummary({ overflowDropped: 2 }) });
    const result = await dispatcher(bridge)("logs_summary", {});

    expect(result.ok).toBe(true);
    expect(bridge.callCounts.logsSummary).toBe(1);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ overflowDropped: 2 });
  });

  it("createRuntime 注入 bridge + logWatch：logs_summary 经注入连接与观测面读取", async () => {
    const { invoke } = createRuntime({
      config: {
        host: "127.0.0.1",
        candidatePorts: [6969],
        anchorVersion: "5.0.0-BEM-20260914",
      },
      connect: () => new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32")),
      bridge: fakeBridge({ logsSummary: logsSummary({ overflowDropped: 7 }) }),
      logWatch: EMPTY_LOG_WATCH,
    });

    const result = await invoke("logs_summary", {});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ overflowDropped: 7 });
  });

  it("createRuntime 未注入 logWatch：缺省构造日志观测面（惰性刷新接缝）", () => {
    const runtime = createRuntime({
      config: {
        host: "127.0.0.1",
        candidatePorts: [6969],
        anchorVersion: "5.0.0-BEM-20260914",
      },
      bridge: fakeBridge(),
    });

    expect(typeof runtime.logWatch.snapshot).toBe("function");
  });

  it("dispatcher 注入日志观测面：服务器组与 fatal 组并入 logs_summary", async () => {
    const result = await dispatcher(fakeBridge({ logsSummary: { groups: [], overflowDropped: 0 } }), {
      snapshot: async () => ({
        groups: [SERVER_GROUP],
        overflowDropped: 4,
        server: { available: true },
        fatal: { available: false, reason: "path_unresolved" },
      }),
    })("logs_summary", {});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as {
      groups: Array<{ source: string }>;
      overflowDropped: number;
      fatal: { available: boolean; reason?: string };
    };
    expect(data.groups.map((group) => group.source)).toEqual(["server:spt20260914.log"]);
    expect(data.overflowDropped).toBe(4);
    expect(data.fatal).toEqual({ available: false, reason: "path_unresolved" });
  });

  it("logs 工具不依赖 raid 状态：非 raid 也返回 ok（桥侧 inRaid 字段不存在）", async () => {
    const bridge = fakeBridge({
      logsRecent: logsRecent(),
      logsSummary: logsSummary(),
    });
    const result = await dispatcher(bridge)("logs_recent", {});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).not.toHaveProperty("inRaid");
  });

  it("未知 logs 工具名：返回 INVALID_INPUT（无 raid.* 式占位兜底）", async () => {
    const result = await dispatcher()("logs_unknown", {});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
  });
});
