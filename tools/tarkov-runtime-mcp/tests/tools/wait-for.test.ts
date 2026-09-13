// tarkov_wait_for 工具的外部行为测试（S3 工具边界）。
//
// 轮询序列以注入的 callTool 假实现驱动；与 server_status 的集成用 fake connection
// 驱动真实工具链（S1 接缝）。

import { describe, expect, it } from "vitest";

import { createDispatcher } from "../../src/index.js";
import { createWaitForTool, type ToolCaller } from "../../src/tools/wait-for.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../../src/types.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";

/** 按顺序返回信封的假调用器；耗尽后重复最后一个 */
function sequencedCaller(envelopes: Envelope[]): {
  callTool: ToolCaller;
  callCount: () => number;
} {
  let index = 0;
  return {
    callTool: async () => {
      const envelope = envelopes[Math.min(index, envelopes.length - 1)];
      index += 1;
      return envelope;
    },
    callCount: () => index,
  };
}

const NOT_READY = okEnv("tarkov_server_status", "启动中", { version: { core: "0.0.0" } });
const READY = okEnv("tarkov_server_status", "就绪", { version: { core: "5.0.0" } });

describe("tarkov_wait_for", () => {
  it("前 N 次不满足、第 N+1 次满足：返回成功与求值结果", async () => {
    const { callTool, callCount } = sequencedCaller([NOT_READY, NOT_READY, READY]);
    const tool = createWaitForTool(callTool);

    const result = await tool({
      tool: "tarkov_server_status",
      predicate: "version.core equals 5.0.0",
      timeoutMs: 500,
      intervalMs: 1,
    });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({
      tool: "tarkov_server_status",
      predicate: "version.core equals 5.0.0",
      attempts: 3,
      value: "5.0.0",
    });
    expect((result.data as { elapsedMs: number }).elapsedMs).toBeGreaterThanOrEqual(0);
    expect(callCount()).toBe(3);
  });

  it("已满足：首次求值即返回，不额外轮询", async () => {
    const { callTool, callCount } = sequencedCaller([READY]);
    const tool = createWaitForTool(callTool);

    const result = await tool({ tool: "tarkov_server_status", predicate: "version.core equals 5.0.0" });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ attempts: 1 });
    expect(callCount()).toBe(1);
  });

  it("超时：返回结构化 WAIT_TIMEOUT（谓词/最后观察值/耗时）", async () => {
    const { callTool } = sequencedCaller([NOT_READY]);
    const tool = createWaitForTool(callTool);

    const result = await tool({
      tool: "tarkov_server_status",
      predicate: "version.core equals 5.0.0",
      timeoutMs: 30,
      intervalMs: 5,
    });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.WAIT_TIMEOUT);
    const details = result.details as Record<string, unknown>;
    expect(details).toMatchObject({
      tool: "tarkov_server_status",
      predicate: "version.core equals 5.0.0",
      lastValue: "0.0.0",
      pathFound: true,
    });
    expect(details.elapsedMs as number).toBeGreaterThanOrEqual(0);
    expect(details.attempts as number).toBeGreaterThanOrEqual(1);
  });

  it("非法谓词语法：返回 INVALID_INPUT 且不轮询目标工具", async () => {
    const { callTool, callCount } = sequencedCaller([READY]);
    const tool = createWaitForTool(callTool);

    const result = await tool({ tool: "tarkov_server_status", predicate: "garbage" });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
    expect(callCount()).toBe(0);
  });

  it("非法谓词路径：轮询至超时，最后观察值缺失", async () => {
    const { callTool } = sequencedCaller([READY]);
    const tool = createWaitForTool(callTool);

    const result = await tool({
      tool: "tarkov_server_status",
      predicate: "nope.missing equals x",
      timeoutMs: 20,
      intervalMs: 5,
    });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.WAIT_TIMEOUT);
    const details = result.details as Record<string, unknown>;
    expect(details.lastValue).toBeNull();
    expect(details.pathFound).toBe(false);
  });

  it("目标工具错误信封也参与轮询：先 SERVER_UNREACHABLE 后成功", async () => {
    const unreachable = errEnv(
      "tarkov_server_status",
      "无法连接",
      RUNTIME_ERROR_CODES.SERVER_UNREACHABLE,
    );
    const { callTool } = sequencedCaller([unreachable, READY]);
    const tool = createWaitForTool(callTool);

    const result = await tool({
      tool: "tarkov_server_status",
      predicate: "version.core equals 5.0.0",
      timeoutMs: 200,
      intervalMs: 1,
    });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ attempts: 2 });
  });

  it("非法 timeout/interval：返回 INVALID_INPUT", async () => {
    const { callTool } = sequencedCaller([READY]);
    const tool = createWaitForTool(callTool);

    const zeroTimeout = await tool({ tool: "t", predicate: "a equals b", timeoutMs: 0 });
    const negativeInterval = await tool({ tool: "t", predicate: "a equals b", intervalMs: -5 });

    expect(zeroTimeout.ok).toBe(false);
    expect(negativeInterval.ok).toBe(false);
    if (zeroTimeout.ok || negativeInterval.ok) return;
    expect(zeroTimeout.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
    expect(negativeInterval.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
  });

  it("超时上限可配置：超过上限返回 INVALID_INPUT 且不轮询", async () => {
    const { callTool, callCount } = sequencedCaller([READY]);
    const tool = createWaitForTool(callTool, { maxTimeoutMs: 100 });

    const result = await tool({ tool: "t", predicate: "a equals b", timeoutMs: 500 });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
    expect(callCount()).toBe(0);
  });
});

describe("tarkov_wait_for 对 server_status 结果可用（fake connection）", () => {
  function dispatcher() {
    const client = fakeClient(
      new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32")),
    );
    return createDispatcher(client);
  }

  it("equals 谓词：立即满足", async () => {
    const invoke = dispatcher();
    const result = await invoke("tarkov_wait_for", {
      tool: "tarkov_server_status",
      predicate: "version.core equals 5.0.0",
      timeoutMs: 500,
      intervalMs: 1,
    });

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ attempts: 1, value: "5.0.0" });
  });

  it("contains / matches / 数值比较：对 server_status 结果均可用", async () => {
    const invoke = dispatcher();

    const contains = await invoke("tarkov_wait_for", {
      tool: "tarkov_server_status",
      predicate: "capabilities.sections contains server_status",
      timeoutMs: 500,
      intervalMs: 1,
    });
    const matches = await invoke("tarkov_wait_for", {
      tool: "tarkov_server_status",
      predicate: "version.raw matches ^SPT 5\\.0",
      timeoutMs: 500,
      intervalMs: 1,
    });
    const numeric = await invoke("tarkov_wait_for", {
      tool: "tarkov_server_status",
      predicate: "connection.port > 6000",
      timeoutMs: 500,
      intervalMs: 1,
    });

    expect(contains.ok && matches.ok && numeric.ok).toBe(true);
  });

  it("未提供 timeout/interval：使用 sane default 且首次满足即返回", async () => {
    const invoke = dispatcher();
    const started = Date.now();

    const result = await invoke("tarkov_wait_for", {
      tool: "tarkov_server_status",
      predicate: "version.core equals 5.0.0",
    });

    expect(result.ok).toBe(true);
    // 默认超时远大于本用例耗时；立即满足不应等待默认间隔
    expect(Date.now() - started).toBeLessThan(2000);
  });
});
