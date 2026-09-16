// =============================================================================
// capture-tools-list.mjs — MCP tools/list golden 捕获器（C2 车道 A 步骤 0）
//
// 用途：
//   1. C2 迁移前/后捕获三台 MCP 的 tools/list 结果，做逐字节 golden 对比；
//   2. 便携包冒烟（wave2 车道 D）复用：指向便携 dist 入口即可。
//
// 握手（stdio JSON-RPC 2.0，参考 tools/mo2-mcp/tests/smoke.test.ts）：
//   initialize（请求）→ notifications/initialized（通知）→ tools/list（请求）
//
// 用法：
//   node .scratch/c2-mcp-kit/tools/capture-tools-list.mjs <入口路径> <输出路径>
//
// 输出：tools/list 的 result 对象（{ tools: [...] }），UTF-8 + 2 空格缩进。
// =============================================================================

import { spawn } from "node:child_process";
import { mkdir, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";

const REQUEST_TIMEOUT_MS = 30_000;
const GLOBAL_TIMEOUT_MS = 90_000;

const [entryArg, outArg] = process.argv.slice(2);
if (!entryArg || !outArg) {
  process.stderr.write(
    "用法: node capture-tools-list.mjs <入口路径> <输出路径>\n",
  );
  process.exit(2);
}

const entry = resolve(entryArg);
const out = resolve(outArg);

const proc = spawn(process.execPath, [entry], {
  stdio: ["pipe", "pipe", "pipe"],
  cwd: process.cwd(),
});

let stdoutBuffer = "";
let stderrBuffer = "";
let nextId = 1;
const pending = new Map();

/** 逐行解析 stdout 上的 JSON-RPC 响应，按 id 派发给等待中的请求 */
function drainStdout() {
  let newlineIndex;
  while ((newlineIndex = stdoutBuffer.indexOf("\n")) >= 0) {
    const line = stdoutBuffer.slice(0, newlineIndex).trim();
    stdoutBuffer = stdoutBuffer.slice(newlineIndex + 1);
    if (!line) continue;
    let message;
    try {
      message = JSON.parse(line);
    } catch {
      continue; // 非 JSON 行（防御性忽略）
    }
    if (!message || message.id === undefined) continue;
    const slot = pending.get(message.id);
    if (!slot) continue;
    pending.delete(message.id);
    if (message.error) {
      slot.reject(new Error(`JSON-RPC 错误: ${JSON.stringify(message.error)}`));
    } else {
      slot.resolve(message);
    }
  }
}

proc.stdout.on("data", (chunk) => {
  stdoutBuffer += chunk.toString("utf8");
  drainStdout();
});
proc.stderr.on("data", (chunk) => {
  stderrBuffer += chunk.toString("utf8");
});

/** 发送请求并等待其响应 */
function request(method, params) {
  const id = nextId++;
  return new Promise((resolvePromise, rejectPromise) => {
    pending.set(id, { resolve: resolvePromise, reject: rejectPromise });
    proc.stdin.write(
      JSON.stringify({ jsonrpc: "2.0", id, method, params }) + "\n",
    );
    setTimeout(() => {
      if (pending.has(id)) {
        pending.delete(id);
        rejectPromise(new Error(`请求超时（${REQUEST_TIMEOUT_MS}ms）: ${method}`));
      }
    }, REQUEST_TIMEOUT_MS);
  });
}

/** 发送通知（无 id、无响应） */
function notify(method, params) {
  proc.stdin.write(JSON.stringify({ jsonrpc: "2.0", method, params }) + "\n");
}

const globalTimer = setTimeout(() => {
  process.stderr.write(`[capture] 全局超时（${GLOBAL_TIMEOUT_MS}ms），终止 ${entryArg}\n`);
  proc.kill();
  process.exit(1);
}, GLOBAL_TIMEOUT_MS);

try {
  await request("initialize", {
    protocolVersion: "2024-11-05",
    capabilities: {},
    clientInfo: { name: "c2-capture", version: "0.0.0" },
  });
  notify("notifications/initialized", {});
  const response = await request("tools/list", {});
  const tools = Array.isArray(response.result?.tools) ? response.result.tools : [];

  await mkdir(dirname(out), { recursive: true });
  await writeFile(out, JSON.stringify(response.result, null, 2) + "\n", "utf8");

  process.stdout.write(`[capture] ${entryArg} -> ${outArg}（${tools.length} 个工具）\n`);
  clearTimeout(globalTimer);
  proc.stdin.end();
  proc.kill();
  process.exit(0);
} catch (error) {
  process.stderr.write(`[capture] 失败: ${error.message}\n`);
  if (stderrBuffer) {
    process.stderr.write(`[capture] 服务器 stderr:\n${stderrBuffer}\n`);
  }
  clearTimeout(globalTimer);
  proc.kill();
  process.exit(1);
}
