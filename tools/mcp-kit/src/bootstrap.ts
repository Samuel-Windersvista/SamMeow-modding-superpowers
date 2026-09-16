// =============================================================================
// mcp-kit/bootstrap.ts — MCP stdio server 引导（三台共用）
//
// 抽取自 spt-mcp / tarkov-runtime-mcp 的双胞胎入口逻辑
// （原 tools/spt-mcp/src/index.ts:141-189；tools/tarkov-runtime-mcp/src/index.ts:292-307），
// 并补齐 mo2 所需的生命周期钩子时序。
//
// 与各台既有行为的对齐：
//   - runStdioServer 的 ListTools/CallTool 处理器、SIGINT/SIGTERM 默认处理
//     （stderr 提示 + exit 0）、`await connect` 顺序与 spt/tarkov 逐字一致。
//   - onBeforeConnect：mo2 的 lifecycle.markReady 时序（connect 之前）。
//   - onConnected：mo2 的 eager bind + ready log 时序（connect 之后）。
//   - runMain 的 invokedAsMain 判定与失败文案与 spt 一致；mo2 由无守卫改为
//     有守卫（更安全，生产路径无行为变化）。
// =============================================================================

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { pathToFileURL } from "node:url";

import type { ToolResult } from "./result.js";

/** tools/list 上线的单个工具定义（inputSchema 必须是 JSON Schema 对象） */
export interface McpToolDefinition {
  name: string;
  description: string;
  inputSchema: Record<string, unknown>;
}

export interface StdioServerOptions {
  /** server 名（同时用于关闭日志与启动失败文案前缀） */
  name: string;
  version: string;
  /** 返回当前注册的工具定义（每次 tools/list 调用） */
  listTools: () => McpToolDefinition[];
  /** 执行工具调用；返回值直接作为 CallTool 的 result */
  callTool: (
    name: string,
    args: Record<string, unknown>,
  ) => ToolResult | Promise<ToolResult>;
  /** connect 之前（mo2: lifecycle.markReady 时序） */
  onBeforeConnect?: () => void | Promise<void>;
  /** connect 之后（mo2: eager bind + ready log 时序） */
  onConnected?: () => void | Promise<void>;
}

/**
 * 启动一个 stdio MCP server 并阻塞至连接建立。
 *
 * 顺序：注册处理器 → 注册 SIGINT/SIGTERM → onBeforeConnect → connect → onConnected。
 * 默认信号处理写入 stderr 并以 exit 0 退出（与 spt/tarkov 既有行为一致）。
 */
export async function runStdioServer(options: StdioServerOptions): Promise<void> {
  const { name, version } = options;

  const server = new Server(
    { name, version },
    { capabilities: { tools: {} } },
  );

  server.setRequestHandler(ListToolsRequestSchema, async () => ({
    tools: options.listTools(),
  }));

  server.setRequestHandler(CallToolRequestSchema, async (req) => {
    const toolName = req.params.name;
    const args = (req.params.arguments ?? {}) as Record<string, unknown>;
    const result = await options.callTool(toolName, args);
    // 结构重建为匿名对象类型：SDK 的 handler 返回类型含 `{ [x: string]: unknown }`
    // 索引签名成员，而 interface 声明（ToolResult）不获得隐式索引签名，
    // 直接返回会被 TS 判为不可赋值。重建后形状与语义完全一致。
    return { content: result.content, isError: result.isError };
  });

  const shutdown = (signal: string): void => {
    process.stderr.write(`${name} 收到 ${signal}，正在关闭...\n`);
    process.exit(0);
  };
  process.on("SIGINT", () => shutdown("SIGINT"));
  process.on("SIGTERM", () => shutdown("SIGTERM"));

  await options.onBeforeConnect?.();
  await server.connect(new StdioServerTransport());
  await options.onConnected?.();
}

/**
 * 入口守卫：仅当本模块作为主入口运行时执行 main，并统一失败处理。
 *
 * metaUrl 必须由调用方传入（kit 内计算会比对错误的 URL）：
 *   runMain(import.meta.url, main, "spt-mcp")
 *
 * @param label 启动失败文案前缀；缺省 "MCP server"
 */
export function runMain(
  metaUrl: string,
  main: () => Promise<void>,
  label?: string,
): void {
  const argv = process.argv[1];
  let invokedAsMain = false;
  if (argv) {
    try {
      invokedAsMain = metaUrl === pathToFileURL(argv).href;
    } catch {
      invokedAsMain = false;
    }
  }
  if (!invokedAsMain) return;

  main().catch((error) => {
    const message = error instanceof Error ? error.message : String(error);
    process.stderr.write(`${label ?? "MCP server"} 启动失败：${message}\n`);
    process.exit(1);
  });
}
