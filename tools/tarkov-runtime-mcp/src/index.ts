// =============================================================================
// tarkov-runtime-mcp — SPT 5.x 运行时状态 MCP server
//
// 工具面：
//   tarkov_server_status  连接信息 + server 版本 + BEM tag 门禁结果 + 能力自报
//   tarkov_instances      候选端口探测出的单实例信息
//   tarkov_snapshot       语义化状态快照（首版 profile section）
//   tarkov_wait_for       谓词轮询原语（任意工具结果，超时返回 WAIT_TIMEOUT）
//   raid_status           raid 元数据 + 桥自报（经 BridgeConnection）
//   raid_player           玩家局内全字段 + 武器/装备（经 BridgeConnection）
//   raid_bots             bot 摘要 / 明细（经 BridgeConnection）
//   raid_events           事件时间线增量拉取（经 BridgeConnection）
//
// 传输层（zlib / PHPSESSID / 5.0 shuffle）封装在 SptConnection 之后（S1 接缝）。
// 局内状态经 BridgeConnection 抽象拉取（Phase 2 唯一新接缝）。
// =============================================================================

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { CallToolRequestSchema, ListToolsRequestSchema } from "@modelcontextprotocol/sdk/types.js";
import { pathToFileURL } from "node:url";
import { zodToJsonSchema } from "zod-to-json-schema";

import { SptClient } from "./client/client.js";
import { loadConfig, DEFAULT_BRIDGE_HOST, DEFAULT_BRIDGE_PORT, type TarkovRuntimeConfig } from "./config.js";
import { toErrorEnvelope } from "./errors.js";
import { HttpBridgeConnection } from "./bridge/http-bridge-connection.js";
import { RecordingBridgeConnection } from "./bridge/recording.js";
import type { BridgeConnection } from "./bridge/connection.js";
import { HttpSptConnection } from "./transport/http-connection.js";
import type { SptConnection } from "./transport/connection.js";
import { InstancesInput, createInstancesTool } from "./tools/instances.js";
import {
  RAID_TOOL_NAMES,
  isRaidToolName,
  raidPlaceholderEnvelope,
} from "./tools/raid.js";
import {
  RAID_BOTS_TOOL_NAME,
  RaidBotsInput,
  createRaidBotsTool,
} from "./tools/raid-bots.js";
import {
  RAID_EVENTS_TOOL_NAME,
  RaidEventsInput,
  createRaidEventsTool,
} from "./tools/raid-events.js";
import {
  RAID_PLAYER_TOOL_NAME,
  RaidPlayerInput,
  createRaidPlayerTool,
} from "./tools/raid-player.js";
import {
  RAID_STATUS_TOOL_NAME,
  RaidStatusInput,
  createRaidStatusTool,
} from "./tools/raid-status.js";
import { ServerStatusInput, createServerStatusTool, type ToolHandler } from "./tools/server-status.js";
import { SNAPSHOT_TOOL_NAME, SnapshotInput, createSnapshotTool } from "./tools/snapshot.js";
import { WaitForInput, createWaitForTool } from "./tools/wait-for.js";
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
  {
    name: SNAPSHOT_TOOL_NAME,
    description:
      "读取 SPT server 局外状态并返回确定性快照。sections 可选（合法值：profile / traders / quests / hideout / inventory），缺省或空数组读取全部；各 section 返回计数型摘要并标注数据来源路由与新鲜度。未知 section 返回 UNSUPPORTED_SECTION。",
    inputSchema: schemaFor(SnapshotInput),
  },
  {
    name: "tarkov_wait_for",
    description:
      "对任意工具结果轮询求值谓词，直到满足或超时。谓词语法：`<字段路径> <运算符> <值>`，运算符含 contains / equals / matches 与数值比较（> >= < <=）；可直接用于 raid.* 工具（如 `raid_bots` 的 `alive > 5`）。满足返回求值结果与耗时；超时返回结构化 WAIT_TIMEOUT（谓词/最后观察值/耗时）。首轮即遇 BRIDGE_UNREACHABLE / CLIENT_BRIDGE_NOT_INSTALLED 时立即返回该错误（bridge 缺席不误报超时）；NOT_IN_RAID 不短路（等进 raid 是合法用法，继续轮询）。",
    inputSchema: schemaFor(WaitForInput),
  },
  {
    name: RAID_STATUS_TOOL_NAME,
    description:
      "读取当前 raid 元数据（地图/状态/剩余时间/raidId）并附带 bridge 自报（pluginVersion/protocolVersion/samplingIntervalMs，经本地 BepInEx Client Bridge 拉取）。不在 raid 返回 NOT_IN_RAID；桥不可达返回 BRIDGE_UNREACHABLE；协议版本不一致返回 BRIDGE_VERSION_MISMATCH。",
    inputSchema: schemaFor(RaidStatusInput),
  },
  {
    name: RAID_PLAYER_TOOL_NAME,
    description:
      "读取当前 raid 中玩家的实时状态（经本地 BepInEx Client Bridge 拉取）：position（x/y/z）、rotation（x/y）、pose（站/蹲/趴）、health（alive/total/各肢体）、weapon（tpl/name/ammoInMag/ammoInChamber，无武器为 null）、equipment（[{slot,tpl,name}]，无装备为空数组）、数据新鲜度 sampleAgeMs。不在 raid 返回 NOT_IN_RAID；桥不可达返回 BRIDGE_UNREACHABLE；协议版本不一致返回 BRIDGE_VERSION_MISMATCH。",
    inputSchema: schemaFor(RaidPlayerInput),
  },
  {
    name: RAID_BOTS_TOOL_NAME,
    description:
      "读取当前 raid 的 bot 状态（经本地 BepInEx Client Bridge 拉取）。默认返回摘要（total/alive/PMC-Scav-Boss-其他分类计数/生成器计数/sampleAgeMs）；detail=true 追加每个 bot 的明细（x/y/z/role/side/alive）与 truncated 截断标记。不在 raid 返回 NOT_IN_RAID；桥不可达返回 BRIDGE_UNREACHABLE；协议版本不一致返回 BRIDGE_VERSION_MISMATCH。",
    inputSchema: schemaFor(RaidBotsInput),
  },
  {
    name: RAID_EVENTS_TOOL_NAME,
    description:
      "增量拉取 raid 事件时间线（经本地 BepInEx Client Bridge 拉取）：damage（部位/伤害量/来源）、death（致死类型 + 击杀者归属，不可得为 null）、extraction（撤离点/状态）。入参 since（只返回 seq > since，缺省从最旧）与 limit（截断条数）；输出 { inRaid, seq, dropped, events }，dropped>0 表示 since 过旧已丢失事件。非 raid 时仍返回缓冲（inRaid:false，赛后时间线含撤离事件可读），故不返回 NOT_IN_RAID；桥不可达返回 BRIDGE_UNREACHABLE；协议版本不一致返回 BRIDGE_VERSION_MISMATCH。",
    inputSchema: schemaFor(RaidEventsInput),
  },
];

const AVAILABLE_TOOLS = [
  "tarkov_server_status",
  "tarkov_instances",
  SNAPSHOT_TOOL_NAME,
  "tarkov_wait_for",
  ...RAID_TOOL_NAMES,
].join(", ");

export function createDispatcher(
  client: SptClient,
  bridge: BridgeConnection = new HttpBridgeConnection(DEFAULT_BRIDGE_HOST, DEFAULT_BRIDGE_PORT),
): (name: string, args: Record<string, unknown>) => Promise<Envelope> {
  const handlers: Record<string, ToolHandler> = {
    tarkov_server_status: createServerStatusTool(client),
    tarkov_instances: createInstancesTool(client),
    [SNAPSHOT_TOOL_NAME]: createSnapshotTool(client),
    [RAID_STATUS_TOOL_NAME]: createRaidStatusTool(bridge),
    [RAID_PLAYER_TOOL_NAME]: createRaidPlayerTool(bridge),
    [RAID_BOTS_TOOL_NAME]: createRaidBotsTool(bridge),
    [RAID_EVENTS_TOOL_NAME]: createRaidEventsTool(bridge),
  };

  const invoke = async function invoke(
    name: string,
    args: Record<string, unknown>,
  ): Promise<Envelope> {
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

  // wait_for 需调用其他工具，故在 invoke 定义后注入（自引用）
  handlers.tarkov_wait_for = createWaitForTool(invoke);

  return invoke;
}

export interface RuntimeOptions {
  config?: TarkovRuntimeConfig;
  /** 注入连接工厂（测试用） */
  connect?: (host: string, port: number) => SptConnection;
  /** 注入 bridge 连接（测试用）；缺省构造 HTTP 实现 */
  bridge?: BridgeConnection;
}

export function createRuntime(options: RuntimeOptions = {}) {
  const config = options.config ?? loadConfig();
  const connect = options.connect ?? ((host, port) => new HttpSptConnection(host, port));
  const client = new SptClient({
    host: config.host,
    candidatePorts: config.candidatePorts,
    anchorVersion: config.anchorVersion,
    username: config.username,
    password: config.password,
    connect,
  });
  const baseBridge =
    options.bridge ??
    new HttpBridgeConnection(
      config.bridgeHost ?? DEFAULT_BRIDGE_HOST,
      config.bridgePort ?? DEFAULT_BRIDGE_PORT,
    );
  // T07：仅当配置提供录制路径时包装（默认关）；注入连接同样可被包装。
  const bridge = config.bridgeRecordPath
    ? new RecordingBridgeConnection(baseBridge, config.bridgeRecordPath)
    : baseBridge;
  return { client, bridge, invoke: createDispatcher(client, bridge), config };
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
