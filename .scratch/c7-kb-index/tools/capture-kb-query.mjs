// =============================================================================
// capture-kb-query.mjs — spt_kb_query golden 捕获器（C7 车道 A 步骤 0）
//
// 用途：迁移前/后捕获 spt_kb_query 五次调用的结果，做逐调用 golden 对比。
//
// 握手（stdio JSON-RPC 2.0，参考 .scratch/c2-mcp-kit/tools/capture-tools-list.mjs）：
//   initialize（请求）→ notifications/initialized（通知）→ tools/call（5 次请求）
//
// 每次调用写入 <out-dir>/call-<n>-<label>.json，内容为：
//   { isError: <wire isError 标志>, envelope: <解析后的信封对象> }
//   —— 取信封（content[0].text 解析）而非原始 content 文本，使 diff 可读。
//
// 用法：
//   node .scratch/c7-kb-index/tools/capture-kb-query.mjs <out-dir> [entry-path]
//   默认 entry-path = tools/spt-mcp/dist/index.js
// =============================================================================

import { spawn } from "node:child_process";
import { mkdir, writeFile } from "node:fs/promises";
import { resolve } from "node:path";

const REQUEST_TIMEOUT_MS = 30_000;
const GLOBAL_TIMEOUT_MS = 120_000;

const [outDirArg, entryArg] = process.argv.slice(2);
if (!outDirArg) {
  process.stderr.write("用法: node capture-kb-query.mjs <out-dir> [entry-path]\n");
  process.exit(2);
}

const outDir = resolve(outDirArg);
const entry = resolve(entryArg ?? "tools/spt-mcp/dist/index.js");

/** 5 次调用：空过滤 / topic / domain / keyword / version（version 迁移前预期 internal_error） */
const CALLS = [
  { label: "empty", args: {} },
  { label: "topic-config", args: { topic: "config" } },
  { label: "domain-server", args: { domain: "server" } },
  { label: "keyword-mod", args: { keyword: "mod" } },
  { label: "version-4.1", args: { version: "4.1" } },
];

const proc = spawn(process.execPath, [entry], {
  stdio: ["pipe", "pipe", "pipe"],
  cwd: process.cwd(),
});

let stdoutBuffer = "";
let stderrBuffer = "";
let nextId = 1;
const pending = new Map();

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
      continue;
    }
    if (!message || message.id === undefined) continue;
    const slot = pending.get(message.id);
    if (!slot) continue;
    pending.delete(message.id);
    if (message.error) slot.reject(new Error(`JSON-RPC 错误: ${JSON.stringify(message.error)}`));
    else slot.resolve(message);
  }
}

proc.stdout.on("data", (chunk) => {
  stdoutBuffer += chunk.toString("utf8");
  drainStdout();
});
proc.stderr.on("data", (chunk) => {
  stderrBuffer += chunk.toString("utf8");
});

function request(method, params) {
  const id = nextId++;
  return new Promise((resolvePromise, rejectPromise) => {
    pending.set(id, { resolve: resolvePromise, reject: rejectPromise });
    proc.stdin.write(JSON.stringify({ jsonrpc: "2.0", id, method, params }) + "\n");
    setTimeout(() => {
      if (pending.has(id)) {
        pending.delete(id);
        rejectPromise(new Error(`请求超时（${REQUEST_TIMEOUT_MS}ms）: ${method}`));
      }
    }, REQUEST_TIMEOUT_MS);
  });
}

function notify(method, params) {
  proc.stdin.write(JSON.stringify({ jsonrpc: "2.0", method, params }) + "\n");
}

const globalTimer = setTimeout(() => {
  process.stderr.write(`[capture-kb] 全局超时（${GLOBAL_TIMEOUT_MS}ms），终止 ${entry}\n`);
  proc.kill();
  process.exit(1);
}, GLOBAL_TIMEOUT_MS);

try {
  await request("initialize", {
    protocolVersion: "2024-11-05",
    capabilities: {},
    clientInfo: { name: "c7-capture", version: "0.0.0" },
  });
  notify("notifications/initialized", {});

  await mkdir(outDir, { recursive: true });

  let index = 0;
  for (const call of CALLS) {
    index += 1;
    const response = await request("tools/call", {
      name: "spt_kb_query",
      arguments: call.args,
    });
    const result = response.result ?? {};
    const text = Array.isArray(result.content) && result.content[0] ? result.content[0].text : "";
    let envelope;
    try {
      envelope = JSON.parse(text);
    } catch {
      envelope = { __unparsed: text };
    }
    const payload = { isError: result.isError === true, envelope };
    const file = `${outDir}/call-${index}-${call.label}.json`;
    await writeFile(file, JSON.stringify(payload, null, 2) + "\n", "utf8");

    const matchInfo =
      envelope && envelope.ok === true
        ? `matchCount=${envelope.data?.matchCount ?? "?"}`
        : `code=${envelope?.code ?? "?"}`;
    process.stdout.write(`[capture-kb] call-${index}-${call.label} -> ${matchInfo}\n`);
  }

  clearTimeout(globalTimer);
  proc.stdin.end();
  proc.kill();
  process.exit(0);
} catch (error) {
  process.stderr.write(`[capture-kb] 失败: ${error.message}\n`);
  if (stderrBuffer) process.stderr.write(`[capture-kb] 服务器 stderr:\n${stderrBuffer}\n`);
  clearTimeout(globalTimer);
  proc.kill();
  process.exit(1);
}
