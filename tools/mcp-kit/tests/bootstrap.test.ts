// =============================================================================
// bootstrap.test.ts — runStdioServer / runMain 冒烟
//
// 1) 单元：runMain 在非主入口时不执行 main（守卫语义）。
// 2) 集成：spawn tests/fixtures/echo-server.mjs，走 stdio JSON-RPC 往返
//    （initialize → notifications/initialized → tools/list → tools/call）。
// =============================================================================

import { describe, it, expect } from "vitest";
import { spawn, type ChildProcessWithoutNullStreams } from "node:child_process";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { runMain } from "../src/bootstrap.js";

const here = dirname(fileURLToPath(import.meta.url));
const fixturePath = join(here, "fixtures", "echo-server.mjs");

interface RpcClient {
  request: (method: string, params: unknown) => Promise<any>;
  notify: (method: string, params: unknown) => void;
  proc: ChildProcessWithoutNullStreams;
}

/** 起一个极简 JSON-RPC stdio 客户端（逐行解析，按 id 派发） */
function startClient(entry: string): RpcClient {
  const proc = spawn(process.execPath, [entry], { stdio: "pipe" });
  let buffer = "";
  let nextId = 1;
  const pending = new Map<
    number,
    { resolve: (value: any) => void; reject: (error: Error) => void }
  >();

  proc.stdout.on("data", (chunk: Buffer) => {
    buffer += chunk.toString("utf8");
    let newlineIndex: number;
    while ((newlineIndex = buffer.indexOf("\n")) >= 0) {
      const line = buffer.slice(0, newlineIndex).trim();
      buffer = buffer.slice(newlineIndex + 1);
      if (!line) continue;
      let message: any;
      try {
        message = JSON.parse(line);
      } catch {
        continue;
      }
      if (!message || message.id === undefined) continue;
      const slot = pending.get(message.id);
      if (!slot) continue;
      pending.delete(message.id);
      if (message.error) slot.reject(new Error(JSON.stringify(message.error)));
      else slot.resolve(message);
    }
  });

  const request = (method: string, params: unknown): Promise<any> =>
    new Promise((resolvePromise, rejectPromise) => {
      const id = nextId++;
      pending.set(id, { resolve: resolvePromise, reject: rejectPromise });
      proc.stdin.write(
        JSON.stringify({ jsonrpc: "2.0", id, method, params }) + "\n",
      );
      setTimeout(() => {
        if (pending.has(id)) {
          pending.delete(id);
          rejectPromise(new Error(`请求超时: ${method}`));
        }
      }, 10_000);
    });

  const notify = (method: string, params: unknown): void => {
    proc.stdin.write(JSON.stringify({ jsonrpc: "2.0", method, params }) + "\n");
  };

  return { request, notify, proc };
}

describe("runStdioServer（stdio 冒烟）", () => {
  it("initialize / tools/list / tools/call 往返成功", async () => {
    const client = startClient(fixturePath);
    try {
      const init = await client.request("initialize", {
        protocolVersion: "2024-11-05",
        capabilities: {},
        clientInfo: { name: "kit-smoke", version: "0.0.0" },
      });
      expect(init.result.serverInfo.name).toBe("echo-fixture");

      client.notify("notifications/initialized", {});

      // tools/list 成功即证明 onBeforeConnect 先于工具面调用：
      // fixture 的 listTools 在钩子未跑时会抛错（S1 时序护栏）。
      const list = await client.request("tools/list", {});
      expect(list.result.tools).toHaveLength(1);
      expect(list.result.tools[0].name).toBe("echo");
      expect(list.result.tools[0].inputSchema.type).toBe("object");

      const call = await client.request("tools/call", {
        name: "echo",
        arguments: { text: "hi" },
      });
      expect(call.result.isError).toBeFalsy();
      expect(JSON.parse(call.result.content[0].text)).toEqual({ echo: "hi" });
    } finally {
      client.proc.stdin.end();
      client.proc.kill();
    }
  });
});

describe("runMain", () => {
  it("非主入口时不执行 main", async () => {
    let called = false;
    runMain(
      import.meta.url,
      async () => {
        called = true;
      },
      "test",
    );
    await new Promise((resolve) => setTimeout(resolve, 20));
    expect(called).toBe(false);
  });
});
