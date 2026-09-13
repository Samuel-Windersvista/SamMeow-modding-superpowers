// =============================================================================
// tarkov-runtime-mcp — SPT 5.x 运行时状态 MCP server
//
// 首版（工单 01）工具面：
//   tarkov_server_status  连接信息 + server 版本 + BEM tag 门禁结果 + 能力自报
//   tarkov_instances      候选端口探测出的单实例信息
//   raid_status           Phase 2 占位，返回 CLIENT_BRIDGE_NOT_INSTALLED
//   raid_player           Phase 2 占位，返回 CLIENT_BRIDGE_NOT_INSTALLED
//   raid_bots             Phase 2 占位，返回 CLIENT_BRIDGE_NOT_INSTALLED
//
// 传输层（zlib / PHPSESSID / 5.0 shuffle）封装在 SptConnection 之后（S1 接缝）。
// =============================================================================

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { CallToolRequestSchema, ListToolsRequestSchema } from "@modelcontextprotocol/sdk/types.js";
import { pathToFileURL } from "node:url";
import { zodToJsonSchema } from "zod-to-json-schema";

import { SptClient } from "./client/client.js";
import { loadConfig, type TarkovRuntimeConfig } from "./config.js";
import { toErrorEnvelope } from "./errors.js";
import { HttpSptConnection } from "./transport/http-connection.js";
import type { SptConnection } from "./transport/connection.js";
import { InstancesInput, createInstancesTool } from "./tools/instances.js";
import {
  RAID_TOOL_NAMES,
  RaidPlaceholderInput,
  createRaidPlaceholderTool,
  isRaidToolName,
  raidPlaceholderEnvelope,
} from "./tools/raid.js";
import { ServerStatusInput, createServerStatusTool, type ToolHandler } from "./tools/server-status.js";
import { RUNTIME_ERROR_CODES, errEnv, type Envelope } from "./types.js";

const SERVER_NAME = "tarkov-runtime-mcp";
const SERVER_VERSION = "0.1.0";

/** zod schema -> JSON Schema（剥离 $schema 顶层键，兼容严格 schema 后端） */
function schemaFor(schema: Parameters<typeof zodToJsonSchema>[0]): Record<string, unknown> {
  const json = zodToJsonSchema(schema, { target: "jsonSchema7" }) as Record<string, unknown>;
  delete json.$schema;
  return json;
}

export const TOOL_DEFINITIONS = [
  {
    name: "tarkov_server_status",
    description:
      "连接本地 SPT server 并返回握手结果：连接信息、server 自报版本、锚定 BEM tag 门禁结果、MCP 能力自报。版本不匹配返回 VERSION_MISMATCH，不可达返回 SERVER_UNREACHABLE。",
    inputSchema: schemaFor(ServerStatusInput),
  },
  {
    name: "tarkov_instances",
    description:
      "探测候选端口（默认 6969，可经 TARKOV_RUNTIME_MCP_PORTS 覆盖），返回发现的单实例信息（host/port/baseUrl/versionLabel）。不执行版本门禁。",
    inputSchema: schemaFor(InstancesInput),
  },
  ...RAID_TOOL_NAMES.map((name) => ({
    name,
    description: `raid.* 局内状态占位工具（Phase 2 BepInEx Client Bridge）。首版固定返回 CLIENT_BRIDGE_NOT_INSTALLED。`,
    inputSchema: schemaFor(RaidPlaceholderInput),
  })),
];

const AVAILABLE_TOOLS = [
  "tarkov_server_status",
  "tarkov_instances",
  ...RAID_TOOL_NAMES,
].join(", ");

export function createDispatcher(
  client: SptClient,
): (name: string, args: Record<string, unknown>) => Promise<Envelope> {
  const handlers: Record<string, ToolHandler> = {
    tarkov_server_status: createServerStatusTool(client),
    tarkov_instances: createInstancesTool(client),
  };
  for (const name of RAID_TOOL_NAMES) {
    handlers[name] = createRaidPlaceholderTool(name);
  }

  return async function invoke(name, args) {
    const handler = handlers[name];
    if (!handler) {
      if (isRaidToolName(name)) {
        return raidPlaceholderEnvelope(name);
      }
      return errEnv(
        name,
        `未知工具：${name}`,
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        `可用工具：${AVAILABLE_TOOLS}`,
      );
    }
    try {
      return await handler(args);
    } catch (error) {
      return toErrorEnvelope(name, error);
    }
  };
}

export interface RuntimeOptions {
  config?: TarkovRuntimeConfig;
  /** 注入连接工厂（测试用） */
  connect?: (host: string, port: number) => SptConnection;
}

export function createRuntime(options: RuntimeOptions = {}) {
  const config = options.config ?? loadConfig();
  const connect = options.connect ?? ((host, port) => new HttpSptConnection(host, port));
  const client = new SptClient({
    host: config.host,
    candidatePorts: config.candidatePorts,
    anchorVersion: config.anchorVersion,
    connect,
  });
  return { client, invoke: createDispatcher(client), config };
}

function jsonResult(body: unknown, isError = false) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(body) }],
    isError,
  };
}

export async function main(): Promise<void> {
  const { invoke } = createRuntime();

  const server = new Server(
    { name: SERVER_NAME, version: SERVER_VERSION },
    { capabilities: { tools: {} } },
  );

  server.setRequestHandler(ListToolsRequestSchema, async () => ({
    tools: TOOL_DEFINITIONS,
  }));

  server.setRequestHandler(CallToolRequestSchema, async (req) => {
    const name = req.params.name;
    const args = (req.params.arguments ?? {}) as Record<string, unknown>;
    const envelope = await invoke(name, args);
    return jsonResult(envelope, !envelope.ok);
  });

  const shutdown = (signal: string) => {
    process.stderr.write(`${SERVER_NAME} 收到 ${signal}，正在关闭...\n`);
    process.exit(0);
  };
  process.on("SIGINT", () => shutdown("SIGINT"));
  process.on("SIGTERM", () => shutdown("SIGTERM"));

  await server.connect(new StdioServerTransport());
}

const invokedAsMain = (() => {
  const argv = process.argv[1];
  if (!argv) return false;
  try {
    return import.meta.url === pathToFileURL(argv).href;
  } catch {
    return false;
  }
})();

if (invokedAsMain) {
  main().catch((error) => {
    const message = error instanceof Error ? error.message : String(error);
    process.stderr.write(`${SERVER_NAME} 启动失败：${message}\n`);
    process.exit(1);
  });
}
